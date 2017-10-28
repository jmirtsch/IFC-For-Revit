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
   /// Represents the type of the container for a property.
   /// </summary>
   public enum PropertyValueType
   {
      /// <summary>
      /// A single property (IfcSingleValue)
      /// </summary>
      SingleValue,
      /// <summary>
      /// An enumerated property (IfcEnumeratedValue)
      /// </summary>
      EnumeratedValue,
      /// <summary>
      /// A list property (IfcListValue)
      /// </summary>
      ListValue,
      /// <summary>
      /// A reference property (IfcPropertyReferenceValue)
      /// </summary>
      ReferenceValue
   }

   /// <summary>
   /// Represents the type of a property.
   /// </summary>
   public enum PropertyType
   {
      /// <summary>
      /// A label (string value), up to 255 characters in length.
      /// </summary>
      Label,
      /// <summary>
      /// A text (string value) of unlimited length.
      /// </summary>
      Text,
      /// <summary>
      /// A boolean value.
      /// </summary>
      Boolean,
      /// <summary>
      /// A real number value.
      /// </summary>
      Integer,
      /// <summary>
      /// An integer number value.
      /// </summary>
      Real,
      /// <summary>
      /// A positive length value.
      /// </summary>
      PositiveLength,
      /// <summary>
      /// A positive ratio value.
      /// </summary>
      PositiveRatio,
      /// <summary>
      /// An angular value.
      /// </summary>
      PlaneAngle,
      /// <summary>
      /// An area value.
      /// </summary>
      Area,
      /// <summary>
      /// An identifier value.
      /// </summary>
      Identifier,
      /// <summary>
      /// A count value.
      /// </summary>
      Count,
      /// <summary>
      /// A thermodynamic temperature value.
      /// </summary>
      ThermodynamicTemperature,
      /// <summary>
      /// A length value.
      /// </summary>
      Length,
      /// <summary>
      /// A ratio value.
      /// </summary>
      Ratio,
      /// <summary>
      /// A thermal transmittance (coefficient of heat transfer) value.
      /// </summary>
      ThermalTransmittance,
      /// <summary>
      /// A volumetric flow rate value.
      /// </summary>
      VolumetricFlowRate,
      /// <summary>
      /// A logical value: true, false, or unknown.
      /// </summary>
      Logical,
      /// <summary>
      /// A power value.
      /// </summary>
      Power,
      /// <summary>
      /// An IfcClassificationReference value.
      /// </summary>
      ClassificationReference,
      /// <summary>
      /// A Frequency value.
      /// </summary>
      Frequency,
      /// <summary>
      /// A positive angular value.
      /// </summary>
      PositivePlaneAngle,
      /// <summary>
      /// An electrical current value
      /// </summary>
      ElectricalCurrent,
      /// <summary>
      /// An electrical voltage value
      /// </summary>
      ElectricalVoltage,
      /// <summary>
      /// Volume
      /// </summary>
      Volume,
      /// <summary>
      /// Luminous Flux
      /// </summary>
      LuminousFlux,
      /// <summary>
      /// Force
      /// </summary>
      Force,
      /// <summary>
      /// Pressure
      /// </summary>
      Pressure,
      /// <summary>
      /// Color temperature, distinguished from thermodyamic temperature
      /// </summary>
      ColorTemperature,
      /// <summary>
      /// Currency
      /// </summary>
      Currency,
      /// <summary>
      /// Electrical Efficacy
      /// </summary>
      ElectricalEfficacy,
      /// <summary>
      /// Luminous Intensity
      /// </summary>
      LuminousIntensity,
      /// <summary>
      /// Illuminance
      /// </summary>
      Illuminance,
      /// <summary>
      /// Normalised Ratio
      /// </summary>
      NormalisedRatio,
      /// <summary>
      /// Linear Velocity
      /// </summary>
      LinearVelocity,
      /// <summary>
      /// Mass Density
      /// </summary>
      MassDensity,
   }

   /// <summary>
   /// Represents a mapping from a Revit parameter or calculated quantity to an IFC property.
   /// </summary>
   public class PropertySetEntry : Entry<PropertySetEntryMap>
   {
      /// <summary>
      /// The type of the IFC property set entry. Default is label.
      /// </summary>
      PropertyType m_PropertyType = PropertyType.Label;

      /// <summary>
      /// The value type of the IFC property set entry.
      /// </summary>
      PropertyValueType m_PropertyValueType = PropertyValueType.SingleValue;

      /// <summary>
      /// The type of the Enum that will validate the value for an enumeration.
      /// </summary>
      Type m_PropertyEnumerationType = null;

      IFCAnyHandle m_DefaultProperty = null;

      GeometryGym.Ifc.IfcValue m_DefaultValue = null;

      /// <summary>
      /// Constructs a PropertySetEntry object.
      /// </summary>
      /// <param name="revitParameterName">
      /// Revit parameter name.
      /// </param>
      public PropertySetEntry(PropertyType propertyType, string revitParameterName)
          : base(revitParameterName)
      {
         m_PropertyType = propertyType;
      }
      public PropertySetEntry(PropertyType propertyType, string propertyName, BuiltInParameter builtInParameter)
             : base(propertyName, new PropertySetEntryMap(propertyName) { RevitBuiltInParameter = builtInParameter })
      {
         m_PropertyType = propertyType;
      }
      public PropertySetEntry(PropertyType propertyType, string propertyName, PropertyCalculator propertyCalculator)
             : base(propertyName, new PropertySetEntryMap(propertyName) { PropertyCalculator = propertyCalculator })
      {
         m_PropertyType = propertyType;
      }
      public PropertySetEntry(PropertyType propertyType, string propertyName, BuiltInParameter builtInParameter, PropertyCalculator propertyCalculator)
             : base(propertyName, new PropertySetEntryMap(propertyName) { RevitBuiltInParameter = builtInParameter, PropertyCalculator = propertyCalculator })
      {
         m_PropertyType = propertyType;
      }
      public PropertySetEntry(PropertyType propertyType, string propertyName, PropertySetEntryMap entry)
           : base(propertyName, entry)
      {
         m_PropertyType = propertyType;
      }
      public PropertySetEntry(PropertyType propertyType, string propertyName, IEnumerable<PropertySetEntryMap> entries)
           : base(propertyName, entries)
      {
         m_PropertyType = propertyType;
      }
      /// <summary>
      /// The type of the IFC property set entry.
      /// </summary>
      public PropertyType PropertyType
      {
         get
         {
            return m_PropertyType;
         }
         private set
         {
            m_PropertyType = value;
         }
      }

      /// <summary>
      /// The value type of the IFC property set entry.
      /// </summary>
      public PropertyValueType PropertyValueType
      {
         get
         {
            return m_PropertyValueType;
         }
         private set
         {
            m_PropertyValueType = value;
         }
      }

      /// <summary>
      /// The type of the Enum that will validate the value for an enumeration.
      /// </summary>
      public Type PropertyEnumerationType
      {
         get
         {
            return m_PropertyEnumerationType;
         }
         private set
         {
            m_PropertyEnumerationType = value;
         }
      }
      public GeometryGym.Ifc.IfcValue DefaultValue { set { m_DefaultValue = value; } }

      private IFCAnyHandle DefaultProperty(IFCFile file)
      {
         if (m_DefaultProperty == null)
         {
            if (m_DefaultValue != null)
            {
               switch (PropertyType)
               {
                  case PropertyType.Label:
                     return m_DefaultProperty = PropertyUtil.CreateLabelProperty(file, PropertyName, m_DefaultValue.ValueString, PropertyValueType, PropertyEnumerationType);
                  case PropertyType.Text:
                     return m_DefaultProperty = PropertyUtil.CreateTextProperty(file, PropertyName, m_DefaultValue.ValueString, PropertyValueType);
                  case PropertyType.Identifier:
                     return m_DefaultProperty = PropertyUtil.CreateIdentifierProperty(file, PropertyName, m_DefaultValue.ValueString, PropertyValueType);

               }
            }
         }
         return m_DefaultProperty;
      }

      public IFCAnyHandle ProcessEntry(IFCFile file, ExporterIFC exporterIFC, IFCExtrusionCreationData extrusionCreationData, Element element,
         ElementType elementType, IFCAnyHandle handle)
      {
         foreach (PropertySetEntryMap map in m_Entries)
         {
            IFCAnyHandle propHnd = map.ProcessEntry(file, exporterIFC, extrusionCreationData, element, elementType, handle, PropertyType, PropertyValueType, PropertyEnumerationType, PropertyName);
            if (propHnd != null)
               return propHnd;
         }
         return DefaultProperty(file);
      }

      /// <summary>
      /// Creates an entry of type enumerated value.
      /// The type of the enumarated value is also given.
      /// Note that the enumeration list is not supported here.
      /// </summary>
      /// <param name="revitParameterName">
      /// Revit parameter name.
      /// </param>
      /// <param name="propertyType">
      /// The property type.
      /// </param>
      /// <returns>
      /// The PropertySetEntry.
      /// </returns>
      public static PropertySetEntry CreateEnumeratedValue(string revitParameterName, PropertyType propertyType, Type enumType)
      {
         return CreateEnumeratedValue(revitParameterName, propertyType, enumType, new PropertySetEntryMap(revitParameterName));
      }
      public static PropertySetEntry CreateEnumeratedValue(string revitParameterName, PropertyType propertyType, Type enumType, PropertySetEntryMap entryMap)
      {
         PropertySetEntry pse = new PropertySetEntry(propertyType, revitParameterName, entryMap);
         pse.PropertyValueType = PropertyValueType.EnumeratedValue;
         pse.PropertyEnumerationType = enumType;
         return pse;
      }

      /// <summary>
      /// Creates an external reference to IfcClassificationReference.
      /// </summary>
      /// <param name="revitParameterName">Revit parameter name.</param>
      /// <returns>The PropertySetEntry.</returns>
      public static PropertySetEntry CreateClassificationReference(string revitParameterName)
      {
         PropertySetEntry pse = new PropertySetEntry(PropertyType.ClassificationReference, revitParameterName);
         pse.PropertyValueType = PropertyValueType.ReferenceValue;
         return pse;
      }

      /// <summary>
      /// Creates an entry of type list value.
      /// The type of the list value is also given.
      /// </summary>
      /// <param name="revitParameterName">Revit parameter name.</param>
      /// <param name="propertyType">The property type.</param>
      /// <returns>The PropertySetEntry.</returns>
      public static PropertySetEntry CreateListValue(string revitParameterName, PropertyType propertyType, PropertySetEntryMap entry)
      {
         PropertySetEntry pse = new PropertySetEntry(propertyType, revitParameterName, entry);
         pse.PropertyValueType = PropertyValueType.ListValue;
         return pse;
      }

      /// <summary>
      /// Creates an entry for a given parameter.
      /// </summary>
      /// <param name="parameter">Revit parameter.</param>
      /// <returns>The PropertySetEntry.</returns>
      public static PropertySetEntry CreateParameterEntry(Parameter parameter, BuiltInParameter builtInParameter)
      {
         Definition parameterDefinition = parameter.Definition;
         if (parameterDefinition == null)
            return null;

         PropertyType propertyType = PropertyType.Text;
         switch (parameter.StorageType)
         {
            case StorageType.None:
               return null;
            case StorageType.Integer:
               {
                  // YesNo or actual integer?
                  if (parameterDefinition.ParameterType == ParameterType.YesNo)
                     propertyType = PropertyType.Boolean;
                  else if (parameterDefinition.ParameterType == ParameterType.Invalid)
                     propertyType = PropertyType.Identifier;
                  else
                     propertyType = PropertyType.Count;
                  break;
               }
            case StorageType.Double:
               {
                  bool assigned = true;
                  switch (parameterDefinition.ParameterType)
                  {
                     case ParameterType.Angle:
                        propertyType = PropertyType.PlaneAngle;
                        break;
                     case ParameterType.Area:
                     case ParameterType.HVACCrossSection:
                     case ParameterType.ReinforcementArea:
                     case ParameterType.SectionArea:
                     case ParameterType.SurfaceArea:
                        propertyType = PropertyType.Area;
                        break;
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
                        propertyType = PropertyType.Length;
                        break;
                     case ParameterType.ColorTemperature:
                        propertyType = PropertyType.ColorTemperature;
                        break;
                     case ParameterType.Currency:
                        propertyType = PropertyType.Currency;
                        break;
                     case ParameterType.ElectricalEfficacy:
                        propertyType = PropertyType.ElectricalEfficacy;
                        break;
                     case ParameterType.ElectricalLuminousIntensity:
                        propertyType = PropertyType.LuminousIntensity;
                        break;
                     case ParameterType.ElectricalIlluminance:
                        propertyType = PropertyType.Illuminance;
                        break;
                     case ParameterType.ElectricalApparentPower:
                     case ParameterType.ElectricalPower:
                     case ParameterType.ElectricalWattage:
                     case ParameterType.HVACPower:
                        propertyType = PropertyType.Power;
                        break;
                     case ParameterType.ElectricalCurrent:
                        propertyType = PropertyType.ElectricalCurrent;
                        break;
                     case ParameterType.ElectricalPotential:
                        propertyType = PropertyType.ElectricalVoltage;
                        break;
                     case ParameterType.ElectricalFrequency:
                        propertyType = PropertyType.Frequency;
                        break;
                     case ParameterType.ElectricalLuminousFlux:
                        propertyType = PropertyType.LuminousFlux;
                        break;
                     case ParameterType.ElectricalTemperature:
                     case ParameterType.HVACTemperature:
                     case ParameterType.PipingTemperature:
                        propertyType = PropertyType.ThermodynamicTemperature;
                        break;
                     case ParameterType.Force:
                        propertyType = PropertyType.Force;
                        break;
                     case ParameterType.HVACAirflow:
                     case ParameterType.PipingFlow:
                        propertyType = PropertyType.VolumetricFlowRate;
                        break;
                     case ParameterType.HVACPressure:
                     case ParameterType.PipingPressure:
                     case ParameterType.Stress:
                        propertyType = PropertyType.Pressure;
                        break;
                     case ParameterType.MassDensity:
                        propertyType = PropertyType.MassDensity;
                        break;
                     case ParameterType.PipingVolume:
                     case ParameterType.ReinforcementVolume:
                     case ParameterType.SectionModulus:
                     case ParameterType.Volume:
                        propertyType = PropertyType.Volume;
                        break;
                     default:
                        assigned = false;
                        break;
                  }

                  if (!assigned)
                     propertyType = PropertyType.Real;
                  break;
               }
            case StorageType.String:
               {
                  propertyType = PropertyType.Text;
                  break;
               }
            case StorageType.ElementId:
               {
                  propertyType = PropertyType.Label;
                  break;
               }
         }
         return new PropertySetEntry(propertyType, parameterDefinition.Name, builtInParameter);
      }
   }
}