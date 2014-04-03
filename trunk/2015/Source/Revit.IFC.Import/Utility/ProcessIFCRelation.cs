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
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Import.Data;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Import.Utility
{
    /// <summary>
    /// Processes IfcRelation entity and its sub-entities, to be stored in another class.
    /// </summary>
    class ProcessIFCRelation
    {
        static private void ValidateIFCRelAssigns(IFCAnyHandle ifcRelAssigns)
        {
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcRelAssigns))
                throw new ArgumentNullException("ifcRelAssigns");

            if (!IFCAnyHandleUtil.IsSubTypeOf(ifcRelAssigns, IFCEntityType.IfcRelAssigns))
                throw new ArgumentException("ifcRelAssigns");
        }

        static private void ValidateIFCRelAssignsOrAggregates(IFCAnyHandle ifcRelAssignsOrAggregates)
        {
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcRelAssignsOrAggregates))
                throw new ArgumentNullException("ifcRelAssignsOrAggregates");

            if (!IFCAnyHandleUtil.IsSubTypeOf(ifcRelAssignsOrAggregates, IFCEntityType.IfcRelAssigns) &&
                (!IFCAnyHandleUtil.IsSubTypeOf(ifcRelAssignsOrAggregates, IFCEntityType.IfcRelAggregates)))
                throw new ArgumentException("ifcRelAssignsOrAggregates");
        }

        /// <summary>
        /// Finds all related objects in IfcRelAssigns.
        /// </summary>
        /// <param name="ifcRelAssigns">The IfcRelAssigns handle.</param>
        /// <returns>The related objects, or null if not found.</returns>
        static public ICollection<IFCObjectDefinition> ProcessRelatedObjects(IFCAnyHandle ifcRelAssignsOrAggregates)
        {
            try
            {
                ValidateIFCRelAssignsOrAggregates(ifcRelAssignsOrAggregates);
            }
            catch
            {
                //LOG: ERROR: Couldn't find valid IfcRelAssignsToGroup for IfcGroup.
                return null;
            }

            HashSet<IFCAnyHandle> relatedObjects = IFCAnyHandleUtil.GetAggregateInstanceAttribute
                <HashSet<IFCAnyHandle>>(ifcRelAssignsOrAggregates, "RelatedObjects");

            // Receiving apps need to decide whether to post an error or not.
            if (relatedObjects == null)
                return null;

            ICollection<IFCObjectDefinition> relatedObjectSet = new HashSet<IFCObjectDefinition>();

            foreach (IFCAnyHandle relatedObject in relatedObjects)
            {
                IFCObjectDefinition objectDefinition = IFCObjectDefinition.ProcessIFCObjectDefinition(relatedObject);
                if (objectDefinition != null)
                    relatedObjectSet.Add(objectDefinition);
            }

            return relatedObjectSet;
        }

        /// <summary>
        /// Finds the relating group in IfcRelAssignsToGroup.
        /// </summary>
        /// <param name="ifcRelAssignsToGroup">The IfcRelAssignsToGroup handle.</param>
        /// <returns>The related group, or null if not found.</returns>
        static public IFCGroup ProcessRelatingGroup(IFCAnyHandle ifcRelAssignsToGroup)
        {
            if (!IFCAnyHandleUtil.IsSubTypeOf(ifcRelAssignsToGroup, IFCEntityType.IfcRelAssignsToGroup))
            {
                //LOG: ERROR: Couldn't find valid IfcRelAssignsToGroup.
                return null;
            }

            IFCAnyHandle relatingGroup = IFCAnyHandleUtil.GetInstanceAttribute(ifcRelAssignsToGroup, "RelatingGroup");

            // Receiving apps need to decide whether to post an error or not.
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(relatingGroup))
                return null;

            IFCGroup group = IFCGroup.ProcessIFCGroup(relatingGroup);
            return group;
        }

        /// <summary>
        /// Gets the related object type in IfcRelAssigns.
        /// </summary>
        /// <param name="ifcRelAssigns">The IfcRelAssigns handle.</param>
        /// <returns>The related object type, or null if not defined.</returns>
        static public string ProcessRelatedObjectType(IFCAnyHandle ifcRelAssigns)
        {
            try
            {
                ValidateIFCRelAssigns(ifcRelAssigns);
            }
            catch
            {
                //LOG: ERROR: Couldn't find valid IfcRelAssignsToGroup for IfcGroup.
                return null;
            }

            return IFCAnyHandleUtil.GetStringAttribute(ifcRelAssigns, "RelatedObjectsType");
        }
    }
}
