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
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Export.Utility;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Exporter
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
        /// <param name="setter">
        /// The PlacementSetter.
        /// </param>
        /// <param name="productWrapper">
        /// The ProductWrapper.
        /// </param>
        public static void Export(ExporterIFC exporterIFC, Mullion mullion, GeometryElement geometryElement,
           IFCAnyHandle localPlacement, PlacementSetter setter, ProductWrapper productWrapper)
        {
            IFCFile file = exporterIFC.GetFile();

            using (PlacementSetter mullionSetter = PlacementSetter.Create(exporterIFC, mullion))
            {
                using (IFCExtrusionCreationData extraParams = new IFCExtrusionCreationData())
                {
                    IFCAnyHandle mullionPlacement = mullionSetter.LocalPlacement;
                    
                    Transform relTrf = ExporterIFCUtils.GetRelativeLocalPlacementOffsetTransform(localPlacement, mullionPlacement);
                    Transform inverseTrf = relTrf.Inverse;

                    IFCAnyHandle mullionLocalPlacement = ExporterUtil.CreateLocalPlacement(file, localPlacement,
                        inverseTrf.Origin, inverseTrf.BasisZ, inverseTrf.BasisX);

                    extraParams.SetLocalPlacement(mullionLocalPlacement);

                    Transform extrusionLCS = null;
                    // Add a custom direction for trying to create an extrusion based on the base curve of the mullion, if it is a line and not an arc.
                    Curve baseCurve = mullion.LocationCurve;
                    if ((baseCurve != null) && (baseCurve is Line))
                    {
                        // We won't use curveBounds and origin yet; just need the axis for now.
                        IFCRange curveBounds;
                        XYZ origin, mullionDirection;
                        GeometryUtil.GetAxisAndRangeFromCurve(baseCurve, out curveBounds, out mullionDirection, out origin);

                        // approx 1.0/sqrt(2.0)
                        XYZ planeY = (Math.Abs(mullionDirection.Z) < 0.707) ? XYZ.BasisZ.CrossProduct(mullionDirection) : XYZ.BasisX.CrossProduct(mullionDirection);
                        planeY.Normalize();

                        XYZ projDir = mullionDirection.CrossProduct(planeY);

                        extrusionLCS = Transform.Identity;
                        extrusionLCS.BasisX = mullionDirection; extrusionLCS.BasisY = planeY; extrusionLCS.BasisZ = projDir; extrusionLCS.Origin = origin;
                    }

                    ElementId catId = CategoryUtil.GetSafeCategoryId(mullion);

                    BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true);
                    bodyExporterOptions.ExtrusionLocalCoordinateSystem = extrusionLCS;

                    IFCAnyHandle repHnd = RepresentationUtil.CreateAppropriateProductDefinitionShape(exporterIFC, mullion, catId,
                        geometryElement, bodyExporterOptions, null, extraParams, true);
                    if (IFCAnyHandleUtil.IsNullOrHasNoValue(repHnd))
                    {
                        extraParams.ClearOpenings();
                        return;
                    }

                    string elemGUID = GUIDUtil.CreateGUID(mullion);
                    IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
                    string elemObjectType = NamingUtil.CreateIFCObjectName(exporterIFC, mullion);
                    string name = NamingUtil.GetNameOverride(mullion, elemObjectType);
                    string description = NamingUtil.GetDescriptionOverride(mullion, null);
                    string objectType = NamingUtil.GetObjectTypeOverride(mullion, elemObjectType);
                    string elemTag = NamingUtil.GetTagOverride(mullion, NamingUtil.CreateIFCElementId(mullion));

                    IFCAnyHandle mullionHnd = IFCInstanceExporter.CreateMember(file, elemGUID, ownerHistory, name, description, objectType,
                       mullionLocalPlacement, repHnd, elemTag, "MULLION");
                    ExporterCacheManager.HandleToElementCache.Register(mullionHnd, mullion.Id);

                    productWrapper.AddElement(mullion, mullionHnd, mullionSetter, extraParams, false);

                    ElementId matId = BodyExporter.GetBestMaterialIdFromGeometryOrParameter(geometryElement, exporterIFC, mullion);
                    CategoryUtil.CreateMaterialAssociation(exporterIFC, mullionHnd, matId);
                }
            }
        }
    }
}
