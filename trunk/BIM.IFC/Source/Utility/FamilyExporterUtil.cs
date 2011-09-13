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
using System.Linq;
using System.Text;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB;

namespace BIM.IFC.Utility
{
    class FamilyExporterUtil
    {
        /// <summary>
        /// Checks if export type is distribution control element.
        /// </summary>
        /// <param name="exportType">
        /// The export type.
        /// </param>
        /// <returns>
        /// True if it is distribution control element, false otherwise.
        /// </returns>
        public static bool IsDistributionControlElementSubType(IFCExportType exportType)
        {
            return (exportType >= IFCExportType.ExportActuatorType && exportType <= IFCExportType.ExportSensorType);
        }

        /// <summary>
        /// Checks if export type is distribution flow element.
        /// </summary>
        /// <param name="exportType">
        /// The export type.
        /// </param>
        /// <returns>
        /// True if it is distribution flow element, false otherwise.
        /// </returns>
        public static bool IsDistributionFlowElementSubType(IFCExportType exportType)
        {
            return (exportType >= IFCExportType.ExportDistributionChamberElementType &&
               exportType <= IFCExportType.ExportFlowController);
        }

        /// <summary>
        /// Checks if export type is conversion device.
        /// </summary>
        /// <param name="exportType">
        /// The export type.
        /// </param>
        /// <returns>
        /// True if it is conversion device, false otherwise.
        /// </returns>
        public static bool IsEnergyConversionDeviceSubType(IFCExportType exportType)
        {
            return (exportType >= IFCExportType.ExportAirToAirHeatRecoveryType &&
               exportType <= IFCExportType.ExportUnitaryEquipmentType);
        }

        /// <summary>
        /// Checks if export type is flow fitting.
        /// </summary>
        /// <param name="exportType">
        /// The export type.
        /// </param>
        /// <returns>
        /// True if it is flow fitting, false otherwise.
        /// </returns>
        public static bool IsFlowFittingSubType(IFCExportType exportType)
        {
            return (exportType >= IFCExportType.ExportCableCarrierFittingType &&
               exportType <= IFCExportType.ExportPipeFittingType);
        }

        /// <summary>
        /// Checks if export type is flow moving device.
        /// </summary>
        /// <param name="exportType">
        /// The export type.
        /// </param>
        /// <returns>
        /// True if it is flow moving device, false otherwise.
        /// </returns>
        public static bool IsFlowMovingDeviceSubType(IFCExportType exportType)
        {
            return (exportType >= IFCExportType.ExportCompressorType &&
               exportType <= IFCExportType.ExportPumpType);
        }

        /// <summary>
        /// Checks if export type is flow segment.
        /// </summary>
        /// <param name="exportType">
        /// The export type.
        /// </param>
        /// <returns>
        /// True if it is flow segment, false otherwise.
        /// </returns>
        public static bool IsFlowSegmentSubType(IFCExportType exportType)
        {
            return (exportType >= IFCExportType.ExportCableCarrierSegmentType &&
               exportType <= IFCExportType.ExportPipeSegmentType);
        }

        /// <summary>
        /// Checks if export type is flow storage device.
        /// </summary>
        /// <param name="exportType">
        /// The export type.
        /// </param>
        /// <returns>
        /// True if it is flow storage device, false otherwise.
        /// </returns>
        public static bool IsFlowStorageDeviceSubType(IFCExportType exportType)
        {
            return (exportType >= IFCExportType.ExportElectricFlowStorageDeviceType &&
               exportType <= IFCExportType.ExportTankType);
        }

        /// <summary>
        /// Checks if export type is flow terminal.
        /// </summary>
        /// <param name="exportType">
        /// The export type.
        /// </param>
        /// <returns>
        /// True if it is flow terminal, false otherwise.
        /// </returns>
        public static bool IsFlowTerminalSubType(IFCExportType exportType)
        {
            return (exportType >= IFCExportType.ExportAirTerminalType &&
               exportType <= IFCExportType.ExportWasteTerminalType);
        }

