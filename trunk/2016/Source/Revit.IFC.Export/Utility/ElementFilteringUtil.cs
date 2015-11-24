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
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB.Structure;
using Revit.IFC.Export.Exporter;

namespace Revit.IFC.Export.Utility
{
    /// <summary>
    /// IFC export type.
    /// </summary>
    public enum IFCExportType
    {
        /// <summary>
        /// This is the default "Don't Export" that could be overwritten by other methods.
        /// </summary>
        DontExport,
        /// <summary>
        /// Annotation.
        /// </summary>
        IfcAnnotation,
        /// <summary>
        /// Beam.
        /// </summary>
        IfcBeam,
        /// <summary>
        /// Building element part.
        /// </summary>
        IfcBuildingElementPart,
        /// <summary>
        /// Building element proxy.
        /// </summary>
        IfcBuildingElementProxy,
        /// <summary>
        /// Building element proxy type.
        /// </summary>
        IfcBuildingElementProxyType,
        /// <summary>
        /// Building storey.
        /// </summary>
        IfcBuildingStorey,
        /// <summary>
        /// Column type.
        /// </summary>
        IfcColumnType,
        /// <summary>
        /// Covering.
        /// </summary>
        IfcCovering,
        /// <summary>
        /// Curtain wall.
        /// </summary>
        IfcCurtainWall,
        /// <summary>
        /// Door type.
        /// </summary>
        IfcDoorType,
        /// <summary>
        /// Assembly.
        /// </summary>
        IfcElementAssembly,
        /// <summary>
        /// Footing.
        /// </summary>
        IfcFooting,
        /// <summary>
        /// Member type.
        /// </summary>
        IfcMemberType,
        /// <summary>
        /// Opening element.
        /// </summary>
        IfcOpeningElement,
        /// <summary>
        /// Plate type.
        /// </summary>
        IfcPlateType,
        /// <summary>
        /// Railing.
        /// </summary>
        IfcRailing,
        /// <summary>
        /// Railing type.
        /// </summary>
        IfcRailingType,
        /// <summary>
        /// Ramp.
        /// </summary>
        IfcRamp,
        /// <summary>
        /// Roof.
        /// </summary>
        IfcRoof,
        /// <summary>
        /// Site.
        /// </summary>
        IfcSite,
        /// <summary>
        /// Slab.
        /// </summary>
        IfcSlab,
        /// <summary>
        /// Space.
        /// </summary>
        IfcSpace,
        /// <summary>
        /// Stair.
        /// </summary>
        IfcStair,
        /// <summary>
        /// Transport element type.
        /// </summary>
        IfcTransportElementType,
        /// <summary>
        /// Wall.
        /// </summary>
        IfcWall,
        /// <summary>
        /// Reinforcing bar.
        /// </summary>
        IfcReinforcingBar,
        /// <summary>
        /// Reinforcing mesh.
        /// </summary>
        IfcReinforcingMesh,
        /// <summary>
        /// Window type.
        /// </summary>
        IfcWindowType,
        /// <summary>
        /// Furnishing element and their subclasses.
        /// </summary>
        IfcFurnishingElement,
        /// <summary>
        /// Direct subclass of FurnishingElementType.
        /// </summary>
        IfcFurnitureType,
        /// <summary>
        /// System furniture element type.
        /// </summary>
        IfcSystemFurnitureElementType,
        /// <summary>
        /// Distribution elements and their subclasses.
        /// </summary>
        IfcDistributionElement,
        /// <summary>
        /// Direct subclasses of DistributionElement.
        /// </summary>
        IfcDistributionControlElement,
        /// <summary>
        /// Distribution flow element.
        /// </summary>
        IfcDistributionFlowElement,
        /// <summary>
        /// Distribution Chamber Element
        /// </summary>
        IfcDistributionChamberElement,
        /// <summary>
        /// Direct subclass of DistributionFlowElement.
        /// </summary>
        IfcDistributionChamberElementType,
        /// <summary>
        /// Energy conversion device.
        /// </summary>
        IfcEnergyConversionDevice,
        /// <summary>
        /// Flow fitting.
        /// </summary>
        IfcFlowFitting,
        /// <summary>
        /// Flow moving device.
        /// </summary>
        IfcFlowMovingDevice,
        /// <summary>
        /// Flow segment.
        /// </summary>
        IfcFlowSegment,
        /// <summary>
        /// Flow storage device.
        /// </summary>
        IfcFlowStorageDevice,
        /// <summary>
        /// Flow terminal.
        /// </summary>
        IfcFlowTerminal,
        /// <summary>
        /// Flow treatment device.
        /// </summary>
        IfcFlowTreatmentDevice,
        /// <summary>
        /// Flow controller.
        /// </summary>
        IfcFlowController,
        // direct subclass of FlowController
        // types of DistributionControlElementType
        /// <summary>
        /// Actuator type.
        /// </summary>
        IfcActuatorType,
        /// <summary>
        /// Alarm type.
        /// </summary>
        IfcAlarmType,
        /// <summary>
        /// Controller type.
        /// </summary>
        IfcControllerType,
        /// <summary>
        /// Flow instrument type.
        /// </summary>
        IfcFlowInstrumentType,
        /// <summary>
        /// Protective Device Tripping Unit type (new in IFC4)
        /// </summary>
        IfcProtectiveDeviceTrippingUnitType,
        /// <summary>
        /// Sensor type.
        /// </summary>
        IfcSensorType,
        /// <summary>
        /// Unitary Control Element type (new in IFC4)
        /// </summary>
        IfcUnitaryControlElementType,
        // types of EnergyConversionDeviceType
        /// <summary>
        /// Air to air heat recovery type.
        /// </summary>
        IfcAirToAirHeatRecoveryType,
        /// <summary>
        /// Boiler type.
        /// </summary>
        IfcBoilerType,
        /// <summary>
        /// Chiller type.
        /// </summary>
        IfcChillerType,
        /// <summary>
        /// Coil type.
        /// </summary>
        IfcCoilType,
        /// <summary>
        /// Condenser type.
        /// </summary>
        IfcCondenserType,
        /// <summary>
        /// Cooled beam type.
        /// </summary>
        IfcCooledBeamType,
        /// <summary>
        /// Cooling tower type.
        /// </summary>
        IfcCoolingTowerType,
        /// <summary>
        /// Electric generator type.
        /// </summary>
        IfcElectricGeneratorType,
        /// <summary>
        /// Electric motor type.
        /// </summary>
        IfcElectricMotorType,
        /// <summary>
        /// Engine type (new in IFC4)
        /// </summary>
        IfcEngineType,
        /// <summary>
        /// Evaporative cooler type.
        /// </summary>
        IfcEvaporativeCoolerType,
        /// <summary>
        /// Evaporator type.
        /// </summary>
        IfcEvaporatorType,
        /// <summary>
        /// Heat exchanger type.
        /// </summary>
        IfcHeatExchangerType,
        /// <summary>
        /// Humidifier type.
        /// </summary>
        IfcHumidifierType,
        /// <summary>
        /// Motor connection type.
        /// </summary>
        IfcMotorConnectionType,
        /// <summary>
        /// Solar Device type (new in IFC4)
        /// </summary>
        IfcSolarDeviceType,
        /// <summary>
        /// Transformer type.
        /// </summary>
        IfcTransformerType,
        /// <summary>
        /// Tube bundle type.
        /// </summary>
        IfcTubeBundleType,
        /// <summary>
        /// Unitary equipment type.
        /// </summary>
        IfcUnitaryEquipmentType,
        // types of FlowControllerType
        /// <summary>
        /// Air terminal box type.
        /// </summary>
        IfcAirTerminalBoxType,
        /// <summary>
        /// Damper type.
        /// </summary>
        IfcDamperType,
        /// <summary>
        /// Electric Distribution Board type (new in IFC4)
        /// </summary>
        IfcElectricDistributionBoardType,
        /// <summary>
        /// Electric time control type.
        /// </summary>
        IfcElectricTimeControlType,
        /// <summary>
        /// Flow meter type.
        /// </summary>
        IfcFlowMeterType,
        /// <summary>
        /// Protective device type.
        /// </summary>
        IfcProtectiveDeviceType,
        /// <summary>
        /// Switching device type.
        /// </summary>
        IfcSwitchingDeviceType,
        /// <summary>
        /// Valve type.
        /// </summary>
        IfcValveType,
        // types of FlowFittingType
        /// <summary>
        /// Cable carrier fitting type.
        /// </summary>
        IfcCableCarrierFittingType,
        /// <summary>
        /// Cable Fitting type (new in IFC4)
        /// </summary>
        IfcCableFittingType,
        /// <summary>
        /// Duct fitting type.
        /// </summary>
        IfcDuctFittingType,
        /// <summary>
        /// Junction box type.
        /// </summary>
        IfcJunctionBoxType,
        /// <summary>
        /// Pipe fitting type.
        /// </summary>
        IfcPipeFittingType,
        // types of FlowMovingDeviceType
        /// <summary>
        /// Compressor type.
        /// </summary>
        IfcCompressorType,
        /// <summary>
        /// Fan type.
        /// </summary>
        IfcFanType,
        /// <summary>
        /// Pump type.
        /// </summary>
        IfcPumpType,
        // types of FlowSegmentType
        /// <summary>
        /// Cable carrier segment type.
        /// </summary>
        IfcCableCarrierSegmentType,
        /// <summary>
        /// Cable segment type.
        /// </summary>
        IfcCableSegmentType,
        /// <summary>
        /// Duct segment type.
        /// </summary>
        IfcDuctSegmentType,
        /// <summary>
        /// Pipe segment type.
        /// </summary>
        IfcPipeSegmentType,
        // types of FlowStorageDeviceType
        /// <summary>
        /// Electric flow storage device type.
        /// </summary>
        IfcElectricFlowStorageDeviceType,
        /// <summary>
        /// Tank type.
        /// </summary>
        IfcTankType,
        // types of FlowTreatmentDeviceType
        /// <summary>
        /// Duct silencer type.
        /// </summary>
        IfcDuctSilencerType,
        /// <summary>
        /// Filter type.
        /// </summary>
        IfcFilterType,
        /// <summary>
        /// Interceptor type (new in IFC4)
        /// </summary>
        IfcInterceptorType,
        // types of FlowTerminalType
        /// <summary>
        /// Air terminal type.
        /// </summary>
        IfcAirTerminalType,
        /// <summary>
        /// Audio Visual Applicance type (new in IFC4)
        /// </summary>
        IfcAudioVisualAppliance,
        /// <summary>
        /// Burner type. (IFC2x3: IfcGasTerminalType)
        /// </summary>
        IfcBurnerType,
        /// <summary>
        /// Communication Applicance type (new in IFC4)
        /// </summary>
        IfcCommunicationAppliance,
        /// <summary>
        /// Electric appliance type.
        /// </summary>
        IfcElectricApplianceType,
        /// <summary>
        /// Electic heater type (IFC2x3 only)
        /// </summary>
        IfcElectricHeaterType,
        /// <summary>
        /// Fire suppression terminal type.
        /// </summary>
        IfcFireSuppressionTerminalType,
        /// <summary>
        /// Lamp type.
        /// </summary>
        IfcLampType,
        /// <summary>
        /// Light fixture type.
        /// </summary>
        IfcLightFixtureType,
        /// <summary>
        /// Medical Device type (new in IFC4)
        /// </summary>
        IfcMedicalDeviceType,
        /// <summary>
        /// Outlet type.
        /// </summary>
        IfcOutletType,
        /// <summary>
        /// Sanitary terminal type.
        /// </summary>
        IfcSanitaryTerminalType,
        /// <summary>
        /// Space heater type
        /// </summary>
        IfcSpaceHeaterType,
        /// <summary>
        /// Stack terminal type.
        /// </summary>
        IfcStackTerminalType,
        /// <summary>
        /// Waste terminal type.
        /// </summary>
        IfcWasteTerminalType,
        /// <summary>
        /// Fastener type.
        /// </summary>
        IfcFastenerType,
        /// <summary>
        /// MechanicalFastener type.
        /// </summary>
        IfcMechanicalFastenerType,
        /// <summary>
        /// Pile - no type in IFC2x3.
        /// </summary>
        IfcPile,
        /// <summary>
        /// Zone - no type in IFC2x3.
        /// </summary>
        IfcZone,
        /// <summary>
        /// Grid - no type in IFC2x3.
        /// </summary>
        IfcGrid,
        /// DiscreteAccessory type.
        /// </summary>
        IfcDiscreteAccessoryType,
        /// <summary>
        /// System
        /// </summary>
        IfcSystem,
        /// <summary>
        /// Group
        /// </summary>
        IfcGroup,
        /// <summary>
        /// Assembly
        /// </summary>
        IfcAssembly,
    }


