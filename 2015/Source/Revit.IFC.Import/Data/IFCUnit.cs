//
// Revit IFC Import library: this library works with Autodesk(R) Revit(R) to import IFC files.
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
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Utility;

using UnitName = Autodesk.Revit.DB.DisplayUnitType;

namespace Revit.IFC.Import.Data
{
    /// <summary>
    /// Presents an IFC unit.
    /// </summary>
    public class IFCUnit : IFCEntity
    {
        /// <summary>
        double m_ScaleFactor = 1.0;

        double m_OffsetFactor = 0.0;

        UnitType m_UnitType = UnitType.UT_Undefined;

        // only used if UnitType = UnitType.UT_Custom.
        string m_CustomUnitType = null;

        UnitSymbolType m_UnitSymbol = UnitSymbolType.UST_NONE;

        UnitName m_UnitName = UnitName.DUT_UNDEFINED;

        UnitSystem m_UnitSystem = UnitSystem.Metric;

        static IDictionary<string, double> m_sPrefixToScaleFactor = null;

        static IDictionary<UnitType, IDictionary<string, KeyValuePair<UnitName, UnitSymbolType>>> m_sSupportedMetricUnitTypes = null;
        
        /// <summary>
        /// The type of unit, such as Length.
        /// </summary>
        public UnitType UnitType 
        {
            get { return m_UnitType; }
            protected set { m_UnitType = value; }
        }

        /// <summary>
        /// The type of unit, if UnitType = UT_Custom.
        /// </summary>
        public string CustomUnitType
        {
            get { return m_CustomUnitType; }
            protected set { m_CustomUnitType = value; }
        }
        
        /// <summary>
        /// The unit system, metric or imperial.
        /// </summary>
        public UnitSystem UnitSystem 
        {
            get { return m_UnitSystem; }
            protected set { m_UnitSystem = value; }
        }

        /// <summary>
        /// The unit name, such as Meters.
        /// </summary>
        public UnitName UnitName 
        {
            get { return m_UnitName; }
            protected set { m_UnitName = value; }
        }

        /// <summary>
        /// The unit symbols, such as "m" for meters.
        /// </summary>
        public UnitSymbolType UnitSymbol
        {
            get { return m_UnitSymbol; }
            protected set { m_UnitSymbol = value; }
        }
        
        /// <summary>
        /// The scale factor to Revit internal unit.
        /// </summary>
        public double ScaleFactor 
        {
            get { return m_ScaleFactor; }
            protected set { m_ScaleFactor = value; }
        }

        /// <summary>
        /// The offset factor to Revit internal unit.
        /// </summary>
        public double OffsetFactor 
        {
            get { return m_OffsetFactor; }
            protected set { m_OffsetFactor = value; }
        }

        /// <summary>
        /// Constructs a Unit object.
        /// </summary>
        protected IFCUnit()
        {
        }

        protected IFCUnit(IFCAnyHandle unit)
        {
            Process(unit);
        }

        /// <summary>
        /// Checks that the unit definition is valid for use.
        /// </summary>
        /// <param name="unit">The IFCUnit to check.</param>
        /// <returns>True if the IFCUnit is null, or has invalid parameters.</returns>
        public static bool IsNullOrInvalid(IFCUnit unit)
        {
            if (unit == null)
                return true;

            return (unit.UnitType == UnitType.UT_Undefined || unit.UnitName == UnitName.DUT_UNDEFINED);
        }

        /// <summary>
        /// Converts the value to this unit type.
        /// </summary>
        /// <param name="inValue">The value to convert.</param>
        /// <returns>The converted value.</returns>
        public double Convert(double inValue)
        {
            return inValue * ScaleFactor - OffsetFactor;
        }

        /// <summary>
        /// Converts the value to this unit type.
        /// </summary>
        /// <param name="inValue">The value to convert.</param>
        /// <returns>The converted value.</returns>
        public int Convert(int inValue)
        {
            return (int) (inValue * ScaleFactor - OffsetFactor);
        }

        protected override void Process(IFCAnyHandle item)
        {
            base.Process(item);
            if (IFCAnyHandleUtil.IsSubTypeOf(item, IFCEntityType.IfcDerivedUnit))
                ProcessIFCDerivedUnit(item);
            else if (IFCAnyHandleUtil.IsSubTypeOf(item, IFCEntityType.IfcMeasureWithUnit))
                ProcessIFCMeasureWithUnit(item);
            else if (IFCAnyHandleUtil.IsSubTypeOf(item, IFCEntityType.IfcMonetaryUnit))
                ProcessIFCMonetaryUnit(item);
            else if (IFCAnyHandleUtil.IsSubTypeOf(item, IFCEntityType.IfcNamedUnit))
                ProcessIFCNamedUnit(item);
            else
                IFCImportFile.TheLog.LogUnhandledSubTypeError(item, "IfcUnit", true);
        }

