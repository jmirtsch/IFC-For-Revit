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
    /// Provides methods to export Pile elements.
    /// </summary>
    class PileExporter
    {
        /// <summary>
        /// Exports an element to IfcPile.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="element">The element.</param>
        /// <param name="geometryElement">The geometry element.</param>
        /// <param name="ifcEnumType">The string value represents the IFC type.</param>
        /// <param name="productWrapper">The ProductWrapper.</param>
        public static void ExportPile(ExporterIFC exporterIFC, Element element, GeometryElement geometryElement,
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
                            prodRep = RepresentationUtil.CreateBRepProductDefinitionShape(element.Document.Application, exporterIFC,
                               element, catId, geometryElement, bodyExporterOptions, null, ecData);
                            if (IFCAnyHandleUtil.IsNullOrHasNoValue(prodRep))
                            {
                                ecData.ClearOpenings();
                                return;
                            }
                        }

                        string instanceGUID = GUIDUtil.CreateGUID(element);
                        string instanceName = NamingUtil.GetIFCName(element);
                        string instanceDescription = NamingUtil.GetDescriptionOverride(element, null);
                        string instanceObjectType = NamingUtil.GetObjectTypeOverride(element, exporterIFC.GetFamilyName());
                        string instanceElemId = NamingUtil.CreateIFCElementId(element);
                        Toolkit.IFCPileType pileType = GetPileType(element, ifcEnumType);

                        IFCAnyHandle pile = IFCInstanceExporter.CreatePile(file, instanceGUID, exporterIFC.GetOwnerHistoryHandle(),
                            instanceName, instanceDescription, instanceObjectType, ecData.GetLocalPlacement(), prodRep, instanceElemId, pileType, null);

                        if (exportParts)
                        {
                            PartExporter.ExportHostPart(exporterIFC, element, pile, productWrapper, setter, setter.GetPlacement(), null);
                        }
                        else
                        {
                            if (matId != ElementId.InvalidElementId)
                            {
                                CategoryUtil.CreateMaterialAssociation(element.Document, exporterIFC, pile, matId);
                            }
                        }

                        productWrapper.AddElement(pile, setter, ecData, LevelUtil.AssociateElementToLevel(element));

                        OpeningUtil.CreateOpeningsIfNecessary(pile, element, ecData, exporterIFC, ecData.GetLocalPlacement(), setter, productWrapper);

                        PropertyUtil.CreateInternalRevitPropertySets(exporterIFC, element, productWrapper);
                    }
                }

                tr.Commit();
            }
        }

        private static IFCPileType GetPileType(Element element, string ifcEnumType)
        {
            string value = null;
            if (!ParameterUtil.GetStringValueFromElementOrSymbol(element, "IfcType", out value))
            {
                value = ifcEnumType;
            }

            if (String.IsNullOrEmpty(value))
                return IFCPileType.NotDefined;

            string newValue = NamingUtil.RemoveSpacesAndUnderscores(value);

            if (String.Compare(newValue, "COHESION", true) == 0)
                return IFCPileType.Cohesion;
            if (String.Compare(newValue, "FRICTION", true) == 0)
                return IFCPileType.Friction;
            if (String.Compare(newValue, "SUPPORT", true) == 0)
                return IFCPileType.Support;

            return IFCPileType.UserDefined;
        }
    }
}
