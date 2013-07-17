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
    /// Provides methods to export ceilings.
    /// </summary>
    class CeilingExporter
    {
        /// <summary>
        /// Gets IFC covering type for an element.
        /// </summary>
        /// <param name="element">
        /// The element.
        /// </param>
        /// <param name="typeName">
        /// The type name.
        /// </param>
        public static Toolkit.IFCCoveringType GetIFCCoveringType(Element element, string typeName)
        {
            string value = null;
            if (!ParameterUtil.GetStringValueFromElementOrSymbol(element, "IfcType", out value))
            {
                value = typeName;
            }
            if (String.IsNullOrEmpty(value))
                return Toolkit.IFCCoveringType.NotDefined;

            string newValue = NamingUtil.RemoveSpacesAndUnderscores(value);

            if (String.Compare(newValue, "USERDEFINED", true) == 0)
                return Toolkit.IFCCoveringType.UserDefined;
            if (String.Compare(newValue, "CEILING", true) == 0)
                return Toolkit.IFCCoveringType.Ceiling;
            if (String.Compare(newValue, "FLOORING", true) == 0)
                return Toolkit.IFCCoveringType.Flooring;
            if (String.Compare(newValue, "CLADDING", true) == 0)
                return Toolkit.IFCCoveringType.Cladding;
            if (String.Compare(newValue, "ROOFING", true) == 0)
                return Toolkit.IFCCoveringType.Roofing;
            if (String.Compare(newValue, "INSULATION", true) == 0)
                return Toolkit.IFCCoveringType.Insulation;
            if (String.Compare(newValue, "MEMBRANE", true) == 0)
                return Toolkit.IFCCoveringType.Membrane;
            if (String.Compare(newValue, "SLEEVING", true) == 0)
                return Toolkit.IFCCoveringType.Sleeving;
            if (String.Compare(newValue, "WRAPPING", true) == 0)
                return Toolkit.IFCCoveringType.Wrapping;

            return Toolkit.IFCCoveringType.NotDefined;
        }

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
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="element">
        /// The element to be exported.
        /// </param>
        /// <param name="geometryElement">
        /// The geometry element.
        /// </param>
        /// <param name="productWrapper">
        /// The ProductWrapper.
        /// </param>
        public static void ExportCovering(ExporterIFC exporterIFC, Element element, GeometryElement geomElem, string ifcEnumType, ProductWrapper productWrapper)
        {
            bool exportParts = PartExporter.CanExportParts(element);
            if (exportParts && !PartExporter.CanExportElementInPartExport(element, element.Level.Id, false))
                return;

            ElementType elemType = element.Document.GetElement(element.GetTypeId()) as ElementType;
            IFCFile file = exporterIFC.GetFile();

            using (IFCTransaction transaction = new IFCTransaction(file))
            {
                using (IFCPlacementSetter setter = IFCPlacementSetter.Create(exporterIFC, element))
                {
                    using (IFCExtrusionCreationData ecData = new IFCExtrusionCreationData())
                    {
                        IFCAnyHandle prodRep = null;
                        if (!exportParts)
                        {
                            ecData.SetLocalPlacement(setter.GetPlacement());
                            ecData.PossibleExtrusionAxes = IFCExtrusionAxes.TryZ;

                            ElementId categoryId = CategoryUtil.GetSafeCategoryId(element);

                            BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true);
                            prodRep = RepresentationUtil.CreateAppropriateProductDefinitionShape(exporterIFC, element,
                                categoryId, geomElem, bodyExporterOptions, null, ecData, true);
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
                        string instanceElemId = NamingUtil.GetTagOverride(element, NamingUtil.CreateIFCElementId(element));
                        Toolkit.IFCCoveringType coveringType = GetIFCCoveringType(element, ifcEnumType);

                        IFCAnyHandle covering = IFCInstanceExporter.CreateCovering(file, instanceGUID, exporterIFC.GetOwnerHistoryHandle(),
                            instanceName, instanceDescription, instanceObjectType, setter.GetPlacement(), prodRep, instanceElemId, coveringType);

                        if (exportParts)
                        {
                            PartExporter.ExportHostPart(exporterIFC, element, covering, productWrapper, setter, setter.GetPlacement(), null);
                        }

                        Boolean containInSpace = false;
                        IFCAnyHandle localPlacementToUse = setter.GetPlacement();

                        // Assign ceiling to room/IfcSpace if it is bounding a single Room for FMHandOver view only
                        ExportOptionsCache exportOptionsCache = ExporterCacheManager.ExportOptionsCache;
                        if (String.Compare(exportOptionsCache.SelectedConfigName, "FMHandOverView") == 0)
                        {
                            if (ExporterCacheManager.CeilingSpaceRelCache.ContainsKey(element.Id))
                            {
                                IList<ElementId> roomlist = ExporterCacheManager.CeilingSpaceRelCache[element.Id];

                                // Process Ceiling to be contained in a Space only when it is exactly bounding one Space
                                if (roomlist.Count == 1)
                                {
                                    productWrapper.AddElement(covering, setter, null, false);

                                    // Modify the Ceiling placement to be relative to the Space that it bounds 
                                    IFCAnyHandle roomPlacement = IFCAnyHandleUtil.GetObjectPlacement(ExporterCacheManager.SpatialElementHandleCache.Find(roomlist[0]));
                                    Transform relTrf = ExporterIFCUtils.GetRelativeLocalPlacementOffsetTransform(roomPlacement, localPlacementToUse);
                                    Transform inverseTrf = relTrf.Inverse;
                                    IFCAnyHandle relLocalPlacement = ExporterUtil.CreateAxis2Placement3D(file, inverseTrf.Origin, inverseTrf.BasisZ, inverseTrf.BasisX);
                                    IFCAnyHandleUtil.SetAttribute(localPlacementToUse, "PlacementRelTo", roomPlacement);
                                    GeometryUtil.SetRelativePlacement(localPlacementToUse, relLocalPlacement);

                                    exporterIFC.RelateSpatialElement(roomlist[0], covering);
                                    containInSpace = true;
                                }
                            }
                        }

                        // if not contained in Space, assign it to default containment in Level
                        if (!containInSpace)
                            productWrapper.AddElement(covering, setter, null, true);

                        if (!exportParts)
                        {
                            Ceiling ceiling = element as Ceiling;
                            if (ceiling != null)
                            {
                                HostObjectExporter.ExportHostObjectMaterials(exporterIFC, ceiling, covering,
                                    geomElem, productWrapper, ElementId.InvalidElementId, Toolkit.IFCLayerSetDirection.Axis3);
                            }
                            else
                            {
                                ElementId matId = BodyExporter.GetBestMaterialIdFromGeometryOrParameter(geomElem, exporterIFC, element);
                                CategoryUtil.CreateMaterialAssociation(element.Document, exporterIFC, covering, matId);
                            }
                        }

                        OpeningUtil.CreateOpeningsIfNecessary(covering, element, ecData, null,
                            exporterIFC, ecData.GetLocalPlacement(), setter, productWrapper);

                        PropertyUtil.CreateInternalRevitPropertySets(exporterIFC, element, productWrapper);
                    }
                }
                transaction.Commit();
            }
        }
    }
}
