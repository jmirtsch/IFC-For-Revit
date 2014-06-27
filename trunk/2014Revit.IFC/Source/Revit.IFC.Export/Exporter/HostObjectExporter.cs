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
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Exporter
{
    /// <summary>
    /// Provides methods to export host objects.
    /// </summary>
    class HostObjectExporter
    {
        /// <summary>
        /// Exports materials for host object.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="hostObject">The host object.</param>
        /// <param name="elemHnds">The host IFC handles.</param>
        /// <param name="geometryElement">The geometry element.</param>
        /// <param name="productWrapper">The ProductWrapper.</param>
        /// <param name="levelId">The level id.</param>
        /// <param name="direction">The IFCLayerSetDirection.</param>
        /// <param name="containsBRepGeometry">True if the geometry contains BRep geoemtry.  If so, we will export an IfcMaterialList</param>
        /// <returns>True if exported successfully, false otherwise.</returns>
        public static bool ExportHostObjectMaterials(ExporterIFC exporterIFC, HostObject hostObject,
            IList<IFCAnyHandle> elemHnds, GeometryElement geometryElement, ProductWrapper productWrapper,
            ElementId levelId, Toolkit.IFCLayerSetDirection direction, bool containsBRepGeometry)
        {
            if (hostObject == null)
                return true; //nothing to do

            if (elemHnds == null || (elemHnds.Count == 0))
                return true; //nothing to do

            IFCFile file = exporterIFC.GetFile();
            using (IFCTransaction tr = new IFCTransaction(file))
            {
                if (productWrapper != null)
                    productWrapper.ClearFinishMaterials();

                double scaledOffset = 0.0, scaledWallWidth = 0.0, wallHeight = 0.0;
                Wall wall = hostObject as Wall;
                if (wall != null)
                {
                    scaledWallWidth = UnitUtil.ScaleLength(wall.Width);
                    scaledOffset = -scaledWallWidth / 2.0;
                    BoundingBoxXYZ boundingBox = wall.get_BoundingBox(null);
                    if (boundingBox != null)
                        wallHeight = boundingBox.Max.Z - boundingBox.Min.Z;
                }

                ElementId typeElemId = hostObject.GetTypeId();
                IFCAnyHandle materialLayerSet = ExporterCacheManager.MaterialLayerSetCache.Find(typeElemId);
                // Roofs with no components are only allowed one material.  We will arbitrarily choose the thickest material.
                IFCAnyHandle primaryMaterialHnd = ExporterCacheManager.MaterialLayerSetCache.FindPrimaryMaterialHnd(typeElemId);
                if (IFCAnyHandleUtil.IsNullOrHasNoValue(materialLayerSet))
                {
                    HostObjAttributes hostObjAttr = hostObject.Document.GetElement(typeElemId) as HostObjAttributes;
                    if (hostObjAttr == null)
                        return true; //nothing to do

                    List<ElementId> matIds = new List<ElementId>();
                    List<double> widths = new List<double>();
                    List<MaterialFunctionAssignment> functions = new List<MaterialFunctionAssignment>();
                    ElementId baseMatId = CategoryUtil.GetBaseMaterialIdForElement(hostObject);
                    CompoundStructure cs = hostObjAttr.GetCompoundStructure();
                    if (cs != null)
                    {
                        //TODO: Vertically compound structures are not yet supported by export.
                        if (!cs.IsVerticallyHomogeneous() && !MathUtil.IsAlmostZero(wallHeight))
                            cs = cs.GetSimpleCompoundStructure(wallHeight, wallHeight / 2.0);

                        for (int i = 0; i < cs.LayerCount; ++i)
                        {
                            ElementId matId = cs.GetMaterialId(i);
                            if (matId != ElementId.InvalidElementId)
                            {
                                matIds.Add(matId);
                            }
                            else
                            {
                                matIds.Add(baseMatId);
                            }
                            widths.Add(cs.GetLayerWidth(i));
                            // save layer function into ProductWrapper, 
                            // it's used while exporting "Function" of Pset_CoveringCommon
                            functions.Add(cs.GetLayerFunction(i));
                        }
                    }

                    if (matIds.Count == 0)
                    {
                        matIds.Add(baseMatId);
                        widths.Add(cs != null ? cs.GetWidth() : 0);
                        functions.Add(MaterialFunctionAssignment.None);
                    }

                    // We can't create IfcMaterialLayers without creating an IfcMaterialLayerSet.  So we will simply collate here.
                    IList<IFCAnyHandle> materialHnds = new List<IFCAnyHandle>();
                    IList<int> widthIndices = new List<int>();
                    double thickestLayer = 0.0;
                    for (int ii = 0; ii < matIds.Count; ++ii)
                    {
                        // Require positive width for IFC2x3 and before, and non-negative width for IFC4.
                        if (widths[ii] < -MathUtil.Eps())
                            continue;

                        bool almostZeroWidth = MathUtil.IsAlmostZero(widths[ii]);
                        if (ExporterCacheManager.ExportOptionsCache.FileVersion != IFCVersion.IFC4 && almostZeroWidth)
                            continue;

                        if (almostZeroWidth)
                            widths[ii] = 0.0;

                        IFCAnyHandle materialHnd = CategoryUtil.GetOrCreateMaterialHandle(exporterIFC, matIds[ii]);
                        if (primaryMaterialHnd == null || (widths[ii] > thickestLayer))
                        {
                            primaryMaterialHnd = materialHnd;
                            thickestLayer = widths[ii];
                        }

                        widthIndices.Add(ii);
                        materialHnds.Add(materialHnd);

                        if ((productWrapper != null) && (functions[ii] == MaterialFunctionAssignment.Finish1 || functions[ii] == MaterialFunctionAssignment.Finish2))
                        {
                            productWrapper.AddFinishMaterial(materialHnd);
                        }
                    }

                    int numLayersToCreate = widthIndices.Count;
                    if (numLayersToCreate == 0)
                        return false;

                    if (!containsBRepGeometry)
                    {
                        IList<IFCAnyHandle> layers = new List<IFCAnyHandle>(numLayersToCreate);

                        for (int ii = 0; ii < numLayersToCreate; ii++)
                        {
                            int widthIndex = widthIndices[ii];
                            double scaledWidth = UnitUtil.ScaleLength(widths[widthIndex]);
                            IFCAnyHandle materialLayer = IFCInstanceExporter.CreateMaterialLayer(file, materialHnds[ii], scaledWidth, null);
                            layers.Add(materialLayer);
                        }

                        string layerSetName = exporterIFC.GetFamilyName();
                        materialLayerSet = IFCInstanceExporter.CreateMaterialLayerSet(file, layers, layerSetName);

                        ExporterCacheManager.MaterialLayerSetCache.Register(typeElemId, materialLayerSet);
                        ExporterCacheManager.MaterialLayerSetCache.RegisterPrimaryMaterialHnd(typeElemId, primaryMaterialHnd);
                    }
                    else
                    {
                        foreach (IFCAnyHandle elemHnd in elemHnds)
                        {
                            CategoryUtil.CreateMaterialAssociations(exporterIFC, elemHnd, matIds);
                        }
                    }
                }

                // IfcMaterialLayerSetUsage is not supported for IfcWall, only IfcWallStandardCase.
                IFCAnyHandle layerSetUsage = null;
                for (int ii = 0; ii < elemHnds.Count; ii++)
                {
                    IFCAnyHandle elemHnd = elemHnds[ii];
                    if (IFCAnyHandleUtil.IsNullOrHasNoValue(elemHnd))
                        continue;

                    SpaceBoundingElementUtil.RegisterSpaceBoundingElementHandle(exporterIFC, elemHnd, hostObject.Id, levelId);
                    if (containsBRepGeometry)
                        continue;

                    HashSet<IFCAnyHandle> relDecomposesSet = IFCAnyHandleUtil.GetRelDecomposes(elemHnd);

                    IList<IFCAnyHandle> subElemHnds = null;
                    if (relDecomposesSet != null && relDecomposesSet.Count == 1)
                    {
                        IFCAnyHandle relAggregates = relDecomposesSet.First();
                        if (IFCAnyHandleUtil.IsTypeOf(relAggregates, IFCEntityType.IfcRelAggregates))
                            subElemHnds = IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(relAggregates, "RelatedObjects");
                    }

                    bool hasSubElems = (subElemHnds != null && subElemHnds.Count != 0);
                    bool isRoof = IFCAnyHandleUtil.IsTypeOf(elemHnd, IFCEntityType.IfcRoof);
                    if (!hasSubElems && !isRoof && !IFCAnyHandleUtil.IsTypeOf(elemHnd, IFCEntityType.IfcWall))
                    {
                        if (layerSetUsage == null)
                        {
                            bool flipDirSense = true;
                            if (wall != null)
                            {
                                // if we have flipped the center curve on export, we need to take that into account here.
                                // We flip the center curve on export if it is an arc and it has a negative Z direction.
                                LocationCurve locCurve = wall.Location as LocationCurve;
                                if (locCurve != null)
                                {
                                    Curve curve = locCurve.Curve;
                                    Plane defPlane = new Plane(XYZ.BasisX, XYZ.BasisY, XYZ.Zero);
                                    bool curveFlipped = GeometryUtil.MustFlipCurve(defPlane, curve);
                                    flipDirSense = !(wall.Flipped ^ curveFlipped);
                                }
                            }
                            else if (hostObject is Floor)
                            {
                                flipDirSense = false;
                            }

                            double offsetFromReferenceLine = flipDirSense ? -scaledOffset : scaledOffset;
                            IFCDirectionSense sense = flipDirSense ? IFCDirectionSense.Negative : IFCDirectionSense.Positive;

                            layerSetUsage = IFCInstanceExporter.CreateMaterialLayerSetUsage(file, materialLayerSet, direction, sense, offsetFromReferenceLine);
                        }
                        ExporterCacheManager.MaterialLayerRelationsCache.Add(layerSetUsage, elemHnd);
                    }
                    else
                    {
                        if (hasSubElems)
                        {
                            foreach (IFCAnyHandle subElemHnd in subElemHnds)
                            {
                                if (!IFCAnyHandleUtil.IsNullOrHasNoValue(subElemHnd))
                                    ExporterCacheManager.MaterialLayerRelationsCache.Add(materialLayerSet, subElemHnd);
                            }
                        }
                        else if (!isRoof)
                        {
                            ExporterCacheManager.MaterialLayerRelationsCache.Add(materialLayerSet, elemHnd);
                        }
                        else if (primaryMaterialHnd != null)
                        {
                            ExporterCacheManager.MaterialLayerRelationsCache.Add(primaryMaterialHnd, elemHnd);
                        }
                    }
                }

                tr.Commit();
                return true;
            }
        }

        /// <summary>
        /// Exports materials for host object.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="hostObject">The host object.</param>
        /// <param name="elemHnd">The host IFC handle.</param>
        /// <param name="geometryElement">The geometry element.</param>
        /// <param name="productWrapper">The ProductWrapper.</param>
        /// <param name="levelId">The level id.</param>
        /// <param name="direction">The IFCLayerSetDirection.</param>
        /// <param name="containsBRepGeometry">True if the geometry contains BRep geoemtry.  If so, we will export an IfcMaterialList.  If null, we will calculate.</param>
        /// <returns>True if exported successfully, false otherwise.</returns>
        public static bool ExportHostObjectMaterials(ExporterIFC exporterIFC, HostObject hostObject,
            IFCAnyHandle elemHnd, GeometryElement geometryElement, ProductWrapper productWrapper,
            ElementId levelId, Toolkit.IFCLayerSetDirection direction, bool? containsBRepGeometry)
        {
            IList<IFCAnyHandle> elemHnds = new List<IFCAnyHandle>();
            elemHnds.Add(elemHnd);

            // Setting doesContainBRepGeometry to false below preserves the original behavior that we created IfcMaterialLists for all geometries.
            // TODO: calculate, or pass in, a valid bool value for Ceilings, Roofs, and Wall Sweeps.
            bool doesContainBRepGeometry = containsBRepGeometry.HasValue ? containsBRepGeometry.Value : false;
            return ExportHostObjectMaterials(exporterIFC, hostObject, elemHnds, geometryElement, productWrapper, levelId, direction, doesContainBRepGeometry);
        }

        /// <summary>
        /// Gets the material Id of the first layer of the host object.
        /// </summary>
        /// <param name="hostObject">The host object.</param>
        /// <returns>The material id.</returns>
        public static ElementId GetFirstLayerMaterialId(HostObject hostObject)
        {
            ElementId typeElemId = hostObject.GetTypeId();
            HostObjAttributes hostObjAttr = hostObject.Document.GetElement(typeElemId) as HostObjAttributes;
            if (hostObjAttr == null)
                return ElementId.InvalidElementId;

            CompoundStructure cs = hostObjAttr.GetCompoundStructure();
            if (cs != null)
            {
                ElementId matId = cs.LayerCount > 0 ? cs.GetMaterialId(0) : ElementId.InvalidElementId;
                if (matId != ElementId.InvalidElementId)
                    return matId;
                else
                    return CategoryUtil.GetBaseMaterialIdForElement(hostObject);;
            }

            return ElementId.InvalidElementId;
        }

        /// <summary>
        /// Gets the material ids of finish function of the host object.
        /// </summary>
        /// <param name="hostObject">The host object.</param>
        /// <returns>The material ids.</returns>
        public static ISet<ElementId> GetFinishMaterialIds(HostObject hostObject)
        {
            HashSet<ElementId> matIds = new HashSet<ElementId>();

            ElementId typeElemId = hostObject.GetTypeId();
            HostObjAttributes hostObjAttr = hostObject.Document.GetElement(typeElemId) as HostObjAttributes;
            if (hostObjAttr == null)
                return matIds;

            ElementId baseMatId = CategoryUtil.GetBaseMaterialIdForElement(hostObject);
            CompoundStructure cs = hostObjAttr.GetCompoundStructure();
            if (cs != null)
            {
                for (int i = 0; i < cs.LayerCount; ++i)
                {
                    MaterialFunctionAssignment function = cs.GetLayerFunction(i);
                    if (function == MaterialFunctionAssignment.Finish1 || function == MaterialFunctionAssignment.Finish2)
                    {
                        ElementId matId = cs.GetMaterialId(i);
                        if (matId != ElementId.InvalidElementId)
                        {
                            matIds.Add(matId);
                        }
                        else if (baseMatId != ElementId.InvalidElementId)
                        {
                            matIds.Add(baseMatId);
                        }
                    }
                }
            }

            return matIds;
        }
    }
}
