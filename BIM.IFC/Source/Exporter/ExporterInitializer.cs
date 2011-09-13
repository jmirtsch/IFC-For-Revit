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
using System.Collections.ObjectModel;
using BIM.IFC.Utility;

namespace BIM.IFC.Exporter
{
    /// <summary>
    /// Initializes user defined parameters and quantities.
    /// </summary>
    class ExporterInitializer
    {
        /// <summary>
        /// Initializes property sets.
        /// </summary>
        /// <param name="fileVersion">The IFC file version.</param>
        public static void InitPropertySets(ParameterCache cache, IFCVersion fileVersion)
        {
            InitCommonPropertySets(cache.PropertySets);

            if (fileVersion == IFCVersion.IFCCOBIE)
            {
                InitCOBIEPropertySets(cache.PropertySets);
            }
        }

        /// <summary>
        /// Initializes quantities.
        /// </summary>
        /// <param name="fileVersion">The IFC file version.</param>
        /// <param name="exportBaseQuantities">True if export base quantities.</param>
        public static void InitQuantities(ParameterCache cache, IFCVersion fileVersion)
        {
            InitCommonQuantities(cache.Quantities);

            if (fileVersion == IFCVersion.IFCCOBIE)
            {
                InitCOBIEQuantities(cache.Quantities);
            }
        }

        // Properties
        /// <summary>
        /// Initializes common property sets.
        /// </summary>
        /// <param name="propertySets">List to store property sets.</param>
        /// <param name="fileVersion">The IFC file version.</param>
        private static void InitCommonPropertySets(IList<IList<IFCPropertySetDescription>> propertySets)
        {
            IList<IFCPropertySetDescription> commonPSets = new List<IFCPropertySetDescription>();
            InitPropertySetWallCommon(commonPSets);
            InitPropertySetBeamCommon(commonPSets);
            InitPropertySetSlabCommon(commonPSets);
            propertySets.Add(commonPSets);
        }

        /// <summary>
        /// Initializes common wall property sets.
        /// </summary>
        /// <param name="commonPropertySets">List to store property sets.</param>
        private static void InitPropertySetWallCommon(IList<IFCPropertySetDescription> commonPSets)
        {
            //property set wall common
            IFCPropertySetDescription propertySetWallCommon = IFCPropertySetDescription.Create();
            propertySetWallCommon.Name = "Pset_WallCommon";
            propertySetWallCommon.EntryType = IFCEntryType.Wall;

            IFCPropertySetEntry ifcPSE = IFCPropertySetEntry.Create("Reference");
            ifcPSE.PropertyType = IFCPropertyType.Identifier;
            ifcPSE.SetPropertyCalculator(IFCPropertyCalculator.GetReferenceCalculator());
            propertySetWallCommon.AddEntry(ifcPSE);

            ifcPSE = IFCPropertySetEntry.Create("LoadBearing");
            ifcPSE.PropertyType = IFCPropertyType.Boolean;
            ifcPSE.SetPropertyCalculator(IFCPropertyCalculator.GetLoadBearingCalculator());
            propertySetWallCommon.AddEntry(ifcPSE);

            ifcPSE = IFCPropertySetEntry.Create("ExtendToStructure");
            ifcPSE.PropertyType = IFCPropertyType.Boolean;
            ifcPSE.SetPropertyCalculator(IFCPropertyCalculator.GetExtendToStructureCalculator());
            propertySetWallCommon.AddEntry(ifcPSE);

            ifcPSE = IFCPropertySetEntry.Create("IsExternal");
            ifcPSE.PropertyType = IFCPropertyType.Boolean;
            ifcPSE.SetPropertyCalculator(IFCPropertyCalculator.GetExternalCalculator());
            propertySetWallCommon.AddEntry(ifcPSE);

            ifcPSE = IFCPropertySetEntry.Create("FireRating");
            ifcPSE.RevitBuiltInParameterId = new ElementId(BuiltInParameter.DOOR_FIRE_RATING);
            propertySetWallCommon.AddEntry(ifcPSE);

            ifcPSE = IFCPropertySetEntry.Create("AcousticRating");
            propertySetWallCommon.AddEntry(ifcPSE);

            ifcPSE = IFCPropertySetEntry.Create("SurfaceSpreadOfFlame");
            propertySetWallCommon.AddEntry(ifcPSE);

            ifcPSE = IFCPropertySetEntry.Create("Combustible");
            ifcPSE.PropertyType = IFCPropertyType.Boolean;
            propertySetWallCommon.AddEntry(ifcPSE);

            ifcPSE = IFCPropertySetEntry.Create("Compartmentation");
            ifcPSE.PropertyType = IFCPropertyType.Boolean;
            propertySetWallCommon.AddEntry(ifcPSE);

            ifcPSE = IFCPropertySetEntry.Create("ThermalTransmittance");
            ifcPSE.PropertyType = IFCPropertyType.Real;
            propertySetWallCommon.AddEntry(ifcPSE);

            commonPSets.Add(propertySetWallCommon);
        }

