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

// The enums below specify sub-element values to be used in the CreateSubElementGUID function.
// This ensures that their GUIDs are consistent across exports.
// Note that sub-element GUIDs can not be stored on import, so they do not survive roundtrips.
namespace Revit.IFC.Export.Toolkit
{
    enum IFCAssemblyInstanceSubElements
    {
        RelContainedInSpatialStructure = 1,
        RelAggregates = 2
    }

    enum IFCBuildingSubElements
    {
        RelContainedInSpatialStructure = 1,
        RelAggregatesProducts = 2,
        RelAggregatesBuildingStoreys = 3
    }

    enum IFCBuildingStoreySubElements
    {
        RelContainedInSpatialStructure = 1,
        RelAggregates = 2
    }

    enum IFCCommonPSets
    {
        PSetAirTerminalTypeCommon = 3048,
        PSetBeamCommon = 3049,
        PSetBECCommon = 3050,
        PSetBuildingCommon = 3051,
        PSetBuildingStoreyCommon = 3052,
        PSetBS8666Common = 3053,
        PSetCoveringCommon = 3054,
        PSetCurtainWallCommon = 3055,
        PSetDoorCommon = 3056,
        PSetDIN135610Common = 3057,
        PSetDistributionFlowElementCommon = 3058,
        PSetFlowTerminalAirTerminal = 3059,
        PSetISOCD3766Common = 3060,
        PSetLightFixtureTypeCommon = 3061,
        PSetMemberCommon = 3062,
        PSetPlateCommon = 3063,
        PSetRampCommon = 3064,
        PSetRoofCommon = 3065,
        PSetSiteCommon = 3066,
        PSetSlabCommon = 3067,
        PSetStairCommon = 3068,
        PSetWallCommon = 3069,
        PSetWindowCommon = 3070,
        PSetDoorWindowGlazingType = 3071,
        PsetDoorWindowShadingType = 3072,
    }

    // Curtain Walls can be created from a variety of elements, including Walls and Roofs.
    // As such, start their subindexes high enough to not bother potential hosts.
    enum IFCCurtainWallSubElements
    {
        RelAggregates = 1024
    }

    enum IFCDoorSubElements
    {    
        DoorLining = 1,
        DoorPanelStart = 2,
        DoorPanelEnd = 17, // 2 through 17 are reserved for panels.
        DoorOpening = 19,
        DoorOpeningRelVoid = 20,
        DoorStyle = 21,
        DoorType = 22
    }

    enum IFCGroupSubElements
    {
        RelAssignsToGroup = 1,
    }

    // Used for internal Revit property sets, split instances, and connectors.
    enum IFCGenericSubElements
    {
        PSetRevitInternalStart = 1536,
        PSetRevitInternalEnd = PSetRevitInternalStart + 255,
        PSetRevitInternalRelStart = PSetRevitInternalEnd+1,
        PSetRevitInternalRelEnd = PSetRevitInternalRelStart + 255, // 2047
        // 2048 is IFCFamilyInstance.InstanceAsType
        SplitInstanceStart = 2049,
        SplitInstanceEnd = SplitInstanceStart + 255,
        SplitTypeStart = SplitInstanceEnd + 1,
        SplitTypeEnd = SplitTypeStart + 255, // 2560
    }

    // Family Instances can create a variety of elements.
    // As such, start their subindexes high enough to not bother potential hosts.
    enum IFCFamilyInstanceSubElements
    {
        InstanceAsType = 2048
    }

    enum IFCHostedSweepSubElements
    {
        PipeSegmentType = 1
    }

    enum IFCRampSubElements
    {
        ContainedRamp = 2,
        ContainmentRelation = 3 // same as IFCStairSubElements.ContainmentRelation
    }

    enum IFCReinforcingBarSubElements
    {
        BarStart = 5,
        BarEnd = BarStart + 255
    }

    enum IFCRoofSubElements
    {
        RoofSlabStart = 2,
        RoofSlabEnd = RoofSlabStart + 255
    }

    enum IFCSlabSubElements
    {
        SubSlabStart = 2,
        SubSlabEnd = SubSlabStart + 255
    }

    enum IFCStairSubElements
    {
        ContainedStair = 2,
        ContainmentRelation = 3
    }

    enum IFCWallSubElements
    {
        RelAggregatesReserved = IFCCurtainWallSubElements.RelAggregates
    }

    enum IFCWindowSubElements
    {
        WindowOpening = IFCDoorSubElements.DoorOpening,
        WindowOpeningRelVoid = IFCDoorSubElements.DoorOpeningRelVoid,
        WindowStyle = IFCDoorSubElements.DoorStyle,
    }

    enum IFCZoneSubElements
    {
        RelAssignsToGroup = 1,
    }
}
