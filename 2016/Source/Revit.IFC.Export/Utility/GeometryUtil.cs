//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2015  Autodesk, Inc.
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Exporter;
using Revit.IFC.Common.Enums;


namespace Revit.IFC.Export.Utility
{
    /// <summary>
    /// Provides static methods for geometry related manipulations.
    /// </summary>
    public class GeometryUtil
    {
        /// <summary>
        /// The comparer for comparing XYZ.
        /// </summary>
        public struct XYZComparer : IEqualityComparer<XYZ>
        {
            /// <summary>
            /// Two XYZ is equal if they are almost equal.
            /// </summary>
            /// <param name="x">The XYZ.</param>
            /// <param name="y">The XYZ.</param>
            /// <returns>True if two XYZ are almost equal; false otherwise.</returns>
            public bool Equals(XYZ x, XYZ y)
            {
                return x.IsAlmostEqualTo(y);
            }
            /// <summary>
            /// Return 0 to let call Equals.
            /// </summary>
            /// <param name="obj">The XYZ.</param>
            /// <returns>0 for all XYZ.</returns>
            public int GetHashCode(XYZ obj)
            {
                return 0;
            }
        }

        /// <summary>
        /// Creates a default plane.
        /// </summary>
        /// <remarks>
        /// The origin of the plane is (0, 0, 0) and the normal is (0, 0, 1).
        /// </remarks>
        /// <returns>
        /// The Plane.
        /// </returns>
        public static Plane CreateDefaultPlane()
        {
            XYZ normal = new XYZ(0, 0, 1);
            XYZ origin = XYZ.Zero;
         return new Plane(normal, origin);
        }

        /// <summary>
        /// Gets a scaled plane from the unscaled plane.
        /// </summary>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="unscaledPlane">The unscaled plane.</param>
        /// <returns>The scaled plane.</returns>
        public static Plane GetScaledPlane(ExporterIFC exporterIFC, Plane unscaledPlane)
        {
           if (exporterIFC == null || unscaledPlane == null)
              throw new ArgumentNullException();

           XYZ scaledOrigin = ExporterIFCUtils.TransformAndScalePoint(exporterIFC, unscaledPlane.Origin);
           XYZ scaledXDir = ExporterIFCUtils.TransformAndScaleVector(exporterIFC, unscaledPlane.XVec);
           XYZ scaledYDir = ExporterIFCUtils.TransformAndScaleVector(exporterIFC, unscaledPlane.YVec);
           XYZ scaledNorm = ExporterIFCUtils.TransformAndScaleVector(exporterIFC, unscaledPlane.Normal);

           return new Plane(scaledXDir, scaledYDir, scaledOrigin);
        }

        /// <summary>
        /// Creates a scaled plane from unscaled origin, X and Y directions.
        /// </summary>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="unscaledXVec">The unscaled X direction.</param>
        /// <param name="unscaledYVec">The unscaled Y direction.</param>
        /// <param name="unscaledOrigin">The unscaled origin.</param>
        /// <returns>The scaled plane.</returns>
        public static Plane CreateScaledPlane(ExporterIFC exporterIFC, XYZ unscaledXVec, XYZ unscaledYVec, XYZ unscaledOrigin)
        {
            if (exporterIFC == null || unscaledXVec == null || unscaledYVec == null || unscaledOrigin == null)
                throw new ArgumentNullException();

            XYZ scaledXDir = ExporterIFCUtils.TransformAndScaleVector(exporterIFC, unscaledXVec);
            XYZ scaledYDir = ExporterIFCUtils.TransformAndScaleVector(exporterIFC, unscaledYVec);
         XYZ scaledNorm = scaledXDir.CrossProduct(scaledYDir);
            XYZ scaledOrigin = ExporterIFCUtils.TransformAndScalePoint(exporterIFC, unscaledOrigin);

            return new Plane(scaledXDir, scaledYDir, scaledOrigin);
        }

        /// <summary>
        /// Checks if curve is flipped to a plane.
        /// </summary>
        /// <param name="plane">
        /// The plane.
        /// </param>
        /// <param name="curve">
        /// The curve.
        /// </param>
        /// <returns>
        /// True if the curve is flipped to the plane, false otherwise.
        /// </returns>
        public static bool MustFlipCurve(Plane plane, Curve curve)
        {
            XYZ xVector = null;
            XYZ yVector = null;
            if (curve is Arc)
            {
                Arc arc = curve as Arc;
                xVector = arc.XDirection;
                yVector = arc.YDirection;
            }
            else if (curve is Ellipse)
            {
                Ellipse ellipse = curve as Ellipse;
                xVector = ellipse.XDirection;
                yVector = ellipse.YDirection;
            }
            else
                return false;

            List<double> realListX = ConvertVectorToLocalCoordinates(plane, xVector);
            List<double> realListY = ConvertVectorToLocalCoordinates(plane, yVector);

            double dot = realListY[0] * (-realListX[1]) + realListY[1] * (realListX[0]);
            if (dot < -MathUtil.Eps())
                return true;

            return false;
        }

        /// <summary>
        /// Calculates the slope of an extrusion relative to an axis.
        /// </summary>
        /// <param name="extrusionDirection">The extrusion direction.</param>
        /// <param name="axis">The axis.</param>
        /// <returns>The slope.</returns>
        /// <remarks>This is a simple routine mainly intended for beams and columns.</remarks>
        static public double GetSimpleExtrusionSlope(XYZ extrusionDirection, IFCExtrusionBasis axis)
        {
            double zOff = (axis == IFCExtrusionBasis.BasisZ) ? (1.0 - Math.Abs(extrusionDirection[2])) : Math.Abs(extrusionDirection[2]);
         double scaledAngle = UnitUtil.ScaleAngle(MathUtil.SafeAsin(zOff));
            return scaledAngle;
        }

        /// <summary>
        /// Converts vector to local coordinates of the plane.
        /// </summary>
        /// <param name="plane">
        /// The plane.
        /// </param>
        /// <param name="vector">
        /// The vector.
        /// </param>
        /// <returns>
        /// The converted values.
        /// </returns>
        public static List<double> ConvertVectorToLocalCoordinates(Plane plane, XYZ vector)
        {
            List<double> measures1 = ConvertPointToLocalCoordinatesCommon(plane, XYZ.Zero);
            List<double> measures2 = ConvertPointToLocalCoordinatesCommon(plane, vector);

            List<double> measures = new List<double>();
            for (int i = 0; i < measures1.Count; ++i)
            {
                measures.Add(measures2[i] - measures1[i]);
            }
            return measures;
        }

        /// <summary>
        /// Converts point to local coordinates of the plane.
        /// </summary>
        /// <param name="plane">
        /// The plane.
        /// </param>
        /// <param name="point">
        /// The point.
        /// </param>
        /// <returns>
        /// The converted values.
        /// </returns>
        private static List<double> ConvertPointToLocalCoordinatesCommon(Plane plane, XYZ point)
        {
            List<double> measures = new List<double>();

            if (plane != null)
            {
                XYZ xVector = plane.XVec;
                XYZ yVector = plane.YVec;
                XYZ origin = plane.Origin;

                XYZ diff = point - origin;

                measures.Add(diff.DotProduct(xVector));
                measures.Add(diff.DotProduct(yVector));
            }
            else
            {
                measures.Add(point.X);
                measures.Add(point.Y);
                measures.Add(point.Z);
            }

            return measures;
        }

        /// <summary>
        /// Obtains a new curve transformed via the indicated translation vector. 
        /// </summary>
        /// <param name="originalCurve">The curve.</param>
        /// <param name="translationVector">The translation vector.</param>
        /// <returns>The new translated curve.</returns>
        public static Curve MoveCurve(Curve originalCurve, XYZ translationVector)
        {
            Transform moveTrf = Transform.CreateTranslation(translationVector);
            return originalCurve.CreateTransformed(moveTrf);
        }

        /// <summary>
        /// Obtains a new CurveLoop transformed via the indicated translation vector. 
        /// </summary>
        /// <param name="originalCurveLoop">The curve loop.</param>
        /// <param name="translationVector">The translation vector.</param>
        /// <returns>The new translated curve loop.</returns>
        public static CurveLoop MoveCurveLoop(CurveLoop originalCurveLoop, XYZ translationVector)
        {
            Transform moveTrf = Transform.CreateTranslation(translationVector);
            CurveLoop newCurveLoop = new CurveLoop();
            foreach (Curve curve in originalCurveLoop)
                newCurveLoop.Append(MoveCurve(curve, translationVector));
            return newCurveLoop;
        }

        /// <summary>
        /// Obtains a new CurveLoop transformed via the indicated transform.
        /// </summary>
        /// <param name="originalCurveLoop">The curve loop.</param>
        /// <param name="trf">The transform.</param>
        /// <returns>The new transformed curve loop.</returns>
        public static CurveLoop TransformCurveLoop(CurveLoop originalCurveLoop, Transform trf)
        {
            CurveLoop newCurveLoop = new CurveLoop();
            foreach (Curve curve in originalCurveLoop)
                newCurveLoop.Append(curve.CreateTransformed(trf));
            return newCurveLoop;
        }

        /// <summary>
        /// Checks if curve is line or arc.
        /// </summary>
        /// <param name="curve">
        /// The curve.
        /// </param>
        /// <returns>
        /// True if the curve is line or arc, false otherwise.
        /// </returns>
        public static bool CurveIsLineOrArc(Curve curve)
        {
            return curve is Line || curve is Arc;
        }

        /// <summary>
        /// Reverses curve loop.
        /// </summary>
        /// <param name="curveloop">
        /// The curveloop.
        /// </param>
        /// <returns>
        /// The reversed curve loop.
        /// </returns>
        public static CurveLoop ReverseOrientation(CurveLoop curveloop)
        {
            CurveLoop copyOfCurveLoop = CurveLoop.CreateViaCopy(curveloop);
            copyOfCurveLoop.Flip();
            return copyOfCurveLoop;
        }

        /// <summary>
        /// Gets origin, X direction and curve bound from a curve.
        /// </summary>
        /// <param name="curve">
        /// The curve.
        /// </param>
        /// <param name="curveBounds">
        /// The output curve bounds.
        /// </param>
        /// <param name="xDirection">
        /// The output X direction.
        /// </param>
        /// <param name="origin">
        /// The output origin.
        /// </param>
        public static void GetAxisAndRangeFromCurve(Curve curve,
           out IFCRange curveBounds, out XYZ xDirection, out XYZ origin)
        {
            curveBounds = new IFCRange(curve.GetEndParameter(0), curve.GetEndParameter(1));
            origin = curve.Evaluate(curveBounds.Start, false);
            if (curve is Arc)
            {
                Arc arc = curve as Arc;
                xDirection = arc.XDirection;
            }
            else
            {
                Transform trf = curve.ComputeDerivatives(curveBounds.Start, false);
                xDirection = trf.get_Basis(0);
            }
        }

        /// <summary>
        /// Creates and returns an instance of the Options class with current view's DetailLevel or the detail level set to Fine if current view is not checked.
        /// </summary>
        public static Options GetIFCExportGeometryOptions()
        { 
            Options options = new Options();
            if (ExporterCacheManager.ExportOptionsCache.FilterViewForExport != null)
            {
                options.DetailLevel = ExporterCacheManager.ExportOptionsCache.FilterViewForExport.DetailLevel;
            }
            else
                options.DetailLevel = ViewDetailLevel.Fine;
            return options;
        }

        /// <summary>
        /// Collects all solids and meshes within a GeometryElement.
        /// </summary>
        /// <remarks>
        /// Added in 2013 to replace the temporary API method ExporterIFCUtils.GetSolidMeshGeometry.
        /// </remarks>
        /// <param name="geomElemToUse">The GeometryElement.</param>
        /// <param name="trf">The initial Transform applied on the GeometryElement.</param>
        /// <returns>The collection of solids and meshes.</returns>
        public static SolidMeshGeometryInfo GetSolidMeshGeometry(GeometryElement geomElemToUse, Transform trf)
        {
            if (geomElemToUse == null)
            {
                throw new ArgumentNullException("geomElemToUse");
            }
            SolidMeshGeometryInfo geometryInfo = new SolidMeshGeometryInfo();
            // call to recursive helper method to obtain all solid and mesh geometry within geomElemToUse
            CollectSolidMeshGeometry(geomElemToUse, trf, geometryInfo);
            return geometryInfo;
        }

        /// <summary>
        /// Collects all meshes within a GeometryElement and all solids clipped between a given IFCRange.
        /// </summary>
        /// <remarks>
        /// Added in 2013 to replace the temporary API method ExporterIFCUtils.GetClippedSolidMeshGeometry.
        /// </remarks>
        /// <param name="elem">
        /// The Element from which we can obtain a bounding box. Not handled directly in this method, it is used in an internal helper method.
        /// </param>
        /// <param name="geomElemToUse">
        /// The GeometryElement.
        /// </param>
        /// <param name="range">
        /// The upper and lower levels which act as the clipping boundaries.
        /// </param>
        /// <returns>The collection of solids and meshes.</returns>
        public static SolidMeshGeometryInfo GetClippedSolidMeshGeometry(GeometryElement geomElemToUse, IFCRange range)
        {
            SolidMeshGeometryInfo geometryInfo = GetSolidMeshGeometry(geomElemToUse, Transform.Identity);
            geometryInfo.ClipSolidsList(geomElemToUse, range);
            return geometryInfo;
        }

        /// <summary>
        /// Collects all solids and meshes within a GeometryElement; the solids which consist of multiple closed volumes
        /// will be split into single closed volume Solids.
        /// </summary>
        /// <remarks>
        /// Added in 2013 to replace the temporary API method ExporterIFCUtils.GetSplitSolidMeshGeometry.
        /// </remarks>
        /// <param name="geomElemToUse">The GeometryElement.</param>
        /// <param name="trf">The transform.</param>
        /// <returns>The collection of solids and meshes.</returns>
        public static SolidMeshGeometryInfo GetSplitSolidMeshGeometry(GeometryElement geomElemToUse, Transform trf)
        {
            SolidMeshGeometryInfo geometryInfo = GetSolidMeshGeometry(geomElemToUse, Transform.Identity);
            geometryInfo.SplitSolidsList();
            return geometryInfo;
        }
        
        /// <summary>
        /// Collects all solids and meshes within a GeometryElement; the solids which consist of multiple closed volumes
        /// will be split into single closed volume Solids.
        /// </summary>
        /// <remarks>
        /// Added in 2013 to replace the temporary API method ExporterIFCUtils.GetSplitSolidMeshGeometry.
        /// </remarks>
        /// <param name="geomElemToUse">The GeometryElement.</param>
        /// <returns>The collection of solids and meshes.</returns>
        public static SolidMeshGeometryInfo GetSplitSolidMeshGeometry(GeometryElement geomElemToUse)
        {
            return GetSplitSolidMeshGeometry(geomElemToUse, Transform.Identity);
        }

