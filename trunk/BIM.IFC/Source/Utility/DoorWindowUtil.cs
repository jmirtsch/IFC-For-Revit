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

namespace BIM.IFC.Utility
{
    /// <summary>
    /// Provides static methods for door and window related manipulations.
    /// </summary>
    class DoorWindowUtil
    {
        /// <summary>
        /// Gets the panel operation from door style operation.
        /// </summary>
        /// <parameter name="ifcDoorStyleOperationType">
        /// The IFCDoorStyleOperation.
        /// </parameter>
        /// <returns>
        /// The string represents the door panel operation.
        /// </returns>
        public static string GetPanelOperationFromDoorStyleOperation(IFCDoorStyleOperation ifcDoorStyleOperationType)
        {
            switch (ifcDoorStyleOperationType)
            {
                case IFCDoorStyleOperation.SingleSwingLeft:
                case IFCDoorStyleOperation.SingleSwingRight:
                case IFCDoorStyleOperation.DoubleDoorSingleSwing:
                case IFCDoorStyleOperation.DoubleDoorSingleSwingOppositeLeft:
                case IFCDoorStyleOperation.DoubleDoorSingleSwingOppositeRight:
                    return "Swinging";
                case IFCDoorStyleOperation.DoubleSwingLeft:
                case IFCDoorStyleOperation.DoubleSwingRight:
                case IFCDoorStyleOperation.DoubleDoorDoubleSwing:
                    return "DoubleActing";
                case IFCDoorStyleOperation.SlidingToLeft:
                case IFCDoorStyleOperation.SlidingToRight:
                case IFCDoorStyleOperation.DoubleDoorSliding:
                    return "Sliding";
                case IFCDoorStyleOperation.FoldingToLeft:
                case IFCDoorStyleOperation.FoldingToRight:
                case IFCDoorStyleOperation.DoubleDoorFolding:
                    return "Folding";
                case IFCDoorStyleOperation.Revolving:
                    return "Revolving";
                case IFCDoorStyleOperation.RollingUp:
                    return "RollingUp";
                case IFCDoorStyleOperation.UserDefined:
                    return "UserDefined";
                default:
                    return "NotDefined";
            }
        }

