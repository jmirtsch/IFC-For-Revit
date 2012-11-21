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
using BIM.IFC.Toolkit;

namespace BIM.IFC.Utility
{
    /// <summary>
    /// A multi-purpose wrapper used within the export of an element to:
    /// 1. Associate IFC handles to their container (e.g., levels, buildings)
    /// 2. Create properties and quantities for top-level handles.
    /// </summary>
    /// <remarks>The intention of this class is to phase out the use of IFCProductWrapper.  As long as handles are created in native code,
    /// IFCProductWrapper will be necessary, but it should be used for as little as possible.  Note that items added directly to the
    /// ProductWrapper and not the internal IFCProductWrapper will not show up on entity counters in journal files.</remarks>
    public class ProductWrapper : System.IDisposable
    {
        HashSet<IFCAnyHandle> m_createdHandles = new HashSet<IFCAnyHandle>();

        IFCProductWrapper m_InternalWrapper = null;

        ProductWrapper m_ParentWrapper = null;

        /// <summary>
        /// Standard static Create function. 
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="allowRelateToLevel">Whether or not handles are allowed to be related to levels.</param>
        /// <returns>A new ProductWrapper.</returns>
        public static ProductWrapper Create(ExporterIFC exporterIFC, bool allowRelateToLevel)
        {
            ProductWrapper productWrapper = new ProductWrapper();
            productWrapper.m_InternalWrapper = IFCProductWrapper.Create(exporterIFC, allowRelateToLevel);
            return productWrapper;
        }

        /// <summary>
        /// Static Create function for a child wrapper.
        /// </summary>
        /// <param name="parentWrapper">The parent wrapper.</param>
        /// <returns>A new ProductWrapper.</returns>
        public static ProductWrapper Create(ProductWrapper parentWrapper)
        {
            ProductWrapper productWrapper = new ProductWrapper();
            productWrapper.m_InternalWrapper = IFCProductWrapper.Create(parentWrapper.m_InternalWrapper);
            productWrapper.m_ParentWrapper = parentWrapper;
            return productWrapper;
        }

        /// <summary>
        /// Static Create function for a child wrapper, with a different allowRelateToLevel value.
        /// </summary>
        /// <param name="parentWrapper">The parent wrapper.</param>
        /// <param name="allowRelateToLevel">Whether or not handles are allowed to be related to levels.</param>
        /// <returns>A new ProductWrapper.</returns>
        public static ProductWrapper Create(ProductWrapper parentWrapper, bool allowRelateToLevel)
        {
            ProductWrapper productWrapper = new ProductWrapper();
            productWrapper.m_InternalWrapper = IFCProductWrapper.Create(parentWrapper.m_InternalWrapper, allowRelateToLevel);
            productWrapper.m_ParentWrapper = parentWrapper;
            return productWrapper;
        }

        /// <summary>
        /// Returns the internal IFCProductWrapper, for use as arguments to native functions.
        /// </summary>
        /// <returns>The internal IFCProductWrapper.</returns>
        public IFCProductWrapper ToNative()
        {
            return m_InternalWrapper;
        }

        /// <summary>
        /// Gets an arbitrary handle from the wrapper, if one exists.
        /// </summary>
        /// <returns>The handle, or null if no handle exists.</returns>
        /// <remarks>Generally intended for when there is only one handle in the wrapper.</remarks>
        public IFCAnyHandle GetAnElement()
        {
            if (m_createdHandles.Count > 0)
            {
                foreach (IFCAnyHandle firstHandle in m_createdHandles)
                    return firstHandle;
            }

            return m_InternalWrapper.GetAnElement();
        }

        /// <summary>
        /// Determines whether there are any handles associated with the wrapper.
        /// </summary>
        /// <returns>True if it is empty, false otherwise.</returns>
        public bool IsEmpty()
        {
            return ((m_createdHandles.Count == 0) && (m_InternalWrapper.Count == 0));
        }

        /// <summary>
        /// Gets the first handle of a particular type, or null if none exists.
        /// </summary>
        /// <param name="type">The entity type.</param>
        /// <returns>The handle, or null.</returns>
        public IFCAnyHandle GetElementOfType(IFCEntityType type)
        {
            foreach (IFCAnyHandle handle in m_createdHandles)
            {
                if (IFCAnyHandleUtil.IsSubTypeOf(handle, type))
                    return handle;
            }

            ICollection<IFCAnyHandle> internalObjects = m_InternalWrapper.GetAllObjects();
            foreach (IFCAnyHandle handle in internalObjects)
            {
                if (IFCAnyHandleUtil.IsSubTypeOf(handle, type))
                    return handle;
            }

            return null;
        }

