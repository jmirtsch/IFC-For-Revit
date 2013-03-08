//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2012  Autodesk, Inc.
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
using BIM.IFC.Toolkit;
using BIM.IFC.Exporter;


namespace BIM.IFC.Utility
{
    /// <summary>
    /// Provides static methods for geometry related manipulations.
    /// </summary>
    public class GeometryUtil
    {
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
            double scaledAngle = Math.Asin(zOff) * 180 / Math.PI;
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
        /// Moves curve along the direction.
        /// </summary>
        /// <param name="originalCurve">
        /// The curve.
        /// </param>
        /// <param name="direction">
        /// The direction.
        /// </param>
        /// <returns>
        /// The moved curve.
        /// </returns>
        public static Curve MoveCurve(Curve originalCurve, XYZ direction)
        {
            Transform moveTrf = Transform.get_Translation(direction);
            return originalCurve.get_Transformed(moveTrf);
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
            curveBounds = new IFCRange(curve.get_EndParameter(0), curve.get_EndParameter(1));
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
                if (solid != null && solid.Faces.Size > 0 && solid.Volume > 0.0)
                {
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
                            if (instanceSymbol != null)
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
        public static void AddItemsToShape(IFCAnyHandle shape, ICollection<IFCAnyHandle> items)
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
        /// <returns>The geometry element.</returns>
        /// <remarks>This routine may not work properly for railings created before 2006.  If you get
        /// poor representations from such railings, please upgrade the railings if possible.</remarks>
        public static GeometryElement GetOneLevelGeometryElement(GeometryElement geomElement)
        {
            if (geomElement == null)
                return null;

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
                            Line line = Line.get_Bound(tessPts[ii], tessPts[ii + 1]);
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
                                Face adjoiningFace = edge.get_Face(1);
                                if (adjoiningFace.Equals(currFace))
                                    adjoiningFace = edge.get_Face(0);

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
                        double val = curve.get_EndPoint(0).DotProduct(extrusionDirection);
                        range.Start = val;
                        range.End = val;
                        init = true;
                    }
                }
                else
                {
                    double val = curve.get_EndPoint(0).DotProduct(extrusionDirection);
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
                XYZ point = curve.get_EndPoint(0);
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
        /// <returns>The new body representation.  If the clipping completely clips the extrusion, this will be null.  Otherwise, this
        /// will be the clipped representation if a clipping was done, or the original representation if not.</returns>
        public static IFCAnyHandle CreateClippingFromFaces(ExporterIFC exporterIFC, Element cuttingElement, Plane extrusionBasePlane,
            XYZ extrusionDirection, ICollection<Face> faces, IFCRange range, IFCAnyHandle origBodyRepHnd)
        {
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
                    for (int ii = 0; ii < numFaces; ii++)
                    {
                        if (facesToSkip.Contains(ii))
                            continue;

                        newBodyRepHnd = ProcessClippingFace(exporterIFC, outerCurveLoops[ii], outerCurveLoopPlanes[ii], 
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
                    Face face1 = edge.get_Face(0);
                    Face face2 = edge.get_Face(1);
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
                    if (planarFace != null && MathUtil.VectorsAreParallel(planarFace.Normal, normal))
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
                        Face sideFace = edge.get_Face(0);
                        if (sideFace == recessFace)
                            sideFace = edge.get_Face(1);

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
                if (MathUtil.VectorsAreParallel(face1.Normal, face2.Normal))
                {
                    firstValidCurves = curves1;
                    secondValidCurves = curves2;
                    removedFace = face3;
                }
                else if (MathUtil.VectorsAreParallel(face1.Normal, face3.Normal))
                {
                    firstValidCurves = curves1;
                    secondValidCurves = curves3;
                    removedFace = face2;
                }
                else if (MathUtil.VectorsAreParallel(face2.Normal, face3.Normal))
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
                curvesCollection.Add(firstValidCurves);
                curvesCollection.Add(secondValidCurves);

                foreach (IList<Curve> curves in curvesCollection)
                {
                    if (curves.Count < 2) //not valid
                        throw new Exception("Can't handle.");

                    XYZ end0 = curves[0].get_EndPoint(0);
                    XYZ end1 = curves[0].get_EndPoint(1);

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
                            XYZ curveEnd0 = curve.get_EndPoint(0);
                            XYZ curveEnd1 = curve.get_EndPoint(1);
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
                    Curve newCurve = Line.get_Bound(end1, end0);
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

            // check planar and parallel and orthogonal to the main extrusion dir

            if (!MathUtil.VectorsAreParallel(plane1.Normal, plane2.Normal))
            {
                //Error, can't handle this
                throw new Exception("Can't handle.");
            }

            if (!MathUtil.VectorsAreOrthogonal(plane1.Normal, extrusionDirection)
                || !MathUtil.VectorsAreOrthogonal(plane2.Normal, extrusionDirection))
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

                if (!MathUtil.IsAlmostEqual(edgeCurve1.get_EndPoint(0).DistanceTo(edgeCurve2.get_EndPoint(1)), planesDistance)
                  || !MathUtil.IsAlmostEqual(edgeCurve1.get_EndPoint(1).DistanceTo(edgeCurve2.get_EndPoint(0)), planesDistance))
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
                    if (!(edgeCurve2 is Ellipse) || !(sideFace is RuledFace))
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
                    if (!(edgeCurve2 is HermiteSpline) || !(sideFace is RuledFace))
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
                    if (!(edgeCurve2 is NurbSpline) || !(sideFace is RuledFace))
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
            IFCAnyHandle extrusionHandle = ExtrusionExporter.CreateExtrudedSolidFromCurveLoop(exporterIFC, null, origCurveLoops, plane1, extDir, planesDistance * exporterIFC.LinearScale);

            IFCAnyHandle booleanBodyItemHnd = IFCInstanceExporter.CreateBooleanResult(exporterIFC.GetFile(), IFCBooleanOperator.Difference,
                origBodyRepHnd, extrusionHandle);

            return booleanBodyItemHnd;
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
                    if (currentCurve.get_EndPoint(1).IsAlmostEqualTo(curve.get_EndPoint(0)))
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
        /// Common method to create a poly line.
        /// </summary>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="points">The line points.</param>
        /// <param name="scaledPlane">The scaled plane.</param>
        /// <returns>The handle.</returns>
        static IFCAnyHandle CreatePolyLineCommon(ExporterIFC exporterIFC, IList<XYZ> points, Plane scaledPlane)
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
                IFCAnyHandle pointHandle = null;
                if (scaledPlane != null)
                {
                    List<double> pointValues = ConvertPointToLocalCoordinatesCommon(scaledPlane, point);
                    pointHandle = ExporterUtil.CreateCartesianPoint(file, pointValues);
                }
                else
                    pointHandle = ExporterUtil.CreateCartesianPoint(file, point);
                polyLinePoints.Add(pointHandle);
            }

            if (isClosed)
                polyLinePoints.Add(polyLinePoints[0]);

            return IFCInstanceExporter.CreatePolyline(file, polyLinePoints);
        }

        /// <summary>
        /// Creates an IFC line from a Revit line object.
        /// </summary>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="line">The line.</param>
        /// <param name="scaledPlane">The scaled plane.</param>
        /// <returns>The line handle.</returns>
        public static IFCAnyHandle CreateLine(ExporterIFC exporterIFC, Line line, Plane scaledPlane)
        {
            List<XYZ> points = new List<XYZ>();
            points.Add(line.get_EndPoint(0));
            points.Add(line.get_EndPoint(1));
            return CreatePolyLineCommon(exporterIFC, points, scaledPlane);
        }

        /// <summary>
        /// Creates an IFC arc from a Revit arc object.
        /// </summary>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="arc">The arc.</param>
        /// <param name="scaledPlane">The scaled plane.</param>
        /// <returns>The arc handle.</returns>
        public static IFCAnyHandle CreateArc(ExporterIFC exporterIFC, Arc arc, Plane scaledPlane)
        {
            IFCFile file = exporterIFC.GetFile();

            XYZ centerPoint = ExporterIFCUtils.TransformAndScalePoint(exporterIFC, arc.Center);

            IFCAnyHandle centerPointHandle;
            if (scaledPlane != null)
            {
                List<double> centerPointValues = ConvertPointToLocalCoordinatesCommon(scaledPlane, centerPoint);
                centerPointHandle = ExporterUtil.CreateCartesianPoint(file, centerPointValues);
            }
            else
                centerPointHandle = ExporterUtil.CreateCartesianPoint(file, centerPoint);

            XYZ xDirection = ExporterIFCUtils.TransformAndScaleVector(exporterIFC, arc.XDirection);
            IFCAnyHandle axis;
            if (scaledPlane != null)
            {
                List<double> xDirectionValues = ConvertVectorToLocalCoordinates(scaledPlane, xDirection);
                IFCAnyHandle xDirectionHandle = ExporterUtil.CreateDirection(file, xDirectionValues);
                axis = IFCInstanceExporter.CreateAxis2Placement2D(file, centerPointHandle, null, xDirectionHandle);
            }
            else
                axis = ExporterUtil.CreateAxis2Placement3D(file, centerPoint, arc.Normal, xDirection);

            double arcRadius = arc.Radius * exporterIFC.LinearScale;

            IFCAnyHandle circle = IFCInstanceExporter.CreateCircle(file, axis, arcRadius);

            IFCAnyHandle arcHandle = circle;
            if (arc.IsBound)
            {
                double endParam0 = arc.get_EndParameter(0);
                double endParam1 = arc.get_EndParameter(1);
                if (scaledPlane != null && MustFlipCurve(scaledPlane, arc))
                {
                    double oldParam0 = endParam0;
                    endParam0 = Math.PI * 2 - endParam1;
                    endParam1 = Math.PI * 2 - oldParam0;
                }
                IFCData firstParam = IFCDataUtil.CreateAsParameterValue(MathUtil.PutInRange(endParam0, Math.PI, 2 * Math.PI) * (180 / Math.PI));
                IFCData secondParam = IFCDataUtil.CreateAsParameterValue(MathUtil.PutInRange(endParam1, Math.PI, 2 * Math.PI) * (180 / Math.PI));

                // todo: check that firstParam != secondParam.
                HashSet<IFCData> trim1 = new HashSet<IFCData>();
                trim1.Add(firstParam);
                HashSet<IFCData> trim2 = new HashSet<IFCData>();
                trim2.Add(secondParam);

                arcHandle = IFCInstanceExporter.CreateTrimmedCurve(file, circle, trim1, trim2, true, IFCTrimmingPreference.Parameter);
            }
            return arcHandle;
        }

        /// <summary>
        /// Creates an IFC composite curve from an array of curves.
        /// </summary>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="curves">The curves.</param>
        /// <returns>The IfcCompositeCurve handle.</returns>
        public static IFCAnyHandle CreateCompositeCurve(ExporterIFC exporterIFC, IList<Curve> curves)
        {
            IFCFile file = exporterIFC.GetFile();
            List<IFCAnyHandle> segments = new List<IFCAnyHandle>();
            foreach (Curve curve in curves)
            {
                IFCAnyHandle curveHandle = null;
                if (curve is Line)
                {
                    curveHandle = CreateLine(exporterIFC, curve as Line, null);
                }
                else if (curve is Arc)
                {
                    curveHandle = CreateArc(exporterIFC, curve as Arc, null);
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

        /// <summary>
        /// Determines if the Normal of the PlanarFace is flipped.
        /// </summary>
        /// <param name="planarFace">The planar face.</param>
        /// <returns>True if the normal is flipped.</returns>
        public static bool IsPlanarFaceNormalFlipped(PlanarFace planarFace)
        {
            XYZ planarFaceNormal = planarFace.Normal;
            XYZ faceNormal = planarFace.ComputeNormal(new UV(0, 0));
            return MathUtil.VectorsAreParallel2(planarFaceNormal, faceNormal) == -1;
        }

        /// <summary>
        /// Splits a Solid into distinct volumes.
        /// </summary>
        /// <param name="solid">The initial solid.</param>
        /// <returns>The list of volumes.</returns>
        /// <remarks>This calls the internal SolidUtils.SplitVolumes routine, but does additional cleanup work to properly dispose of stale data.</remarks>
        public static IList<Solid> SplitVolumes(Solid solid)
        {
            IList<Solid> splitVolumes = SolidUtils.SplitVolumes(solid);
            foreach (Solid currSolid in splitVolumes)
            {
                // The geometry element created by SplitVolumes is a copy which will have its own allocated
                // membership - this needs to be stored and disposed of (see AllocatedGeometryObjectCache
                // for details)
                ExporterCacheManager.AllocatedGeometryObjectCache.AddGeometryObject(currSolid);
            }
            return splitVolumes;
        }
    }
}