    /// <summary>
    /// Provides static methods for filtering elements.
    /// </summary>
    class ElementFilteringUtil
    {
        /// <summary>
        /// Gets spatial element filter.
        /// </summary>
        /// <param name="document">The Revit document.</param>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <returns>The element filter.</returns>
        public static ElementFilter GetSpatialElementFilter(Document document, ExporterIFC exporterIFC)
        {
            return GetExportFilter(document, exporterIFC, true);
        }

        /// <summary>
        /// Gets filter for non spatial elements.
        /// </summary>
        /// <param name="document">The Revit document.</param>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <returns>The Element filter.</returns>
        public static ElementFilter GetNonSpatialElementFilter(Document document, ExporterIFC exporterIFC)
        {
            return GetExportFilter(document, exporterIFC, false);
        }

        /// <summary>
        /// Gets element filter for export.
        /// </summary>
        /// <param name="document">The Revit document.</param>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="forSpatialElements">True to get spatial element filter, false for non spatial elements filter.</param>
        /// <returns>The element filter.</returns>
        private static ElementFilter GetExportFilter(Document document, ExporterIFC exporterIFC, bool forSpatialElements)
        {
            List<ElementFilter> filters = new List<ElementFilter>();

            // Class types & categories
            ElementFilter classFilter = GetClassFilter(forSpatialElements);

            // Special handling for family instances and view specific elements
            if (!forSpatialElements)
            {
                ElementFilter familyInstanceFilter = GetFamilyInstanceFilter(exporterIFC);

                List<ElementFilter> classFilters = new List<ElementFilter>();
                classFilters.Add(classFilter);
                classFilters.Add(familyInstanceFilter);

                if (ExporterCacheManager.ExportOptionsCache.ExportAnnotations)
                {
                    ElementFilter ownerViewFilter = GetViewSpecificTypesFilter(exporterIFC);
                    classFilters.Add(ownerViewFilter);
                }

                classFilter = new LogicalOrFilter(classFilters);
            }

            filters.Add(classFilter);

            // Design options
            filters.Add(GetDesignOptionFilter());

            // Phases: only for non-spatial elements.  For spatial elements, we will do a check afterwards.
            if (!forSpatialElements && !ExporterCacheManager.ExportOptionsCache.ExportingLink)
                filters.Add(GetPhaseStatusFilter(document));

            return new LogicalAndFilter(filters);
        }