        /// <summary>
        /// Initializes common beam property sets.
        /// </summary>
        /// <param name="commonPropertySets">List to store property sets.</param>
        private static void InitPropertySetBeamCommon(IList<IFCPropertySetDescription> commonPSets)
        {
            //property beam common
            IFCPropertySetDescription propertySetBeamCommon = IFCPropertySetDescription.Create();
            propertySetBeamCommon.Name = "Pset_BeamCommon";
            propertySetBeamCommon.EntryType = IFCEntryType.Beam;

            IFCPropertySetEntry ifcPSE = IFCPropertySetEntry.Create("Reference");
            ifcPSE.PropertyType = IFCPropertyType.Identifier;
            ifcPSE.SetPropertyCalculator(IFCPropertyCalculator.GetReferenceCalculator());
            propertySetBeamCommon.AddEntry(ifcPSE);

            ifcPSE = IFCPropertySetEntry.Create("LoadBearing");
            ifcPSE.PropertyType = IFCPropertyType.Boolean;
            ifcPSE.SetPropertyCalculator(IFCPropertyCalculator.GetBeamLoadBearingCalculator());
            propertySetBeamCommon.AddEntry(ifcPSE);

            ifcPSE = IFCPropertySetEntry.Create("IsExternal");
            ifcPSE.PropertyType = IFCPropertyType.Boolean;
            ifcPSE.SetPropertyCalculator(IFCPropertyCalculator.GetExternalCalculator());
            propertySetBeamCommon.AddEntry(ifcPSE);

            ifcPSE = IFCPropertySetEntry.Create("FireRating");
            ifcPSE.RevitBuiltInParameterId = new ElementId(BuiltInParameter.DOOR_FIRE_RATING);
            propertySetBeamCommon.AddEntry(ifcPSE);

            ifcPSE = IFCPropertySetEntry.Create("Span");
            ifcPSE.PropertyType = IFCPropertyType.PositiveLength;
            ifcPSE.SetPropertyCalculator(IFCPropertyCalculator.GetBeamSpanCalculator());
            propertySetBeamCommon.AddEntry(ifcPSE);

            ifcPSE = IFCPropertySetEntry.Create("Slope");
            ifcPSE.PropertyType = IFCPropertyType.PlaneAngle;
            ifcPSE.SetPropertyCalculator(IFCPropertyCalculator.GetBeamSlopeCalculator());
            propertySetBeamCommon.AddEntry(ifcPSE);

            commonPSets.Add(propertySetBeamCommon);
        }

