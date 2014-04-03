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
    /// Base level class for all objects created from an IFC entity.
    /// </summary>
    public abstract class IFCEntity
    {
        private int m_StepId;

        private IFCEntityType m_EntityType;

        /// <summary>
        /// The id of the entity, corresponding to the STEP id of the IFC entity.
        /// </summary>
        public int Id
        {
            get { return m_StepId; }
            protected set { m_StepId = value; }
        }

        /// <summary>
        /// The entity type of the corresponding IFC entity.
        /// </summary>
        public IFCEntityType EntityType
        {
            get { return m_EntityType; }
            protected set { m_EntityType = value; }
        }

        protected IFCEntity()
        {
        }

        virtual protected void Process(IFCAnyHandle item)
        {
            IFCImportFile.TheFile.EntityMap.Add(item.StepId, this);
            IFCImportFile.TheLog.AddProcessedEntity(item);
            Id = item.StepId;
            EntityType = IFCAnyHandleUtil.GetEntityType(item);
        }
    }
}