        /// <summary>
        /// Collects all solids and meshes within a GeometryElement; the solids which consist of multiple closed volumes
        /// will be split into single closed volume Solids.
        /// </summary>
        /// <remarks>
        /// Added in 2013 to replace the temporary API method ExporterIFCUtils.GetSplitClippedSolidMeshGeometry.
        /// </remarks>
        /// <param name="range">
        /// The upper and lower levels which act as the clipping boundaries.
        /// </param>
        /// <param name="geomElemToUse">The GeometryElement.</param>
        /// <returns>The collection of solids and meshes.</returns>
        public static SolidMeshGeometryInfo GetSplitClippedSolidMeshGeometry(GeometryElement geomElemToUse, IFCRange range)
        {
            SolidMeshGeometryInfo geometryInfo = GetClippedSolidMeshGeometry(geomElemToUse, range);
            geometryInfo.SplitSolidsList();
            return geometryInfo;
        }

        /// <summary>
        /// Transforms a geometry by a given transform.
        /// </summary>
        /// <remarks>The geometry element created by "GetTransformed" is a copy which will have its own allocated
        /// membership - this needs to be stored and disposed of (see AllocatedGeometryObjectCache
        /// for details)</remarks>
        /// <param name="geomElem">The geometry.</param>
        /// <param name="trf">The transform.</param>
        /// <returns>The transformed geometry.</returns>
        public static GeometryElement GetTransformedGeometry(GeometryElement geomElem, Transform trf)
        {
            if (geomElem == null)
                return null;

            GeometryElement currGeomElem = geomElem.GetTransformed(trf);
            ExporterCacheManager.AllocatedGeometryObjectCache.AddGeometryObject(currGeomElem);
            return currGeomElem;
        }

