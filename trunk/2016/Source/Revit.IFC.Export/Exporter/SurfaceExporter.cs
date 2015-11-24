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
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Exporter
{
    /// <summary>
    /// Provides methods to export geometry to surface representation.
    /// </summary>
    class SurfaceExporter
    {
        /// <summary>
        /// Exports a geometry element to boundary representation.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="element">The element.</param>
        /// <param name="geometryElement">The geometry element.</param>
        /// <param name="exportBoundaryRep">True if to export boundary representation.</param>
        /// <param name="exportAsFacetation">True if to export the geometry as facetation.</param>
        /// <param name="bodyRep">Body representation.</param>
        /// <param name="boundaryRep">Boundary representation.</param>
        /// <returns>True if success, false if fail.</returns>
        public static bool ExportSurface(ExporterIFC exporterIFC, Element element, GeometryElement geometryElement,
           bool exportBoundaryRep, bool exportAsFacetation, ref IFCAnyHandle bodyRep, ref IFCAnyHandle boundaryRep)
        {
            if (geometryElement == null)
                return false;

            IFCGeometryInfo ifcGeomInfo = null;
            Document doc = element.Document;
            Plane plane = GeometryUtil.CreateDefaultPlane();
            XYZ projDir = new XYZ(0, 0, 1);
            double eps = UnitUtil.ScaleLength(doc.Application.VertexTolerance);

            ifcGeomInfo = IFCGeometryInfo.CreateFaceGeometryInfo(exporterIFC, plane, projDir, eps, exportBoundaryRep);

            ExporterIFCUtils.CollectGeometryInfo(exporterIFC, ifcGeomInfo, geometryElement, XYZ.Zero, true);

            IFCFile file = exporterIFC.GetFile();
            IFCAnyHandle surface;

            // Use tessellated geometry for surface in IFC Reference View
            if (ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
            {
                BodyExporterOptions options = new BodyExporterOptions(false);
                surface = BodyExporter.ExportBodyAsTriangulatedFaceSet(exporterIFC, element, options, geometryElement);
            }
            else
            {
                HashSet<IFCAnyHandle> faceSets = new HashSet<IFCAnyHandle>();
                IList<ICollection<IFCAnyHandle>> faceList = ifcGeomInfo.GetFaces();
                foreach (ICollection<IFCAnyHandle> faces in faceList)
                {
                    // no faces, don't complain.
                    if (faces.Count == 0)
                        continue;
                    HashSet<IFCAnyHandle> faceSet = new HashSet<IFCAnyHandle>(faces);
                    faceSets.Add(IFCInstanceExporter.CreateConnectedFaceSet(file, faceSet));
                }

                if (faceSets.Count == 0)
                    return false;

                surface = IFCInstanceExporter.CreateFaceBasedSurfaceModel(file, faceSets);
            }

            if (IFCAnyHandleUtil.IsNullOrHasNoValue(surface))
                return false;

            BodyExporter.CreateSurfaceStyleForRepItem(exporterIFC, doc, surface, BodyExporter.GetBestMaterialIdFromGeometryOrParameter(geometryElement, exporterIFC, element));

            ISet<IFCAnyHandle> surfaceItems = new HashSet<IFCAnyHandle>();
            surfaceItems.Add(surface);

            ElementId catId = CategoryUtil.GetSafeCategoryId(element);

            bodyRep = RepresentationUtil.CreateSurfaceRep(exporterIFC, element, catId, exporterIFC.Get3DContextHandle("Body"), surfaceItems,
                exportAsFacetation, bodyRep);
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(bodyRep))
                return false;

            ICollection<IFCAnyHandle> boundaryRepresentations = ifcGeomInfo.GetRepresentations();
            if (exportBoundaryRep && boundaryRepresentations.Count > 0)
            {
                HashSet<IFCAnyHandle> boundaryRepresentationSet = new HashSet<IFCAnyHandle>();
                boundaryRepresentationSet.UnionWith(boundaryRepresentations);
                boundaryRep = RepresentationUtil.CreateBoundaryRep(exporterIFC, element, catId, exporterIFC.Get3DContextHandle("FootPrint"), boundaryRepresentationSet,
                    boundaryRep);
            }

            return true;
        }
    }
}
