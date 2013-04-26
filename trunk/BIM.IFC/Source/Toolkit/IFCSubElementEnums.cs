//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2012  Autodesk, Inc.
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
namespace BIM.IFC.Toolkit
{
    enum IFCAssemblyInstanceSubElements
    {
        RelContainedInSpatialStructure = 1,
        RelAggregates = 2
    }
    
    enum IFCBeamSubElements
    {
        PSetBeamCommon = 1
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

    enum IFCCoveringSubElements
    {
        PSetCoveringCommon = 1
    }

    // Curtain Walls can be created from a variety of elements, including Walls and Roofs.
    // As such, start their subindexes high enough to not bother potential hosts.
    enum IFCCurtainWallSubElements
    {
        PSetCurtainWallCommon = 1,
        RelAggregates = 1024
    }

    enum IFCDoorSubElements
    {    
        DoorLining = 1,
        DoorPanelStart = 2,
        DoorPanelEnd = 17, // 2 through 17 are reserved for panels.
        PSetDoorCommon = 18,
        DoorOpening = 19,
        DoorOpeningRelVoid = 20,
        DoorStyle = 21,
    }

    enum IFCDistributionFlowElementSubElements
    {
        PSetDistributionFlowElementCommon = 1024,
        PSetFlowTerminalAirTerminal = 1025,
        PSetAirTerminalTypeCommon = 1026,
    }

    enum IFCGroupSubElements
    {
        RelAssignsToGroup = 1,
    }

    // Used for internal Revit property sets and connectors.
    enum IFCGenericSubElements
    {
        PSetRevitInternalStart = 1536,
        PSetRevitInternalEnd = PSetRevitInternalStart + 255,
        PSetRevitInternalRelStart = PSetRevitInternalEnd+1,
        PSetRevitInternalRelEnd = PSetRevitInternalRelStart + 255, // 2047
    }

    enum IFCLightFixtureTypeSubElements
    {
        PSetLightFixtureTypeCommon = 1
    }

    // Family Instances can create a variety of elements.
    // As such, start their subindexes high enough to not bother potential hosts.
    enum IFCFamilyInstanceSubElements
    {
        InstanceAsType = 2048
    }

    enum IFCLevelSubElements
    {
        PSetBuildingStoreyCommon = 1,
    }

    enum IFCMemberSubElements
    {
        PSetMemberCommon = 1
    }

    enum IFCPlateSubElements
    {
        PSetPlateCommon = 1
    }

    enum IFCProjectSubElements
    {
        PSetBuildingCommon = 1,
        PSetSiteCommon = 2
    }

    enum IFCRampSubElements
    {
        PSetRampCommon = 1,
        ContainedRamp = 2,
        ContainmentRelation = 3 // same as IFCStairSubElements.ContainmentRelation
    }

    enum IFCReinforcingBarSubElements
    {
        PSetBECCommon = 1,
        PSetBS8666Common = 2,
        PSetDIN135610Common = 3,
        PSetISOCD3766Common = 4,
        BarStart = 5,
        BarEnd = BarStart + 255
    }

    enum IFCRoofSubElements
    {
        PSetRoofCommon = 1,
        RoofSlabStart = 2,
        RoofSlabEnd = RoofSlabStart + 255
    }

    enum IFCSlabSubElements
    {
        PSetSlabCommon = 1
    }

    enum IFCStairSubElements
    {
        PSetStairCommon = 1,
        ContainedStair = 2,
        ContainmentRelation = 3
    }

    enum IFCWallSubElements
    {
        PSetWallCommon = 1,
        RelAggregatesReserved = IFCCurtainWallSubElements.RelAggregates
    }

    enum IFCWindowSubElements
    {
        PSetWindowCommon = 1,
        WindowOpening = IFCDoorSubElements.DoorOpening,
        WindowOpeningRelVoid = IFCDoorSubElements.DoorOpeningRelVoid,
        WindowStyle = IFCDoorSubElements.DoorStyle,
    }

    enum IFCZoneSubElements
    {
        RelAssignsToGroup = 1,
    }
}
