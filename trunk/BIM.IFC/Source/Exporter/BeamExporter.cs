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
using BIM.IFC.Utility;
using BIM.IFC.Toolkit;
using BIM.IFC.Exporter.PropertySet;

namespace BIM.IFC.Exporter
{
    /// <summary>
    /// Provides methods to export beams.
    /// </summary>
    class BeamExporter
    {
        /// <summary>
        /// Exports a beam to IFC beam.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="element">
        /// The element to be exported.
        /// </param>
        /// <param name="geometryElement">
        /// The geometry element.
        /// </param>
        /// <param name="productWrapper">
        /// The IFCProductWrapper.
        /// </param>
        public static void ExportBeam(ExporterIFC exporterIFC,
           Element element, GeometryElement geometryElement, IFCProductWrapper productWrapper)
        {
            if (geometryElement == null)
                return;

            IFCFile file = exporterIFC.GetFile();
            double scale = exporterIFC.LinearScale;

            using (IFCTransaction transaction = new IFCTransaction(file))
            {
                LocationCurve locCurve = element.Location as LocationCurve;
                Transform orientTrf = Transform.Identity;

                bool canExportAxis = (locCurve != null);
                IFCAnyHandle axisRep = null;
                XYZ projDir = null;
                Curve curve = null;

                Plane plane = null;
                if (canExportAxis)
                {
                    curve = locCurve.Curve;
                    if (curve is Line)
                    {
                        Line line = curve as Line;
                        XYZ planeX, planeY, planeOrig;
                        planeOrig = line.Origin;
                        planeX = line.Direction;
                        if (Math.Abs(planeX.Z) < 0.707)  // approx 1.0/sqrt(2.0)
                        {
                            planeY = XYZ.BasisZ.CrossProduct(planeX);
                        }
                        else
                        {
                            planeY = XYZ.BasisX.CrossProduct(planeX);
                        }
                        planeY = planeY.Normalize();
                        projDir = planeX.CrossProduct(planeY);
                        plane = new Plane(planeX, planeY, planeOrig);
                        orientTrf.BasisX = planeX; orientTrf.BasisY = planeY; orientTrf.BasisZ = projDir; orientTrf.Origin = planeOrig;
                    }
                    else if (curve is Arc)
                    {
                        XYZ xDir, yDir, center;
                        Arc arc = curve as Arc;
                        xDir = arc.XDirection; yDir = arc.YDirection; projDir = arc.Normal; center = arc.Center;
                        plane = new Plane(xDir, yDir, center);
                        orientTrf.BasisX = xDir; orientTrf.BasisY = yDir; orientTrf.BasisZ = projDir; orientTrf.Origin = center;
                    }
                    else
                        canExportAxis = false;
                }

                using (IFCPlacementSetter setter = IFCPlacementSetter.Create(exporterIFC, element, null, canExportAxis ? orientTrf : null, ElementId.InvalidElementId))
                {
                    IFCAnyHandle localPlacement = setter.GetPlacement();
                    SolidMeshGeometryInfo solidMeshInfo = GeometryUtil.GetSolidMeshGeometry(geometryElement, Transform.Identity);

                    using (IFCExtrusionCreationData extrusionCreationData = new IFCExtrusionCreationData())
                    {
                        extrusionCreationData.SetLocalPlacement(localPlacement);
                        if (canExportAxis && (orientTrf.BasisX != null))
                        {
                            extrusionCreationData.CustomAxis = orientTrf.BasisX;
                            extrusionCreationData.PossibleExtrusionAxes = IFCExtrusionAxes.TryCustom;
                        }
                        else
                            extrusionCreationData.PossibleExtrusionAxes = IFCExtrusionAxes.TryXY;

                        IList<Solid> solids = solidMeshInfo.GetSolids();
                        IList<Mesh> meshes = solidMeshInfo.GetMeshes();

                        ElementId catId = CategoryUtil.GetSafeCategoryId(element);
                        BodyData bodyData = null;
                        BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true);
                        if (solids.Count > 0 || meshes.Count > 0)
                        {
                            bodyData = BodyExporter.ExportBody(element.Document.Application, exporterIFC, element, catId,
                                solids, meshes, bodyExporterOptions, extrusionCreationData);
                        }
                        else
                        {
                            IList<GeometryObject> geomlist = new List<GeometryObject>();
                            geomlist.Add(geometryElement);
                            bodyData = BodyExporter.ExportBody(element.Document.Application, exporterIFC, element, catId, geomlist,
                                bodyExporterOptions, extrusionCreationData);
                        }

                        if (IFCAnyHandleUtil.IsNullOrHasNoValue(bodyData.RepresentationHnd))
                        {
                            extrusionCreationData.ClearOpenings();
                            return;
                        }

                        IList<IFCAnyHandle> representations = new List<IFCAnyHandle>();

                        if (canExportAxis)
                        {
                            XYZ curveOffset = new XYZ(0, 0, 0);
                            if (bodyData.BrepOffsetTransform != null)
                                curveOffset = -bodyData.BrepOffsetTransform.Origin / scale;

                            Plane offsetPlane = new Plane(plane.XVec, plane.YVec, XYZ.Zero);
                            IFCGeometryInfo info = IFCGeometryInfo.CreateCurveGeometryInfo(exporterIFC, offsetPlane, projDir, false);
                            ExporterIFCUtils.CollectGeometryInfo(exporterIFC, info, curve, curveOffset, true);

                            IList<IFCAnyHandle> axis_items = info.GetCurves();

                            if (axis_items.Count > 0)
                            {
                                string identifierOpt = "Axis";	// this is by IFC2x2 convention, not temporary
                                string representationTypeOpt = "Curve2D";  // this is by IFC2x2 convention, not temporary
                                axisRep = RepresentationUtil.CreateShapeRepresentation(exporterIFC, element, catId, exporterIFC.Get3DContextHandle(identifierOpt),
                                   identifierOpt, representationTypeOpt, axis_items);
                                representations.Add(axisRep);
                            }
                        }
                        representations.Add(bodyData.RepresentationHnd);

                        IFCAnyHandle prodRep = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, representations);

                        string instanceGUID = ExporterIFCUtils.CreateGUID(element);
                        string origInstanceName = exporterIFC.GetName();
                        string instanceName = NamingUtil.GetNameOverride(element, origInstanceName);
                        string instanceDescription = NamingUtil.GetDescriptionOverride(element, null);
                        string instanceObjectType = NamingUtil.GetObjectTypeOverride(element, NamingUtil.CreateIFCObjectName(exporterIFC, element));
                        string instanceElemId = NamingUtil.CreateIFCElementId(element);

                        IFCAnyHandle beam = IFCInstanceExporter.CreateBeam(file, instanceGUID, exporterIFC.GetOwnerHistoryHandle(),
                            instanceName, instanceDescription, instanceObjectType, extrusionCreationData.GetLocalPlacement(), prodRep, instanceElemId);

                        productWrapper.AddElement(beam, setter, extrusionCreationData, LevelUtil.AssociateElementToLevel(element));

                        OpeningUtil.CreateOpeningsIfNecessary(beam, element, extrusionCreationData, exporterIFC, extrusionCreationData.GetLocalPlacement(), setter, productWrapper);

                        FamilyTypeInfo typeInfo = new FamilyTypeInfo();
                        typeInfo.ScaledDepth = extrusionCreationData.ScaledLength;
                        typeInfo.ScaledArea = extrusionCreationData.ScaledArea;
                        typeInfo.ScaledInnerPerimeter = extrusionCreationData.ScaledInnerPerimeter;
                        typeInfo.ScaledOuterPerimeter = extrusionCreationData.ScaledOuterPerimeter;
                        PropertyUtil.CreateBeamColumnBaseQuantities(exporterIFC, beam, element, typeInfo);

                        if (bodyData.MaterialIds.Count != 0)
                        {
                            CategoryUtil.CreateMaterialAssociations(element.Document, exporterIFC, beam, bodyData.MaterialIds);
                        }

                        PropertyUtil.CreateInternalRevitPropertySets(exporterIFC, element, productWrapper);
                    }
                }

                transaction.Commit();
            }
        }
    }
}