        /// <summary>
        /// Creates door panel properties.
        /// </summary>
        /// <parameter name="exporterIFC">
        /// The ExporterIFC object.
        /// </parameter>
        /// <parameter name="doorWindowInfo">
        /// The IFCDoorWindowInfo object.
        /// </parameter>
        /// <parameter name="familyInstance">
        /// The family instance of a door.
        /// </parameter>
        /// <returns>
        /// The list of handles created.
        /// </returns>
        public static IList<IFCAnyHandle> CreateDoorPanelProperties(ExporterIFC exporterIFC,
           IFCDoorWindowInfo doorWindowInfo, Element familyInstance)
        {
            IFCFile file = exporterIFC.GetFile();
            IFCLabel descriptionOpt = IFCLabel.Create();
            IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();

            IList<IFCAnyHandle> doorPanels = new List<IFCAnyHandle>();

            IList<IFCMeasureValue> panelDepthOptList = new List<IFCMeasureValue>();
            IList<IFCMeasureValue> panelWidthOptList = new List<IFCMeasureValue>();

            IList<string> panelOperationList = new List<string>();
            IList<string> panelPositionList = new List<string>();

            int panelNum = 1;
            const int maxPanels = 64;  // arbitrary large number to prevent infinite loops.
            for (; panelNum < maxPanels; panelNum++)
            {
                string panelDepthCurrString = "PanelDepth" + panelNum.ToString();
                string panelWidthCurrString = "PanelWidth" + panelNum.ToString();

                // We will always have at least one panel definition as long as the panelOperation is not
                // NotDefined.

                panelOperationList.Add(GetPanelOperationFromDoorStyleOperation(doorWindowInfo.DoorOperationType));

                // If the panel operation is defined we'll allow no panel position for the 1st panel.
                string panelPosition = GetIFCDoorPanelPosition("", familyInstance, panelNum);
                if (panelPosition == "")
                {
                    if (panelNum == 1)
                        panelPosition = GetIFCDoorPanelPosition("", familyInstance, -1);
                    if ((panelPosition == "") && (panelNum > 1))
                    {
                        panelPositionList.Add("");
                        break;
                    }
                }

                if (doorWindowInfo.IsFlippedInX ^ doorWindowInfo.IsFlippedInY)
                    panelPosition = ReverseDoorPanelPosition(panelPosition);

                panelPositionList.Add(panelPosition);

                double value1 = 0.0, value2 = 0.0;
                bool foundDepth = ParameterUtil.GetPositiveDoubleValueFromElementOrSymbol(familyInstance, panelDepthCurrString, out value1);
                if (!foundDepth && (panelNum == 1))
                    foundDepth = ParameterUtil.GetPositiveDoubleValueFromElementOrSymbol(familyInstance, "PanelDepth", out value1);

                bool foundWidth = ParameterUtil.GetPositiveDoubleValueFromElementOrSymbol(familyInstance, panelWidthCurrString, out value2);
                if (!foundWidth && (panelNum == 1))
                    foundWidth = ParameterUtil.GetPositiveDoubleValueFromElementOrSymbol(familyInstance, "PanelWidth", out value2);

                if (foundDepth && foundWidth)
                {
                    panelDepthOptList.Add(IFCMeasureValue.Create(value1));
                    panelWidthOptList.Add(IFCMeasureValue.Create(value2));
                }
                else
                {
                    panelDepthOptList.Add(IFCMeasureValue.Create());
                    panelWidthOptList.Add(IFCMeasureValue.Create());
                }
            }

            // calculate panelWidths
            double totalPanelWidth = 0.0;
            for (int panelIdx = 0; (panelIdx < panelNum - 1); panelIdx++)
            {
                if (!panelDepthOptList[panelIdx].HasValue || MathUtil.IsAlmostZero(panelDepthOptList[panelIdx].GetValue()) ||
                    !panelWidthOptList[panelIdx].HasValue || MathUtil.IsAlmostZero(panelWidthOptList[panelIdx].GetValue()))
                {
                    totalPanelWidth = 0.0;
                    break;
                }
                totalPanelWidth += panelWidthOptList[panelIdx].GetValue();
            }

            if (!MathUtil.IsAlmostZero(totalPanelWidth))
            {
                for (int panelIdx = 0; (panelIdx < panelNum - 1); panelIdx++)
                {
                    IFCMeasureValue currPanelWidthOpt = IFCMeasureValue.Create(panelWidthOptList[panelIdx].GetValue() / totalPanelWidth);

                    IFCLabel doorPanelGUID = IFCLabel.CreateGUID();
                    IFCLabel doorPanelName = NamingUtil.CreateIFCName(exporterIFC, -1);
                    IFCAnyHandle shapeAspectStyleOpt = IFCAnyHandle.Create();
                    IFCAnyHandle doorPanel = file.CreateDoorPanelProperties(doorPanelGUID, ownerHistory,
                       doorPanelName, descriptionOpt, panelDepthOptList[panelIdx], panelOperationList[panelIdx],
                       currPanelWidthOpt, panelPositionList[panelIdx], shapeAspectStyleOpt);
                    doorPanels.Add(doorPanel);
                }
            }

            return doorPanels;
        }

