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
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
    /// <summary>
    /// Represents an IfcGrid, which corresponds to a group of Revit Grid elements.
    /// The fields of the IFCGrid class correspond to the IfcGrid entity defined in the IFC schema.
    /// </summary>
    public class IFCGrid : IFCProduct
    {
        private IList<IFCGridAxis> m_UAxes = null;

        private IList<IFCGridAxis> m_VAxes = null;

        private IList<IFCGridAxis> m_WAxes = null;

        /// <summary>
        /// The required list of U Axes.
        /// </summary>
        public IList<IFCGridAxis> UAxes
        {
            get 
            { 
                if (m_UAxes == null)
                    m_UAxes = new List<IFCGridAxis>();
                
                return m_UAxes; 
            }
            private set { m_UAxes = value; }
        }

        /// <summary>
        /// The required list of V Axes.
        /// </summary>
        public IList<IFCGridAxis> VAxes
        {
            get 
            { 
                if (m_VAxes == null)
                    m_VAxes = new List<IFCGridAxis>();
                
                return m_VAxes; 
            }
            private set { m_VAxes = value; }
        }

        /// <summary>
        /// The optional list of W Axes.
        /// </summary>
        public IList<IFCGridAxis> WAxes
        {
            get 
            { 
                if (m_WAxes == null)
                    m_WAxes = new List<IFCGridAxis>();
                
                return m_WAxes; 
            }
            private set { m_WAxes = value; }
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        protected IFCGrid()
        {

        }

        /// <summary>
        /// Constructs an IFCGrid from the IfcGrid handle.
        /// </summary>
        /// <param name="ifcGrid">The IfcGrid handle.</param>
        protected IFCGrid(IFCAnyHandle ifcGrid)
        {
            Process(ifcGrid);
        }

        private IList<IFCGridAxis> ProcessOneAxis(IFCAnyHandle ifcGrid, string axisName)
        {
            IList<IFCGridAxis> gridAxes = new List<IFCGridAxis>();

            List<IFCAnyHandle> ifcAxes = IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(ifcGrid, axisName);
            if (ifcAxes != null)
            {
                foreach (IFCAnyHandle axis in ifcAxes)
                {
                    IFCGridAxis gridAxis = IFCGridAxis.ProcessIFCGridAxis(axis);
                    if (gridAxis != null)
                    {
                        if (gridAxis.DuplicateAxisId == -1)
                            gridAxes.Add(gridAxis);
                        else
                        {
                            IFCEntity originalEntity;
                            if (IFCImportFile.TheFile.EntityMap.TryGetValue(gridAxis.DuplicateAxisId, out originalEntity))
                            {
                                IFCGridAxis originalGridAxis = originalEntity as IFCGridAxis;
                                if (originalGridAxis != null)
                                    gridAxes.Add(originalGridAxis);
                            }
                        }
                    }
                }
            }

            return gridAxes;
        }

        /// <summary>
        /// Add to a set of created element ids, based on elements created from the contained grid axes.
        /// </summary>
        /// <param name="createdElementIds">The set of created element ids.</param>
        public override void GetCreatedElementIds(ISet<ElementId> createdElementIds)
        {
            foreach (IFCGridAxis uaxis in UAxes)
            {
                ElementId gridId = uaxis.CreatedElementId;
                if (gridId != ElementId.InvalidElementId)
                    createdElementIds.Add(gridId);
            }

            foreach (IFCGridAxis vaxis in VAxes)
            {
                ElementId gridId = vaxis.CreatedElementId;
                if (gridId != ElementId.InvalidElementId)
                    createdElementIds.Add(gridId);
            }

            foreach (IFCGridAxis waxis in WAxes)
            {
                ElementId gridId = waxis.CreatedElementId;
                if (gridId != ElementId.InvalidElementId)
                    createdElementIds.Add(gridId);
            }
        }

        /// <summary>
        /// Processes IfcGrid attributes.
        /// </summary>
        /// <param name="ifcGrid">The IfcGrid handle.</param>
        protected override void Process(IFCAnyHandle ifcGrid)
        {
            base.Process(ifcGrid);

            // We will be lenient and allow for missing U and V axes.
            UAxes = ProcessOneAxis(ifcGrid, "UAxes");
            VAxes = ProcessOneAxis(ifcGrid, "VAxes");
            WAxes = ProcessOneAxis(ifcGrid, "WAxes");
        }

        private void CreateOneDirection(IList<IFCGridAxis> axes, Document doc, Transform lcs)
        {
            foreach (IFCGridAxis axis in axes)
            {
                try
                {
                    axis.Create(doc, lcs);
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Creates or populates Revit elements based on the information contained in this class.
        /// </summary>
        /// <param name="doc">The document.</param>
        protected override void Create(Document doc)
        {
            Transform lcs = (ObjectLocation != null) ? ObjectLocation.TotalTransform : Transform.Identity;

            CreateOneDirection(UAxes, doc, lcs);
            CreateOneDirection(VAxes, doc, lcs);
            CreateOneDirection(WAxes, doc, lcs);

            ISet<ElementId> createdElementIds = new HashSet<ElementId>();
            GetCreatedElementIds(createdElementIds);

            foreach (ElementId createdElementId in createdElementIds)
            {
                CreatedElementId = createdElementId;
                CreateParameters(doc);
            }

            CreatedElementId = ElementId.InvalidElementId;
        }

        /// <summary>
        /// Processes an IfcGrid object.
        /// </summary>
        /// <param name="ifcGrid">The IfcGrid handle.</param>
        /// <returns>The IFCGrid object.</returns>
        public static IFCGrid ProcessIFCGrid(IFCAnyHandle ifcGrid)
        {
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcGrid))
            {
                IFCImportFile.TheLog.LogNullError(IFCEntityType.IfcGrid);
                return null;
            }

            IFCEntity grid;
            if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcGrid.StepId, out grid))
                grid = new IFCGrid(ifcGrid);
            return (grid as IFCGrid);
        }
    }
}
