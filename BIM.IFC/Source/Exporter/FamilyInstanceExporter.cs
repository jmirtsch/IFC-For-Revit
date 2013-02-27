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
using System.Linq;
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
    /// Provides methods to export family instances.
    /// </summary>
    class FamilyInstanceExporter
    {
        /// <summary>
        /// Exports a family instance to corresponding IFC object.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="familyInstance">
        /// The family instance to be exported.
        /// </param>
        /// <param name="geometryElement">
        /// The geometry element.
        /// </param>
        /// <param name="productWrapper">
        /// The ProductWrapper.
        /// </param>
        public static void ExportFamilyInstanceElement(ExporterIFC exporterIFC,
           FamilyInstance familyInstance, GeometryElement geometryElement, ProductWrapper productWrapper)
        {
            // Don't export family if it is invisible, or has a null geometry.
            if (familyInstance.Invisible || geometryElement == null)
                return;

            // Don't export family instance if it has a curtain grid host; the host will be in charge of exporting.
            Element host = familyInstance.Host;
            if (CurtainSystemExporter.IsCurtainSystem(host))
                return;

            FamilySymbol familySymbol = familyInstance.Symbol;
            Family family = familySymbol.Family;
            if (family == null)
                return;

            IFCFile file = exporterIFC.GetFile();

            using (IFCTransaction tr = new IFCTransaction(file))
            {
                string ifcEnumType;
                IFCExportType exportType = ExporterUtil.GetExportType(exporterIFC, familyInstance, out ifcEnumType);

                if (exportType == IFCExportType.DontExport)
                    return;

                if (ExportFamilyInstanceAsStandardElement(exporterIFC, familyInstance, geometryElement, exportType, ifcEnumType, productWrapper))
                {
                    tr.Commit();
                    return;
                }

                // If we are exporting a column, we may need to split it into parts by level.  Create a list of ranges.
                IList<ElementId> levels = new List<ElementId>();
                IList<IFCRange> ranges = new List<IFCRange>();

                // We will not split walls and columns if the assemblyId is set, as we would like to keep the original wall
                // associated with the assembly, on the level of the assembly.
                bool splitColumn = (exportType == IFCExportType.ExportColumnType) && (ExporterCacheManager.ExportOptionsCache.WallAndColumnSplitting) &&
                    (familyInstance.AssemblyInstanceId == ElementId.InvalidElementId);
                if (splitColumn)
                {
                    LevelUtil.CreateSplitLevelRangesForElement(exporterIFC, exportType, familyInstance, out levels, out ranges);
                }

                int numPartsToExport = ranges.Count;
                if (numPartsToExport == 0)
                {
                    ExportFamilyInstanceAsMappedItem(exporterIFC, familyInstance, exportType, ifcEnumType, productWrapper,
                       ElementId.InvalidElementId, null, null);
                }
                else
                {
                    for (int ii = 0; ii < numPartsToExport; ii++)
                    {
                        ExportFamilyInstanceAsMappedItem(exporterIFC, familyInstance, exportType, ifcEnumType, productWrapper,
                          levels[ii], ranges[ii], null);
                    }

                    if (ExporterCacheManager.DummyHostCache.HasRegistered(familyInstance.Id))
                    {
                        List<KeyValuePair<ElementId, IFCRange>> levelRangeList = ExporterCacheManager.DummyHostCache.Find(familyInstance.Id);
                        foreach (KeyValuePair<ElementId, IFCRange> levelRange in levelRangeList)
                        {
                            ExportFamilyInstanceAsMappedItem(exporterIFC, familyInstance, exportType, ifcEnumType, productWrapper, levelRange.Key, levelRange.Value, null);
                        }
                    }
                }

                tr.Commit();
            }
        }

        /// <summary>
        /// Exports a family instance as a mapped item.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="familyInstance">
        /// The family instance to be exported.
        /// </param>
        /// <param name="exportType">
        /// The export type.
        /// </param>
        /// <param name="ifcEnumType">
        /// The string value represents the IFC type.
        /// </param>
        /// <param name="wrapper">
        /// The ProductWrapper.
        /// </param>
        /// <param name="overrideLevelId">
        /// The level id.
        /// </param>
        /// <param name="range">
        /// The range of this family instance to be exported.
        /// </param>
        public static void ExportFamilyInstanceAsMappedItem(ExporterIFC exporterIFC,
           FamilyInstance familyInstance, IFCExportType exportType, string ifcEnumType,
           ProductWrapper wrapper, ElementId overrideLevelId, IFCRange range, IFCAnyHandle parentLocalPlacement)
        {
            bool exportParts = PartExporter.CanExportParts(familyInstance);
            bool isSplit = range != null;
            if (exportParts && !PartExporter.CanExportElementInPartExport(familyInstance, isSplit ? overrideLevelId : familyInstance.Level.Id, isSplit))
                return;

            Document doc = familyInstance.Document;
            IFCFile file = exporterIFC.GetFile();

            // The "originalFamilySymbol" has the right geometry, but should be used as little as possible.
            FamilySymbol originalFamilySymbol = ExporterIFCUtils.GetOriginalSymbol(familyInstance);
            FamilySymbol familySymbol = familyInstance.Symbol;
            if (originalFamilySymbol == null || familySymbol == null)
                return;

            ProductWrapper familyProductWrapper = ProductWrapper.Create(wrapper);
            double scale = exporterIFC.LinearScale;
            Options options = GeometryUtil.GetIFCExportGeometryOptions();

            IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();

            HostObject hostElement = familyInstance.Host as HostObject; //hostElement could be null
            ElementId categoryId = CategoryUtil.GetSafeCategoryId(familySymbol);

            //string emptyString = "";
            string familyName = familySymbol.Name;
            string revitObjectType = familyName;

            // A Family Instance can have its own copy of geometry, or use the symbol's copy with a transform.
            // The routine below tells us whether to use the Instance's copy or the Symbol's copy.
            bool useInstanceGeometry = ExporterIFCUtils.UsesInstanceGeometry(familyInstance);
            Transform trf = familyInstance.GetTransform();

            using (IFCExtrusionCreationData extraParams = new IFCExtrusionCreationData())
            {
                bool exportingDoor = (exportType == IFCExportType.ExportDoorType);
                bool exportingWindow = (exportType == IFCExportType.ExportWindowType);
                bool exportingHostParts = PartExporter.CanExportParts(hostElement);
                HostObject hostElementForDoorWindow = exportingHostParts ? null : hostElement;

                // Extra information if we are exporting a door or a window.
                IFCDoorWindowInfo doorWindowInfo = null;
                if (exportingDoor)
                    doorWindowInfo = IFCDoorWindowInfo.CreateDoorInfo(exporterIFC, familyInstance, originalFamilySymbol, hostElementForDoorWindow, overrideLevelId, trf);
                else if (exportingWindow)
                    doorWindowInfo = IFCDoorWindowInfo.CreateWindowInfo(exporterIFC, familyInstance, originalFamilySymbol, hostElementForDoorWindow, overrideLevelId, trf);

                bool ignoreDoorWindowOpening = ((exportingDoor || exportingWindow) && exportingHostParts);
                
                FamilyTypeInfo typeInfo = new FamilyTypeInfo();

                bool flipped = doorWindowInfo != null ? doorWindowInfo.IsSymbolFlipped : false;
                FamilyTypeInfo currentTypeInfo = ExporterCacheManager.TypeObjectsCache.Find(originalFamilySymbol.Id, flipped);
                bool found = currentTypeInfo.IsValid();

                Family family = familySymbol.Family;

                IList<GeometryObject> geomObjects = new List<GeometryObject>();
                Transform offsetTransform = null;

                Transform doorWindowTrf = Transform.Identity;
                // We will create a new mapped type if:
                // 1.  We are exporting part of a column or in-place wall (range != null), OR
                // 2.  We are using the instance's copy of the geometry (that it, it has unique geometry), OR
                // 3.  We haven't already created the type.
                bool creatingType = ((range != null) || useInstanceGeometry || !found);
                if (creatingType)
                {
                    IFCAnyHandle bodyRepresentation = null;
                    IFCAnyHandle planRepresentation = null;

                    IFCAnyHandle dummyPlacement = null;
                    if (doorWindowInfo != null)
                    {
                        doorWindowTrf = ExporterIFCUtils.GetTransformForDoorOrWindow(familyInstance, originalFamilySymbol, doorWindowInfo);
                    }
                    else
                    {
                        dummyPlacement = IFCInstanceExporter.CreateLocalPlacement(file, null, ExporterUtil.CreateAxis2Placement3D(file));
                        extraParams.SetLocalPlacement(dummyPlacement);
                    }

                    bool needToCreate2d = ExporterCacheManager.ExportOptionsCache.ExportAnnotations;
                    GeometryElement exportGeometry =
                       useInstanceGeometry ? familyInstance.get_Geometry(options) : originalFamilySymbol.get_Geometry(options);

                    if (!exportParts)
                    {
                        using (IFCTransformSetter trfSetter = IFCTransformSetter.Create())
                    {
                        if (doorWindowInfo != null)
                        {
                            trfSetter.Initialize(exporterIFC, doorWindowTrf);
                        }

                        if (exportGeometry == null)
                            return;

                        SolidMeshGeometryInfo solidMeshCapsule = null;

                        if (range == null)
                        {
                            solidMeshCapsule = GeometryUtil.GetSplitSolidMeshGeometry(exportGeometry);
                        }
                        else
                        {
                            solidMeshCapsule = GeometryUtil.GetSplitClippedSolidMeshGeometry(exportGeometry, range);
                        }

                        IList<Solid> solids = solidMeshCapsule.GetSolids();
                        IList<Mesh> polyMeshes = solidMeshCapsule.GetMeshes();

                        // If we are exporting parts, it is OK to have no geometry here - it will be added by the host Part.
                        bool hasGeometryInSymbol = (solids.Count > 0 || polyMeshes.Count > 0);
                        if (range != null && !hasGeometryInSymbol && !exportParts)
                            return; // no proper split geometry to export.

                        if (hasGeometryInSymbol)
                            geomObjects = FamilyExporterUtil.RemoveSolidsAndMeshesSetToDontExport(doc, exporterIFC, solids, polyMeshes);

                        if ((geomObjects.Count == 0) && hasGeometryInSymbol && !exportParts)
                            return; // no proper visible split geometry to export.

                        if (geomObjects.Count > 0)
                        {
                            bool tryToExportAsExtrusion = (!ExporterCacheManager.ExportOptionsCache.ExportAs2x2 || (exportType == IFCExportType.ExportColumnType));

                            if (exportType == IFCExportType.ExportColumnType)
                            {
                                extraParams.PossibleExtrusionAxes = IFCExtrusionAxes.TryZ;

                                if (ExporterCacheManager.ExportOptionsCache.ExportAs2x3CoordinationView2 &&
                                    solids.Count > 0)
                                {
                                    LocationPoint point = familyInstance.Location as LocationPoint;
                                    XYZ orig = XYZ.Zero;
                                    if (point != null)
                                        orig = point.Point;

                                    Plane plane = new Plane(XYZ.BasisX, XYZ.BasisY, orig);
                                    bool completelyClipped = false;
                                    HashSet<ElementId> materialIds = null;
                                    bodyRepresentation = ExtrusionExporter.CreateExtrusionWithClipping(exporterIFC, familyInstance,
                                        categoryId, solids, plane, XYZ.BasisZ, null, out completelyClipped, out materialIds);
                                    typeInfo.MaterialIds = materialIds;
                                }
                            }
                            else
                            {
                                extraParams.PossibleExtrusionAxes = IFCExtrusionAxes.TryXYZ;
                            }

                            BodyData bodyData = null;
                            if (IFCAnyHandleUtil.IsNullOrHasNoValue(bodyRepresentation))
                            {
                                BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(tryToExportAsExtrusion);
                                if (geomObjects.Count > 0)
                                {
                                    bodyData = BodyExporter.ExportBody(exporterIFC, familyInstance, categoryId, ElementId.InvalidElementId,
                                        geomObjects, bodyExporterOptions, extraParams);
                                    typeInfo.MaterialIds = bodyData.MaterialIds;
                                }
                                else
                                {
                                    IList<GeometryObject> exportedGeometries = new List<GeometryObject>();
                                    exportedGeometries.Add(exportGeometry);
                                    bodyData = BodyExporter.ExportBody(exporterIFC, familyInstance, categoryId, ElementId.InvalidElementId,
                                        exportedGeometries, bodyExporterOptions, extraParams);
                                }
                                bodyRepresentation = bodyData.RepresentationHnd;
                                    offsetTransform = bodyData.OffsetTransform;
                            }
                        }

                        // We will allow a door or window to be exported without any geometry, or an element with parts.
                        // Anything else doesn't really make sense.
                            if (IFCAnyHandleUtil.IsNullOrHasNoValue(bodyRepresentation) && (doorWindowInfo == null))
                        {
                            extraParams.ClearOpenings();
                            return;
                        }
                        }

                        // By default: if exporting IFC2x3 or later, export 2D plan rep of family, if it exists, unless we are exporting Coordination View V2.
                        // This default can be overridden in the export options.
                        if (needToCreate2d)
                        {
                            XYZ curveOffset = new XYZ(0, 0, 0);
                            if (offsetTransform != null)
                                curveOffset = -offsetTransform.Origin / scale;

                            HashSet<IFCAnyHandle> curveSet = new HashSet<IFCAnyHandle>();
                            {
                                Transform planeTrf = doorWindowTrf.Inverse;
                                Plane plane = new Plane(planeTrf.get_Basis(0), planeTrf.get_Basis(1), planeTrf.Origin);
                                XYZ projDir = new XYZ(0, 0, 1);

                                IFCGeometryInfo IFCGeometryInfo = IFCGeometryInfo.CreateCurveGeometryInfo(exporterIFC, plane, projDir, true);
                                ExporterIFCUtils.CollectGeometryInfo(exporterIFC, IFCGeometryInfo, exportGeometry, curveOffset, false);

                                IList<IFCAnyHandle> curves = IFCGeometryInfo.GetCurves();
                                foreach (IFCAnyHandle curve in curves)
                                    curveSet.Add(curve);

                                if (curveSet.Count > 0)
                                {
                                    IFCAnyHandle contextOfItems2d = exporterIFC.Get2DContextHandle();
                                    IFCAnyHandle curveRepresentationItem = IFCInstanceExporter.CreateGeometricSet(file, curveSet);
                                    HashSet<IFCAnyHandle> bodyItems = new HashSet<IFCAnyHandle>();
                                    bodyItems.Add(curveRepresentationItem);
                                    planRepresentation = RepresentationUtil.CreateGeometricSetRep(exporterIFC, familyInstance, categoryId, "Annotation",
                                       contextOfItems2d, bodyItems);
                                }
                            }
                        }
                    }

                    if (doorWindowInfo != null)
                    {
                        typeInfo.StyleTransform = doorWindowTrf.Inverse;
                    }
                    else
                    {
                        typeInfo.StyleTransform = ExporterIFCUtils.GetUnscaledTransform(exporterIFC, extraParams.GetLocalPlacement());
                    }

                    IFCAnyHandle origin = ExporterUtil.CreateAxis2Placement3D(file);
                    IList<IFCAnyHandle> repMapList = new List<IFCAnyHandle>();

                    IFCAnyHandle repMap3dHnd = null;
                    if (bodyRepresentation != null)
                    {
                        repMap3dHnd = IFCInstanceExporter.CreateRepresentationMap(file, origin, bodyRepresentation);
                        repMapList.Add(repMap3dHnd);
                    }

                    IFCAnyHandle repMap2dHnd = null;
                    if (!IFCAnyHandleUtil.IsNullOrHasNoValue(planRepresentation))
                    {
                        repMap2dHnd = IFCInstanceExporter.CreateRepresentationMap(file, origin, planRepresentation);
                        repMapList.Add(repMap2dHnd);
                    }

                    // for Door, Window
                    bool paramTakesPrecedence = false; // For Revit, this is currently always false.
                    bool sizeable = false;

                    // for many
                    HashSet<IFCAnyHandle> propertySets = new HashSet<IFCAnyHandle>();

                    string guid = useInstanceGeometry ?
                        GUIDUtil.CreateSubElementGUID(familyInstance, (int)IFCFamilyInstanceSubElements.InstanceAsType) :
                        GUIDUtil.CreateGUID(originalFamilySymbol);
                    string symId = NamingUtil.CreateIFCElementId(originalFamilySymbol);

                    string gentypeName = NamingUtil.GetNameOverride(familySymbol, revitObjectType);
                    string gentypeDescription = NamingUtil.GetDescriptionOverride(familySymbol, null);
                    string gentypeApplicableOccurrence = NamingUtil.GetOverrideStringValue(familySymbol, "IfcApplicableOccurrence", null);
                    string gentypeTag = NamingUtil.GetTagOverride(familySymbol, symId);
                    string gentypeElementType = NamingUtil.GetOverrideStringValue(familySymbol, "IfcElementType", revitObjectType);

                    // This covers many generic types.  If we can't find it in the list here, do custom exports.
                    IFCAnyHandle typeStyle = FamilyExporterUtil.ExportGenericType(exporterIFC, exportType, ifcEnumType, guid,
                       gentypeName, gentypeDescription, gentypeApplicableOccurrence, propertySets, repMapList, gentypeTag, gentypeElementType,
                       familyInstance, familySymbol);

                    // Cover special cases not covered above.
                    if (IFCAnyHandleUtil.IsNullOrHasNoValue(typeStyle))
                    {
                        string symbolTag = NamingUtil.GetTagOverride(familySymbol, NamingUtil.CreateIFCElementId(familySymbol));
                        switch (exportType)
                        {
                            case IFCExportType.ExportColumnType:
                                {
                                    string columnType = "Column";
                                    typeStyle = IFCInstanceExporter.CreateColumnType(file, guid, ownerHistory, gentypeName,
                                        gentypeDescription, gentypeApplicableOccurrence, propertySets, repMapList, symbolTag,

                                       gentypeElementType, GetColumnType(familyInstance, columnType));

                                    break;
                                }
                            case IFCExportType.ExportDoorType:
                                {
                                    IFCAnyHandle doorLining = DoorWindowUtil.CreateDoorLiningProperties(exporterIFC, familyInstance);
                                    if (!IFCAnyHandleUtil.IsNullOrHasNoValue(doorLining))
                                        propertySets.Add(doorLining);

                                    IList<IFCAnyHandle> doorPanels = DoorWindowUtil.CreateDoorPanelProperties(exporterIFC, doorWindowInfo,
                                       familyInstance);
                                    propertySets.UnionWith(doorPanels);

                                    string doorStyleGUID = GUIDUtil.CreateSubElementGUID(originalFamilySymbol, (int)IFCDoorSubElements.DoorStyle);
                                    typeStyle = IFCInstanceExporter.CreateDoorStyle(file, doorStyleGUID, ownerHistory, gentypeName,
                                       gentypeDescription, gentypeApplicableOccurrence, propertySets, repMapList, symbolTag,
                                       DoorWindowUtil.GetDoorStyleOperation(doorWindowInfo.DoorOperationType),
                                       DoorWindowUtil.GetDoorStyleConstruction(familyInstance),
                                       paramTakesPrecedence, sizeable);
                                    break;
                                }
                            case IFCExportType.ExportSystemFurnitureElementType:
                                {
                                    typeStyle = IFCInstanceExporter.CreateSystemFurnitureElementType(file, guid, ownerHistory, gentypeName,
                                       gentypeDescription, gentypeApplicableOccurrence, propertySets, repMapList, symbolTag,
                                       gentypeElementType);

                                    break;
                                }
                            case IFCExportType.ExportWindowType:
                                {
                                    Toolkit.IFCWindowStyleOperation operationType = DoorWindowUtil.GetIFCWindowStyleOperation(familySymbol);
                                    IFCWindowStyleConstruction constructionType = DoorWindowUtil.GetIFCWindowStyleConstruction(familyInstance);

                                    IFCAnyHandle windowLining = DoorWindowUtil.CreateWindowLiningProperties(exporterIFC, familyInstance, null);
                                    if (!IFCAnyHandleUtil.IsNullOrHasNoValue(windowLining))
                                        propertySets.Add(windowLining);

                                    IList<IFCAnyHandle> windowPanels =
                                       DoorWindowUtil.CreateWindowPanelProperties(exporterIFC, familyInstance, null);
                                    propertySets.UnionWith(windowPanels);

                                    string windowStyleGUID = GUIDUtil.CreateSubElementGUID(originalFamilySymbol, (int)IFCWindowSubElements.WindowStyle);

                                    typeStyle = IFCInstanceExporter.CreateWindowStyle(file, windowStyleGUID, ownerHistory, gentypeName,
                                       gentypeDescription, gentypeApplicableOccurrence, propertySets, repMapList, symbolTag,
                                       constructionType, operationType, paramTakesPrecedence, sizeable);
                                    break;
                                }
                            case IFCExportType.ExportBuildingElementProxy:
                            default:
                                {
                                    if (!IFCAnyHandleUtil.IsNullOrHasNoValue(repMap2dHnd))
                                        typeInfo.Map2DHandle = repMap2dHnd;
                                    if (!IFCAnyHandleUtil.IsNullOrHasNoValue(repMap3dHnd))
                                        typeInfo.Map3DHandle = repMap3dHnd;
                                    break;
                                }
                        }
                    }

                    if (!IFCAnyHandleUtil.IsNullOrHasNoValue(typeStyle))
                    {
                        CategoryUtil.CreateMaterialAssociations(doc, exporterIFC, typeStyle, typeInfo.MaterialIds);

                        typeInfo.Style = typeStyle;

                        if ((exportType == IFCExportType.ExportColumnType) || (exportType == IFCExportType.ExportMemberType))
                        {
                            typeInfo.ScaledArea = extraParams.ScaledArea;
                            typeInfo.ScaledDepth = extraParams.ScaledLength;
                            typeInfo.ScaledInnerPerimeter = extraParams.ScaledInnerPerimeter;
                            typeInfo.ScaledOuterPerimeter = extraParams.ScaledOuterPerimeter;
                        }

                        ClassificationUtil.CreateClassification(exporterIFC, file, originalFamilySymbol, typeStyle, "");        // Create other generic classification from ClassificationCode(s)
                        ClassificationUtil.CreateUniformatClassification(exporterIFC, file, originalFamilySymbol, typeStyle);
                    }
                }

                if (found && !typeInfo.IsValid())
                {
                    typeInfo = currentTypeInfo;
                }

                // we'll pretend we succeeded, but we'll do nothing.
                if (!typeInfo.IsValid())
                    return;

                // add to the map, as long as we are not using range, not using instance geometry, and don't have extra openings.
                if ((range == null) && !useInstanceGeometry && (extraParams.GetOpenings().Count == 0))
                    ExporterCacheManager.TypeObjectsCache.Register(originalFamilySymbol.Id, flipped, typeInfo);

                // If we are using the instance geometry, ignore the transformation.
                if (useInstanceGeometry)
                    trf = Transform.Identity;

                if ((range != null) && exportParts)
                {
                    XYZ rangeOffset = trf.Origin;
                    rangeOffset += new XYZ(0, 0, range.Start);
                    trf.Origin = rangeOffset;
                }

                Transform originalTrf = new Transform(trf);
                XYZ scaledMapOrigin = XYZ.Zero;

                trf = trf.Multiply(typeInfo.StyleTransform);

                // create instance.  
                IList<IFCAnyHandle> shapeReps = new List<IFCAnyHandle>();
                {
                    IFCAnyHandle contextOfItems2d = exporterIFC.Get2DContextHandle();
                    IFCAnyHandle contextOfItems3d = exporterIFC.Get3DContextHandle("Body");

                    // for proxies, we store the IfcRepresentationMap directly since there is no style.
                    IFCAnyHandle style = typeInfo.Style;
                    IList<IFCAnyHandle> repMapList = !IFCAnyHandleUtil.IsNullOrHasNoValue(style) ?
                        GeometryUtil.GetRepresentationMaps(style) : null;
                    int numReps = repMapList != null ? repMapList.Count : 0;

                    IFCAnyHandle repMap2dHnd = typeInfo.Map2DHandle;
                    IFCAnyHandle repMap3dHnd = typeInfo.Map3DHandle;
                    if (IFCAnyHandleUtil.IsNullOrHasNoValue(repMap3dHnd) && (numReps > 0))
                        repMap3dHnd = repMapList[0];
                    if (IFCAnyHandleUtil.IsNullOrHasNoValue(repMap2dHnd) && (numReps > 1))
                        repMap2dHnd = repMapList[1];

                    if (!IFCAnyHandleUtil.IsNullOrHasNoValue(repMap3dHnd))
                    {
                        IList<IFCAnyHandle> representations = new List<IFCAnyHandle>();
                        representations.Add(ExporterUtil.CreateDefaultMappedItem(file, repMap3dHnd, scaledMapOrigin));
                        IFCAnyHandle shapeRep = RepresentationUtil.CreateBodyMappedItemRep(exporterIFC, familyInstance, categoryId, contextOfItems3d,
                            representations);
                        if (IFCAnyHandleUtil.IsNullOrHasNoValue(shapeRep))
                            return;
                        shapeReps.Add(shapeRep);
                    }

                    if (!IFCAnyHandleUtil.IsNullOrHasNoValue(repMap2dHnd))
                    {
                        HashSet<IFCAnyHandle> representations = new HashSet<IFCAnyHandle>();
                        representations.Add(ExporterUtil.CreateDefaultMappedItem(file, repMap2dHnd, scaledMapOrigin));
                        IFCAnyHandle shapeRep = RepresentationUtil.CreatePlanMappedItemRep(exporterIFC, familyInstance, categoryId, contextOfItems2d,
                            representations);
                        if (IFCAnyHandleUtil.IsNullOrHasNoValue(shapeRep))
                            return;
                        shapeReps.Add(shapeRep);
                    }
                }

                IFCAnyHandle boundingBoxRep = null;
                Transform boundingBoxTrf = (offsetTransform != null) ? offsetTransform.Inverse : Transform.Identity;
                if (geomObjects.Count > 0)
                    boundingBoxRep = BoundingBoxExporter.ExportBoundingBox(exporterIFC, geomObjects, boundingBoxTrf);
                else
                {
                    boundingBoxTrf = boundingBoxTrf.Multiply(trf.Inverse);
                    boundingBoxRep = BoundingBoxExporter.ExportBoundingBox(exporterIFC, familyInstance.get_Geometry(options), boundingBoxTrf);
                }

                if (boundingBoxRep != null)
                    shapeReps.Add(boundingBoxRep);

                IFCAnyHandle repHnd = (shapeReps.Count > 0) ? IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, shapeReps) : null;

                if (!(repHnd == null && ExporterCacheManager.ExportOptionsCache.ExportAs2x3CoordinationView2))
                {
                    IFCAnyHandle instanceHandle = null;
                    using (IFCPlacementSetter setter = IFCPlacementSetter.Create(exporterIFC, familyInstance, trf, null, overrideLevelId))
                    {
                        string instanceGUID = GUIDUtil.CreateGUID(familyInstance);
                        string instanceName = NamingUtil.GetNameOverride(familyInstance, NamingUtil.GetIFCName(familyInstance));
                        string instanceDescription = NamingUtil.GetDescriptionOverride(familyInstance, null);
                        string instanceObjectType = NamingUtil.GetObjectTypeOverride(familyInstance, revitObjectType);
                        string instanceTag = NamingUtil.GetTagOverride(familyInstance, NamingUtil.CreateIFCElementId(familyInstance));

						bool isChildInContainer = familyInstance.AssemblyInstanceId != ElementId.InvalidElementId;

                        IFCAnyHandle localPlacement = setter.GetPlacement();
                        IFCAnyHandle overrideLocalPlacement = null;

                        if (parentLocalPlacement != null)
                        {
                            Transform relTrf = ExporterIFCUtils.GetRelativeLocalPlacementOffsetTransform(parentLocalPlacement, localPlacement);
                            Transform inverseTrf = relTrf.Inverse;

                            IFCAnyHandle relativePlacement = ExporterUtil.CreateAxis2Placement3D(file, inverseTrf.Origin, inverseTrf.BasisZ, inverseTrf.BasisX);
                            IFCAnyHandle plateLocalPlacement = IFCInstanceExporter.CreateLocalPlacement(file, parentLocalPlacement, relativePlacement);
                            overrideLocalPlacement = plateLocalPlacement;
                        }

                        instanceHandle = FamilyExporterUtil.ExportGenericInstance(exportType, exporterIFC, familyInstance,
                           wrapper, setter, extraParams, instanceGUID, ownerHistory, instanceName, instanceDescription, instanceObjectType,
                           exportParts ? null : repHnd, instanceTag, overrideLocalPlacement);

                        if (exportParts)
                        {
                            PartExporter.ExportHostPart(exporterIFC, familyInstance, instanceHandle, familyProductWrapper, setter, setter.GetPlacement(), overrideLevelId);
                        }

                        if (ElementFilteringUtil.IsMEPType(exportType))
                            ExporterCacheManager.MEPCache.Register(familyInstance, instanceHandle);

                        switch (exportType)
                        {
                            case IFCExportType.ExportColumnType:
                                {
                                    IFCAnyHandle placementToUse = localPlacement;
                                    if (!useInstanceGeometry)
                                    {
                                        bool needToCreateOpenings = OpeningUtil.NeedToCreateOpenings(instanceHandle, extraParams);
                                        if (needToCreateOpenings)
                                        {
                                            Transform openingTrf = new Transform(originalTrf);
                                            Transform extraRot = new Transform(originalTrf);
                                            extraRot.Origin = XYZ.Zero;
                                            openingTrf = openingTrf.Multiply(extraRot);
                                            openingTrf = openingTrf.Multiply(typeInfo.StyleTransform);

                                           	IFCAnyHandle openingRelativePlacement = ExporterUtil.CreateAxis2Placement3D(file, openingTrf.Origin * scale,
                                               openingTrf.get_Basis(2), openingTrf.get_Basis(0));
                                            IFCAnyHandle openingPlacement = ExporterUtil.CopyLocalPlacement(file, localPlacement);
                                            GeometryUtil.SetRelativePlacement(openingPlacement, openingRelativePlacement);
                                            placementToUse = openingPlacement;
                                        }
                                    }

                                    OpeningUtil.CreateOpeningsIfNecessary(instanceHandle, familyInstance, extraParams, offsetTransform,
                                        exporterIFC, placementToUse, setter, wrapper);

                                    //export Base Quantities.
                                    PropertyUtil.CreateBeamColumnBaseQuantities(exporterIFC, instanceHandle, familyInstance, typeInfo);

                                    PropertyUtil.CreateInternalRevitPropertySets(exporterIFC, familyInstance, wrapper);
                                    break;
                                }
                            case IFCExportType.ExportDoorType:
                            case IFCExportType.ExportWindowType:
                                {
                                    double doorHeight = doorWindowInfo.OpeningHeight;
                                    if (doorHeight < MathUtil.Eps())
                                        doorHeight = GetMinSymbolHeight(originalFamilySymbol);
                                    double doorWidth = doorWindowInfo.OpeningWidth;
                                    if (doorWidth < MathUtil.Eps())
                                        doorWidth = GetMinSymbolWidth(originalFamilySymbol);

                                	double height = doorHeight * scale;
                                	double width = doorWidth * scale;

                                    IFCAnyHandle doorWindowLocalPlacement = null;
                                    if (!IFCAnyHandleUtil.IsNullOrHasNoValue(overrideLocalPlacement))
                                        doorWindowLocalPlacement = overrideLocalPlacement;
                                    else
                                    {
                                        if (IFCAnyHandleUtil.IsNullOrHasNoValue(doorWindowInfo.GetLocalPlacement()))
                                            doorWindowInfo.SetLocalPlacement(localPlacement);

                                        IFCAnyHandle doorWindowOrigLocalPlacement = doorWindowInfo.GetLocalPlacement();
                                        Transform relTrf = ExporterIFCUtils.GetRelativeLocalPlacementOffsetTransform(localPlacement, doorWindowOrigLocalPlacement);

                                        IFCAnyHandle doorWindowRelativePlacement = ExporterUtil.CreateAxis2Placement3D(file, relTrf.Origin, relTrf.BasisZ, relTrf.BasisX);
                                        doorWindowLocalPlacement = IFCInstanceExporter.CreateLocalPlacement(file, doorWindowOrigLocalPlacement, doorWindowRelativePlacement);
                                    }
                                    if (exportType == IFCExportType.ExportDoorType)
                                        instanceHandle = IFCInstanceExporter.CreateDoor(file, instanceGUID, ownerHistory,
                                           instanceName, instanceDescription, instanceObjectType, doorWindowLocalPlacement,
                                           repHnd, instanceTag, height, width);
                                    else if (exportType == IFCExportType.ExportWindowType)
                                        instanceHandle = IFCInstanceExporter.CreateWindow(file, instanceGUID, ownerHistory,
                                           instanceName, instanceDescription, instanceObjectType, doorWindowLocalPlacement,
                                           repHnd, instanceTag, height, width);
                                    wrapper.AddElement(instanceHandle, setter, extraParams, true);

                                    exporterIFC.RegisterSpaceBoundingElementHandle(instanceHandle, familyInstance.Id, setter.LevelId);

                                    IFCAnyHandle placementToUse = doorWindowLocalPlacement;
                                    if (!useInstanceGeometry)
                                    {
                                        // correct the placement to the symbol space
                                        bool needToCreateOpenings = OpeningUtil.NeedToCreateOpenings(instanceHandle, extraParams);
                                        if (needToCreateOpenings)
                                        {
                                            Transform openingTrf = Transform.Identity;
                                            openingTrf.Origin = new XYZ(0, 0, setter.Offset);
                                            openingTrf = openingTrf.Multiply(doorWindowTrf);
                                            XYZ scaledOrigin = openingTrf.Origin * exporterIFC.LinearScale;
                                            IFCAnyHandle openingRelativePlacement = ExporterUtil.CreateAxis2Placement3D(file, scaledOrigin, openingTrf.BasisZ, openingTrf.BasisX);
                                        	IFCAnyHandle openingLocalPlacement =
                                           		IFCInstanceExporter.CreateLocalPlacement(file, doorWindowLocalPlacement, openingRelativePlacement);
                                            placementToUse = openingLocalPlacement;
                                        }
                                    }
                                    
                                    OpeningUtil.CreateOpeningsIfNecessary(instanceHandle, familyInstance, extraParams, offsetTransform,
                                        exporterIFC, placementToUse, setter, wrapper);
                                    //if (ExporterCacheManager.ExportOptionsCache.ExportBaseQuantities)
                                    //    ExporterIFCUtils.CreateDoorWindowBaseQuantities(exporterIFC, instanceHandle, height, width);

                                    PropertyUtil.CreateInternalRevitPropertySets(exporterIFC, familyInstance, wrapper);
                                    break;
                                }
                            case IFCExportType.ExportMemberType:
                                {
                                    OpeningUtil.CreateOpeningsIfNecessary(instanceHandle, familyInstance, extraParams, offsetTransform,
                                        exporterIFC, localPlacement, setter, wrapper);

                                    //export Base Quantities.
                                    PropertyUtil.CreateBeamColumnBaseQuantities(exporterIFC, instanceHandle, familyInstance, typeInfo);
                                    
                                    PropertyUtil.CreateInternalRevitPropertySets(exporterIFC, familyInstance, wrapper);
                                    break;
                                }
                            case IFCExportType.ExportPlateType:
                                {
                                    OpeningUtil.CreateOpeningsIfNecessary(instanceHandle, familyInstance, extraParams, offsetTransform,
                                        exporterIFC, localPlacement, setter, wrapper);

                                    PropertyUtil.CreateInternalRevitPropertySets(exporterIFC, familyInstance, wrapper);
                                    break;
                                }
                            case IFCExportType.ExportTransportElementType:
                                {
                                    IFCAnyHandle localPlacementToUse;
                                    ElementId roomId = setter.UpdateRoomRelativeCoordinates(familyInstance, out localPlacementToUse);

                                    IFCTransportElementType operationType = FamilyExporterUtil.GetTransportElementType(familyInstance);

                                    double capacityByWeight = 0.0;
                                    ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "IfcCapacityByWeight", out capacityByWeight);
                                    double capacityByNumber = 0.0;
                                    ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "IfcCapacityByNumber", out capacityByNumber);

                                    instanceHandle = IFCInstanceExporter.CreateTransportElement(file, instanceGUID, ownerHistory,
                                       instanceName, instanceDescription, instanceObjectType,
                                       localPlacementToUse, repHnd, instanceTag, operationType, capacityByWeight, capacityByNumber);

                                    if (roomId == ElementId.InvalidElementId)
                                    {
                                        wrapper.AddElement(instanceHandle, setter, extraParams, true);
                                    }
                                    else
                                    {
                                        exporterIFC.RelateSpatialElement(roomId, instanceHandle);
                                        wrapper.AddElement(instanceHandle, setter, extraParams, false);
                                    }

                                    PropertyUtil.CreateInternalRevitPropertySets(exporterIFC, familyInstance, wrapper);
                                    break;
                                }
                            case IFCExportType.ExportBuildingElementProxy:
                            default:
                                {
                                    bool isBuildingElementProxy = (exportType == IFCExportType.ExportBuildingElementProxy);

                                    IFCAnyHandle localPlacementToUse;
                                    ElementId roomId = setter.UpdateRoomRelativeCoordinates(familyInstance, out localPlacementToUse);

                                    if (!isBuildingElementProxy)
                                    {
                                        if (FamilyExporterUtil.IsDistributionControlElementSubType(exportType))
                                        {
                                            string ifcelementType = null;
                                            ParameterUtil.GetStringValueFromElement(familyInstance, "IfcElementType", out ifcelementType);

                                            instanceHandle = IFCInstanceExporter.CreateDistributionControlElement(file, instanceGUID,
                                               ownerHistory, instanceName, instanceDescription, instanceObjectType,
                                               localPlacementToUse, repHnd, instanceTag, ifcelementType);

                                            if (roomId == ElementId.InvalidElementId)
                                            {
                                                wrapper.AddElement(instanceHandle, setter, extraParams, true);
                                            }
                                            else
                                            {
                                                exporterIFC.RelateSpatialElement(roomId, instanceHandle);
                                                wrapper.AddElement(instanceHandle, setter, extraParams, false);
                                            }
                                        }
                                        else if (IFCAnyHandleUtil.IsNullOrHasNoValue(instanceHandle))
                                        {
                                            isBuildingElementProxy = true;
                                        }
                                    }

                                    if (isBuildingElementProxy)
                                    {
                                        Toolkit.IFCElementComposition proxyType = Toolkit.IFCElementComposition.Element;

                                        instanceHandle = IFCInstanceExporter.CreateBuildingElementProxy(file, instanceGUID,
                                           ownerHistory, instanceName, instanceDescription, instanceObjectType,
                                           localPlacementToUse, repHnd, instanceTag, proxyType);

                                        if (roomId == ElementId.InvalidElementId)
                                        {
                                            wrapper.AddElement(instanceHandle, setter, extraParams, !isChildInContainer);
                                        }
                                        else
                                        {
                                            exporterIFC.RelateSpatialElement(roomId, instanceHandle);
                                            wrapper.AddElement(instanceHandle, setter, extraParams, false);
                                        }
                                    }

                                    IFCAnyHandle placementToUse = localPlacement;
                                    if (!useInstanceGeometry)
                                    {
                                        bool needToCreateOpenings = OpeningUtil.NeedToCreateOpenings(instanceHandle, extraParams);
                                        if (needToCreateOpenings)
                                        {
                                            Transform openingTrf = new Transform(originalTrf);
                                            Transform extraRot = new Transform(originalTrf);
                                            extraRot.Origin = XYZ.Zero;
                                            openingTrf = openingTrf.Multiply(extraRot);
                                            openingTrf = openingTrf.Multiply(typeInfo.StyleTransform);

                                            XYZ scaledOrigin = openingTrf.Origin * scale;
                                            IFCAnyHandle openingRelativePlacement = ExporterUtil.CreateAxis2Placement3D(file, scaledOrigin,
                                               openingTrf.get_Basis(2), openingTrf.get_Basis(0));
                                            IFCAnyHandle openingPlacement = ExporterUtil.CopyLocalPlacement(file, localPlacement);
                                            GeometryUtil.SetRelativePlacement(openingPlacement, openingRelativePlacement);
                                            placementToUse = openingPlacement;
                                        }
                                    }

                                    OpeningUtil.CreateOpeningsIfNecessary(instanceHandle, familyInstance, extraParams, offsetTransform,
                                        exporterIFC, placementToUse, setter, wrapper);
                                    PropertyUtil.CreateInternalRevitPropertySets(exporterIFC, familyInstance, wrapper);
                                    break;
                                }
                        }

                        if (!IFCAnyHandleUtil.IsNullOrHasNoValue(instanceHandle))
                        {
                            if (doorWindowInfo != null)
                            {
                                if (!ignoreDoorWindowOpening)
                                {
                                    if (!IFCAnyHandleUtil.IsNullOrHasNoValue(doorWindowInfo.GetOpening()))
                                    {
                                        string relGUID = GUIDUtil.CreateGUID();
                                        IFCInstanceExporter.CreateRelFillsElement(file, relGUID, ownerHistory, null, null, doorWindowInfo.GetOpening(), instanceHandle);
                                    }
                                    else if (doorWindowInfo.NeedsOpening)
                                    {
                                        bool added = doorWindowInfo.SetDelayedFamilyInstance(instanceHandle, localPlacement, doorWindowInfo.AssignedLevelId);
                                        if (added)
                                            exporterIFC.RegisterDoorWindowForOpeningUpdate(doorWindowInfo);
                                        else
                                        {
                                            // we need to fill a later opening.
                                            exporterIFC.RegisterDoorWindowForUncreatedOpening(familyInstance.Id, instanceHandle);
                                        }
                                    }
                                }
                            }

                            if (!exportParts)
                                CategoryUtil.CreateMaterialAssociations(doc, exporterIFC, instanceHandle, typeInfo.MaterialIds);

                            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(typeInfo.Style))
                                ExporterCacheManager.TypeRelationsCache.Add(typeInfo.Style, instanceHandle);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Exports a family instance as standard element.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="element">The element to be exported.</param>
        /// <param name="geometryElement">The geometry element.</param>
        /// <param name="familyType">The export type.</param>
        /// <param name="ifcEnumTypeString">The string value represents the IFC type.</param>
        /// <param name="productWrapper">The ProductWrapper.</param>
        /// <returns>True if the family instance was exported, false otherwise.</returns>
        static bool ExportFamilyInstanceAsStandardElement(ExporterIFC exporterIFC, Element element, GeometryElement geometryElement, IFCExportType familyType,
            string ifcEnumTypeString, ProductWrapper productWrapper)
        {
            switch (familyType)
            {
                // These entities don't get exported as a mapped instance.  As such, we export them using
                // the standard methods.

                // standard building elements
                case IFCExportType.ExportBeam:
                    BeamExporter.ExportBeam(exporterIFC, element, geometryElement, productWrapper);
                    return true;
                case IFCExportType.ExportBuildingElementProxy:
                    {
                        Element type = element.Document.GetElement(element.GetTypeId());
                        string objectType = NamingUtil.GetObjectTypeOverride(element, (type != null) ? type.Name : "");
                        if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(objectType, "ProvisionForVoid"))
                        {
                            IFCAnyHandle proxyHnd = ProxyElementExporter.ExportBuildingElementProxy(exporterIFC, element, geometryElement, productWrapper);
                            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(proxyHnd))
                            {
                                ExporterCacheManager.ElementToHandleCache.Register(element.Id, proxyHnd);
                                return true;
                            }
                        }
                        break;
                    }
                case IFCExportType.ExportFooting:
                    FootingExporter.ExportFooting(exporterIFC, element, geometryElement, ifcEnumTypeString, productWrapper);
                    return true;
                case IFCExportType.ExportCovering:
                    CeilingExporter.ExportCovering(exporterIFC, element, geometryElement, ifcEnumTypeString, productWrapper);
                    return true;
                case IFCExportType.ExportPile:
                    PileExporter.ExportPile(exporterIFC, element, geometryElement, ifcEnumTypeString, productWrapper);
                    return true;
                case IFCExportType.ExportRamp:
                    RampExporter.ExportRamp(exporterIFC, ifcEnumTypeString, element, geometryElement, 1, productWrapper);
                    return true;
                case IFCExportType.ExportRailing:
                    if (ExporterCacheManager.RailingCache.Contains(element.Id))
                    {
                        // Don't export this object if it is part of a parent railing.
                        if (!ExporterCacheManager.RailingSubElementCache.Contains(element.Id))
                            RailingExporter.ExportRailing(exporterIFC, element, geometryElement, ifcEnumTypeString, productWrapper);
                    }
                    else
                    {
                        ExporterCacheManager.RailingCache.Add(element.Id);
                    }
                    return true;
                case IFCExportType.ExportRoof:
                    RoofExporter.ExportRoof(exporterIFC, ifcEnumTypeString, element, geometryElement, productWrapper);
                    return true;
                case IFCExportType.ExportSlab:
                    FloorExporter.ExportFloor(exporterIFC, element, geometryElement, ifcEnumTypeString, productWrapper, false);
                    return true;
                case IFCExportType.ExportStair:
                    StairsExporter.ExportStairAsSingleGeometry(exporterIFC, ifcEnumTypeString, element, geometryElement, 1, productWrapper);
                    return true;
                case IFCExportType.ExportWall:
                    WallExporter.ExportWall(exporterIFC, element, geometryElement, productWrapper);
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Gets minimum height of a family symbol.
        /// </summary>
        /// <param name="symbol">
        /// The family symbol.
        /// </param>
        static double GetMinSymbolHeight(FamilySymbol symbol)
        {
            return ExporterIFCUtils.GetMinSymbolHeight(symbol);
        }

        /// <summary>
        /// Gets minimum width of a family symbol.
        /// </summary>
        /// <param name="symbol">
        /// The family symbol.
        /// </param>
        static double GetMinSymbolWidth(FamilySymbol symbol)
        {
            return ExporterIFCUtils.GetMinSymbolWidth(symbol);
        }

        /// <summary>
        /// Gets IFCColumnType from column type name.
        /// </summary>
        /// <param name="element">The column element.</param>
        /// <param name="columnType">The column type name.</param>
        /// <returns>The IFCColumnType.</returns>
        static IFCColumnType GetColumnType(Element element, string columnType)
        {
            string value = null;
            if (!ParameterUtil.GetStringValueFromElementOrSymbol(element, "IfcType", out value))
            {
                value = columnType;
            }

            if (String.IsNullOrEmpty(value))
                return IFCColumnType.Column;

            string newValue = NamingUtil.RemoveSpacesAndUnderscores(value);

            if (String.Compare(newValue, "USERDEFINED", true) == 0)
                return IFCColumnType.UserDefined;

            return IFCColumnType.Column;
        }
    }   
}
