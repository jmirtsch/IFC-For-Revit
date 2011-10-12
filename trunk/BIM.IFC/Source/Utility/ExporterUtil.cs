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
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;

namespace BIM.IFC.Utility
{
    /// <summary>
    /// Provides general utility methods for IFC export.
    /// </summary>
    class ExporterUtil
    {
        /// <summary>
        /// Creates IfcAxis2Placement3D object.
        /// </summary>
        /// <param name="file">
        /// The IFC file.
        /// </param>
        /// <param name="origin">
        /// The origin.
        /// </param>
        /// <param name="zDirection">
        /// The Z direction.
        /// </param>
        /// <param name="xDirection">
        /// The X direction.
        /// </param>
        /// <returns>
        /// The handle.
        /// </returns>
        public static IFCAnyHandle CreateAxis(IFCFile file, XYZ origin, XYZ zDirection, XYZ xDirection)
        {
            IFCAnyHandle directionOpt = IFCAnyHandle.Create();
            IFCAnyHandle refOpt = IFCAnyHandle.Create();
            IFCAnyHandle location = IFCAnyHandle.Create();

            if (origin != null)
            {
                IList<double> measure = new List<double>();
                measure.Add(origin.X); measure.Add(origin.Y); measure.Add(origin.Z);
                location = CreateCartesianPoint(file, measure);
            }
            else
            {
                location = ExporterIFCUtils.GetGlobal3DOriginHandle();
            }

            bool exportzDirectionAndxDirection = (zDirection != null && xDirection != null && (!MathUtil.IsAlmostEqual(zDirection[2], 1.0) || !MathUtil.IsAlmostEqual(xDirection[0], 1.0)));

            if (exportzDirectionAndxDirection)
            {
                IList<double> axisPts = new List<double>();
                axisPts.Add(zDirection.X); axisPts.Add(zDirection.Y); axisPts.Add(zDirection.Z);
                directionOpt = CreateDirection(file, axisPts);
            }

            if (exportzDirectionAndxDirection)
            {
                IList<double> axisPts = new List<double>();
                axisPts.Add(xDirection.X); axisPts.Add(xDirection.Y); axisPts.Add(xDirection.Z);
                refOpt = CreateDirection(file, axisPts);
            }

            return file.CreateAxis2Placement3D(location, directionOpt, refOpt);
        }

        /// <summary>
        /// Creates IfcDirection object.
        /// </summary>
        /// <param name="file">
        /// The IFC file.
        /// </param>
        /// <param name="realList">
        /// The list of doubles to create the direction.
        /// </param>
        /// <returns>
        /// The handle.
        /// </returns>
        public static IFCAnyHandle CreateDirection(IFCFile file, IList<double> realList)
        {
            IList<double> cleanList = new List<double>();

            foreach (double measure in realList)
            {
                double ceilMeasure = Math.Ceiling(measure);
                double floorMeasure = Math.Floor(measure);

                if (MathUtil.IsAlmostEqual(measure, ceilMeasure))
                    cleanList.Add(ceilMeasure);
                else if (MathUtil.IsAlmostEqual(measure, floorMeasure))
                    cleanList.Add(floorMeasure);
                else
                    cleanList.Add(measure);
            }

            int sz = realList.Count;

            if (sz == 3)
            {
                for (int ii = 0; ii < 3; ii++)
                {
                    if (MathUtil.IsAlmostEqual(cleanList[ii], 1.0))
                    {
                        if (!MathUtil.IsAlmostZero(cleanList[(ii + 1) % 3]) || !MathUtil.IsAlmostZero(cleanList[(ii + 2) % 3]))
                            break;
                        return ExporterIFCUtils.GetGlobal3DDirectionHandles(true)[ii];
                    }
                    else if (MathUtil.IsAlmostEqual(cleanList[ii], -1.0))
                    {
                        if (!MathUtil.IsAlmostZero(cleanList[(ii + 1) % 3]) || !MathUtil.IsAlmostZero(cleanList[(ii + 2) % 3]))
                            break;
                        return ExporterIFCUtils.GetGlobal3DDirectionHandles(false)[ii];
                    }
                }
            }
            else if (sz == 2)
            {
                for (int ii = 0; ii < 2; ii++)
                {
                    if (MathUtil.IsAlmostEqual(cleanList[ii], 1.0))
                    {
                        if (!MathUtil.IsAlmostZero(cleanList[1 - ii]))
                            break;
                        return ExporterIFCUtils.GetGlobal2DDirectionHandles(true)[ii];
                    }
                    else if (MathUtil.IsAlmostEqual(cleanList[ii], -1.0))
                    {
                        if (!MathUtil.IsAlmostZero(cleanList[1 - ii]))
                            break;
                        return ExporterIFCUtils.GetGlobal2DDirectionHandles(false)[ii];
                    }
                }
            }

            IFCAnyHandle directionHandle = file.CreateDirection(cleanList);
            return directionHandle;
        }

