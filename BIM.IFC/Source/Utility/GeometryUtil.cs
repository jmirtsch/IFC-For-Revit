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
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using BIM.IFC.Toolkit;


namespace BIM.IFC.Utility
{
    /// <summary>
    /// Provides static methods for geometry related manipulations.
    /// </summary>
    class GeometryUtil
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
        /// Creates and returns an instance of the Options class with the detail level set to Fine.
        /// </summary>
        public static Options GetIFCExportGeometryOptions()
        {
            Options options = new Options();
            options.DetailLevel = ViewDetailLevel.Fine;
            return options;
        }

        /// <summary>
        /// Collects all solids and meshes within a GeometryElement.
        /// </summary>
        /// <remarks>
        /// Added in 2013 to replace the temporary API method ExporterIFCUtils.GetSolidMeshGeometry.
        /// </remarks>
        /// <param name="geomElemToUse">
        /// The GeometryElement.
        /// </param>
        /// <param name="trf">
        /// The initial Transform applied on the GeometryElement.
        /// </param>
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
        /// <returns>The collection of solids and meshes.</returns>
        public static SolidMeshGeometryInfo GetSplitSolidMeshGeometry(GeometryElement geomElemToUse)
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
        /// Collects all solids and meshes within all nested levels of a given GeometryElement.
        /// </summary>
        /// <remarks>
        /// This is a private helper method for the GetSolidMeshGeometry type collection methods.
        /// </remarks>
        /// <param name="geomElem">
        /// The GeometryElement we are collecting solids and meshes from.
        /// </param>
        /// <param name="trf">
        /// The initial Transform applied on the GeometryElement.
        /// </param>
        /// <param name="solidMeshCapsule">
        /// The SolidMeshGeometryInfo object that contains the lists of collected solids and meshes.
        /// </param>
        private static void CollectSolidMeshGeometry(GeometryElement geomElem, Transform trf, SolidMeshGeometryInfo solidMeshCapsule)
        {
            if (geomElem == null)
            {
                return;
            }
            GeometryElement currGeomElem = geomElem;
            Transform localTrf = trf;
            if (localTrf == null)
            {
                localTrf = Transform.Identity;
            }
            else if (!localTrf.IsIdentity)
            {
                currGeomElem = geomElem.GetTransformed(localTrf);
                // The geometry element created by "GetTransformed" is a copy which will have its own allocated
                // membership - this needs to be stored and disposed of (see AllocatedGeometryObjectCache
                // for details)
                ExporterCacheManager.AllocatedGeometryObjectCache.AddGeometryObject(currGeomElem);
            }
            // iterate through the GeometryObjects contained in the GeometryElement
            foreach (GeometryObject geomObj in currGeomElem){
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
        /// <param name="boundary">The boudary.</param>
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
        /// <returns></returns>
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
                    return symbolGeomElement;
            }

            return geomElement;
        }
    }
}
