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
using System.Text;
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB.Structure;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Common.Utility;


namespace Revit.IFC.Export.Exporter
{
    /// <summary>
    /// Provides methods to export floor elements.
    /// </summary>
    class FloorExporter
    {
        /// <summary>
        /// Exports a generic element as an IfcSlab.</summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="floor">The floor element.</param>
        /// <param name="geometryElement">The geometry element.</param>
        /// <param name="ifcEnumType">The string value represents the IFC type.</param>
        /// <param name="productWrapper">The ProductWrapper.</param>
        /// <returns>True if the floor is exported successfully, false otherwise.</returns>
        public static void ExportGenericSlab(ExporterIFC exporterIFC, Element slabElement, GeometryElement geometryElement, string ifcEnumType,
            ProductWrapper productWrapper)
        {
            if (geometryElement == null)
                return;

            bool exportParts = PartExporter.CanExportParts(slabElement);
                    
            IFCFile file = exporterIFC.GetFile();
            IList<IFCAnyHandle> slabHnds = new List<IFCAnyHandle>();
            IList<IFCAnyHandle> brepSlabHnds = new List<IFCAnyHandle>();
            IList<IFCAnyHandle> nonBrepSlabHnds = new List<IFCAnyHandle>();

            using (IFCTransaction tr = new IFCTransaction(file))
            {
                using (IFCTransformSetter transformSetter = IFCTransformSetter.Create())
                {
                    using (PlacementSetter placementSetter = PlacementSetter.Create(exporterIFC, slabElement))
                    {
                        IFCAnyHandle localPlacement = placementSetter.LocalPlacement;
                        IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();
                        bool exportedAsInternalExtrusion = false;

                        ElementId catId = CategoryUtil.GetSafeCategoryId(slabElement);

                        IList<IFCAnyHandle> prodReps = new List<IFCAnyHandle>();
                        IList<ShapeRepresentationType> repTypes = new List<ShapeRepresentationType>();
                        IList<IList<CurveLoop>> extrusionLoops = new List<IList<CurveLoop>>();
                        IList<IFCExtrusionCreationData> loopExtraParams = new List<IFCExtrusionCreationData>();
                        Plane floorPlane = GeometryUtil.CreateDefaultPlane();

                        IList<IFCAnyHandle> localPlacements = new List<IFCAnyHandle>();

                        using (IFCExtrusionCreationData ecData = new IFCExtrusionCreationData())
                        {
                            BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true);
                            bodyExporterOptions.TessellationLevel = BodyExporter.GetTessellationLevel();
                            BodyData bodyData;
                            IFCAnyHandle prodDefHnd = RepresentationUtil.CreateAppropriateProductDefinitionShape(exporterIFC,
                                slabElement, catId, geometryElement, bodyExporterOptions, null, ecData, out bodyData);
                            if (IFCAnyHandleUtil.IsNullOrHasNoValue(prodDefHnd))
                            {
                                ecData.ClearOpenings();
                                return;
                            }

                            prodReps.Add(prodDefHnd);
                            repTypes.Add(bodyData.ShapeRepresentationType);
                        }

                        // Create the slab from either the extrusion or the BRep information.
                        string ifcGUID = GUIDUtil.CreateGUID(slabElement);

                        int numReps = exportParts ? 1 : prodReps.Count;

                        string entityType = IFCValidateEntry.GetValidIFCType<IFCSlabType>(slabElement, ifcEnumType, "FLOOR");

                        for (int ii = 0; ii < numReps; ii++)
                        {
                            string ifcName = NamingUtil.GetNameOverride(slabElement, NamingUtil.GetIFCNamePlusIndex(slabElement, ii == 0 ? -1 : ii + 1));
                            string ifcDescription = NamingUtil.GetDescriptionOverride(slabElement, null);
                            string ifcObjectType = NamingUtil.GetObjectTypeOverride(slabElement, exporterIFC.GetFamilyName());
                            string ifcTag = NamingUtil.GetTagOverride(slabElement, NamingUtil.CreateIFCElementId(slabElement));

                            string currentGUID = (ii == 0) ? ifcGUID : GUIDUtil.CreateGUID();
                            IFCAnyHandle localPlacementHnd = exportedAsInternalExtrusion ? localPlacements[ii] : localPlacement;

                            IFCAnyHandle slabHnd = IFCInstanceExporter.CreateSlab(file, currentGUID, ownerHistory, ifcName,
                                    ifcDescription, ifcObjectType, localPlacementHnd, exportParts ? null : prodReps[ii],
                                    ifcTag, entityType);

                            if (IFCAnyHandleUtil.IsNullOrHasNoValue(slabHnd))
                                return;

                            if (exportParts)
                            {
                                PartExporter.ExportHostPart(exporterIFC, slabElement, slabHnd, productWrapper, placementSetter, localPlacementHnd, null);
                            }

                            slabHnds.Add(slabHnd);

                            if (!exportParts)
                            {
                                if (repTypes[ii] == ShapeRepresentationType.Brep)
                                    brepSlabHnds.Add(slabHnd);
                                else
                                    nonBrepSlabHnds.Add(slabHnd);
                            }
                        }

                        for (int ii = 0; ii < numReps; ii++)
                        {
                            IFCExtrusionCreationData loopExtraParam = ii < loopExtraParams.Count ? loopExtraParams[ii] : null;
                            productWrapper.AddElement(slabElement, slabHnds[ii], placementSetter, loopExtraParam, true);
                        }

                        if (exportedAsInternalExtrusion)
                            ExporterIFCUtils.ExportExtrudedSlabOpenings(exporterIFC, slabElement, placementSetter.LevelInfo,
                               localPlacements[0], slabHnds, extrusionLoops, floorPlane, productWrapper.ToNative());
                    }

                    if (!exportParts)
                    {
                        if (slabElement is HostObject)
                        {
                            HostObject hostObject = slabElement as HostObject;
                            if (nonBrepSlabHnds.Count > 0)
                            {
                                HostObjectExporter.ExportHostObjectMaterials(exporterIFC, hostObject, nonBrepSlabHnds,
                                    geometryElement, productWrapper, ElementId.InvalidElementId, Toolkit.IFCLayerSetDirection.Axis3, false);
                            }
                            if (brepSlabHnds.Count > 0)
                            {
                                HostObjectExporter.ExportHostObjectMaterials(exporterIFC, hostObject, brepSlabHnds,
                                    geometryElement, productWrapper, ElementId.InvalidElementId, Toolkit.IFCLayerSetDirection.Axis3, true);
                            }
                        }
                        else if (slabElement is FamilyInstance && slabHnds.Count > 0)
                        {
                            ElementId matId = BodyExporter.GetBestMaterialIdFromGeometryOrParameter(geometryElement, exporterIFC, slabElement);
                            Document doc = slabElement.Document;
                            foreach (IFCAnyHandle slabHnd in slabHnds)
                            {
                                CategoryUtil.CreateMaterialAssociation(exporterIFC, slabHnd, matId);
                            }
                        }
                    }
                }
                tr.Commit();

                return;
            }
        }

