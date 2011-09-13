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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
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
        /// The IFCProductWrapper.
        /// </param>
        public static void Export(ExporterIFC exporterIFC, Mullion mullion, GeometryElement geometryElement,
           IFCAnyHandle localPlacement, IFCExtrusionCreationData extraParams, IFCPlacementSetter setter, IFCProductWrapper productWrapper)
        {
            IFCFile file = exporterIFC.GetFile();

            ElementId catId = CategoryUtil.GetSafeCategoryId(mullion);


            IFCSolidMeshGeometryInfo solidMeshInfo = ExporterIFCUtils.GetSolidMeshGeometry(exporterIFC, geometryElement, Transform.Identity);
            IList<Solid> solids = solidMeshInfo.GetSolids();
            IList<Mesh> polyMeshes = solidMeshInfo.GetMeshes();

            bool tryToExportAsExtrusion = true;
            if (solids.Count != 1 || polyMeshes.Count != 0)
                tryToExportAsExtrusion = false;

            IFCAnyHandle shapeRep = BodyExporter.ExportBody(mullion.Document.Application, exporterIFC, catId, solids, polyMeshes, tryToExportAsExtrusion, extraParams);
            IList<IFCAnyHandle> shapeReps = new List<IFCAnyHandle>();
            shapeReps.Add(shapeRep);
            IFCAnyHandle repHnd = file.CreateProductDefinitionShape(IFCLabel.Create(), IFCLabel.Create(), shapeReps);

            IFCLabel elemGUID = IFCLabel.CreateGUID(mullion);
            IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();
            IFCLabel elemObjectType = NamingUtil.CreateIFCObjectName(exporterIFC, mullion);
            IFCLabel elemId = NamingUtil.CreateIFCElementId(mullion);
            //IFCLabel elemType = IFCLabel.Create("MULLION");

            IFCAnyHandle mullionHnd = file.CreateMember(elemGUID, ownerHistory, elemObjectType, IFCLabel.Create(), elemObjectType,
               localPlacement, repHnd, elemId);
            productWrapper.AddElement(mullionHnd, setter, extraParams, true);
        }
    }
}