        /// <summary>
        /// Creates IfcCartesianPoint object.
        /// </summary>
        /// <param name="file">
        /// The IFC file.
        /// </param>
        /// <param name="measure">
        /// The list of doubles to create the Cartesian point.
        /// </param>
        /// <returns>
        /// The handle.
        /// </returns>
        public static IFCAnyHandle CreateCartesianPoint(IFCFile file, IList<double> measure)
        {
            IList<double> cleanMeasure = new List<double>();
            foreach (double value in measure)
            {
                double ceilMeasure = Math.Ceiling(value);
                double floorMeasure = Math.Floor(value);

                if (MathUtil.IsAlmostEqual(value, ceilMeasure))
                    cleanMeasure.Add(ceilMeasure);
                else if (MathUtil.IsAlmostEqual(value, floorMeasure))
                    cleanMeasure.Add(floorMeasure);
                else
                    cleanMeasure.Add(value);
            }

            if (MathUtil.IsAlmostZero(cleanMeasure[0]) && MathUtil.IsAlmostZero(cleanMeasure[1]))
            {
                if (measure.Count == 2)
                {
                    return ExporterIFCUtils.GetGlobal2DOriginHandle();
                }
                if (measure.Count == 3 && MathUtil.IsAlmostZero(cleanMeasure[2]))
                {
                    return ExporterIFCUtils.GetGlobal3DOriginHandle();
                }

            }

            IFCAnyHandle pointHandle = file.CreateCartesianPoint(cleanMeasure);

            return pointHandle;
        }

        /// <summary>
        /// Creates IfcMappedItem object.
        /// </summary>
        /// <param name="file">
        /// The IFC file.
        /// </param>
        /// <param name="repMap">
        /// The handle to be mapped.
        /// </param>
        /// <param name="orig">
        /// The orig for mapping transformation.
        /// </param>
        /// <returns>
        /// The handle.
        /// </returns>
        public static IFCAnyHandle CreateDefaultMappedItem(IFCFile file, IFCAnyHandle repMap, XYZ orig)
        {
            IFCAnyHandle origin = file.CreateCartesianPoint(orig);
            IFCMeasureValue scale = IFCMeasureValue.Create(1.0);
            IFCAnyHandle mappingTarget =
               file.CreateCartesianTransformationOperator3D(IFCAnyHandle.Create(), IFCAnyHandle.Create(), origin, scale, IFCAnyHandle.Create());
            return file.CreateMappedItem(repMap, mappingTarget);
        }

        /// <summary>
        /// Creates a copy of local placement object.
        /// </summary>
        /// <param name="file">
        /// The IFC file.
        /// </param>
        /// <param name="originalPlacement">
        /// The original placement object to be copied.
        /// </param>
        /// <returns>
        /// The handle.
        /// </returns>
        public static IFCAnyHandle CopyLocalPlacement(IFCFile file, IFCAnyHandle originalPlacement)
        {
            IFCAnyHandle placementRelToOpt = IFCGeometryUtils.GetPlacementRelToFromLocalPlacement(originalPlacement);
            IFCAnyHandle relativePlacement = IFCGeometryUtils.GetRelativePlacementFromLocalPlacement(originalPlacement);
            return file.CreateLocalPlacement(placementRelToOpt, relativePlacement);
        }

        /// <summary>
        /// Gets export type for an element.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="element">
        /// The element.
        /// </param>
        /// <param name="enumTypeValue">
        /// The output string value represents the enum type.
        /// </param>
        /// <returns>
        /// The IFCExportType.
        /// </returns>
        public static IFCExportType GetExportType(ExporterIFC exporterIFC, Element element,
           out string enumTypeValue)
        {
            enumTypeValue = "";
            IFCExportType exportType = IFCExportType.DontExport;

            // Get potential override value first.
            {
                string symbolClassName;

                string exportAsEntity = "IFCExportAs";
                string exportAsType = "IFCExportType";

                ParameterUtil.GetStringValueFromElementOrSymbol(element, exportAsEntity, out symbolClassName);
                ParameterUtil.GetStringValueFromElementOrSymbol(element, exportAsType, out enumTypeValue);

                if (!String.IsNullOrEmpty(symbolClassName))
                {
                    exportType = ElementFilteringUtil.GetExportTypeFromClassName(symbolClassName);
                    if (exportType != IFCExportType.DontExport)
                        return exportType;
                }
            }

            Category category = element.Category;
            if (category == null)
                return IFCExportType.DontExport;

            ElementId categoryId = category.Id;

            string ifcClassName = ExporterIFCUtils.GetIFCClassName(element, exporterIFC);
            if (ifcClassName != "")
            {
                enumTypeValue = ExporterIFCUtils.GetIFCType(elem, exporterIFC);
                // if using name, override category id if match is found.
                if (!ifcClassName.Equals("Default", StringComparison.OrdinalIgnoreCase))
                    exportType = ElementFilteringUtil.GetExportTypeFromClassName(ifcClassName);
            }

            // if not set, fall back on category id.
            if (exportType == IFCExportType.DontExport)
            {
                //bool exportSeparately = true;
                exportType = ElementFilteringUtil.GetExportTypeFromCategoryId(categoryId, out enumTypeValue /*, out bool exportSeparately*/);
            }

            // if not set, fall back on symbol functions.
            // allow override of IfcBuildingElementProxy.
            if ((exportType == IFCExportType.DontExport) || (exportType == IFCExportType.ExportBuildingElementProxy))
            {
                // TODO: add isColumn.
                //if (familySymbol.IsColumn())
                //exportType = IFCExportType.ExportColumnType;
                //else 
                FamilyInstance familyInstance = element as FamilyInstance;
                if (familyInstance != null)
                {
                    switch (familyInstance.StructuralType)
                    {
                        case Autodesk.Revit.DB.Structure.StructuralType.Beam:
                            exportType = IFCExportType.ExportBeam;
                            break;
                        case Autodesk.Revit.DB.Structure.StructuralType.Footing:
                            exportType = IFCExportType.ExportFooting;
                            break;
                    }
                }
            }

            return exportType;
        }
    }
}
