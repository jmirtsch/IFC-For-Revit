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
using System.Text;
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using BIM.IFC.Toolkit;
using BIM.IFC.Utility;

namespace BIM.IFC.Utility
{
    /// <summary>
    /// Provides static methods for GUID related manipulations.
    /// </summary>
    class GUIDUtil
    {
        /// <summary>
        /// Checks if a GUID string is properly formatted as an IFC GUID.
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        static public bool IsValidIFCGUID(string guid)
        {
            if (guid == null)
                return false;

            if (guid.Length != 22)
                return false;

            foreach (char guidChar in guid)
            {
                if ((guidChar >= '0' && guidChar <= '9') ||
                    (guidChar >= 'A' && guidChar <= 'Z') ||
                    (guidChar >= 'a' && guidChar <= 'z') ||
                    (guidChar == '_' || guidChar == '$'))
                    continue;

                return false;
            }

            return true;
        }

        /// <summary>
        /// Creates a Project, Site, or Building GUID.  If a shared parameter is set with a valid IFC GUID value,
        /// that value will override the default one.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="guidType">The GUID being created.</param>
        /// <returns>The IFC GUID value.</returns>
        /// <remarks>For Sites, the user should only use this routine if there is no Site element in the file.  Otherwise, they
        /// should use CreateSiteGUID below, which takes an Element pointer.</remarks>
        static public string CreateProjectLevelGUID(Document document, IFCProjectLevelGUIDType guidType)
        {
            string parameterName = "Ifc" + guidType.ToString() + " GUID";
            ProjectInfo projectInfo = document.ProjectInformation;

            if (projectInfo != null)
            {
                string paramValue = null;
                ParameterUtil.GetStringValueFromElement(projectInfo, parameterName, out paramValue);
                if ((paramValue != null) && (IsValidIFCGUID(paramValue)))
                    return paramValue;
            }

            return ExporterIFCUtils.CreateProjectLevelGUID(document, guidType);
        }

        /// <summary>
        /// Creates a Site GUID for a Site element.  If "IfcSite GUID" is set to a valid IFC GUID in Project Information, that value will
        /// override the default GUID generation for the Site element.
        /// </summary>
        /// <param name="document">The document pointer.</param>
        /// <param name="element">The Site element.</param>
        /// <returns></returns>
        static public string CreateSiteGUID(Document document, Element element)
        {
            ProjectInfo projectInfo = document.ProjectInformation;

            if (projectInfo != null)
            {
                string paramValue = null;
                ParameterUtil.GetStringValueFromElement(projectInfo, "IfcSiteGUID", out paramValue);
                if ((paramValue != null) && (IsValidIFCGUID(paramValue)))
                    return paramValue;
            }

            return ExporterIFCUtils.CreateGUID(element);
        }
    }
}