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
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Exporter.PropertySet
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
            switch (valueType)
            {
                case PropertyValueType.EnumeratedValue:
                    {
                        IList<IFCData> valueList = new List<IFCData>();
                        valueList.Add(IFCDataUtil.CreateAsText(value));
                        return IFCInstanceExporter.CreatePropertyEnumeratedValue(file, propertyName, null, valueList, null);
                    }
                case PropertyValueType.SingleValue:
                    return IFCInstanceExporter.CreatePropertySingleValue(file, propertyName, null, IFCDataUtil.CreateAsText(value), null);
                default:
                    throw new InvalidOperationException("Missing case!");
            }
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
                textHandle = stringInfoCache.Find(propertyName, value);
                if (!IFCAnyHandleUtil.IsNullOrHasNoValue(textHandle))
                    return textHandle;
            }

            textHandle = CreateTextProperty(file, propertyName, value, valueType);

            if (canCache)
                stringInfoCache.Add(propertyName, value, textHandle);

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
        public static IFCAnyHandle CreateTextPropertyFromElement(IFCFile file, Element elem, string revitParameterName, string ifcPropertyName,
            PropertyValueType valueType, Type propertyEnumerationType)
        {
            string propertyValue;
            if (ParameterUtil.GetStringValueFromElement(elem, revitParameterName, out propertyValue))
            {
                return CreateTextPropertyFromCache(file, ifcPropertyName, propertyValue, valueType);
            }
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
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <param name="cacheAllStrings">Whether to cache all strings (true), or only the empty string (false).</param>
        /// <param name="propertyEnumerationType">The type of the enum, null if valueType isn't EnumeratedValue.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateLabelPropertyFromCache(IFCFile file, string propertyName, string value, PropertyValueType valueType,
            bool cacheAllStrings, Type propertyEnumerationType)
        {
            bool canCache = (value == String.Empty) || cacheAllStrings;
            StringPropertyInfoCache stringInfoCache = null;
            IFCAnyHandle labelHandle = null;

            if (canCache)
            {
                stringInfoCache = ExporterCacheManager.PropertyInfoCache.LabelCache;
                labelHandle = stringInfoCache.Find(propertyName, value);
                if (!IFCAnyHandleUtil.IsNullOrHasNoValue(labelHandle))
                    return labelHandle;
            }

            labelHandle = CreateLabelProperty(file, propertyName, value, valueType, propertyEnumerationType);

            if (canCache)
                stringInfoCache.Add(propertyName, value, labelHandle);

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
        /// <param name="file">
        /// The IFC file.
        /// </param>
        /// <param name="propertyName">
        /// The name of the property.
        /// </param>
        /// <param name="value">
        /// The value of the property.
        /// </param>
        /// <param name="valueType">
        /// The value type of the property.
        /// </param>
        /// <returns>
        /// The created property handle.
        /// </returns>
        public static IFCAnyHandle CreateIdentifierProperty(IFCFile file, string propertyName, string value, PropertyValueType valueType)
        {
            switch (valueType)
            {
                case PropertyValueType.EnumeratedValue:
                    {
                        IList<IFCData> valueList = new List<IFCData>();
                        valueList.Add(IFCDataUtil.CreateAsIdentifier(value));
                        return IFCInstanceExporter.CreatePropertyEnumeratedValue(file, propertyName, null, valueList, null);
                    }
                case PropertyValueType.SingleValue:
                    {
                        return IFCInstanceExporter.CreatePropertySingleValue(file, propertyName, null, IFCDataUtil.CreateAsIdentifier(value), null);
                    }
                default:
                    throw new InvalidOperationException("Missing case!");
            }
        }

        /// <summary>
        /// Create an identifier property.
        /// </summary>
        /// <param name="file">
        /// The IFC file.
        /// </param>
        /// <param name="propertyName">
        /// The name of the property.
        /// </param>
        /// <param name="value">
        /// The value of the property.
        /// </param>
        /// <param name="valueType">
        /// The value type of the property.
        /// </param>
        /// <returns>
        /// The created property handle.
        /// </returns>
        public static IFCAnyHandle CreateIdentifierPropertyFromCache(IFCFile file, string propertyName, string value, PropertyValueType valueType)
        {
            StringPropertyInfoCache stringInfoCache = ExporterCacheManager.PropertyInfoCache.IdentifierCache;
            IFCAnyHandle stringHandle = stringInfoCache.Find(propertyName, value);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(stringHandle))
                return stringHandle;

            stringHandle = CreateIdentifierProperty(file, propertyName, value, valueType);

            stringInfoCache.Add(propertyName, value, stringHandle);
            return stringHandle;
        }

        /// <summary>
        /// Create a boolean property.
        /// </summary>
        /// <param name="file">
        /// The IFC file.
        /// </param>
        /// <param name="propertyName">
        /// The name of the property.
        /// </param>
        /// <param name="value">
        /// The value of the property.
        /// </param>
        /// <param name="valueType">
        /// The value type of the property.
        /// </param>
        /// <returns>
        /// The created property handle.
        /// </returns>
        public static IFCAnyHandle CreateBooleanProperty(IFCFile file, string propertyName, bool value, PropertyValueType valueType)
        {
            switch (valueType)
            {
                case PropertyValueType.EnumeratedValue:
                    {
                        IList<IFCData> valueList = new List<IFCData>();
                        valueList.Add(IFCDataUtil.CreateAsBoolean(value));
                        return IFCInstanceExporter.CreatePropertyEnumeratedValue(file, propertyName, null, valueList, null);
                    }
                case PropertyValueType.SingleValue:
                    return IFCInstanceExporter.CreatePropertySingleValue(file, propertyName, null, IFCDataUtil.CreateAsBoolean(value), null);
                default:
                    throw new InvalidOperationException("Missing case!");
            }
        }

        /// <summary>
        /// Create a logical property.
        /// </summary>
        /// <param name="file">
        /// The IFC file.
        /// </param>
        /// <param name="propertyName">
        /// The name of the property.
        /// </param>
        /// <param name="value">
        /// The value of the property.
        /// </param>
        /// <param name="valueType">
        /// The value type of the property.
        /// </param>
        /// <returns>
        /// The created property handle.
        /// </returns>
        public static IFCAnyHandle CreateLogicalProperty(IFCFile file, string propertyName, IFCLogical value, PropertyValueType valueType)
        {
            switch (valueType)
            {
                case PropertyValueType.EnumeratedValue:
                    {
                        IList<IFCData> valueList = new List<IFCData>();
                        valueList.Add(IFCDataUtil.CreateAsLogical(value));
                        return IFCInstanceExporter.CreatePropertyEnumeratedValue(file, propertyName, null, valueList, null);
                    }
                case PropertyValueType.SingleValue:
                    return IFCInstanceExporter.CreatePropertySingleValue(file, propertyName, null, IFCDataUtil.CreateAsLogical(value), null);
                default:
                    throw new InvalidOperationException("Missing case!");
            }
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
        /// <param name="file">
        /// The IFC file.
        /// </param>
        /// <param name="propertyName">
        /// The name of the property.
        /// </param>
        /// <param name="value">
        /// The value of the property.
        /// </param>
        /// <param name="valueType">
        /// The value type of the property.
        /// </param>
        /// <returns>
        /// The created property handle.
        /// </returns>
        public static IFCAnyHandle CreateIntegerProperty(IFCFile file, string propertyName, int value, PropertyValueType valueType)
        {
            switch (valueType)
            {
                case PropertyValueType.EnumeratedValue:
                    {
                        IList<IFCData> valueList = new List<IFCData>();
                        valueList.Add(IFCDataUtil.CreateAsInteger(value));
                        return IFCInstanceExporter.CreatePropertyEnumeratedValue(file, propertyName, null, valueList, null);
                    }
                case PropertyValueType.SingleValue:
                    return IFCInstanceExporter.CreatePropertySingleValue(file, propertyName, null, IFCDataUtil.CreateAsInteger(value), null);
                default:
                    throw new InvalidOperationException("Missing case!");
            }
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

        internal static double? CanCacheDouble(double value)
        {
            // We have a partial cache here - cache multiples of 0.5 up to 10.
            if (MathUtil.IsAlmostZero(value))
                return 0.0;

            double valueTimes2 = Math.Floor(value * 2 + MathUtil.Eps());
            if (valueTimes2 > 0 && valueTimes2 <= 20 && MathUtil.IsAlmostZero(value * 2 - valueTimes2))
                return valueTimes2 / 2;
            
            return null;
        }

        internal static double? CanCacheLength(double unscaledValue, double value)
        {
            // We have a partial cache here, based on the unscaledValue.
            // Cache multiples of 0.05 up to 10.
            // Cache multiples of 50 up to 10000.
            
            if (MathUtil.IsAlmostZero(value))
                return 0.0;

            // approximate tests for most common scales are good enough here.
            if (unscaledValue < 0)
                return null;

            if (unscaledValue <= 10.0 + MathUtil.Eps())
            {
                double unscaledValueTimes2 = Math.Floor(unscaledValue * 2 + MathUtil.Eps());
                if (MathUtil.IsAlmostZero(unscaledValue * 2 - unscaledValueTimes2))
                    return UnitUtil.ScaleLength(unscaledValueTimes2 / 2);
                return null;
            }

            if (unscaledValue <= 10000.0  + MathUtil.Eps())
            {
                double unscaledValueDiv50 = Math.Floor(unscaledValue/50.0 + MathUtil.Eps());
                if (MathUtil.IsAlmostEqual(unscaledValue / 50.0, unscaledValueDiv50))
                    return UnitUtil.ScaleLength(unscaledValueDiv50 * 50.0);
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
        /// <param name="file">
        /// The IFC file.
        /// </param>
        /// <param name="propertyName">
        /// The name of the property.
        /// </param>
        /// <param name="value">
        /// The value of the property.
        /// </param>
        /// <param name="valueType">
        /// The value type of the property.
        /// </param>
        /// <returns>
        /// The created property handle.
        /// </returns>
        public static IFCAnyHandle CreateRealProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
        {
            switch (valueType)
            {
                case PropertyValueType.EnumeratedValue:
                    {
                        IList<IFCData> valueList = new List<IFCData>();
                        valueList.Add(IFCDataUtil.CreateAsReal(value));
                        return IFCInstanceExporter.CreatePropertyEnumeratedValue(file, propertyName, null, valueList, null);
                    }
                case PropertyValueType.SingleValue:
                    return IFCInstanceExporter.CreatePropertySingleValue(file, propertyName, null, IFCDataUtil.CreateAsReal(value), null);
                default:
                    throw new InvalidOperationException("Missing case!");
            }
        }

        /// <summary>Create a real property, using a cached value if possible.</summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created or cached property handle.</returns>
        public static IFCAnyHandle CreateRealPropertyFromCache(IFCFile file, string propertyName, double value, PropertyValueType valueType)
        {
            double? adjustedValue = CanCacheDouble(value);
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
        /// <param name="value">The unscaled value of the property, used for cache purposes.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateLengthMeasurePropertyFromCache(IFCFile file, string propertyName, double value, 
            PropertyValueType valueType)
        {
            double unscaledValue = UnitUtil.UnscaleLength(value);
                                            
            double? adjustedValue = CanCacheLength(unscaledValue, value);
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
            switch (valueType)
            {
                case PropertyValueType.EnumeratedValue:
                    {
                        IList<IFCData> valueList = new List<IFCData>();
                        valueList.Add(IFCDataUtil.CreateAsVolumeMeasure(value));
                        return IFCInstanceExporter.CreatePropertyEnumeratedValue(file, propertyName, null, valueList, null);
                    }
                case PropertyValueType.SingleValue:
                    return IFCInstanceExporter.CreatePropertySingleValue(file, propertyName, null, IFCDataUtil.CreateAsVolumeMeasure(value), null);
                default:
                    throw new InvalidOperationException("Missing case!");
            }
        }

        /// <summary>
        /// Create a positive length measure property.
        /// </summary>
        /// <param name="file">
        /// The IFC file.
        /// </param>
        /// <param name="propertyName">
        /// The name of the property.
        /// </param>
        /// <param name="value">
        /// The value of the property.
        /// </param>
        /// <param name="valueType">
        /// The value type of the property.
        /// </param>
        /// <returns>
        /// The created property handle.
        /// </returns>
        public static IFCAnyHandle CreatePositiveLengthMeasureProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
        {
            if (value > MathUtil.Eps())
            {
                switch (valueType)
                {
                    case PropertyValueType.EnumeratedValue:
                        {
                            IList<IFCData> valueList = new List<IFCData>();
                            valueList.Add(IFCDataUtil.CreateAsPositiveLengthMeasure(value));
                            return IFCInstanceExporter.CreatePropertyEnumeratedValue(file, propertyName, null, valueList, null);
                        }
                    case PropertyValueType.SingleValue:
                        return IFCInstanceExporter.CreatePropertySingleValue(file, propertyName, null, IFCDataUtil.CreateAsPositiveLengthMeasure(value), null);
                    default:
                        throw new InvalidOperationException("Missing case!");
                }
            }
            return null;
        }

        private static IFCAnyHandle CreateRatioMeasurePropertyCommon(IFCFile file, string propertyName, double value, PropertyValueType valueType,
            bool positiveOnly)
        {
            if (positiveOnly && (value <= MathUtil.Eps()))
                return null;

            IFCData ratioData = positiveOnly ? IFCDataUtil.CreateAsPositiveRatioMeasure(value) : IFCDataUtil.CreateAsRatioMeasure(value);
            
            switch (valueType)
            {
                case PropertyValueType.EnumeratedValue:
                    {
                        IList<IFCData> valueList = new List<IFCData>();
                        valueList.Add(ratioData);
                        return IFCInstanceExporter.CreatePropertyEnumeratedValue(file, propertyName, null, valueList, null);
                    }
                case PropertyValueType.SingleValue:
                    return IFCInstanceExporter.CreatePropertySingleValue(file, propertyName, null, ratioData, null);
                default:
                    throw new InvalidOperationException("Missing case!");
            }
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
        /// <param name="file">
        /// The IFC file.
        /// </param>
        /// <param name="propertyName">
        /// The name of the property.
        /// </param>
        /// <param name="value">
        /// The value of the property.
        /// </param>
        /// <param name="valueType">
        /// The value type of the property.
        /// </param>
        /// <returns>
        /// The created property handle.
        /// </returns>
        public static IFCAnyHandle CreatePlaneAngleMeasureProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
        {
            switch (valueType)
            {
                case PropertyValueType.EnumeratedValue:
                    {
                        IList<IFCData> valueList = new List<IFCData>();
                        valueList.Add(IFCDataUtil.CreateAsPlaneAngleMeasure(value));
                        return IFCInstanceExporter.CreatePropertyEnumeratedValue(file, propertyName, null, valueList, null);
                    }
                case PropertyValueType.SingleValue:
                    return IFCInstanceExporter.CreatePropertySingleValue(file, propertyName, null, IFCDataUtil.CreateAsPlaneAngleMeasure(value), null);
                default:
                    throw new InvalidOperationException("Missing case!");
            }
        }

        /// <summary>
        /// Create a label property, or retrieve from cache.
        /// </summary>
        /// <param name="file">
        /// The IFC file.
        /// </param>
        /// <param name="propertyName">
        /// The name of the property.
        /// </param>
        /// <param name="value">
        /// The value of the property.
        /// </param>
        /// <param name="valueType">
        /// The value type of the property.
        /// </param>
        /// <returns>
        /// The created or cached property handle.
        /// </returns>
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
        /// <param name="file">
        /// The IFC file.
        /// </param>
        /// <param name="propertyName">
        /// The name of the property.
        /// </param>
        /// <param name="value">
        /// The value of the property.
        /// </param>
        /// <param name="valueType">
        /// The value type of the property.
        /// </param>
        /// <returns>
        /// The created property handle.
        /// </returns>
        public static IFCAnyHandle CreateAreaMeasureProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
        {
            switch (valueType)
            {
                case PropertyValueType.EnumeratedValue:
                    {
                        IList<IFCData> valueList = new List<IFCData>();
                        valueList.Add(IFCDataUtil.CreateAsAreaMeasure(value));
                        return IFCInstanceExporter.CreatePropertyEnumeratedValue(file, propertyName, null, valueList, null);
                    }
                case PropertyValueType.SingleValue:
                    return IFCInstanceExporter.CreatePropertySingleValue(file, propertyName, null, IFCDataUtil.CreateAsAreaMeasure(value), null);
                default:
                    throw new InvalidOperationException("Missing case!");
            }
        }

        /// <summary>Create a count measure property.</summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateCountMeasureProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
        {
            switch (valueType)
            {
                case PropertyValueType.EnumeratedValue:
                    {
                        IList<IFCData> valueList = new List<IFCData>();
                        valueList.Add(IFCDataUtil.CreateAsCountMeasure(value));
                        return IFCInstanceExporter.CreatePropertyEnumeratedValue(file, propertyName, null, valueList, null);
                    }
                case PropertyValueType.SingleValue:
                    return IFCInstanceExporter.CreatePropertySingleValue(file, propertyName, null, IFCDataUtil.CreateAsCountMeasure(value), null);
                default:
                    throw new InvalidOperationException("Missing case!");
            }
        }

        /// <summary>Create a ThermodynamicTemperature property.</summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateThermodynamicTemperatureProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
        {
            switch (valueType)
            {
                case PropertyValueType.EnumeratedValue:
                    {
                        IList<IFCData> valueList = new List<IFCData>();
                        valueList.Add(IFCDataUtil.CreateAsThermodynamicTemperatureMeasure(value));
                        return IFCInstanceExporter.CreatePropertyEnumeratedValue(file, propertyName, null, valueList, null);
                    }
                case PropertyValueType.SingleValue:
                    return IFCInstanceExporter.CreatePropertySingleValue(file, propertyName, null, IFCDataUtil.CreateAsThermodynamicTemperatureMeasure(value), null);
                default:
                    throw new InvalidOperationException("Missing case!");
            }
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

        /// <summary>Create a PowerMeasure property.</summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreatePowerProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
        {
            switch (valueType)
            {
                case PropertyValueType.EnumeratedValue:
                    {
                        IList<IFCData> valueList = new List<IFCData>();
                        valueList.Add(IFCDataUtil.CreateAsPowerMeasure(value));
                        return IFCInstanceExporter.CreatePropertyEnumeratedValue(file, propertyName, null, valueList, null);
                    }
                case PropertyValueType.SingleValue:
                    return IFCInstanceExporter.CreatePropertySingleValue(file, propertyName, null, IFCDataUtil.CreateAsPowerMeasure(value), null);
                default:
                    throw new InvalidOperationException("Missing case!");
            }
        }

        /// <summary>Create a ThermalTransmittance property.</summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateThermalTransmittanceProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
        {
            switch (valueType)
            {
                case PropertyValueType.EnumeratedValue:
                    {
                        IList<IFCData> valueList = new List<IFCData>();
                        valueList.Add(IFCDataUtil.CreateAsThermalTransmittanceMeasure(value));
                        return IFCInstanceExporter.CreatePropertyEnumeratedValue(file, propertyName, null, valueList, null);
                    }
                case PropertyValueType.SingleValue:
                    return IFCInstanceExporter.CreatePropertySingleValue(file, propertyName, null, IFCDataUtil.CreateAsThermalTransmittanceMeasure(value), null);
                default:
                    throw new InvalidOperationException("Missing case!");
            }
        }

        /// <summary>Create a VolumetricFlowRate property.</summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateVolumetricFlowRateMeasureProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
        {
            switch (valueType)
            {
                case PropertyValueType.EnumeratedValue:
                    {
                        IList<IFCData> valueList = new List<IFCData>();
                        valueList.Add(IFCDataUtil.CreateAsVolumetricFlowRateMeasure(value));
                        return IFCInstanceExporter.CreatePropertyEnumeratedValue(file, propertyName, null, valueList, null);
                    }
                case PropertyValueType.SingleValue:
                    return IFCInstanceExporter.CreatePropertySingleValue(file, propertyName, null, IFCDataUtil.CreateAsVolumetricFlowRateMeasure(value), null);
                default:
                    throw new InvalidOperationException("Missing case!");
            }
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
            if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue))
            {
                propertyValue = UnitUtil.ScaleVolumetricFlowRate(propertyValue);
                return CreateVolumetricFlowRateMeasureProperty(file, ifcPropertyName, propertyValue, valueType);
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
            if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue))
                return CreateThermodynamicTemperaturePropertyFromCache(file, ifcPropertyName, propertyValue, valueType);
            if (ParameterUtil.GetDoubleValueFromElement(elem, null, ifcPropertyName, out propertyValue))
                return CreateThermodynamicTemperaturePropertyFromCache(file, ifcPropertyName, propertyValue, valueType);
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
            string propertyValue;
            if (ParameterUtil.GetStringValueFromElement(elem, revitParameterName, out propertyValue))
            {
                return CreateClassificationReferenceProperty(file, ifcPropertyName, propertyValue);
            }
            return null;
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
            if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue))
            {
                double scaledpropertyValue = UnitUtil.ScalePower(propertyValue);
                return CreatePowerPropertyFromCache(file, ifcPropertyName, scaledpropertyValue, valueType);
            }
            return null;
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
            if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue))
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
            string propertyValue;
            if (ParameterUtil.GetStringValueFromElement(elem, revitParameterName, out propertyValue))
            {
                return CreateLabelPropertyFromCache(file, ifcPropertyName, propertyValue, valueType, false, propertyEnumerationType);
            }
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
        /// <param name="file">
        /// The IFC file.
        /// </param>
        /// <param name="elem">
        /// The Element.
        /// </param>
        /// <param name="revitParameterName">
        /// The name of the parameter.
        /// </param>
        /// <param name="ifcPropertyName">
        /// The name of the property.
        /// </param>
        /// <param name="valueType">
        /// The value type of the property.
        /// </param>
        /// <returns>
        /// The created property handle.
        /// </returns>
        public static IFCAnyHandle CreateIdentifierPropertyFromElement(IFCFile file, Element elem, string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
        {
            string propertyValue;
            if (ParameterUtil.GetStringValueFromElement(elem, revitParameterName, out propertyValue))
            {
                return CreateIdentifierPropertyFromCache(file, ifcPropertyName, propertyValue, valueType);
            }
            return null;
        }

        /// <summary>
        /// Create an identifier property from the element's or type's parameter.
        /// </summary>
        /// <param name="file">
        /// The IFC file.
        /// </param>
        /// <param name="elem">
        /// The Element.
        /// </param>
        /// <param name="revitParameterName">
        /// The name of the parameter.
        /// </param>
        /// <param name="revitBuiltInParam">
        /// The built in parameter.
        /// </param>
        /// <param name="ifcPropertyName">
        /// The name of the property.
        /// </param>
        /// <param name="valueType">
        /// The value type of the property.
        /// </param>
        /// <returns>
        /// The created property handle.
        /// </returns>
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
            if (ParameterUtil.GetIntValueFromElement(elem, revitParameterName, out propertyValue))
                return CreateBooleanPropertyFromCache(file, ifcPropertyName, propertyValue != 0, valueType);
            if (ParameterUtil.GetIntValueFromElement(elem, ifcPropertyName, out propertyValue))
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
        /// <param name="file">
        /// The IFC file.
        /// </param>
        /// <param name="elem">
        /// The Element.
        /// </param>
        /// <param name="revitParameterName">
        /// The name of the parameter.
        /// </param>
        /// <param name="ifcPropertyName">
        /// The name of the property.
        /// </param>
        /// <param name="valueType">
        /// The value type of the property.
        /// </param>
        /// <returns>
        /// The created property handle.
        /// </returns>
        public static IFCAnyHandle CreateLogicalPropertyFromElementOrSymbol(IFCFile file, Element elem,
           string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
        {
            IFCLogical ifcLogical = IFCLogical.Unknown;
            int propertyValue;
            if (ParameterUtil.GetIntValueFromElement(elem, revitParameterName, out propertyValue))
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
        /// <param name="file">
        /// The IFC file.
        /// </param>
        /// <param name="elem">
        /// The Element.
        /// </param>
        /// <param name="revitParameterName">
        /// The name of the parameter.
        /// </param>
        /// <param name="ifcPropertyName">
        /// The name of the property.
        /// </param>
        /// <param name="valueType">
        /// The value type of the property.
        /// </param>
        /// <returns>
        /// The created property handle.
        /// </returns>
        public static IFCAnyHandle CreateIntegerPropertyFromElementOrSymbol(IFCFile file, Element elem,
           string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
        {
            int propertyValue;
            if (ParameterUtil.GetIntValueFromElement(elem, revitParameterName, out propertyValue))
            {
                return CreateIntegerPropertyFromCache(file, ifcPropertyName, propertyValue, valueType);
            }

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
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="ifcPropertyName">The name of the property.  Also, the backup name of the parameter.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateRealPropertyFromElementOrSymbol(IFCFile file, Element elem, string revitParameterName, string ifcPropertyName,
            PropertyValueType valueType)
        {
            double propertyValue;
            if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue))
                return CreateRealPropertyFromCache(file, ifcPropertyName, propertyValue, valueType);
            if (ParameterUtil.GetDoubleValueFromElement(elem, null, ifcPropertyName, out propertyValue))
                return CreateRealPropertyFromCache(file, ifcPropertyName, propertyValue, valueType);
            
            // For Symbol
            Document document = elem.Document;
            ElementId typeId = elem.GetTypeId();
            Element elemType = document.GetElement(typeId);
            if (elemType != null)
                return CreateRealPropertyFromElementOrSymbol(file, elemType, revitParameterName, ifcPropertyName, valueType);
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

            if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue))
            {
                return CreateLengthMeasurePropertyFromCache(file, ifcPropertyName, UnitUtil.ScaleLength(propertyValue), valueType);
            }

            if (builtInParameterName != null && ParameterUtil.GetDoubleValueFromElement(elem, null, builtInParameterName, out propertyValue))
            {
                return CreateLengthMeasurePropertyFromCache(file, ifcPropertyName, UnitUtil.ScaleLength(propertyValue), valueType);
            }

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

            if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue))
            {
                propertyValue = UnitUtil.ScaleLength(propertyValue);
                return CreatePositiveLengthMeasureProperty(file, ifcPropertyName, propertyValue, valueType);
            }

            if (builtInParameterName != null && ParameterUtil.GetDoubleValueFromElement(elem, null, builtInParameterName, out propertyValue))
            {
                propertyValue = UnitUtil.ScaleLength(propertyValue);
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
            if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue))
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
            if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue))
                return CreatePositiveRatioMeasureProperty(file, ifcPropertyName, propertyValue, valueType);
            if (ParameterUtil.GetDoubleValueFromElement(elem, null, ifcPropertyName, out propertyValue))
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
        /// <param name="file">
        /// The IFC file.
        /// </param>
        /// <param name="elem">
        /// The Element.
        /// </param>
        /// <param name="revitParameterName">
        /// The name of the parameter.
        /// </param>
        /// <param name="ifcPropertyName">
        /// The name of the property.
        /// </param>
        /// <param name="valueType">
        /// The value type of the property.
        /// </param>
        /// <returns>
        /// The created property handle.
        /// </returns>
        public static IFCAnyHandle CreatePlaneAngleMeasurePropertyFromElementOrSymbol(IFCFile file, Element elem, string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
        {
            double propertyValue;
            if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue))
            {
                propertyValue = UnitUtil.ScaleAngle(propertyValue);
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
        /// <param name="file">
        /// The IFC file.
        /// </param>
        /// <param name="exporterIFC">
        /// The ExporterIFC.
        /// </param>
        /// <param name="elem">
        /// The Element.
        /// </param>
        /// <param name="revitParameterName">
        /// The name of the parameter.
        /// </param>
        /// <param name="ifcPropertyName">
        /// The name of the property.
        /// </param>
        /// <param name="valueType">
        /// The value type of the property.
        /// </param>
        /// <returns>
        /// The created property handle.
        /// </returns>
        public static IFCAnyHandle CreateAreaMeasurePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
            string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
        {
            double propertyValue;
            if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue))
            {
                propertyValue = UnitUtil.ScaleArea(propertyValue);
                return CreateAreaMeasureProperty(file, ifcPropertyName, propertyValue, valueType);
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
            if (ParameterUtil.GetIntValueFromElement(elem, revitParameterName, out propertyValue))
                return CreateCountMeasureProperty(file, ifcPropertyName, propertyValue, valueType);
            if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValueReal))
                return CreateCountMeasureProperty(file, ifcPropertyName, propertyValueReal, valueType);
            return null;
        }

        /// <summary>
        /// Create an area measure property from the element's or type's parameter.
        /// </summary>
        /// <param name="file">
        /// The IFC file.
        /// </param>
        /// <param name="exporterIFC">
        /// The ExporterIFC.
        /// </param>
        /// <param name="elem">
        /// The Element.
        /// </param>
        /// <param name="revitParameterName">
        /// The name of the parameter.
        /// </param>
        /// <param name="revitBuiltInParam">
        /// The built in parameter to use, if revitParameterName isn't found.
        /// </param>
        /// <param name="ifcPropertyName">
        /// The name of the property.
        /// </param>
        /// <param name="valueType">
        /// The value type of the property.
        /// </param>
        /// <returns>
        /// The created property handle.
        /// </returns>
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
            // Make sure QTO export is enabled.
            if (!ExporterCacheManager.ExportOptionsCache.ExportBaseQuantities || (ExporterCacheManager.ExportOptionsCache.FileVersion == IFCVersion.IFCCOBIE))
                return;                        

            IFCFile file = exporterIFC.GetFile();
            HashSet<IFCAnyHandle> quantityHnds = new HashSet<IFCAnyHandle>();
            double scaledLength = typeInfo.ScaledDepth;
            double scaledArea = typeInfo.ScaledArea;
            double crossSectionArea = scaledArea;
            double scaledOuterPerimeter = typeInfo.ScaledOuterPerimeter;
            double scaledInnerPerimeter = typeInfo.ScaledInnerPerimeter;

            if (scaledLength > MathUtil.Eps())
            {
                IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityLength(file, "Length", null, null, scaledLength);
                quantityHnds.Add(quantityHnd);
            }

            if (MathUtil.AreaIsAlmostZero(crossSectionArea))
            {
                if (element != null)
                {
                    ParameterUtil.GetDoubleValueFromElement(element, BuiltInParameter.HOST_AREA_COMPUTED, out crossSectionArea);
                    crossSectionArea = UnitUtil.ScaleArea(crossSectionArea);
                }
            }

            if (!MathUtil.AreaIsAlmostZero(scaledArea))
            {
                IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityArea(file, "CrossSectionArea", null, null, crossSectionArea);
                quantityHnds.Add(quantityHnd);
            }

            if (!MathUtil.AreaIsAlmostZero(scaledArea) && !MathUtil.IsAlmostZero(scaledLength) && !MathUtil.IsAlmostZero(scaledOuterPerimeter))
            {
                double scaledPerimeter = scaledOuterPerimeter + scaledInnerPerimeter;
                double outSurfaceArea = scaledArea * 2 + scaledLength * scaledPerimeter;
                IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityArea(file, "OuterSurfaceArea", null, null, outSurfaceArea);
                quantityHnds.Add(quantityHnd);
            }

            double volume = 0.0;
            if (element != null)
            {

                ParameterUtil.GetDoubleValueFromElement(element, BuiltInParameter.HOST_VOLUME_COMPUTED, out volume);
                volume = UnitUtil.ScaleArea(volume);
            }
            if (MathUtil.VolumeIsAlmostZero(volume))
            {
                volume = scaledLength * scaledArea;
            }
            if (!MathUtil.VolumeIsAlmostZero(volume))
            {
                IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityVolume(file, "GrossVolume", null, null, volume);
                quantityHnds.Add(quantityHnd);
            }
            CreateAndRelateBaseQuantities(file, exporterIFC, elemHandle, quantityHnds);
        }
        
        /// <summary>
        /// Creates the spatial element quantities required by GSA before COBIE and adds them to the export.
        /// </summary>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="elemHnd">The element handle.</param>
        /// <param name="quantityName">The quantity name.</param>
        /// <param name="areaName">The area name.</param>
        /// <param name="area">The area.</param>
        public static void CreatePreCOBIEGSAQuantities(ExporterIFC exporterIFC, IFCAnyHandle elemHnd, string quantityName, string areaName, double area)
        {
            IFCFile file = exporterIFC.GetFile();
            IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();
            IFCAnyHandle areaQuantityHnd = IFCInstanceExporter.CreateQuantityArea(file, quantityName, null, null, area);
            HashSet<IFCAnyHandle> areaQuantityHnds = new HashSet<IFCAnyHandle>();
            areaQuantityHnds.Add(areaQuantityHnd);
            IFCAnyHandle quantity = IFCInstanceExporter.CreateElementQuantity(file, GUIDUtil.CreateGUID(), ownerHistory, quantityName, null, areaName, areaQuantityHnds);
            HashSet<IFCAnyHandle> relatedObjects = new HashSet<IFCAnyHandle>();
            relatedObjects.Add(elemHnd);
            IFCInstanceExporter.CreateRelDefinesByProperties(file, GUIDUtil.CreateGUID(), ownerHistory, null, null, relatedObjects, quantity);
        }

        /// <summary>
        /// Creates the opening quantities and adds them to the export.
        /// </summary>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="openingElement">The opening element handle.</param>
        /// <param name="extraParams">The extrusion creation data.</param>
        public static void CreateOpeningQuantities(ExporterIFC exporterIFC, IFCAnyHandle openingElement, IFCExtrusionCreationData extraParams)
        {
            IFCFile file = exporterIFC.GetFile();
            HashSet<IFCAnyHandle> quantityHnds = new HashSet<IFCAnyHandle>();
            if (extraParams.ScaledLength > MathUtil.Eps())
            {
                IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityLength(file, "Depth", null, null, extraParams.ScaledLength);
                quantityHnds.Add(quantityHnd);
            }
            if (extraParams.ScaledHeight > MathUtil.Eps())
            {
                IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityLength(file, "Height", null, null, extraParams.ScaledHeight);
                quantityHnds.Add(quantityHnd);
                quantityHnd = IFCInstanceExporter.CreateQuantityLength(file, "Width", null, null, extraParams.ScaledWidth);
                quantityHnds.Add(quantityHnd);
            }
            else if (extraParams.ScaledArea > MathUtil.Eps())
            {
                IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityArea(file, "Area", null, null, extraParams.ScaledArea);
                quantityHnds.Add(quantityHnd);
            }
            CreateAndRelateBaseQuantities(file, exporterIFC, openingElement, quantityHnds);
        }

        /// <summary>
        /// Creates the wall base quantities and adds them to the export.
        /// </summary>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="wallElement">The wall element.</param>
        /// <param name="wallHnd">The wall handle.</param>
        /// <param name="scaledLength">The scaled length.</param>
        /// <param name="scaledDepth">The scaled depth.</param>
        /// <param name="scaledFootPrintArea">The scaled foot print area.</param>
        public static void CreateWallBaseQuantities(ExporterIFC exporterIFC, Wall wallElement, IFCAnyHandle wallHnd, double scaledLength, double scaledDepth, double scaledFootPrintArea)
        {
            IFCFile file = exporterIFC.GetFile();
            HashSet<IFCAnyHandle> quantityHnds = new HashSet<IFCAnyHandle>();
            if (scaledDepth > MathUtil.Eps())
            {
                IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityLength(file, "Height", null, null, scaledDepth);
                quantityHnds.Add(quantityHnd);
            }

            if (!MathUtil.IsAlmostZero(scaledLength))
            {
                IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityLength(file, "Length", null, null, scaledLength);
                quantityHnds.Add(quantityHnd);
            }

            double scaledWidth = UnitUtil.ScaleLength(wallElement.Width);
            if (!MathUtil.IsAlmostZero(scaledWidth))
            {
                IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityLength(file, "Width", null, null, scaledWidth);
                quantityHnds.Add(quantityHnd);
            }

            double area = 0;
            ParameterUtil.GetDoubleValueFromElement(wallElement, BuiltInParameter.HOST_AREA_COMPUTED, out area);
            if (!MathUtil.IsAlmostZero(area))
            {
                area = UnitUtil.ScaleLength(area);
                IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityArea(file, "GrossSideArea", null, null, area);
                quantityHnds.Add(quantityHnd);
            }

            if (!MathUtil.IsAlmostZero(scaledFootPrintArea))
            {
                IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityArea(file, "GrossFootprintArea", null, null, scaledFootPrintArea);
                quantityHnds.Add(quantityHnd);
            }

            double volume = 0;
            ParameterUtil.GetDoubleValueFromElement(wallElement, BuiltInParameter.HOST_VOLUME_COMPUTED, out volume);
            if (!MathUtil.IsAlmostZero(volume))
            {
                volume = UnitUtil.ScaleLength(volume);
                IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityVolume(file, "GrossVolume", null, null, volume);
                quantityHnds.Add(quantityHnd);
            }

            CreateAndRelateBaseQuantities(file, exporterIFC, wallHnd, quantityHnds);
        }

        /// <summary>
        /// Creates and relate base quantities to quantity handle.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="elemHnd">The element handle.</param>
        /// <param name="quantityHnds">The quantity handles.</param>
        static void CreateAndRelateBaseQuantities(IFCFile file, ExporterIFC exporterIFC, IFCAnyHandle elemHnd, HashSet<IFCAnyHandle> quantityHnds)
        {
            if (quantityHnds.Count > 0)
            {
                IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();
                IFCAnyHandle quantity = IFCInstanceExporter.CreateElementQuantity(file, GUIDUtil.CreateGUID(), ownerHistory, "BaseQuantities", null, null, quantityHnds);
                HashSet<IFCAnyHandle> relatedObjects = new HashSet<IFCAnyHandle>();
                relatedObjects.Add(elemHnd);
                IFCInstanceExporter.CreateRelDefinesByProperties(file, GUIDUtil.CreateGUID(), ownerHistory, null, null, relatedObjects, quantity);
            }
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
            FamilyTypeInfo ifcTypeInfo = new FamilyTypeInfo();
            ifcTypeInfo.ScaledDepth = ecData.ScaledLength;
            ifcTypeInfo.ScaledArea = ecData.ScaledArea;
            ifcTypeInfo.ScaledInnerPerimeter = ecData.ScaledInnerPerimeter;
            ifcTypeInfo.ScaledOuterPerimeter = ecData.ScaledOuterPerimeter;
            CreateBeamColumnBaseQuantities(exporterIFC, elemHandle, element, ifcTypeInfo);
        }

        /// <summary>
        /// Creates property sets for Revit groups and parameters, if export options is set.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC.
        /// </param>
        /// <param name="element">
        /// The Element.
        /// </param>
        /// <param name="elementSets">
        /// The collection of IFCAnyHandles to relate properties to.
        /// </param>
        public static void CreateInternalRevitPropertySets(ExporterIFC exporterIFC, Element element, ISet<IFCAnyHandle> elementSets)
        {
            if (exporterIFC == null || element == null ||
                !ExporterCacheManager.ExportOptionsCache.PropertySetOptions.ExportInternalRevit)
                return;

            // We will allow creating internal Revit property sets for element types with no associated element handles.
            if ((elementSets == null || elementSets.Count == 0) && !(element is ElementType))
                return;

            IFCFile file = exporterIFC.GetFile();

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
                    ParameterUtil.GetNonIFCParametersForElement(whichElement);
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

                                    switch (parameterDefinition.ParameterType)
                                    {
                                        case ParameterType.Length:
                                            {
                                                propertyHandle = CreateLengthMeasurePropertyFromCache(file, parameterCaption,
                                                    UnitUtil.ScaleLength(value), PropertyValueType.SingleValue);
                                                break;
                                            }
                                        case ParameterType.Angle:
                                            {
                                                propertyHandle = CreatePlaneAngleMeasurePropertyFromCache(file, parameterCaption,
                                                    UnitUtil.ScaleAngle(value), PropertyValueType.SingleValue);
                                                break;
                                            }
                                        case ParameterType.Area:
                                            {
                                                double scaledValue = UnitUtil.ScaleArea(value);
                                                propertyHandle = CreateAreaMeasureProperty(file, parameterCaption,
                                                    scaledValue, PropertyValueType.SingleValue);
                                                break;
                                            }
                                        case ParameterType.Volume:
                                            {
                                                double scaledValue = UnitUtil.ScaleVolume(value);
                                                propertyHandle = CreateVolumeMeasureProperty(file, parameterCaption,
                                                    scaledValue, PropertyValueType.SingleValue);
                                                break;
                                            }
                                        case ParameterType.HVACAirflow:
                                        case ParameterType.PipingFlow:
                                            {
                                                double scaledValue = UnitUtil.ScaleVolumetricFlowRate(value);
                                                propertyHandle = CreateVolumetricFlowRateMeasureProperty(file, parameterCaption,
                                                    scaledValue, PropertyValueType.SingleValue);
                                                break;
                                            }
                                        case ParameterType.HVACPower:
                                            {
                                                double scaledValue = UnitUtil.ScalePower(value);
                                                propertyHandle = CreatePowerProperty(file, parameterCaption,
                                                    scaledValue, PropertyValueType.SingleValue);
                                                break;
                                            }
                                        case ParameterType.ElectricalCurrent:
                                            {
                                                double scaledValue = UnitUtil.ScaleElectricalCurrent(value);
                                                propertyHandle = ElectricalCurrentPropertyUtil.CreateElectricalCurrentMeasureProperty(file, parameterCaption,
                                                    scaledValue, PropertyValueType.SingleValue);
                                                break;
                                            }
                                        case ParameterType.ElectricalPotential:
                                            {
                                                double scaledValue = UnitUtil.ScaleElectricalVoltage(value);
                                                propertyHandle = ElectricalVoltagePropertyUtil.CreateElectricalVoltageMeasureProperty(file, parameterCaption,
                                                    scaledValue, PropertyValueType.SingleValue);
                                                break;
                                            }
                                        case ParameterType.ElectricalFrequency:
                                            {
                                                propertyHandle = FrequencyPropertyUtil.CreateFrequencyProperty(file, parameterCaption,
                                                    value, PropertyValueType.SingleValue);
                                                break;
                                            }
                                        default:
                                            assigned = false;
                                            break;
                                    }

                                    if (!assigned)
                                        propertyHandle = CreateRealPropertyFromCache(file, parameterCaption, value, PropertyValueType.SingleValue);

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

                                    Element paramElement = element.Document.GetElement(value);
                                    string valueString = (paramElement != null) ? paramElement.Name : null;
                                    if (!string.IsNullOrEmpty(valueString))
                                    {
                                        ElementType paramElementType = paramElement is ElementType ? paramElement as ElementType :
                                            element.Document.GetElement(paramElement.GetTypeId()) as ElementType;
                                        string paramElementTypeName = (paramElementType != null) ? ExporterIFCUtils.GetFamilyName(paramElementType) : null;
                                        if (!string.IsNullOrEmpty(paramElementTypeName))
                                            valueString = paramElementTypeName + ": " + valueString;
                                    }
                                    else
                                        valueString = value.ToString();

                                    currPropertiesForGroup.Add(CreateLabelPropertyFromCache(file, parameterCaption, valueString, PropertyValueType.SingleValue, true, null));
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
                    continue;

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
        /// Creates property sets for Revit groups and parameters, if export options is set.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC.
        /// </param>
        /// <param name="element">
        /// The Element.
        /// </param>
        /// <param name="productWrapper">
        /// The product wrapper.
        /// </param>
        public static void CreateInternalRevitPropertySets(ExporterIFC exporterIFC, Element element, ProductWrapper productWrapper)
        {
            if (exporterIFC == null || element == null || productWrapper == null ||
                !ExporterCacheManager.ExportOptionsCache.PropertySetOptions.ExportInternalRevit)
                return;

            IFCFile file = exporterIFC.GetFile();

            ISet<IFCAnyHandle> elements = productWrapper.GetAllObjects();
            if (elements.Count == 0)
                return;

            CreateInternalRevitPropertySets(exporterIFC, element, elements);
        }
    }
}