        /// <summary>
        /// Initializes common slab property sets.
        /// </summary>
        /// <param name="commonPropertySets">List to store property sets.</param>
        private static void InitPropertySetSlabCommon(IList<IFCPropertySetDescription> commonPSets)
        {
            // PSet_SlabCommon
            IFCPropertySetDescription propertySetSlabCommon = IFCPropertySetDescription.Create();
            propertySetSlabCommon.Name = "Pset_SlabCommon";
            propertySetSlabCommon.EntryType = IFCEntryType.Floor;

            IFCPropertySetEntry ifcPSE = IFCPropertySetEntry.Create("Reference");
            ifcPSE.PropertyType = IFCPropertyType.Identifier;
            ifcPSE.SetPropertyCalculator(IFCPropertyCalculator.GetReferenceCalculator());
            propertySetSlabCommon.AddEntry(ifcPSE);

            ifcPSE = IFCPropertySetEntry.Create("IsExternal");
            ifcPSE.PropertyType = IFCPropertyType.Boolean;
            ifcPSE.SetPropertyCalculator(IFCPropertyCalculator.GetExternalCalculator());
            propertySetSlabCommon.AddEntry(ifcPSE);

            ifcPSE = IFCPropertySetEntry.Create("LoadBearing");
            ifcPSE.PropertyType = IFCPropertyType.Boolean;
            ifcPSE.SetPropertyCalculator(IFCPropertyCalculator.GetFloorLoadBearingCalculator()); // always true
            propertySetSlabCommon.AddEntry(ifcPSE);

            ifcPSE = IFCPropertySetEntry.Create("FireRating");
            ifcPSE.RevitBuiltInParameterId = new ElementId(BuiltInParameter.DOOR_FIRE_RATING);
            propertySetSlabCommon.AddEntry(ifcPSE);

            ifcPSE = IFCPropertySetEntry.Create("AcousticRating");
            propertySetSlabCommon.AddEntry(ifcPSE);

            ifcPSE = IFCPropertySetEntry.Create("SurfaceSpreadOfFlame");
            propertySetSlabCommon.AddEntry(ifcPSE);

            ifcPSE = IFCPropertySetEntry.Create("Combustible");
            ifcPSE.PropertyType = IFCPropertyType.Boolean;
            propertySetSlabCommon.AddEntry(ifcPSE);

            ifcPSE = IFCPropertySetEntry.Create("Compartmentation");
            ifcPSE.PropertyType = IFCPropertyType.Boolean;
            propertySetSlabCommon.AddEntry(ifcPSE);

            ifcPSE = IFCPropertySetEntry.Create("ThermalTransmittance");
            ifcPSE.PropertyType = IFCPropertyType.Real;
            propertySetSlabCommon.AddEntry(ifcPSE);

            commonPSets.Add(propertySetSlabCommon);
        }


        /// <summary>
        /// Initializes COBIE property sets.
        /// </summary>
        /// <param name="propertySets">List to store property sets.</param>
        private static void InitCOBIEPropertySets(IList<IList<IFCPropertySetDescription>> propertySets)
        {
            IList<IFCPropertySetDescription> cobiePSets = new List<IFCPropertySetDescription>();
            InitCOBIEPSetSpaceOccupant(cobiePSets);
            InitCOBIEPSetSpaceThermalSimulationProperties(cobiePSets);
            InitCOBIEPSetSpaceThermalDesign(cobiePSets);
            InitCOBIEPSetSpaceVentilationCriteria(cobiePSets);
            InitCOBIEPSetBuildingEnergyTarget(cobiePSets);
            propertySets.Add(cobiePSets);
        }

        /// <summary>
        /// Initializes COBIE space occupant property sets.
        /// </summary>
        /// <param name="cobiePropertySets">List to store property sets.</param>
        private static void InitCOBIEPSetSpaceOccupant(IList<IFCPropertySetDescription> cobiePropertySets)
        {
            IFCPropertySetDescription propertySetSpaceOccupant = IFCPropertySetDescription.Create();
            propertySetSpaceOccupant.Name = "ePset_SpaceOccupant";
            propertySetSpaceOccupant.EntryType = IFCEntryType.SpatialElement;

            IFCPropertySetEntry ifcPSE = IFCPropertySetEntry.Create("Space Occupant Organization Abbreviation");
            propertySetSpaceOccupant.AddEntry(ifcPSE);

            ifcPSE = IFCPropertySetEntry.Create("Space Occupant Organization Name");
            propertySetSpaceOccupant.AddEntry(ifcPSE);

            cobiePropertySets.Add(propertySetSpaceOccupant);
        }