        /// <summary>
        /// Collects all solids and meshes within all nested levels of a given GeometryElement.
        /// </summary>
        /// <remarks>
        /// This is a private helper method for the GetSolidMeshGeometry type collection methods.
        /// </remarks>
        /// <param name="geomElem">The GeometryElement we are collecting solids and meshes from.</param>
        /// <param name="trf">The initial Transform applied on the GeometryElement.</param>
        /// <param name="solidMeshCapsule">The SolidMeshGeometryInfo object that contains the lists of collected solids and meshes.</param>
        private static void CollectSolidMeshGeometry(GeometryElement geomElem, Transform trf, SolidMeshGeometryInfo solidMeshCapsule)
        {
            if (geomElem == null)
                return;
            
            GeometryElement currGeomElem = geomElem;
            Transform localTrf = trf;
            if (localTrf == null)
                localTrf = Transform.Identity;
            else if (!localTrf.IsIdentity)
                currGeomElem = GetTransformedGeometry(geomElem, localTrf);
            
            // iterate through the GeometryObjects contained in the GeometryElement
            foreach (GeometryObject geomObj in currGeomElem)
            {
                Solid solid = geomObj as Solid;
                if (solid != null && solid.Faces.Size > 0)
                {
                    try
                    {
                        if (solid.Volume <= MathUtil.Eps())
                            continue;
                    }
                    catch
                    {
                        // solid.Volume can throw an exception.  In this case, we don't really care;
                        // there is geometry there, and we will export it best we can.
                    }
                    
                    solidMeshCapsule.AddSolid(solid);
                }
                else
                {
                    Mesh mesh = geomObj as Mesh;
                    if (mesh != null)
                    {
                        solidMeshCapsule.AddMesh(mesh);
                    }
                    else
                    {
                        // if the current geomObj is castable as a GeometryInstance, then we perform the same collection on its symbol geometry
                        GeometryInstance inst = geomObj as GeometryInstance;
                        if (inst != null)
                        {
                            GeometryElement instanceSymbol = inst.GetSymbolGeometry();
                            if (instanceSymbol != null && instanceSymbol.Count() != 0)
                            {
                                Transform instanceTransform = localTrf.Multiply(inst.Transform);
                                CollectSolidMeshGeometry(instanceSymbol, instanceTransform, solidMeshCapsule);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Indicates whether or not the loop has the same sense when used to bound the face as when first defined.
        /// </summary>
        /// <param name="boundary">The boundary.</param>
        /// <returns>If false the senses of all its component oriented edges are implicitly reversed when used in the face.</returns>
        public static bool BoundaryHasSameSense(IFCAnyHandle boundary)
        {
            bool? hasSameSense = IFCAnyHandleUtil.GetBooleanAttribute(boundary, "Orientation");
            return hasSameSense != null ? (bool)hasSameSense : false;
        }

        /// <summary>
        /// Gets the boundary polygon for a given boundary.
        /// </summary>
        /// <param name="boundary">The boundary.</param>
        /// <returns>The boundary curves for the polygon.</returns>
        public static List<IFCAnyHandle> GetBoundaryPolygon(IFCAnyHandle boundary)
        {
            IFCAnyHandle bound = IFCAnyHandleUtil.GetInstanceAttribute(boundary, "Bound");

            return IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(bound, "Polygon");
        }

        /// <summary>
        /// Gets the boundary handles for a given face.
        /// </summary>
        /// <param name="face">The face.</param>
        /// <returns>The boundary handles.</returns>
        public static HashSet<IFCAnyHandle> GetFaceBounds(IFCAnyHandle face)
        {
            return IFCAnyHandleUtil.GetAggregateInstanceAttribute<HashSet<IFCAnyHandle>>(face, "Bounds");
        }

        /// <summary>
        /// Gets the IfcObjectPlacement handle stored as the reference for an IfcLocalPlacement.
        /// </summary>
        /// <param name="localPlacement"></param>
        /// <returns>The IfcObjectPlacement handle.  Return can be a handle without a value, if there is no value set in the IfcLocalPlacement.</returns>
        public static IFCAnyHandle GetPlacementRelToFromLocalPlacement(IFCAnyHandle localPlacement)
        {
            return IFCAnyHandleUtil.GetInstanceAttribute(localPlacement, "PlacementRelTo");
        }

        /// <summary>
        ///  Gets the IfcAxis2Placement handle stored as the relative placement for an IfcLocalPlacement.
        /// </summary>
        /// <param name="localPlacement"> The IfcLocalPlacement handle.</param>
        /// <returns>The IfcAxis2Placement handle.</returns>
        public static IFCAnyHandle GetRelativePlacementFromLocalPlacement(IFCAnyHandle localPlacement)
        {
            return IFCAnyHandleUtil.GetInstanceAttribute(localPlacement, "RelativePlacement");
        }

        /// <summary>
        /// Gets the collection of IfcRepresentationMaps stored in an IfcTypeProduct handle.
        /// </summary>
        /// <param name="typeProduct">The IfcTypeProduct handle.</param>
        /// <returns>The representation maps.</returns>
        public static List<IFCAnyHandle> GetRepresentationMaps(IFCAnyHandle typeProduct)
        {
            return IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(typeProduct, "RepresentationMaps");
        }

        /// <summary>
        /// Adds items to a given shape handle.
        /// </summary>
        /// <param name="shape">The shape handle.</param>
        /// <param name="items">The items.</param>
        public static void AddItemsToShape(IFCAnyHandle shape, ISet<IFCAnyHandle> items)
        {
            HashSet<IFCAnyHandle> repItemSet = IFCAnyHandleUtil.GetAggregateInstanceAttribute<HashSet<IFCAnyHandle>>(shape, "Items");
            foreach (IFCAnyHandle repItem in items)
            {
                repItemSet.Add(repItem);
            }
            IFCAnyHandleUtil.SetAttribute(shape, "Items", repItemSet);
        }

        /// <summary>
        /// Sets the IfcAxis2Placement handle stored as the placement relative to for an IfcLocalPlacement.
        /// </summary>
        /// <param name="localPlacement">The IfcLocalPlacement handle.</param>
        /// <param name="newPlacementRelTo">The IfcObjectPlacement handle to use as the placement relative to.</param>
        public static void SetPlacementRelTo(IFCAnyHandle localPlacement, IFCAnyHandle newPlacementRelTo)
        {
            IFCAnyHandleUtil.SetAttribute(localPlacement, "PlacementRelTo", newPlacementRelTo);
        }

        /// <summary>
        /// Sets the IfcAxis2Placement handle stored as the relative placement for an IfcLocalPlacement.
        /// </summary>
        /// <param name="localPlacement">The IfcLocalPlacement handle.</param>
        /// <param name="newRelativePlacement">The IfcAxis2Placement handle to use as the relative placement.</param>
        public static void SetRelativePlacement(IFCAnyHandle localPlacement, IFCAnyHandle newRelativePlacement)
        {
            IFCAnyHandleUtil.SetAttribute(localPlacement, "RelativePlacement", newRelativePlacement);
        }

        /// <summary>
        /// Get geometry of one level of a potentially multi-story stair, ramp, or railing.
        /// </summary>
        /// <param name="geomElement">The original geometry.</param>
        /// <param name="numFlights">The number of stair flights, or 0 if unknown.  If there is exactly 1 flight, return the original geoemtry.</param>
        /// <returns>The geometry element.</returns>
        /// <remarks>This routine may not work properly for railings created before 2006.  If you get
        /// poor representations from such railings, please upgrade the railings if possible.</remarks>
        public static GeometryElement GetOneLevelGeometryElement(GeometryElement geomElement, int numFlights)
        {
            if (geomElement == null)
                return null;

            if (numFlights == 1)
                return geomElement;

            foreach (GeometryObject geomObject in geomElement)
            {
                if (!(geomObject is GeometryInstance))
                    continue;
                GeometryInstance geomInstance = geomObject as GeometryInstance;
                if (!MathUtil.IsAlmostZero(geomInstance.Transform.Origin.Z))
                    continue;
                Element baseSymbol = geomInstance.Symbol;
                if (!(baseSymbol is ElementType))
                    continue;
                GeometryElement symbolGeomElement = geomInstance.GetSymbolGeometry();

                // For railings created before 2006, the GeometryElement could be null.  In this case, we will use
                // a more general technique of getting geometry below, which will unfortanately result in worse
                // representations.  If this is a concern, please upgrade the railings to any format since 2006.
                if (symbolGeomElement != null)
                {
                    Transform trf = geomInstance.Transform;
                    if (trf != null && !trf.IsIdentity)
                        return symbolGeomElement.GetTransformed(trf);
                    else
                        return symbolGeomElement;
                }
            }

            return geomElement;
        }

        /// <summary>
        /// Projects a point to the closest point on a plane.
        /// </summary>
        /// <param name="plane">The plane.</param>
        /// <param name="point">The point.</param>
        /// <returns>The UV of the projected point.</returns>
        public static UV ProjectPointToPlane(Plane plane, XYZ point)
        {
            if (plane == null || point == null)
                return null;

            XYZ diff = (point - plane.Origin);
            return new UV(diff.DotProduct(plane.XVec), diff.DotProduct(plane.YVec));
        }
        
        /// <summary>
        /// Generates the UV value of a point projected to a plane, given an extrusion direction.
        /// </summary>
        /// <param name="plane">The plane.</param>
        /// <param name="projDir">The projection direction.</param>
        /// <param name="point">The point.</param>
        /// <returns>The UV value.</returns>
        public static UV ProjectPointToPlane(Plane plane, XYZ projDir, XYZ point)
        {
            XYZ zDir = plane.Normal;

            double denom = projDir.DotProduct(zDir);
            if (MathUtil.IsAlmostZero(denom))
                return null;

            XYZ xDir = plane.XVec;
            XYZ yDir = plane.YVec;
            XYZ orig = plane.Origin;

            double distToPlane = ((orig - point).DotProduct(zDir)) / denom;
            XYZ pointProj = distToPlane * projDir + point;
            XYZ pointProjOffset = pointProj - orig;
            UV pointProjUV = new UV(pointProjOffset.DotProduct(xDir), pointProjOffset.DotProduct(yDir));
            return pointProjUV;
        }

        /// <summary>
        /// Specifies the types of curves found in a boundary curve loop.
        /// </summary>
        public enum FaceBoundaryType
        {
            Polygonal,  // all curves are line segments.
            LinesAndArcs, // all curves are line segments or arcs.
            Complex // some curves are neither line segments nor arcs.
        }

        private static CurveLoop GetFaceBoundary(Face face, EdgeArray faceBoundary, XYZ baseLoopOffset,
            bool polygonalOnly, out FaceBoundaryType faceBoundaryType)
        {
            faceBoundaryType = FaceBoundaryType.Polygonal;
            CurveLoop currLoop = new CurveLoop();
            foreach (Edge faceBoundaryEdge in faceBoundary)
            {
                Curve edgeCurve = faceBoundaryEdge.AsCurveFollowingFace(face);
                Curve offsetCurve = (baseLoopOffset != null) ? MoveCurve(edgeCurve, baseLoopOffset) : edgeCurve;
                if (!(offsetCurve is Line))
                {
                    if (polygonalOnly)
                    {
                        IList<XYZ> tessPts = offsetCurve.Tessellate();
                        int numTessPts = tessPts.Count;
                        for (int ii = 0; ii < numTessPts - 1; ii++)
                        {
                            Line line = Line.CreateBound(tessPts[ii], tessPts[ii + 1]);
                            currLoop.Append(line);
                        }
                    }
                    else
                    {
                        currLoop.Append(offsetCurve);
                    }

                    if (offsetCurve is Arc)
                        faceBoundaryType = FaceBoundaryType.LinesAndArcs;
                    else
                        faceBoundaryType = FaceBoundaryType.Complex;
                }
                else
                    currLoop.Append(offsetCurve);
            }
            return currLoop;
        }

        /// <summary>
        /// Gets the outer and inner boundaries of a Face as CurveLoops.
        /// </summary>
        /// <param name="face">The face.</param>
        /// <param name="baseLoopOffset">The amount to translate the origin of the face plane.  This is used if the start of the extrusion
        /// is offset from the base face.  The argument is null otherwise.</param>
        /// <param name="faceBoundaryTypes">Returns whether the boundaries consist of lines only, lines and arcs, or complex curves.</param>
        /// <returns>1 outer and 0 or more inner curve loops corresponding to the face boundaries.</returns>
        public static IList<CurveLoop> GetFaceBoundaries(Face face, XYZ baseLoopOffset, out IList<FaceBoundaryType> faceBoundaryTypes)
        {
            faceBoundaryTypes = new List<FaceBoundaryType>();

            EdgeArrayArray faceBoundaries = face.EdgeLoops;
            IList<CurveLoop> extrusionBoundaryLoops = new List<CurveLoop>();
            foreach (EdgeArray faceBoundary in faceBoundaries)
            {
                FaceBoundaryType currFaceBoundaryType;
                CurveLoop currLoop = GetFaceBoundary(face, faceBoundary, baseLoopOffset, false, out currFaceBoundaryType);
                faceBoundaryTypes.Add(currFaceBoundaryType);
                extrusionBoundaryLoops.Add(currLoop);
            }
            return extrusionBoundaryLoops;
        }

        private static CurveLoop GetOuterFaceBoundary(Face face, XYZ baseLoopOffset, bool polygonalOnly, out FaceBoundaryType faceBoundaryType)
        {
            faceBoundaryType = FaceBoundaryType.Polygonal;

            EdgeArrayArray faceBoundaries = face.EdgeLoops;
            foreach (EdgeArray faceOuterBoundary in faceBoundaries)
                return GetFaceBoundary(face, faceOuterBoundary, baseLoopOffset, polygonalOnly, out faceBoundaryType);
            return null;
        }

        /// <summary>
        /// Group the extra faces in the extrusion by element id, representing clippings, recesses, and openings.
        /// </summary>
        /// <param name="elem">The element generating the base extrusion.</param>
        /// <param name="analyzer">The extrusion analyzer.</param>
        /// <returns>A list of connected faces for each element id that cuts the extrusion</returns>
        public static IDictionary<ElementId, ICollection<ICollection<Face>>> GetCuttingElementFaces(Element elem, ExtrusionAnalyzer analyzer)
        {
            IDictionary<ElementId, HashSet<Face>> cuttingElementFaces = new Dictionary<ElementId, HashSet<Face>>();

            IDictionary<Face, ExtrusionAnalyzerFaceAlignment> allFaces = analyzer.CalculateFaceAlignment();
            foreach (KeyValuePair<Face, ExtrusionAnalyzerFaceAlignment> currFace in allFaces)
            {
                if (currFace.Value == ExtrusionAnalyzerFaceAlignment.FullyAligned)
                    continue;

                EdgeArrayArray faceEdges = currFace.Key.EdgeLoops;
                int numBoundaries = faceEdges.Size;
                if (numBoundaries == 0)
                    continue;
                if (numBoundaries > 1)
                    throw new Exception("Can't handle faces with interior boundaries.");

                ICollection<ElementId> generatingElementIds = elem.GetGeneratingElementIds(currFace.Key);
                foreach (ElementId generatingElementId in generatingElementIds)
                {
                    HashSet<Face> elementFaces;
                    if (cuttingElementFaces.ContainsKey(generatingElementId))
                    {
                        elementFaces = cuttingElementFaces[generatingElementId];
                    }
                    else
                    {
                        elementFaces = new HashSet<Face>();
                        cuttingElementFaces[generatingElementId] = elementFaces;
                    }
                    elementFaces.Add(currFace.Key);
                }
            }

            IDictionary<ElementId, ICollection<ICollection<Face>>> cuttingElementFaceCollections =
                new Dictionary<ElementId, ICollection<ICollection<Face>>>();
            foreach (KeyValuePair<ElementId, HashSet<Face>> cuttingElementFaceCollection in cuttingElementFaces)
            {
                ICollection<ICollection<Face>> faceCollections = new List<ICollection<Face>>();
                // Split into separate collections based on connectivity.
                while (cuttingElementFaceCollection.Value.Count > 0)
                {
                    IList<Face> currCollection = new List<Face>();
                    IEnumerator<Face> cuttingElementFaceCollectionEnumerator = cuttingElementFaceCollection.Value.GetEnumerator();
                    cuttingElementFaceCollectionEnumerator.MoveNext();
                    Face currFace = cuttingElementFaceCollectionEnumerator.Current;
                    currCollection.Add(currFace);
                    cuttingElementFaceCollection.Value.Remove(currFace);

                    IList<Face> facesToProcess = new List<Face>();
                    facesToProcess.Add(currFace);

                    if (cuttingElementFaceCollection.Value.Count > 0)
                    {
                        while (facesToProcess.Count > 0)
                        {
                            currFace = facesToProcess[0];
                            EdgeArray faceOuterBoundary = currFace.EdgeLoops.get_Item(0);

                            foreach (Edge edge in faceOuterBoundary)
                            {
                                Face adjoiningFace = edge.GetFace(1);
                                if (adjoiningFace.Equals(currFace))
                                    adjoiningFace = edge.GetFace(0);

                                if (cuttingElementFaceCollection.Value.Contains(adjoiningFace))
                                {
                                    currCollection.Add(adjoiningFace);
                                    cuttingElementFaceCollection.Value.Remove(adjoiningFace);
                                    facesToProcess.Add(adjoiningFace);
                                }
                            }

                            facesToProcess.Remove(facesToProcess[0]);
                        }
                    }

                    faceCollections.Add(currCollection);
                }

                cuttingElementFaceCollections[cuttingElementFaceCollection.Key] = faceCollections;
            }

            return cuttingElementFaceCollections;
        }

        private static IFCRange GetExtrusionRangeOfCurveLoop(CurveLoop loop, XYZ extrusionDirection)
        {
            IFCRange range = new IFCRange();
            bool init = false;
            foreach (Curve curve in loop)
            {
                if (!init)
                {
                    if (curve.IsBound)
                    {
                        IList<XYZ> coords = curve.Tessellate();
                        foreach (XYZ coord in coords)
                        {
                            double val = coord.DotProduct(extrusionDirection);
                            if (!init)
                            {
                                range.Start = val;
                                range.End = val;
                                init = true;
                            }
                            else
                            {
                                range.Start = Math.Min(range.Start, val);
                                range.End = Math.Max(range.End, val);
                            }
                        }
                    }
                    else
                    {
                        double val = curve.GetEndPoint(0).DotProduct(extrusionDirection);
                        range.Start = val;
                        range.End = val;
                        init = true;
                    }
                }
                else
                {
                    double val = curve.GetEndPoint(0).DotProduct(extrusionDirection);
                    range.Start = Math.Min(range.Start, val);
                    range.End = Math.Max(range.End, val);
                }
            }
            return range;
        }

        private static bool IsInRange(IFCRange range, CurveLoop loop, Plane plane, XYZ extrusionDirection, out bool clipCompletely)
        {
            clipCompletely = false;
            if (range != null)
            {
                // This check is only applicable for cuts that are perpendicular to the extrusion direction.
                // For cuts that aren't, we can't easily tell if this cut is extraneous or not.
                if (!MathUtil.IsAlmostEqual(Math.Abs(plane.Normal.DotProduct(extrusionDirection)), 1.0))
                    return true;

                double eps = MathUtil.Eps();

                double parameterValue = plane.Origin.DotProduct(extrusionDirection);

                if (range.Start > parameterValue - eps)
                {
                    clipCompletely = true;
                    return false;
                }
                if (range.End < parameterValue + eps)
                    return false;
            }

            return true;
        }

        private static IList<UV> TransformAndProjectCurveLoopToPlane(ExporterIFC exporterIFC, CurveLoop loop, Plane projScaledPlane)
        {
            IList<UV> uvs = new List<UV>();

            XYZ projDir = projScaledPlane.Normal;
            foreach (Curve curve in loop)
            {
                XYZ point = curve.GetEndPoint(0);
                XYZ scaledPoint = ExporterIFCUtils.TransformAndScalePoint(exporterIFC, point);

                UV scaledUV = ProjectPointToPlane(projScaledPlane, projDir, scaledPoint);
                uvs.Add(scaledUV);
            }
            return uvs;
        }

        private static int GetNumberOfFaceBoundaries(Face face)
        {
            return face.EdgeLoops.Size;
        }

        // return null if parent should be completely clipped.
        // TODO: determine whether or not to use face boundary.
        private static IFCAnyHandle ProcessClippingFace(ExporterIFC exporterIFC, CurveLoop outerBoundary, Plane boundaryPlane, 
            Plane extrusionBasePlane, XYZ extrusionDirection, IFCRange range, bool useFaceBoundary, IFCAnyHandle bodyItemHnd)
        {
            if (outerBoundary == null || boundaryPlane == null)
                throw new Exception("Invalid face boundary.");

            double clippingSlant = boundaryPlane.Normal.DotProduct(extrusionDirection);
            if (useFaceBoundary)
            {
                if (MathUtil.IsAlmostZero(clippingSlant))
                    return bodyItemHnd;
            }

            bool clipCompletely;
            if (!IsInRange(range, outerBoundary, boundaryPlane, extrusionDirection, out clipCompletely))
                return clipCompletely ? null : bodyItemHnd;

            if (MathUtil.IsAlmostZero(clippingSlant))
                throw new Exception("Can't create clipping perpendicular to extrusion.");

            IFCFile file = exporterIFC.GetFile();

            XYZ scaledOrig = ExporterIFCUtils.TransformAndScalePoint(exporterIFC, boundaryPlane.Origin);
            XYZ scaledNorm = ExporterIFCUtils.TransformAndScaleVector(exporterIFC, boundaryPlane.Normal);
            XYZ scaledXDir = ExporterIFCUtils.TransformAndScaleVector(exporterIFC, boundaryPlane.XVec);

            IFCAnyHandle planeAxisHnd = ExporterUtil.CreateAxis(file, scaledOrig, scaledNorm, scaledXDir);
            IFCAnyHandle surfHnd = IFCInstanceExporter.CreatePlane(file, planeAxisHnd);

            IFCAnyHandle clippedBodyItemHnd = null;
            IFCAnyHandle halfSpaceHnd = null;

            if (useFaceBoundary)
            {
                IFCAnyHandle boundedCurveHnd;
                if (boundaryPlane != null)
                {
                    XYZ projScaledOrigin = ExporterIFCUtils.TransformAndScalePoint(exporterIFC, extrusionBasePlane.Origin);
                    XYZ projScaledX = ExporterIFCUtils.TransformAndScaleVector(exporterIFC, extrusionBasePlane.XVec);
                    XYZ projScaledY= ExporterIFCUtils.TransformAndScaleVector(exporterIFC, extrusionBasePlane.YVec);
                    XYZ projScaledNorm = projScaledX.CrossProduct(projScaledY);
                    
               Plane projScaledPlane = new Plane(projScaledX, projScaledY, projScaledOrigin);

                    IList<UV> polylinePts = TransformAndProjectCurveLoopToPlane(exporterIFC, outerBoundary, projScaledPlane);
                    polylinePts.Add(polylinePts[0]);
                    boundedCurveHnd = ExporterUtil.CreatePolyline(file, polylinePts);

                    IFCAnyHandle boundedAxisHnd = ExporterUtil.CreateAxis(file, projScaledOrigin, projScaledNorm, projScaledX);

                    halfSpaceHnd = IFCInstanceExporter.CreatePolygonalBoundedHalfSpace(file, boundedAxisHnd, boundedCurveHnd, surfHnd, false);
                }
                else
                {
                    throw new Exception("Can't create non-polygonal face boundary.");
                }
            }
            else
            {
                halfSpaceHnd = IFCInstanceExporter.CreateHalfSpaceSolid(file, surfHnd, false);
            }

            if (halfSpaceHnd == null)
                throw new Exception("Can't create clipping.");

            if (IFCAnyHandleUtil.IsSubTypeOf(bodyItemHnd, IFCEntityType.IfcBooleanClippingResult) || 
                IFCAnyHandleUtil.IsSubTypeOf(bodyItemHnd, IFCEntityType.IfcSweptAreaSolid))
                clippedBodyItemHnd = IFCInstanceExporter.CreateBooleanClippingResult(file, IFCBooleanOperator.Difference,
                    bodyItemHnd, halfSpaceHnd);
            else
                clippedBodyItemHnd = IFCInstanceExporter.CreateBooleanResult(file, IFCBooleanOperator.Difference,
                    bodyItemHnd, halfSpaceHnd);

            return clippedBodyItemHnd;
        }

        // returns true if either, but not both, the start or end of the extrusion is clipped.
        private static KeyValuePair<bool, bool> CollectionClipsExtrusionEnds(IList<CurveLoop> curveLoopBoundaries, XYZ extrusionDirection, 
            IFCRange extrusionRange)
        {
            bool clipStart = false;
            bool clipEnd = false;
            double eps = MathUtil.Eps();

            foreach (CurveLoop curveLoop in curveLoopBoundaries)
            {
                IFCRange loopRange = GetExtrusionRangeOfCurveLoop(curveLoop, extrusionDirection);
                if (loopRange.End >= extrusionRange.End - eps)
                    clipEnd = true;
                if (loopRange.Start <= extrusionRange.Start + eps)
                    clipStart = true;
            }

            KeyValuePair<bool, bool> clipResults = new KeyValuePair<bool, bool>(clipStart, clipEnd);
            return clipResults;
        }

        private static bool CreateOpeningForCategory(Element cuttingElement)
        {
            ElementId categoryId = cuttingElement.Category.Id;
            return (categoryId == new ElementId(BuiltInCategory.OST_Doors) ||
                categoryId == new ElementId(BuiltInCategory.OST_Windows));
        }

        /// <summary>
        /// Attempts to create a clipping, recess, or opening from a collection of faces.
        /// </summary>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="cuttingElement">The cutting element.  This will help determine whether to use a clipping or opening in boundary cases.</param>
        /// <param name="extrusionBasePlane">The plane of the extrusion base.</param>
        /// <param name="extrusionDirection">The extrusion direction.</param>
        /// <param name="faces">The collection of faces.</param>
        /// <param name="range">The valid range of the extrusion.</param>
        /// <param name="origBodyRepHnd">The original body representation.</param>
        /// <param name="skippedFaces">The faces that weren't handled by this operation.  These may represent openings.</param>
        /// <returns>The new body representation.  If the clipping completely clips the extrusion, this will be null.  Otherwise, this
        /// will be the clipped representation if a clipping was done, or the original representation if not.</returns>
        public static IFCAnyHandle CreateClippingFromFaces(ExporterIFC exporterIFC, Element cuttingElement, Plane extrusionBasePlane,
            XYZ extrusionDirection, ICollection<Face> faces, IFCRange range, IFCAnyHandle origBodyRepHnd, out ICollection<Face> skippedFaces)
        {
            skippedFaces = null;

            if (IFCAnyHandleUtil.IsNullOrHasNoValue(origBodyRepHnd))
                return null;

            bool polygonalOnly = ExporterCacheManager.ExportOptionsCache.ExportAs2x2;

            IList<CurveLoop> outerCurveLoops = new List<CurveLoop>();
            IList<Plane> outerCurveLoopPlanes = new List<Plane>();
            IList<bool> boundaryIsPolygonal = new List<bool>();

            bool allPlanes = true;
            UV faceOriginUV = new UV(0, 0);
            foreach (Face face in faces)
            {
                FaceBoundaryType faceBoundaryType;
                CurveLoop curveLoop = GetOuterFaceBoundary(face, null, polygonalOnly, out faceBoundaryType);
                outerCurveLoops.Add(curveLoop);
                boundaryIsPolygonal.Add(faceBoundaryType == FaceBoundaryType.Polygonal);

                if (face is PlanarFace)
                {
                    PlanarFace planarFace = face as PlanarFace;
                    XYZ faceOrigin = planarFace.Origin;
                    XYZ faceNormal = planarFace.ComputeNormal(faceOriginUV);

               Plane plane = new Plane(faceNormal, faceOrigin);
                    outerCurveLoopPlanes.Add(plane);

                    if (!curveLoop.IsCounterclockwise(faceNormal))
                        curveLoop.Flip();
                }
                else
                {
                    outerCurveLoopPlanes.Add(null);
                    allPlanes = false;
                }
            }

            // We only currently handle all planar faces.
            if (allPlanes)
            {
                int numFaces = faces.Count;
                    
                // Special case: one face is a clip plane.
                if (numFaces == 1)
                {
                    return ProcessClippingFace(exporterIFC, outerCurveLoops[0], outerCurveLoopPlanes[0], extrusionBasePlane,
                        extrusionDirection, range, false, origBodyRepHnd);
                }

                KeyValuePair<bool, bool> clipsExtrusionEnds = CollectionClipsExtrusionEnds(outerCurveLoops, extrusionDirection, range);
                if (clipsExtrusionEnds.Key == true || clipsExtrusionEnds.Value == true)
                {
                    // Don't clip for a door, window or opening.
                    if (CreateOpeningForCategory(cuttingElement))
                        throw new Exception("Unhandled opening.");

                    ICollection<int> facesToSkip = new HashSet<int>();
                    bool clipStart = (clipsExtrusionEnds.Key == true);
                    bool clipBoth = (clipsExtrusionEnds.Key == true && clipsExtrusionEnds.Value == true);
                    if (!clipBoth)
                    {
                        for (int ii = 0; ii < numFaces; ii++)
                        {
                            double slant = outerCurveLoopPlanes[ii].Normal.DotProduct(extrusionDirection);
                            if (!MathUtil.IsAlmostZero(slant))
                            {
                                if (clipStart && (slant > 0.0))
                                    throw new Exception("Unhandled clip plane direction.");
                                if (!clipStart && (slant < 0.0))
                                    throw new Exception("Unhandled clip plane direction.");
                            }
                            else
                            {
                                facesToSkip.Add(ii);
                            }
                        }
                    }
                    else       
                    {
                        // If we are clipping both the start and end of the extrusion, we have to make sure all of the clipping
                        // planes have the same a non-negative dot product relative to one another.
                        int clipOrientation = 0;
                        for (int ii = 0; ii < numFaces; ii++)
                        {
                            double slant = outerCurveLoopPlanes[ii].Normal.DotProduct(extrusionDirection);
                            if (!MathUtil.IsAlmostZero(slant))
                            {
                                if (slant > 0.0)
                                {
                                    if (clipOrientation < 0)
                                        throw new Exception("Unhandled clipping orientations.");
                                    clipOrientation = 1;
                                }
                                else
                                {
                                    if (clipOrientation > 0)
                                        throw new Exception("Unhandled clipping orientations.");
                                    clipOrientation = -1;
                                }
                            }
                            else
                            {
                                facesToSkip.Add(ii);
                            }
                        }
                    }

                    IFCAnyHandle newBodyRepHnd = origBodyRepHnd;
                    skippedFaces = new HashSet<Face>();

                    // faces is an ICollection, so we can't index it.  Instead, we will keep the count ourselves.
                    int faceIdx = -1;   // start at -1 so that first increment puts faceIdx at 0.
                    foreach (Face face in faces)
                    {
                        faceIdx++;

                        if (facesToSkip.Contains(faceIdx))
                    {
                            skippedFaces.Add(face);
                            continue;
                        }

                        newBodyRepHnd = ProcessClippingFace(exporterIFC, outerCurveLoops[faceIdx], outerCurveLoopPlanes[faceIdx], 
                            extrusionBasePlane, extrusionDirection, range, true, newBodyRepHnd);
                        if (newBodyRepHnd == null)
                            return null;
                    }
                    return newBodyRepHnd;
                }
            }

            //not handled
            throw new Exception("Unhandled clipping.");
        }

        public static IFCAnyHandle CreateOpeningFromFaces(ExporterIFC exporterIFC, Element cuttingElement, Plane extrusionBasePlane,
            XYZ extrusionDirection, ICollection<Face> faces, IFCRange range, IFCAnyHandle origBodyRepHnd)
        {
            // If we have no faces, return the original value.
            if (faces.Count == 0)
                return origBodyRepHnd;

            // We will attempt to "sew" the faces, and see what we have left over.  Depending on what we have, we have an opening, recess, or clipping.

            // top and bottom profile curves to create extrusion
            IDictionary<Face, IList<Curve>> boundaryCurves = new Dictionary<Face, IList<Curve>>();
            // curves on same side face to check if they are valid for extrusion
            IDictionary<Face, IList<Curve>> boundaryCurvesInSameExistingFace = new Dictionary<Face, IList<Curve>>();

            foreach (Face face in faces)
            {
                EdgeArrayArray faceBoundaries = face.EdgeLoops;
                // We only know how to deal with the outer loop; we'll throw if we have multiple boundaries.
                if (faceBoundaries.Size != 1)
                    throw new Exception("Can't process faces with inner boundaries.");

                EdgeArray faceBoundary = faceBoundaries.get_Item(0);
                foreach (Edge edge in faceBoundary)
                {
                    Face face1 = edge.GetFace(0);
                    Face face2 = edge.GetFace(1);
                    Face missingFace = null;
                    Face existingFace = null;
                    if (!faces.Contains(face1))
                    {
                        missingFace = face1;
                        existingFace = face2;
                    }
                    else if (!faces.Contains(face2))
                    {
                        missingFace = face2;
                        existingFace = face1;
                    }

                    if (missingFace != null)
                    {
                        Curve curve = edge.AsCurve();
                        if (!boundaryCurves.ContainsKey(missingFace))
                            boundaryCurves[missingFace] = new List<Curve>();
                        boundaryCurves[missingFace].Add(curve);
                        if (!boundaryCurvesInSameExistingFace.ContainsKey(existingFace))
                            boundaryCurvesInSameExistingFace[existingFace] = new List<Curve>();
                        boundaryCurvesInSameExistingFace[existingFace].Add(curve);
                    }
                }
            }

            //might be recess
            if (boundaryCurves.Count == 1)
            {
                // boundaryCurves contains one curve loop of top profile of an extrusion
                // try to find bottom profile
                IList<Curve> curves1 = boundaryCurves.Values.ElementAt(0);
                CurveLoop curveloop = CurveLoop.Create(curves1);

                // find the parallel face
                XYZ normal = curveloop.GetPlane().Normal;

                PlanarFace recessFace = null;
                foreach (Face face in faces)
                {
                    PlanarFace planarFace = face as PlanarFace;
                    if (planarFace != null && MathUtil.VectorsAreParallel(planarFace.FaceNormal, normal))
                    {
                        if (recessFace == null)
                            recessFace = planarFace;
                        else
                            throw new Exception("Can't handle.");
                    }
                }

                if (recessFace != null)
                {
                    EdgeArrayArray edgeLoops = recessFace.EdgeLoops;
                    if (edgeLoops.Size != 1)
                        throw new Exception("Can't handle.");

                    EdgeArray edges = edgeLoops.get_Item(0);

                    IList<Edge> recessFaceEdges = new List<Edge>();
                    foreach (Edge edge in edges)
                    {
                        Face sideFace = edge.GetFace(0);
                        if (sideFace == recessFace)
                            sideFace = edge.GetFace(1);

                        // there should be already one exist during above processing
                        if (!boundaryCurvesInSameExistingFace.ContainsKey(sideFace))
                            throw new Exception("Can't handle.");
                        boundaryCurvesInSameExistingFace[sideFace].Add(edge.AsCurve());
                    }
                }
            }
            else if (boundaryCurves.Count == 2)
            {
                // might be an internal opening, process them later
                // do nothing now
            }
            else if (boundaryCurves.Count == 3) //might be an opening on an edge
            {
                IList<Curve> curves1 = boundaryCurves.Values.ElementAt(0);
                IList<Curve> curves2 = boundaryCurves.Values.ElementAt(1);
                IList<Curve> curves3 = boundaryCurves.Values.ElementAt(2);

                IList<Curve> firstValidCurves = null;
                IList<Curve> secondValidCurves = null;

                PlanarFace face1 = boundaryCurves.Keys.ElementAt(0) as PlanarFace;
                PlanarFace face2 = boundaryCurves.Keys.ElementAt(1) as PlanarFace;
                PlanarFace face3 = boundaryCurves.Keys.ElementAt(2) as PlanarFace;

                if (face1 == null || face2 == null || face3 == null)
                {
                    //Error
                    throw new Exception("Can't handle.");
                }

                Face removedFace = null;

                // find two parallel faces
                if (MathUtil.VectorsAreParallel(face1.FaceNormal, face2.FaceNormal))
                {
                    firstValidCurves = curves1;
                    secondValidCurves = curves2;
                    removedFace = face3;
                }
                else if (MathUtil.VectorsAreParallel(face1.FaceNormal, face3.FaceNormal))
                {
                    firstValidCurves = curves1;
                    secondValidCurves = curves3;
                    removedFace = face2;
                }
                else if (MathUtil.VectorsAreParallel(face2.FaceNormal, face3.FaceNormal))
                {
                    firstValidCurves = curves2;
                    secondValidCurves = curves3;
                    removedFace = face1;
                }

                // remove the third one and its edge curves
                if (removedFace != null)
                {
                    foreach (Curve curve in boundaryCurves[removedFace])
                    {
                        foreach (KeyValuePair<Face, IList<Curve>> faceEdgePair in boundaryCurvesInSameExistingFace)
                        {
                            if (faceEdgePair.Value.Contains(curve))
                                boundaryCurvesInSameExistingFace[faceEdgePair.Key].Remove(curve);
                        }
                    }
                    boundaryCurves.Remove(removedFace);
                }

                // sew, “closing” them with a simple line
                IList<IList<Curve>> curvesCollection = new List<IList<Curve>>();
                if (firstValidCurves != null)
                    curvesCollection.Add(firstValidCurves);
                if (secondValidCurves != null)
                    curvesCollection.Add(secondValidCurves);

                foreach (IList<Curve> curves in curvesCollection)
                {
                    if (curves.Count < 2) //not valid
                        throw new Exception("Can't handle.");

                    XYZ end0 = curves[0].GetEndPoint(0);
                    XYZ end1 = curves[0].GetEndPoint(1);

                    IList<Curve> processedCurves = new List<Curve>();
                    processedCurves.Add(curves[0]);
                    curves.Remove(curves[0]);

                    Curve nextCurve = null;
                    Curve preCurve = null;

                    // find the end points on the edges not connected
                    while (curves.Count > 0)
                    {
                        foreach (Curve curve in curves)
                        {
                            XYZ curveEnd0 = curve.GetEndPoint(0);
                            XYZ curveEnd1 = curve.GetEndPoint(1);
                            if (end1.IsAlmostEqualTo(curveEnd0))
                            {
                                nextCurve = curve;
                                end1 = curveEnd1;
                                break;
                            }
                            else if (end0.IsAlmostEqualTo(curveEnd1))
                            {
                                preCurve = curve;
                                end0 = curveEnd0;
                                break;
                            }
                        }

                        if (nextCurve != null)
                        {
                            processedCurves.Add(nextCurve);
                            curves.Remove(nextCurve);
                            nextCurve = null;
                        }
                        else if (preCurve != null)
                        {
                            processedCurves.Insert(0, preCurve);
                            curves.Remove(preCurve);
                            preCurve = null;
                        }
                        else
                            throw new Exception("Can't process edges.");
                    }

                    // connect them with a simple line
                    Curve newCurve = Line.CreateBound(end1, end0);
                    processedCurves.Add(newCurve);
                    if (!boundaryCurvesInSameExistingFace.ContainsKey(removedFace))
                        boundaryCurvesInSameExistingFace[removedFace] = new List<Curve>();
                    boundaryCurvesInSameExistingFace[removedFace].Add(newCurve);
                    foreach (Curve curve in processedCurves)
                    {
                        curves.Add(curve);
                    }
                }
            }
            else
                throw new Exception("Can't handle.");

            // now we should have 2 boundary curve loops
            IList<Curve> firstCurves = boundaryCurves.Values.ElementAt(0);
            IList<Curve> secondCurves = boundaryCurves.Values.ElementAt(1);
            PlanarFace firstFace = boundaryCurves.Keys.ElementAt(0) as PlanarFace;
            PlanarFace secondFace = boundaryCurves.Keys.ElementAt(1) as PlanarFace;

            if (firstFace == null || secondFace == null)
            {
                //Error, can't handle this
                throw new Exception("Can't handle.");
            }

            if (firstCurves.Count != secondCurves.Count)
            {
                //Error, can't handle this
                throw new Exception("Can't handle.");
            }

            CurveLoop curveLoop1 = null;
            CurveLoop curveLoop2 = null;

            SortCurves(firstCurves);
            curveLoop1 = CurveLoop.Create(firstCurves);
            SortCurves(secondCurves);
            curveLoop2 = CurveLoop.Create(secondCurves);

            if (curveLoop1.IsOpen() || curveLoop2.IsOpen() || !curveLoop1.HasPlane() || !curveLoop2.HasPlane())
            {
                //Error, can't handle this
                throw new Exception("Can't handle.");
            }

            Plane plane1 = curveLoop1.GetPlane();
            Plane plane2 = curveLoop2.GetPlane();

            if (!curveLoop1.IsCounterclockwise(plane1.Normal))
            {
                curveLoop1.Flip();
            }

            if (!curveLoop2.IsCounterclockwise(plane2.Normal))
            {
                curveLoop2.Flip();
            }

            // Check that the faces are planar and parallel to each other.
            // If the faces have normals that are parallel to the extrusion direction, it could be better to either:
            // 1. export the opening as an IfcOpeningElement, or
            // 2. add the boundary as an inner boundary of the extrusion.
            // as these would be less sensitive to Boolean operation errors.
            // This will be considered for a future improvement.

            if (!MathUtil.VectorsAreParallel(plane1.Normal, plane2.Normal))
            {
                //Error, can't handle this
                throw new Exception("Can't handle.");
            }

            //get the distance
            XYZ origDistance = plane1.Origin - plane2.Origin;
            double planesDistance = Math.Abs(origDistance.DotProduct(plane1.Normal));

            // check the curves on top and bottom profiles are “identical”
            foreach (KeyValuePair<Face, IList<Curve>> faceEdgeCurvePair in boundaryCurvesInSameExistingFace)
            {
                IList<Curve> curves = faceEdgeCurvePair.Value;
                if (curves.Count != 2)
                {
                    //Error, can't handle this
                    throw new Exception("Can't handle.");
                }

                Curve edgeCurve1 = curves[0];
                Curve edgeCurve2 = curves[1];
                Face sideFace = faceEdgeCurvePair.Key;

                if (!MathUtil.IsAlmostEqual(edgeCurve1.GetEndPoint(0).DistanceTo(edgeCurve2.GetEndPoint(1)), planesDistance)
                  || !MathUtil.IsAlmostEqual(edgeCurve1.GetEndPoint(1).DistanceTo(edgeCurve2.GetEndPoint(0)), planesDistance))
                {
                    //Error, can't handle this
                    throw new Exception("Can't handle.");
                }

                if (edgeCurve1 is Line)
                {
                    if (!(edgeCurve2 is Line) || !(sideFace is PlanarFace))
                    {
                        //Error, can't handle this
                        throw new Exception("Can't handle.");
                    }
                }
                else if (edgeCurve1 is Arc)
                {
                    if (!(edgeCurve2 is Arc) || !(sideFace is CylindricalFace))
                    {
                        //Error, can't handle this
                        throw new Exception("Can't handle.");
                    }

                    Arc arc1 = edgeCurve1 as Arc;
                    Arc arc2 = edgeCurve2 as Arc;

                    if (!MathUtil.IsAlmostEqual(arc1.Center.DistanceTo(arc2.Center), planesDistance))
                    {
                        //Error, can't handle this
                        throw new Exception("Can't handle.");
                    }

                    XYZ sideFaceAxis = (sideFace as CylindricalFace).Axis;
                    if (!MathUtil.VectorsAreOrthogonal(sideFaceAxis, extrusionDirection))
                    {
                        throw new Exception("Can't handle.");
                    }
                }
                else if (edgeCurve1 is Ellipse)
                {
                    if (!(edgeCurve2 is Ellipse) || !(sideFace is RuledFace) || !(sideFace as RuledFace).RulingsAreParallel)
                    {
                        //Error, can't handle this
                        throw new Exception("Can't handle.");
                    }

                    Ellipse ellipse1 = edgeCurve1 as Ellipse;
                    Ellipse ellipse2 = edgeCurve2 as Ellipse;

                    if (!MathUtil.IsAlmostEqual(ellipse1.Center.DistanceTo(ellipse2.Center), planesDistance)
                        || !MathUtil.IsAlmostEqual(ellipse1.RadiusX, ellipse2.RadiusX) || !MathUtil.IsAlmostEqual(ellipse1.RadiusY, ellipse2.RadiusY))
                    {
                        //Error, can't handle this
                        throw new Exception("Can't handle.");
                    }
                }
                else if (edgeCurve1 is HermiteSpline)
                {
                    if (!(edgeCurve2 is HermiteSpline) || !(sideFace is RuledFace) || !(sideFace as RuledFace).RulingsAreParallel)
                    {
                        //Error, can't handle this
                        throw new Exception("Can't handle.");
                    }

                    HermiteSpline hermiteSpline1 = edgeCurve1 as HermiteSpline;
                    HermiteSpline hermiteSpline2 = edgeCurve2 as HermiteSpline;

                    IList<XYZ> controlPoints1 = hermiteSpline1.ControlPoints;
                    IList<XYZ> controlPoints2 = hermiteSpline2.ControlPoints;

                    int controlPointCount = controlPoints1.Count;
                    if (controlPointCount != controlPoints2.Count)
                    {
                        //Error, can't handle this
                        throw new Exception("Can't handle.");
                    }

                    for (int i = 0; i < controlPointCount; i++)
                    {
                        if (!MathUtil.IsAlmostEqual(controlPoints1[i].DistanceTo(controlPoints2[controlPointCount - i - 1]), planesDistance))
                        {
                            //Error, can't handle this
                            throw new Exception("Can't handle.");
                        }
                    }

                    DoubleArray parameters1 = hermiteSpline1.Parameters;
                    DoubleArray parameters2 = hermiteSpline1.Parameters;

                    int parametersCount = parameters1.Size;
                    if (parametersCount != parameters2.Size)
                    {
                        //Error, can't handle this
                        throw new Exception("Can't handle.");
                    }

                    for (int i = 0; i < parametersCount; i++)
                    {
                        if (!MathUtil.IsAlmostEqual(parameters1.get_Item(i), parameters2.get_Item(i)))
                        {
                            //Error, can't handle this
                            throw new Exception("Can't handle.");
                        }
                    }
                }
                else if (edgeCurve1 is NurbSpline)
                {
                    if (!(edgeCurve2 is NurbSpline) || !(sideFace is RuledFace) || !(sideFace as RuledFace).RulingsAreParallel)
                    {
                        //Error, can't handle this
                        throw new Exception("Can't handle.");
                    }

                    NurbSpline nurbSpline1 = edgeCurve1 as NurbSpline;
                    NurbSpline nurbSpline2 = edgeCurve2 as NurbSpline;

                    IList<XYZ> controlPoints1 = nurbSpline1.CtrlPoints;
                    IList<XYZ> controlPoints2 = nurbSpline2.CtrlPoints;

                    int controlPointCount = controlPoints1.Count;
                    if (controlPointCount != controlPoints2.Count)
                    {
                        //Error, can't handle this
                        throw new Exception("Can't handle.");
                    }

                    for (int i = 0; i < controlPointCount; i++)
                    {
                        if (!MathUtil.IsAlmostEqual(controlPoints1[i].DistanceTo(controlPoints2[controlPointCount - i - 1]), planesDistance))
                        {
                            //Error, can't handle this
                            throw new Exception("Can't handle.");
                        }
                    }

                    DoubleArray weights1 = nurbSpline1.Weights;
                    DoubleArray weights2 = nurbSpline2.Weights;

                    int weightsCount = weights1.Size;
                    if (weightsCount != weights2.Size)
                    {
                        //Error, can't handle this
                        throw new Exception("Can't handle.");
                    }

                    for (int i = 0; i < weightsCount; i++)
                    {
                        if (!MathUtil.IsAlmostEqual(weights1.get_Item(i), weights2.get_Item(i)))
                        {
                            //Error, can't handle this
                            throw new Exception("Can't handle.");
                        }
                    }

                    DoubleArray knots1 = nurbSpline1.Knots;
                    DoubleArray knots2 = nurbSpline2.Knots;

                    int knotsCount = knots1.Size;
                    if (knotsCount != knots2.Size)
                    {
                        //Error, can't handle this
                        throw new Exception("Can't handle.");
                    }

                    for (int i = 0; i < knotsCount; i++)
                    {
                        if (!MathUtil.IsAlmostEqual(knots1.get_Item(i), knots2.get_Item(i)))
                        {
                            //Error, can't handle this
                            throw new Exception("Can't handle.");
                        }
                    }
                }
                else
                {
                    //Error, can't handle this
                    throw new Exception("Can't handle.");
                }
            }

            XYZ extDir = plane2.Origin - plane1.Origin;
            XYZ plane1Normal = plane1.Normal;
            int vecParallel = MathUtil.VectorsAreParallel2(extDir, plane1Normal);
            if (vecParallel == 1)
            {
                extDir = plane1Normal;
            }
            else if (vecParallel == -1)
            {
                extDir = -plane1Normal;
            }
            else
                throw new Exception("Can't handle.");

            IList<CurveLoop> origCurveLoops = new List<CurveLoop>();
            origCurveLoops.Add(curveLoop1);

            double scaledPlanesDistance = UnitUtil.ScaleLength(planesDistance);
            IFCAnyHandle extrusionHandle = ExtrusionExporter.CreateExtrudedSolidFromCurveLoop(exporterIFC, null, origCurveLoops, plane1, extDir, scaledPlanesDistance);

            IFCAnyHandle booleanBodyItemHnd = IFCInstanceExporter.CreateBooleanResult(exporterIFC.GetFile(), IFCBooleanOperator.Difference,
                origBodyRepHnd, extrusionHandle);

            return booleanBodyItemHnd;
        }

        /// <summary>
        /// Common method to create a poly line segment for an IfcCompositeCurve.
        /// </summary>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="points">The line points.</param>
        /// <returns>The handle.</returns>
        static IFCAnyHandle CreatePolyLineSegmentCommon(ExporterIFC exporterIFC, IList<XYZ> points)
        {
            if (exporterIFC == null || points == null)
                throw new ArgumentNullException();

            int count = points.Count;
            if (count < 2)
                throw new InvalidOperationException("Invalid polyline.");

            bool isClosed = points[0].IsAlmostEqualTo(points[count - 1]);
            if (isClosed)
                count--;

            if (count < 2)
                throw new InvalidOperationException("Invalid polyline.");

            IFCFile file = exporterIFC.GetFile();
            List<IFCAnyHandle> polyLinePoints = new List<IFCAnyHandle>();
            for (int ii = 0; ii < count; ii++)
            {
                XYZ point = ExporterIFCUtils.TransformAndScalePoint(exporterIFC, points[ii]);
                IFCAnyHandle pointHandle = ExporterUtil.CreateCartesianPoint(file, point);
                polyLinePoints.Add(pointHandle);
            }

            if (isClosed)
                polyLinePoints.Add(polyLinePoints[0]);

            return IFCInstanceExporter.CreatePolyline(file, polyLinePoints);
        }

        /// <summary>
        /// Creates an IFC line segment for an IfcCompositeCurve from a Revit line object.
        /// </summary>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="line">The line.</param>
        /// <returns>The line handle.</returns>
        public static IFCAnyHandle CreateLineSegment(ExporterIFC exporterIFC, Line line)
        {
            List<XYZ> points = new List<XYZ>();
            points.Add(line.GetEndPoint(0));
            points.Add(line.GetEndPoint(1));
            return CreatePolyLineSegmentCommon(exporterIFC, points);
        }

        /// <summary>
        /// Returns an Ifc handle corresponding to the curve segment after processing its bounds, if any.
        /// </summary>
        /// <param name="file">The IFCFile handle.</param>
        /// <param name="curveHnd">The unbounded curve handle.</param>
        /// <param name="curve">The Revit curve to process.</param>
        /// <returns>The original handle if the curve is unbound, or a new IfcTrimmedCurve handle if bounded.</returns>
        /// <remarks>This routine expects that bounded curves are periodic curves with a periodicity of 2*PI.</remarks>
        private static IFCAnyHandle CreateBoundsIfNecessary(IFCFile file, IFCAnyHandle curveHnd, Curve curve)
        {
            if (!curve.IsBound)
                return curveHnd;

            if (!curve.IsCyclic || !MathUtil.IsAlmostEqual(curve.Period, 2 * Math.PI))
                throw new InvalidOperationException("Expected periodic curve with period of 2*PI.");

            double endParam0 = curve.GetEndParameter(0);
            double endParam1 = curve.GetEndParameter(1);

            IFCData firstParam = IFCDataUtil.CreateAsParameterValue(UnitUtil.ScaleAngle(MathUtil.PutInRange(endParam0, Math.PI, 2 * Math.PI)));
            IFCData secondParam = IFCDataUtil.CreateAsParameterValue(UnitUtil.ScaleAngle(MathUtil.PutInRange(endParam1, Math.PI, 2 * Math.PI)));

            // todo: check that firstParam != secondParam.
            HashSet<IFCData> trim1 = new HashSet<IFCData>();
            trim1.Add(firstParam);
            HashSet<IFCData> trim2 = new HashSet<IFCData>();
            trim2.Add(secondParam);

            return IFCInstanceExporter.CreateTrimmedCurve(file, curveHnd, trim1, trim2, true, IFCTrimmingPreference.Parameter);
        }

        /// <summary>
        /// Creates an IFC arc segment for an IfcCompositeCurve from a Revit arc object.
        /// </summary>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="arc">The arc.</param>
        /// <returns>The arc handle.</returns>
        public static IFCAnyHandle CreateArcSegment(ExporterIFC exporterIFC, Arc arc)
        {
            IFCFile file = exporterIFC.GetFile();

            XYZ centerPoint = ExporterIFCUtils.TransformAndScalePoint(exporterIFC, arc.Center);

            IFCAnyHandle centerPointHandle = ExporterUtil.CreateCartesianPoint(file, centerPoint);

            XYZ xDirection = ExporterIFCUtils.TransformAndScaleVector(exporterIFC, arc.XDirection);
            IFCAnyHandle axis = ExporterUtil.CreateAxis2Placement3D(file, centerPoint, arc.Normal, xDirection);

            double arcRadius = UnitUtil.ScaleLength(arc.Radius);

            IFCAnyHandle circle = IFCInstanceExporter.CreateCircle(file, axis, arcRadius);
            return CreateBoundsIfNecessary(file, circle, arc);
        }

        /// <summary>
        /// Creates an IFC ellipse segment for an IfcCompositeCurve from a Revit ellipse object.
        /// </summary>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="ellipticalArc">The elliptical arc.</param>
        /// <returns>The ellipse handle.</returns>
        public static IFCAnyHandle CreateEllipticalArcSegment(ExporterIFC exporterIFC, Ellipse ellipticalArc)
        {
            IFCFile file = exporterIFC.GetFile();

            XYZ centerPoint = ExporterIFCUtils.TransformAndScalePoint(exporterIFC, ellipticalArc.Center);

            IFCAnyHandle centerPointHandle = ExporterUtil.CreateCartesianPoint(file, centerPoint);

            XYZ xDirection = ExporterIFCUtils.TransformAndScaleVector(exporterIFC, ellipticalArc.XDirection);
            IFCAnyHandle axis = ExporterUtil.CreateAxis2Placement3D(file, centerPoint, ellipticalArc.Normal, xDirection);

            double ellipseRadiusX = UnitUtil.ScaleLength(ellipticalArc.RadiusX);
            double ellipseRadiusY = UnitUtil.ScaleLength(ellipticalArc.RadiusY);

            IFCAnyHandle ellipse = IFCInstanceExporter.CreateEllipse(file, axis, ellipseRadiusX, ellipseRadiusY);
            return CreateBoundsIfNecessary(file, ellipse, ellipticalArc);
        }
        
        /// <summary>
        /// Creates an IFC composite curve from an array of curves.
        /// </summary>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="curves">The curves.</param>
        /// <returns>The IfcCompositeCurve handle.</returns>
        /// <remarks>This function tessellates all curve types except lines, arcs, and ellipses.</remarks>
        public static IFCAnyHandle CreateCompositeCurve(ExporterIFC exporterIFC, IList<Curve> curves)
        {
            IFCFile file = exporterIFC.GetFile();
            List<IFCAnyHandle> segments = new List<IFCAnyHandle>();
            foreach (Curve curve in curves)
            {
                if (curve == null)
                    continue;

                IFCAnyHandle curveHandle = null;
                if (curve is Line)
                {
                    curveHandle = CreateLineSegment(exporterIFC, curve as Line);
                }
                else if (curve is Arc)
                {
                    curveHandle = CreateArcSegment(exporterIFC, curve as Arc);
                }
                else if (curve is Ellipse)
                {
                    curveHandle = CreateEllipticalArcSegment(exporterIFC, curve as Ellipse);
                }
                else
                {
                    IList<XYZ> points = curve.Tessellate();
                    curveHandle = CreatePolyLineSegmentCommon(exporterIFC, points);
                }

                if (!IFCAnyHandleUtil.IsNullOrHasNoValue(curveHandle))
                {
                    segments.Add(IFCInstanceExporter.CreateCompositeCurveSegment(file, IFCTransitionCode.Continuous, true, curveHandle));
                }
            }

            if (segments.Count > 0)
            {
                return IFCInstanceExporter.CreateCompositeCurve(file, segments, IFCLogical.False);
            }

            return null;
        }

        /// <summary>
        /// Create an IfcSweptDiskSolid from a base curve.
        /// </summary>
        /// <param name="exporterIFC">The exporterIFC class.</param>
        /// <param name="file">The IFCFile.</param>
        /// <param name="centerCurve">The directrix of the sweep.</param>
        /// <param name="radius">The outer radius.</param>
        /// <param name="innerRadius">The optional inner radius.</param>
        /// <returns>The IfcSweptDiskSolid.</returns>
        public static IFCAnyHandle CreateSweptDiskSolid(ExporterIFC exporterIFC, IFCFile file, Curve centerCurve, double radius, double? innerRadius)
        {
            if (centerCurve == null || radius < MathUtil.Eps() || (innerRadius.HasValue && innerRadius.Value > radius - MathUtil.Eps()))
                return null;

            IList<Curve> curves = new List<Curve>();
            double endParam = 0.0;
            if (centerCurve is Arc || centerCurve is Ellipse)
            {
                if (centerCurve.IsBound)
                    endParam = UnitUtil.ScaleAngle(centerCurve.GetEndParameter(1) - centerCurve.GetEndParameter(0));
                else
                    endParam = UnitUtil.ScaleAngle(2 * Math.PI);
            }
            else
                endParam = 1.0;
            curves.Add(centerCurve);

            IFCAnyHandle compositeCurve = GeometryUtil.CreateCompositeCurve(exporterIFC, curves);
            return IFCInstanceExporter.CreateSweptDiskSolid(file, compositeCurve, radius, innerRadius, 0, endParam);
        }


        /// <summary>
        /// Sorts curves to allow CurveLoop creation that means each curve end must meet next curve start.
        /// </summary>
        /// <param name="curves">The curves.</param>
        public static void SortCurves(IList<Curve> curves)
        {
            IList<Curve> sortedCurves = new List<Curve>();
            Curve currentCurve = curves[0];
            sortedCurves.Add(currentCurve);

            bool found = false;

            do 
            {
                found = false;
                for (int i = 1; i < curves.Count; i++)
                {
                    Curve curve = curves[i];
                    if (currentCurve.GetEndPoint(1).IsAlmostEqualTo(curve.GetEndPoint(0)))
                    {
                        sortedCurves.Add(curve);
                        currentCurve = curve;
                        found = true;
                        break;
                    }
                }
            } while (found);

            if (sortedCurves.Count != curves.Count)
                throw new InvalidOperationException("Failed to sort curves.");

            // add back
            curves.Clear();
            foreach (Curve curve in sortedCurves)
            {
                curves.Add(curve);
            }
        }

        /// <summary>
        /// Get the color RGB values from color integer value.
        /// </summary>
        /// <param name="color">The color integer value</param>
        /// <param name="blueValue">The blue value.</param>
        /// <param name="greenValue">The green value.</param>
        /// <param name="redValue">The red value.</param>
        public static void GetRGBFromIntValue(int color, out double blueValue, out double greenValue, out double redValue)
        {
            blueValue = ((double)((color & 0xff0000) >> 16)) / 255.0;
            greenValue = ((double)((color & 0xff00) >> 8)) / 255.0;
            redValue = ((double)(color & 0xff)) / 255.0;
        }

        /// <summary>
        /// Gets bounding box of geometries.
        /// </summary>
        /// <param name="geometryList">The geometries.</param>
        /// <returns>The bounding box.</returns>
        public static BoundingBoxXYZ GetBBoxOfGeometries(IList<GeometryObject> geometryList)
        {
            BoundingBoxXYZ bbox = new BoundingBoxXYZ();
            bool bboxIsSet = false;
            bbox.Min = new XYZ(1, 1, 1);
            bbox.Max = new XYZ(0, 0, 0);

            foreach (GeometryObject geomObject in geometryList)
            {
                BoundingBoxXYZ localBbox = null;
                if (geomObject is GeometryElement)
                {
                    localBbox = (geomObject as GeometryElement).GetBoundingBox();
                }
                else if (geomObject is Solid)
                {                    
                    localBbox = (geomObject as Solid).GetBoundingBox();
                }

                if (localBbox != null)
                {
                    Transform trf = localBbox.Transform;

                    XYZ origMin = bbox.Min;
                    XYZ origMax = bbox.Max;
                    XYZ localMin = trf.OfPoint(localBbox.Min);
                    XYZ localMax = trf.OfPoint(localBbox.Max);
                    if (bboxIsSet)
                    {
                        double newCornerX, newCornerY, newCornerZ;
                        newCornerX = Math.Min(origMin.X, localMin.X);
                        newCornerY = Math.Min(origMin.Y, localMin.Y);
                        newCornerZ = Math.Min(origMin.Z, localMin.Z);
                        bbox.Min = new XYZ(newCornerX, newCornerY, newCornerZ);
                        newCornerX = Math.Max(origMax.X, localMax.X);
                        newCornerY = Math.Max(origMax.Y, localMax.Y);
                        newCornerZ = Math.Max(origMax.Z, localMax.Z);
                        bbox.Max = new XYZ(newCornerX, newCornerY, newCornerZ);
                    }
                    else
                    {
                        bboxIsSet = true;
                        bbox.Min = localMin;
                        bbox.Max = localMax;
                    }
                }
            }

            if (!bboxIsSet)
                return null;

            if (bbox.Min.X > bbox.Max.X || bbox.Min.Y > bbox.Max.Y || bbox.Min.Z > bbox.Max.Z)
                return null;

            return bbox;
        }

        /// <summary>
        /// Gets a scaled transform.
        /// </summary>
        /// <param name="exporterIFC">The exporter.</param>
        /// <returns>The transform.</returns>
        public static Transform GetScaledTransform(ExporterIFC exporterIFC)
        {
            XYZ scaledOrigin = ExporterIFCUtils.TransformAndScalePoint(exporterIFC, XYZ.Zero);
            XYZ scaledXDir = ExporterIFCUtils.TransformAndScaleVector(exporterIFC, XYZ.BasisX);
            XYZ scaledYDir = ExporterIFCUtils.TransformAndScaleVector(exporterIFC, XYZ.BasisY);
            XYZ scaledZDir = ExporterIFCUtils.TransformAndScaleVector(exporterIFC, XYZ.BasisZ);

            Transform scaledTrf = Transform.Identity;
            scaledTrf.Origin = scaledOrigin;
            scaledTrf.BasisX = scaledXDir;
            scaledTrf.BasisY = scaledYDir;
            scaledTrf.BasisZ = scaledZDir;

            return scaledTrf;
        }

        /// <summary>
        /// Gets ratios of a direction.
        /// </summary>
        /// <param name="dirHandle">The direction handle.</param>
        /// <returns>The XYZ represents the ratios.</returns>
        public static XYZ GetDirectionRatios(IFCAnyHandle dirHandle)
        {
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(dirHandle))
            {
                List<double> ratios = IFCAnyHandleUtil.GetAggregateDoubleAttribute<List<double>>(dirHandle, "DirectionRatios");
                int size = ratios.Count;
                double x = size > 0 ? ratios[0] : 0;
                double y = size > 1 ? ratios[1] : 0;
                double z = size > 2 ? ratios[2] : 0;
                return new XYZ(x, y, z);
            }
            return null;
        }

        /// <summary>
        /// Gets coordinates of a point.
        /// </summary>
        /// <param name="cartesianPoint">The point handle.</param>
        /// <returns>The XYZ represents coordinates.</returns>
        public static XYZ GetCoordinates(IFCAnyHandle cartesianPoint)
        {
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(cartesianPoint))
            {
                List<double> ratios = IFCAnyHandleUtil.GetAggregateDoubleAttribute<List<double>>(cartesianPoint, "Coordinates");
                int size = ratios.Count;
                double x = size > 0 ? ratios[0] : 0;
                double y = size > 1 ? ratios[1] : 0;
                double z = size > 2 ? ratios[2] : 0;
                return new XYZ(x, y, z);
            }
            return null;
        }

        /// <summary>
        /// Checks if a CurveLoop is inside another CurveLoop.
        /// </summary>
        /// <param name="innerLoop">The inner loop.</param>
        /// <param name="outterLoop">The outter loop.</param>
        /// <returns>True if the CurveLoop is inside the other CurveLoop.</returns>
        public static bool CurveLoopsInside(CurveLoop innerLoop, CurveLoop outterLoop)
        {
            if (innerLoop == null || outterLoop == null)
                return false;

            if (!innerLoop.HasPlane() || !outterLoop.HasPlane() || outterLoop.IsOpen())
                return false;

            XYZ outterOrigin = outterLoop.GetPlane().Origin;
            XYZ outterNormal = outterLoop.GetPlane().Normal;

            foreach (Curve innerCurve in innerLoop)
            {
                XYZ innerCurveEnd0 = innerCurve.GetEndPoint(0);

                XYZ outterOriginToEnd0 = innerCurveEnd0 - outterOrigin;
                if (!MathUtil.VectorsAreOrthogonal(outterOriginToEnd0, outterNormal))
                    return false;

                Line line0 = Line.CreateBound(innerCurveEnd0, outterOrigin);
                foreach (Curve outterCurve in outterLoop)
                {
                    SetComparisonResult result = line0.Intersect(outterCurve);
                    if (result == SetComparisonResult.Overlap)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if a CurveLoop intersects with another CurveLoop.
        /// </summary>
        /// <param name="loop1">The CurveLoop.</param>
        /// <param name="loop2">The CurveLoop.</param>
        /// <returns>True if the CurveLoop intersects with the other CurveLoop.</returns>
        public static bool CurveLoopsIntersect(CurveLoop loop1, CurveLoop loop2)
        {
            if (loop1 == null || loop2 == null)
                return false;

            foreach (Curve curve1 in loop1)
            {
                foreach (Curve curve2 in loop2)
                {
                    SetComparisonResult result = curve1.Intersect(curve2);
                    if (result != SetComparisonResult.Overlap)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Computes the height and width of a CurveLoop.
        /// </summary>
        /// <param name="curveLoop">The CurveLoop.</param>
        /// <param name="plane">The plane.</param>
        /// <param name="height">The height.</param>
        /// <param name="width">The width.</param>
        /// <returns>True if gets the values successfully.</returns>
        public static bool ComputeHeightWidthOfCurveLoop(CurveLoop curveLoop, Plane plane, out double height, out double width)
        {
            height = width = 0;

            if (plane == null)
            {
                if (curveLoop.HasPlane())
                    plane = curveLoop.GetPlane();
                else
                    return false;
            }

            if (curveLoop.IsRectangular(plane))
            {
                height = curveLoop.GetRectangularHeight(plane);
                width = curveLoop.GetRectangularWidth(plane);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Computes the area defined by a polygonal loop.
        /// </summary>
        /// <param name="loop">The polygonal loop.</param>
        /// <param name="normal">The normal of the face.</param>
        /// <param name="refPoint">Reference point for area computation.</param>
        /// <returns>The area.</returns>
        public static double ComputePolygonalLoopArea(IList<XYZ> loop, XYZ normal, XYZ refPoint)
        {
            double area = 0.0;
            int numVertices = loop.Count;
            for (int ii = 0; ii < numVertices; ii++)
            {
                XYZ currEdge = loop[(ii + 1) % numVertices] - loop[ii];
                double length = currEdge.GetLength();

                XYZ heightVec = normal.CrossProduct(currEdge).Normalize();
                XYZ otherEdge = refPoint - loop[ii];
                double height = heightVec.DotProduct(otherEdge);
                area += (length * height);
            }
            return area / 2.0;
        }

        /// <summary>
        /// Splits a Solid into distinct volumes.
        /// </summary>
        /// <param name="solid">The initial solid.</param>
        /// <returns>The list of volumes.</returns>
        /// <remarks>This calls the internal SolidUtils.SplitVolumes routine, but does additional cleanup work to properly dispose of stale data.</remarks>
        public static IList<Solid> SplitVolumes(Solid solid)
        {
         IList<Solid> splitVolumes = null;
         try
         {
            splitVolumes = SolidUtils.SplitVolumes(solid);
            foreach (Solid currSolid in splitVolumes)
            {
                // The geometry element created by SplitVolumes is a copy which will have its own allocated
                // membership - this needs to be stored and disposed of (see AllocatedGeometryObjectCache
                // for details)
                ExporterCacheManager.AllocatedGeometryObjectCache.AddGeometryObject(currSolid);
            }
         }
         catch
         {
            // Split volumes can fail; in this case, we'll export the original solid.
            splitVolumes = new List<Solid>();
            splitVolumes.Add(solid);
         }
            return splitVolumes;
        }


        /// <summary>
        /// Creates IFC curve from curve loop.
        /// </summary>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="curveLoop">The curve loop.</param>
        /// <param name="plane">The plane on which the curves are projected.</param>
        /// <param name="projDir">The project direction.</param>
        /// <returns>The created curve.</returns>
        public static IFCAnyHandle CreateIFCCurveFromCurveLoop(ExporterIFC exporterIFC, CurveLoop curveLoop, Plane plane, XYZ projDir)
        {
            IFCFile file = exporterIFC.GetFile();

            List<IFCAnyHandle> segments = new List<IFCAnyHandle>();
            List<UV> polylinePts = new List<UV>(); // for simple case

            bool useSimpleBoundary = false;
            if (!AllowComplexBoundary(plane, projDir, curveLoop, null, exporterIFC.ExportAs2x2))
                useSimpleBoundary = true;

            foreach (Curve curve in curveLoop)
            {
                bool success = ProcessCurve(exporterIFC, curve, plane, projDir, useSimpleBoundary,
                 polylinePts, segments);
                if (!success)
                    return null;
            }

            bool needToClose = false;
            if (useSimpleBoundary)
            {
                int sz = polylinePts.Count;
                if (sz < 2)
                    return null;

                if (!curveLoop.IsOpen())
                {
                    polylinePts.RemoveAt(sz - 1);
                    needToClose = true;
                }
            }

            return CreateCurveFromComponents(file, useSimpleBoundary, needToClose, polylinePts, segments);
        }

        /// <summary>
        /// Creates IFC curve from curve array.
        /// </summary>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="curves">The curves.</param>
        /// <param name="plane">The plane on which the curves are projected.</param>
        /// <param name="projDir">The project direction.</param>
        /// <returns>The created curve.</returns>
        public static IFCAnyHandle CreateIFCCurveFromCurves(ExporterIFC exporterIFC, IList<Curve> curves, Plane plane, XYZ projDir)
        {
            IFCFile file = exporterIFC.GetFile();

            List<IFCAnyHandle> segments = new List<IFCAnyHandle>();
            List<UV> polylinePts = new List<UV>(); // for simple case

            bool useSimpleBoundary = false;
            if (!AllowComplexBoundary(plane, projDir, null, curves, exporterIFC.ExportAs2x2))
                useSimpleBoundary = true;

            foreach (Curve curve in curves)
            {
                bool success = ProcessCurve(exporterIFC, curve, plane, projDir, useSimpleBoundary,
                 polylinePts, segments);
                if (!success)
                    return null;
            }

            bool needToClose = false;
            if (useSimpleBoundary)
            {
                int polySz = polylinePts.Count;
                if (polySz > 2)
                {
                    if (MathUtil.IsAlmostEqual(polylinePts[0][0], polylinePts[polySz - 1][0]) && MathUtil.IsAlmostEqual(polylinePts[0][1], polylinePts[polySz - 1][1]))
                    {
                        needToClose = true;
                        polylinePts.RemoveAt(polySz - 1);
                    }
                }
            }

            return CreateCurveFromComponents(file, useSimpleBoundary, needToClose, polylinePts, segments);
        }

        /// <summary>
        /// Gets polyline points or curve segments from a curve.
        /// </summary>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="curve">The curve.</param>
        /// <param name="plane">The plane on which the curve is projected.</param>
        /// <param name="projectDir">The project direction.</param>
        /// <param name="useSimpleBoundary">True if to create tessellated curve, false to create segments.</param>
        /// <param name="polylinePoints">The polyline points get from the curve.</param>
        /// <param name="curveSegments">The curve segments get from the curve.</param>
        /// <returns>True if process successfully.</returns>
        public static bool ProcessCurve(ExporterIFC exporterIFC, Curve curve, Plane plane, XYZ projectDir, bool useSimpleBoundary,
            List<UV> polylinePoints, List<IFCAnyHandle> curveSegments)
        {
            IFCFile file = exporterIFC.GetFile();

            bool exportAs2x2 = exporterIFC.ExportAs2x2;

            if (!useSimpleBoundary)
            {
                XYZ zDir = plane.Normal;
                if (exportAs2x2 && !MathUtil.IsAlmostEqual(Math.Abs(zDir.DotProduct(projectDir)), 1.0))
                {
                    useSimpleBoundary = true;
                }
            }

            if (useSimpleBoundary)
            {
                IList<UV> currPts = new List<UV>();

                IList<XYZ> points = curve.Tessellate();
                foreach (XYZ point in points)
                {
                    UV projectPoint = GeometryUtil.ProjectPointToPlane(plane, projectDir, point);
                    currPts.Add(UnitUtil.ScaleLength(projectPoint));
                }

                if (polylinePoints.Count > 0)
                {
                    if (currPts.Count > 1)
                    {
                        currPts.RemoveAt(0);
                    }
                }
                polylinePoints.AddRange(currPts);
            }
            else
            {
                IFCGeometryInfo info = IFCGeometryInfo.CreateCurveGeometryInfo(exporterIFC, plane, projectDir, false);
                ExporterIFCUtils.CollectGeometryInfo(exporterIFC, info, curve, XYZ.Zero, false);
                IList<IFCAnyHandle> curves = info.GetCurves();
                if (curves.Count != 1 || !IFCAnyHandleUtil.IsSubTypeOf(curves[0], IFCEntityType.IfcBoundedCurve))
                    return false;

                IFCAnyHandle boundedCurve = curves[0];

                bool mustFlip = MustFlipCurve(plane, curve);
                curveSegments.Add(IFCInstanceExporter.CreateCompositeCurveSegment(file, IFCTransitionCode.Continuous, !mustFlip, boundedCurve));
            }

            return true;
        }

        /// <summary>
        /// Checks if complex boundary is allowed for the CurveLoop or the curve array. 
        /// </summary>
        /// <param name="plane">The plane to project on.</param>
        /// <param name="projDir">The project direction.</param>
        /// <param name="curveLoop">The curve loop.</param>
        /// <param name="curves">The curve array.</param>
        /// <param name="exportAs2x2">True to export as IFC2x2.</param>
        /// <returns>True if complex boundary is allowed.</returns>
        static bool AllowComplexBoundary(Plane plane, XYZ projDir, CurveLoop curveLoop, IList<Curve> curves, bool exportAs2x2)
        {
            XYZ zDir = plane.Normal;
            if (exportAs2x2 && !MathUtil.IsAlmostEqual(Math.Abs(zDir.DotProduct(projDir)), 1.0))
            {
                return false;
            }

            if (curveLoop != null)
            {
                bool allLines = true;

                foreach (Curve curve in curveLoop)
                {
                    if (!(curve is Line) && !(curve is Arc) && !(curve is Ellipse))
                        return false;
                    if (!(curve is Line))
                        allLines = false;
                }

                if (allLines)
                    return false;
            }

            if (curves != null)
            {
                bool allLines = true;

                foreach (Curve curve in curves)
                {
                    if (!(curve is Line) && !(curve is Arc) && !(curve is Ellipse))
                        return false;
                    if (!(curve is Line))
                        allLines = false;
                }

                if (allLines)
                    return false;

                if (allLines)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Creates an IFC curve from polyline points or curve segments.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="useSimpleBoundary">True to use simple boundary.</param>
        /// <param name="needToClose">True if the curve needs to be close.</param>
        /// <param name="pts">The polyline points.</param>
        /// <param name="segments">The curve segments.</param>
        /// <returns>The created IFC curve.</returns>
        static IFCAnyHandle CreateCurveFromComponents(IFCFile file, bool useSimpleBoundary, bool needToClose, IList<UV> pts, IList<IFCAnyHandle> segments)
        {
            IFCAnyHandle profileCurve;

            if (useSimpleBoundary)
            {
                int sz = pts.Count;
                if (sz < 2)
                    return null;

                IList<IFCAnyHandle> polyLinePts = new List<IFCAnyHandle>();
                foreach (UV pt in pts)
                {
                    polyLinePts.Add(ExporterUtil.CreateCartesianPoint(file, pt));
                }

                if (needToClose)
                    polyLinePts.Add(polyLinePts[0]);

                if (polyLinePts.Count < 2)
                    return null;

                profileCurve = IFCInstanceExporter.CreatePolyline(file, polyLinePts);
            }
            else
            {
                profileCurve = IFCInstanceExporter.CreateCompositeCurve(file, segments, IFCLogical.False);
            }
            return profileCurve;
        }        

        /// <summary>
        /// Function to process list of triangles set into an indexed triangles format for Tessellated geometry
        /// </summary>
        /// <param name="file">the IFC file</param>
        /// <param name="triangleList">the list of triangles</param>
        /// <returns>an IFC handle for IfcTriangulatedFaceSet Item</returns>
        public static IFCAnyHandle GetIndexedTriangles(IFCFile file, List<List<XYZ>> triangleList)
        {
            List<XYZ> vertList = new List<XYZ>();
         IList<IList<double>> coordList = new List<IList<double>>();
         IList<IList<int>> triIndex = new List<IList<int>>();

            if (triangleList.Count == 0)
                return null;

            foreach (List<XYZ> triangle in triangleList)
            {
                // Create triangle index and insert the index list of 3 into the triangle index list
                List<int> tri = new List<int>();

                foreach (XYZ vert in triangle)
                {
                    int idx = -1;

                    idx = vertList.FindIndex(x => x.IsAlmostEqualTo(vert));
                    if (idx < 0)
                    {
                        // Point not found, insert the point into the list
                        vertList.Add(vert);
                        idx = vertList.Count - 1; // Since the item is added at the end of the list, the index will be the last item in the List
                    }

                    tri.Add((idx) + 1); //!!! The index starts at 1 (and not 0) following X3D standard
                }
                triIndex.Add(tri);
            }

            if (vertList.Count == 0)
                return null;

            foreach (XYZ vert in vertList)
            {
                List<double> coord = new List<double>();
                coord.Add(vert.X);
                coord.Add(vert.Y);
                coord.Add(vert.Z);
                coordList.Add(coord);
            }
            IFCAnyHandle coordPointLists = IFCAnyHandleUtil.CreateInstance(file, IFCEntityType.IfcCartesianPointList3D);
            IFCAnyHandleUtil.SetAttribute(coordPointLists, "CoordList", coordList, 1, null, 3, 3);

            IFCAnyHandle triangulatedItem = IFCInstanceExporter.CreateTriangulatedFaceSet(file, coordPointLists, null, null, triIndex, null);

            return triangulatedItem;
        }

        /// Check if two bounding boxes overlap. 
        /// </summary>
        /// <param name="originalBox1">The first bounding box</param>
        /// <param name="originalBox2">The second bounding box</param>
        /// <returns>true if two bounding boxes overlap</returns>
        /// <remarks>This method only works under the assumption that two bounding boxes are in the same coordinate system, 
        ///          which means that their respective faces are parallel to each other. 
        ///          If the given boxes are transformed, then this function will create two axes-aligned bounding boxes
        ///          of these two boxes in the model coordinate system, 
        ///          and then check if the two new bounding boxes overlap</remarks>
        public static bool BoundingBoxesOverlap(BoundingBoxXYZ originalBox1, BoundingBoxXYZ originalBox2)
        {
            if ((originalBox1 != null && originalBox1.Enabled && originalBox2 != null && originalBox2.Enabled))
            {
                BoundingBoxXYZ bbox1 = BoundingBoxInModelCoordinate(originalBox1);
                BoundingBoxXYZ bbox2 = BoundingBoxInModelCoordinate(originalBox2);

                if (bbox1 == null || bbox2 == null)
                    return false;

                return (bbox1.Max.X >= bbox2.Min.X) && (bbox1.Min.X <= bbox2.Max.X)
                    && (bbox1.Max.Y >= bbox2.Min.Y) && (bbox1.Min.Y <= bbox2.Max.Y)
                    && (bbox1.Max.Z >= bbox2.Min.Z) && (bbox1.Min.Z <= bbox2.Max.Z);
            }
            else
            {
                return false;
            }
        }

        // return the bounding box in model coordinate of the given box
        private static BoundingBoxXYZ BoundingBoxInModelCoordinate(BoundingBoxXYZ bbox)
        {
            if (bbox == null)
                return null;

            double[] xVals = new double[] { bbox.Min.X, bbox.Max.X };
            double[] yVals = new double[] { bbox.Min.Y, bbox.Max.Y };
            double[] zVals = new double[] { bbox.Min.Z, bbox.Max.Z };

            XYZ toTest;

            double minX, minY, minZ, maxX, maxY, maxZ;
            minX = minY = minZ = double.MaxValue;
            maxX = maxY = maxZ = double.MinValue;

            // Get the max and min coordinate from the 8 vertices
            for (int iX = 0; iX < 2; iX++)
            {
                for (int iY = 0; iY < 2; iY++)
                {
                    for (int iZ = 0; iZ < 2; iZ++)
                    {
                        toTest = bbox.Transform.OfPoint(new XYZ(xVals[iX], yVals[iY], zVals[iZ]));
                        minX = Math.Min(minX, toTest.X);
                        minY = Math.Min(minY, toTest.Y);
                        minZ = Math.Min(minZ, toTest.Z);

                        maxX = Math.Max(maxX, toTest.X);
                        maxY = Math.Max(maxY, toTest.Y);
                        maxZ = Math.Max(maxZ, toTest.Z);
                    }
                }
            }

            BoundingBoxXYZ returnBox = new BoundingBoxXYZ();
            returnBox.Max = new XYZ(maxX, maxY, maxZ);
            returnBox.Min = new XYZ(minX, minY, minZ);

            return returnBox;
        }

        /// <summary>
        /// Sort the edge loops in the given face
        /// </summary>
        /// <param name="edgeArrays">The list of loops</param>
        /// <param name="face">The given face</param>
        /// <returns>Returns a map that maps every outer loop to its corresponding inner loops</returns>
        public static Dictionary<EdgeArray, IList<EdgeArray>> SortEdgeLoop(EdgeArrayArray edgeArrays, Face face) 
        {
            // we will sort these loops by tessellating every edgeArray on the given face to get the uv loop. 
            // The connection between each edge array and its corresponding uv loop will be stored in the loopMap. 
            // We will then sort the uv loops and store the result in the sortedTessellatedLoops. After we 
            // finish sorting uv loops, we convert them back to edge arrays and return the result
          
            Dictionary<EdgeArray, IList<EdgeArray>> sortedEdgeLoops = new Dictionary<EdgeArray, IList<EdgeArray>>();
            Dictionary<IList<UV>, IList<IList<UV>>> sortedTessellatedLoops = new Dictionary<IList<UV>,IList<IList<UV>>>();
            IDictionary<IList<UV>, EdgeArray> loopMap = new Dictionary<IList<UV>, EdgeArray>();
            
            foreach (EdgeArray edgeArray in edgeArrays) 
            {
                // We will tessellate edgeArray to get tessellatedLoop
                List<UV> tessellatedLoop = new List<UV>();
                

                // the number of already processed edges, we only use this to know if we are processing the last edge or not
                int count = 0;

                // Tessellate each edge to get a list of UV points and add them to tessellatedLoop
                // we have to make sure that we don't add the same point twice to the list, since each point is shared by 2 edges in the loop
                foreach (Edge edge in edgeArray) 
                {
                    
                    bool lastEdge = (++count == edgeArray.Size);
                    List<UV> tessellatedEdge = edge.TessellateOnFace(face).ToList<UV>();

                    // For the first edge in the loop, we will add all of its tessellated points to the list
                    if (tessellatedLoop.Count == 0)
                    {
                        tessellatedLoop.AddRange(tessellatedEdge);
                    }
                    else
                    {
                        // For every other edge that is not the first one, one of its end point will already be in tessellatedLoop (if not then
                        // we have a disconnected edge loop, in that case we will stop the process and throw an exception). 
                        // However, because tessellateOnFace is not consistent in the direction that it tessellates an edge, we don't know how 
                        // this edge connects to the existing loop. Thus we have to check 2 end points of this edge against 2 end points of the
                        // loops to decide which 2 of them are equal. 

                        // If this edge is the last edge in the loop, then both of its end point will already be in the loop, hence we need an extra
                        // check to avoid adding redundant points.
                  double distEndToStart = tessellatedEdge[tessellatedEdge.Count - 1].DistanceTo(tessellatedLoop[0]);
                  double distStartToStart = tessellatedEdge[0].DistanceTo(tessellatedLoop[0]);
                  double distEndToEnd = tessellatedEdge[tessellatedEdge.Count - 1].DistanceTo(tessellatedLoop[tessellatedLoop.Count - 1]);
                  double distStartToEnd = tessellatedEdge[0].DistanceTo(tessellatedLoop[tessellatedLoop.Count - 1]);

                  double minDist = Math.Min(Math.Min(distEndToStart, distStartToStart), Math.Min(distEndToEnd, distStartToEnd));
                  double uvTol = ExporterCacheManager.Document.Application.VertexTolerance;
                  if (minDist > uvTol)
                     throw new InvalidOperationException("Disconnected edge loop");

                  if (MathUtil.IsAlmostEqual(distEndToStart, minDist))
                        {
                            // if the last point of the edge is the first point of the loop, then remove that last point, and
                            // append the loop to this edge
                            tessellatedEdge.RemoveAt(tessellatedEdge.Count - 1);
                            if (lastEdge) 
                            {
                                tessellatedEdge.RemoveAt(0);
                            }
                            tessellatedEdge.AddRange(tessellatedLoop);
                            tessellatedLoop = tessellatedEdge;
                        }
                  else if (MathUtil.IsAlmostEqual(distStartToStart, minDist))
                        {
                            // if the first point of the edge is the first point of the loop, we reverse the edge, remove the last point (which used to be 
                            // the first one), and append the loop to this edge
                            tessellatedEdge.Reverse();
                            tessellatedEdge.RemoveAt(tessellatedEdge.Count - 1);
                            if (lastEdge) 
                            {
                                tessellatedEdge.RemoveAt(0);
                            }
                            tessellatedEdge.AddRange(tessellatedLoop);
                            tessellatedLoop = tessellatedEdge;
                        }
                  else if (MathUtil.IsAlmostEqual(distEndToEnd, minDist))
                        {
                            // if the last point of the edge is the last point of the loop, we remove that point and append the reversed edge to the loop
                            tessellatedEdge.Reverse();
                            tessellatedEdge.RemoveAt(0);
                            if (lastEdge) 
                            {
                                tessellatedEdge.RemoveAt(tessellatedEdge.Count - 1);
                            }
                            tessellatedLoop.AddRange(tessellatedEdge);
                        }
                  else if (MathUtil.IsAlmostEqual(distStartToEnd, minDist))
                        {
                            // if the last point of the loop is the first point of the edge, then we remove that point and append the edge to the loop
                            tessellatedEdge.RemoveAt(0);
                            if (lastEdge)
                            {
                                tessellatedEdge.RemoveAt(tessellatedEdge.Count - 1);
                            }
                            tessellatedLoop.AddRange(tessellatedEdge);
                        }
                        else 
                        {
                     throw new InvalidOperationException("Unexpected case.");
                        }
                    }
                }

                // After finishing tessellating this loop, store a map from the tessellatedLoop to the edgeArray in the loopMap
                loopMap.Add(tessellatedLoop, edgeArray);

                bool created = false;
                // After getting the tessellatedLoop, we will add it to the sortedTessellatedLoops by first checking if this loop is inside 
                // any of the outer loops in the map (which are the keys in this map)
                // 1. If this loop is inside one of them, says outerLoop, then we will have to check if this loop is inside or contains any of the outerLoop's inners:
                //      - if it is inside one of the outerLoop's inners, then this loop will be an outer loop and we will just have to add it as a new key to the map
                //      - if it contains some of the outerLoop's inners, then all of these inners will become outer loops
                //      - if none of the above, then we will add this loop as an another inner loop of the outerLoop
                // 2. If this loop is not inside any of the outer loops, then it will be an outer loop.
                foreach (KeyValuePair<IList<UV>, IList<IList<UV>>> entry in sortedTessellatedLoops)
                {
                    // first we check if tessellatedLoop is inside any of the loop in the sortedTessellatedEdges
                    if (PointInsidePolygon(tessellatedLoop[0], entry.Key))
                    {
                        // now we need to check if each loop in entry.Value is inside this loop
                        IList<IList<UV>> innerLoops = new List<IList<UV>>();
                        if (IsInsideAnotherLoop(tessellatedLoop, entry.Value))
                        {
                            // if tessellateLoop is inside another loop, then it will become the outer loop
                            sortedTessellatedLoops.Add(tessellatedLoop, new List<IList<UV>>());
                        }
                        else if (IsOutsideOtherLoops(tessellatedLoop, entry.Value, out innerLoops))
                        {
                            // if tessellatedLoop contains some other loops, then all of these loops become outer loop
                            entry.Value.Add(tessellatedLoop);
                            foreach (IList<UV> innerLoop in innerLoops)
                            {
                                entry.Value.Remove(innerLoop);
                                sortedTessellatedLoops.Add(innerLoop, new List<IList<UV>>());
                            }
                        }
                        else 
                        {
                            entry.Value.Add(tessellatedLoop);
                        }
                        created = true;
                        break;
                    }
                }

                if (!created)
                {
                    // this means tessellatedLoop is not inside any of the outerloop in the sortedTessellatedLoop
                    sortedTessellatedLoops.Add(tessellatedLoop, new List<IList<UV>>());
                }
            }

            // convert IList<UV> back into EdgeArray and return the result;

            foreach (KeyValuePair<IList<UV>, IList<IList<UV>>> entry in sortedTessellatedLoops)
            {
                IList<UV> key = entry.Key;
                IList<EdgeArray> innerLoops = new List<EdgeArray>();

                foreach (IList<UV> uvLoop in entry.Value) 
                {
                    innerLoops.Add(loopMap[uvLoop]);
                }

                sortedEdgeLoops.Add(loopMap[key], innerLoops);
            }

            return sortedEdgeLoops;
        }

        /// <summary>
        /// Check if the given loop (loopToCheck) is inside any of the loop in the given list of loops (listOfLoops). 
        /// The loop that contains loopToCheck will be stored in outerLoop
        /// Currently this method is only used in SortEdgeLoop, and we are sure that there is at most one loop that can contain
        /// loopToCheck. If there is no such loop, then outerLoop will be an empty list
        /// </summary>
        /// <param name="loopToCheck">The given loop</param>
        /// <param name="listOfLoops">The given list of loops</param>
        /// <returns>true if the given loop is inside any of the loop in the given list of loops</returns>
        private static bool IsInsideAnotherLoop(IList<UV> loopToCheck, IList<IList<UV>> listOfLoops) 
        {
            foreach (IList<UV> loop in listOfLoops) 
            {
                if (PointInsidePolygon(loopToCheck[0], loop)) 
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Check if the given loop (loopToCheck) is outside any of the loop in the given list of loops (listOfLoops).
        /// Every loop that is inside loopToCheck will be collected and stored in resultedList
        /// </summary>
        /// <param name="loopToCheck">The given loop</param>
        /// <param name="listOfLoops">The given list of loops</param>
        /// <param name="resultedList">The list of loops that is inside loopToCheck</param>
        /// <returns>true if loopToCheck is outside any loop in the given list of loops</returns>
        private static bool IsOutsideOtherLoops(IList<UV> loopToCheck, IList<IList<UV>> listOfLoops, out IList<IList<UV>> resultedList) 
        {
            resultedList = new List<IList<UV>>();
            foreach (IList<UV> loop in listOfLoops) 
            {
                if (PointInsidePolygon(loop[0], loopToCheck)) 
                {
                    resultedList.Add(loop);
                }
            }

            return resultedList.Count > 0;
        }

        /// <summary>
        /// Checks if the given point is inside the given loop
        /// </summary>
        /// <param name="pnt">The given point</param>
        /// <param name="polyNodes">The given loop</param>
        /// <returns>true if the given point is inside the given loop</returns>
        /// <remarks>This function returns an arbitrary result when the point is on the boundary of the polygon. The caller of this function should check
        ///          if the point is on the boundary first</remarks>
        private static bool PointInsidePolygon(UV pnt, IList<UV> polyNodes) 
        {
            if (pnt == null || polyNodes == null || polyNodes.Count == 0)
                return false;

            int nNodes = polyNodes.Count;
            // Find the number of intersections of the ray strting from the 'pnt'
            // in the left direction, with the edges of the polygon.
            int count = 0; // number of intersections
            for (int iPrev = 0; iPrev < nNodes; iPrev++)
            {
                int iNext = (iPrev + 1) % nNodes;
                if (polyNodes[iPrev].V >= pnt.V == polyNodes[iNext].V < pnt.V)
                {
                    if ((pnt.V - polyNodes[iPrev].V) * (polyNodes[iPrev].U - polyNodes[iNext].U) /
                       (polyNodes[iPrev].V - polyNodes[iNext].V) + polyNodes[iPrev].U < pnt.U)
                        count++;
                }
            }
            return (count % 2) != 0;
        }

        /// <summary>
        /// Create IFCCurve from the given curve
        /// </summary>
        /// <param name="file">The file</param>
        /// <param name="exporterIFC">The exporter</param>
        /// <param name="curve">The curve that needs to convert to IFCCurve</param>
        /// <param name="allowAdvancedCurve">indicates whether (TRUE) we want to convert "advanced" curve type 
        ///                                  like Hermite or NURBS to IfcCurve or (FALSE) we want to tessellate them</param>
      /// <param name="cartesianPoints">A map of already created cartesian points, to avoid duplication.</param>
        /// <returns>The handle representing the IFCCurve</returns>
      /// <remarks>This cartesianPoints map caches certain 3D points computed by this function that are related to the 
      /// curve, such as the start point of a line and the center of an arc.  It uses the cached values when possible.</remarks>
      public static IFCAnyHandle CreateIFCCurveFromRevitCurve(IFCFile file, ExporterIFC exporterIFC, Curve curve, bool allowAdvancedCurve,
         IDictionary<IFCFuzzyXYZ, IFCAnyHandle> cartesianPoints)
        {
            IFCAnyHandle ifcCurve = null;

            // if the Curve is a line, do the following
            if (curve is Line)
            {
                // Unbounded line doesn't make sense, skip if somehow it is 
                if (curve.IsBound)
                {
                    Line curveLine = curve as Line;
               IFCAnyHandle point = XYZtoIfcCartesianPoint(exporterIFC, curveLine.GetEndPoint(0), cartesianPoints);
               IFCAnyHandle vector = VectorToIfcVector(exporterIFC, curveLine.Direction);

                    ifcCurve = IFCInstanceExporter.CreateLine(file, point, vector);
                }
            }
            // if the Curve is an Arc do following
            else if (curve is Arc)
            {
                Arc curveArc = curve as Arc;
                XYZ curveArcCenter = curveArc.Center;
                XYZ curveArcNormal = curveArc.Normal;
                XYZ curveArcXDirection = curveArc.XDirection;

                if (curveArcCenter == null || curveArcNormal == null || curveArcXDirection == null)
                {
                    // encounter invalid curve, return null
                    return null;
                }
            IFCAnyHandle location3D = XYZtoIfcCartesianPoint(exporterIFC, curveArcCenter, cartesianPoints);

                // Create the z-direction
            IFCAnyHandle axis = VectorToIfcDirection(exporterIFC, curveArcNormal);

                // Create the x-direction
            IFCAnyHandle refDirection = VectorToIfcDirection(exporterIFC, curveArcXDirection);

                IFCAnyHandle position3D = IFCInstanceExporter.CreateAxis2Placement3D(file, location3D, axis, refDirection);
                ifcCurve = IFCInstanceExporter.CreateCircle(file, position3D, UnitUtil.ScaleLength(curveArc.Radius));
            }
            // If curve is an ellipse or elliptical Arc type
            else if (curve is Ellipse)
            {
                Ellipse curveEllipse = curve as Ellipse;
                IList<double> direction = new List<double>();
                XYZ ellipseNormal = curveEllipse.Normal;
                XYZ ellipseXDirection = curveEllipse.XDirection;

            IFCAnyHandle location3D = XYZtoIfcCartesianPoint(exporterIFC, curveEllipse.Center, cartesianPoints);

            IFCAnyHandle axis = VectorToIfcDirection(exporterIFC, ellipseNormal);

                // Create the x-direction
            IFCAnyHandle refDirection = VectorToIfcDirection(exporterIFC, ellipseXDirection);

                IFCAnyHandle position = IFCInstanceExporter.CreateAxis2Placement3D(file, location3D, axis, refDirection);

                ifcCurve = IFCInstanceExporter.CreateEllipse(file, position, UnitUtil.ScaleLength(curveEllipse.RadiusX), UnitUtil.ScaleLength(curveEllipse.RadiusY));
            }
            else if (allowAdvancedCurve && curve is NurbSpline)
            {
                NurbSpline nurbSpline = curve as NurbSpline;

                int degree = nurbSpline.Degree;
                IList<XYZ> controlPoints = nurbSpline.CtrlPoints;
                IList<IFCAnyHandle> controlPointsInIfc = new List<IFCAnyHandle>();
                foreach (XYZ xyz in controlPoints)
                {
               controlPointsInIfc.Add(XYZtoIfcCartesianPoint(exporterIFC, xyz, cartesianPoints));
                }

                // Based on IFC4 specification, curveForm is for information only, leave it as UNSPECIFIED for now.
                Revit.IFC.Export.Toolkit.IFC4.IFCBSplineCurveForm curveForm = Toolkit.IFC4.IFCBSplineCurveForm.UNSPECIFIED;

                IFCLogical closedCurve = nurbSpline.isClosed ? IFCLogical.True : IFCLogical.False;

                // Based on IFC4 specification, selfIntersect is for information only, leave it as Unknown for now
                IFCLogical selfIntersect = IFCLogical.Unknown;

                // Unlike Revit, IFC uses 2 lists to store knots information. The first list contain every distinct knot, 
                // and the second list stores the multiplicities of each knot. The following code creates those 2 lists  
                // from the Knots property of Revit NurbSpline
                DoubleArray revitKnots = nurbSpline.Knots;
                IList<double> ifcKnots = new List<double>();
                IList<int> knotMultiplitices = new List<int>();

                foreach (double knot in revitKnots)
                {
                    if (ifcKnots.Count == 0 || !MathUtil.IsAlmostEqual(knot, ifcKnots[ifcKnots.Count - 1]))
                    {
                        ifcKnots.Add(knot);
                        knotMultiplitices.Add(1);
                    }
                    else
                    {
                        knotMultiplitices[knotMultiplitices.Count - 1]++;
                    }
                }

                // Based on IFC4 specification, knotSpec is for information only, leave it as UNSPECIFIED for now.
                Toolkit.IFC4.IFCKnotType knotSpec = Toolkit.IFC4.IFCKnotType.UNSPECIFIED;

                if (!nurbSpline.isRational)
                {
                    ifcCurve = IFCInstanceExporter.CreateBSplineCurveWithKnots
                        (file, degree, controlPointsInIfc, curveForm, closedCurve, selfIntersect, knotMultiplitices, ifcKnots, knotSpec);
                }
                else
                {
                    DoubleArray revitWeights = nurbSpline.Weights;
                    IList<double> ifcWeights = new List<double>();

                    foreach (double weight in revitWeights)
                    {
                        ifcWeights.Add(weight);
                    }

                    ifcCurve = IFCInstanceExporter.CreateRationalBSplineCurveWithKnots
                        (file, degree, controlPointsInIfc, curveForm, closedCurve, selfIntersect, knotMultiplitices, ifcKnots, knotSpec, ifcWeights);
                }
            }
            // if the Curve is of any other type, tessellate it and use polyline to represent it
            else
            {
                // any other curve is not supported, we will tessellate it
                IList<XYZ> tessCurve = curve.Tessellate();
                IList<IFCAnyHandle> polylineVertices = new List<IFCAnyHandle>();
                foreach (XYZ vertex in tessCurve)
                {
               IFCAnyHandle ifcVert = XYZtoIfcCartesianPoint(exporterIFC, vertex, cartesianPoints);
                    polylineVertices.Add(ifcVert);
                }
                ifcCurve = IFCInstanceExporter.CreatePolyline(file, polylineVertices);
            }
            return ifcCurve;
        }

        /// <summary>
        /// Converts the given XYZ point to IfcCartesianPoint3D
        /// </summary>
        /// <param name="exporterIFC">The exporter</param>
        /// <param name="thePoint">The point</param>
      /// <param name="cartesianPoints">A map of already created IfcCartesianPoints.  This argument may be null.</param>
      /// <returns>The handle representing IfcCartesianPoint</returns>
      public static IFCAnyHandle XYZtoIfcCartesianPoint(ExporterIFC exporterIFC, XYZ thePoint, IDictionary<IFCFuzzyXYZ, IFCAnyHandle> cartesianPoints)
        {
            IFCFile file = exporterIFC.GetFile();
         XYZ vertexScaled = ExporterIFCUtils.TransformAndScalePoint(exporterIFC, thePoint);
         IFCFuzzyXYZ fuzzyVertexScaled = (cartesianPoints != null) ? new IFCFuzzyXYZ(vertexScaled) : null;

         IFCAnyHandle cartesianPoint = null;
         if (fuzzyVertexScaled != null && cartesianPoints.TryGetValue(fuzzyVertexScaled, out cartesianPoint))
            return cartesianPoint;

         cartesianPoint = ExporterUtil.CreateCartesianPoint(file, vertexScaled);
         if (fuzzyVertexScaled != null)
            cartesianPoints[fuzzyVertexScaled] = cartesianPoint;

            return cartesianPoint;
        }

        /// <summary>
      /// Converts the given XYZ vector to IfcDirection
        /// </summary>
        /// <param name="exporterIFC">The exporter</param>
      /// <param name="theVector">The vector</param>
      /// <returns>The hanlde representing IfcDirection</returns>
      public static IFCAnyHandle VectorToIfcDirection(ExporterIFC exporterIFC, XYZ theVector)
        {
            IFCFile file = exporterIFC.GetFile();
         XYZ vectorScaled = ExporterIFCUtils.TransformAndScaleVector(exporterIFC, theVector);

         IFCAnyHandle direction = ExporterUtil.CreateDirection(file, vectorScaled);
         return direction;
      }

      /// <summary>
      /// Converts the given XYZ vector to IfcVector
      /// </summary>
      /// <param name="exporterIFC">The exporter</param>
      /// <param name="theVector">The vector</param>
      /// <returns>The hanlde representing IfcVector</returns>
      public static IFCAnyHandle VectorToIfcVector(ExporterIFC exporterIFC, XYZ theVector)
      {
         IFCFile file = exporterIFC.GetFile();
         XYZ vectorScaled = ExporterIFCUtils.TransformAndScaleVector(exporterIFC, theVector);

         double lineLength = UnitUtil.ScaleLength(theVector.GetLength());

         IFCAnyHandle vector = ExporterUtil.CreateVector(file, vectorScaled, lineLength);
         return vector;
        }

    }
}
