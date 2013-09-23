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
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using BIM.IFC.Utility;

namespace BIM.IFC.Exporter.PropertySet.Calculators
{
    /// <summary>
    /// A calculation class to calculate slope value for a beam.
    /// </summary>
    class BeamSlopeCalculator : PropertyCalculator
    {
        /// <summary>
        /// A double variable to keep the calculated value.
        /// </summary>
        private double m_Slope = 0;

        /// <summary>
        /// A static instance of this class.
        /// </summary>
        static BeamSlopeCalculator s_Instance = new BeamSlopeCalculator();

        /// <summary>
        /// The BeamSlopeCalculator instance.
        /// </summary>
        public static BeamSlopeCalculator Instance
        {
            get { return s_Instance; }
        }

        /// <summary>
        /// Calculates slope value for a beam.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="extrusionCreationData">The IFCExtrusionCreationData.</param>
        /// <param name="element">The element to calculate the value.</param>
        /// <param name="elementType">The element type.</param>
        /// <returns>True if the operation succeed, false otherwise.</returns>
        public override bool Calculate(ExporterIFC exporterIFC, IFCExtrusionCreationData extrusionCreationData, Element element, ElementType elementType)
        {
            // We may have an extrusionCreationData that doesn't have anything set.  We will check this by seeing if there is a valid length set.
            if (extrusionCreationData == null || MathUtil.IsAlmostZero(extrusionCreationData.ScaledLength))
            {
                // Try looking for parameters that we can calculate slope from.
                double startParamHeight;
                if (ParameterUtil.GetDoubleValueFromElement(element, BuiltInParameter.STRUCTURAL_BEAM_END0_ELEVATION, out startParamHeight) == null)
                    return false;
                double endParamHeight;
                if (ParameterUtil.GetDoubleValueFromElement(element, BuiltInParameter.STRUCTURAL_BEAM_END1_ELEVATION, out endParamHeight) == null)
                    return false;
                double length;
                if (ParameterUtil.GetDoubleValueFromElement(element, BuiltInParameter.INSTANCE_LENGTH_PARAM, out length) == null)
                    return false;
                m_Slope = Math.Atan2(Math.Abs(endParamHeight - startParamHeight), length) * 180 / Math.PI;
                return true;
            }
            m_Slope = extrusionCreationData.Slope;
            return true;
        }

        /// <summary>
        /// Gets the calculated double value.
        /// </summary>
        /// <returns>
        /// The double value.
        /// </returns>
        public override double GetDoubleValue()
        {
            return m_Slope;
        }
    }
}