        /// <summary>
        /// Processes a named unit.
        /// </summary>
        /// <param name="unitHnd">The unit handle.</param>
        void ProcessIFCNamedUnit(IFCAnyHandle unitHnd)
        {
            if (IFCAnyHandleUtil.IsSubTypeOf(unitHnd, IFCEntityType.IfcSIUnit))
                ProcessIFCSIUnit(unitHnd);
            else if (IFCAnyHandleUtil.IsSubTypeOf(unitHnd, IFCEntityType.IfcConversionBasedUnit))
                ProcessIFCConversionBasedUnit(unitHnd);
            else
                IFCImportFile.TheLog.LogUnhandledSubTypeError(unitHnd, IFCEntityType.IfcNamedUnit, true);
        }

        private void InitPrefixToScaleFactor()
        {
            m_sPrefixToScaleFactor = new Dictionary<string, double>();
            m_sPrefixToScaleFactor["EXA"] = 1e+18;
            m_sPrefixToScaleFactor["PETA"] = 1e+15;
            m_sPrefixToScaleFactor["TERA"] = 1e+12;
            m_sPrefixToScaleFactor["GIGA"] = 1e+9;
            m_sPrefixToScaleFactor["MEGA"] = 1e+6;
            m_sPrefixToScaleFactor["KILO"] = 1e+3;
            m_sPrefixToScaleFactor["HECTO"] = 1e+2;
            m_sPrefixToScaleFactor["DECA"] = 1e+1;
            m_sPrefixToScaleFactor[""] = 1e+0;
            m_sPrefixToScaleFactor["DECI"] = 1e-1;
            m_sPrefixToScaleFactor["CENTI"] = 1e-2;
            m_sPrefixToScaleFactor["MILLI"] = 1e-3;
            m_sPrefixToScaleFactor["MICRO"] = 1e-6;
            m_sPrefixToScaleFactor["NANO"] = 1e-9;
            m_sPrefixToScaleFactor["PICO"] = 1e-12;
            m_sPrefixToScaleFactor["FEMTO"] = 1e-15;
            m_sPrefixToScaleFactor["ATTO"] = 1e-18;
        }

