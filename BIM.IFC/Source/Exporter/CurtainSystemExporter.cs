//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2011  Autodesk, Inc.
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
using Autodesk.Revit.DB;
using BIM.IFC.Utility;

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
        /// The IFCProductWrapper.
        /// </param>
        public static void ExportCurtainObjectCommonAsContainer(ElementSet allSubElements, Element wallElement,
           ExporterIFC exporterIFC, out IFCProductWrapper productWrapper)
        {
            HashSet<ElementId> alreadyVisited = new HashSet<ElementId>();  // just in case.
            Options geomOptions = new Options();
            productWrapper = IFCProductWrapper.Create(exporterIFC, true);
            {
                foreach (Element subElem in allSubElements)
                {
                    if (subElem == null)
                        continue;

                    GeometryElement geometryElement = subElem.get_Geometry(geomOptions);
                    if (geometryElement == null)
                        continue;

                    if (alreadyVisited.Contains(subElem.Id))
                        continue;
                    alreadyVisited.Add(subElem.Id);

                    try
                    {
                        if (subElem is FamilyInstance)
                        {
                            FamilyInstance subFamInst = subElem as FamilyInstance;

                            string ifcEnumType;
                            ElementId catId = CategoryUtil.GetSafeCategoryId(subElem);
                            IFCExportType exportType = ElementFilteringUtil.GetExportTypeFromCategoryId(catId, out ifcEnumType);

                            if (exporterIFC.ExportAs2x2)
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
                            FamilyInstanceExporter.ExportFamilyInstanceAsMappedItem(exporterIFC, subFamInst, exportType, ifcEnumType, productWrapper,
                               ElementId.InvalidElementId, null);
                        }
                        else if (subElem is Mullion)
                        {
                            if (exporterIFC.ExportAs2x2)
                                ExporterIFCUtils.ExportProxyElement(exporterIFC, subElem, geometryElement, productWrapper);
                            else
                            {
                                using (IFCPlacementSetter currSetter = IFCPlacementSetter.Create(exporterIFC, wallElement))
                                {
                                    IFCAnyHandle currLocalPlacement = currSetter.GetPlacement();
                                    IFCExtrusionCreationData extraParams = new IFCExtrusionCreationData();
                                    MullionExporter.Export(exporterIFC, subElem as Mullion, geometryElement, currLocalPlacement, extraParams, currSetter, productWrapper);
                                }
                            }
                        }
                        else if (subElem is CurtainGridLine)
                        {
                            ExporterIFCUtils.ExportProxyElement(exporterIFC, subElem, geometryElement, productWrapper);
                        }
                    }
                    catch
                    {
                        continue;
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
        public static IFCAnyHandle ExportCurtainObjectCommonAsOneBRep(ElementSet allSubElements, Element wallElement,
           ExporterIFC exporterIFC, IFCPlacementSetter setter, IFCAnyHandle localPlacement)
        {
            IFCAnyHandle prodDefRep = IFCAnyHandle.Create();
            double eps = wallElement.Document.Application.VertexTolerance * exporterIFC.LinearScale;

            IFCFile file = exporterIFC.GetFile();
            IFCAnyHandle contextOfItems = exporterIFC.Get3DContextHandle();

            IFCGeometryInfo info = IFCGeometryInfo.CreateSurfaceGeometryInfo(eps);

            HashSet<IFCAnyHandle> bodyItems = new HashSet<IFCAnyHandle>();

            // Want to make sure we don't accidentally add a mullion or curtain line more than once.
            HashSet<ElementId> alreadyVisited = new HashSet<ElementId>();

            Options geomOptions = new Options();
            foreach (Element subElem in allSubElements)
            {
                GeometryElement geometryElement = subElem.get_Geometry(geomOptions);
                if (geometryElement == null)
                    continue;

                if (alreadyVisited.Contains(subElem.Id))
                    continue;
                alreadyVisited.Add(subElem.Id);

                ExporterIFCUtils.CollectGeometryInfo(exporterIFC, info, geometryElement, XYZ.Zero, false);
                IFCAnyHandle outer = file.CreateClosedShell(info.GetSurfaces());

                if (outer.HasValue)
                    bodyItems.Add(RepresentationUtil.CreateFacetedBRep(exporterIFC, outer));
            }

            if (bodyItems.Count == 0)
                return prodDefRep;

            ElementId catId = CategoryUtil.GetSafeCategoryId(wallElement);
            IFCAnyHandle shapeRep = RepresentationUtil.CreateBRepRep(exporterIFC, catId, contextOfItems, bodyItems);
            if (!shapeRep.HasValue)
                return prodDefRep;

            IList<IFCAnyHandle> shapeReps = new List<IFCAnyHandle>();
            shapeReps.Add(shapeRep);
            prodDefRep = file.CreateProductDefinitionShape(IFCLabel.Create(), IFCLabel.Create(), shapeReps);
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
        private static bool CanExportCurtainWallAsContainer(ElementSet allSubElements)
        {
            Options geomOptions = new Options();
            foreach (Element subElem in allSubElements)
            {
                GeometryElement geometryElement = subElem.get_Geometry(geomOptions);
                if (geometryElement == null)
                    continue;

                if (subElem is CurtainGridLine)
                    continue;

                if (!(subElem is FamilyInstance) && !(subElem is Mullion))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Export Curtain Walls and Roofs.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="allSubElements">
        /// Collection of elements contained in the host curtain element.
        /// </param>
        /// <param name="hostElement">
        /// The host element to be exported.
        /// </param>
        /// <param name="productWrapper">
        /// The IFCProductWrapper.
        /// </param>
        public static void ExportBase(ExporterIFC exporterIFC, ElementSet allSubElements,
           HostObject hostElement, IFCProductWrapper wrapper)
        {
            IFCFile file = exporterIFC.GetFile();
            IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();

            IFCPlacementSetter setter = null;
            IFCProductWrapper curtainWallSubWrapper = null;
            try
            {
                IFCAnyHandle localPlacement = IFCAnyHandle.Create();
                bool canExportCurtainWallAsContainer = CanExportCurtainWallAsContainer(allSubElements);
                IFCAnyHandle rep = IFCAnyHandle.Create();
                if (canExportCurtainWallAsContainer)
                {
                    curtainWallSubWrapper = IFCProductWrapper.Create(exporterIFC, true);
                    setter = IFCPlacementSetter.Create(exporterIFC, hostElement);
                    localPlacement = setter.GetPlacement();
                    rep = ExportCurtainObjectCommonAsOneBRep(allSubElements, hostElement, exporterIFC, setter, localPlacement);
                }
                else
                {
                    ExportCurtainObjectCommonAsContainer(allSubElements, hostElement, exporterIFC, out curtainWallSubWrapper);
                    // This has to go LAST.  Why?  Because otherwise we apply the level transform twice -- once in the familyTrf, once here.
                    // This will be used just to put the CurtainWall on the right level.
                    setter = IFCPlacementSetter.Create(exporterIFC, hostElement);
                    localPlacement = setter.GetPlacement();
                }

                if (!rep.HasValue)
                    return;

                IFCLabel objectType = NamingUtil.CreateIFCObjectName(exporterIFC, hostElement);

                IFCExtrusionCreationData extraParams = new IFCExtrusionCreationData();
                extraParams.SetLocalPlacement(localPlacement);

                IFCAnyHandle prodRepHnd = IFCAnyHandle.Create();
                IFCAnyHandle elemHnd = IFCAnyHandle.Create();
                IFCLabel elemGUID = IFCLabel.CreateGUID(hostElement);
                IFCLabel elemName = NamingUtil.GetNameOverride(hostElement, exporterIFC.GetNameFromExportState());
                IFCLabel elemDesc = NamingUtil.GetDescriptionOverride(hostElement, IFCLabel.Create());
                IFCLabel elemType = NamingUtil.GetObjectTypeOverride(hostElement, objectType);
                IFCLabel elemId = NamingUtil.CreateIFCElementId(hostElement);
                if (hostElement is Wall || hostElement is CurtainSystem)
                {
                    elemHnd = file.CreateCurtainWall(elemGUID, ownerHistory, elemName, elemDesc, elemType, localPlacement, elemId, prodRepHnd);
                }
                else if (hostElement is RoofBase)
                {
                    string ifcEnumType = CategoryUtil.GetIFCEnumTypeName(exporterIFC, hostElement);
                    elemHnd = file.CreateRoof(elemGUID, ownerHistory, elemName, elemDesc, elemType, localPlacement, elemId, ifcEnumType, prodRepHnd);
                }
                else
                {
                    return;
                }

                if (!elemHnd.HasValue)
                    return;

                wrapper.AddElement(elemHnd, setter, extraParams, true);
                OpeningUtil.CreateOpeningsIfNecessary(elemHnd, hostElement, extraParams, exporterIFC, localPlacement, setter, wrapper);

                ExporterIFCUtils.CreateCurtainWallPropertySet(exporterIFC, hostElement, wrapper);
                ICollection<IFCAnyHandle> relatedElementIds = curtainWallSubWrapper.GetAllObjects();
                if (relatedElementIds.Count > 0)
                {
                    IFCLabel guid = IFCLabel.CreateGUID();
                    IFCLabel nameOpt = IFCLabel.Create();
                    IFCLabel descOpt = IFCLabel.Create();
                    file.CreateRelAggregates(guid, ownerHistory, nameOpt, descOpt, elemHnd, relatedElementIds);
                }
                exporterIFC.RegisterSpaceBoundingElementHandle(elemHnd, hostElement.Id, ElementId.InvalidElementId);
            }
            finally
            {
                if (setter != null)
                    setter.Dispose();
                if (curtainWallSubWrapper != null)
                    curtainWallSubWrapper.Dispose();
            }
        }

        /// <summary>
        /// Exports a curtain wall to IFC curtain wall.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="wallElement">
        /// The wall element to be exported.
        /// </param>
        /// <param name="productWrapper">
        /// The IFCProductWrapper.
        /// </param>
        public static void Export(ExporterIFC exporterIFC, Wall wallElement, IFCProductWrapper productWrapper)
        {
            // Don't export the Curtain Wall itself, which has no useful geometry; instead export all of the GReps of the
            // mullions and panels.
            CurtainGrid grid = wallElement.CurtainGrid;
            ElementSet allSubElements = grid.Panels;
            foreach (Element subElem in grid.Mullions)
                allSubElements.Insert(subElem);
            ExportBase(exporterIFC, allSubElements, wallElement, productWrapper);
        }
    }
}
