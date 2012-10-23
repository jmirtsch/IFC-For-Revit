//
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
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB;
using BIM.IFC.Toolkit;
using BIM.IFC.Utility;
using BIM.IFC.Exporter;

namespace BIM.IFC.Exporter
{
    /// <summary>
    /// Provides methods to export swept solid.
    /// </summary>
    class SweptSolidExporter
    {
        /// <summary>
        /// The representaion item.
        /// </summary>
        IFCAnyHandle m_RepresentationItem;

        /// <summary>
        /// True if it is a simple extrusion, false if it is a simple swept solid.
        /// </summary>
        bool m_IsExtrusion = false;

        /// <summary>
        /// True if it is a simple extrusion, false if it is a simple swept solid.
        /// </summary>
        public bool IsExtrusion
        {
            get { return m_IsExtrusion; }
        }

        /// <summary>
        /// The representation item.
        /// </summary>
        public IFCAnyHandle RepresentationItem
        {
            get { return m_RepresentationItem; }
        }

        /// <summary>
        /// Creates a SweptSolidExporter.
        /// </summary>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="element">The element.</param>
        /// <param name="solid">The solid.</param>
        /// <param name="normal">The normal of the plane that the path lies on.</param>
        /// <returns>The SweptSolidExporter.</returns>
        public static SweptSolidExporter Create(ExporterIFC exporterIFC, Element element, Solid solid, XYZ normal)
        {
            try
            {
                SweptSolidExporter sweptSolidExporter = null;
                SimpleSweptSolidAnalyzer sweptAnalyzer = SimpleSweptSolidAnalyzer.Create(element.Document.Application.Create, solid, normal);
                if (sweptAnalyzer != null)
                {
                    // TODO: support openings and recess for a swept solid
                    if (sweptAnalyzer.UnalignedFaces != null && sweptAnalyzer.UnalignedFaces.Count > 0)
                        return null;

                    IList<GeometryUtil.FaceBoundaryType> faceBoundaryTypes;
                    IList<CurveLoop> faceBoundaries = GeometryUtil.GetFaceBoundaries(sweptAnalyzer.ProfileFace, null, out faceBoundaryTypes);

                    string profileName = null;
                    if (element != null)
                    {
                        ElementType type = element.Document.GetElement(element.GetTypeId()) as ElementType;
                        if (type != null)
                            profileName = type.Name;
                    }

                    // is extrusion?
                    if (sweptAnalyzer.PathCurve is Line)
                    {
                        Line line = sweptAnalyzer.PathCurve as Line;

                        // invalid case
                        if (MathUtil.VectorsAreOrthogonal(line.Direction, sweptAnalyzer.ProfileFace.Normal))
                            return null;

                        sweptSolidExporter = new SweptSolidExporter();
                        sweptSolidExporter.m_IsExtrusion = true;
                        Plane plane = new Plane(sweptAnalyzer.ProfileFace.Normal, sweptAnalyzer.ProfileFace.Origin);
                        sweptSolidExporter.m_RepresentationItem = ExtrusionExporter.CreateExtrudedSolidFromCurveLoop(exporterIFC, profileName, faceBoundaries, plane,
                            line.Direction, line.Length * exporterIFC.LinearScale);
                    }
                    else
                    {
                        sweptSolidExporter = new SweptSolidExporter();
                        sweptSolidExporter.m_RepresentationItem = CreateSimpleSweptSolid(exporterIFC, profileName, faceBoundaries, normal, sweptAnalyzer.PathCurve);
                    }
                }
                return sweptSolidExporter;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a simple swept solid.
        /// </summary>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="element">The element.</param>
        /// <param name="solid">The solid.</param>
        /// <param name="normal">The normal of the plane that the path lies on.</param>
        /// <returns>The swept solid representation handle.</returns>
        public static IFCAnyHandle CreateSimpleSweptSolid(ExporterIFC exporterIFC, Element element, Solid solid, XYZ normal)
        {
            try
            {
                if (element == null || element.Document == null)
                    return null;

                SimpleSweptSolidAnalyzer sweptAnalyzer = SimpleSweptSolidAnalyzer.Create(element.Document.Application.Create, solid, normal);
                if (sweptAnalyzer != null)
                {
                    // TODO: support openings and recess for a swept solid
                    if (sweptAnalyzer.UnalignedFaces != null && sweptAnalyzer.UnalignedFaces.Count > 0)
                        return null;

                    IList<GeometryUtil.FaceBoundaryType> faceBoundaryTypes;
                    IList<CurveLoop> faceBoundaries = GeometryUtil.GetFaceBoundaries(sweptAnalyzer.ProfileFace, null, out faceBoundaryTypes);

                    string profileName = null;
                    if (element != null)
                    {
                        ElementType type = element.Document.GetElement(element.GetTypeId()) as ElementType;
                        if (type != null)
                            profileName = type.Name;
                    }

                    return CreateSimpleSweptSolid(exporterIFC, profileName, faceBoundaries, normal, sweptAnalyzer.PathCurve);
                }
            }
            catch (Exception)
            {
                return null;
            }

            return null;
        }

        /// <summary>
        /// Creates a simple swept solid from a list of curve loops.
        /// </summary>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="profileName">The profile name.</param>
        /// <param name="profileCurveLoops">The profile curve loops.</param>
        /// <param name="normal">The normal of the plane that the path lies on.</param>
        /// <param name="directrix">The path curve.</param>
        /// <returns>The swept solid handle.</returns>
        public static IFCAnyHandle CreateSimpleSweptSolid(ExporterIFC exporterIFC, string profileName, IList<CurveLoop> profileCurveLoops,
            XYZ normal, Curve directrix)
        {
            // see definition of IfcSurfaceCurveSweptAreaSolid from
            // http://www.buildingsmart-tech.org/ifc/IFC2x4/rc4/html/schema/ifcgeometricmodelresource/lexical/ifcsurfacecurvesweptareasolid.htm

            IFCAnyHandle simpleSweptSolidHnd = null;

            if (profileCurveLoops.Count == 0)
                return simpleSweptSolidHnd;

            IFCFile file = exporterIFC.GetFile();

            XYZ startPoint = directrix.get_EndPoint(0);
            XYZ profilePlaneNormal = null;
            XYZ profilePlaneXDir = null;
            XYZ profilePlaneYDir = null;

            IFCAnyHandle curveHandle = null;

            double startParam = 0, endParam = 1;
            Plane scaledReferencePlane = null;
            if (directrix is Line)
            {
                Line line = directrix as Line;
                startParam = line.get_EndParameter(0);
                endParam = line.get_EndParameter(1);
                profilePlaneNormal = line.Direction;
                profilePlaneYDir = normal;
                profilePlaneXDir = profilePlaneNormal.CrossProduct(profilePlaneYDir);

                XYZ linePlaneNormal = profilePlaneYDir;
                XYZ linePlaneXDir = profilePlaneXDir;
                XYZ linePlaneYDir = linePlaneNormal.CrossProduct(linePlaneXDir);
                XYZ linePlaneOrig = startPoint;

                scaledReferencePlane = GeometryUtil.CreateScaledPlane(exporterIFC, linePlaneXDir, linePlaneYDir, linePlaneOrig);
                curveHandle = GeometryUtil.CreateLine(exporterIFC, line, scaledReferencePlane);
            }
            else if (directrix is Arc)
            {
                Arc arc = directrix as Arc;

                startParam = MathUtil.PutInRange(arc.get_EndParameter(0), Math.PI, 2 * Math.PI) * (180 / Math.PI);
                endParam = MathUtil.PutInRange(arc.get_EndParameter(1), Math.PI, 2 * Math.PI) * (180 / Math.PI);

                profilePlaneNormal = directrix.ComputeDerivatives(0, true).BasisX;
                profilePlaneXDir = (arc.Center - startPoint).Normalize();
                profilePlaneYDir = profilePlaneNormal.CrossProduct(profilePlaneXDir).Normalize();

                XYZ arcPlaneNormal = arc.Normal;
                XYZ arcPlaneXDir = profilePlaneXDir;
                XYZ arcPlaneYDir = arcPlaneNormal.CrossProduct(arcPlaneXDir);
                XYZ arcPlaneOrig = startPoint;

                scaledReferencePlane = GeometryUtil.CreateScaledPlane(exporterIFC, arcPlaneXDir, arcPlaneYDir, arcPlaneOrig);
                curveHandle = GeometryUtil.CreateArc(exporterIFC, arc, scaledReferencePlane);
            }
            else
                return simpleSweptSolidHnd;

            IFCAnyHandle referencePlaneAxisHandle = ExporterUtil.CreateAxis(file, scaledReferencePlane.Origin, scaledReferencePlane.Normal, scaledReferencePlane.XVec);
            IFCAnyHandle referencePlaneHandle = IFCInstanceExporter.CreatePlane(file, referencePlaneAxisHandle);

            Plane profilePlane = new Plane(profilePlaneXDir, profilePlaneYDir, startPoint);

            IList<CurveLoop> curveLoops = null;
            try
            {
                // Check that curve loops are valid.
                curveLoops = ExporterIFCUtils.ValidateCurveLoops(profileCurveLoops, profilePlaneNormal);
            }
            catch (Exception)
            {
                return null;
            }

            if (curveLoops == null || curveLoops.Count == 0)
                return simpleSweptSolidHnd;

            IFCAnyHandle sweptArea = ExtrusionExporter.CreateSweptArea(exporterIFC, profileName, curveLoops, profilePlane, profilePlaneNormal);
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(sweptArea))
                return simpleSweptSolidHnd;

            profilePlane = GeometryUtil.GetScaledPlane(exporterIFC, profilePlane);
            IFCAnyHandle solidAxis = ExporterUtil.CreateAxis(file, profilePlane.Origin, profilePlane.Normal, profilePlane.XVec);

            simpleSweptSolidHnd = IFCInstanceExporter.CreateSurfaceCurveSweptAreaSolid(file, sweptArea, solidAxis, curveHandle, startParam,
                endParam, referencePlaneHandle);
            return simpleSweptSolidHnd;
        }
    }
}