        private IDictionary<string, KeyValuePair<UnitName, UnitSymbolType>> GetSupportedDisplayTypes(UnitType unitType)
        {
            if (m_sSupportedMetricUnitTypes == null)
                m_sSupportedMetricUnitTypes = new Dictionary<UnitType, IDictionary<string, KeyValuePair<UnitName, UnitSymbolType>>>();

            IDictionary<string, KeyValuePair<UnitName, UnitSymbolType>> supportedTypes = null;
            if (!m_sSupportedMetricUnitTypes.TryGetValue(unitType, out supportedTypes))
            {
                supportedTypes = new Dictionary<string, KeyValuePair<UnitName, UnitSymbolType>>();
                switch (unitType)
                {
                    case UnitType.UT_Area:
                        supportedTypes[""] = new KeyValuePair<UnitName, UnitSymbolType>(UnitName.DUT_SQUARE_METERS, UnitSymbolType.UST_M_SUP_2);
                        supportedTypes["CENTI"] = new KeyValuePair<UnitName, UnitSymbolType>(UnitName.DUT_SQUARE_CENTIMETERS, UnitSymbolType.UST_CM_SUP_2);
                        supportedTypes["MILLI"] = new KeyValuePair<UnitName, UnitSymbolType>(UnitName.DUT_SQUARE_MILLIMETERS, UnitSymbolType.UST_MM_SUP_2);
                        break;
                    case UnitType.UT_Electrical_Current:
                        supportedTypes[""] = new KeyValuePair<UnitName, UnitSymbolType>(UnitName.DUT_AMPERES, UnitSymbolType.UST_AMPERE);
                        supportedTypes["KILO"] = new KeyValuePair<UnitName, UnitSymbolType>(UnitName.DUT_KILOAMPERES, UnitSymbolType.UST_KILOAMPERE);
                        supportedTypes["MILLI"] = new KeyValuePair<UnitName, UnitSymbolType>(UnitName.DUT_MILLIAMPERES, UnitSymbolType.UST_MILLIAMPERE);
                        break;
                    case UnitType.UT_Electrical_Frequency:
                        supportedTypes[""] = new KeyValuePair<UnitName, UnitSymbolType>(UnitName.DUT_HERTZ, UnitSymbolType.UST_HZ);
                        break;
                    case UnitType.UT_Electrical_Illuminance:
                        supportedTypes[""] = new KeyValuePair<UnitName, UnitSymbolType>(UnitName.DUT_LUX, UnitSymbolType.UST_LX);
                        break;
                    case UnitType.UT_Electrical_Luminous_Flux:
                        supportedTypes[""] = new KeyValuePair<UnitName, UnitSymbolType>(UnitName.DUT_LUMENS, UnitSymbolType.UST_LM);
                        break;
                    case UnitType.UT_Electrical_Luminous_Intensity:
                        supportedTypes[""] = new KeyValuePair<UnitName, UnitSymbolType>(UnitName.DUT_CANDELAS, UnitSymbolType.UST_CD);
                        break;
                    case UnitType.UT_Electrical_Potential:
                        supportedTypes[""] = new KeyValuePair<UnitName, UnitSymbolType>(UnitName.DUT_VOLTS, UnitSymbolType.UST_VOLT);
                        supportedTypes["KILO"] = new KeyValuePair<UnitName, UnitSymbolType>(UnitName.DUT_KILOVOLTS, UnitSymbolType.UST_KILOVOLT);
                        supportedTypes["MILLI"] = new KeyValuePair<UnitName, UnitSymbolType>(UnitName.DUT_MILLIVOLTS, UnitSymbolType.UST_MILLIVOLT);
                        break;
                    case UnitType.UT_Force:
                        supportedTypes[""] = new KeyValuePair<UnitName, UnitSymbolType>(UnitName.DUT_NEWTONS, UnitSymbolType.UST_N);    // Even if unit is grams, display kg.
                        supportedTypes["KILO"] = new KeyValuePair<UnitName, UnitSymbolType>(UnitName.DUT_KILONEWTONS, UnitSymbolType.UST_K_N);
                        break;
                    case UnitType.UT_HVAC_Power:
                        supportedTypes[""] = new KeyValuePair<UnitName, UnitSymbolType>(UnitName.DUT_WATTS, UnitSymbolType.UST_WATT);
                        break;
                    case UnitType.UT_HVAC_Pressure:
                        supportedTypes[""] = new KeyValuePair<UnitName, UnitSymbolType>(UnitName.DUT_PASCALS, UnitSymbolType.UST_PASCAL);
                        supportedTypes["KILO"] = new KeyValuePair<UnitName, UnitSymbolType>(UnitName.DUT_KILOPASCALS, UnitSymbolType.UST_KILOPASCAL);
                        supportedTypes["MEGA"] = new KeyValuePair<UnitName, UnitSymbolType>(UnitName.DUT_MEGAPASCALS, UnitSymbolType.UST_MEGAPASCAL);
                        break;
                    case UnitType.UT_Length:
                        supportedTypes[""] = new KeyValuePair<UnitName, UnitSymbolType>(UnitName.DUT_METERS, UnitSymbolType.UST_M);
                        supportedTypes["CENTI"] = new KeyValuePair<UnitName, UnitSymbolType>(UnitName.DUT_CENTIMETERS, UnitSymbolType.UST_CM);
                        supportedTypes["MILLI"] = new KeyValuePair<UnitName, UnitSymbolType>(UnitName.DUT_MILLIMETERS, UnitSymbolType.UST_MM);
                        break;
                    case UnitType.UT_Mass:
                        supportedTypes[""] = new KeyValuePair<UnitName, UnitSymbolType>(UnitName.DUT_KILOGRAMS_MASS, UnitSymbolType.UST_KGM);    // Even if unit is grams, display kg.
                        supportedTypes["KILO"] = new KeyValuePair<UnitName, UnitSymbolType>(UnitName.DUT_KILOGRAMS_MASS, UnitSymbolType.UST_KGM);
                        break;
                    case UnitType.UT_Volume:
                        supportedTypes[""] = new KeyValuePair<UnitName, UnitSymbolType>(UnitName.DUT_CUBIC_METERS, UnitSymbolType.UST_M_SUP_3);
                        supportedTypes["CENTI"] = new KeyValuePair<UnitName, UnitSymbolType>(UnitName.DUT_CUBIC_CENTIMETERS, UnitSymbolType.UST_CM_SUP_3);
                        supportedTypes["MILLI"] = new KeyValuePair<UnitName, UnitSymbolType>(UnitName.DUT_CUBIC_MILLIMETERS, UnitSymbolType.UST_MM_SUP_3);
                        break;
                }
                m_sSupportedMetricUnitTypes[unitType] = supportedTypes;
            }
                
            return supportedTypes;
        }

        private double GetScaleFactorForUnitType(string prefix, UnitType unitType)
        {
            double scaleFactor = m_sPrefixToScaleFactor[prefix];

            const double lengthFactor = (1.0 / 0.3048);
            const double areaFactor = lengthFactor * lengthFactor;
            const double volumeFactor = areaFactor * lengthFactor;

            switch (unitType)
            {
                // length ^ -2
                case UnitType.UT_Electrical_Illuminance:
                    return (scaleFactor * scaleFactor) / areaFactor;
                // length ^ -1
                case UnitType.UT_HVAC_Pressure:
                    return scaleFactor / lengthFactor;
                // length
                case UnitType.UT_Force:
                case UnitType.UT_Length:
                    return (scaleFactor * lengthFactor);
                // length ^ 2
                case UnitType.UT_Area:
                case UnitType.UT_Electrical_Potential:
                case UnitType.UT_HVAC_Power:
                    return (scaleFactor * scaleFactor) * areaFactor;
                // length ^ 3
                case UnitType.UT_Volume:
                    return (scaleFactor * scaleFactor * scaleFactor) * volumeFactor;
                // Misc. units
                case UnitType.UT_Mass:
                    return (scaleFactor / 1000.0);   // Standard internal scale is kg.
                default:
                    return scaleFactor;
            }
        }

