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
using Revit.IFC.Common.Utility;


namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Provides methods to export generic family instances and types.
   /// </summary>
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
         return (exportType >= IFCExportType.IfcActuatorType && exportType <= IFCExportType.IfcSensorType);
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
         return (exportType >= IFCExportType.IfcDistributionChamberElementType &&
            exportType <= IFCExportType.IfcFlowController);
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
         // Note: Implementer's agreement #CV-2x3-166 changes IfcSpaceHeaterType from IfcEnergyConversionDevice to IfcFlowTerminal.
         return (exportType >= IFCExportType.IfcAirToAirHeatRecoveryType &&
            exportType <= IFCExportType.IfcUnitaryEquipmentType) && (exportType != IFCExportType.IfcSpaceHeaterType);
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
         return (exportType >= IFCExportType.IfcCableCarrierFittingType &&
            exportType <= IFCExportType.IfcPipeFittingType);
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
         return (exportType >= IFCExportType.IfcCompressorType &&
            exportType <= IFCExportType.IfcPumpType);
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
         return (exportType >= IFCExportType.IfcCableCarrierSegmentType &&
            exportType <= IFCExportType.IfcPipeSegmentType);
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
         return (exportType >= IFCExportType.IfcElectricFlowStorageDeviceType &&
            exportType <= IFCExportType.IfcTankType);
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
         // Note: Implementer's agreement #CV-2x3-166 changes IfcSpaceHeaterType from IfcEnergyConversionDevice to IfcFlowTerminal.
         return (exportType >= IFCExportType.IfcAirTerminalType &&
            exportType <= IFCExportType.IfcWasteTerminalType) || (exportType == IFCExportType.IfcSpaceHeaterType);
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
         return (exportType >= IFCExportType.IfcDuctSilencerType &&
            exportType <= IFCExportType.IfcFilterType);
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
         return (exportType >= IFCExportType.IfcAirTerminalBoxType &&
            exportType <= IFCExportType.IfcValveType);
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
         return (exportType >= IFCExportType.IfcFurnishingElement &&
            exportType <= IFCExportType.IfcSystemFurnitureElementType);
      }

      /// <summary>
      /// Exports a generic family instance as IFC instance.
      /// </summary>
      /// <param name="type">The export type.</param>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="familyInstance">The element.</param>
      /// <param name="wrapper">The ProductWrapper.</param>
      /// <param name="setter">The PlacementSetter.</param>
      /// <param name="extraParams">The extrusion creation data.</param>
      /// <param name="instanceGUID">The guid.</param>
      /// <param name="ownerHistory">The owner history handle.</param>
      /// <param name="instanceName">The name.</param>
      /// <param name="instanceDescription">The description.</param>
      /// <param name="instanceObjectType">The object type.</param>
      /// <param name="productRepresentation">The representation handle.</param>
      /// <param name="instanceTag">The tag for the entity, usually based on the element id.</param>
      /// <param name="ifcEnumType">The predefined type/shape type, if any, for the object.</param>
      /// <param name="overrideLocalPlacement">The local placement to use instead of the one in the placement setter, if appropriate.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle ExportGenericInstance(IFCExportType type,
         ExporterIFC exporterIFC, Element familyInstance,
         ProductWrapper wrapper, PlacementSetter setter, IFCExtrusionCreationData extraParams,
         string instanceGUID, IFCAnyHandle ownerHistory,
         string instanceName, string instanceDescription, string instanceObjectType,
         IFCAnyHandle productRepresentation,
         string instanceTag, string ifcEnumType, IFCAnyHandle overrideLocalPlacement)
      {
         IFCFile file = exporterIFC.GetFile();
         Document doc = familyInstance.Document;

         bool isRoomRelated = IsRoomRelated(type);
         bool isChildInContainer = familyInstance.AssemblyInstanceId != ElementId.InvalidElementId;

         IFCAnyHandle localPlacementToUse = setter.LocalPlacement;
         ElementId roomId = ElementId.InvalidElementId;
         if (isRoomRelated)
         {
            roomId = setter.UpdateRoomRelativeCoordinates(familyInstance, out localPlacementToUse);
         }

         //should remove the create method where there is no use of this handle for API methods
         //some places uses the return value of ExportGenericInstance as input parameter for API methods
         IFCAnyHandle instanceHandle = null;
         switch (type)
         {
            case IFCExportType.IfcBeam:
               {
                  string preDefinedType = string.IsNullOrWhiteSpace(ifcEnumType) ? "BEAM" : ifcEnumType;
                  instanceHandle = IFCInstanceExporter.CreateBeam(file, instanceGUID, ownerHistory,
                     instanceName, instanceDescription, instanceObjectType, localPlacementToUse,
                     productRepresentation, instanceTag, preDefinedType);
                  break;
               }
            case IFCExportType.IfcColumnType:
               {
                  string preDefinedType = string.IsNullOrWhiteSpace(ifcEnumType) ? "COLUMN" : ifcEnumType;
                  instanceHandle = IFCInstanceExporter.CreateColumn(file, instanceGUID, ownerHistory,
                     instanceName, instanceDescription, instanceObjectType, localPlacementToUse, productRepresentation, instanceTag, preDefinedType);
                  break;
               }
            case IFCExportType.IfcMemberType:
               {
                  string preDefinedType = string.IsNullOrWhiteSpace(ifcEnumType) ? "BRACE" : ifcEnumType;
                  instanceHandle = IFCInstanceExporter.CreateMember(file, instanceGUID, ownerHistory,
                     instanceName, instanceDescription, instanceObjectType, localPlacementToUse, productRepresentation, instanceTag, preDefinedType);

                  // Register the members's IFC handle for later use by truss export.
                  ExporterCacheManager.ElementToHandleCache.Register(familyInstance.Id, instanceHandle);
                  break;
               }
            case IFCExportType.IfcPlateType:
               {
                  IFCAnyHandle localPlacement = localPlacementToUse;
                  if (overrideLocalPlacement != null)
                  {
                     isChildInContainer = true;
                     localPlacement = overrideLocalPlacement;
                  }

                  string preDefinedType = string.IsNullOrWhiteSpace(ifcEnumType) ? "NOTDEFINED" : ifcEnumType;
                  instanceHandle = IFCInstanceExporter.CreatePlate(file, instanceGUID, ownerHistory,
                      instanceName, instanceDescription, instanceObjectType, localPlacement, productRepresentation, instanceTag, preDefinedType);
                  break;
               }
            case IFCExportType.IfcDiscreteAccessoryType:
               {
                  instanceHandle = IFCInstanceExporter.CreateDiscreteAccessory(file, instanceGUID, ownerHistory,
                     instanceName, instanceDescription, instanceObjectType, localPlacementToUse, productRepresentation, instanceTag);
                  break;
               }
            case IFCExportType.IfcDistributionControlElement:
               {
                  instanceHandle = IFCInstanceExporter.CreateDistributionControlElement(file, instanceGUID, ownerHistory,
                     instanceName, instanceDescription, instanceObjectType, localPlacementToUse, productRepresentation, instanceTag,
                     null);
                  break;
               }
            case IFCExportType.IfcDistributionFlowElement:
               {
                  instanceHandle = IFCInstanceExporter.CreateDistributionFlowElement(file, instanceGUID, ownerHistory,
                     instanceName, instanceDescription, instanceObjectType, localPlacementToUse, productRepresentation, instanceTag);
                  break;
               }
            case IFCExportType.IfcDistributionChamberElement:
            case IFCExportType.IfcDistributionChamberElementType:
               {
                  instanceHandle = IFCInstanceExporter.CreateGenericIFCEntity(Common.Enums.IFCEntityType.IfcDistributionChamberElement, file, instanceGUID, ownerHistory,
                      instanceName, instanceDescription, instanceObjectType, localPlacementToUse, productRepresentation, instanceTag);
                  break;
               }
            case IFCExportType.IfcFastenerType:
               {
                  instanceHandle = IFCInstanceExporter.CreateFastener(file, instanceGUID, ownerHistory,
                     instanceName, instanceDescription, instanceObjectType, localPlacementToUse, productRepresentation, instanceTag);
                  break;
               }
            case IFCExportType.IfcMechanicalFastenerType:
               {
                  double? nominalDiameter = null;
                  double? nominalLength = null;

                  double nominalDiameterVal, nominalLengthVal;
                  if (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "NominalDiameter", out nominalDiameterVal) != null)
                     nominalDiameter = UnitUtil.ScaleLength(nominalDiameterVal);
                  if (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "NominalLength", out nominalLengthVal) != null)
                     nominalLength = UnitUtil.ScaleLength(nominalLengthVal);

                  string preDefinedType = string.IsNullOrWhiteSpace(ifcEnumType) ? "NOTDEFINED" : ifcEnumType;

                  instanceHandle = IFCInstanceExporter.CreateMechanicalFastener(file, instanceGUID, ownerHistory,
                     instanceName, instanceDescription, instanceObjectType, localPlacementToUse, productRepresentation, instanceTag,
                     nominalDiameter, nominalLength, preDefinedType);
                  break;
               }
            case IFCExportType.IfcRailingType:
               {
                  string strEnumType;
                  IFCExportType exportAs = ExporterUtil.GetExportType(exporterIFC, familyInstance, out strEnumType);
                  if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
                  {
                     instanceHandle = IFCInstanceExporter.CreateRailing(file, instanceGUID, ownerHistory, instanceName, instanceDescription,
                         instanceObjectType, localPlacementToUse, productRepresentation, instanceTag,
                         GetPreDefinedType<Toolkit.IFC4.IFCRailingType>(familyInstance, strEnumType).ToString());
                  }
                  else
                  {
                     instanceHandle = IFCInstanceExporter.CreateRailing(file, instanceGUID, ownerHistory, instanceName, instanceDescription,
                         instanceObjectType, localPlacementToUse, productRepresentation, instanceTag,
                         GetPreDefinedType<Toolkit.IFCRailingType>(familyInstance, strEnumType).ToString());
                  }
                  break;
               }
            case IFCExportType.IfcSpace:
               {
                  string instanceLongName = NamingUtil.GetLongNameOverride(familyInstance, NamingUtil.GetLongNameOverride(familyInstance, instanceName));
                  IFCInternalOrExternal internalOrExternal = CategoryUtil.IsElementExternal(familyInstance) ? IFCInternalOrExternal.External : IFCInternalOrExternal.Internal;

                  instanceHandle = IFCInstanceExporter.CreateSpace(file, instanceGUID, ownerHistory, instanceName, instanceDescription,
                      instanceObjectType, localPlacementToUse, productRepresentation, instanceLongName, IFCElementComposition.Element,
                      internalOrExternal, null);
                  break;
               }
            default:
               {
                  if (ExporterCacheManager.ExportOptionsCache.ExportAs4 &&
                         (type == IFCExportType.IfcDistributionElement ||
                          type == IFCExportType.IfcEnergyConversionDevice ||
                          type == IFCExportType.IfcFlowController ||
                          type == IFCExportType.IfcFlowFitting ||
                          type == IFCExportType.IfcFlowMovingDevice ||
                          type == IFCExportType.IfcFlowSegment ||
                          type == IFCExportType.IfcFlowStorageDevice ||
                          type == IFCExportType.IfcFlowTerminal ||
                          type == IFCExportType.IfcFlowTreatmentDevice))
                  {
                     // for IFC4, there are several entities that are valid in IFC2x3 but now have been made abstract or deprecated, so cannot be created. Create proxy instead.
                     instanceHandle = IFCInstanceExporter.CreateBuildingElementProxy(file, instanceGUID, ownerHistory,
                         instanceName, instanceDescription, instanceObjectType, localPlacementToUse, productRepresentation, instanceTag,
                         Toolkit.IFC4.IFCBuildingElementProxyType.USERDEFINED.ToString());
                  }
                  else if ((type == IFCExportType.IfcFurnishingElement) || IsFurnishingElementSubType(type) ||
                              (type == IFCExportType.IfcEnergyConversionDevice) || IsEnergyConversionDeviceSubType(type) ||
                              (type == IFCExportType.IfcFlowFitting) || IsFlowFittingSubType(type) ||
                              (type == IFCExportType.IfcFlowMovingDevice) || IsFlowMovingDeviceSubType(type) ||
                              (type == IFCExportType.IfcFlowSegment) || IsFlowSegmentSubType(type) ||
                              (type == IFCExportType.IfcFlowStorageDevice) || IsFlowStorageDeviceSubType(type) ||
                              (type == IFCExportType.IfcFlowTerminal) || IsFlowTerminalSubType(type) ||
                              (type == IFCExportType.IfcFlowTreatmentDevice) || IsFlowTreatmentDeviceSubType(type) ||
                              (type == IFCExportType.IfcFlowController) || IsFlowControllerSubType(type) ||
                              (type == IFCExportType.IfcDistributionFlowElement) || IsDistributionFlowElementSubType(type) ||
                              (type == IFCExportType.IfcBuildingElementProxy) || (type == IFCExportType.IfcBuildingElementProxyType))
                  {
                     string exportEntityStr = type.ToString();
                     Common.Enums.IFCEntityType exportEntity = Common.Enums.IFCEntityType.UnKnown;

                     if (String.Compare(exportEntityStr.Substring(exportEntityStr.Length - 4), "Type", true) == 0)
                        exportEntityStr = exportEntityStr.Substring(0, (exportEntityStr.Length - 4));
                     if (!Enum.TryParse(exportEntityStr, out exportEntity))
                     {
                        // This is a special case.   IFC2x3 has IfcFlowElement for the instance, and both IfcElectricHeaterType and IfcSpaceHeaterType.
                        // IFC4 has IfcSpaceHeater and IfcSpaceHeaterType.
                        // Since IfcElectricHeater doesn't exist in IFC4, TryParse will fail.  For the instance only, we will map it to IfcSpaceHeater,
                        // which will in turn be redirected to IfcFlowElement for IFC2x3, if necessary.
                        if (type == IFCExportType.IfcElectricHeaterType)
                           exportEntity = Common.Enums.IFCEntityType.IfcSpaceHeater;
                        else
                           exportEntity = Common.Enums.IFCEntityType.UnKnown;
                     }

                     if (exportEntity != Common.Enums.IFCEntityType.UnKnown)
                     {
                        instanceHandle = IFCInstanceExporter.CreateGenericIFCEntity(exportEntity, file, instanceGUID, ownerHistory,
                           instanceName, instanceDescription, instanceObjectType, localPlacementToUse, productRepresentation, instanceTag);
                     }
                  }
                  break;
               }
         }

         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(instanceHandle))
         {
            bool containedInSpace = (roomId != ElementId.InvalidElementId);
            bool associateToLevel = containedInSpace ? false : !isChildInContainer;
            wrapper.AddElement(familyInstance, instanceHandle, setter, extraParams, associateToLevel);
            if (containedInSpace)
               ExporterCacheManager.SpaceInfoCache.RelateToSpace(roomId, instanceHandle);
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
      /// <param name="exporterIFC">The ExporterIFC class.</param>
      /// <param name="type">The export type.</param>
      /// <param name="ifcEnumType">The string value represents the IFC type.</param>
      /// <param name="guid">The guid.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The optional data type of the entity.</param>
      /// <param name="propertySets">The property sets.</param>
      /// <param name="representationMapList">List of representations.</param>
      /// <param name="elemId">The element id label.</param>
      /// <param name="typeName">The type name.</param>
      /// <param name="instance">The family instance.</param>
      /// <param name="symbol">The element type.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle ExportGenericType(ExporterIFC exporterIFC,
         IFCExportType type,
         string ifcEnumType,
         string guid,
         string name,
         string description,
         string applicableOccurrence,
         HashSet<IFCAnyHandle> propertySets,
         IList<IFCAnyHandle> representationMapList,
         string elemId,
         string typeName,
         Element instance,
         ElementType symbol)
      {
         IFCFile file = exporterIFC.GetFile();
         IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

         string elemIdToUse = elemId;
         string instanceElementType = null;
         switch (type)
         {
            case IFCExportType.IfcFurnitureType:
            case IFCExportType.IfcMemberType:
            case IFCExportType.IfcPlateType:
               {
                  elemIdToUse = NamingUtil.GetTagOverride(instance, NamingUtil.CreateIFCElementId(instance));
                  instanceElementType = NamingUtil.GetOverrideStringValue(instance, "IfcElementType", typeName);
                  break;
               }
         }

         IFCAnyHandle typeHandle = ExportGenericTypeBase(file, type, ifcEnumType, guid, ownerHistory, name, description, applicableOccurrence,
            null, representationMapList, elemIdToUse, instanceElementType, instance, symbol);

         return typeHandle;
      }

      /// <summary>
      /// Exports IFC type.
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
      /// <param name="elementTag">The element tag.</param>
      /// <param name="typeName">The type name.</param>
      /// <param name="instance">The family instance.</param>
      /// <param name="symbol">The element type.</param>
      /// <returns>The handle.</returns>
      private static IFCAnyHandle ExportGenericTypeBase(IFCFile file,
         IFCExportType type,
         string ifcEnumType,
         string guid,
         IFCAnyHandle ownerHistory,
         string name,
         string description,
         string applicableOccurrence,
         HashSet<IFCAnyHandle> propertySets,
         IList<IFCAnyHandle> representationMapList,
         string elementTag,
         string typeName,
         Element instance,
         ElementType symbol)
      {
         // TODO: This routine needs to be simplified.  The long list of IFC2x3 and IFC4 calls to CreateGenericIFCType make it too easy to miss an entry.

         Revit.IFC.Common.Enums.IFCEntityType IFCTypeEntity;
         if (!Enum.TryParse(type.ToString(), out IFCTypeEntity))
            return null;    // The export type is unknown IFC type entity

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            switch (type)
            {
               case IFCExportType.IfcActuatorType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCActuatorType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcAirTerminalBoxType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCAirTerminalBoxType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcAirTerminalType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCAirTerminalType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcAirToAirHeatRecoveryType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCAirToAirHeatRecoveryType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcAlarmType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCAlarmType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcAudioVisualAppliance:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCAudioVisualApplianceType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcBoilerType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCBoilerType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcBurnerType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCBurnerType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcCableCarrierFittingType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCCableCarrierFittingType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcCableCarrierSegmentType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCCableCarrierSegmentType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcCableFittingType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCCableFittingType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcCableSegmentType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCCableSegmentType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcChillerType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCChillerType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcCoilType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCCoilType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcCommunicationAppliance:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCCommunicationsApplianceType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcCompressorType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCCompressorType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcCondenserType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCCondenserType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcControllerType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCControllerType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcCooledBeamType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCCooledBeamType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcCoolingTowerType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCCoolingTowerType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcDamperType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCDamperType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcDistributionChamberElementType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCDistributionChamberElementType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcDuctFittingType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCDuctFittingType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcDuctSegmentType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCDuctSegmentType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcDuctSilencerType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCDuctSilencerType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcElectricApplianceType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCElectricApplianceType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcElectricDistributionBoardType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCElectricDistributionBoardType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcElectricFlowStorageDeviceType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCElectricFlowStorageDeviceType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcElectricGeneratorType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCElectricGeneratorType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcElectricHeaterType:
                  {
                     // Convert to IfcSpaceHeaterType for IFC4.
                     return IFCInstanceExporter.CreateGenericIFCType(Common.Enums.IFCEntityType.IfcSpaceHeaterType, file, guid, ownerHistory, name,
                         description, applicableOccurrence, propertySets, representationMapList, elementTag,
                         typeName, GetPreDefinedType<Toolkit.IFC4.IFCSpaceHeaterType>(instance, ifcEnumType).ToString());
                  }
               case IFCExportType.IfcElectricMotorType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCElectricMotorType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcElectricTimeControlType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCElectricTimeControlType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcEngineType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCEngineType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcEvaporativeCoolerType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCEvaporativeCoolerType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcEvaporatorType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCEvaporatorType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcFanType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCFanType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcFilterType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCFilterType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcFireSuppressionTerminalType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCFireSuppressionTerminalType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcFlowInstrumentType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCFlowInstrumentType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcFlowMeterType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCFlowMeterType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcHeatExchangerType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCHeatExchangerType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcHumidifierType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCHumidifierType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcInterceptorType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCInterceptorType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcJunctionBoxType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCJunctionBoxType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcLampType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCLampType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcLightFixtureType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCLightFixtureType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcMedicalDeviceType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCMedicalDeviceType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcMotorConnectionType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCMotorConnectionType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcOutletType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCOutletType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcPipeFittingType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCPipeFittingType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcPipeSegmentType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCPipeSegmentType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcProtectiveDeviceType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCProtectiveDeviceType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcProtectiveDeviceTrippingUnitType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCProtectiveDeviceTrippingUnitType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcPumpType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCPumpType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcSanitaryTerminalType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCSanitaryTerminalType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcSensorType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCSensorType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcSolarDeviceType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCSolarDeviceType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcSpaceHeaterType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCSpaceHeaterType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcStackTerminalType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCStackTerminalType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcSwitchingDeviceType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCSwitchingDeviceType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcTankType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCTankType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcTransformerType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCTransformerType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcTubeBundleType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCTubeBundleType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcUnitaryControlElementType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCUnitaryControlElementType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcUnitaryEquipmentType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCUnitaryEquipmentType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcValveType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCValveType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcWasteTerminalType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCWasteTerminalType>(instance, ifcEnumType).ToString());

               // Non MEP types
               case IFCExportType.IfcDiscreteAccessoryType:
                  return IFCInstanceExporter.CreateDiscreteAccessoryType(file, guid, ownerHistory, name,
                     description, applicableOccurrence, propertySets, representationMapList, elementTag,
                     typeName);
               case IFCExportType.IfcFastenerType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCFastenerType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcSystemFurnitureElementType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCSystemFurnitureElementType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcFurnitureType:
                  return IFCInstanceExporter.CreateFurnitureType(file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetAssemblyPlace(instance, ifcEnumType), GetPreDefinedType<Toolkit.IFC4.IFCFurnitureType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcMechanicalFastenerType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCMechanicalFastenerType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcMemberType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCMemberType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcPlateType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCPlateType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcTransportElementType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCTransportElementType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcRailingType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCRailingType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcBuildingElementProxy:
               case IFCExportType.IfcBuildingElementProxyType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFC4.IFCBuildingElementProxyType>(instance, ifcEnumType).ToString());
               default:
                  return null;
            }
         }
         else
         {   // For IFC2x3 schema version
            switch (type)
            {
               case IFCExportType.IfcActuatorType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCActuatorType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcAirTerminalBoxType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCAirTerminalBoxType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcAirTerminalType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCAirTerminalType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcAirToAirHeatRecoveryType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCAirToAirHeatRecoveryType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcAlarmType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCAlarmType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcBoilerType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCBoilerType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcBurnerType:
                  {
                     // Map to IFC2x3 IfcGasTerminalType instad.
                     return IFCInstanceExporter.CreateGenericIFCType(Common.Enums.IFCEntityType.IfcGasTerminalType, file, guid, ownerHistory, name,
                         description, applicableOccurrence, propertySets, representationMapList, elementTag,
                         typeName, GetPreDefinedType<Toolkit.IFCGasTerminalType>(instance, ifcEnumType).ToString());
                  }
               case IFCExportType.IfcCableCarrierFittingType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCCableCarrierFittingType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcCableCarrierSegmentType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCCableCarrierSegmentType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcCableSegmentType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCCableSegmentType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcChillerType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCChillerType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcCoilType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCCoilType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcCompressorType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCCompressorType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcCondenserType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCCondenserType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcControllerType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCControllerType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcCooledBeamType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCCooledBeamType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcCoolingTowerType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCCoolingTowerType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcDamperType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCDamperType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcDistributionChamberElementType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCDistributionChamberElementType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcDuctFittingType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCDuctFittingType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcDuctSegmentType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCDuctSegmentType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcDuctSilencerType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCDuctSilencerType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcElectricApplianceType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCElectricApplianceType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcElectricFlowStorageDeviceType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCElectricFlowStorageDeviceType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcElectricGeneratorType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCElectricGeneratorType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcElectricHeaterType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCElectricHeaterType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcElectricMotorType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCElectricMotorType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcElectricTimeControlType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCElectricTimeControlType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcEvaporativeCoolerType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCEvaporativeCoolerType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcEvaporatorType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCEvaporatorType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcFanType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCFanType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcFilterType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCFilterType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcFireSuppressionTerminalType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCFireSuppressionTerminalType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcFlowInstrumentType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCFlowInstrumentType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcFlowMeterType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCFlowMeterType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcHeatExchangerType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCHeatExchangerType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcHumidifierType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCHumidifierType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcJunctionBoxType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCJunctionBoxType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcLampType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCLampType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcLightFixtureType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCLightFixtureType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcMotorConnectionType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCMotorConnectionType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcOutletType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCOutletType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcPipeFittingType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCPipeFittingType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcPipeSegmentType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCPipeSegmentType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcProtectiveDeviceType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCProtectiveDeviceType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcPumpType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCPumpType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcSanitaryTerminalType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCSanitaryTerminalType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcSensorType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCSensorType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcSpaceHeaterType:
                  {
                     // Backwards compatibility with IFC2x3.
                     return IFCInstanceExporter.CreateGenericIFCType(Common.Enums.IFCEntityType.IfcElectricHeaterType, file, guid, ownerHistory, name,
                         description, applicableOccurrence, propertySets, representationMapList, elementTag,
                         typeName, GetPreDefinedType<Toolkit.IFCElectricHeaterType>(instance, ifcEnumType).ToString());
                  }
               case IFCExportType.IfcStackTerminalType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCStackTerminalType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcSwitchingDeviceType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCSwitchingDeviceType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcTankType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCTankType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcTransformerType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCTransformerType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcTubeBundleType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCTubeBundleType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcUnitaryEquipmentType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCUnitaryEquipmentType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcValveType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCValveType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcWasteTerminalType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCWasteTerminalType>(instance, ifcEnumType).ToString());

               // Non MEP types
               case IFCExportType.IfcDiscreteAccessoryType:
                  return IFCInstanceExporter.CreateDiscreteAccessoryType(file, guid, ownerHistory, name,
                     description, applicableOccurrence, propertySets, representationMapList, elementTag,
                     typeName);
               case IFCExportType.IfcFastenerType:
                  return IFCInstanceExporter.CreateFastenerType(file, guid, ownerHistory, name,
                     description, applicableOccurrence, propertySets, representationMapList, elementTag,
                     typeName);
               case IFCExportType.IfcFurnishingElement:
                  return IFCInstanceExporter.CreateFurnishingElementType(file, guid, ownerHistory, name,
                     description, applicableOccurrence, propertySets, representationMapList, elementTag,
                     typeName);
               case IFCExportType.IfcFurnitureType:
                  return IFCInstanceExporter.CreateFurnitureType(file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetAssemblyPlace(instance, ifcEnumType), null);
               case IFCExportType.IfcMechanicalFastenerType:
                  return IFCInstanceExporter.CreateMechanicalFastenerType(file, guid, ownerHistory, name,
                     description, applicableOccurrence, propertySets, representationMapList, elementTag,
                     typeName);
               case IFCExportType.IfcMemberType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCMemberType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcPlateType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCPlateType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcTransportElementType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCTransportElementType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcRailingType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCRailingType>(instance, ifcEnumType).ToString());
               case IFCExportType.IfcBuildingElementProxy:
               case IFCExportType.IfcBuildingElementProxyType:
                  return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, file, guid, ownerHistory, name,
                      description, applicableOccurrence, propertySets, representationMapList, elementTag,
                      typeName, GetPreDefinedType<Toolkit.IFCBuildingElementProxyType>(instance, ifcEnumType).ToString());
               default:
                  // NOTE: There is no type in IFC2x3 for IfcElectricDistributionPoint.
                  return null;
            }
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
             IsFlowControllerSubType(exportType) ||
             exportType == IFCExportType.IfcBuildingElementProxy ||
             exportType == IFCExportType.IfcBuildingElementProxyType);
      }

      //        #region MEP Type's PreDefinedType

      /// <summary>
      /// Generic check for the PreDefinedType string from either IfcExportAs with (.predefinedtype), or IfcExportType param, or legacy IfcType param
      /// </summary>
      /// <typeparam name="TEnum">The Enum to verify</typeparam>
      /// <param name="element"></param>
      /// <param name="ifcEnumTypeStr">Enum String if already obtained from IfcExportAs or IfcExportType</param>
      /// <returns>"NotDeined if the string is not defined as Enum</returns>
      public static TEnum GetPreDefinedType<TEnum>(Element element, string ifcEnumTypeStr) where TEnum : struct
      {
         TEnum enumValue;
         Enum.TryParse("NotDefined", true, out enumValue);

         string value = null;
         if ((ParameterUtil.GetStringValueFromElementOrSymbol(element, "IfcExportType", out value) == null) &&
             (ParameterUtil.GetStringValueFromElementOrSymbol(element, "IfcType", out value) == null))
            value = ifcEnumTypeStr;

         if (String.IsNullOrEmpty(value))
            return enumValue;

         // string newValue = NamingUtil.RemoveSpacesAndUnderscores(value);
         // Enum.TryParse(newValue, true, out enumValue);

         Enum.TryParse(value, true, out enumValue);
         return enumValue;
      }

      private static IFCAssemblyPlace GetAssemblyPlace(Element element, string ifcEnumType)
      {
         string value = null;
         if (ParameterUtil.GetStringValueFromElementOrSymbol(element, "IfcType", out value) == null)
            value = ifcEnumType;

         if (String.IsNullOrEmpty(value))
            return IFCAssemblyPlace.NotDefined;

         string newValue = NamingUtil.RemoveSpacesAndUnderscores(value);

         if (String.Compare(newValue, "SITE", true) == 0)
            return IFCAssemblyPlace.Site;
         if (String.Compare(newValue, "FACTORY", true) == 0)
            return IFCAssemblyPlace.Factory;

         return IFCAssemblyPlace.NotDefined;
      }

      /// <summary>
      /// Create a list of geometry objects to export from an initial list of solids and meshes, excluding invisible and not exported categories.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="solids">The list of solids, possibly empty.</param>
      /// <param name="meshes">The list of meshes, possibly empty.</param>
      /// <returns>The combined list of solids and meshes that are visible given category export settings and view visibility settings.</returns>
      public static List<GeometryObject> RemoveInvisibleSolidsAndMeshes(Document doc, ExporterIFC exporterIFC, IList<Solid> solids, IList<Mesh> meshes)
      {
         List<GeometryObject> geomObjectsIn = new List<GeometryObject>();
         geomObjectsIn.AddRange(solids);
         geomObjectsIn.AddRange(meshes);

         List<GeometryObject> geomObjectsOut = new List<GeometryObject>();

         View filterView = ExporterCacheManager.ExportOptionsCache.FilterViewForExport;

         foreach (GeometryObject obj in geomObjectsIn)
         {
            GraphicsStyle gStyle = doc.GetElement(obj.GraphicsStyleId) as GraphicsStyle;
            if (gStyle != null)
            {
               Category graphicsStyleCategory = gStyle.GraphicsStyleCategory;
               if (graphicsStyleCategory != null)
               {
                  if (!ElementFilteringUtil.IsCategoryVisible(graphicsStyleCategory, filterView))
                     continue;

                  ElementId catId = graphicsStyleCategory.Id;

                  string ifcClassName = ExporterUtil.GetIFCClassNameFromExportTable(exporterIFC, catId);
                  if (!string.IsNullOrEmpty(ifcClassName))
                  {
                     bool foundName = String.Compare(ifcClassName, "Default", true) != 0;
                     if (foundName)
                     {
                        IFCExportType exportType = ElementFilteringUtil.GetExportTypeFromClassName(ifcClassName);
                        if (exportType == IFCExportType.DontExport)
                           continue;
                     }
                  }
               }
            }
            geomObjectsOut.Add(obj);
         }

         return geomObjectsOut;
      }
   }
}
