//
// Revit IFC Import library: this library works with Autodesk(R) Revit(R) to import IFC files.
// Copyright (C) 2015  Autodesk, Inc.
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
using Revit.IFC.Import.Data;
using UnitSystem = Autodesk.Revit.DB.DisplayUnit;
using UnitName = Autodesk.Revit.DB.DisplayUnitType;

namespace Revit.IFC.Import.Utility
{
   /// <summary>
   /// Provides methods to scale IFC units.
   /// </summary>
   public class IFCUnitUtil
   {
      /// <summary>
      /// Converts a value from default unit to the project unit for the active IFCProject.
      /// </summary>
      /// <param name="unitType">The unit type.</param>
      /// <param name="inValue">The value to convert.</param>
      /// <returns>The result value.</returns>
      static public double ScaleValue(UnitType unitType, double inValue)
      {
         return IFCUnitUtil.ProjectScale(unitType, inValue);
      }

      /// <summary>
      /// Converts a value from default unit to the project unit for the active IFCProject.
      /// </summary>
      /// <param name="unitType">The unit type.</param>
      /// <param name="inValue">The value to convert.</param>
      /// <returns>The result value.</returns>
      static public XYZ ScaleValue(UnitType unitType, XYZ inValue)
      {
         return IFCUnitUtil.ProjectScale(unitType, inValue);
      }

      /// <summary>
      /// Converts values from default unit to the project unit for the active IFCProject.
      /// </summary>
      /// <param name="unitType">The unit type.</param>
      /// <param name="inValues">The value to convert.</param>
      static public void ScaleValues(UnitType unitType, IList<XYZ> inValues)
      {
         IFCUnitUtil.ProjectScale(unitType, inValues);
      }

      /// <summary>
      /// Converts an angle from default unit to the project unit for the active IFCProject.
      /// </summary>
      /// <param name="inValue">The angle to convert.</param>
      /// <returns>The resulting angle.</returns>
      static public double ScaleAngle(double inValue)
      {
         return ScaleValue(UnitType.UT_Angle, inValue);
      }

      /// <summary>
      /// Converts a length from default unit to the project unit for the active IFCProject.
      /// </summary>
      /// <param name="inValue">The length to convert.</param>
      /// <returns>The resulting length.</returns>
      static public double ScaleLength(double inValue)
      {
         return ScaleValue(UnitType.UT_Length, inValue);
      }

      /// <summary>
      /// Converts a length from default unit to the project unit for the active IFCProject.
      /// </summary>
      /// <param name="inValue">The length to convert.</param>
      /// <returns>The resulting length.</returns>
      static public XYZ ScaleLength(XYZ inValue)
      {
         return ScaleValue(UnitType.UT_Length, inValue);
      }

      /// <summary>
      /// Converts lengths from default unit to the project unit for the active IFCProject.
      /// </summary>
      /// <param name="inValues">The lengths to convert.</param>
      static public void ScaleLengths(IList<XYZ> inValues)
      {
         ScaleValues(UnitType.UT_Length, inValues);
      }

      /// <summary>
      /// Converts a value from default unit to the project unit.
      /// </summary>
      /// <param name="unitType">The unit type.</param>
      /// <param name="inValue">The value to convert.</param>
      /// <returns>The result value.</returns>
      static public double ProjectScale(UnitType unitType, double inValue)
      {
         IFCUnit projectUnit = IFCImportFile.TheFile.IFCUnits.GetIFCProjectUnit(unitType);
         if (projectUnit != null)
            return inValue * projectUnit.ScaleFactor - projectUnit.OffsetFactor;

         return inValue;
      }

      /// <summary>
      /// Converts a value from default unit to the project unit.
      /// </summary>
      /// <param name="unitType">The unit type.</param>
      /// <param name="inValue">The value to convert.</param>
      /// <returns>The result value.</returns>
      /// <remarks>Note the the OffsetFactor is ignored, as it is irrelevant for a location.</remarks>
      static public XYZ ProjectScale(UnitType unitType, XYZ inValue)
      {
         IFCUnit projectUnit = IFCImportFile.TheFile.IFCUnits.GetIFCProjectUnit(unitType);
         if (projectUnit != null)
            return inValue * projectUnit.ScaleFactor;

         return inValue;
      }

      /// <summary>
      /// Converts values from default unit to the project unit.
      /// </summary>
      /// <param name="unitType">The unit type.</param>
      /// <param name="inValues">The value to convert.</param>
      /// <remarks>Note the the OffsetFactor is ignored, as it is irrelevant for a location.</remarks>
      static public void ProjectScale(UnitType unitType, IList<XYZ> inValues)
      {
         if (inValues == null)
            return;

         IFCUnit projectUnit = IFCImportFile.TheFile.IFCUnits.GetIFCProjectUnit(unitType);
         if (projectUnit == null)
            return;

         double factor = projectUnit.ScaleFactor;
         if (MathUtil.IsAlmostEqual(factor, 1.0))
            return;

         int count = inValues.Count;
         for (int ii = 0; ii < count; ii++)
            inValues[ii] *= factor;
      }

      static public string FormatLengthAsString(double value)
      {
         FormatValueOptions formatValueOptions = new FormatValueOptions();
         formatValueOptions.AppendUnitSymbol = true;
         return UnitFormatUtils.Format(IFCImportFile.TheFile.Document.GetUnits(), UnitType.UT_Length, value, true, false, formatValueOptions);
      }
   }
}
