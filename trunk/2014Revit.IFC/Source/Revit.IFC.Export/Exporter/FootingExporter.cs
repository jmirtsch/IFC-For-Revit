﻿//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
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
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Exporter
{
    /// <summary>
    /// Provides methods to export footing elements.
    /// </summary>
    class FootingExporter
    {
        /// <summary>
        /// Exports a footing to IFC footing.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="footing">
        /// The footing element.
        /// </param>
        /// <param name="geometryElement">
        /// The geometry element.
        /// </param>
        /// <param name="productWrapper">
        /// The ProductWrapper.
        /// </param>
        public static void ExportFootingElement(ExporterIFC exporterIFC,
           ContFooting footing, GeometryElement geometryElement, ProductWrapper productWrapper)
        {
            String ifcEnumType = "STRIP_FOOTING";
            ExportFooting(exporterIFC, footing, geometryElement, ifcEnumType, productWrapper);
        }

        /// <summary>
        /// Exports an element to IFC footing.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="element">
        /// The element.
        /// </param>
        /// <param name="geometryElement">
        /// The geometry element.
        /// </param>
        /// <param name="ifcEnumType">
        /// The string value represents the IFC type.
        /// </param>
        /// <param name="productWrapper">
        /// The ProductWrapper.
        /// </param>
        public static void ExportFooting(ExporterIFC exporterIFC, Element element, GeometryElement geometryElement,
           string ifcEnumType, ProductWrapper productWrapper)
        {
            // export parts or not
            bool exportParts = PartExporter.CanExportParts(element);
            if (exportParts && !PartExporter.CanExportElementInPartExport(element, element.LevelId, false))
                return;

            IFCFile file = exporterIFC.GetFile();

            using (IFCTransaction tr = new IFCTransaction(file))
            {
                using (PlacementSetter setter = PlacementSetter.Create(exporterIFC, element))
                {
                    using (IFCExtrusionCreationData ecData = new IFCExtrusionCreationData())
                    {                      
                        ecData.SetLocalPlacement(setter.LocalPlacement);
                      
                        IFCAnyHandle prodRep = null;
                        ElementId matId = ElementId.InvalidElementId;
                        if (!exportParts)
                        {
                            ElementId catId = CategoryUtil.GetSafeCategoryId(element);


                            matId = BodyExporter.GetBestMaterialIdFromGeometryOrParameter(geometryElement, exporterIFC, element);
                            BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true);
                            prodRep = RepresentationUtil.CreateAppropriateProductDefinitionShape(exporterIFC,
                               element, catId, geometryElement, bodyExporterOptions, null, ecData, true);
                            if (IFCAnyHandleUtil.IsNullOrHasNoValue(prodRep))
                            {
                                ecData.ClearOpenings();
                                return;
                            }
                        }

                        string instanceGUID = GUIDUtil.CreateGUID(element);
                        string instanceName = NamingUtil.GetNameOverride(element, NamingUtil.GetIFCName(element));
                        string instanceDescription = NamingUtil.GetDescriptionOverride(element, null);
                        string instanceObjectType = NamingUtil.GetObjectTypeOverride(element, exporterIFC.GetFamilyName());
                        string instanceTag = NamingUtil.GetTagOverride(element, NamingUtil.CreateIFCElementId(element));
                        string footingType = GetIFCFootingType(ifcEnumType);    // need to keep it for legacy support when original data follows slightly diff naming
                        footingType = IFCValidateEntry.GetValidIFCType(element, footingType);

                        IFCAnyHandle footing = IFCInstanceExporter.CreateFooting(file, instanceGUID, exporterIFC.GetOwnerHistoryHandle(),
                            instanceName, instanceDescription, instanceObjectType, ecData.GetLocalPlacement(), prodRep, instanceTag, footingType);

                        if (exportParts)
                        {
                            PartExporter.ExportHostPart(exporterIFC, element, footing, productWrapper, setter, setter.LocalPlacement, null);
                        }
                        else
                        {
                            if (matId != ElementId.InvalidElementId)
                            {
                                CategoryUtil.CreateMaterialAssociation(element.Document, exporterIFC, footing, matId);
                            }
                        }

                        productWrapper.AddElement(element, footing, setter, ecData, true);

                        OpeningUtil.CreateOpeningsIfNecessary(footing, element, ecData, null, 
                            exporterIFC, ecData.GetLocalPlacement(), setter, productWrapper);
                    }
                }

                tr.Commit();
            }
        }

        /// <summary>
        /// Gets IFC footing type from a string.
        /// </summary>
        /// <param name="value">The type name.</param>
        /// <returns>The IFCFootingType.</returns>
        public static string GetIFCFootingType(string value)
        {
            if (String.IsNullOrEmpty(value))
                return "NOTDEFINED";

            string newValue = NamingUtil.RemoveSpacesAndUnderscores(value);

            if (String.Compare(newValue, "USERDEFINED", true) == 0)
                return "USERDEFINED";
            if (String.Compare(newValue, "FOOTINGBEAM", true) == 0)
                return "FOOTING_BEAM";
            if (String.Compare(newValue, "PADFOOTING", true) == 0)
                return "PAD_FOOTING";
            if (String.Compare(newValue, "PILECAP", true) == 0)
                return "PILE_CAP";
            if (String.Compare(newValue, "STRIPFOOTING", true) == 0)
                return "STRIP_FOOTING";

            return newValue;
        }

    }
}
