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

namespace BIM.IFC.Utility
{
    /// <summary>
    /// Provides static methods to create varies IFC representations.
    /// </summary>
    class RepresentationUtil
    {
        /// <summary>
        /// Creates a shape representation or appends existing ones to original representation.
        /// </summary>
        /// <remarks>
        /// This function has two modes. 
        /// If originalShapeRepresentation has no value, then this function will create a new ShapeRepresentation handle. 
        /// If originalShapeRepresentation has a value, then it is expected to be an aggregation of representations, and the new representation
        /// will be appended to the end of the list.
        /// </remarks>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="categoryId">
        /// The category id.
        /// </param>
        /// <param name="contextOfItems">
        /// The context for which the different subtypes of representation are valid. 
        /// </param>
        /// <param name="identifierOpt">
        /// The identifier for the representation.
        /// </param>
        /// <param name="representationTypeOpt">
        /// The type handle for the representation.
        /// </param>
        /// <param name="items">
        /// Collection of geometric representation items that are defined for this representation.
        /// </param>
        /// <param name="originalShapeRepresentation">
        /// The original shape representation.
        /// </param>
        /// <returns>
        /// The handle.
        /// </returns>
        public static IFCAnyHandle CreateOrAppendShapeRepresentation(ExporterIFC exporterIFC, ElementId categoryId, IFCAnyHandle contextOfItems,
           IFCLabel identifierOpt, IFCLabel representationTypeOpt, HashSet<IFCAnyHandle> items, IFCAnyHandle originalShapeRepresentation)
        {
            if (originalShapeRepresentation.HasValue)
            {
                IFCGeometryUtils.AddItemsToShape(originalShapeRepresentation, items);
                return originalShapeRepresentation;
            }

            return CreateShapeRepresentation(exporterIFC, categoryId, contextOfItems, identifierOpt, representationTypeOpt, items);
        }

        /// <summary>
        /// Creates a shape representation and register it to shape representation layer.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="categoryId">
        /// The category id.
        /// </param>
        /// <param name="contextOfItems">
        /// The context for which the different subtypes of representation are valid. 
        /// </param>
        /// <param name="identifierOpt">
        /// The identifier for the representation.
        /// </param>
        /// <param name="representationTypeOpt">
        /// The type handle for the representation.
        /// </param>
        /// <param name="items">
        /// Collection of geometric representation items that are defined for this representation.
        /// </param>
        /// <returns>
        /// The handle.
        /// </returns>
        public static IFCAnyHandle CreateShapeRepresentation(ExporterIFC exporterIFC, ElementId categoryId, IFCAnyHandle contextOfItems,
           IFCLabel identifierOpt, IFCLabel representationTypeOpt, HashSet<IFCAnyHandle> items)
        {
            IFCFile file = exporterIFC.GetFile();
            IFCAnyHandle newShapeRepresentation = file.CreateShapeRepresentation(contextOfItems, identifierOpt, representationTypeOpt, items);
            if (!newShapeRepresentation.HasValue)
                return newShapeRepresentation;

            // We are using the DWG export layer table to correctly map category to DWG layer for the 
            // IfcPresentationLayerAsssignment.
            exporterIFC.RegisterShapeForPresentationLayer(categoryId, newShapeRepresentation);
            return newShapeRepresentation;
        }

        /// <summary>
        /// Creates an IfcFacetedBrep handle.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="shell">
        /// The closed shell handle.
        /// </param>
        /// <returns>
        /// The handle.
        /// </returns>
        public static IFCAnyHandle CreateFacetedBRep(ExporterIFC exporterIFC, IFCAnyHandle shell)
        {
            IFCFile file = exporterIFC.GetFile();
            IFCAnyHandle bRep = file.CreateFacetedBrep(shell);

            IFCAnyHandle styledItem = file.CreateStyle(exporterIFC, bRep);
            return bRep;
        }

