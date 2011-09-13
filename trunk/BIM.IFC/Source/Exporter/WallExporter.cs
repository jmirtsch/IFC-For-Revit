//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2011  Autodesk, Inc.
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


namespace BIM.IFC.Exporter
{
    /// <summary>
    /// Provides methods to export walls.
    /// </summary>
    class WallExporter
    {
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
        /// <param name="productWrapper">
        /// The IFCProductWrapper.
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
           IFCProductWrapper productWrapper, ElementId overrideLevelId, UV range)
        {
            using (IFCProductWrapper localWrapper = IFCProductWrapper.Create(productWrapper))
            {
                ElementId catId = CategoryUtil.GetSafeCategoryId(element);

                Wall wallElement = element as Wall;
                FamilyInstance famInstWallElem = element as FamilyInstance;

                if (wallElement == null && famInstWallElem == null)
                    return IFCAnyHandle.Create();

                if (wallElement != null && IsWallCompletelyClipped(wallElement, exporterIFC, range))
                    return IFCAnyHandle.Create();

                // get global values.
                Document doc = element.Document;
                double scale = exporterIFC.LinearScale;

                IFCFile file = exporterIFC.GetFile();
                IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();
                IFCAnyHandle contextOfItems = exporterIFC.Get3DContextHandle();

                UV zSpan = UV.Zero;
                double depth = 0.0;
                bool validRange = (!MathUtil.IsAlmostZero(range.V - range.U));

                // get bounding box height so that we can subtract out pieces properly.
                // only for Wall, not FamilyInstance.
                if (wallElement != null && geometryElement != null)
                {
                    BoundingBoxXYZ boundingBox = element.get_BoundingBox(null);
                    zSpan = new UV(boundingBox.Min.Z, boundingBox.Max.Z);

                    // if we have a top clipping plane, modify depth accordingly.
                    double bottomHeight = validRange ? Math.Max(zSpan[0], range[0]) : zSpan[0];
                    double topHeight = validRange ? Math.Min(zSpan[1], range[1]) : zSpan[1];
                    depth = topHeight - bottomHeight;
                    if (MathUtil.IsAlmostZero(depth))
                        return IFCAnyHandle.Create();
                    depth *= scale;
                }

                IFCAnyHandle axisRep = IFCAnyHandle.Create();
                IFCAnyHandle bodyRep = IFCAnyHandle.Create();

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

                    UV curveBounds;
                    XYZ oldOrig;
                    GeometryUtil.GetAxisAndRangeFromCurve(curve, out curveBounds, out localXDir, out oldOrig);

                    localOrig = oldOrig;
                    if (baseCurve != null)
                    {
                        if (!validRange || (MathUtil.IsAlmostEqual(range[0], zSpan[0])))
                        {
                            XYZ newOrig = baseCurve.Evaluate(curveBounds.U, false);
                            if (validRange && (zSpan[0] < newOrig[2] - eps))
                                localOrig = new XYZ(localOrig.X, localOrig.Y, zSpan[0]);
                            else
                                localOrig = new XYZ(localOrig.X, localOrig.Y, newOrig[2]);
                        }
                        else
                        {
                            localOrig = new XYZ(localOrig.X, localOrig.Y, range[0]);
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

                Transform orientationTrf = Transform.Identity;
                orientationTrf.BasisX = localXDir;
                orientationTrf.BasisY = localYDir;
                orientationTrf.BasisZ = localZDir;
                orientationTrf.Origin = localOrig;

                using (IFCPlacementSetter setter = IFCPlacementSetter.Create(exporterIFC, element, null, orientationTrf, overrideLevelId))
                {
                    IFCAnyHandle localPlacement = setter.GetPlacement();

                    Plane plane = new Plane(localXDir, localYDir, localOrig);  // project curve to XY plane.
                    XYZ projDir = new XYZ(0, 0, 1);

                    // two representations: axis, body.         
                    {
                        if ((centerCurve != null) && (GeometryUtil.CurveIsLineOrArc(centerCurve)))
                        {
                            exportingAxis = true;

                            IFCLabel identifierOpt = IFCLabel.Create("Axis");	// IFC2x2 convention
                            IFCLabel representationTypeOpt = IFCLabel.Create("Curve2D");  // IFC2x2 convention

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

                                axisRep = RepresentationUtil.CreateShapeRepresentation(exporterIFC, catId, contextOfItems,
                                   identifierOpt, representationTypeOpt, axisItemSet);
                            }
                        }
                    }

                    IList<IFCExtrusionData> cutPairOpenings = new List<IFCExtrusionData>();

                    do
                    {
                        if (wallElement == null || !exportingAxis || curve == null)
                            break;

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
                                    break;

                                // Current limitation: can't handle wall split into multiple disjointed pieces.
                                int numSortedLoops = sortedLoops.Count;
                                if (numSortedLoops > 1)
                                    break;

                                bool ignoreExtrusion = true;
                                bool cantHandle = false;
                                bool hasGeometry = false;
                                for (int ii = 0; (ii < numSortedLoops) && !cantHandle; ii++)
                                {
                                    int sortedLoopSize = sortedLoops[ii].Count;
                                    if (sortedLoopSize == 0)
                                        continue;
                                    if (!ExporterIFCUtils.IsCurveLoopConvexWithOpenings(sortedLoops[ii][0], range, wallElement, out ignoreExtrusion))
                                    {
                                        if (ignoreExtrusion)
                                        {
                                            // we need more information.  Is there something to export?  If so, we'll
                                            // ignore the extrusion.  Otherwise, we will fail.

                                            IFCSolidMeshGeometryInfo solidMeshInfo = ExporterIFCUtils.GetClippedSolidMeshGeometry(exporterIFC, range, geometryElement);
                                            if (solidMeshInfo.GetSolids().Count == 0 && solidMeshInfo.GetMeshes().Count == 0)
                                                continue;
                                            hasExtrusion = false;
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
                                    return IFCAnyHandle.Create();
                                if (cantHandle)
                                    break;
                            }
                        }

                        if (!CanExportWallGeometryAsExtrusion(element, range))
                            break;

                        // extrusion direction.
                        XYZ extrusionDir = GetWallHeightDirection(wallElement);

                        // create extrusion boundary.
                        IList<CurveLoop> boundaryLoops = new List<CurveLoop>();

                        bool alwaysThickenCurve = IsWallBaseRectangular(wallElement, curve);

                        if (!alwaysThickenCurve)
                        {
                            boundaryLoops = GetLoopsFromTopBottomFace(wallElement, exporterIFC);
                            if (boundaryLoops.Count == 0)
                                continue;
                        }
                        else
                        {
                            CurveLoop newLoop = CurveLoop.CreateViaThicken(curve, wallElement.Width, new XYZ(0, 0, 1));
                            if (newLoop == null)
                                break;

                            if (!GeometryUtil.IsIFCLoopCCW(newLoop, new XYZ(0, 0, 1)))
                                newLoop = GeometryUtil.ReverseOrientation(newLoop);
                            boundaryLoops.Add(newLoop);
                        }

                        // origin gets scaled later.
                        XYZ setterOffset = new XYZ(0, 0, setter.Offset + (localOrig[2] - setter.BaseOffset));

                        IFCAnyHandle baseBodyItemHnd = file.CreateExtrudedSolidFromCurveLoop(exporterIFC, catId,
                           boundaryLoops, plane, extrusionDir, depth);
                        if (!baseBodyItemHnd.HasValue)
                            break;

                        IFCAnyHandle bodyItemHnd = AddClippingsToBaseExtrusion(exporterIFC, wallElement,
                           setterOffset, range, zSpan, baseBodyItemHnd, out cutPairOpenings);
                        if (!bodyItemHnd.HasValue)
                            break;

                        HashSet<IFCAnyHandle> bodyItems = new HashSet<IFCAnyHandle>();
                        bodyItems.Add(bodyItemHnd);

                        if (baseBodyItemHnd.Id == bodyItemHnd.Id)
                        {
                            bodyRep = RepresentationUtil.CreateSweptSolidRep(exporterIFC, catId, contextOfItems, bodyItems, IFCAnyHandle.Create());
                        }
                        else
                        {
                            bodyRep = RepresentationUtil.CreateClippingRep(exporterIFC, catId, contextOfItems, bodyItems);
                        }

                        if (bodyRep.HasValue)
                            exportedAsWallWithAxis = true;
                    } while (false);

                    IFCExtrusionCreationData extraParams = new IFCExtrusionCreationData();
                    ElementId matId = ElementId.InvalidElementId;

                    if (!exportedAsWallWithAxis)
                    {
                        IFCSolidMeshGeometryInfo solidMeshInfo;

                        if (wallElement != null)
                        {
                            if (validRange)
                            {
                                solidMeshInfo = ExporterIFCUtils.GetClippedSolidMeshGeometry(exporterIFC, range, geometryElement);
                                if (solidMeshInfo.GetSolids().Count == 0 && solidMeshInfo.GetMeshes().Count == 0)
                                    return IFCAnyHandle.Create();
                            }
                            else
                            {
                                solidMeshInfo = ExporterIFCUtils.GetSplitSolidMeshGeometry(exporterIFC, geometryElement);
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
                            solidMeshInfo = ExporterIFCUtils.GetSolidMeshGeometry(exporterIFC, geomElemToUse, trf);
                        }

                        IList<Solid> solids = solidMeshInfo.GetSolids();
                        IList<Mesh> meshes = solidMeshInfo.GetMeshes();

                        extraParams.PossibleExtrusionAxes = IFCExtrusionAxes.TryZ;   // only allow vertical extrusions!
                        extraParams.AreInnerRegionsOpenings = true;

                        if ((solids.Count > 0) || (meshes.Count > 0))
                        {
                            matId = BodyExporter.GetBestMaterialIdForGeometry(solids, meshes);
                            bodyRep = BodyExporter.ExportBody(element.Document.Application, exporterIFC, catId, solids, meshes, true, extraParams);
                        }
                        else
                        {
                            IList<GeometryObject> geomElemList = new List<GeometryObject>();
                            geomElemList.Add(geometryElement);
                            bodyRep = BodyExporter.ExportBody(element.Document.Application, exporterIFC, catId, geomElemList, true, extraParams);
                        }

                        if (!bodyRep.HasValue)
                            return IFCAnyHandle.Create();

                        // We will be able to export as a IfcWallStandardCase as long as we have an axis curve.
                        XYZ extrDirUsed = XYZ.Zero;
                        if (extraParams.HasCustomAxis)
                        {
                            extrDirUsed = extraParams.CustomAxis;
                            if (MathUtil.IsAlmostEqual(Math.Abs(extrDirUsed[2]), 1.0))
                            {
                                if ((solids.Count == 1) && (meshes.Count == 0))
                                    exportedAsWallWithAxis = exportingAxis;
                                exportedBodyDirectly = true;
                            }
                        }
                    }

                    IFCAnyHandle prodRep = IFCAnyHandle.Create();
                    IList<IFCAnyHandle> representations = new List<IFCAnyHandle>();
                    if (exportingAxis)
                        representations.Add(axisRep);

                    representations.Add(bodyRep);
                    prodRep = file.CreateProductDefinitionShape(IFCLabel.Create(), IFCLabel.Create(), representations);

                    IFCLabel objectType = NamingUtil.CreateIFCObjectName(exporterIFC, element);
                    IFCAnyHandle wallHnd = IFCAnyHandle.Create();

                    IFCLabel elemGUID = (validRange) ? IFCLabel.CreateGUID() : IFCLabel.CreateGUID(element);
                    IFCLabel elemName = NamingUtil.GetNameOverride(element, NamingUtil.CreateIFCName(exporterIFC, -1));
                    IFCLabel elemDesc = NamingUtil.GetDescriptionOverride(element, IFCLabel.Create());
                    IFCLabel elemObjectType = NamingUtil.GetObjectTypeOverride(element, objectType);
                    IFCLabel elemId = NamingUtil.CreateIFCElementId(element);

                    if (exportedAsWallWithAxis)
                    {
                        wallHnd = file.CreateWallStandardCase(elemGUID, ownerHistory, elemName, elemDesc, elemObjectType, localPlacement,
                           elemId, prodRep);
                        localWrapper.AddElement(wallHnd, setter, extraParams, true);

                        OpeningUtil.CreateOpeningsIfNecessary(wallHnd, element, cutPairOpenings, exporterIFC, localPlacement, setter, localWrapper);
                        if (exportedBodyDirectly)
                        {
                            OpeningUtil.CreateOpeningsIfNecessary(wallHnd, element, extraParams, exporterIFC, localPlacement, setter, localWrapper);
                        }
                        else
                        {
                            double scaledWidth = wallElement.Width * scale;
                            ExporterIFCUtils.AddOpeningsToWall(exporterIFC, wallHnd, wallElement, scaledWidth, range, setter, localPlacement, localWrapper);
                        }

                        // export Base Quantities
                        if (exporterIFC.ExportBaseQuantities)
                        {
                            CreateWallBaseQuantities(exporterIFC, wallElement, wallHnd, depth);
                        }
                    }
                    else
                    {
                        wallHnd = file.CreateWall(elemGUID, ownerHistory, elemName, elemDesc, elemObjectType,
                           localPlacement, elemId, prodRep);
                        localWrapper.AddElement(wallHnd, setter, extraParams, true);

                        // Only export one material for 2x2; for future versions, export the whole list.
                        if (exporterIFC.ExportAs2x2 && (matId != ElementId.InvalidElementId))
                        {
                            CategoryUtil.CreateMaterialAssociation(doc, exporterIFC, wallHnd, matId);
                        }

                        if (exportingInplaceOpenings)
                        {
                            double scaledWidth = wallElement.Width * scale;
                            ExporterIFCUtils.AddOpeningsToWall(exporterIFC, wallHnd, wallElement, scaledWidth, range, setter, localPlacement, localWrapper);
                        }

                        if (exportedBodyDirectly)
                            OpeningUtil.CreateOpeningsIfNecessary(wallHnd, element, extraParams, exporterIFC, localPlacement, setter, localWrapper);
                    }

                    ExporterIFCUtils.CreateGenericElementPropertySet(exporterIFC, element, localWrapper);

                    ElementId wallLevelId = (validRange) ? setter.LevelId : ElementId.InvalidElementId;

                    if (wallElement != null)
                        ExporterIFCUtils.ExportHostObject(exporterIFC, wallElement, geometryElement, localWrapper);

                    exporterIFC.RegisterSpaceBoundingElementHandle(wallHnd, element.Id, wallLevelId);
                    return wallHnd;
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
        /// The IFCProductWrapper.
        /// </param>
        public static void ExportWall(ExporterIFC exporterIFC, Element element, GeometryElement geometryElement,
           IFCProductWrapper productWrapper)
        {
            IList<IFCAnyHandle> createdWalls = new List<IFCAnyHandle>();

            if (exporterIFC.WallAndColumnSplitting)
            {
                Wall wallElement = element as Wall;
                IList<ElementId> levels = new List<ElementId>();
                IList<UV> ranges = new List<UV>();
                if (wallElement != null && geometryElement != null)
                {
                    LevelUtil.CreateSplitLevelRangesForElement(exporterIFC, IFCExportType.ExportWall, element, out levels, out ranges);
                }

                int numPartsToExport = ranges.Count;
                if (numPartsToExport == 0)
                {
                    IFCAnyHandle wallElemHnd = ExportWallBase(exporterIFC, element, geometryElement, productWrapper, ElementId.InvalidElementId, new UV());
                    if (wallElemHnd.HasValue)
                        createdWalls.Add(wallElemHnd);
                }
                else
                {
                    IFCAnyHandle wallElemHnd = ExportWallBase(exporterIFC, element, geometryElement, productWrapper, levels[0], ranges[0]);
                    if (wallElemHnd.HasValue)
                        createdWalls.Add(wallElemHnd);
                    for (int ii = 1; ii < numPartsToExport; ii++)
                    {
                        wallElemHnd = ExportWallBase(exporterIFC, element, geometryElement, productWrapper, levels[ii], ranges[ii]);
                        if (wallElemHnd.HasValue)
                            createdWalls.Add(wallElemHnd);
                    }
                }
            }

            if (createdWalls.Count == 0)
                ExportWallBase(exporterIFC, element, geometryElement, productWrapper, ElementId.InvalidElementId, new UV());
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
        /// The IFCProductWrapper.
        /// </param>
        public static void Export(ExporterIFC exporterIFC, Wall wallElement, GeometryElement geometryElement, IFCProductWrapper productWrapper)
        {
            IFCFile file = exporterIFC.GetFile();
            using (IFCTransaction tr = new IFCTransaction(file))
            {
                bool exportAsOldCurtainWall = ExporterIFCUtils.IsLegacyCurtainWall(wallElement);
                bool exportAsCurtainWall = (wallElement.CurtainGrid != null);

                try
                {
                    if (exportAsOldCurtainWall)
                        ExporterIFCUtils.ExportLegacyCurtainWall(exporterIFC, wallElement, geometryElement, productWrapper);
                    else if (exportAsCurtainWall)
                        ExporterIFCUtils.ExportCurtainWall(exporterIFC, wallElement, geometryElement, productWrapper);
                    else
                        ExportWall(exporterIFC, wallElement, geometryElement, productWrapper);
                }
                catch (System.Exception ex)
                {
                    throw ex;
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

                        exporterIFC.RegisterWallConnectionData(id, otherId, currConnection, joinedEnd, IFCAnyHandle.Create());
                    }
                }

                // look for connected columns.  Note that this is only for columns that interrupt the wall path.
                IList<FamilyInstance> attachedColumns = ExporterIFCUtils.GetAttachedColumns(wallElement);
                int numAttachedColumns = attachedColumns.Count;
                for (int ii = 0; ii < numAttachedColumns; ii++)
                {
                    ElementId otherId = attachedColumns[ii].Id;

                    IFCConnectedWallDataLocation connect1 = IFCConnectedWallDataLocation.NotDefined;   // can't determine at the moment.
                    IFCConnectedWallDataLocation connect2 = IFCConnectedWallDataLocation.NotDefined;   // meaningless for column

                    exporterIFC.RegisterWallConnectionData(id, otherId, connect1, connect2, IFCAnyHandle.Create());
                }

                tr.Commit();
            }
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
        public static bool IsWallCompletelyClipped(Wall wallElem, ExporterIFC exporterIFC, UV range)
        {
            return ExporterIFCUtils.IsWallCompletelyClipped(wallElem, exporterIFC, range);
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
        public static bool CanExportWallGeometryAsExtrusion(Element elem, UV range)
        {
            return ExporterIFCUtils.CanExportWallGeometryAsExtrusion(elem, range);
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
        public static IFCAnyHandle AddClippingsToBaseExtrusion(ExporterIFC exporterIFC, Wall wallElem,
           XYZ setterOffset, UV range, UV zSpan, IFCAnyHandle baseBodyItemHnd, out IList<IFCExtrusionData> cutPairOpenings)
        {
            return ExporterIFCUtils.AddClippingsToBaseExtrusion(exporterIFC, wallElem, setterOffset, range, zSpan, baseBodyItemHnd, out cutPairOpenings);
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
    }
}
