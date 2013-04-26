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
using Revit.IFC.Export.Exporter;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Utility
{
    using IFCDoorStyleOperation = Autodesk.Revit.DB.IFC.IFCDoorStyleOperation;
    using IFCWindowStyleOperation = Autodesk.Revit.DB.IFC.IFCWindowStyleOperation;
    using Revit.IFC.Common.Utility;
    using Revit.IFC.Export.Exporter.PropertySet;

    /// <summary>
    /// Provides static methods for door and window related manipulations.
    /// </summary>
    class DoorWindowUtil
    {
        /// <summary>
        /// Gets the panel operation from door style operation.
        /// </summary>
        /// <param name="ifcDoorStyleOperationType">
        /// The IFCDoorStyleOperation.
        /// </param>
        /// <returns>
        /// The string represents the door panel operation.
        /// </returns>
        public static IFCDoorPanelOperation GetPanelOperationFromDoorStyleOperation(IFCDoorStyleOperation ifcDoorStyleOperationType)
        {
            switch (ifcDoorStyleOperationType)
            {
                case IFCDoorStyleOperation.SingleSwingLeft:
                case IFCDoorStyleOperation.SingleSwingRight:
                case IFCDoorStyleOperation.DoubleDoorSingleSwing:
                case IFCDoorStyleOperation.DoubleDoorSingleSwingOppositeLeft:
                case IFCDoorStyleOperation.DoubleDoorSingleSwingOppositeRight:
                    return IFCDoorPanelOperation.Swinging;
                case IFCDoorStyleOperation.DoubleSwingLeft:
                case IFCDoorStyleOperation.DoubleSwingRight:
                case IFCDoorStyleOperation.DoubleDoorDoubleSwing:
                    return IFCDoorPanelOperation.Double_Acting;
                case IFCDoorStyleOperation.SlidingToLeft:
                case IFCDoorStyleOperation.SlidingToRight:
                case IFCDoorStyleOperation.DoubleDoorSliding:
                    return IFCDoorPanelOperation.Sliding;
                case IFCDoorStyleOperation.FoldingToLeft:
                case IFCDoorStyleOperation.FoldingToRight:
                case IFCDoorStyleOperation.DoubleDoorFolding:
                    return IFCDoorPanelOperation.Folding;
                case IFCDoorStyleOperation.Revolving:
                    return IFCDoorPanelOperation.Revolving;
                case IFCDoorStyleOperation.RollingUp:
                    return IFCDoorPanelOperation.RollingUp;
                case IFCDoorStyleOperation.UserDefined:
                    return IFCDoorPanelOperation.UserDefined;
                default:
                    return IFCDoorPanelOperation.NotDefined;
            }
        }

        /// <summary>
        /// Creates door panel properties.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="doorWindowInfo">The DoorWindowInfo object.</param>
        /// <param name="familyInstance">The family instance of a door.</param>
        /// <returns>The list of handles created.</returns>
        public static IList<IFCAnyHandle> CreateDoorPanelProperties(ExporterIFC exporterIFC,
           DoorWindowInfo doorWindowInfo, Element familyInstance)
        {
            IFCFile file = exporterIFC.GetFile();
            IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();

            IList<IFCAnyHandle> doorPanels = new List<IFCAnyHandle>();

            IList<double?> panelDepthList = new List<double?>();
            IList<double?> panelWidthList = new List<double?>();

            IList<IFCDoorPanelOperation> panelOperationList = new List<IFCDoorPanelOperation>();
            IList<IFCDoorPanelPosition> panelPositionList = new List<IFCDoorPanelPosition>();

            int panelNumber = 1;
            const int maxPanels = 64;  // arbitrary large number to prevent infinite loops.
            for (; panelNumber < maxPanels; panelNumber++)
            {
                string panelDepthCurrString = "PanelDepth" + panelNumber.ToString();
                string panelWidthCurrString = "PanelWidth" + panelNumber.ToString();

                // We will always have at least one panel definition as long as the panelOperation is not
                // NotDefined.

                panelOperationList.Add(GetPanelOperationFromDoorStyleOperation(doorWindowInfo.DoorOperationType));

                // If the panel operation is defined we'll allow no panel position for the 1st panel.
                IFCDoorPanelPosition? panelPosition = GetIFCDoorPanelPosition("", familyInstance, panelNumber);
                if (panelPosition == null)
                {
                    if (panelNumber == 1)
                        panelPosition = GetIFCDoorPanelPosition("", familyInstance, -1);
                    if ((panelPosition == null) && (panelNumber > 1))
                    {
                        panelPositionList.Add(IFCDoorPanelPosition.NotDefined);
                        break;
                    }
                }

                if (doorWindowInfo.FlippedX ^ doorWindowInfo.FlippedY)
                    panelPosition = ReverseDoorPanelPosition(panelPosition);

                panelPositionList.Add(panelPosition != null ? (IFCDoorPanelPosition)panelPosition : IFCDoorPanelPosition.NotDefined);

                double value1 = 0.0, value2 = 0.0;
                bool foundDepth = ParameterUtil.GetPositiveDoubleValueFromElementOrSymbol(familyInstance, panelDepthCurrString, out value1);
                if (!foundDepth && (panelNumber == 1))
                    foundDepth = ParameterUtil.GetPositiveDoubleValueFromElementOrSymbol(familyInstance, "PanelDepth", out value1);

                bool foundWidth = ParameterUtil.GetPositiveDoubleValueFromElementOrSymbol(familyInstance, panelWidthCurrString, out value2);
                if (!foundWidth && (panelNumber == 1))
                    foundWidth = ParameterUtil.GetPositiveDoubleValueFromElementOrSymbol(familyInstance, "PanelWidth", out value2);

                if (foundDepth && foundWidth)
                {
                    panelDepthList.Add(UnitUtil.ScaleLength(value1));
                    // Make sure value is in [0,1] range.
                    if (value2 < 0.0) value2 = 0.0; else if (value2 > 1.0) value2 = 1.0;
                    panelWidthList.Add(value2);
                }
                else
                {
                    panelDepthList.Add(null);
                    panelWidthList.Add(null);
                }
            }

            string baseDoorPanelName = NamingUtil.GetIFCName(familyInstance);
            for (int panelIndex = 0; (panelIndex < panelNumber - 1); panelIndex++)
            {
                double? currentPanelWidth = null;
                if (panelWidthList[panelIndex].HasValue)
                    currentPanelWidth = (double)panelWidthList[panelIndex];

                string doorPanelName = baseDoorPanelName;
                string doorPanelGUID = GUIDUtil.CreateGUID();
                IFCAnyHandle doorPanel = IFCInstanceExporter.CreateDoorPanelProperties(file, doorPanelGUID, ownerHistory,
                   doorPanelName, null, panelDepthList[panelIndex], panelOperationList[panelIndex],
                   currentPanelWidth, panelPositionList[panelIndex], null);
                doorPanels.Add(doorPanel);
            }

            return doorPanels;
        }

        /// <summary>
        /// Creates door lining properties.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="familyInstance">
        /// The family instance of a door.
        /// </param>
        /// <returns>
        /// The handle created.
        /// </returns>
        public static IFCAnyHandle CreateDoorLiningProperties(ExporterIFC exporterIFC, Element familyInstance)
        {
            IFCFile file = exporterIFC.GetFile();
            IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();

            double? liningDepthOpt = null;
            double? liningThicknessOpt = null;
            double? thresholdDepthOpt = null;
            double? thresholdThicknessOpt = null;
            double? transomThicknessOpt = null;
            double? transomOffsetOpt = null;
            double? liningOffsetOpt = null;
            double? thresholdOffsetOpt = null;
            double? casingThicknessOpt = null;
            double? casingDepthOpt = null;

            double value1, value2;

            // both of these must be defined, or not defined - if only one is defined, we ignore the values.
            if (ParameterUtil.GetPositiveDoubleValueFromElementOrSymbol(familyInstance, "LiningDepth", out value1))
            {
                if (ParameterUtil.GetPositiveDoubleValueFromElementOrSymbol(familyInstance, "LiningThickness", out value2))
                {
                    liningDepthOpt = UnitUtil.ScaleLength(value1);
                    liningThicknessOpt = UnitUtil.ScaleLength(value2);
                }
            }

            if (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "LiningOffset", out value1))
                liningOffsetOpt = UnitUtil.ScaleLength(value1);

            // both of these must be defined, or not defined - if only one is defined, we ignore the values.
            if (ParameterUtil.GetPositiveDoubleValueFromElementOrSymbol(familyInstance, "ThresholdDepth", out value1))
            {
                if (ParameterUtil.GetPositiveDoubleValueFromElementOrSymbol(familyInstance, "ThresholdThickness", out value2))
                {
                    thresholdDepthOpt = UnitUtil.ScaleLength(value1);
                    thresholdThicknessOpt = UnitUtil.ScaleLength(value2);
                }
            }

            if (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "ThreshholdOffset", out value1))
                liningOffsetOpt = UnitUtil.ScaleLength(value1);

            // both of these must be defined, or not defined - if only one is defined, we ignore the values.
            if (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "TransomOffset", out value1))
            {
                if (ParameterUtil.GetPositiveDoubleValueFromElementOrSymbol(familyInstance, "TransomThickness", out value2))
                {
                    transomOffsetOpt = UnitUtil.ScaleLength(value1);
                    transomThicknessOpt = UnitUtil.ScaleLength(value2);
                }
            }

            // both of these must be defined, or not defined - if only one is defined, we ignore the values.
            if (ParameterUtil.GetPositiveDoubleValueFromElementOrSymbol(familyInstance, "CasingDepth", out value1))
            {
                if (ParameterUtil.GetPositiveDoubleValueFromElementOrSymbol(familyInstance, "CasingThickness", out value2))
                {
                    casingDepthOpt = UnitUtil.ScaleLength(value1);
                    casingThicknessOpt = UnitUtil.ScaleLength(value2);
                }
            }

            string doorLiningGUID = GUIDUtil.CreateSubElementGUID(familyInstance, (int)IFCDoorSubElements.DoorLining);
            string doorLiningName = NamingUtil.GetIFCName(familyInstance);
            return IFCInstanceExporter.CreateDoorLiningProperties(file, doorLiningGUID, ownerHistory,
               doorLiningName, null, liningDepthOpt, liningThicknessOpt, thresholdDepthOpt, thresholdThicknessOpt,
               transomThicknessOpt, transomOffsetOpt, liningOffsetOpt, thresholdOffsetOpt, casingThicknessOpt,
               casingDepthOpt, null);
        }

        /// <summary>
        /// Gets door panel position.
        /// </summary>
        /// <param name="typeName">
        /// The type name of the door.
        /// </param>
        /// <param name="element">
        /// The door element.
        /// </param>
        /// <param name="number">
        /// The number of panel position.
        /// </param>
        /// <returns>
        /// The string represents the door panel position.
        /// </returns>
        public static IFCDoorPanelPosition? GetIFCDoorPanelPosition(string typeName, Element element, int number)
        {
            string currPanelName;
            if (number == -1)
                currPanelName = "PanelPosition";
            else
                currPanelName = "PanelPosition" + number.ToString();

            string value = "";
            if (!ParameterUtil.GetStringValueFromElementOrSymbol(element, currPanelName, out value))
                value = typeName;

            if (value == "")
                return null;
            else if (String.Compare(value, "left", true) == 0)
                return IFCDoorPanelPosition.Left;
            else if (String.Compare(value, "middle", true) == 0)
                return IFCDoorPanelPosition.Middle;
            else if (String.Compare(value, "right", true) == 0)
                return IFCDoorPanelPosition.Right;
            else
                return IFCDoorPanelPosition.NotDefined;
        }

        /// <summary>
        /// Reverses door panel position.
        /// </summary>
        /// <param name="originalPosition">
        /// The original position.
        /// </param>
        /// <returns>
        /// The string represents the reversed door panel position.
        /// </returns>
        public static IFCDoorPanelPosition ReverseDoorPanelPosition(IFCDoorPanelPosition? originalPosition)
        {
            if (originalPosition == null)
                return IFCDoorPanelPosition.NotDefined;
            else if (originalPosition == IFCDoorPanelPosition.Left)
                return IFCDoorPanelPosition.Right;
            else if (originalPosition == IFCDoorPanelPosition.Right)
                return IFCDoorPanelPosition.Left;
            return (IFCDoorPanelPosition)originalPosition;
        }

        /// <summary>
        /// Gets window style operation.
        /// </summary>
        /// <param name="familySymbol">
        /// The element type of window.
        /// </param>
        /// <returns>
        /// The IFCWindowStyleOperation.
        /// </returns>
        public static Toolkit.IFCWindowStyleOperation GetIFCWindowStyleOperation(ElementType familySymbol)
        {
            string value;
            ParameterUtil.GetStringValueFromElement(familySymbol, BuiltInParameter.WINDOW_OPERATION_TYPE, out value);

            if (String.IsNullOrEmpty(value))
                return Toolkit.IFCWindowStyleOperation.NotDefined;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "UserDefined"))
                return Toolkit.IFCWindowStyleOperation.UserDefined;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "SinglePanel"))
                return Toolkit.IFCWindowStyleOperation.Single_Panel;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "DoublePanelVertical"))
                return Toolkit.IFCWindowStyleOperation.Double_Panel_Vertical;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "DoublePanelHorizontal"))
                return Toolkit.IFCWindowStyleOperation.Double_Panel_Horizontal;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "TriplePanelVertical"))
                return Toolkit.IFCWindowStyleOperation.Triple_Panel_Vertical;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "TriplePanelBottom"))
                return Toolkit.IFCWindowStyleOperation.Triple_Panel_Bottom;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "TriplePanelTop"))
                return Toolkit.IFCWindowStyleOperation.Triple_Panel_Top;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "TriplePanelLeft"))
                return Toolkit.IFCWindowStyleOperation.Triple_Panel_Left;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "TriplePanelRight"))
                return Toolkit.IFCWindowStyleOperation.Triple_Panel_Right;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "TriplePanelHorizontal"))
                return Toolkit.IFCWindowStyleOperation.Triple_Panel_Horizontal;

            return Toolkit.IFCWindowStyleOperation.NotDefined;
        }

        /// <summary>
        /// Gets IFCDoorStyleConstruction from construction type name.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>The IFCDoorStyleConstruction.</returns>
        public static IFCDoorStyleConstruction GetDoorStyleConstruction(Element element)
        {
            string value = null;
            if (!ParameterUtil.GetStringValueFromElementOrSymbol(element, "Construction", out value))
                ParameterUtil.GetStringValueFromElementOrSymbol(element, BuiltInParameter.DOOR_CONSTRUCTION_TYPE, false, out value);

            if (String.IsNullOrEmpty(value))
                return IFCDoorStyleConstruction.NotDefined;

            string newValue = NamingUtil.RemoveSpacesAndUnderscores(value);

            if (String.Compare(newValue, "USERDEFINED", true) == 0)
                return IFCDoorStyleConstruction.UserDefined;
            if (String.Compare(newValue, "ALUMINIUM", true) == 0)
                return IFCDoorStyleConstruction.Aluminium;
            if (String.Compare(newValue, "HIGH_GRADE_STEEL", true) == 0)
                return IFCDoorStyleConstruction.High_Grade_Steel;
            if (String.Compare(newValue, "STEEL", true) == 0)
                return IFCDoorStyleConstruction.Steel;
            if (String.Compare(newValue, "WOOD", true) == 0)
                return IFCDoorStyleConstruction.Wood;
            if (String.Compare(newValue, "ALUMINIUM_WOOD", true) == 0)
                return IFCDoorStyleConstruction.Aluminium_Wood;
            if (String.Compare(newValue, "ALUMINIUM_PLASTIC", true) == 0)
                return IFCDoorStyleConstruction.Aluminium_Plastic;
            if (String.Compare(newValue, "PLASTIC", true) == 0)
                return IFCDoorStyleConstruction.Plastic;

            return IFCDoorStyleConstruction.UserDefined;
        }
    
        /// <summary>
        /// Gets window style construction.
        /// </summary>
        /// <param name="element">The window element.</param>
        /// <returns>The string represents the window style construction.</returns>
        public static IFCWindowStyleConstruction GetIFCWindowStyleConstruction(Element element)
        {
            string value;
            if (!ParameterUtil.GetStringValueFromElementOrSymbol(element, "Construction", out value))
                ParameterUtil.GetStringValueFromElementOrSymbol(element, BuiltInParameter.WINDOW_CONSTRUCTION_TYPE, false, out value);

            if (String.IsNullOrWhiteSpace(value))
                return IFCWindowStyleConstruction.NotDefined;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "Aluminum"))
                return IFCWindowStyleConstruction.Aluminium;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "HighGradeSteel"))
                return IFCWindowStyleConstruction.High_Grade_Steel;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "Steel"))
                return IFCWindowStyleConstruction.Steel;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "Wood"))
                return IFCWindowStyleConstruction.Wood;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "AluminumWood"))
                return IFCWindowStyleConstruction.Aluminium_Wood;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "Plastic"))
                return IFCWindowStyleConstruction.Plastic;
            
            //else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "OtherConstruction"))
            return IFCWindowStyleConstruction.Other_Construction;
        }

        /// <summary>
        /// Gets window panel operation.
        /// </summary>
        /// <param name="initialValue">
        /// The initial value.
        /// </param>
        /// <param name="element">
        /// The window element.
        /// </param>
        /// <param name="number">
        /// The number of panel operation.
        /// </param>
        /// <returns>
        /// The string represents the window panel operation.
        /// </returns>
        public static IFCWindowPanelOperation GetIFCWindowPanelOperation(string initialValue, Element element, int number)
        {
            string currPanelName = "PanelOperation" + number.ToString();

            string value;
            if (!ParameterUtil.GetStringValueFromElementOrSymbol(element, currPanelName, out value))
                value = initialValue;

            if (value == "")
                return IFCWindowPanelOperation.NotDefined;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "SideHungRightHand"))
                return IFCWindowPanelOperation.SideHungRightHand;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "SideHungLeftHand"))
                return IFCWindowPanelOperation.SideHungLeftHand;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "TiltAndTurnRightHand"))
                return IFCWindowPanelOperation.TiltAndTurnRightHand;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "TiltAndTurnLeftHand"))
                return IFCWindowPanelOperation.TiltAndTurnLeftHand;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "TopHung"))
                return IFCWindowPanelOperation.TopHung;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "BottomHung"))
                return IFCWindowPanelOperation.BottomHung;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "PivotHorizontal"))
                return IFCWindowPanelOperation.PivotHorizontal;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "PivotVertical"))
                return IFCWindowPanelOperation.PivotVertical;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "SlidingHorizontal"))
                return IFCWindowPanelOperation.SlidingHorizontal;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "SlidingVertical"))
                return IFCWindowPanelOperation.SlidingVertical;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "RemovableCasement"))
                return IFCWindowPanelOperation.RemovableCasement;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "FixedCasement"))
                return IFCWindowPanelOperation.FixedCasement;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "OtherOperation"))
                return IFCWindowPanelOperation.OtherOperation;

            return IFCWindowPanelOperation.NotDefined;
        }

        /// <summary>
        /// Gets window panel position.
        /// </summary>
        /// <param name="initialValue">
        /// The initial value.
        /// </param>
        /// <param name="element">
        /// The window element.
        /// </param>
        /// <param name="number">
        /// The number of panel position.
        /// </param>
        /// <returns>
        /// The string represents the window panel position.
        /// </returns>
        public static IFCWindowPanelPosition GetIFCWindowPanelPosition(string initialValue, Element element, int number)
        {
            string currPanelName = "PanelPosition" + number.ToString();

            string value;
            if (!ParameterUtil.GetStringValueFromElementOrSymbol(element, currPanelName, out value))
                value = initialValue;

            if (value == "")
                return IFCWindowPanelPosition.NotDefined;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "Left"))
                return IFCWindowPanelPosition.Left;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "Middle"))
                return IFCWindowPanelPosition.Middle;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "Right"))
                return IFCWindowPanelPosition.Right;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "Bottom"))
                return IFCWindowPanelPosition.Bottom;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "Top"))
                return IFCWindowPanelPosition.Top;

            return IFCWindowPanelPosition.NotDefined;
        }

        /// <summary>
        /// Creates window panel position.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="familyInstance">
        /// The family instance of a window.
        /// </param>
        /// <param name="description">
        /// The description.
        /// </param>
        /// <returns>
        /// The handle created.
        /// </returns>
        public static IFCAnyHandle CreateWindowLiningProperties(ExporterIFC exporterIFC,
           Element familyInstance, string description)
        {
            IFCFile file = exporterIFC.GetFile();
            IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();

            double? liningDepthOpt = null;
            double? liningThicknessOpt = null;
            double? transomThicknessOpt = null;
            double? mullionThicknessOpt = null;
            double? firstTransomOffsetOpt = null;
            double? secondTransomOffsetOpt = null;
            double? firstMullionOffsetOpt = null;
            double? secondMullionOffsetOpt = null;

            double value1 = 0.0;
            double value2 = 0.0;

            // both of these must be defined (or not defined)
            if (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "LiningDepth", out value1) &&
               ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "LiningThickness", out value2))
            {
                liningDepthOpt = UnitUtil.ScaleLength(value1);
                liningThicknessOpt = UnitUtil.ScaleLength(value2);
            }

            if (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "TransomThickness", out value1))
                transomThicknessOpt = UnitUtil.ScaleLength(value1);

            if (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "FirstTransomOffset", out value1))
                firstTransomOffsetOpt = UnitUtil.ScaleLength(value1);

            if (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "SecondTransomOffset", out value1))
                secondTransomOffsetOpt = UnitUtil.ScaleLength(value1);

            if (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "MullionThickness", out value1))
                mullionThicknessOpt = UnitUtil.ScaleLength(value1);

            if (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "FirstMullionOffset", out value1))
                firstMullionOffsetOpt = UnitUtil.ScaleLength(value1);

            if (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "SecondMullionOffset", out value1))
                secondMullionOffsetOpt = UnitUtil.ScaleLength(value1);

            string windowLiningGUID = GUIDUtil.CreateGUID();
            string windowLiningName = NamingUtil.GetIFCName(familyInstance);
            return IFCInstanceExporter.CreateWindowLiningProperties(file, windowLiningGUID, ownerHistory,
               windowLiningName, description, liningDepthOpt, liningThicknessOpt, transomThicknessOpt, mullionThicknessOpt,
               firstTransomOffsetOpt, secondTransomOffsetOpt, firstMullionOffsetOpt, secondMullionOffsetOpt, null);
        }

        /// <summary>
        /// Creates window panel properties.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="doorWindowInfo">
        /// The IFCDoorWindowInfo object.
        /// </param>
        /// <param name="familyInstance">
        /// The family instance of a window.
        /// </param>
        /// <param name="description">
        /// The description.
        /// </param>
        /// <returns>
        /// The list of handles created.
        /// </returns>
        public static IList<IFCAnyHandle> CreateWindowPanelProperties(ExporterIFC exporterIFC,
           Element familyInstance, string description)
        {
            IList<IFCAnyHandle> panels = new List<IFCAnyHandle>();
            IFCFile file = exporterIFC.GetFile();
            IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();

            const int maxPanels = 1000;  // arbitrary large number to prevent infinite loops.
            for (int panelNumber = 1; panelNumber < maxPanels; panelNumber++)
            {
                string frameDepthCurrString = "FrameDepth" + panelNumber.ToString();
                string frameThicknessCurrString = "FrameThickness" + panelNumber.ToString();

                IFCWindowPanelOperation panelOperation = GetIFCWindowPanelOperation("", familyInstance, panelNumber);
                IFCWindowPanelPosition panelPosition = GetIFCWindowPanelPosition("", familyInstance, panelNumber);
                if (panelOperation == IFCWindowPanelOperation.NotDefined && panelPosition == IFCWindowPanelPosition.NotDefined)
                    break;

                double? frameDepth = null;
                double? frameThickness = null;

                double value1, value2;
                if ((ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, frameDepthCurrString, out value1) ||
                    ((panelNumber == 1) && (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "FrameDepth", out value1)))) &&
                   (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, frameThicknessCurrString, out value2) ||
                    ((panelNumber == 1) && (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "FrameThickness", out value2)))))
                {
                    frameDepth = UnitUtil.ScaleLength(value1);
                    frameThickness = UnitUtil.ScaleLength(value2);
                }

                string panelGUID = GUIDUtil.CreateGUID();
                string panelName = NamingUtil.GetIFCNamePlusIndex(familyInstance, panelNumber);
                panels.Add(IFCInstanceExporter.CreateWindowPanelProperties(file, panelGUID, ownerHistory,
                   panelName, description, panelOperation, panelPosition, frameDepth, frameThickness, null));
            }
            return panels;
        }

        /// <summary>
        /// Access the HostObjects map to get the handle associated with a wall at a particular level.  This does something special only 
        /// for walls split by level.
        /// </summary>
        /// <param name="exporterIFC">The exporterIFC class.</param>
        /// <param name="hostId">The (wall) host id.</param>
        /// <param name="levelId">The level id.</param>
        /// <returns>The IFC handle associated with the host at that level.</returns>
        static public IFCAnyHandle GetHndForHostAndLevel(ExporterIFC exporterIFC, ElementId hostId, ElementId levelId)
        {
            if (hostId == ElementId.InvalidElementId)
                return null;

            IFCAnyHandle hostObjectHnd = null;

            IList<IDictionary<ElementId, IFCAnyHandle>> hostObjects = exporterIFC.GetHostObjects();
            int idx = -1;
            if (ExporterCacheManager.HostObjectsLevelIndex.TryGetValue(levelId, out idx))
            {
                IDictionary<ElementId, IFCAnyHandle> mapForLevel = hostObjects[idx];
                mapForLevel.TryGetValue(hostId, out hostObjectHnd);
            }

            // If we can't find a specific handle for the host on that level, look for a generic handle for the host.
            // These are stored in the "invalidElementId" level id map.
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(hostObjectHnd))
            {
                if (ExporterCacheManager.HostObjectsLevelIndex.TryGetValue(ElementId.InvalidElementId, out idx))
                {
                    IDictionary<ElementId, IFCAnyHandle> mapForLevel = hostObjects[idx];
                    mapForLevel.TryGetValue(hostId, out hostObjectHnd);
                }
            }

            return hostObjectHnd;
        }

        private static void ComputeArcBoundingBox(Arc arc, IList<XYZ> pts, double startParam, double endParam)
        {
            XYZ point = arc.Evaluate(startParam, false);
            XYZ otherPoint = arc.Evaluate(endParam, false);

            double eps = MathUtil.Eps();
            XYZ maximum = new XYZ(Math.Max(point[0], otherPoint[0]),
                Math.Max(point[1], otherPoint[1]),
                Math.Max(point[2], otherPoint[2]));
            XYZ minimum = new XYZ(Math.Min(point[0], otherPoint[0]),
                Math.Min(point[1], otherPoint[1]),
                Math.Min(point[2], otherPoint[2]));
            
            if (endParam < startParam + eps)
                return;

            // find mins and maxs along each axis
            for (int aa = 0; aa < 3; aa++)    // aa is the axis index
            {
                XYZ axis = new XYZ((aa == 0) ? 1 : 0, (aa == 1) ? 1 : 0, (aa == 2) ? 1 : 0);
                double xProj = arc.XDirection.DotProduct(axis);
                double yProj = arc.YDirection.DotProduct(axis);
                if (Math.Abs(xProj) < eps && Math.Abs(yProj) < eps)
                    continue;

                double angle = Math.Atan2(yProj, xProj);
      
                if (angle > startParam)
                    angle -= Math.PI * ((int) ((angle - startParam)/Math.PI));
                else
                    angle += Math.PI * (1 + ((int) ((startParam - angle)/Math.PI)));

                for (; angle < endParam; angle += Math.PI)
                {
                    point = arc.Evaluate(angle, false);
                    maximum = new XYZ(Math.Max(point[0], maximum[0]),
                        Math.Max(point[1], maximum[1]),
                        Math.Max(point[2], maximum[2]));
                    minimum = new XYZ(Math.Min(point[0], minimum[0]),
                        Math.Min(point[1], minimum[1]),
                        Math.Min(point[2], minimum[2]));
                }
            }

            pts.Add(minimum);
            pts.Add(maximum);
        }

        private static void ComputeArcBoundingBox(Arc arc, IList<XYZ> pts)
        {
            if (arc == null)
                return;

            if (arc.IsBound)
            {
                ComputeArcBoundingBox(arc, pts, arc.GetEndParameter(0), arc.GetEndParameter(1));
            }
            else
            {
                ComputeArcBoundingBox(arc, pts, 0.0, Math.PI);
                ComputeArcBoundingBox(arc, pts, Math.PI, 2.0 * Math.PI);
            }
        }

        private static BoundingBoxXYZ ComputeApproximateCurveLoopBBoxForOpening(CurveLoop curveLoop, Plane plane)
        {
            Transform trf = null;
            Transform trfInv = null;

            XYZ ll = null;
            XYZ ur = null;
            if (plane != null)
            {
                trf = Transform.Identity;
                trf.BasisX = plane.XVec;
                trf.BasisY = plane.YVec;
                trf.BasisZ = plane.Normal;
                trf.Origin = plane.Origin;
                trfInv = trf.Inverse;
            }

            bool init = false;
            foreach (Curve curve in curveLoop)
            {
                IList<XYZ> pts = new List<XYZ>();
                if (curve is Line) 
                {
                    pts.Add(curve.GetEndPoint(0));
                    pts.Add(curve.GetEndPoint(1));
                }
                else if (curve is Arc)
                {
                    ComputeArcBoundingBox(curve as Arc, pts);
                }
                else
                    pts = curve.Tessellate();

                foreach (XYZ pt in pts)
                {
                    XYZ ptToUse = (trf != null) ? trfInv.OfPoint(pt) : pt;
                    if (!init)
                    {
                        ll = ptToUse;
                        ur = ptToUse;
                        init = true;
                    }
                    else
                    {
                        ll = new XYZ(Math.Min(ll.X, ptToUse.X), Math.Min(ll.Y, ptToUse.Y), Math.Min(ll.Z, ptToUse.Z));
                        ur = new XYZ(Math.Max(ur.X, ptToUse.X), Math.Max(ur.Y, ptToUse.Y), Math.Max(ur.Z, ptToUse.Z));
                    }
                }
            }

            if (!init)
                return null;

            if (trf != null)
            {
                ll = trf.OfPoint(ll);
                ur = trf.OfPoint(ur);
            }

            BoundingBoxXYZ curveLoopBounds = new BoundingBoxXYZ();
            curveLoopBounds.set_Bounds(0, ll);
            curveLoopBounds.set_Bounds(1, ur);
            return curveLoopBounds;
        }

        private static IFCAnyHandle CopyOpeningHandle(ExporterIFC exporterIFC, Element elem, ElementId catId, IFCAnyHandle origOpeningHnd)
        {
            IFCFile file = exporterIFC.GetFile();

            IFCAnyHandle origLocalPlacement = IFCAnyHandleUtil.GetObjectPlacement(origOpeningHnd);
            IFCAnyHandle newLocalPlacement = ExporterUtil.CopyLocalPlacement(file, origLocalPlacement);
            IFCAnyHandle oldRepresentation = IFCAnyHandleUtil.GetRepresentation(origOpeningHnd);
            IFCAnyHandle newProdRep = ExporterUtil.CopyProductDefinitionShape(exporterIFC, elem, catId, oldRepresentation);

            IFCAnyHandle copyOwnerHistory = IFCAnyHandleUtil.GetInstanceAttribute(origOpeningHnd, "OwnerHistory");
            string copyName = IFCAnyHandleUtil.GetStringAttribute(origOpeningHnd, "Name");
            string copyDescription = IFCAnyHandleUtil.GetStringAttribute(origOpeningHnd, "Description");
            string copyObjectType = IFCAnyHandleUtil.GetStringAttribute(origOpeningHnd, "ObjectType");
            string copyElemId = IFCAnyHandleUtil.GetStringAttribute(origOpeningHnd, "Tag");

            return IFCInstanceExporter.CreateOpeningElement(file, GUIDUtil.CreateGUID(), copyOwnerHistory, copyName, copyDescription, copyObjectType,
                newLocalPlacement, newProdRep, copyElemId);
        }

        private static void CopyOpeningForSplitHosts(ExporterIFC exporterIFC, IFCAnyHandle hostObjHnd, IFCAnyHandle openingHnd, ElementId hostId,
            double openingZNonScaled, double openingHeight)
        {
            double eps = MathUtil.Eps();
            if (!ExporterCacheManager.ExportOptionsCache.WallAndColumnSplitting || (openingHeight <= eps))
                return;

            ICollection<int> usedHosts = new HashSet<int>();
            usedHosts.Add(hostObjHnd.StepId);

            double openingTop = openingZNonScaled + openingHeight;

            IDictionary<ElementId, IFCLevelInfo> storeys = exporterIFC.GetLevelInfos();
            foreach (KeyValuePair<ElementId, IFCLevelInfo> storey in storeys)
            {
                IFCLevelInfo levelInfo = storey.Value;
                double startHeight = levelInfo.Elevation - eps;
                double height = levelInfo.DistanceToNextLevel;
                bool useHeight = !MathUtil.IsAlmostZero(height);
                double endHeight = startHeight + height - eps;

                bool useThisLevel = false;

                if (!useHeight)
                {
                    if (openingTop > startHeight + eps)
                        useThisLevel = true;
                }
                else
                {
                    if ((openingZNonScaled < endHeight - eps) && (openingTop > startHeight + eps))
                        useThisLevel = true;
                }

                if (useThisLevel)
                {
                    IFCAnyHandle currHostObjHnd = DoorWindowUtil.GetHndForHostAndLevel(exporterIFC, hostId, storey.Key);
                    if (!IFCAnyHandleUtil.IsNullOrHasNoValue(currHostObjHnd) && !usedHosts.Contains(currHostObjHnd.StepId))
                    {
                        usedHosts.Add(currHostObjHnd.StepId);

                        Element hostElem = ExporterCacheManager.Document.GetElement(hostId);
                        ElementId catId = CategoryUtil.GetSafeCategoryId(hostElem);

                        IFCFile file = exporterIFC.GetFile();
                        IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();
                        string voidGuid = GUIDUtil.CreateGUID();
                        IFCAnyHandle openingElementHnd = CopyOpeningHandle(exporterIFC, hostElem, catId, openingHnd);
                        IFCInstanceExporter.CreateRelVoidsElement(file, voidGuid, ownerHistory, null, null, currHostObjHnd, openingElementHnd);
                    }
                }
            }
        }

        static public DoorWindowOpeningInfo CreateOpeningForDoorWindow(ExporterIFC exporterIFC, Document doc,
            IFCAnyHandle hostObjHnd, ElementId hostId, ElementId insertId, CurveLoop cutLoop, XYZ cutDir,
            double origUnscaledDepth, bool posHingeSide, bool isRecess)
        {
            // calculate some values.
            IFCAnyHandle openingPlacement = null;
            double openingHeight = -1.0;
            double openingWidth = -1.0;
            
            Element wallElement = doc.GetElement(hostId);
            Wall wall = (wallElement != null) ? wallElement as Wall : null;
            Curve curve = WallExporter.GetWallAxis(wall);
            if (curve == null)
                return null;

            // Don't export opening if we are exporting parts on a wall, as the parts will already have the openings cut out.
            if (PartExporter.CanExportParts(wall))
                return null;

            ElementId catId = CategoryUtil.GetSafeCategoryId(wall);

            double unScaledDepth = origUnscaledDepth;

            IFCAnyHandle hostObjPlacementHnd = IFCAnyHandleUtil.GetObjectPlacement(hostObjHnd);
            IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();

            XYZ relOrig = XYZ.Zero;
            XYZ relZ = XYZ.BasisZ;
            XYZ relX = XYZ.BasisX;

            double openingZNonScaled = -1.0;

            Plane plane = new Plane(cutDir, XYZ.Zero);

            // get height, width before transform
            BoundingBoxXYZ cutLoopBBox = ComputeApproximateCurveLoopBBoxForOpening(cutLoop, null);
            if (cutLoopBBox != null)
            {
                XYZ dist = cutLoopBBox.Max - cutLoopBBox.Min;
                openingZNonScaled = cutLoopBBox.Min.Z;
                openingHeight = Math.Abs(dist.Z);
                openingWidth = Math.Sqrt(dist.X * dist.X + dist.Y * dist.Y);
            }

            Transform openingTrf = ExporterIFCUtils.GetUnscaledTransform(exporterIFC, hostObjPlacementHnd);
            XYZ hostObjYDir = openingTrf.BasisY;
            XYZ hostObjOrig = openingTrf.Origin;
            openingTrf = openingTrf.Inverse;

            // move to wall axis
            CurveLoop tmpCutLoop = GeometryUtil.TransformCurveLoop(cutLoop, openingTrf);

            cutDir = openingTrf.OfVector(cutDir);
            if (curve is Line)
            {
                Plane cutLoopPlane = null;
                try
                {
                    cutLoopPlane = tmpCutLoop.GetPlane();
                }
                catch
                {
                    return null;
                }

                XYZ clOrig = cutLoopPlane.Origin;

                double wantOriginAtY = posHingeSide ? (-unScaledDepth / 2.0) : (unScaledDepth / 2.0);

                if (!MathUtil.IsAlmostEqual(wantOriginAtY, clOrig[1]))
                {
                    XYZ moveVec = new XYZ(0, wantOriginAtY - clOrig[1], 0);
                    tmpCutLoop = GeometryUtil.MoveCurveLoop(tmpCutLoop, moveVec);
                }

                bool cutDirRelToHostObjY = (cutDir[1] > 0.0); // true = same sense, false = opp. sense

                if (posHingeSide != cutDirRelToHostObjY)
                {
                    cutDir = cutDir * -1.0;
                    cutDirRelToHostObjY = !cutDirRelToHostObjY;  // not used beyond this point.
                }
            }
            else if ((cutLoopBBox != null) && (curve is Arc))
            {
                Arc arc = curve as Arc;
                double radius = arc.Radius;

                XYZ curveCtr = arc.Center;

                // check orientation to cutDir, make sure it points to center of arc.

                XYZ origLL = new XYZ(cutLoopBBox.Min.X, cutLoopBBox.Min.Y, curveCtr.Z);
                XYZ origUR = new XYZ(cutLoopBBox.Max.X, cutLoopBBox.Max.Y, curveCtr.Z);
                XYZ origCtr = (origLL + origUR) / 2.0;

                double centerDist = origCtr.DistanceTo(curveCtr);
                XYZ approxMoveDir = (origCtr - curveCtr).Normalize();

                bool cutDirPointingIn = (cutDir.DotProduct(approxMoveDir) < 0.0);
                bool centerInsideArc = (centerDist < radius);
                if (centerInsideArc == cutDirPointingIn)
                {
                    XYZ moveVec = cutDir * -unScaledDepth;
                    origCtr += moveVec;
                    tmpCutLoop = GeometryUtil.MoveCurveLoop(tmpCutLoop, moveVec);
                }

                // not for windows that are too big ... forget about it.  Very rare case.
                double depthFactor = openingWidth / (2.0 * radius);
                double eps = MathUtil.Eps();
                if (depthFactor < 1.0 - eps)
                {
                    double depthFactorSq = depthFactor * depthFactor * 4;
                    double extraDepth = radius * (1.0 - Math.Sqrt(1.0 - depthFactorSq));
                    if (extraDepth > eps)
                    {
                        XYZ moveVec = cutDir * -extraDepth;
                        tmpCutLoop = GeometryUtil.MoveCurveLoop(tmpCutLoop, moveVec);
                        unScaledDepth += extraDepth;
                    }
                }

                // extra fudge on the other side of the window opening.
                depthFactor = origUnscaledDepth / (2.0 * radius);
                if (depthFactor < 1.0 - eps)
                {
                    double extraDepth = radius * (1.0 - Math.Sqrt(1.0 - depthFactor));
                    if (extraDepth > eps)
                        unScaledDepth += extraDepth;
                }
            }

            XYZ cutXDir = XYZ.BasisZ;
            XYZ cutOrig = XYZ.Zero;
            XYZ cutYDir = cutDir.CrossProduct(cutXDir);
            plane = new Plane(cutXDir, cutYDir, cutOrig);

            // now move to origin in this coordinate system.
            // todo: update openingtrf if we are to use it again!
            BoundingBoxXYZ tmpBBox = ComputeApproximateCurveLoopBBoxForOpening(tmpCutLoop, plane);
            if (tmpBBox != null)
            {
                relOrig = tmpBBox.Min;
                XYZ moveVec = relOrig * -1.0;
                tmpCutLoop = GeometryUtil.MoveCurveLoop(tmpCutLoop, moveVec);
            }

            IList<CurveLoop> oCutLoopList = new List<CurveLoop>();
            oCutLoopList.Add(tmpCutLoop);

            double depth = UnitUtil.ScaleLength(unScaledDepth);

            Element doorWindowElement = doc.GetElement(insertId);

            IFCAnyHandle openingRepHnd = RepresentationUtil.CreateExtrudedProductDefShape(exporterIFC, doorWindowElement, catId, 
                oCutLoopList, plane, cutDir, depth);
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(openingRepHnd))
                return null;

            // care only about first loop.
            IFCFile file = exporterIFC.GetFile();
            XYZ scaledOrig = UnitUtil.ScaleLength(relOrig);
            openingPlacement = ExporterUtil.CreateLocalPlacement(file, hostObjPlacementHnd, scaledOrig, relZ, relX);

            string openingObjectType = isRecess ? "Recess": "Opening";
            string openingGUID = GUIDUtil.CreateSubElementGUID(doorWindowElement, (int)IFCDoorSubElements.DoorOpening);
            string origOpeningName = NamingUtil.GetIFCNamePlusIndex(doorWindowElement, 1);
            string openingName = NamingUtil.GetNameOverride(doorWindowElement, origOpeningName);

            IFCAnyHandle openingHnd = IFCInstanceExporter.CreateOpeningElement(file, openingGUID, ownerHistory, openingName, null,
                openingObjectType, openingPlacement, openingRepHnd, null);

            string openingVoidsGUID = GUIDUtil.CreateSubElementGUID(doorWindowElement, (int)IFCDoorSubElements.DoorOpeningRelVoid);
            IFCInstanceExporter.CreateRelVoidsElement(file, openingVoidsGUID, ownerHistory, null, null, hostObjHnd, openingHnd);

            if (ExporterCacheManager.ExportOptionsCache.ExportBaseQuantities)
            {
                using (IFCExtrusionCreationData extraParams = new IFCExtrusionCreationData())
                {
                    double height = 0.0, width = 0.0;
                    if (ExtrusionExporter.ComputeHeightWidthOfCurveLoop(tmpCutLoop, plane, out height, out width))
                    {
                        extraParams.ScaledHeight = UnitUtil.ScaleLength(height);
                        extraParams.ScaledWidth = UnitUtil.ScaleLength(width);
                    }

                    IList<CurveLoop> curveLoops = new List<CurveLoop>();
                    curveLoops.Add(tmpCutLoop);
                    double area = ExporterIFCUtils.ComputeAreaOfCurveLoops(curveLoops);
                    if (area > 0.0)
                        extraParams.ScaledArea = UnitUtil.ScaleArea(area);

                    extraParams.ScaledLength = depth;
                    PropertyUtil.CreateOpeningQuantities(exporterIFC, openingHnd, extraParams);
                }
            }

            // we may need to create copies of the opening if we have split the original wall.
            CopyOpeningForSplitHosts(exporterIFC, hostObjHnd, openingHnd, hostId, openingZNonScaled, openingHeight);

            return DoorWindowOpeningInfo.Create(openingHnd, openingPlacement, openingHeight, openingWidth);
        }

        static public DoorWindowOpeningInfo CreateOpeningForDoorWindow(ExporterIFC exporterIFC, Document doc,
            IFCAnyHandle hostObjHnd, ElementId hostId, ElementId insertId, Solid solid, double scaledHostWidth, bool isRecess)
        {
            IFCFile file = exporterIFC.GetFile();
            Element hostElement = doc.GetElement(hostId);
            Element insertElement = doc.GetElement(insertId);

            ElementId catId = CategoryUtil.GetSafeCategoryId(hostElement);

            using (PlacementSetter setter = PlacementSetter.Create(exporterIFC, insertElement))
            {
                using (IFCExtrusionCreationData extrusionCreationData = new IFCExtrusionCreationData())
                {
                    extrusionCreationData.SetLocalPlacement(ExporterUtil.CreateLocalPlacement(file, setter.LocalPlacement, null));
                    extrusionCreationData.ReuseLocalPlacement = true;

                    IFCAnyHandle openingHnd = OpeningUtil.CreateOpening(exporterIFC, hostObjHnd, hostElement, insertElement, solid, scaledHostWidth, isRecess, extrusionCreationData, null, null);

                    return DoorWindowOpeningInfo.Create(openingHnd, extrusionCreationData.GetLocalPlacement(),
                        UnitUtil.UnscaleLength(extrusionCreationData.ScaledHeight), UnitUtil.UnscaleLength(extrusionCreationData.ScaledWidth));
                }
            }
        }

        /// <summary>
        /// Gets IFCDoorStyleOperation from Revit IFCDoorStyleOperation.
        /// </summary>
        /// <param name="operation">The Revit IFCDoorStyleOperation.</param>
        /// <returns>The IFCDoorStyleOperation.</returns>
        public static Toolkit.IFCDoorStyleOperation GetDoorStyleOperation(Autodesk.Revit.DB.IFC.IFCDoorStyleOperation operation)
        {
            switch (operation)
            {
                case Autodesk.Revit.DB.IFC.IFCDoorStyleOperation.DoubleDoorDoubleSwing:
                    return Toolkit.IFCDoorStyleOperation.Double_Door_Double_Swing;
                case Autodesk.Revit.DB.IFC.IFCDoorStyleOperation.DoubleDoorFolding:
                    return Toolkit.IFCDoorStyleOperation.Double_Door_Folding;
                case Autodesk.Revit.DB.IFC.IFCDoorStyleOperation.DoubleDoorSingleSwing:
                    return Toolkit.IFCDoorStyleOperation.Double_Door_Single_Swing;
                case Autodesk.Revit.DB.IFC.IFCDoorStyleOperation.DoubleDoorSingleSwingOppositeLeft:
                    return Toolkit.IFCDoorStyleOperation.Double_Door_Single_Swing_Opposite_Left;
                case Autodesk.Revit.DB.IFC.IFCDoorStyleOperation.DoubleDoorSingleSwingOppositeRight:
                    return Toolkit.IFCDoorStyleOperation.Double_Door_Single_Swing_Opposite_Right;
                case Autodesk.Revit.DB.IFC.IFCDoorStyleOperation.DoubleDoorSliding:
                    return Toolkit.IFCDoorStyleOperation.Double_Door_Sliding;
                case Autodesk.Revit.DB.IFC.IFCDoorStyleOperation.DoubleSwingLeft:
                    return Toolkit.IFCDoorStyleOperation.Double_Swing_Left;
                case Autodesk.Revit.DB.IFC.IFCDoorStyleOperation.DoubleSwingRight:
                    return Toolkit.IFCDoorStyleOperation.Double_Swing_Right;
                case Autodesk.Revit.DB.IFC.IFCDoorStyleOperation.FoldingToLeft:
                    return Toolkit.IFCDoorStyleOperation.Folding_To_Left;
                case Autodesk.Revit.DB.IFC.IFCDoorStyleOperation.FoldingToRight:
                    return Toolkit.IFCDoorStyleOperation.Folding_To_Right;
                case Autodesk.Revit.DB.IFC.IFCDoorStyleOperation.Revolving:
                    return Toolkit.IFCDoorStyleOperation.Revolving;
                case Autodesk.Revit.DB.IFC.IFCDoorStyleOperation.RollingUp:
                    return Toolkit.IFCDoorStyleOperation.RollingUp;
                case Autodesk.Revit.DB.IFC.IFCDoorStyleOperation.SingleSwingLeft:
                    return Toolkit.IFCDoorStyleOperation.Single_Swing_Left;
                case Autodesk.Revit.DB.IFC.IFCDoorStyleOperation.SingleSwingRight:
                    return Toolkit.IFCDoorStyleOperation.Single_Swing_Right;
                case Autodesk.Revit.DB.IFC.IFCDoorStyleOperation.SlidingToLeft:
                    return Toolkit.IFCDoorStyleOperation.Sliding_To_Left;
                case Autodesk.Revit.DB.IFC.IFCDoorStyleOperation.SlidingToRight:
                    return Toolkit.IFCDoorStyleOperation.Sliding_To_Right;
                case Autodesk.Revit.DB.IFC.IFCDoorStyleOperation.UserDefined:
                    return Toolkit.IFCDoorStyleOperation.UserDefined;
                case Autodesk.Revit.DB.IFC.IFCDoorStyleOperation.NotDefined:
                    return Toolkit.IFCDoorStyleOperation.NotDefined;
                default:
                    throw new ArgumentException("No corresponding type.", "operation");
            }
        }
    }
}