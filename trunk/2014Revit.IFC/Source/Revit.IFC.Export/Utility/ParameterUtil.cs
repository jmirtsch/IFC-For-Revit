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


namespace Revit.IFC.Export.Utility
{
    /// <summary>
    /// Provides static methods for parameter related manipulations.
    /// </summary>
    class ParameterUtil
    {
        // Cache the parameters for the current Element.
        private static IDictionary<ElementId, IDictionary<BuiltInParameterGroup, ParameterElementCache>> m_NonIFCParameters =
            new Dictionary<ElementId, IDictionary<BuiltInParameterGroup, ParameterElementCache>>();

        private static IDictionary<ElementId, ParameterElementCache> m_IFCParameters =
            new Dictionary<ElementId, ParameterElementCache>();

        public static IDictionary<BuiltInParameterGroup, ParameterElementCache> GetNonIFCParametersForElement(Element element)
        {
            if (element == null)
                return null;

            IDictionary<BuiltInParameterGroup, ParameterElementCache> nonIFCParametersForElement = null;
            if (!m_NonIFCParameters.TryGetValue(element.Id, out nonIFCParametersForElement))
            {
                CacheParametersForElement(element);
                m_NonIFCParameters.TryGetValue(element.Id, out nonIFCParametersForElement);
            }

            return nonIFCParametersForElement;
        }

        /// <summary>
        /// Clears parameter cache.
        /// </summary>
        public static void ClearParameterCache()
        {
            m_NonIFCParameters.Clear();
            m_IFCParameters.Clear();
        }

        /// <summary>
        /// Gets string value from parameter of an element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="propertyValue">The output property value.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when element is null.</exception>
        /// <exception cref="System.ArgumentException">Thrown when propertyName is null or empty.</exception>
        /// <returns>True if get the value successfully, false otherwise.</returns>
        public static bool GetStringValueFromElement(Element element, string propertyName, out string propertyValue)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            if (String.IsNullOrEmpty(propertyName))
                throw new ArgumentException("It is null or empty.", "propertyName");

            propertyValue = string.Empty;

            Parameter parameter = GetParameterFromName(element, null, propertyName);

            if (parameter != null && parameter.HasValue && parameter.StorageType == StorageType.String)
            {
                if (parameter.AsString() != null)
                {
                    propertyValue = parameter.AsString();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets integer value from parameter of an element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="propertyValue">The output property value.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when element is null.</exception>
        /// <exception cref="System.ArgumentException">Thrown when propertyName is null or empty.</exception>
        /// <returns>True if get the value successfully, false otherwise.</returns>
        public static bool GetIntValueFromElement(Element element, string propertyName, out int propertyValue)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            if (String.IsNullOrEmpty(propertyName))
                throw new ArgumentException("It is null or empty.", "propertyName");

            propertyValue = 0;

            Parameter parameter = GetParameterFromName(element, null, propertyName);

            if (parameter != null && parameter.HasValue && parameter.StorageType == StorageType.Integer)
            {
                switch (parameter.StorageType)
                {
                    case StorageType.Double:
                        {
                            try
                            {
                                propertyValue = (int)parameter.AsDouble();
                                return true;
                            }
                            catch
                            {
                                return false;
                            }
                        }
                    case StorageType.Integer:
                        propertyValue = parameter.AsInteger();
                        return true;
                    case StorageType.String:
                        {
                            try
                            {
                                propertyValue = Convert.ToInt32(parameter.AsString());
                                return true;
                            }
                            catch
                            {
                                return false;
                            }
                        }
                }
            }

            return false;
        }

        /// <summary>
        /// Gets double value from parameter of an element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="group">Optional property group to limit search to.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="propertyValue">The output property value.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when element is null.</exception>
        /// <exception cref="System.ArgumentException">Thrown when propertyName is null or empty.</exception>
        /// <returns>True if get the value successfully, false otherwise.</returns>
        public static bool GetDoubleValueFromElement(Element element, BuiltInParameterGroup? group, string propertyName, out double propertyValue)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            if (String.IsNullOrEmpty(propertyName))
                throw new ArgumentException("It is null or empty.", "propertyName");

            propertyValue = 0.0;

            Parameter parameter = GetParameterFromName(element, group, propertyName);

            if (parameter != null && parameter.HasValue)
            {
                switch (parameter.StorageType)
                {
                    case StorageType.Double:
                        propertyValue = parameter.AsDouble();
                        return true;
                    case StorageType.Integer:
                        propertyValue = parameter.AsInteger();
                        return true;
                    case StorageType.String:
                        {
                            try
                            {
                                propertyValue = Convert.ToDouble(parameter.AsString());
                                return true;
                            }
                            catch
                            {
                                return false;
                            }
                        }
                }
            }

            return false;
        }

