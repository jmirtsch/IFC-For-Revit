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
using System.Linq;
using Autodesk.Revit.DB;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Export.Exporter.PropertySet.Calculators;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Initializes user defined parameters and quantities.
   /// </summary>
   class ExporterInitializer
   {
      /// <summary>
      /// Initializes property sets.
      /// </summary>
      /// <param name="propertySetsToExport">Existing functions to call for property set initialization.</param>
      public static void InitPropertySets(Exporter.PropertySetsToExport propertySetsToExport)
      {
         ParameterCache cache = ExporterCacheManager.ParameterCache;


         if (ExporterCacheManager.ExportOptionsCache.PropertySetOptions.ExportIFCCommon)
         {
            if (propertySetsToExport == null)
               propertySetsToExport = InitCommonPropertySets;
            else
               propertySetsToExport += InitCommonPropertySets;
         }

         if (ExporterCacheManager.ExportOptionsCache.PropertySetOptions.ExportSchedulesAsPsets)
         {
            if (propertySetsToExport == null)
               propertySetsToExport = InitCustomPropertySets;
            else
               propertySetsToExport += InitCustomPropertySets;
         }

         if (ExporterCacheManager.ExportOptionsCache.PropertySetOptions.ExportUserDefinedPsets)
         {
            if (propertySetsToExport == null)
               propertySetsToExport = InitUserDefinedPropertySets;
            else
               propertySetsToExport += InitUserDefinedPropertySets;
         }

         if (ExporterCacheManager.ExportOptionsCache.ExportAsCOBIE)
         {
            if (propertySetsToExport == null)
               propertySetsToExport = InitCOBIEPropertySets;
            else
               propertySetsToExport += InitCOBIEPropertySets;
         }

         if (propertySetsToExport != null)
            propertySetsToExport(cache.PropertySets);
      }

      /// <summary>
      /// Initializes quantities.
      /// </summary>
      /// <param name="fileVersion">The IFC file version.</param>
      /// <param name="exportBaseQuantities">True if export base quantities.</param>
      public static void InitQuantities(Exporter.QuantitiesToExport quantitiesToExport, bool exportBaseQuantities)
      {
         ParameterCache cache = ExporterCacheManager.ParameterCache;

         if (exportBaseQuantities)
         {
            if (quantitiesToExport == null)
               quantitiesToExport = InitBaseQuantities;
            else
               quantitiesToExport += InitBaseQuantities;
         }

         if (ExporterCacheManager.ExportOptionsCache.ExportAsCOBIE)
         {
            if (quantitiesToExport == null)
               quantitiesToExport = InitCOBIEQuantities;
            else
               quantitiesToExport += InitCOBIEQuantities;
         }

         if (quantitiesToExport != null)
            quantitiesToExport(cache.Quantities);
      }

      // Properties

      private static ISet<IFCEntityType> GetListOfRelatedEntities(IFCEntityType entityType)
      {
         // This is currently only for extending IfcElementType for schemas before IFC4, but could be expanded in the future.
         // TODO: add types for elements that didn't have types in IFC2x3 for IFC4 support.
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
            return null;

         // Check IfcElementType and its parent types.
         if (entityType == IFCEntityType.IfcElementType ||
            entityType == IFCEntityType.IfcTypeProduct ||
            entityType == IFCEntityType.IfcTypeObject)
         {
            ISet<IFCEntityType> relatedEntities = new HashSet<IFCEntityType>();
            relatedEntities.Add(IFCEntityType.IfcFooting);
            relatedEntities.Add(IFCEntityType.IfcPile);
            relatedEntities.Add(IFCEntityType.IfcRamp);
            relatedEntities.Add(IFCEntityType.IfcRoof);
            relatedEntities.Add(IFCEntityType.IfcStair);
            return relatedEntities;
         }

         return null;
      }

      /// <summary>
      /// Initialize user-defined property sets (from external file)
      /// </summary>
      /// <param name="propertySets">List of Psets</param>
      /// <param name="fileVersion">file version - (not used)</param>
      private static void InitUserDefinedPropertySets(IList<IList<PropertySetDescription>> propertySets)
      {
         Document document = ExporterCacheManager.Document;
         IList<PropertySetDescription> userDefinedPropertySets = new List<PropertySetDescription>();

         // get the Pset definitions (using the same file as PropertyMap)
         IList<PropertySetDef> userDefinedPsetDefs = new List<PropertySetDef>();
         userDefinedPsetDefs = PropertyMap.LoadUserDefinedPset();

         bool exportPre4 = (ExporterCacheManager.ExportOptionsCache.ExportAs2x2 || ExporterCacheManager.ExportOptionsCache.ExportAs2x3);

         // Loop through each definition and add the Pset entries into Cache
         foreach (PropertySetDef psetDef in userDefinedPsetDefs)
         {
            // Add Propertyset entry
            Description description = null;
            if (string.Compare(psetDef.propertySetName, "Attribute Mapping", true) == 0)
            {
               AttributeSetDescription attributeDescription = new AttributeSetDescription();
               ExporterCacheManager.AttributeCache.AddAttributeSet(attributeDescription);
               foreach(PropertyDef prop in psetDef.propertyDefs)
               {
                  PropertyType dataType;

                  if (!Enum.TryParse(prop.PropertyDataType, true, out dataType))
                     dataType = PropertyType.Text;           // force default to Text/string if the type does not match with any correct datatype

                  List<AttributeEntryMap> mappings = new List<AttributeEntryMap>();
                  foreach (PropertyParameterDefinition def in prop.ParameterDefinitions)
                  {
                     mappings.Add(new AttributeEntryMap(def.RevitParameterName, def.RevitBuiltInParameter));
                  }
                  AttributeEntry aSE = new AttributeEntry(prop.PropertyName, dataType, mappings);
                  attributeDescription.AddEntry(aSE);
               }
               description = attributeDescription;
            }
            else
            {
               PropertySetDescription userDefinedPropertySet = new PropertySetDescription();
               description = userDefinedPropertySet;
               userDefinedPropertySet.Name = psetDef.propertySetName;

               foreach (PropertyDef prop in psetDef.propertyDefs)
               {
                  PropertyType dataType;

                  if (!Enum.TryParse(prop.PropertyDataType, true, out dataType))
                     dataType = PropertyType.Text;           // force default to Text/string if the type does not match with any correct datatype
                  List<PropertySetEntryMap> mappings = new List<PropertySetEntryMap>();
                  foreach (PropertyParameterDefinition def in prop.ParameterDefinitions)
                  {
                     mappings.Add(new PropertySetEntryMap(def.RevitParameterName, def.RevitBuiltInParameter));
                  }
                  PropertySetEntry pSE = new PropertySetEntry(dataType, prop.PropertyName, mappings);
                  //pSE.DefaultValue = prop.DefaultValue;
                  userDefinedPropertySet.AddEntry(pSE);
               }
               userDefinedPropertySets.Add(userDefinedPropertySet);
            }

            foreach (string elem in psetDef.applicableElements)
            {
               Common.Enums.IFCEntityType ifcEntity;
               if (Enum.TryParse(elem, out ifcEntity))
               {
                  if (exportPre4)
                  {
                     IFCEntityType originalEntity = ifcEntity;
                     IFCCompatibilityType.checkCompatibleType(originalEntity, out ifcEntity);
                  }

                  description.EntityTypes.Add(ifcEntity);
                  // This is intended mostly as a workaround in IFC2x3 for IfcElementType.  Not all elements have an associated type (e.g. IfcRoof),
                  // but we still want to be able to export type property sets for that element.  So we will manually add these extra types here without
                  // forcing the user to guess.  If this causes issues, we may come up with a different design.
                  ISet<IFCEntityType> relatedEntities = GetListOfRelatedEntities(ifcEntity);
                  if (relatedEntities != null)
                     description.EntityTypes.UnionWith(relatedEntities);
               }
            }
         }

         propertySets.Add(userDefinedPropertySets);
      }

      /// <summary>
      /// Initializes custom property sets from schedules.
      /// </summary>
      /// <param name="propertySets">List to store property sets.</param>
      /// <param name="fileVersion">The IFC file version.</param>
      private static void InitCustomPropertySets(IList<IList<PropertySetDescription>> propertySets)
      {
         Document document = ExporterCacheManager.Document;
         IList<PropertySetDescription> customPropertySets = new List<PropertySetDescription>();

         // Collect all ViewSchedules from the document to use as custom property sets.
         FilteredElementCollector viewScheduleElementCollector = new FilteredElementCollector(document);

         ElementFilter viewScheduleElementFilter = new ElementClassFilter(typeof(ViewSchedule));
         viewScheduleElementCollector.WherePasses(viewScheduleElementFilter);
         List<ViewSchedule> filteredSchedules = viewScheduleElementCollector.Cast<ViewSchedule>().ToList();

         int unnamedScheduleIndex = 1;

         string includePattern = "PSET|IFC|COMMON";

         if (ExporterCacheManager.ExportOptionsCache.PropertySetOptions.ExportSpecificSchedules)
         {
            var resultQuery =
                from viewSchedule in viewScheduleElementCollector
                where viewSchedule.Name != null &&
                System.Text.RegularExpressions.Regex.IsMatch(viewSchedule.Name, includePattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                select viewSchedule;
            filteredSchedules = resultQuery.Cast<ViewSchedule>().ToList();
         }

         foreach (ViewSchedule schedule in filteredSchedules)
         {
            // Since 2018, schedules can have shared parameters.  Allow schedules to be skipped if IfcExportAs is set to DontExport.
            if (ElementFilteringUtil.IsIFCExportAsSetToDontExport(schedule))
               continue;

            PropertySetDescription customPSet = new PropertySetDescription();

            string scheduleName = schedule.Name;
            if (string.IsNullOrWhiteSpace(scheduleName))
            {
               scheduleName = "Unnamed Schedule " + unnamedScheduleIndex;
               unnamedScheduleIndex++;
            }
            customPSet.Name = scheduleName;

            ScheduleDefinition definition = schedule.Definition;
            if (definition == null)
               continue;

            // The schedule will be responsible for determining which elements to actually export.
            customPSet.ViewScheduleId = schedule.Id;
            customPSet.EntityTypes.Add(IFCEntityType.IfcProduct);

            int fieldCount = definition.GetFieldCount();
            if (fieldCount == 0)
               continue;

            HashSet<ElementId> containedElementIds = new HashSet<ElementId>();
            FilteredElementCollector elementsInViewScheduleCollector = new FilteredElementCollector(document, schedule.Id);
            foreach (Element containedElement in elementsInViewScheduleCollector)
            {
               containedElementIds.Add(containedElement.Id);
            }
            ExporterCacheManager.ViewScheduleElementCache.Add(new KeyValuePair<ElementId, HashSet<ElementId>>(schedule.Id, containedElementIds));

            IDictionary<ElementId, Element> cachedElementTypes = new Dictionary<ElementId, Element>();

            for (int ii = 0; ii < fieldCount; ii++)
            {
               ScheduleField field = definition.GetField(ii);

               ScheduleFieldType fieldType = field.FieldType;
               if (fieldType != ScheduleFieldType.Instance && fieldType != ScheduleFieldType.ElementType)
                  continue;

               ElementId parameterId = field.ParameterId;
               if (parameterId == ElementId.InvalidElementId)
                  continue;

               // We use asBuiltInParameterId to get the parameter by id below.  We don't want to use it later, however, so
               // we store builtInParameterId only if it is a proper member of the enumeration.
               BuiltInParameter asBuiltInParameterId = (BuiltInParameter)parameterId.IntegerValue;
               BuiltInParameter builtInParameterId =
                   Enum.IsDefined(typeof(BuiltInParameter), asBuiltInParameterId) ? asBuiltInParameterId : BuiltInParameter.INVALID;

               Parameter containedElementParameter = null;

               // We could cache the actual elements when we store the element ids.  However, this would almost certainly take more
               // time than getting one of the first few elements in the collector.
               foreach (Element containedElement in elementsInViewScheduleCollector)
               {
                  if (fieldType == ScheduleFieldType.Instance)
                     containedElementParameter = containedElement.get_Parameter(asBuiltInParameterId);

                  // shared parameters can return ScheduleFieldType.Instance, even if they are type parameters, so take a look.
                  if (containedElementParameter == null)
                  {
                     ElementId containedElementTypeId = containedElement.GetTypeId();
                     Element containedElementType = null;
                     if (containedElementTypeId != ElementId.InvalidElementId)
                     {
                        if (!cachedElementTypes.TryGetValue(containedElementTypeId, out containedElementType))
                        {
                           containedElementType = document.GetElement(containedElementTypeId);
                           cachedElementTypes[containedElementTypeId] = containedElementType;
                        }
                     }
                     if (containedElementType != null)
                        containedElementParameter = containedElementType.get_Parameter(asBuiltInParameterId);
                  }

                  if (containedElementParameter != null)
                     break;
               }
               if (containedElementParameter == null)
                  continue;

               PropertySetEntry ifcPSE = PropertySetEntry.CreateParameterEntry(containedElementParameter, builtInParameterId);
               ifcPSE.PropertyName = field.ColumnHeading;
               customPSet.AddEntry(ifcPSE);
            }

            customPropertySets.Add(customPSet);
         }

         propertySets.Add(customPropertySets);
      }

      /// <summary>
      /// Initializes common property sets.
      /// </summary>
      /// <param name="propertySets">List to store property sets.</param>
      /// <param name="fileVersion">The IFC file version.</param>
      private static void InitCommonPropertySets(IList<IList<PropertySetDescription>> propertySets)
      {
         IList<PropertySetDescription> commonPropertySets = new List<PropertySetDescription>();

         // Building/Site property sets.
         InitPropertySetBuildingCommon(commonPropertySets);
         InitPropertySetSiteCommon(commonPropertySets);

         // Architectural property sets.
         InitPropertySetBuildingElementProxyCommon(commonPropertySets);
         InitPropertySetCoveringCommon(commonPropertySets);
         InitPropertySetCurtainWallCommon(commonPropertySets);
         InitPropertySetDoorCommon(commonPropertySets);
         InitPropertySetDoorWindowGlazingType(commonPropertySets);
         InitPropertySetDoorWindowShadingType(commonPropertySets);
         InitPropertySetLevelCommon(commonPropertySets);
         InitPropertySetRailingCommon(commonPropertySets);
         InitPropertySetRampCommon(commonPropertySets);
         InitPropertySetRampFlightCommon(commonPropertySets);
         InitPropertySetRoofCommon(commonPropertySets);
         InitPropertySetSlabCommon(commonPropertySets);
         InitPropertySetStairCommon(commonPropertySets);
         InitPropertySetStairFlightCommon(commonPropertySets);
         InitPropertySetWallCommon(commonPropertySets);
         InitPropertySetWindowCommon(commonPropertySets);

         // Space property sets.
         InitPropertySetSpaceCommon(commonPropertySets);
         InitPropertySetSpaceFireSafetyRequirements(commonPropertySets);
         InitPropertySetSpaceLightingRequirements(commonPropertySets);
         InitPropertySetSpaceThermalDesign(commonPropertySets);
         InitPropertySetSpaceThermalRequirements(commonPropertySets);
         InitPropertySetGSASpaceCategories(commonPropertySets);
         InitPropertySetSpaceOccupant(commonPropertySets);
         InitPropertySetSpaceOccupancyRequirements(commonPropertySets);
         InitPropertySetSpaceZones(commonPropertySets);

         // Structural property sets.
         InitPropertySetBeamCommon(commonPropertySets);
         InitPropertySetColumnCommon(commonPropertySets);
         InitPropertySetMemberCommon(commonPropertySets);
         InitPropertySetPlateCommon(commonPropertySets);

         // MEP property sets.
         InitPropertySetAirTerminalTypeCommon(commonPropertySets);
         InitPropertySetElectricalDeviceCommon(commonPropertySets);
         InitPropertySetLightFixtureTypeCommon(commonPropertySets);
         InitPropertySetProvisionForVoid(commonPropertySets);
         InitPropertySetSanitaryTerminalTypeBath(commonPropertySets);
         InitPropertySetSanitaryTerminalTypeShower(commonPropertySets);
         InitPropertySetSanitaryTerminalTypeSink(commonPropertySets);
         InitPropertySetSanitaryTerminalTypeToiletPan(commonPropertySets);
         InitPropertySetSanitaryTerminalTypeWashHandBasin(commonPropertySets);
         InitPropertySetSwitchingDeviceTypeCommon(commonPropertySets);
         InitPropertySetSwitchingDeviceTypeToggleSwitch(commonPropertySets);
         InitPropertySetZoneCommon(commonPropertySets);

         // Misc. property sets
         InitPropertySetManufacturerTypeInformation(commonPropertySets);

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
            InitPropertySetSpaceCoveringRequirements(commonPropertySets);
         else
         {
            InitPropertySetBuildingWaterStorage(commonPropertySets);

            // Structural property sets.
            InitPropertySetReinforcingBarBendingsBECCommon(commonPropertySets);
            InitPropertySetReinforcingBarBendingsBS8666Common(commonPropertySets);
            InitPropertySetReinforcingBarBendingsDIN135610Common(commonPropertySets);
            InitPropertySetReinforcingBarBendingsISOCD3766Common(commonPropertySets);

            // MEP property sets
            InitPropertySetDistributionFlowElementCommon(commonPropertySets);
            InitPropertySetElectricalCircuit(commonPropertySets);
            InitPropertySetFlowTerminalAirTerminal(commonPropertySets);

            // Energy Analysis property sets.
            InitPropertySetElementShading(commonPropertySets);
         }


         propertySets.Add(commonPropertySets);
      }

      /// <summary>
      /// Initializes manufacturer type information property sets for all IfcElements.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetManufacturerTypeInformation(IList<PropertySetDescription> commonPropertySets)
      {
         //property set Manufacturer Information
         PropertySetDescription propertySetManufacturer = new PropertySetDescription();
         propertySetManufacturer.Name = "Pset_ManufacturerTypeInformation";

         // sub type of IfcElement
         propertySetManufacturer.EntityTypes.Add(IFCEntityType.IfcElement);

         PropertySetEntry ifcPSE = new PropertySetEntry(PropertyType.Identifier,"ArticleNumber");
         propertySetManufacturer.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.Label,"ModelReference");
         propertySetManufacturer.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.Label,"ModelLabel");
         propertySetManufacturer.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.Label, "Manufacturer", BuiltInParameter.ALL_MODEL_MANUFACTURER);
         propertySetManufacturer.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.Label,"ProductionYear");
         propertySetManufacturer.AddEntry(ifcPSE);

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            propertySetManufacturer.AddEntry(new PropertySetEntry(PropertyType.Identifier,"GlobalTradeItemNumber"));
            propertySetManufacturer.AddEntry(PropertySetEntry.CreateEnumeratedValue("AssemblyPlace",
                PropertyType.Label, typeof(Toolkit.IFC4.PsetManufacturerTypeInformation_AssemblyPlace)));
         }

         commonPropertySets.Add(propertySetManufacturer);
      }

      /// <summary>
      /// Initializes common wall property sets.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetWallCommon(IList<PropertySetDescription> commonPropertySets)
      {
         //property set wall common
         PropertySetDescription propertySetWallCommon = new PropertySetDescription();
         propertySetWallCommon.Name = "Pset_WallCommon";
         propertySetWallCommon.SubElementIndex = (int)IFCCommonPSets.PSetWallCommon;

         propertySetWallCommon.EntityTypes.Add(IFCEntityType.IfcWall);

         PropertySetEntry ifcPSE = PropertySetEntryUtil.CreateReferenceEntry();
         propertySetWallCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateLoadBearingEntry(LoadBearingCalculator.Instance);
         propertySetWallCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateExtendToStructureEntry();
         propertySetWallCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateIsExternalEntry();
         propertySetWallCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateFireRatingEntry();
         propertySetWallCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateThermalTransmittanceEntry();
         propertySetWallCommon.AddEntry(ifcPSE);

         propertySetWallCommon.AddEntry(PropertySetEntryUtil.CreateAcousticRatingEntry());
         propertySetWallCommon.AddEntry(PropertySetEntryUtil.CreateSurfaceSpreadOfFlameEntry());
         propertySetWallCommon.AddEntry(PropertySetEntryUtil.CreateCombustibleEntry());
         propertySetWallCommon.AddEntry(PropertySetEntryUtil.CreateCompartmentationEntry());

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
            propertySetWallCommon.AddEntry(PropertySetEntryUtil.CreateStatusEntry());

         commonPropertySets.Add(propertySetWallCommon);
      }

      /// <summary>
      /// Initializes common curtain wall property sets.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetCurtainWallCommon(IList<PropertySetDescription> commonPropertySets)
      {
         //property set curtain wall common
         PropertySetDescription propertySetCurtainWallCommon = new PropertySetDescription();
         propertySetCurtainWallCommon.Name = "Pset_CurtainWallCommon";
         propertySetCurtainWallCommon.SubElementIndex = (int)IFCCommonPSets.PSetCurtainWallCommon;

         propertySetCurtainWallCommon.EntityTypes.Add(IFCEntityType.IfcCurtainWall);

         PropertySetEntry ifcPSE = PropertySetEntryUtil.CreateReferenceEntry();
         propertySetCurtainWallCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateIsExternalEntry();
         propertySetCurtainWallCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateFireRatingEntry();
         propertySetCurtainWallCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateThermalTransmittanceEntry();
         propertySetCurtainWallCommon.AddEntry(ifcPSE);

         propertySetCurtainWallCommon.AddEntry(PropertySetEntryUtil.CreateAcousticRatingEntry());
         propertySetCurtainWallCommon.AddEntry(PropertySetEntryUtil.CreateSurfaceSpreadOfFlameEntry());
         propertySetCurtainWallCommon.AddEntry(PropertySetEntryUtil.CreateCombustibleEntry());

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
            propertySetCurtainWallCommon.AddEntry(PropertySetEntryUtil.CreateStatusEntry());

         commonPropertySets.Add(propertySetCurtainWallCommon);
      }

      /// <summary>
      /// Initializes common covering property sets.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetCoveringCommon(IList<PropertySetDescription> commonPropertySets)
      {
         //property set covering common
         PropertySetDescription propertySetCoveringCommon = new PropertySetDescription();
         propertySetCoveringCommon.Name = "Pset_CoveringCommon";
         propertySetCoveringCommon.SubElementIndex = (int)IFCCommonPSets.PSetCoveringCommon;

         propertySetCoveringCommon.EntityTypes.Add(IFCEntityType.IfcCovering);

         PropertySetEntry ifcPSE = PropertySetEntryUtil.CreateReferenceEntry();
         propertySetCoveringCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateFireRatingEntry();
         propertySetCoveringCommon.AddEntry(ifcPSE);

         propertySetCoveringCommon.AddEntry(PropertySetEntryUtil.CreateAcousticRatingEntry());
         propertySetCoveringCommon.AddEntry(new PropertySetEntry(PropertyType.Label,"FlammabilityRating"));
         propertySetCoveringCommon.AddEntry(PropertySetEntryUtil.CreateSurfaceSpreadOfFlameEntry());
         propertySetCoveringCommon.AddEntry(PropertySetEntryUtil.CreateCombustibleEntry());


         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
            propertySetCoveringCommon.AddEntry(new PropertySetEntry(PropertyType.Label,"FragilityRating"));
         else
            propertySetCoveringCommon.AddEntry(new PropertySetEntry(PropertyType.Label,"Fragility"));

         ifcPSE = new PropertySetEntry(PropertyType.Text, "Finish", CoveringFinishCalculator.Instance);
         propertySetCoveringCommon.AddEntry(ifcPSE);


         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            propertySetCoveringCommon.AddEntry(PropertySetEntryUtil.CreateStatusEntry());
            propertySetCoveringCommon.AddEntry(PropertySetEntryUtil.CreateIsExternalEntry());
            propertySetCoveringCommon.AddEntry(PropertySetEntryUtil.CreateThermalTransmittanceEntry());
         }
         else
         {
			ifcPSE = new PropertySetEntry(PropertyType.PositiveLength, "TotalThickness", BuiltInParameter.CEILING_THICKNESS);
            propertySetCoveringCommon.AddEntry(ifcPSE);
         }

         commonPropertySets.Add(propertySetCoveringCommon);
      }

      /// <summary>
      /// Initializes common door property sets.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetDoorCommon(IList<PropertySetDescription> commonPropertySets)
      {
         //property set wall common
         PropertySetDescription propertySetDoorCommon = new PropertySetDescription();
         propertySetDoorCommon.Name = "Pset_DoorCommon";
         propertySetDoorCommon.SubElementIndex = (int)IFCCommonPSets.PSetDoorCommon;

         propertySetDoorCommon.EntityTypes.Add(IFCEntityType.IfcDoor);

         PropertySetEntry ifcPSE = PropertySetEntryUtil.CreateReferenceEntry();
         propertySetDoorCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateIsExternalEntry();
         propertySetDoorCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateFireRatingEntry();
         propertySetDoorCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateThermalTransmittanceEntry();
         propertySetDoorCommon.AddEntry(ifcPSE);

         propertySetDoorCommon.AddEntry(PropertySetEntryUtil.CreateAcousticRatingEntry());
         propertySetDoorCommon.AddEntry(new PropertySetEntry(PropertyType.Label,"SecurityRating"));
         propertySetDoorCommon.AddEntry(PropertySetEntryUtil.CreateHandicapAccessibleEntry());
         propertySetDoorCommon.AddEntry(new PropertySetEntry(PropertyType.Boolean,"FireExit"));
         propertySetDoorCommon.AddEntry(new PropertySetEntry(PropertyType.Boolean,"SelfClosing"));
         propertySetDoorCommon.AddEntry(new PropertySetEntry(PropertyType.Boolean,"SmokeStop"));
         propertySetDoorCommon.AddEntry(new PropertySetEntry(PropertyType.Real,"GlazingAreaFraction"));
         propertySetDoorCommon.AddEntry(new PropertySetEntry(PropertyType.VolumetricFlowRate,"Infiltration"));

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            propertySetDoorCommon.AddEntry(PropertySetEntryUtil.CreateStatusEntry());
            propertySetDoorCommon.AddEntry(new PropertySetEntry(PropertyType.Label,"DurabilityRating"));
            propertySetDoorCommon.AddEntry(new PropertySetEntry(PropertyType.Label,"HygrothermalRating"));
            propertySetDoorCommon.AddEntry(new PropertySetEntry(PropertyType.Boolean,"HasDrive"));
         }

         commonPropertySets.Add(propertySetDoorCommon);
      }

      /// <summary>
      /// Initializes a common door/window property set (Pset_DoorWindowGlazingType).
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetDoorWindowGlazingType(IList<PropertySetDescription> commonPropertySets)
      {
         //property set Pset_DoorWindowGlazingType
         PropertySetDescription propertySetDoorWindowGlazingType = new PropertySetDescription();
         propertySetDoorWindowGlazingType.Name = "Pset_DoorWindowGlazingType";
         propertySetDoorWindowGlazingType.SubElementIndex = (int)IFCCommonPSets.PSetDoorWindowGlazingType;

         propertySetDoorWindowGlazingType.EntityTypes.Add(IFCEntityType.IfcDoor);
         propertySetDoorWindowGlazingType.EntityTypes.Add(IFCEntityType.IfcWindow);

         propertySetDoorWindowGlazingType.AddEntry(new PropertySetEntry(PropertyType.Count,"GlassLayers"));
         propertySetDoorWindowGlazingType.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"GlassThickness1"));
         propertySetDoorWindowGlazingType.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"GlassThickness2"));
         propertySetDoorWindowGlazingType.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"GlassThickness3"));
         propertySetDoorWindowGlazingType.AddEntry(new PropertySetEntry(PropertyType.Label,"FillGas"));
         propertySetDoorWindowGlazingType.AddEntry(new PropertySetEntry(PropertyType.Label,"GlassColor"));
         propertySetDoorWindowGlazingType.AddEntry(new PropertySetEntry(PropertyType.Boolean,"IsTempered"));
         propertySetDoorWindowGlazingType.AddEntry(new PropertySetEntry(PropertyType.Boolean,"IsLaminated"));
         propertySetDoorWindowGlazingType.AddEntry(new PropertySetEntry(PropertyType.Boolean,"IsCoated"));
         propertySetDoorWindowGlazingType.AddEntry(new PropertySetEntry(PropertyType.Boolean,"IsWired"));

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            propertySetDoorWindowGlazingType.AddEntry(new PropertySetEntry(PropertyType.NormalisedRatio,"VisibleLightReflectance"));
            propertySetDoorWindowGlazingType.AddEntry(new PropertySetEntry(PropertyType.NormalisedRatio,"VisibleLightTransmittance"));
            propertySetDoorWindowGlazingType.AddEntry(new PropertySetEntry(PropertyType.NormalisedRatio,"SolarAbsorption"));
            propertySetDoorWindowGlazingType.AddEntry(new PropertySetEntry(PropertyType.NormalisedRatio,"SolarReflectance"));
            propertySetDoorWindowGlazingType.AddEntry(new PropertySetEntry(PropertyType.NormalisedRatio,"SolarTransmittance"));
            propertySetDoorWindowGlazingType.AddEntry(new PropertySetEntry(PropertyType.NormalisedRatio,"SolarHeatGainTransmittance"));
            propertySetDoorWindowGlazingType.AddEntry(new PropertySetEntry(PropertyType.NormalisedRatio,"ShadingCoefficient"));
         }
         else
         {
            propertySetDoorWindowGlazingType.AddEntry(new PropertySetEntry(PropertyType.PositiveRatio,"Translucency"));
            propertySetDoorWindowGlazingType.AddEntry(new PropertySetEntry(PropertyType.PositiveRatio,"Reflectivity"));
            propertySetDoorWindowGlazingType.AddEntry(new PropertySetEntry(PropertyType.PositiveRatio,"BeamRadiationTransmittance"));
            propertySetDoorWindowGlazingType.AddEntry(new PropertySetEntry(PropertyType.PositiveRatio,"SolarHeatGainTransmittance"));
         }

         propertySetDoorWindowGlazingType.AddEntry(new PropertySetEntry(PropertyType.ThermalTransmittance,"ThermalTransmittanceSummer"));
         propertySetDoorWindowGlazingType.AddEntry(new PropertySetEntry(PropertyType.ThermalTransmittance,"ThermalTransmittanceWinter"));

         commonPropertySets.Add(propertySetDoorWindowGlazingType);
      }

      /// <summary>
      /// Initializes a common door/window property set (Pset_DoorWindowShadingType)
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetDoorWindowShadingType(IList<PropertySetDescription> commonPropertySets)
      {
         //property set Pset_DoorWindowShadingType
         PropertySetDescription propertySetDoorWindowShadingType = new PropertySetDescription();
         propertySetDoorWindowShadingType.Name = "Pset_DoorWindowShadingType";
         propertySetDoorWindowShadingType.SubElementIndex = (int)IFCCommonPSets.PsetDoorWindowShadingType;

         propertySetDoorWindowShadingType.EntityTypes.Add(IFCEntityType.IfcDoor);
         propertySetDoorWindowShadingType.EntityTypes.Add(IFCEntityType.IfcWindow);

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            // Note: This conflicts with the property of the same name in Pset_DoorWindowGlazingType.
            propertySetDoorWindowShadingType.AddEntry(new PropertySetEntry(PropertyType.NormalisedRatio,"ShadingCoefficient"));
         }

         propertySetDoorWindowShadingType.AddEntry(new PropertySetEntry(PropertyType.PositiveRatio,"ExternalShadingCoefficient"));
         propertySetDoorWindowShadingType.AddEntry(new PropertySetEntry(PropertyType.PositiveRatio,"InternalShadingCoefficient"));
         propertySetDoorWindowShadingType.AddEntry(new PropertySetEntry(PropertyType.PositiveRatio,"InsetShadingCoefficient"));

         commonPropertySets.Add(propertySetDoorWindowShadingType);
      }

      /// <summary>
      /// Initializes common window property sets.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetWindowCommon(IList<PropertySetDescription> commonPropertySets)
      {
         //property set wall common
         PropertySetDescription propertySetWindowCommon = new PropertySetDescription();
         propertySetWindowCommon.Name = "Pset_WindowCommon";
         propertySetWindowCommon.SubElementIndex = (int)IFCCommonPSets.PSetWindowCommon;

         propertySetWindowCommon.EntityTypes.Add(IFCEntityType.IfcWindow);

         PropertySetEntry ifcPSE = PropertySetEntryUtil.CreateReferenceEntry();
         propertySetWindowCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateIsExternalEntry();
         propertySetWindowCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateFireRatingEntry();
         propertySetWindowCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateThermalTransmittanceEntry();
         propertySetWindowCommon.AddEntry(ifcPSE);

         propertySetWindowCommon.AddEntry(PropertySetEntryUtil.CreateAcousticRatingEntry());
         propertySetWindowCommon.AddEntry(new PropertySetEntry(PropertyType.Label,"SecurityRating"));

         propertySetWindowCommon.AddEntry(new PropertySetEntry(PropertyType.Boolean,"SmokeStop"));
         propertySetWindowCommon.AddEntry(new PropertySetEntry(PropertyType.Real,"GlazingAreaFraction"));
         propertySetWindowCommon.AddEntry(new PropertySetEntry(PropertyType.VolumetricFlowRate,"Infiltration"));

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            propertySetWindowCommon.AddEntry(PropertySetEntryUtil.CreateStatusEntry());
            propertySetWindowCommon.AddEntry(new PropertySetEntry(PropertyType.Boolean,"HasSillExternal"));
            propertySetWindowCommon.AddEntry(new PropertySetEntry(PropertyType.Boolean,"HasSillInternal"));
            propertySetWindowCommon.AddEntry(new PropertySetEntry(PropertyType.Boolean,"HasDrive"));
            propertySetWindowCommon.AddEntry(new PropertySetEntry(PropertyType.Boolean,"FireExit"));
         }

         commonPropertySets.Add(propertySetWindowCommon);
      }

		/// <summary>
		/// Initializes common LightFixtureType property sets.
		/// </summary>
		/// <param name="commonPropertySets">List to store property sets.</param>
		private static void InitPropertySetLightFixtureTypeCommon(IList<PropertySetDescription> commonPropertySets)
		{
			//property beam common
			PropertySetDescription propertySetLightFixtureTypeCommon = new PropertySetDescription();
			propertySetLightFixtureTypeCommon.Name = "Pset_LightFixtureTypeCommon";
			propertySetLightFixtureTypeCommon.SubElementIndex = (int)IFCCommonPSets.PSetLightFixtureTypeCommon;

			propertySetLightFixtureTypeCommon.EntityTypes.Add(IFCEntityType.IfcLightFixtureType);

			PropertySetEntry ifcPSE = new PropertySetEntry(PropertyType.Integer,"NumberOfSources");
			propertySetLightFixtureTypeCommon.AddEntry(ifcPSE);

			ifcPSE = new PropertySetEntry(PropertyType.Power, "TotalWattage", BuiltInParameter.LIGHTING_FIXTURE_WATTAGE);
         propertySetLightFixtureTypeCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntry.CreateEnumeratedValue("LightFixtureMountingType", PropertyType.Label,
             typeof(PSetLightFixtureTypeCommon_LightFixtureMountingType));
         propertySetLightFixtureTypeCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntry.CreateEnumeratedValue("LightFixturePlacingType", PropertyType.Label,
             typeof(PSetLightFixtureTypeCommon_LightFixturePlacingType));
         propertySetLightFixtureTypeCommon.AddEntry(ifcPSE);

         propertySetLightFixtureTypeCommon.AddEntry(new PropertySetEntry(PropertyType.Real,"MaintenanceFactor"));

         // The value below is incorrect.  Although it is specified in IFC2x3, it is a duplicate of Pset_ManufacturerTypeInformation,
         // where it is correctly labelled as IfcIdentifier.
         //propertySetLightFixtureTypeCommon.AddEntry(PropertySetEntry.CreateClassificationReference("ArticleNumber"));

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            propertySetLightFixtureTypeCommon.AddEntry(PropertySetEntryUtil.CreateReferenceEntry());
            propertySetLightFixtureTypeCommon.AddEntry(PropertySetEntryUtil.CreateStatusEntry());
            propertySetLightFixtureTypeCommon.AddEntry(new PropertySetEntry(PropertyType.Power,"MaximumPlenumSensibleLoad"));
            propertySetLightFixtureTypeCommon.AddEntry(new PropertySetEntry(PropertyType.Power,"MaximumSpaceSensibleLoad"));
            propertySetLightFixtureTypeCommon.AddEntry(new PropertySetEntry(PropertyType.PositiveRatio,"SensibleLoadToRadiant"));
         }
         else
            propertySetLightFixtureTypeCommon.AddEntry(new PropertySetEntry(PropertyType.Text,"ManufacturersSpecificInformation"));

         commonPropertySets.Add(propertySetLightFixtureTypeCommon);
      }

      /// <summary>
      /// Initializes the Pset_DistributionFlowElementCommon property set.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetDistributionFlowElementCommon(IList<PropertySetDescription> commonPropertySets)
      {
         //property beam common
         PropertySetDescription propertyDistributionFlowElementCommon = new PropertySetDescription();
         propertyDistributionFlowElementCommon.Name = "Pset_DistributionFlowElementCommon";
         propertyDistributionFlowElementCommon.SubElementIndex = (int)IFCCommonPSets.PSetDistributionFlowElementCommon;

         propertyDistributionFlowElementCommon.EntityTypes.Add(IFCEntityType.IfcDistributionFlowElement);

         PropertySetEntry ifcPSE = PropertySetEntryUtil.CreateReferenceEntry();
         propertyDistributionFlowElementCommon.AddEntry(ifcPSE);

         commonPropertySets.Add(propertyDistributionFlowElementCommon);
      }

      /// <summary>
      /// Initializes the Pset_ElectricalCircuit property set.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetElectricalCircuit(IList<PropertySetDescription> commonPropertySets)
      {
         //property beam common
         PropertySetDescription propertySetElectricalCircuit = new PropertySetDescription();
         propertySetElectricalCircuit.Name = "Pset_ElectricalCircuit";

         propertySetElectricalCircuit.EntityTypes.Add(IFCEntityType.IfcElectricalCircuit);

         propertySetElectricalCircuit.AddEntry(new PropertySetEntry(PropertyType.PositiveRatio,"Diversity"));
         propertySetElectricalCircuit.AddEntry(new PropertySetEntry(PropertyType.Integer,"NumberOfPhases"));
         //propertySetElectricalCircuit.AddEntry(PropertySetEntry.CreateElectricVoltage("MaximumAllowedVoltageDrop"));
         //propertySetElectricalCircuit.AddEntry(PropertySetEntry.CreateElectricResistance("NetImpedance"));

         commonPropertySets.Add(propertySetElectricalCircuit);
      }

      /// <summary>
      /// Initializes the Pset_ElectricalDeviceCommon property set.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetElectricalDeviceCommon(IList<PropertySetDescription> commonPropertySets)
      {
         //property ElectricalDeviceCommon common
         PropertySetDescription propertySetElectricalDeviceCommon = new PropertySetDescription();
         propertySetElectricalDeviceCommon.Name = "Pset_ElectricalDeviceCommon";

         propertySetElectricalDeviceCommon.EntityTypes.Add(IFCEntityType.IfcDistributionElement);

         propertySetElectricalDeviceCommon.AddEntry(new PropertySetEntry(PropertyType.Frequency,"NominalFrequencyRange"));
         propertySetElectricalDeviceCommon.AddEntry(new PropertySetEntry(PropertyType.Integer,"NumberOfPoles"));
         propertySetElectricalDeviceCommon.AddEntry(new PropertySetEntry(PropertyType.Boolean,"HasProtectiveEarth"));
         propertySetElectricalDeviceCommon.AddEntry(new PropertySetEntry(PropertyType.Label,"IP_Code"));
         propertySetElectricalDeviceCommon.AddEntry(PropertySetEntry.CreateEnumeratedValue("InsulationStandardClass",
             PropertyType.Label, typeof(PSetElectricalDeviceCommon_InsulationStandardClass)));

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            propertySetElectricalDeviceCommon.AddEntry(new PropertySetEntry(PropertyType.ElectricalCurrent,"RatedCurrent"));
            propertySetElectricalDeviceCommon.AddEntry(new PropertySetEntry(PropertyType.ElectricalVoltage,"RatedVoltage"));
            propertySetElectricalDeviceCommon.AddEntry(new PropertySetEntry(PropertyType.Ratio,"PowerFactor"));
            propertySetElectricalDeviceCommon.AddEntry(PropertySetEntry.CreateEnumeratedValue("ConductorFunction", PropertyType.Label,
                typeof(Toolkit.IFC4.PsetElectricalDeviceCommon_ConductorFunction)));
         }
         else
         {
            propertySetElectricalDeviceCommon.AddEntry(new PropertySetEntry(PropertyType.ElectricalCurrent,"NominalCurrent"));
            propertySetElectricalDeviceCommon.AddEntry(new PropertySetEntry(PropertyType.ElectricalCurrent,"UsageCurrent"));
            propertySetElectricalDeviceCommon.AddEntry(new PropertySetEntry(PropertyType.ElectricalVoltage,"NominalVoltage"));
            propertySetElectricalDeviceCommon.AddEntry(new PropertySetEntry(PropertyType.Power,"ElectricalDeviceNominalPower"));
            propertySetElectricalDeviceCommon.AddEntry(new PropertySetEntry(PropertyType.PositivePlaneAngle,"PhaseAngle"));
            propertySetElectricalDeviceCommon.AddEntry(new PropertySetEntry(PropertyType.Identifier,"PhaseReference"));
         }

         commonPropertySets.Add(propertySetElectricalDeviceCommon);
      }

      /// <summary>
      /// Initializes the Pset_FlowTerminalAirTerminal property set.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetFlowTerminalAirTerminal(IList<PropertySetDescription> commonPropertySets)
      {
         //property beam common
         PropertySetDescription propertyFlowTerminalAirTerminal = new PropertySetDescription();
         propertyFlowTerminalAirTerminal.Name = "Pset_FlowTerminalAirTerminal";
         propertyFlowTerminalAirTerminal.SubElementIndex = (int)IFCCommonPSets.PSetFlowTerminalAirTerminal;

         propertyFlowTerminalAirTerminal.EntityTypes.Add(IFCEntityType.IfcFlowTerminal);

         propertyFlowTerminalAirTerminal.AddEntry(PropertySetEntry.CreateEnumeratedValue("AirflowType", PropertyType.Label,
             typeof(PSetFlowTerminalAirTerminal_AirTerminalAirflowType)));
         propertyFlowTerminalAirTerminal.AddEntry(PropertySetEntry.CreateEnumeratedValue("Location", PropertyType.Label,
             typeof(PSetFlowTerminalAirTerminal_AirTerminalLocation)));

         commonPropertySets.Add(propertyFlowTerminalAirTerminal);
      }

      /// <summary>
      /// Initializes the Pset_AirTerminalTypeCommon property set.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetAirTerminalTypeCommon(IList<PropertySetDescription> commonPropertySets)
      {
         //property air terminal type common
         PropertySetDescription propertyAirTerminalTypeCommon = new PropertySetDescription();
         propertyAirTerminalTypeCommon.Name = "Pset_AirTerminalTypeCommon";
         propertyAirTerminalTypeCommon.SubElementIndex = (int)IFCCommonPSets.PSetAirTerminalTypeCommon;

         propertyAirTerminalTypeCommon.EntityTypes.Add(IFCEntityType.IfcAirTerminalType);

         propertyAirTerminalTypeCommon.AddEntry(PropertySetEntry.CreateEnumeratedValue("Shape", PropertyType.Label,
             typeof(PSetAirTerminalTypeCommon_AirTerminalShape)));
         propertyAirTerminalTypeCommon.AddEntry(PropertySetEntry.CreateEnumeratedValue("FlowPattern", PropertyType.Label,
             typeof(PSetAirTerminalTypeCommon_AirTerminalFlowPattern)));
         propertyAirTerminalTypeCommon.AddEntry(new PropertySetEntry(PropertyType.VolumetricFlowRate,"AirFlowrateRange"));
         propertyAirTerminalTypeCommon.AddEntry(new PropertySetEntry(PropertyType.ThermodynamicTemperature,"TemperatureRange"));
         propertyAirTerminalTypeCommon.AddEntry(PropertySetEntry.CreateEnumeratedValue("DischargeDirection", PropertyType.Label,
             typeof(PSetAirTerminalTypeCommon_AirTerminalDischargeDirection)));
         propertyAirTerminalTypeCommon.AddEntry(new PropertySetEntry(PropertyType.Length,"ThrowLength"));
         propertyAirTerminalTypeCommon.AddEntry(new PropertySetEntry(PropertyType.Real,"AirDiffusionPerformanceIndex"));
         //propertyAirTerminalTypeCommon.AddEntry(PropertySetEntry.CreateMaterial("Material"));
         propertyAirTerminalTypeCommon.AddEntry(PropertySetEntry.CreateEnumeratedValue("FinishType", PropertyType.Label,
             typeof(PSetAirTerminalTypeCommon_AirTerminalFinishType)));
         propertyAirTerminalTypeCommon.AddEntry(new PropertySetEntry(PropertyType.Label,"FinishColor"));
         propertyAirTerminalTypeCommon.AddEntry(PropertySetEntry.CreateEnumeratedValue("MountingType", PropertyType.Label,
             typeof(PSetAirTerminalTypeCommon_AirTerminalMountingType)));
         propertyAirTerminalTypeCommon.AddEntry(PropertySetEntry.CreateEnumeratedValue("CoreType", PropertyType.Label,
             typeof(PSetAirTerminalTypeCommon_AirTerminalCoreType)));
         propertyAirTerminalTypeCommon.AddEntry(new PropertySetEntry(PropertyType.PlaneAngle,"CoreSetHorizontal"));
         propertyAirTerminalTypeCommon.AddEntry(new PropertySetEntry(PropertyType.PlaneAngle,"CoreSetVertical"));
         propertyAirTerminalTypeCommon.AddEntry(new PropertySetEntry(PropertyType.Boolean,"HasIntegralControl"));
         propertyAirTerminalTypeCommon.AddEntry(PropertySetEntry.CreateEnumeratedValue("FlowControlType", PropertyType.Label,
             typeof(PSetAirTerminalTypeCommon_AirTerminalFlowControlType)));
         propertyAirTerminalTypeCommon.AddEntry(new PropertySetEntry(PropertyType.Boolean,"HasSoundAttenuator"));
         propertyAirTerminalTypeCommon.AddEntry(new PropertySetEntry(PropertyType.Boolean,"HasThermalInsulation"));
         propertyAirTerminalTypeCommon.AddEntry(new PropertySetEntry(PropertyType.Area,"NeckArea"));
         propertyAirTerminalTypeCommon.AddEntry(new PropertySetEntry(PropertyType.Area,"EffectiveArea"));

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            PropertySetEntry ifcPSE = PropertySetEntryUtil.CreateReferenceEntry();
            propertyAirTerminalTypeCommon.AddEntry(ifcPSE);
            propertyAirTerminalTypeCommon.AddEntry(PropertySetEntryUtil.CreateStatusEntry());
            propertyAirTerminalTypeCommon.AddEntry(new PropertySetEntry(PropertyType.Label,"FaceType"));
            propertyAirTerminalTypeCommon.AddEntry(new PropertySetEntry(PropertyType.Length,"SlotWidth"));
            propertyAirTerminalTypeCommon.AddEntry(new PropertySetEntry(PropertyType.Length,"SlotLength"));
            propertyAirTerminalTypeCommon.AddEntry(new PropertySetEntry(PropertyType.Integer,"NumberOfSlots"));
            propertyAirTerminalTypeCommon.AddEntry(new PropertySetEntry(PropertyType.PositiveRatio,"AirFlowrateVersusFlowControlElement"));
         }
         //propertyAirTerminalTypeCommon.AddEntry(PropertySetEntry.CreateMass("Weight"));
         //AirFlowrateVersusFlowControlElement: IfcPropertyTableValue not supported.

         commonPropertySets.Add(propertyAirTerminalTypeCommon);
      }

      /// <summary>
      /// Initializes common beam property sets.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetBeamCommon(IList<PropertySetDescription> commonPropertySets)
      {
         //property beam common
         PropertySetDescription propertySetBeamCommon = new PropertySetDescription();
         propertySetBeamCommon.Name = "Pset_BeamCommon";
         propertySetBeamCommon.SubElementIndex = (int)IFCCommonPSets.PSetBeamCommon;

         propertySetBeamCommon.EntityTypes.Add(IFCEntityType.IfcBeam);

         PropertySetEntry ifcPSE = PropertySetEntryUtil.CreateReferenceEntry();
         propertySetBeamCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateLoadBearingEntry(BeamLoadBearingCalculator.Instance);
         propertySetBeamCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateIsExternalEntry();
         propertySetBeamCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateFireRatingEntry();
         propertySetBeamCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateSpanEntry(BeamMemberSpanCalculator.Instance);
         //ifcPSE.RevitBuiltInParameter = BuiltInParameter.INSTANCE_LENGTH_PARAM;
         propertySetBeamCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateSlopeEntry(SlopeCalculator.Instance);
         propertySetBeamCommon.AddEntry(ifcPSE);

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            propertySetBeamCommon.AddEntry(PropertySetEntryUtil.CreateStatusEntry());

            ifcPSE = PropertySetEntryUtil.CreateRollEntry(StructuralMemberRollCalculator.Instance);
            //ifcPSE.RevitBuiltInParameter = BuiltInParameter.STRUCTURAL_BEND_DIR_ANGLE;
            propertySetBeamCommon.AddEntry(ifcPSE);

            propertySetBeamCommon.AddEntry(PropertySetEntryUtil.CreateThermalTransmittanceEntry());
         }

         commonPropertySets.Add(propertySetBeamCommon);
      }

		/// <summary>
		/// Initializes common IfcMember property sets.
		/// </summary>
		/// <param name="commonPropertySets">List to store property sets.</param>
		/// <remarks>Reuses beam calculators for some values.</remarks>
		private static void InitPropertySetMemberCommon(IList<PropertySetDescription> commonPropertySets)
		{
			//property beam common
			PropertySetDescription propertySetMemberCommon = new PropertySetDescription();
			propertySetMemberCommon.Name = "Pset_MemberCommon";
			propertySetMemberCommon.SubElementIndex = (int)IFCCommonPSets.PSetMemberCommon;

			propertySetMemberCommon.EntityTypes.Add(IFCEntityType.IfcMember);

			PropertySetEntry ifcPSE = PropertySetEntryUtil.CreateReferenceEntry();
			propertySetMemberCommon.AddEntry(ifcPSE);

			ifcPSE = PropertySetEntryUtil.CreateLoadBearingEntry(BeamLoadBearingCalculator.Instance);
			propertySetMemberCommon.AddEntry(ifcPSE);

			ifcPSE = PropertySetEntryUtil.CreateIsExternalEntry();
			propertySetMemberCommon.AddEntry(ifcPSE);

			ifcPSE = PropertySetEntryUtil.CreateFireRatingEntry();
			propertySetMemberCommon.AddEntry(ifcPSE);

			ifcPSE = new PropertySetEntry(PropertyType.PositiveLength, "Span", BuiltInParameter.INSTANCE_LENGTH_PARAM, BeamMemberSpanCalculator.Instance);
         propertySetMemberCommon.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.PlaneAngle, "Slope", SlopeCalculator.Instance);
         propertySetMemberCommon.AddEntry(ifcPSE);

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            propertySetMemberCommon.AddEntry(PropertySetEntryUtil.CreateStatusEntry());

            ifcPSE = new PropertySetEntry(PropertyType.PlaneAngle, "Roll", BuiltInParameter.STRUCTURAL_BEND_DIR_ANGLE, StructuralMemberRollCalculator.Instance);
            propertySetMemberCommon.AddEntry(ifcPSE);

            propertySetMemberCommon.AddEntry(PropertySetEntryUtil.CreateThermalTransmittanceEntry());
         }

         commonPropertySets.Add(propertySetMemberCommon);
      }

      /// <summary>
      /// Initializes Pset_PlateCommon.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      /// <remarks>Reuses BeamLoadBearingCalculator for load bearing calculation.</remarks>
      private static void InitPropertySetPlateCommon(IList<PropertySetDescription> commonPropertySets)
      {
         PropertySetDescription propertySetPlateCommon = new PropertySetDescription();
         propertySetPlateCommon.Name = "Pset_PlateCommon";
         propertySetPlateCommon.SubElementIndex = (int)IFCCommonPSets.PSetPlateCommon;

         propertySetPlateCommon.EntityTypes.Add(IFCEntityType.IfcPlate);

         propertySetPlateCommon.AddEntry(PropertySetEntryUtil.CreateReferenceEntry());
         propertySetPlateCommon.AddEntry(PropertySetEntryUtil.CreateIsExternalEntry());
         propertySetPlateCommon.AddEntry(PropertySetEntryUtil.CreateLoadBearingEntry(BeamLoadBearingCalculator.Instance));
         propertySetPlateCommon.AddEntry(PropertySetEntryUtil.CreateAcousticRatingEntry());
         propertySetPlateCommon.AddEntry(PropertySetEntryUtil.CreateFireRatingEntry());
         propertySetPlateCommon.AddEntry(PropertySetEntryUtil.CreateThermalTransmittanceEntry());

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
            propertySetPlateCommon.AddEntry(PropertySetEntryUtil.CreateStatusEntry());

         commonPropertySets.Add(propertySetPlateCommon);
      }

      /// <summary>
      /// Initializes Pset_ReinforcingBarBendingsBECCommon.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetReinforcingBarBendingsBECCommon(IList<PropertySetDescription> commonPropertySets)
      {
         PropertySetDescription propertySetReinforcingBarCommon = new PropertySetDescription();
         propertySetReinforcingBarCommon.Name = "Pset_ReinforcingBarBendingsBECCommon";
         propertySetReinforcingBarCommon.SubElementIndex = (int)IFCCommonPSets.PSetBECCommon;

         propertySetReinforcingBarCommon.EntityTypes.Add(IFCEntityType.IfcReinforcingBar);

         propertySetReinforcingBarCommon.AddEntry(new PropertySetEntry(PropertyType.Label,"BECBarShapeCode"));
         for (char shapeParameterSuffix = 'a'; shapeParameterSuffix <= 'l'; shapeParameterSuffix++)
            propertySetReinforcingBarCommon.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"BECShapeParameter_" + shapeParameterSuffix.ToString()));
         propertySetReinforcingBarCommon.AddEntry(new PropertySetEntry(PropertyType.PlaneAngle,"BECBendingParameter_u"));
         propertySetReinforcingBarCommon.AddEntry(new PropertySetEntry(PropertyType.PlaneAngle,"BECBendingParameter_v"));
         propertySetReinforcingBarCommon.AddEntry(new PropertySetEntry(PropertyType.PlaneAngle,"BECBendingParameter_ul"));
         propertySetReinforcingBarCommon.AddEntry(new PropertySetEntry(PropertyType.PlaneAngle,"BECBendingParameter_vl"));
         propertySetReinforcingBarCommon.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"BECShapeAid_x"));
         propertySetReinforcingBarCommon.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"BECShapeAid_y"));
         propertySetReinforcingBarCommon.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"BECRollerDiameter"));

         commonPropertySets.Add(propertySetReinforcingBarCommon);
      }

      /// <summary>
      /// Initializes Pset_ReinforcingBarBendingsBS8666Common.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetReinforcingBarBendingsBS8666Common(IList<PropertySetDescription> commonPropertySets)
      {
         PropertySetDescription propertySetReinforcingBarCommon = new PropertySetDescription();
         propertySetReinforcingBarCommon.Name = "Pset_ReinforcingBarBendingsBS8666Common";
         propertySetReinforcingBarCommon.SubElementIndex = (int)IFCCommonPSets.PSetBS8666Common;

         propertySetReinforcingBarCommon.EntityTypes.Add(IFCEntityType.IfcReinforcingBar);

         propertySetReinforcingBarCommon.AddEntry(new PropertySetEntry(PropertyType.Label,"BS8666ShapeCode"));
         for (char shapeParameterSuffix = 'A'; shapeParameterSuffix <= 'E'; shapeParameterSuffix++)
            propertySetReinforcingBarCommon.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"BS8666ShapeParameter_" + shapeParameterSuffix.ToString()));
         propertySetReinforcingBarCommon.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"BS8666ShapeParameter_R"));

         commonPropertySets.Add(propertySetReinforcingBarCommon);
      }

      /// <summary>
      /// Initializes Pset_ReinforcingBarBendingsDIN135610Common.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetReinforcingBarBendingsDIN135610Common(IList<PropertySetDescription> commonPropertySets)
      {
         PropertySetDescription propertySetReinforcingBarCommon = new PropertySetDescription();
         propertySetReinforcingBarCommon.Name = "Pset_ReinforcingBarBendingsDIN135610Common";
         propertySetReinforcingBarCommon.SubElementIndex = (int)IFCCommonPSets.PSetDIN135610Common;

         propertySetReinforcingBarCommon.EntityTypes.Add(IFCEntityType.IfcReinforcingBar);

         propertySetReinforcingBarCommon.AddEntry(new PropertySetEntry(PropertyType.Label,"DIN135610ShapeCode"));
         for (char shapeParameterSuffix = 'a'; shapeParameterSuffix <= 'e'; shapeParameterSuffix++)
            propertySetReinforcingBarCommon.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"DIN135610ShapeParameter_" + shapeParameterSuffix.ToString()));
         propertySetReinforcingBarCommon.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"DIN135610ShapeParameter_z"));

         commonPropertySets.Add(propertySetReinforcingBarCommon);
      }

      /// <summary>
      /// Initializes Pset_ReinforcingBarBendingsISOCD3766Common.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetReinforcingBarBendingsISOCD3766Common(IList<PropertySetDescription> commonPropertySets)
      {
         PropertySetDescription propertySetReinforcingBarCommon = new PropertySetDescription();
         propertySetReinforcingBarCommon.Name = "Pset_ReinforcingBarBendingsISOCD3766Common";
         propertySetReinforcingBarCommon.SubElementIndex = (int)IFCCommonPSets.PSetISOCD3766Common;

         propertySetReinforcingBarCommon.EntityTypes.Add(IFCEntityType.IfcReinforcingBar);

         PropertySetEntry ifcPSE = new PropertySetEntry(PropertyType.Label,"ISOCD3766ShapeCode", ISOCD3766ShapeCodeCalculator.Instance);
         propertySetReinforcingBarCommon.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.PositiveLength, "ISOCD3766ShapeParameter_a", ISOCD3766ShapeParameterACalculator.Instance);
         propertySetReinforcingBarCommon.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.PositiveLength,"ISOCD3766ShapeParameter_b", ISOCD3766ShapeParameterBCalculator.Instance);
         propertySetReinforcingBarCommon.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.PositiveLength,"ISOCD3766ShapeParameter_c", ISOCD3766ShapeParameterCCalculator.Instance);
         propertySetReinforcingBarCommon.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.PositiveLength,"ISOCD3766ShapeParameter_d", ISOCD3766ShapeParameterDCalculator.Instance);
         propertySetReinforcingBarCommon.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.PositiveLength,"ISOCD3766ShapeParameter_e", ISOCD3766ShapeParameterECalculator.Instance);
         propertySetReinforcingBarCommon.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.PositiveLength,"ISOCD3766ShapeParameter_R", ISOCD3766BendingRadiusCalculator.Instance);
         propertySetReinforcingBarCommon.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.PlaneAngle,"ISOCD3766BendingStartHook", ISOCD3766BendingStartHookCalculator.Instance);
         propertySetReinforcingBarCommon.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.PlaneAngle,"ISOCD3766BendingEndHook", ISOCD3766BendingEndHookCalculator.Instance);
         propertySetReinforcingBarCommon.AddEntry(ifcPSE);

         commonPropertySets.Add(propertySetReinforcingBarCommon);
      }

      /// <summary>
      /// Initializes common column property sets.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetColumnCommon(IList<PropertySetDescription> commonPropertySets)
      {
         //property column common
         PropertySetDescription propertySetColumnCommon = new PropertySetDescription();
         propertySetColumnCommon.Name = "Pset_ColumnCommon";

         propertySetColumnCommon.EntityTypes.Add(IFCEntityType.IfcColumn);

         PropertySetEntry ifcPSE = PropertySetEntryUtil.CreateReferenceEntry();
         propertySetColumnCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateLoadBearingEntry(ColumnLoadBearingCalculator.Instance);
         propertySetColumnCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateIsExternalEntry();
         propertySetColumnCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateFireRatingEntry();
         propertySetColumnCommon.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.PlaneAngle, "Slope", SlopeCalculator.Instance);
         propertySetColumnCommon.AddEntry(ifcPSE);

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            propertySetColumnCommon.AddEntry(PropertySetEntryUtil.CreateStatusEntry());

            ifcPSE = new PropertySetEntry(PropertyType.PlaneAngle, "Roll", BuiltInParameter.STRUCTURAL_BEND_DIR_ANGLE, StructuralMemberRollCalculator.Instance);
            propertySetColumnCommon.AddEntry(ifcPSE);

            propertySetColumnCommon.AddEntry(PropertySetEntryUtil.CreateThermalTransmittanceEntry());
         }

         commonPropertySets.Add(propertySetColumnCommon);
      }

      /// <summary>
      /// Initializes common roof property sets.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetRoofCommon(IList<PropertySetDescription> commonPropertySets)
      {
         // Pset_RoofCommon
         PropertySetDescription propertySetRoofCommon = new PropertySetDescription();
         propertySetRoofCommon.Name = "Pset_RoofCommon";
         propertySetRoofCommon.SubElementIndex = (int)IFCCommonPSets.PSetRoofCommon;

         propertySetRoofCommon.EntityTypes.Add(IFCEntityType.IfcRoof);

         PropertySetEntry ifcPSE = PropertySetEntryUtil.CreateReferenceEntry();
         propertySetRoofCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateIsExternalEntry();
         propertySetRoofCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateFireRatingEntry();
         propertySetRoofCommon.AddEntry(ifcPSE);

         if (!ExporterCacheManager.ExportOptionsCache.ExportAs2x2)
         {
            ifcPSE = new PropertySetEntry(PropertyType.Area,"TotalArea", BuiltInParameter.HOST_AREA_COMPUTED);
            propertySetRoofCommon.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry(PropertyType.Area,"ProjectedArea", RoofProjectedAreaCalculator.Instance);
            propertySetRoofCommon.AddEntry(ifcPSE);

            if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
            {
               propertySetRoofCommon.AddEntry(PropertySetEntryUtil.CreateStatusEntry());
               propertySetRoofCommon.AddEntry(new PropertySetEntry(PropertyType.Label,"AcousticRating"));
               propertySetRoofCommon.AddEntry(PropertySetEntryUtil.CreateThermalTransmittanceEntry());
            }
         }

         commonPropertySets.Add(propertySetRoofCommon);
      }

      /// <summary>
      /// Initializes common slab property sets.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetSlabCommon(IList<PropertySetDescription> commonPropertySets)
      {
         // Pset_SlabCommon
         PropertySetDescription propertySetSlabCommon = new PropertySetDescription();
         propertySetSlabCommon.Name = "Pset_SlabCommon";
         propertySetSlabCommon.SubElementIndex = (int)IFCCommonPSets.PSetSlabCommon;

         propertySetSlabCommon.EntityTypes.Add(IFCEntityType.IfcSlab);

         PropertySetEntry ifcPSE = PropertySetEntryUtil.CreateReferenceEntry();
         propertySetSlabCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateIsExternalEntry();
         propertySetSlabCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateLoadBearingEntry(SlabLoadBearingCalculator.Instance); // always true
         propertySetSlabCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateFireRatingEntry();
         propertySetSlabCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateAcousticRatingEntry();
         propertySetSlabCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateSurfaceSpreadOfFlameEntry();
         propertySetSlabCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateCombustibleEntry();
         propertySetSlabCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateCompartmentationEntry();
         propertySetSlabCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateThermalTransmittanceEntry();
         propertySetSlabCommon.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.PlaneAngle,"PitchAngle", SlopeCalculator.Instance);
         propertySetSlabCommon.AddEntry(ifcPSE);

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
            propertySetSlabCommon.AddEntry(PropertySetEntryUtil.CreateStatusEntry());

         commonPropertySets.Add(propertySetSlabCommon);
      }

      /// <summary>
      /// Initializes common railing property sets.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetRailingCommon(IList<PropertySetDescription> commonPropertySets)
      {
         // Pset_RailingCommon
         PropertySetDescription propertySetRailingCommon = new PropertySetDescription();
         propertySetRailingCommon.Name = "Pset_RailingCommon";

         propertySetRailingCommon.EntityTypes.Add(IFCEntityType.IfcRailing);

         PropertySetEntry ifcPSE = PropertySetEntryUtil.CreateReferenceEntry();
         propertySetRailingCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateIsExternalEntry();
         propertySetRailingCommon.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.PositiveLength,"Height", RailingHeightCalculator.Instance);
         propertySetRailingCommon.AddEntry(ifcPSE);

         // Railing diameter not supported.

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
            propertySetRailingCommon.AddEntry(PropertySetEntryUtil.CreateStatusEntry());

         commonPropertySets.Add(propertySetRailingCommon);
      }

      /// <summary>
      /// Initializes common ramp property sets.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetRampCommon(IList<PropertySetDescription> commonPropertySets)
      {
         // Pset_RampCommon
         PropertySetDescription propertySetRampCommon = new PropertySetDescription();
         propertySetRampCommon.Name = "Pset_RampCommon";
         propertySetRampCommon.SubElementIndex = (int)IFCCommonPSets.PSetRampCommon;

         propertySetRampCommon.EntityTypes.Add(IFCEntityType.IfcRamp);

         PropertySetEntry ifcPSE = PropertySetEntryUtil.CreateReferenceEntry();
         propertySetRampCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateIsExternalEntry();
         propertySetRampCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateFireRatingEntry();
         propertySetRampCommon.AddEntry(ifcPSE);

         propertySetRampCommon.AddEntry(PropertySetEntryUtil.CreateHandicapAccessibleEntry());
         propertySetRampCommon.AddEntry(new PropertySetEntry(PropertyType.Boolean,"FireExit"));
         propertySetRampCommon.AddEntry(new PropertySetEntry(PropertyType.Boolean,"HasNonSkidSurface"));
         propertySetRampCommon.AddEntry(new PropertySetEntry(PropertyType.Real,"RequiredHeadroom"));
         propertySetRampCommon.AddEntry(new PropertySetEntry(PropertyType.Real,"RequiredSlope"));

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
            propertySetRampCommon.AddEntry(PropertySetEntryUtil.CreateStatusEntry());

         commonPropertySets.Add(propertySetRampCommon);
      }

      /// <summary>
      /// Initializes common stair flight property sets.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetStairFlightCommon(IList<PropertySetDescription> commonPropertySets)
      {
         // Pset_StairFlightCommon
         PropertySetDescription propertySetStairFlightCommon = new PropertySetDescription();
         propertySetStairFlightCommon.Name = "Pset_StairFlightCommon";
         // Add Calculator for SubElementIndex.

         propertySetStairFlightCommon.EntityTypes.Add(IFCEntityType.IfcStairFlight);

         PropertySetEntry ifcPSE = PropertySetEntryUtil.CreateReferenceEntry();
         propertySetStairFlightCommon.AddEntry(ifcPSE);

         PropertyCalculator stairRiserAndTreadsCalculator = StairRiserTreadsCalculator.Instance;
         ifcPSE = new PropertySetEntry(PropertyType.Count,"NumberOfRiser", stairRiserAndTreadsCalculator);
         propertySetStairFlightCommon.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.Count,"NumberOfTreads", stairRiserAndTreadsCalculator);
         propertySetStairFlightCommon.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.PositiveLength,"RiserHeight", stairRiserAndTreadsCalculator);
         propertySetStairFlightCommon.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.PositiveLength,"TreadLength", stairRiserAndTreadsCalculator);
         propertySetStairFlightCommon.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.PositiveLength,"TreadLengthAtOffset", stairRiserAndTreadsCalculator);
         propertySetStairFlightCommon.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.PositiveLength,"TreadLengthAtInnerSide", stairRiserAndTreadsCalculator);
         propertySetStairFlightCommon.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.Length,"NosingLength", stairRiserAndTreadsCalculator);
         propertySetStairFlightCommon.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.PositiveLength,"WalkingLineOffset", stairRiserAndTreadsCalculator);
         propertySetStairFlightCommon.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.PositiveLength,"WaistThickness", stairRiserAndTreadsCalculator);
         propertySetStairFlightCommon.AddEntry(ifcPSE);

         propertySetStairFlightCommon.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"Headroom"));

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
            propertySetStairFlightCommon.AddEntry(PropertySetEntryUtil.CreateStatusEntry());

         commonPropertySets.Add(propertySetStairFlightCommon);
      }

      /// <summary>
      /// Initializes common ramp flight property sets.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetRampFlightCommon(IList<PropertySetDescription> commonPropertySets)
      {
         // Pset_RampFlightCommon
         PropertySetDescription propertySetRampFlightCommon = new PropertySetDescription();
         propertySetRampFlightCommon.Name = "Pset_RampFlightCommon";

         propertySetRampFlightCommon.EntityTypes.Add(IFCEntityType.IfcRampFlight);

         PropertySetEntry ifcPSE = PropertySetEntryUtil.CreateReferenceEntry();
         propertySetRampFlightCommon.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.PlaneAngle,"Slope", RampFlightSlopeCalculator.Instance);
         propertySetRampFlightCommon.AddEntry(ifcPSE);

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            propertySetRampFlightCommon.AddEntry(PropertySetEntryUtil.CreateStatusEntry());
            propertySetRampFlightCommon.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"ClearWidth"));
            propertySetRampFlightCommon.AddEntry(new PropertySetEntry(PropertyType.PlaneAngle,"CounterSlope"));
         }

         commonPropertySets.Add(propertySetRampFlightCommon);
      }

      /// <summary>
      /// Initializes common stair property sets.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetStairCommon(IList<PropertySetDescription> commonPropertySets)
      {
         // Pset_StairCommon
         PropertySetDescription propertySetStairCommon = new PropertySetDescription();
         propertySetStairCommon.Name = "Pset_StairCommon";
         propertySetStairCommon.SubElementIndex = (int)IFCCommonPSets.PSetStairCommon;

         propertySetStairCommon.EntityTypes.Add(IFCEntityType.IfcStair);

         PropertySetEntry ifcPSE = PropertySetEntryUtil.CreateReferenceEntry();
         propertySetStairCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateIsExternalEntry();
         propertySetStairCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateFireRatingEntry();
         propertySetStairCommon.AddEntry(ifcPSE);

         PropertyCalculator stairRiserAndTreadsCalculator = StairRiserTreadsCalculator.Instance;
         ifcPSE = new PropertySetEntry(PropertyType.Count,"NumberOfRiser", stairRiserAndTreadsCalculator);
         propertySetStairCommon.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.Count,"NumberOfTreads", stairRiserAndTreadsCalculator);
         propertySetStairCommon.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.PositiveLength,"RiserHeight", stairRiserAndTreadsCalculator);
         propertySetStairCommon.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.PositiveLength,"TreadLength", stairRiserAndTreadsCalculator);
         propertySetStairCommon.AddEntry(ifcPSE);

         propertySetStairCommon.AddEntry(PropertySetEntryUtil.CreateHandicapAccessibleEntry());
         propertySetStairCommon.AddEntry(new PropertySetEntry(PropertyType.Boolean,"FireExit"));
         propertySetStairCommon.AddEntry(new PropertySetEntry(PropertyType.Boolean,"HasNonSkidSurface"));
         propertySetStairCommon.AddEntry(new PropertySetEntry(PropertyType.Boolean,"RequiredHeadroom"));

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            propertySetStairCommon.AddEntry(PropertySetEntryUtil.CreateStatusEntry());
            propertySetStairCommon.AddEntry(new PropertySetEntry(PropertyType.Length,"NosingLength"));
            propertySetStairCommon.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"WalkingLineOffset"));
            propertySetStairCommon.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"TreadLengthAtOffset"));
            propertySetStairCommon.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"TreadLengthAtInnerSide"));
            propertySetStairCommon.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"WaistThickness"));
         }

         commonPropertySets.Add(propertySetStairCommon);
      }

      /// <summary>
      /// Initializes common building property sets.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      /// <param name="fileVersion">The IFC file version.</param>
      private static void InitPropertySetBuildingCommon(IList<PropertySetDescription> commonPropertySets)
      {
         // Pset_BuildingCommon
         PropertySetDescription propertySetBuildingCommon = new PropertySetDescription();
         propertySetBuildingCommon.Name = "Pset_BuildingCommon";
         propertySetBuildingCommon.EntityTypes.Add(IFCEntityType.IfcBuilding);
         propertySetBuildingCommon.SubElementIndex = (int)IFCCommonPSets.PSetBuildingCommon;

         propertySetBuildingCommon.AddEntry(new PropertySetEntry(PropertyType.Identifier, "BuildingID"));
         propertySetBuildingCommon.AddEntry(new PropertySetEntry(PropertyType.Boolean, "IsPermanentID"));
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            PropertySetEntry ifcPSE = PropertySetEntryUtil.CreateReferenceEntry();
            propertySetBuildingCommon.AddEntry(ifcPSE);
            propertySetBuildingCommon.AddEntry(new PropertySetEntry(PropertyType.Label, "ConstructionMethod"));
            propertySetBuildingCommon.AddEntry(new PropertySetEntry(PropertyType.Label, "FireProtectionClass"));
         }
         else
         {
            propertySetBuildingCommon.AddEntry(new PropertySetEntry(PropertyType.Label, "MainFireUse"));
            propertySetBuildingCommon.AddEntry(new PropertySetEntry(PropertyType.Label, "AncillaryFireUse"));
         }
         propertySetBuildingCommon.AddEntry(new PropertySetEntry(PropertyType.Boolean, "SprinklerProtection"));
         propertySetBuildingCommon.AddEntry(new PropertySetEntry(PropertyType.Boolean, "SprinklerProtectionAutomatic"));
         propertySetBuildingCommon.AddEntry(new PropertySetEntry(PropertyType.Label, "OccupancyType"));
         propertySetBuildingCommon.AddEntry(new PropertySetEntry(PropertyType.Area, "GrossPlannedArea"));
         propertySetBuildingCommon.AddEntry(new PropertySetEntry(PropertyType.Label, "YearOfConstruction"));
         propertySetBuildingCommon.AddEntry(new PropertySetEntry(PropertyType.Boolean, "IsLandmarked"));

         if (!ExporterCacheManager.ExportOptionsCache.ExportAs2x2)
         {
            PropertySetEntry ifcPSE = new PropertySetEntry(PropertyType.Integer, "NumberOfStoreys", NumberOfStoreysCalculator.Instance);
            propertySetBuildingCommon.AddEntry(ifcPSE);

         }

         commonPropertySets.Add(propertySetBuildingCommon);
      }

      /// <summary>
      /// Initializes common level property sets.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      /// <param name="fileVersion">The IFC file version.</param>
      private static void InitPropertySetLevelCommon(IList<PropertySetDescription> commonPropertySets)
      {
         //property level common
         PropertySetDescription propertySetLevelCommon = new PropertySetDescription();
         propertySetLevelCommon.Name = "Pset_BuildingStoreyCommon";
         propertySetLevelCommon.EntityTypes.Add(IFCEntityType.IfcBuildingStorey);
         propertySetLevelCommon.SubElementIndex = (int)IFCCommonPSets.PSetBuildingStoreyCommon;

         PropertySetEntry ifcPSE = new PropertySetEntry(PropertyType.Boolean,"EntranceLevel");
         propertySetLevelCommon.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.Logical,"AboveGround");
         propertySetLevelCommon.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.Boolean,"SprinklerProtection");
         propertySetLevelCommon.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.Boolean,"SprinklerProtectionAutomatic");
         propertySetLevelCommon.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.Real,"GrossAreaPlanned");
         propertySetLevelCommon.AddEntry(ifcPSE);

         if (!ExporterCacheManager.ExportOptionsCache.ExportAs2x2)
         {
            ifcPSE = new PropertySetEntry(PropertyType.Real,"NetAreaPlanned");
            propertySetLevelCommon.AddEntry(ifcPSE);
         }

         commonPropertySets.Add(propertySetLevelCommon);
      }

      /// <summary>
      /// Initializes common site property sets.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetSiteCommon(IList<PropertySetDescription> commonPropertySets)
      {
         //property site common
         PropertySetDescription propertySetSiteCommon = new PropertySetDescription();
         propertySetSiteCommon.Name = "Pset_SiteCommon";
         propertySetSiteCommon.EntityTypes.Add(IFCEntityType.IfcSite);
         propertySetSiteCommon.SubElementIndex = (int)IFCCommonPSets.PSetSiteCommon;

         PropertySetEntry ifcPSE = new PropertySetEntry(PropertyType.Area,"BuildableArea");
         propertySetSiteCommon.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.PositiveLength,"BuildingHeightLimit");
         propertySetSiteCommon.AddEntry(ifcPSE);

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            propertySetSiteCommon.AddEntry(PropertySetEntryUtil.CreateReferenceEntry());
            propertySetSiteCommon.AddEntry(new PropertySetEntry(PropertyType.PositiveRatio,"SiteCoverageRatio"));
            propertySetSiteCommon.AddEntry(new PropertySetEntry(PropertyType.PositiveRatio,"FloorAreaRatio"));
            propertySetSiteCommon.AddEntry(new PropertySetEntry(PropertyType.Area,"TotalArea"));
         }
         else
         {
            ifcPSE = new PropertySetEntry(PropertyType.Area,"GrossAreaPlanned");
            propertySetSiteCommon.AddEntry(ifcPSE);
         }

         commonPropertySets.Add(propertySetSiteCommon);
      }

      /// <summary>
      /// Initializes common building element proxy property sets.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetBuildingElementProxyCommon(IList<PropertySetDescription> commonPropertySets)
      {
         //property building element proxy common
         PropertySetDescription propertySetBuildingElementProxyCommon = new PropertySetDescription();
         propertySetBuildingElementProxyCommon.Name = "Pset_BuildingElementProxyCommon";
         propertySetBuildingElementProxyCommon.EntityTypes.Add(IFCEntityType.IfcBuildingElementProxy);

         PropertySetEntry ifcPSE = PropertySetEntryUtil.CreateReferenceEntry();
         propertySetBuildingElementProxyCommon.AddEntry(ifcPSE);
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            propertySetBuildingElementProxyCommon.AddEntry(PropertySetEntryUtil.CreateStatusEntry());

            ifcPSE = PropertySetEntryUtil.CreateIsExternalEntry();
            propertySetBuildingElementProxyCommon.AddEntry(ifcPSE);

            propertySetBuildingElementProxyCommon.AddEntry(PropertySetEntryUtil.CreateThermalTransmittanceEntry());
            propertySetBuildingElementProxyCommon.AddEntry(new PropertySetEntry(PropertyType.Boolean,"LoadBearing"));

            ifcPSE = PropertySetEntryUtil.CreateFireRatingEntry();
            propertySetBuildingElementProxyCommon.AddEntry(ifcPSE);
         }

         commonPropertySets.Add(propertySetBuildingElementProxyCommon);
      }

      /// <summary>
      /// Initializes common space property sets.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetSpaceCommon(IList<PropertySetDescription> commonPropertySets)
      {
         //property set space common
         PropertySetDescription propertySetSpaceCommon = new PropertySetDescription();
         propertySetSpaceCommon.Name = "Pset_SpaceCommon";

         propertySetSpaceCommon.EntityTypes.Add(IFCEntityType.IfcSpace);

         PropertySetEntry ifcPSE = PropertySetEntryUtil.CreateReferenceEntry();
         propertySetSpaceCommon.AddEntry(ifcPSE);

         propertySetSpaceCommon.AddEntry(new PropertySetEntry(PropertyType.Boolean,"PubliclyAccessible"));
         propertySetSpaceCommon.AddEntry(PropertySetEntryUtil.CreateHandicapAccessibleEntry());
         propertySetSpaceCommon.AddEntry(new PropertySetEntry(PropertyType.Area,"GrossPlannedArea"));

         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2)
         {
            propertySetSpaceCommon.AddEntry(new PropertySetEntry(PropertyType.Label,"OccupancyType"));
            propertySetSpaceCommon.AddEntry(new PropertySetEntry(PropertyType.Real,"OccupancyNumber"));

            ifcPSE = new PropertySetEntry(PropertyType.Boolean,"Concealed", SpaceConcealCalculator.Instance);
            propertySetSpaceCommon.AddEntry(ifcPSE);
         }
         else if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            propertySetSpaceCommon.AddEntry(new PropertySetEntry(PropertyType.Area,"NetPlannedArea"));

            ifcPSE = PropertySetEntryUtil.CreateIsExternalEntry();
            propertySetSpaceCommon.AddEntry(ifcPSE);
         }
         else
         {
            propertySetSpaceCommon.AddEntry(new PropertySetEntry(PropertyType.Label, "Category"));

            ifcPSE = new PropertySetEntry(PropertyType.Label, "CeilingCovering", BuiltInParameter.ROOM_FINISH_CEILING);
            propertySetSpaceCommon.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry(PropertyType.Label,"WallCovering", BuiltInParameter.ROOM_FINISH_WALL);
            propertySetSpaceCommon.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry(PropertyType.Label,"FloorCovering", BuiltInParameter.ROOM_FINISH_FLOOR);
            propertySetSpaceCommon.AddEntry(ifcPSE);

            propertySetSpaceCommon.AddEntry(new PropertySetEntry(PropertyType.Label, "SkirtingBoard"));
            propertySetSpaceCommon.AddEntry(new PropertySetEntry(PropertyType.Area,"NetPlannedArea"));
            propertySetSpaceCommon.AddEntry(new PropertySetEntry(PropertyType.Boolean,"ConcealedFlooring"));
            propertySetSpaceCommon.AddEntry(new PropertySetEntry(PropertyType.Boolean,"ConcealedCeiling"));
         }

         commonPropertySets.Add(propertySetSpaceCommon);
      }

      /// <summary>
      /// Initializes SpaceOccupancyRequirements property set.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetSpaceOccupancyRequirements(IList<PropertySetDescription> commonPropertySets)
      {
         //property set space common
         PropertySetDescription propertySetSpaceOccupancyRequirements = new PropertySetDescription();
         propertySetSpaceOccupancyRequirements.Name = "Pset_SpaceOccupancyRequirements";

         propertySetSpaceOccupancyRequirements.EntityTypes.Add(IFCEntityType.IfcSpace);

         propertySetSpaceOccupancyRequirements.AddEntry(new PropertySetEntry(PropertyType.Label,"OccupancyType"));
         propertySetSpaceOccupancyRequirements.AddEntry(new PropertySetEntry(PropertyType.Count,"OccupancyNumber"));
         propertySetSpaceOccupancyRequirements.AddEntry(new PropertySetEntry(PropertyType.Count,"OccupancyNumberPeak"));
         //propertySetSpaceOccupancyRequirements.AddEntry(PropertySetEntry.CreateTime("OccupancyTimePerDay"));
         propertySetSpaceOccupancyRequirements.AddEntry(new PropertySetEntry(PropertyType.Area,"AreaPerOccupant"));
         propertySetSpaceOccupancyRequirements.AddEntry(new PropertySetEntry(PropertyType.Length,"MinimumHeadroom"));
         propertySetSpaceOccupancyRequirements.AddEntry(new PropertySetEntry(PropertyType.Boolean,"IsOutlookDesirable"));

         commonPropertySets.Add(propertySetSpaceOccupancyRequirements);
      }

      /// <summary>
      /// Initializes common zone property sets.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetZoneCommon(IList<PropertySetDescription> commonPropertySets)
      {
         //property set zone common
         PropertySetDescription propertySetZoneCommon = new PropertySetDescription();
         propertySetZoneCommon.Name = "Pset_ZoneCommon";

         propertySetZoneCommon.EntityTypes.Add(IFCEntityType.IfcZone);

         PropertySetEntry ifcPSE = PropertySetEntryUtil.CreateReferenceEntry();
         propertySetZoneCommon.AddEntry(ifcPSE);

         propertySetZoneCommon.AddEntry(new PropertySetEntry(PropertyType.Label,"Category"));
         propertySetZoneCommon.AddEntry(new PropertySetEntry(PropertyType.Area,"GrossAreaPlanned"));
         propertySetZoneCommon.AddEntry(new PropertySetEntry(PropertyType.Area,"NetAreaPlanned"));
         propertySetZoneCommon.AddEntry(new PropertySetEntry(PropertyType.Boolean,"PubliclyAccessible"));
         propertySetZoneCommon.AddEntry(PropertySetEntryUtil.CreateHandicapAccessibleEntry());

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
            propertySetZoneCommon.AddEntry(PropertySetEntryUtil.CreateIsExternalEntry());
         else
         {
            propertySetZoneCommon.AddEntry(new PropertySetEntry(PropertyType.Label,"OccupancyType"));
            propertySetZoneCommon.AddEntry(new PropertySetEntry(PropertyType.Count,"OccupancyNumber"));
            propertySetZoneCommon.AddEntry(new PropertySetEntry(PropertyType.Boolean,"NaturalVentilation"));
            propertySetZoneCommon.AddEntry(new PropertySetEntry(PropertyType.Count,"NaturalVentilationRate"));
            propertySetZoneCommon.AddEntry(new PropertySetEntry(PropertyType.Count,"MechanicalVentilationRate"));
         }

         commonPropertySets.Add(propertySetZoneCommon);
      }

      /// <summary>
      /// Initializes SpaceFireSafetyRequirements property sets.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetSpaceFireSafetyRequirements(IList<PropertySetDescription> commonPropertySets)
      {
         PropertySetDescription propertySetSpaceFireSafetyRequirements = new PropertySetDescription();
         propertySetSpaceFireSafetyRequirements.Name = "Pset_SpaceFireSafetyRequirements";

         propertySetSpaceFireSafetyRequirements.EntityTypes.Add(IFCEntityType.IfcSpace);

         if (!ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            propertySetSpaceFireSafetyRequirements.AddEntry(new PropertySetEntry(PropertyType.Label,"MainFireUse"));
            propertySetSpaceFireSafetyRequirements.AddEntry(new PropertySetEntry(PropertyType.Label,"AncillaryFireUse"));
            propertySetSpaceFireSafetyRequirements.AddEntry(new PropertySetEntry(PropertyType.Label,"FireHazardFactor"));
         }
         propertySetSpaceFireSafetyRequirements.AddEntry(new PropertySetEntry(PropertyType.Label,"FireRiskFactor"));
         propertySetSpaceFireSafetyRequirements.AddEntry(new PropertySetEntry(PropertyType.Boolean,"FlammableStorage"));
         propertySetSpaceFireSafetyRequirements.AddEntry(new PropertySetEntry(PropertyType.Boolean,"FireExit"));
         propertySetSpaceFireSafetyRequirements.AddEntry(new PropertySetEntry(PropertyType.Boolean,"SprinklerProtection"));
         propertySetSpaceFireSafetyRequirements.AddEntry(new PropertySetEntry(PropertyType.Boolean,"SprinklerProtectionAutomatic"));
         propertySetSpaceFireSafetyRequirements.AddEntry(new PropertySetEntry(PropertyType.Boolean,"AirPressurization"));

         commonPropertySets.Add(propertySetSpaceFireSafetyRequirements);
      }

      /// <summary>
      /// Initializes SpaceLightingRequirements property sets.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetSpaceLightingRequirements(IList<PropertySetDescription> commonPropertySets)
      {
         PropertySetDescription propertySetSpaceLightingRequirements = new PropertySetDescription();
         propertySetSpaceLightingRequirements.Name = "Pset_SpaceLightingRequirements";

         propertySetSpaceLightingRequirements.EntityTypes.Add(IFCEntityType.IfcSpace);

         propertySetSpaceLightingRequirements.AddEntry(new PropertySetEntry(PropertyType.Boolean,"ArtificialLighting"));
         propertySetSpaceLightingRequirements.AddEntry(new PropertySetEntry(PropertyType.Real,"Illuminance"));

         commonPropertySets.Add(propertySetSpaceLightingRequirements);
      }

      /// <summary>
      /// Initializes SpaceThermalRequirements property sets.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetSpaceThermalRequirements(IList<PropertySetDescription> commonPropertySets)
      {
         PropertySetDescription propertySetSpaceThermalRequirements = new PropertySetDescription();
         propertySetSpaceThermalRequirements.Name = "Pset_SpaceThermalRequirements";

         propertySetSpaceThermalRequirements.EntityTypes.Add(IFCEntityType.IfcSpace);


         propertySetSpaceThermalRequirements.AddEntry(new PropertySetEntry(PropertyType.ThermodynamicTemperature,"SpaceTemperatureMax"));
         propertySetSpaceThermalRequirements.AddEntry(new PropertySetEntry(PropertyType.ThermodynamicTemperature,"SpaceTemperatureMin"));
         propertySetSpaceThermalRequirements.AddEntry(new PropertySetEntry(PropertyType.Ratio,"SpaceHumidity"));
         propertySetSpaceThermalRequirements.AddEntry(new PropertySetEntry(PropertyType.Ratio,"SpaceHumiditySummer"));
         propertySetSpaceThermalRequirements.AddEntry(new PropertySetEntry(PropertyType.Ratio,"SpaceHumidityWinter"));
         propertySetSpaceThermalRequirements.AddEntry(new PropertySetEntry(PropertyType.Boolean,"DiscontinuedHeating"));
         propertySetSpaceThermalRequirements.AddEntry(new PropertySetEntry(PropertyType.Boolean,"NaturalVentilation"));
         propertySetSpaceThermalRequirements.AddEntry(new PropertySetEntry(PropertyType.Count,"NaturalVentilationRate"));
         propertySetSpaceThermalRequirements.AddEntry(new PropertySetEntry(PropertyType.Count,"MechanicalVentilationRate"));
         propertySetSpaceThermalRequirements.AddEntry(new PropertySetEntry(PropertyType.Boolean,"AirConditioning"));
         propertySetSpaceThermalRequirements.AddEntry(new PropertySetEntry(PropertyType.Boolean,"AirConditioningCentral"));

         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2)
         {
            PropertySetEntry ifcPSE = new PropertySetEntry(PropertyType.ThermodynamicTemperature,"SpaceTemperatureSummer", new SpaceTemperatureCalculator("SpaceTemperatureSummer"));
            propertySetSpaceThermalRequirements.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry(PropertyType.ThermodynamicTemperature,"SpaceTemperatureWinter", new SpaceTemperatureCalculator("SpaceTemperatureWinter"));
            propertySetSpaceThermalRequirements.AddEntry(ifcPSE);
         }
         else if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            propertySetSpaceThermalRequirements.AddEntry(new PropertySetEntry(PropertyType.ThermodynamicTemperature,"SpaceTemperature"));
            propertySetSpaceThermalRequirements.AddEntry(new PropertySetEntry(PropertyType.ThermodynamicTemperature,"SpaceTemperatureSummerMax"));
            propertySetSpaceThermalRequirements.AddEntry(new PropertySetEntry(PropertyType.ThermodynamicTemperature,"SpaceTemperatureSummerMin"));
            propertySetSpaceThermalRequirements.AddEntry(new PropertySetEntry(PropertyType.ThermodynamicTemperature,"SpaceTemperatureWinterMax"));
            propertySetSpaceThermalRequirements.AddEntry(new PropertySetEntry(PropertyType.ThermodynamicTemperature,"SpaceTemperatureWinterMin"));
            propertySetSpaceThermalRequirements.AddEntry(new PropertySetEntry(PropertyType.ThermodynamicTemperature,"SpaceHumidityMax"));
            propertySetSpaceThermalRequirements.AddEntry(new PropertySetEntry(PropertyType.ThermodynamicTemperature,"SpaceHumidityMin"));
         }
         else
         {
            propertySetSpaceThermalRequirements.AddEntry(new PropertySetEntry(PropertyType.ThermodynamicTemperature,"SpaceTemperatureSummerMax"));
            propertySetSpaceThermalRequirements.AddEntry(new PropertySetEntry(PropertyType.ThermodynamicTemperature,"SpaceTemperatureSummerMin"));
            propertySetSpaceThermalRequirements.AddEntry(new PropertySetEntry(PropertyType.ThermodynamicTemperature,"SpaceTemperatureWinterMax"));
            propertySetSpaceThermalRequirements.AddEntry(new PropertySetEntry(PropertyType.ThermodynamicTemperature,"SpaceTemperatureWinterMin"));
         }

         commonPropertySets.Add(propertySetSpaceThermalRequirements);
      }

      private static void InitPropertySetSpaceCoveringRequirements(IList<PropertySetDescription> commonPropertySets)
      {
         PropertySetDescription propertySetSpaceCoveringRequirements = new PropertySetDescription();
         propertySetSpaceCoveringRequirements.Name = "Pset_SpaceCoveringRequirements";

         propertySetSpaceCoveringRequirements.EntityTypes.Add(IFCEntityType.IfcSpace);

         PropertySetEntry ifcPSE = new PropertySetEntry(PropertyType.Label,"CeilingCovering", BuiltInParameter.ROOM_FINISH_CEILING);
         propertySetSpaceCoveringRequirements.AddEntry(ifcPSE);
         propertySetSpaceCoveringRequirements.AddEntry(new PropertySetEntry(PropertyType.Real,"CeilingCoveringThickness"));

         ifcPSE = new PropertySetEntry(PropertyType.Label,"WallCovering", BuiltInParameter.ROOM_FINISH_WALL);
         propertySetSpaceCoveringRequirements.AddEntry(ifcPSE);
         propertySetSpaceCoveringRequirements.AddEntry(new PropertySetEntry(PropertyType.Real,"WallCoveringThickness"));

         ifcPSE = new PropertySetEntry(PropertyType.Label,"FloorCovering", BuiltInParameter.ROOM_FINISH_FLOOR);
         propertySetSpaceCoveringRequirements.AddEntry(ifcPSE);
         propertySetSpaceCoveringRequirements.AddEntry(new PropertySetEntry(PropertyType.Real,"FloorCoveringThickness"));

         propertySetSpaceCoveringRequirements.AddEntry(new PropertySetEntry(PropertyType.Label,"SkirtingBoard"));
         propertySetSpaceCoveringRequirements.AddEntry(new PropertySetEntry(PropertyType.Real,"SkirtingBoardHeight"));
         propertySetSpaceCoveringRequirements.AddEntry(new PropertySetEntry(PropertyType.Label,"Molding"));
         propertySetSpaceCoveringRequirements.AddEntry(new PropertySetEntry(PropertyType.Real,"MoldingHeight"));
         propertySetSpaceCoveringRequirements.AddEntry(new PropertySetEntry(PropertyType.Boolean,"ConcealedFlooring"));
         propertySetSpaceCoveringRequirements.AddEntry(new PropertySetEntry(PropertyType.Boolean,"ConcealedCeiling"));

         commonPropertySets.Add(propertySetSpaceCoveringRequirements);
      }

      /// <summary>
      /// Initializes GSA Space Categories property sets.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetGSASpaceCategories(IList<PropertySetDescription> commonPropertySets)
      {
         PropertySetDescription propertySetGSASpaceCategories = new PropertySetDescription();
         propertySetGSASpaceCategories.Name = "GSA Space Categories";

         propertySetGSASpaceCategories.EntityTypes.Add(IFCEntityType.IfcSpace);

         propertySetGSASpaceCategories.AddEntry(new PropertySetEntry(PropertyType.Label,"GSA STAR Space Type"));
         propertySetGSASpaceCategories.AddEntry(new PropertySetEntry(PropertyType.Label,"GSA STAR Space Category"));
         propertySetGSASpaceCategories.AddEntry(new PropertySetEntry(PropertyType.Label,"ANSI/BOMA Space Category"));

         commonPropertySets.Add(propertySetGSASpaceCategories);
      }

      /// <summary>
      /// Initializes Space Occupant Properties sets.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetSpaceOccupant(IList<PropertySetDescription> commonPropertySets)
      {
         PropertySetDescription propertySetSpaceOccupant = new PropertySetDescription();
         propertySetSpaceOccupant.Name = "Space Occupant Properties";

         propertySetSpaceOccupant.EntityTypes.Add(IFCEntityType.IfcSpace);

         propertySetSpaceOccupant.AddEntry(new PropertySetEntry(PropertyType.Label,"Occupant Organization Code"));
         propertySetSpaceOccupant.AddEntry(new PropertySetEntry(PropertyType.Label,"Occupant Organization Abbreviation"));
         propertySetSpaceOccupant.AddEntry(new PropertySetEntry(PropertyType.Label,"Occupant Organization Name"));
         propertySetSpaceOccupant.AddEntry(new PropertySetEntry(PropertyType.Label,"Occupant Sub-Organization Code"));
         propertySetSpaceOccupant.AddEntry(new PropertySetEntry(PropertyType.Label,"Occupant Billing ID"));

         commonPropertySets.Add(propertySetSpaceOccupant);
      }

      /// <summary>
      /// Initializes Space Zones property sets.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetSpaceZones(IList<PropertySetDescription> commonPropertySets)
      {
         PropertySetDescription propertySetSpaceZones = new PropertySetDescription();
         propertySetSpaceZones.Name = "Space Zones";

         propertySetSpaceZones.EntityTypes.Add(IFCEntityType.IfcSpace);

         propertySetSpaceZones.AddEntry(new PropertySetEntry(PropertyType.Label,"Security Zone"));
         propertySetSpaceZones.AddEntry(new PropertySetEntry(PropertyType.Label,"Preservation Zone"));
         propertySetSpaceZones.AddEntry(new PropertySetEntry(PropertyType.Label,"Privacy Zone"));
         if (!ExporterCacheManager.ExportOptionsCache.ExportAs2x2)
         {
            propertySetSpaceZones.AddEntry(new PropertySetEntry(PropertyType.Label,"Zone GrossAreaPlanned"));
            propertySetSpaceZones.AddEntry(new PropertySetEntry(PropertyType.Label,"Zone NetAreaPlanned"));
         }

			PropertySetEntryMap map = new PropertySetEntryMap(SpecificZoneCalculator.Instance) { UseCalculatorOnly = true };
         PropertySetEntry ifcPSE = PropertySetEntry.CreateListValue("Project Specific Zone", PropertyType.Label, map);
         propertySetSpaceZones.AddEntry(ifcPSE);

         commonPropertySets.Add(propertySetSpaceZones);
      }

      /// <summary>
      /// Initializes building water storage property sets.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetBuildingWaterStorage(IList<PropertySetDescription> commonPropertySets)
      {
         PropertySetDescription propertySetBuildingWaterStorage = new PropertySetDescription();
         propertySetBuildingWaterStorage.Name = "Pset_BuildingWaterStorage";
         propertySetBuildingWaterStorage.EntityTypes.Add(IFCEntityType.IfcBuilding);

         propertySetBuildingWaterStorage.AddEntry(new PropertySetEntry(PropertyType.Real,"OneDayPotableWater"));
         propertySetBuildingWaterStorage.AddEntry(new PropertySetEntry(PropertyType.Real,"OneDayProcessOrProductionWater"));
         propertySetBuildingWaterStorage.AddEntry(new PropertySetEntry(PropertyType.Real,"OneDayCoolingTowerMakeupWater"));

         commonPropertySets.Add(propertySetBuildingWaterStorage);
      }

      /// <summary>
      /// Initializes element shading property sets.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetElementShading(IList<PropertySetDescription> commonPropertySets)
      {
         PropertySetDescription propertySetElementShading = new PropertySetDescription();
         propertySetElementShading.Name = "Pset_ElementShading";
         propertySetElementShading.EntityTypes.Add(IFCEntityType.IfcBuildingElementProxy);
         propertySetElementShading.ObjectType = "Solar Shade";

         propertySetElementShading.AddEntry(PropertySetEntry.CreateEnumeratedValue("ShadingDeviceType", PropertyType.Label,
             typeof(PSetElementShading_ShadingDeviceType)));
         propertySetElementShading.AddEntry(new PropertySetEntry(PropertyType.PlaneAngle,"Azimuth"));
         propertySetElementShading.AddEntry(new PropertySetEntry(PropertyType.PlaneAngle,"Inclination"));
         propertySetElementShading.AddEntry(new PropertySetEntry(PropertyType.PlaneAngle,"TiltRange"));
         propertySetElementShading.AddEntry(new PropertySetEntry(PropertyType.PositiveRatio,"AverageSolarTransmittance"));
         propertySetElementShading.AddEntry(new PropertySetEntry(PropertyType.PositiveRatio,"AverageVisibleTransmittance"));
         propertySetElementShading.AddEntry(new PropertySetEntry(PropertyType.PositiveRatio,"Reflectance"));
         propertySetElementShading.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"Roughness"));
         propertySetElementShading.AddEntry(new PropertySetEntry(PropertyType.Label,"Color"));

         commonPropertySets.Add(propertySetElementShading);
      }

      /// <summary>
      /// Initializes Pset_ProvisionForVoid.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetProvisionForVoid(IList<PropertySetDescription> commonPropertySets)
      {
         PropertySetDescription propertySetProvisionForVoid = new PropertySetDescription();
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
            propertySetProvisionForVoid.Name = "Pset_BuildingElementProxyProvisionForVoid";
         else
            propertySetProvisionForVoid.Name = "Pset_ProvisionForVoid";

         propertySetProvisionForVoid.EntityTypes.Add(IFCEntityType.IfcBuildingElementProxy);
         propertySetProvisionForVoid.ObjectType = "ProvisionForVoid";

         // The Shape value must be determined first, as other calculators will use the value stored.
         PropertySetEntry ifcPSE = new PropertySetEntry(PropertyType.Label, "Shape", ProvisionForVoidShapeCalculator.Instance);
         propertySetProvisionForVoid.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.PositiveLength, "Width", ProvisionForVoidWidthCalculator.Instance);
         propertySetProvisionForVoid.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.PositiveLength, "Height", ProvisionForVoidHeightCalculator.Instance);
         propertySetProvisionForVoid.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.PositiveLength, "Diameter", ProvisionForVoidDiameterCalculator.Instance);
         propertySetProvisionForVoid.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.PositiveLength, "Depth", ProvisionForVoidDepthCalculator.Instance);
         propertySetProvisionForVoid.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.Label,"System");
         propertySetProvisionForVoid.AddEntry(ifcPSE);

         commonPropertySets.Add(propertySetProvisionForVoid);
      }

      /// <summary>
      /// Initializes Pset_SanitaryTerminalTypeBath
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetSanitaryTerminalTypeBath(IList<PropertySetDescription> commonPropertySets)
      {
         PropertySetDescription propertySetBath = new PropertySetDescription();
         propertySetBath.Name = "Pset_SanitaryTerminalTypeBath";
         propertySetBath.EntityTypes.Add(IFCEntityType.IfcSanitaryTerminalType);
         propertySetBath.PredefinedType = "BATH";

         propertySetBath.AddEntry(PropertySetEntry.CreateEnumeratedValue("BathType", PropertyType.Label,
             typeof(PsetSanitaryTerminalTypeBath_BathType)));
         propertySetBath.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"NominalLength"));
         propertySetBath.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"NominalWidth"));
         propertySetBath.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"NominalDepth"));

         //propertySetBath.AddEntry(PropertySetEntry.CreateMaterial("Material"));
         propertySetBath.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"MaterialThickness"));
         propertySetBath.AddEntry(new PropertySetEntry(PropertyType.Text,"Color"));
         propertySetBath.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"DrainSize"));
         propertySetBath.AddEntry(new PropertySetEntry(PropertyType.Boolean,"HasGrabHandles"));

         commonPropertySets.Add(propertySetBath);
      }

      /// <summary>
      /// Initializes Pset_SanitaryTerminalTypeShower
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetSanitaryTerminalTypeShower(IList<PropertySetDescription> commonPropertySets)
      {
         PropertySetDescription propertySetShower = new PropertySetDescription();
         propertySetShower.Name = "Pset_SanitaryTerminalTypeShower";
         propertySetShower.EntityTypes.Add(IFCEntityType.IfcSanitaryTerminalType);
         propertySetShower.PredefinedType = "SHOWER";

         propertySetShower.AddEntry(PropertySetEntry.CreateEnumeratedValue("ShowerType", PropertyType.Label,
             typeof(PsetSanitaryTerminalTypeShower_ShowerType)));
         propertySetShower.AddEntry(new PropertySetEntry(PropertyType.Boolean,"HasTray"));
         propertySetShower.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"NominalLength"));
         propertySetShower.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"NominalWidth"));
         propertySetShower.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"NominalDepth"));

         //propertySetShower.AddEntry(PropertySetEntry.CreateMaterial("Material"));
         propertySetShower.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"MaterialThickness"));
         propertySetShower.AddEntry(new PropertySetEntry(PropertyType.Text,"Color"));
         propertySetShower.AddEntry(new PropertySetEntry(PropertyType.Text,"ShowerHeadDescription"));
         propertySetShower.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"DrainSize"));

         commonPropertySets.Add(propertySetShower);
      }

      /// <summary>
      /// Initializes Pset_SanitaryTerminalTypeSink
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetSanitaryTerminalTypeSink(IList<PropertySetDescription> commonPropertySets)
      {
         PropertySetDescription propertySetSink = new PropertySetDescription();
         propertySetSink.Name = "Pset_SanitaryTerminalTypeSink";
         propertySetSink.EntityTypes.Add(IFCEntityType.IfcSanitaryTerminalType);
         propertySetSink.PredefinedType = "SINK";

         propertySetSink.AddEntry(PropertySetEntry.CreateEnumeratedValue("SinkType", PropertyType.Label,
             typeof(PsetSanitaryTerminalTypeSink_SinkType)));
         // PsetSanitaryTerminalTypeToiletPan_SanitaryMounting is purposely reused, as it is identical.
         propertySetSink.AddEntry(PropertySetEntry.CreateEnumeratedValue("SinkMounting", PropertyType.Label,
             typeof(PsetSanitaryTerminalTypeToiletPan_SanitaryMounting)));
         propertySetSink.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"NominalLength"));
         propertySetSink.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"NominalWidth"));
         propertySetSink.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"NominalDepth"));

         //propertySetSink.AddEntry(PropertySetEntry.CreateMaterial("Material"));
         propertySetSink.AddEntry(new PropertySetEntry(PropertyType.Text,"Color"));
         propertySetSink.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"DrainSize"));

         commonPropertySets.Add(propertySetSink);
      }

      /// <summary>
      /// Initializes Pset_SanitaryTerminalTypeToiletPan
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetSanitaryTerminalTypeToiletPan(IList<PropertySetDescription> commonPropertySets)
      {
         PropertySetDescription propertySetToiletPan = new PropertySetDescription();
         propertySetToiletPan.Name = "Pset_SanitaryTerminalTypeToiletPan";
         propertySetToiletPan.EntityTypes.Add(IFCEntityType.IfcSanitaryTerminalType);
         propertySetToiletPan.PredefinedType = "TOILETPAN";

         propertySetToiletPan.AddEntry(PropertySetEntry.CreateEnumeratedValue("ToiletType", PropertyType.Label,
             typeof(PsetSanitaryTerminalTypeToiletPan_ToiletType)));
         propertySetToiletPan.AddEntry(PropertySetEntry.CreateEnumeratedValue("ToiletPanType", PropertyType.Label,
             typeof(PsetSanitaryTerminalTypeToiletPan_ToiletPanType)));
         propertySetToiletPan.AddEntry(PropertySetEntry.CreateEnumeratedValue("PanMounting", PropertyType.Label,
             typeof(PsetSanitaryTerminalTypeToiletPan_SanitaryMounting)));
         //propertySetToiletPan.AddEntry(PropertySetEntry.CreateMaterial("PanMaterial"));
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
            propertySetToiletPan.AddEntry(new PropertySetEntry(PropertyType.Label,"Color"));
         else
            propertySetToiletPan.AddEntry(new PropertySetEntry(PropertyType.Text,"PanColor"));
         propertySetToiletPan.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"SpilloverLevel"));
         propertySetToiletPan.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"NominalLength"));
         propertySetToiletPan.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"NominalWidth"));
         propertySetToiletPan.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"NominalDepth"));

         commonPropertySets.Add(propertySetToiletPan);
      }

      /// <summary>
      /// Initializes Pset_SanitaryTerminalTypeWashHandBasin
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetSanitaryTerminalTypeWashHandBasin(IList<PropertySetDescription> commonPropertySets)
      {
         PropertySetDescription propertySetWashHandBasin = new PropertySetDescription();
         propertySetWashHandBasin.Name = "Pset_SanitaryTerminalTypeWashHandBasin";
         propertySetWashHandBasin.EntityTypes.Add(IFCEntityType.IfcSanitaryTerminalType);
         propertySetWashHandBasin.PredefinedType = "WASHHANDBASIN";

         propertySetWashHandBasin.AddEntry(PropertySetEntry.CreateEnumeratedValue("WashHandBasinType", PropertyType.Label,
             typeof(PsetSanitaryTerminalTypeWashHandBasin_WashHandBasinType)));
         // PsetSanitaryTerminalTypeToiletPan_SanitaryMounting is purposely reused, as it is identical.
         propertySetWashHandBasin.AddEntry(PropertySetEntry.CreateEnumeratedValue("WashHandBasinMounting", PropertyType.Label,
             typeof(PsetSanitaryTerminalTypeToiletPan_SanitaryMounting)));
         //propertySetWashHandBasin.AddEntry(PropertySetEntry.CreateMaterial("Material"));
         propertySetWashHandBasin.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"NominalLength"));
         propertySetWashHandBasin.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"NominalWidth"));
         propertySetWashHandBasin.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"NominalDepth"));
         propertySetWashHandBasin.AddEntry(new PropertySetEntry(PropertyType.Text,"Color"));
         propertySetWashHandBasin.AddEntry(new PropertySetEntry(PropertyType.PositiveLength,"DrainSize"));

         commonPropertySets.Add(propertySetWashHandBasin);
      }

      /// <summary>
      /// Initializes Pset_SwitchingDeviceTypeCommon
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetSwitchingDeviceTypeCommon(IList<PropertySetDescription> commonPropertySets)
      {
         PropertySetDescription propertySetSwitchingDeviceTypeCommon = new PropertySetDescription();
         propertySetSwitchingDeviceTypeCommon.Name = "Pset_SwitchingDeviceTypeCommon";
         propertySetSwitchingDeviceTypeCommon.EntityTypes.Add(IFCEntityType.IfcSwitchingDeviceType);

         propertySetSwitchingDeviceTypeCommon.AddEntry(new PropertySetEntry(PropertyType.Integer,"NumberOfGangs"));
         propertySetSwitchingDeviceTypeCommon.AddEntry(PropertySetEntry.CreateEnumeratedValue("SwitchFunction",
             PropertyType.Label, typeof(PsetSwitchingDeviceTypeCommon_SwitchFunction)));
         propertySetSwitchingDeviceTypeCommon.AddEntry(new PropertySetEntry(PropertyType.Boolean,"HasLock"));

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            propertySetSwitchingDeviceTypeCommon.AddEntry(PropertySetEntryUtil.CreateReferenceEntry());
            propertySetSwitchingDeviceTypeCommon.AddEntry(PropertySetEntryUtil.CreateStatusEntry());
            propertySetSwitchingDeviceTypeCommon.AddEntry(new PropertySetEntry(PropertyType.Boolean,"IsIlluminated"));
            propertySetSwitchingDeviceTypeCommon.AddEntry(new PropertySetEntry(PropertyType.Label,"Legend"));
            // cannot support table value: skip SetPoint property
         }

         commonPropertySets.Add(propertySetSwitchingDeviceTypeCommon);
      }

      /// <summary>
      /// Initializes Pset_SwitchingDeviceTypeToggleSwitch
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetSwitchingDeviceTypeToggleSwitch(IList<PropertySetDescription> commonPropertySets)
      {
         PropertySetDescription propertySetSwitchingDeviceTypeToggleSwitch = new PropertySetDescription();
         propertySetSwitchingDeviceTypeToggleSwitch.Name = "Pset_SwitchingDeviceTypeToggleSwitch";
         propertySetSwitchingDeviceTypeToggleSwitch.EntityTypes.Add(IFCEntityType.IfcSwitchingDeviceType);
         // TODO: Restrict to TOGGLESWITCH.

         propertySetSwitchingDeviceTypeToggleSwitch.AddEntry(PropertySetEntry.CreateEnumeratedValue("ToggleSwitchType",
             PropertyType.Label, typeof(PsetSwitchingDeviceTypeToggleSwitch_ToggleSwitchType)));
         propertySetSwitchingDeviceTypeToggleSwitch.AddEntry(PropertySetEntry.CreateEnumeratedValue("SwitchUsage",
             PropertyType.Label, typeof(PsetSwitchingDeviceTypeToggleSwitch_SwitchUsage)));
         propertySetSwitchingDeviceTypeToggleSwitch.AddEntry(PropertySetEntry.CreateEnumeratedValue("SwitchActivation",
             PropertyType.Label, typeof(PsetSwitchingDeviceTypeToggleSwitch_SwitchActivation)));

         if (!ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            // properties have been removed in IFc4
            propertySetSwitchingDeviceTypeToggleSwitch.AddEntry(new PropertySetEntry(PropertyType.Boolean,"IsIlluminated"));
            propertySetSwitchingDeviceTypeToggleSwitch.AddEntry(new PropertySetEntry(PropertyType.Label,"Legend"));
         }

         commonPropertySets.Add(propertySetSwitchingDeviceTypeToggleSwitch);
      }

      /// <summary>
      /// Initializes COBIE property sets.
      /// </summary>
      /// <param name="propertySets">List to store property sets.</param>
      private static void InitCOBIEPropertySets(IList<IList<PropertySetDescription>> propertySets)
      {
         IList<PropertySetDescription> cobiePSets = new List<PropertySetDescription>();
         InitCOBIEPSetSpaceThermalSimulationProperties(cobiePSets);
         InitCOBIEPSetSpaceVentilationCriteria(cobiePSets);
         InitCOBIEPSetBuildingEnergyTarget(cobiePSets);
         InitCOBIEPSetGlazingPropertiesEnergyAnalysis(cobiePSets);
         InitCOBIEPSetPhotovoltaicArray(cobiePSets);
         propertySets.Add(cobiePSets);
      }

      /// <summary>
      /// Initializes COBIE space thermal simulation property sets.
      /// </summary>
      /// <param name="cobiePropertySets">List to store property sets.</param>
      private static void InitCOBIEPSetSpaceThermalSimulationProperties(IList<PropertySetDescription> cobiePropertySets)
      {
         PropertySetDescription propertySetSpaceThermalSimulationProperties = new PropertySetDescription();
         propertySetSpaceThermalSimulationProperties.Name = "ePset_SpaceThermalSimulationProperties";
         propertySetSpaceThermalSimulationProperties.EntityTypes.Add(IFCEntityType.IfcSpace);

         PropertySetEntry ifcPSE = new PropertySetEntry(PropertyType.Label,"Space Thermal Simulation Type");
         ifcPSE.PropertyName = "SpaceThermalSimulationType";
         propertySetSpaceThermalSimulationProperties.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.Label,"Space Conditioning Requirement");
         ifcPSE.PropertyName = "SpaceConditioningRequirement";
         propertySetSpaceThermalSimulationProperties.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.Real,"Space Occupant Density");
         ifcPSE.PropertyName = "SpaceOccupantDensity";
         propertySetSpaceThermalSimulationProperties.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.Real,"Space Occupant Heat Rate");
         ifcPSE.PropertyName = "SpaceOccupantHeatRate";
         propertySetSpaceThermalSimulationProperties.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.Real,"Space Occupant Load");
         ifcPSE.PropertyName = "SpaceOccupantLoad";
         propertySetSpaceThermalSimulationProperties.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.Real,"Space Equipment Load");
         ifcPSE.PropertyName = "SpaceEquipmentLoad";
         propertySetSpaceThermalSimulationProperties.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.Real,"Space Lighting Load");
         ifcPSE.PropertyName = "SpaceLightingLoad";
         propertySetSpaceThermalSimulationProperties.AddEntry(ifcPSE);

         cobiePropertySets.Add(propertySetSpaceThermalSimulationProperties);
      }

      /// <summary>
      /// Initializes space thermal design property sets.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetSpaceThermalDesign(IList<PropertySetDescription> commonPropertySets)
      {
         PropertySetDescription propertySetSpaceThermalDesign = new PropertySetDescription();
         propertySetSpaceThermalDesign.Name = "Pset_SpaceThermalDesign";
         propertySetSpaceThermalDesign.EntityTypes.Add(IFCEntityType.IfcSpace);

         PropertySetEntry ifcPSE = new PropertySetEntry(PropertyType.VolumetricFlowRate,"CoolingDesignAirflow");
         propertySetSpaceThermalDesign.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.VolumetricFlowRate,"HeatingDesignAirflow");
         propertySetSpaceThermalDesign.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.Power,"TotalSensibleHeatGain");
         propertySetSpaceThermalDesign.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.Power,"TotalHeatGain");
         propertySetSpaceThermalDesign.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.Power,"TotalHeatLoss");
         propertySetSpaceThermalDesign.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.ThermodynamicTemperature,"Inside Dry Bulb Temperature - Cooling");
         ifcPSE.PropertyName = "CoolingDryBulb";
         propertySetSpaceThermalDesign.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.PositiveRatio,"Inside Relative Humidity - Cooling");
         ifcPSE.PropertyName = "CoolingRelativeHumidity";
         propertySetSpaceThermalDesign.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.ThermodynamicTemperature,"Inside Dry Bulb Temperature - Heating");
         ifcPSE.PropertyName = "HeatingDryBulb";
         propertySetSpaceThermalDesign.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.PositiveRatio,"Inside Relative Humidity - Heating");
         ifcPSE.PropertyName = "HeatingRelativeHumidity";
         propertySetSpaceThermalDesign.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.VolumetricFlowRate,"VentilationAirFlowrate");
         propertySetSpaceThermalDesign.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.VolumetricFlowRate,"ExhaustAirFlowrate");
         propertySetSpaceThermalDesign.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.Boolean,"Inside Return Air Plenum");
         ifcPSE.PropertyName = "CeilingRAPlenum";
         propertySetSpaceThermalDesign.AddEntry(ifcPSE);

         // BoundaryAreaHeatLoss not yet supported.

         commonPropertySets.Add(propertySetSpaceThermalDesign);
      }

      /// <summary>
      /// Initializes COBIE space ventilation criteria property sets.
      /// </summary>
      /// <param name="cobiePropertySets">List to store property sets.</param>
      private static void InitCOBIEPSetSpaceVentilationCriteria(IList<PropertySetDescription> cobiePropertySets)
      {
         PropertySetDescription propertySetSpaceVentilationCriteria = new PropertySetDescription();
         propertySetSpaceVentilationCriteria.Name = "ePset_SpaceVentilationCriteria";
         propertySetSpaceVentilationCriteria.EntityTypes.Add(IFCEntityType.IfcSpace);

         PropertySetEntry ifcPSE = new PropertySetEntry(PropertyType.Label,"Ventilation Type");
         ifcPSE.PropertyName = "VentilationType";
         propertySetSpaceVentilationCriteria.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.Real,"Outside Air Per Person");
         ifcPSE.PropertyName = "OutsideAirPerPerson";
         propertySetSpaceVentilationCriteria.AddEntry(ifcPSE);

         cobiePropertySets.Add(propertySetSpaceVentilationCriteria);
      }

      /// <summary>
      /// Initializes COBIE building energy target property sets.
      /// </summary>
      /// <param name="cobiePropertySets">List to store property sets.</param>
      private static void InitCOBIEPSetBuildingEnergyTarget(IList<PropertySetDescription> cobiePropertySets)
      {
         PropertySetDescription propertySetBuildingEnergyTarget = new PropertySetDescription();
         propertySetBuildingEnergyTarget.Name = "ePset_BuildingEnergyTarget";
         propertySetBuildingEnergyTarget.EntityTypes.Add(IFCEntityType.IfcBuilding);

         PropertySetEntry ifcPSE = new PropertySetEntry(PropertyType.Real,"Building Energy Target Value");
         ifcPSE.PropertyName = "BuildingEnergyTargetValue";
         propertySetBuildingEnergyTarget.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.Label,"Building Energy Target Units");
         ifcPSE.PropertyName = "BuildingEnergyTargetUnits";
         propertySetBuildingEnergyTarget.AddEntry(ifcPSE);

         cobiePropertySets.Add(propertySetBuildingEnergyTarget);
      }

      /// <summary>
      /// Initializes COBIE glazing properties energy analysis property sets.
      /// </summary>
      /// <param name="cobiePropertySets">List to store property sets.</param>
      private static void InitCOBIEPSetGlazingPropertiesEnergyAnalysis(IList<PropertySetDescription> cobiePropertySets)
      {
         PropertySetDescription propertySetGlazingPropertiesEnergyAnalysis = new PropertySetDescription();
         propertySetGlazingPropertiesEnergyAnalysis.Name = "ePset_GlazingPropertiesEnergyAnalysis";
         propertySetGlazingPropertiesEnergyAnalysis.EntityTypes.Add(IFCEntityType.IfcCurtainWall);

         PropertySetEntry ifcPSE = new PropertySetEntry(PropertyType.Label,"Windows 6 Glazing System Name");
         ifcPSE.PropertyName = "Windows6GlazingSystemName";
         propertySetGlazingPropertiesEnergyAnalysis.AddEntry(ifcPSE);

         cobiePropertySets.Add(propertySetGlazingPropertiesEnergyAnalysis);
      }

      /// <summary>
      /// Initializes COBIE photo voltaic array property sets.
      /// </summary>
      /// <param name="cobiePropertySets">List to store property sets.</param>
      private static void InitCOBIEPSetPhotovoltaicArray(IList<PropertySetDescription> cobiePropertySets)
      {
         PropertySetDescription propertySetPhotovoltaicArray = new PropertySetDescription();
         propertySetPhotovoltaicArray.Name = "ePset_PhotovoltaicArray";
         propertySetPhotovoltaicArray.EntityTypes.Add(IFCEntityType.IfcRoof);
         propertySetPhotovoltaicArray.EntityTypes.Add(IFCEntityType.IfcWall);
         propertySetPhotovoltaicArray.EntityTypes.Add(IFCEntityType.IfcSlab);

         PropertySetEntry ifcPSE = new PropertySetEntry(PropertyType.Boolean,"Hosts Photovoltaic Array");
         ifcPSE.PropertyName = "HostsPhotovoltaicArray";
         propertySetPhotovoltaicArray.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.Real,"Active Area Ratio");
         ifcPSE.PropertyName = "ActiveAreaRatio";
         propertySetPhotovoltaicArray.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.Real,"DC to AC Conversion Efficiency");
         ifcPSE.PropertyName = "DcToAcConversionEfficiency";
         propertySetPhotovoltaicArray.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.Label,"Photovoltaic Surface Integration");
         ifcPSE.PropertyName = "PhotovoltaicSurfaceIntegration";
         propertySetPhotovoltaicArray.AddEntry(ifcPSE);

         ifcPSE = new PropertySetEntry(PropertyType.Real,"Photovoltaic Cell Efficiency");
         ifcPSE.PropertyName = "PhotovoltaicCellEfficiency";
         propertySetPhotovoltaicArray.AddEntry(ifcPSE);

         cobiePropertySets.Add(propertySetPhotovoltaicArray);
      }

      // Quantities

      /// <summary>
      /// Initializes ceiling base quantities.
      /// </summary>
      /// <param name="baseQuantities">List to store quantities.</param>
      private static void InitCeilingBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcCeilingQuantity = new QuantityDescription();
		 BuiltInParameter builtInParameter = BuiltInParameter.HOST_AREA_COMPUTED;
         QuantityEntry ifcQE;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            ifcCeilingQuantity.Name = "Qto_CoveringBaseQuantities";
            ifcQE = new QuantityEntry("NetArea", builtInParameter);
         }
         else
         {
            ifcCeilingQuantity.Name = "BaseQuantities";
            ifcQE = new QuantityEntry("GrossCeilingArea", builtInParameter);
         }
         ifcCeilingQuantity.EntityTypes.Add(IFCEntityType.IfcCovering);

         ifcQE.QuantityType = QuantityType.Area;
         ifcCeilingQuantity.AddEntry(ifcQE);

         baseQuantities.Add(ifcCeilingQuantity);
      }

      /// <summary>
      /// Initializes railing base quantities.
      /// </summary>
      /// <param name="baseQuantities">List to store quantities.</param>
      private static void InitRailingBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcRailingQuantity = new QuantityDescription();
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            ifcRailingQuantity.Name = "Qto_RailingBaseQuantities";
         }
         else
         {
            ifcRailingQuantity.Name = "BaseQuantities";
         }
         ifcRailingQuantity.EntityTypes.Add(IFCEntityType.IfcRailing);

         QuantityEntry ifcQE = new QuantityEntry("Length", BuiltInParameter.CURVE_ELEM_LENGTH);
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcRailingQuantity.AddEntry(ifcQE);

         baseQuantities.Add(ifcRailingQuantity);
      }

      /// <summary>
      /// Initializes slab base quantities.
      /// </summary>
      /// <param name="baseQuantities">List to store quantities.</param>
      private static void InitSlabBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcSlabQuantity = new QuantityDescription();
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            ifcSlabQuantity.Name = "Qto_SlabBaseQuantities";
         }
         else
         {
            ifcSlabQuantity.Name = "BaseQuantities";
         }
         ifcSlabQuantity.EntityTypes.Add(IFCEntityType.IfcSlab);

         QuantityEntry ifcQE = new QuantityEntry("GrossArea", GrossAreaCalculator.Instance);
         ifcQE.QuantityType = QuantityType.Area;
         ifcSlabQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("NetArea", NetSurfaceAreaCalculator.Instance);
         ifcQE.QuantityType = QuantityType.Area;
         ifcSlabQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossVolume", GrossVolumeCalculator.Instance);
         ifcQE.QuantityType = QuantityType.Volume;
         ifcSlabQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("NetVolume", NetVolumeCalculator.Instance);
         ifcQE.QuantityType = QuantityType.Volume;
         ifcSlabQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("Perimeter", SlabPerimeterCalculator.Instance);
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcSlabQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("Width", SlabWidthCalculator.Instance);
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcSlabQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossWeight", GrossWeightCalculator.Instance);
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcSlabQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("NetWeight", NetWeightCalculator.Instance);
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcSlabQuantity.AddEntry(ifcQE);

         baseQuantities.Add(ifcSlabQuantity);
      }

      /// <summary>
      /// Initializes ramp flight base quantities.
      /// </summary>
      /// <param name="baseQuantities">List to store quantities.</param>
      private static void InitRampFlightBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcBaseQuantity = new QuantityDescription();
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            ifcBaseQuantity.Name = "Qto_RampFlightBaseQuantities";
         }
         else
         {
            ifcBaseQuantity.Name = "BaseQuantities";
         }
         ifcBaseQuantity.EntityTypes.Add(IFCEntityType.IfcRampFlight);

         QuantityEntry ifcQE = new QuantityEntry("Width", BuiltInParameter.STAIRS_ATTR_TREAD_WIDTH);
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcBaseQuantity.AddEntry(ifcQE);

         baseQuantities.Add(ifcBaseQuantity);
      }

      /// <summary>
      /// Initializes Building Storey base quantity
      /// </summary>
      /// <param name="baseQuantities"></param>
      private static void InitBuildingStoreyBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcBaseQuantity = new QuantityDescription();
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            ifcBaseQuantity.Name = "Qto_BuildingStoreyBaseQuantities";
         }
         else
         {
            ifcBaseQuantity.Name = "BaseQuantities";
         }
         ifcBaseQuantity.EntityTypes.Add(IFCEntityType.IfcBuildingStorey);

         QuantityEntry ifcQE = new QuantityEntry("NetHeight", "IfcQtyNetHeight");
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcBaseQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossHeight", "IfcQtyGrossHeight");
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcBaseQuantity.AddEntry(ifcQE);

         ExportOptionsCache exportOptionsCache = ExporterCacheManager.ExportOptionsCache;
         if (!ExporterCacheManager.ExportOptionsCache.ExportAs2x3COBIE24DesignDeliverable)   // FMHandOver view exclude NetArea, GrossArea, NetVolume and GrossVolumne
         {
            ifcQE = new QuantityEntry("NetFloorArea" , SpaceLevelAreaCalculator.Instance);
            ifcQE.QuantityType = QuantityType.Area;
            ifcBaseQuantity.AddEntry(ifcQE);

            ifcQE = new QuantityEntry("GrossFloorArea", SpaceLevelAreaCalculator.Instance);
            ifcQE.QuantityType = QuantityType.Area;
            ifcBaseQuantity.AddEntry(ifcQE);

            ifcQE = new QuantityEntry("GrossPerimeter", "IfcQtyGrossPerimeter");
            ifcQE.QuantityType = QuantityType.PositiveLength;
            ifcBaseQuantity.AddEntry(ifcQE);

            ifcQE = new QuantityEntry("NetVolume", "IfcQtyNetVolume");
            ifcQE.QuantityType = QuantityType.Volume;
            ifcBaseQuantity.AddEntry(ifcQE);

            ifcQE = new QuantityEntry("GrossVolume", "IfcQtyGrossVolume");
            ifcQE.QuantityType = QuantityType.Volume;
            ifcBaseQuantity.AddEntry(ifcQE);
         }

         baseQuantities.Add(ifcBaseQuantity);
      }

      /// <summary>
      /// Initializes Space base quantity
      /// </summary>
      /// <param name="baseQuantities"></param>
      private static void InitSpaceBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcBaseQuantity = new QuantityDescription();
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            ifcBaseQuantity.Name = "Qto_SpaceBaseQuantities";
         }
         else
         {
            ifcBaseQuantity.Name = "BaseQuantities";
         }
         ifcBaseQuantity.EntityTypes.Add(IFCEntityType.IfcSpace);

         QuantityEntry ifcQE = new QuantityEntry("NetFloorArea", SpaceAreaCalculator.Instance);
         ifcQE.MethodOfMeasurement = "area measured in geometry";
         ifcQE.QuantityType = QuantityType.Area;
         ifcBaseQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("FinishCeilingHeight", "IfcQtyFinishCeilingHeight");
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcBaseQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("NetCeilingArea", "IfcQtyNetCeilingArea");
         ifcQE.QuantityType = QuantityType.Area;
         ifcBaseQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossCeilingArea", "IfcQtyGrossCeilingArea");
         ifcQE.QuantityType = QuantityType.Area;
         ifcBaseQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("NetWallArea", "IfcQtyNetWallArea");
         ifcQE.QuantityType = QuantityType.Area;
         ifcBaseQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossWallArea", "IfcQtyGrossWallArea");
         ifcQE.QuantityType = QuantityType.Area;
         ifcBaseQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("Height", SpaceHeightCalculator.Instance);
         ifcQE.MethodOfMeasurement = "length measured in geometry";
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcBaseQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("NetPerimeter", "IfcQtyNetPerimeter");
         ifcQE.MethodOfMeasurement = "length measured in geometry";
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcBaseQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossPerimeter", SpacePerimeterCalculator.Instance);
         ifcQE.MethodOfMeasurement = "length measured in geometry";
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcBaseQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossFloorArea", SpaceAreaCalculator.Instance);
         ifcQE.MethodOfMeasurement = "area measured in geometry";
         ifcQE.QuantityType = QuantityType.Area;
         ifcBaseQuantity.AddEntry(ifcQE);

         ExportOptionsCache exportOptionsCache = ExporterCacheManager.ExportOptionsCache;
         if (!ExporterCacheManager.ExportOptionsCache.ExportAs2x3COBIE24DesignDeliverable)   // FMHandOver view exclude GrossVolumne, FinishFloorHeight
         {
            ifcQE = new QuantityEntry("GrossVolume", SpaceVolumeCalculator.Instance);
            ifcQE.MethodOfMeasurement = "volume measured in geometry";
            ifcQE.QuantityType = QuantityType.Volume;
            ifcBaseQuantity.AddEntry(ifcQE);

            ifcQE = new QuantityEntry("FinishFloorHeight", "IfcQtyFinishFloorHeight");
            ifcQE.QuantityType = QuantityType.PositiveLength;
            ifcBaseQuantity.AddEntry(ifcQE);
         }

         baseQuantities.Add(ifcBaseQuantity);
      }

      /// <summary>
      /// Initializes Covering base quantity
      /// </summary>
      /// <param name="baseQuantities"></param>
      private static void InitCoveringBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcBaseQuantity = new QuantityDescription();
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            ifcBaseQuantity.Name = "Qto_CoveringBaseQuantities";
         }
         else
         {
            ifcBaseQuantity.Name = "BaseQuantities";
         }
         ifcBaseQuantity.EntityTypes.Add(IFCEntityType.IfcCovering);

         QuantityEntry ifcQE = new QuantityEntry("GrossArea", "IfcQtyGrossArea");
         ifcQE.QuantityType = QuantityType.Area;
         ifcBaseQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("NetArea", "IfcQtyNetArea");
         ifcQE.QuantityType = QuantityType.Area;
         ifcBaseQuantity.AddEntry(ifcQE);

         baseQuantities.Add(ifcBaseQuantity);
      }

      /// <summary>
      /// Initializes Window base quantity
      /// </summary>
      /// <param name="baseQuantities"></param>
      private static void InitWindowBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcBaseQuantity = new QuantityDescription();
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            ifcBaseQuantity.Name = "Qto_WindowBaseQuantities";
         }
         else
         {
            ifcBaseQuantity.Name = "BaseQuantities";
         }
         ifcBaseQuantity.EntityTypes.Add(IFCEntityType.IfcWindow);

         QuantityEntry ifcQE = new QuantityEntry("Height", BuiltInParameter.WINDOW_HEIGHT);
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcBaseQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("Width", BuiltInParameter.WINDOW_WIDTH);
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcBaseQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("Area", WindowAreaCalculator.Instance);
         ifcQE.MethodOfMeasurement = "area measured in geometry";
         ifcQE.QuantityType = QuantityType.Area;
         ifcBaseQuantity.AddEntry(ifcQE);

         baseQuantities.Add(ifcBaseQuantity);
      }

      /// <summary>
      /// Initializes Door base quantity
      /// </summary>
      /// <param name="baseQuantities"></param>
      private static void InitDoorBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcBaseQuantity = new QuantityDescription();
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            ifcBaseQuantity.Name = "Qto_DoorBaseQuantities";
         }
         else
         {
            ifcBaseQuantity.Name = "BaseQuantities";
         }
         ifcBaseQuantity.EntityTypes.Add(IFCEntityType.IfcDoor);

         QuantityEntry ifcQE = new QuantityEntry("Height", BuiltInParameter.DOOR_HEIGHT);
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcBaseQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("Width", BuiltInParameter.DOOR_WIDTH);
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcBaseQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("Area", DoorAreaCalculator.Instance);
         ifcQE.MethodOfMeasurement = "area measured in geometry";
         ifcQE.QuantityType = QuantityType.Area;
         ifcBaseQuantity.AddEntry(ifcQE);

         baseQuantities.Add(ifcBaseQuantity);
      }

      /// <summary>
      /// Initialize Beam Base Quantities
      /// </summary>
      /// <param name="baseQuantities"></param>
      private static void InitBeamBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcBeamQuantity = new QuantityDescription();
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            ifcBeamQuantity.Name = "Qto_BeamBaseQuantities";
         }
         else
         {
            ifcBeamQuantity.Name = "BaseQuantities";
         }
         ifcBeamQuantity.EntityTypes.Add(IFCEntityType.IfcBeam);

         QuantityEntry ifcQE = new QuantityEntry("Length", LengthCalculator.Instance);
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("CrossSectionArea", CrossSectionAreaCalculator.Instance);
         ifcQE.QuantityType = QuantityType.Area;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("OuterSurfaceArea", OuterSurfaceAreaCalculator.Instance);
         ifcQE.QuantityType = QuantityType.Area;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossSurfaceArea", GrossSurfaceAreaCalculator.Instance);
         ifcQE.QuantityType = QuantityType.Area;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("NetSurfaceArea", NetSurfaceAreaCalculator.Instance);
         ifcQE.QuantityType = QuantityType.Area;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossVolume", GrossVolumeCalculator.Instance);
         ifcQE.QuantityType = QuantityType.Volume;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("NetVolume", NetVolumeCalculator.Instance);
         ifcQE.QuantityType = QuantityType.Volume;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossWeight", GrossWeightCalculator.Instance);
         ifcQE.QuantityType = QuantityType.Weight;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("NetWeight", NetWeightCalculator.Instance);
         ifcQE.QuantityType = QuantityType.Weight;
         ifcBeamQuantity.AddEntry(ifcQE);

         baseQuantities.Add(ifcBeamQuantity);
      }

      /// <summary>
      /// Initialize Column Base Quantities
      /// </summary>
      /// <param name="baseQuantities"></param>
      private static void InitColumnBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcBeamQuantity = new QuantityDescription();
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            ifcBeamQuantity.Name = "Qto_ColumnBaseQuantities";
         }
         else
         {
            ifcBeamQuantity.Name = "BaseQuantities";
         }
         ifcBeamQuantity.EntityTypes.Add(IFCEntityType.IfcColumn);

         QuantityEntry ifcQE = new QuantityEntry("Length", LengthCalculator.Instance);
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("CrossSectionArea", CrossSectionAreaCalculator.Instance);
         ifcQE.QuantityType = QuantityType.Area;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("OuterSurfaceArea", OuterSurfaceAreaCalculator.Instance);
         ifcQE.QuantityType = QuantityType.Area;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossSurfaceArea", GrossSurfaceAreaCalculator.Instance);
         ifcQE.QuantityType = QuantityType.Area;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("NetSurfaceArea", NetSurfaceAreaCalculator.Instance);
         ifcQE.QuantityType = QuantityType.Area;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossVolume", GrossVolumeCalculator.Instance);
         ifcQE.QuantityType = QuantityType.Volume;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("NetVolume", NetVolumeCalculator.Instance);
         ifcQE.QuantityType = QuantityType.Volume;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossWeight", GrossWeightCalculator.Instance);
         ifcQE.QuantityType = QuantityType.Weight;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("NetWeight", NetWeightCalculator.Instance);
         ifcQE.QuantityType = QuantityType.Weight;
         ifcBeamQuantity.AddEntry(ifcQE);

         baseQuantities.Add(ifcBeamQuantity);
      }

      /// <summary>
      /// Initialize Member Base Quantities
      /// </summary>
      /// <param name="baseQuantities"></param>
      private static void InitMemberBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcBeamQuantity = new QuantityDescription();
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            ifcBeamQuantity.Name = "Qto_MemberBaseQuantities";
         }
         else
         {
            ifcBeamQuantity.Name = "BaseQuantities";
         }
         ifcBeamQuantity.EntityTypes.Add(IFCEntityType.IfcMember);

         QuantityEntry ifcQE = new QuantityEntry("Length", LengthCalculator.Instance);
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("CrossSectionArea", CrossSectionAreaCalculator.Instance);
         ifcQE.QuantityType = QuantityType.Area;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("OuterSurfaceArea", OuterSurfaceAreaCalculator.Instance);
         ifcQE.QuantityType = QuantityType.Area;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossSurfaceArea", GrossSurfaceAreaCalculator.Instance);
         ifcQE.QuantityType = QuantityType.Area;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("NetSurfaceArea", NetSurfaceAreaCalculator.Instance);
         ifcQE.QuantityType = QuantityType.Area;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossVolume", GrossVolumeCalculator.Instance);
         ifcQE.QuantityType = QuantityType.Volume;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("NetVolume", NetVolumeCalculator.Instance);
         ifcQE.QuantityType = QuantityType.Volume;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossWeight", GrossWeightCalculator.Instance);
         ifcQE.QuantityType = QuantityType.Weight;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("NetWeight", NetWeightCalculator.Instance);
         ifcQE.QuantityType = QuantityType.Weight;
         ifcBeamQuantity.AddEntry(ifcQE);

         baseQuantities.Add(ifcBeamQuantity);
      }

      /// <summary>
      /// Initializes base quantities.
      /// </summary>
      /// <param name="quantities">List to store quantities.</param>
      /// <param name="fileVersion">The file version, currently unused.</param>
      private static void InitBaseQuantities(IList<IList<QuantityDescription>> quantities)
      {
         IList<QuantityDescription> baseQuantities = new List<QuantityDescription>();
         InitCeilingBaseQuantities(baseQuantities);
         InitRailingBaseQuantities(baseQuantities);
         InitSlabBaseQuantities(baseQuantities);
         InitRampFlightBaseQuantities(baseQuantities);
         InitBuildingStoreyBaseQuantities(baseQuantities);
         InitSpaceBaseQuantities(baseQuantities);
         InitCoveringBaseQuantities(baseQuantities);
         InitWindowBaseQuantities(baseQuantities);
         InitDoorBaseQuantities(baseQuantities);
         InitBeamBaseQuantities(baseQuantities);

         // TODO: Make this work with split columns by wall.
         //InitColumnBaseQuantities(baseQuantities);
         InitMemberBaseQuantities(baseQuantities);

         quantities.Add(baseQuantities);
      }

      /// <summary>
      /// Initializes COBIE quantities.
      /// </summary>
      /// <param name="quantities">List to store quantities.</param>
      /// <param name="fileVersion">The file version, currently unused.</param>
      private static void InitCOBIEQuantities(IList<IList<QuantityDescription>> quantities)
      {
         IList<QuantityDescription> cobieQuantities = new List<QuantityDescription>();
         InitCOBIESpaceQuantities(cobieQuantities);
         InitCOBIESpaceLevelQuantities(cobieQuantities);
         InitCOBIEPMSpaceQuantities(cobieQuantities);
         quantities.Add(cobieQuantities);
      }

      /// <summary>
      /// Initializes COBIE space quantities.
      /// </summary>
      /// <param name="cobieQuantities">List to store quantities.</param>
      private static void InitCOBIESpaceQuantities(IList<QuantityDescription> cobieQuantities)
      {
         QuantityDescription ifcCOBIEQuantity = new QuantityDescription();
         ifcCOBIEQuantity.Name = "BaseQuantities";
         ifcCOBIEQuantity.EntityTypes.Add(IFCEntityType.IfcSpace);

         QuantityEntry ifcQE = new QuantityEntry("Height", SpaceHeightCalculator.Instance);
         ifcQE.MethodOfMeasurement = "length measured in geometry";
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcCOBIEQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossPerimeter", SpacePerimeterCalculator.Instance);
         ifcQE.MethodOfMeasurement = "length measured in geometry";
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcCOBIEQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossFloorArea", SpaceAreaCalculator.Instance);
         ifcQE.MethodOfMeasurement = "area measured in geometry";
         ifcQE.QuantityType = QuantityType.Area;
         ifcCOBIEQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("NetFloorArea", SpaceAreaCalculator.Instance);
         ifcQE.MethodOfMeasurement = "area measured in geometry";
         ifcQE.QuantityType = QuantityType.Area;
         ifcCOBIEQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossVolume", SpaceVolumeCalculator.Instance);
         ifcQE.MethodOfMeasurement = "volume measured in geometry";
         ifcQE.QuantityType = QuantityType.Volume;
         ifcCOBIEQuantity.AddEntry(ifcQE);

         cobieQuantities.Add(ifcCOBIEQuantity);
      }

      /// <summary>
      /// Initializes COBIE space level quantities.
      /// </summary>
      /// <param name="cobieQuantities">List to store quantities.</param>
      private static void InitCOBIESpaceLevelQuantities(IList<QuantityDescription> cobieQuantities)
      {
         QuantityDescription ifcCOBIEQuantity = new QuantityDescription();
         ifcCOBIEQuantity.Name = "BaseQuantities";
         ifcCOBIEQuantity.EntityTypes.Add(IFCEntityType.IfcSpace);
         ifcCOBIEQuantity.DescriptionCalculator = SpaceLevelDescriptionCalculator.Instance;

         QuantityEntry ifcQE = new QuantityEntry("GrossFloorArea", SpaceLevelAreaCalculator.Instance);
         ifcQE.MethodOfMeasurement = "area measured in geometry";
         ifcQE.QuantityType = QuantityType.Area;
         ifcCOBIEQuantity.AddEntry(ifcQE);

         cobieQuantities.Add(ifcCOBIEQuantity);
      }

      /// <summary>
      /// Initializes COBIE BM space quantities.
      /// </summary>
      /// <param name="cobieQuantities">List to store quantities.</param>
      private static void InitCOBIEPMSpaceQuantities(IList<QuantityDescription> cobieQuantities)
      {
         QuantityDescription ifcCOBIEQuantity = new QuantityDescription();
         ifcCOBIEQuantity.Name = "Space Quantities (Property Management)";
         ifcCOBIEQuantity.MethodOfMeasurement = "As defined by BOMA (see www.boma.org)";
         ifcCOBIEQuantity.EntityTypes.Add(IFCEntityType.IfcSpace);

         QuantityEntry ifcQE = new QuantityEntry("NetFloorArea_BOMA", SpaceAreaCalculator.Instance);
         ifcQE.MethodOfMeasurement = "area measured in geometry";
         ifcQE.QuantityType = QuantityType.Area;
         ifcCOBIEQuantity.AddEntry(ifcQE);

         cobieQuantities.Add(ifcCOBIEQuantity);
      }
   }
}