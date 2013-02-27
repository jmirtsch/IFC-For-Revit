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
    /// Provides methods to export curtain systems.
    /// </summary>
    class CurtainSystemExporter
    {
        /// <summary>
        /// Exports curtain object as container.
        /// </summary>
        /// <param name="allSubElements">
        /// Collection of elements contained in the host curtain element.
        /// </param>
        /// <param name="wallElement">
        /// The curtain wall element.
        /// </param>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="productWrapper">
        /// The ProductWrapper.
        /// </param>
        public static void ExportCurtainObjectCommonAsContainer(ICollection<ElementId> allSubElements, Element wallElement,
           ExporterIFC exporterIFC, ProductWrapper origWrapper, IFCPlacementSetter currSetter)
        {
            if (wallElement == null)
                return;

            HashSet<ElementId> alreadyVisited = new HashSet<ElementId>();  // just in case.
            Options geomOptions = GeometryUtil.GetIFCExportGeometryOptions();
            {
                foreach (ElementId subElemId in allSubElements)
                {
                    using (ProductWrapper productWrapper = ProductWrapper.Create(origWrapper))
                    {
                        Element subElem = wallElement.Document.GetElement(subElemId);
                        if (subElem == null)
                            continue;
                        GeometryElement geomElem = subElem.get_Geometry(geomOptions);
                        if (geomElem == null)
                            continue;

                        if (alreadyVisited.Contains(subElem.Id))
                            continue;
                        alreadyVisited.Add(subElem.Id);

                        try
                        {
                            if (subElem is FamilyInstance)
                            {
                                if (subElem is Mullion)
                                {
                                    if (exporterIFC.ExportAs2x2)
                                        ProxyElementExporter.Export(exporterIFC, subElem, geomElem, productWrapper);
                                    else
                                    {
                                        IFCAnyHandle currLocalPlacement = currSetter.GetPlacement();
                                        MullionExporter.Export(exporterIFC, subElem as Mullion, geomElem, currLocalPlacement, currSetter,
                                            productWrapper);
                                    }
                                }
                                else
                                {
                                    FamilyInstance subFamInst = subElem as FamilyInstance;

                                    string ifcEnumType;
                                    IFCExportType exportType = ExporterUtil.GetExportType(exporterIFC, subElem, out ifcEnumType);
                                    if (exportType == IFCExportType.ExportCurtainWall)
                                    {
                                        // By default, panels and mullions are set to the same category as their parent.  In this case,
                                        // ask to get the exportType from the category id, since we don't want to inherit the parent class.
                                        ElementId catId = CategoryUtil.GetSafeCategoryId(subElem);
                                        exportType = ElementFilteringUtil.GetExportTypeFromCategoryId(catId, out ifcEnumType);
                                    }


                                    if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2)
                                    {
                                        if ((exportType == IFCExportType.DontExport) || (exportType == IFCExportType.ExportPlateType) ||
                                           (exportType == IFCExportType.ExportMemberType))
                                            exportType = IFCExportType.ExportBuildingElementProxy;
                                    }
                                    else
                                    {
                                        if (exportType == IFCExportType.DontExport)
                                        {
                                            ifcEnumType = "CURTAIN_PANEL";
                                            exportType = IFCExportType.ExportPlateType;
                                        }
                                    }

                                    IFCAnyHandle currLocalPlacement = currSetter.GetPlacement();
                                    using (IFCExtrusionCreationData extraParams = new IFCExtrusionCreationData())
                                    {
                                        FamilyInstanceExporter.ExportFamilyInstanceAsMappedItem(exporterIFC, subFamInst, exportType, ifcEnumType, productWrapper,
                                            ElementId.InvalidElementId, null, currLocalPlacement);
                                    }
                                }
                            }
                            else if (subElem is CurtainGridLine)
                            {
                                ProxyElementExporter.Export(exporterIFC, subElem, geomElem, productWrapper);
                            }
                        }
                        catch (Exception ex)
                        {
                            if (ExporterUtil.IsFatalException(wallElement.Document, ex))
                                throw ex;
                            continue;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Exports curtain object as one Brep.
        /// </summary>
        /// <param name="allSubElements">
        /// Collection of elements contained in the host curtain element.
        /// </param>
        /// <param name="wallElement">
        /// The curtain wall element.
        /// </param>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="setter">
        /// The IFCPlacementSetter object.
        /// </param>
        /// <param name="localPlacement">
        /// The local placement handle.
        /// </param>
        /// <returns>
        /// The handle.
        /// </returns>
        public static IFCAnyHandle ExportCurtainObjectCommonAsOneBRep(ICollection<ElementId> allSubElements, Element wallElement,
           ExporterIFC exporterIFC, IFCPlacementSetter setter, IFCAnyHandle localPlacement)
        {
            IFCAnyHandle prodDefRep = null;
            Document document = wallElement.Document;
            double eps = document.Application.VertexTolerance * exporterIFC.LinearScale;

            IFCFile file = exporterIFC.GetFile();
            IFCAnyHandle contextOfItems = exporterIFC.Get3DContextHandle("Body");

            IFCGeometryInfo info = IFCGeometryInfo.CreateFaceGeometryInfo(eps);

            IList<IFCAnyHandle> bodyItems = new List<IFCAnyHandle>();

            // Want to make sure we don't accidentally add a mullion or curtain line more than once.
            HashSet<ElementId> alreadyVisited = new HashSet<ElementId>();

            Options geomOptions = GeometryUtil.GetIFCExportGeometryOptions();
            foreach (ElementId subElemId in allSubElements)
            {
                Element subElem = wallElement.Document.GetElement(subElemId);
                GeometryElement geomElem = subElem.get_Geometry(geomOptions);
                if (geomElem == null)
                    continue;

                if (alreadyVisited.Contains(subElem.Id))
                    continue;
                alreadyVisited.Add(subElem.Id);

                ExporterIFCUtils.CollectGeometryInfo(exporterIFC, info, geomElem, XYZ.Zero, false);
                HashSet<IFCAnyHandle> faces = new HashSet<IFCAnyHandle>(info.GetSurfaces());
                IFCAnyHandle outer = IFCInstanceExporter.CreateClosedShell(file, faces);

                if (!IFCAnyHandleUtil.IsNullOrHasNoValue(outer))
                    bodyItems.Add(RepresentationUtil.CreateFacetedBRep(exporterIFC, document, outer, ElementId.InvalidElementId));
            }

            if (bodyItems.Count == 0)
                return prodDefRep;

            ElementId catId = CategoryUtil.GetSafeCategoryId(wallElement);
            IFCAnyHandle shapeRep = RepresentationUtil.CreateBRepRep(exporterIFC, wallElement, catId, contextOfItems, bodyItems);
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(shapeRep))
                return prodDefRep;

            IList<IFCAnyHandle> shapeReps = new List<IFCAnyHandle>();
            shapeReps.Add(shapeRep);

            IFCAnyHandle boundingBoxRep = BoundingBoxExporter.ExportBoundingBox(exporterIFC, wallElement.get_Geometry(geomOptions), Transform.Identity);
            if (boundingBoxRep != null)
                shapeReps.Add(boundingBoxRep);

            prodDefRep = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, shapeReps);
            return prodDefRep;
        }

        /// <summary>
        /// Checks if the curtain element can be exported as container.
        /// </summary>
        /// <remarks>
        /// It checks if all sub elements to be exported have geometries.
        /// </remarks>
        /// <param name="allSubElements">
        /// Collection of elements contained in the host curtain element.
        /// </param>
        /// <param name="document">
        /// The Revit document.
        /// </param>
        /// <returns>
        /// True if it can be exported as container, false otherwise.
        /// </returns>
        private static bool CanExportCurtainWallAsContainer(ICollection<ElementId> allSubElements, Document document)
        {
            Options geomOptions = GeometryUtil.GetIFCExportGeometryOptions();

            FilteredElementCollector collector = new FilteredElementCollector(document, allSubElements);

            List<Type> curtainWallSubElementTypes = new List<Type>();
            curtainWallSubElementTypes.Add(typeof(FamilyInstance));
            curtainWallSubElementTypes.Add(typeof(CurtainGridLine));

            ElementMulticlassFilter multiclassFilter = new ElementMulticlassFilter(curtainWallSubElementTypes, true);
            collector.WherePasses(multiclassFilter);
            ICollection<ElementId> filteredSubElemments = collector.ToElementIds();
            foreach (ElementId subElemId in filteredSubElemments)
            {
                Element subElem = document.GetElement(subElemId);
                GeometryElement geomElem = subElem.get_Geometry(geomOptions);
                if (geomElem == null)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Export Curtain Walls and Roofs.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="allSubElements">Collection of elements contained in the host curtain element.</param>
        /// <param name="element">The element to be exported.</param>
        /// <param name="productWrapper">The ProductWrapper.</param>
        private static void ExportBase(ExporterIFC exporterIFC, ICollection<ElementId> allSubElements, Element element, ProductWrapper wrapper)
        {
            IFCFile file = exporterIFC.GetFile();
            IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();

            IFCPlacementSetter setter = null;

            using (ProductWrapper curtainWallSubWrapper = ProductWrapper.Create(wrapper, false))
            {
                try
                {
                    ElementId curtainWallLevel = ElementId.InvalidElementId;
                    if (element.Level != null)
                    {
                        curtainWallLevel = element.Level.Id;
                    }
                    Transform orientationTrf = Transform.Identity;
                    IFCAnyHandle localPlacement = null;
                    setter = IFCPlacementSetter.Create(exporterIFC, element, null, orientationTrf, curtainWallLevel);
                    localPlacement = setter.GetPlacement();

                    string objectType = NamingUtil.CreateIFCObjectName(exporterIFC, element);

                    IFCAnyHandle prodRepHnd = null;
                    IFCAnyHandle elemHnd = null;
                    string elemGUID = GUIDUtil.CreateGUID(element);
                    string elemName = NamingUtil.GetNameOverride(element, NamingUtil.GetIFCName(element));
                    string elemDesc = NamingUtil.GetDescriptionOverride(element, null);
                    string elemType = NamingUtil.GetObjectTypeOverride(element, objectType);
                    string elemTag = NamingUtil.GetTagOverride(element, NamingUtil.CreateIFCElementId(element));
                    if (element is Wall || element is CurtainSystem || IsLegacyCurtainElement(element))
                    {
                        elemHnd = IFCInstanceExporter.CreateCurtainWall(file, elemGUID, ownerHistory, elemName, elemDesc, elemType, localPlacement, prodRepHnd, elemTag);
                    }
                    else if (element is RoofBase)
                    {
                        //need to convert the string to enum
                        string ifcEnumType = CategoryUtil.GetIFCEnumTypeName(exporterIFC, element);
                        elemHnd = IFCInstanceExporter.CreateRoof(file, elemGUID, ownerHistory, elemName, elemDesc, elemType, localPlacement,
                            prodRepHnd, elemTag, RoofExporter.GetIFCRoofType(ifcEnumType));
                    }
                    else
                    {
                        return;
                    }

                    if (IFCAnyHandleUtil.IsNullOrHasNoValue(elemHnd))
                        return;

                    wrapper.AddElement(elemHnd, setter, null, true);

                    bool canExportCurtainWallAsContainer = CanExportCurtainWallAsContainer(allSubElements, element.Document);
                    IFCAnyHandle rep = null;
                    if (!canExportCurtainWallAsContainer)
                    {
                        rep = ExportCurtainObjectCommonAsOneBRep(allSubElements, element, exporterIFC, setter, localPlacement);
                        if (IFCAnyHandleUtil.IsNullOrHasNoValue(rep))
                            return;
                    }
                    else
                    {
                        ExportCurtainObjectCommonAsContainer(allSubElements, element, exporterIFC, curtainWallSubWrapper, setter);
                    }

                    PropertyUtil.CreateInternalRevitPropertySets(exporterIFC, element, wrapper);
                    ICollection<IFCAnyHandle> relatedElementIds = curtainWallSubWrapper.GetAllObjects();
                    if (relatedElementIds.Count > 0)
                    {
                        string guid = ExporterIFCUtils.CreateSubElementGUID(element, (int)IFCCurtainWallSubElements.RelAggregates);
                        HashSet<IFCAnyHandle> relatedElementIdSet = new HashSet<IFCAnyHandle>(relatedElementIds);
                        IFCInstanceExporter.CreateRelAggregates(file, guid, ownerHistory, null, null, elemHnd, relatedElementIdSet);
                    }
                    exporterIFC.RegisterSpaceBoundingElementHandle(elemHnd, element.Id, ElementId.InvalidElementId);

                }
                finally
                {
                    if (setter != null)
                        setter.Dispose();
                }
            }
        }

        /// <summary>
        /// Export non-legacy Curtain Walls and Roofs.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="allSubElements">Collection of elements contained in the host curtain element.</param>
        /// <param name="element">The element to be exported.</param>
        /// <param name="productWrapper">The ProductWrapper.</param>
        private static void ExportBaseWithGrids(ExporterIFC exporterIFC, Element hostElement, ProductWrapper wrapper)
        {
            // Don't export the Curtain Wall itself, which has no useful geometry; instead export all of the GReps of the
            // mullions and panels.
            CurtainGridSet gridSet = CurtainSystemExporter.GetCurtainGridSet(hostElement);
            if (gridSet == null || gridSet.Size == 0)
                return;

            HashSet<ElementId> allSubElements = new HashSet<ElementId>();
            foreach (CurtainGrid grid in gridSet)
            {
                allSubElements.UnionWith(grid.GetPanelIds());
                allSubElements.UnionWith(grid.GetMullionIds());
            }

            ExportBase(exporterIFC, allSubElements, hostElement, wrapper);
        }

        /// <summary>
        /// Exports a curtain wall to IFC curtain wall.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="hostElement">The host object element to be exported.</param>
        /// <param name="productWrapper">The ProductWrapper.</param>
        public static void ExportWall(ExporterIFC exporterIFC, Wall hostElement, ProductWrapper productWrapper)
        {
            // Don't export the Curtain Wall itself, which has no useful geometry; instead export all of the GReps of the
            // mullions and panels.
            CurtainGridSet gridSet = CurtainSystemExporter.GetCurtainGridSet(hostElement);
            if (gridSet == null)
            {
                ExportLegacyCurtainElement(exporterIFC, hostElement, productWrapper);
                return;
            }

            if (gridSet.Size == 0)
                return;

            HashSet<ElementId> allSubElements = new HashSet<ElementId>();
            foreach (CurtainGrid grid in gridSet)
            {
                allSubElements.UnionWith(grid.GetPanelIds());
                allSubElements.UnionWith(grid.GetMullionIds());
            }

            ExportBase(exporterIFC, allSubElements, hostElement, productWrapper);
        }

        /// <summary>
        /// Exports a curtain roof to IFC curtain wall.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="hostElement">The host object element to be exported.</param>
        /// <param name="productWrapper">The ProductWrapper.</param>
        public static void ExportCurtainRoof(ExporterIFC exporterIFC, RoofBase hostElement, ProductWrapper productWrapper)
        {
            ExportBaseWithGrids(exporterIFC, hostElement, productWrapper);
        }

        /// <summary>
        /// Exports a curtain system to IFC curtain system.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="hostElement">
        /// The curtain system element to be exported.
        /// </param>
        /// <param name="productWrapper">
        /// The ProductWrapper.
        /// </param>
        public static void ExportCurtainSystem(ExporterIFC exporterIFC, CurtainSystem curtainSystem, ProductWrapper productWrapper)
        {
            IFCFile file = exporterIFC.GetFile();
            using (IFCTransaction transaction = new IFCTransaction(file))
            {
                ExportBaseWithGrids(exporterIFC, curtainSystem, productWrapper);
                transaction.Commit();
            }
        }

        /// <summary>
        /// Exports a legacy curtain element to IFC curtain wall.
        /// </summary>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="curtainElement">The curtain element.</param>
        /// <param name="productWrapper">The ProductWrapper.</param>
        public static void ExportLegacyCurtainElement(ExporterIFC exporterIFC, Element curtainElement, ProductWrapper productWrapper)
        {
            ICollection<ElementId> allSubElements = ExporterIFCUtils.GetLegacyCurtainSubElements(curtainElement);

            IFCFile file = exporterIFC.GetFile();
            using (IFCTransaction transaction = new IFCTransaction(file))
            {
                ExportBase(exporterIFC, allSubElements, curtainElement, productWrapper);
                transaction.Commit();
            }
        }

        /// <summary>
        /// Checks if the element is legacy curtain element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>True if it is legacy curtain element.</returns>
        public static bool IsLegacyCurtainElement(Element element)
        {
            //for now, it is sufficient to check its category.
            return (CategoryUtil.GetSafeCategoryId(element) == new ElementId(BuiltInCategory.OST_Curtain_Systems));
        }
            
        /// <summary>
        /// Checks if the wall is legacy curtain wall.
        /// </summary>
        /// <param name="wall">The wall.</param>
        /// <returns>True if it is legacy curtain wall, false otherwise.</returns>
        public static bool IsLegacyCurtainWall(Wall wall)
        {
            try
            {
                CurtainGrid curtainGrid = wall.CurtainGrid;
                if (curtainGrid != null)
                {
                    curtainGrid.GetPanelIds();
                }
                else
                    return false;
            }
            catch (Autodesk.Revit.Exceptions.InvalidOperationException ex)
            {
                if (ex.Message == "The host object is obsolete.")
                    return true;
                else
                    throw ex;
            }

            return false;
        }

        /// <summary>
        /// Returns if an element is a legacy or non-legacy curtain system of any base element type.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>True if it is a legacy or non-legacy curtain system of any base element type, false otherwise.</returns>
        public static bool IsCurtainSystem(Element element)
        {
            if (element == null)
                return false;

            CurtainGridSet curtainGridSet = GetCurtainGridSet(element);
            if (curtainGridSet == null)
                return (element is Wall);
            return (curtainGridSet.Size > 0);
        }

        /// <summary>
        /// Provides a unified interface to get the curtain grids associated with an element.
        /// </summary>
        /// <param name="element">The host element.</param>
        /// <returns>A CurtainGridSet with 0 or more CurtainGrids, or null if invalid.</returns>
        public static CurtainGridSet GetCurtainGridSet(Element element)
        {
            CurtainGridSet curtainGridSet = null;
            if (element is Wall)
            {
                Wall wall = element as Wall;
                if (!CurtainSystemExporter.IsLegacyCurtainWall(wall))
                {
                    CurtainGrid curtainGrid = wall.CurtainGrid;
                    curtainGridSet = new CurtainGridSet();
                    if (curtainGrid != null)
                        curtainGridSet.Insert(curtainGrid);
                }
            }
            else if (element is FootPrintRoof)
            {
                FootPrintRoof footPrintRoof = element as FootPrintRoof;
                curtainGridSet = footPrintRoof.CurtainGrids;
            }
            else if (element is ExtrusionRoof)
            {
                ExtrusionRoof extrusionRoof = element as ExtrusionRoof;
                curtainGridSet = extrusionRoof.CurtainGrids;
            }
            else if (element is CurtainSystem)
            {
                CurtainSystem curtainSystem = element as CurtainSystem;
                curtainGridSet = curtainSystem.CurtainGrids;
            }

            return curtainGridSet;
        }
    }
}
