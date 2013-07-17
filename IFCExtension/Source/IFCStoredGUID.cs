//
// BIM IFC export alternate UI library: this library works with Autodesk(R) Revit(R) to provide an alternate user interface for the export of IFC files from Revit.
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB.ExtensibleStorage;


namespace Revit.IFC.Common.Extensions
{
    public class IFCStoredGUID
    {
        private static Dictionary<ElementId, string> m_ElementIdToGUID;

        private static Dictionary<BuiltInParameter, string> m_ProjectInfoParameterGUID;

        public static IDictionary<ElementId, string> ElementIdToGUID
        {
            get 
            {
                if (m_ElementIdToGUID == null)
                    m_ElementIdToGUID = new Dictionary<ElementId, string>();
                return m_ElementIdToGUID; 
            }
        }

        public static IDictionary<BuiltInParameter, string> ProjectInfoParameterGUID
        {
            get
            {
                if (m_ProjectInfoParameterGUID == null)
                    m_ProjectInfoParameterGUID = new Dictionary<BuiltInParameter, string>();
                return m_ProjectInfoParameterGUID;
            }
        }
        
        public static void Clear()
        {
            m_ElementIdToGUID = null;
            m_ProjectInfoParameterGUID = null;
        }
    }
}
