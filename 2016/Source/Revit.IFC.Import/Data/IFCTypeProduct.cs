//
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
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Import.Data
{
    /// <summary>
    /// Represents an IfcTypeProduct.
    /// </summary>
    public class IFCTypeProduct : IFCTypeObject
    {
        // TODO: representation maps.
        private string m_Tag;

        /// <summary>
        /// The tag.
        /// </summary>
        public string Tag
        {
            get { return m_Tag; }
            protected set { m_Tag = value; }
        }

        protected IFCTypeProduct()
        {
        }

        /// <summary>
        /// Constructs an IFCTypeProduct from the IfcTypeProduct handle.
        /// </summary>
        /// <param name="ifcTypeProduct">The IfcTypeProduct handle.</param>
        protected IFCTypeProduct(IFCAnyHandle ifcTypeProduct)
        {
            Process(ifcTypeProduct);
        }

        /// <summary>
        /// Processes IfcTypeObject attributes.
        /// </summary>
        /// <param name="ifcTypeProduct">The IfcTypeProduct handle.</param>
        protected override void Process(IFCAnyHandle ifcTypeProduct)
        {
            base.Process(ifcTypeProduct);

            Tag = IFCAnyHandleUtil.GetStringAttribute(ifcTypeProduct, "Tag");
        }

        /// <summary>
        /// Processes an IfcTypeProduct.
        /// </summary>
        /// <param name="ifcTypeProduct">The IfcTypeProduct handle.</param>
        /// <returns>The IFCTypeProduct object.</returns>
        public static IFCTypeProduct ProcessIFCTypeProduct(IFCAnyHandle ifcTypeProduct)
        {
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcTypeProduct))
            {
                Importer.TheLog.LogNullError(IFCEntityType.IfcTypeProduct);
                return null;
            }

            IFCEntity typeProduct;
            if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcTypeProduct.StepId, out typeProduct))
                return (typeProduct as IFCTypeProduct);

            if (IFCAnyHandleUtil.IsSubTypeOf(ifcTypeProduct, IFCEntityType.IfcDoorStyle))
                return IFCDoorStyle.ProcessIFCDoorStyle(ifcTypeProduct);

            if (IFCAnyHandleUtil.IsSubTypeOf(ifcTypeProduct, IFCEntityType.IfcElementType))
                return IFCElementType.ProcessIFCElementType(ifcTypeProduct);

            return new IFCTypeProduct(ifcTypeProduct);
        }
    }
}
