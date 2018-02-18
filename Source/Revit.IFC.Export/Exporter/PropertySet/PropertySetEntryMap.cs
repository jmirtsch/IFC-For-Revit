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
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Utility;

namespace Revit.IFC.Export.Exporter.PropertySet
{
   /// <summary>
   /// Represents a mapping from a Revit parameter or calculated quantity to an IFC property.
   /// </summary>
   public class PropertySetEntryMap : EntryMap
   {
      public PropertySetEntryMap() { }
      /// <summary>
      /// Constructs a PropertySetEntry object.
      /// </summary>
      /// <param name="revitParameterName">
      /// Revit parameter name.
      /// </param>
      public PropertySetEntryMap(string revitParameterName)
          : base(revitParameterName)
      {

      }

      public PropertySetEntryMap(PropertyCalculator calculator)
           : base(calculator)
      {

      }
      public PropertySetEntryMap(string revitParameterName, BuiltInParameter builtInParameter)
       : base(revitParameterName, builtInParameter)
      {

      }
	   
      /// <summary>
      /// Process to create element property.
      /// </summary>
      /// <param name="file">
      /// The IFC file.
      /// </param>
      /// <param name="exporterIFC">
      /// The ExporterIFC object.
      /// </param>
      /// <param name="extrusionCreationData">
      /// The IFCExtrusionCreationData.
      /// </param>
      /// <param name="element">
      /// The element of which this property is created for.
      /// </param>
      /// <param name="elementType">
      /// The element type of which this property is created for.
      /// </param>
      /// <param name="handle">
      /// The handle for which this property is created for.
      /// </param>
      /// <returns>
      /// Then created property handle.
      /// </returns>
      public IFCAnyHandle ProcessEntry(IFCFile file, ExporterIFC exporterIFC, IFCExtrusionCreationData extrusionCreationData, Element element,
        ElementType elementType, IFCAnyHandle handle, PropertyType propertyType, PropertyValueType valueType, Type propertyEnumerationType, string propertyName)
      {
         IFCAnyHandle propHnd = null;

         if (ParameterNameIsValid)
         {
            propHnd = CreatePropertyFromElementOrSymbol(file, exporterIFC, element, propertyType, valueType, propertyEnumerationType, propertyName);
         }

         if (IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
         {
            propHnd = CreatePropertyFromCalculator(file, exporterIFC, extrusionCreationData, element, elementType, handle, propertyType, valueType, propertyEnumerationType, propertyName);
         }
         return propHnd;
      }

      // This function is static to make sure that no properties are used directly from the entry.
      private static IFCAnyHandle CreatePropertyFromElementOrSymbolBase(IFCFile file, ExporterIFC exporterIFC, Element element,
          string revitParamNameToUse, string ifcPropertyName, BuiltInParameter builtInParameter,
          PropertyType propertyType, PropertyValueType valueType, Type propertyEnumerationType)
      {
         IFCAnyHandle propHnd = null;

         switch (propertyType)
         {
            case PropertyType.Text:
               {
                  propHnd = PropertyUtil.CreateTextPropertyFromElementOrSymbol(file, element, revitParamNameToUse, builtInParameter, ifcPropertyName, valueType, propertyEnumerationType);
                  break;
               }
            case PropertyType.Label:
               {
                  propHnd = PropertyUtil.CreateLabelPropertyFromElementOrSymbol(file, element, revitParamNameToUse, builtInParameter, ifcPropertyName, valueType, propertyEnumerationType);
                  break;
               }
            case PropertyType.Identifier:
               {
                  propHnd = PropertyUtil.CreateIdentifierPropertyFromElementOrSymbol(file, element, revitParamNameToUse, builtInParameter, ifcPropertyName, valueType);
                  break;
               }
            case PropertyType.Boolean:
               {
                  propHnd = PropertyUtil.CreateBooleanPropertyFromElementOrSymbol(file, element, revitParamNameToUse, ifcPropertyName, valueType);
                  break;
               }
            case PropertyType.Logical:
               {
                  propHnd = PropertyUtil.CreateLogicalPropertyFromElementOrSymbol(file, element, revitParamNameToUse, ifcPropertyName, valueType);
                  break;
               }
            case PropertyType.Integer:
               {
                  propHnd = PropertyUtil.CreateIntegerPropertyFromElementOrSymbol(file, element, revitParamNameToUse, ifcPropertyName, valueType);
                  break;
               }
            case PropertyType.Real:
               {
                  propHnd = PropertyUtil.CreateRealPropertyFromElementOrSymbol(file, element, revitParamNameToUse, ifcPropertyName, valueType);
                  break;
               }
            case PropertyType.Length:
               {
                  propHnd = PropertyUtil.CreateLengthMeasurePropertyFromElementOrSymbol(file, exporterIFC, element, revitParamNameToUse,
                      builtInParameter, ifcPropertyName, valueType);
                  break;
               }
            case PropertyType.PositiveLength:
               {
                  propHnd = PropertyUtil.CreatePositiveLengthMeasurePropertyFromElementOrSymbol(file, exporterIFC, element, revitParamNameToUse,
                      builtInParameter, ifcPropertyName, valueType);
                  break;
               }
            case PropertyType.NormalisedRatio:
               {
                  propHnd = PropertyUtil.CreateNormalisedRatioPropertyFromElementOrSymbol(file, exporterIFC, element, revitParamNameToUse,
                      ifcPropertyName, valueType);
                  break;
               }
            case PropertyType.PositiveRatio:
               {
                  propHnd = PropertyUtil.CreatePositiveRatioPropertyFromElementOrSymbol(file, exporterIFC, element, revitParamNameToUse,
                      ifcPropertyName, valueType);
                  break;
               }
            case PropertyType.Ratio:
               {
                  propHnd = PropertyUtil.CreateRatioPropertyFromElementOrSymbol(file, exporterIFC, element, revitParamNameToUse, ifcPropertyName,
                      valueType);
                  break;
               }
            case PropertyType.PlaneAngle:
               {
                  propHnd = PropertyUtil.CreatePlaneAngleMeasurePropertyFromElementOrSymbol(file, element, revitParamNameToUse, ifcPropertyName,
                      valueType);
                  break;
               }
            case PropertyType.PositivePlaneAngle:
               {
                  propHnd = PositivePlaneAnglePropertyUtil.CreatePositivePlaneAngleMeasurePropertyFromElementOrSymbol(file, element, revitParamNameToUse, ifcPropertyName,
                      valueType);
                  break;
               }
            case PropertyType.Area:
               {
                  propHnd = PropertyUtil.CreateAreaMeasurePropertyFromElementOrSymbol(file, exporterIFC, element, revitParamNameToUse,
                      builtInParameter, ifcPropertyName, valueType);
                  break;
               }
            case PropertyType.Volume:
               {
                  propHnd = PropertyUtil.CreateVolumeMeasurePropertyFromElementOrSymbol(file, exporterIFC, element, revitParamNameToUse,
                      builtInParameter, ifcPropertyName, valueType);
                  break;
               }
            case PropertyType.Count:
               {
                  propHnd = PropertyUtil.CreateCountMeasurePropertyFromElementOrSymbol(file, exporterIFC, element, revitParamNameToUse,
                      builtInParameter, ifcPropertyName, valueType);
                  break;
               }
            case PropertyType.Frequency:
               {
                  propHnd = FrequencyPropertyUtil.CreateFrequencyPropertyFromElementOrSymbol(file, exporterIFC, element, revitParamNameToUse, builtInParameter,
                      ifcPropertyName, valueType);
                  break;
               }
            case PropertyType.ElectricalCurrent:
               {
                  propHnd = ElectricalCurrentPropertyUtil.CreateElectricalCurrentMeasurePropertyFromElementOrSymbol(file, element, revitParamNameToUse, ifcPropertyName, valueType);
                  break;
               }
            case PropertyType.ElectricalVoltage:
               {
                  propHnd = ElectricalVoltagePropertyUtil.CreateElectricalVoltageMeasurePropertyFromElementOrSymbol(file, element, revitParamNameToUse, ifcPropertyName, valueType);
                  break;
               }
            case PropertyType.LuminousFlux:
               {
                  propHnd = PropertyUtil.CreateLuminousFluxMeasurePropertyFromElementOrSymbol(file, exporterIFC, element, revitParamNameToUse,
                      builtInParameter, ifcPropertyName, valueType);
                  break;
               }
            case PropertyType.Force:
               {
                  propHnd = FrequencyPropertyUtil.CreateForcePropertyFromElementOrSymbol(file, exporterIFC, element, revitParamNameToUse, builtInParameter,
                      ifcPropertyName, valueType);
                  break;
               }
            case PropertyType.Pressure:
               {
                  propHnd = FrequencyPropertyUtil.CreatePressurePropertyFromElementOrSymbol(file, exporterIFC, element, revitParamNameToUse, builtInParameter,
                      ifcPropertyName, valueType);
                  break;
               }
            case PropertyType.ColorTemperature:
               {
                  propHnd = PropertyUtil.CreateColorTemperaturePropertyFromElementOrSymbol(file, exporterIFC, element, revitParamNameToUse, builtInParameter,
                      ifcPropertyName, valueType);
                  break;
               }
            case PropertyType.Currency:
               {
                  propHnd = PropertyUtil.CreateCurrencyPropertyFromElementOrSymbol(file, exporterIFC, element, revitParamNameToUse, builtInParameter,
                      ifcPropertyName, valueType);
                  break;
               }
            case PropertyType.ElectricalEfficacy:
               {
                  propHnd = PropertyUtil.CreateElectricalEfficacyPropertyFromElementOrSymbol(file, exporterIFC, element, revitParamNameToUse, builtInParameter,
                      ifcPropertyName, valueType);
                  break;
               }
            case PropertyType.LuminousIntensity:
               {
                  propHnd = PropertyUtil.CreateLuminousIntensityPropertyFromElementOrSymbol(file, exporterIFC, element, revitParamNameToUse, builtInParameter,
                      ifcPropertyName, valueType);
                  break;
               }
            case PropertyType.MassDensity:
               {
                  propHnd = PropertyUtil.CreateMassDensityPropertyFromElementOrSymbol(file, exporterIFC, element, revitParamNameToUse, builtInParameter,
                      ifcPropertyName, valueType);
                  break;
               }

            case PropertyType.Illuminance:
               {
                  propHnd = PropertyUtil.CreateIlluminancePropertyFromElementOrSymbol(file, exporterIFC, element, revitParamNameToUse, builtInParameter,
                      ifcPropertyName, valueType);
                  break;
               }
            case PropertyType.Power:
               {
                  propHnd = PropertyUtil.CreatePowerPropertyFromElementOrSymbol(file, exporterIFC, element, revitParamNameToUse, builtInParameter,
                      ifcPropertyName, valueType);
                  break;
               }
            case PropertyType.ThermodynamicTemperature:
               {
                  propHnd = PropertyUtil.CreateThermodynamicTemperaturePropertyFromElementOrSymbol(file, exporterIFC, element, revitParamNameToUse,
                      builtInParameter, ifcPropertyName, valueType);
                  break;
               }
            case PropertyType.ThermalTransmittance:
               {
                  propHnd = PropertyUtil.CreateThermalTransmittancePropertyFromElementOrSymbol(file, exporterIFC, element, revitParamNameToUse,
                      builtInParameter, ifcPropertyName, valueType);
                  break;
               }
            case PropertyType.VolumetricFlowRate:
               {
                  propHnd = PropertyUtil.CreateVolumetricFlowRatePropertyFromElementOrSymbol(file, exporterIFC, element, revitParamNameToUse,
                      builtInParameter, ifcPropertyName, valueType);
                  break;
               }
            case PropertyType.ClassificationReference:
               {
                  propHnd = PropertyUtil.CreateClassificationReferencePropertyFromElementOrSymbol(file, exporterIFC, element, revitParamNameToUse,
                      builtInParameter, ifcPropertyName);
                  break;
               }
            case PropertyType.LinearVelocity:
               {
                  propHnd = PropertyUtil.CreateLinearVelocityPropertyFromElementOrSymbol(file, exporterIFC, element, revitParamNameToUse,
                      ifcPropertyName, valueType);
                  break;
               }
            default:
               throw new InvalidOperationException();
         }

         return propHnd;
      }

      /// <summary>
      /// Creates a property from element or its type's parameter.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="element">The element.</param>
      /// <returns>The property handle.</returns>
      IFCAnyHandle CreatePropertyFromElementOrSymbol(IFCFile file, ExporterIFC exporterIFC, Element element, PropertyType propertyType, PropertyValueType valueType, Type propertyEnumerationType, string propertyName)
      {
         string localizedRevitParameterName = LocalizedRevitParameterName(ExporterCacheManager.LanguageType);
         string revitParameterName = RevitParameterName;

         IFCAnyHandle propHnd = null;
         if (localizedRevitParameterName != null)
         {
            propHnd = PropertySetEntryMap.CreatePropertyFromElementOrSymbolBase(file, exporterIFC, element,
                 localizedRevitParameterName, propertyName, BuiltInParameter.INVALID,
                 propertyType, valueType, propertyEnumerationType);
         }

         if (IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
         {
            propHnd = PropertySetEntryMap.CreatePropertyFromElementOrSymbolBase(file, exporterIFC, element,
                 revitParameterName, propertyName, RevitBuiltInParameter,
                 propertyType, valueType, propertyEnumerationType);
         }
         return propHnd;
      }


      /// <summary>
      /// Creates a property from the calculator.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="extrusionCreationData">The IFCExtrusionCreationData.</param>
      /// <param name="element">The element.</param>
      /// <param name="elementType">The element type.</param>
      /// <param name="handle">The handle for which we calculate the property.</param>
      /// <returns>The property handle.</returns>
      IFCAnyHandle CreatePropertyFromCalculator(IFCFile file, ExporterIFC exporterIFC, IFCExtrusionCreationData extrusionCreationData, Element element,
            ElementType elementType, IFCAnyHandle handle, PropertyType propertyType, PropertyValueType valueType, Type propertyEnumerationType, string propertyName)
      {
         IFCAnyHandle propHnd = null;

         if (PropertyCalculator == null)
            return propHnd;

         if (PropertyCalculator.GetParameterFromSubelementCache(element, handle) ||
             PropertyCalculator.Calculate(exporterIFC, extrusionCreationData, element, elementType))
         {

            switch (propertyType)
            {
               case PropertyType.Label:
                  {
                     if (PropertyCalculator.CalculatesMutipleValues)
                        propHnd = PropertyUtil.CreateLabelProperty(file, propertyName, PropertyCalculator.GetStringValues(), valueType, propertyEnumerationType);
                     else
                     {
                        bool cacheLabel = PropertyCalculator.CacheStringValues;
                        propHnd = PropertyUtil.CreateLabelPropertyFromCache(file, null, propertyName, PropertyCalculator.GetStringValue(), valueType, cacheLabel, propertyEnumerationType);
                     }
                     break;
                  }
               case PropertyType.Text:
                  {
                     propHnd = PropertyUtil.CreateTextPropertyFromCache(file, propertyName, PropertyCalculator.GetStringValue(), valueType);
                     break;
                  }
               case PropertyType.Identifier:
                  {
                     propHnd = PropertyUtil.CreateIdentifierPropertyFromCache(file, propertyName, PropertyCalculator.GetStringValue(), valueType);
                     break;
                  }
               case PropertyType.Boolean:
                  {
                     propHnd = PropertyUtil.CreateBooleanPropertyFromCache(file, propertyName, PropertyCalculator.GetBooleanValue(), valueType);
                     break;
                  }
               case PropertyType.Logical:
                  {
                     propHnd = PropertyUtil.CreateLogicalPropertyFromCache(file, propertyName, PropertyCalculator.GetLogicalValue(), valueType);
                     break;
                  }
               case PropertyType.Integer:
                  {
                     if (PropertyCalculator.CalculatesMultipleParameters)
                        propHnd = PropertyUtil.CreateIntegerPropertyFromCache(file, propertyName, PropertyCalculator.GetIntValue(propertyName), valueType);
                     else
                        propHnd = PropertyUtil.CreateIntegerPropertyFromCache(file, propertyName, PropertyCalculator.GetIntValue(), valueType);
                     break;
                  }
               case PropertyType.Real:
                  {
                     propHnd = PropertyUtil.CreateRealPropertyFromCache(file, propertyName, PropertyCalculator.GetDoubleValue(), valueType);
                     break;
                  }
               case PropertyType.Length:
                  {
                     if (PropertyCalculator.CalculatesMultipleParameters)
                        propHnd = PropertyUtil.CreateLengthMeasurePropertyFromCache(file, propertyName, PropertyCalculator.GetDoubleValue(propertyName), valueType);
                     else
                        propHnd = PropertyUtil.CreateLengthMeasurePropertyFromCache(file, propertyName, PropertyCalculator.GetDoubleValue(), valueType);
                     break;
                  }
               case PropertyType.PositiveLength:
                  {
                     if (PropertyCalculator.CalculatesMultipleParameters)
                        propHnd = PropertyUtil.CreatePositiveLengthMeasureProperty(file, propertyName, PropertyCalculator.GetDoubleValue(propertyName), valueType);
                     else
                        propHnd = PropertyUtil.CreatePositiveLengthMeasureProperty(file, propertyName, PropertyCalculator.GetDoubleValue(), valueType);
                     break;
                  }
               case PropertyType.NormalisedRatio:
                  {
                     propHnd = PropertyUtil.CreateNormalisedRatioMeasureProperty(file, propertyName, PropertyCalculator.GetDoubleValue(), valueType);
                     break;
                  }
               case PropertyType.PositiveRatio:
                  {
                     propHnd = PropertyUtil.CreatePositiveRatioMeasureProperty(file, propertyName, PropertyCalculator.GetDoubleValue(), valueType);
                     break;
                  }
               case PropertyType.Ratio:
                  {
                     propHnd = PropertyUtil.CreateRatioMeasureProperty(file, propertyName, PropertyCalculator.GetDoubleValue(), valueType);
                     break;
                  }
               case PropertyType.PlaneAngle:
                  {
                     propHnd = PropertyUtil.CreatePlaneAngleMeasurePropertyFromCache(file, propertyName, PropertyCalculator.GetDoubleValue(), valueType);
                     break;
                  }
               case PropertyType.PositivePlaneAngle:
                  {
                     propHnd = PositivePlaneAnglePropertyUtil.CreatePositivePlaneAngleMeasurePropertyFromCache(file, propertyName, PropertyCalculator.GetDoubleValue(), valueType);
                     break;
                  }
               case PropertyType.Area:
                  {
                     propHnd = PropertyUtil.CreateAreaMeasureProperty(file, propertyName, PropertyCalculator.GetDoubleValue(), valueType);
                     break;
                  }
               case PropertyType.Count:
                  {
                     if (PropertyCalculator.CalculatesMultipleParameters)
                        propHnd = PropertyUtil.CreateCountMeasureProperty(file, propertyName, PropertyCalculator.GetIntValue(propertyName), valueType);
                     else
                        propHnd = PropertyUtil.CreateCountMeasureProperty(file, propertyName, PropertyCalculator.GetIntValue(), valueType);
                     break;
                  }
               case PropertyType.Frequency:
                  {
                     propHnd = FrequencyPropertyUtil.CreateFrequencyProperty(file, propertyName, PropertyCalculator.GetDoubleValue(), valueType);
                     break;
                  }
               case PropertyType.Power:
                  {
                     propHnd = PropertyUtil.CreatePowerPropertyFromCache(file, propertyName, PropertyCalculator.GetDoubleValue(), valueType);
                     break;
                  }
               case PropertyType.ThermodynamicTemperature:
                  {
                     propHnd = PropertyUtil.CreateThermodynamicTemperaturePropertyFromCache(file, propertyName, PropertyCalculator.GetDoubleValue(), valueType);
                     break;
                  }
               case PropertyType.ThermalTransmittance:
                  {
                     propHnd = PropertyUtil.CreateThermalTransmittancePropertyFromCache(file, propertyName, PropertyCalculator.GetDoubleValue(), valueType);
                     break;
                  }
               case PropertyType.VolumetricFlowRate:
                  {
                     propHnd = PropertyUtil.CreateVolumetricFlowRateMeasureProperty(file, propertyName, PropertyCalculator.GetDoubleValue(), valueType);
                     break;
                  }
               case PropertyType.LinearVelocity:
                  {
                     propHnd = PropertyUtil.CreateLinearVelocityMeasureProperty(file, propertyName, PropertyCalculator.GetDoubleValue(), valueType);
                     break;
                  }
               default:
                  throw new InvalidOperationException();
            }
         }

         return propHnd;
      }
   }
}