        /// <summary>
        /// Creates a sweep solid representation.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="categoryId">
        /// The category id.
        /// </param>
        /// <param name="contextOfItems">
        /// The context for which the different subtypes of representation are valid. 
        /// </param>
        /// <param name="bodyItems">
        /// Set of geometric representation items that are defined for this representation.
        /// </param>
        /// <param name="originalRepresentation">
        /// The original shape representation.
        /// </param>
        /// <returns>
        /// The handle.
        /// </returns>
        public static IFCAnyHandle CreateSweptSolidRep(ExporterIFC exporterIFC, ElementId categoryId, IFCAnyHandle contextOfItems, HashSet<IFCAnyHandle> bodyItems,
           IFCAnyHandle originalRepresentation)
        {
            IFCLabel identifierOpt = IFCLabel.Create("Body");	// this is by IFC2x2 convention, not temporary
            IFCLabel repTypeOpt = IFCLabel.Create("SweptSolid");  // this is by IFC2x2 convention, not temporary
            IFCAnyHandle bodyRepresentation =
               CreateOrAppendShapeRepresentation(exporterIFC, categoryId, contextOfItems, identifierOpt, repTypeOpt,
                  bodyItems, originalRepresentation);
            return bodyRepresentation;
        }

        /// <summary>
        /// Creates a clipping representation.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="categoryId">
        /// The category id.
        /// </param>
        /// <param name="contextOfItems">
        /// The context for which the different subtypes of representation are valid. 
        /// </param>
        /// <param name="bodyItems">
        /// Set of geometric representation items that are defined for this representation.
        /// </param>
        /// <returns>
        /// The handle.
        /// </returns>
        public static IFCAnyHandle CreateClippingRep(ExporterIFC exporterIFC, ElementId categoryId,
           IFCAnyHandle contextOfItems, HashSet<IFCAnyHandle> bodyItems)
        {
            IFCLabel identifierOpt = IFCLabel.Create("Body");	// this is by IFC2x2 convention, not temporary
            IFCLabel repTypeOpt = IFCLabel.Create("Clipping");  // this is by IFC2x2 convention, not temporary
            IFCAnyHandle bodyRepresentation = CreateShapeRepresentation(exporterIFC, categoryId,
               contextOfItems, identifierOpt, repTypeOpt, bodyItems);
            return bodyRepresentation;
        }

        /// <summary>
        /// Creates a Brep representation.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="categoryId">
        /// The category id.
        /// </param>
        /// <param name="contextOfItems">
        /// The context for which the different subtypes of representation are valid. 
        /// </param>
        /// <param name="bodyItems">
        /// Set of geometric representation items that are defined for this representation.
        /// </param>
        /// <returns>
        /// The handle.
        /// </returns>
        public static IFCAnyHandle CreateBRepRep(ExporterIFC exporterIFC, ElementId categoryId,
           IFCAnyHandle contextOfItems, HashSet<IFCAnyHandle> bodyItems)
        {
            IFCLabel identifierOpt = IFCLabel.Create("Body");	// this is by IFC2x2 convention, not temporary
            IFCLabel repTypeOpt = IFCLabel.Create("Brep");	// this is by IFC2x2 convention, not temporary
            IFCAnyHandle bodyRepresentation = CreateShapeRepresentation(exporterIFC, categoryId,
               contextOfItems, identifierOpt, repTypeOpt, bodyItems);
            return bodyRepresentation;
        }

        /// <summary>
        /// Creates a Brep representation.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="categoryId">
        /// The category id.
        /// </param>
        /// <param name="contextOfItems">
        /// The context for which the different subtypes of representation are valid. 
        /// </param>
        /// <param name="bodyItems">
        /// Set of geometric representation items that are defined for this representation.
        /// </param>
        /// <param name="exportAsFacetation">
        /// If this is true, the identifier for the representation is "Facetation" as required by IfcSite.
        /// If this is false, the identifier for the representation is "Body" as required by IfcBuildingElement.
        /// </param>
        /// <param name="originalShapeRepresentation">
        /// The original shape representation.
        /// </param>
        /// <returns>
        /// The handle.
        /// </returns>
        public static IFCAnyHandle CreateSurfaceRep(ExporterIFC exporterIFC, ElementId categoryId,
            IFCAnyHandle contextOfItems, HashSet<IFCAnyHandle> bodyItems, bool exportAsFacetation)
        {
            IFCLabel identifierOpt = null;
            if (exportAsFacetation)
                identifierOpt = IFCLabel.Create("Facetation");
            else
                identifierOpt = IFCLabel.Create("Body");	// this is by IFC2x3 convention, not temporary

            IFCLabel repTypeOpt = IFCLabel.Create("SurfaceModel");  // this is by IFC2x2 convention, not temporary
            IFCAnyHandle bodyRepresentation = CreateShapeRepresentation(exporterIFC, categoryId,
               contextOfItems, identifierOpt, repTypeOpt, bodyItems);
            return bodyRepresentation;
        }

