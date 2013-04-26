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
using Revit.IFC.Export.Exporter;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Utility
{
    /// <summary>
    /// Provides static methods to create varies IFC representations.
    /// </summary>
    class RepresentationUtil
    {
        /// <summary>
        /// Creates a shape representation and register it to shape representation layer.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
        /// <param name="identifier">The identifier for the representation.</param>
        /// <param name="representationType">The type handle for the representation.</param>
        /// <param name="items">Collection of geometric representation items that are defined for this representation.</param>
        /// <returns>The handle.</returns>
        public static IFCAnyHandle CreateBaseShapeRepresentation(ExporterIFC exporterIFC, IFCAnyHandle contextOfItems,
           string identifier, string representationType, ISet<IFCAnyHandle> items)
        {
            IFCFile file = exporterIFC.GetFile();
            IFCAnyHandle newShapeRepresentation = IFCInstanceExporter.CreateShapeRepresentation(file, contextOfItems, identifier, representationType, items);
            return newShapeRepresentation;
        }

        /// <summary>
        /// Creates a shape representation or appends existing ones to original representation.
        /// </summary>
        /// <remarks>
        /// This function has two modes. 
        /// If originalShapeRepresentation has no value, then this function will create a new ShapeRepresentation handle. 
        /// If originalShapeRepresentation has a value, then it is expected to be an aggregation of representations, and the new representation
        /// will be appended to the end of the list.
        /// </remarks>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="categoryId">The category id.</param>
        /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
        /// <param name="identifierOpt">The identifier for the representation.</param>
        /// <param name="representationTypeOpt">The type handle for the representation.</param>
        /// <param name="items">Collection of geometric representation items that are defined for this representation.</param>
        /// <param name="originalShapeRepresentation">The original shape representation.</param>
        /// <returns>The handle.</returns>
        public static IFCAnyHandle CreateOrAppendShapeRepresentation(ExporterIFC exporterIFC, Element element, ElementId categoryId, IFCAnyHandle contextOfItems,
           string identifierOpt, string representationTypeOpt, ISet<IFCAnyHandle> items, IFCAnyHandle originalShapeRepresentation)
        {
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(originalShapeRepresentation))
            {
                GeometryUtil.AddItemsToShape(originalShapeRepresentation, items);
                return originalShapeRepresentation;
            }

            return CreateShapeRepresentation(exporterIFC, element, categoryId, contextOfItems, identifierOpt, representationTypeOpt, items);
        }

        /// <summary>
        /// Creates a shape representation and register it to shape representation layer.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="categoryId">The category id.</param>
        /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
        /// <param name="identifier">The identifier for the representation.</param>
        /// <param name="representationType">The type handle for the representation.</param>
        /// <param name="items">Collection of geometric representation items that are defined for this representation.</param>
        /// <returns>The handle.</returns>
        public static IFCAnyHandle CreateShapeRepresentation(ExporterIFC exporterIFC, Element element, ElementId categoryId, IFCAnyHandle contextOfItems,
           string identifier, string representationType, ISet<IFCAnyHandle> items)
        {
            IFCAnyHandle newShapeRepresentation = CreateBaseShapeRepresentation(exporterIFC, contextOfItems, identifier, representationType, items);
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(newShapeRepresentation))
                return newShapeRepresentation;

            string ifcCADLayer = null;
            if (!ParameterUtil.GetStringValueFromElementOrSymbol(element, "IFCCadLayer", out ifcCADLayer) || string.IsNullOrWhiteSpace(ifcCADLayer))
                ifcCADLayer = ExporterStateManager.GetCurrentCADLayerOverride();

            // We are using the DWG export layer table to correctly map category to DWG layer for the 
            // IfcPresentationLayerAsssignment, if it is not overridden.
            if (!string.IsNullOrWhiteSpace(ifcCADLayer))
                ExporterCacheManager.PresentationLayerSetCache.AddRepresentationToLayer(ifcCADLayer, newShapeRepresentation);
            else
                exporterIFC.RegisterShapeForPresentationLayer(element, categoryId, newShapeRepresentation);
            
            return newShapeRepresentation;
        }

        /// <summary>
        /// Creates a shape representation and register it to shape representation layer.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
        /// <param name="identifier">The identifier for the representation.</param>
        /// <param name="representationType">The type handle for the representation.</param>
        /// <param name="items">Collection of geometric representation items that are defined for this representation.</param>
        /// <param name="ifcCADLayer">The IFC CAD layer name.</param>
        /// <returns>The handle.</returns>
        public static IFCAnyHandle CreateShapeRepresentation(ExporterIFC exporterIFC, IFCAnyHandle contextOfItems,
           string identifier, string representationType, ISet<IFCAnyHandle> items, string ifcCADLayer)
        {
            if (string.IsNullOrWhiteSpace(ifcCADLayer))
                throw new ArgumentNullException("ifcCADLayer");

            IFCAnyHandle newShapeRepresentation = CreateBaseShapeRepresentation(exporterIFC, contextOfItems, identifier, representationType, items);
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(newShapeRepresentation))
                return newShapeRepresentation;

            ExporterCacheManager.PresentationLayerSetCache.AddRepresentationToLayer(ifcCADLayer, newShapeRepresentation);
            return newShapeRepresentation;
        }
        
        /// <summary>
        /// Creates a shape representation and register it to shape representation layer.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="categoryId">The category id.</param>
        /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
        /// <param name="identifierOpt">The identifier for the representation.</param>
        /// <param name="representationTypeOpt">The type handle for the representation.</param>
        /// <param name="items">List of geometric representation items that are defined for this representation.</param>
        /// <returns>The handle.</returns>
        public static IFCAnyHandle CreateShapeRepresentation(ExporterIFC exporterIFC, Element element, ElementId categoryId, IFCAnyHandle contextOfItems,
           string identifierOpt, string representationTypeOpt, IList<IFCAnyHandle> items)
        {
            HashSet<IFCAnyHandle> itemSet = new HashSet<IFCAnyHandle>();
            foreach (IFCAnyHandle axisItem in items)
                itemSet.Add(axisItem);
            return CreateShapeRepresentation(exporterIFC, element, categoryId, contextOfItems, identifierOpt, representationTypeOpt, itemSet);
        }

        /// <summary>
        /// Creates an IfcFacetedBrep handle.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="shell">The closed shell handle.</param>
        /// <returns>The handle.</returns>
        public static IFCAnyHandle CreateFacetedBRep(ExporterIFC exporterIFC, Document document, IFCAnyHandle shell, ElementId overrideMaterialId)
        {
            IFCFile file = exporterIFC.GetFile();
            IFCAnyHandle brep = IFCInstanceExporter.CreateFacetedBrep(file, shell);

            // The option can be changed by alternate UI.
            if (ExporterCacheManager.ExportOptionsCache.ExportSurfaceStyles)
            {
                BodyExporter.CreateSurfaceStyleForRepItem(exporterIFC, document, brep, overrideMaterialId);
            }
            return brep;
        }

        /// <summary>
        /// Creates a sweep solid representation.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="categoryId">The category id.</param>
        /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
        /// <param name="bodyItems">Set of geometric representation items that are defined for this representation.</param>
        /// <param name="originalShapeRepresentation">The original shape representation.</param>
        /// <returns>The handle.</returns>
        public static IFCAnyHandle CreateSweptSolidRep(ExporterIFC exporterIFC, Element element, ElementId categoryId, IFCAnyHandle contextOfItems, 
            ISet<IFCAnyHandle> bodyItems, IFCAnyHandle originalRepresentation)
        {
            string identifierOpt = "Body";	// this is by IFC2x2 convention, not temporary
            string repTypeOpt = "SweptSolid";  // this is by IFC2x2 convention, not temporary
            IFCAnyHandle bodyRepresentation =
               CreateOrAppendShapeRepresentation(exporterIFC, element, categoryId, contextOfItems, identifierOpt, repTypeOpt,
                  bodyItems, originalRepresentation);
            return bodyRepresentation;
        }

        /// <summary>
        /// Creates an advanced sweep solid representation.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="categoryId">The category id.</param>
        /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
        /// <param name="bodyItems">Set of geometric representation items that are defined for this representation.</param>
        /// <param name="originalShapeRepresentation">The original shape representation.</param>
        /// <returns>The handle.</returns>
        public static IFCAnyHandle CreateAdvancedSweptSolidRep(ExporterIFC exporterIFC, Element element, ElementId categoryId, IFCAnyHandle contextOfItems,
            ISet<IFCAnyHandle> bodyItems, IFCAnyHandle originalRepresentation)
        {
            string identifierOpt = "Body";	// this is by IFC2x2 convention, not temporary
            string repTypeOpt = "AdvancedSweptSolid";  // this is by IFC2x2 convention, not temporary
            IFCAnyHandle bodyRepresentation =
               CreateOrAppendShapeRepresentation(exporterIFC, element, categoryId, contextOfItems, identifierOpt, repTypeOpt,
                  bodyItems, originalRepresentation);
            return bodyRepresentation;
        }

        /// <summary>
        /// Creates a clipping representation.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="categoryId">The category id.</param>
        /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
        /// <param name="bodyItems">Set of geometric representation items that are defined for this representation.</param>
        /// <returns>The handle.</returns>
        public static IFCAnyHandle CreateClippingRep(ExporterIFC exporterIFC, Element element, ElementId categoryId,
           IFCAnyHandle contextOfItems, HashSet<IFCAnyHandle> bodyItems)
        {
            string identifierOpt = "Body";	// this is by IFC2x2 convention, not temporary
            string repTypeOpt = "Clipping";  // this is by IFC2x2 convention, not temporary
            IFCAnyHandle bodyRepresentation = CreateShapeRepresentation(exporterIFC, element, categoryId,
               contextOfItems, identifierOpt, repTypeOpt, bodyItems);
            return bodyRepresentation;
        }

        /// <summary>
        /// Creates a CSG representation which contains the result of boolean operations between solid models, half spaces, and other boolean operations.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="categoryId">The category id.</param>
        /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
        /// <param name="bodyItems">Set of geometric representation items that are defined for this representation.</param>
        /// <returns>The handle.</returns>
        public static IFCAnyHandle CreateCSGRep(ExporterIFC exporterIFC, Element element, ElementId categoryId,
           IFCAnyHandle contextOfItems, ISet<IFCAnyHandle> bodyItems)
        {
            string identifierOpt = "Body";	// this is by IFC2x2 convention, not temporary
            string repTypeOpt = "CSG";  // this is by IFC2x2 convention, not temporary
            IFCAnyHandle bodyRepresentation = CreateShapeRepresentation(exporterIFC, element, categoryId,
               contextOfItems, identifierOpt, repTypeOpt, bodyItems);
            return bodyRepresentation;
        }

        /// <summary>
        /// Creates a Brep representation.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="categoryId">The category id.</param>
        /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
        /// <param name="bodyItems">Set of geometric representation items that are defined for this representation.</param>
        /// <returns>The handle.</returns>
        public static IFCAnyHandle CreateBRepRep(ExporterIFC exporterIFC, Element element, ElementId categoryId,
           IFCAnyHandle contextOfItems, ISet<IFCAnyHandle> bodyItems)
        {
            string identifierOpt = "Body";	// this is by IFC2x2 convention, not temporary
            string repTypeOpt = "Brep";	// this is by IFC2x2 convention, not temporary
            IFCAnyHandle bodyRepresentation = CreateShapeRepresentation(exporterIFC, element, categoryId,
               contextOfItems, identifierOpt, repTypeOpt, bodyItems);
            return bodyRepresentation;
        }

        /// <summary>
        /// Creates a Solid model representation.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="categoryId">The category id.</param>
        /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
        /// <param name="bodyItems">Set of geometric representation items that are defined for this representation.</param>
        /// <returns>The handle.</returns>
        public static IFCAnyHandle CreateSolidModelRep(ExporterIFC exporterIFC, Element element, ElementId categoryId,
           IFCAnyHandle contextOfItems, ISet<IFCAnyHandle> bodyItems)
        {
            string identifierOpt = "Body";
            string repTypeOpt = "SolidModel";
            IFCAnyHandle bodyRepresentation = CreateShapeRepresentation(exporterIFC, element, categoryId,
               contextOfItems, identifierOpt, repTypeOpt, bodyItems);
            return bodyRepresentation;
        }

        /// <summary>
        /// Creates a Brep representation.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="categoryId">The category id.</param>
        /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
        /// <param name="bodyItems">Set of geometric representation items that are defined for this representation.</param>
        /// <param name="exportAsFacetationOrMesh">
        /// If this is true, the identifier for the representation is "Facetation" as required by IfcSite for IFC2x2, IFC2x3, or "Mesh" for GSA.
        /// If this is false, the identifier for the representation is "Body" as required by IfcBuildingElement, or IfcSite for IFC2x3 v2.
        /// </param>
        /// <param name="originalShapeRepresentation">The original shape representation.</param>
        /// <returns>The handle.</returns>
        public static IFCAnyHandle CreateSurfaceRep(ExporterIFC exporterIFC, Element element, ElementId categoryId,
            IFCAnyHandle contextOfItems, ISet<IFCAnyHandle> bodyItems, bool exportAsFacetationOrMesh, IFCAnyHandle originalRepresentation)
        {
            string identifierOpt = null;
            if (exportAsFacetationOrMesh)
            {
                if (ExporterCacheManager.ExportOptionsCache.FileVersion == IFCVersion.IFCCOBIE)
                    identifierOpt = "Mesh"; // IFC GSA convention
                else
                    identifierOpt = "Facetation"; // IFC2x2+ convention
            }
            else
                identifierOpt = "Body";	// IFC2x2+ convention for IfcBuildingElement, IFC2x3 v2 convention for IfcSite

            string repTypeOpt = "SurfaceModel";  // IFC2x2+ convention
            IFCAnyHandle bodyRepresentation = CreateOrAppendShapeRepresentation(exporterIFC, element, categoryId,
               contextOfItems, identifierOpt, repTypeOpt, bodyItems, originalRepresentation);
            return bodyRepresentation;
        }

        /// <summary>
        /// Creates a boundary representation.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="categoryId">The category id.</param>
        /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
        /// <param name="bodyItems">Collection of geometric representation items that are defined for this representation.</param>
        /// <param name="originalShapeRepresentation">The original shape representation.</param>
        /// <returns>The handle.</returns>
        public static IFCAnyHandle CreateBoundaryRep(ExporterIFC exporterIFC, Element element, ElementId categoryId,
            IFCAnyHandle contextOfItems, ISet<IFCAnyHandle> bodyItems, IFCAnyHandle originalRepresentation)
        {
            string identifierOpt = "FootPrint";	// this is by IFC2x3 convention, not temporary

            string repTypeOpt = "Curve2D";  // this is by IFC2x2 convention, not temporary
            IFCAnyHandle bodyRepresentation = CreateOrAppendShapeRepresentation(exporterIFC, element, categoryId,
               contextOfItems, identifierOpt, repTypeOpt, bodyItems, originalRepresentation);
            return bodyRepresentation;
        }

        /// <summary>
        /// Creates a geometric set representation.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="categoryId">The category id.</param>
        /// <param name="type">The representation type.</param>
        /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
        /// <param name="bodyItems">Set of geometric representation items that are defined for this representation.</param>
        /// <returns>The handle.</returns>
        public static IFCAnyHandle CreateGeometricSetRep(ExporterIFC exporterIFC, Element element, ElementId categoryId,
           string type, IFCAnyHandle contextOfItems, HashSet<IFCAnyHandle> bodyItems)
        {
            string identifierOpt = type;
            string repTypeOpt = "GeometricSet";	// this is by IFC2x2 convention, not temporary
            IFCAnyHandle bodyRepresentation = CreateShapeRepresentation(exporterIFC, element, categoryId,
               contextOfItems, identifierOpt, repTypeOpt, bodyItems);
            return bodyRepresentation;
        }

        /// <summary>
        /// Creates a body bounding box representation.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
        /// <param name="boundingBoxItem">Set of geometric representation items that are defined for this representation.</param>
        /// <returns>The handle.</returns>
        public static IFCAnyHandle CreateBoundingBoxRep(ExporterIFC exporterIFC, IFCAnyHandle contextOfItems, IFCAnyHandle boundingBoxItem)
        {
            string identifierOpt = "Box";	// this is by IFC2x2+ convention
            string repTypeOpt = "BoundingBox";  // this is by IFC2x2+ convention
            ISet<IFCAnyHandle> bodyItems = new HashSet<IFCAnyHandle>();
            bodyItems.Add(boundingBoxItem);
            IFCAnyHandle bodyRepresentation = CreateBaseShapeRepresentation(exporterIFC, contextOfItems, identifierOpt, repTypeOpt, bodyItems);
            return bodyRepresentation;
        }

        /// <summary>
        /// Creates a body mapped item representation.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="element">The element.</param>
        /// <param name="categoryId">The category id.</param>
        /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
        /// <param name="bodyItems">Set of geometric representation items that are defined for this representation.</param>
        /// <returns>The handle.</returns>
        public static IFCAnyHandle CreateBodyMappedItemRep(ExporterIFC exporterIFC, Element element, ElementId categoryId,
           IFCAnyHandle contextOfItems, ISet<IFCAnyHandle> bodyItems)
        {
            string identifierOpt = "Body";	// this is by IFC2x2+ convention
            string repTypeOpt = "MappedRepresentation";  // this is by IFC2x2+ convention
            IFCAnyHandle bodyRepresentation = CreateShapeRepresentation(exporterIFC, element, categoryId,
               contextOfItems, identifierOpt, repTypeOpt, bodyItems);
            return bodyRepresentation;
        }

        /// <summary>
        /// Creates a plan mapped item representation.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="categoryId">The category id.</param>
        /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
        /// <param name="bodyItems">Set of geometric representation items that are defined for this representation.</param>
        /// <returns>The handle.</returns>
        public static IFCAnyHandle CreatePlanMappedItemRep(ExporterIFC exporterIFC, Element element, ElementId categoryId,
            IFCAnyHandle contextOfItems, HashSet<IFCAnyHandle> bodyItems)
        {
            string identifierOpt = "Annotation";	// this is by IFC2x2+ convention
            string repTypeOpt = "MappedRepresentation";  // this is by IFC2x2+ convention
            IFCAnyHandle bodyRepresentation = CreateShapeRepresentation(exporterIFC, element, categoryId,
                contextOfItems, identifierOpt, repTypeOpt, bodyItems);
            return bodyRepresentation;
        }

        /// <summary>
        /// Creates an annotation representation.
        /// </summary>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="element">The element.</param>
        /// <param name="categoryId">The category id.</param>
        /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
        /// <param name="bodyItems">Set of geometric representation items that are defined for this representation.</param>
        /// <returns>The handle.</returns>
        public static IFCAnyHandle CreateAnnotationSetRep(ExporterIFC exporterIFC, Element element, ElementId categoryId,
            IFCAnyHandle contextOfItems, HashSet<IFCAnyHandle> bodyItems)
        {
            string identifierOpt = "Annotation";
            string repTypeOpt = "Annotation2D";	// this is by IFC2x3 convention

            IFCAnyHandle bodyRepresentation = CreateShapeRepresentation(exporterIFC, element, categoryId,
                contextOfItems, identifierOpt, repTypeOpt, bodyItems);
            return bodyRepresentation;
        }

        /// <summary>
        /// Creates a SweptSolid, Brep, or Surface product definition shape representation, depending on the geoemtry and export version.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="categoryId">The category id.</param>
        /// <param name="geometryElement">The geometry element.</param>
        /// <param name="bodyExporterOptions">The body exporter options.</param>
        /// <param name="extraReps">Extra representations (e.g. Axis, Boundary).  May be null.</param>
        /// <param name="extrusionCreationData">The extrusion creation data.</param>
        /// <returns>The handle.</returns>
        public static IFCAnyHandle CreateAppropriateProductDefinitionShape(ExporterIFC exporterIFC, Element element, ElementId categoryId,
            GeometryElement geometryElement, BodyExporterOptions bodyExporterOptions, IList<IFCAnyHandle> extraReps, 
            IFCExtrusionCreationData extrusionCreationData)
        {
            BodyData bodyData;
            BodyExporterOptions newBodyExporterOptions = new BodyExporterOptions(bodyExporterOptions);
            newBodyExporterOptions.AllowOffsetTransform = false;

            return CreateAppropriateProductDefinitionShape(exporterIFC, element, categoryId,
                geometryElement, newBodyExporterOptions, extraReps, extrusionCreationData, out bodyData);
        }

        /// <summary>
        /// Creates a SweptSolid, Brep, or SurfaceModel product definition shape representation, based on the geometry and IFC version.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="categoryId">The category id.</param>
        /// <param name="geometryElement">The geometry element.</param>
        /// <param name="bodyExporterOptions">The body exporter options.</param>
        /// <param name="extraReps">Extra representations (e.g. Axis, Boundary).  May be null.</param>
        /// <param name="extrusionCreationData">The extrusion creation data.</param>
        /// <param name="bodyData">The body data.</param>
        /// <returns>The handle.</returns>
        public static IFCAnyHandle CreateAppropriateProductDefinitionShape(ExporterIFC exporterIFC, Element element, ElementId categoryId,
            GeometryElement geometryElement, BodyExporterOptions bodyExporterOptions, IList<IFCAnyHandle> extraReps, 
            IFCExtrusionCreationData extrusionCreationData, out BodyData bodyData)
        {
            bodyData = null;
            SolidMeshGeometryInfo info = null;
            IList<GeometryObject> geometryList = new List<GeometryObject>();

            if (!ExporterCacheManager.ExportOptionsCache.ExportAs2x2)
            {
                info = GeometryUtil.GetSplitSolidMeshGeometry(geometryElement, Transform.Identity);
                IList<Mesh> meshes = info.GetMeshes();
                if (meshes.Count == 0)
                {
                    IList<Solid> solidList = info.GetSolids();
                    foreach (Solid solid in solidList)
                    {
                        geometryList.Add(solid);
                    }
                }
            }

            if (geometryList.Count == 0)
                geometryList.Add(geometryElement);
            else
                bodyExporterOptions.TryToExportAsExtrusion = true;
           
            bodyData = BodyExporter.ExportBody(exporterIFC, element, categoryId, ElementId.InvalidElementId, geometryList,
                bodyExporterOptions, extrusionCreationData);
            IFCAnyHandle bodyRep = bodyData.RepresentationHnd;
            List<IFCAnyHandle> bodyReps = new List<IFCAnyHandle>();
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(bodyRep))
            {
                if (extrusionCreationData != null)
                    extrusionCreationData.ClearOpenings();
            }
            else
                bodyReps.Add(bodyRep);

            if (extraReps != null)
            {
                foreach (IFCAnyHandle hnd in extraReps)
                    bodyReps.Add(hnd);
            }

            IFCAnyHandle boundingBoxRep = BoundingBoxExporter.ExportBoundingBox(exporterIFC, geometryElement, Transform.Identity);
            if (boundingBoxRep != null)
                bodyReps.Add(boundingBoxRep);

            if (bodyReps.Count == 0)
                return null;
            return IFCInstanceExporter.CreateProductDefinitionShape(exporterIFC.GetFile(), null, null, bodyReps);
        }

        /// <summary>
        /// Creates a surface product definition shape representation.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="element">The element.</param>
        /// <param name="geometryElement">The geometry element.</param>
        /// <param name="exportBoundaryRep">If this is true, it will export boundary representations.</param>
        /// <param name="exportAsFacetation">If this is true, it will export the geometry as facetation.</param>
        /// <returns>The handle.</returns>
        public static IFCAnyHandle CreateSurfaceProductDefinitionShape(ExporterIFC exporterIFC, Element element,
           GeometryElement geometryElement, bool exportBoundaryRep, bool exportAsFacetation)
        {
            IFCAnyHandle bodyRep = null;
            IFCAnyHandle boundaryRep = null;
            return CreateSurfaceProductDefinitionShape(exporterIFC, element, geometryElement, exportBoundaryRep, exportAsFacetation, ref bodyRep, ref boundaryRep);
        }

        /// <summary>
        /// Creates a surface product definition shape representation.
        /// </summary>
        /// <remarks>
        /// If a body representation is supplied, then we expect that this is already contained in a representation list, inside
        /// a product representation. As such, just modify the list and return.
        /// </remarks>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="element">The element.</param>
        /// <param name="geometryElement">The geometry element.</param>
        /// <param name="exportBoundaryRep">If this is true, it will export boundary representations.</param>
        /// <param name="exportAsFacetation">If this is true, it will export the geometry as facetation.</param>
        /// <param name="bodyRep">Body representation.</param>
        /// <param name="boundaryRep">Boundary representation.</param>
        /// <returns>The handle.</returns>
        public static IFCAnyHandle CreateSurfaceProductDefinitionShape(ExporterIFC exporterIFC, Element element,
           GeometryElement geometryElement, bool exportBoundaryRep, bool exportAsFacetation, ref IFCAnyHandle bodyRep, ref IFCAnyHandle boundaryRep)
        {
            bool hasOriginalBodyRepresentation = bodyRep != null;
            bool success = SurfaceExporter.ExportSurface(exporterIFC, element, geometryElement, exportBoundaryRep, exportAsFacetation, ref bodyRep, ref boundaryRep);

            if (!success)
                return null;

            // If we supplied a body representation, then we expect that this is already contained in a representation list, inside
            // a product representation.  As such, just modify the list and return.
            if (hasOriginalBodyRepresentation)
                return null;

            List<IFCAnyHandle> representations = new List<IFCAnyHandle>();
            representations.Add(bodyRep);
            if (exportBoundaryRep && !IFCAnyHandleUtil.IsNullOrHasNoValue(boundaryRep))
                representations.Add(boundaryRep);

            IFCAnyHandle boundingBoxRep = BoundingBoxExporter.ExportBoundingBox(exporterIFC, geometryElement, Transform.Identity);
            if (boundingBoxRep != null)
                representations.Add(boundingBoxRep);

            return IFCInstanceExporter.CreateProductDefinitionShape(exporterIFC.GetFile(), null, null, representations);
        }

        /// <summary>
        /// Creates a extruded product definition shape representation.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="element">The base element.</param>
        /// <param name="categoryId">The category of the element.</param>
        /// <param name="curveLoops">The curve loops defining the extruded surface.</param>
        /// <param name="plane">The extrusion base plane.</param>
        /// <param name="extrDirVec">The extrusion direction.</param>
        /// <param name="extrusionSize">The scaled extrusion length.</param>
        /// <returns>The handle.</returns>
        public static IFCAnyHandle CreateExtrudedProductDefShape(ExporterIFC exporterIFC, Element element, ElementId categoryId,
            IList<CurveLoop> curveLoops, Plane plane, XYZ extrDirVec, double extrusionSize)
        {
            IFCFile file = exporterIFC.GetFile();

            IFCAnyHandle extrusionHnd = ExtrusionExporter.CreateExtrudedSolidFromCurveLoop(exporterIFC, null, curveLoops, plane,
                extrDirVec, extrusionSize);

            if (IFCAnyHandleUtil.IsNullOrHasNoValue(extrusionHnd))
                return null;

            ISet<IFCAnyHandle> bodyItems = new HashSet<IFCAnyHandle>();
            bodyItems.Add(extrusionHnd);

            IFCAnyHandle contextOfItems = exporterIFC.Get3DContextHandle("Body"); 
            IFCAnyHandle shapeRepHnd = CreateSweptSolidRep(exporterIFC, element, categoryId, contextOfItems, bodyItems, null);

            IList<IFCAnyHandle> shapeReps = new List<IFCAnyHandle>();
            shapeReps.Add(shapeRepHnd);
            return IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, shapeReps);
        }
    }
}
