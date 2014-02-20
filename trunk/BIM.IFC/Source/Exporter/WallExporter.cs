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
using BIM.IFC.Utility;
using BIM.IFC.Toolkit;
using BIM.IFC.Exporter.PropertySet;

namespace BIM.IFC.Exporter
{
    /// <summary>
    /// Provides methods to export walls.
    /// </summary>
    class WallExporter
    {
        private static IFCAnyHandle FallbackTryToCreateAsExtrusion(ExporterIFC exporterIFC, Wall wallElement, SolidMeshGeometryInfo smCapsule, double baseWallElevation,
            ElementId catId, Curve curve, Plane plane, double depth, IFCRange zSpan, IFCRange range, IFCPlacementSetter setter,
            out IList<IFCExtrusionData> cutPairOpenings, out bool isCompletelyClipped)
        {
            cutPairOpenings = new List<IFCExtrusionData>();

            IFCAnyHandle bodyRep;
            isCompletelyClipped = false;

            XYZ localOrig = plane.Origin;

            bool hasExtrusion = HasElevationProfile(wallElement);
            if (hasExtrusion)
            {
                IList<CurveLoop> loops = GetElevationProfile(wallElement);
                if (loops.Count == 0)
                    hasExtrusion = false;
                else
                {
                    IList<IList<CurveLoop>> sortedLoops = ExporterIFCUtils.SortCurveLoops(loops);
                    if (sortedLoops.Count == 0)
                        return null;

                    // Current limitation: can't handle wall split into multiple disjointed pieces.
                    int numSortedLoops = sortedLoops.Count;
                    if (numSortedLoops > 1)
                        return null;

                    bool ignoreExtrusion = true;
                    bool cantHandle = false;
                    bool hasGeometry = false;
                    for (int ii = 0; (ii < numSortedLoops) && !cantHandle; ii++)
                    {
                        int sortedLoopSize = sortedLoops[ii].Count;
                        if (sortedLoopSize == 0)
                            continue;
                        if (!ExporterIFCUtils.IsCurveLoopConvexWithOpenings(sortedLoops[ii][0], wallElement, range, out ignoreExtrusion))
                        {
                            if (ignoreExtrusion)
                            {
                                // we need more information.  Is there something to export?  If so, we'll
                                // ignore the extrusion.  Otherwise, we will fail.

                                if (smCapsule.SolidsCount() == 0 && smCapsule.MeshesCount() == 0)
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                cantHandle = true;
                            }
                            hasGeometry = true;
                        }
                        else
                        {
                            hasGeometry = true;
                        }
                    }

                    if (!hasGeometry)
                    {
                        isCompletelyClipped = true;
                        return null;
                    }

                    if (cantHandle)
                        return null;
                }
            }

            if (!CanExportWallGeometryAsExtrusion(wallElement, range))
                return null;

            // extrusion direction.
            XYZ extrusionDir = GetWallHeightDirection(wallElement);

            // create extrusion boundary.
            IList<CurveLoop> boundaryLoops = new List<CurveLoop>();

            bool alwaysThickenCurve = IsWallBaseRectangular(wallElement, curve);

            if (!alwaysThickenCurve)
            {
                boundaryLoops = GetLoopsFromTopBottomFace(wallElement, exporterIFC);
                if (boundaryLoops.Count == 0)
                    return null;
            }
            else
            {
                CurveLoop newLoop = null;
                try
                {
                    newLoop = CurveLoop.CreateViaThicken(curve, wallElement.Width, XYZ.BasisZ);
                }
                catch
                {
                }

                if (newLoop == null)
                    return null;

                if (!newLoop.IsCounterclockwise(XYZ.BasisZ))
                    newLoop = GeometryUtil.ReverseOrientation(newLoop);
                boundaryLoops.Add(newLoop);
            }

            // origin gets scaled later.
            XYZ setterOffset = new XYZ(0, 0, setter.Offset + (localOrig[2] - baseWallElevation));

            IFCAnyHandle baseBodyItemHnd = ExtrusionExporter.CreateExtrudedSolidFromCurveLoop(exporterIFC, null, boundaryLoops, plane,
                extrusionDir, depth);
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(baseBodyItemHnd))
                return null;

            IFCAnyHandle bodyItemHnd = AddClippingsToBaseExtrusion(exporterIFC, wallElement,
               setterOffset, range, zSpan, baseBodyItemHnd, out cutPairOpenings);
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(bodyItemHnd))
                return null;

            ElementId matId = HostObjectExporter.GetFirstLayerMaterialId(wallElement);
            IFCAnyHandle styledItemHnd = BodyExporter.CreateSurfaceStyleForRepItem(exporterIFC, wallElement.Document,
                baseBodyItemHnd, matId);

            HashSet<IFCAnyHandle> bodyItems = new HashSet<IFCAnyHandle>();
            bodyItems.Add(bodyItemHnd);

            IFCAnyHandle contextOfItemsBody = exporterIFC.Get3DContextHandle("Body");
            if (baseBodyItemHnd.Id == bodyItemHnd.Id)
            {
                bodyRep = RepresentationUtil.CreateSweptSolidRep(exporterIFC, wallElement, catId, contextOfItemsBody, bodyItems, null);
            }
            else
            {
                bodyRep = RepresentationUtil.CreateClippingRep(exporterIFC, wallElement, catId, contextOfItemsBody, bodyItems);
            }