        /// <summary>
        /// Creates door lining properties.
        /// </summary>
        /// <parameter name="exporterIFC">
        /// The ExporterIFC object.
        /// </parameter>
        /// <parameter name="familyInstance">
        /// The family instance of a door.
        /// </parameter>
        /// <returns>
        /// The handle created.
        /// </returns>
        public static IFCAnyHandle CreateDoorLiningProperties(ExporterIFC exporterIFC, Element familyInstance)
        {
            IFCFile file = exporterIFC.GetFile();
            IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();
            IFCLabel descriptionOpt = IFCLabel.Create();

            IFCMeasureValue liningDepthOpt = IFCMeasureValue.Create();
            IFCMeasureValue liningThicknessOpt = IFCMeasureValue.Create();
            IFCMeasureValue thresholdDepthOpt = IFCMeasureValue.Create();
            IFCMeasureValue thresholdThicknessOpt = IFCMeasureValue.Create();
            IFCMeasureValue transomThicknessOpt = IFCMeasureValue.Create();
            IFCMeasureValue transomOffsetOpt = IFCMeasureValue.Create();
            IFCMeasureValue liningOffsetOpt = IFCMeasureValue.Create();
            IFCMeasureValue thresholdOffsetOpt = IFCMeasureValue.Create();
            IFCMeasureValue casingThicknessOpt = IFCMeasureValue.Create();
            IFCMeasureValue casingDepthOpt = IFCMeasureValue.Create();
            IFCAnyHandle shapeAspectStyleOpt = IFCAnyHandle.Create();

            double value1, value2;

            // both of these must be defined, or not defined - if only one is defined, we ignore the values.
            if (ParameterUtil.GetPositiveDoubleValueFromElementOrSymbol(familyInstance, "LiningDepth", out value1))
            {
                if (ParameterUtil.GetPositiveDoubleValueFromElementOrSymbol(familyInstance, "LiningThickness", out value2))
                {
                    liningDepthOpt = IFCMeasureValue.Create(value1);
                    liningThicknessOpt = IFCMeasureValue.Create(value2);
                }
            }

            if (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "LiningOffset", out value1))
                liningOffsetOpt = IFCMeasureValue.Create(value1);

            // both of these must be defined, or not defined - if only one is defined, we ignore the values.
            if (ParameterUtil.GetPositiveDoubleValueFromElementOrSymbol(familyInstance, "ThresholdDepth", out value1))
            {
                if (ParameterUtil.GetPositiveDoubleValueFromElementOrSymbol(familyInstance, "ThresholdThickness", out value2))
                {
                    thresholdDepthOpt = IFCMeasureValue.Create(value1);
                    thresholdThicknessOpt = IFCMeasureValue.Create(value2);
                }
            }

            if (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "ThreshholdOffset", out value1))
                liningOffsetOpt = IFCMeasureValue.Create(value1);

