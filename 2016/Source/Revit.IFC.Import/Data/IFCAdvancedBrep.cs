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
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
    /// <summary>
    /// Class that represents IFCAdvancedBrep entity
    /// </summary>
    public class IFCAdvancedBrep : IFCManifoldSolidBrep
    {
        protected IFCAdvancedBrep()
        {
        }

        protected IFCAdvancedBrep(IFCAnyHandle item)
        {
            Process(item);
        }

        override protected void Process(IFCAnyHandle ifcAdvancedBrep)
        {
            base.Process(ifcAdvancedBrep);
        }

        /// <summary>
        /// Create geometry for a particular representation item.
        /// </summary>
        /// <param name="shapeEditScope">The geometry creation scope.</param>
        /// <param name="lcs">Local coordinate system for the geometry, without scale.</param>
        /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
        /// <param name="guid">The guid of an element for which represntation is being created.</param>
        protected override void CreateShapeInternal(IFCImportShapeEditScope shapeEditScope, Transform lcs, Transform scaledLcs, string guid)
        {
            base.CreateShapeInternal(shapeEditScope, scaledLcs, lcs, guid);
        }

        /// <summary>
        /// Return geometry for a particular representation item.
        /// </summary>
        /// <param name="shapeEditScope">The shape edit scope.</param>
        /// <param name="lcs">Local coordinate system for the geometry, without scale.</param>
        /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
        /// <param name="guid">The guid of an element for which represntation is being created.</param>
        /// <returns>The created geometry.</returns>
        protected override IList<GeometryObject> CreateGeometryInternal(
           IFCImportShapeEditScope shapeEditScope, Transform lcs, Transform scaledLcs, string guid)
        {
           throw new InvalidOperationException("Unhandled code.");
        }


        /// <summary>
        /// Create an IFCAdvancedBrep object from a handle of type IfcAdvancedBrep.
        /// </summary>
        /// <param name="ifcAdvancedBrep">The IFC handle.</param>
        /// <returns>The IFCAdvancedBrep object.</returns>
        public static IFCAdvancedBrep ProcessIFCAdvancedBrep(IFCAnyHandle ifcAdvancedBrep)
        {
           throw new InvalidOperationException("Unhandled code.");
        }
    }
}
