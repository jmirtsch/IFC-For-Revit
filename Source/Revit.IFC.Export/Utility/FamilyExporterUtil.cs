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
using System.Reflection;
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
         return (exportType >= IFCExportType.IfcActuator && exportType <= IFCExportType.IfcUnitaryControlElementType);
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
            exportType <= IFCExportType.IfcInterceptorType);
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
         return (exportType >= IFCExportType.IfcAirToAirHeatRecovery &&
         exportType <= IFCExportType.IfcUnitaryEquipmentType) && (exportType != IFCExportType.IfcSpaceHeaterType && exportType != IFCExportType.IfcSpaceHeater);
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
         return (exportType >= IFCExportType.IfcCableCarrierFitting &&
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
         return (exportType >= IFCExportType.IfcCompressor &&
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
         return (exportType >= IFCExportType.IfcCableCarrierSegment &&
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
         return (exportType >= IFCExportType.IfcElectricFlowStorageDevice &&
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
         return (exportType >= IFCExportType.IfcAirTerminal && exportType <= IFCExportType.IfcWasteTerminalType) ||
         (exportType == IFCExportType.IfcSpaceHeaterType || exportType == IFCExportType.IfcSpaceHeater);
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
         return (exportType >= IFCExportType.IfcDuctSilencer &&
         exportType <= IFCExportType.IfcInterceptorType);
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
         return (exportType >= IFCExportType.IfcAirTerminalBox &&
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
   string instanceGUID, IFCAnyHandle ownerHistory, IFCAnyHandle productRepresentation,
   string ifcEnumType, IFCAnyHandle overrideLocalPlacement)
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
            case IFCExportType.IfcBeamType:
               {
                  string preDefinedType = string.IsNullOrWhiteSpace(ifcEnumType) ? "BEAM" : ifcEnumType;
                  instanceHandle = IFCInstanceExporter.CreateBeam(exporterIFC, familyInstance, instanceGUID, ownerHistory,
                      localPlacementToUse, productRepresentation, preDefinedType);
                  break;
               }
            case IFCExportType.IfcColumn:
            case IFCExportType.IfcColumnType:
               {
                  string preDefinedType = string.IsNullOrWhiteSpace(ifcEnumType) ? "COLUMN" : ifcEnumType;
                  instanceHandle = IFCInstanceExporter.CreateColumn(exporterIFC, familyInstance, instanceGUID, ownerHistory,
                     localPlacementToUse, productRepresentation, preDefinedType);
                  break;
               }
            case IFCExportType.IfcCurtainWall:
            case IFCExportType.IfcCurtainWallType:
               {
                  instanceHandle = IFCInstanceExporter.CreateCurtainWall(exporterIFC, familyInstance, instanceGUID, ownerHistory,
                     localPlacementToUse, productRepresentation);
                  break;
               }
            case IFCExportType.IfcMember:
            case IFCExportType.IfcMemberType:
               {
                  string preDefinedType = string.IsNullOrWhiteSpace(ifcEnumType) ? "BRACE" : ifcEnumType;
                  instanceHandle = IFCInstanceExporter.CreateMember(exporterIFC, familyInstance, instanceGUID, ownerHistory,
                     localPlacementToUse, productRepresentation, preDefinedType);

                  // Register the members's IFC handle for later use by truss export.
                  ExporterCacheManager.ElementToHandleCache.Register(familyInstance.Id, instanceHandle);
                  break;
               }
            case IFCExportType.IfcPlate:
            case IFCExportType.IfcPlateType:
               {
                  IFCAnyHandle localPlacement = localPlacementToUse;
                  if (overrideLocalPlacement != null)
                  {
                     isChildInContainer = true;
                     localPlacement = overrideLocalPlacement;
                  }

                  string preDefinedType = string.IsNullOrWhiteSpace(ifcEnumType) ? "NOTDEFINED" : ifcEnumType;
                  instanceHandle = IFCInstanceExporter.CreatePlate(exporterIFC, familyInstance, instanceGUID, ownerHistory,
                      localPlacement, productRepresentation, preDefinedType);
                  break;
               }
            case IFCExportType.IfcDiscreteAccessory:
            case IFCExportType.IfcDiscreteAccessoryType:
               {
                  instanceHandle = IFCInstanceExporter.CreateDiscreteAccessory(exporterIFC, familyInstance, instanceGUID, ownerHistory,
                     localPlacementToUse, productRepresentation);
                  break;
               }
            case IFCExportType.IfcDistributionControlElement:
            case IFCExportType.IfcDistributionControlElementType:
               {
                  instanceHandle = IFCInstanceExporter.CreateDistributionControlElement(exporterIFC, familyInstance, instanceGUID, ownerHistory,
                     localPlacementToUse, productRepresentation, null);
                  break;
               }
            case IFCExportType.IfcDistributionFlowElement:
            case IFCExportType.IfcDistributionFlowElementType:
               {
                  instanceHandle = IFCInstanceExporter.CreateDistributionFlowElement(exporterIFC, familyInstance, instanceGUID, ownerHistory,
                     localPlacementToUse, productRepresentation);
                  break;
               }
            case IFCExportType.IfcDistributionChamberElement:
            case IFCExportType.IfcDistributionChamberElementType:
               {
                  instanceHandle = IFCInstanceExporter.CreateGenericIFCEntity(Common.Enums.IFCEntityType.IfcDistributionChamberElement, exporterIFC, familyInstance, instanceGUID, ownerHistory,
                      localPlacementToUse, productRepresentation);
                  break;
               }
            case IFCExportType.IfcFastener:
            case IFCExportType.IfcFastenerType:
               {
                  instanceHandle = IFCInstanceExporter.CreateFastener(exporterIFC, familyInstance, instanceGUID, ownerHistory,
                     localPlacementToUse, productRepresentation);
                  break;
               }
            case IFCExportType.IfcMechanicalFastener:
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

                  instanceHandle = IFCInstanceExporter.CreateMechanicalFastener(exporterIFC, familyInstance, instanceGUID, ownerHistory,
                     localPlacementToUse, productRepresentation, nominalDiameter, nominalLength, preDefinedType);
                  break;
               }
            case IFCExportType.IfcRailing:
            case IFCExportType.IfcRailingType:
               {
                  string strEnumType;
                  IFCExportType exportAs = ExporterUtil.GetExportType(exporterIFC, familyInstance, out strEnumType);
                  if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
                  {
                     instanceHandle = IFCInstanceExporter.CreateRailing(exporterIFC, familyInstance, instanceGUID, ownerHistory,
                         localPlacementToUse, productRepresentation, GetPreDefinedType<Toolkit.IFC4.IFCRailingType>(familyInstance, strEnumType).ToString());
                  }
                  else
                  {
                     instanceHandle = IFCInstanceExporter.CreateRailing(exporterIFC, familyInstance, instanceGUID, ownerHistory,
                         localPlacementToUse, productRepresentation, GetPreDefinedType<Toolkit.IFCRailingType>(familyInstance, strEnumType).ToString());
                  }
                  break;
               }
            case IFCExportType.IfcSpace:
               {
                  IFCInternalOrExternal internalOrExternal = CategoryUtil.IsElementExternal(familyInstance) ? IFCInternalOrExternal.External : IFCInternalOrExternal.Internal;

                  instanceHandle = IFCInstanceExporter.CreateSpace(exporterIFC, familyInstance, instanceGUID, ownerHistory,
                      localPlacementToUse, productRepresentation, IFCElementComposition.Element, internalOrExternal, null);
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
                     //instanceHandle = IFCInstanceExporter.CreateBuildingElementProxy(file, instanceGUID, ownerHistory,
                     //    instanceName, instanceDescription, instanceObjectType, localPlacementToUse, productRepresentation, instanceTag,
                     //    Toolkit.IFC4.IFCBuildingElementProxyType.USERDEFINED.ToString());
                     Common.Enums.IFCEntityType exportEntity = Common.Enums.IFCEntityType.UnKnown;
                     if (Enum.TryParse(type.ToString(), out exportEntity))
                     {
                        instanceHandle = IFCInstanceExporter.CreateGenericIFCEntity(exportEntity, exporterIFC, familyInstance, instanceGUID, ownerHistory,
                           localPlacementToUse, productRepresentation);
                     }
                  }
                  else if ((type == IFCExportType.IfcPile) ||
                     (type == IFCExportType.IfcFurnishingElement) || IsFurnishingElementSubType(type) ||
                     (type == IFCExportType.IfcEnergyConversionDevice) || IsEnergyConversionDeviceSubType(type) ||
                     (type == IFCExportType.IfcFlowFitting) || IsFlowFittingSubType(type) ||
                     (type == IFCExportType.IfcFlowMovingDevice) || IsFlowMovingDeviceSubType(type) ||
                     (type == IFCExportType.IfcFlowSegment) || IsFlowSegmentSubType(type) ||
                     (type == IFCExportType.IfcFlowStorageDevice) || IsFlowStorageDeviceSubType(type) ||
                     (type == IFCExportType.IfcFlowTerminal) || IsFlowTerminalSubType(type) ||
                     (type == IFCExportType.IfcFlowTreatmentDevice) || IsFlowTreatmentDeviceSubType(type) ||
                     (type == IFCExportType.IfcFlowController) || IsFlowControllerSubType(type) ||
                     (type == IFCExportType.IfcDistributionFlowElement) || IsDistributionFlowElementSubType(type) ||
                     (type == IFCExportType.IfcDistributionControlElement) || IsDistributionControlElementSubType(type) ||
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
                        instanceHandle = IFCInstanceExporter.CreateGenericIFCEntity(exportEntity, exporterIFC, familyInstance, instanceGUID, ownerHistory,
                           localPlacementToUse, productRepresentation);
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
         HashSet<IFCAnyHandle> propertySets,
         IList<IFCAnyHandle> representationMapList,
         Element instance,
         ElementType symbol)
      {
         IFCFile file = exporterIFC.GetFile();
         IFCAnyHandle typeHandle = null;


         try
         {
            // Skip export type object that does not have associated IfcTypeObject
            if (type != IFCExportType.IfcSite && type != IFCExportType.IfcBuildingStorey && type != IFCExportType.IfcSystem
                     && type != IFCExportType.IfcZone && type != IFCExportType.IfcGroup && type != IFCExportType.IfcGrid)
            {
               string elemIdToUse = null;
               switch (type)
               {
                  case IFCExportType.IfcFurnitureType:
                  case IFCExportType.IfcFurniture:
                  case IFCExportType.IfcMemberType:
                  case IFCExportType.IfcMember:
                  case IFCExportType.IfcPlateType:
                  case IFCExportType.IfcPlate:

                     {
                        elemIdToUse = NamingUtil.GetTagOverride(instance, NamingUtil.CreateIFCElementId(instance));
                        break;
                     }
               }

               typeHandle = ExportGenericTypeBase(file, type, ifcEnumType, propertySets, representationMapList, instance, symbol);
               if (!string.IsNullOrEmpty(elemIdToUse))
                  IFCAnyHandleUtil.SetAttribute(typeHandle, "Tag", elemIdToUse);
            }
         }
         catch
         {
         }

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
         IFCExportType originalType,
         string ifcEnumType,
         HashSet<IFCAnyHandle> propertySets,
         IList<IFCAnyHandle> representationMapList,
         Element instance,
         ElementType symbol)
      {
         Revit.IFC.Common.Enums.IFCEntityType IFCTypeEntity;
         string typeAsString = originalType.ToString();

         // We'll accept the IFCExportType with or without "Type", but we'll append "Type" if we don't find it.
         if (string.Compare(typeAsString.Substring(typeAsString.Length - 4), "Type", true) != 0)
            typeAsString += "Type";

         if (!Enum.TryParse(typeAsString, out IFCTypeEntity))
            return null;    // The export type is unknown IFC type entity

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            // TODO: Create a routine that does this mapping automatically.
            switch (IFCTypeEntity)
            {
               case Common.Enums.IFCEntityType.IfcGasTerminalType:
                  IFCTypeEntity = Common.Enums.IFCEntityType.IfcBurnerType;
                  typeAsString = IFCExportType.IfcBurnerType.ToString();
                  break;
               case Common.Enums.IFCEntityType.IfcElectricHeaterType:
                  IFCTypeEntity = Common.Enums.IFCEntityType.IfcSpaceHeaterType;
                  typeAsString = IFCExportType.IfcSpaceHeaterType.ToString();
                  break;
            }
         }
         else
         {
            // TODO: Create a routine that does this mapping automatically.
            switch (IFCTypeEntity)
            {
               case Common.Enums.IFCEntityType.IfcBurnerType:
                  IFCTypeEntity = Common.Enums.IFCEntityType.IfcGasTerminalType;
                  typeAsString = IFCExportType.IfcGasTerminalType.ToString();
                  break;
               case Common.Enums.IFCEntityType.IfcSpaceHeaterType:
                  IFCTypeEntity = Common.Enums.IFCEntityType.IfcElectricHeaterType;
                  typeAsString = IFCExportType.IfcElectricHeaterType.ToString();
                  break;
               case Common.Enums.IFCEntityType.IfcDoorType:
                  IFCTypeEntity = Common.Enums.IFCEntityType.IfcDoorStyle;
                  break;
               case Common.Enums.IFCEntityType.IfcWindowType:
                  IFCTypeEntity = Common.Enums.IFCEntityType.IfcWindowStyle;
                  break;
            }
         }
         switch (IFCTypeEntity) //Use SuperType if Abstract, not all listed yet.
         {
            case Common.Enums.IFCEntityType.IfcEnergyConversionDeviceType:
            case Common.Enums.IFCEntityType.IfcFlowControllerType:
            case Common.Enums.IFCEntityType.IfcFlowFittingType:
            case Common.Enums.IFCEntityType.IfcFlowMovingDeviceType:
            case Common.Enums.IFCEntityType.IfcFlowSegmentType:
            case Common.Enums.IFCEntityType.IfcFlowStorageDeviceType:
            case Common.Enums.IFCEntityType.IfcFlowTerminalType:
            case Common.Enums.IFCEntityType.IfcFlowTreatmentDeviceType:
               IFCTypeEntity = Common.Enums.IFCEntityType.IfcDistributionElementType;
               typeAsString = "IfcDistributionElementType";
               break;
         }
         string[] typeStr = typeAsString.Split('.');
         string desiredTypeBase = "Revit.IFC.Export.Toolkit.";
         string desiredTypeBaseExtra = ExporterCacheManager.ExportOptionsCache.ExportAs4 ? "IFC4." : string.Empty;
         string desiredType = desiredTypeBase + desiredTypeBaseExtra + typeStr[typeStr.Length - 1];

         object enumValue = null;

         {
            Type theEnumType = null;
            try
            {
               // Not all entity types have enum values before IFC4.
               theEnumType = Type.GetType(desiredType, true, true);
            }
            catch
            {
               theEnumType = null;
            }

            if (theEnumType != null)
            {
               try
               {
                  // Not all entity types have "NotDefined" as an option.
                  enumValue = Enum.Parse(theEnumType, "NotDefined", true);
               }
               catch
               {
                  enumValue = null;
               }
            }

            try
            {
               string value = null;
               if ((ParameterUtil.GetStringValueFromElementOrSymbol(instance, "IfcExportType", out value) == null) &&
                     (ParameterUtil.GetStringValueFromElementOrSymbol(instance, "IfcType", out value) == null))
                  value = ifcEnumType;

               if (theEnumType != null && !string.IsNullOrEmpty(value))
               {
                  object enumValuePar = Enum.Parse(theEnumType, value, true);
                  enumValue = enumValuePar;
               }
            }
            catch
            {
               enumValue = null;
            }
         }

         string enumValueAsString = (enumValue == null) ? null : enumValue.ToString();
         return IFCInstanceExporter.CreateGenericIFCType(IFCTypeEntity, symbol, file, propertySets, representationMapList, enumValueAsString);
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