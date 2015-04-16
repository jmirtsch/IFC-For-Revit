//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
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

namespace Revit.IFC.Export.Utility
{
    /// <summary>
    /// Used to keep a cache of the element ids mapping to a IfcMaterialLayerSet handle.
    /// </summary>
    public class MaterialLayerSetCache
    {
        /// <summary>
        /// The dictionary mapping from an ElementId to an IfcMaterialLayerSet handle. 
        /// </summary>
        private Dictionary<ElementId, IFCAnyHandle> m_ElementIdToMatLayerSetDictionary = new Dictionary<ElementId, IFCAnyHandle>();

        /// <summary>
        /// The dictionary mapping from an ElementId to a primary IfcMaterial handle. 
        /// </summary>
        private Dictionary<ElementId, IFCAnyHandle> m_ElementIdToMaterialHndDictionary = new Dictionary<ElementId, IFCAnyHandle>();

        /// <summary>
        /// Finds the IfcMaterialLayerSet handle from the dictionary.
        /// </summary>
        /// <param name="id">
        /// The element id.
        /// </param>
        /// <returns>
        /// The IfcMaterialLayerSet handle.
        /// </returns>
        public IFCAnyHandle Find(ElementId id)
        {
            IFCAnyHandle handle;
            if (m_ElementIdToMatLayerSetDictionary.TryGetValue(id, out handle))
            {
                return handle;
            }
            return null;
        }

        /// <summary>
        /// Adds the IfcMaterialLayerSet handle to the dictionary.
        /// </summary>
        /// <param name="elementId">
        /// The element elementId.
        /// </param>
        /// <param name="handle">
        /// The IfcMaterialLayerSet handle.
        /// </param>
        public void Register(ElementId elementId, IFCAnyHandle handle)
        {
            if (m_ElementIdToMatLayerSetDictionary.ContainsKey(elementId))
                return;

            m_ElementIdToMatLayerSetDictionary[elementId] = handle;
        }

        /// <summary>
        /// Finds the primary IfcMaterial handle from the dictionary.
        /// </summary>
        /// <param name="id">
        /// The element id.
        /// </param>
        /// <returns>
        /// The IfcMaterial handle.
        /// </returns>
        public IFCAnyHandle FindPrimaryMaterialHnd(ElementId id)
        {
            IFCAnyHandle handle;
            if (m_ElementIdToMaterialHndDictionary.TryGetValue(id, out handle))
            {
                return handle;
            }
            return null;
        }

        /// <summary>
        /// Adds the primary IfcMaterial handle to the dictionary.
        /// </summary>
        /// <param name="elementId">
        /// The element elementId.
        /// </param>
        /// <param name="handle">
        /// The IfcMaterial handle.
        /// </param>
        public void RegisterPrimaryMaterialHnd(ElementId elementId, IFCAnyHandle handle)
        {
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(handle))
                return;

            if (m_ElementIdToMaterialHndDictionary.ContainsKey(elementId))
                return;

            m_ElementIdToMaterialHndDictionary[elementId] = handle;
        }
    }
}
