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
    public class IFCSurface : IFCRepresentationItem
    {
        protected IFCSurface()
        {
        }
        
        override protected void Process(IFCAnyHandle ifcCurve)
        {
            base.Process(ifcCurve);
        }

        /// <summary>
        /// Get the local surface transform at a given point on the surface.
        /// </summary>
        /// <param name="pointOnSurface">The point.</param>
        /// <returns>The transform.</returns>
        public virtual Transform GetTransformAtPoint(XYZ pointOnSurface)
        {
            return null;
        }

        /// <summary>
        /// Create an IFCSurface object from a handle of type IfcSurface.
        /// </summary>
        /// <param name="ifcSurface">The IFC handle.</param>
        /// <returns>The IFCSurface object.</returns>
        public static IFCSurface ProcessIFCSurface(IFCAnyHandle ifcSurface)
        {
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcSurface))
            {
                IFCImportFile.TheLog.LogNullError(IFCEntityType.IfcSurface);
                return null;
            }

            IFCEntity surface;
            if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcSurface.StepId, out surface))
                return (surface as IFCSurface);

            if (IFCAnyHandleUtil.IsSubTypeOf(ifcSurface, IFCEntityType.IfcElementarySurface))
                return IFCElementarySurface.ProcessIFCElementarySurface(ifcSurface);
            else if (IFCAnyHandleUtil.IsSubTypeOf(ifcSurface, IFCEntityType.IfcSweptSurface))
                return IFCSweptSurface.ProcessIFCSweptSurface(ifcSurface);

            IFCImportFile.TheLog.LogUnhandledSubTypeError(ifcSurface, IFCEntityType.IfcSurface, true);
            return null;
        }
    }
}
