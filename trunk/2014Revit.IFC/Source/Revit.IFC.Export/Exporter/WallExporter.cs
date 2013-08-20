//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2013  Autodesk, Inc.
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
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Exporter
{
    /// <summary>
    /// Provides methods to export walls.
    /// </summary>
    class WallExporter
    {
        private static IFCAnyHandle FallbackTryToCreateAsExtrusion(ExporterIFC exporterIFC, Wall wallElement, SolidMeshGeometryInfo smCapsule, double baseWallElevation,
            ElementId catId, Curve curve, Plane plane, double depth, IFCRange zSpan, IFCRange range, PlacementSetter setter,
            out IList<IFCExtrusionData> cutPairOpenings, out bool isCompletelyClipped, out double scaledFootprintArea, out double scaledLength)
        {
            cutPairOpenings = new List<IFCExtrusionData>();

            IFCAnyHandle bodyRep;
            isCompletelyClipped = false;
            scaledFootprintArea = 0;
            scaledLength = curve != null ? UnitUtil.ScaleLength(curve.Length) : 0;

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
                CurveLoop newLoop = CurveLoop.CreateViaThicken(curve, wallElement.Width, XYZ.BasisZ);
                if (newLoop == null)
                    return null;

                if (!newLoop.IsCounterclockwise(XYZ.BasisZ))
                    newLoop = GeometryUtil.ReverseOrientation(newLoop);
                boundaryLoops.Add(newLoop);
            }

            scaledFootprintArea = ExporterIFCUtils.ComputeAreaOfCurveLoops(boundaryLoops);

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
                    
                    double baseWallElevation = 0.0;
                    ElementId baseLevelId = PlacementSetter.GetBaseLevelIdForElement(element);
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
                    if (exportParts && !PartExporter.CanExportElementInPartExport(element, validRange ? overrideLevelId : element.LevelId, validRange))
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
                        depth = UnitUtil.ScaleLength(depth);
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

                    double scaledFootprintArea = 0;
                    double scaledLength = 0;

                    using (PlacementSetter setter = PlacementSetter.Create(exporterIFC, element, null, orientationTrf, overrideLevelId))
                    {
                        IFCAnyHandle localPlacement = setter.LocalPlacement;

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
                                    out cutPairOpenings, out isCompletelyClipped, out scaledFootprintArea, out scaledLength);
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

                            string elemGUID = null;
                            int subElementIndex = ExporterStateManager.GetCurrentRangeIndex();
                            if (subElementIndex == 0)
                                elemGUID = GUIDUtil.CreateGUID(element);
                            else if (subElementIndex <= ExporterStateManager.RangeIndexSetter.GetMaxStableGUIDs())
                                elemGUID = GUIDUtil.CreateSubElementGUID(element, subElementIndex + (int)IFCGenericSubElements.SplitInstanceStart - 1);
                            else
                                elemGUID = GUIDUtil.CreateGUID();
                            
                            string elemName = NamingUtil.GetNameOverride(element, NamingUtil.GetIFCName(element));
                            string elemDesc = NamingUtil.GetDescriptionOverride(element, null);
                            string elemObjectType = NamingUtil.GetObjectTypeOverride(element, objectType);
                            string elemTag = NamingUtil.GetTagOverride(element, NamingUtil.CreateIFCElementId(element));

                            string ifcType = IFCValidateEntry.GetValidIFCType(element, null);

                            // For Foundation and Retaining walls, allow exporting as IfcFooting instead.
                            bool exportAsFooting = false;
                            if (wallElement != null)
                            {
                                WallType wallType = wallElement.WallType;

                                if (wallType != null)
                                {
                                    int wallFunction;
                                    if (ParameterUtil.GetIntValueFromElement(wallType, BuiltInParameter.FUNCTION_PARAM, out wallFunction))
                                    {
                                        if (wallFunction == (int)WallFunction.Retaining || wallFunction == (int)WallFunction.Foundation)
                                        {
                                            // In this case, allow potential to export foundation and retaining walls as footing.
                                            string enumTypeValue = null;
                                            IFCExportType exportType = ExporterUtil.GetExportType(exporterIFC, wallElement, out enumTypeValue);
                                            if (exportType == IFCExportType.IfcFooting)
                                                exportAsFooting = true;
                                        }
                                    }
                                }
                            }

                            if (exportedAsWallWithAxis)
                            {
                                if (exportAsFooting)
                                {
                                    wallHnd = IFCInstanceExporter.CreateFooting(file, elemGUID, ownerHistory, elemName, elemDesc, elemObjectType,
                                        localPlacement, exportParts ? null : prodRep, elemTag, ifcType);
                                }
                                else if (exportParts)
                                {
                                    wallHnd = IFCInstanceExporter.CreateWall(file, elemGUID, ownerHistory, elemName, elemDesc, elemObjectType,
                                            localPlacement, null, elemTag, ifcType);
                                }
                                else
                                {
                                    wallHnd = IFCInstanceExporter.CreateWallStandardCase(file, elemGUID, ownerHistory, elemName, elemDesc, elemObjectType,
                                        localPlacement, prodRep, elemTag, ifcType);
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
                                        double scaledWidth = UnitUtil.ScaleLength(wallElement.Width);
                                        OpeningUtil.AddOpeningsToElement(exporterIFC, wallHnd, wallElement, null, scaledWidth, range, setter, localPlacement, localWrapper);
                                    }
                                }

                                // export Base Quantities
                                if (ExporterCacheManager.ExportOptionsCache.ExportBaseQuantities)
                                {
                                    scaledFootprintArea = MathUtil.AreaIsAlmostZero(scaledFootprintArea) ? extraParams.ScaledArea : scaledFootprintArea;
                                    scaledLength = MathUtil.IsAlmostZero(scaledLength) ? extraParams.ScaledLength : scaledLength;
                                    PropertyUtil.CreateWallBaseQuantities(exporterIFC, wallElement, wallHnd, scaledLength, depth, scaledFootprintArea);
                                }
                            }
                            else
                            {
                                if (exportAsFooting)
                                {
                                    wallHnd = IFCInstanceExporter.CreateFooting(file, elemGUID, ownerHistory, elemName, elemDesc, elemObjectType,
                                        localPlacement, exportParts ? null : prodRep, elemTag, ifcType);
                                }
                                else
                                {
                                    wallHnd = IFCInstanceExporter.CreateWall(file, elemGUID, ownerHistory, elemName, elemDesc, elemObjectType,
                                        localPlacement, exportParts ? null : prodRep, elemTag, ifcType);
                                }

                                if (exportParts)
                                    PartExporter.ExportHostPart(exporterIFC, element, wallHnd, localWrapper, setter, localPlacement, overrideLevelId);

                                localWrapper.AddElement(element, wallHnd, setter, extraParams, true);

                                if (!exportParts)
                                {
                                    // Only export one material for 2x2; for future versions, export the whole list.
                                    if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2 || famInstWallElem != null)
                                    {
                                        matId = BodyExporter.GetBestMaterialIdFromGeometryOrParameter(solids, meshes, element);
                                        if (matId != ElementId.InvalidElementId)
                                            CategoryUtil.CreateMaterialAssociation(doc, exporterIFC, wallHnd, matId);
                                    }

                                    if (exportingInplaceOpenings)
                                    {
                                        OpeningUtil.AddOpeningsToElement(exporterIFC, wallHnd, famInstWallElem, null, 0.0, range, setter, localPlacement, localWrapper);
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
                                if (!ExporterCacheManager.ExportOptionsCache.ExportAs2x2 || exportedAsWallWithAxis) //will move this check into ExportHostObject
                                    HostObjectExporter.ExportHostObjectMaterials(exporterIFC, hostObject, localWrapper.GetAnElement(),
                                        geometryElement, localWrapper, wallLevelId, Toolkit.IFCLayerSetDirection.Axis2);
                            }

                            ExportWallType(exporterIFC, localWrapper, wallHnd, element, matId, exportedAsWallWithAxis, exportAsFooting);

                            SpaceBoundingElementUtil.RegisterSpaceBoundingElementHandle(exporterIFC, wallHnd, element.Id, wallLevelId);

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
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="element">
        /// The element.
        /// </param>
        /// <param name="geometryElement">
        /// The geometry element.
        /// </param>
        /// <param name="productWrapper">
        /// The ProductWrapper.
        /// </param>
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
                    LevelUtil.CreateSplitLevelRangesForElement(exporterIFC, IFCExportType.IfcWall, element, out levels, out ranges);
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
                    using (ExporterStateManager.RangeIndexSetter rangeSetter = new ExporterStateManager.RangeIndexSetter())
                    {
                        rangeSetter.IncreaseRangeIndex();
                        IFCAnyHandle wallElemHnd = ExportWallBase(exporterIFC, element, geometryElement, productWrapper, levels[0], ranges[0]);
                        if (!IFCAnyHandleUtil.IsNullOrHasNoValue(wallElemHnd))
                            createdWalls.Add(wallElemHnd);

                        for (int ii = 1; ii < numPartsToExport; ii++)
                        {
                            rangeSetter.IncreaseRangeIndex();
                            wallElemHnd = ExportWallBase(exporterIFC, element, geometryElement, productWrapper, levels[ii], ranges[ii]);
                            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(wallElemHnd))
                                createdWalls.Add(wallElemHnd);
                        }
                    }
                }

                if (ExporterCacheManager.DummyHostCache.HasRegistered(element.Id))
                {
                    using (ExporterStateManager.RangeIndexSetter rangeSetter = new ExporterStateManager.RangeIndexSetter())
                    {
                        List<KeyValuePair<ElementId, IFCRange>> levelRangeList = ExporterCacheManager.DummyHostCache.Find(element.Id);
                        foreach (KeyValuePair<ElementId, IFCRange> levelRange in levelRangeList)
                        {
                            rangeSetter.IncreaseRangeIndex();
                            IFCAnyHandle wallElemHnd = ExportDummyWall(exporterIFC, element, geometryElement, productWrapper, levelRange.Key, levelRange.Value);
                            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(wallElemHnd))
                                createdWalls.Add(wallElemHnd);
                        }
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
                WallType wallType =  wallElement.WallType;
                WallKind wallTypeKind = wallType.Kind;

                //stacked wall is not supported yet.
                if (wallTypeKind == WallKind.Stacked)
                    return;

                if (CurtainSystemExporter.IsCurtainSystem(wallElement))
                    CurtainSystemExporter.ExportWall(exporterIFC, wallElement, productWrapper);
                else
                {
                    // ExportWall may decide to export as an IfcFooting for some retaining and foundation walls.
                    ExportWall(exporterIFC, wallElement, geometryElement, productWrapper);
                }

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
                
                IFCFile file = exporterIFC.GetFile();
                IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();

                bool validRange = (range != null && !MathUtil.IsAlmostZero(range.Start - range.End));

                bool exportParts = PartExporter.CanExportParts(wallElement);
                if (exportParts && !PartExporter.CanExportElementInPartExport(wallElement, validRange ? overrideLevelId : wallElement.LevelId, validRange))
                    return null;

                string objectType = NamingUtil.CreateIFCObjectName(exporterIFC, element);
                IFCAnyHandle wallHnd = null;

                string elemGUID = null;
                int subElementIndex = ExporterStateManager.GetCurrentRangeIndex();
                if (subElementIndex == 0)
                    elemGUID = GUIDUtil.CreateGUID(element);
                else if (subElementIndex <= ExporterStateManager.RangeIndexSetter.GetMaxStableGUIDs())
                    elemGUID = GUIDUtil.CreateSubElementGUID(element, subElementIndex + (int)IFCGenericSubElements.SplitInstanceStart - 1);
                else
                    elemGUID = GUIDUtil.CreateGUID();

                string elemName = NamingUtil.GetNameOverride(element, NamingUtil.GetIFCName(element));
                string elemDesc = NamingUtil.GetDescriptionOverride(element, null);
                string elemObjectType = NamingUtil.GetObjectTypeOverride(element, objectType);
                string elemTag = NamingUtil.GetTagOverride(element, NamingUtil.CreateIFCElementId(element));

                Transform orientationTrf = Transform.Identity;

                using (PlacementSetter setter = PlacementSetter.Create(exporterIFC, element, null, orientationTrf, overrideLevelId))
                {
                    IFCAnyHandle localPlacement = setter.LocalPlacement;
                    wallHnd = IFCInstanceExporter.CreateWall(file, elemGUID, ownerHistory, elemName, elemDesc, elemObjectType,
                        localPlacement, null, elemTag, "NOTDEFINED");

                    if (exportParts)
                        PartExporter.ExportHostPart(exporterIFC, element, wallHnd, localWrapper, setter, localPlacement, overrideLevelId);

                    IFCExtrusionCreationData extraParams = new IFCExtrusionCreationData();
                    extraParams.PossibleExtrusionAxes = IFCExtrusionAxes.TryZ;   // only allow vertical extrusions!
                    extraParams.AreInnerRegionsOpenings = true;
                    localWrapper.AddElement(element, wallHnd, setter, extraParams, true);

                    ElementId wallLevelId = (validRange) ? setter.LevelId : ElementId.InvalidElementId;
                    SpaceBoundingElementUtil.RegisterSpaceBoundingElementHandle(exporterIFC, wallHnd, element.Id, wallLevelId);
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
                    elemName, elemDesc, elemApplicableOccurence, null, null, elemTag, elemElementType, isStandard ? "STANDARD" : "NOTDEFINED");

            wrapper.RegisterHandleWithElementType(elementType as ElementType, wallType, null);

            if (overrideMaterialId != ElementId.InvalidElementId)
            {
                CategoryUtil.CreateMaterialAssociation(doc, exporterIFC, wallType, overrideMaterialId);
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
        static public Curve GetWallAxis(Wall wallElement)
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

            Transform trf = Transform.CreateTranslation(new XYZ(0, 0, baseOffset));

            return nonBaseCurve.CreateTransformed(trf);
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
        /// <param name="wallElement">The wall.</param>
        /// <returns>The direction of the wall.</returns>
        /// <remarks>All Wall elements in Revit have a direction of (0,0,1).  If this changes in the future, this
        /// routine may need to be revisited.</remarks>
        public static XYZ GetWallHeightDirection(Wall wallElement)
        {
            return new XYZ(0,0,1);
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