        /// <summary>
        /// Gets element filter for family instance.
        /// </summary>
        /// <param name="exporter">The ExporterIFC object.</param>
        /// <returns>The element filter.</returns>
        private static ElementFilter GetFamilyInstanceFilter(ExporterIFC exporter)
        {
            List<ElementFilter> filters = new List<ElementFilter>();
            filters.Add(new ElementOwnerViewFilter(ElementId.InvalidElementId));
            filters.Add(new ElementClassFilter(typeof(FamilyInstance)));
            LogicalAndFilter andFilter = new LogicalAndFilter(filters);

            return andFilter;
        }

        /// <summary>
        /// Gets element filter meeting design option requirements.
        /// </summary>
        /// <returns>The element filter.</returns>
        private static ElementFilter GetDesignOptionFilter()
        {
            // We will respect the active design option if we are exporting a specific view.
            ElementFilter noDesignOptionFilter = new ElementDesignOptionFilter(ElementId.InvalidElementId);
            ElementFilter primaryOptionsFilter = new PrimaryDesignOptionMemberFilter();
            ElementFilter designOptionFilter = new LogicalOrFilter(noDesignOptionFilter, primaryOptionsFilter);

            View filterView = ExporterCacheManager.ExportOptionsCache.FilterViewForExport;
            if (filterView != null)
            {
                ElementId designOptionId = DesignOption.GetActiveDesignOptionId(ExporterCacheManager.Document);
                if (designOptionId != ElementId.InvalidElementId)
                {
                    ElementFilter activeDesignOptionFilter = new ElementDesignOptionFilter(designOptionId);
                    return new LogicalOrFilter(designOptionFilter, activeDesignOptionFilter);
                }
            }

            return designOptionFilter;
        }

        // Cannot be implemented until ExportLayerTable can be read.  Replacement is ShouldCategoryBeExported()
        /*private static ElementFilter GetCategoryFilter()
        {
            
        }*/

        /// <summary>
        /// Checks if element in certain category should be exported.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="element">The element.</param>
        /// <param name="allowSeparateOpeningExport">True if IfcOpeningElement is allowed to be exported.</param>
        /// <returns>True if the element should be exported, false otherwise.</returns>
        private static bool ShouldCategoryBeExported(ExporterIFC exporterIFC, Element element, bool allowSeparateOpeningExport)
        {
            IFCExportType exportType = IFCExportType.DontExport;
            ElementId categoryId;
            string ifcClassName = ExporterUtil.GetIFCClassNameFromExportTable(exporterIFC, element, out categoryId);
            if (string.IsNullOrEmpty(ifcClassName))
            {
                // Special case: these elements aren't contained in the default export layers mapping table.
                // This allows these elements to be exported by default.
                if (element is AreaScheme || element is Group)
                    ifcClassName = "IfcGroup";
                else if (element is ElectricalSystem)
                    ifcClassName = "IfcSystem";
                else
                    return false;
            }

            bool foundName = string.Compare(ifcClassName, "Default", true) != 0;
            if (foundName)
                exportType = GetExportTypeFromClassName(ifcClassName);
            if (!foundName)
                return true;

            if (exportType == IFCExportType.DontExport)
                return false;

            // We don't export openings directly, only via the element they are opening, unless flag is set.
            if (exportType == IFCExportType.IfcOpeningElement && !allowSeparateOpeningExport)
                return false;

            return true;
        }

