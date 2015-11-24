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
using System.Linq;
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

                if (ExportGenericBuildingElement(exporterIFC, familyInstance, geometryElement, exportType, ifcEnumType, productWrapper))
                {
                    tr.Commit();
                    return;
                }

                // If we are exporting a column, we may need to split it into parts by level.  Create a list of ranges.
                IList<ElementId> levels = new List<ElementId>();
                IList<IFCRange> ranges = new List<IFCRange>();

                // We will not split walls and columns if the assemblyId is set, as we would like to keep the original wall
                // associated with the assembly, on the level of the assembly.
                bool splitColumn = (exportType == IFCExportType.IfcColumnType) && (ExporterCacheManager.ExportOptionsCache.WallAndColumnSplitting) &&
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
                    using (ExporterStateManager.RangeIndexSetter rangeSetter = new ExporterStateManager.RangeIndexSetter())
                    {
                        for (int ii = 0; ii < numPartsToExport; ii++)
                        {
                            rangeSetter.IncreaseRangeIndex();
                            ExportFamilyInstanceAsMappedItem(exporterIFC, familyInstance, exportType, ifcEnumType, productWrapper,
                              levels[ii], ranges[ii], null);
                        }
                    }

                    if (ExporterCacheManager.DummyHostCache.HasRegistered(familyInstance.Id))
                    {
                        using (ExporterStateManager.RangeIndexSetter rangeSetter = new ExporterStateManager.RangeIndexSetter())
                        {
                            List<KeyValuePair<ElementId, IFCRange>> levelRangeList = ExporterCacheManager.DummyHostCache.Find(familyInstance.Id);
                            foreach (KeyValuePair<ElementId, IFCRange> levelRange in levelRangeList)
                            {
                                rangeSetter.IncreaseRangeIndex();
                                ExportFamilyInstanceAsMappedItem(exporterIFC, familyInstance, exportType, ifcEnumType, productWrapper, levelRange.Key, levelRange.Value, null);
                            }
                        }
                    }
                }

                tr.Commit();
            }
        }

        private static IFCAnyHandle CreateFamilyTypeHandle(ExporterIFC exporterIFC, FamilyTypeInfo typeInfo, DoorWindowInfo doorWindowInfo, 
            IFCAnyHandle bodyRepresentation, IFCAnyHandle planRepresentation,
            Element familyInstance, ElementType familySymbol, ElementType originalFamilySymbol, 
            bool useInstanceGeometry, bool exportParts,
            IFCExportType exportType, string revitObjectType, string ifcEnumType, 
            out HashSet<IFCAnyHandle> propertySets)
        {
            // for many
            propertySets = new HashSet<IFCAnyHandle>();

            IFCFile file = exporterIFC.GetFile();

            IFCAnyHandle repMap2dHnd = null;
            IFCAnyHandle repMap3dHnd = null;

            IList<IFCAnyHandle> repMapList = new List<IFCAnyHandle>();
            {
                IFCAnyHandle origin = null;
                if (bodyRepresentation != null)
                {
                    if (origin == null)
                        origin = ExporterUtil.CreateAxis2Placement3D(file);
                    repMap3dHnd = IFCInstanceExporter.CreateRepresentationMap(file, origin, bodyRepresentation);
                    repMapList.Add(repMap3dHnd);
                }

                if (!IFCAnyHandleUtil.IsNullOrHasNoValue(planRepresentation))
                {
                    if (origin == null)
                        origin = ExporterUtil.CreateAxis2Placement3D(file);
                    repMap2dHnd = IFCInstanceExporter.CreateRepresentationMap(file, origin, planRepresentation);
                    repMapList.Add(repMap2dHnd);
                }
            }

            // We won't allow creating a type if we aren't creating an instance.
            // We won't create the instance if: we are exporting to CV2.0, we have no 2D, 3D, or bounding box geometry, and we aren't exporting parts.
            bool willCreateInstance = !(repMapList.Count == 0 && ExporterCacheManager.ExportOptionsCache.ExportAsCoordinationView2 &&
                !ExporterCacheManager.ExportOptionsCache.ExportBoundingBox && !exportParts);
            if (!willCreateInstance)
                return null;

            IFCAnyHandle typeStyle = null;

            IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

            // for Door, Window
            bool paramTakesPrecedence = false; // For Revit, this is currently always false.
            bool sizeable = false;

            string guid = null;
            if (useInstanceGeometry)
            {
                int subElementIndex = ExporterStateManager.GetCurrentRangeIndex();
                if (subElementIndex == 0)
                    guid = GUIDUtil.CreateSubElementGUID(familyInstance, (int)IFCFamilyInstanceSubElements.InstanceAsType);
                else if (subElementIndex <= ExporterStateManager.RangeIndexSetter.GetMaxStableGUIDs())
                    guid = GUIDUtil.CreateSubElementGUID(familyInstance, (int)IFCGenericSubElements.SplitTypeStart + subElementIndex - 1);
                else
                    guid = GUIDUtil.CreateGUID();
            }
            else
                guid = GUIDUtil.CreateGUID(originalFamilySymbol);

            string symId = NamingUtil.CreateIFCElementId(originalFamilySymbol);

            string gentypeName = NamingUtil.GetNameOverride(familySymbol, revitObjectType);
            string gentypeDescription = NamingUtil.GetDescriptionOverride(familySymbol, null);
            string gentypeApplicableOccurrence = NamingUtil.GetOverrideStringValue(familySymbol, "IfcApplicableOccurrence", null);
            string gentypeTag = NamingUtil.GetTagOverride(familySymbol, symId);
            string gentypeElementType = NamingUtil.GetOverrideStringValue(familySymbol, "IfcElementType", revitObjectType);

            // This covers many generic types.  If we can't find it in the list here, do custom exports.
            typeStyle = FamilyExporterUtil.ExportGenericType(exporterIFC, exportType, ifcEnumType, guid,
               gentypeName, gentypeDescription, gentypeApplicableOccurrence, propertySets, repMapList, gentypeTag, gentypeElementType,
               familyInstance, familySymbol);

            // Cover special cases not covered above.
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(typeStyle))
            {
                string symbolTag = NamingUtil.GetTagOverride(familySymbol, NamingUtil.CreateIFCElementId(familySymbol));
                switch (exportType)
                {
                    case IFCExportType.IfcColumnType:
                        {
                            string columnType = "Column";
                            typeStyle = IFCInstanceExporter.CreateColumnType(file, guid, ownerHistory, gentypeName,
                                gentypeDescription, gentypeApplicableOccurrence, propertySets, repMapList, symbolTag,
                                gentypeElementType, GetColumnType(familyInstance, columnType));
                            break;
                        }
                    case IFCExportType.IfcDoorType:
                        {
                            IFCAnyHandle doorLining = DoorWindowUtil.CreateDoorLiningProperties(exporterIFC, familyInstance);
                            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(doorLining))
                                propertySets.Add(doorLining);

                            IList<IFCAnyHandle> doorPanels = DoorWindowUtil.CreateDoorPanelProperties(exporterIFC, doorWindowInfo,
                               familyInstance);
                            propertySets.UnionWith(doorPanels);

                            if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
                            {
                                string doorTypeGUID = GUIDUtil.CreateSubElementGUID(originalFamilySymbol, (int)IFCDoorSubElements.DoorType);
                                typeStyle = IFCInstanceExporter.CreateDoorType(file, doorTypeGUID, ownerHistory, gentypeName,
                                   gentypeDescription, gentypeApplicableOccurrence, propertySets, repMapList, symbolTag,
                                   doorWindowInfo.PreDefinedType, doorWindowInfo.DoorOperationTypeString,
                                   paramTakesPrecedence, doorWindowInfo.UserDefinedOperationType);
                            }
                            else
                            {
                                string doorStyleGUID = GUIDUtil.CreateSubElementGUID(originalFamilySymbol, (int)IFCDoorSubElements.DoorStyle);
                                typeStyle = IFCInstanceExporter.CreateDoorStyle(file, doorStyleGUID, ownerHistory, gentypeName,
                                   gentypeDescription, gentypeApplicableOccurrence, propertySets, repMapList, symbolTag,
                                   doorWindowInfo.DoorOperationTypeString, DoorWindowUtil.GetDoorStyleConstruction(familyInstance),
                                   paramTakesPrecedence, sizeable);
                            }
                            break;
                        }
                    case IFCExportType.IfcSpace:
                        {
                            typeStyle = IFCInstanceExporter.CreateSpaceType(file, guid, ownerHistory, gentypeName,
                               gentypeDescription, gentypeApplicableOccurrence, propertySets, repMapList, symbolTag,
                               gentypeElementType);

                            break;
                        }
                    case IFCExportType.IfcSystemFurnitureElementType:
                        {
                            typeStyle = IFCInstanceExporter.CreateSystemFurnitureElementType(file, guid, ownerHistory, gentypeName,
                               gentypeDescription, gentypeApplicableOccurrence, propertySets, repMapList, symbolTag,
                               gentypeElementType);

                            break;
                        }
                    case IFCExportType.IfcWindowType:
                        {
                            Toolkit.IFCWindowStyleOperation operationType = DoorWindowUtil.GetIFCWindowStyleOperation(originalFamilySymbol);
                            IFCWindowStyleConstruction constructionType = DoorWindowUtil.GetIFCWindowStyleConstruction(familyInstance);

                            IFCAnyHandle windowLining = DoorWindowUtil.CreateWindowLiningProperties(exporterIFC, familyInstance, null);
                            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(windowLining))
                                propertySets.Add(windowLining);

                            IList<IFCAnyHandle> windowPanels =
                               DoorWindowUtil.CreateWindowPanelProperties(exporterIFC, familyInstance, null);
                            propertySets.UnionWith(windowPanels);

                            string windowStyleGUID = GUIDUtil.CreateSubElementGUID(originalFamilySymbol, (int)IFCWindowSubElements.WindowStyle);

                            if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
                            {
                                typeStyle = IFCInstanceExporter.CreateWindowType(file, windowStyleGUID, ownerHistory, gentypeName,
                                   gentypeDescription, gentypeApplicableOccurrence, propertySets, repMapList, symbolTag,
                                   doorWindowInfo.PreDefinedType, DoorWindowUtil.GetIFCWindowPartitioningType(originalFamilySymbol),
                                   paramTakesPrecedence, doorWindowInfo.UserDefinedOperationType);
                            }
                            else
                            {
                                typeStyle = IFCInstanceExporter.CreateWindowStyle(file, windowStyleGUID, ownerHistory, gentypeName,
                                   gentypeDescription, gentypeApplicableOccurrence, propertySets, repMapList, symbolTag,
                                   constructionType, operationType, paramTakesPrecedence, sizeable);
                            }
                            break;
                        }
                    case IFCExportType.IfcBuildingElementProxy:
                    case IFCExportType.IfcBuildingElementProxyType:
                        {
                            Revit.IFC.Common.Enums.IFCEntityType IFCTypeEntity;
                            if (!Enum.TryParse(ifcEnumType, out IFCTypeEntity))
                                break;    // The export type is unknown IFC type entity
                            typeStyle = IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, gentypeName,
                                gentypeDescription, gentypeApplicableOccurrence, propertySets, repMapList, symbolTag,
                                gentypeElementType, FamilyExporterUtil.GetPreDefinedType<Toolkit.IFCBuildingElementProxyType>(familyInstance, ifcEnumType).ToString());
                            break;
                        }
                }

                if (IFCAnyHandleUtil.IsNullOrHasNoValue(typeStyle))
                {
                    if (!IFCAnyHandleUtil.IsNullOrHasNoValue(repMap2dHnd))
                        typeInfo.Map2DHandle = repMap2dHnd;
                    if (!IFCAnyHandleUtil.IsNullOrHasNoValue(repMap3dHnd))
                        typeInfo.Map3DHandle = repMap3dHnd;
                }
            }

            return typeStyle;
        }

        private static bool CanHaveInsulationOrLining(IFCExportType exportType, ElementId categoryId)
        {
           // This is intended to reduce the number of exceptions thrown in GetLiningIds and GetInsulationIds.
           // There may still be some exceptions thrown as the category list below is still too large for GetLiningIds.
           if (exportType != IFCExportType.IfcDuctFittingType && exportType != IFCExportType.IfcPipeFittingType &&
              exportType != IFCExportType.IfcDuctSegmentType && exportType != IFCExportType.IfcPipeSegmentType)
              return false;

           int catIdAsInt = categoryId.IntegerValue;
           if ((catIdAsInt == (int)BuiltInCategory.OST_DuctAccessory) ||
              (catIdAsInt == (int)BuiltInCategory.OST_DuctCurves) ||
              (catIdAsInt == (int)BuiltInCategory.OST_DuctFitting) ||
              (catIdAsInt == (int)BuiltInCategory.OST_FlexDuctCurves) ||
              (catIdAsInt == (int)BuiltInCategory.OST_FlexPipeCurves) ||
              (catIdAsInt == (int)BuiltInCategory.OST_PipeAccessory) ||
              (catIdAsInt == (int)BuiltInCategory.OST_PipeCurves) ||
              (catIdAsInt == (int)BuiltInCategory.OST_PipeFitting))
              return true;

           return false;
        }

        /// <summary>
        /// Exports a family instance as a mapped item.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="familyInstance">The family instance to be exported.</param>
        /// <param name="exportType">The export type.</param>
        /// <param name="ifcEnumType">The string value represents the IFC type.</param>
        /// <param name="wrapper">The ProductWrapper.</param>
        /// <param name="overrideLevelId">The level id.</param>
        /// <param name="range">The range of this family instance to be exported.</param>
        public static void ExportFamilyInstanceAsMappedItem(ExporterIFC exporterIFC,
           FamilyInstance familyInstance, IFCExportType exportType, string ifcEnumType,
           ProductWrapper wrapper, ElementId overrideLevelId, IFCRange range, IFCAnyHandle parentLocalPlacement)
        {
            bool exportParts = PartExporter.CanExportParts(familyInstance);
            bool isSplit = range != null;
            if (exportParts && !PartExporter.CanExportElementInPartExport(familyInstance, isSplit ? overrideLevelId : familyInstance.LevelId, isSplit))
                return;

            Document doc = familyInstance.Document;
            IFCFile file = exporterIFC.GetFile();

            // The "originalFamilySymbol" has the right geometry, but should be used as little as possible.
            FamilySymbol originalFamilySymbol = ExporterIFCUtils.GetOriginalSymbol(familyInstance);
            FamilySymbol familySymbol = familyInstance.Symbol;
            if (originalFamilySymbol == null || familySymbol == null)
                return;

            ProductWrapper familyProductWrapper = ProductWrapper.Create(wrapper);
            Options options = GeometryUtil.GetIFCExportGeometryOptions();

            IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

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
                // Extra information if we are exporting a door or a window.
                DoorWindowInfo doorWindowInfo = null;
                if (exportType == IFCExportType.IfcDoorType)
                    doorWindowInfo = DoorWindowExporter.CreateDoor(exporterIFC, familyInstance, hostElement, overrideLevelId, trf);
                else if (exportType == IFCExportType.IfcWindowType)
                    doorWindowInfo = DoorWindowExporter.CreateWindow(exporterIFC, familyInstance, hostElement, overrideLevelId, trf);

                FamilyTypeInfo typeInfo = new FamilyTypeInfo();

                bool flipped = doorWindowInfo != null ? doorWindowInfo.FlippedSymbol : false;
                FamilyTypeInfo currentTypeInfo = ExporterCacheManager.TypeObjectsCache.Find(originalFamilySymbol.Id, flipped, exportType);
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
                        doorWindowTrf = ExporterIFCUtils.GetTransformForDoorOrWindow(familyInstance, originalFamilySymbol,
                            doorWindowInfo.FlippedX, doorWindowInfo.FlippedY);
                    }
                    else
                    {
                        dummyPlacement = ExporterUtil.CreateLocalPlacement(file, null, null);
                        extraParams.SetLocalPlacement(dummyPlacement);
                    }

                    bool needToCreate2d = ExporterCacheManager.ExportOptionsCache.ExportAnnotations;
                    GeometryElement exportGeometry =
                       useInstanceGeometry ? familyInstance.get_Geometry(options) : originalFamilySymbol.get_Geometry(options);

                    if (!exportParts)
                    {
                        using (TransformSetter trfSetter = TransformSetter.Create())
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
                            bool hasSolidsOrMeshesInSymbol = (solids.Count > 0 || polyMeshes.Count > 0);

                            if (range != null && !hasSolidsOrMeshesInSymbol)
                                return; // no proper split geometry to export.

                            if (hasSolidsOrMeshesInSymbol)
                            {
                               geomObjects = FamilyExporterUtil.RemoveInvisibleSolidsAndMeshes(doc, exporterIFC, solids, polyMeshes);
                                if ((geomObjects.Count == 0))
                                    return; // no proper visible split geometry to export.
                            }
                            else
                                geomObjects.Add(exportGeometry);

                            bool tryToExportAsExtrusion = (!ExporterCacheManager.ExportOptionsCache.ExportAs2x2 || (exportType == IFCExportType.IfcColumnType));

                            if (exportType == IFCExportType.IfcColumnType)
                            {
                                extraParams.PossibleExtrusionAxes = IFCExtrusionAxes.TryZ;

                                if (ExporterCacheManager.ExportOptionsCache.ExportAsCoordinationView2 && solids.Count > 0)
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
                                bodyData = BodyExporter.ExportBody(exporterIFC, familyInstance, categoryId, ElementId.InvalidElementId,
                                    geomObjects, bodyExporterOptions, extraParams);
                                typeInfo.MaterialIds = bodyData.MaterialIds;
                                bodyRepresentation = bodyData.RepresentationHnd;
                                offsetTransform = bodyData.OffsetTransform;
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
                                curveOffset = -UnitUtil.UnscaleLength(offsetTransform.Origin);

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
                                    planRepresentation = RepresentationUtil.CreateGeometricSetRep(exporterIFC, familyInstance, categoryId, "FootPrint",
                                       contextOfItems2d, bodyItems);
                                }
                            }
                        }
                    }

                    if (doorWindowInfo != null)
                        typeInfo.StyleTransform = doorWindowTrf.Inverse;
                    else
                        typeInfo.StyleTransform = ExporterIFCUtils.GetUnscaledTransform(exporterIFC, extraParams.GetLocalPlacement());

                    // for many
                    HashSet<IFCAnyHandle> propertySets = null;
                    IFCAnyHandle typeStyle = CreateFamilyTypeHandle(exporterIFC, typeInfo, doorWindowInfo, bodyRepresentation, planRepresentation,
                        familyInstance, familySymbol, originalFamilySymbol, useInstanceGeometry, exportParts,
                        exportType, revitObjectType, ifcEnumType, out propertySets);

                    if (!IFCAnyHandleUtil.IsNullOrHasNoValue(typeStyle))
                    {
                        wrapper.RegisterHandleWithElementType(familySymbol, typeStyle, propertySets);

                        CategoryUtil.CreateMaterialAssociations(exporterIFC, typeStyle, typeInfo.MaterialIds);

                        typeInfo.Style = typeStyle;

                        if ((exportType == IFCExportType.IfcColumnType) || (exportType == IFCExportType.IfcMemberType))
                        {
                            typeInfo.ScaledArea = extraParams.ScaledArea;
                            typeInfo.ScaledDepth = extraParams.ScaledLength;
                            typeInfo.ScaledInnerPerimeter = extraParams.ScaledInnerPerimeter;
                            typeInfo.ScaledOuterPerimeter = extraParams.ScaledOuterPerimeter;
                        }

                        ClassificationUtil.CreateClassification(exporterIFC, file, familySymbol, typeStyle);        // Create other generic classification from ClassificationCode(s)
                        ClassificationUtil.CreateUniformatClassification(exporterIFC, file, originalFamilySymbol, typeStyle);
                    }
                }

                if (found && !typeInfo.IsValid())
                    typeInfo = currentTypeInfo;

                // we'll pretend we succeeded, but we'll do nothing.
                if (!typeInfo.IsValid())
                    return;

                // add to the map, as long as we are not using range, not using instance geometry, and don't have extra openings.
                if ((range == null) && !useInstanceGeometry && (extraParams.GetOpenings().Count == 0))
                    ExporterCacheManager.TypeObjectsCache.Register(originalFamilySymbol.Id, flipped, exportType, typeInfo);

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
                        ISet<IFCAnyHandle> representations = new HashSet<IFCAnyHandle>();
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
                {
                    boundingBoxTrf = boundingBoxTrf.Multiply(doorWindowTrf);
                    boundingBoxRep = BoundingBoxExporter.ExportBoundingBox(exporterIFC, geomObjects, boundingBoxTrf);
                }
                else
                {
                    boundingBoxTrf = boundingBoxTrf.Multiply(trf.Inverse);
                    boundingBoxRep = BoundingBoxExporter.ExportBoundingBox(exporterIFC, familyInstance.get_Geometry(options), boundingBoxTrf);
                }

                if (boundingBoxRep != null)
                    shapeReps.Add(boundingBoxRep);

                IFCAnyHandle repHnd = (shapeReps.Count > 0) ? IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, shapeReps) : null;

                using (PlacementSetter setter = PlacementSetter.Create(exporterIFC, familyInstance, trf, null, overrideLevelId))
                {
                    IFCAnyHandle instanceHandle = null;
                    IFCAnyHandle localPlacement = setter.LocalPlacement;

                    // We won't create the instance if: 
                    // (1) we are exporting to CV2.0, (2) we have no 2D, 3D, or bounding box geometry, and (3) we aren't exporting parts.
                    if (!(repHnd == null && ExporterCacheManager.ExportOptionsCache.ExportAsCoordinationView2 && !exportParts))
                    {
                        string instanceGUID = null;

                        int subElementIndex = ExporterStateManager.GetCurrentRangeIndex();
                        if (subElementIndex == 0)
                            instanceGUID = GUIDUtil.CreateGUID(familyInstance);
                        else if (subElementIndex <= ExporterStateManager.RangeIndexSetter.GetMaxStableGUIDs())
                            instanceGUID = GUIDUtil.CreateSubElementGUID(familyInstance, subElementIndex + (int)IFCGenericSubElements.SplitInstanceStart - 1);
                        else
                            instanceGUID = GUIDUtil.CreateGUID();

                        string instanceName = NamingUtil.GetNameOverride(familyInstance, NamingUtil.GetIFCName(familyInstance));
                        string instanceDescription = NamingUtil.GetDescriptionOverride(familyInstance, null);
                        string instanceObjectType = NamingUtil.GetObjectTypeOverride(familyInstance, revitObjectType);
                        string instanceTag = NamingUtil.GetTagOverride(familyInstance, NamingUtil.CreateIFCElementId(familyInstance));

                        IFCAnyHandle overrideLocalPlacement = null;
                        bool isChildInContainer = familyInstance.AssemblyInstanceId != ElementId.InvalidElementId;

                        if (parentLocalPlacement != null)
                        {
                            Transform relTrf = ExporterIFCUtils.GetRelativeLocalPlacementOffsetTransform(parentLocalPlacement, localPlacement);
                            Transform inverseTrf = relTrf.Inverse;

                            IFCAnyHandle plateLocalPlacement = ExporterUtil.CreateLocalPlacement(file, parentLocalPlacement,
                                inverseTrf.Origin, inverseTrf.BasisZ, inverseTrf.BasisX);
                            overrideLocalPlacement = plateLocalPlacement;
                        }

                        instanceHandle = FamilyExporterUtil.ExportGenericInstance(exportType, exporterIFC, familyInstance,
                           wrapper, setter, extraParams, instanceGUID, ownerHistory, instanceName, instanceDescription, instanceObjectType,
                           exportParts ? null : repHnd, instanceTag, ifcEnumType, overrideLocalPlacement);

                        if (exportParts)
                            PartExporter.ExportHostPart(exporterIFC, familyInstance, instanceHandle, familyProductWrapper, setter, setter.LocalPlacement, overrideLevelId);

                        if (ElementFilteringUtil.IsMEPType(exportType) || ElementFilteringUtil.ProxyForMEPType(familyInstance, exportType))
                        {
                            ExporterCacheManager.MEPCache.Register(familyInstance, instanceHandle);
                            // For ducts and pipes, check later if there is an associated duct or pipe.
                            if (CanHaveInsulationOrLining(exportType, categoryId))
                                ExporterCacheManager.MEPCache.CoveredElementsCache.Add(familyInstance.Id);
                        }

                        switch (exportType)
                        {
                            case IFCExportType.IfcBeam:
                                {
                                    IFCAnyHandle placementToUse = localPlacement;
                                    
                                    // NOTE: We do not expect openings here, as they are created as part of creating an extrusion in ExportBody above.
                                    // However, if this were the case, we would have exported this beam in ExportBeamAsStandardElement above.

                                    OpeningUtil.CreateOpeningsIfNecessary(instanceHandle, familyInstance, extraParams, offsetTransform,
                                        exporterIFC, placementToUse, setter, wrapper);

                                    //export Base Quantities.
                                    PropertyUtil.CreateBeamColumnBaseQuantities(exporterIFC, instanceHandle, familyInstance, typeInfo, geomObjects);

                                    // Register the beam's IFC handle for later use by truss and beam system export.
                                    ExporterCacheManager.ElementToHandleCache.Register(familyInstance.Id, instanceHandle);

                                    break;
                                }
                            case IFCExportType.IfcColumnType:
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

                                            XYZ scaledOrigin = UnitUtil.ScaleLength(openingTrf.Origin);
                                            IFCAnyHandle openingRelativePlacement = ExporterUtil.CreateAxis2Placement3D(file, scaledOrigin,
                                               openingTrf.get_Basis(2), openingTrf.get_Basis(0));
                                            IFCAnyHandle openingPlacement = ExporterUtil.CopyLocalPlacement(file, localPlacement);
                                            GeometryUtil.SetRelativePlacement(openingPlacement, openingRelativePlacement);
                                            placementToUse = openingPlacement;
                                        }
                                    }

                                    OpeningUtil.CreateOpeningsIfNecessary(instanceHandle, familyInstance, extraParams, offsetTransform,
                                        exporterIFC, placementToUse, setter, wrapper);

                                    //export Base Quantities.
                                    PropertyUtil.CreateBeamColumnBaseQuantities(exporterIFC, instanceHandle, familyInstance, typeInfo, geomObjects);
                                    break;
                                }
                            case IFCExportType.IfcDoorType:
                            case IFCExportType.IfcWindowType:
                                {
                                    double doorHeight = GetMinSymbolHeight(originalFamilySymbol);
                                    double doorWidth = GetMinSymbolWidth(originalFamilySymbol);

                                    double height = UnitUtil.ScaleLength(doorHeight);
                                    double width = UnitUtil.ScaleLength(doorWidth);

                                    IFCAnyHandle doorWindowLocalPlacement = !IFCAnyHandleUtil.IsNullOrHasNoValue(overrideLocalPlacement) ?
                                        overrideLocalPlacement : localPlacement;
                                    if (exportType == IFCExportType.IfcDoorType)
                                        instanceHandle = IFCInstanceExporter.CreateDoor(file, instanceGUID, ownerHistory,
                                           instanceName, instanceDescription, instanceObjectType, doorWindowLocalPlacement,
                                           repHnd, instanceTag, height, width, doorWindowInfo.PreDefinedType,doorWindowInfo.DoorOperationTypeString,
                                           doorWindowInfo.UserDefinedOperationType);
                                    else if (exportType == IFCExportType.IfcWindowType)
                                        instanceHandle = IFCInstanceExporter.CreateWindow(file, instanceGUID, ownerHistory,
                                           instanceName, instanceDescription, instanceObjectType, doorWindowLocalPlacement,
                                           repHnd, instanceTag, height, width, doorWindowInfo.PreDefinedType, DoorWindowUtil.GetIFCWindowPartitioningType(originalFamilySymbol),
                                           doorWindowInfo.UserDefinedPartitioningType);
                                    wrapper.AddElement(familyInstance, instanceHandle, setter, extraParams, true);

                                    SpaceBoundingElementUtil.RegisterSpaceBoundingElementHandle(exporterIFC, instanceHandle, familyInstance.Id,
                                        setter.LevelId);

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
                                            XYZ scaledOrigin = UnitUtil.ScaleLength(openingTrf.Origin);
                                            IFCAnyHandle openingLocalPlacement = ExporterUtil.CreateLocalPlacement(file, doorWindowLocalPlacement,
                                                scaledOrigin, openingTrf.BasisZ, openingTrf.BasisX);
                                            placementToUse = openingLocalPlacement;
                                        }
                                    }

                                    OpeningUtil.CreateOpeningsIfNecessary(instanceHandle, familyInstance, extraParams, offsetTransform,
                                        exporterIFC, placementToUse, setter, wrapper);
                                    break;
                                }
                            case IFCExportType.IfcMemberType:
                                {
                                    OpeningUtil.CreateOpeningsIfNecessary(instanceHandle, familyInstance, extraParams, offsetTransform,
                                        exporterIFC, localPlacement, setter, wrapper);

                                    //export Base Quantities.
                                    PropertyUtil.CreateBeamColumnBaseQuantities(exporterIFC, instanceHandle, familyInstance, typeInfo, null);
                                    break;
                                }
                            case IFCExportType.IfcPlateType:
                                {
                                    OpeningUtil.CreateOpeningsIfNecessary(instanceHandle, familyInstance, extraParams, offsetTransform,
                                        exporterIFC, localPlacement, setter, wrapper);
                                    break;
                                }
                            case IFCExportType.IfcTransportElementType:
                                {
                                    IFCAnyHandle localPlacementToUse;
                                    ElementId roomId = setter.UpdateRoomRelativeCoordinates(familyInstance, out localPlacementToUse);

                                    string operationTypeStr;
                                    if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
                                    {
                                        // It is PreDefinedType attribute in IFC4
                                        Toolkit.IFC4.IFCTransportElementType operationType = FamilyExporterUtil.GetPreDefinedType<Toolkit.IFC4.IFCTransportElementType>(familyInstance, ifcEnumType);
                                        operationTypeStr = operationType.ToString();
                                    }
                                    else
                                    {
                                        Toolkit.IFCTransportElementType operationType = FamilyExporterUtil.GetPreDefinedType<Toolkit.IFCTransportElementType>(familyInstance, ifcEnumType);
                                        operationTypeStr = operationType.ToString();
                                    }

                                    double capacityByWeight = 0.0;
                                    ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "IfcCapacityByWeight", out capacityByWeight);
                                    double capacityByNumber = 0.0;
                                    ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "IfcCapacityByNumber", out capacityByNumber);

                                    instanceHandle = IFCInstanceExporter.CreateTransportElement(file, instanceGUID, ownerHistory,
                                       instanceName, instanceDescription, instanceObjectType,
                                       localPlacementToUse, repHnd, instanceTag, operationTypeStr, capacityByWeight, capacityByNumber);

                                    bool containedInSpace = (roomId != ElementId.InvalidElementId);
                                    wrapper.AddElement(familyInstance, instanceHandle, setter, extraParams, !containedInSpace);
                                    if (containedInSpace)
                                         ExporterCacheManager.SpaceInfoCache.RelateToSpace(roomId, instanceHandle);

                                    break;
                                }
                            //case IFCExportType.IfcBuildingElementProxy:
                            //case IFCExportType.IfcBuildingElementProxyType:
                            default:
                                {
                                    if (IFCAnyHandleUtil.IsNullOrHasNoValue(instanceHandle))
                                    {
                                        bool isBuildingElementProxy =
                                            ((exportType == IFCExportType.IfcBuildingElementProxy) ||
                                            (exportType == IFCExportType.IfcBuildingElementProxyType));

                                        IFCAnyHandle localPlacementToUse = null;
                                        ElementId roomId = setter.UpdateRoomRelativeCoordinates(familyInstance, out localPlacementToUse);

                                        if (!isBuildingElementProxy && FamilyExporterUtil.IsDistributionControlElementSubType(exportType))
                                        {
                                            string ifcelementType = null;
                                            ParameterUtil.GetStringValueFromElement(familyInstance.Id, "IfcElementType", out ifcelementType);

                                            instanceHandle = IFCInstanceExporter.CreateDistributionControlElement(file, instanceGUID,
                                               ownerHistory, instanceName, instanceDescription, instanceObjectType,
                                               localPlacementToUse, repHnd, instanceTag, ifcelementType);
                                        }
                                        else 
                                        {
                                            instanceHandle = IFCInstanceExporter.CreateBuildingElementProxy(file, instanceGUID,
                                               ownerHistory, instanceName, instanceDescription, instanceObjectType,
                                               localPlacementToUse, repHnd, instanceTag, null);
                                        }

                                        bool containedInSpace = (roomId != ElementId.InvalidElementId);
                                        bool associateToLevel = containedInSpace ? false : !isChildInContainer;
                                        wrapper.AddElement(familyInstance, instanceHandle, setter, extraParams, associateToLevel);
                                        if (containedInSpace)
                                            ExporterCacheManager.SpaceInfoCache.RelateToSpace(roomId, instanceHandle);
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

                                            XYZ scaledOrigin = UnitUtil.ScaleLength(openingTrf.Origin);
                                            IFCAnyHandle openingRelativePlacement = ExporterUtil.CreateAxis2Placement3D(file, scaledOrigin,
                                               openingTrf.get_Basis(2), openingTrf.get_Basis(0));
                                            IFCAnyHandle openingPlacement = ExporterUtil.CopyLocalPlacement(file, localPlacement);
                                            GeometryUtil.SetRelativePlacement(openingPlacement, openingRelativePlacement);
                                            placementToUse = openingPlacement;
                                        }
                                    }

                                    OpeningUtil.CreateOpeningsIfNecessary(instanceHandle, familyInstance, extraParams, offsetTransform,
                                        exporterIFC, placementToUse, setter, wrapper);
                                    break;
                                }
                        }

                        if (!IFCAnyHandleUtil.IsNullOrHasNoValue(instanceHandle))
                        {
                            ExporterCacheManager.HandleToElementCache.Register(instanceHandle, familyInstance.Id);

                            if (!exportParts)
                                CategoryUtil.CreateMaterialAssociations(exporterIFC, instanceHandle, typeInfo.MaterialIds);

                            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(typeInfo.Style))
                                ExporterCacheManager.TypeRelationsCache.Add(typeInfo.Style, instanceHandle);
                        }
                    }

                    if (doorWindowInfo != null)
                    {
                        DoorWindowDelayedOpeningCreator delayedCreator = DoorWindowDelayedOpeningCreator.Create(exporterIFC, doorWindowInfo, instanceHandle, setter.LevelId);
                        if (delayedCreator != null)
                            ExporterCacheManager.DoorWindowDelayedOpeningCreatorCache.Add(delayedCreator);
                    }
                }
            }
        }

        /// <summary>
        /// Exports a generic element as one of a few IFC building element entity types.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="element">The element to be exported.</param>
        /// <param name="geometryElement">The geometry element.</param>
        /// <param name="familyType">The export type.</param>
        /// <param name="ifcEnumTypeString">The string value represents the IFC type.</param>
        /// <param name="productWrapper">The ProductWrapper.</param>
        /// <returns>True if the elements was exported, false otherwise.</returns>
        static public bool ExportGenericBuildingElement(ExporterIFC exporterIFC, Element element, GeometryElement geometryElement, IFCExportType exportType,
            string ifcEnumTypeString, ProductWrapper productWrapper)
        {
           // This function is here because it was originally used exclusive by FamilyInstances.  Moving forward, this will be combined with some other
           // functions to attempt to create a way to export any element as any IFC entity.  There will still be functions that do a better job of mapping
           // specific Revit element types to specific IFC entity types (e.g., a Revit Wall to an IFC IfcWallStandardCase), but most elements will use generic
           // handling.
           // Note that this function doesn't support creating types - it exports a simple IFC instance only of a few possible types.
           switch (exportType)
            {
                // standard building elements
                case IFCExportType.IfcBeam:
                    {
                        // We will say that we exported the beam if either we generated an IfcBeam, or if we determined that there
                        // was nothing to export, either because the beam had no geometry to export, or it was completely clipped.
                        bool dontExport;
                        IFCAnyHandle beamHnd = BeamExporter.ExportBeamAsStandardElement(exporterIFC, element, geometryElement, productWrapper, out dontExport);
                        return (dontExport || !IFCAnyHandleUtil.IsNullOrHasNoValue(beamHnd));
                    }
                case IFCExportType.IfcBuildingElementProxy:
                case IFCExportType.IfcBuildingElementProxyType:
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
                case IFCExportType.IfcFooting:
                    FootingExporter.ExportFooting(exporterIFC, element, geometryElement, ifcEnumTypeString, productWrapper);
                    return true;
                case IFCExportType.IfcCovering:
                    CeilingExporter.ExportCovering(exporterIFC, element, geometryElement, ifcEnumTypeString, productWrapper);
                    return true;
                case IFCExportType.IfcPile:
                    PileExporter.ExportPile(exporterIFC, element, geometryElement, ifcEnumTypeString, productWrapper);
                    return true;
                case IFCExportType.IfcRamp:
                    RampExporter.ExportRamp(exporterIFC, ifcEnumTypeString, element, geometryElement, 1, productWrapper);
                    return true;
                case IFCExportType.IfcRailing:
                case IFCExportType.IfcRailingType:
                    if (ExporterCacheManager.RailingCache.Contains(element.Id))
                    {
                        // Don't export this object if it is part of a parent railing.
                        if (!ExporterCacheManager.RailingSubElementCache.Contains(element.Id))
                        {
                            // RailingExporter.ExportRailing(exporterIFC, element, geometryElement, ifcEnumTypeString, productWrapper);
                            // Allow railing code to create instance and type.
                            return false;
                        }
                    }
                    else
                    {
                        ExporterCacheManager.RailingCache.Add(element.Id);
                    }
                    return true;
                case IFCExportType.IfcRoof:
                    RoofExporter.ExportRoof(exporterIFC, ifcEnumTypeString, element, geometryElement, productWrapper);
                    return true;
                case IFCExportType.IfcSlab:
                    FloorExporter.ExportGenericSlab(exporterIFC, element, geometryElement, ifcEnumTypeString, productWrapper);
                    return true;
                case IFCExportType.IfcStair:
                    StairsExporter.ExportStairAsSingleGeometry(exporterIFC, ifcEnumTypeString, element, geometryElement, 1, productWrapper);
                    return true;
                case IFCExportType.IfcWall:
                    WallExporter.ExportWall(exporterIFC, element, null, geometryElement, productWrapper);
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
            if (ParameterUtil.GetStringValueFromElementOrSymbol(element, "IfcType", out value) == null)
                value = columnType;

            if (String.IsNullOrEmpty(value))
                return IFCColumnType.Column;

            string newValue = NamingUtil.RemoveSpacesAndUnderscores(value);

            if (String.Compare(newValue, "USERDEFINED", true) == 0)
                return IFCColumnType.UserDefined;

            return IFCColumnType.Column;
        }
    }
}
