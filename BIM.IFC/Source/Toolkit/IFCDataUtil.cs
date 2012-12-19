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
using Autodesk.Revit.DB.IFC;
using BIM.IFC.Utility;

namespace BIM.IFC.Toolkit
{
    /// <summary>
    /// Represents IfcValue.
    /// </summary>
    class IFCDataUtil
    {
        /// <summary>
        /// Creates an IFCData object as IfcLabel.
        /// </summary>
        /// <param name="value">The string value.</param>
        /// <returns>The IFCData object.</returns>
        public static IFCData CreateAsLabel(string value)
        {
            return IFCData.CreateStringOfType(value.Length > 255 ? value.Remove(255) : value, "IfcLabel");
        }

        /// <summary>
        /// Creates an IFCData object as IfcText.
        /// </summary>
        /// <param name="value">The string value.</param>
        /// <returns>The IFCData object.</returns>
        public static IFCData CreateAsText(string value)
        {
            return IFCData.CreateStringOfType(value, "IfcText");
        }

        /// <summary>
        /// Creates an IFCData object as IfcIdentifier.
        /// </summary>
        /// <param name="value">The string value.</param>
        /// <returns>The IFCData object.</returns>
        public static IFCData CreateAsIdentifier(string value)
        {
            return IFCData.CreateStringOfType(value.Length > 255 ? value.Remove(255) : value, "IfcIdentifier");
        }

        /// <summary>
        /// Creates an IFCData object as IfcBoolean.
        /// </summary>
        /// <param name="value">The boolean value.</param>
        /// <returns>The IFCData object.</returns>
        public static IFCData CreateAsBoolean(bool value)
        {
            return IFCData.CreateBooleanOfType(value, "IfcBoolean");
        }

        /// <summary>
        /// Creates an IFCData object as IfcLogical.
        /// </summary>
        /// <param name="value">The logical value.</param>
        /// <returns>The IFCData object.</returns>
        public static IFCData CreateAsLogical(IFCLogical value)
        {
            IFCData ifcData = IFCData.CreateLogical(value);
            IList<string> types = new List<string>();
            types.Add("IfcLogical");
            ifcData.SetTypeList(types);
            return ifcData;
        }

        /// <summary>
        /// Creates an IFCData object as IfcInteger.
        /// </summary>
        /// <param name="value">The integer value.</param>
        /// <returns>The IFCData object.</returns>
        public static IFCData CreateAsInteger(int value)
        {
            return IFCData.CreateIntegerOfType(value, "IfcInteger");
        }

        /// <summary>
        /// Creates an IFCData object as IfcReal.
        /// </summary>
        /// <param name="value">The double value.</param>
        /// <returns>The IFCData object.</returns>
        public static IFCData CreateAsReal(double value)
        {
            return IFCData.CreateDoubleOfType(value, "IfcReal");
        }

        /// <summary>
        /// Creates an IFCData object as IfcRatioMeasure.
        /// </summary>
        /// <param name="value">The double value.</param>
        /// <returns>The IFCData object.</returns>
        public static IFCData CreateAsRatioMeasure(double value)
        {
            return IFCData.CreateDoubleOfType(value, "IfcRatioMeasure");
        }

        /// <summary>
        /// Creates an IFCData object as IfcNormalisedRatioMeasure.
        /// </summary>
        /// <param name="value">The double value.</param>
        /// <returns>The IFCData object.</returns>
        public static IFCData CreateAsNormalisedRatioMeasure(double value)
        {
            return IFCData.CreateDoubleOfType(value, "IfcNormalisedRatioMeasure");
        }

        /// <summary>
        /// Creates an IFCData object as IfcSpecularExponent.
        /// </summary>
        /// <param name="value">The double value.</param>
        /// <returns>The IFCData object.</returns>
        public static IFCData CreateAsSpecularExponent(double value)
        {
            return IFCData.CreateDoubleOfType(value, "IfcSpecularExponent");
        }

        /// <summary>
        /// Creates an IFCData object as IfcPositiveRatioMeasure.
        /// </summary>
        /// <param name="value">The double value.</param>
        /// <returns>The IFCData object.</returns>
        public static IFCData CreateAsPositiveRatioMeasure(double value)
        {
            return IFCData.CreateDoubleOfType(value, "IfcPositiveRatioMeasure");
        }