        /// <summary>
        /// Processes the metric prefix of a dimension.
        /// </summary>
        /// <param name="prefix">The prefix name.</param>
        /// <param name="unitType">The unit type.</param>
        /// <returns>True if the prefix is supported, false if not.</returns>
        private bool ProcessMetricPrefix(string prefix, UnitType unitType)
        {
            if (prefix == null)
                prefix = "";

            IDictionary<string, KeyValuePair<UnitName, UnitSymbolType>> supportedDisplayTypes = GetSupportedDisplayTypes(unitType);
            if (!supportedDisplayTypes.ContainsKey(prefix))
                return false;

            if (m_sPrefixToScaleFactor == null)
                InitPrefixToScaleFactor();

            if (!m_sPrefixToScaleFactor.ContainsKey(prefix))
                return false;

            KeyValuePair<UnitName, UnitSymbolType> unitNameAndSymbol = supportedDisplayTypes[prefix];
            UnitName = unitNameAndSymbol.Key;
            UnitSymbol = unitNameAndSymbol.Value;
            ScaleFactor *= GetScaleFactorForUnitType(prefix, unitType);
            return true;
        }

        /// <summary>
        /// Processes an IfcDerivedUnit.
        /// </summary>
        /// <param name="unitHnd">The unit handle.</param>
        void ProcessIFCDerivedUnit(IFCAnyHandle unitHnd)
        {
            List<IFCAnyHandle> elements =
                IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(unitHnd, "Elements");

            IList<KeyValuePair<IFCUnit, int>> derivedElementUnitHnds = new List<KeyValuePair<IFCUnit, int>>();
            foreach (IFCAnyHandle subElement in elements)
            {
                IFCAnyHandle derivedElementUnitHnd = IFCImportHandleUtil.GetRequiredInstanceAttribute(subElement, "Unit", false);
                IFCUnit subUnit = IFCAnyHandleUtil.IsNullOrHasNoValue(derivedElementUnitHnd) ? null : IFCUnit.ProcessIFCUnit(derivedElementUnitHnd);
                if (subUnit != null)
                {
                    bool found;
                    int exponent = IFCImportHandleUtil.GetRequiredIntegerAttribute(subElement, "Exponent", out found);
                    if (found)
                        derivedElementUnitHnds.Add(new KeyValuePair<IFCUnit, int>(subUnit, exponent));
                }
            }

            ISet<Tuple<int, UnitType, string>> expectedTypes = new HashSet<Tuple<int, UnitType, string>>();

            string unitType = IFCAnyHandleUtil.GetEnumerationAttribute(unitHnd, "UnitType");
            if (string.Compare(unitType, "THERMALTRANSMITTANCEUNIT", true) == 0)
            {
                UnitType = UnitType.UT_HVAC_CoefficientOfHeatTransfer;
                UnitSystem = UnitSystem.Metric;
                UnitName = UnitName.DUT_WATTS_PER_SQUARE_METER_KELVIN;

                // Support only kg / (K * s^3).
                expectedTypes.Add(new Tuple<int, Autodesk.Revit.DB.UnitType, string>(1, UnitType.UT_Mass, null));
                expectedTypes.Add(new Tuple<int, Autodesk.Revit.DB.UnitType, string>(-1, UnitType.UT_HVAC_Temperature, null));
                expectedTypes.Add(new Tuple<int, Autodesk.Revit.DB.UnitType, string>(-3, UnitType.UT_Custom, "TIMEUNIT"));
            }
            else if (string.Compare(unitType, "VOLUMETRICFLOWRATEUNIT", true) == 0)
            {
                UnitType = UnitType.UT_HVAC_Airflow;
                UnitSystem = UnitSystem.Metric;
                UnitName = UnitName.DUT_CUBIC_METERS_PER_SECOND;

                // Support only m^3 / s.
                expectedTypes.Add(new Tuple<int, Autodesk.Revit.DB.UnitType, string>(3, UnitType.UT_Length, null));
                expectedTypes.Add(new Tuple<int, Autodesk.Revit.DB.UnitType, string>(-1, UnitType.UT_Custom, "TIMEUNIT"));
            }
            else if (string.Compare(unitType, "USERDEFINED", true) == 0)
            {
                // Look at the sub-types to see what we support.
                string userDefinedType = IFCImportHandleUtil.GetOptionalStringAttribute(unitHnd, "UserDefinedType", null);
                if (!string.IsNullOrWhiteSpace(userDefinedType))
                {
                    if (string.Compare(userDefinedType, "Luminous Efficacy", true) == 0)
                    {
                        UnitType = UnitType.UT_Electrical_Efficacy;
                        UnitSystem = UnitSystem.Metric;
                        UnitName = UnitName.DUT_LUMENS_PER_WATT;

                        // Support only lm / W.
                        expectedTypes.Add(new Tuple<int, Autodesk.Revit.DB.UnitType, string>(-1, UnitType.UT_Mass, null));
                        expectedTypes.Add(new Tuple<int, Autodesk.Revit.DB.UnitType, string>(-2, UnitType.UT_Length, null));
                        expectedTypes.Add(new Tuple<int, Autodesk.Revit.DB.UnitType, string>(3, UnitType.UT_Custom, "TIMEUNIT"));
                        expectedTypes.Add(new Tuple<int, Autodesk.Revit.DB.UnitType, string>(1, UnitType.UT_Electrical_Luminous_Flux, null));
                    }
                }
            }

            double scaleFactor = 1.0;
            if (derivedElementUnitHnds.Count == expectedTypes.Count)
            {
                foreach (KeyValuePair<IFCUnit, int> derivedElementUnitHnd in derivedElementUnitHnds)
                {
                    int dimensionality = derivedElementUnitHnd.Value;
                    Tuple<int, UnitType, string> currKey = new Tuple<int, UnitType, string>(dimensionality, derivedElementUnitHnd.Key.UnitType, derivedElementUnitHnd.Key.CustomUnitType);
                    if (expectedTypes.Contains(currKey))
                    {
                        expectedTypes.Remove(currKey);
                        scaleFactor *= Math.Pow(derivedElementUnitHnd.Key.ScaleFactor, dimensionality);
                    }
                    else
                        break;
                }
            
                // Found all supported units.
                if (expectedTypes.Count == 0)
                {
                    ScaleFactor = scaleFactor;
                    return;
                }
            }

            IFCImportFile.TheLog.LogUnhandledUnitTypeError(unitHnd, unitType);
        }
        
