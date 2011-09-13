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
using System.Text;
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using BIM.IFC.Utility;


namespace BIM.IFC.Exporter
{
    /// <summary>
    /// Provides methods to export curve elements.
    /// </summary>
    class CurveElementExporter
    {
        /// <summary>
        /// Checks if the curve element should be exported.
        /// </summary>
        /// <param name="curveElement">
        /// The curve element.
        /// </param>
        /// <returns>
        /// True if the curve element should be exported, false otherwise.
        /// </returns>
        private static bool ShouldCurveElementBeExported(CurveElement curveElement)
        {
            CurveElementType curveElementType = curveElement.CurveElementType;
            bool exported = false;
            if (curveElementType == CurveElementType.ModelCurve || curveElementType == CurveElementType.CurveByPoints)
                exported = true;

            if (exported)
            {
                // Confirm curve is not used by another element
                exported = !ExporterIFCUtils.IsCurveFromOtherElementSketch(curveElement);

                // Confirm the geometry curve is valid.
                Curve curve = curveElement.GeometryCurve;

                if (curve == null)
                    exported = false;
                else if (curve is Line)
                {
                    if (!curve.IsBound)
                        exported = false;
                    else
                    {
                        XYZ end1 = curve.get_EndPoint(0);
                        XYZ end2 = curve.get_EndPoint(1);
                        if (end1.IsAlmostEqualTo(end2))
                            exported = false;
                    }
                }
            }

            return exported;
        }

        /// <summary>
        /// Exports a curve element to IFC curve annotation.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="curveElement">
        /// The curve element to be exported.
        /// </param>
        /// <param name="geometryElement">
        /// The geometry element.
        /// </param>
        /// <param name="productWrapper">
        /// The IFCProductWrapper.
        /// </param>
        public static void ExportCurveElement(ExporterIFC exporterIFC, CurveElement curveElement, GeometryElement geometryElement,
                                               IFCProductWrapper productWrapper, CurveAnnotationCache annotationCache)
        {
            if (geometryElement == null || !ShouldCurveElementBeExported(curveElement))
                return;

            SketchPlane sketchPlane = curveElement.SketchPlane;
            if (sketchPlane == null)
                return;

            IFCFile file = exporterIFC.GetFile();

            using (IFCTransaction tr = new IFCTransaction(file))
            {
                using (IFCPlacementSetter setter = IFCPlacementSetter.Create(exporterIFC, curveElement))
                {
                    IFCAnyHandle localPlacement = setter.GetPlacement();
                    IFCAnyHandle axisPlacement = file.Create3DAxisFromLocalPlacement(localPlacement);

                    Plane planeSK = sketchPlane.Plane;
                    XYZ projDir = planeSK.Normal;
                    XYZ origin = planeSK.Origin;
                    bool useOffsetTrf = false;
                    if (projDir.IsAlmostEqualTo(XYZ.BasisZ))
                    {
                        XYZ offset = XYZ.BasisZ * setter.Offset;
                        origin -= offset;
                    }
                    else
                        useOffsetTrf = true;

                    Plane plane = new Plane(planeSK.XVec, planeSK.YVec, origin);
                    IFCGeometryInfo info = IFCGeometryInfo.CreateCurveGeometryInfo(exporterIFC, plane, projDir, false);

                    if (useOffsetTrf)
                    {
                        XYZ offsetOrig = -XYZ.BasisZ * setter.Offset;
                        Transform trf = Transform.get_Translation(offsetOrig);
                        ExporterIFCUtils.CollectGeometryInfo(exporterIFC, info, geometryElement, XYZ.Zero, false, trf);
                    }
                    else
                    {
                        ExporterIFCUtils.CollectGeometryInfo(exporterIFC, info, geometryElement, XYZ.Zero, false);
                    }

                    IList<IFCAnyHandle> curves = info.GetCurves();

                    if (curves.Count != 1)
                    {
                        throw new Exception("IFC: expected 1 curve when export curve element.");
                    }

                    IFCAnyHandle repItemHnd = file.CreateGeometricCurveSet(curves);

                    IFCAnyHandle curveStyle = file.CreateStyle(exporterIFC, repItemHnd);

                    //IFCAnyHandle curveAnno = ExporterIFCUtils.GetCurveAnnotation(exporterIFC, sketchPlane.Id, curveStyle);
                    IFCAnyHandle curveAnno = annotationCache.GetAnnotation(sketchPlane.Id, curveStyle);
                    if (curveAnno != null && curveAnno.HasValue)
                    {
                        ExporterIFCUtils.AddCurveToAnnotation(curveAnno, curves[0]);
                    }
                    else
                    {
                        curveAnno = ExporterIFCUtils.CreateCurveAnnotation(exporterIFC, curveElement.Category.Id, sketchPlane.Id, plane, curveStyle, setter, localPlacement, repItemHnd);

                        annotationCache.AddAnnotation(sketchPlane.Id, curveStyle, curveAnno);
                    }
                }
                tr.Commit();
            }
        }
    }
}
