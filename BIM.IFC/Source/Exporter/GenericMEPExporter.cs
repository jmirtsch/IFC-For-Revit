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
    /// Provides methods to export generic MEP family instances.
    /// </summary>
    class GenericMEPExporter
    {
        /// <summary>
        /// Exports a MEP family instance.
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
        public static void Export(ExporterIFC exporterIFC, Element element, GeometryElement geometryElement, IFCProductWrapper productWrapper)
        {
            IFCFile file = exporterIFC.GetFile();
            using (IFCTransaction tr = new IFCTransaction(file))
            {
                string ifcEnumType;
                IFCExportType exportType = ExporterUtil.GetExportType(exporterIFC, element, out ifcEnumType);

                using (IFCPlacementSetter setter = IFCPlacementSetter.Create(exporterIFC, element))
                {
                    IFCAnyHandle localPlacementToUse = setter.GetPlacement();
                    IFCExtrusionCreationData extraParams = new IFCExtrusionCreationData();

                    ElementId catId = CategoryUtil.GetSafeCategoryId(element);


                    IFCSolidMeshGeometryInfo solidMeshInfo = ExporterIFCUtils.GetSolidMeshGeometry(exporterIFC, geometryElement, Transform.Identity);
                    IList<Solid> solids = solidMeshInfo.GetSolids();
                    IList<Mesh> polyMeshes = solidMeshInfo.GetMeshes();

                    bool tryToExportAsExtrusion = true;
                    if (solids.Count != 1 || polyMeshes.Count != 0)
                        tryToExportAsExtrusion = false;

                    IFCAnyHandle shapeRep = BodyExporter.ExportBody(element.Document.Application, exporterIFC, catId, solids, polyMeshes, tryToExportAsExtrusion, extraParams);
                    if (!shapeRep.HasValue)
                        return;

                    IList<IFCAnyHandle> shapeReps = new List<IFCAnyHandle>();
                    shapeReps.Add(shapeRep);
                    IFCAnyHandle productRepresentation = file.CreateProductDefinitionShape(IFCLabel.Create(), IFCLabel.Create(), shapeReps);

                    IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();
                    ElementId typeId = element.GetTypeId();
                    ElementType type = element.Document.get_Element(typeId) as ElementType;
                    IFCTypeInfo currentTypeInfo = exporterIFC.FindType(typeId, false);

                    bool found = currentTypeInfo.IsValid();
                    if (!found)
                    {
                        IFCLabel typeGUID = IFCLabel.CreateGUID(type);
                        IFCLabel origTypeName = NamingUtil.CreateIFCName(exporterIFC, -1);
                        IFCLabel typeName = NamingUtil.GetNameOverride(type, origTypeName);
                        IFCLabel typeObjectType = NamingUtil.CreateIFCObjectName(exporterIFC, type);
                        IFCLabel applicableOccurance = NamingUtil.GetObjectTypeOverride(type, typeObjectType);
                        IFCLabel typeDescription = NamingUtil.GetDescriptionOverride(type, IFCLabel.Create());
                        IFCLabel typeElemId = NamingUtil.CreateIFCElementId(type);

                        HashSet<IFCAnyHandle> propertySetsOpt = new HashSet<IFCAnyHandle>();
                        IList<IFCAnyHandle> repMapListOpt = new List<IFCAnyHandle>();

                        IFCAnyHandle styleHandle = FamilyExporterUtil.ExportGenericType(file, exportType, ifcEnumType, typeGUID, ownerHistory, typeName,
                           typeDescription, applicableOccurance, propertySetsOpt, repMapListOpt, typeElemId, typeName, element, type);
                        if (styleHandle.HasValue)
                        {
                            currentTypeInfo.SetStyle(styleHandle);
                            exporterIFC.AddType(typeId, false, currentTypeInfo);
                        }
                    }
                    IFCLabel instanceGUID = IFCLabel.CreateGUID(element);
                    IFCLabel origInstanceName = NamingUtil.CreateIFCName(exporterIFC, -1);
                    IFCLabel instanceName = NamingUtil.GetNameOverride(element, origInstanceName);
                    IFCLabel objectType = NamingUtil.CreateIFCObjectName(exporterIFC, element);
                    IFCLabel instanceObjectType = NamingUtil.GetObjectTypeOverride(element, objectType);
                    IFCLabel instanceDescription = NamingUtil.GetDescriptionOverride(element, IFCLabel.Create());
                    IFCLabel instanceElemId = NamingUtil.CreateIFCElementId(element);

                    bool roomRelated = !FamilyExporterUtil.IsDistributionFlowElementSubType(exportType);

                    ElementId roomId = ElementId.InvalidElementId;
                    if (roomRelated)
                    {
                        roomId = setter.UpdateRoomRelativeCoordinates(element, out localPlacementToUse);
                    }

                    IFCAnyHandle instanceHandle = IFCAnyHandle.Create();
                    if (FamilyExporterUtil.IsFurnishingElementSubType(exportType))
                    {
                        instanceHandle = file.CreateFurnishingElement(instanceGUID, ownerHistory,
                           instanceName, instanceDescription, instanceObjectType, localPlacementToUse, productRepresentation, instanceElemId);
                    }
                    else if (FamilyExporterUtil.IsDistributionFlowElementSubType(exportType))
                    {
                        instanceHandle = file.CreateDistributionFlowElement(instanceGUID, ownerHistory,
                           instanceName, instanceDescription, instanceObjectType, localPlacementToUse, productRepresentation, instanceElemId);
                    }
                    else if (FamilyExporterUtil.IsEnergyConversionDeviceSubType(exportType))
                    {
                        instanceHandle = file.CreateEnergyConversionDevice(instanceGUID, ownerHistory,
                           instanceName, instanceDescription, instanceObjectType, localPlacementToUse, productRepresentation, instanceElemId);
                    }
                    else if (FamilyExporterUtil.IsFlowFittingSubType(exportType))
                    {
                        instanceHandle = file.CreateFlowFitting(instanceGUID, ownerHistory,
                          instanceName, instanceDescription, instanceObjectType, localPlacementToUse, productRepresentation, instanceElemId);
                    }
                    else if (FamilyExporterUtil.IsFlowMovingDeviceSubType(exportType))
                    {
                        instanceHandle = file.CreateFlowMovingDevice(instanceGUID, ownerHistory,
                           instanceName, instanceDescription, instanceObjectType, localPlacementToUse, productRepresentation, instanceElemId);
                    }
                    else if (FamilyExporterUtil.IsFlowSegmentSubType(exportType))
                    {
                        instanceHandle = file.CreateFlowSegment(instanceGUID, ownerHistory,
                           instanceName, instanceDescription, instanceObjectType, localPlacementToUse, productRepresentation, instanceElemId);
                    }
                    else if (FamilyExporterUtil.IsFlowStorageDeviceSubType(exportType))
                    {
                        instanceHandle = file.CreateFlowStorageDevice(instanceGUID, ownerHistory,
                           instanceName, instanceDescription, instanceObjectType, localPlacementToUse, productRepresentation, instanceElemId);
                    }
                    else if (FamilyExporterUtil.IsFlowTerminalSubType(exportType))
                    {
                        instanceHandle = file.CreateFlowTerminal(instanceGUID, ownerHistory,
                           instanceName, instanceDescription, instanceObjectType, localPlacementToUse, productRepresentation, instanceElemId);
                    }
                    else if (FamilyExporterUtil.IsFlowTreatmentDeviceSubType(exportType))
                    {
                        instanceHandle = file.CreateFlowTreatmentDevice(instanceGUID, ownerHistory,
                           instanceName, instanceDescription, instanceObjectType, localPlacementToUse, productRepresentation, instanceElemId);
                    }
                    else if (FamilyExporterUtil.IsFlowControllerSubType(exportType))
                    {
                        instanceHandle = file.CreateFlowController(instanceGUID, ownerHistory,
                           instanceName, instanceDescription, instanceObjectType, localPlacementToUse, productRepresentation, instanceElemId);
                    }

                    if (!instanceHandle.HasValue)
                        return;

                    if (roomId != ElementId.InvalidElementId)
                    {
                        exporterIFC.RelateSpatialElement(roomId, instanceHandle);
                        productWrapper.AddElement(instanceHandle, setter, extraParams, false);
                    }
                    else
                    {
                        productWrapper.AddElement(instanceHandle, setter, extraParams, true);
                    }

                    OpeningUtil.CreateOpeningsIfNecessary(instanceHandle, element, extraParams, exporterIFC, localPlacementToUse, setter, productWrapper);

                    if (currentTypeInfo.IsValid())
                        exporterIFC.AddTypeRelation(currentTypeInfo.GetStyle(), instanceHandle);

                    ExporterIFCUtils.CreateGenericElementPropertySet(exporterIFC, element, productWrapper);

                    tr.Commit();
                }
            }
        }
    }
}
