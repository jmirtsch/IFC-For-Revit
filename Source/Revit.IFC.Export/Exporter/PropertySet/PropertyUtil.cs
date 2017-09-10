//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC dbs containing model geometry.
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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;

using GeometryGym.Ifc;

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

      protected static IfcProperty CreateCommonProperty(DatabaseIfc db, string propertyName, IfcValue valueData, PropertyValueType valueType, string unitTypeKey)
      {
         switch (valueType)
         {
            case PropertyValueType.EnumeratedValue:
               {
                  return new IfcPropertyEnumeratedValue(db, propertyName, valueData);
               }
            case PropertyValueType.SingleValue:
               {
                  IfcPropertySingleValue result = new IfcPropertySingleValue(db, propertyName, valueData);
                  if (unitTypeKey != null)
                     result.Unit = ExporterCacheManager.UnitsCache[unitTypeKey];
                  return result;
               }
            default:
               throw new InvalidOperationException("Missing case!");
         }
      }

      /// <summary>
      /// Create a label property.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateLabelProperty(DatabaseIfc db, string propertyName, string value, PropertyValueType valueType,
          Type propertyEnumerationType)
      {
         switch (valueType)
         {
            case PropertyValueType.EnumeratedValue:
               return new IfcPropertyEnumeratedValue(db, propertyName, new IfcLabel(value));
            case PropertyValueType.SingleValue:
               return new IfcPropertySingleValue(db, propertyName, new IfcLabel(value));
            default:
               throw new InvalidOperationException("Missing case!");
         }
      }

      /// <summary>
      /// Create a text property.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateTextProperty(DatabaseIfc db, string propertyName, string value, PropertyValueType valueType)
      {
         return CreateCommonProperty(db, propertyName, new IfcText(value), valueType, null);
      }

      /// <summary>
      /// Create a text property, using the cached value if possible.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateTextPropertyFromCache(DatabaseIfc db, string propertyName, string value, PropertyValueType valueType)
      {
         bool canCache = (value == String.Empty);
         StringPropertyInfoCache stringInfoCache = null;
         IfcProperty textHandle = null;

         if (canCache)
         {
            stringInfoCache = ExporterCacheManager.PropertyInfoCache.TextCache;
            textHandle = stringInfoCache.Find(null, propertyName, value);
            if (textHandle != null)
               return textHandle;
         }

         textHandle = CreateTextProperty(db, propertyName, value, valueType);

         if (canCache)
            stringInfoCache.Add(null, propertyName, value, textHandle);

         return textHandle;
      }

      /// <summary>
      /// Create a text property from the element's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <param name="propertyEnumerationType">The type of the enum, null if valueType isn't EnumeratedValue.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateTextPropertyFromElement(DatabaseIfc db, Element elem, string revitParameterName, string ifcPropertyName, PropertyValueType valueType, Type propertyEnumerationType)
      {
         if (elem == null)
            return null;

         string propertyValue;
         if (ParameterUtil.GetStringValueFromElement(elem.Id, revitParameterName, out propertyValue) != null)
            return CreateTextPropertyFromCache(db, ifcPropertyName, propertyValue, valueType);

         return null;
      }

      /// <summary>
      /// Create a text property from the element's or type's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <param name="propertyEnumerationType">The type of the enum, null if valueType isn't EnumeratedValue.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateTextPropertyFromElementOrSymbol(DatabaseIfc db, Element elem, string revitParameterName,
         BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType, Type propertyEnumerationType)
      {
         // For Instance
         IfcProperty propHnd = CreateTextPropertyFromElement(db, elem, revitParameterName, ifcPropertyName, valueType,
             propertyEnumerationType);
         if (propHnd != null)
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateTextPropertyFromElement(db, elem, builtInParamName, ifcPropertyName, valueType, propertyEnumerationType);
            if (propHnd != null)
               return propHnd;
         }

         // For Symbol
         Document document = elem.Document;
         ElementId typeId = elem.GetTypeId();
         Element elemType = document.GetElement(typeId);
         if (elemType != null)
            return CreateTextPropertyFromElementOrSymbol(db, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType,
                propertyEnumerationType);
         else
            return null;
      }

      /// <summary>
      /// Create a label property, using the cached value if possible.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="parameterId">The id of the parameter that generated the value.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <param name="cacheAllStrings">Whether to cache all strings (true), or only the empty string (false).</param>
      /// <param name="propertyEnumerationType">The type of the enum, null if valueType isn't EnumeratedValue.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateLabelPropertyFromCache(DatabaseIfc db, ElementId parameterId, string propertyName, string value, PropertyValueType valueType,
          bool cacheAllStrings, Type propertyEnumerationType)
      {
         bool canCache = (value == String.Empty) || cacheAllStrings;
         StringPropertyInfoCache stringInfoCache = null;
         IfcProperty labelHandle = null;

         if (canCache)
         {
            stringInfoCache = ExporterCacheManager.PropertyInfoCache.LabelCache;
            labelHandle = stringInfoCache.Find(parameterId, propertyName, value);
            if (labelHandle != null)
               return labelHandle;
         }

         labelHandle = CreateLabelProperty(db, propertyName, value, valueType, propertyEnumerationType);

         if (canCache)
            stringInfoCache.Add(parameterId, propertyName, value, labelHandle);

         return labelHandle;
      }

      /// <summary>
      /// Create a label property.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <param name="propertyEnumerationType">The type of the enum, null if valueType isn't EnumeratedValue.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateLabelProperty(DatabaseIfc db, string propertyName, IList<string> values, PropertyValueType valueType,
          Type propertyEnumerationType)
      {
         switch (valueType)
         {
            case PropertyValueType.EnumeratedValue:
               {
                  IList<IfcValue> valueList = new List<IfcValue>();
                  foreach (string value in values)
                  {
                     valueList.Add(new IfcLabel(value));
                  }
                  return new IfcPropertyEnumeratedValue(db, propertyName, valueList);
               }
            case PropertyValueType.ListValue:
               {
                  IList<IfcValue> valueList = new List<IfcValue>();
                  foreach (string value in values)
                  {
                     valueList.Add(new IfcLabel(value));
                  }
                  return new IfcPropertyListValue(db, propertyName, valueList);
               }
            default:
               throw new InvalidOperationException("Missing case!");
         }
      }

      /// <summary>
      /// Create an identifier property.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateIdentifierProperty(DatabaseIfc db, string propertyName, string value, PropertyValueType valueType)
      {
         return CreateCommonProperty(db, propertyName, new IfcIdentifier(value), valueType, null);
      }

      /// <summary>
      /// Create an identifier property.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateIdentifierPropertyFromCache(DatabaseIfc db, string propertyName, string value, PropertyValueType valueType)
      {
         StringPropertyInfoCache stringInfoCache = ExporterCacheManager.PropertyInfoCache.IdentifierCache;
         IfcProperty stringHandle = stringInfoCache.Find(null, propertyName, value);
         if (stringHandle != null)
            return stringHandle;

         stringHandle = CreateIdentifierProperty(db, propertyName, value, valueType);

         stringInfoCache.Add(null, propertyName, value, stringHandle);
         return stringHandle;
      }

      /// <summary>
      /// Create a boolean property.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateBooleanProperty(DatabaseIfc db, string propertyName, bool value, PropertyValueType valueType)
      {
         return CreateCommonProperty(db, propertyName, new IfcBoolean(value), valueType, null);
      }

      /// <summary>
      /// Create a logical property.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateLogicalProperty(DatabaseIfc db, string propertyName, IfcLogicalEnum value, PropertyValueType valueType)
      {
         return CreateCommonProperty(db, propertyName, new IfcLogical(value), valueType, null);
      }

      /// <summary>
      /// Create a boolean property or gets one from cache.
      /// </summary>
      /// <param name="db">The db.</param>
      /// <param name="propertyName">The property name.</param>
      /// <param name="value">The value.</param>
      /// <param name="valueType">The value type.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateBooleanPropertyFromCache(DatabaseIfc db, string propertyName, bool value, PropertyValueType valueType)
      {
         BooleanPropertyInfoCache boolInfoCache = ExporterCacheManager.PropertyInfoCache.BooleanCache;
         IfcProperty boolHandle = boolInfoCache.Find(propertyName, value);
         if (boolHandle != null)
            return boolHandle;

         boolHandle = CreateBooleanProperty(db, propertyName, value, valueType);
         boolInfoCache.Add(propertyName, value, boolHandle);
         return boolHandle;
      }

      /// <summary>
      /// Create a logical property or gets one from cache.
      /// </summary>
      /// <param name="db">The db.</param>
      /// <param name="propertyName">The property name.</param>
      /// <param name="value">The value.</param>
      /// <param name="valueType">The value type.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateLogicalPropertyFromCache(DatabaseIfc db, string propertyName, IFCLogical value, PropertyValueType valueType)
      {
         IfcLogicalEnum logical = (value == IFCLogical.True ? IfcLogicalEnum.TRUE : (value == IFCLogical.False ? IfcLogicalEnum.FALSE : IfcLogicalEnum.UNKNOWN));
         LogicalPropertyInfoCache logicalInfoCache = ExporterCacheManager.PropertyInfoCache.LogicalCache;
         IfcProperty logicalHandle = logicalInfoCache.Find(propertyName, logical);
         if (logicalHandle != null)
            return logicalHandle;

         logicalHandle = CreateLogicalProperty(db, propertyName, logical, valueType);
         logicalInfoCache.Add(propertyName, logical, logicalHandle);
         return logicalHandle;
      }

      /// <summary>
      /// Create an integer property.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateIntegerProperty(DatabaseIfc db, string propertyName, int value, PropertyValueType valueType)
      {
         return CreateCommonProperty(db, propertyName, new IfcInteger(value), valueType, null);
      }

      /// <summary>
      /// Create an integer property or gets one from cache.
      /// </summary>
      /// <param name="db">The db.</param>
      /// <param name="propertyName">The property name.</param>
      /// <param name="value">The value.</param>
      /// <param name="valueType">The value type.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateIntegerPropertyFromCache(DatabaseIfc db, string propertyName, int value, PropertyValueType valueType)
      {
         bool canCache = (value >= -10 && value <= 10);
         IfcProperty intHandle = null;
         IntegerPropertyInfoCache intInfoCache = null;
         if (canCache)
         {
            intInfoCache = ExporterCacheManager.PropertyInfoCache.IntegerCache;
            intHandle = intInfoCache.Find(propertyName, value);
            if (intHandle != null)
               return intHandle;
         }

         intHandle = CreateIntegerProperty(db, propertyName, value, valueType);
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
         // Cache multiples of +/- 0.05 up to 10.
         // Cache multiples of +/- 50 up to 10000.

         if (MathUtil.IsAlmostZero(value))
            return 0.0;

         // approximate tests for most common scales are good enough here.
         bool isNegative = (unscaledValue < 0);
         double unscaledPositiveValue = isNegative ? -unscaledValue : unscaledValue;
         double eps = MathUtil.Eps();

         if (unscaledPositiveValue <= 10.0 + eps)
         {
            double unscaledPositiveValueTimes2 = Math.Floor(unscaledPositiveValue * 2 + eps);
            if (MathUtil.IsAlmostZero(unscaledPositiveValue * 2 - unscaledPositiveValueTimes2))
            {
               double scaledPositiveValue = UnitUtil.ScaleLength(unscaledPositiveValueTimes2 / 2);
               return isNegative ? -scaledPositiveValue : scaledPositiveValue;
            }
            return null;
         }

         if (unscaledPositiveValue <= 10000.0 + eps)
         {
            double unscaledPositiveValueDiv50 = Math.Floor(unscaledPositiveValue / 50.0 + eps);
            if (MathUtil.IsAlmostEqual(unscaledPositiveValue / 50.0, unscaledPositiveValueDiv50))
            {
               double scaledPositiveValue = UnitUtil.ScaleLength(unscaledPositiveValueDiv50 * 50.0);
               return isNegative ? -scaledPositiveValue : scaledPositiveValue;
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
            return Math.Truncate(value * 2.0) / 2.0;
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
      /// <param name="db">The IFC db.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateRealProperty(DatabaseIfc db, string propertyName, double value, PropertyValueType valueType)
      {
         return CreateCommonProperty(db, propertyName, new IfcReal(value), valueType, null);
      }

      /// <summary>Create a real property, using a cached value if possible.</summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IfcProperty CreateRealPropertyFromCache(DatabaseIfc db, string propertyName, double value, PropertyValueType valueType)
      {
         double? adjustedValue = CanCacheDouble(value);
         bool canCache = adjustedValue.HasValue;
         if (canCache)
         {
            value = adjustedValue.GetValueOrDefault();
         }

         IfcProperty propertyHandle;
         if (canCache)
         {
            propertyHandle = ExporterCacheManager.PropertyInfoCache.RealCache.Find(propertyName, value);
            if (propertyHandle != null)
               return propertyHandle;
         }

         propertyHandle = CreateRealProperty(db, propertyName, value, valueType);

         if (canCache && propertyHandle != null)
         {
            ExporterCacheManager.PropertyInfoCache.RealCache.Add(propertyName, value, propertyHandle);
         }

         return propertyHandle;
      }

      /// <summary>Create a Thermodyanamic Temperature property, using a cached value if possible.</summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IfcProperty CreateThermodynamicTemperaturePropertyFromCache(DatabaseIfc db, string propertyName, double value, PropertyValueType valueType)
      {
         double? adjustedValue = CanCacheTemperature(value);
         bool canCache = adjustedValue.HasValue;
         if (canCache)
            value = adjustedValue.GetValueOrDefault();

         IfcProperty propertyHandle;
         if (canCache)
         {
            propertyHandle = ExporterCacheManager.PropertyInfoCache.ThermodynamicTemperatureCache.Find(propertyName, value);
            if (propertyHandle != null)
               return propertyHandle;
         }

         double scaledValue = UnitUtil.ScaleDouble(UnitType.UT_HVAC_Temperature, value);
         propertyHandle = CreateThermodynamicTemperatureProperty(db, propertyName, scaledValue, valueType);

         if (canCache && propertyHandle != null)
            ExporterCacheManager.PropertyInfoCache.ThermodynamicTemperatureCache.Add(propertyName, value, propertyHandle);

         return propertyHandle;
      }

      /// <summary>Create a Power measure property, using a cached value if possible.</summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IfcProperty CreatePowerPropertyFromCache(DatabaseIfc db, string propertyName, double value, PropertyValueType valueType)
      {
         double? adjustedValue = CanCachePower(value);
         bool canCache = adjustedValue.HasValue;
         if (canCache)
            value = adjustedValue.GetValueOrDefault();

         IfcProperty propertyHandle;
         if (canCache)
         {
            propertyHandle = ExporterCacheManager.PropertyInfoCache.PowerCache.Find(propertyName, value);
            if (propertyHandle != null)
               return propertyHandle;
         }

         propertyHandle = CreatePowerProperty(db, propertyName, value, valueType);

         if (canCache && propertyHandle != null)
            ExporterCacheManager.PropertyInfoCache.PowerCache.Add(propertyName, value, propertyHandle);

         return propertyHandle;
      }

      /// <summary>Create a Thermal Transmittance property, using a cached value if possible.</summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IfcProperty CreateThermalTransmittancePropertyFromCache(DatabaseIfc db, string propertyName, double value, PropertyValueType valueType)
      {
         double? adjustedValue = CanCacheThermalTransmittance(value);
         bool canCache = adjustedValue.HasValue;
         if (canCache)
            value = adjustedValue.GetValueOrDefault();

         IfcProperty propertyHandle;
         if (canCache)
         {
            propertyHandle = ExporterCacheManager.PropertyInfoCache.ThermalTransmittanceCache.Find(propertyName, value);
            if (propertyHandle != null)
               return propertyHandle;
         }

         propertyHandle = CreateThermalTransmittanceProperty(db, propertyName, value, valueType);

         if (canCache && propertyHandle != null)
            ExporterCacheManager.PropertyInfoCache.ThermalTransmittanceCache.Add(propertyName, value, propertyHandle);

         return propertyHandle;
      }

      /// <summary>
      /// Creates a length measure property or gets one from cache.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The unscaled value of the property, used for cache purposes.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateLengthMeasurePropertyFromCache(DatabaseIfc db, string propertyName, double value,
          PropertyValueType valueType)
      {
         double unscaledValue = UnitUtil.UnscaleLength(value);

         double? adjustedValue = CanCacheLength(unscaledValue, value);
         bool canCache = adjustedValue.HasValue;
         if (canCache)
         {
            value = adjustedValue.GetValueOrDefault();
         }

         IfcProperty propertyHandle;
         if (canCache)
         {
            propertyHandle = ExporterCacheManager.PropertyInfoCache.LengthMeasureCache.Find(propertyName, value);
            if (propertyHandle != null)
               return propertyHandle;
         }

         switch (valueType)
         {
            case PropertyValueType.EnumeratedValue:
               propertyHandle = new IfcPropertyEnumeratedValue(db, propertyName, new IfcLengthMeasure(value));
               break;
            case PropertyValueType.SingleValue:
               propertyHandle = new IfcPropertySingleValue(db, propertyName, new IfcLengthMeasure(value));
               break;
            default:
               throw new InvalidOperationException("Missing case!");
         }

         if (canCache && propertyHandle != null)
         {
            ExporterCacheManager.PropertyInfoCache.LengthMeasureCache.Add(propertyName, value, propertyHandle);
         }

         return propertyHandle;
      }

      /// <summary>
      /// Creates a volume measure property.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateVolumeMeasureProperty(DatabaseIfc db, string propertyName, double value, PropertyValueType valueType)
      {
         return CreateCommonProperty(db, propertyName, new IfcVolumeMeasure(value), valueType, null);
      }

      /// <summary>
      /// Create a positive length measure property.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreatePositiveLengthMeasureProperty(DatabaseIfc db, string propertyName, double value, PropertyValueType valueType)
      {
         if (value > MathUtil.Eps())
         {
            return CreateCommonProperty(db, propertyName, new IfcPositiveLengthMeasure(value), valueType, null);
         }
         return null;
      }

      /// <summary>
      /// Create a linear velocity measure property.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateLinearVelocityMeasureProperty(DatabaseIfc db, string propertyName, double value, PropertyValueType valueType)
      {
         return CreateCommonProperty(db, propertyName, new IfcLinearVelocityMeasure(value), valueType, null);
      }

      private static IfcProperty CreateRatioMeasurePropertyCommon(DatabaseIfc db, string propertyName, double value, PropertyValueType valueType,
          PropertyType propertyType)
      {
         IfcValue ratioData = null;
         switch (propertyType)
         {
            case PropertyType.PositiveRatio:
               {
                  if (value <= MathUtil.Eps())
                     return null;

                  ratioData = new IfcPositiveRatioMeasure(value);
                  break;
               }
            case PropertyType.NormalisedRatio:
               {
                  if (value < -MathUtil.Eps() || value > 1.0 + MathUtil.Eps())
                     return null;

                  ratioData = new IfcNormalisedRatioMeasure(value);
                  break;
               }
            default:
               {
                  ratioData = new IfcRatioMeasure(value);
                  break;
               }
         }

         return CreateCommonProperty(db, propertyName, ratioData, valueType, null);
      }

      /// <summary>
      /// Create a ratio measure property.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateRatioMeasureProperty(DatabaseIfc db, string propertyName, double value, PropertyValueType valueType)
      {
         return CreateRatioMeasurePropertyCommon(db, propertyName, value, valueType, PropertyType.Ratio);
      }

      /// <summary>
      /// Create a normalised ratio measure property.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateNormalisedRatioMeasureProperty(DatabaseIfc db, string propertyName, double value, PropertyValueType valueType)
      {
         return CreateRatioMeasurePropertyCommon(db, propertyName, value, valueType, PropertyType.NormalisedRatio);
      }

      /// <summary>
      /// Create a positive ratio measure property.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreatePositiveRatioMeasureProperty(DatabaseIfc db, string propertyName, double value, PropertyValueType valueType)
      {
         return CreateRatioMeasurePropertyCommon(db, propertyName, value, valueType, PropertyType.PositiveRatio);
      }

      /// <summary>
      /// Create a label property.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreatePlaneAngleMeasureProperty(DatabaseIfc db, string propertyName, double value, PropertyValueType valueType)
      {
         return CreateCommonProperty(db, propertyName, new IfcPlaneAngleMeasure(value), valueType, null);
      }

      /// <summary>
      /// Create a label property, or retrieve from cache.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IfcProperty CreatePlaneAngleMeasurePropertyFromCache(DatabaseIfc db, string propertyName, double value, PropertyValueType valueType)
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

         IfcProperty propertyHandle;
         if (canCache)
         {
            propertyHandle = ExporterCacheManager.PropertyInfoCache.PlaneAngleCache.Find(propertyName, value);
            if (propertyHandle != null)
               return propertyHandle;
         }

         propertyHandle = CreatePlaneAngleMeasureProperty(db, propertyName, value, valueType);

         if (canCache && propertyHandle != null)
         {
            ExporterCacheManager.PropertyInfoCache.PlaneAngleCache.Add(propertyName, value, propertyHandle);
         }

         return propertyHandle;
      }

      /// <summary>
      /// Create a area measure property.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateAreaMeasureProperty(DatabaseIfc db, string propertyName, double value, PropertyValueType valueType)
      {
         return CreateCommonProperty(db, propertyName, new IfcAreaMeasure(value), valueType, null);
      }

      /// <summary>Create a count measure property.</summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateCountMeasureProperty(DatabaseIfc db, string propertyName, double value, PropertyValueType valueType)
      {
         return CreateCommonProperty(db, propertyName, new IfcCountMeasure(value), valueType, null);
      }

      /// <summary>Create a ThermodynamicTemperature property.</summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateThermodynamicTemperatureProperty(DatabaseIfc db, string propertyName, double value, PropertyValueType valueType)
      {
         return CreateCommonProperty(db, propertyName, new IfcThermodynamicTemperatureMeasure(value), valueType, null);
      }

      /// <summary>Create a ClassificationReference property.</summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateClassificationReferenceProperty(DatabaseIfc db, string propertyName, string value)
      {
         IfcClassificationReference classificationReferenceHandle = IFCInstanceExporter.CreateClassificationReference(db, null, value, null, null);
         return new IfcPropertyReferenceValue(db, propertyName) { PropertyReference = classificationReferenceHandle };
      }

      /// <summary>Create an IlluminanceMeasure property.</summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateIlluminanceProperty(DatabaseIfc db, string propertyName, double value, PropertyValueType valueType)
      {
         return CreateCommonProperty(db, propertyName, new IfcIlluminanceMeasure(value), valueType, null);
      }

      /// <summary>Create a LuminousFluxMeasure property.</summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateLuminousFluxMeasureProperty(DatabaseIfc db, string propertyName, double value, PropertyValueType valueType)
      {
         return CreateCommonProperty(db, propertyName, new IfcLuminousFluxMeasure(value), valueType, null);
      }

      /// <summary>Create a LuminousIntensityMeasure property.</summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateLuminousIntensityProperty(DatabaseIfc db, string propertyName, double value, PropertyValueType valueType)
      {
         return CreateCommonProperty(db, propertyName, new IfcLuminousIntensityMeasure(value), valueType, null);
      }

      /// <summary>Create a ForceMeasure property.</summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateForceProperty(DatabaseIfc db, string propertyName, double value, PropertyValueType valueType)
      {
         return CreateCommonProperty(db, propertyName, new IfcForceMeasure(value), valueType, null);
      }

      /// <summary>Create a PowerMeasure property.</summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreatePowerProperty(DatabaseIfc db, string propertyName, double value, PropertyValueType valueType)
      {
         return CreateCommonProperty(db, propertyName, new IfcPowerMeasure(value), valueType, null);
      }

      /// <summary>Create a ThermalTransmittance property.</summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateThermalTransmittanceProperty(DatabaseIfc db, string propertyName, double value, PropertyValueType valueType)
      {
         return CreateCommonProperty(db, propertyName, new IfcThermalTransmittanceMeasure(value), valueType, null);
      }

      /// <summary>Create a VolumetricFlowRate property.</summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateVolumetricFlowRateMeasureProperty(DatabaseIfc db, string propertyName, double value, PropertyValueType valueType)
      {
         return CreateCommonProperty(db, propertyName, new IfcVolumetricFlowRateMeasure(value), valueType, null);
      }

      /// <summary>
      /// Create a VolumetricFlowRate measure property from the element's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateVolumetricFlowRatePropertyFromElement(DatabaseIfc db, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         return CreateDoublePropertyFromElement(db, elem, revitParameterName, ifcPropertyName,
             new IfcVolumetricFlowRateMeasure(0), UnitType.UT_HVAC_Airflow, valueType);
      }

      /// <summary>
      /// Create a Color Temperature measure property from the element's parameter.  This will be an IfcReal with a custom unit.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.  Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateColorTemperaturePropertyFromElement(DatabaseIfc db, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         double propertyValue;
         if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue) != null)
         {
            double scaledValue = UnitUtil.ScaleDouble(UnitType.UT_Color_Temperature, propertyValue);
            return CreateCommonProperty(db, ifcPropertyName, new IfcReal(scaledValue),
                PropertyValueType.SingleValue, "COLORTEMPERATURE");
         }
         return null;
      }

      /// <summary>
      /// Create an electrical efficacy custom measure property from the element's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.  Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateElectricalEfficacyPropertyFromElement(DatabaseIfc db, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         double propertyValue;
         if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue) != null)
         {
            double scaledValue = UnitUtil.ScaleDouble(UnitType.UT_Electrical_Efficacy, propertyValue);
            return CreateCommonProperty(db, ifcPropertyName, new IfcReal(scaledValue),
                PropertyValueType.SingleValue, "LUMINOUSEFFICACY");
         }
         return null;
      }

      /// <summary>
      /// Create a currency measure property from the element's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.  Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateCurrencyPropertyFromElement(DatabaseIfc db, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         double propertyValue;
         if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue) != null)
         {
            IfcValue currencyData = null;
            if (ExporterCacheManager.UnitsCache.ContainsKey("CURRENCY"))
               currencyData = new IfcMonetaryMeasure(propertyValue);
            else
               currencyData = new IfcReal(propertyValue);
            return CreateCommonProperty(db, ifcPropertyName, currencyData, PropertyValueType.SingleValue, null);
         }
         return null;
      }

      /// <summary>
      /// Create a ThermodynamicTemperature measure property from the element's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.  Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateThermodynamicTemperaturePropertyFromElement(DatabaseIfc db, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         double propertyValue;
         if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue) != null)
            return CreateThermodynamicTemperaturePropertyFromCache(db, ifcPropertyName, propertyValue, valueType);
         if (ParameterUtil.GetDoubleValueFromElement(elem, null, ifcPropertyName, out propertyValue) != null)
            return CreateThermodynamicTemperaturePropertyFromCache(db, ifcPropertyName, propertyValue, valueType);
         return null;
      }

      /// <summary>
      /// Create a color temperature property from the element's or type's parameter.  This will be an IfcReal with a special temperature unit.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateColorTemperaturePropertyFromElementOrSymbol(DatabaseIfc db, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IfcProperty propHnd = CreateColorTemperaturePropertyFromElement(db, elem, revitParameterName, ifcPropertyName, valueType);
         if (propHnd != null)
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateColorTemperaturePropertyFromElement(db,  elem, builtInParamName, ifcPropertyName, valueType);
            if (propHnd != null)
               return propHnd;
         }

         // For Symbol
         Document document = elem.Document;
         ElementId typeId = elem.GetTypeId();
         Element elemType = document.GetElement(typeId);
         if (elemType != null)
            return CreateColorTemperaturePropertyFromElementOrSymbol(db, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType);
         else
            return null;
      }

      /// <summary>
      /// Create an electrical efficacy custom property from the element's or type's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateElectricalEfficacyPropertyFromElementOrSymbol(DatabaseIfc db, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IfcProperty propHnd = CreateElectricalEfficacyPropertyFromElement(db, elem, revitParameterName, ifcPropertyName, valueType);
         if (propHnd != null)
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateElectricalEfficacyPropertyFromElement(db, elem, builtInParamName, ifcPropertyName, valueType);
            if (propHnd != null)
               return propHnd;
         }

         // For Symbol
         Document document = elem.Document;
         ElementId typeId = elem.GetTypeId();
         Element elemType = document.GetElement(typeId);
         if (elemType != null)
            return CreateElectricalEfficacyPropertyFromElementOrSymbol(db, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType);
         else
            return null;
      }

      /// <summary>
      /// Create a currency property from the element's or type's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateCurrencyPropertyFromElementOrSymbol(DatabaseIfc db, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IfcProperty propHnd = CreateCurrencyPropertyFromElement(db, elem, revitParameterName, ifcPropertyName, valueType);
         if (propHnd != null)
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateCurrencyPropertyFromElement(db, elem, builtInParamName, ifcPropertyName, valueType);
            if (propHnd != null)
               return propHnd;
         }

         // For Symbol
         Document document = elem.Document;
         ElementId typeId = elem.GetTypeId();
         Element elemType = document.GetElement(typeId);
         if (elemType != null)
            return CreateCurrencyPropertyFromElementOrSymbol(db, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType);
         else
            return null;
      }

      /// <summary>
      /// Create a ThermodynamicTemperature measure property from the element's or type's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateThermodynamicTemperaturePropertyFromElementOrSymbol(DatabaseIfc db, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IfcProperty propHnd = CreateThermodynamicTemperaturePropertyFromElement(db, elem, revitParameterName, ifcPropertyName, valueType);
         if (propHnd != null)
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateThermodynamicTemperaturePropertyFromElement(db, elem, builtInParamName, ifcPropertyName, valueType);
            if (propHnd != null)
               return propHnd;
         }

         // For Symbol
         Document document = elem.Document;
         ElementId typeId = elem.GetTypeId();
         Element elemType = document.GetElement(typeId);
         if (elemType != null)
            return CreateThermodynamicTemperaturePropertyFromElementOrSymbol(db, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType);
         else
            return null;
      }

      /// <summary>
      /// Create a VolumetricFlowRate measure property from the element's or type's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateVolumetricFlowRatePropertyFromElementOrSymbol(DatabaseIfc db, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IfcProperty propHnd = CreateVolumetricFlowRatePropertyFromElement(db, elem, revitParameterName, ifcPropertyName, valueType);
         if (propHnd != null)
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateVolumetricFlowRatePropertyFromElement(db, elem, builtInParamName, ifcPropertyName, valueType);
            if (propHnd != null)
               return propHnd;
         }

         // For Symbol
         Document document = elem.Document;
         ElementId typeId = elem.GetTypeId();
         Element elemType = document.GetElement(typeId);
         if (elemType != null)
            return CreateVolumetricFlowRatePropertyFromElementOrSymbol(db, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType);
         else
            return null;
      }

      /// <summary>
      /// Create an IfcClassificationReference property from the element's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateClassificationReferencePropertyFromElement(DatabaseIfc db, Element elem,
          string revitParameterName, string ifcPropertyName)
      {
         if (elem == null)
            return null;

         string propertyValue;
         if (ParameterUtil.GetStringValueFromElement(elem.Id, revitParameterName, out propertyValue) != null)
            return CreateClassificationReferenceProperty(db, ifcPropertyName, propertyValue);

         return null;
      }

      /// <summary>
      /// Create a generic measure property from the element's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="measureType">The IfcMeasure type of the property.</param>
      /// <param name="unitType">The unit type of the property.</param>
      /// <param name="valueType">The property value type of the property.</param>
      /// <returns>The created property handle.</returns>
      private static IfcProperty CreateDoublePropertyFromElement(DatabaseIfc db, Element elem,
          string revitParameterName, string ifcPropertyName, IfcDerivedMeasureValue value, UnitType unitType, PropertyValueType valueType)
      {
         double propertyValue;
         if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue) != null)
         {
            double scaledValue = UnitUtil.ScaleDouble(unitType, propertyValue);
            value.Measure = UnitUtil.ScaleDouble(unitType, propertyValue);
            return CreateCommonProperty(db, ifcPropertyName, value, valueType, null);
         }
         return null;
      }

      private static IfcProperty CreateDoublePropertyFromElement(DatabaseIfc db, Element elem,
          string revitParameterName, string ifcPropertyName, IfcMeasureValue value, UnitType unitType, PropertyValueType valueType)
      {
         double propertyValue;
         if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue) != null)
         {
            double scaledValue = UnitUtil.ScaleDouble(unitType, propertyValue);
            value.Measure = UnitUtil.ScaleDouble(unitType, propertyValue);
            return CreateCommonProperty(db, ifcPropertyName, value, valueType, null);
         }
         return null;
      }

      /// <summary>
      /// Create a Force measure property from the element's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateForcePropertyFromElement(DatabaseIfc db, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         return CreateDoublePropertyFromElement(db, elem, revitParameterName, ifcPropertyName,
             new IfcForceMeasure(0), UnitType.UT_Force, valueType);
      }

      /// <summary>
      /// Create a Power measure property from the element's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreatePowerPropertyFromElement(DatabaseIfc db, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         double propertyValue;
         Parameter powerParam = ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue);
         if (powerParam != null)
         {
            // We are going to do a little hack here which we will need to extend in a nice way. The built-in parameter corresponding
            // to "TotalWattage" is a string value in Revit that is likely going to be in the current units, and doesn't need to be scaled twice.
            bool needToScale = !(ifcPropertyName == "TotalWattage" && powerParam.StorageType == StorageType.String);
            double scaledpropertyValue = needToScale ? UnitUtil.ScalePower(propertyValue) : propertyValue;
            return CreatePowerPropertyFromCache(db, ifcPropertyName, scaledpropertyValue, valueType);
         }
         return null;
      }

      /// <summary>
      /// Create a Luminous flux measure property from the element's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateLuminousFluxMeasurePropertyFromElement(DatabaseIfc db, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         return CreateDoublePropertyFromElement(db, elem, revitParameterName, ifcPropertyName,
             new IfcLuminousFluxMeasure(1), UnitType.UT_Electrical_Luminous_Flux, valueType);
      }

      /// <summary>
      /// Create a Luminous intensity measure property from the element's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateLuminousIntensityMeasurePropertyFromElement(DatabaseIfc db, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         return CreateDoublePropertyFromElement(db, elem, revitParameterName, ifcPropertyName,
             new IfcLuminousIntensityMeasure(0), UnitType.UT_Electrical_Luminous_Intensity, valueType);
      }

      /// <summary>
      /// Create a illuminance measure property from the element's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateIlluminanceMeasurePropertyFromElement(DatabaseIfc db, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         return CreateDoublePropertyFromElement(db, elem, revitParameterName, ifcPropertyName,
             new IfcIlluminanceMeasure(0), UnitType.UT_Electrical_Illuminance, valueType);
      }

      /// <summary>
      /// Create a Mass density measure property from the element's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateMassDensityPropertyFromElement(DatabaseIfc db, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         return CreateDoublePropertyFromElement(db, elem, revitParameterName, ifcPropertyName,
             new IfcMassDensityMeasure(0), UnitType.UT_MassDensity, valueType);
      }

      /// <summary>
      /// Create a pressure measure property from the element's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreatePressurePropertyFromElement(DatabaseIfc db, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         return CreateDoublePropertyFromElement(db, elem, revitParameterName, ifcPropertyName,
             new IfcPressureMeasure(0), UnitType.UT_HVAC_Pressure, valueType);
      }

      /// <summary>
      /// Create a ThermalTransmittance measure property from the element's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateThermalTransmittancePropertyFromElement(DatabaseIfc db, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         double propertyValue;
         if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue) != null)
         {
            // TODO: scale!
            return CreateThermalTransmittancePropertyFromCache(db, ifcPropertyName, propertyValue, valueType);
         }
         return null;
      }

      /// <summary>
      /// Create an IfcClassificationReference property from the element's or type's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateClassificationReferencePropertyFromElementOrSymbol(DatabaseIfc db, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName)
      {
         IfcProperty propHnd = CreateClassificationReferencePropertyFromElement(db, elem, revitParameterName, ifcPropertyName);
         if (propHnd != null)
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateClassificationReferencePropertyFromElement(db, elem, builtInParamName, ifcPropertyName);
            if (propHnd != null)
               return propHnd;
         }

         // For Symbol
         Document document = elem.Document;
         ElementId typeId = elem.GetTypeId();
         Element elemType = document.GetElement(typeId);
         if (elemType != null)
            return CreateClassificationReferencePropertyFromElementOrSymbol(db, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName);
         else
            return null;
      }

      /// <summary>
      /// Create a Force measure property from the element's or type's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateForcePropertyFromElementOrSymbol(DatabaseIfc db, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IfcProperty propHnd = CreateForcePropertyFromElement(db, elem, revitParameterName, ifcPropertyName, valueType);
         if (propHnd != null)
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateForcePropertyFromElement(db, elem, builtInParamName, ifcPropertyName, valueType);
            if (propHnd != null)
               return propHnd;
         }

         // For Symbol
         Document document = elem.Document;
         ElementId typeId = elem.GetTypeId();
         Element elemType = document.GetElement(typeId);
         if (elemType != null)
            return CreateForcePropertyFromElementOrSymbol(db, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType);
         else
            return null;
      }

      /// <summary>
      /// Create a Power measure property from the element's or type's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreatePowerPropertyFromElementOrSymbol(DatabaseIfc db, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IfcProperty propHnd = CreatePowerPropertyFromElement(db, elem, revitParameterName, ifcPropertyName, valueType);
         if (propHnd != null)
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreatePowerPropertyFromElement(db, elem, builtInParamName, ifcPropertyName, valueType);
            if (propHnd != null)
               return propHnd;
         }

         // For Symbol
         Document document = elem.Document;
         ElementId typeId = elem.GetTypeId();
         Element elemType = document.GetElement(typeId);
         if (elemType != null)
            return CreatePowerPropertyFromElementOrSymbol(db, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType);
         else
            return null;
      }

      /// <summary>
      /// Create a Mass density measure property from the element's or type's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateMassDensityPropertyFromElementOrSymbol(DatabaseIfc db, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IfcProperty propHnd = CreateMassDensityPropertyFromElement(db, elem, revitParameterName, ifcPropertyName, valueType);
         if (propHnd != null)
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateMassDensityPropertyFromElement(db, elem, builtInParamName, ifcPropertyName, valueType);
            if (propHnd != null)
               return propHnd;
         }

         // For Symbol
         Document document = elem.Document;
         ElementId typeId = elem.GetTypeId();
         Element elemType = document.GetElement(typeId);
         if (elemType != null)
            return CreateMassDensityPropertyFromElementOrSymbol(db, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType);
         else
            return null;
      }
      
      /// <summary>
      /// Create a Luminous flux measure property from the element's or type's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateLuminousFluxMeasurePropertyFromElementOrSymbol(DatabaseIfc db, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IfcProperty propHnd = CreateLuminousFluxMeasurePropertyFromElement(db, elem, revitParameterName, ifcPropertyName, valueType);
         if (propHnd != null)
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateLuminousFluxMeasurePropertyFromElement(db, elem, builtInParamName, ifcPropertyName, valueType);
            if (propHnd != null)
               return propHnd;
         }

         // For Symbol
         Document document = elem.Document;
         ElementId typeId = elem.GetTypeId();
         Element elemType = document.GetElement(typeId);
         if (elemType != null)
            return CreateLuminousFluxMeasurePropertyFromElementOrSymbol(db, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType);
         else
            return null;
      }

      /// <summary>
      /// Create a Luminous intensity measure property from the element's or type's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateLuminousIntensityPropertyFromElementOrSymbol(DatabaseIfc db, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IfcProperty propHnd = CreateLuminousIntensityMeasurePropertyFromElement(db, elem, revitParameterName, ifcPropertyName, valueType);
         if (propHnd != null)
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateLuminousIntensityMeasurePropertyFromElement(db, elem, builtInParamName, ifcPropertyName, valueType);
            if (propHnd != null)
               return propHnd;
         }

         // For Symbol
         Document document = elem.Document;
         ElementId typeId = elem.GetTypeId();
         Element elemType = document.GetElement(typeId);
         if (elemType != null)
            return CreateLuminousIntensityPropertyFromElementOrSymbol(db, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType);
         else
            return null;
      }

      /// <summary>
      /// Create an illuminance measure property from the element's or type's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateIlluminancePropertyFromElementOrSymbol(DatabaseIfc db, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IfcProperty propHnd = CreateIlluminanceMeasurePropertyFromElement(db, elem, revitParameterName, ifcPropertyName, valueType);
         if (propHnd != null)
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateIlluminanceMeasurePropertyFromElement(db, elem, builtInParamName, ifcPropertyName, valueType);
            if (propHnd != null)
               return propHnd;
         }

         // For Symbol
         Document document = elem.Document;
         ElementId typeId = elem.GetTypeId();
         Element elemType = document.GetElement(typeId);
         if (elemType != null)
            return CreateIlluminancePropertyFromElementOrSymbol(db, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType);
         else
            return null;
      }

      /// <summary>
      /// Create a pressure measure property from the element's or type's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreatePressurePropertyFromElementOrSymbol(DatabaseIfc db, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IfcProperty propHnd = CreatePressurePropertyFromElement(db, elem, revitParameterName, ifcPropertyName, valueType);
         if (propHnd != null)
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreatePressurePropertyFromElement(db, elem, builtInParamName, ifcPropertyName, valueType);
            if (propHnd != null)
               return propHnd;
         }

         // For Symbol
         Document document = elem.Document;
         ElementId typeId = elem.GetTypeId();
         Element elemType = document.GetElement(typeId);
         if (elemType != null)
            return CreatePressurePropertyFromElementOrSymbol(db, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType);
         else
            return null;
      }

      /// <summary>
      /// Create a ThermalTransmittance measure property from the element's or type's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateThermalTransmittancePropertyFromElementOrSymbol(DatabaseIfc db, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IfcProperty propHnd = CreateThermalTransmittancePropertyFromElement(db, elem, revitParameterName, ifcPropertyName, valueType);
         if (propHnd != null)
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateThermalTransmittancePropertyFromElement(db, elem, builtInParamName, ifcPropertyName, valueType);
            if (propHnd != null)
               return propHnd;
         }

         // For Symbol
         Document document = elem.Document;
         ElementId typeId = elem.GetTypeId();
         Element elemType = document.GetElement(typeId);
         if (elemType != null)
            return CreateThermalTransmittancePropertyFromElementOrSymbol(db, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType);
         else
            return null;
      }

      /// <summary>
      /// Create a label property from the element's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <param name="propertyEnumerationType">The type of the enum, null if valueType isn't EnumeratedValue.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateLabelPropertyFromElement(DatabaseIfc db, Element elem, string revitParameterName, string ifcPropertyName,
          PropertyValueType valueType, Type propertyEnumerationType)
      {
         if (elem == null)
            return null;

         string propertyValue;
         Parameter parameter = ParameterUtil.GetStringValueFromElement(elem.Id, revitParameterName, out propertyValue);
         if (parameter != null)
            return CreateLabelPropertyFromCache(db, parameter.Id, ifcPropertyName, propertyValue, valueType, false, propertyEnumerationType);

         return null;
      }

      /// <summary>
      /// Create a label property from the element's or type's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <param name="propertyEnumerationType">The type of the enum, null if valueType isn't EnumeratedValue.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateLabelPropertyFromElementOrSymbol(DatabaseIfc db, Element elem, string revitParameterName,
         BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType, Type propertyEnumerationType)
      {
         // For Instance
         IfcProperty propHnd = CreateLabelPropertyFromElement(db, elem, revitParameterName, ifcPropertyName, valueType,
             propertyEnumerationType);
         if (propHnd != null)
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateLabelPropertyFromElement(db, elem, builtInParamName, ifcPropertyName, valueType, propertyEnumerationType);
            if (propHnd != null)
               return propHnd;
         }

         // For Symbol
         Document document = elem.Document;
         ElementId typeId = elem.GetTypeId();
         Element elemType = document.GetElement(typeId);
         if (elemType != null)
            return CreateLabelPropertyFromElementOrSymbol(db, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType,
                propertyEnumerationType);
         else
            return null;
      }

      /// <summary>
      /// Create an identifier property from the element's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateIdentifierPropertyFromElement(DatabaseIfc db, Element elem, string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         if (elem == null)
            return null;

         string propertyValue;
         if (ParameterUtil.GetStringValueFromElement(elem.Id, revitParameterName, out propertyValue) != null)
            return CreateIdentifierPropertyFromCache(db, ifcPropertyName, propertyValue, valueType);

         return null;
      }

      /// <summary>
      /// Create an identifier property from the element's or type's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateIdentifierPropertyFromElementOrSymbol(DatabaseIfc db, Element elem, string revitParameterName,
         BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         // For Instance
         IfcProperty propHnd = CreateIdentifierPropertyFromElement(db, elem, revitParameterName, ifcPropertyName, valueType);
         if (propHnd != null)
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateIdentifierPropertyFromElement(db, elem, builtInParamName, ifcPropertyName, valueType);
            if (propHnd != null)
               return propHnd;
         }

         // For Symbol
         Document document = elem.Document;
         ElementId typeId = elem.GetTypeId();
         Element elemType = document.GetElement(typeId);
         if (elemType != null)
            return CreateIdentifierPropertyFromElementOrSymbol(db, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType);
         else
            return null;
      }

      /// <summary>
      /// Create a boolean property from the element's or type's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.  Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateBooleanPropertyFromElementOrSymbol(DatabaseIfc db, Element elem,
         string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         int propertyValue;
         if (ParameterUtil.GetIntValueFromElement(elem, revitParameterName, out propertyValue) != null)
            return CreateBooleanPropertyFromCache(db, ifcPropertyName, propertyValue != 0, valueType);
         if (ParameterUtil.GetIntValueFromElement(elem, ifcPropertyName, out propertyValue) != null)
            return CreateBooleanPropertyFromCache(db, ifcPropertyName, propertyValue != 0, valueType);

         // For Symbol
         Document document = elem.Document;
         ElementId typeId = elem.GetTypeId();
         Element elemType = document.GetElement(typeId);
         if (elemType != null)
            return CreateBooleanPropertyFromElementOrSymbol(db, elemType, revitParameterName, ifcPropertyName, valueType);
         else
            return null;
      }

      /// <summary>
      /// Create a logical property from the element's or type's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateLogicalPropertyFromElementOrSymbol(DatabaseIfc db, Element elem,
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
               return CreateLogicalPropertyFromElementOrSymbol(db, elemType, revitParameterName, ifcPropertyName, valueType);
         }

         return CreateLogicalPropertyFromCache(db, ifcPropertyName, ifcLogical, valueType);
      }

      /// <summary>
      /// Create an integer property from the element's or type's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateIntegerPropertyFromElementOrSymbol(DatabaseIfc db, Element elem,
         string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         int propertyValue;
         if (ParameterUtil.GetIntValueFromElement(elem, revitParameterName, out propertyValue) != null)
            return CreateIntegerPropertyFromCache(db, ifcPropertyName, propertyValue, valueType);

         // For Symbol
         Document document = elem.Document;
         ElementId typeId = elem.GetTypeId();
         Element elemType = document.GetElement(typeId);
         if (elemType != null)
            return CreateIntegerPropertyFromElementOrSymbol(db, elemType, revitParameterName, ifcPropertyName, valueType);
         else
            return null;
      }

      /// <summary>Create a real property from the element's or type's parameter.</summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.  Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateRealPropertyFromElementOrSymbol(DatabaseIfc db, Element elem, string revitParameterName, string ifcPropertyName,
          PropertyValueType valueType)
      {
         double propertyValue;
         if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue) != null)
            return CreateRealPropertyFromCache(db, ifcPropertyName, propertyValue, valueType);
         if (ParameterUtil.GetDoubleValueFromElement(elem, null, ifcPropertyName, out propertyValue) != null)
            return CreateRealPropertyFromCache(db, ifcPropertyName, propertyValue, valueType);

         // For Symbol
         Document document = elem.Document;
         ElementId typeId = elem.GetTypeId();
         Element elemType = document.GetElement(typeId);
         if (elemType != null)
            return CreateRealPropertyFromElementOrSymbol(db, elemType, revitParameterName, ifcPropertyName, valueType);
         else
            return null;
      }

      /// <summary>
      /// Create a length property from the element's or type's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="builtInParameterName">The name of the built-in parameter, can be null.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateLengthMeasurePropertyFromElement(DatabaseIfc db, Element elem,
         string revitParameterName, string builtInParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         double propertyValue;

         if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue) != null)
            return CreateLengthMeasurePropertyFromCache(db, ifcPropertyName, UnitUtil.ScaleLength(propertyValue), valueType);

         if ((builtInParameterName != null) && (ParameterUtil.GetDoubleValueFromElement(elem, null, builtInParameterName, out propertyValue) != null))
            return CreateLengthMeasurePropertyFromCache(db, ifcPropertyName, UnitUtil.ScaleLength(propertyValue), valueType);

         return null;
      }

      /// <summary>
      /// Create a positive length property from the element's or type's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="builtInParameterName">The name of the built-in parameter, can be null.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreatePositiveLengthMeasurePropertyFromElement(DatabaseIfc db, Element elem,
         string revitParameterName, string builtInParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         double propertyValue;

         if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue) != null)
         {
            propertyValue = UnitUtil.ScaleLength(propertyValue);
            return CreatePositiveLengthMeasureProperty(db, ifcPropertyName, propertyValue, valueType);
         }

         if ((builtInParameterName != null) && (ParameterUtil.GetDoubleValueFromElement(elem, null, builtInParameterName, out propertyValue) != null))
         {
            propertyValue = UnitUtil.ScaleLength(propertyValue);
            return CreatePositiveLengthMeasureProperty(db, ifcPropertyName, propertyValue, valueType);
         }

         return null;
      }

      /// <summary>
      /// Create a length property from the element's or type's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The optional built-in parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateLengthMeasurePropertyFromElementOrSymbol(DatabaseIfc db, Element elem,
         string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         string builtInParamName = null;
         if (revitBuiltInParam != BuiltInParameter.INVALID)
            builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);

         IfcProperty propHnd = CreateLengthMeasurePropertyFromElement(db, elem, revitParameterName, builtInParamName, ifcPropertyName, valueType);
         if (propHnd != null)
            return propHnd;

         // For Symbol
         Document document = elem.Document;
         ElementId typeId = elem.GetTypeId();
         Element elemType = document.GetElement(typeId);
         if (elemType != null)
            return CreateLengthMeasurePropertyFromElement(db, elemType, revitParameterName, builtInParamName, ifcPropertyName, valueType);
         else
            return null;
      }

      /// <summary>
      /// Create a positive length property from the element's or type's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The optional built-in parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreatePositiveLengthMeasurePropertyFromElementOrSymbol(DatabaseIfc db, Element elem,
         string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         string builtInParamName = null;
         if (revitBuiltInParam != BuiltInParameter.INVALID)
            builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);

         IfcProperty propHnd = CreatePositiveLengthMeasurePropertyFromElement(db, elem, revitParameterName, builtInParamName, ifcPropertyName, valueType);
         if (propHnd != null)
            return propHnd;

         // For Symbol
         Document document = elem.Document;
         ElementId typeId = elem.GetTypeId();
         Element elemType = document.GetElement(typeId);
         if (elemType != null)
            return CreatePositiveLengthMeasurePropertyFromElement(db, elemType, revitParameterName, builtInParamName, ifcPropertyName, valueType);
         else
            return null;
      }

      /// <summary>
      /// Create a ratio property from the element's or type's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateRatioPropertyFromElementOrSymbol(DatabaseIfc db, Element elem,
         string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         double propertyValue;
         if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue) != null)
            return CreateRatioMeasureProperty(db, ifcPropertyName, propertyValue, valueType);

         // For Symbol
         Document document = elem.Document;
         ElementId typeId = elem.GetTypeId();
         Element elemType = document.GetElement(typeId);
         if (elemType == null)
            return null;

         return CreateRatioPropertyFromElementOrSymbol(db, elemType, revitParameterName, ifcPropertyName, valueType);
      }

      /// <summary>
      /// Create a normalised ratio property from the element's or type's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.  Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateNormalisedRatioPropertyFromElementOrSymbol(DatabaseIfc db, Element elem,
         string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         double propertyValue;
         if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue) != null)
            return CreateNormalisedRatioMeasureProperty(db, ifcPropertyName, propertyValue, valueType);
         if (ParameterUtil.GetDoubleValueFromElement(elem, null, ifcPropertyName, out propertyValue) != null)
            return CreateNormalisedRatioMeasureProperty(db, ifcPropertyName, propertyValue, valueType);

         // For Symbol
         Document document = elem.Document;
         ElementId typeId = elem.GetTypeId();
         Element elemType = document.GetElement(typeId);
         if (elemType == null)
            return null;

         return CreateNormalisedRatioPropertyFromElementOrSymbol(db, elemType, revitParameterName, ifcPropertyName, valueType);
      }

      private static IfcProperty CreateLinearVelocityPropertyFromElement(DatabaseIfc db, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         return CreateDoublePropertyFromElement(db, elem, revitParameterName, ifcPropertyName,
              new IfcLinearVelocityMeasure(0), UnitType.UT_HVAC_Velocity, valueType);
      }

      /// <summary>
      /// Create a linear velocity property from the element's or type's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.  Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateLinearVelocityPropertyFromElementOrSymbol(DatabaseIfc db, Element elem,
         string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {

         IfcProperty linearVelocityHandle = CreateLinearVelocityPropertyFromElement(db, elem, revitParameterName, ifcPropertyName, valueType);
         if (linearVelocityHandle != null)
            return linearVelocityHandle;

         // For Symbol
         Document document = elem.Document;
         ElementId typeId = elem.GetTypeId();
         Element elemType = document.GetElement(typeId);

         if (elemType == null)
            return null;
         else
            return CreateLinearVelocityPropertyFromElement(db, elemType, revitParameterName, ifcPropertyName, valueType);
      }

      /// <summary>
      /// Create a positive ratio property from the element's or type's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.  Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreatePositiveRatioPropertyFromElementOrSymbol(DatabaseIfc db, Element elem,
         string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         double propertyValue;
         if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue) != null)
            return CreatePositiveRatioMeasureProperty(db, ifcPropertyName, propertyValue, valueType);
         if (ParameterUtil.GetDoubleValueFromElement(elem, null, ifcPropertyName, out propertyValue) != null)
            return CreatePositiveRatioMeasureProperty(db, ifcPropertyName, propertyValue, valueType);

         // For Symbol
         Document document = elem.Document;
         ElementId typeId = elem.GetTypeId();
         Element elemType = document.GetElement(typeId);
         if (elemType == null)
            return null;

         return CreatePositiveRatioPropertyFromElementOrSymbol(db, elemType, revitParameterName, ifcPropertyName, valueType);
      }

      /// <summary>
      /// Create a plane angle measure property from the element's or type's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreatePlaneAngleMeasurePropertyFromElementOrSymbol(DatabaseIfc db, Element elem, string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         double propertyValue;
         if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue) != null)
         {
            propertyValue = UnitUtil.ScaleAngle(propertyValue);
            return CreatePlaneAngleMeasurePropertyFromCache(db, ifcPropertyName, propertyValue, valueType);
         }
         // For Symbol
         Document document = elem.Document;
         ElementId typeId = elem.GetTypeId();
         Element elemType = document.GetElement(typeId);
         if (elemType != null)
            return CreatePlaneAngleMeasurePropertyFromElementOrSymbol(db, elemType, revitParameterName, ifcPropertyName, valueType);
         else
            return null;
      }

      /// <summary>
      /// Create an area measure property from the element's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateAreaMeasurePropertyFromElement(DatabaseIfc db, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         double propertyValue;
         if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue) != null)
         {
            propertyValue = UnitUtil.ScaleArea(propertyValue);
            return CreateAreaMeasureProperty(db, ifcPropertyName, propertyValue, valueType);
         }
         return null;
      }

      /// <summary>
      /// Create an volume measure property from the element's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateVolumeMeasurePropertyFromElement(DatabaseIfc db, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         double propertyValue;
         if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue) != null)
         {
            propertyValue = UnitUtil.ScaleVolume(propertyValue);
            return CreateVolumeMeasureProperty(db, ifcPropertyName, propertyValue, valueType);
         }
         return null;
      }

      /// <summary>
      /// Create a count measure property from the element's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateCountMeasurePropertyFromElement(DatabaseIfc db, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         int propertyValue;
         double propertyValueReal;
         if (ParameterUtil.GetIntValueFromElement(elem, revitParameterName, out propertyValue) != null)
            return CreateCountMeasureProperty(db, ifcPropertyName, propertyValue, valueType);
         if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValueReal) != null)
            return CreateCountMeasureProperty(db, ifcPropertyName, propertyValueReal, valueType);
         return null;
      }

      /// <summary>
      /// Create an area measure property from the element's or type's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateAreaMeasurePropertyFromElementOrSymbol(DatabaseIfc db, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IfcProperty propHnd = CreateAreaMeasurePropertyFromElement(db, elem, revitParameterName, ifcPropertyName, valueType);
         if (propHnd != null)
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateAreaMeasurePropertyFromElement(db, elem, builtInParamName, ifcPropertyName, valueType);
            if (propHnd != null)
               return propHnd;
         }

         // For Symbol
         Document document = elem.Document;
         ElementId typeId = elem.GetTypeId();
         Element elemType = document.GetElement(typeId);
         if (elemType != null)
            return CreateAreaMeasurePropertyFromElementOrSymbol(db, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType);
         else
            return null;
      }

      /// <summary>
      /// Create an volume measure property from the element's or type's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateVolumeMeasurePropertyFromElementOrSymbol(DatabaseIfc db, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IfcProperty propHnd = CreateVolumeMeasurePropertyFromElement(db, elem, revitParameterName, ifcPropertyName, valueType);
         if (propHnd != null)
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateVolumeMeasurePropertyFromElement(db, elem, builtInParamName, ifcPropertyName, valueType);
            if (propHnd != null)
               return propHnd;
         }

         // For Symbol
         Document document = elem.Document;
         ElementId typeId = elem.GetTypeId();
         Element elemType = document.GetElement(typeId);
         if (elemType != null)
            return CreateVolumeMeasurePropertyFromElementOrSymbol(db, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType);
         else
            return null;
      }

      /// <summary>
      /// Create a count measure property from the element's or type's parameter.
      /// </summary>
      /// <param name="db">The IFC db.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IfcProperty CreateCountMeasurePropertyFromElementOrSymbol(DatabaseIfc db, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IfcProperty propHnd = CreateCountMeasurePropertyFromElement(db, elem, revitParameterName, ifcPropertyName, valueType);
         if (propHnd != null)
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateCountMeasurePropertyFromElement(db, elem, builtInParamName, ifcPropertyName, valueType);
            if (propHnd != null)
               return propHnd;
         }

         // For Symbol
         Document document = elem.Document;
         ElementId typeId = elem.GetTypeId();
         Element elemType = document.GetElement(typeId);
         if (elemType != null)
            return CreateCountMeasurePropertyFromElementOrSymbol(db, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType);
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
      /// <param name="geomObjects">The list of geometries for the exported column only, used if split walls and columns is set.</param>
      /// <remarks>The geomObjects is used if we have the split by level option.  It is intended only for columns, as beams and members are not split by level.  
      /// In this case, we use the solids in the list to determine the real volume of the exported objects. If the list contains meshes, we won't export the volume at all.</remarks>
      public static void CreateBeamColumnBaseQuantities(ExporterIFC exporterIFC, IfcElement elemHandle, Element element, FamilyTypeInfo typeInfo, IList<GeometryObject> geomObjects)
      {
         // Make sure QTO export is enabled.
         if (!ExporterCacheManager.ExportOptionsCache.ExportBaseQuantities || (ExporterCacheManager.ExportOptionsCache.ExportAsCOBIE))
            return;

         DatabaseIfc db = elemHandle.Database;
         HashSet<IfcPhysicalSimpleQuantity> quantityHnds = new HashSet<IfcPhysicalSimpleQuantity>();
         double scaledLength = typeInfo.ScaledDepth;
         double scaledArea = typeInfo.ScaledArea;
         double crossSectionArea = scaledArea;
         double scaledOuterPerimeter = typeInfo.ScaledOuterPerimeter;
         double scaledInnerPerimeter = typeInfo.ScaledInnerPerimeter;

         if (scaledLength > MathUtil.Eps())
         {
            IfcQuantityLength quantityHnd = new IfcQuantityLength(db, "Length", scaledLength);
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
            IfcQuantityArea quantityHnd = new IfcQuantityArea(db, "CrossSectionArea", crossSectionArea);
            quantityHnds.Add(quantityHnd);
         }

         if (!MathUtil.AreaIsAlmostZero(scaledArea) && !MathUtil.IsAlmostZero(scaledLength) && !MathUtil.IsAlmostZero(scaledOuterPerimeter))
         {
            double scaledPerimeter = scaledOuterPerimeter + scaledInnerPerimeter;
            double outSurfaceArea = scaledArea * 2 + scaledLength * scaledPerimeter;
            IfcQuantityArea quantityHnd = new IfcQuantityArea(db, "OuterSurfaceArea", outSurfaceArea);
            quantityHnds.Add(quantityHnd);
         }

         double volume = 0.0;
         if (element != null)
         {
            // If we are splitting columns, we will look at the actual geometry used when exporting this segment
            // of the column, but only if we have the geomObjects passed in.
            if (geomObjects != null && ExporterCacheManager.ExportOptionsCache.WallAndColumnSplitting)
            {
               foreach (GeometryObject geomObj in geomObjects)
               {
                  // We don't suport calculating the volume of Meshes at this time.
                  if (geomObj is Mesh)
                  {
                     volume = 0.0;
                     break;
                  }

                  if (geomObj is Solid)
                     volume += (geomObj as Solid).Volume;
               }
            }
            else
               ParameterUtil.GetDoubleValueFromElement(element, BuiltInParameter.HOST_VOLUME_COMPUTED, out volume);
            volume = UnitUtil.ScaleVolume(volume);
         }

         // If we didn't calculate the volume above, but we did pass in a non-zero scaled length and area, calculate the volume.
         if (MathUtil.VolumeIsAlmostZero(volume))
            volume = scaledLength * scaledArea;

         if (!MathUtil.VolumeIsAlmostZero(volume))
         {
            IfcQuantityVolume quantityHnd = new IfcQuantityVolume(db, "GrossVolume", volume);
            quantityHnds.Add(quantityHnd);
         }

            string quantitySetName = string.Empty;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
            {
                if (elemHandle is IfcColumn)
                    quantitySetName = "Qto_ColumnBaseQuantities";
                if (elemHandle is IfcBeam)
                    quantitySetName = "Qto_BeamBaseQuantities";
                if (elemHandle is IfcMember)
                    quantitySetName = "Qto_MemberBaseQuantities";
            }
            CreateAndRelateBaseQuantities(elemHandle, quantityHnds, quantitySetName);
      }

      /// <summary>
      /// Creates the spatial element quantities required by GSA before COBIE and adds them to the export.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="elemHnd">The element handle.</param>
      /// <param name="quantityName">The quantity name.</param>
      /// <param name="areaName">The area name.</param>
      /// <param name="area">The area.</param>
      public static void CreatePreCOBIEGSAQuantities(ExporterIFC exporterIFC, IfcElement elemHnd, string quantityName, string areaName, double area)
      {
         DatabaseIfc db = elemHnd.Database;
         IfcPhysicalSimpleQuantity areaQuantityHnd = new IfcQuantityArea(db, quantityName, area);
         IfcElementQuantity quantity = new IfcElementQuantity(quantityName, areaQuantityHnd);
         new IfcRelDefinesByProperties(elemHnd, quantity);
      }

      /// <summary>
      /// Creates the opening quantities and adds them to the export.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="openingElement">The opening element handle.</param>
      /// <param name="extraParams">The extrusion creation data.</param>
      public static void CreateOpeningQuantities(IfcOpeningElement openingElement, IFCExtrusionCreationData extraParams)
      {
         DatabaseIfc db = openingElement.Database;
         HashSet<IfcPhysicalSimpleQuantity> quantityHnds = new HashSet<IfcPhysicalSimpleQuantity>();
         if (extraParams.ScaledLength > MathUtil.Eps())
         {
            IfcPhysicalSimpleQuantity quantityHnd = new IfcQuantityLength(db, "Depth", extraParams.ScaledLength);
            quantityHnds.Add(quantityHnd);
         }
         if (extraParams.ScaledHeight > MathUtil.Eps())
         {
            IfcPhysicalSimpleQuantity quantityHnd = new IfcQuantityLength(db, "Height", extraParams.ScaledHeight);
            quantityHnds.Add(quantityHnd);
            quantityHnd = new IfcQuantityLength(db, "Width", extraParams.ScaledWidth);
            quantityHnds.Add(quantityHnd);
         }
         else if (extraParams.ScaledArea > MathUtil.Eps())
         {
            IfcPhysicalSimpleQuantity quantityHnd = new IfcQuantityArea(db, "Area", extraParams.ScaledArea);
            quantityHnds.Add(quantityHnd);
         }

         string quantitySetName = string.Empty;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            quantitySetName = "Qto_OpeningElementBaseQuantities";
         }
         CreateAndRelateBaseQuantities(openingElement, quantityHnds, quantitySetName);
      }

      /// <summary>
      /// Creates the wall base quantities and adds them to the export.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="wallElement">The wall element.</param>
      /// <param name="wallHnd">The wall handle.</param>
      /// <param name="solids">The list of solids for the entity created for the wall element.</param>
      /// <param name="meshes">The list of meshes for the entity created for the wall element.</param>
      /// <param name="scaledLength">The scaled length.</param>
      /// <param name="scaledDepth">The scaled depth.</param>
      /// <param name="scaledFootPrintArea">The scaled foot print area.</param>
      /// <remarks>If we are splitting walls by level, the list of solids and meshes represent the currently
      /// exported section of wall, not the entire wall.</remarks>
      public static void CreateWallBaseQuantities(Wall wallElement, List<Solid> solids, IList<Mesh> meshes,
          IfcWall wallHnd, double scaledLength, double scaledDepth, double scaledFootPrintArea)
      {
         DatabaseIfc db = wallHnd.Database;
         HashSet<IfcPhysicalSimpleQuantity> quantityHnds = new HashSet<IfcPhysicalSimpleQuantity>();
         if (scaledDepth > MathUtil.Eps())
         {
            IfcPhysicalSimpleQuantity quantityHnd = new IfcQuantityLength(db, "Height", scaledDepth);
            quantityHnds.Add(quantityHnd);
         }

         if (!MathUtil.IsAlmostZero(scaledLength))
         {
            IfcPhysicalSimpleQuantity quantityHnd = new IfcQuantityLength(db, "Length", scaledLength);
            quantityHnds.Add(quantityHnd);
         }

         double scaledWidth = UnitUtil.ScaleLength(wallElement.Width);
         if (!MathUtil.IsAlmostZero(scaledWidth))
         {
            IfcPhysicalSimpleQuantity quantityHnd = new IfcQuantityLength(db, "Width", scaledWidth);
            quantityHnds.Add(quantityHnd);
         }

         if (!MathUtil.IsAlmostZero(scaledFootPrintArea))
         {
            IfcPhysicalSimpleQuantity quantityHnd = new IfcQuantityArea(db, "GrossFootprintArea", scaledFootPrintArea);
            quantityHnds.Add(quantityHnd);
         }

         double area = 0;
         double volume = 0;

         if (ExporterCacheManager.ExportOptionsCache.WallAndColumnSplitting)
         {
            // We will only assign the area if we have all solids that we are exporting; we won't bother calcuting values for Meshes.
            if (solids != null && (meshes == null || meshes.Count == 0))
            {
               foreach (Solid solid in solids)
               {
                  area += solid.SurfaceArea;
                  volume += solid.Volume;
               }
            }
         }
         else
         {
            ParameterUtil.GetDoubleValueFromElement(wallElement, BuiltInParameter.HOST_AREA_COMPUTED, out area);
            ParameterUtil.GetDoubleValueFromElement(wallElement, BuiltInParameter.HOST_VOLUME_COMPUTED, out volume);
         }

         if (!MathUtil.IsAlmostZero(area))
         {
            area = UnitUtil.ScaleArea(area);
            IfcPhysicalSimpleQuantity quantityHnd = new IfcQuantityArea(db, "GrossSideArea", area);
            quantityHnds.Add(quantityHnd);
         }

         if (!MathUtil.IsAlmostZero(volume))
         {
            volume = UnitUtil.ScaleVolume(volume);
            IfcPhysicalSimpleQuantity quantityHnd = new IfcQuantityVolume(db, "GrossVolume", volume);
            quantityHnds.Add(quantityHnd);
         }

        string quantitySetName = string.Empty;
        if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
        {
            quantitySetName = "Qto_WallBaseQuantities";
        }

        CreateAndRelateBaseQuantities(wallHnd, quantityHnds, quantitySetName);
      }

      /// <summary>
      /// Creates and relate base quantities to quantity handle.
      /// </summary>
      /// <param name="db">The db.</param>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="elemHnd">The element handle.</param>
      /// <param name="quantityHnds">The quantity handles.</param>
      static void CreateAndRelateBaseQuantities(IfcElement elemHnd, HashSet<IfcPhysicalSimpleQuantity> quantityHnds, string quantitySetName = null)
      {
         if (quantityHnds.Count > 0)
         {
            if (string.IsNullOrEmpty(quantitySetName))
                quantitySetName = "BaseQuantities";
            IfcElementQuantity quantity = new IfcElementQuantity(quantitySetName, quantityHnds);
            new IfcRelDefinesByProperties(elemHnd, quantity);
         }
      }

      /// <summary>
      ///  Creates the shared beam, column and member QTO values.  
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="elemHandle">The element handle.</param>
      /// <param name="element">The element.</param>
      /// <param name="ecData">The IFCExtrusionCreationData containing the appropriate data.</param>
      public static void CreateBeamColumnMemberBaseQuantities(ExporterIFC exporterIFC, IfcElement elemHandle, Element element, IFCExtrusionCreationData ecData)
      {
         FamilyTypeInfo ifcTypeInfo = new FamilyTypeInfo();
         ifcTypeInfo.ScaledDepth = ecData.ScaledLength;
         ifcTypeInfo.ScaledArea = ecData.ScaledArea;
         ifcTypeInfo.ScaledInnerPerimeter = ecData.ScaledInnerPerimeter;
         ifcTypeInfo.ScaledOuterPerimeter = ecData.ScaledOuterPerimeter;
         CreateBeamColumnBaseQuantities(exporterIFC, elemHandle, element, ifcTypeInfo, null);
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
               string paramElementTypeName = (paramElementType != null) ? paramElementType.FamilyName : null;
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
      /// <param name="elementSets">The collection of IfcPropertys to relate properties to.</param>
      public static void CreateInternalRevitPropertySets(Element element, DatabaseIfc db, ISet<IfcObjectDefinition> elementSets)
      {
         if (element == null || !ExporterCacheManager.ExportOptionsCache.PropertySetOptions.ExportInternalRevit)
            return;

         // We will allow creating internal Revit property sets for element types with no associated element handles.
         if ((elementSets == null || elementSets.Count == 0) && !(element is ElementType))
            return;

         ElementId typeId = element.GetTypeId();
         Element elementType = element.Document.GetElement(typeId);
         int whichStart = elementType != null ? 0 : (element is ElementType ? 1 : 0);
         if (whichStart == 1)
         {
            typeId = element.Id;
            elementType = element as ElementType;
         }

         IDictionary<string, int> paramGroupNameToSubElemIndex = new Dictionary<string, int>();

         SortedDictionary<string, HashSet<IfcProperty>>[] propertySets;
         propertySets = new SortedDictionary<string, HashSet<IfcProperty>>[2];
         propertySets[0] = new SortedDictionary<string, HashSet<IfcProperty>>(StringComparer.InvariantCultureIgnoreCase);
         propertySets[1] = new SortedDictionary<string, HashSet<IfcProperty>>(StringComparer.InvariantCultureIgnoreCase);

         // pass through: element and element type.  If the element is a ElementType, there will only be one pass.
         for (int which = whichStart; which < 2; which++)
         {
            Element whichElement = (which == 0) ? element : elementType;
            if (whichElement == null)
               continue;

            ElementId whichElementId = whichElement.Id;

            bool createType = (which == 1);
            if (createType)
            {
               if (ExporterCacheManager.TypePropertyInfoCache.HasTypeProperties(typeId))
                  continue;
            }

            IDictionary<BuiltInParameterGroup, ParameterElementCache> parameterElementCache =
                ParameterUtil.GetNonIFCParametersForElement(whichElementId);
            if (parameterElementCache == null)
               continue;

            foreach (KeyValuePair<BuiltInParameterGroup, ParameterElementCache> parameterElementGroup in parameterElementCache)
            {
               BuiltInParameterGroup parameterGroup = parameterElementGroup.Key;
               string groupName = LabelUtils.GetLabelFor(parameterGroup);

               // We are only going to append the "(Type)" suffix if we aren't also exporting the corresponding entity type.
               // In general, we'd like to always export them entity type, regardles of whether it holds any geometry or not - it can hold
               // at least the parameteric information.  When this is acheived, when can get rid of this entirely.
               // Unfortunately, IFC2x3 doesn't have types for all entities, so for IFC2x3 at least this will continue to exist
               // in some fashion.
               // There was a suggestion in SourceForge that we could "merge" the instance/type property sets in the cases where we aren't
               // creating an entity type, and in the cases where two properties had the same name, use the instance over type.
               // However, given our intention to generally export all types, this seems like a lot of work for diminishing returns.
               if (which == 1 && ExporterCacheManager.ElementTypeToHandleCache.Find(whichElementId) == null)
                  groupName += Properties.Resources.PropertySetTypeSuffix;

               HashSet<IfcProperty> currPropertiesForGroup = new HashSet<IfcProperty>();
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
                              currPropertiesForGroup.Add(CreateBooleanPropertyFromCache(db, parameterCaption, value != 0, PropertyValueType.SingleValue));
                           }
                           else if (parameterDefinition.ParameterType == ParameterType.Invalid && (valueAsString != null))
                           {
                              // This is probably an internal enumerated type that should be exported as a string.
                              currPropertiesForGroup.Add(CreateIdentifierPropertyFromCache(db, parameterCaption, valueAsString, PropertyValueType.SingleValue));
                           }
                           else
                           {
                              currPropertiesForGroup.Add(CreateIntegerPropertyFromCache(db, parameterCaption, value, PropertyValueType.SingleValue));
                           }
                           break;
                        }
                     case StorageType.Double:
                        {
                           double value = parameter.AsDouble();
                           IfcProperty propertyHandle = null;
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
                                    propertyHandle = CreatePlaneAngleMeasurePropertyFromCache(db, parameterCaption,
                                        UnitUtil.ScaleAngle(value), PropertyValueType.SingleValue);
                                    break;
                                 }
                              case ParameterType.Area:
                              case ParameterType.HVACCrossSection:
                              case ParameterType.ReinforcementArea:
                              case ParameterType.SectionArea:
                              case ParameterType.SurfaceArea:
                                 {
                                    double scaledValue = UnitUtil.ScaleArea(value);
                                    propertyHandle = CreateAreaMeasureProperty(db, parameterCaption,
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
                                    propertyHandle = CreateLengthMeasurePropertyFromCache(db, parameterCaption,
                                        UnitUtil.ScaleLength(value), PropertyValueType.SingleValue);
                                    break;
                                 }
                              case ParameterType.ColorTemperature:
                                 {
                                    double scaledValue = UnitUtil.ScaleDouble(UnitType.UT_Color_Temperature, value);
                                    propertyHandle = CreateCommonProperty(db, parameterCaption, new IfcReal(scaledValue),
                                        PropertyValueType.SingleValue, "COLORTEMPERATURE");
                                    break;
                                 }
                              case ParameterType.Currency:
                                 {
                                    IfcValue currencyData = null;
                                    if (ExporterCacheManager.UnitsCache.ContainsKey("CURRENCY"))
                                       currencyData = new IfcMonetaryMeasure(value);
                                    else
                                       currencyData = new IfcReal(value);
                                    propertyHandle = CreateCommonProperty(db, parameterCaption, currencyData,
                                        PropertyValueType.SingleValue, null);
                                    break;
                                 }
                              case ParameterType.ElectricalApparentPower:
                              case ParameterType.ElectricalPower:
                              case ParameterType.ElectricalWattage:
                              case ParameterType.HVACPower:
                                 {
                                    double scaledValue = UnitUtil.ScalePower(value);
                                    propertyHandle = CreatePowerProperty(db, parameterCaption,
                                        scaledValue, PropertyValueType.SingleValue);
                                    break;
                                 }
                              case ParameterType.ElectricalCurrent:
                                 {
                                    double scaledValue = UnitUtil.ScaleElectricalCurrent(value);
                                    propertyHandle = ElectricalCurrentPropertyUtil.CreateElectricalCurrentMeasureProperty(db, parameterCaption,
                                        scaledValue, PropertyValueType.SingleValue);
                                    break;
                                 }
                              case ParameterType.ElectricalEfficacy:
                                 {
                                    double scaledValue = UnitUtil.ScaleDouble(UnitType.UT_Electrical_Efficacy, value);
                                    propertyHandle = CreateCommonProperty(db, parameterCaption, new IfcReal(scaledValue),
                                        PropertyValueType.SingleValue, "LUMINOUSEFFICACY");
                                    break;
                                 }
                              case ParameterType.ElectricalFrequency:
                                 {
                                    propertyHandle = FrequencyPropertyUtil.CreateFrequencyProperty(db, parameterCaption,
                                        value, PropertyValueType.SingleValue);
                                    break;
                                 }
                              case ParameterType.ElectricalIlluminance:
                                 {
                                    double scaledValue = UnitUtil.ScaleIlluminance(value);
                                    propertyHandle = CreateIlluminanceProperty(db, parameterCaption,
                                        scaledValue, PropertyValueType.SingleValue);
                                    break;
                                 }
                              case ParameterType.ElectricalLuminousFlux:
                                 {
                                    double scaledValue = UnitUtil.ScaleLuminousFlux(value);
                                    propertyHandle = CreateLuminousFluxMeasureProperty(db, parameterCaption,
                                        scaledValue, PropertyValueType.SingleValue);
                                    break;
                                 }
                              case ParameterType.ElectricalLuminousIntensity:
                                 {
                                    double scaledValue = UnitUtil.ScaleLuminousIntensity(value);
                                    propertyHandle = CreateLuminousIntensityProperty(db, parameterCaption,
                                        scaledValue, PropertyValueType.SingleValue);
                                    break;
                                 }
                              case ParameterType.ElectricalPotential:
                                 {
                                    double scaledValue = UnitUtil.ScaleElectricalVoltage(value);
                                    propertyHandle = ElectricalVoltagePropertyUtil.CreateElectricalVoltageMeasureProperty(db, parameterCaption,
                                        scaledValue, PropertyValueType.SingleValue);
                                    break;
                                 }
                              case ParameterType.ElectricalTemperature:
                              case ParameterType.HVACTemperature:
                              case ParameterType.PipingTemperature:
                                 {
                                    double scaledValue = UnitUtil.ScaleDouble(UnitType.UT_HVAC_Temperature, value);
                                    propertyHandle = CreateCommonProperty(db, parameterCaption, new IfcThermalTransmittanceMeasure(scaledValue),
                                        PropertyValueType.SingleValue, null);
                                    break;
                                 }
                              case ParameterType.Force:
                                 {
                                    double scaledValue = UnitUtil.ScaleForce(value);
                                    propertyHandle = CreateForceProperty(db, parameterCaption,
                                        scaledValue, PropertyValueType.SingleValue);
                                    break;
                                 }
                              case ParameterType.HVACAirflow:
                              case ParameterType.PipingFlow:
                                 {
                                    double scaledValue = UnitUtil.ScaleVolumetricFlowRate(value);
                                    propertyHandle = CreateVolumetricFlowRateMeasureProperty(db, parameterCaption,
                                        scaledValue, PropertyValueType.SingleValue);
                                    break;
                                 }
                              case ParameterType.HVACFriction:
                                 {
                                    double scaledValue = UnitUtil.ScaleDouble(UnitType.UT_HVAC_Friction, value);
                                    propertyHandle = CreateCommonProperty(db, parameterCaption, new IfcReal(scaledValue),
                                        PropertyValueType.SingleValue, "FRICTIONLOSS");
                                    break;
                                 }
                              case ParameterType.HVACPressure:
                              case ParameterType.PipingPressure:
                              case ParameterType.Stress:
                                 {
                                    double scaledValue = UnitUtil.ScaleDouble(UnitType.UT_HVAC_Pressure, value);
                                    propertyHandle = CreateCommonProperty(db, parameterCaption, new IfcPressureMeasure(scaledValue),
                                        PropertyValueType.SingleValue, null);
                                    break;
                                 }
                              case ParameterType.HVACVelocity:
                              case ParameterType.PipingVelocity:
                                 {
                                    double scaledValue = UnitUtil.ScaleDouble(UnitType.UT_HVAC_Velocity, value);
                                    propertyHandle = CreateCommonProperty(db, parameterCaption, new IfcLinearVelocityMeasure(scaledValue),
                                        PropertyValueType.SingleValue, null);
                                    break;
                                 }
                              case ParameterType.MassDensity:
                                 {
                                    double scaledValue = UnitUtil.ScaleDouble(UnitType.UT_MassDensity, value);
                                    propertyHandle = CreateCommonProperty(db, parameterCaption, new IfcMassDensityMeasure(scaledValue),
                                        PropertyValueType.SingleValue, null);
                                    break;
                                 }
                              case ParameterType.PipingVolume:
                              case ParameterType.ReinforcementVolume:
                              case ParameterType.SectionModulus:
                              case ParameterType.Volume:
                                 {
                                    double scaledValue = UnitUtil.ScaleVolume(value);
                                    propertyHandle = CreateVolumeMeasureProperty(db, parameterCaption,
                                        scaledValue, PropertyValueType.SingleValue);
                                    break;
                                 }
                              default:
                                 assigned = false;
                                 break;
                           }

                           if (!assigned)
                              propertyHandle = CreateRealPropertyFromCache(db, parameterCaption, value, PropertyValueType.SingleValue);

                           if (propertyHandle != null)
                              currPropertiesForGroup.Add(propertyHandle);
                           break;
                        }
                     case StorageType.String:
                        {
                           string value = parameter.AsString();

                           currPropertiesForGroup.Add(CreateTextPropertyFromCache(db, parameterCaption, value, PropertyValueType.SingleValue));
                           break;
                        }
                     case StorageType.ElementId:
                        {
                           ElementId value = parameter.AsElementId();
                           if (value == ElementId.InvalidElementId)
                              continue;

                           string valueString = ElementIdParameterAsString(parameter);
                           currPropertiesForGroup.Add(CreateLabelPropertyFromCache(db, parameter.Id, parameterCaption, valueString, PropertyValueType.SingleValue, true, null));
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

            HashSet<IfcPropertySet> typePropertySets = new HashSet<IfcPropertySet>();

            int size = propertySets[which].Count;
            if (size == 0)
            {
               ExporterCacheManager.TypePropertyInfoCache.AddNewElementHandles(typeId, elementSets);
               continue;
            }

            foreach (KeyValuePair<string, HashSet<IfcProperty>> currPropertySet in propertySets[which])
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

               IfcPropertySet propertySet = new IfcPropertySet(currPropertySet.Key, currPropertySet.Value) { GlobalId = psetGUID };

               if (which == 1)
                  typePropertySets.Add(propertySet);
               else
               {
                  new IfcRelDefinesByProperties(elementSets, propertySet) { GlobalId = psetRelGUID };
               }
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
          HashSet<IfcPropertySet> existingPropertySets, IfcTypeProduct prodTypeHnd)
      {
         HashSet<IfcPropertySet> propertySets = new HashSet<IfcPropertySet>();

         // Pass in an empty set of handles - we don't want IfcRelDefinesByProperties for type properties.
         ISet<IfcObjectDefinition> associatedObjectIds = new HashSet<IfcObjectDefinition>();
         PropertyUtil.CreateInternalRevitPropertySets(elementType, prodTypeHnd.Database, associatedObjectIds);

         TypePropertyInfo additionalPropertySets = null;
         if (ExporterCacheManager.TypePropertyInfoCache.TryGetValue(elementType.Id, out additionalPropertySets))
            propertySets.UnionWith(additionalPropertySets.PropertySets);

         if (existingPropertySets != null && existingPropertySets.Count > 0)
            propertySets.UnionWith(existingPropertySets);

         DatabaseIfc db = prodTypeHnd.Database;
         IFCFile file = exporterIFC.GetFile();
         using (IFCTransaction transaction = new IFCTransaction(file))
         {
            Document doc = elementType.Document;

            IList<IList<PropertySetDescription>> psetsToCreate = ExporterCacheManager.ParameterCache.PropertySets;

            IList<PropertySetDescription> currPsetsToCreate = ExporterUtil.GetCurrPSetsToCreate(prodTypeHnd, psetsToCreate);
            foreach (PropertySetDescription currDesc in currPsetsToCreate)
            {
               ISet<IfcProperty> props = currDesc.ProcessEntries(null, elementType, elementType, prodTypeHnd);
               if (props.Count > 0)
               {
                  int subElementIndex = currDesc.SubElementIndex;
                  string guid = GUIDUtil.CreateSubElementGUID(elementType, subElementIndex);

                  string paramSetName = currDesc.Name;
                  IfcPropertySet propertySet = new IfcPropertySet(paramSetName, props) { GlobalId = guid };
                  propertySets.Add(propertySet);
               }
            }

            if (propertySets.Count != 0)
            {
               foreach (IfcPropertySet pset in propertySets)
                  prodTypeHnd.AddPropertySet(pset);
               // Don't assign the property sets to the instances if we have just assigned them to the type.
               if (additionalPropertySets != null)
                  additionalPropertySets.AssignedToType = true;
            }

            transaction.Commit();
         }
      }
   }
}