        /// <summary>
        /// Exports a CeilingAndFloor element to IFC.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="floor">The floor element.</param>
        /// <param name="geometryElement">The geometry element.</param>
        /// <param name="productWrapper">The ProductWrapper.</param>
        public static void ExportCeilingAndFloorElement(ExporterIFC exporterIFC, CeilingAndFloor floorElement, GeometryElement geometryElement, 
            ProductWrapper productWrapper)
        {
            if (geometryElement == null)
                return;

            // export parts or not
            bool exportParts = PartExporter.CanExportParts(floorElement);
            if (exportParts && !PartExporter.CanExportElementInPartExport(floorElement, floorElement.LevelId, false))
                return;

            string ifcEnumType;
            IFCExportType exportType = ExporterUtil.GetExportType(exporterIFC, floorElement, out ifcEnumType);

            IFCFile file = exporterIFC.GetFile();
            IList<IFCAnyHandle> slabHnds = new List<IFCAnyHandle>();
            IList<IFCAnyHandle> brepSlabHnds = new List<IFCAnyHandle>();
            IList<IFCAnyHandle> nonBrepSlabHnds = new List<IFCAnyHandle>();
                        
            using (IFCTransaction tr = new IFCTransaction(file))
            {
                using (IFCTransformSetter transformSetter = IFCTransformSetter.Create())
                {
                    using (PlacementSetter placementSetter = PlacementSetter.Create(exporterIFC, floorElement))
                    {
                        IFCAnyHandle localPlacement = placementSetter.LocalPlacement;
                        IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();
                        bool exportedAsInternalExtrusion = false;

                        ElementId catId = CategoryUtil.GetSafeCategoryId(floorElement);

                        IList<IFCAnyHandle> prodReps = new List<IFCAnyHandle>();
                        IList<ShapeRepresentationType> repTypes = new List<ShapeRepresentationType>();
                        IList<IList<CurveLoop>> extrusionLoops = new List<IList<CurveLoop>>();
                        IList<IFCExtrusionCreationData> loopExtraParams = new List<IFCExtrusionCreationData>();
                        Plane floorPlane = GeometryUtil.CreateDefaultPlane();

                        IList<IFCAnyHandle> localPlacements = new List<IFCAnyHandle>();

                        if (!exportParts && (floorElement is Floor))
                        {
                            Floor floor = floorElement as Floor;

                            // First, try to use the ExtrusionAnalyzer for the limited cases it handles - 1 solid, no openings, end clippings only.
                            // Also limited to cases with line and arc boundaries.
                            //
                            SolidMeshGeometryInfo solidMeshInfo = GeometryUtil.GetSplitSolidMeshGeometry(geometryElement);
                            IList<Solid> solids = solidMeshInfo.GetSolids();
                            IList<Mesh> meshes = solidMeshInfo.GetMeshes();

                            if (solids.Count == 1 && meshes.Count == 0)
                            {
                                bool completelyClipped;
                                XYZ floorExtrusionDirection = new XYZ(0, 0, -1);
                                XYZ modelOrigin = XYZ.Zero;

                                XYZ floorOrigin = floor.GetVerticalProjectionPoint(modelOrigin, FloorFace.Top);
                                if (floorOrigin == null)
                                {
                                    // GetVerticalProjectionPoint may return null if FloorFace.Top is an edited face that doesn't 
                                    // go thruough te Revit model orgigin.  We'll try the midpoint of the bounding box instead.
                                    BoundingBoxXYZ boundingBox = floorElement.get_BoundingBox(null);
                                    modelOrigin = (boundingBox.Min + boundingBox.Max) / 2.0;
                                    floorOrigin = floor.GetVerticalProjectionPoint(modelOrigin, FloorFace.Top);
                                }

                                if (floorOrigin != null)
                                {
                                    XYZ floorDir = floor.GetNormalAtVerticalProjectionPoint(floorOrigin, FloorFace.Top);
                                    Plane extrusionAnalyzerFloorPlane = new Plane(floorDir, floorOrigin);

                                    HandleAndData floorAndProperties =
                                        ExtrusionExporter.CreateExtrusionWithClippingAndProperties(exporterIFC, floorElement,
                                        catId, solids[0], extrusionAnalyzerFloorPlane, floorExtrusionDirection, null, out completelyClipped);
                                    if (completelyClipped)
                                        return;
                                    if (floorAndProperties.Handle != null)
                                    {
                                        IList<IFCAnyHandle> representations = new List<IFCAnyHandle>();
                                        representations.Add(floorAndProperties.Handle);
                                        IFCAnyHandle prodRep = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, representations);
                                        prodReps.Add(prodRep);
                                        repTypes.Add(ShapeRepresentationType.SweptSolid);

                                        if (floorAndProperties.Data != null)
                                            loopExtraParams.Add(floorAndProperties.Data);
                                    }
                                }
                            }
                        }
                     
                        // Use internal routine as backup that handles openings.
                        if (prodReps.Count == 0)
                        {
                            exportedAsInternalExtrusion = ExporterIFCUtils.ExportSlabAsExtrusion(exporterIFC, floorElement,
                                geometryElement, transformSetter, localPlacement, out localPlacements, out prodReps,
                                out extrusionLoops, out loopExtraParams, floorPlane);
                            for (int ii = 0; ii < prodReps.Count; ii++)
                            {
                                // all are extrusions
                                repTypes.Add(ShapeRepresentationType.SweptSolid);
                            }
                        }

                        if (prodReps.Count == 0)
                        {
                            using (IFCExtrusionCreationData ecData = new IFCExtrusionCreationData())
                            {
                                BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true);
                                bodyExporterOptions.TessellationLevel = BodyExporter.GetTessellationLevel();
                                BodyData bodyData;
                                IFCAnyHandle prodDefHnd = RepresentationUtil.CreateAppropriateProductDefinitionShape(exporterIFC,
                                    floorElement, catId, geometryElement, bodyExporterOptions, null, ecData, out bodyData);
                                if (IFCAnyHandleUtil.IsNullOrHasNoValue(prodDefHnd))
                                {
                                    ecData.ClearOpenings();
                                    return;
                                }

                                prodReps.Add(prodDefHnd);
                                repTypes.Add(bodyData.ShapeRepresentationType);
                            }
                        }
                    
                        // Create the slab from either the extrusion or the BRep information.
                        string ifcGUID = GUIDUtil.CreateGUID(floorElement);

                        int numReps = exportParts ? 1 : prodReps.Count;

                        string entityType = null;

                        switch (exportType)
                        {
                            case IFCExportType.IfcFooting:
                                if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
                                    entityType = IFCValidateEntry.GetValidIFCType<Revit.IFC.Export.Toolkit.IFC4.IFCFootingType>(floorElement, ifcEnumType, null);
                                else
                                    entityType = IFCValidateEntry.GetValidIFCType<IFCFootingType>(floorElement, ifcEnumType, null);
                                break;
                            case IFCExportType.IfcCovering:
                                entityType = IFCValidateEntry.GetValidIFCType<IFCCoveringType>(floorElement, ifcEnumType, "FLOORING");
                                break;
                            case IFCExportType.IfcRamp:
                                if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
                                    entityType = IFCValidateEntry.GetValidIFCType<Revit.IFC.Export.Toolkit.IFC4.IFCRampType>(floorElement, ifcEnumType, null);
                                else
                                    entityType = IFCValidateEntry.GetValidIFCType<IFCRampType>(floorElement, ifcEnumType, null);
                                break;
                            default:
                                bool isBaseSlab = false;
                                AnalyticalModel analyticalModel = floorElement.GetAnalyticalModel();
                                if (analyticalModel != null)
                                {
                                    AnalyzeAs slabFoundationType = analyticalModel.GetAnalyzeAs();
                                    isBaseSlab = (slabFoundationType == AnalyzeAs.SlabOnGrade) || (slabFoundationType == AnalyzeAs.Mat);
                                }
                                entityType = IFCValidateEntry.GetValidIFCType<IFCSlabType>(floorElement, ifcEnumType, isBaseSlab ? "BASESLAB" : "FLOOR");
                                break;
                        }
                        
                        for (int ii = 0; ii < numReps; ii++)
                        {
                            string ifcName = NamingUtil.GetNameOverride(floorElement, NamingUtil.GetIFCNamePlusIndex(floorElement, ii == 0 ? -1 : ii + 1));
                            string ifcDescription = NamingUtil.GetDescriptionOverride(floorElement, null);
                            string ifcObjectType = NamingUtil.GetObjectTypeOverride(floorElement, exporterIFC.GetFamilyName());
                            string ifcTag = NamingUtil.GetTagOverride(floorElement, NamingUtil.CreateIFCElementId(floorElement));

                            string currentGUID = (ii == 0) ? ifcGUID : GUIDUtil.CreateGUID();
                            IFCAnyHandle localPlacementHnd = exportedAsInternalExtrusion ? localPlacements[ii] : localPlacement;

                            IFCAnyHandle slabHnd = null;

                            // TODO: replace with CreateGenericBuildingElement.
                            switch (exportType)
                            {
                                case IFCExportType.IfcFooting:
                                    slabHnd = IFCInstanceExporter.CreateFooting(file, currentGUID, ownerHistory, ifcName,
                                        ifcDescription, ifcObjectType, localPlacementHnd, exportParts ? null : prodReps[ii],
                                        ifcTag, entityType);
                                    break;
                                case IFCExportType.IfcCovering:
                                    slabHnd = IFCInstanceExporter.CreateCovering(file, currentGUID, ownerHistory, ifcName,
                                        ifcDescription, ifcObjectType, localPlacementHnd, exportParts ? null : prodReps[ii],
                                        ifcTag, entityType); 
                                    break;
                                case IFCExportType.IfcRamp:
                                    slabHnd = IFCInstanceExporter.CreateRamp(file, currentGUID, ownerHistory, ifcName,
                                        ifcDescription, ifcObjectType, localPlacementHnd, exportParts ? null : prodReps[ii],
                                        ifcTag, entityType);
                                    break;
                                default:
                                    slabHnd = IFCInstanceExporter.CreateSlab(file, currentGUID, ownerHistory, ifcName,
                                        ifcDescription, ifcObjectType, localPlacementHnd, exportParts ? null : prodReps[ii],
                                        ifcTag, entityType);
                                    break;
                            }

                            if (IFCAnyHandleUtil.IsNullOrHasNoValue(slabHnd))
                                return;

                            if (exportParts)
                                PartExporter.ExportHostPart(exporterIFC, floorElement, slabHnd, productWrapper, placementSetter, localPlacementHnd, null);

                            slabHnds.Add(slabHnd);

                            if (!exportParts)
                            {
                                if (repTypes[ii] == ShapeRepresentationType.Brep)
                                    brepSlabHnds.Add(slabHnd);
                                else
                                    nonBrepSlabHnds.Add(slabHnd);
                            }
                        }

                        for (int ii = 0; ii < numReps; ii++)
                        {
                            IFCExtrusionCreationData loopExtraParam = ii < loopExtraParams.Count ? loopExtraParams[ii] : null;
                            productWrapper.AddElement(floorElement, slabHnds[ii], placementSetter, loopExtraParam, true);
                        }

                        if (exportedAsInternalExtrusion)
                            ExporterIFCUtils.ExportExtrudedSlabOpenings(exporterIFC, floorElement, placementSetter.LevelInfo,
                               localPlacements[0], slabHnds, extrusionLoops, floorPlane, productWrapper.ToNative());
                    }

                    if (!exportParts)
                    {
                        if (nonBrepSlabHnds.Count > 0)
                        {
                            HostObjectExporter.ExportHostObjectMaterials(exporterIFC, floorElement, nonBrepSlabHnds,
                                geometryElement, productWrapper, ElementId.InvalidElementId, Toolkit.IFCLayerSetDirection.Axis3, false);
                        }
                        if (brepSlabHnds.Count > 0)
                        {
                            HostObjectExporter.ExportHostObjectMaterials(exporterIFC, floorElement, brepSlabHnds,
                                geometryElement, productWrapper, ElementId.InvalidElementId, Toolkit.IFCLayerSetDirection.Axis3, true);
                        }
                    }
                }
                tr.Commit();