        /// <summary>
        /// Creates a geometric set representation.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="categoryId">
        /// The category id.
        /// </param>
        /// <param name="type">
        /// The representation type.
        /// </param>
        /// <param name="contextOfItems">
        /// The context for which the different subtypes of representation are valid. 
        /// </param>
        /// <param name="bodyItems">
        /// Set of geometric representation items that are defined for this representation.
        /// </param>
        /// <returns>
        /// The handle.
        /// </returns>
        public static IFCAnyHandle CreateGeometricSetRep(ExporterIFC exporterIFC, ElementId categoryId,
           string type, IFCAnyHandle contextOfItems, HashSet<IFCAnyHandle> bodyItems)
        {
            IFCLabel identifierOpt = IFCLabel.Create(type);
            IFCLabel repTypeOpt = IFCLabel.Create("GeometricSet");	// this is by IFC2x2 convention, not temporary
            IFCAnyHandle bodyRepresentation = CreateShapeRepresentation(exporterIFC, categoryId,
               contextOfItems, identifierOpt, repTypeOpt, bodyItems);
            return bodyRepresentation;
        }

        /// <summary>
        /// Creates a body mapped item representation.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="categoryId">
        /// The category id.
        /// </param>
        /// <param name="contextOfItems">
        /// The context for which the different subtypes of representation are valid. 
        /// </param>
        /// <param name="bodyItems">
        /// Set of geometric representation items that are defined for this representation.
        /// </param>
        /// <returns>
        /// The handle.
        /// </returns>
        public static IFCAnyHandle CreateBodyMappedItemRep(ExporterIFC exporterIFC, ElementId categoryId,
           IFCAnyHandle contextOfItems, HashSet<IFCAnyHandle> bodyItems)
        {
            IFCLabel identifierOpt = IFCLabel.Create("Body");	// this is by IFC2x2+ convention
            IFCLabel repTypeOpt = IFCLabel.Create("MappedRepresentation");  // this is by IFC2x2+ convention
            IFCAnyHandle bodyRepresentation = CreateShapeRepresentation(exporterIFC, categoryId,
               contextOfItems, identifierOpt, repTypeOpt, bodyItems);
            return bodyRepresentation;
        }

        /// <summary>
        /// Creates a plan mapped item representation.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="categoryId">
        /// The category id.
        /// </param>
        /// <param name="contextOfItems">
        /// The context for which the different subtypes of representation are valid. 
        /// </param>
        /// <param name="bodyItems">
        /// Set of geometric representation items that are defined for this representation.
        /// </param>
        /// <returns>
        /// The handle.
        /// </returns>
        public static IFCAnyHandle CreatePlanMappedItemRep(ExporterIFC exporterIFC, ElementId categoryId,
           IFCAnyHandle contextOfItems, HashSet<IFCAnyHandle> bodyItems)
        {
            IFCLabel identifierOpt = IFCLabel.Create("Annotation");	// this is by IFC2x2+ convention
            IFCLabel repTypeOpt = IFCLabel.Create("MappedRepresentation");  // this is by IFC2x2+ convention
            IFCAnyHandle bodyRepresentation = CreateShapeRepresentation(exporterIFC, categoryId,
               contextOfItems, identifierOpt, repTypeOpt, bodyItems);
            return bodyRepresentation;
        }
    }
}