        /// <summary>
        /// Gets string value from built-in parameter of an element.
        /// </summary>
        /// <param name="element">
        /// The element.
        /// </param>
        /// <param name="builtInParameter">
        /// The built-in parameter.
        /// </param>
        /// <param name="propertyValue">
        /// The output property value.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown when element is null.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// Thrown when builtInParameter in invalid.
        /// </exception>
        /// <returns>
        /// True if get the value successfully, false otherwise.
        /// </returns>
        public static bool GetStringValueFromElement(Element element, BuiltInParameter builtInParameter, out string propertyValue)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            if (builtInParameter == BuiltInParameter.INVALID)
                throw new ArgumentException("BuiltInParameter is INVALID", "builtInParameter");

            propertyValue = String.Empty;

            Parameter parameter = element.get_Parameter(builtInParameter);
            if (parameter != null && parameter.HasValue)
            {
                switch (parameter.StorageType)
                {
                    case StorageType.Double:
                        propertyValue = parameter.AsDouble().ToString();
                        return true;
                    case StorageType.Integer:
                        propertyValue = parameter.AsInteger().ToString();
                        return true;
                    case StorageType.String:
                        propertyValue = parameter.AsString();
                        return true;
                }
            }

            return false;
        }

        /// <summary>Gets string value from built-in parameter of an element or its type.</summary>
        /// <param name="element">The element.</param>
        /// <param name="builtInParameter">The built-in parameter.</param>
        /// <param name="nullAllowed">true if we allow the property value to be empty.</param>
        /// <param name="propertyValue">The output property value.</param>
        /// <returns>True if get the value successfully, false otherwise.</returns>
        public static bool GetStringValueFromElementOrSymbol(Element element, BuiltInParameter builtInParameter, bool nullAllowed, out string propertyValue)
        {
            if (GetStringValueFromElement(element, builtInParameter, out propertyValue))
            {
                if (!String.IsNullOrEmpty(propertyValue))
                    return true;
            }

            bool found = false;
            Element elementType = element.Document.GetElement(element.GetTypeId());
            if (elementType != null)
            {
                found = GetStringValueFromElement(elementType, builtInParameter, out propertyValue);
                if (found && !nullAllowed && String.IsNullOrEmpty(propertyValue))
                    found = false;
            }

            return found;
        }

        /// <summary>
        /// Sets string value of a built-in parameter of an element.
        /// </summary>
        /// <param name="element">
        /// The element.
        /// </param>
        /// <param name="builtInParameter">
        /// The built-in parameter.
        /// </param>
        /// <param name="propertyValue">
        /// The property value.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown when element is null.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// Thrown when builtInParameter in invalid.
        /// </exception>
        public static void SetStringParameter(Element element, BuiltInParameter builtInParameter, string propertyValue)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            if (builtInParameter == BuiltInParameter.INVALID)
                throw new ArgumentException("BuiltInParameter is INVALID", "builtInParameter");