        /// <summary>
        /// Checks if element should be exported by checking IfcExportAs.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="element">The element.</param>
        /// <param name="allowSeparateOpeningExport">True if IfcOpeningElement is allowed to be exported.</param>
        /// <returns>True if the element should be exported, false otherwise.</returns>
        public static bool ShouldElementBeExported(ExporterIFC exporterIFC, Element element, bool allowSeparateOpeningExport)
        {
            if (ExporterStateManager.CanExportElementOverride())
                return true;

            if (!ShouldCategoryBeExported(exporterIFC, element, allowSeparateOpeningExport))
                return false;

            string exportAsEntity = "IFCExportAs";
            string elementClassName;
            if (ParameterUtil.GetStringValueFromElementOrSymbol(element, exportAsEntity, out elementClassName) != null)
            {
                if (CompareAlphaOnly(elementClassName, "DONTEXPORT"))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Determines if the selected element meets extra criteria for export.
        /// </summary>
        /// <param name="exporterIFC">The exporter class.</param>
        /// <param name="element">The current element to export.</param>
        /// <param name="allowSeparateOpeningExport">True if IfcOpeningElement is allowed to be exported.</param>
        /// <returns>True if the element should be exported, false otherwise.</returns>
        public static bool CanExportElement(ExporterIFC exporterIFC, Autodesk.Revit.DB.Element element, bool allowSeparateOpeningExport)
        {
            if (!ElementFilteringUtil.ShouldElementBeExported(exporterIFC, element, allowSeparateOpeningExport))
                return false;

            // if we allow exporting parts as independent building elements, then prevent also exporting the host elements containing the parts.
            if (ExporterCacheManager.ExportOptionsCache.ExportPartsAsBuildingElements && PartExporter.CanExportParts(element))
                return false;

            return true;
        }

        /// <summary>
        /// Checks if name is equal to base or its type name.
        /// </summary>
        /// <param name="name">The object type name.</param>
        /// <param name="baseName">The IFC base name.</param>
        /// <returns>True if equal, false otherwise.</returns>
        private static bool IsEqualToTypeName(String name, String baseName)
        {
            if (String.Compare(name, baseName, true) == 0)
                return true;

            String typeName = baseName + "Type";
            return (String.Compare(name, typeName, true) == 0);
        }

        /// <summary>
        /// Compares two strings, ignoring spaces, punctuation and case.
        /// </summary>
        /// <param name="name">The string to compare.</param>
        /// <param name="baseNameAllCapsNoSpaces">String to compare to, all caps, no punctuation or cases.</param>
        /// <returns></returns>
        private static bool CompareAlphaOnly(String name, String baseNameAllCapsNoSpaces)
        {
            string nameToUpper = name.ToUpper();
            int loc = 0;
            int maxLen = baseNameAllCapsNoSpaces.Length;
            foreach (char c in nameToUpper)
            {
                if (c >= 'A' && c <= 'Z')
                {
                    if (baseNameAllCapsNoSpaces[loc] != c)
                        return false;
                    loc++;
                    if (loc == maxLen)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets export type from IFC class name.
        /// </summary>
        /// <param name="ifcClassName">The IFC class name.</param>
        /// <returns>The export type.</returns>
        public static IFCExportType GetExportTypeFromClassName(String ifcClassName)
        {
            if (ifcClassName.StartsWith("Ifc", true, null))
            {
                if (String.Compare(ifcClassName, "IfcAnnotation", true) == 0)
                {
                    // Used to mark curves, text, and filled regions for export.
                    return IFCExportType.IfcAnnotation;
                }
                else if (String.Compare(ifcClassName, "IfcAssembly", true) == 0)
                    return IFCExportType.IfcAssembly;
                else if (String.Compare(ifcClassName, "IfcBeam", true) == 0)
                    return IFCExportType.IfcBeam;
                else if (String.Compare(ifcClassName, "IfcBuildingElementPart", true) == 0)
                    return IFCExportType.IfcBuildingElementPart;
                else if (IsEqualToTypeName(ifcClassName, ("IfcBuildingElementProxy")))
                    return IFCExportType.IfcBuildingElementProxyType;
                else if (String.Compare(ifcClassName, "IfcBuildingStorey", true) == 0)
                    return IFCExportType.IfcBuildingStorey;  // Only to be used for exporting level information!
                else if (IsEqualToTypeName(ifcClassName, ("IfcColumn")))
                    return IFCExportType.IfcColumnType;
                else if (String.Compare(ifcClassName, "IfcCovering", true) == 0)
                    return IFCExportType.IfcCovering;
                else if (String.Compare(ifcClassName, "IfcCurtainWall", true) == 0)
                    return IFCExportType.IfcCurtainWall;
                else if (IsEqualToTypeName(ifcClassName, ("IfcDoor")))
                    return IFCExportType.IfcDoorType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcElementAssembly")))
                    return IFCExportType.IfcElementAssembly;
                else if (String.Compare(ifcClassName, "IfcFloor", true) == 0)
                {
                    // IfcFloor is not a real type, but is being kept for backwards compatibility as a typo.
                    return IFCExportType.IfcSlab;
                }
                else if (IsEqualToTypeName(ifcClassName, ("IfcFooting")))
                    return IFCExportType.IfcFooting;
                else if (String.Compare(ifcClassName, "IfcGrid", true) == 0)
                    return IFCExportType.IfcGrid;
                else if (String.Compare(ifcClassName, "IfcGroup", true) == 0)
                    return IFCExportType.IfcGroup;
                else if (String.Compare(ifcClassName, "IfcMember", true) == 0)
                    return IFCExportType.IfcMemberType;
                else if (String.Compare(ifcClassName, "IfcOpeningElement", true) == 0)
                {
                    // Used to mark reveals for export.
                    return IFCExportType.IfcOpeningElement;
                }
                else if (String.Compare(ifcClassName, "IfcPlate", true) == 0)
                    return IFCExportType.IfcPlateType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcRailing")))
                    return IFCExportType.IfcRailingType;
                else if (String.Compare(ifcClassName, "IfcRamp", true) == 0)
                    return IFCExportType.IfcRamp;
                else if (String.Compare(ifcClassName, "IfcRoof", true) == 0)
                    return IFCExportType.IfcRoof;
                else if (String.Compare(ifcClassName, "IfcSite", true) == 0)
                    return IFCExportType.IfcSite;
                else if (String.Compare(ifcClassName, "IfcSlab", true) == 0)
                    return IFCExportType.IfcSlab;
                else if (String.Compare(ifcClassName, "IfcSpace", true) == 0)
                {
                    // Not a real type; used to mark rooms for export.
                    return IFCExportType.IfcSpace;
                }
                else if (String.Compare(ifcClassName, "IfcStair", true) == 0)
                    return IFCExportType.IfcStair;
                else if (String.Compare(ifcClassName, "IfcSystem", true) == 0)
                    return IFCExportType.IfcSystem;
                else if (IsEqualToTypeName(ifcClassName, ("IfcTransportElement")))
                    return IFCExportType.IfcTransportElementType;
                else if ((String.Compare(ifcClassName, "IfcWall", true) == 0) ||
                         (String.Compare(ifcClassName, "IfcWallStandardCase", true) == 0))
                    return IFCExportType.IfcWall;
                else if (IsEqualToTypeName(ifcClassName, ("IfcWindow")))
                    return IFCExportType.IfcWindowType;
                // furnishing type(s) to be exported as geometry, not mapped item
                else if (IsEqualToTypeName(ifcClassName, ("IfcFurnishingElement")))
                    return IFCExportType.IfcFurnishingElement;
                // furnishing types to be exported as mapped item
                else if (IsEqualToTypeName(ifcClassName, ("IfcFurniture")))
                    return IFCExportType.IfcFurnitureType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcSystemFurnitureElement")))
                    return IFCExportType.IfcSystemFurnitureElementType;
                // distribution types to be exported as geometry, not mapped item
                else if (IsEqualToTypeName(ifcClassName, ("IfcDistributionElement")))
                    return IFCExportType.IfcDistributionElement;
                else if (IsEqualToTypeName(ifcClassName, ("IfcDistributionControlElement")))
                    return IFCExportType.IfcDistributionControlElement;
                else if (IsEqualToTypeName(ifcClassName, ("IfcDistributionFlowElement")))
                    return IFCExportType.IfcDistributionFlowElement;
                else if (IsEqualToTypeName(ifcClassName, ("IfcEnergyConversionDevice")))
                    return IFCExportType.IfcEnergyConversionDevice;
                else if (IsEqualToTypeName(ifcClassName, ("IfcFlowFitting")))
                    return IFCExportType.IfcFlowFitting;
                else if (IsEqualToTypeName(ifcClassName, ("IfcFlowMovingDevice")))
                    return IFCExportType.IfcFlowMovingDevice;
                else if (IsEqualToTypeName(ifcClassName, ("IfcFlowSegment")))
                    return IFCExportType.IfcFlowSegment;
                else if (IsEqualToTypeName(ifcClassName, ("IfcFlowSegmentStorageDevice")))
                    return IFCExportType.IfcFlowStorageDevice;
                else if (IsEqualToTypeName(ifcClassName, ("IfcFlowTerminal")))
                    return IFCExportType.IfcFlowTerminal;
                else if (IsEqualToTypeName(ifcClassName, ("IfcFlowTreatmentDevice")))
                    return IFCExportType.IfcFlowTreatmentDevice;
                else if (IsEqualToTypeName(ifcClassName, ("IfcFlowController")))
                    return IFCExportType.IfcFlowController;
                // distribution types to be exported as mapped item
                else if (IsEqualToTypeName(ifcClassName, ("IfcActuator")))
                    return IFCExportType.IfcActuatorType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcAirTerminalBox")))
                    return IFCExportType.IfcAirTerminalBoxType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcAirTerminal")))
                    return IFCExportType.IfcAirTerminalType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcAirToAirHeatRecovery")))
                    return IFCExportType.IfcAirToAirHeatRecoveryType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcAlarm")))
                    return IFCExportType.IfcAlarmType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcBoiler")))
                    return IFCExportType.IfcBoilerType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcBurner")) || 
                   IsEqualToTypeName(ifcClassName, ("IfcGasTerminal")))
                {
                   // IFC2x3 IfcGasTerminalType has been renamed to IfcBurnerType in IFC4.
                   return IFCExportType.IfcBurnerType;
                }
                else if (IsEqualToTypeName(ifcClassName, ("IfcCableCarrierFitting")))
                    return IFCExportType.IfcCableCarrierFittingType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcCableCarrierSegment")))
                    return IFCExportType.IfcCableCarrierSegmentType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcCableSegment")))
                    return IFCExportType.IfcCableSegmentType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcChiller")))
                    return IFCExportType.IfcChillerType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcCoil")))
                    return IFCExportType.IfcCoilType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcCompressor")))
                    return IFCExportType.IfcCompressorType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcCondenser")))
                    return IFCExportType.IfcCondenserType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcController")))
                    return IFCExportType.IfcControllerType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcCooledBeam")))
                    return IFCExportType.IfcCooledBeamType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcCoolingTower")))
                    return IFCExportType.IfcCoolingTowerType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcDamper")))
                    return IFCExportType.IfcDamperType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcDiscreteAccessory")))
                    return IFCExportType.IfcDiscreteAccessoryType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcDistributionChamberElement")))
                    return IFCExportType.IfcDistributionChamberElementType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcDuctFitting")))
                    return IFCExportType.IfcDuctFittingType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcDuctSegment")))
                    return IFCExportType.IfcDuctSegmentType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcDuctSilencer")))
                    return IFCExportType.IfcDuctSilencerType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcElectricAppliance")))
                    return IFCExportType.IfcElectricApplianceType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcElectricDistributionPoint")) || 
                   IsEqualToTypeName(ifcClassName, ("IfcElectricDistributionBoard")))
                {
                   // IFC2x3 IfcElectricDistributionPoint has been renamed to IfcElectricDistributionBoard in IFC4.
                   // IFC2x3 IfcElectricDistributionBoardType has been added to IFC4.
                   return IFCExportType.IfcElectricDistributionBoardType;
                }
                else if (IsEqualToTypeName(ifcClassName, ("IfcElectricFlowStorageDevice")))
                    return IFCExportType.IfcElectricFlowStorageDeviceType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcElectricGenerator")))
                    return IFCExportType.IfcElectricGeneratorType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcElectricHeater")))
                {
                   // IFC4 IfcElectricHeaterType is obsolete and should be replaced by IfcSpaceHeaterType.
                   if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
                      return IFCExportType.IfcSpaceHeaterType;
                   else
                     return IFCExportType.IfcElectricHeaterType;
                }
                else if (IsEqualToTypeName(ifcClassName, ("IfcElectricMotor")))
                    return IFCExportType.IfcElectricMotorType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcElectricTimeControl")))
                    return IFCExportType.IfcElectricTimeControlType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcEnergyConversionDevice")))
                    return IFCExportType.IfcEnergyConversionDevice;
                else if (IsEqualToTypeName(ifcClassName, ("IfcEvaporativeCooler")))
                    return IFCExportType.IfcEvaporativeCoolerType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcEvaporator")))
                    return IFCExportType.IfcEvaporatorType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcFastener")))
                    return IFCExportType.IfcFastenerType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcFan")))
                    return IFCExportType.IfcFanType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcFilter")))
                    return IFCExportType.IfcFilterType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcFireSuppressionTerminal")))
                    return IFCExportType.IfcFireSuppressionTerminalType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcFlowController")))
                    return IFCExportType.IfcFlowController;
                else if (IsEqualToTypeName(ifcClassName, ("IfcFlowFitting")))
                    return IFCExportType.IfcFlowFitting;
                else if (IsEqualToTypeName(ifcClassName, ("IfcFlowInstrument")))
                    return IFCExportType.IfcFlowInstrumentType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcFlowMeter")))
                    return IFCExportType.IfcFlowMeterType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcFlowMovingDevice")))
                    return IFCExportType.IfcFlowMovingDevice;
                else if (IsEqualToTypeName(ifcClassName, ("IfcFlowSegment")))
                    return IFCExportType.IfcFlowSegment;
                else if (IsEqualToTypeName(ifcClassName, ("IfcFlowStorageDevice")))
                    return IFCExportType.IfcFlowStorageDevice;
                else if (IsEqualToTypeName(ifcClassName, ("IfcFlowTerminal")))
                    return IFCExportType.IfcFlowTerminal;
                else if (IsEqualToTypeName(ifcClassName, ("IfcFlowTreatmentDevice")))
                    return IFCExportType.IfcFlowTreatmentDevice;
                else if (IsEqualToTypeName(ifcClassName, ("IfcHeatExchanger")))
                    return IFCExportType.IfcHeatExchangerType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcHumidifier")))
                    return IFCExportType.IfcHumidifierType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcJunctionBox")))
                    return IFCExportType.IfcJunctionBoxType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcLamp")))
                    return IFCExportType.IfcLampType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcLightFixture")))
                    return IFCExportType.IfcLightFixtureType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcMechanicalFastener")))
                    return IFCExportType.IfcMechanicalFastenerType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcMotorConnection")))
                    return IFCExportType.IfcMotorConnectionType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcOutlet")))
                    return IFCExportType.IfcOutletType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcPile")))
                    return IFCExportType.IfcPile;
                else if (IsEqualToTypeName(ifcClassName, ("IfcPipeFitting")))
                    return IFCExportType.IfcPipeFittingType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcPipeSegment")))
                    return IFCExportType.IfcPipeSegmentType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcProtectiveDevice")))
                    return IFCExportType.IfcProtectiveDeviceType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcPump")))
                    return IFCExportType.IfcPumpType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcReinforcingBar")))
                    return IFCExportType.IfcReinforcingBar;
                else if (IsEqualToTypeName(ifcClassName, ("IfcReinforcingMesh")))
                    return IFCExportType.IfcReinforcingMesh;
                else if (IsEqualToTypeName(ifcClassName, ("IfcSanitaryTerminal")))
                    return IFCExportType.IfcSanitaryTerminalType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcSensor")))
                    return IFCExportType.IfcSensorType;
                else if ((IsEqualToTypeName(ifcClassName, ("IfcSpaceHeater"))) ||
                   (IsEqualToTypeName(ifcClassName, ("IfcElectricHeater"))))
                {
                   // IFC2x3 IfcElectricHeaterType has been renamed to IfcSpaceHeaterType in IFC4.
                   return IFCExportType.IfcSpaceHeaterType;
                }
                else if (IsEqualToTypeName(ifcClassName, ("IfcStackTerminal")))
                   return IFCExportType.IfcStackTerminalType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcSwitchingDevice")))
                   return IFCExportType.IfcSwitchingDeviceType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcTank")))
                   return IFCExportType.IfcTankType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcTransformer")))
                   return IFCExportType.IfcTransformerType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcTubeBundle")))
                   return IFCExportType.IfcTubeBundleType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcUnitaryEquipment")))
                   return IFCExportType.IfcUnitaryEquipmentType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcValve")))
                   return IFCExportType.IfcValveType;
                else if (IsEqualToTypeName(ifcClassName, ("IfcWasteTerminal")))
                   return IFCExportType.IfcWasteTerminalType;

                // This used to throw an exception, but this could abort export if the user enters a bad IFC class name
                // in the ExportLayerOptions table.  In the future, we should log this.
                //throw new Exception("IFC: Unknown IFC type in getExportTypeFromClassName: " + ifcClassName);
                return IFCExportType.IfcBuildingElementProxyType;
            }

            return IFCExportType.DontExport;
        }

        // TODO: implement  out bool exportSeparately
        /// <summary>
        /// Gets export type from category id.
        /// </summary>
        /// <param name="categoryId">The category id.</param>
        /// <param name="ifcEnumType">The string value represents the IFC type.</param>
        /// <returns>The export type.</returns>
        public static IFCExportType GetExportTypeFromCategoryId(ElementId categoryId, out string ifcEnumType /*, out bool exportSeparately*/)
        {
            ifcEnumType = "";
            //exportSeparately = true;

            if (categoryId == new ElementId(BuiltInCategory.OST_Cornices))
                return IFCExportType.IfcBeam;
            else if (categoryId == new ElementId(BuiltInCategory.OST_Ceilings))
                return IFCExportType.IfcCovering;
            else if (categoryId == new ElementId(BuiltInCategory.OST_CurtainWallPanels))
            {
                ifcEnumType = "CURTAIN_PANEL";
                //exportSeparately = false;
                return IFCExportType.IfcPlateType;
            }
            else if (categoryId == new ElementId(BuiltInCategory.OST_Doors))
                return IFCExportType.IfcDoorType;
            else if (categoryId == new ElementId(BuiltInCategory.OST_Furniture))
                return IFCExportType.IfcFurnitureType;
            else if (categoryId == new ElementId(BuiltInCategory.OST_Floors))
            {
                ifcEnumType = "FLOOR";
                return IFCExportType.IfcSlab;
            }
            else if (categoryId == new ElementId(BuiltInCategory.OST_IOSModelGroups))
                return IFCExportType.IfcGroup;
            else if (categoryId == new ElementId(BuiltInCategory.OST_Mass))
                return IFCExportType.IfcBuildingElementProxyType;
            else if (categoryId == new ElementId(BuiltInCategory.OST_CurtainWallMullions))
            {
                ifcEnumType = "MULLION";
                //exportSeparately = false;
                return IFCExportType.IfcMemberType;
            }
            else if (categoryId == new ElementId(BuiltInCategory.OST_Railings))
                return IFCExportType.IfcRailing;
            else if (categoryId == new ElementId(BuiltInCategory.OST_Ramps))
                return IFCExportType.IfcRamp;
            else if (categoryId == new ElementId(BuiltInCategory.OST_Roofs))
                return IFCExportType.IfcRoof;
            else if (categoryId == new ElementId(BuiltInCategory.OST_Site))
                return IFCExportType.IfcSite;
            else if (categoryId == new ElementId(BuiltInCategory.OST_Stairs))
                return IFCExportType.IfcStair;
            else if (categoryId == new ElementId(BuiltInCategory.OST_Walls))
                return IFCExportType.IfcWall;
            else if (categoryId == new ElementId(BuiltInCategory.OST_Windows))
                return IFCExportType.IfcWindowType;

            return IFCExportType.DontExport;
        }

        /// <summary>
        /// Gets element filter for specific views.
        /// </summary>
        /// <param name="exporter">The ExporterIFC object.</param>
        /// <returns>The element filter.</returns>
        private static ElementFilter GetViewSpecificTypesFilter(ExporterIFC exporter)
        {
            ElementFilter ownerViewFilter = GetOwnerViewFilter(exporter);

            List<Type> viewSpecificTypes = new List<Type>();
            viewSpecificTypes.Add(typeof(TextNote));
            viewSpecificTypes.Add(typeof(FilledRegion));
            ElementMulticlassFilter classFilter = new ElementMulticlassFilter(viewSpecificTypes);


            LogicalAndFilter viewSpecificTypesFilter = new LogicalAndFilter(ownerViewFilter, classFilter);
            return viewSpecificTypesFilter;
        }

        /// <summary>
        /// Gets element filter to match elements which are owned by a particular view.
        /// </summary>
        /// <param name="exporter">The exporter.</param>
        /// <returns>The element filter.</returns>
        private static ElementFilter GetOwnerViewFilter(ExporterIFC exporter)
        {
            List<ElementFilter> filters = new List<ElementFilter>();
            ICollection<ElementId> viewIds = ExporterCacheManager.DBViewsToExport.Keys;
            foreach (ElementId id in viewIds)
            {
                filters.Add(new ElementOwnerViewFilter(id));
            }
            filters.Add(new ElementOwnerViewFilter(ElementId.InvalidElementId));
            LogicalOrFilter viewFilters = new LogicalOrFilter(filters);

            return viewFilters;
        }

        /// <summary>
        /// Gets element filter that match certain types.
        /// </summary>
        /// <param name="forSpatialElements">True if to get filter for spatial element, false for other elements.</param>
        /// <returns>The element filter.</returns>
        private static ElementFilter GetClassFilter(bool forSpatialElements)
        {
            if (forSpatialElements)
            {
                return new ElementClassFilter(typeof(SpatialElement));
            }
            else
            {
                List<Type> excludedTypes = new List<Type>();

                // FamilyInstances are handled in separate filter.
                excludedTypes.Add(typeof(FamilyInstance));

                // Spatial element are exported in a separate pass.
                excludedTypes.Add(typeof(SpatialElement));

                // AreaScheme elements are exported as groups after all Areas have been exported.
                excludedTypes.Add(typeof(AreaScheme));
                // FabricArea elements are exported as groups after all FabricSheets have been exported.
                excludedTypes.Add(typeof(FabricArea));

                if (!ExporterCacheManager.ExportOptionsCache.ExportAnnotations)
                    excludedTypes.Add(typeof(CurveElement));

                excludedTypes.Add(typeof(ElementType));
                
                excludedTypes.Add(typeof(BaseArray));

                excludedTypes.Add(typeof(FillPatternElement));
                excludedTypes.Add(typeof(LinePatternElement));
                excludedTypes.Add(typeof(Material));
                excludedTypes.Add(typeof(GraphicsStyle));
                excludedTypes.Add(typeof(Family));
                excludedTypes.Add(typeof(SketchPlane));
                excludedTypes.Add(typeof(View));
                excludedTypes.Add(typeof(Autodesk.Revit.DB.Structure.LoadBase));

                // curtain wall sub-types we are ignoring.
                excludedTypes.Add(typeof(CurtainGridLine));
                // excludedTypes.Add(typeof(Mullion));

                // this will be gotten from the element(s) it cuts.
                excludedTypes.Add(typeof(Opening));

                // 2D types we are ignoring
                excludedTypes.Add(typeof(SketchBase));
                excludedTypes.Add(typeof(FaceSplitter));

                // 2D types covered by the element owner view filter
                excludedTypes.Add(typeof(TextNote));
                excludedTypes.Add(typeof(FilledRegion));

                // exclude levels that are covered in BeginExport
                excludedTypes.Add(typeof(Level));

                // exclude analytical models
                excludedTypes.Add(typeof(Autodesk.Revit.DB.Structure.AnalyticalModel));

                ElementFilter excludedClassFilter = new ElementMulticlassFilter(excludedTypes, true);

                List<BuiltInCategory> excludedCategories = new List<BuiltInCategory>();

                // Native Revit types without match in API
                excludedCategories.Add(BuiltInCategory.OST_ConduitCenterLine);
                excludedCategories.Add(BuiltInCategory.OST_ConduitFittingCenterLine);
                excludedCategories.Add(BuiltInCategory.OST_DecalElement);
                //excludedCategories.Add(BuiltInCategory.OST_Parts);
                //excludedCategories.Add(BuiltInCategory.OST_RvtLinks);
                excludedCategories.Add(BuiltInCategory.OST_DuctCurvesCenterLine);
                excludedCategories.Add(BuiltInCategory.OST_DuctFittingCenterLine);
                excludedCategories.Add(BuiltInCategory.OST_FlexDuctCurvesCenterLine);
                excludedCategories.Add(BuiltInCategory.OST_FlexPipeCurvesCenterLine);
                excludedCategories.Add(BuiltInCategory.OST_IOS_GeoLocations);
                excludedCategories.Add(BuiltInCategory.OST_PipeCurvesCenterLine);
                excludedCategories.Add(BuiltInCategory.OST_PipeFittingCenterLine);
                excludedCategories.Add(BuiltInCategory.OST_Property);
                excludedCategories.Add(BuiltInCategory.OST_SiteProperty);
                excludedCategories.Add(BuiltInCategory.OST_SitePropertyLineSegment);
                excludedCategories.Add(BuiltInCategory.OST_TopographyContours);
                excludedCategories.Add(BuiltInCategory.OST_Viewports);
                excludedCategories.Add(BuiltInCategory.OST_Views);

                // Exclude elements with no category. 
                excludedCategories.Add(BuiltInCategory.INVALID);

                ElementMulticategoryFilter excludedCategoryFilter = new ElementMulticategoryFilter(excludedCategories, true);

                LogicalAndFilter exclusionFilter = new LogicalAndFilter(excludedClassFilter, excludedCategoryFilter);

                ElementOwnerViewFilter ownerViewFilter = new ElementOwnerViewFilter(ElementId.InvalidElementId);

                LogicalAndFilter returnedFilter = new LogicalAndFilter(exclusionFilter, ownerViewFilter);

                return returnedFilter;
            }
        }

        /// <summary>
        /// Checks if the room is in an invalid phase.
        /// </summary>
        /// <param name="element">The element, which may or may not be a room element.</param>
        /// <returns>True if the element is in the room, has a phase set, which is different from the active phase.</returns>
        public static bool IsRoomInInvalidPhase(Element element)
        {
            if (element is Room)
            {
                Parameter phaseParameter = element.get_Parameter(BuiltInParameter.ROOM_PHASE);
                if (phaseParameter != null)
                {
                    ElementId phaseId = phaseParameter.AsElementId();
                    if (phaseId != ElementId.InvalidElementId && phaseId != ExporterCacheManager.ExportOptionsCache.ActivePhaseId)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets element filter that match certain phases. 
        /// </summary>
        /// <param name="document">The Revit document.</param>
        /// <returns>The element filter.</returns>
        private static ElementFilter GetPhaseStatusFilter(Document document)
        {
            ElementId phaseId = ExporterCacheManager.ExportOptionsCache.ActivePhaseId;

            List<ElementOnPhaseStatus> phaseStatuses = new List<ElementOnPhaseStatus>();
            phaseStatuses.Add(ElementOnPhaseStatus.None);  //include "none" because we might want to export phaseless elements.
            phaseStatuses.Add(ElementOnPhaseStatus.Existing);
            phaseStatuses.Add(ElementOnPhaseStatus.New);

            return new ElementPhaseStatusFilter(phaseId, phaseStatuses);
        }

        private static IDictionary<ElementId, bool> m_CategoryVisibilityCache = new Dictionary<ElementId, bool>();

        public static void InitCategoryVisibilityCache()
        {
            m_CategoryVisibilityCache.Clear();
        }

        /// <summary>
        /// Checks if a category is visible for certain view.
        /// </summary>
        /// <param name="category">The category.</param>
        /// <param name="filterView">The view.</param>
        /// <returns>True if the category is visible, false otherwise.</returns>
        public static bool IsCategoryVisible(Category category, View filterView)
        {
           // This routine is generally used to decide whether or not to export geometry assigned to a praticular category.
           // Default behavior is to return true, even for a null category.  In general, we want to err on the side of showing geometry over hiding it.
           if (category == null || filterView == null)
              return true;

           bool isVisible = false;
           if (m_CategoryVisibilityCache.TryGetValue(category.Id, out isVisible))
              return isVisible;

           // The category will be visible if either we don't allow visibility controls (default: true), or
           // we do allow visibility controls and the category is visible in the view.
           isVisible = (!category.get_AllowsVisibilityControl(filterView) || category.get_Visible(filterView));
           m_CategoryVisibilityCache[category.Id] = isVisible;
           return isVisible;
        }

        /// <summary>
        /// Checks if element is visible for certain view.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>True if the element is visible, false otherwise.</returns>
        public static bool IsElementVisible(Element element)
        {
            View filterView = ExporterCacheManager.ExportOptionsCache.FilterViewForExport;
            if (filterView == null)
                return true;

            bool hidden = element.IsHidden(filterView);
            if (hidden)
                return false;

            Category category = element.Category;
            hidden = !IsCategoryVisible(category, filterView);
            if (hidden)
                return false;

            bool temporaryVisible = filterView.IsElementVisibleInTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate, element.Id);

            return temporaryVisible;
        }

        /// <summary>
        /// Checks if the IFC type is MEP type.
        /// </summary>
        /// <param name="exportType">IFC Export Type to check</param>
        /// <returns>True for MEP type of elements.</returns>
        public static bool IsMEPType(IFCExportType exportType)
        {
            return (exportType >= IFCExportType.IfcDistributionElement && exportType <= IFCExportType.IfcWasteTerminalType);
        }

        /// <summary>
        /// Check if an element assigned to IfcBuildingElementProxy is of MEP Type (by checking its connectors) to enable IfcBuildingElementProxy to take part
        /// in the System component and connectivity
        /// </summary>
        /// <param name="element">The element</param>
        /// <param name="exportType">IFC Export Type to check: only for IfcBuildingElementProxy or IfcBuildingElementProxyType</param>
        /// <returns></returns>
        public static bool ProxyForMEPType(Element element, IFCExportType exportType)
        {
            if ((exportType == IFCExportType.IfcBuildingElementProxy) || (exportType == IFCExportType.IfcBuildingElementProxyType))
            {
                try
                {
                    if (element is FamilyInstance)
                    {
                        MEPModel m = ((FamilyInstance)element).MEPModel;
                        if (m != null && m.ConnectorManager != null)
                        {
                            return true;
                        }
                    }
                    else
                        return false;
                }
                catch
                {
                }
            }

            return false;
        }
    }
}