        /// <summary>
        /// Initializes COBIE space thermal simulation property sets.
        /// </summary>
        /// <param name="cobiePropertySets">List to store property sets.</param>
        private static void InitCOBIEPSetSpaceThermalSimulationProperties(IList<IFCPropertySetDescription> cobiePropertySets)
        {
            IFCPropertySetDescription propertySetSpaceThermalSimulationProperties = IFCPropertySetDescription.Create();
            propertySetSpaceThermalSimulationProperties.Name = "ePset_SpaceThermalSimulationProperties";
            propertySetSpaceThermalSimulationProperties.EntryType = IFCEntryType.SpatialElement;

            IFCPropertySetEntry ifcPSE = IFCPropertySetEntry.Create("Space Thermal Simulation Type");
            ifcPSE.PropertyName = "SpaceThermalSimulationType";
            propertySetSpaceThermalSimulationProperties.AddEntry(ifcPSE);

            ifcPSE = IFCPropertySetEntry.Create("Space Conditioning Requirement");
            ifcPSE.PropertyName = "SpaceConditioningRequirement";
            propertySetSpaceThermalSimulationProperties.AddEntry(ifcPSE);

            ifcPSE = IFCPropertySetEntry.Create("Space Occupant Density");
            ifcPSE.PropertyName = "SpaceOccupantDensity";
            ifcPSE.PropertyType = IFCPropertyType.Real;
            propertySetSpaceThermalSimulationProperties.AddEntry(ifcPSE);

            ifcPSE = IFCPropertySetEntry.Create("Space Occupant Heat Rate");
            ifcPSE.PropertyName = "SpaceOccupantHeatRate";
            ifcPSE.PropertyType = IFCPropertyType.Real;
            propertySetSpaceThermalSimulationProperties.AddEntry(ifcPSE);

            ifcPSE = IFCPropertySetEntry.Create("Space Occupant Load");
            ifcPSE.PropertyName = "SpaceOccupantLoad";
            ifcPSE.PropertyType = IFCPropertyType.Real;
            propertySetSpaceThermalSimulationProperties.AddEntry(ifcPSE);

            ifcPSE = IFCPropertySetEntry.Create("Space Equipment Load");
            ifcPSE.PropertyName = "SpaceEquipmentLoad";
            ifcPSE.PropertyType = IFCPropertyType.Real;
            propertySetSpaceThermalSimulationProperties.AddEntry(ifcPSE);

            ifcPSE = IFCPropertySetEntry.Create("Space Lighting Load");
            ifcPSE.PropertyName = "SpaceLightingLoad";
            ifcPSE.PropertyType = IFCPropertyType.Real;
            propertySetSpaceThermalSimulationProperties.AddEntry(ifcPSE);

            cobiePropertySets.Add(propertySetSpaceThermalSimulationProperties);
        }

