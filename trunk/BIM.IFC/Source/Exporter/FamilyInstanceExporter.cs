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
using System.Linq;

using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using BIM.IFC.Utility;

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
        /// The IFCProductWrapper.
        /// </param>
        public static void ExportFamilyInstanceElement(ExporterIFC exporterIFC,
           FamilyInstance familyInstance, GeometryElement geometryElement, IFCProductWrapper productWrapper)
        {
            // Don't export family if it is invisible.
            if (familyInstance.Invisible)
                return;

            // Don't export mullions and panels if they have a host and their host is not a mass
            // as they will be exported with the host (curtain wall/roof).
            if (familyInstance.Category.Id == new ElementId(BuiltInCategory.OST_CurtainWallMullions) ||
                familyInstance.Category.Id == new ElementId(BuiltInCategory.OST_CurtainWallPanels))
            {
                Element host = familyInstance.Host;
                if (host != null && host.Category.Id != new ElementId(BuiltInCategory.OST_Mass))
                    return;
            }
		 
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

                if (ExportFamilyInstanceAsStandardElement(exportType))
                {
                    using (IFCProductWrapper wrapper = IFCProductWrapper.Create(exporterIFC, true))
                    {
                        ExporterIFCUtils.ExportElementInternal(exporterIFC, familyInstance, wrapper);
                        tr.Commit();
                        return;
                    }
                }

                // If we are exporting a column, we may need to split it into parts by level.  Create a list of ranges.
                IList<ElementId> levels = new List<ElementId>();
                IList<UV> ranges = new List<UV>();
                if ((exportType == IFCExportType.ExportColumnType) && (exporterIFC.WallAndColumnSplitting))
                {
                    LevelUtil.CreateSplitLevelRangesForElement(exporterIFC, exportType, familyInstance, out levels, out ranges);
                }

                int numPartsToExport = ranges.Count;
                if (numPartsToExport == 0)
                {
                    ExportFamilyInstanceAsMappedItem(exporterIFC, familyInstance, exportType, ifcEnumType, productWrapper,
                       ElementId.InvalidElementId, null);
                }
                else
                {
                    for (int ii = 0; ii < numPartsToExport; ii++)
                    {
                        ExportFamilyInstanceAsMappedItem(exporterIFC, familyInstance, exportType, ifcEnumType, productWrapper,
                          levels[ii], ranges[ii]);
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
        /// The IFCProductWrapper.
        /// </param>
        /// <param name="overrideLevelId">
        /// The level id.
        /// </param>
        /// <param name="range">
        /// The range of this family instance to be exported.
        /// </param>
        public static void ExportFamilyInstanceAsMappedItem(ExporterIFC exporterIFC,
           FamilyInstance familyInstance, IFCExportType exportType, string ifcEnumType,
           IFCProductWrapper wrapper, ElementId overrideLevelId, UV range)
        {
            Document doc = familyInstance.Document;
            IFCFile file = exporterIFC.GetFile();

            FamilySymbol familySymbol = ExporterIFCUtils.GetOriginalSymbol(familyInstance);
            if (familySymbol == null)
                return;

            IFCProductWrapper familyProductWrapper = IFCProductWrapper.Create(wrapper);
            double scale = exporterIFC.LinearScale;

            IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();

            HostObject hostElement = familyInstance.Host as HostObject; //hostElement could be null
            ElementId categoryId = CategoryUtil.GetSafeCategoryId(familySymbol);

            //string emptyString = "";
            string familyName = familySymbol.Name;
            IFCLabel objectType = IFCLabel.Create(familyName);

            // A Family Instance can have its own copy of geometry, or use the symbol's copy with a transform.
            // The routine below tells us whether to use the Instance's copy or the Symbol's copy.
            bool useInstanceGeometry = ExporterIFCUtils.UsesInstanceGeometry(familyInstance);

            IList<IFCExtrusionData> cutPairOpeningsForColumns = new List<IFCExtrusionData>();
            IFCExtrusionCreationData extraParams = new IFCExtrusionCreationData();

            Transform trf = familyInstance.GetTransform();

            // Extra information if we are exporting a door or a window.
            IFCDoorWindowInfo doorWindowInfo = null;
            if (exportType == IFCExportType.ExportDoorType)
                doorWindowInfo = IFCDoorWindowInfo.CreateDoorInfo(exporterIFC, familyInstance, familySymbol, hostElement, overrideLevelId, trf);
            else if (exportType == IFCExportType.ExportWindowType)
                doorWindowInfo = IFCDoorWindowInfo.CreateWindowInfo(exporterIFC, familyInstance, familySymbol, hostElement, overrideLevelId, trf);

            IFCTypeInfo typeInfo = new IFCTypeInfo();
            XYZ extraOffset = XYZ.Zero;

            bool flipped = doorWindowInfo != null ? doorWindowInfo.IsSymbolFlipped : false;
            IFCTypeInfo currentTypeInfo = exporterIFC.FindType(familySymbol.Id, flipped);
            bool found = currentTypeInfo.IsValid();

            Family family = familySymbol.Family;

            // TODO: this code to be removed by ExtrusionAnalyzer code.
            bool trySpecialColumnCreation = ((exportType == IFCExportType.ExportColumnType) && (!family.IsInPlace));

            // We will create a new mapped type if:
            // 1.  We are exporting part of a column or in-place wall (range != null), OR
            // 2.  We are using the instance's copy of the geometry (that it, it has unique geometry), OR
            // 3.  We haven't already created the type.
            bool creatingType = ((range != null) || useInstanceGeometry || !found);
            if (creatingType)
            {
                IFCAnyHandle bodyRepresentation = IFCAnyHandle.Create();
                IFCAnyHandle planRepresentation = IFCAnyHandle.Create();

                // If we are using the instance geometry, ignore the transformation.
                if (useInstanceGeometry)
                    trf = Transform.Identity;

                // TODO: this code to be removed by ExtrusionAnalyzer code.
                if (trySpecialColumnCreation)
                {
                    XYZ rangeOffset = trf.Origin;
                    IFCFamilyInstanceExtrusionExportResults results;
                    if (range != null)
                    {
                        results = ExporterIFCUtils.ExportFamilyInstanceAsExtrusion(exporterIFC, familyInstance, useInstanceGeometry, range, overrideLevelId, extraParams);
                    }
                    else
                    {
                        results = ExporterIFCUtils.ExportFamilyInstanceAsExtrusion(exporterIFC, familyInstance, useInstanceGeometry, overrideLevelId, extraParams);
                    }
                    bodyRepresentation = results.GetExtrusionHandle();
                    extraOffset = results.ExtraOffset;
                    cutPairOpeningsForColumns = results.GetCutPairOpenings();

                    if (bodyRepresentation.HasValue)
                    {
                        typeInfo.MaterialId = results.MaterialId;
                        // add in level for real columns, not in-place ones.
                        Element actualLevel =
                           (overrideLevelId == ElementId.InvalidElementId) ? familyInstance.Level : doc.get_Element(overrideLevelId);
                        if (actualLevel != null)
                        {
                            IFCLevelInfo levelInfo = exporterIFC.GetLevelInfo(actualLevel.Id);
                            double nonStoryLevelOffset = LevelUtil.GetNonStoryLevelOffsetIfAny(exporterIFC, actualLevel as Level);
                            if (range != null)
                            {
                                rangeOffset = new XYZ(rangeOffset.X, rangeOffset.Y, levelInfo.Elevation + nonStoryLevelOffset);
                            }
                        }
                    }

                    rangeOffset += extraOffset;
                    trf.Origin = rangeOffset;
                }

                Transform doorWindowTrf = Transform.Identity;
                IFCAnyHandle dummyPlacement = IFCAnyHandle.Create();
                if (doorWindowInfo != null)
                {
                    doorWindowTrf = ExporterIFCUtils.GetTransformForDoorOrWindow(familyInstance, familySymbol, doorWindowInfo);
                }
                else
                {
                    dummyPlacement = file.CreateLocalPlacement(IFCAnyHandle.Create(), file.CreateAxis2Placement3D());
                    extraParams.SetLocalPlacement(dummyPlacement);
                }

                bool needToCreate2d = (!exporterIFC.ExportAs2x2);
                bool needToCreate3d = (!bodyRepresentation.HasValue);

                if (needToCreate2d || needToCreate3d)
                {
                    using (IFCTransformSetter trfSetter = IFCTransformSetter.Create())
                    {
                        if (doorWindowInfo != null)
                        {
                            trfSetter.Initialize(exporterIFC, doorWindowTrf);
                        }

                        Options options = new Options();
                        GeometryElement exportGeometry =
                           useInstanceGeometry ? familyInstance.get_Geometry(options) : familySymbol.get_Geometry(options);
                        if (exportGeometry == null)
                            return;

                        if (needToCreate3d)
                        {
                            IFCSolidMeshGeometryInfo solidMeshInfo;

                            if (range == null)
                                solidMeshInfo = ExporterIFCUtils.GetSolidMeshGeometry(exporterIFC, exportGeometry, Transform.Identity);
                            else
                                solidMeshInfo = ExporterIFCUtils.GetClippedSolidMeshGeometry(exporterIFC, range, exportGeometry);

                            IList<Solid> solids = solidMeshInfo.GetSolids();
                            IList<Mesh> polyMeshes = solidMeshInfo.GetMeshes();

                            bool tryToExportAsExtrusion = (!exporterIFC.ExportAs2x2 || (exportType == IFCExportType.ExportColumnType));

                            if (exportType == IFCExportType.ExportColumnType)
                            {
                                extraParams.PossibleExtrusionAxes = IFCExtrusionAxes.TryZ;
                            }
                            else
                            {
                                extraParams.PossibleExtrusionAxes = IFCExtrusionAxes.TryXYZ;
                            }

                            if (solids.Count > 0 || polyMeshes.Count > 0)
                            {
                                bodyRepresentation = BodyExporter.ExportBody(familyInstance.Document.Application, exporterIFC, categoryId, solids, polyMeshes,
                                    tryToExportAsExtrusion, extraParams);
                                typeInfo.MaterialId = BodyExporter.GetBestMaterialIdForGeometry(solids, polyMeshes);
                            }
                            else
                            {
                                IList<GeometryObject> exportedGeometries = new List<GeometryObject>();
                                exportedGeometries.Add(exportGeometry);
                                bodyRepresentation = BodyExporter.ExportBody(familyInstance.Document.Application, exporterIFC, categoryId, exportedGeometries,
                                   tryToExportAsExtrusion, extraParams);
                            }

                            if (!bodyRepresentation.HasValue)
                            {
                                extraParams.ClearOpenings();
                                return;
                            }
                        }

                        // if exporting IFC2x3 (or later), export 2D plan rep of family (if it exists).
                        if (needToCreate2d)
                        {
                            HashSet<IFCAnyHandle> curveSet = new HashSet<IFCAnyHandle>();
                            {
                                Transform planeTrf = doorWindowTrf.Inverse;
                                Plane plane = new Plane(planeTrf.get_Basis(0), planeTrf.get_Basis(1), planeTrf.Origin);
                                XYZ projDir = new XYZ(0, 0, 1);

                                IFCGeometryInfo IFCGeometryInfo = IFCGeometryInfo.CreateCurveGeometryInfo(exporterIFC, plane, projDir, true);
                                ExporterIFCUtils.CollectGeometryInfo(exporterIFC, IFCGeometryInfo, exportGeometry, planeTrf.Origin, false);

                                IList<IFCAnyHandle> curves = IFCGeometryInfo.GetCurves();
                                foreach (IFCAnyHandle curve in curves)
                                    curveSet.Add(curve);

                                if (curveSet.Count > 0)
                                {
                                    IFCAnyHandle contextOfItems2d = exporterIFC.Get2DContextHandle();
                                    IFCAnyHandle curveRepresentationItem = file.CreateGeometricSet(curveSet);
                                    HashSet<IFCAnyHandle> bodyItems = new HashSet<IFCAnyHandle>();
                                    bodyItems.Add(curveRepresentationItem);
                                    planRepresentation = RepresentationUtil.CreateGeometricSetRep(exporterIFC, categoryId, "Annotation",
                                       contextOfItems2d, bodyItems);
                                }
                            }
                        }
                    }
                }

                if (doorWindowInfo != null)
                {
                    typeInfo.SetStyleTransform(doorWindowTrf.Inverse);
                }
                else
                {
                    if (!MathUtil.IsAlmostZero(extraOffset.DotProduct(extraOffset)))
                    {
                        Transform newTransform = typeInfo.GetStyleTransform();
                        XYZ newOrig = newTransform.Origin + extraOffset;
                        newTransform.Origin = newOrig;
                        typeInfo.SetStyleTransform(newTransform);
                    }
                    typeInfo.SetStyleTransform(ExporterIFCUtils.GetUnscaledTransform(exporterIFC, extraParams.GetLocalPlacement()));
                }

                IFCLabel descriptionOpt = IFCLabel.Create();
                IFCLabel applicableOccurrenceOpt = IFCLabel.Create();

                IFCAnyHandle origin = file.CreateAxis2Placement3D();
                IFCAnyHandle repMap2dHnd = IFCAnyHandle.Create();
                IFCAnyHandle repMap3dHnd = file.CreateRepresentationMap(origin, bodyRepresentation);

                IList<IFCAnyHandle> repMapList = new List<IFCAnyHandle>();
                repMapList.Add(repMap3dHnd);
                if (planRepresentation.HasValue)
                {
                    repMap2dHnd = file.CreateRepresentationMap(origin, planRepresentation);
                    repMapList.Add(repMap2dHnd);
                }

                // for Door, Window
                bool paramTakesPrecedence = false; // For Revit, this is currently always false.
                bool sizeable = false;

                // for many
                HashSet<IFCAnyHandle> propertySets = new HashSet<IFCAnyHandle>();

                IFCLabel guid = IFCLabel.CreateGUID(familySymbol);
                IFCLabel symIdAsLabel = NamingUtil.CreateIFCElementId(familySymbol);

                // This covers many generic types.  If we can't find it in the list here, do custom exports.
                IFCAnyHandle typeStyle = FamilyExporterUtil.ExportGenericType(file, exportType, ifcEnumType, guid,
                   ownerHistory, objectType, descriptionOpt, applicableOccurrenceOpt, propertySets, repMapList, symIdAsLabel, objectType,
                   familyInstance, familySymbol);

                // Cover special cases not covered above.
                if (!typeStyle.HasValue)
                {
                    switch (exportType)
                    {
                        case IFCExportType.ExportColumnType:
                            {
                                // If we are using the instance GRep, then we have to create a generic GUID for the
                                // column type, as they share the same ElementId.
                                IFCLabel colGUID = IFCLabel.Create();
                                IFCLabel colElemId = IFCLabel.Create();

                                if (useInstanceGeometry)
                                {
                                    colGUID = IFCLabel.CreateGUID();
                                    colElemId = NamingUtil.CreateIFCElementId(familyInstance);
                                }
                                else
                                {
                                    colGUID = guid;
                                    colElemId = NamingUtil.CreateIFCElementId(familySymbol);
                                }

                                string columnType = "Column";
                                typeStyle = file.CreateColumnType(columnType, colGUID, ownerHistory, objectType,
                                   descriptionOpt, applicableOccurrenceOpt, propertySets, repMapList, colElemId,
                                   objectType, familyInstance, familySymbol);

                                break;
                            }
                        case IFCExportType.ExportDoorType:
                            {
                                string constructionType = string.Empty;
                                ParameterUtil.GetStringValueFromElementOrSymbol(familyInstance, "Construction", out constructionType);

                                IFCAnyHandle doorLining = DoorWindowUtil.CreateDoorLiningProperties(exporterIFC, familyInstance);
                                if (doorLining.HasValue)
                                    propertySets.Add(doorLining);

                                IList<IFCAnyHandle> doorPanels = DoorWindowUtil.CreateDoorPanelProperties(exporterIFC, doorWindowInfo,
                                   familyInstance);
                                propertySets.UnionWith(doorPanels);

                                IFCLabel doorStyleGUID = IFCLabel.CreateGUID();
                                IFCLabel doorStyleElemId = NamingUtil.CreateIFCElementId(familyInstance);
                                typeStyle = file.CreateDoorStyle(doorStyleGUID, ownerHistory, objectType,
                                   descriptionOpt, applicableOccurrenceOpt, propertySets, repMapList, doorStyleElemId,
                                   doorWindowInfo.DoorOperationType, constructionType, paramTakesPrecedence, sizeable);
                                break;
                            }
                        case IFCExportType.ExportSystemFurnitureElementType:
                            {
                                IFCLabel furnitureId = NamingUtil.CreateIFCElementId(familyInstance);
                                typeStyle = file.CreateSystemFurnitureElementType(guid, ownerHistory, objectType,
                                   descriptionOpt, applicableOccurrenceOpt, propertySets, repMapList, furnitureId,
                                   objectType);
                                break;
                            }
                        case IFCExportType.ExportWindowType:
                            {
                                IFCWindowStyleOperation operationType = DoorWindowUtil.GetIFCWindowStyleOperation(familySymbol);
                                string constructionType = DoorWindowUtil.GetIFCWindowStyleConstruction(familyInstance, "");

                                IFCAnyHandle windowLining = DoorWindowUtil.CreateWindowLiningProperties(exporterIFC, familyInstance, descriptionOpt);
                                if (windowLining.HasValue)
                                    propertySets.Add(windowLining);

                                IList<IFCAnyHandle> windowPanels =
                                   DoorWindowUtil.CreateWindowPanelProperties(exporterIFC, familyInstance, descriptionOpt);
                                propertySets.UnionWith(windowPanels);

                                IFCLabel windowStyleGUID = IFCLabel.CreateGUID();
                                IFCLabel windowStyleElemId = NamingUtil.CreateIFCElementId(familyInstance);
                                typeStyle = file.CreateWindowStyle(windowStyleGUID, ownerHistory, objectType,
                                   descriptionOpt, applicableOccurrenceOpt, propertySets, repMapList, windowStyleElemId,
                                   operationType, constructionType, paramTakesPrecedence, sizeable);
                                break;
                            }
                        case IFCExportType.ExportBuildingElementProxy:
                        default:
                            {
                                typeInfo.Set2DMapHandle(repMap2dHnd);
                                typeInfo.Set3DMapHandle(repMap3dHnd);
                                break;
                            }
                    }
                }

                typeInfo.SetStyle(typeStyle);

                // Transfer extraParams information for certain types.
                if (typeStyle.HasValue)
                {
                    if (((exportType == IFCExportType.ExportColumnType) && trySpecialColumnCreation) ||
                       (exportType == IFCExportType.ExportMemberType))
                    {
                        typeInfo.ScaledArea = extraParams.ScaledArea;
                        typeInfo.ScaledDepth = extraParams.ScaledLength;
                        typeInfo.ScaledInnerPerimeter = extraParams.ScaledInnerPerimeter;
                        typeInfo.ScaledOuterPerimeter = extraParams.ScaledOuterPerimeter;
                    }
                }
            }
            else if (!creatingType && (trySpecialColumnCreation))
            {
                // still need to modify instance trf for columns.
                trf.Origin += GetLevelOffsetForExtrudedColumns(exporterIFC, familyInstance, overrideLevelId, extraParams);
            }

            if (found && !typeInfo.GetStyle().HasValue)
            {
                typeInfo = currentTypeInfo;
            }

            // we'll pretend we succeeded, but we'll do nothing.
            if (!typeInfo.GetStyle().HasValue && !typeInfo.Get2DMapHandle().HasValue && !typeInfo.Get3DMapHandle().HasValue)
                return;

            // add to the map, as long as we are not using range, not using instance geometry, and don't have extra openings.
            if ((range == null) && !useInstanceGeometry && (extraParams.GetOpenings().Count == 0))
                exporterIFC.AddType(familySymbol.Id, flipped, typeInfo);

            Transform oldTrf = new Transform(trf);
            XYZ scaledMapOrigin = XYZ.Zero;

            trf = trf.Multiply(typeInfo.GetStyleTransform());

            // create instance.  
            IList<IFCAnyHandle> shapeReps = new List<IFCAnyHandle>();
            {
                IFCAnyHandle contextOfItems2d = exporterIFC.Get2DContextHandle();
                IFCAnyHandle contextOfItems3d = exporterIFC.Get3DContextHandle();

                // for proxies, we store the IfcRepresentationMap directly since there is no style.
                IList<IFCAnyHandle> repMapList = IFCGeometryUtils.GetRepresentationMaps(typeInfo.GetStyle());
                int numReps = repMapList.Count;

                IFCAnyHandle repMap2dHnd = typeInfo.Get2DMapHandle();
                IFCAnyHandle repMap3dHnd = typeInfo.Get3DMapHandle();
                if (!repMap3dHnd.HasValue && (numReps > 0))
                    repMap3dHnd = repMapList[0];
                if (!repMap2dHnd.HasValue && (numReps > 1))
                    repMap2dHnd = repMapList[1];

                if (repMap3dHnd.HasValue)
                {
                    HashSet<IFCAnyHandle> representations = new HashSet<IFCAnyHandle>();
                    representations.Add(ExporterUtil.CreateDefaultMappedItem(file, repMap3dHnd, scaledMapOrigin));
                    IFCAnyHandle shapeRep = RepresentationUtil.CreateBodyMappedItemRep(exporterIFC, categoryId, contextOfItems3d, representations);
                    if (!shapeRep.HasValue)
                        return;
                    shapeReps.Add(shapeRep);
                }

                if (repMap2dHnd.HasValue)
                {
                    HashSet<IFCAnyHandle> representations = new HashSet<IFCAnyHandle>();
                    representations.Add(ExporterUtil.CreateDefaultMappedItem(file, repMap2dHnd, scaledMapOrigin));
                    IFCAnyHandle shapeRep = RepresentationUtil.CreatePlanMappedItemRep(exporterIFC, categoryId, contextOfItems2d, representations);
                    if (!shapeRep.HasValue)
                        return;
                    shapeReps.Add(shapeRep);
                }
            }

            IFCLabel noDescriptionOpt = IFCLabel.Create();
            IFCLabel noNameOpt = IFCLabel.Create();
            IFCAnyHandle rep = file.CreateProductDefinitionShape(noNameOpt, noDescriptionOpt, shapeReps);

            IFCAnyHandle instanceHandle = IFCAnyHandle.Create();
            using (IFCPlacementSetter setter = IFCPlacementSetter.Create(exporterIFC, familyInstance, trf, null, overrideLevelId))
            {
                IFCLabel instanceGUID = IFCLabel.CreateGUID(familyInstance);
                IFCLabel origInstanceName = NamingUtil.CreateIFCName(exporterIFC, -1);
                IFCLabel instanceName = NamingUtil.GetNameOverride(familyInstance, origInstanceName);
                IFCLabel instanceDescription = NamingUtil.GetDescriptionOverride(familyInstance, noDescriptionOpt);
                IFCLabel instanceObjectType = NamingUtil.GetObjectTypeOverride(familyInstance, objectType);
                IFCLabel instanceElemId = NamingUtil.CreateIFCElementId(familyInstance);

                IFCAnyHandle localPlacement = setter.GetPlacement();
                instanceHandle = FamilyExporterUtil.ExportGenericInstance(exportType, exporterIFC, familyInstance,
                   wrapper, setter, extraParams, instanceGUID, ownerHistory, instanceName, instanceDescription, instanceObjectType,
                   rep, instanceElemId);

                switch (exportType)
                {
                    case IFCExportType.ExportColumnType:
                        {
                            IFCAnyHandle placementToUse = localPlacement;
                            if (!useInstanceGeometry)
                            {
                                Transform openingTrf = new Transform(oldTrf);
                                Transform extraRot = new Transform(oldTrf);
                                extraRot.Origin = XYZ.Zero;
                                openingTrf = openingTrf.Multiply(extraRot);
                                openingTrf = openingTrf.Multiply(typeInfo.GetStyleTransform());

                                IFCAnyHandle openingRelativePlacement = file.CreateAxis2Placement3D(openingTrf.Origin * scale,
                                   openingTrf.get_Basis(2), openingTrf.get_Basis(0));
                                IFCAnyHandle openingPlacement = ExporterUtil.CopyLocalPlacement(file, localPlacement);
                                IFCGeometryUtils.SetRelativePlacement(openingPlacement, openingRelativePlacement);
                                placementToUse = openingPlacement;
                            }

                            OpeningUtil.CreateOpeningsIfNecessary(instanceHandle, familyInstance, cutPairOpeningsForColumns,
                               exporterIFC, placementToUse, setter, wrapper);
                            OpeningUtil.CreateOpeningsIfNecessary(instanceHandle, familyInstance, extraParams, exporterIFC,
                               placementToUse, setter, wrapper);

                            //export Base Quantities.
                            ExporterIFCUtils.CreateBeamColumnBaseQuantities(exporterIFC, instanceHandle, familyInstance, typeInfo);

                            CategoryUtil.CreateMaterialAssociation(doc, exporterIFC, instanceHandle, typeInfo.MaterialId);

                            ExporterIFCUtils.CreateColumnPropertySet(exporterIFC, familyInstance, extraParams, wrapper);
                            break;
                        }
                    case IFCExportType.ExportDoorType:
                    case IFCExportType.ExportWindowType:
                        {
                            double doorHeight = doorWindowInfo.OpeningHeight;
                            if (doorHeight < MathUtil.Eps())
                                doorHeight = GetMinSymbolHeight(familySymbol);
                            double doorWidth = doorWindowInfo.OpeningWidth;
                            if (doorWidth < MathUtil.Eps())
                                doorWidth = GetMinSymbolWidth(familySymbol);

                            IFCMeasureValue height = IFCMeasureValue.Create(doorHeight * scale);
                            IFCMeasureValue width = IFCMeasureValue.Create(doorWidth * scale);

                            if (!doorWindowInfo.GetLocalPlacement().HasValue)
                                doorWindowInfo.SetLocalPlacement(localPlacement);

                            IFCAnyHandle doorWindowOrigLocalPlacement = doorWindowInfo.GetLocalPlacement();
                            Transform relTrf = ExporterIFCUtils.GetRelativeLocalPlacementOffsetTransform(localPlacement, doorWindowOrigLocalPlacement);

                            IFCAnyHandle doorWindowRelativePlacement = file.CreateAxis2Placement3D(relTrf);
                            IFCAnyHandle doorWindowLocalPlacement =
                               file.CreateLocalPlacement(doorWindowOrigLocalPlacement, doorWindowRelativePlacement);
                            if (exportType == IFCExportType.ExportDoorType)
                                instanceHandle = file.CreateDoor(instanceGUID, ownerHistory,
                                   instanceName, instanceDescription, instanceObjectType, doorWindowLocalPlacement,
                                   rep, instanceElemId, height, width);
                            else if (exportType == IFCExportType.ExportWindowType)
                                instanceHandle = file.CreateWindow(instanceGUID, ownerHistory,
                                   instanceName, instanceDescription, instanceObjectType, doorWindowLocalPlacement,
                                   rep, instanceElemId, height, width);
                            wrapper.AddElement(instanceHandle, setter, extraParams, true);

                            exporterIFC.RegisterSpaceBoundingElementHandle(instanceHandle, familyInstance.Id, setter.LevelId);

                            // only necessary when exporting as possible breps.
                            OpeningUtil.CreateOpeningsIfNecessary(instanceHandle, familyInstance, extraParams, exporterIFC,
                               doorWindowLocalPlacement, setter, wrapper);
                            if (exporterIFC.ExportBaseQuantities)
                                ExporterIFCUtils.CreateDoorWindowBaseQuantities(exporterIFC, instanceHandle, (doorHeight * scale), (doorWidth * scale));

                            if (exportType == IFCExportType.ExportDoorType)
                                ExporterIFCUtils.CreateDoorPropertySet(exporterIFC, familyInstance, wrapper);
                            else
                                ExporterIFCUtils.CreateWindowPropertySet(exporterIFC, familyInstance, wrapper);
                            break;
                        }
                    case IFCExportType.ExportMemberType:
                        {
                            OpeningUtil.CreateOpeningsIfNecessary(instanceHandle, familyInstance, extraParams, exporterIFC,
                              localPlacement, setter, wrapper);
                            CategoryUtil.CreateMaterialAssociation(doc, exporterIFC, instanceHandle, typeInfo.MaterialId);

                            //export Base Quantities.
                            ExporterIFCUtils.CreateBeamColumnBaseQuantities(exporterIFC, instanceHandle, familyInstance, typeInfo);
                            // TODO: create PropertySet!
                            //createMemberPropertySet(exporter, pFamInst, pWrapper, extraParams);
                            ExporterIFCUtils.CreateGenericElementPropertySet(exporterIFC, familyInstance, wrapper);
                            break;
                        }
                    case IFCExportType.ExportPlateType:
                        {
                            OpeningUtil.CreateOpeningsIfNecessary(instanceHandle, familyInstance, extraParams, exporterIFC,
                              localPlacement, setter, wrapper);
                            CategoryUtil.CreateMaterialAssociation(doc, exporterIFC, instanceHandle, typeInfo.MaterialId);

                            // TODO: create PropertySet!
                            //createPlatePropertySet(exporter, pFamInst, pWrapper, extraParams);
                            ExporterIFCUtils.CreateGenericElementPropertySet(exporterIFC, familyInstance, wrapper);
                            break;
                        }
                    case IFCExportType.ExportTransportElementType:
                        {
                            string operationTypeOpt = "";
                            IFCMeasureValue capByWeightOpt = IFCMeasureValue.Create();
                            IFCMeasureValue capByNumOpt = IFCMeasureValue.Create();

                            IFCAnyHandle localPlacementToUse;
                            ElementId roomId = setter.UpdateRoomRelativeCoordinates(familyInstance, out localPlacementToUse);

                            instanceHandle = file.CreateTransportElement(instanceGUID, ownerHistory,
                               instanceName, instanceDescription, instanceObjectType,
                               localPlacementToUse, rep, instanceElemId, operationTypeOpt, capByWeightOpt, capByNumOpt,
                               familyInstance, familySymbol);

                            if (roomId == ElementId.InvalidElementId)
                            {
                                wrapper.AddElement(instanceHandle, setter, extraParams, true);
                            }
                            else
                            {
                                exporterIFC.RelateSpatialElement(roomId, instanceHandle);
                                wrapper.AddElement(instanceHandle, setter, extraParams, false);
                            }

                            ExporterIFCUtils.CreateGenericElementPropertySet(exporterIFC, familyInstance, wrapper);
                            break;
                        }
                    case IFCExportType.ExportBuildingElementProxy:
                    default:
                        {
                            bool isBuildingElementProxy = (exportType == IFCExportType.ExportBuildingElementProxy);

                            if (!isBuildingElementProxy)
                            {
                                if (FamilyExporterUtil.IsDistributionControlElementSubType(exportType))
                                {
                                    IFCLabel controlElementId = IFCLabel.Create();
                                    IFCAnyHandle localPlacementToUse;
                                    ElementId roomId = setter.UpdateRoomRelativeCoordinates(familyInstance, out localPlacementToUse);

                                    instanceHandle = file.CreateDistributionControlElement(instanceGUID,
                                       ownerHistory, instanceName, instanceDescription, instanceObjectType,
                                       localPlacement, rep, instanceElemId, controlElementId);

                                    if (roomId == ElementId.InvalidElementId)
                                    {
                                        wrapper.AddElement(instanceHandle, setter, extraParams, true);
                                    }
                                    else
                                    {
                                        exporterIFC.RelateSpatialElement(roomId, instanceHandle);
                                        wrapper.AddElement(instanceHandle, setter, extraParams, false);
                                    }

                                    OpeningUtil.CreateOpeningsIfNecessary(instanceHandle, familyInstance, extraParams, exporterIFC,
                                       localPlacement, setter, wrapper);
                                }
                                else if (!instanceHandle.HasValue)
                                {
                                    isBuildingElementProxy = true;
                                }
                            }

                            if (isBuildingElementProxy)
                            {
                                IFCElementComposition proxyType = IFCElementComposition.Element;
                                IFCAnyHandle localPlacementToUse;
                                ElementId roomId = setter.UpdateRoomRelativeCoordinates(familyInstance, out localPlacementToUse);

                                instanceHandle = file.CreateBuildingElementProxy(instanceGUID,
                                   ownerHistory, instanceName, instanceDescription, instanceObjectType,
                                   localPlacementToUse, rep, instanceElemId, proxyType);

                                if (roomId == ElementId.InvalidElementId)
                                {
                                    wrapper.AddElement(instanceHandle, setter, extraParams, true);
                                }
                                else
                                {
                                    exporterIFC.RelateSpatialElement(roomId, instanceHandle);
                                    wrapper.AddElement(instanceHandle, setter, extraParams, false);
                                }

                                OpeningUtil.CreateOpeningsIfNecessary(instanceHandle, familyInstance, extraParams, exporterIFC,
                                   localPlacement, setter, wrapper);
                            }

                            ExporterIFCUtils.CreateGenericElementPropertySet(exporterIFC, familyInstance, wrapper);
                            break;
                        }
                }

                if (instanceHandle.HasValue)
                {
                    if (doorWindowInfo != null)
                    {
                        if (doorWindowInfo.GetOpening().HasValue)
                        {
                            IFCLabel relGUID = IFCLabel.CreateGUID();
                            file.CreateRelFillsElement(relGUID, ownerHistory, IFCLabel.Create(), IFCLabel.Create(), doorWindowInfo.GetOpening(), instanceHandle);
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

                    if (typeInfo.GetStyle().HasValue)
                        exporterIFC.AddTypeRelation(typeInfo.GetStyle(), instanceHandle);
                }
            }
        }


        /// <summary>
        /// Exports a family instance as standard element.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="element">
        /// The element to be exported.
        /// </param>
        /// <param name="geometryElement">
        /// The geometry element.
        /// </param>
        /// <param name="familyType">
        /// The export type.
        /// </param>
        /// <param name="ifcEnumTypeString">
        /// The string value represents the IFC type.
        /// </param>
        /// <param name="productWrapper">
        /// The IFCProductWrapper.
        /// </param>
        public static bool ExportFamilyInstanceAsStandardElement(IFCExportType familyType)
        {
            switch (familyType)
            {
                // These entities don't get exported as a mapped instance.  As such, we export them using
                // the standard methods.

                // standard building elements
                case IFCExportType.ExportBeam:
                case IFCExportType.ExportCovering:
                case IFCExportType.ExportFooting:
                case IFCExportType.ExportRailing:
                case IFCExportType.ExportRamp:
                case IFCExportType.ExportRoof:
                case IFCExportType.ExportSlab:
                case IFCExportType.ExportStair:
                case IFCExportType.ExportWall:
                // distribution elements
                case IFCExportType.ExportDistributionElement:
                case IFCExportType.ExportDistributionControlElement:
                case IFCExportType.ExportDistributionFlowElement:
                case IFCExportType.ExportEnergyConversionDevice:
                case IFCExportType.ExportFlowFitting:
                case IFCExportType.ExportFlowMovingDevice:
                case IFCExportType.ExportFlowSegment:
                case IFCExportType.ExportFlowStorageDevice:
                case IFCExportType.ExportFlowTerminal:
                case IFCExportType.ExportFlowTreatmentDevice:
                case IFCExportType.ExportFlowController:
                // furniture elements
                case IFCExportType.ExportFurnishingElement:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Gets level offset for extruded columns.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="familyInstance">
        /// The family instance.
        /// </param>
        /// <param name="overrideLevelId">
        /// The level id.
        /// </param>
        /// <param name="extraParams">
        /// The extrusion creation data.
        /// </param>
        static XYZ GetLevelOffsetForExtrudedColumns(ExporterIFC exporterIFC,
           FamilyInstance familyInstance, ElementId overrideLevelId, IFCExtrusionCreationData extraParams)
        {
            IFCFamilyInstanceExtrusionExportResults results = ExporterIFCUtils.ExportFamilyInstanceAsExtrusion(exporterIFC, familyInstance, false, overrideLevelId, extraParams);
            IFCAnyHandle extrusionHandle = results.GetExtrusionHandle();
            XYZ levelOffset = (extrusionHandle.HasValue) ? results.ExtraOffset : XYZ.Zero;

            extrusionHandle.Delete();

            return levelOffset;
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
    }
}
