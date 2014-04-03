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
    public abstract class IFCSolidModel : IFCRepresentationItem, IIFCBooleanOperand
    {
        protected IFCSolidModel()
        {
        }

        override protected void Process(IFCAnyHandle ifcSolidModel)
        {
            base.Process(ifcSolidModel);
        }

        protected abstract GeometryObject CreateGeometryInternal(
           IFCImportShapeEditScope shapeEditScope, Transform lcs, Transform scaledLcs, bool forceSolid, string guid);

        /// <summary>
        /// Return geometry for a particular representation item.
        /// </summary>
        /// <param name="shapeEditScope">The shape edit scope.</param>
        /// <param name="lcs">Local coordinate system for the geometry, without scale.</param>
        /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
        /// <param name="forceSolid">True if we require a Solid.</param>
        /// <param name="guid">The guid of an element for which represntation is being created.</param>
        /// <returns>The created geometry.</returns>
        public GeometryObject CreateGeometry(IFCImportShapeEditScope shapeEditScope, Transform lcs, Transform scaledLcs, bool forceSolid, string guid)
        {
            if (StyledByItem != null)
                StyledByItem.Create(shapeEditScope);
            return CreateGeometryInternal(shapeEditScope, lcs, scaledLcs, forceSolid, guid);
        }

        protected IFCSolidModel(IFCAnyHandle item)
        {
            Process(item);
        }

        /// <summary>
        /// Create an IFCSolidModel object from a handle of type IfcSolidModel.
        /// </summary>
        /// <param name="ifcSolidModel">The IFC handle.</param>
        /// <returns>The IFCSolidModel object.</returns>
        public static IFCSolidModel ProcessIFCSolidModel(IFCAnyHandle ifcSolidModel)
        {
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcSolidModel))
            {
                IFCImportFile.TheLog.LogNullError(IFCEntityType.IfcSolidModel);
                return null;
            }

            if (IFCAnyHandleUtil.IsSubTypeOf(ifcSolidModel, IFCEntityType.IfcCsgSolid))
                return IFCCSGSolid.ProcessIFCCSGSolid(ifcSolidModel);
            if (IFCAnyHandleUtil.IsSubTypeOf(ifcSolidModel, IFCEntityType.IfcManifoldSolidBrep))
                return IFCManifoldSolidBrep.ProcessIFCManifoldSolidBrep(ifcSolidModel);
            if (IFCAnyHandleUtil.IsSubTypeOf(ifcSolidModel, IFCEntityType.IfcSweptAreaSolid))
                return IFCSweptAreaSolid.ProcessIFCSweptAreaSolid(ifcSolidModel);
            if (IFCAnyHandleUtil.IsSubTypeOf(ifcSolidModel, IFCEntityType.IfcSweptDiskSolid))
                return IFCSweptDiskSolid.ProcessIFCSweptDiskSolid(ifcSolidModel); 
            
            IFCImportFile.TheLog.LogUnhandledSubTypeError(ifcSolidModel, IFCEntityType.IfcSolidModel, true);
            return null;
        }
    }
}
