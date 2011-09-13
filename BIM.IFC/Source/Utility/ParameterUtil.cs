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
    /// Provides static methods for parameter related manipulations.
    /// </summary>
    class ParameterUtil
    {
        /// <summary>
        /// Gets string value from parameter of an element.
        /// </summary>
        /// <param name="element">
        /// The element.
        /// </param>
        /// <param name="propertyName">
        /// The property name.
        /// </param>
        /// <param name="propertyValue">
        /// The output property value.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown when element is null.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// Thrown when propertyName is null or empty.
        /// </exception>
        /// <returns>
        /// True if get the value successfully, false otherwise.
        /// </returns>
        public static bool GetStringValueFromElement(Element element, string propertyName, out string propertyValue)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            if (String.IsNullOrEmpty(propertyName))
                throw new ArgumentException("It is null or empty.", "propertyName");

            propertyValue = string.Empty;

            Parameter param = getParameterFromName(element, propertyName);

            if (param != null && param.HasValue && param.StorageType == StorageType.String)
            {
                if (param.AsString() != null)
                {
                    propertyValue = param.AsString();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets integer value from parameter of an element.
        /// </summary>
        /// <param name="element">
        /// The element.
        /// </param>
        /// <param name="propertyName">
        /// The property name.
        /// </param>
        /// <param name="propertyValue">
        /// The output property value.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown when element is null.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// Thrown when propertyName is null or empty.
        /// </exception>
        /// <returns>
        /// True if get the value successfully, false otherwise.
        /// </returns>
        public static bool GetIntValueFromElement(Element element, string propertyName, out int propertyValue)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            if (String.IsNullOrEmpty(propertyName))
                throw new ArgumentException("It is null or empty.", "propertyName");

            propertyValue = 0;

            Parameter param = getParameterFromName(element, propertyName);

            if (param != null && param.HasValue && param.StorageType == StorageType.Integer)
            {
                propertyValue = param.AsInteger();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets double value from parameter of an element.
        /// </summary>
        /// <parameter name="element">
        /// The element.
        /// </parameter>
        /// <parameter name="propertyName">
        /// The property name.
        /// </parameter>
        /// <parameter name="propertyValue">
        /// The output property value.
        /// </parameter>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown when element is null.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// Thrown when propertyName is null or empty.
        /// </exception>
        /// <returns>
        /// True if get the value successfully, false otherwise.
        /// </returns>
        public static bool GetDoubleValueFromElement(Element element, string propertyName, out double propertyValue)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            if (String.IsNullOrEmpty(propertyName))
                throw new ArgumentException("It is null or empty.", "propertyName");

            propertyValue = 0.0;

            Parameter parameter = getParameterFromName(element, propertyName);

            if (parameter != null && parameter.HasValue && parameter.StorageType == StorageType.Double)
            {
                propertyValue = parameter.AsDouble();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets double value from parameter of an element or its element type.
        /// </summary>
        /// <parameter name="element">
        /// The element.
        /// </parameter>
        /// <parameter name="propertyName">
        /// The property name.
        /// </parameter>
        /// <parameter name="propertyValue">
        /// The output property value.
        /// </parameter>
        /// <returns>
        /// True if get the value successfully, false otherwise.
        /// </returns>
        public static bool GetDoubleValueFromElementOrSymbol(Element element, string propertyName, out double propertyValue)
        {
            if (GetDoubleValueFromElement(element, propertyName, out propertyValue))
                return true;
            else
            {
                Document document = element.Document;
                ElementId typeId = element.GetTypeId();

                Element elemType = document.get_Element(typeId);
                if (elemType != null)
                {
                    return GetDoubleValueFromElement(elemType, propertyName, out propertyValue);
                }
                else
                    return false;
            }
        }

        /// <summary>
        /// Gets positive double value from parameter of an element or its element type.
        /// </summary>
        /// <parameter name="element">
        /// The element.
        /// </parameter>
        /// <parameter name="propertyName">
        /// The property name.
        /// </parameter>
        /// <parameter name="propertyValue">
        /// The output property value.
        /// </parameter>
        /// <returns>
        /// True if get the value successfully and the value is positive, false otherwise.
        /// </returns>
        public static bool GetPositiveDoubleValueFromElementOrSymbol(Element element, string propertyName, out double propertyValue)
        {
            bool found = GetDoubleValueFromElementOrSymbol(element, propertyName, out propertyValue);
            if (found && (propertyValue > 0.0))
                return true;
            return false;
        }

        /// <summary>
        /// Gets the parameter by name from an element.
        /// </summary>
        /// <parameter name="element">
        /// The element.
        /// </parameter>
        /// <parameter name="propertyName">
        /// The property name.
        /// </parameter>
        /// <returns>
        /// The Parameter.
        /// </returns>
        static Parameter getParameterFromName(Element element, string propertyName)
        {
            ParameterSet parameterIds = element.Parameters;
            if (parameterIds.Size == 0)
                return null;

            // We will do two passes.  In the first pass, we will look at parameters in the IFC group.
            // In the second pass, we will look at all other groups.
            int pass = 0;
            for (; pass < 2; pass++)
            {
                bool lookAtIFCParameters = (pass == 0);
                ParameterSetIterator parameterIt = parameterIds.ForwardIterator();

                while (parameterIt.MoveNext())
                {
                    Parameter parameter = parameterIt.Current as Parameter;

                    Definition paramDef = parameter.Definition;
                    if (lookAtIFCParameters ^ (paramDef.ParameterGroup == BuiltInParameterGroup.PG_IFC))
                        continue;

                    if (NamingUtil.IsEqualIgnoringCaseAndSpaces(paramDef.Name, propertyName))
                        return parameter;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets string value from parameter of an element or its element type.
        /// </summary>
        /// <parameter name="element">
        /// The element.
        /// </parameter>
        /// <parameter name="propertyName">
        /// The property name.
        /// </parameter>
        /// <parameter name="propertyValue">
        /// The output property value.
        /// </parameter>
        /// <returns>
        /// True if get the value successfully, false otherwise.
        /// </returns>
        public static bool GetStringValueFromElementOrSymbol(Element element, string propertyName, out string propertyValue)
        {
            if (GetStringValueFromElement(element, propertyName, out propertyValue))
                return true;
            else
            {
                Document document = element.Document;
                ElementId typeId = element.GetTypeId();

                Element elemType = document.get_Element(typeId);
                if (elemType != null)
                {
                    return GetStringValueFromElement(elemType, propertyName, out propertyValue);
                }
                else
                    return false;
            }
        }
    }
}