            return bodyRep;
        }

        /// <summary>
        /// Computes the area of a simple face.
        /// </summary>
        /// <param name="face">The face.</param>
        /// <param name="facePlane">The plane of the face.</param>
        /// <param name="refPoint">Reference point for area computation.</param>
        /// <returns>The area.</returns>
        private static double ComputeSimpleFaceArea(IList<XYZ> face, Plane facePlane, XYZ refPoint)
        {
            double area = 0.0;
            int numVertices = face.Count;
            XYZ normal = facePlane.Normal;
            for (int ii = 0; ii < numVertices; ii++)
            {
                XYZ currEdge = face[(ii + 1) % numVertices] - face[ii];
                double length = currEdge.GetLength();

                XYZ heightVec = normal.CrossProduct(currEdge).Normalize();
                XYZ otherEdge = refPoint - face[ii];
                double height = heightVec.DotProduct(otherEdge);
                area += (length * height);
            }
            return area / 2.0;
        }

        /// <summary>
        /// Computes the signed volume of a polyhedral cone formed by a simpleface and a reference point.
        /// </summary>
        /// <param name="face">The face.</param>
        /// <param name="centroid">The centroid of the face.</param>
        /// <param name="refPoint">The vertex of the cone.</param>
        /// <param name="orientation">The orientation of the boundary.  If false, the volume should be reversed.</param>
        /// <returns>The signed volume.</returns>
        private static double ComputeSimpleFaceVolume(IList<XYZ> face, XYZ centroid, XYZ refPoint, bool orientation)
        {
            Plane facePlane = ComputeSimpleFacePlane(face, orientation);
            double area = ComputeSimpleFaceArea(face, facePlane, centroid);
            XYZ heightVec = refPoint - facePlane.Origin;
            double height = -facePlane.Normal.DotProduct(heightVec);
            double volume = area * height / 3.0;
            return volume;
        }

        /// <summary>
        /// Compute the plane of a polygonal face.
        /// </summary>
        /// <param name="face">The face.</param>
        /// <param name="orientation">The orientation of the boundary.  If false, the normal should be reversed.</param>
        /// <returns>The normal.</returns>
        private static Plane ComputeSimpleFacePlane(IList<XYZ> face, bool orientation)
        {
            int numVertices = face.Count;

            // Calculate normal of face and projected angle at vertex.
            XYZ dir1 = (face[1] - face[0]).Normalize();
            XYZ dir2 = null;
            for (int nextDirIndex = 2; nextDirIndex < numVertices; nextDirIndex++)
            {
                XYZ nextDir = (face[nextDirIndex] - face[1]).Normalize();
                if (MathUtil.IsAlmostEqual(Math.Abs(nextDir.DotProduct(dir1)), 1.0))
                    continue;
                dir2 = nextDir;
                break;
            }

            if (dir2 == null)
                throw new InvalidOperationException("Invalid outer face boundary.");

            XYZ faceNormal = dir1.CrossProduct(dir2).Normalize();
            if (!orientation)
                faceNormal = -faceNormal;
            return new Plane(faceNormal, face[0]);
        }

        /// <summary>
        /// This routine corrects three problems with the native code:
        /// 1.  Correct the orientation of a polyhedron whose normals face inwards instead of outwards.  The assumption
        /// is made that all of the faces have either the correct or incorrect orientation for simplicity and performance issues.
        /// 2.  Correct openings that are incorrectly labelled as recesses.
        /// 3.  Correct recesses that are incorrectly labelled as openings.
        /// </summary>
        /// <param name="openingHnd">The opening handle.</param>
        private static void PotentiallyCorrectOpeningOrientationAndOpeningType(IFCAnyHandle openingHnd, IFCAnyHandle wallLocalPlacement, double scaledWidth)
        {
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(openingHnd))
                return;

            IFCAnyHandle prodRep = IFCAnyHandleUtil.GetInstanceAttribute(openingHnd, "Representation");
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(prodRep))
                return;

            HashSet<IFCAnyHandle> reps = IFCAnyHandleUtil.GetAggregateInstanceAttribute<HashSet<IFCAnyHandle>>(prodRep, "Representations");
            if (reps == null || reps.Count != 1)
                return;

            IFCAnyHandle localPlacement = IFCAnyHandleUtil.GetInstanceAttribute(openingHnd, "ObjectPlacement");

            string openingLabel = IFCAnyHandleUtil.GetStringAttribute(openingHnd, "ObjectType");
            bool possiblyFixRecess = (String.Compare(openingLabel, "Recess", true) == 0) && (!IFCAnyHandleUtil.IsNullOrHasNoValue(localPlacement)) &&
                (!IFCAnyHandleUtil.IsNullOrHasNoValue(wallLocalPlacement));
            bool possiblyFixOpening = (String.Compare(openingLabel, "Opening", true) == 0) && (!IFCAnyHandleUtil.IsNullOrHasNoValue(localPlacement)) &&
                (!IFCAnyHandleUtil.IsNullOrHasNoValue(wallLocalPlacement));

            // We are limiting this hotfix to the case where the opening has a local offset but no transformation.
            XYZ placementOrigin = new XYZ(0, 0, 0);
            XYZ placementNormal = new XYZ(1, 0, 0);
            if (possiblyFixRecess || possiblyFixOpening)
            {
                IFCAnyHandle relativePlacementHnd = IFCAnyHandleUtil.GetInstanceAttribute(localPlacement, "RelativePlacement");
                if (!IFCAnyHandleUtil.IsNullOrHasNoValue(relativePlacementHnd))
                {
                    IFCAnyHandle originHnd = IFCAnyHandleUtil.GetInstanceAttribute(relativePlacementHnd, "Location");
                    if (!IFCAnyHandleUtil.IsNullOrHasNoValue(originHnd))
                    {
                        IList<double> coordList = IFCAnyHandleUtil.GetCoordinates(originHnd);
                        placementOrigin = new XYZ(-coordList[0], -coordList[1], -coordList[2]);
                    }
                    IFCAnyHandle openingRefDirHnd = IFCAnyHandleUtil.GetInstanceAttribute(relativePlacementHnd, "RefDirection");
                    IFCAnyHandle openingAxisHnd = IFCAnyHandleUtil.GetInstanceAttribute(relativePlacementHnd, "Axis");
                    if (!IFCAnyHandleUtil.IsNullOrHasNoValue(openingRefDirHnd) && !IFCAnyHandleUtil.IsNullOrHasNoValue(openingAxisHnd))
                    {
                        possiblyFixRecess = false;
                        possiblyFixOpening = false;
                    }
                }
                if (possiblyFixRecess || possiblyFixOpening)
                {
                    IFCAnyHandle wallRelativePlacementHnd = IFCAnyHandleUtil.GetInstanceAttribute(wallLocalPlacement, "RelativePlacement");
                    {
                        IFCAnyHandle normalHnd = IFCAnyHandleUtil.GetInstanceAttribute(wallRelativePlacementHnd, "RefDirection");
                        if (!IFCAnyHandleUtil.IsNullOrHasNoValue(normalHnd))
                        {
                            IList<double> coordList = IFCAnyHandleUtil.GetAggregateDoubleAttribute<List<double>>(normalHnd, "DirectionRatios");
                            placementNormal = (new XYZ(coordList[0], coordList[1], coordList[2])).Normalize();
                        }
                    }
                }
            }
            double minRelZAlongAxis = 1e+20;
            double maxRelZAlongAxis = -1e+20;

            // Making eps relatively big (0.1mm) to make sure that something that is supposed to be an opening but is missing by a tiny amount gets labelled
            // properly.
            double eps = 0.1 / (25.4 * 12);

            foreach (IFCAnyHandle rep in reps)
            {
                HashSet<IFCAnyHandle> repItems = IFCAnyHandleUtil.GetAggregateInstanceAttribute<HashSet<IFCAnyHandle>>(rep, "Items");
                if (repItems == null)
                    return;
                foreach (IFCAnyHandle repItem in repItems)
                {
                    if (!IFCAnyHandleUtil.IsSubTypeOf(repItem, IFCEntityType.IfcFacetedBrep))
                    {
                        if (IFCAnyHandleUtil.IsSubTypeOf(repItem, IFCEntityType.IfcExtrudedAreaSolid))
                        {
                            double? depthVal = IFCAnyHandleUtil.GetDoubleAttribute(repItem, "Depth");
                            if (!depthVal.HasValue)
                                return;

                            double depth = depthVal.Value;

                            // Limiting fix to cases where extrusion is orthogonal to wall axis.  This means:
                            // ExtrudedDirection = (0,0,1)
                            // Extrusion.Position.Axis = (0,1,0)
                            IFCAnyHandle extrudedDirection = IFCAnyHandleUtil.GetInstanceAttribute(repItem, "ExtrudedDirection");
                            if (IFCAnyHandleUtil.IsNullOrHasNoValue(extrudedDirection))
                                return;

                            IList<double> coordList = IFCAnyHandleUtil.GetAggregateDoubleAttribute<List<double>>(extrudedDirection, "DirectionRatios");
                            if (coordList.Count < 3)
                                return;

                            XYZ localExtrusionDirection = (new XYZ(coordList[0], coordList[1], coordList[2])).Normalize();
                            if (!MathUtil.IsAlmostEqual(localExtrusionDirection.Z, 1.0))
                                return;

                            IFCAnyHandle extrusionPosition = IFCAnyHandleUtil.GetInstanceAttribute(repItem, "Position");
                            if (IFCAnyHandleUtil.IsNullOrHasNoValue(extrusionPosition))
                                return;

                            IFCAnyHandle extrusionPositionAxisHnd = IFCAnyHandleUtil.GetInstanceAttribute(extrusionPosition, "Axis");
                            if (IFCAnyHandleUtil.IsNullOrHasNoValue(extrusionPositionAxisHnd))
                                return;

                            coordList = IFCAnyHandleUtil.GetAggregateDoubleAttribute<List<double>>(extrusionPositionAxisHnd, "DirectionRatios");
                            if (coordList.Count < 3)
                                return;
                            
                            XYZ extrusionPositionAxis = (new XYZ(coordList[0], coordList[1], coordList[2])).Normalize();
                            if (!MathUtil.IsAlmostEqual(extrusionPositionAxis.Y, 1.0))
                                return;

                            IFCAnyHandle extrusionPositionLocationHnd = IFCAnyHandleUtil.GetInstanceAttribute(extrusionPosition, "Location");
                            if (IFCAnyHandleUtil.IsNullOrHasNoValue(extrusionPositionLocationHnd))
                                return;

                            coordList = IFCAnyHandleUtil.GetAggregateDoubleAttribute<List<double>>(extrusionPositionLocationHnd, "Coordinates");
                            if (coordList.Count < 3)
                                return;

                            XYZ extrusionPositionLocation = new XYZ(coordList[0], coordList[1], coordList[2]);

                            minRelZAlongAxis = Math.Min(minRelZAlongAxis, extrusionPositionLocation.Y);
                            maxRelZAlongAxis = Math.Max(maxRelZAlongAxis, extrusionPositionLocation.Y + depth);
                            continue;
                        }
                        else
                            return;
                    }

                    IFCAnyHandle outer = IFCAnyHandleUtil.GetInstanceAttribute(repItem, "Outer");
                    if (IFCAnyHandleUtil.IsNullOrHasNoValue(outer))
                        return;

                    HashSet<IFCAnyHandle> cfsFaces = IFCAnyHandleUtil.GetAggregateInstanceAttribute<HashSet<IFCAnyHandle>>(outer, "CfsFaces");
                    if (cfsFaces == null || cfsFaces.Count == 0)
                        return;

                    // We are going to get all of the faces and compute the volume of the solid.  If it is negative, the solid is reversed.

                    // Generate bounding box, generate solid outline.
                    IList<IFCAnyHandle> listOfAllBoundaries = new List<IFCAnyHandle>();
                    IList<bool> listOfAllOrientations = new List<bool>();
                    double volume = 0.0;
                    XYZ basePoint = null;

                    try
                    {
                        foreach (IFCAnyHandle cfsFace in cfsFaces)
                        {
                            HashSet<IFCAnyHandle> bounds = IFCAnyHandleUtil.GetAggregateInstanceAttribute<HashSet<IFCAnyHandle>>(cfsFace, "Bounds");
                            if (bounds == null || bounds.Count == 0)
                                throw new InvalidOperationException("Expected bounded face.");

                            foreach (IFCAnyHandle bound in bounds)
                            {
                                IFCAnyHandle boundLoop = IFCAnyHandleUtil.GetInstanceAttribute(bound, "Bound");
                                if (IFCAnyHandleUtil.IsNullOrHasNoValue(boundLoop))
                                    throw new InvalidOperationException("Expected bounded face.");

                                if (!IFCAnyHandleUtil.IsSubTypeOf(boundLoop, IFCEntityType.IfcPolyLoop))
                                    throw new InvalidOperationException("Expected IfcPolyLoop.");

                                bool? orientationOpt = IFCAnyHandleUtil.GetBooleanAttribute(bound, "Orientation");
                                bool orientation = orientationOpt.HasValue ? orientationOpt.Value : true;

                                listOfAllBoundaries.Add(bound);
                                listOfAllOrientations.Add(orientation);

                                IList<IFCAnyHandle> polygon = IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(boundLoop, "Polygon");

                                IList<XYZ> currBound = new List<XYZ>();

                                XYZ currBasePoint = new XYZ(0, 0, 0);
                                foreach (IFCAnyHandle vertex in polygon)
                                {
                                    IList<double> vertices = IFCAnyHandleUtil.GetAggregateDoubleAttribute<List<double>>(vertex, "Coordinates");
                                    if (vertices == null || vertices.Count != 3)
                                        throw new InvalidOperationException("Expected IfcCartesianPoint of dimensionality 3.");

                                    XYZ currPoint = new XYZ(vertices[0], vertices[1], vertices[2]);
                                    if (possiblyFixRecess)
                                    {
                                        XYZ relPoint = currPoint - placementOrigin;
                                        double relZ = relPoint.DotProduct(placementNormal);
                                        minRelZAlongAxis = Math.Min(minRelZAlongAxis, relZ);
                                        maxRelZAlongAxis = Math.Max(maxRelZAlongAxis, relZ);
                                    }

                                    currBound.Add(currPoint);

                                    currBasePoint += currPoint;
                                }

                                int numVertices = currBound.Count;
                                if (numVertices < 3)
                                    throw new InvalidOperationException("Invalid outer face boundary.");

                                currBasePoint /= numVertices;
                                if (basePoint == null)
                                    basePoint = currBasePoint;
                                volume += ComputeSimpleFaceVolume(currBound, currBasePoint, basePoint, orientation);
                            }
                        }
                    }
                    catch
                    {
                        return;
                    }

                    if (volume < -eps)
                    {
                        int currIndex = 0;
                        foreach (IFCAnyHandle boundary in listOfAllBoundaries)
                        {
                            IFCAnyHandleUtil.SetAttribute(boundary, "Orientation", !listOfAllOrientations[currIndex++]);
                        }
                    }
                }

                if (possiblyFixRecess && (minRelZAlongAxis < -scaledWidth / 2.0 + eps) && (maxRelZAlongAxis > scaledWidth / 2.0 - eps))
                {
                    IFCAnyHandleUtil.SetAttribute(openingHnd, "ObjectType", "Opening");
                }
                else if (possiblyFixOpening && ((minRelZAlongAxis > -scaledWidth / 2.0 + eps) || (maxRelZAlongAxis < scaledWidth / 2.0 - eps)))
                {
                    IFCAnyHandleUtil.SetAttribute(openingHnd, "ObjectType", "Recess");
                }
            }
        }

        /// <summary>
        /// Main implementation to export walls.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="element">
        /// The element.
        /// </param>
        /// <param name="geometryElement">
        /// The geometry element.
        /// </param>
        /// <param name="origWrapper">
        /// The ProductWrapper.
        /// </param>
        /// <param name="overrideLevelId">
        /// The level id.
        /// </param>
        /// <param name="range">
        /// The range to be exported for the element.
        /// </param>
        /// <returns>
        /// The exported wall handle.
        /// </returns>
        public static IFCAnyHandle ExportWallBase(ExporterIFC exporterIFC, Element element, GeometryElement geometryElement,
           ProductWrapper origWrapper, ElementId overrideLevelId, IFCRange range)
        {
            IFCFile file = exporterIFC.GetFile();
            using (IFCTransaction tr = new IFCTransaction(file))
            {
                using (ProductWrapper localWrapper = ProductWrapper.Create(origWrapper))
                {
                    ElementId catId = CategoryUtil.GetSafeCategoryId(element);

                    Wall wallElement = element as Wall;
                    FamilyInstance famInstWallElem = element as FamilyInstance;
                    FaceWall faceWall = element as FaceWall;
                    
                    if (wallElement == null && famInstWallElem == null && faceWall == null)
                        return null;

                    if (wallElement != null && IsWallCompletelyClipped(wallElement, exporterIFC, range))
                        return null;

                    Document doc = element.Document;
                    double scale = exporterIFC.LinearScale;
                    
                    double baseWallElevation = 0.0;
                    ElementId baseLevelId = ExporterUtil.GetBaseLevelIdForElement(element);
                    if (baseLevelId != ElementId.InvalidElementId)
                    {
                        Element baseLevel = doc.GetElement(baseLevelId);
                        if (baseLevel is Level)
                            baseWallElevation = (baseLevel as Level).Elevation;
                    }
                    
                    IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();
                    IFCAnyHandle contextOfItemsAxis = exporterIFC.Get3DContextHandle("Axis");
                    IFCAnyHandle contextOfItemsBody = exporterIFC.Get3DContextHandle("Body");

                    IFCRange zSpan = new IFCRange();
                    double depth = 0.0;
                    bool validRange = (range != null && !MathUtil.IsAlmostZero(range.Start - range.End));

                    bool exportParts = PartExporter.CanExportParts(element);
                    if (exportParts && !PartExporter.CanExportElementInPartExport(element, validRange ? overrideLevelId : element.Level.Id, validRange))
                        return null;

                    // get bounding box height so that we can subtract out pieces properly.
                    // only for Wall, not FamilyInstance.
                    if (wallElement != null && geometryElement != null)
                    {
                        BoundingBoxXYZ boundingBox = element.get_BoundingBox(null);
                        if (boundingBox == null)
                            return null;
                        zSpan = new IFCRange(boundingBox.Min.Z, boundingBox.Max.Z);

                        // if we have a top clipping plane, modify depth accordingly.
                        double bottomHeight = validRange ? Math.Max(zSpan.Start, range.Start) : zSpan.Start;
                        double topHeight = validRange ? Math.Min(zSpan.End, range.End) : zSpan.End;
                        depth = topHeight - bottomHeight;
                        if (MathUtil.IsAlmostZero(depth))
                            return null;
                        depth *= scale;
                    }

                    IFCAnyHandle axisRep = null;
                    IFCAnyHandle bodyRep = null;

                    bool exportingAxis = false;
                    Curve curve = null;

                    bool exportedAsWallWithAxis = false;
                    bool exportedBodyDirectly = false;
                    bool exportingInplaceOpenings = false;

                    Curve centerCurve = GetWallAxis(wallElement);

                    XYZ localXDir = new XYZ(1, 0, 0);
                    XYZ localYDir = new XYZ(0, 1, 0);
                    XYZ localZDir = new XYZ(0, 0, 1);
                    XYZ localOrig = new XYZ(0, 0, 0);
                    double eps = MathUtil.Eps();

                    if (centerCurve != null)
                    {
                        Curve baseCurve = GetWallAxisAtBaseHeight(wallElement);
                        curve = GetWallTrimmedCurve(wallElement, baseCurve);

                        IFCRange curveBounds;
                        XYZ oldOrig;
                        GeometryUtil.GetAxisAndRangeFromCurve(curve, out curveBounds, out localXDir, out oldOrig);

                        localOrig = oldOrig;
                        if (baseCurve != null)
                        {
                            if (!validRange || (MathUtil.IsAlmostEqual(range.Start, zSpan.Start)))
                            {
                                XYZ newOrig = baseCurve.Evaluate(curveBounds.Start, false);
                                if (!validRange && (zSpan.Start < newOrig[2] - eps))
                                    localOrig = new XYZ(localOrig.X, localOrig.Y, zSpan.Start);
                                else
                                    localOrig = new XYZ(localOrig.X, localOrig.Y, newOrig[2]);
                            }
                            else
                            {
                                localOrig = new XYZ(localOrig.X, localOrig.Y, range.Start);
                            }
                        }

                        double dist = localOrig[2] - oldOrig[2];
                        if (!MathUtil.IsAlmostZero(dist))
                        {
                            XYZ moveVec = new XYZ(0, 0, dist);
                            curve = GeometryUtil.MoveCurve(curve, moveVec);
                        }
                        localYDir = localZDir.CrossProduct(localXDir);

                        // ensure that X and Z axes are orthogonal.
                        double xzDot = localZDir.DotProduct(localXDir);
                        if (!MathUtil.IsAlmostZero(xzDot))
                            localXDir = localYDir.CrossProduct(localZDir);
                    }
                    else
                    {
                        BoundingBoxXYZ boundingBox = element.get_BoundingBox(null);
                        if (boundingBox != null)
                        {
                            XYZ bBoxMin = boundingBox.Min;
                            XYZ bBoxMax = boundingBox.Max;
                            if (validRange)
                                localOrig = new XYZ(bBoxMin.X, bBoxMin.Y, range.Start);
                            else
                                localOrig = boundingBox.Min;

                            XYZ localXDirMax = null;
                            Transform bTrf = boundingBox.Transform;
                            XYZ localXDirMax1 = new XYZ(bBoxMax.X, localOrig.Y, localOrig.Z);
                            localXDirMax1 = bTrf.OfPoint(localXDirMax1);
                            XYZ localXDirMax2 = new XYZ(localOrig.X, bBoxMax.Y, localOrig.Z);
                            localXDirMax2 = bTrf.OfPoint(localXDirMax2);
                            if (localXDirMax1.DistanceTo(localOrig) >= localXDirMax2.DistanceTo(localOrig))
                                localXDirMax = localXDirMax1;
                            else
                                localXDirMax = localXDirMax2;
                            localXDir = localXDirMax.Subtract(localOrig);
                            localXDir = localXDir.Normalize();
                            localYDir = localZDir.CrossProduct(localXDir);

                            // ensure that X and Z axes are orthogonal.
                            double xzDot = localZDir.DotProduct(localXDir);
                            if (!MathUtil.IsAlmostZero(xzDot))
                                localXDir = localYDir.CrossProduct(localZDir);
                        }
                    }

                    Transform orientationTrf = Transform.Identity;
                    orientationTrf.BasisX = localXDir;
                    orientationTrf.BasisY = localYDir;
                    orientationTrf.BasisZ = localZDir;
                    orientationTrf.Origin = localOrig;

                    if (overrideLevelId == ElementId.InvalidElementId)
                        overrideLevelId = ExporterUtil.GetBaseLevelIdForElement(element);
                    using (IFCPlacementSetter setter = IFCPlacementSetter.Create(exporterIFC, element, null, orientationTrf, overrideLevelId))
                    {
                        IFCAnyHandle localPlacement = setter.GetPlacement();

                        Plane plane = new Plane(localXDir, localYDir, localOrig);  // project curve to XY plane.
                        XYZ projDir = XYZ.BasisZ;

                        // two representations: axis, body.         
                        {
                            if (!exportParts && (centerCurve != null) && (GeometryUtil.CurveIsLineOrArc(centerCurve)))
                            {
                                exportingAxis = true;

                                string identifierOpt = "Axis";	// IFC2x2 convention
                                string representationTypeOpt = "Curve2D";  // IFC2x2 convention

                                IFCGeometryInfo info = IFCGeometryInfo.CreateCurveGeometryInfo(exporterIFC, plane, projDir, false);
                                ExporterIFCUtils.CollectGeometryInfo(exporterIFC, info, curve, XYZ.Zero, true);
                                IList<IFCAnyHandle> axisItems = info.GetCurves();

                                if (axisItems.Count == 0)
                                {
                                    exportingAxis = false;
                                }
                                else
                                {
                                    HashSet<IFCAnyHandle> axisItemSet = new HashSet<IFCAnyHandle>();
                                    foreach (IFCAnyHandle axisItem in axisItems)
                                        axisItemSet.Add(axisItem);

                                    axisRep = RepresentationUtil.CreateShapeRepresentation(exporterIFC, element, catId, contextOfItemsAxis,
                                       identifierOpt, representationTypeOpt, axisItemSet);
                                }
                            }
                        }

                        IList<IFCExtrusionData> cutPairOpenings = new List<IFCExtrusionData>();
                        Document document = element.Document;

                        IList<Solid> solids = new List<Solid>();
                        IList<Mesh> meshes = new List<Mesh>();

                        if (!exportParts && wallElement != null && exportingAxis && curve != null)
                        {
                            SolidMeshGeometryInfo solidMeshInfo =
                                (range == null) ? GeometryUtil.GetSplitSolidMeshGeometry(geometryElement) :
                                    GeometryUtil.GetSplitClippedSolidMeshGeometry(geometryElement, range);

                            solids = solidMeshInfo.GetSolids();
                            meshes = solidMeshInfo.GetMeshes();
                            if (solids.Count == 0 && meshes.Count == 0)
                                return null;

                            bool useNewCode = false;
                            if (useNewCode && solids.Count == 1 && meshes.Count == 0)
                            {
                                bool completelyClipped;
                                bodyRep = ExtrusionExporter.CreateExtrusionWithClipping(exporterIFC, wallElement, catId, solids[0],
                                    plane, projDir, range, out completelyClipped);

                                if (completelyClipped)
                                    return null;

                                if (!IFCAnyHandleUtil.IsNullOrHasNoValue(bodyRep))
                                {
                                    exportedAsWallWithAxis = true;
                                    exportedBodyDirectly = true;
                                }
                                else
                                {
                                    exportedAsWallWithAxis = false;
                                    exportedBodyDirectly = false;
                                }
                            }

                            if (!exportedAsWallWithAxis)
                            {
                                // Fallback - use native routines to try to export wall.
                                bool isCompletelyClipped;
                                bodyRep = FallbackTryToCreateAsExtrusion(exporterIFC, wallElement, solidMeshInfo, baseWallElevation,
                                    catId, curve, plane, depth, zSpan, range, setter,
                                    out cutPairOpenings, out isCompletelyClipped);
                                if (isCompletelyClipped)
                                    return null;
                                if (!IFCAnyHandleUtil.IsNullOrHasNoValue(bodyRep))
                                    exportedAsWallWithAxis = true;
                            }
                        }

                        using (IFCExtrusionCreationData extraParams = new IFCExtrusionCreationData())
                        {
                            BodyData bodyData = null;

                            if (!exportedAsWallWithAxis)
                            {
                                SolidMeshGeometryInfo solidMeshCapsule = null;

                                if (wallElement != null || faceWall != null)
                                {
                                    if (validRange)
                                    {
                                        solidMeshCapsule = GeometryUtil.GetSplitClippedSolidMeshGeometry(geometryElement, range);
                                    }
                                    else
                                    {
                                        solidMeshCapsule = GeometryUtil.GetSplitSolidMeshGeometry(geometryElement);
                                    }
                                    if (solidMeshCapsule.SolidsCount() == 0 && solidMeshCapsule.MeshesCount() == 0)
                                    {
                                        return null;
                                    }
                                }
                                else
                                {
                                    GeometryElement geomElemToUse = GetGeometryFromInplaceWall(famInstWallElem);
                                    if (geomElemToUse != null)
                                    {
                                        exportingInplaceOpenings = true;
                                    }
                                    else
                                    {
                                        exportingInplaceOpenings = false;
                                        geomElemToUse = geometryElement;
                                    }
                                    Transform trf = Transform.Identity;
                                    if (geomElemToUse != geometryElement)
                                        trf = famInstWallElem.GetTransform();
                                    solidMeshCapsule = GeometryUtil.GetSplitSolidMeshGeometry(geomElemToUse, trf);
                                }

                                solids = solidMeshCapsule.GetSolids();
                                meshes = solidMeshCapsule.GetMeshes();

                                extraParams.PossibleExtrusionAxes = IFCExtrusionAxes.TryZ;   // only allow vertical extrusions!
                                extraParams.AreInnerRegionsOpenings = true;

                                BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true);
                                
                                // Swept solids are not natively exported as part of CV2.0.  
                                // We have removed the UI toggle for this, so that it is by default false, but keep for possible future use.
                                if (ExporterCacheManager.ExportOptionsCache.ExportAdvancedSweptSolids)
                                    bodyExporterOptions.TryToExportAsSweptSolid = true;

                                ElementId overrideMaterialId = ElementId.InvalidElementId;
                                if (wallElement != null)
                                    overrideMaterialId = HostObjectExporter.GetFirstLayerMaterialId(wallElement);

                                if (!exportParts)
                                {
                                    if ((solids.Count > 0) || (meshes.Count > 0))
                                    {
                                        bodyRep = BodyExporter.ExportBody(exporterIFC, element, catId, overrideMaterialId,
                                            solids, meshes, bodyExporterOptions, extraParams).RepresentationHnd;
                                    }
                                    else
                                    {
                                        IList<GeometryObject> geomElemList = new List<GeometryObject>();
                                        geomElemList.Add(geometryElement);
                                        bodyData = BodyExporter.ExportBody(exporterIFC, element, catId, overrideMaterialId,
                                            geomElemList, bodyExporterOptions, extraParams);
                                        bodyRep = bodyData.RepresentationHnd;
                                    }

                                    if (IFCAnyHandleUtil.IsNullOrHasNoValue(bodyRep))
                                    {
                                        extraParams.ClearOpenings();
                                        return null;
                                    }
                                }

                                // We will be able to export as a IfcWallStandardCase as long as we have an axis curve.
                                XYZ extrDirUsed = XYZ.Zero;
                                if (extraParams.HasExtrusionDirection)
                                {
                                    extrDirUsed = extraParams.ExtrusionDirection;
                                    if (MathUtil.IsAlmostEqual(Math.Abs(extrDirUsed[2]), 1.0))
                                    {
                                        if ((solids.Count == 1) && (meshes.Count == 0))
                                            exportedAsWallWithAxis = exportingAxis;
                                        exportedBodyDirectly = true;
                                    }
                                }
                            }

                            IFCAnyHandle prodRep = null;
                            if (!exportParts)
                            {
                                IList<IFCAnyHandle> representations = new List<IFCAnyHandle>();
                                if (exportingAxis)
                                    representations.Add(axisRep);

                                representations.Add(bodyRep);

                                IFCAnyHandle boundingBoxRep = null;
                                if ((solids.Count > 0) || (meshes.Count > 0))
                                    boundingBoxRep = BoundingBoxExporter.ExportBoundingBox(exporterIFC, solids, meshes, Transform.Identity);
                                else
                                    boundingBoxRep = BoundingBoxExporter.ExportBoundingBox(exporterIFC, geometryElement, Transform.Identity);

                                if (boundingBoxRep != null)
                                    representations.Add(boundingBoxRep);

                                prodRep = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, representations);
                            }

                            ElementId matId = ElementId.InvalidElementId;
                            string objectType = NamingUtil.CreateIFCObjectName(exporterIFC, element);
                            IFCAnyHandle wallHnd = null;

                            string elemGUID = (validRange) ? GUIDUtil.CreateGUID() : GUIDUtil.CreateGUID(element);
                            string elemName = NamingUtil.GetNameOverride(element, NamingUtil.GetIFCName(element));
                            string elemDesc = NamingUtil.GetDescriptionOverride(element, null);
                            string elemObjectType = NamingUtil.GetObjectTypeOverride(element, objectType);
                            string elemTag = NamingUtil.GetTagOverride(element, NamingUtil.CreateIFCElementId(element));

                            // For Foundation and Retaining walls, allow exporting as IfcFooting instead.
                            bool exportAsFooting = false;
                            string enumTypeValue = null;

                            if (wallElement != null)
                            {
                                WallType wallType = wallElement.WallType;

                                if (wallType != null)
                                {
                                    int wallFunction;
                                    if (ParameterUtil.GetIntValueFromElement(wallType, BuiltInParameter.FUNCTION_PARAM, out wallFunction) != null)
                                    {
                                        if (wallFunction == (int)WallFunction.Retaining || wallFunction == (int)WallFunction.Foundation)
                                        {
                                            // In this case, allow potential to export foundation and retaining walls as footing.
                                            IFCExportType exportType = ExporterUtil.GetExportType(exporterIFC, wallElement, out enumTypeValue);
                                            if (exportType == IFCExportType.ExportFooting)
                                                exportAsFooting = true;
                                        }
                                    }
                                }
                            }

                            if (exportedAsWallWithAxis)
                            {
                                if (exportAsFooting)
                                {
                                    Toolkit.IFCFootingType footingType = FootingExporter.GetIFCFootingType(element, enumTypeValue);
                                    wallHnd = IFCInstanceExporter.CreateFooting(file, elemGUID, ownerHistory, elemName, elemDesc, elemObjectType,
                                        localPlacement, exportParts ? null : prodRep, elemTag, footingType);
                                }
                                else if (exportParts)
                                {
                                    wallHnd = IFCInstanceExporter.CreateWall(file, elemGUID, ownerHistory, elemName, elemDesc, elemObjectType,
                                        localPlacement, null, elemTag);
                                }
                                else
                                {
                                    wallHnd = IFCInstanceExporter.CreateWallStandardCase(file, elemGUID, ownerHistory, elemName, elemDesc, elemObjectType,
                                        localPlacement, prodRep, elemTag);
                                }

                                if (exportParts)
                                    PartExporter.ExportHostPart(exporterIFC, element, wallHnd, localWrapper, setter, localPlacement, overrideLevelId);

                                localWrapper.AddElement(element, wallHnd, setter, extraParams, true);

                                if (!exportParts)
                                {
                                    OpeningUtil.CreateOpeningsIfNecessary(wallHnd, element, cutPairOpenings, null,
                                        exporterIFC, localPlacement, setter, localWrapper);
                                    if (exportedBodyDirectly)
                                    {
                                        Transform offsetTransform = (bodyData != null) ? bodyData.OffsetTransform : Transform.Identity;
                                        OpeningUtil.CreateOpeningsIfNecessary(wallHnd, element, extraParams, offsetTransform,
                                            exporterIFC, localPlacement, setter, localWrapper);
                                    }
                                    else
                                    {
                                        ICollection<IFCAnyHandle> beforeOpenings = localWrapper.GetAllObjects();
                                        double scaledWidth = wallElement.Width * scale;
                                        ExporterIFCUtils.AddOpeningsToElement(exporterIFC, wallHnd, wallElement, scaledWidth, range, setter, localPlacement, localWrapper.ToNative());
                                        ICollection<IFCAnyHandle> afterOpenings = localWrapper.GetAllObjects();
                                        if (beforeOpenings.Count != afterOpenings.Count)
                                        {
                                            foreach (IFCAnyHandle before in beforeOpenings)
                                                afterOpenings.Remove(before);
                                            foreach (IFCAnyHandle potentiallyBadOpening in afterOpenings)
                                            {
                                                PotentiallyCorrectOpeningOrientationAndOpeningType(potentiallyBadOpening, localPlacement, scaledWidth);
                                            }
                                        }
                                    }
                                }

                                // export Base Quantities
                                if (ExporterCacheManager.ExportOptionsCache.ExportBaseQuantities)
                                {
                                    CreateWallBaseQuantities(exporterIFC, wallElement, wallHnd, depth);
                                }
                            }
                            else
                            {
                                if (exportAsFooting)
                                {
                                    Toolkit.IFCFootingType footingType = FootingExporter.GetIFCFootingType(element, enumTypeValue);
                                    wallHnd = IFCInstanceExporter.CreateFooting(file, elemGUID, ownerHistory, elemName, elemDesc, elemObjectType,
                                        localPlacement, exportParts ? null : prodRep, elemTag, footingType);
                                }
                                else
                                {
                                    wallHnd = IFCInstanceExporter.CreateWall(file, elemGUID, ownerHistory, elemName, elemDesc, elemObjectType,
                                        localPlacement, exportParts ? null : prodRep, elemTag);
                                }

                                if (exportParts)
                                    PartExporter.ExportHostPart(exporterIFC, element, wallHnd, localWrapper, setter, localPlacement, overrideLevelId);

                                localWrapper.AddElement(element, wallHnd, setter, extraParams, true);

                                if (!exportParts)
                                {
                                    // Only export one material for 2x2; for future versions, export the whole list.
                                    if (exporterIFC.ExportAs2x2 || famInstWallElem != null)
                                    {
                                        matId = BodyExporter.GetBestMaterialIdFromGeometryOrParameter(solids, meshes, element);
                                        if (matId != ElementId.InvalidElementId)
                                            CategoryUtil.CreateMaterialAssociation(exporterIFC, wallHnd, matId);
                                    }

                                    if (exportingInplaceOpenings)
                                    {
                                        ExporterIFCUtils.AddOpeningsToElement(exporterIFC, wallHnd, famInstWallElem, 0.0, range, setter, localPlacement, localWrapper.ToNative());
                                    }

                                    if (exportedBodyDirectly)
                                    {
                                        Transform offsetTransform = (bodyData != null) ? bodyData.OffsetTransform : Transform.Identity;
                                        OpeningUtil.CreateOpeningsIfNecessary(wallHnd, element, extraParams, offsetTransform,
                                            exporterIFC, localPlacement, setter, localWrapper);
                                    }
                                }
                            }

                            ElementId wallLevelId = (validRange) ? setter.LevelId : ElementId.InvalidElementId;

                            if ((wallElement != null || faceWall != null) && !exportParts)
                            {
                                HostObject hostObject = null;
                                if (wallElement != null)
                                    hostObject = wallElement;
                                else
                                    hostObject = faceWall;
                                if (!exporterIFC.ExportAs2x2 || exportedAsWallWithAxis)
                                    HostObjectExporter.ExportHostObjectMaterials(exporterIFC, hostObject, localWrapper.GetAnElement(),
                                        geometryElement, localWrapper, wallLevelId, Toolkit.IFCLayerSetDirection.Axis2, !exportedAsWallWithAxis);
                            }

                            ExportWallType(exporterIFC, localWrapper, wallHnd, element, matId, exportedAsWallWithAxis, exportAsFooting);

                            exporterIFC.RegisterSpaceBoundingElementHandle(wallHnd, element.Id, wallLevelId);

                            tr.Commit();
                            return wallHnd;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Exports element as Wall.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="element">The element.</param>
        /// <param name="geometryElement">The geometry element.</param>
        /// <param name="productWrapper">The ProductWrapper.</param>
        public static void ExportWall(ExporterIFC exporterIFC, Element element, GeometryElement geometryElement,
           ProductWrapper productWrapper)
        {
            IList<IFCAnyHandle> createdWalls = new List<IFCAnyHandle>();

            // We will not split walls and columns if the assemblyId is set, as we would like to keep the original wall
            // associated with the assembly, on the level of the assembly.
            bool splitWall = ExporterCacheManager.ExportOptionsCache.WallAndColumnSplitting && (element.AssemblyInstanceId == ElementId.InvalidElementId);
            if (splitWall)
            {
                Wall wallElement = element as Wall;
                IList<ElementId> levels = new List<ElementId>();
                IList<IFCRange> ranges = new List<IFCRange>();
                if (wallElement != null && geometryElement != null)
                {
                    LevelUtil.CreateSplitLevelRangesForElement(exporterIFC, IFCExportType.ExportWall, element, out levels, out ranges);
                }

                int numPartsToExport = ranges.Count;
                if (numPartsToExport == 0)
                {
                    IFCAnyHandle wallElemHnd = ExportWallBase(exporterIFC, element, geometryElement, productWrapper, ElementId.InvalidElementId, null);
                    if (!IFCAnyHandleUtil.IsNullOrHasNoValue(wallElemHnd))
                        createdWalls.Add(wallElemHnd);
                }
                else
                {
                        IFCAnyHandle wallElemHnd = ExportWallBase(exporterIFC, element, geometryElement, productWrapper, levels[0], ranges[0]);
                        if (!IFCAnyHandleUtil.IsNullOrHasNoValue(wallElemHnd))
                            createdWalls.Add(wallElemHnd);
                        for (int ii = 1; ii < numPartsToExport; ii++)
                        {
                            wallElemHnd = ExportWallBase(exporterIFC, element, geometryElement, productWrapper, levels[ii], ranges[ii]);
                            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(wallElemHnd))
                                createdWalls.Add(wallElemHnd);
                        }
                    }

                if (ExporterCacheManager.DummyHostCache.HasRegistered(element.Id))
                {
                        List<KeyValuePair<ElementId, IFCRange>> levelRangeList = ExporterCacheManager.DummyHostCache.Find(element.Id);
                        foreach (KeyValuePair<ElementId, IFCRange> levelRange in levelRangeList)
                        {
                            IFCAnyHandle wallElemHnd = ExportDummyWall(exporterIFC, element, geometryElement, productWrapper, levelRange.Key, levelRange.Value);
                            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(wallElemHnd))
                                createdWalls.Add(wallElemHnd);
                        }
                    }
                }

            if (createdWalls.Count == 0)
                ExportWallBase(exporterIFC, element, geometryElement, productWrapper, ElementId.InvalidElementId, null);
        }

        /// <summary>
        /// Exports Walls.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="wallElement">
        /// The wall element.
        /// </param>
        /// <param name="geometryElement">
        /// The geometry element.
        /// </param>
        /// <param name="productWrapper">
        /// The ProductWrapper.
        /// </param>
        public static void Export(ExporterIFC exporterIFC, Wall wallElement, GeometryElement geometryElement, ProductWrapper productWrapper)
        {
            IFCFile file = exporterIFC.GetFile();
            using (IFCTransaction tr = new IFCTransaction(file))
            {
                WallType wallType = wallElement.WallType;
                WallKind wallTypeKind = wallType.Kind;

                //stacked wall is not supported yet.
                if (wallTypeKind == WallKind.Stacked)
                    return;

                if (CurtainSystemExporter.IsCurtainSystem(wallElement))
                    CurtainSystemExporter.ExportWall(exporterIFC, wallElement, productWrapper);
                else
                    ExportWall(exporterIFC, wallElement, geometryElement, productWrapper);

                // create join information.
                ElementId id = wallElement.Id;

                IList<IList<IFCConnectedWallData>> connectedWalls = new List<IList<IFCConnectedWallData>>();
                connectedWalls.Add(ExporterIFCUtils.GetConnectedWalls(wallElement, IFCConnectedWallDataLocation.Start));
                connectedWalls.Add(ExporterIFCUtils.GetConnectedWalls(wallElement, IFCConnectedWallDataLocation.End));
                for (int ii = 0; ii < 2; ii++)
                {
                    int count = connectedWalls[ii].Count;
                    IFCConnectedWallDataLocation currConnection = (ii == 0) ? IFCConnectedWallDataLocation.Start : IFCConnectedWallDataLocation.End;
                    for (int jj = 0; jj < count; jj++)
                    {
                        ElementId otherId = connectedWalls[ii][jj].ElementId;
                        IFCConnectedWallDataLocation joinedEnd = connectedWalls[ii][jj].Location;

                        if ((otherId == id) && (joinedEnd == currConnection))  //self-reference
                            continue;

                        ExporterCacheManager.WallConnectionDataCache.Add(new WallConnectionData(id, otherId, GetIFCConnectionTypeFromLocation(currConnection),
                            GetIFCConnectionTypeFromLocation(joinedEnd), null));
                    }
                }

                // look for connected columns.  Note that this is only for columns that interrupt the wall path.
                IList<FamilyInstance> attachedColumns = ExporterIFCUtils.GetAttachedColumns(wallElement);
                int numAttachedColumns = attachedColumns.Count;
                for (int ii = 0; ii < numAttachedColumns; ii++)
                {
                    ElementId otherId = attachedColumns[ii].Id;

                    IFCConnectionType connect1 = IFCConnectionType.NotDefined;   // can't determine at the moment.
                    IFCConnectionType connect2 = IFCConnectionType.NotDefined;   // meaningless for column

                    ExporterCacheManager.WallConnectionDataCache.Add(new WallConnectionData(id, otherId, connect1, connect2, null));
                }

                tr.Commit();
            }
        }

        /// <summary>
        /// Export the dummy wall to host an orphan part. It usually happens in the cases of associated parts are higher than split sub-wall.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="element">The wall element.</param>
        /// <param name="geometryElement">The geometry of wall.</param>
        /// <param name="origWrapper">The ProductWrapper.</param>
        /// <param name="overrideLevelId">The ElementId that will crate the dummy wall.</param>
        /// <param name="range">The IFCRange corresponding to the dummy wall.</param>
        /// <returns>The handle of dummy wall.</returns>
        public static IFCAnyHandle ExportDummyWall(ExporterIFC exporterIFC, Element element, GeometryElement geometryElement,
           ProductWrapper origWrapper, ElementId overrideLevelId, IFCRange range)
        {
            using (ProductWrapper localWrapper = ProductWrapper.Create(origWrapper))
            {
                ElementId catId = CategoryUtil.GetSafeCategoryId(element);

                Wall wallElement = element as Wall;
                if (wallElement == null)
                    return null;

                if (wallElement != null && IsWallCompletelyClipped(wallElement, exporterIFC, range))
                    return null;

                // get global values.
                Document doc = element.Document;
                double scale = exporterIFC.LinearScale;
                
                IFCFile file = exporterIFC.GetFile();
                IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();

                bool validRange = (range != null && !MathUtil.IsAlmostZero(range.Start - range.End));

                bool exportParts = PartExporter.CanExportParts(wallElement);
                if (exportParts && !PartExporter.CanExportElementInPartExport(wallElement, validRange ? overrideLevelId : wallElement.Level.Id, validRange))
                    return null;

                string objectType = NamingUtil.CreateIFCObjectName(exporterIFC, element);
                IFCAnyHandle wallHnd = null;

                string elemGUID = (validRange) ? GUIDUtil.CreateGUID() : GUIDUtil.CreateGUID(element);
                string elemName = NamingUtil.GetNameOverride(element, NamingUtil.GetIFCName(element));
                string elemDesc = NamingUtil.GetDescriptionOverride(element, null);
                string elemObjectType = NamingUtil.GetObjectTypeOverride(element, objectType);
                string elemTag = NamingUtil.GetTagOverride(element, NamingUtil.CreateIFCElementId(element));

                Transform orientationTrf = Transform.Identity;

                if (overrideLevelId == ElementId.InvalidElementId)
                    overrideLevelId = ExporterUtil.GetBaseLevelIdForElement(element);
                using (IFCPlacementSetter setter = IFCPlacementSetter.Create(exporterIFC, element, null, orientationTrf, overrideLevelId))
                {
                    IFCAnyHandle localPlacement = setter.GetPlacement();
                    wallHnd = IFCInstanceExporter.CreateWall(file, elemGUID, ownerHistory, elemName, elemDesc, elemObjectType,
                                    localPlacement, null, elemTag);

                    if (exportParts)
                        PartExporter.ExportHostPart(exporterIFC, element, wallHnd, localWrapper, setter, localPlacement, overrideLevelId);

                    IFCExtrusionCreationData extraParams = new IFCExtrusionCreationData();
                    extraParams.PossibleExtrusionAxes = IFCExtrusionAxes.TryZ;   // only allow vertical extrusions!
                    extraParams.AreInnerRegionsOpenings = true;
                    localWrapper.AddElement(element, wallHnd, setter, extraParams, true);

                    ElementId wallLevelId = (validRange) ? setter.LevelId : ElementId.InvalidElementId;
                    exporterIFC.RegisterSpaceBoundingElementHandle(wallHnd, element.Id, wallLevelId);
                }

                return wallHnd;
            }
        }

        /// <summary>
        /// Exports wall types.
        /// </summary>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="wrapper">The ProductWrapper class.</param>
        /// <param name="elementHandle">The element handle.</param>
        /// <param name="element">The element.</param>
        /// <param name="overrideMaterialId">The material id used for the element type.</param>
        /// <param name="isStandard">True if it is a standard wall, false otherwise.</param>
        /// <param name="asFooting">Export as IfcFootingType instead.</param>
        public static void ExportWallType(ExporterIFC exporterIFC, ProductWrapper wrapper, IFCAnyHandle elementHandle, Element element, ElementId overrideMaterialId, 
            bool isStandard, bool asFooting)
        {
            if (elementHandle == null || element == null)
                return;

            Document doc = element.Document;
            ElementId typeElemId = element.GetTypeId();
            Element elementType = doc.GetElement(typeElemId);
            if (elementType == null)
                return;

            IFCAnyHandle wallType = null;
            if (ExporterCacheManager.WallTypeCache.TryGetValue(typeElemId, out wallType))
            {
                ExporterCacheManager.TypeRelationsCache.Add(wallType, elementHandle);
                return;
            }

            string elemGUID = GUIDUtil.CreateGUID(elementType);
            string elemName = NamingUtil.GetNameOverride(elementType, NamingUtil.GetIFCName(elementType));
            string elemDesc = NamingUtil.GetDescriptionOverride(elementType, null);
            string elemTag = NamingUtil.GetTagOverride(elementType, NamingUtil.CreateIFCElementId(elementType));
            string elemApplicableOccurence = NamingUtil.GetOverrideStringValue(elementType, "IfcApplicableOccurence", null);
            string elemElementType = NamingUtil.GetOverrideStringValue(elementType, "IfcElementType", null);

            // Property sets will be set later.
            if (asFooting)
                wallType = IFCInstanceExporter.CreateFootingType(exporterIFC.GetFile(), elemGUID, exporterIFC.GetOwnerHistoryHandle(),
                    elemName, elemDesc, elemApplicableOccurence, null, null, null, null, null);
            else
                wallType = IFCInstanceExporter.CreateWallType(exporterIFC.GetFile(), elemGUID, exporterIFC.GetOwnerHistoryHandle(),
                    elemName, elemDesc, elemApplicableOccurence, null, null, elemTag, elemElementType, isStandard ? IFCWallType.Standard : IFCWallType.NotDefined);

            wrapper.RegisterHandleWithElementType(elementType as ElementType, wallType, null);

            if (overrideMaterialId != ElementId.InvalidElementId)
            {
                CategoryUtil.CreateMaterialAssociation(exporterIFC, wallType, overrideMaterialId);
            }
            else
            {
                // try to get material set from the cache
                IFCAnyHandle materialLayerSet = ExporterCacheManager.MaterialLayerSetCache.Find(typeElemId);
                if (materialLayerSet != null)
                    ExporterCacheManager.MaterialLayerRelationsCache.Add(materialLayerSet, wallType);
            }

            ExporterCacheManager.WallTypeCache[typeElemId] = wallType;
            ExporterCacheManager.TypeRelationsCache.Add(wallType, elementHandle);
        }

        /// <summary>
        /// Checks if the wall is clipped completely.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="wallElement">
        /// The wall element.
        /// </param>
        /// <param name="range">
        /// The range of which may clip the wall.
        /// </param>
        /// <returns>
        /// True if the wall is clipped completely, false otherwise.
        /// </returns>
        static bool IsWallCompletelyClipped(Wall wallElement, ExporterIFC exporterIFC, IFCRange range)
        {
            return ExporterIFCUtils.IsWallCompletelyClipped(wallElement, exporterIFC, range);
        }

        /// <summary>
        /// Gets wall axis.
        /// </summary>
        /// <param name="wallElement">
        /// The wall element.
        /// </param>
        /// <returns>
        /// The curve.
        /// </returns>
        static Curve GetWallAxis(Wall wallElement)
        {
            if (wallElement == null)
                return null;
            LocationCurve locationCurve = wallElement.Location as LocationCurve;
            return locationCurve.Curve;
        }

        /// <summary>
        /// Gets wall axis at base height.
        /// </summary>
        /// <param name="wallElement">
        /// The wall element.
        /// </param>
        /// <returns>
        /// The curve.
        /// </returns>
        static Curve GetWallAxisAtBaseHeight(Wall wallElement)
        {
            LocationCurve locationCurve = wallElement.Location as LocationCurve;
            Curve nonBaseCurve = locationCurve.Curve;

            double baseOffset = ExporterIFCUtils.GetWallBaseOffset(wallElement);

            Transform trf = Transform.get_Translation(new XYZ(0, 0, baseOffset));

            return nonBaseCurve.get_Transformed(trf);
        }

        /// <summary>
        /// Gets wall trimmed curve.
        /// </summary>
        /// <param name="wallElement">
        /// The wall element.
        /// </param>
        /// <param name="baseCurve">
        /// The base curve.
        /// </param>
        /// <returns>
        /// The curve.
        /// </returns>
        static Curve GetWallTrimmedCurve(Wall wallElement, Curve baseCurve)
        {
            Curve result = ExporterIFCUtils.GetWallTrimmedCurve(wallElement);
            if (result == null)
                return baseCurve;

            return result;
        }

        /// <summary>
        /// Identifies if the wall has a sketched elevation profile.
        /// </summary>
        /// <param name="wallElement">
        /// The wall element.
        /// </param>
        /// <returns>
        /// True if the wall has a sketch elevation profile, false otherwise.
        /// </returns>
        static bool HasElevationProfile(Wall wallElement)
        {
            return ExporterIFCUtils.HasElevationProfile(wallElement);
        }

        /// <summary>
        /// Obtains the curve loops which bound the wall's elevation profile.
        /// </summary>
        /// <param name="wallElement">
        /// The wall element.
        /// </param>
        /// <returns>
        /// The collection of curve loops.
        /// </returns>
        static IList<CurveLoop> GetElevationProfile(Wall wallElement)
        {
            return ExporterIFCUtils.GetElevationProfile(wallElement);
        }

        /// <summary>
        /// Identifies if the base geometry of the wall can be represented as an extrusion.
        /// </summary>
        /// <param name="element">
        /// The wall element.
        /// </param>
        /// <param name="range">
        /// The range. This consists of two double values representing the height in Z at the start and the end
        /// of the range.  If the values are identical the entire wall is used.
        /// </param>
        /// <returns>
        /// True if the wall export can be made in the form of an extrusion, false if the
        /// geometry cannot be assigned to an extrusion.
        /// </returns>
        static bool CanExportWallGeometryAsExtrusion(Element element, IFCRange range)
        {
            return ExporterIFCUtils.CanExportWallGeometryAsExtrusion(element, range);
        }

        /// <summary>
        /// Obtains a special snapshot of the geometry of an in-place wall element suitable for export.
        /// </summary>
        /// <param name="famInstWallElem">
        /// The in-place wall instance.
        /// </param>
        /// <returns>
        /// The in-place wall geometry.
        /// </returns>
        static GeometryElement GetGeometryFromInplaceWall(FamilyInstance famInstWallElem)
        {
            return ExporterIFCUtils.GetGeometryFromInplaceWall(famInstWallElem);
        }

        /// <summary>
        /// Obtains a special snapshot of the geometry of an in-place wall element suitable for export.
        /// </summary>
        /// <param name="wallElement">
        /// The wall.
        /// </param>
        /// <returns>
        /// The direction of the wall.
        /// </returns>
        static XYZ GetWallHeightDirection(Wall wallElement)
        {
            return ExporterIFCUtils.GetWallHeightDirection(wallElement);
        }

        /// <summary>
        /// Identifies if the wall's base can be represented by a direct thickening of the wall's base curve.
        /// </summary>
        /// <param name="wallElement">
        /// The wall.
        /// </param>
        /// <param name="curve">
        /// The wall's base curve.
        /// </param>
        /// <returns>
        /// True if the wall's base can be represented by a direct thickening of the wall's base curve.
        /// False is the wall's base shape is affected by other geometry, and thus cannot be represented
        /// by a direct thickening of the wall's base cure.
        /// </returns>
        static bool IsWallBaseRectangular(Wall wallElement, Curve curve)
        {
            return ExporterIFCUtils.IsWallBaseRectangular(wallElement, curve);
        }

        /// <summary>
        /// Gets the curve loop(s) that represent the bottom or top face of the wall.
        /// </summary>
        /// <param name="wallElement">
        /// The wall.
        /// </param>
        /// <param name="exporterIFC">
        /// The exporter.
        /// </param>
        /// <returns>
        /// The curve loops.
        /// </returns>
        static IList<CurveLoop> GetLoopsFromTopBottomFace(Wall wallElement, ExporterIFC exporterIFC)
        {
            return ExporterIFCUtils.GetLoopsFromTopBottomFace(exporterIFC, wallElement);
        }

        /// <summary>
        /// Processes the geometry of the wall to create an extruded area solid representing the geometry of the wall (including
        /// any clippings imposed by neighboring elements).
        /// </summary>
        /// <param name="exporterIFC">
        /// The exporter.
        /// </param>
        /// <param name="wallElement">
        /// The wall.
        /// </param>
        /// <param name="setterOffset">
        /// The offset from the placement setter.
        /// </param>
        /// <param name="range">
        /// The range.  This consists of two double values representing the height in Z at the start and the end
        /// of the range.  If the values are identical the entire wall is used.
        /// </param>
        /// <param name="zSpan">
        /// The overall span in Z of the wall.
        /// </param>
        /// <param name="baseBodyItemHnd">
        /// The IfcExtrudedAreaSolid handle generated initially for the wall.
        /// </param>
        /// <param name="cutPairOpenings">
        /// A collection of extruded openings that can be derived from the wall geometry.
        /// </param>
        /// <returns>
        /// IfcEtxtrudedAreaSolid handle.  This may be the same handle as was input, or a modified handle derived from the clipping
        /// geometry.  If the function fails this handle will have no value.
        /// </returns>
        static IFCAnyHandle AddClippingsToBaseExtrusion(ExporterIFC exporterIFC, Wall wallElement,
           XYZ setterOffset, IFCRange range, IFCRange zSpan, IFCAnyHandle baseBodyItemHnd, out IList<IFCExtrusionData> cutPairOpenings)
        {
            return ExporterIFCUtils.AddClippingsToBaseExtrusion(exporterIFC, wallElement, setterOffset, range, zSpan, baseBodyItemHnd, out cutPairOpenings);
        }

        /// <summary>
        /// Creates the wall base quantities and adds them to the export.
        /// </summary>
        /// <param name="exporterIFC">
        /// The exporter.
        /// </param>
        /// <param name="wallHnd">
        /// The wall element handle.
        /// </param>
        /// <param name="wallElement">
        /// The wall element.
        /// </param>
        /// <param name="depth">
        /// The depth of the wall.
        /// </param>
        static void CreateWallBaseQuantities(ExporterIFC exporterIFC, Wall wallElement, IFCAnyHandle wallHnd, double depth)
        {
            ExporterIFCUtils.CreateWallBaseQuantities(exporterIFC, wallHnd, wallElement, depth);
        }

        /// <summary>
        /// Gets IFCConnectionType from IFCConnectedWallDataLocation.
        /// </summary>
        /// <param name="location">The IFCConnectedWallDataLocation.</param>
        /// <returns>The IFCConnectionType.</returns>
        static IFCConnectionType GetIFCConnectionTypeFromLocation(IFCConnectedWallDataLocation location)
        {
            switch (location)
            {
                case IFCConnectedWallDataLocation.Start:
                    return IFCConnectionType.AtStart;
                case IFCConnectedWallDataLocation.End:
                    return IFCConnectionType.AtEnd;
                case IFCConnectedWallDataLocation.Path:
                    return IFCConnectionType.AtPath;
                case IFCConnectedWallDataLocation.NotDefined:
                    return IFCConnectionType.NotDefined;
                default:
                    throw new ArgumentException("Invalid IFCConnectedWallDataLocation", "location");
            }
        }
    }
}
