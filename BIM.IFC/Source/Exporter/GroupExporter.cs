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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB.Mechanical;
using BIM.IFC.Utility;
using BIM.IFC.Toolkit;
using BIM.IFC.Exporter.PropertySet;

namespace BIM.IFC.Exporter
{
    /// <summary>
    /// Provides methods to export a Group element as IfcGroup.
    /// </summary>
    class GroupExporter
    {
        /// <summary>
        /// Exports a Group as an IfcGroup.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="element">The element.</param>
        /// <param name="productWrapper">The ProductWrapper.</param>
        /// <returns>True if exported successfully, false otherwise.</returns>
        public static bool ExportGroupElement(ExporterIFC exporterIFC, Group element,
            ProductWrapper productWrapper)
        {
            if (element == null)
                return false;

            IFCFile file = exporterIFC.GetFile();

            using (IFCTransaction tr = new IFCTransaction(file))
            {
                IFCAnyHandle groupHnd = null;

                string guid = GUIDUtil.CreateGUID(element);
                IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();
                string name = NamingUtil.GetNameOverride(element, NamingUtil.GetIFCName(element));
                string description = NamingUtil.GetDescriptionOverride(element, null);
                string objectType = NamingUtil.GetObjectTypeOverride(element, exporterIFC.GetFamilyName());

                string ifcEnumType;
                IFCExportType exportAs = ExporterUtil.GetExportType(exporterIFC, element, out ifcEnumType);
                if (exportAs == IFCExportType.ExportGroup)
                {
                    groupHnd = IFCInstanceExporter.CreateGroup(file, guid, ownerHistory, name, description, objectType);
                }

                if (groupHnd == null)
                    return false;

                productWrapper.AddElement(groupHnd);

                PropertyUtil.CreateInternalRevitPropertySets(exporterIFC, element, productWrapper);

                ExporterCacheManager.GroupCache.RegisterGroup(element.Id, groupHnd);

                tr.Commit();
                return true;
            }
        }
    }
}