            // both of these must be defined, or not defined - if only one is defined, we ignore the values.
            if (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "TransomOffset", out value1))
            {
                if (ParameterUtil.GetPositiveDoubleValueFromElementOrSymbol(familyInstance, "TransomThickness", out value2))
                {
                    transomOffsetOpt = IFCMeasureValue.Create(value1);
                    transomThicknessOpt = IFCMeasureValue.Create(value2);
                }
            }

            // both of these must be defined, or not defined - if only one is defined, we ignore the values.
            if (ParameterUtil.GetPositiveDoubleValueFromElementOrSymbol(familyInstance, "CasingDepth", out value1))
            {
                if (ParameterUtil.GetPositiveDoubleValueFromElementOrSymbol(familyInstance, "CasingThickness", out value2))
                {
                    casingDepthOpt = IFCMeasureValue.Create(value1);
                    casingThicknessOpt = IFCMeasureValue.Create(value2);
                }
            }

            IFCLabel doorLiningGUID = IFCLabel.CreateGUID();
            IFCLabel doorLiningName = NamingUtil.CreateIFCName(exporterIFC, -1);
            return file.CreateDoorLiningProperties(doorLiningGUID, ownerHistory,
               doorLiningName, descriptionOpt, liningDepthOpt, liningThicknessOpt, thresholdDepthOpt, thresholdThicknessOpt,
               transomThicknessOpt, transomOffsetOpt, liningOffsetOpt, thresholdOffsetOpt, casingThicknessOpt,
               casingDepthOpt, shapeAspectStyleOpt);
        }

        /// <summary>
        /// Gets door panel position.
        /// </summary>
        /// <parameter name="typeName">
        /// The type name of the door.
        /// </parameter>
        /// <parameter name="element">
        /// The door element.
        /// </parameter>
        /// <parameter name="number">
        /// The number of panel position.
        /// </parameter>
        /// <returns>
        /// The string represents the door panel position.
        /// </returns>
        public static string GetIFCDoorPanelPosition(string typeName, Element element, int number)
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
                return "";
            else if (String.Compare(value, "left", true) == 0)
                return "left";
            else if (String.Compare(value, "middle", true) == 0)
                return "middle";
            else if (String.Compare(value, "right", true) == 0)
                return "right";
            else
                return "not defined";
        }

        /// <summary>
        /// Reverses door panel position.
        /// </summary>
        /// <parameter name="originalPosition">
        /// The original position.
        /// </parameter>
        /// <returns>
        /// The string represents the reversed door panel position.
        /// </returns>
        public static string ReverseDoorPanelPosition(string originalPosition)
        {
            if (String.Compare(originalPosition, "left", true) == 0)
                return "right";
            else if (String.Compare(originalPosition, "right", true) == 0)
                return "left";
            return originalPosition;
        }

        /// <summary>
        /// Gets window style operation.
        /// </summary>
        /// <parameter name="familySymbol">
        /// The element type of window.
        /// </parameter>
        /// <returns>
        /// The IFCWindowStyleOperation.
        /// </returns>
        public static IFCWindowStyleOperation GetIFCWindowStyleOperation(ElementType familySymbol)
        {
            Parameter parameter = familySymbol.get_Parameter(BuiltInParameter.WINDOW_OPERATION_TYPE);
            string value = parameter.AsValueString();

            if (String.IsNullOrEmpty(value))
                return IFCWindowStyleOperation.NotDefined;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "UserDefined"))
                return IFCWindowStyleOperation.UserDefined;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "SinglePanel"))
                return IFCWindowStyleOperation.SinglePanel;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "DoublePanelVertical"))
                return IFCWindowStyleOperation.DoublePanelVertical;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "DoublePanelHorizontal"))
                return IFCWindowStyleOperation.DoublePanelHorizontal;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "TriplePanelVertical"))
                return IFCWindowStyleOperation.TriplePanelVertical;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "TriplePanelBottom"))
                return IFCWindowStyleOperation.TriplePanelBottom;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "TriplePanelTop"))
                return IFCWindowStyleOperation.TriplePanelTop;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "TriplePanelLeft"))
                return IFCWindowStyleOperation.TriplePanelLeft;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "TriplePanelRight"))
                return IFCWindowStyleOperation.TriplePanelRight;
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "TriplePanelHorizontal"))
                return IFCWindowStyleOperation.TriplePanelHorizontal;

            return IFCWindowStyleOperation.NotDefined;
        }

        /// <summary>
        /// Gets window style construction.
        /// </summary>
        /// <parameter name="element">
        /// The window element.
        /// </parameter>
        /// <parameter name="initialValue">
        /// The initial value.
        /// </parameter>
        /// <returns>
        /// The string represents the window style construction.
        /// </returns>
        public static string GetIFCWindowStyleConstruction(Element element, string initialValue)
        {
            string value;
            if (!ParameterUtil.GetStringValueFromElementOrSymbol(element, "Construction", out value))
                value = initialValue;

            if (value == "")
                return "NotDefined";
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "Aluminum"))
                return "Aluminum";
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "HighGradeSteel"))
                return "HighGradeSteel";
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "Steel"))
                return "Steel";
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "Wood"))
                return "Wood";
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "AluminumWood"))
                return "AluminumWood";
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "Plastic"))
                return "Plastic";
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "OtherConstruction"))
                return "OtherConstruction";

            return "NotDefined";
        }

        /// <summary>
        /// Gets window panel operation.
        /// </summary>
        /// <parameter name="initialValue">
        /// The initial value.
        /// </parameter>
        /// <parameter name="element">
        /// The window element.
        /// </parameter>
        /// <parameter name="number">
        /// The number of panel operation.
        /// </parameter>
        /// <returns>
        /// The string represents the window panel operation.
        /// </returns>
        public static string GetIFCWindowPanelOperation(string initialValue, Element element, int number)
        {
            string currPanelName = "PanelOperation" + number.ToString();

            string value;
            if (!ParameterUtil.GetStringValueFromElementOrSymbol(element, currPanelName, out value))
                value = initialValue;

            if (value == "")
                return "NotDefined";
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "SideHungRightHand"))
                return "SideHungRightHand";
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "SideHungLeftHand"))
                return "SideHungLeftHand";
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "TiltAndTurnRightHand"))
                return "TiltAndTurnRightHand";
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "TiltAndTurnLeftHand"))
                return "TiltAndTurnLeftHand";
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "TopHung"))
                return "TopHung";
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "BottomHung"))
                return "BottomHung";
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "PivotHorizontal"))
                return "PivotHorizontal";
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "PivotVertical"))
                return "PivotVertical";
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "SlidingHorizontal"))
                return "SlidingHorizontal";
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "SlidingVertical"))
                return "SlidingVertical";
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "RemovableCasement"))
                return "RemovableCasement";
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "FixedCasement"))
                return "FixedCasement";
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "OtherOperation"))
                return "OtherOperation";

            return "NotDefined";
        }

        /// <summary>
        /// Gets window panel position.
        /// </summary>
        /// <parameter name="initialValue">
        /// The initial value.
        /// </parameter>
        /// <parameter name="element">
        /// The window element.
        /// </parameter>
        /// <parameter name="number">
        /// The number of panel position.
        /// </parameter>
        /// <returns>
        /// The string represents the window panel position.
        /// </returns>
        public static string GetIFCWindowPanelPosition(string initialValue, Element element, int number)
        {
            string currPanelName = "PanelPosition" + number.ToString();

            string value;
            if (!ParameterUtil.GetStringValueFromElementOrSymbol(element, currPanelName, out value))
                value = initialValue;

            if (value == "")
                return "NotDefined";
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "Left"))
                return "Left";
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "Middle"))
                return "Middle";
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "Right"))
                return "Right";
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "Bottom"))
                return "Bottom";
            else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "Top"))
                return "Top";

            return "NotDefined";
        }

        /// <summary>
        /// Creates window panel position.
        /// </summary>
        /// <parameter name="exporterIFC">
        /// The ExporterIFC object.
        /// </parameter>
        /// <parameter name="familyInstance">
        /// The family instance of a window.
        /// </parameter>
        /// <parameter name="description">
        /// The description.
        /// </parameter>
        /// <returns>
        /// The handle created.
        /// </returns>
        public static IFCAnyHandle CreateWindowLiningProperties(ExporterIFC exporterIFC,
           Element familyInstance, IFCLabel description)
        {
            IFCFile file = exporterIFC.GetFile();
            IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();

            IFCMeasureValue liningDepthOpt = IFCMeasureValue.Create();
            IFCMeasureValue liningThicknessOpt = IFCMeasureValue.Create();
            IFCMeasureValue transomThicknessOpt = IFCMeasureValue.Create();
            IFCMeasureValue mullionThicknessOpt = IFCMeasureValue.Create();
            IFCMeasureValue firstTransomOffsetOpt = IFCMeasureValue.Create();
            IFCMeasureValue secondTransomOffsetOpt = IFCMeasureValue.Create();
            IFCMeasureValue firstMullionOffsetOpt = IFCMeasureValue.Create();
            IFCMeasureValue secondMullionOffsetOpt = IFCMeasureValue.Create();
            IFCAnyHandle shapeAspectStyleOpt = IFCAnyHandle.Create();

            double value1 = 0.0;
            double value2 = 0.0;

            // both of these must be defined (or not defined)
            if (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "LiningDepth", out value1) &&
               ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "LiningThickness", out value2))
            {
                liningDepthOpt = IFCMeasureValue.Create(value1);
                liningThicknessOpt = IFCMeasureValue.Create(value2);
            }

            if (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "TransomThickness", out value1))
                transomThicknessOpt = IFCMeasureValue.Create(value1);

            if (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "FirstTransomOffset", out value1))
                firstTransomOffsetOpt = IFCMeasureValue.Create(value1);

            if (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "SecondTransomOffset", out value1))
                secondTransomOffsetOpt = IFCMeasureValue.Create(value1);

            if (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "MullionThickness", out value1))
                mullionThicknessOpt = IFCMeasureValue.Create(value1);

            if (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "FirstMullionOffset", out value1))
                firstMullionOffsetOpt = IFCMeasureValue.Create(value1);

            if (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "SecondMullionOffset", out value1))
                secondMullionOffsetOpt = IFCMeasureValue.Create(value1);

            IFCLabel windowLiningGUID = IFCLabel.CreateGUID();
            IFCLabel windowLiningName = NamingUtil.CreateIFCName(exporterIFC, -1);
            return file.CreateWindowLiningProperties(windowLiningGUID, ownerHistory,
               windowLiningName, description, liningDepthOpt, liningThicknessOpt, transomThicknessOpt, mullionThicknessOpt,
               firstTransomOffsetOpt, secondTransomOffsetOpt, firstMullionOffsetOpt, secondMullionOffsetOpt,
               shapeAspectStyleOpt);
        }

        /// <summary>
        /// Creates window panel properties.
        /// </summary>
        /// <parameter name="exporterIFC">
        /// The ExporterIFC object.
        /// </parameter>
        /// <parameter name="doorWindowInfo">
        /// The IFCDoorWindowInfo object.
        /// </parameter>
        /// <parameter name="familyInstance">
        /// The family instance of a window.
        /// </parameter>
        /// <parameter name="description">
        /// The description.
        /// </parameter>
        /// <returns>
        /// The list of handles created.
        /// </returns>
        public static IList<IFCAnyHandle> CreateWindowPanelProperties(ExporterIFC exporterIFC,
           Element familyInstance, IFCLabel description)
        {
            IList<IFCAnyHandle> panels = new List<IFCAnyHandle>();
            IFCFile file = exporterIFC.GetFile();
            IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();

            const int maxPanels = 1000;  // arbitrary large number to prevent infinite loops.
            for (int panelNum = 1; panelNum < maxPanels; panelNum++)
            {
                string frameDepthCurrString = "FrameDepth" + panelNum.ToString();
                string frameThicknessCurrString = "FrameThickness" + panelNum.ToString();

                string panelOperation = GetIFCWindowPanelOperation("", familyInstance, panelNum);
                string panelPosition = GetIFCWindowPanelPosition("", familyInstance, panelNum);
                if (panelOperation == "NotDefined" && panelPosition == "NotDefined")
                    break;

                IFCMeasureValue frameDepthOpt = IFCMeasureValue.Create();
                IFCMeasureValue frameThicknessOpt = IFCMeasureValue.Create();
                IFCAnyHandle shapeAspectStyleOpt = IFCAnyHandle.Create();

                double value1, value2;
                if ((ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, frameDepthCurrString, out value1) ||
                    ((panelNum == 1) && (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "FrameDepth", out value1)))) &&
                   (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, frameThicknessCurrString, out value2) ||
                    ((panelNum == 1) && (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "FrameThickness", out value2)))))
                {
                    frameDepthOpt = IFCMeasureValue.Create(value1);
                    frameThicknessOpt = IFCMeasureValue.Create(value2);
                }

                IFCLabel panelGUID = IFCLabel.CreateGUID();
                IFCLabel panelName = NamingUtil.CreateIFCName(exporterIFC, panelNum);
                panels.Add(file.CreateWindowPanelProperties(panelGUID, ownerHistory,
                   panelName, description, panelOperation, panelPosition, frameDepthOpt, frameThicknessOpt,
                   shapeAspectStyleOpt));
            }
            return panels;
        }
    }
}
