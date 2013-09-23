﻿//
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
using System.Text;
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;


namespace BIM.IFC.Utility
{
    /// <summary>
    /// Provides static methods for naming and string related operations.
    /// </summary>
    public class NamingUtil
    {
        /// <summary>
        /// Removes spaces in a string.
        /// </summary>
        /// <param name="originalString">The original string.</param>
        /// <returns>The string without spaces.</returns>
        public static string RemoveSpaces(string originalString)
        {
            return originalString.Replace(" ", null);
        }

        /// <summary>
        /// Removes underscores in a string.
        /// </summary>
        /// <param name="originalString">The original string.</param>
        /// <returns>The string without underscores.</returns>
        public static string RemoveUnderscores(string originalString)
        {
            return originalString.Replace("_", null);
        }

        /// <summary>
        /// Removes spaces and underscores in a string.
        /// </summary>
        /// <param name="originalString">The original string.</param>
        /// <returns>The string without spaces or underscores.</returns>
        public static string RemoveSpacesAndUnderscores(string originalString)
        {
            return originalString.Replace(" ", null).Replace("_", null);
        }

        /// <summary>
        /// Checks if two strings are equal ignoring case and spaces.
        /// </summary>
        /// <param name="string1">
        /// The string to be compared.
        /// </param>
        /// <param name="string2">
        /// The other string to be compared.
        /// </param>
        /// <returns>
        /// True if they are equal, false otherwise.
        /// </returns>
        public static bool IsEqualIgnoringCaseAndSpaces(string string1, string string2)
        {
            string nospace1 = RemoveSpaces(string1);
            string nospace2 = RemoveSpaces(string2);
            return (string.Compare(nospace1, nospace2, true) == 0);
        }

        /// <summary>
        /// Checks if two strings are equal ignoring case, spaces and underscores.
        /// </summary>
        /// <param name="string1">
        /// The string to be compared.
        /// </param>
        /// <param name="string2">
        /// The other string to be compared.
        /// </param>
        /// <returns>
        /// True if they are equal, false otherwise.
        /// </returns>
        public static bool IsEqualIgnoringCaseSpacesAndUnderscores(string string1, string string2)
        {
            string nospaceOrUndescore1 = RemoveUnderscores(RemoveSpaces(string1));
            string nospaceOrUndescore2 = RemoveUnderscores(RemoveSpaces(string2));
            return (string.Compare(nospaceOrUndescore1, nospaceOrUndescore2, true) == 0);
        }