        /// <summary>
        /// Processes an SI unit.
        /// </summary>
        /// <param name="unitHnd">The unit handle.</param>
        void ProcessIFCSIUnit(IFCAnyHandle unitHnd)
        {
            UnitSystem = UnitSystem.Metric;

            string unitType = IFCAnyHandleUtil.GetEnumerationAttribute(unitHnd, "UnitType");
            string unitName = IFCAnyHandleUtil.GetEnumerationAttribute(unitHnd, "Name");
            string prefix = IFCAnyHandleUtil.GetEnumerationAttribute(unitHnd, "Prefix");
            bool unitNameSupported = true;

            if (string.Compare(unitType, "AREAUNIT", true) == 0)
            {
                UnitType = UnitType.UT_Area;
                unitNameSupported = (string.Compare(unitName, "SQUARE_METRE", true) == 0) && ProcessMetricPrefix(prefix, UnitType);
            }
            else if (string.Compare(unitType, "ELECTRICCURRENTUNIT", true) == 0)
            {
                UnitType = UnitType.UT_Electrical_Current;
                unitNameSupported = ProcessMetricPrefix(prefix, UnitType);
            }
            else if (string.Compare(unitType, "ELECTRICVOLTAGEUNIT", true) == 0)
            {
                UnitType = UnitType.UT_Electrical_Potential;
                unitNameSupported = ProcessMetricPrefix(prefix, UnitType);
            }
            else if (string.Compare(unitType, "FORCEUNIT", true) == 0)
            {
                UnitType = UnitType.UT_Force;
                unitNameSupported = ProcessMetricPrefix(prefix, UnitType);
            }
            else if (string.Compare(unitType, "FREQUENCYUNIT", true) == 0)
            {
                UnitType = UnitType.UT_Electrical_Frequency;
                unitNameSupported = ProcessMetricPrefix(prefix, UnitType);
            }
            else if (string.Compare(unitType, "ILLUMINANCEUNIT", true) == 0)
            {
                UnitType = UnitType.UT_Electrical_Illuminance;
                unitNameSupported = ProcessMetricPrefix(prefix, UnitType);
            }
            else if (string.Compare(unitType, "LENGTHUNIT", true) == 0)
            {
                UnitType = UnitType.UT_Length;
                unitNameSupported = (string.Compare(unitName, "METRE", true) == 0) && ProcessMetricPrefix(prefix, UnitType.UT_Length);
            }
            else if (string.Compare(unitType, "LUMINOUSFLUXUNIT", true) == 0)
            {
                UnitType = UnitType.UT_Electrical_Luminous_Flux;
                unitNameSupported = ProcessMetricPrefix(prefix, UnitType);
            }
            else if (string.Compare(unitType, "LUMINOUSINTENSITYUNIT", true) == 0)
            {
                UnitType = UnitType.UT_Electrical_Luminous_Intensity;
                unitNameSupported = ProcessMetricPrefix(prefix, UnitType);
            }
            else if (string.Compare(unitType, "MASSUNIT", true) == 0)
            {
                UnitType = UnitType.UT_Mass;
                unitNameSupported = ProcessMetricPrefix(prefix, UnitType);
            }
            else if (string.Compare(unitType, "PLANEANGLEUNIT", true) == 0)
            {
                UnitType = UnitType.UT_Angle;
                UnitName = UnitName.DUT_RADIANS;
                unitNameSupported = (string.Compare(unitName, "RADIAN", true) == 0) && (string.IsNullOrWhiteSpace(prefix));
            }
            else if (string.Compare(unitType, "POWERUNIT", true) == 0)
            {
                UnitType = UnitType.UT_HVAC_Power;
                unitNameSupported = ProcessMetricPrefix(prefix, UnitType);
            }
            else if (string.Compare(unitType, "PRESSUREUNIT", true) == 0)
            {
                UnitType = UnitType.UT_HVAC_Pressure;
                unitNameSupported = ProcessMetricPrefix(prefix, UnitType);
            }
            else if (string.Compare(unitType, "SOLIDANGLEUNIT", true) == 0)
            {
                // Will warn if not steridians.
                UnitType = UnitType.UT_Custom;
                CustomUnitType = unitType;
                unitNameSupported = (string.Compare(unitName, "STERADIAN", true) == 0) && (string.IsNullOrWhiteSpace(prefix));
            }
            else if (string.Compare(unitType, "THERMODYNAMICTEMPERATUREUNIT", true) == 0)
            {
                UnitType = UnitType.UT_HVAC_Temperature;
                if (string.Compare(unitName, "DEGREE_CELSIUS", true) == 0 ||
                    string.Compare(unitName, "CELSIUS", true) == 0)
                {
                    UnitName = UnitName.DUT_CELSIUS;
                    UnitSymbol = UnitSymbolType.UST_DEGREE_C;
                    OffsetFactor = -273.15;
                }
                else if (string.Compare(unitName, "KELVIN", true) == 0 ||
                    string.Compare(unitName, "DEGREE_KELVIN", true) == 0)
                {
                    UnitName = UnitName.DUT_KELVIN;
                    UnitSymbol = UnitSymbolType.UST_KELVIN;
                }
                else if (string.Compare(unitName, "FAHRENHEIT", true) == 0 ||
                    string.Compare(unitName, "DEGREE_FAHRENHEIT", true) == 0)
                {
                    UnitSystem = UnitSystem.Imperial;
                    UnitName = UnitName.DUT_FAHRENHEIT;
                    UnitSymbol = UnitSymbolType.UST_DEGREE_F;
                    ScaleFactor = 5.0 / 9.0;
                    OffsetFactor = (5.0 / 9.0) * 32 - 273.15;
                }
                else
                    unitNameSupported = false;
            }
            else if (string.Compare(unitType, "TIMEUNIT", true) == 0)
            {
                // Will warn if not seconds.
                UnitType = UnitType.UT_Custom;
                CustomUnitType = unitType;
                unitNameSupported = (string.Compare(unitName, "SECOND", true) == 0) && (string.IsNullOrWhiteSpace(prefix));
            }
            else if (string.Compare(unitType, "VOLUMEUNIT", true) == 0)
            {
                UnitType = UnitType.UT_Volume;
                unitNameSupported = (string.Compare(unitName, "CUBIC_METRE", true) == 0) && ProcessMetricPrefix(prefix, UnitType.UT_Volume);
            }
            else
            {
                IFCImportFile.TheLog.LogUnhandledUnitTypeError(unitHnd, unitType);
            }

            if (unitName != null && !unitNameSupported)
            {
                if (prefix != null)
                    IFCImportFile.TheLog.LogError(unitHnd.StepId, "Unhandled type of " + unitType + ": " + prefix + unitName, false);
                else
                    IFCImportFile.TheLog.LogError(unitHnd.StepId, "Unhandled type of " + unitType + ": " + unitName, false);
            }
        }

