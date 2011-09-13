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
    /// Provides static methods for naming and string related operations.
    /// </summary>
    class NamingUtil
    {
        /// <summary>
        /// Removes spaces in a string.
        /// </summary>
        /// <param name="originalString">
        /// The original string.
        /// </param>
        /// <param name="newString">
        /// The output string.
        /// </param>
        public static void RemoveSpaces(string originalString, out string newString)
        {
            newString = string.Empty;
            string[] subSections = originalString.Split(' ');
            int size = subSections.Length;
            for (int ii = 0; ii < size; ii++)
                newString += subSections[ii];
            return;
        }

        /// <summary>
        /// Removes underscores in a string.
        /// </summary>
        /// <param name="originalString">
        /// The original string.
        /// </param>
        /// <param name="newString">
        /// The output string.
        /// </param>
        public static void RemoveUnderscores(string originalString, out string newString)
        {
            newString = string.Empty;
            string[] subSections = originalString.Split('_');
            int size = subSections.Length;
            for (int ii = 0; ii < size; ii++)
                newString += subSections[ii];
            return;
        }

        /// <summary>
        /// Checks if two strings are equal ignoring case and spaces.
        /// </summary>
        /// <param name="str1">
        /// The string to be compared.
        /// </param>
        /// <param name="str2">
        /// The other string to be compared.
        /// </param>
        /// <returns>
        /// True if they are equal, false otherwise.
        /// </returns>
        public static bool IsEqualIgnoringCaseAndSpaces(string str1, string str2)
        {
            string nospace1 = string.Empty;
            string nospace2 = string.Empty;
            RemoveSpaces(str1, out nospace1);
            RemoveSpaces(str2, out nospace2);
            return (string.Compare(nospace1, nospace2, true) == 0);
        }

        /// <summary>
        /// Checks if two strings are equal ignoring case, spaces and underscores.
        /// </summary>
        /// <param name="str1">
        /// The string to be compared.
        /// </param>
        /// <param name="str2">
        /// The other string to be compared.
        /// </param>
        /// <returns>
        /// True if they are equal, false otherwise.
        /// </returns>
        public static bool IsEqualIgnoringCaseSpacesAndUnderscores(string str1, string str2)
        {
            string nospace1 = string.Empty;
            string nospace2 = string.Empty;
            RemoveSpaces(str1, out nospace1);
            RemoveSpaces(str2, out nospace2);
            string nospaceOrUndescore1 = string.Empty;
            string nospaceOrUndescore2 = string.Empty;
            RemoveUnderscores(nospace1, out nospaceOrUndescore1);
            RemoveUnderscores(nospace2, out nospaceOrUndescore2);
            return (string.Compare(nospaceOrUndescore1, nospaceOrUndescore2, true) == 0);
        }

        /// <summary>
        /// Gets override string value from element parameter.
        /// </summary>
        /// <param name="element">
        /// The element.
        /// </param>
        /// <param name="parameterName">
        /// The parameter name.
        /// </param>
        /// <param name="originalValue">
        /// The original value.
        /// </param>
        /// <returns>
        /// The IFCLabel contains the string value.
        /// </returns>
        public static IFCLabel GetOverrideStringValue(Element element, string parameterName, IFCLabel originalValue)
        {
            string strValue;

            // get the IFC Name Override 
            if (element != null)
            {
                if (ParameterUtil.GetStringValueFromElement(element, parameterName, out strValue))
                {
                    return IFCLabel.Create(strValue);
                }
            }

            return originalValue;
        }

        /// <summary>
        /// Gets override name from element.
        /// </summary>
        /// <param name="element">
        /// The element.
        /// </param>
        /// <param name="originalValue">
        /// The original value.
        /// </param>
        /// <returns>
        /// The IFCLabel contains the name string value.
        /// </returns>
        public static IFCLabel GetNameOverride(Element element, IFCLabel originalValue)
        {
            string nameOverride = "NameOverride";
            return GetOverrideStringValue(element, nameOverride, originalValue);
        }

        /// <summary>
        /// Gets override description from element.
        /// </summary>
        /// <param name="element">
        /// The element.
        /// </param>
        /// <param name="originalValue">
        /// The original value.
        /// </param>
        /// <returns>
        /// The IFCLabel contains the description string value.
        /// </returns>
        public static IFCLabel GetDescriptionOverride(Element element, IFCLabel originalValue)
        {
            string nameOverride = "IfcDescription";
            return GetOverrideStringValue(element, nameOverride, originalValue);
        }

        /// <summary>
        /// Gets override object type from element.
        /// </summary>
        /// <param name="element">
        /// The element.
        /// </param>
        /// <param name="originalValue">
        /// The original value.
        /// </param>
        /// <returns>
        /// The IFCLabel contains the object type string value.
        /// </returns>
        public static IFCLabel GetObjectTypeOverride(Element element, IFCLabel originalValue)
        {
            string nameOverride = "ObjectTypeOverride";
            return GetOverrideStringValue(element, nameOverride, originalValue);
        }

        /// <summary>
        /// Creates an IFC name from export state.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="index">
        /// The index of the name. If it is larger than 0, it is appended to the name.
        /// </param>
        /// <returns>
        /// The IFCLabel contains the name string value.
        /// </returns>
        public static IFCLabel CreateIFCName(ExporterIFC exporterIFC, int index)
        {
            IFCLabel origName = exporterIFC.GetNameFromExportState();
            string elemName = origName.String;
            if (index >= 0)
            {
                elemName += ":";
                elemName += index.ToString();
            }

            return IFCLabel.Create(elemName);
        }

        /// <summary>
        /// Creates an IFC object name from export state.
        /// </summary>
        /// <remarks>
        /// It is combined with family name and element type id.
        /// </remarks>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="element">
        /// The element.
        /// </param>
        /// <returns>
        /// The IFCLabel contains the name string value.
        /// </returns>
        public static IFCLabel CreateIFCObjectName(ExporterIFC exporterIFC, Element element)
        {
            ElementId typeId = element.GetTypeId();

            string objectName = "";// exporterIFC.GetFamilyName();
            if (typeId != ElementId.InvalidElementId)
            {
                if (objectName == "")
                    return IFCLabel.Create(typeId.ToString());
                else
                    return IFCLabel.Create(objectName + ":" + typeId.ToString());
            }
            return IFCLabel.Create("");
        }

        /// <summary>
        /// Creates an IFC element id string from element id.
        /// </summary>
        /// <param name="element">
        /// The element.
        /// </param>
        /// <returns>
        /// The IFCLabel contains the name string value.
        /// </returns>
        public static IFCLabel CreateIFCElementId(Element element)
        {
            if (element == null)
                return IFCLabel.Create("NULL");

            string elemIdString = element.Id.ToString();
            return IFCLabel.Create(elemIdString);
        }
    }
}