        /// <summary>
        /// Initializes COBIE space thermal design property sets.
        /// </summary>
        /// <param name="cobiePropertySets">List to store property sets.</param>
        private static void InitCOBIEPSetSpaceThermalDesign(IList<IFCPropertySetDescription> cobiePropertySets)
        {
            IFCPropertySetDescription propertySetSpaceThermalDesign = IFCPropertySetDescription.Create();
            propertySetSpaceThermalDesign.Name = "Pset_SpaceThermalDesign";
            propertySetSpaceThermalDesign.EntryType = IFCEntryType.SpatialElement;

            IFCPropertySetEntry ifcPSE = IFCPropertySetEntry.Create("Inside Dry Bulb Temperature - Heating");
            ifcPSE.PropertyName = "HeatingDryBulb";
            ifcPSE.PropertyType = IFCPropertyType.Real;
            propertySetSpaceThermalDesign.AddEntry(ifcPSE);

            ifcPSE = IFCPropertySetEntry.Create("Inside Relative Humidity - Heating");
            ifcPSE.PropertyName = "HeatingRelativeHumidity";
            ifcPSE.PropertyType = IFCPropertyType.Real;
            propertySetSpaceThermalDesign.AddEntry(ifcPSE);

            ifcPSE = IFCPropertySetEntry.Create("Inside Dry Bulb Temperature - Cooling");
            ifcPSE.PropertyName = "CoolingDryBulb";
            ifcPSE.PropertyType = IFCPropertyType.Real;
            propertySetSpaceThermalDesign.AddEntry(ifcPSE);

            ifcPSE = IFCPropertySetEntry.Create("Inside Relative Humidity - Cooling");
            ifcPSE.PropertyName = "CoolingRelativeHumidity";
            ifcPSE.PropertyType = IFCPropertyType.Real;
            propertySetSpaceThermalDesign.AddEntry(ifcPSE);

            ifcPSE = IFCPropertySetEntry.Create("Inside Return Air Plenum");
            ifcPSE.PropertyName = "InsideReturnAirPlenum";
            ifcPSE.PropertyType = IFCPropertyType.Real;
            propertySetSpaceThermalDesign.AddEntry(ifcPSE);

            cobiePropertySets.Add(propertySetSpaceThermalDesign);
        }

        /// <summary>
        /// Initializes COBIE space ventilation criteria property sets.
        /// </summary>
        /// <param name="cobiePropertySets">List to store property sets.</param>
        private static void InitCOBIEPSetSpaceVentilationCriteria(IList<IFCPropertySetDescription> cobiePropertySets)
        {
            IFCPropertySetDescription propertySetSpaceVentilationCriteria = IFCPropertySetDescription.Create();
            propertySetSpaceVentilationCriteria.Name = "ePset_SpaceVentilationCriteria";
            propertySetSpaceVentilationCriteria.EntryType = IFCEntryType.SpatialElement;

            IFCPropertySetEntry ifcPSE = IFCPropertySetEntry.Create("Ventilation Type");
            ifcPSE.PropertyName = "VentilationType";
            propertySetSpaceVentilationCriteria.AddEntry(ifcPSE);

            ifcPSE = IFCPropertySetEntry.Create("Outside Air Per Person");
            ifcPSE.PropertyName = "OutsideAirPerPerson";
            ifcPSE.PropertyType = IFCPropertyType.Real;
            propertySetSpaceVentilationCriteria.AddEntry(ifcPSE);

            cobiePropertySets.Add(propertySetSpaceVentilationCriteria);
        }

        /// <summary>
        /// Initializes COBIE building energy target property sets.
        /// </summary>
        /// <param name="cobiePropertySets">List to store property sets.</param>
        private static void InitCOBIEPSetBuildingEnergyTarget(IList<IFCPropertySetDescription> cobiePropertySets)
        {
            IFCPropertySetDescription propertySetBuildingEnergyTarget = IFCPropertySetDescription.Create();
            propertySetBuildingEnergyTarget.Name = "ePset_BuildingEnergyTarget";
            propertySetBuildingEnergyTarget.EntryType = IFCEntryType.Building;

            IFCPropertySetEntry ifcPSE = IFCPropertySetEntry.Create("Building Energy Target Value");
            ifcPSE.PropertyName = "BuildingEnergyTargetValue";
            ifcPSE.PropertyType = IFCPropertyType.Real;
            propertySetBuildingEnergyTarget.AddEntry(ifcPSE);

            ifcPSE = IFCPropertySetEntry.Create("Building Energy Target Units");
            ifcPSE.PropertyName = "BuildingEnergyTargetUnits";
            propertySetBuildingEnergyTarget.AddEntry(ifcPSE);

            cobiePropertySets.Add(propertySetBuildingEnergyTarget);
        }

