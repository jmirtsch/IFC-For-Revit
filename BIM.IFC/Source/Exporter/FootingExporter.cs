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
            if (exportParts && !PartExporter.CanExportElementInPartExport(element, element.Level.Id, false))
                return;

            IFCFile file = exporterIFC.GetFile();

            using (IFCTransaction tr = new IFCTransaction(file))
            {
                using (IFCPlacementSetter setter = IFCPlacementSetter.Create(exporterIFC, element))
                {
                    using (IFCExtrusionCreationData ecData = new IFCExtrusionCreationData())
                    {                      
                        ecData.SetLocalPlacement(setter.GetPlacement());
                      
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
                        Toolkit.IFCFootingType footingType = GetIFCFootingType(element, ifcEnumType);

                        IFCAnyHandle footing = IFCInstanceExporter.CreateFooting(file, instanceGUID, exporterIFC.GetOwnerHistoryHandle(),
                            instanceName, instanceDescription, instanceObjectType, ecData.GetLocalPlacement(), prodRep, instanceTag, footingType);

                        if (exportParts)
                        {
                            PartExporter.ExportHostPart(exporterIFC, element, footing, productWrapper, setter, setter.GetPlacement(), null);
                        }
                        else
                        {
                            if (matId != ElementId.InvalidElementId)
                            {
                                CategoryUtil.CreateMaterialAssociation(element.Document, exporterIFC, footing, matId);
                            }
                        }

                        productWrapper.AddElement(footing, setter, ecData, true);

                        OpeningUtil.CreateOpeningsIfNecessary(footing, element, ecData, null, 
                            exporterIFC, ecData.GetLocalPlacement(), setter, productWrapper);

                        PropertyUtil.CreateInternalRevitPropertySets(exporterIFC, element, productWrapper);
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
        public static Toolkit.IFCFootingType GetIFCFootingType(string value)
        {
            if (String.IsNullOrEmpty(value))
                return Toolkit.IFCFootingType.NotDefined;

            string newValue = NamingUtil.RemoveSpacesAndUnderscores(value);

            if (String.Compare(newValue, "USERDEFINED", true) == 0)
                return Toolkit.IFCFootingType.UserDefined;
            if (String.Compare(newValue, "FOOTINGBEAM", true) == 0)
                return Toolkit.IFCFootingType.Footing_Beam;
            if (String.Compare(newValue, "PADFOOTING", true) == 0)
                return Toolkit.IFCFootingType.Pad_Footing;
            if (String.Compare(newValue, "PILECAP", true) == 0)
                return Toolkit.IFCFootingType.Pile_Cap;
            if (String.Compare(newValue, "STRIPFOOTING", true) == 0)
                return Toolkit.IFCFootingType.Strip_Footing;

            return Toolkit.IFCFootingType.UserDefined;
        }

        /// <summary>
        /// Gets IFC footing type for an element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="typeName">The type name.</param>
        /// <returns>The IFCFootingType.</returns>
        public static Toolkit.IFCFootingType GetIFCFootingType(Element element, string typeName)
        {
            string value = null;
            if (!ParameterUtil.GetStringValueFromElementOrSymbol(element, "IfcType", out value))
            {
                value = typeName;
            }

            return GetIFCFootingType(value);
        }
    }
}
