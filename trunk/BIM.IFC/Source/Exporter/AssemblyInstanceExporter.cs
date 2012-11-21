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
using BIM.IFC.Utility;
using BIM.IFC.Toolkit;
using BIM.IFC.Exporter.PropertySet;

namespace BIM.IFC.Exporter
{
    /// <summary>
    /// Provides methods to export a Revit element as IfcElementAssembly.
    /// </summary>
    class AssemblyInstanceExporter
    {
        private static IFCElementAssemblyType GetPredefinedTypeFromObjectType(string objectType)
        {
            if (String.IsNullOrEmpty(objectType))
                return IFCElementAssemblyType.NotDefined;

            foreach (IFCElementAssemblyType val in Enum.GetValues(typeof(IFCElementAssemblyType)))
            {
                if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(objectType, val.ToString()))
                    return val;
            }

            return IFCElementAssemblyType.UserDefined;
        }

        /// <summary>
        /// Exports an element as an IFC assembly.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="element">The element.</param>
        /// <param name="geometryElement">The geometry element.</param>
        /// <param name="productWrapper">The ProductWrapper.</param>
        /// <returns>True if exported successfully, false otherwise.</returns>
        public static bool ExportAssemblyInstanceElement(ExporterIFC exporterIFC, AssemblyInstance element,
            ProductWrapper productWrapper)
        {
            if (element == null)
                return false;

            IFCFile file = exporterIFC.GetFile();

            using (IFCTransaction tr = new IFCTransaction(file))
            {
                using (IFCPlacementSetter placementSetter = IFCPlacementSetter.Create(exporterIFC, element))
                {
                    string ifcEnumType;
                    IFCExportType exportAs = ExporterUtil.GetExportType(exporterIFC, element, out ifcEnumType);
                    IFCAnyHandle assemblyInstanceHnd = null;
        
                    string guid = GUIDUtil.CreateGUID(element);
                    IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();
                    string name = NamingUtil.GetIFCName(element);
                    string objectType = exporterIFC.GetFamilyName();
                    IFCAnyHandle localPlacement = placementSetter.GetPlacement();
                    IFCAnyHandle representation = null;
                    string elementTag = NamingUtil.CreateIFCElementId(element);

                    // We have limited support for exporting assemblies as other container types.
                    switch (exportAs)
                    {
                        case IFCExportType.ExportRamp:
                            IFCRampType rampPredefinedType = RampExporter.GetIFCRampType(ifcEnumType);
                            assemblyInstanceHnd = IFCInstanceExporter.CreateRamp(file, guid,
                                ownerHistory, name, null, objectType, localPlacement, representation, elementTag,
                                rampPredefinedType);
                            break;
                        case IFCExportType.ExportRoof:
                            IFCRoofType roofPredefinedType = RoofExporter.GetIFCRoofType(ifcEnumType);
                            assemblyInstanceHnd = IFCInstanceExporter.CreateRoof(file, guid,
                                ownerHistory, name, null, objectType, localPlacement, representation, elementTag,
                                roofPredefinedType);
                            break;
                        case IFCExportType.ExportStair:
                            IFCStairType stairPredefinedType = StairsExporter.GetIFCStairType(ifcEnumType);
                            assemblyInstanceHnd = IFCInstanceExporter.CreateStair(file, guid,
                                ownerHistory, name, null, objectType, localPlacement, representation, elementTag,
                                stairPredefinedType);
                            break;
                        case IFCExportType.ExportWall:
                            assemblyInstanceHnd = IFCInstanceExporter.CreateWall(file, guid,
                                ownerHistory, name, null, objectType, localPlacement, representation, elementTag);
                            break;
                        default:
                            IFCElementAssemblyType assemblyPredefinedType = GetPredefinedTypeFromObjectType(objectType);
                            assemblyInstanceHnd = IFCInstanceExporter.CreateElementAssembly(file, guid,
                                ownerHistory, name, null, objectType, localPlacement, representation, elementTag,
                                IFCAssemblyPlace.NotDefined, assemblyPredefinedType);
                            break;
                    }

                    if (assemblyInstanceHnd == null)
                        return false;

                    productWrapper.AddElement(assemblyInstanceHnd, placementSetter.GetLevelInfo(), null, true);

                    PropertyUtil.CreateInternalRevitPropertySets(exporterIFC, element, productWrapper);

                    ExporterCacheManager.AssemblyInstanceCache.RegisterAssemblyInstance(element.Id, assemblyInstanceHnd);
                }
                tr.Commit();
                return true;
            }
        }
    }
}
