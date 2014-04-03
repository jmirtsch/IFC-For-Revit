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
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
    /// <summary>
    /// Represents an IfcRoot object.
    /// </summary>
    public abstract class IFCRoot : IFCEntity
    {
        private string m_GlobalId = null;
        private string m_Name = null;
        private string m_Description = null;
        private IFCOwnerHistory m_OwnerHistory = null;
        
        /// <summary>
        /// Default constructor.
        /// </summary>
        protected IFCRoot()
        {
            IFCImportFile.TheLog.CurrentlyProcessedEntity = this;
        }

        /// <summary>
        /// Processes IfcRoot attributes.
        /// </summary>
        /// <param name="ifcRoot">The IfcRoot handle.</param>
        protected override void Process(IFCAnyHandle ifcRoot)
        {
            base.Process(ifcRoot);

            m_GlobalId = IFCImportHandleUtil.GetRequiredStringAttribute(ifcRoot, "GlobalId", false);
            if (Importer.TheCache.CreatedGUIDs.Contains(m_GlobalId))
                IFCImportFile.TheLog.LogWarning(Id, "Duplicate GUID: " + m_GlobalId, false);
            else
                Importer.TheCache.CreatedGUIDs.Add(m_GlobalId);

            m_Name = IFCAnyHandleUtil.GetStringAttribute(ifcRoot, "Name");
            m_Description = IFCAnyHandleUtil.GetStringAttribute(ifcRoot, "Description");
            
            IFCAnyHandle ownerHistoryHandle = IFCAnyHandleUtil.GetInstanceAttribute(ifcRoot, "OwnerHistory");
            m_OwnerHistory = IFCOwnerHistory.ProcessIFCOwnerHistory(ownerHistoryHandle);
        }

        /// <summary>
        /// Creates or populates Revit elements based on the information contained in this class.
        /// </summary>
        protected virtual void Create(Document doc)
        {
        }

        /// <summary>
        /// Cleans out the IFCEntity to save memory.
        /// </summary>
        public virtual void CleanEntity()
        {
            m_GlobalId = null;
            m_Name = null;
            m_Description = null;
            m_OwnerHistory = null;
        }
        
        /// <summary>
        /// The global id.
        /// </summary>
        public string GlobalId
        {
            get { return m_GlobalId; }
        }

        /// <summary>
        /// The name.
        /// </summary>
        public string Name
        {
            get { return m_Name; }
        }

        /// <summary>
        /// The description.
        /// </summary>
        public string Description
        {
            get { return m_Description; }
        }

        /// <summary>
        /// The owner history.
        /// </summary>
        public IFCOwnerHistory OwnerHistory
        {
            get { return m_OwnerHistory; }
        }

        /// <summary>
        /// True if two IFCRoots have the same id, or are both null.
        /// </summary>
        /// <param name="first">The first IFCRoot.</param>
        /// <param name="second">The second IFCRoot.</param>
        /// <returns>True if two IFCRoots have the same id, or are both null, or false otherwise.</returns>
        public static bool Equals(IFCRoot first, IFCRoot second)
        {
            if (first == null)
            {
                if (second != null) return false;
            }
            else if (second == null)
            {
                return false;   // first != null, otherwise we are in first case above.
            }
            else
            {
                if (first.Id != second.Id) return false;
            }

            return true;
        }
    }
}