            Parameter parameter = element.get_Parameter(builtInParameter);
            if (parameter != null && parameter.HasValue && parameter.StorageType == StorageType.String)
            {
                parameter.SetValueString(propertyValue);
                return;
            }
            else
            {
                ElementId parameterId = new ElementId(builtInParameter);
                ExporterIFCUtils.AddValueString(element, parameterId, propertyValue);
            }
        }

        /// <summary>
        /// Gets double value from built-in parameter of an element.
        /// </summary>
        /// <param name="element">
        /// The element.
        /// </param>
        /// <param name="builtInParameter">
        /// The built-in parameter.
        /// </param>
        /// <param name="propertyValue">
        /// The output property value.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown when element is null.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// Thrown when builtInParameter in invalid.
        /// </exception>
        /// <returns>
        /// True if get the value successfully, false otherwise.
        /// </returns>
        public static bool GetDoubleValueFromElement(Element element, BuiltInParameter builtInParameter, out double propertyValue)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            if (builtInParameter == BuiltInParameter.INVALID)
                throw new ArgumentException("BuiltInParameter is INVALID", "builtInParameter");

            propertyValue = 0.0;

            Parameter parameter = element.get_Parameter(builtInParameter);

            if (parameter != null && parameter.HasValue && parameter.StorageType == StorageType.Double)
            {
                propertyValue = parameter.AsDouble();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets integer value from built-in parameter of an element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="builtInParameter">The built-in parameter.</param>
        /// <param name="propertyValue">The output property value.</param>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown when element is null.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// Thrown when builtInParameter in invalid.
        /// </exception>
        /// <returns>True if get the value successfully, false otherwise.</returns>
        public static bool GetIntValueFromElement(Element element, BuiltInParameter builtInParameter, out int propertyValue)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            if (builtInParameter == BuiltInParameter.INVALID)
                throw new ArgumentException("BuiltInParameter is INVALID", "builtInParameter");

            propertyValue = 0;

            Parameter parameter = element.get_Parameter(builtInParameter);

            if (parameter != null && parameter.HasValue && parameter.StorageType == StorageType.Integer)
            {
                propertyValue = parameter.AsInteger();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets double value from built-in parameter of an element or its element type.
        /// </summary>
        /// <param name="element">
        /// The element.
        /// </param>
        /// <param name="builtInParameter">
        /// The built-in parameter.
        /// </param>
        /// <param name="propertyValue">
        /// The output property value.
        /// </param>
        /// <returns>
        /// True if get the value successfully, false otherwise.
        /// </returns>
        public static bool GetDoubleValueFromElementOrSymbol(Element element, BuiltInParameter builtInParameter, out double propertyValue)
        {
            if (GetDoubleValueFromElement(element, builtInParameter, out propertyValue))
                return true;
            else
            {
                Document document = element.Document;
                ElementId typeId = element.GetTypeId();

                Element elemType = document.GetElement(typeId);
                if (elemType != null)
                {
                    return GetDoubleValueFromElement(elemType, builtInParameter, out propertyValue);
                }
                else
                    return false;
            }
        }

        /// <summary>
        /// Gets double value from parameter of an element or its element type.
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
        /// <returns>
        /// True if get the value successfully, false otherwise.
        /// </returns>
        public static bool GetDoubleValueFromElementOrSymbol(Element element, string propertyName, out double propertyValue)
        {
            if (GetDoubleValueFromElement(element, null, propertyName, out propertyValue))
                return true;
            else
            {
                Document document = element.Document;
                ElementId typeId = element.GetTypeId();

                Element elemType = document.GetElement(typeId);
                if (elemType != null)
                {
                    return GetDoubleValueFromElement(elemType, null, propertyName, out propertyValue);
                }
                else
                    return false;
            }
        }

        /// <summary>
        /// Gets positive double value from parameter of an element or its element type.
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
        /// Gets element id value from parameter of an element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="builtInParameter">The built in parameter.</param>
        /// <param name="propertyValue">The output property value.</param>
        /// <returns>True if get the value successfully, false otherwise.</returns>
        public static bool GetElementIdValueFromElement(Element element, BuiltInParameter builtInParameter, out ElementId propertyValue)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            if (builtInParameter == BuiltInParameter.INVALID)
                throw new ArgumentException("BuiltInParameter is INVALID", "builtInParameter");

            propertyValue = ElementId.InvalidElementId;

            Parameter parameter = element.get_Parameter(builtInParameter);
            if (parameter != null && parameter.HasValue && parameter.StorageType == StorageType.ElementId)
            {
                propertyValue = parameter.AsElementId();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets element id value from parameter of an element or its element type.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="builtInParameter">The built in parameter.</param>
        /// <param name="propertyValue">The output property value.</param>
        /// <returns>True if get the value successfully, false otherwise.</returns>
        public static bool GetElementIdValueFromElementOrSymbol(Element element, BuiltInParameter builtInParameter, out ElementId propertyValue)
        {
            if (GetElementIdValueFromElement(element, builtInParameter, out propertyValue))
                return true;
            else
            {
                Document document = element.Document;
                ElementId typeId = element.GetTypeId();

                Element elemType = document.GetElement(typeId);
                if (elemType != null)
                {
                    return GetElementIdValueFromElement(elemType, builtInParameter, out propertyValue);
                }
                else
                    return false;
            }
        }

        /// <summary>
        /// Gets the parameter by name from an element from the parameter cache.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The Parameter.</returns>
        static private Parameter getParameterByNameFromCache(Element element, string propertyName)
        {
            Parameter parameter = null;
            string cleanPropertyName = NamingUtil.RemoveSpaces(propertyName);

            if (m_IFCParameters[element.Id].ParameterCache.TryGetValue(cleanPropertyName, out parameter))
                return parameter;

            foreach (ParameterElementCache otherCache in m_NonIFCParameters[element.Id].Values)
            {
                if (otherCache.ParameterCache.TryGetValue(cleanPropertyName, out parameter))
                    return parameter;
            }

            return parameter;
        }

        /// <summary>
        /// Gets the parameter by name from an element from the parameter cache.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="group">The parameter group.</param>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The Parameter.</returns>
        static private Parameter getParameterByNameFromCache(Element element, BuiltInParameterGroup group,
            string propertyName)
        {
            Parameter parameter = null;
            string cleanPropertyName = NamingUtil.RemoveSpaces(propertyName);

            if (group == BuiltInParameterGroup.PG_IFC)
            {
                m_IFCParameters[element.Id].ParameterCache.TryGetValue(cleanPropertyName, out parameter);
                return null;
            }

            ParameterElementCache otherCache = null;
            m_NonIFCParameters[element.Id].TryGetValue(group, out otherCache);
            if (otherCache != null)
                otherCache.ParameterCache.TryGetValue(cleanPropertyName, out parameter);

            return parameter;
        }
        
        /// <summary>
        /// Cache the parameters for an element, allowing quick access later.
        /// </summary>
        /// <param name="element">The element.</param>
        static private void CacheParametersForElement(Element element)
        {
            if (element == null)
                return;

            ElementId id = element.Id;
            if (m_NonIFCParameters.ContainsKey(id))
                return;

            IDictionary<BuiltInParameterGroup, ParameterElementCache> nonIFCParameters = new SortedDictionary<BuiltInParameterGroup, ParameterElementCache>();
            ParameterElementCache ifcParameters = new ParameterElementCache();

            m_NonIFCParameters[id] = nonIFCParameters;
            m_IFCParameters[id] = ifcParameters;

            ParameterSet parameterIds = element.Parameters;
            if (parameterIds.Size == 0)
                return;

            // We will do two passes.  In the first pass, we will look at parameters in the IFC group.
            // In the second pass, we will look at all other groups.
            ParameterSetIterator parameterIt = parameterIds.ForwardIterator();

            while (parameterIt.MoveNext())
            {
                Parameter parameter = parameterIt.Current as Parameter;
                if (parameter == null)
                    continue;
                
                Definition paramDefinition = parameter.Definition;
                if (paramDefinition == null)
                    continue;

                // Don't cache parameters that aren't visible to the user.
                InternalDefinition internalDefinition = paramDefinition as InternalDefinition;
                if (internalDefinition != null && internalDefinition.Visible == false)
                    continue;

                if (string.IsNullOrWhiteSpace(paramDefinition.Name))
                    continue;

                string cleanPropertyName = NamingUtil.RemoveSpaces(paramDefinition.Name);
                
                BuiltInParameterGroup groupId = paramDefinition.ParameterGroup;
                if (groupId != BuiltInParameterGroup.PG_IFC)
                {
                    ParameterElementCache cacheForGroup = null;
                    if (!nonIFCParameters.TryGetValue(groupId, out cacheForGroup))
                    {
                        cacheForGroup = new ParameterElementCache();
                        nonIFCParameters[groupId] = cacheForGroup;
                    }
                    cacheForGroup.ParameterCache[cleanPropertyName] = parameter;
                }
                else
                {
                    ifcParameters.ParameterCache[cleanPropertyName] = parameter;
                }
            }
        }

        /// <summary>
        /// Remove an element from the parameter cache, to save space.
        /// </summary>
        /// <param name="element">The element to be used.</param>
        /// <remarks>Generally speaking, we expect to need to access an element's parameters in one pass (this is not true
        /// for types, which could get accessed repeatedly).  As such, we are wasting space keeping an element's parameters cached
        /// after it has already been exported.</remarks>
        static public void RemoveElementFromCache(Element element)
        {
            if (element == null)
                return;

            ElementId id = element.Id;
            m_NonIFCParameters.Remove(id);
            m_IFCParameters.Remove(id);
        }

        /// <summary>
        /// Gets the parameter by name from an element for a specific parameter group.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="group">The optional parameter group.</param>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The Parameter.</returns>
        static Parameter GetParameterFromName(Element element, BuiltInParameterGroup? group, string propertyName)
        {
            if (!m_IFCParameters.ContainsKey(element.Id))
                CacheParametersForElement(element);

            return group.HasValue ? 
                getParameterByNameFromCache(element, group.Value, propertyName) :
                getParameterByNameFromCache(element, propertyName);
        }
        
        /// <summary>
        /// Gets string value from parameter of an element or its element type.
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

                Element elemType = document.GetElement(typeId);
                if (elemType != null)
                {
                    return GetStringValueFromElement(elemType, propertyName, out propertyValue);
                }
                else
                    return false;
            }
        }

        /// <summary>
        /// Gets integer value from parameter of an element or its element type.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="propertyValue">The output property value.</param>
        /// <returns>True if get the value successfully, false otherwise.</returns>
        public static bool GetIntValueFromElementOrSymbol(Element element, string propertyName, out int propertyValue)
        {
            if (GetIntValueFromElement(element, propertyName, out propertyValue))
                return true;
            else
            {
                Document document = element.Document;
                ElementId typeId = element.GetTypeId();

                Element elemType = document.GetElement(typeId);
                if (elemType != null)
                {
                    return GetIntValueFromElement(elemType, propertyName, out propertyValue);
                }
                else
                    return false;
            }
        }
    }
}