        // Note: the ScaleFactor will be likely overwritten.
        void CopyUnit(IFCUnit unit)
        {
            UnitType = unit.UnitType;
            UnitName = unit.UnitName;
            UnitSystem = unit.UnitSystem;
            UnitSymbol = unit.UnitSymbol;
            ScaleFactor = unit.ScaleFactor;
            OffsetFactor = unit.OffsetFactor;
        }

        /// <summary>
        /// Processes measure with unit.
        /// </summary>
        /// <param name="measureUnitHnd">The measure unit handle.</param>
        void ProcessIFCMeasureWithUnit(IFCAnyHandle measureUnitHnd)
        {
            double baseScale = 0.0;

            IFCData ifcData = measureUnitHnd.GetAttribute("ValueComponent");
            if (!ifcData.HasValue)
                throw new InvalidOperationException("#" + measureUnitHnd.StepId + ": Missing required attribute ValueComponent.");

            if (ifcData.PrimitiveType == IFCDataPrimitiveType.Double)
                baseScale = ifcData.AsDouble();
            else if (ifcData.PrimitiveType == IFCDataPrimitiveType.Integer)
                baseScale = (double)ifcData.AsInteger();

            if (MathUtil.IsAlmostZero(baseScale))
                throw new InvalidOperationException("#" + measureUnitHnd.StepId + ": ValueComponent should not be almost zero.");

            IFCAnyHandle unitHnd = IFCImportHandleUtil.GetRequiredInstanceAttribute(measureUnitHnd, "UnitComponent", true);

            IFCUnit unit = ProcessIFCUnit(unitHnd);
            CopyUnit(unit);
            ScaleFactor = unit.ScaleFactor * baseScale;
        }

