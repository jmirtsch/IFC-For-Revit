//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Export.Exporter.PropertySet;

namespace Revit.IFC.Export.Exporter
{
    /// <summary>
    /// Provides methods to export ceilings.
    /// </summary>
    class CeilingExporter
    {
        /// <summary>
        /// Exports a ceiling to IFC covering.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="ceiling">
        /// The ceiling element to be exported.
        /// </param>
        /// <param name="geomElement">
        /// The geometry element.
        /// </param>
        /// <param name="productWrapper">
        /// The ProductWrapper.
        /// </param>
        public static void ExportCeilingElement(ExporterIFC exporterIFC, Ceiling ceiling, GeometryElement geomElement, ProductWrapper productWrapper)
        {
            string ifcEnumType = ExporterUtil.GetIFCTypeFromExportTable(exporterIFC, ceiling);
            if (String.IsNullOrEmpty(ifcEnumType))
                ifcEnumType = "CEILING";
            ExportCovering(exporterIFC, ceiling, geomElement, ifcEnumType, productWrapper);
        }

        /// <summary>
        /// Exports an element as IFC covering.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="element">The element to be exported.</param>
        /// <param name="geometryElement">The geometry element.</param>
        /// <param name="productWrapper">The ProductWrapper.</param>
        public static void ExportCovering(ExporterIFC exporterIFC, Element element, GeometryElement geomElem, string ifcEnumType, ProductWrapper productWrapper)
        {
            bool exportParts = PartExporter.CanExportParts(element);
            if (exportParts && !PartExporter.CanExportElementInPartExport(element, element.LevelId, false))
                return;

            ElementType elemType = element.Document.GetElement(element.GetTypeId()) as ElementType;
            IFCFile file = exporterIFC.GetFile();

            using (IFCTransaction transaction = new IFCTransaction(file))
            {
                using (PlacementSetter setter = PlacementSetter.Create(exporterIFC, element))
                {
                    using (IFCExtrusionCreationData ecData = new IFCExtrusionCreationData())
                    {
                        ElementId categoryId = CategoryUtil.GetSafeCategoryId(element);

                        IFCAnyHandle prodRep = null;
                        if (!exportParts)
                        {
                            ecData.SetLocalPlacement(setter.LocalPlacement);
                            ecData.PossibleExtrusionAxes = IFCExtrusionAxes.TryZ;

                            BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true);
                            prodRep = RepresentationUtil.CreateAppropriateProductDefinitionShape(exporterIFC, element,
                                categoryId, geomElem, bodyExporterOptions, null, ecData, true);
                            if (IFCAnyHandleUtil.IsNullOrHasNoValue(prodRep))
                            {
                                ecData.ClearOpenings();
                                return;
                            }
                        }

                        // We will use the category of the element to set a default value for the covering.
                        string defaultCoveringEnumType = null;

                        if (categoryId == new ElementId(BuiltInCategory.OST_Ceilings))
                            defaultCoveringEnumType = "CEILING";
                        else if (categoryId == new ElementId(BuiltInCategory.OST_Floors))
                            defaultCoveringEnumType = "FLOORING";
                        else if (categoryId == new ElementId(BuiltInCategory.OST_Roofs))
                            defaultCoveringEnumType = "ROOFING";

                        string instanceGUID = GUIDUtil.CreateGUID(element);
                        string instanceName = NamingUtil.GetNameOverride(element, NamingUtil.GetIFCName(element));
                        string instanceDescription = NamingUtil.GetDescriptionOverride(element, null);
                        string instanceObjectType = NamingUtil.GetObjectTypeOverride(element, exporterIFC.GetFamilyName());
                        string instanceTag = NamingUtil.GetTagOverride(element, NamingUtil.CreateIFCElementId(element));
                        string coveringType = IFCValidateEntry.GetValidIFCType(element, ifcEnumType, defaultCoveringEnumType);

                        IFCAnyHandle covering = IFCInstanceExporter.CreateCovering(file, instanceGUID, ExporterCacheManager.OwnerHistoryHandle,
                            instanceName, instanceDescription, instanceObjectType, setter.LocalPlacement, prodRep, instanceTag, coveringType);

                        if (exportParts)
                        {
                            PartExporter.ExportHostPart(exporterIFC, element, covering, productWrapper, setter, setter.LocalPlacement, null);
                        }

                        bool containInSpace = false;
                        IFCAnyHandle localPlacementToUse = setter.LocalPlacement;

                        // Ceiling containment in Space is generally required and not specific to any view
                        if (ExporterCacheManager.CeilingSpaceRelCache.ContainsKey(element.Id))
                        {
                            IList<ElementId> roomlist = ExporterCacheManager.CeilingSpaceRelCache[element.Id];

                            // Process Ceiling to be contained in a Space only when it is exactly bounding one Space
                            if (roomlist.Count == 1)
                            {
                                productWrapper.AddElement(element, covering, setter, null, false);

                                // Modify the Ceiling placement to be relative to the Space that it bounds 
                                IFCAnyHandle roomPlacement = IFCAnyHandleUtil.GetObjectPlacement(ExporterCacheManager.SpaceInfoCache.FindSpaceHandle(roomlist[0]));
                                Transform relTrf = ExporterIFCUtils.GetRelativeLocalPlacementOffsetTransform(roomPlacement, localPlacementToUse);
                                Transform inverseTrf = relTrf.Inverse;
                                IFCAnyHandle relLocalPlacement = ExporterUtil.CreateAxis2Placement3D(file, inverseTrf.Origin, inverseTrf.BasisZ, inverseTrf.BasisX);
                                IFCAnyHandleUtil.SetAttribute(localPlacementToUse, "PlacementRelTo", roomPlacement);
                                GeometryUtil.SetRelativePlacement(localPlacementToUse, relLocalPlacement);

                                ExporterCacheManager.SpaceInfoCache.RelateToSpace(roomlist[0], covering);
                                containInSpace = true;
                            }
                        }

                        // if not contained in Space, assign it to default containment in Level
                        if (!containInSpace)
                            productWrapper.AddElement(element, covering, setter, null, true);

                        if (!exportParts)
                        {
                            Ceiling ceiling = element as Ceiling;
                            if (ceiling != null)
                            {
                                HostObjectExporter.ExportHostObjectMaterials(exporterIFC, ceiling, covering,
                                    geomElem, productWrapper, ElementId.InvalidElementId, Toolkit.IFCLayerSetDirection.Axis3, null);
                            }
                            else
                            {
                                ElementId matId = BodyExporter.GetBestMaterialIdFromGeometryOrParameter(geomElem, exporterIFC, element);
                                CategoryUtil.CreateMaterialAssociation(exporterIFC, covering, matId);
                            }
                        }

                        OpeningUtil.CreateOpeningsIfNecessary(covering, element, ecData, null,
                            exporterIFC, ecData.GetLocalPlacement(), setter, productWrapper);
                    }
                }
                transaction.Commit();
            }
        }
    }
}