                return;
            }
    }

        /// <summary>
        /// Gets IFCSlabType from slab type name.
        /// </summary>
        /// <param name="ifcEnumType">The slab type name.</param>
        /// <returns>The IFCSlabType.</returns>
        public static IFCSlabType GetIFCSlabType(string ifcEnumType)
        {
            if (String.IsNullOrEmpty(ifcEnumType))
                return IFCSlabType.Floor;

            string ifcEnumTypeWithoutSpaces = NamingUtil.RemoveSpacesAndUnderscores(ifcEnumType);

            if (String.Compare(ifcEnumTypeWithoutSpaces, "USERDEFINED", true) == 0)
                return IFCSlabType.UserDefined;
            if (String.Compare(ifcEnumTypeWithoutSpaces, "FLOOR", true) == 0)
                return IFCSlabType.Floor;
            if (String.Compare(ifcEnumTypeWithoutSpaces, "ROOF", true) == 0)
                return IFCSlabType.Roof;
            if (String.Compare(ifcEnumTypeWithoutSpaces, "LANDING", true) == 0)
                return IFCSlabType.Landing;
            if (String.Compare(ifcEnumTypeWithoutSpaces, "BASESLAB", true) == 0)
                return IFCSlabType.BaseSlab;

            return IFCSlabType.Floor;
        }
    }
}