        /// <summary>
        /// Processes monetary unit.
        /// </summary>
        /// <param name="monetaryUnitHnd">The monetary unit handle.</param>
        void ProcessIFCMonetaryUnit(IFCAnyHandle monetaryUnitHnd)
        {
            string currencyType = IFCAnyHandleUtil.GetEnumerationAttribute(monetaryUnitHnd, "Currency");

            UnitType = UnitType.UT_Currency;
            UnitName = UnitName.DUT_CURRENCY;

            UnitSymbol = UnitSymbolType.UST_NONE;
            if ((string.Compare(currencyType, "CAD", true) == 0) ||
                (string.Compare(currencyType, "USD", true) == 0))
                UnitSymbol = UnitSymbolType.UST_DOLLAR;
            else if (string.Compare(currencyType, "EUR", true) == 0)
                UnitSymbol = UnitSymbolType.UST_EURO_PREFIX;
            else if (string.Compare(currencyType, "GBP", true) == 0)
                UnitSymbol = UnitSymbolType.UST_POUND;
            else if (string.Compare(currencyType, "HKD", true) == 0)
                UnitSymbol = UnitSymbolType.UST_CHINESE_HONG_KONG_SAR;
            else if ((string.Compare(currencyType, "ICK", true) == 0) ||
                (string.Compare(currencyType, "NOK", true) == 0) ||
                (string.Compare(currencyType, "SEK", true) == 0))
                UnitSymbol = UnitSymbolType.UST_KRONER;
            else if (string.Compare(currencyType, "ILS", true) == 0)
                UnitSymbol = UnitSymbolType.UST_SHEQEL;
            else if (string.Compare(currencyType, "JPY", true) == 0)
                UnitSymbol = UnitSymbolType.UST_YEN;
            else if (string.Compare(currencyType, "KRW", true) == 0)
                UnitSymbol = UnitSymbolType.UST_WON;
            else if (string.Compare(currencyType, "THB", true) == 0)
                UnitSymbol = UnitSymbolType.UST_BAHT;
            else if (string.Compare(currencyType, "VND", true) == 0)
                UnitSymbol = UnitSymbolType.UST_DONG;
            else
                IFCImportFile.TheLog.LogWarning(Id, "Unhandled type of currency: " + currencyType, true);
        }

        /// <summary>
        /// Processes a conversion based unit.
        /// </summary>
        /// <param name="convUnitHnd">The unit handle.</param>
        void ProcessIFCConversionBasedUnit(IFCAnyHandle convUnitHnd)
        {
            IFCAnyHandle measureWithUnitHnd = IFCAnyHandleUtil.GetInstanceAttribute(convUnitHnd, "ConversionFactor");
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(measureWithUnitHnd))
                throw new InvalidOperationException("#" + convUnitHnd.StepId + ": Missing required attribute ConversionFactor.");

            IFCUnit measureWithUnit = IFCUnit.ProcessIFCUnit(measureWithUnitHnd);
            if (measureWithUnit == null)
                throw new InvalidOperationException("#" + convUnitHnd.StepId + ": Invalid base ConversionFactor, aborting.");

            CopyUnit(measureWithUnit);