        /// <summary>
        /// Checks if export type is flow treatment device.
        /// </summary>
        /// <param name="exportType">
        /// The export type.
        /// </param>
        /// <returns>
        /// True if it is flow treatment device, false otherwise.
        /// </returns>
        public static bool IsFlowTreatmentDeviceSubType(IFCExportType exportType)
        {
            return (exportType >= IFCExportType.ExportDuctSilencerType &&
               exportType <= IFCExportType.ExportFilterType);
        }

        /// <summary>
        /// Checks if export type is flow controller.
        /// </summary>
        /// <param name="exportType">
        /// The export type.
        /// </param>
        /// <returns>
        /// True if it is flow controller, false otherwise.
        /// </returns>
        public static bool IsFlowControllerSubType(IFCExportType exportType)
        {
            return (exportType >= IFCExportType.ExportAirTerminalBoxType &&
               exportType <= IFCExportType.ExportValveType);
        }

        /// <summary>
        /// Checks if export type is furnishing element.
        /// </summary>
        /// <param name="exportType">
        /// The export type.
        /// </param>
        /// <returns>
        /// True if it is furnishing element, false otherwise.
        /// </returns>
        public static bool IsFurnishingElementSubType(IFCExportType exportType)
        {
            return (exportType >= IFCExportType.ExportFurnitureType &&
               exportType <= IFCExportType.ExportSystemFurnitureElementType);
        }