        /// <summary>
        /// Gets override string value from element parameter.
        /// </summary>
        /// <param name="element">
        /// The element.
        /// </param>
        /// <param name="paramName">
        /// The parameter name.
        /// </param>
        /// <param name="originalValue">
        /// The original value.
        /// </param>
        /// <returns>
        /// The string contains the string value.
        /// </returns>
        public static string GetOverrideStringValue(Element element, string paramName, string originalValue)
        {
            string strValue;

            // get the IFC Name Override 
            if (element != null)
            {
                if (ParameterUtil.GetStringValueFromElement(element.Id, paramName, out strValue) != null)
                {
                    if (!String.IsNullOrWhiteSpace(strValue))
                        return strValue;
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
        /// The string contains the name string value.
        /// </returns>
        public static string GetNameOverride(Element element, string originalValue)
        {
            string nameOverride = "NameOverride";
            string overrideValue = GetOverrideStringValue(element, nameOverride, originalValue);
            if ((String.Compare(originalValue, overrideValue) == 0) || overrideValue == null)
            {
                //if NameOverride is not used or does not exist, test for the actual IFC attribute name: Name (using parameter name: IfcName)
                nameOverride = "IfcName";
                overrideValue = GetOverrideStringValue(element, nameOverride, originalValue);
            }
            //GetOverrideStringValue will return the override value from the parameter specified, otherwise it will return the originalValue
            return overrideValue;
        }

        /// <summary>
        /// Gets override long name from element.
        /// </summary>
        /// <param name="element">
        /// The element.
        /// </param>
        /// <param name="originalValue">
        /// The original value.
        /// </param>
        /// <returns>
        /// The string contains the long name string value.
        /// </returns>
        public static string GetLongNameOverride(Element element, string originalValue)
        {
            string longNameOverride = "LongNameOverride";
            string overrideValue = GetOverrideStringValue(element, longNameOverride, originalValue);
            if ((String.Compare(originalValue, overrideValue) == 0) || overrideValue == null)
            {
                //if LongNameOverride is not used or does not exist, test for the actual IFC attribute name: LongName (using parameter name IfcLongName)
                longNameOverride = "IfcLongName";
                overrideValue = GetOverrideStringValue(element, longNameOverride, originalValue);
            }
            //GetOverrideStringValue will return the override value from the parameter specified, otherwise it will return the originalValue
            return overrideValue;
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
        /// The string contains the description string value.
        /// </returns>
        public static string GetDescriptionOverride(Element element, string originalValue)
        {
            string nameOverride = "IfcDescription";
            string overrideValue = GetOverrideStringValue(element, nameOverride, originalValue);
            //GetOverrideStringValue will return the override value from the parameter specified, otherwise it will return the originalValue
            return overrideValue;
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
        /// <returns>The string contains the object type string value.</returns>
        public static string GetObjectTypeOverride(Element element, string originalValue)
        {
            string nameOverride = "ObjectTypeOverride";
            string overrideValue = GetOverrideStringValue(element, nameOverride, originalValue);
            if ((String.Compare(originalValue, overrideValue) == 0) || overrideValue == null)
            {
                //if ObjectTypeOverride is not used or does not exist, test for the actual IFC attribute name: ObjectType (using IfcObjectType)
                nameOverride = "IfcObjectType";
                overrideValue = GetOverrideStringValue(element, nameOverride, originalValue);
            }
            //GetOverrideStringValue will return the override value from the parameter specified, otherwise it will return the originalValue
            return overrideValue;

        }

        /// <summary>
        /// Gets Tag override from element.
        /// </summary>
        /// <param name="element">
        /// The element.
        /// </param>
        /// <param name="originalValue">
        /// The original value.
        /// </param>
        /// <returns>The string contains the object type string value.</returns>
        public static string GetTagOverride(Element element, string originalValue)
        {
            string nameOverride = "IfcTag";
            return GetOverrideStringValue(element, nameOverride, originalValue);
        }
       
        /// <summary>
        /// Generates the IFC name for the current element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns> The string containing the name.</returns>
        static private string GetIFCBaseName(Element element)
        {
            if (element == null)
                return "";

            bool isType = (element is ElementType);

            string elementName = element.Name;
            if (elementName == "???")
                elementName = "";

            string familyName = "";
            ElementType elementType = (isType ? element : element.Document.GetElement(element.GetTypeId())) as ElementType;
            if (elementType != null)
            {
                familyName = ExporterIFCUtils.GetFamilyName(elementType);
                if (familyName == "???")
                    familyName = "";
            }

            string fullName = familyName;
            if (elementName != "")
            {
                if (fullName != "")
                    fullName = fullName + ":" + elementName;
                else
                    fullName = elementName;
            }

            if (isType)
                return fullName;
            if (fullName != "")
                return fullName + ":" + CreateIFCElementId(element);
            return CreateIFCElementId(element);
        }

        /// <summary>
        /// Generates the IFC name based on the Revit display name.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns> The string containing the name.</returns>
        static private string GetRevitDisplayName(Element element)
        {
            if (element == null)
                return "";

            string fullName = (element.Category != null) ? element.Category.Name : "";
            string typeName = element.Name;
            string familyName = "";

            ElementType elementType = null;
            if (element is ElementType)
                elementType = element as ElementType;
            else
                elementType = element.Document.GetElement(element.GetTypeId()) as ElementType;

            if (elementType != null)
                familyName = ExporterIFCUtils.GetFamilyName(elementType);

            if (familyName != "")
            {
                if (fullName != "")
                    fullName = fullName + " : " + familyName;
                else
                    fullName = familyName;
            }

            if (typeName != "")
            {
                if (fullName != "")
                    fullName = fullName + " : " + typeName;
                else
                    fullName = typeName;
            }

            return fullName;
        }

        /// <summary>
        /// Get the IFC name of an element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>The name.</returns>
        static public string GetIFCName(Element element)
        {
            if (element == null)
                return "";

            if (ExporterCacheManager.ExportOptionsCache.NamingOptions.UseVisibleRevitNameAsEntityName)
                return GetRevitDisplayName(element);
            
            string baseName = GetIFCBaseName(element);
            return GetNameOverride(element, baseName);
        }

        /// <summary>
        /// Creates an IFC name for an element, with a suffix.
        /// </summary>
        /// <param name="element">The element./// </param>
        /// <param name="index">/// The index of the name. If it is larger than 0, it is appended to the name./// </param>
        /// <returns>/// The string contains the name string value./// </returns>
        public static string GetIFCNamePlusIndex(Element element, int index)
        {
            string elementName = GetIFCName(element);
            if (index >= 0)
            {
                elementName += ":";
                elementName += index.ToString();
            }

            return elementName;
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
        /// The string contains the name string value.
        /// </returns>
        public static string CreateIFCObjectName(ExporterIFC exporterIFC, Element element)
        {
            ElementId typeId = element != null ? element.GetTypeId() : ElementId.InvalidElementId;

            string objectName = exporterIFC.GetFamilyName();
            if (typeId != ElementId.InvalidElementId)
            {
                if (objectName == "")
                    return typeId.ToString();
                else
                    return (objectName + ":" + typeId.ToString());
            }
            return "";
        }

        /// <summary>
        /// Creates an IFC element id string from element id.
        /// </summary>
        /// <param name="element">
        /// The element.
        /// </param>
        /// <returns>
        /// The string contains the name string value.
        /// </returns>
        public static string CreateIFCElementId(Element element)
        {
            if (element == null)
                return "NULL";

            string elemIdString = element.Id.ToString();
            return elemIdString;
        }

        /// <summary>
        /// Parses the name string and gets the parts separately.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="lastName">
        /// The output last name.
        /// </param>
        /// <param name="firstName">
        /// The output first name.
        /// </param>
        /// <param name="middleNames">
        /// The output middle names.
        /// </param>
        /// <param name="prefixTitles">
        /// The output prefix titles.
        /// </param>
        /// <param name="suffixTitles">
        /// The output suffix titles.
        /// </param>
        public static void ParseName(string name, out string lastName, out string firstName, out List<string> middleNames, out List<string> prefixTitles, out List<string> suffixTitles)
        {
            lastName = string.Empty;
            firstName = string.Empty;
            middleNames = null;
            prefixTitles = null;
            suffixTitles = null;

            if (String.IsNullOrEmpty(name))
                return;

            string currName = name;
            List<string> names = new List<string>();
            int noEndlessLoop = 0;
            int index = 0;
            bool foundComma = false;

            do
            {
                int currIndex = index;   // index might get reset by comma.

                currName = currName.TrimStart(' ');
                if (String.IsNullOrEmpty(currName))
                    break;

                int comma = foundComma ? currName.Length : currName.IndexOf(',');
                if (comma == -1) comma = currName.Length;
                int space = currName.IndexOf(' ');
                if (space == -1) space = currName.Length;

                // treat comma as space, mark found.
                if (comma < space)
                {
                    foundComma = true;
                    index = -1; // start inserting at the beginning again.
                    space = comma;
                }

                if (space == currName.Length)
                {
                    names.Add(currName);
                    break;
                }
                else if (space == 0)
                {
                    if (comma == 0)
                        continue;
                    else
                        break;   // shouldn't happen
                }

                names.Insert(currIndex, currName.Substring(0, space));
                index++;
                currName = currName.Substring(space + 1);
                noEndlessLoop++;

            } while (noEndlessLoop < 100);


            // parse names.
            // assuming anything ending in a dot is a prefix.
            int sz = names.Count;
            for (index = 0; index < sz; index++)
            {
                if (names[index].LastIndexOf('.') == names[index].Length - 1)
                {
                    if (prefixTitles == null)
                        prefixTitles = new List<string>();
                    prefixTitles.Add(names[index]);
                }
                else
                    break;
            }

            if (index < sz)
            {
                firstName = names[index++];
            }

            // suffixes, if any.  Note this misses "III", "IV", etc., but this is not that important!
            int lastIndex;
            for (lastIndex = sz - 1; lastIndex >= index; lastIndex--)
            {
                if (names[lastIndex].LastIndexOf('.') == names[lastIndex].Length - 1)
                {
                    if (suffixTitles == null)
                        suffixTitles = new List<string>();
                    suffixTitles.Insert(0, names[lastIndex]);
                }
                else
                    break;
            }

            if (lastIndex >= index)
            {
                lastName = names[lastIndex--];
            }

            // rest are middle names.
            for (; index <= lastIndex; index++)
            {
                if (middleNames == null)
                    middleNames = new List<string>();
                middleNames.Add(names[index]);
            }
        }
    }
}