        // Quantities
        /// <summary>
        /// Initializes slab base quantities.
        /// </summary>
        /// <param name="slabQuantities">List to store quantities.</param>
        private static void InitSlabBaseQuantities(IList<IFCQuantityDescription> slabQuantities)
        {
            IFCQuantityDescription ifcSlabQuantity = IFCQuantityDescription.Create();
            ifcSlabQuantity.Name = "BaseQuantities";
            ifcSlabQuantity.EntryType = IFCEntryType.Floor;

            IFCQuantityEntry ifcQE = IFCQuantityEntry.Create("CrossArea");
            ifcQE.QuantityType = IFCQuantityType.Area;
            ifcQE.SetPropertyCalculator(IFCPropertyCalculator.GetFloorCrossAreaCalculator());
            ifcSlabQuantity.AddEntry(ifcQE);

            ifcQE = IFCQuantityEntry.Create("GrossVolume");
            ifcQE.QuantityType = IFCQuantityType.Area;
            ifcQE.SetPropertyCalculator(IFCPropertyCalculator.GetFloorGrossVolumeCalculator());
            ifcSlabQuantity.AddEntry(ifcQE);

            ifcQE = IFCQuantityEntry.Create("Perimeter");
            ifcQE.QuantityType = IFCQuantityType.Area;
            ifcQE.SetPropertyCalculator(IFCPropertyCalculator.GetFloorPerimeterCalculator());
            ifcSlabQuantity.AddEntry(ifcQE);

            ifcQE = IFCQuantityEntry.Create("Width");
            ifcQE.QuantityType = IFCQuantityType.PositiveLength;
            ifcQE.SetPropertyCalculator(IFCPropertyCalculator.GetFloorWidthCalculator());
            ifcSlabQuantity.AddEntry(ifcQE);

            slabQuantities.Add(ifcSlabQuantity);
        }

        /// <summary>
        /// Initializes common quantities.
        /// </summary>
        /// <param name="quantities">List to store quantities.</param>
        private static void InitCommonQuantities(IList<IList<IFCQuantityDescription>> quantities)
        {
            IList<IFCQuantityDescription> commonQuantities = new List<IFCQuantityDescription>();
            InitSlabBaseQuantities(commonQuantities);
            quantities.Add(commonQuantities);
        }

        /// <summary>
        /// Initializes base quantities.
        /// </summary>
        /// <param name="quantities">List to store quantities.</param>
        private static void InitCOBIEQuantities(IList<IList<IFCQuantityDescription>> quantities)
        {
            IList<IFCQuantityDescription> cobieQuantities = new List<IFCQuantityDescription>();
            InitCOBIESpaceQuantities(cobieQuantities);
            InitCOBIESpaceLevelQuantities(cobieQuantities);
            InitCOBIEPMSpaceQuantities(cobieQuantities);
            quantities.Add(cobieQuantities);
        }