        /// <summary>
        /// Exports a generic family instance as IFC instance.
        /// </summary>
        /// <param name="type">The export type.</param>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="familyInstance">The element.</param>
        /// <param name="wrapper">The IFCProductWrapper.</param>
        /// <param name="setter">The IFCPlacementSetter.</param>
        /// <param name="extraParams">The extrusion creation data.</param>
        /// <param name="instanceGUID">The guid.</param>
        /// <param name="ownerHistory">The owner history handle.</param>
        /// <param name="instanceName">The name.</param>
        /// <param name="instanceDescription">The description.</param>
        /// <param name="instanceObjectType">The object type.</param>
        /// <param name="productRepresentation">The representation handle.</param>
        /// <param name="instanceElemId">The element id label.</param>
        /// <returns>The handle.</returns>
        public static IFCAnyHandle ExportGenericInstance(IFCExportType type,
           ExporterIFC exporterIFC, Element familyInstance,
           IFCProductWrapper wrapper, IFCPlacementSetter setter, IFCExtrusionCreationData extraParams,
           IFCLabel instanceGUID, IFCAnyHandle ownerHistory,
           IFCLabel instanceName, IFCLabel instanceDescription, IFCLabel instanceObjectType,
           IFCAnyHandle productRepresentation,
           IFCLabel instanceElemId)
        {
            IFCFile file = exporterIFC.GetFile();
            Document doc = familyInstance.Document;

            bool isRoomRelated = IsRoomRelated(type);

            IFCAnyHandle localPlacementToUse = setter.GetPlacement();
            ElementId roomId = ElementId.InvalidElementId;
            if (isRoomRelated)
            {
                roomId = setter.UpdateRoomRelativeCoordinates(familyInstance, out localPlacementToUse);
            }

            IFCAnyHandle instanceHandle = IFCAnyHandle.Create();
            switch (type)
            {
                case IFCExportType.ExportColumnType:
                    {
                        instanceHandle = file.CreateColumn(instanceGUID, ownerHistory,
                           instanceName, instanceDescription, instanceObjectType, localPlacementToUse, productRepresentation, instanceElemId);
                        break;
                    }
                case IFCExportType.ExportMemberType:
                    {
                        instanceHandle = file.CreateMember(instanceGUID, ownerHistory,
                           instanceName, instanceDescription, instanceObjectType, localPlacementToUse, productRepresentation, instanceElemId);
                        break;
                    }
                case IFCExportType.ExportPlateType:
                    {
                        instanceHandle = file.CreatePlate(instanceGUID, ownerHistory,
                           instanceName, instanceDescription, instanceObjectType, localPlacementToUse, productRepresentation, instanceElemId);
                        break;
                    }
                default:
                    {
                        if (IsFurnishingElementSubType(type))
                        {
                            instanceHandle = file.CreateFurnishingElement(instanceGUID, ownerHistory,
                               instanceName, instanceDescription, instanceObjectType, localPlacementToUse, productRepresentation, instanceElemId);
                        }
                        else if (IsDistributionFlowElementSubType(type))
                        {
                            instanceHandle = file.CreateDistributionFlowElement(instanceGUID, ownerHistory,
                               instanceName, instanceDescription, instanceObjectType, localPlacementToUse, productRepresentation, instanceElemId);
                        }
                        else if (IsEnergyConversionDeviceSubType(type))
                        {
                            instanceHandle = file.CreateEnergyConversionDevice(instanceGUID, ownerHistory,
                               instanceName, instanceDescription, instanceObjectType, localPlacementToUse, productRepresentation, instanceElemId);
                        }
                        else if (IsFlowFittingSubType(type))
                        {
                            instanceHandle = file.CreateFlowFitting(instanceGUID, ownerHistory,
                               instanceName, instanceDescription, instanceObjectType, localPlacementToUse, productRepresentation, instanceElemId);
                        }
                        else if (IsFlowMovingDeviceSubType(type))
                        {
                            instanceHandle = file.CreateFlowMovingDevice(instanceGUID, ownerHistory,
                               instanceName, instanceDescription, instanceObjectType, localPlacementToUse, productRepresentation, instanceElemId);
                        }
                        else if (IsFlowSegmentSubType(type))
                        {
                            instanceHandle = file.CreateFlowSegment(instanceGUID, ownerHistory,
                               instanceName, instanceDescription, instanceObjectType, localPlacementToUse, productRepresentation, instanceElemId);
                        }
                        else if (IsFlowStorageDeviceSubType(type))
                        {
                            instanceHandle = file.CreateFlowStorageDevice(instanceGUID, ownerHistory,
                               instanceName, instanceDescription, instanceObjectType, localPlacementToUse, productRepresentation, instanceElemId);
                        }
                        else if (IsFlowTerminalSubType(type))
                        {
                            instanceHandle = file.CreateFlowTerminal(instanceGUID, ownerHistory,
                               instanceName, instanceDescription, instanceObjectType, localPlacementToUse, productRepresentation, instanceElemId);
                        }
                        else if (IsFlowTreatmentDeviceSubType(type))
                        {
                            instanceHandle = file.CreateFlowTreatmentDevice(instanceGUID, ownerHistory,
                               instanceName, instanceDescription, instanceObjectType, localPlacementToUse, productRepresentation, instanceElemId);
                        }
                        else if (IsFlowControllerSubType(type))
                        {
                            instanceHandle = file.CreateFlowController(instanceGUID, ownerHistory,
                               instanceName, instanceDescription, instanceObjectType, localPlacementToUse, productRepresentation, instanceElemId);
                        }
                        break;
                    }
            }

            if (instanceHandle.HasValue)
            {
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
            return instanceHandle;
        }

        /// <summary>
        /// Exports IFC type.
        /// </summary>
        /// <remarks>
        /// This method will override the default value of the elemId label for certain element types, and then pass it on
        /// to the generic routine.
        /// </remarks>
        /// <param name="file">The IFC file.</param>
        /// <param name="type">The export type.</param>
        /// <param name="ifcEnumType">The string value represents the IFC type.</param>
        /// <param name="guid">The guid.</param>
        /// <param name="ownerHistory">The owner history handle.</param>
        /// <param name="name">The name.</param>
        /// <param name="description">The description.</param>
        /// <param name="applicableOccurrence">The optional data type of the entity.</param>
        /// <param name="propertySets">The property sets.</param>
        /// <param name="representationMapList">List of representations.</param>
        /// <param name="elemId">The element id label.</param>
        /// <param name="typeName">The IFCPlacementSetter.</param>
        /// <param name="instance">The family instance.</param>
        /// <param name="symbol">The element type.</param>
        /// <returns>The handle.</returns>
        public static IFCAnyHandle ExportGenericType(IFCFile file,
           IFCExportType type,
           string ifcEnumType,
           IFCLabel guid,
           IFCAnyHandle ownerHistory,
           IFCLabel name,
           IFCLabel description,
           IFCLabel applicableOccurrence,
           HashSet<IFCAnyHandle> propertySets,
           IList<IFCAnyHandle> representationMapList,
           IFCLabel elemId,
           IFCLabel typeName,
           Element instance,
           ElementType symbol)
        {
            IFCLabel elemIdToUse = elemId;
            switch (type)
            {
                case IFCExportType.ExportFurnitureType:
                case IFCExportType.ExportMemberType:
                case IFCExportType.ExportPlateType:
                    {
                        elemIdToUse = NamingUtil.CreateIFCElementId(instance);
                        break;
                    }
            }
            return ExportGenericTypeBase(file, type, ifcEnumType, guid, ownerHistory, name, description, applicableOccurrence,
               propertySets, representationMapList, elemIdToUse, typeName, instance, symbol);
        }

        /// <summary>
        /// Exports IFC type base implementation.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="type">The export type.</param>
        /// <param name="ifcEnumType">The string value represents the IFC type.</param>
        /// <param name="guid">The guid.</param>
        /// <param name="ownerHistory">The owner history handle.</param>
        /// <param name="name">The name.</param>
        /// <param name="description">The description.</param>
        /// <param name="applicableOccurrence">The optional data type of the entity.</param>
        /// <param name="propertySets">The property sets.</param>
        /// <param name="representationMapList">List of representations.</param>
        /// <param name="elemId">The element id label.</param>
        /// <param name="typeName">The IFCPlacementSetter.</param>
        /// <param name="instance">The family instance.</param>
        /// <param name="symbol">The element type.</param>
        /// <returns>The handle.</returns>
        private static IFCAnyHandle ExportGenericTypeBase(IFCFile file,
           IFCExportType type,
           string ifcEnumType,
           IFCLabel guid,
           IFCAnyHandle ownerHistory,
           IFCLabel name,
           IFCLabel description,
           IFCLabel applicableOccurrence,
           HashSet<IFCAnyHandle> propertySets,
           IList<IFCAnyHandle> representationMapList,
           IFCLabel elemId,
           IFCLabel typeName,
           Element instance,
           ElementType symbol)
        {
            switch (type)
            {
                case IFCExportType.ExportActuatorType:
                    return file.CreateActuatorType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportAirTerminalBoxType:
                    return file.CreateAirTerminalBoxType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportAirTerminalType:
                    return file.CreateAirTerminalType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportAirToAirHeatRecoveryType:
                    return file.CreateAirToAirHeatRecoveryType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportAlarmType:
                    return file.CreateAlarmType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportBoilerType:
                    return file.CreateBoilerType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportCableCarrierFittingType:
                    return file.CreateCableCarrierFittingType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportCableCarrierSegmentType:
                    return file.CreateCableCarrierSegmentType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportCableSegmentType:
                    return file.CreateCableSegmentType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportChillerType:
                    return file.CreateChillerType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportCoilType:
                    return file.CreateCoilType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportCompressorType:
                    return file.CreateCompressorType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportCondenserType:
                    return file.CreateCondenserType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportControllerType:
                    return file.CreateControllerType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportCooledBeamType:
                    return file.CreateCooledBeamType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportCoolingTowerType:
                    return file.CreateCoolingTowerType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportDamperType:
                    return file.CreateDamperType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportDistributionChamberElementType:
                    return file.CreateDistributionChamberElementType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportDuctFittingType:
                    return file.CreateDuctFittingType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportDuctSegmentType:
                    return file.CreateDuctSegmentType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportDuctSilencerType:
                    return file.CreateDuctSilencerType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportElectricApplianceType:
                    return file.CreateElectricApplianceType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportElectricFlowStorageDeviceType:
                    return file.CreateElectricFlowStorageDeviceType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportElectricGeneratorType:
                    return file.CreateElectricGeneratorType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportElectricHeaterType:
                    return file.CreateElectricHeaterType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportElectricMotorType:
                    return file.CreateElectricMotorType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportElectricTimeControlType:
                    return file.CreateElectricTimeControlType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportEvaporativeCoolerType:
                    return file.CreateEvaporativeCoolerType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportEvaporatorType:
                    return file.CreateEvaporatorType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportFanType:
                    return file.CreateFanType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportFilterType:
                    return file.CreateFilterType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportFireSuppressionTerminalType:
                    return file.CreateFireSuppressionTerminalType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportFlowInstrumentType:
                    return file.CreateFlowInstrumentType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportFlowMeterType:
                    return file.CreateFlowMeterType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportFurnitureType:
                    return file.CreateFurnitureType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportGasTerminalType:
                    return file.CreateGasTerminalType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportHeatExchangerType:
                    return file.CreateHeatExchangerType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportHumidifierType:
                    return file.CreateHumidifierType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportJunctionBoxType:
                    return file.CreateJunctionBoxType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportLampType:
                    return file.CreateLampType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportLightFixtureType:
                    return file.CreateLightFixtureType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportMemberType:
                    return file.CreateMemberType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportMotorConnectionType:
                    return file.CreateMotorConnectionType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportOutletType:
                    return file.CreateOutletType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportPlateType:
                    return file.CreatePlateType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportPipeFittingType:
                    return file.CreatePipeFittingType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportPipeSegmentType:
                    return file.CreatePipeSegmentType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportProtectiveDeviceType:
                    return file.CreateProtectiveDeviceType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportPumpType:
                    return file.CreatePumpType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportSanitaryTerminalType:
                    return file.CreateSanitaryTerminalType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportSensorType:
                    return file.CreateSensorType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportSpaceHeaterType:
                    return file.CreateSpaceHeaterType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportStackTerminalType:
                    return file.CreateStackTerminalType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportSwitchingDeviceType:
                    return file.CreateSwitchingDeviceType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportTankType:
                    return file.CreateTankType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportTransformerType:
                    return file.CreateTransformerType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportTransportElementType:
                    return file.CreateTransportElementType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportTubeBundleType:
                    return file.CreateTubeBundleType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportUnitaryEquipmentType:
                    return file.CreateUnitaryEquipmentType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportValveType:
                    return file.CreateValveType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                case IFCExportType.ExportWasteTerminalType:
                    return file.CreateWasteTerminalType(ifcEnumType, guid, ownerHistory, name,
                       description, applicableOccurrence, propertySets, representationMapList, elemId,
                       typeName, instance, symbol);
                default:
                    return IFCAnyHandle.Create();
            }
        }

        /// <summary>
        /// Checks if export type is room related.
        /// </summary>
        /// <param name="exportType">The export type.</param>
        /// <returns>True if the export type is room related, false otherwise.</returns>
        private static bool IsRoomRelated(IFCExportType exportType)
        {
            return (IsFurnishingElementSubType(exportType) ||
                IsDistributionControlElementSubType(exportType) ||
                IsDistributionFlowElementSubType(exportType) ||
                IsEnergyConversionDeviceSubType(exportType) ||
                IsFlowFittingSubType(exportType) ||
                IsFlowMovingDeviceSubType(exportType) ||
                IsFlowSegmentSubType(exportType) ||
                IsFlowStorageDeviceSubType(exportType) ||
                IsFlowTerminalSubType(exportType) ||
                IsFlowTreatmentDeviceSubType(exportType) ||
                IsFlowControllerSubType(exportType));
        }
    }
}
