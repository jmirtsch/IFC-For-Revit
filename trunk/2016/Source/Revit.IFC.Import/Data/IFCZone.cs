﻿//
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
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
    /// <summary>
    /// Represents an IfcZone.
    /// </summary>
    public class IFCZone : IFCGroup
    {
        /// <summary>
        /// Processes IfcZone attributes.
        /// </summary>
        /// <param name="ifcZone">The IfcZone handle.</param>
        protected override void Process(IFCAnyHandle ifcZone)
        {
            base.Process(ifcZone);
        }

        protected IFCZone()
        {
        }

        protected IFCZone(IFCAnyHandle zone)
        {
            Process(zone);
        }

        /// <summary>
        /// Processes IfcZone handle.
        /// </summary>
        /// <param name="ifcZone">The IfcZone handle.</param>
        /// <returns>The IFCZone object.</returns>
        public static IFCZone ProcessIFCZone(IFCAnyHandle ifcZone)
        {
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcZone))
            {
                IFCImportFile.TheLog.LogNullError(IFCEntityType.IfcZone);
                return null;
            }

            IFCEntity cachedIFCZone;
            IFCImportFile.TheFile.EntityMap.TryGetValue(ifcZone.StepId, out cachedIFCZone);
            if (cachedIFCZone != null)
                return cachedIFCZone as IFCZone;

            return new IFCZone(ifcZone);
        }

        /// <summary>
        /// Creates or populates Revit elements based on the information contained in this class.
        /// </summary>
        /// <param name="doc">The document.</param>
        protected override void Create(Document doc)
        {
            // If we created an element above, then we will set the shape of it to be the same of the shapes of the contained spaces.
            IList<GeometryObject> geomObjs = new List<GeometryObject>();
            foreach (IFCObjectDefinition objDef in RelatedObjects)
            {
                if (!(objDef is IFCSpace))
                    continue;

                // This lets us create a copy of the space geometry with the Zone graphics style.
                IFCSpace spaceClone = IFCSpace.CreateSpaceClone(objDef as IFCSpace, this);
                CreateElement(doc, spaceClone);
                foreach (IFCSolidInfo solidGeom in spaceClone.Solids)
                    geomObjs.Add(solidGeom.GeometryObject);
            }

            DirectShape zoneElement = IFCElementUtil.CreateElement(doc, CategoryId, GlobalId, geomObjs);
            if (zoneElement != null)
            {
                CreatedElementId = zoneElement.Id;
                CreatedGeometry = geomObjs;
            }

            base.Create(doc);
        }
    }
}