            // For some common cases, get the units correct.
            string unitType = IFCAnyHandleUtil.GetEnumerationAttribute(convUnitHnd, "UnitType");
            if (string.Compare(unitType, "LENGTHUNIT", true) == 0)
            {
                UnitType = UnitType.UT_Length;
                string name = IFCAnyHandleUtil.GetStringAttribute(convUnitHnd, "Name");

                if (string.Compare(name, "FOOT", true) == 0 ||
                   string.Compare(name, "FEET", true) == 0)
                {
                    UnitSystem = UnitSystem.Imperial;
                    UnitName = UnitName.DUT_FEET_FRACTIONAL_INCHES;
                    UnitSymbol = UnitSymbolType.UST_NONE;
                }
                else if (string.Compare(name, "INCH", true) == 0 ||
                   string.Compare(name, "INCHES", true) == 0)
                {
                    UnitSystem = UnitSystem.Imperial;
                    UnitName = UnitName.DUT_FRACTIONAL_INCHES;
                    UnitSymbol = UnitSymbolType.UST_NONE;
                }
            }
            else if (string.Compare(unitType, "PLANEANGLEUNIT", true) == 0)
            {
                UnitType = UnitType.UT_Angle;
                string name = IFCAnyHandleUtil.GetStringAttribute(convUnitHnd, "Name");

                if (string.Compare(name, "GRAD", true) == 0 ||
                   string.Compare(name, "GRADIAN", true) == 0 ||
                    string.Compare(name, "GRADS", true) == 0 ||
                   string.Compare(name, "GRADIANS", true) == 0)
                {
                    UnitSystem = UnitSystem.Metric;
                    UnitName = UnitName.DUT_GRADS;
                    UnitSymbol = UnitSymbolType.UST_GRAD;
                }
                else if (string.Compare(name, "DEGREE", true) == 0 ||
                   string.Compare(name, "DEGREES", true) == 0)
                {
                    UnitSystem = UnitSystem.Imperial;
                    UnitName = UnitName.DUT_DECIMAL_DEGREES;
                    UnitSymbol = UnitSymbolType.UST_DEGREE_SYMBOL;
                }
            }
            else if (string.Compare(unitType, "AREAUNIT", true) == 0)
            {
                UnitType = UnitType.UT_Area;
                string name = IFCAnyHandleUtil.GetStringAttribute(convUnitHnd, "Name");

                if (string.Compare(name, "SQUARE FOOT", true) == 0 ||
                   string.Compare(name, "SQUARE_FOOT", true) == 0 ||
                   string.Compare(name, "SQUARE FEET", true) == 0 ||
                   string.Compare(name, "SQUARE_FEET", true) == 0)
                {
                    UnitSystem = UnitSystem.Imperial;
                    UnitName = UnitName.DUT_SQUARE_FEET;
                    UnitSymbol = UnitSymbolType.UST_FT_SUP_2;
                }
            }
            else if (string.Compare(unitType, "VOLUMEUNIT", true) == 0)
            {
                UnitType = UnitType.UT_Volume;
                string name = IFCAnyHandleUtil.GetStringAttribute(convUnitHnd, "Name");

                if (string.Compare(name, "CUBIC FOOT", true) == 0 ||
                   string.Compare(name, "CUBIC_FOOT", true) == 0 ||
                   string.Compare(name, "CUBIC FEET", true) == 0 ||
                   string.Compare(name, "CUBIC_FEET", true) == 0)
                {
                    UnitSystem = UnitSystem.Imperial;
                    UnitName = UnitName.DUT_CUBIC_FEET;
                    UnitSymbol = UnitSymbolType.UST_FT_SUP_3;
                }
            }
            else if (string.Compare(unitType, "THERMODYNAMICMEASUREUNIT", true) == 0)
            {
                UnitType = UnitType.UT_HVAC_Temperature;
                string name = IFCAnyHandleUtil.GetStringAttribute(convUnitHnd, "Name");

                if ((string.Compare(name, "F", true) == 0) ||
                   (string.Compare(name, "FAHRENHEIT", true) == 0))
                {
                    UnitSystem = UnitSystem.Imperial;
                    UnitName = UnitName.DUT_FAHRENHEIT;
                    UnitSymbol = UnitSymbolType.UST_DEGREE_F;
                }
                else if ((string.Compare(name, "R", true) == 0) ||
                   (string.Compare(name, "RANKINE", true) == 0))
                {
                    UnitSystem = UnitSystem.Imperial;
                    UnitName = UnitName.DUT_RANKINE;
                    UnitSymbol = UnitSymbolType.UST_DEGREE_R;
                }
            }
        }

        /// <summary>
        /// Processes a unit.
        /// </summary>
        /// <param name="unitHnd">The unit handle.</param>
        /// <returns>The Unit object.</returns>
        public static IFCUnit ProcessIFCUnit(IFCAnyHandle unitHnd)
        {
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(unitHnd))
            {
                //LOG: ERROR: IfcUnit is null or has no value.
                return null;
            }

            try
            {
                IFCEntity ifcUnit;
                if (!IFCImportFile.TheFile.EntityMap.TryGetValue(unitHnd.StepId, out ifcUnit))
                    ifcUnit = new IFCUnit(unitHnd);
                return (ifcUnit as IFCUnit);
            }
            catch (InvalidOperationException ex)
            {
                IFCImportFile.TheLog.LogError(unitHnd.StepId, ex.Message, false);
            }

            return null;
        }

        /// <summary>
        /// Constructs a default IFCUnit of a specific type.
        /// </summary>
        /// <param name="unitType">The unit type.</param>
        /// <param name="unitSystem">The unit system.</param>
        /// <param name="unitName">The unit name.</param>
        /// <remarks>This is only intended to create a unit container for units that are necessary for the file,
        /// but are not found in the file.  It should not be used for IfcUnit entities in the file.</remarks>
        public static IFCUnit ProcessIFCDefaultUnit(UnitType unitType, UnitSystem unitSystem, UnitName unitName, double? scaleFactor)
        {
            IFCUnit unit = new IFCUnit();

            unit.UnitType = unitType;
            unit.UnitName = unitName;
            unit.UnitSystem = unitSystem;
            if (scaleFactor.HasValue)
                unit.ScaleFactor = scaleFactor.Value;
            unit.OffsetFactor = 0.0;

            return unit;
        }
    }
}
