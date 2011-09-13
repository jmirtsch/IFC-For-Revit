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
    /// Provides static methods for mathematical functions.
    /// </summary>
    class MathUtil
    {
        /// <summary>
        /// Returns a small value for use in comparing doubles.
        /// </summary>
        /// <returns>
        /// The value.
        /// </returns>
        public static double Eps()
        {
            return 1.0e-9;
        }

        /// <summary>
        /// Check if two double variables are almost equal.
        /// </summary>
        /// <param name="value1"> The double value to be compared. </param>
        /// <param name="value2"> Another double value to be compared. </param>
        /// <returns>
        /// True if they are almost equal, false otherwise.
        /// </returns>
        public static bool IsAlmostEqual(double value1, double value2)
        {
            double sum = Math.Abs(value1) + Math.Abs(value2);
            if (sum < Eps())
                return true;
            return (Math.Abs(value1 - value2) <= sum * Eps());
        }

        /// <summary>
        /// Check if the double variable is almost equal to zero.
        /// </summary>
        /// <param name="value">The double value.</param>
        /// <returns>
        /// True if the value is almost zero, false otherwise.
        /// </returns>
        public static bool IsAlmostZero(double value)
        {
            return Math.Abs(value) <= Eps();
        }
    }
}
