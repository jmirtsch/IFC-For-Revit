﻿//
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
using BIM.IFC.Exporter.PropertySet;
using BIM.IFC.Toolkit;
using BIM.IFC.Utility;

namespace BIM.IFC.Exporter
{
    /// <summary>
    /// Provides methods to export mullions.
    /// </summary>
    class MullionExporter
    {
        /// <summary>
        /// Exports mullion.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="mullion">
        /// The mullion object.
        /// </param>
        /// <param name="geometryElement">
        /// The geometry element.
        /// </param>
        /// <param name="localPlacement">
        /// The local placement handle.
        /// </param>
        /// <param name="extraParams">
        /// The extrusion creation data.
        /// </param>
        /// <param name="setter">
        /// The IFCPlacementSetter.
        /// </param>
        /// <param name="productWrapper">
        /// The ProductWrapper.
        /// </param>
        public static void Export(ExporterIFC exporterIFC, Mullion mullion, GeometryElement geometryElement,
           IFCAnyHandle localPlacement, IFCExtrusionCreationData extraParams, IFCPlacementSetter setter, ProductWrapper productWrapper)
        {
            IFCFile file = exporterIFC.GetFile();

            using (IFCPlacementSetter mullionSetter = IFCPlacementSetter.Create(exporterIFC, mullion))
            {
                IFCAnyHandle mullionPlacement = mullionSetter.GetPlacement();

                Transform relTrf = ExporterIFCUtils.GetRelativeLocalPlacementOffsetTransform(localPlacement, mullionPlacement);
                Transform inverseTrf = relTrf.Inverse;

                IFCAnyHandle mullionRelativePlacement = ExporterUtil.CreateAxis2Placement3D(file, inverseTrf.Origin, inverseTrf.BasisZ, inverseTrf.BasisX);
                IFCAnyHandle mullionLocalPlacement = IFCInstanceExporter.CreateLocalPlacement(file, localPlacement, mullionRelativePlacement);

                extraParams.SetLocalPlacement(mullionLocalPlacement);

                ElementId catId = CategoryUtil.GetSafeCategoryId(mullion);

                BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true);
                IFCAnyHandle repHnd = RepresentationUtil.CreateBRepProductDefinitionShape(mullion.Document.Application, exporterIFC, mullion, catId,
                    geometryElement, bodyExporterOptions, null, extraParams);
                if (IFCAnyHandleUtil.IsNullOrHasNoValue(repHnd))
                {
                    extraParams.ClearOpenings();
                    return;
                }

                string elemGUID = GUIDUtil.CreateGUID(mullion);
                IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();
                string elemObjectType = NamingUtil.CreateIFCObjectName(exporterIFC, mullion);
                string name = NamingUtil.GetNameOverride(mullion, elemObjectType);
                string description = NamingUtil.GetDescriptionOverride(mullion, null);
                string objectType = NamingUtil.GetObjectTypeOverride(mullion, elemObjectType);
                string elemTag = NamingUtil.GetTagOverride(mullion, NamingUtil.CreateIFCElementId(mullion));

                IFCAnyHandle mullionHnd = IFCInstanceExporter.CreateMember(file, elemGUID, ownerHistory, name, description, objectType,
                   mullionLocalPlacement, repHnd, elemTag);
                productWrapper.AddElement(mullionHnd, mullionSetter, extraParams, false);

                ElementId matId = BodyExporter.GetBestMaterialIdFromGeometryOrParameter(geometryElement, exporterIFC, mullion);
                CategoryUtil.CreateMaterialAssociation(mullion.Document, exporterIFC, mullionHnd, matId);
                PropertyUtil.CreateInternalRevitPropertySets(exporterIFC, mullion, productWrapper);
            }
        }
    }
}