        /// <summary>
        /// Get all handles in the wrapper.
        /// </summary>
        /// <returns>The collection of handles.</returns>
        public ICollection<IFCAnyHandle> GetAllObjects()
        {
            ICollection<IFCAnyHandle> internalObjects = m_InternalWrapper.GetAllObjects();
            if (m_createdHandles.Count == 0)
                return internalObjects;

            if (internalObjects.Count == 0)
                return m_createdHandles;

            HashSet<IFCAnyHandle> allObjects = new HashSet<IFCAnyHandle>();
            allObjects.UnionWith(internalObjects);
            allObjects.UnionWith(m_createdHandles);
            return allObjects;
        }

        /// <summary>
        /// Add a generic element to the wrapper.
        /// </summary>
        /// <param name="handle">The handle.</param>
        public void AddElement(IFCAnyHandle handle)
        {
            m_createdHandles.Add(handle);
        }

        /// <summary>
        /// Add a generic element to the wrapper, with associated setter and extrusion data information.
        /// </summary>
        /// <param name="handle">The element handle.</param>
        /// <param name="setter">The placement setter.</param>
        /// <param name="data">The extrusion creation data (can be null.)</param>
        /// <param name="relateToLevel">Relate to the level in the setter, or not.</param>
        public void AddElement(IFCAnyHandle handle, IFCPlacementSetter setter, IFCExtrusionCreationData data, bool relateToLevel)
        {
            m_InternalWrapper.AddElement(handle, setter, data, relateToLevel);
        }

        /// <summary>
        /// Add a generic element to the wrapper, with associated level and extrusion data information.
        /// </summary>
        /// <param name="handle">The element handle.</param>
        /// <param name="levelInfo">The level information.</param>
        /// <param name="data">The extrusion creation data (can be null.)</param>
        /// <param name="relateToLevel">Relate to the level in the setter, or not.</param>
        public void AddElement(IFCAnyHandle handle, IFCLevelInfo levelInfo, IFCExtrusionCreationData data, bool relateToLevel)
        {
            m_InternalWrapper.AddElement(handle, levelInfo, data, relateToLevel);
        }

        /// <summary>
        /// Add a space to the wrapper, with associated level and extrusion data information.
        /// </summary>
        /// <param name="handle">The element handle.</param>
        /// <param name="levelInfo">The level information.</param>
        /// <param name="data">The extrusion creation data (can be null.)</param>
        /// <param name="relateToLevel">Relate to the level in the setter, or not.</param>
        public void AddSpace(IFCAnyHandle handle, IFCLevelInfo levelInfo, IFCExtrusionCreationData data, bool relateToLevel)
        {
            m_InternalWrapper.AddSpace(handle, levelInfo, data, relateToLevel);
        }

        /// <summary>
        /// Adds an annotation handle to associate with the IfcProduct in this wrapper.
        /// </summary>
        /// <param name="handle">The annotation handle.</param>
        /// <param name="levelInfo">The level information, can be null if relateToLevel is false.</param>
        /// <param name="relateToLevel">Whether the annotation is contained in a level.</param>
        public void AddAnnotation(IFCAnyHandle handle, IFCLevelInfo levelInfo, bool relateToLevel)
        {
            m_InternalWrapper.AddAnnotation(handle, levelInfo, relateToLevel);
        }

        /// <summary>
        /// Adds a building handle to this wrapper.
        /// </summary>
        /// <param name="handle">The building handle.</param>
        public void AddBuilding(IFCAnyHandle handle)
        {
            m_InternalWrapper.AddBuilding(handle);
        }

        /// <summary>
        /// Adds a site (IfcObject) handle to associate with the IfcProduct in this wrapper.
        /// </summary>
        /// <param name="handle">The site handle.</param>
        public void AddSite(IFCAnyHandle handle)
        {
            m_InternalWrapper.AddSite(handle);
        }

        /// <summary>
        /// Adds a material handle to associate with the IfcProduct in this wrapper.
        /// </summary>
        /// <param name="materialHnd"></param>
        public void AddFinishMaterial(IFCAnyHandle materialHnd)
        {
            m_InternalWrapper.AddFinishMaterial(materialHnd);
        }

        /// <summary>
        /// Clear finish materials in this wrapper.
        /// </summary>
        public void ClearFinishMaterials()
        {
            m_InternalWrapper.ClearFinishMaterials();
        }

        /// <summary>
        /// Gets the extrusion creation data associated with a handle.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <returns>The extrusion creation data, or null.</returns>
        public IFCExtrusionCreationData FindExtrusionCreationParameters(IFCAnyHandle handle)
        {
            return m_InternalWrapper.FindExtrusionCreationParameters(handle);
        }

        /// <summary>
        /// The Dispose function, to do bookkeeping at end of "using" block.
        /// </summary>
        public void Dispose()
        {
            if (m_ParentWrapper != null)
                m_ParentWrapper.m_createdHandles.UnionWith(m_createdHandles);
            m_InternalWrapper.Dispose();
        }

        private ProductWrapper()
        {
        }
    }
}
