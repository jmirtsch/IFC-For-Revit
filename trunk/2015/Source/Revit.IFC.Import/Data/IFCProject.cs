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
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
    /// <summary>
    /// Represents an IfcProject.
    /// </summary>
    public class IFCProject : IFCObject
    {
        List<double> m_TrueNorthDirection = null;

        HashSet<IFCUnit> m_UnitsInContext = null;

        /// <summary>
        /// The true north direction of the project.
        /// </summary>
        public List<double> TrueNorthDirection
        {
            get { return m_TrueNorthDirection; }
        }

        /// <summary>
        /// The units in the project.
        /// </summary>
        public HashSet<IFCUnit> UnitsInContext
        {
            get { return m_UnitsInContext; }
        }

        /// <summary>
        /// Constructs an IFCProject from the IfcProject handle.
        /// </summary>
        /// <param name="ifcProject">The IfcProject handle.</param>
        protected IFCProject(IFCAnyHandle ifcProject)
        {
            IFCImportFile.TheFile.IFCProject = this;
            Process(ifcProject);
        }

        /// <summary>
        /// Returns true if sub-elements should be grouped; false otherwise.
        /// </summary>
        public override bool GroupSubElements()
        {
            return false;
        }

        /// <summary>
        /// Processes IfcProject attributes.
        /// </summary>
        /// <param name="ifcProjectHandle">The IfcProject handle.</param>
        protected override void Process(IFCAnyHandle ifcProjectHandle)
        {
            IFCAnyHandle unitsInContext = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcProjectHandle, "UnitsInContext", false);

            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(unitsInContext))
            {
                IList<IFCAnyHandle> units = IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(unitsInContext, "Units");

                if (units != null)
                {
                    m_UnitsInContext = new HashSet<IFCUnit>();

                    foreach (IFCAnyHandle unit in units)
                    {
                        IFCUnit ifcUnit = IFCImportFile.TheFile.IFCUnits.ProcessIFCProjectUnit(unit);
                        if (!IFCUnit.IsNullOrInvalid(ifcUnit))
                            m_UnitsInContext.Add(ifcUnit);
                    }
                }
                else
                {
                    IFCImportFile.TheLog.LogMissingRequiredAttributeError(unitsInContext, "Units", false);
                }
            }

            // We need to process the units before we process the rest of the file, since we will scale values as we go along.
            base.Process(ifcProjectHandle);

            // process true north - take the first valid representation context that has a true north value.
            HashSet<IFCAnyHandle> repContexts = IFCAnyHandleUtil.GetAggregateInstanceAttribute<HashSet<IFCAnyHandle>>(ifcProjectHandle, "RepresentationContexts");

            if (repContexts != null)
            {
                foreach (IFCAnyHandle geomRepContextHandle in repContexts)
                {
                    if (!IFCAnyHandleUtil.IsNullOrHasNoValue(geomRepContextHandle) &&
                        IFCAnyHandleUtil.IsSubTypeOf(geomRepContextHandle, IFCEntityType.IfcGeometricRepresentationContext))
                    {
                        IFCAnyHandle trueNorthHandle = IFCAnyHandleUtil.GetInstanceAttribute(geomRepContextHandle, "TrueNorth");
                        if (!IFCAnyHandleUtil.IsNullOrHasNoValue(trueNorthHandle))
                        {
                            List<double> trueNorthDir = IFCAnyHandleUtil.GetAggregateDoubleAttribute<List<double>>(trueNorthHandle, "DirectionRatios");
                            if (trueNorthDir != null && trueNorthDir.Count >= 2)
                            {
                                m_TrueNorthDirection = trueNorthDir;
                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates or populates Revit elements based on the information contained in this class.
        /// </summary>
        /// <param name="doc">The document.</param>
        protected override void Create(Document doc)
        {
            Units documentUnits = new Units(doc.DisplayUnitSystem == DisplayUnit.METRIC ?
                UnitSystem.Metric : UnitSystem.Imperial); 
            foreach (IFCUnit unit in UnitsInContext)
            {
                if (!IFCUnit.IsNullOrInvalid(unit))
                {
                    try
                    {
                        FormatOptions formatOptions = new FormatOptions(unit.UnitName);
                        formatOptions.UnitSymbol = unit.UnitSymbol;
                        documentUnits.SetFormatOptions(unit.UnitType, formatOptions);
                    }
                    catch (Exception ex)
                    {
                        IFCImportFile.TheLog.LogError(unit.Id, ex.Message, false);
                    }
                }
            }
            doc.SetUnits(documentUnits);

            base.Create(doc);
        }

        /// <summary>
        /// Cleans out the IFCEntity to save memory.
        /// </summary>
        public override void CleanEntity()
        {
            base.CleanEntity();
        }

        /// <summary>
        /// Processes an IfcProject object.
        /// </summary>
        /// <param name="ifcProject">The IfcProject handle.</param>
        /// <returns>The IFCProject object.</returns>
        public static IFCProject ProcessIFCProject(IFCAnyHandle ifcProject)
        {
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcProject))
            {
                IFCImportFile.TheLog.LogNullError(IFCEntityType.IfcProject);
                return null;
            }

            IFCEntity project;
            if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcProject.StepId, out project))
                return (project as IFCProject);

            if (IFCAnyHandleUtil.IsSubTypeOf(ifcProject, IFCEntityType.IfcProject))
            {
                return new IFCProject(ifcProject);
            }

            //LOG: ERROR: Not processed project.
            return null;
        }
    }
}
