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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using BIM.IFC.Utility;
using BIM.IFC.Toolkit;

namespace BIM.IFC.Exporter.PropertySet
{
    /// <summary>
    /// Provides static methods to create varies IFC properties.
    /// </summary>
    public class PropertyUtil
    {
        private static void ValidateEnumeratedValue(string value, Type propertyEnumerationType)
        {
            if (propertyEnumerationType != null && propertyEnumerationType.IsEnum)
            {
                foreach (object enumeratedValue in Enum.GetValues(propertyEnumerationType))
                {
                    string enumValue = enumeratedValue.ToString();
                    if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, enumValue))
                    {
                        value = enumValue;
                        return;
                    }
                }
                value = null;
            }
        }

        protected static IFCAnyHandle CreateCommonProperty(IFCFile file, string propertyName, IFCData valueData, PropertyValueType valueType, string unitTypeKey)
        {
            switch (valueType)
            {
                case PropertyValueType.EnumeratedValue:
                    {
                        IList<IFCData> valueList = new List<IFCData>();
                        valueList.Add(valueData);
                        return IFCInstanceExporter.CreatePropertyEnumeratedValue(file, propertyName, null, valueList, null);
                    }
                case PropertyValueType.SingleValue:
                    {
                        if (unitTypeKey != null)
                            return IFCInstanceExporter.CreatePropertySingleValue(file, propertyName, null, valueData, ExporterCacheManager.UnitsCache[unitTypeKey]);
                        else
                            return IFCInstanceExporter.CreatePropertySingleValue(file, propertyName, null, valueData, null);
                    }
                default:
                    throw new InvalidOperationException("Missing case!");
            }
        }

        /// <summary>
        /// Create a label property.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateLabelProperty(IFCFile file, string propertyName, string value, PropertyValueType valueType,
            Type propertyEnumerationType)
        {
            switch (valueType)
            {
                case PropertyValueType.EnumeratedValue:
                    {
                        IList<IFCData> valueList = new List<IFCData>();
                        ValidateEnumeratedValue(value, propertyEnumerationType);
                        valueList.Add(IFCDataUtil.CreateAsLabel(value));
                        return IFCInstanceExporter.CreatePropertyEnumeratedValue(file, propertyName, null, valueList, null);
                    }
                case PropertyValueType.SingleValue:
                    return IFCInstanceExporter.CreatePropertySingleValue(file, propertyName, null, IFCDataUtil.CreateAsLabel(value), null);
                default:
                    throw new InvalidOperationException("Missing case!");
            }
        }

        /// <summary>
        /// Create a text property.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateTextProperty(IFCFile file, string propertyName, string value, PropertyValueType valueType)
        {
            IFCData textData = IFCDataUtil.CreateAsText(value);
            return CreateCommonProperty(file, propertyName, textData, valueType, null);
                    }

        /// <summary>
        /// Create a text property, using the cached value if possible.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateTextPropertyFromCache(IFCFile file, string propertyName, string value, PropertyValueType valueType)
        {
            bool canCache = (value == String.Empty);
            StringPropertyInfoCache stringInfoCache = null;
            IFCAnyHandle textHandle = null;

            if (canCache)
            {
                stringInfoCache = ExporterCacheManager.PropertyInfoCache.TextCache;
                textHandle = stringInfoCache.Find(null, propertyName, value);
                if (!IFCAnyHandleUtil.IsNullOrHasNoValue(textHandle))
                    return textHandle;
            }

            textHandle = CreateTextProperty(file, propertyName, value, valueType);

            if (canCache)
                stringInfoCache.Add(null, propertyName, value, textHandle);

            return textHandle;
        }

        /// <summary>
        /// Create a text property from the element's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <param name="propertyEnumerationType">The type of the enum, null if valueType isn't EnumeratedValue.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateTextPropertyFromElement(IFCFile file, Element elem, string revitParameterName, string ifcPropertyName, PropertyValueType valueType, Type propertyEnumerationType)
        {
            if (elem == null)
                return null;

            string propertyValue;
            if (ParameterUtil.GetStringValueFromElement(elem.Id, revitParameterName, out propertyValue) != null)
                return CreateTextPropertyFromCache(file, ifcPropertyName, propertyValue, valueType);
            
            return null;
        }

        /// <summary>
        /// Create a text property from the element's or type's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="revitBuiltInParam">The built in parameter.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <param name="propertyEnumerationType">The type of the enum, null if valueType isn't EnumeratedValue.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateTextPropertyFromElementOrSymbol(IFCFile file, Element elem, string revitParameterName,
           BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType, Type propertyEnumerationType)
        {
            // For Instance
            IFCAnyHandle propHnd = CreateTextPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType,
                propertyEnumerationType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                return propHnd;

            if (revitBuiltInParam != BuiltInParameter.INVALID)
            {
                string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
                propHnd = CreateTextPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType, propertyEnumerationType);
                if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                    return propHnd;
            }

            // For Symbol
            Document document = elem.Document;
            ElementId typeId = elem.GetTypeId();
            Element elemType = document.GetElement(typeId);
            if (elemType != null)
                return CreateTextPropertyFromElementOrSymbol(file, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType,
                    propertyEnumerationType);
            else
                return null;
        }

        /// <summary>
        /// Create a label property, using the cached value if possible.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="parameterId">The id of the parameter that generated the value.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <param name="cacheAllStrings">Whether to cache all strings (true), or only the empty string (false).</param>
        /// <param name="propertyEnumerationType">The type of the enum, null if valueType isn't EnumeratedValue.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateLabelPropertyFromCache(IFCFile file, ElementId parameterId, string propertyName, string value, PropertyValueType valueType,
            bool cacheAllStrings, Type propertyEnumerationType)
        {
            bool canCache = (value == String.Empty) || cacheAllStrings;
            StringPropertyInfoCache stringInfoCache = null;
            IFCAnyHandle labelHandle = null;

            if (canCache)
            {
                stringInfoCache = ExporterCacheManager.PropertyInfoCache.LabelCache;
                labelHandle = stringInfoCache.Find(parameterId, propertyName, value);
                if (!IFCAnyHandleUtil.IsNullOrHasNoValue(labelHandle))
                    return labelHandle;
            }

            labelHandle = CreateLabelProperty(file, propertyName, value, valueType, propertyEnumerationType);

            if (canCache)
                stringInfoCache.Add(parameterId, propertyName, value, labelHandle);

            return labelHandle;
        }

        /// <summary>
        /// Create a label property.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="values">The values of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <param name="propertyEnumerationType">The type of the enum, null if valueType isn't EnumeratedValue.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateLabelProperty(IFCFile file, string propertyName, IList<string> values, PropertyValueType valueType,
            Type propertyEnumerationType)
        {
            switch (valueType)
            {
                case PropertyValueType.EnumeratedValue:
                    {
                        IList<IFCData> valueList = new List<IFCData>();
                        foreach (string value in values)
                        {
                            valueList.Add(IFCDataUtil.CreateAsLabel(value));
                        }
                        return IFCInstanceExporter.CreatePropertyEnumeratedValue(file, propertyName, null, valueList, null);
                    }
                case PropertyValueType.ListValue:
                    {
                        IList<IFCData> valueList = new List<IFCData>();
                        foreach (string value in values)
                        {
                            valueList.Add(IFCDataUtil.CreateAsLabel(value));
                        }
                        return IFCInstanceExporter.CreatePropertyListValue(file, propertyName, null, valueList, null);
                    }
                default:
                    throw new InvalidOperationException("Missing case!");
            }
        }

        /// <summary>
        /// Create an identifier property.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateIdentifierProperty(IFCFile file, string propertyName, string value, PropertyValueType valueType)
        {
            IFCData idData = IFCDataUtil.CreateAsIdentifier(value);
            return CreateCommonProperty(file, propertyName, idData, valueType, null);
                    }

        /// <summary>
        /// Create an identifier property.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateIdentifierPropertyFromCache(IFCFile file, string propertyName, string value, PropertyValueType valueType)
        {
            StringPropertyInfoCache stringInfoCache = ExporterCacheManager.PropertyInfoCache.IdentifierCache;
            IFCAnyHandle stringHandle = stringInfoCache.Find(null, propertyName, value);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(stringHandle))
                return stringHandle;

            stringHandle = CreateIdentifierProperty(file, propertyName, value, valueType);

            stringInfoCache.Add(null, propertyName, value, stringHandle);
            return stringHandle;
        }

        /// <summary>
        /// Create a boolean property.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateBooleanProperty(IFCFile file, string propertyName, bool value, PropertyValueType valueType)
        {
            IFCData boolData = IFCDataUtil.CreateAsBoolean(value);
            return CreateCommonProperty(file, propertyName, boolData, valueType, null);
                    }

        /// <summary>
        /// Create a logical property.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateLogicalProperty(IFCFile file, string propertyName, IFCLogical value, PropertyValueType valueType)
        {
            IFCData logicalData = IFCDataUtil.CreateAsLogical(value);
            return CreateCommonProperty(file, propertyName, logicalData, valueType, null);
            }

        /// <summary>
        /// Create a boolean property or gets one from cache.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="value">The value.</param>
        /// <param name="valueType">The value type.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateBooleanPropertyFromCache(IFCFile file, string propertyName, bool value, PropertyValueType valueType)
        {
            BooleanPropertyInfoCache boolInfoCache = ExporterCacheManager.PropertyInfoCache.BooleanCache;
            IFCAnyHandle boolHandle = boolInfoCache.Find(propertyName, value);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(boolHandle))
                return boolHandle;

            boolHandle = CreateBooleanProperty(file, propertyName, value, valueType);
            boolInfoCache.Add(propertyName, value, boolHandle);
            return boolHandle;
        }

        /// <summary>
        /// Create a logical property or gets one from cache.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="value">The value.</param>
        /// <param name="valueType">The value type.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateLogicalPropertyFromCache(IFCFile file, string propertyName, IFCLogical value, PropertyValueType valueType)
        {
            LogicalPropertyInfoCache logicalInfoCache = ExporterCacheManager.PropertyInfoCache.LogicalCache;
            IFCAnyHandle logicalHandle = logicalInfoCache.Find(propertyName, value);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(logicalHandle))
                return logicalHandle;

            logicalHandle = CreateLogicalProperty(file, propertyName, value, valueType);
            logicalInfoCache.Add(propertyName, value, logicalHandle);
            return logicalHandle;
        }

        /// <summary>
        /// Create an integer property.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateIntegerProperty(IFCFile file, string propertyName, int value, PropertyValueType valueType)
        {
            IFCData intData = IFCDataUtil.CreateAsInteger(value);
            return CreateCommonProperty(file, propertyName, intData, valueType, null);
                    }

        /// <summary>
        /// Create an integer property or gets one from cache.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="value">The value.</param>
        /// <param name="valueType">The value type.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateIntegerPropertyFromCache(IFCFile file, string propertyName, int value, PropertyValueType valueType)
        {
            bool canCache = (value >= -10 && value <= 10);
            IFCAnyHandle intHandle = null;
            IntegerPropertyInfoCache intInfoCache = null;
            if (canCache)
            {
                intInfoCache = ExporterCacheManager.PropertyInfoCache.IntegerCache;
                intHandle = intInfoCache.Find(propertyName, value);
                if (!IFCAnyHandleUtil.IsNullOrHasNoValue(intHandle))
                    return intHandle;
            }

            intHandle = CreateIntegerProperty(file, propertyName, value, valueType);
            if (canCache)
            {
                intInfoCache.Add(propertyName, value, intHandle);
            }
            return intHandle;
        }

        internal static double? CanCacheDouble(double scale, double value)
        {
            // We have a partial cache here.
            // For scale = 1.0 (feet), cache multiples of 1/2" up to 10'.
            // For scale = 0.03048 (meter), cache multiples of 50mm up to 10m.
            // For scale = 304.8 (mm), cache multiples of 50mm up to 10m.

            if (MathUtil.IsAlmostZero(value))
                return 0.0;

            // approximate tests for most common scales are good enough here.
            if (MathUtil.IsAlmostEqual(scale, 1.0) || MathUtil.IsAlmostEqual(scale, 12.0))
            {
               double multiplier = 24 / scale;
               double lengthInHalfInches = Math.Floor(value * multiplier + 0.5);
               if (lengthInHalfInches > 0 && lengthInHalfInches <= 240 && MathUtil.IsAlmostZero(value * multiplier - lengthInHalfInches))
                   return lengthInHalfInches / multiplier;
                }
            else
            {
                double multiplier = (304.8 / scale) / 50;
                double lengthIn50mm = Math.Floor(value * multiplier + 0.5);
                if (lengthIn50mm > 0 && lengthIn50mm <= 200 && MathUtil.IsAlmostZero(value * multiplier - lengthIn50mm))
                {
                }
            }
            
            return null;
        }

        internal static double? CanCachePower(double value)
        {
            // Allow caching of values between 0 and 300, in multiples of 5
            double eps = MathUtil.Eps();
            if (value < -eps || value > 300.0 + eps)
                return null;
            if (MathUtil.IsAlmostZero(value % 5.0))
                return Math.Truncate(value + 0.5);
            return null;
        }

        internal static double? CanCacheTemperature(double value)
        {
            // Allow caching of integral temperatures and half-degrees.
            if (MathUtil.IsAlmostEqual(value * 2.0, Math.Truncate(value * 2.0)))
                return Math.Truncate(value * 2.0)/2.0;
            return null;
        }

        internal static double? CanCacheThermalTransmittance(double value)
        {
            // Allow caching of values between 0 and 6.0, in multiples of 0.05
            double eps = MathUtil.Eps();
            if (value < -eps || value > 6.0 + eps)
                return null;
            if (MathUtil.IsAlmostEqual(value * 20.0, Math.Truncate(value * 20.0)))
                return Math.Truncate(value * 20.0) / 20.0;
            return null;
        }

        /// <summary>
        /// Create a real property.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateRealProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
        {
            IFCData realData = IFCDataUtil.CreateAsReal(value);
            return CreateCommonProperty(file, propertyName, realData, valueType, null);
            }

        /// <summary>Create a real property, using a cached value if possible.</summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created or cached property handle.</returns>
        public static IFCAnyHandle CreateRealPropertyFromCache(IFCFile file, double scale, string propertyName, double value, PropertyValueType valueType)
        {
            double? adjustedValue = CanCacheDouble(scale, value);
            bool canCache = adjustedValue.HasValue;
            if (canCache)
            {
                value = adjustedValue.GetValueOrDefault();
            }

            IFCAnyHandle propertyHandle;
            if (canCache)
            {
                propertyHandle = ExporterCacheManager.PropertyInfoCache.RealCache.Find(propertyName, value);
                if (propertyHandle != null)
                    return propertyHandle;
            }

            propertyHandle = CreateRealProperty(file, propertyName, value, valueType);

            if (canCache && !IFCAnyHandleUtil.IsNullOrHasNoValue(propertyHandle))
            {
                ExporterCacheManager.PropertyInfoCache.RealCache.Add(propertyName, value, propertyHandle);
            }

            return propertyHandle;
        }

        /// <summary>Create a Thermodyanamic Temperature property, using a cached value if possible.</summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created or cached property handle.</returns>
        public static IFCAnyHandle CreateThermodynamicTemperaturePropertyFromCache(IFCFile file, string propertyName, double value, PropertyValueType valueType)
        {
            double? adjustedValue = CanCacheTemperature(value);
            bool canCache = adjustedValue.HasValue;
            if (canCache)
                value = adjustedValue.GetValueOrDefault();

            IFCAnyHandle propertyHandle;
            if (canCache)
            {
                propertyHandle = ExporterCacheManager.PropertyInfoCache.ThermodynamicTemperatureCache.Find(propertyName, value);
                if (propertyHandle != null)
                    return propertyHandle;
            }

            propertyHandle = CreateThermodynamicTemperatureProperty(file, propertyName, value, valueType);

            if (canCache && !IFCAnyHandleUtil.IsNullOrHasNoValue(propertyHandle))
                ExporterCacheManager.PropertyInfoCache.ThermodynamicTemperatureCache.Add(propertyName, value, propertyHandle);
            
            return propertyHandle;
        }

        /// <summary>Create a Power measure property, using a cached value if possible.</summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created or cached property handle.</returns>
        public static IFCAnyHandle CreatePowerPropertyFromCache(IFCFile file, string propertyName, double value, PropertyValueType valueType)
        {
            double? adjustedValue = CanCachePower(value);
            bool canCache = adjustedValue.HasValue;
            if (canCache)
                value = adjustedValue.GetValueOrDefault();

            IFCAnyHandle propertyHandle;
            if (canCache)
            {
                propertyHandle = ExporterCacheManager.PropertyInfoCache.PowerCache.Find(propertyName, value);
                if (propertyHandle != null)
                    return propertyHandle;
            }

            propertyHandle = CreatePowerProperty(file, propertyName, value, valueType);

            if (canCache && !IFCAnyHandleUtil.IsNullOrHasNoValue(propertyHandle))
                ExporterCacheManager.PropertyInfoCache.PowerCache.Add(propertyName, value, propertyHandle);

            return propertyHandle;
        }

        /// <summary>Create a Thermal Transmittance property, using a cached value if possible.</summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created or cached property handle.</returns>
        public static IFCAnyHandle CreateThermalTransmittancePropertyFromCache(IFCFile file, string propertyName, double value, PropertyValueType valueType)
        {
            double? adjustedValue = CanCacheThermalTransmittance(value);
            bool canCache = adjustedValue.HasValue;
            if (canCache)
                value = adjustedValue.GetValueOrDefault();

            IFCAnyHandle propertyHandle;
            if (canCache)
            {
                propertyHandle = ExporterCacheManager.PropertyInfoCache.ThermalTransmittanceCache.Find(propertyName, value);
                if (propertyHandle != null)
                    return propertyHandle;
            }

            propertyHandle = CreateThermalTransmittanceProperty(file, propertyName, value, valueType);

            if (canCache && !IFCAnyHandleUtil.IsNullOrHasNoValue(propertyHandle))
                ExporterCacheManager.PropertyInfoCache.ThermalTransmittanceCache.Add(propertyName, value, propertyHandle);

            return propertyHandle;
        }

        /// <summary>
        /// Creates a length measure property or gets one from cache.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateLengthMeasurePropertyFromCache(IFCFile file, double scale, string propertyName, double value, PropertyValueType valueType)
        {
            double? adjustedValue = CanCacheDouble(scale, value);
            bool canCache = adjustedValue.HasValue;
            if (canCache)
            {
                value = adjustedValue.GetValueOrDefault();
            }

            IFCAnyHandle propertyHandle;
            if (canCache)
            {
                propertyHandle = ExporterCacheManager.PropertyInfoCache.LengthMeasureCache.Find(propertyName, value);
                if (propertyHandle != null)
                    return propertyHandle;
            }

            switch (valueType)
            {
                case PropertyValueType.EnumeratedValue:
                    {
                        IList<IFCData> valueList = new List<IFCData>();
                        valueList.Add(IFCDataUtil.CreateAsLengthMeasure(value));
                        propertyHandle = IFCInstanceExporter.CreatePropertyEnumeratedValue(file, propertyName, null, valueList, null);
                        break;
                    }
                case PropertyValueType.SingleValue:
                    propertyHandle = IFCInstanceExporter.CreatePropertySingleValue(file, propertyName, null, IFCDataUtil.CreateAsLengthMeasure(value), null);
                    break;
                default:
                    throw new InvalidOperationException("Missing case!");
            }

            if (canCache && !IFCAnyHandleUtil.IsNullOrHasNoValue(propertyHandle))
            {
                ExporterCacheManager.PropertyInfoCache.LengthMeasureCache.Add(propertyName, value, propertyHandle);
            }

            return propertyHandle;
        }

        /// <summary>
        /// Creates a volume measure property.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateVolumeMeasureProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
        {
            IFCData volumeData = IFCDataUtil.CreateAsVolumeMeasure(value);
            return CreateCommonProperty(file, propertyName, volumeData, valueType, null);
                    }

        /// <summary>
        /// Create a positive length measure property.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreatePositiveLengthMeasureProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
        {
            if (value > MathUtil.Eps())
            {
                IFCData posLengthData = IFCDataUtil.CreateAsPositiveLengthMeasure(value);
                return CreateCommonProperty(file, propertyName, posLengthData, valueType, null);
            }
            return null;
        }

        private static IFCAnyHandle CreateRatioMeasurePropertyCommon(IFCFile file, string propertyName, double value, PropertyValueType valueType,
            bool positiveOnly)
        {
            if (positiveOnly && (value <= MathUtil.Eps()))
                return null;

            IFCData ratioData = positiveOnly ? IFCDataUtil.CreateAsPositiveRatioMeasure(value) : IFCDataUtil.CreateAsRatioMeasure(value);
            return CreateCommonProperty(file, propertyName, ratioData, valueType, null);  
        }

        /// <summary>
        /// Create a ratio measure property.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateRatioMeasureProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
        {
            return CreateRatioMeasurePropertyCommon(file, propertyName, value, valueType, false);
        }
        
        /// <summary>
        /// Create a positive ratio measure property.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreatePositiveRatioMeasureProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
        {
            return CreateRatioMeasurePropertyCommon(file, propertyName, value, valueType, true);
        }

        /// <summary>
        /// Create a label property.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreatePlaneAngleMeasureProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
        {
            IFCData planeAngleData = IFCDataUtil.CreateAsPlaneAngleMeasure(value);
            return CreateCommonProperty(file, propertyName, planeAngleData, valueType, null);
        }

        /// <summary>
        /// Create a label property, or retrieve from cache.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created or cached property handle.</returns>
        public static IFCAnyHandle CreatePlaneAngleMeasurePropertyFromCache(IFCFile file, string propertyName, double value, PropertyValueType valueType)
        {
            // We have a partial cache here - we will only cache multiples of 15 degrees.
            bool canCache = false;
            double degreesDiv15 = Math.Floor(value / 15.0 + 0.5);
            double integerDegrees = degreesDiv15 * 15.0;
            if (MathUtil.IsAlmostEqual(value, integerDegrees))
            {
                canCache = true;
                value = integerDegrees;
            }

            IFCAnyHandle propertyHandle;
            if (canCache)
            {
                propertyHandle = ExporterCacheManager.PropertyInfoCache.PlaneAngleCache.Find(propertyName, value);
                if (propertyHandle != null)
                    return propertyHandle;
            }

            propertyHandle = CreatePlaneAngleMeasureProperty(file, propertyName, value, valueType);

            if (canCache && !IFCAnyHandleUtil.IsNullOrHasNoValue(propertyHandle))
            {
                ExporterCacheManager.PropertyInfoCache.PlaneAngleCache.Add(propertyName, value, propertyHandle);
            }

            return propertyHandle;
        }

        /// <summary>
        /// Create a area measure property.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateAreaMeasureProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
        {
            IFCData areaData = IFCDataUtil.CreateAsAreaMeasure(value);
            return CreateCommonProperty(file, propertyName, areaData, valueType, null);
                    }

        /// <summary>Create a count measure property.</summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateCountMeasureProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
        {
            IFCData countData = IFCDataUtil.CreateAsCountMeasure(value);
            return CreateCommonProperty(file, propertyName, countData, valueType, null);
                    }

        /// <summary>Create a ThermodynamicTemperature property.</summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateThermodynamicTemperatureProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
        {
            IFCData thermodynamicTemperatureMeasureData = IFCDataUtil.CreateAsThermodynamicTemperatureMeasure(value);
            return CreateCommonProperty(file, propertyName, thermodynamicTemperatureMeasureData, valueType, null);
            }

        /// <summary>Create a ClassificationReference property.</summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateClassificationReferenceProperty(IFCFile file, string propertyName, string value)
        {
            IFCAnyHandle classificationReferenceHandle = IFCInstanceExporter.CreateClassificationReference(file, null, value, null, null);
            return IFCInstanceExporter.CreatePropertyReferenceValue(file, propertyName, null, null, classificationReferenceHandle);
        }

        /// <summary>Create an IlluminanceMeasure property.</summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateIlluminanceProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
        {
            IFCData illuminanceData = IFCDataUtil.CreateAsIlluminanceMeasure(value);
            return CreateCommonProperty(file, propertyName, illuminanceData, valueType, null);
        }

        /// <summary>Create a LuminousFluxMeasure property.</summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateLuminousFluxMeasureProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
            {
            IFCData luminousFluxData = IFCDataUtil.CreateAsLuminousFluxMeasure(value);
            return CreateCommonProperty(file, propertyName, luminousFluxData, valueType, null);
        }

        /// <summary>Create a LuminousIntensityMeasure property.</summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateLuminousIntensityProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
                    {
            IFCData luminousIntensityData = IFCDataUtil.CreateAsLuminousIntensityMeasure(value);
            return CreateCommonProperty(file, propertyName, luminousIntensityData, valueType, null);
                    }
        
        /// <summary>Create a ForceMeasure property.</summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateForceProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
        {
            IFCData forceData = IFCDataUtil.CreateAsForceMeasure(value);
            return CreateCommonProperty(file, propertyName, forceData, valueType, null);
            }

        /// <summary>Create a PowerMeasure property.</summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreatePowerProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
        {
            IFCData powerData = IFCDataUtil.CreateAsPowerMeasure(value);
            return CreateCommonProperty(file, propertyName, powerData, valueType, null);
        }

        /// <summary>Create a ThermalTransmittance property.</summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateThermalTransmittanceProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
        {
            IFCData thermalTransmittanceData = IFCDataUtil.CreateAsThermalTransmittanceMeasure(value);
            return CreateCommonProperty(file, propertyName, thermalTransmittanceData, valueType, null);
                    }

        /// <summary>Create a VolumetricFlowRate property.</summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateVolumetricFlowRateMeasureProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
        {
            IFCData volumetricFlowRateData = IFCDataUtil.CreateAsVolumetricFlowRateMeasure(value);
            return CreateCommonProperty(file, propertyName, volumetricFlowRateData, valueType, null); 
                    }

        /// <summary>
        /// Create a VolumetricFlowRate measure property from the element's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateVolumetricFlowRatePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
            string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
        {
            double propertyValue;
            if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue) != null)
            {
                double scale = 0.3048;
                propertyValue = propertyValue * scale * scale * scale;
                return CreateVolumetricFlowRateMeasureProperty(file, ifcPropertyName, propertyValue, valueType);
            }
            return null;
        }

        /// <summary>
        /// Create a Color Temperature measure property from the element's parameter.  This will be an IfcReal with a custom unit.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="ifcPropertyName">The name of the property.  Also, the backup name of the parameter.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateColorTemperaturePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
            string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
        {
            double propertyValue;
            if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue) != null)
            {
                IFCData colorTemperatureData = IFCDataUtil.CreateAsMeasure(propertyValue, "IfcReal");
                return CreateCommonProperty(file, ifcPropertyName, colorTemperatureData,
                    PropertyValueType.SingleValue, "COLORTEMPERATURE");
            }
            return null;
        }

        /// <summary>
        /// Create an electrical efficacy custom measure property from the element's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="ifcPropertyName">The name of the property.  Also, the backup name of the parameter.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateElectricalEfficacyPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
            string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
        {
            double propertyValue;
            if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue) != null)
            {
                double scale = 0.3048;
                double scaledValue = propertyValue / (scale * scale);
                IFCData electricalEfficacyData = IFCDataUtil.CreateAsMeasure(scaledValue, "IfcReal");
                return CreateCommonProperty(file, ifcPropertyName, electricalEfficacyData,
                    PropertyValueType.SingleValue, "LUMINOUSEFFICACY");
            }
            return null;
        }

        /// <summary>
        /// Create a currency measure property from the element's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="ifcPropertyName">The name of the property.  Also, the backup name of the parameter.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateCurrencyPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
            string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
        {
            double propertyValue;
            if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue) != null)
            {
                IFCData currencyData = ExporterCacheManager.UnitsCache.ContainsKey("CURRENCY") ?
                    IFCDataUtil.CreateAsMeasure(propertyValue, "IfcMonetaryMeasure") :
                    IFCDataUtil.CreateAsMeasure(propertyValue, "IfcReal");
                return CreateCommonProperty(file, ifcPropertyName, currencyData, PropertyValueType.SingleValue, null);
            }
            return null;
        }
        
        /// <summary>
        /// Create a ThermodynamicTemperature measure property from the element's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="ifcPropertyName">The name of the property.  Also, the backup name of the parameter.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateThermodynamicTemperaturePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
            string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
        {
            double propertyValue;
            if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue) != null)
                return CreateThermodynamicTemperaturePropertyFromCache(file, ifcPropertyName, propertyValue, valueType);
            if (ParameterUtil.GetDoubleValueFromElement(elem, null, ifcPropertyName, out propertyValue) != null)
                return CreateThermodynamicTemperaturePropertyFromCache(file, ifcPropertyName, propertyValue, valueType);
            return null;
        }

        /// <summary>
        /// Create a color temperature property from the element's or type's parameter.  This will be an IfcReal with a special temperature unit.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateColorTemperaturePropertyFromElementOrSymbol(IFCFile file, ExporterIFC exporterIFC, Element elem,
            string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
        {
            IFCAnyHandle propHnd = CreateColorTemperaturePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                return propHnd;

            if (revitBuiltInParam != BuiltInParameter.INVALID)
            {
                string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
                propHnd = CreateColorTemperaturePropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
                if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                    return propHnd;
            }

            // For Symbol
            Document document = elem.Document;
            ElementId typeId = elem.GetTypeId();
            Element elemType = document.GetElement(typeId);
            if (elemType != null)
                return CreateColorTemperaturePropertyFromElementOrSymbol(file, exporterIFC, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType);
            else
                return null;
        }

        /// <summary>
        /// Create an electrical efficacy custom property from the element's or type's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateElectricalEfficacyPropertyFromElementOrSymbol(IFCFile file, ExporterIFC exporterIFC, Element elem,
            string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
        {
            IFCAnyHandle propHnd = CreateElectricalEfficacyPropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                return propHnd;

            if (revitBuiltInParam != BuiltInParameter.INVALID)
            {
                string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
                propHnd = CreateElectricalEfficacyPropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
                if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                    return propHnd;
            }

            // For Symbol
            Document document = elem.Document;
            ElementId typeId = elem.GetTypeId();
            Element elemType = document.GetElement(typeId);
            if (elemType != null)
                return CreateElectricalEfficacyPropertyFromElementOrSymbol(file, exporterIFC, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType);
            else
                return null;
        }

        /// <summary>
        /// Create a currency property from the element's or type's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateCurrencyPropertyFromElementOrSymbol(IFCFile file, ExporterIFC exporterIFC, Element elem,
            string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
        {
            IFCAnyHandle propHnd = CreateCurrencyPropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                return propHnd;

            if (revitBuiltInParam != BuiltInParameter.INVALID)
            {
                string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
                propHnd = CreateCurrencyPropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
                if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                    return propHnd;
            }

            // For Symbol
            Document document = elem.Document;
            ElementId typeId = elem.GetTypeId();
            Element elemType = document.GetElement(typeId);
            if (elemType != null)
                return CreateCurrencyPropertyFromElementOrSymbol(file, exporterIFC, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType);
            else
                return null;
        }

        /// <summary>
        /// Create a ThermodynamicTemperature measure property from the element's or type's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateThermodynamicTemperaturePropertyFromElementOrSymbol(IFCFile file, ExporterIFC exporterIFC, Element elem,
            string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
        {
            IFCAnyHandle propHnd = CreateThermodynamicTemperaturePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                return propHnd;

            if (revitBuiltInParam != BuiltInParameter.INVALID)
            {
                string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
                propHnd = CreateThermodynamicTemperaturePropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
                if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                    return propHnd;
            }

            // For Symbol
            Document document = elem.Document;
            ElementId typeId = elem.GetTypeId();
            Element elemType = document.GetElement(typeId);
            if (elemType != null)
                return CreateThermodynamicTemperaturePropertyFromElementOrSymbol(file, exporterIFC, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType);
            else
                return null;
        }

        /// <summary>
        /// Create a VolumetricFlowRate measure property from the element's or type's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateVolumetricFlowRatePropertyFromElementOrSymbol(IFCFile file, ExporterIFC exporterIFC, Element elem,
            string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
        {
            IFCAnyHandle propHnd = CreateVolumetricFlowRatePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                return propHnd;

            if (revitBuiltInParam != BuiltInParameter.INVALID)
            {
                string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
                propHnd = CreateVolumetricFlowRatePropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
                if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                    return propHnd;
            }

            // For Symbol
            Document document = elem.Document;
            ElementId typeId = elem.GetTypeId();
            Element elemType = document.GetElement(typeId);
            if (elemType != null)
                return CreateVolumetricFlowRatePropertyFromElementOrSymbol(file, exporterIFC, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType);
            else
                return null;
        }

        /// <summary>
        /// Create an IfcClassificationReference property from the element's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateClassificationReferencePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
            string revitParameterName, string ifcPropertyName)
        {
            if (elem == null)
                return null;

            string propertyValue;
            if (ParameterUtil.GetStringValueFromElement(elem.Id, revitParameterName, out propertyValue) != null)
                return CreateClassificationReferenceProperty(file, ifcPropertyName, propertyValue);
            
            return null;
        }

        /// <summary>
        /// Create a generic measure property from the element's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="measureType">The IfcMeasure type of the property.</param>
        /// <param name="unitType">The unit type of the property.</param>
        /// <param name="valueType">The property value type of the property.</param>
        /// <returns>The created property handle.</returns>
        private static IFCAnyHandle CreateDoublePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
            string revitParameterName, string ifcPropertyName, string measureType, UnitType unitType, PropertyValueType valueType)
        {
            double propertyValue;
            if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue) != null)
            {
                double scale = 0.3048;
                double scaledPropertyValue = propertyValue * scale;
                return CreateForceProperty(file, ifcPropertyName, scaledPropertyValue, valueType);
            }
            return null;
        }
        
        /// <summary>
        /// Create a Force measure property from the element's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateForcePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
            string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
        {
            return CreateDoublePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, 
                "IfcForceMeasure", UnitType.UT_Force, valueType);
        }
        
        /// <summary>
        /// Create a Power measure property from the element's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreatePowerPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
            string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
        {
            double propertyValue;
            Parameter powerParam = ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue);
            if (powerParam != null)
            {
                double scale = 0.3048;
                double scaledpropertyValue = propertyValue * scale * scale;
                return CreatePowerPropertyFromCache(file, ifcPropertyName, scaledpropertyValue, valueType);
            }
            return null;
        }

        /// <summary>
        /// Create a Luminous flux measure property from the element's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateLuminousFluxMeasurePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
            string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
        {
            return CreateDoublePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName,
                "IfcLuminousFluxMeasure", UnitType.UT_Electrical_Luminous_Flux, valueType);
        }

        /// <summary>
        /// Create a Luminous intensity measure property from the element's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateLuminousIntensityMeasurePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
            string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
        {
            return CreateDoublePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName,
                "IfcLuminousIntensityMeasure", UnitType.UT_Electrical_Luminous_Intensity, valueType);
        }

        /// <summary>
        /// Create a illuminance measure property from the element's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateIlluminanceMeasurePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
            string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
        {
            return CreateDoublePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName,
                "IfcIlluminanceMeasure", UnitType.UT_Electrical_Illuminance, valueType);
        }

        /// <summary>
        /// Create a pressure measure property from the element's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreatePressurePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
            string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
        {
            return CreateDoublePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName,
                "IfcPressureMeasure", UnitType.UT_HVAC_Pressure, valueType);
        }

        /// <summary>
        /// Create a ThermalTransmittance measure property from the element's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateThermalTransmittancePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
            string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
        {
            double propertyValue;
            if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue) != null)
            {
                // TODO: scale!
                return CreateThermalTransmittancePropertyFromCache(file, ifcPropertyName, propertyValue, valueType);
            }
            return null;
        }

        /// <summary>
        /// Create an IfcClassificationReference property from the element's or type's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateClassificationReferencePropertyFromElementOrSymbol(IFCFile file, ExporterIFC exporterIFC, Element elem,
            string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName)
        {
            IFCAnyHandle propHnd = CreateClassificationReferencePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                return propHnd;

            if (revitBuiltInParam != BuiltInParameter.INVALID)
            {
                string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
                propHnd = CreateClassificationReferencePropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName);
                if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                    return propHnd;
            }

            // For Symbol
            Document document = elem.Document;
            ElementId typeId = elem.GetTypeId();
            Element elemType = document.GetElement(typeId);
            if (elemType != null)
                return CreateClassificationReferencePropertyFromElementOrSymbol(file, exporterIFC, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName);
            else
                return null;
        }

        /// <summary>
        /// Create a Force measure property from the element's or type's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateForcePropertyFromElementOrSymbol(IFCFile file, ExporterIFC exporterIFC, Element elem,
            string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
        {
            IFCAnyHandle propHnd = CreateForcePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                return propHnd;

            if (revitBuiltInParam != BuiltInParameter.INVALID)
            {
                string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
                propHnd = CreateForcePropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
                if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                    return propHnd;
            }

            // For Symbol
            Document document = elem.Document;
            ElementId typeId = elem.GetTypeId();
            Element elemType = document.GetElement(typeId);
            if (elemType != null)
                return CreateForcePropertyFromElementOrSymbol(file, exporterIFC, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType);
            else
                return null;
        }

        /// <summary>
        /// Create a Power measure property from the element's or type's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreatePowerPropertyFromElementOrSymbol(IFCFile file, ExporterIFC exporterIFC, Element elem,
            string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
        {
            IFCAnyHandle propHnd = CreatePowerPropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                return propHnd;

            if (revitBuiltInParam != BuiltInParameter.INVALID)
            {
                string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
                propHnd = CreatePowerPropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
                if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                    return propHnd;
            }

            // For Symbol
            Document document = elem.Document;
            ElementId typeId = elem.GetTypeId();
            Element elemType = document.GetElement(typeId);
            if (elemType != null)
                return CreatePowerPropertyFromElementOrSymbol(file, exporterIFC, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType);
            else
                return null;
        }

        /// <summary>
        /// Create a Luminous flux measure property from the element's or type's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateLuminousFluxMeasurePropertyFromElementOrSymbol(IFCFile file, ExporterIFC exporterIFC, Element elem,
            string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
        {
            IFCAnyHandle propHnd = CreateLuminousFluxMeasurePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                return propHnd;

            if (revitBuiltInParam != BuiltInParameter.INVALID)
            {
                string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
                propHnd = CreateLuminousFluxMeasurePropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
                if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                    return propHnd;
            }

            // For Symbol
            Document document = elem.Document;
            ElementId typeId = elem.GetTypeId();
            Element elemType = document.GetElement(typeId);
            if (elemType != null)
                return CreateLuminousFluxMeasurePropertyFromElementOrSymbol(file, exporterIFC, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType);
            else
                return null;
        }

        /// <summary>
        /// Create a Luminous intensity measure property from the element's or type's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateLuminousIntensityPropertyFromElementOrSymbol(IFCFile file, ExporterIFC exporterIFC, Element elem,
            string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
        {
            IFCAnyHandle propHnd = CreateLuminousIntensityMeasurePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                return propHnd;

            if (revitBuiltInParam != BuiltInParameter.INVALID)
            {
                string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
                propHnd = CreateLuminousIntensityMeasurePropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
                if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                    return propHnd;
            }

            // For Symbol
            Document document = elem.Document;
            ElementId typeId = elem.GetTypeId();
            Element elemType = document.GetElement(typeId);
            if (elemType != null)
                return CreateLuminousIntensityPropertyFromElementOrSymbol(file, exporterIFC, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType);
            else
                return null;
        }

        /// <summary>
        /// Create an illuminance measure property from the element's or type's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateIlluminancePropertyFromElementOrSymbol(IFCFile file, ExporterIFC exporterIFC, Element elem,
            string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
        {
            IFCAnyHandle propHnd = CreateIlluminanceMeasurePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                return propHnd;

            if (revitBuiltInParam != BuiltInParameter.INVALID)
            {
                string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
                propHnd = CreateIlluminanceMeasurePropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
                if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                    return propHnd;
            }

            // For Symbol
            Document document = elem.Document;
            ElementId typeId = elem.GetTypeId();
            Element elemType = document.GetElement(typeId);
            if (elemType != null)
                return CreateIlluminancePropertyFromElementOrSymbol(file, exporterIFC, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType);
            else
                return null;
        }
        
        /// <summary>
        /// Create a pressure measure property from the element's or type's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreatePressurePropertyFromElementOrSymbol(IFCFile file, ExporterIFC exporterIFC, Element elem,
            string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
        {
            IFCAnyHandle propHnd = CreatePressurePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                return propHnd;

            if (revitBuiltInParam != BuiltInParameter.INVALID)
            {
                string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
                propHnd = CreatePressurePropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
                if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                    return propHnd;
            }

            // For Symbol
            Document document = elem.Document;
            ElementId typeId = elem.GetTypeId();
            Element elemType = document.GetElement(typeId);
            if (elemType != null)
                return CreatePressurePropertyFromElementOrSymbol(file, exporterIFC, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType);
            else
                return null;
        }
        
        /// <summary>
        /// Create a ThermalTransmittance measure property from the element's or type's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateThermalTransmittancePropertyFromElementOrSymbol(IFCFile file, ExporterIFC exporterIFC, Element elem,
            string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
        {
            IFCAnyHandle propHnd = CreateThermalTransmittancePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                return propHnd;

            if (revitBuiltInParam != BuiltInParameter.INVALID)
            {
                string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
                propHnd = CreateThermalTransmittancePropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
                if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                    return propHnd;
            }

            // For Symbol
            Document document = elem.Document;
            ElementId typeId = elem.GetTypeId();
            Element elemType = document.GetElement(typeId);
            if (elemType != null)
                return CreateThermalTransmittancePropertyFromElementOrSymbol(file, exporterIFC, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType);
            else
                return null;
        }
        
        /// <summary>
        /// Create a label property from the element's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <param name="propertyEnumerationType">The type of the enum, null if valueType isn't EnumeratedValue.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateLabelPropertyFromElement(IFCFile file, Element elem, string revitParameterName, string ifcPropertyName,
            PropertyValueType valueType, Type propertyEnumerationType)
        {
            if (elem == null)
                return null;

            string propertyValue;
            Parameter parameter = ParameterUtil.GetStringValueFromElement(elem.Id, revitParameterName, out propertyValue);
            if (parameter != null)
                return CreateLabelPropertyFromCache(file, parameter.Id, ifcPropertyName, propertyValue, valueType, false, propertyEnumerationType);
            
            return null;
        }

        /// <summary>
        /// Create a label property from the element's or type's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="revitBuiltInParam">The built in parameter.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <param name="propertyEnumerationType">The type of the enum, null if valueType isn't EnumeratedValue.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateLabelPropertyFromElementOrSymbol(IFCFile file, Element elem, string revitParameterName,
           BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType, Type propertyEnumerationType)
        {
            // For Instance
            IFCAnyHandle propHnd = CreateLabelPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType,
                propertyEnumerationType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                return propHnd;

            if (revitBuiltInParam != BuiltInParameter.INVALID)
            {
                string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
                propHnd = CreateLabelPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType, propertyEnumerationType);
                if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                    return propHnd;
            }

            // For Symbol
            Document document = elem.Document;
            ElementId typeId = elem.GetTypeId();
            Element elemType = document.GetElement(typeId);
            if (elemType != null)
                return CreateLabelPropertyFromElementOrSymbol(file, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType,
                    propertyEnumerationType);
            else
                return null;
        }

        /// <summary>
        /// Create an identifier property from the element's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateIdentifierPropertyFromElement(IFCFile file, Element elem, string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
        {
            if (elem == null)
                return null;

            string propertyValue;
            if (ParameterUtil.GetStringValueFromElement(elem.Id, revitParameterName, out propertyValue) != null)
                return CreateIdentifierPropertyFromCache(file, ifcPropertyName, propertyValue, valueType);

            return null;
        }

        /// <summary>
        /// Create an identifier property from the element's or type's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="revitBuiltInParam">The built in parameter.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateIdentifierPropertyFromElementOrSymbol(IFCFile file, Element elem, string revitParameterName,
           BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
        {
            // For Instance
            IFCAnyHandle propHnd = CreateIdentifierPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                return propHnd;

            if (revitBuiltInParam != BuiltInParameter.INVALID)
            {
                string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
                propHnd = CreateIdentifierPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
                if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                    return propHnd;
            }

            // For Symbol
            Document document = elem.Document;
            ElementId typeId = elem.GetTypeId();
            Element elemType = document.GetElement(typeId);
            if (elemType != null)
                return CreateIdentifierPropertyFromElementOrSymbol(file, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType);
            else
                return null;
        }

        /// <summary>
        /// Create a boolean property from the element's or type's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="ifcPropertyName">The name of the property.  Also, the backup name of the parameter.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateBooleanPropertyFromElementOrSymbol(IFCFile file, Element elem,
           string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
        {
            int propertyValue;
            if (ParameterUtil.GetIntValueFromElement(elem, revitParameterName, out propertyValue) != null)
                return CreateBooleanPropertyFromCache(file, ifcPropertyName, propertyValue != 0, valueType);
            if (ParameterUtil.GetIntValueFromElement(elem, ifcPropertyName, out propertyValue) != null)
                return CreateBooleanPropertyFromCache(file, ifcPropertyName, propertyValue != 0, valueType);

            // For Symbol
            Document document = elem.Document;
            ElementId typeId = elem.GetTypeId();
            Element elemType = document.GetElement(typeId);
            if (elemType != null)
                return CreateBooleanPropertyFromElementOrSymbol(file, elemType, revitParameterName, ifcPropertyName, valueType);
            else
                return null;
        }

        /// <summary>
        /// Create a logical property from the element's or type's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateLogicalPropertyFromElementOrSymbol(IFCFile file, Element elem,
           string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
        {
            IFCLogical ifcLogical = IFCLogical.Unknown;
            int propertyValue;
            if (ParameterUtil.GetIntValueFromElement(elem, revitParameterName, out propertyValue) != null)
            {
                ifcLogical = propertyValue != 0 ? IFCLogical.True : IFCLogical.False;
            }
            else
            {
                // For Symbol
                Document document = elem.Document;
                ElementId typeId = elem.GetTypeId();
                Element elemType = document.GetElement(typeId);
                if (elemType != null)
                    return CreateLogicalPropertyFromElementOrSymbol(file, elemType, revitParameterName, ifcPropertyName, valueType);
            }

            return CreateLogicalPropertyFromCache(file, ifcPropertyName, ifcLogical, valueType);
        }

        /// <summary>
        /// Create an integer property from the element's or type's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateIntegerPropertyFromElementOrSymbol(IFCFile file, Element elem,
           string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
        {
            int propertyValue;
            if (ParameterUtil.GetIntValueFromElement(elem, revitParameterName, out propertyValue) != null)
                return CreateIntegerPropertyFromCache(file, ifcPropertyName, propertyValue, valueType);

            // For Symbol
            Document document = elem.Document;
            ElementId typeId = elem.GetTypeId();
            Element elemType = document.GetElement(typeId);
            if (elemType != null)
                return CreateIntegerPropertyFromElementOrSymbol(file, elemType, revitParameterName, ifcPropertyName, valueType);
            else
                return null;
        }

        /// <summary>Create a real property from the element's or type's parameter.</summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="scale">The length scale.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="ifcPropertyName">The name of the property.  Also, the backup name of the parameter.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateRealPropertyFromElementOrSymbol(IFCFile file, double scale, Element elem, string revitParameterName, string ifcPropertyName,
            PropertyValueType valueType)
        {
            double propertyValue;
            if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue) != null)
                return CreateRealPropertyFromCache(file, scale, ifcPropertyName, propertyValue, valueType);
            if (ParameterUtil.GetDoubleValueFromElement(elem, null, ifcPropertyName, out propertyValue) != null)
                return CreateRealPropertyFromCache(file, scale, ifcPropertyName, propertyValue, valueType);
            
            // For Symbol
            Document document = elem.Document;
            ElementId typeId = elem.GetTypeId();
            Element elemType = document.GetElement(typeId);
            if (elemType != null)
                return CreateRealPropertyFromElementOrSymbol(file, scale, elemType, revitParameterName, ifcPropertyName, valueType);
            else
                return null;
        }

        /// <summary>
        /// Create a length property from the element's or type's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="builtInParameterName">The name of the built-in parameter, can be null.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateLengthMeasurePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
           string revitParameterName, string builtInParameterName, string ifcPropertyName, PropertyValueType valueType)
        {
            double propertyValue;
            double scale = exporterIFC.LinearScale;

            if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue) != null)
                return CreateLengthMeasurePropertyFromCache(file, scale, ifcPropertyName, propertyValue, valueType);

            if ((builtInParameterName != null) && (ParameterUtil.GetDoubleValueFromElement(elem, null, builtInParameterName, out propertyValue) != null))
                return CreateLengthMeasurePropertyFromCache(file, scale, ifcPropertyName, propertyValue, valueType);

            return null;
        }
        
        /// <summary>
        /// Create a positive length property from the element's or type's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="builtInParameterName">The name of the built-in parameter, can be null.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreatePositiveLengthMeasurePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
           string revitParameterName, string builtInParameterName, string ifcPropertyName, PropertyValueType valueType)
        {
            double propertyValue;

            if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue) != null)
            {
                propertyValue = propertyValue * exporterIFC.LinearScale;
                return CreatePositiveLengthMeasureProperty(file, ifcPropertyName, propertyValue, valueType);
            }

            if ((builtInParameterName != null) && (ParameterUtil.GetDoubleValueFromElement(elem, null, builtInParameterName, out propertyValue) != null))
            {
                propertyValue = propertyValue * exporterIFC.LinearScale;
                return CreatePositiveLengthMeasureProperty(file, ifcPropertyName, propertyValue, valueType);
            }

            return null;
        }

        /// <summary>
        /// Create a length property from the element's or type's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="revitBuiltInParam">The optional built-in parameter.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateLengthMeasurePropertyFromElementOrSymbol(IFCFile file, ExporterIFC exporterIFC, Element elem,
           string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
        {
            string builtInParamName = null;
            if (revitBuiltInParam != BuiltInParameter.INVALID)
                builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);

            IFCAnyHandle propHnd = CreateLengthMeasurePropertyFromElement(file, exporterIFC, elem, revitParameterName, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                return propHnd;

            // For Symbol
            Document document = elem.Document;
            ElementId typeId = elem.GetTypeId();
            Element elemType = document.GetElement(typeId);
            if (elemType != null)
                return CreateLengthMeasurePropertyFromElement(file, exporterIFC, elemType, revitParameterName, builtInParamName, ifcPropertyName, valueType);
            else
                return null;
        }

        /// <summary>
        /// Create a positive length property from the element's or type's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="revitBuiltInParam">The optional built-in parameter.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreatePositiveLengthMeasurePropertyFromElementOrSymbol(IFCFile file, ExporterIFC exporterIFC, Element elem,
           string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
        {
            string builtInParamName = null;
            if (revitBuiltInParam != BuiltInParameter.INVALID)
                builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);

            IFCAnyHandle propHnd = CreatePositiveLengthMeasurePropertyFromElement(file, exporterIFC, elem, revitParameterName, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                return propHnd;

            // For Symbol
            Document document = elem.Document;
            ElementId typeId = elem.GetTypeId();
            Element elemType = document.GetElement(typeId);
            if (elemType != null)
                return CreatePositiveLengthMeasurePropertyFromElement(file, exporterIFC, elemType, revitParameterName, builtInParamName, ifcPropertyName, valueType);
            else
                return null;
        }

        /// <summary>
        /// Create a ratio property from the element's or type's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateRatioPropertyFromElementOrSymbol(IFCFile file, ExporterIFC exporterIFC, Element elem,
           string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
        {
            double propertyValue;
            if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue) != null)
                return CreateRatioMeasureProperty(file, ifcPropertyName, propertyValue, valueType);

            // For Symbol
            Document document = elem.Document;
            ElementId typeId = elem.GetTypeId();
            Element elemType = document.GetElement(typeId);
            if (elemType == null)
                return null;

            return CreateRatioPropertyFromElementOrSymbol(file, exporterIFC, elemType, revitParameterName, ifcPropertyName, valueType);
        }
        
        /// <summary>
        /// Create a positive ratio property from the element's or type's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="ifcPropertyName">The name of the property.  Also, the backup name of the parameter.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreatePositiveRatioPropertyFromElementOrSymbol(IFCFile file, ExporterIFC exporterIFC, Element elem,
           string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
        {
            double propertyValue;
            if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue) != null)
                return CreatePositiveRatioMeasureProperty(file, ifcPropertyName, propertyValue, valueType);
            if (ParameterUtil.GetDoubleValueFromElement(elem, null, ifcPropertyName, out propertyValue) != null)
                return CreatePositiveRatioMeasureProperty(file, ifcPropertyName, propertyValue, valueType);
            
            // For Symbol
            Document document = elem.Document;
            ElementId typeId = elem.GetTypeId();
            Element elemType = document.GetElement(typeId);
            if (elemType == null)
                return null;

            return CreatePositiveRatioPropertyFromElementOrSymbol(file, exporterIFC, elemType, revitParameterName, ifcPropertyName, valueType);
        }

        /// <summary>
        /// Create a plane angle measure property from the element's or type's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreatePlaneAngleMeasurePropertyFromElementOrSymbol(IFCFile file, Element elem, string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
        {
            double propertyValue;
            if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue) != null)
            {
                // Although the default units for IFC files is radians, IFC files almost universally use degrees as their unit of measurement. 
                // However, many old IFC files failed to include degrees as the unit of measurement.
                // As such, we assume that the IFC file is in degrees, regardless of whether or not it is explicitly stated in the file.
                propertyValue *= 180 / Math.PI;
                return CreatePlaneAngleMeasurePropertyFromCache(file, ifcPropertyName, propertyValue, valueType);
            }
            // For Symbol
            Document document = elem.Document;
            ElementId typeId = elem.GetTypeId();
            Element elemType = document.GetElement(typeId);
            if (elemType != null)
                return CreatePlaneAngleMeasurePropertyFromElementOrSymbol(file, elemType, revitParameterName, ifcPropertyName, valueType);
            else
                return null;
        }

        /// <summary>
        /// Create an area measure property from the element's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateAreaMeasurePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
            string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
        {
            double propertyValue;
            if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue) != null)
            {
                propertyValue *= (exporterIFC.LinearScale * exporterIFC.LinearScale);
                return CreateAreaMeasureProperty(file, ifcPropertyName, propertyValue, valueType);
            }
            return null;
        }

        /// <summary>
        /// Create an volume measure property from the element's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateVolumeMeasurePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
            string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
        {
            double propertyValue;
            if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue) != null)
            {
                double linearScale = exporterIFC.LinearScale;
                propertyValue *= (linearScale * linearScale * linearScale);
                return CreateVolumeMeasureProperty(file, ifcPropertyName, propertyValue, valueType);
            }
            return null;
        }
        
        /// <summary>
        /// Create a count measure property from the element's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateCountMeasurePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
            string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
        {
            int propertyValue;
            double propertyValueReal;
            if (ParameterUtil.GetIntValueFromElement(elem, revitParameterName, out propertyValue) != null)
                return CreateCountMeasureProperty(file, ifcPropertyName, propertyValue, valueType);
            if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValueReal) != null)
                return CreateCountMeasureProperty(file, ifcPropertyName, propertyValueReal, valueType);
            return null;
        }

        /// <summary>
        /// Create an area measure property from the element's or type's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateAreaMeasurePropertyFromElementOrSymbol(IFCFile file, ExporterIFC exporterIFC, Element elem,
            string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
        {
            IFCAnyHandle propHnd = CreateAreaMeasurePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                return propHnd;

            if (revitBuiltInParam != BuiltInParameter.INVALID)
            {
                string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
                propHnd = CreateAreaMeasurePropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
                if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                    return propHnd;
            }

            // For Symbol
            Document document = elem.Document;
            ElementId typeId = elem.GetTypeId();
            Element elemType = document.GetElement(typeId);
            if (elemType != null)
                return CreateAreaMeasurePropertyFromElementOrSymbol(file, exporterIFC, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType);
            else
                return null;
        }

        /// <summary>
        /// Create an volume measure property from the element's or type's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateVolumeMeasurePropertyFromElementOrSymbol(IFCFile file, ExporterIFC exporterIFC, Element elem,
            string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
        {
            IFCAnyHandle propHnd = CreateVolumeMeasurePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                return propHnd;

            if (revitBuiltInParam != BuiltInParameter.INVALID)
            {
                string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
                propHnd = CreateVolumeMeasurePropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
                if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                    return propHnd;
            }

            // For Symbol
            Document document = elem.Document;
            ElementId typeId = elem.GetTypeId();
            Element elemType = document.GetElement(typeId);
            if (elemType != null)
                return CreateVolumeMeasurePropertyFromElementOrSymbol(file, exporterIFC, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType);
            else
                return null;
        }
        
        /// <summary>
        /// Create a count measure property from the element's or type's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateCountMeasurePropertyFromElementOrSymbol(IFCFile file, ExporterIFC exporterIFC, Element elem,
            string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
        {
            IFCAnyHandle propHnd = CreateCountMeasurePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                return propHnd;

            if (revitBuiltInParam != BuiltInParameter.INVALID)
            {
                string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
                propHnd = CreateCountMeasurePropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
                if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                    return propHnd;
            }

            // For Symbol
            Document document = elem.Document;
            ElementId typeId = elem.GetTypeId();
            Element elemType = document.GetElement(typeId);
            if (elemType != null)
                return CreateCountMeasurePropertyFromElementOrSymbol(file, exporterIFC, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType);
            else
                return null;
        }

        /// <summary>
        /// Creates the shared beam and column QTO values.  
        /// </summary>
        /// <remarks>
        /// This code uses the native implementation for creating these quantities, and the native class for storing the information.
        /// This will be obsoleted.
        /// </remarks>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="elemHandle">The element handle.</param>
        /// <param name="element">The beam or column element.</param>
        /// <param name="typeInfo">The FamilyTypeInfo containing the appropriate data.</param>
        public static void CreateBeamColumnBaseQuantities(ExporterIFC exporterIFC, IFCAnyHandle elemHandle, Element element, FamilyTypeInfo typeInfo)
        {
            IFCTypeInfo ifcTypeInfo = new IFCTypeInfo();
            ifcTypeInfo.ScaledDepth = typeInfo.ScaledDepth;
            ifcTypeInfo.ScaledArea = typeInfo.ScaledArea;
            ifcTypeInfo.ScaledInnerPerimeter = typeInfo.ScaledInnerPerimeter;
            ifcTypeInfo.ScaledOuterPerimeter = typeInfo.ScaledOuterPerimeter;
            ExporterIFCUtils.CreateBeamColumnBaseQuantities(exporterIFC, elemHandle, element, ifcTypeInfo);
            }

        /// <summary>
        ///  Creates the shared beam, column and member QTO values.  
        /// </summary>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="elemHandle">The element handle.</param>
        /// <param name="element">The element.</param>
        /// <param name="ecData">The IFCExtrusionCreationData containing the appropriate data.</param>
        public static void CreateBeamColumnMemberBaseQuantities(ExporterIFC exporterIFC, IFCAnyHandle elemHandle, Element element, IFCExtrusionCreationData ecData)
        {
            // Make sure QTO export is enabled.
            if (!ExporterCacheManager.ExportOptionsCache.ExportBaseQuantities || (ExporterCacheManager.ExportOptionsCache.FileVersion == IFCVersion.IFCCOBIE))
                return;                        

            IFCTypeInfo ifcTypeInfo = new IFCTypeInfo();
            ifcTypeInfo.ScaledDepth = ecData.ScaledLength;
            ifcTypeInfo.ScaledArea = ecData.ScaledArea;
            ifcTypeInfo.ScaledInnerPerimeter = ecData.ScaledInnerPerimeter;
            ifcTypeInfo.ScaledOuterPerimeter = ecData.ScaledOuterPerimeter;
            ExporterIFCUtils.CreateBeamColumnBaseQuantities(exporterIFC, elemHandle, element, ifcTypeInfo);
        }

        /// <summary>
        /// Returns a string value corresponding to an ElementId Parameter.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        /// <returns>The string.</returns>
        public static string ElementIdParameterAsString(Parameter parameter)
        {
            ElementId value = parameter.AsElementId();
            if (value == ElementId.InvalidElementId)
                return null;

            string valueString = null;
            // All real elements in Revit have non-negative ids.
            if (value.IntegerValue >= 0)
            {
                // Get the family and element name.
                Element paramElement = ExporterCacheManager.Document.GetElement(value);
                valueString = (paramElement != null) ? paramElement.Name : null;
                if (!string.IsNullOrEmpty(valueString))
                {
                    ElementType paramElementType = paramElement is ElementType ? paramElement as ElementType :
                        ExporterCacheManager.Document.GetElement(paramElement.GetTypeId()) as ElementType;
                    string paramElementTypeName = (paramElementType != null) ? ExporterIFCUtils.GetFamilyName(paramElementType) : null;
                    if (!string.IsNullOrEmpty(paramElementTypeName))
                        valueString = paramElementTypeName + ": " + valueString;
                }
            }
            else 
            {
                valueString = parameter.AsValueString();
            }

            if (string.IsNullOrEmpty(valueString))
                valueString = value.ToString();

            return valueString;
        }

        /// <summary>
        /// Creates property sets for Revit groups and parameters, if export options is set.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="element">The Element.</param>
        /// <param name="elementSets">The collection of IFCAnyHandles to relate properties to.</param>
        public static void CreateInternalRevitPropertySets(ExporterIFC exporterIFC, Element element, ICollection<IFCAnyHandle> elementSets)
        {
            if (exporterIFC == null || element == null ||
                !ExporterCacheManager.ExportOptionsCache.PropertySetOptions.ExportInternalRevit)
                return;

            // We will allow creating internal Revit property sets for element types with no associated element handles.
            if ((elementSets == null || elementSets.Count == 0) && !(element is ElementType))
                return;

            IFCFile file = exporterIFC.GetFile();

            double lengthScale = exporterIFC.LinearScale;
            double scale = 0.3048;
            double angleScale = 180.0 / Math.PI;

            ElementId typeId = element.GetTypeId();
            Element elementType = element.Document.GetElement(typeId);
            int whichStart = elementType != null ? 0 : (element is ElementType ? 1 : 0);
            if (whichStart == 1)
            {
                typeId = element.Id;
                elementType = element as ElementType;
            }

            IDictionary<string, int> paramGroupNameToSubElemIndex = new Dictionary<string, int>();

            SortedDictionary<string, HashSet<IFCAnyHandle>>[] propertySets;
            propertySets = new SortedDictionary<string, HashSet<IFCAnyHandle>>[2];
            propertySets[0] = new SortedDictionary<string, HashSet<IFCAnyHandle>>(StringComparer.InvariantCultureIgnoreCase);
            propertySets[1] = new SortedDictionary<string, HashSet<IFCAnyHandle>>(StringComparer.InvariantCultureIgnoreCase);

            // pass through: element and element type.  If the element is a ElementType, there will only be one pass.
            for (int which = whichStart; which < 2; which++)
            {
                Element whichElement = (which == 0) ? element : elementType;
                if (whichElement == null)
                    continue;

                bool createType = (which == 1);
                if (createType)
                {
                    if (ExporterCacheManager.TypePropertyInfoCache.HasTypeProperties(typeId))
                        continue;
                }

                IDictionary<BuiltInParameterGroup, ParameterElementCache> parameterElementCache =
                    ParameterUtil.GetNonIFCParametersForElement(whichElement.Id);
                if (parameterElementCache == null)
                    continue;

                foreach (KeyValuePair<BuiltInParameterGroup, ParameterElementCache> parameterElementGroup in parameterElementCache)
                {
                    BuiltInParameterGroup parameterGroup = parameterElementGroup.Key;
                    string groupName = LabelUtils.GetLabelFor(parameterGroup);
                    
                    HashSet<IFCAnyHandle> currPropertiesForGroup = new HashSet<IFCAnyHandle>();
                    propertySets[which][groupName] = currPropertiesForGroup;

                    int unadjustedSubElementIndex = -(5000000 + (int)parameterGroup + 99);
                    if (unadjustedSubElementIndex > 0)
                    {
                        int subElementIndex = unadjustedSubElementIndex + (int)IFCGenericSubElements.PSetRevitInternalStart;
                        if (subElementIndex <= (int)IFCGenericSubElements.PSetRevitInternalEnd)
                            paramGroupNameToSubElemIndex[groupName] = subElementIndex;
                    }

                    foreach (Parameter parameter in parameterElementGroup.Value.ParameterCache.Values)
                    {
                        if (!parameter.HasValue)
                            continue;

                        Definition parameterDefinition = parameter.Definition;
                        if (parameterDefinition == null)
                            continue;

                        string parameterCaption = parameterDefinition.Name;
                        
                        switch (parameter.StorageType)
                        {
                            case StorageType.None:
                                break;
                            case StorageType.Integer:
                                {
                                    int value = parameter.AsInteger();
                                    string valueAsString = parameter.AsValueString();

                                    // YesNo or actual integer?
                                    if (parameterDefinition.ParameterType == ParameterType.YesNo)
                                    {
                                        currPropertiesForGroup.Add(CreateBooleanPropertyFromCache(file, parameterCaption, value != 0, PropertyValueType.SingleValue));
                                    }
                                    else if (parameterDefinition.ParameterType == ParameterType.Invalid && (valueAsString != null))
                                    {
                                        // This is probably an internal enumerated type that should be exported as a string.
                                        currPropertiesForGroup.Add(CreateIdentifierPropertyFromCache(file, parameterCaption, valueAsString, PropertyValueType.SingleValue));
                                    }
                                    else
                                    {
                                        currPropertiesForGroup.Add(CreateIntegerPropertyFromCache(file, parameterCaption, value, PropertyValueType.SingleValue));
                                    }
                                    break;
                                }
                            case StorageType.Double:
                                {
                                    double value = parameter.AsDouble();
                                    IFCAnyHandle propertyHandle = null;
                                    bool assigned = true;
                                    // There are many different ParameterTypes in Revit that share the same unit dimensions, but that
                                    // have potentially different display units (e.g. Bar Diameter could be in millimeters while the project 
                                    // default length parameter is in meters.)  For now, we will only support one unit type.  At a later
                                    // point, we could decide to have different caches for each parameter type, and export a different
                                    // IFCUnit for each one.
                                    switch (parameterDefinition.ParameterType)
                                    {
                                        case ParameterType.Angle:
                                            {
                                                propertyHandle = CreatePlaneAngleMeasurePropertyFromCache(file, parameterCaption, value * angleScale, PropertyValueType.SingleValue);
                                                break;
                                            }
                                        case ParameterType.Area:
                                        case ParameterType.HVACCrossSection:
                                        case ParameterType.ReinforcementArea:
                                        case ParameterType.SectionArea:
                                        case ParameterType.SurfaceArea:
                                            {
                                                double scaledValue = value * lengthScale * lengthScale;
                                                propertyHandle = CreateAreaMeasureProperty(file, parameterCaption,
                                                    scaledValue, PropertyValueType.SingleValue);
                                                break;
                                            }
                                        case ParameterType.BarDiameter:
                                        case ParameterType.CrackWidth:
                                        case ParameterType.DisplacementDeflection:
                                        case ParameterType.ElectricalCableTraySize:
                                        case ParameterType.ElectricalConduitSize:
                                        case ParameterType.Length:
                                        case ParameterType.HVACDuctInsulationThickness:
                                        case ParameterType.HVACDuctLiningThickness:
                                        case ParameterType.HVACDuctSize:
                                        case ParameterType.HVACRoughness:
                                        case ParameterType.PipeInsulationThickness:
                                        case ParameterType.PipeSize:
                                        case ParameterType.PipingRoughness:
                                        case ParameterType.ReinforcementCover:
                                        case ParameterType.ReinforcementLength:
                                        case ParameterType.ReinforcementSpacing:
                                        case ParameterType.SectionDimension:
                                        case ParameterType.SectionProperty:
                                        case ParameterType.WireSize:
                                            {
                                            	propertyHandle = CreateLengthMeasurePropertyFromCache(file, lengthScale, parameterCaption,
                                                	value * lengthScale, PropertyValueType.SingleValue);
                                                break;
                                            }
                                        case ParameterType.ColorTemperature:
                                            {
                                                IFCData colorTemperatureData = IFCDataUtil.CreateAsMeasure(value, "IfcReal");
                                                propertyHandle = CreateCommonProperty(file, parameterCaption, colorTemperatureData, 
                                                    PropertyValueType.SingleValue, "COLORTEMPERATURE");
                                                break;
                                            }
                                        case ParameterType.Currency:
                                            {
                                                IFCData currencyData = ExporterCacheManager.UnitsCache.ContainsKey("CURRENCY") ? 
                                                    IFCDataUtil.CreateAsMeasure(value, "IfcMonetaryMeasure") :
                                                    IFCDataUtil.CreateAsMeasure(value, "IfcReal");
                                                propertyHandle = CreateCommonProperty(file, parameterCaption, currencyData,
                                                    PropertyValueType.SingleValue, null);
                                                break;
                                            }
                                        case ParameterType.ElectricalApparentPower:
                                        case ParameterType.ElectricalPower:
                                        case ParameterType.ElectricalWattage:
                                        case ParameterType.HVACPower:
                                            {
                                                propertyHandle = CreatePowerProperty(file, parameterCaption,
	                                                value * scale * scale, PropertyValueType.SingleValue);
                                                break;
                                            }
                                        case ParameterType.ElectricalCurrent:
                                            {
                                                propertyHandle = ElectricalCurrentPropertyUtil.CreateElectricalCurrentMeasureProperty(file, parameterCaption,
                                                    value * scale * scale, PropertyValueType.SingleValue);
                                                break;
                                            }
                                        case ParameterType.ElectricalEfficacy:
                                            {
                                                double scaledValue = value / (scale * scale);
                                                IFCData electricalEfficacyData = IFCDataUtil.CreateAsMeasure(scaledValue, "IfcReal");
                                                propertyHandle = CreateCommonProperty(file, parameterCaption, electricalEfficacyData,
                                                    PropertyValueType.SingleValue, "LUMINOUSEFFICACY");
                                                break;
                                            }
                                        case ParameterType.ElectricalFrequency:
                                            {
                                                propertyHandle = FrequencyPropertyUtil.CreateFrequencyProperty(file, parameterCaption,
                                                    value, PropertyValueType.SingleValue);
                                                break;
                                            }
                                        case ParameterType.ElectricalIlluminance:
                                            {
                                                double scaledValue = value / (scale * scale);
                                                IFCData electricalIlluminanceData = IFCDataUtil.CreateAsMeasure(scaledValue, "IfcElectricalIlluminanceMeasure");
                                                propertyHandle = CreateCommonProperty(file, parameterCaption, electricalIlluminanceData,
                                                    PropertyValueType.SingleValue, null);
                                                break;
                                            }
                                        case ParameterType.ElectricalLuminousFlux:
                                            {
                                                propertyHandle = CreateLuminousFluxMeasureProperty(file, parameterCaption,
                                                    value, PropertyValueType.SingleValue);
                                                break;
                                            }
                                        case ParameterType.ElectricalLuminousIntensity:
                                            {
                                                propertyHandle = CreateLuminousIntensityProperty(file, parameterCaption, value, PropertyValueType.SingleValue);
                                                break;
                                            }
                                        case ParameterType.ElectricalPotential:
                                            {
                                                double scaledValue = value * scale * scale;
                                                propertyHandle = ElectricalVoltagePropertyUtil.CreateElectricalVoltageMeasureProperty(file, parameterCaption,
                                                    scaledValue, PropertyValueType.SingleValue);
                                                break;
                                            }
                                        case ParameterType.ElectricalTemperature:
                                        case ParameterType.HVACTemperature:
                                        case ParameterType.PipingTemperature:
                                            {
                                                IFCData temperatureData = IFCDataUtil.CreateAsMeasure(value, "IfcThermalTransmittanceMeasure");
                                                propertyHandle = CreateCommonProperty(file, parameterCaption, temperatureData,
                                                    PropertyValueType.SingleValue, null);
                                                break;
                                            }
                                        case ParameterType.Force:
                                            {
                                                double scaledValue = value * scale;
                                                propertyHandle = CreateForceProperty(file, parameterCaption,
                                                    scaledValue, PropertyValueType.SingleValue);
                                                break;
                                            }
                                        case ParameterType.HVACAirflow:
                                        case ParameterType.PipingFlow:
                                            {
                                                double scaledValue = value * scale * scale * scale;
                                                propertyHandle = CreateVolumetricFlowRateMeasureProperty(file, parameterCaption,
                                                    scaledValue, PropertyValueType.SingleValue);
                                                break;
                                            }
                                        case ParameterType.HVACPressure:
                                        case ParameterType.PipingPressure:
                                        case ParameterType.Stress:
                                            {
                                                double scaledValue = value / scale;
                                                IFCData pressureData = IFCDataUtil.CreateAsMeasure(scaledValue, "IfcPressureMeasure");
                                                propertyHandle = CreateCommonProperty(file, parameterCaption, pressureData,
                                                    PropertyValueType.SingleValue, null);
                                                break;
                                            }
                                        case ParameterType.PipingVolume:
                                        case ParameterType.ReinforcementVolume:
                                        case ParameterType.SectionModulus:
                                        case ParameterType.Volume:
                                            {
                                                double scaledValue = value * lengthScale * lengthScale * lengthScale;
                                                propertyHandle = CreateVolumeMeasureProperty(file, parameterCaption,
                                                    scaledValue, PropertyValueType.SingleValue);
                                                break;
                                            }
                                        default:
                                            assigned = false;
                                            break;
                                    }

                                    if (!assigned)
                                        propertyHandle = CreateRealPropertyFromCache(file, lengthScale, parameterCaption, value,
                                            PropertyValueType.SingleValue);

                                    if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propertyHandle))
                                        currPropertiesForGroup.Add(propertyHandle);
                                    break;
                                }
                            case StorageType.String:
                                {
                                    string value = parameter.AsString();

                                    currPropertiesForGroup.Add(CreateTextPropertyFromCache(file, parameterCaption, value, PropertyValueType.SingleValue));
                                    break;
                                }
                            case StorageType.ElementId:
                                {
                                    ElementId value = parameter.AsElementId();
                                    if (value == ElementId.InvalidElementId)
                                        continue;

                                    string valueString = ElementIdParameterAsString(parameter);
                                    currPropertiesForGroup.Add(CreateLabelPropertyFromCache(file, parameter.Id, parameterCaption, valueString, PropertyValueType.SingleValue, true, null));
                                    break;
                                }
                        }
                    }
                }
            }

            for (int which = whichStart; which < 2; which++)
            {
                Element whichElement = (which == 0) ? element : elementType;
                if (whichElement == null)
                    continue;

                HashSet<IFCAnyHandle> typePropertySets = new HashSet<IFCAnyHandle>();

                int size = propertySets[which].Count;
                if (size == 0)
                {
                    ExporterCacheManager.TypePropertyInfoCache.AddNewElementHandles(typeId, elementSets);
                    continue;
                }

                foreach (KeyValuePair<string, HashSet<IFCAnyHandle>> currPropertySet in propertySets[which])
                {
                    if (currPropertySet.Value.Count == 0)
                        continue;

                    string psetGUID = null;
                    string psetRelGUID = null;

                    const int offsetForRelDefinesByProperties =
                        IFCGenericSubElements.PSetRevitInternalRelStart - IFCGenericSubElements.PSetRevitInternalStart;

                    int idx;
                    if (paramGroupNameToSubElemIndex.TryGetValue(currPropertySet.Key, out idx))
                    {
                        psetGUID = GUIDUtil.CreateSubElementGUID(whichElement, idx);
                        if (which == 0) psetRelGUID = GUIDUtil.CreateSubElementGUID(whichElement, idx + offsetForRelDefinesByProperties);
                    }
                    else
                    {
                        psetGUID = GUIDUtil.CreateGUID();
                        if (which == 0) psetRelGUID = GUIDUtil.CreateGUID();
                    }

                    IFCAnyHandle propertySet = IFCInstanceExporter.CreatePropertySet(file, psetGUID, exporterIFC.GetOwnerHistoryHandle(),
                        currPropertySet.Key, null, currPropertySet.Value);

                    if (which == 1)
                        typePropertySets.Add(propertySet);
                    else
                        IFCInstanceExporter.CreateRelDefinesByProperties(file, psetRelGUID, exporterIFC.GetOwnerHistoryHandle(),
                            null, null, elementSets, propertySet);
                }

                if (which == 1)
                    ExporterCacheManager.TypePropertyInfoCache.AddNewTypeProperties(typeId, typePropertySets, elementSets);
            }
        }

        /// <summary>
        /// Creates and associates the common property sets associated with ElementTypes.  These are handled differently than for elements.
        /// </summary>
        /// <param name="exporterIFC">The IFC exporter object.</param>
        /// <param name="elementType">The element type whose properties are exported.</param>
        /// <param name="existingPropertySets">The handles of property sets already associated with the type.</param>
        /// <param name="prodTypeHnd">The handle of the entity associated with the element type object.</param>
        public static void CreateElementTypeProperties(ExporterIFC exporterIFC, ElementType elementType,
            HashSet<IFCAnyHandle> existingPropertySets, IFCAnyHandle prodTypeHnd)
        {
            HashSet<IFCAnyHandle> propertySets = new HashSet<IFCAnyHandle>();

            // Pass in an empty set of handles - we don't want IfcRelDefinesByProperties for type properties.
            ISet<IFCAnyHandle> associatedObjectIds = new HashSet<IFCAnyHandle>();
            PropertyUtil.CreateInternalRevitPropertySets(exporterIFC, elementType, associatedObjectIds);

            TypePropertyInfo additionalPropertySets = null;
            if (ExporterCacheManager.TypePropertyInfoCache.TryGetValue(elementType.Id, out additionalPropertySets))
                propertySets.UnionWith(additionalPropertySets.PropertySets);

            if (existingPropertySets != null && existingPropertySets.Count > 0)
                propertySets.UnionWith(existingPropertySets);

            IFCFile file = exporterIFC.GetFile();
            using (IFCTransaction transaction = new IFCTransaction(file))
            {
                Document doc = elementType.Document;
                IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();

                IList<IList<PropertySetDescription>> psetsToCreate = ExporterCacheManager.ParameterCache.PropertySets;

                IList<PropertySetDescription> currPsetsToCreate = ExporterUtil.GetCurrPSetsToCreate(prodTypeHnd, psetsToCreate);
                foreach (PropertySetDescription currDesc in currPsetsToCreate)
                {
                    HashSet<IFCAnyHandle> props = currDesc.ProcessEntries(file, exporterIFC, null, elementType, elementType);
                    if (props.Count > 0)
                    {
                        int subElementIndex = currDesc.SubElementIndex;
                        string guid = GUIDUtil.CreateSubElementGUID(elementType, subElementIndex);

                        string paramSetName = currDesc.Name;
                        IFCAnyHandle propertySet = IFCInstanceExporter.CreatePropertySet(file, guid, ownerHistory, paramSetName, null, props);
                        propertySets.Add(propertySet);
                    }
                }

                if (propertySets.Count != 0)
                {
                    prodTypeHnd.SetAttribute("HasPropertySets", propertySets);
                    // Don't assign the property sets to the instances if we have just assigned them to the type.
                    if (additionalPropertySets != null)
                        additionalPropertySets.AssignedToType = true;
                }

                transaction.Commit();
            }
        }
    }
}