        /// <summary>
        /// Creates an IFCData object as IfcLengthMeasure.
        /// </summary>
        /// <param name="value">The double value.</param>
        /// <returns>The IFCData object.</returns>
        public static IFCData CreateAsLengthMeasure(double value)
        {
            return IFCData.CreateDoubleOfType(value, "IfcLengthMeasure");
        }

        /// <summary>
        /// Creates an IFCData object as IfcVolumeMeasure.
        /// </summary>
        /// <param name="value">The double value.</param>
        /// <returns>The IFCData object.</returns>
        public static IFCData CreateAsVolumeMeasure(double value)
        {
            return IFCData.CreateDoubleOfType(value, "IfcVolumeMeasure");
        }

        /// <summary>
        /// Creates an IFCData object as IfcPositiveLengthMeasure.
        /// </summary>
        /// <param name="value">The double value.</param>
        /// <returns>The IFCData object.</returns>
        public static IFCData CreateAsPositiveLengthMeasure(double value)
        {
            return IFCData.CreateDoubleOfType(value, "IfcPositiveLengthMeasure");
        }

        /// <summary>
        /// Creates an IFCData object as IfcPlaneAngleMeasure.
        /// </summary>
        /// <param name="value">The double value.</param>
        /// <returns>The IFCData object.</returns>
        public static IFCData CreateAsPlaneAngleMeasure(double value)
        {
            return IFCData.CreateDoubleOfType(value, "IfcPlaneAngleMeasure");
        }

        /// <summary>
        /// Creates an IFCData object as IfcAreaMeasure.
        /// </summary>
        /// <param name="value">The double value.</param>
        /// <returns>The IFCData object.</returns>
        public static IFCData CreateAsAreaMeasure(double value)
        {
            return IFCData.CreateDoubleOfType(value, "IfcAreaMeasure");
        }

        /// <summary>
        /// Creates an IFCData object as IfcCountMeasure.
        /// </summary>
        /// <param name="value">The integer value.</param>
        /// <returns>The IFCData object.</returns>
        public static IFCData CreateAsCountMeasure(double value)
        {
            int valueAsInt = Convert.ToInt32(value);
            if (MathUtil.IsAlmostZero(value - valueAsInt))
                return IFCData.CreateIntegerOfType(valueAsInt, "IfcCountMeasure");
            else
                return IFCData.CreateDoubleOfType(value, "IfcCountMeasure");
        }

        /// <summary>
        /// Creates an IFCData object as IfcParameterValue.
        /// </summary>
        /// <param name="value">The double value.</param>
        /// <returns>The IFCData object.</returns>
        public static IFCData CreateAsParameterValue(double value)
        {
            return IFCData.CreateDoubleOfType(value, "IfcParameterValue");
        }

        /// <summary>
        /// Creates an IFCData object as IfcPowerMeasure.
        /// </summary>
        /// <param name="value">The double value.</param>
        /// <returns>The IFCData object.</returns>
        public static IFCData CreateAsPowerMeasure(double value)
        {
            return IFCData.CreateDoubleOfType(value, "IfcPowerMeasure");
        }
        
        /// <summary>
        /// Creates an IFCData object as IfcThermodynamicTemperatureMeasure.
        /// </summary>
        /// <param name="value">The double value.</param>
        /// <returns>The IFCData object.</returns>
        public static IFCData CreateAsThermodynamicTemperatureMeasure(double value)
        {
            return IFCData.CreateDoubleOfType(value, "IfcThermodynamicTemperatureMeasure");
        }

        /// <summary>
        /// Creates an IFCData object as IfcThermalTransmittanceMeasure.
        /// </summary>
        /// <param name="value">The double value.</param>
        /// <returns>The IFCData object.</returns>
        public static IFCData CreateAsThermalTransmittanceMeasure(double value)
        {
            return IFCData.CreateDoubleOfType(value, "IfcThermalTransmittanceMeasure");
        }

        /// <summary>
        /// Creates an IFCData object as IfcVolumetricFlowRate.
        /// </summary>
        /// <param name="value">The double value.</param>
        /// <returns>The IFCData object.</returns>
        public static IFCData CreateAsVolumetricFlowRateMeasure(double value)
        {
            return IFCData.CreateDoubleOfType(value, "IfcVolumetricFlowRateMeasure");
        }
    }
}
