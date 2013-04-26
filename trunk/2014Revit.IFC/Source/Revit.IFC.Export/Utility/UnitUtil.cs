﻿//
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
using Revit.IFC.Export.Exporter;
using Revit.IFC.Export.Toolkit;

namespace Revit.IFC.Export.Utility
{
    /// <summary>
    /// Utilities to work with ExporterCacheManager.UnitCache.
    /// </summary>
    public class UnitUtil
    {
        /// <summary>
        /// Returns the scaling factor for length from Revit internal units to IFC units.
        /// </summary>
        /// <returns>The scaling factors.</returns>
        /// <remarks>This routine is intended to be used for API routines that expect a scale parameter.
        /// For .NET routines, use ScaleLength() instead.</remarks>
        static public double ScaleLengthForRevitAPI()
        {
            return ExporterCacheManager.UnitsCache.Scale(UnitType.UT_Length, 1.0);
        }

        /// <summary>
        /// Converts a length in Revit internal units to IFC units.
        /// </summary>
        /// <param name="unscaledLength">The length in Revit internal units.</param>
        /// <returns>The length in IFC units.</returns>
        static public double ScaleLength(double unscaledLength)
        {
            return ExporterCacheManager.UnitsCache.Scale(UnitType.UT_Length, unscaledLength);
        }

        /// <summary>
        /// Converts a position in Revit internal units to IFC units.
        /// </summary>
        /// <param name="unscaledUV">The position in Revit internal units.</param>
        /// <returns>The position in IFC units.</returns>
        static public UV ScaleLength(UV unscaledUV)
        {
            return ExporterCacheManager.UnitsCache.Scale(UnitType.UT_Length, unscaledUV);
        }

        /// <summary>
        /// Converts a position in Revit internal units to IFC units.
        /// </summary>
        /// <param name="unscaledXYZ">The position in Revit internal units.</param>
        /// <returns>The position in IFC units.</returns>
        static public XYZ ScaleLength(XYZ unscaledXYZ)
        {
            return ExporterCacheManager.UnitsCache.Scale(UnitType.UT_Length, unscaledXYZ);
        }

        /// <summary>
        /// Converts a power value in Revit internal units to IFC units.
        /// </summary>
        /// <param name="unscaledPower">The power value in Revit internal units.</param>
        /// <returns>The volume in IFC units.</returns>
        static public double ScalePower(double unscaledPower)
        {
            return ExporterCacheManager.UnitsCache.Scale(UnitType.UT_HVAC_Power, unscaledPower);
        }

        /// <summary>
        /// Converts an area in Revit internal units to IFC units.
        /// </summary>
        /// <param name="unscaledArea">The area in Revit internal units.</param>
        /// <returns>The area in IFC units.</returns>
        static public double ScaleArea(double unscaledArea)
        {
            return ExporterCacheManager.UnitsCache.Scale(UnitType.UT_Area, unscaledArea);
        }

        /// <summary>
        /// Converts a volume in Revit internal units to IFC units.
        /// </summary>
        /// <param name="unscaledVolume">The volume in Revit internal units.</param>
        /// <returns>The volume in IFC units.</returns>
        static public double ScaleVolume(double unscaledVolume)
        {
            return ExporterCacheManager.UnitsCache.Scale(UnitType.UT_Volume, unscaledVolume);
        }

        /// <summary>
        /// Converts a VolumetricFlowRate in Revit internal units to IFC units.
        /// </summary>
        /// <param name="unscaledVolumetricFlowRate">The volumetric flow rate in Revit internal units.</param>
        /// <returns>The volumetric flow rate in IFC units.</returns>
        static public double ScaleVolumetricFlowRate(double unscaledVolumetricFlowRate)
        {
            return ExporterCacheManager.UnitsCache.Scale(UnitType.UT_HVAC_Airflow, unscaledVolumetricFlowRate);
        }

        /// <summary>
        /// Converts an angle in Revit internal units to Revit display units.
        /// </summary>
        /// <param name="unscaledArea">The angle in Revit internal units.</param>
        /// <returns>The angle in Revit display units.</returns>
        static public double ScaleAngle(double unscaledAngle)
        {
            return ExporterCacheManager.UnitsCache.Scale(UnitType.UT_Angle, unscaledAngle);
        }

        // <summary>
        /// Converts an electrical current in Revit internal units to Revit display units.
        /// </summary>
        /// <param name="unscaledCurrent">The electrical current in Revit internal units.</param>
        /// <returns>The electrical current in Revit display units.</returns>
        static public double ScaleElectricalCurrent(double unscaledCurrent)
        {
            return ExporterCacheManager.UnitsCache.Scale(UnitType.UT_Electrical_Current, unscaledCurrent);
        }

        // <summary>
        /// Converts an electrical voltage in Revit internal units to Revit display units.
        /// </summary>
        /// <param name="unscaledVoltage">The elecrical voltage in Revit internal units.</param>
        /// <returns>The electrical current in Revit display units.</returns>
        static public double ScaleElectricalVoltage(double unscaledVoltage)
        {
            return ExporterCacheManager.UnitsCache.Scale(UnitType.UT_Electrical_Potential, unscaledVoltage);
        }
        
        /// <summary>
        /// Converts a position in IFC units to Revit internal units.
        /// </summary>
        /// <param name="unscaledArea">The position in IFC units.</param>
        /// <returns>The position in Revit internal units.</returns>
        static public XYZ UnscaleLength(XYZ scaledXYZ)
        {
            return ExporterCacheManager.UnitsCache.Unscale(UnitType.UT_Length, scaledXYZ);
        }

        /// <summary>
        /// Converts a position in IFC units to Revit internal units.
        /// </summary>
        /// <param name="scaledLength">The length in IFC units.</param>
        /// <returns>The length in Revit internal units.</returns>
        static public double UnscaleLength(double scaledLength)
        {
            return ExporterCacheManager.UnitsCache.Unscale(UnitType.UT_Length, scaledLength);
        }
    }
}