        /// <summary>
        /// Initializes COBIE space quantities.
        /// </summary>
        /// <param name="cobieQuantities">List to store quantities.</param>
        private static void InitCOBIESpaceQuantities(IList<IFCQuantityDescription> cobieQuantities)
        {
            IFCQuantityDescription ifcCOBIEQuantity = IFCQuantityDescription.Create();
            ifcCOBIEQuantity.Name = "BaseQuantities";
            ifcCOBIEQuantity.EntryType = IFCEntryType.SpatialElement;

            IFCQuantityEntry ifcQE = IFCQuantityEntry.Create("Height");
            ifcQE.MethodOfMeasurement = "length measured in geometry";
            ifcQE.QuantityType = IFCQuantityType.PositiveLength;
            ifcQE.SetPropertyCalculator(IFCPropertyCalculator.GetSpatialElementHeightCalculator());
            ifcCOBIEQuantity.AddEntry(ifcQE);

            ifcQE = IFCQuantityEntry.Create("GrossPerimeter");
            ifcQE.MethodOfMeasurement = "length measured in geometry";
            ifcQE.QuantityType = IFCQuantityType.PositiveLength;
            ifcQE.SetPropertyCalculator(IFCPropertyCalculator.GetSpatialElementPerimeterCalculator());
            ifcCOBIEQuantity.AddEntry(ifcQE);

            ifcQE = IFCQuantityEntry.Create("GrossFloorArea");
            ifcQE.MethodOfMeasurement = "area measured in geometry";
            ifcQE.QuantityType = IFCQuantityType.Area;
            ifcQE.SetPropertyCalculator(IFCPropertyCalculator.GetSpatialElementAreaCalculator());
            ifcCOBIEQuantity.AddEntry(ifcQE);

            ifcQE = IFCQuantityEntry.Create("NetFloorArea");
            ifcQE.MethodOfMeasurement = "area measured in geometry";
            ifcQE.QuantityType = IFCQuantityType.Area;
            ifcQE.SetPropertyCalculator(IFCPropertyCalculator.GetSpatialElementAreaCalculator());
            ifcCOBIEQuantity.AddEntry(ifcQE);

            ifcQE = IFCQuantityEntry.Create("GrossVolume");
            ifcQE.MethodOfMeasurement = "volume measured in geometry";
            ifcQE.QuantityType = IFCQuantityType.Volume;
            ifcQE.SetPropertyCalculator(IFCPropertyCalculator.GetSpatialElementVolumeCalculator());
            ifcCOBIEQuantity.AddEntry(ifcQE);

            cobieQuantities.Add(ifcCOBIEQuantity);
        }

        /// <summary>
        /// Initializes COBIE space level quantities.
        /// </summary>
        /// <param name="cobieQuantities">List to store quantities.</param>
        private static void InitCOBIESpaceLevelQuantities(IList<IFCQuantityDescription> cobieQuantities)
        {
            IFCQuantityDescription ifcCOBIEQuantity = IFCQuantityDescription.Create();
            ifcCOBIEQuantity.Name = "BaseQuantities";
            ifcCOBIEQuantity.EntryType = IFCEntryType.SpatialElement;
            ifcCOBIEQuantity.SetRedirectDescriptionCalculator(IFCRedirectDescriptionCalculator.GetSpatialElementLevelRedirect());

            IFCQuantityEntry ifcQE = IFCQuantityEntry.Create("GrossFloorArea");
            ifcQE.MethodOfMeasurement = "area measured in geometry";
            ifcQE.QuantityType = IFCQuantityType.Area;
            ifcQE.SetPropertyCalculator(IFCPropertyCalculator.GetSpatialElementLevelAreaCalculator());
            ifcCOBIEQuantity.AddEntry(ifcQE);

            cobieQuantities.Add(ifcCOBIEQuantity);
        }

        /// <summary>
        /// Initializes COBIE BM space quantities.
        /// </summary>
        /// <param name="cobieQuantities">List to store quantities.</param>
        private static void InitCOBIEPMSpaceQuantities(IList<IFCQuantityDescription> cobieQuantities)
        {
            IFCQuantityDescription ifcCOBIEQuantity = IFCQuantityDescription.Create();
            ifcCOBIEQuantity.Name = "Space Quantities (Property Management)";
            ifcCOBIEQuantity.MethodOfMeasurement = "As defined by BOMA (see www.boma.org)";
            ifcCOBIEQuantity.EntryType = IFCEntryType.SpatialElement;

            IFCQuantityEntry ifcQE = IFCQuantityEntry.Create("NetFloorArea_BOMA");
            ifcQE.MethodOfMeasurement = "area measured in geometry";
            ifcQE.QuantityType = IFCQuantityType.Area;
            ifcQE.SetPropertyCalculator(IFCPropertyCalculator.GetSpatialElementAreaCalculator());
            ifcCOBIEQuantity.AddEntry(ifcQE);

            cobieQuantities.Add(ifcCOBIEQuantity);
        }
    }
}
