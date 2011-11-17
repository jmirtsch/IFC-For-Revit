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
using Autodesk.Revit.DB.Analysis;
using BIM.IFC.Utility;


namespace BIM.IFC.Exporter
{
    /// <summary>
    /// Provides methods to export spatial elements.
    /// </summary>
    class SpatialElementExporter
    {
        //This will cache some useful results from previous calculator.
        private static SpatialElementGeometryCalculator s_SpatialElemGeometryCalculator = null;

        /// <summary>
        /// Initialize SpatialElementGeometryCalculator object.
        /// </summary>
        /// <param name="document">
        /// The Revit document.
        /// </param>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        public static void InitializeSpatialElementGeometryCalculator(Document document, ExporterIFC exporterIFC)
        {
            SpatialElementBoundaryOptions options = ExporterIFCUtils.GetSpatialElementBoundaryOptions(exporterIFC, null);
            s_SpatialElemGeometryCalculator = new SpatialElementGeometryCalculator(document, options);
        }

        /// <summary>
        /// Export spatial elements, including rooms, areas and spaces. 1st level space boundaries.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="spatialElement">
        /// The spatial element.
        /// </param>
        /// <param name="productWrapper">
        /// The IFCProductWrapper.
        /// </param>
        public static void ExportSpatialElement(ExporterIFC exporterIFC, SpatialElement spatialElement, IFCProductWrapper productWrapper)
        {
            //quick reject
            bool isArea = spatialElement is Area;
            if (isArea)
            {
                if (!IsAreaGrossInterior(exporterIFC, spatialElement))
                    return;
            }

            IFCFile file = exporterIFC.GetFile();
            using (IFCTransaction tr = new IFCTransaction(file))
            {
                ElementId levelId = spatialElement.Level != null ? spatialElement.Level.Id : ElementId.InvalidElementId;
                using (IFCPlacementSetter setter = IFCPlacementSetter.Create(exporterIFC, spatialElement, null, null, levelId))
                {
                    CreateIFCSpace(exporterIFC, spatialElement, productWrapper, setter);

                    // Do not create boundary information, or extra property sets.
                    if (spatialElement is Area)
                    {
                        tr.Commit();
                        return;
                    }

                    if (exporterIFC.SpaceBoundaryLevel == 1)
                    {
                        Document document = spatialElement.Document;
                        IFCLevelInfo levelInfo = exporterIFC.GetLevelInfo(levelId);
                        double baseHeightNonScaled = levelInfo.Elevation;

                        SpatialElementGeometryResults spatialElemGeomResult = s_SpatialElemGeometryCalculator.CalculateSpatialElementGeometry(spatialElement);

                        Solid spatialElemGeomSolid = spatialElemGeomResult.GetGeometry();
                        FaceArray faces = spatialElemGeomSolid.Faces;
                        foreach (Face face in faces)
                        {
                            IList<SpatialElementBoundarySubface> spatialElemBoundarySubfaces = spatialElemGeomResult.GetBoundaryFaceInfo(face);
                            foreach (SpatialElementBoundarySubface spatialElemBSubface in spatialElemBoundarySubfaces)
                            {
                                if (spatialElemBSubface.SubfaceType == SubfaceType.Side)
                                    continue;

                                if (spatialElemBSubface.GetSubface() == null)
                                    continue;

                                ElementId elemId = spatialElemBSubface.SpatialBoundaryElement.LinkInstanceId;
                                if (elemId == ElementId.InvalidElementId)
                                {
                                    elemId = spatialElemBSubface.SpatialBoundaryElement.HostElementId;
                                }

                                Element boundingElement = document.get_Element(elemId);
                                if (boundingElement == null)
                                    continue;

                                bool isObjectExt = CategoryUtil.IsElementExternal(boundingElement);

                                IFCGeometryInfo info = IFCGeometryInfo.CreateSurfaceGeometryInfo(spatialElement.Document.Application.VertexTolerance);

                                Face subFace = spatialElemBSubface.GetSubface();
                                ExporterIFCUtils.CollectGeometryInfo(exporterIFC, info, subFace, XYZ.Zero, false);

                                IFCAnyHandle ifcOptionalHnd = IFCAnyHandle.Create();
                                foreach (IFCAnyHandle surfaceHnd in info.GetSurfaces())
                                {
                                    IFCAnyHandle connectionGeometry = file.CreateConnectionSurfaceGeometry(surfaceHnd, ifcOptionalHnd);

                                    IFCSpaceBoundary spaceBoundary = IFCSpaceBoundary.Create(spatialElement.Id, boundingElement.Id, connectionGeometry, IFCSpaceBoundaryType.Physical, isObjectExt);

                                    if (!ProcessIFCSpaceBoundary(exporterIFC, spaceBoundary, file))
                                        exporterIFC.RegisterIFCSpaceBoundary(spaceBoundary);
                                }
                            }
                        }

                        IList<IList<BoundarySegment>> roomBoundaries = spatialElement.GetBoundarySegments(ExporterIFCUtils.GetSpatialElementBoundaryOptions(exporterIFC, spatialElement));
                        double roomHeight = GetHeight(spatialElement, exporterIFC.LinearScale, levelInfo);
                        XYZ zDir = new XYZ(0, 0, 1);

                        foreach (IList<BoundarySegment> roomBoundaryList in roomBoundaries)
                        {
                            foreach (BoundarySegment roomBoundary in roomBoundaryList)
                            {
                                Element boundingElement = roomBoundary.Element;

                                if (boundingElement == null)
                                    continue;

                                ElementId buildingElemId = boundingElement.Id;
                                Curve trimmedCurve = roomBoundary.Curve;

                                if (trimmedCurve == null)
                                    continue;

                                //trimmedCurve.Visibility = Visibility.Visible; readonly
                                IFCAnyHandle connectionGeometry = ExporterIFCUtils.CreateExtrudedSurfaceFromCurve(
                                   exporterIFC, trimmedCurve, zDir, roomHeight, baseHeightNonScaled);

                                IFCSpaceBoundaryType physOrVirt = IFCSpaceBoundaryType.Physical;
                                if (boundingElement is CurveElement)
                                    physOrVirt = IFCSpaceBoundaryType.Virtual;
                                else if (boundingElement is Autodesk.Revit.DB.Architecture.Room)
                                    physOrVirt = IFCSpaceBoundaryType.Undefined;

                                bool isObjectExt = CategoryUtil.IsElementExternal(boundingElement);
                                bool isObjectPhys = (physOrVirt == IFCSpaceBoundaryType.Physical);

                                ElementId actualBuildingElemId = isObjectPhys ? buildingElemId : ElementId.InvalidElementId;
                                IFCSpaceBoundary boundary = IFCSpaceBoundary.Create(spatialElement.Id, actualBuildingElemId, connectionGeometry, physOrVirt, isObjectExt);

                                if (!ProcessIFCSpaceBoundary(exporterIFC, boundary, file))
                                    exporterIFC.RegisterIFCSpaceBoundary(boundary);

                                // try to add doors and windows for host objects if appropriate.
                                if (isObjectPhys && boundingElement is HostObject)
                                {
                                    HostObject hostObj = boundingElement as HostObject;
                                    IList<ElementId> elemIds = hostObj.FindInserts(false, false, false, false);
                                    foreach (ElementId elemId in elemIds)
                                    {
                                        // we are going to do a simple bbox export, not complicated geometry.
                                        Element instElem = document.get_Element(elemId);
                                        if (instElem == null)
                                            continue;

                                        BoundingBoxXYZ instBBox = instElem.get_BoundingBox(null);

                                        // make copy of original trimmed curve.
                                        Curve instCurve = trimmedCurve.Clone();
                                        XYZ instOrig = instCurve.get_EndPoint(0);

                                        // make sure that the insert is on this level.
                                        if (instBBox.Max.Z < instOrig.Z)
                                            continue;
                                        if (instBBox.Min.Z > instOrig.Z + roomHeight)
                                            continue;

                                        double insHeight = Math.Min(instBBox.Max.Z, instOrig.Z + roomHeight) - Math.Max(instOrig.Z, instBBox.Min.Z);
                                        if (insHeight < (1.0 / (12.0 * 16.0)))
                                            continue;

                                        // move base curve to bottom of bbox.
                                        XYZ moveDir = new XYZ(0.0, 0.0, instBBox.Min.Z - instOrig.Z);
                                        Transform moveTrf = Transform.get_Translation(moveDir);
                                        instCurve = instCurve.get_Transformed(moveTrf);

                                        bool isHorizOrVert = false;
                                        if (instCurve is Line)
                                        {
                                            Line instLine = instCurve as Line;
                                            XYZ lineDir = instLine.Direction;
                                            if (MathUtil.IsAlmostEqual(Math.Abs(lineDir.X), 1.0) || (MathUtil.IsAlmostEqual(Math.Abs(lineDir.Y), 1.0)))
                                                isHorizOrVert = true;
                                        }

                                        double[] parameters = new double[2];
                                        double[] origEndParams = new double[2];
                                        if (isHorizOrVert)
                                        {
                                            parameters[0] = instCurve.Project(instBBox.Min).Parameter;
                                            parameters[1] = instCurve.Project(instBBox.Max).Parameter;
                                        }
                                        else
                                        {
                                            FamilyInstance famInst = instElem as FamilyInstance;
                                            if (famInst == null)
                                                continue;

                                            ElementType elementType = document.get_Element(famInst.GetTypeId()) as ElementType;
                                            if (elementType == null)
                                                continue;

                                            BoundingBoxXYZ symBBox = elementType.get_BoundingBox(null);
                                            Curve symCurve = trimmedCurve.Clone();
                                            Transform trf = famInst.GetTransform();
                                            Transform invTrf = trf.Inverse;
                                            Curve trfCurve = symCurve.get_Transformed(invTrf);
                                            parameters[0] = trfCurve.Project(symBBox.Min).Parameter;
                                            parameters[1] = trfCurve.Project(symBBox.Max).Parameter;
                                        }

                                        // ignore if less than 1/16".
                                        if (Math.Abs(parameters[1] - parameters[0]) < 1.0 / (12.0 * 16.0))
                                            continue;
                                        if (parameters[0] > parameters[1])
                                        {
                                            //swap
                                            double tempParam = parameters[0];
                                            parameters[0] = parameters[1];
                                            parameters[1] = tempParam;
                                        }

                                        origEndParams[0] = instCurve.get_EndParameter(0);
                                        origEndParams[1] = instCurve.get_EndParameter(1);

                                        if (origEndParams[0] > parameters[1] - (1.0 / (12.0 * 16.0)))
                                            continue;
                                        if (origEndParams[1] < parameters[0] + (1.0 / (12.0 * 16.0)))
                                            continue;

                                        if (parameters[0] > origEndParams[0])
                                            instCurve.set_EndParameter(0, parameters[0]);
                                        if (parameters[1] < origEndParams[1])
                                            instCurve.set_EndParameter(1, parameters[1]);

                                        IFCAnyHandle insConnectionGeom = ExporterIFCUtils.CreateExtrudedSurfaceFromCurve(exporterIFC, instCurve, zDir,
                                           insHeight, baseHeightNonScaled);

                                        IFCSpaceBoundary instBoundary = IFCSpaceBoundary.Create(spatialElement.Id, elemId, insConnectionGeom, physOrVirt, isObjectExt);
                                        if (!ProcessIFCSpaceBoundary(exporterIFC, instBoundary, file))
                                            exporterIFC.RegisterIFCSpaceBoundary(instBoundary);
                                    }
                                }
                            }
                        }
                    }
                    ExporterIFCUtils.CreateSpatialElementPropertySet(exporterIFC, spatialElement, productWrapper);
                }
                tr.Commit();
            }
        }

        /// <summary>
        /// Export spatial elements, including rooms, areas and spaces. 2nd level space boundaries.
        /// </summary>
        /// <param name="ifcExporter">
        /// The Exporter object.
        /// </param>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="document">
        /// The Revit document.
        /// </param>
        /// <param name="filterView">
        /// The view not exported.
        /// </param>
        /// <param name="spaceExported">
        /// The output boolean value indicates if exported successfully.
        /// </param>
        public static void ExportSpatialElement2ndLevel(BIM.IFC.Exporter.Exporter ifcExporter, ExporterIFC exporterIFC, Document document, View filterView)
        {
            using (SubTransaction st = new SubTransaction(document))
            {
                st.Start();

                bool createEnergyAnalysisDetailModelFailed = false;
                EnergyAnalysisDetailModel model = null;
                try
                {
                    IFCFile file = exporterIFC.GetFile();
                    using (IFCTransaction tr = new IFCTransaction(file))
                    {

                        EnergyAnalysisDetailModelOptions options = new EnergyAnalysisDetailModelOptions();
                        options.Tier = EnergyAnalysisDetailModelTier.SecondLevelBoundaries; //2nd level space boundaries
                        options.SimplifyCurtainSystems = true;
                        try
                        {
                            model = EnergyAnalysisDetailModel.Create(document, options);
                        }
                        catch (Exception)
                        {
                            createEnergyAnalysisDetailModelFailed = true;
                            throw;
                        }
                        IList<EnergyAnalysisSpace> spaces = model.GetAnalyticalSpaces();

                        foreach (EnergyAnalysisSpace space in spaces)
                        {
                            SpatialElement spatialElement = document.get_Element(space.SpatialElementId) as SpatialElement;

                            if (spatialElement == null)
                                continue;

                            //quick reject
                            bool isArea = spatialElement is Area;
                            if (isArea)
                            {
                                if (!IsAreaGrossInterior(exporterIFC, spatialElement))
                                    continue;
                            }

                            //current view only
                            if (filterView != null && !ElementFilteringUtil.IsElementVisible(filterView, spatialElement))
                                continue;
                            //

                            if (!ElementFilteringUtil.ShouldCategoryBeExported(exporterIFC, spatialElement))
                                continue;

                            Options geomOptions = new Options();
                            View ownerView = spatialElement.Document.get_Element(spatialElement.OwnerViewId) as View;
                            if (ownerView != null)
                                geomOptions.View = ownerView;
                            GeometryElement geomElem = spatialElement.get_Geometry(geomOptions);

                            try
                            {
                                exporterIFC.PushExportState(spatialElement, geomElem);

                                using (IFCProductWrapper productWrapper = IFCProductWrapper.Create(exporterIFC, true))
                                {
                                    ElementId levelId = spatialElement.Level != null ? spatialElement.Level.Id : ElementId.InvalidElementId;
                                    using (IFCPlacementSetter setter = IFCPlacementSetter.Create(exporterIFC, spatialElement, null, null, levelId))
                                    {
                                        try
                                        {
                                            CreateIFCSpace(exporterIFC, spatialElement, productWrapper, setter);
                                        }
                                        catch (System.Exception)
                                        {
                                            continue;
                                        }
                                        //get boundary information from surfaces
                                        IList<EnergyAnalysisSurface> surfaces = space.GetAnalyticalSurfaces();
                                        foreach (EnergyAnalysisSurface surface in surfaces)
                                        {
                                            Element boundingElement = GetBoundaryElement(document, surface.OriginatingElementId);
                                            if (boundingElement == null)
                                                continue;

                                            IList<EnergyAnalysisOpening> openings = surface.GetAnalyticalOpenings();
                                            IFCAnyHandle connectionGeometry = CreateConnectionSurfaceGeometry(file, surface, openings);
                                            CreateIFCSpaceBoundary(file, exporterIFC, spatialElement, boundingElement, connectionGeometry);

                                            // try to add doors and windows for host objects if appropriate.
                                            if (boundingElement is HostObject)
                                            {
                                                foreach (EnergyAnalysisOpening opening in openings)
                                                {
                                                    Element openingBoundingElem = GetBoundaryElement(document, opening.OriginatingElementId);
                                                    IFCAnyHandle openingConnectionGeom = CreateConnectionSurfaceGeometry(file, opening);
                                                    CreateIFCSpaceBoundary(file, exporterIFC, spatialElement, openingBoundingElem, openingConnectionGeom);
                                                }
                                            }
                                        }
                                        ExporterIFCUtils.CreateSpatialElementPropertySet(exporterIFC, spatialElement, productWrapper);
                                        ifcExporter.ExportElementProperties(exporterIFC, spatialElement, productWrapper);
                                        ifcExporter.ExportElementQuantities(exporterIFC, spatialElement, productWrapper);
                                    }
                                }
                            }
                            finally
                            {
                                exporterIFC.PopExportState();
                            }
                        }
                        tr.Commit();
                    }
                }
                catch (System.Exception ex)
                {
                    document.Application.WriteJournalComment("IFC error: " + ex.ToString(), true);
                }
                finally
                {
                    if (model != null)
                        EnergyAnalysisDetailModel.Destroy(model);
                }

                //if failed, just export the space element
                if (createEnergyAnalysisDetailModelFailed)
                {
                    IFCFile file = exporterIFC.GetFile();
                    using (IFCTransaction tr = new IFCTransaction(file))
                    {
                        ElementFilter spatialElementFilter = ElementFilteringUtil.GetSpatialElementFilter(document, exporterIFC);
                        FilteredElementCollector collector = (filterView == null) ? new FilteredElementCollector(document) : new FilteredElementCollector(document, filterView.Id);
                        collector.WherePasses(spatialElementFilter);
                        foreach (Element elem in collector)
                        {
                            SpatialElement spatialElement = elem as SpatialElement;

                            if (spatialElement == null)
                                continue;

                            //current view only
                            if (filterView != null && !ElementFilteringUtil.IsElementVisible(filterView, spatialElement))
                                continue;
                            //
                            if (!ElementFilteringUtil.ShouldCategoryBeExported(exporterIFC, spatialElement))
                                continue;

                            Options geomOptions = new Options();
                            View ownerView = spatialElement.Document.get_Element(spatialElement.OwnerViewId) as View;
                            if (ownerView != null)
                                geomOptions.View = ownerView;
                            GeometryElement geomElem = spatialElement.get_Geometry(geomOptions);

                            try
                            {
                                exporterIFC.PushExportState(spatialElement, geomElem);

                                using (IFCProductWrapper productWrapper = IFCProductWrapper.Create(exporterIFC, true))
                                {
                                    ElementId levelId = spatialElement.Level != null ? spatialElement.Level.Id : ElementId.InvalidElementId;
                                    using (IFCPlacementSetter setter = IFCPlacementSetter.Create(exporterIFC, spatialElement, null, null, levelId))
                                    {
                                        try
                                        {
                                            CreateIFCSpace(exporterIFC, spatialElement, productWrapper, setter);
                                        }
                                        catch (System.Exception)
                                        {
                                            continue;
                                        }
                                        if (!(spatialElement is Area))
                                            ExporterIFCUtils.CreateSpatialElementPropertySet(exporterIFC, spatialElement, productWrapper);
                                        ifcExporter.ExportElementProperties(exporterIFC, spatialElement, productWrapper);
                                        ifcExporter.ExportElementQuantities(exporterIFC, spatialElement, productWrapper);
                                    }
                                }
                            }
                            finally
                            {
                                exporterIFC.PopExportState();
                            }
                        }
                        tr.Commit();
                    }
                }
                st.RollBack();
            }
        }

        /// <summary>
        /// Creates SpaceBoundary from a bounding element.
        /// </summary>
        /// <param name="file">
        /// The IFC file.
        /// </param>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="spatialElement">
        /// The spatial element.
        /// </param>
        /// <param name="boundingElement">
        /// The bounding element.
        /// </param>
        /// <param name="connectionGeometry">
        /// The connection geometry handle.
        /// </param>
        static void CreateIFCSpaceBoundary(IFCFile file, ExporterIFC exporterIFC, SpatialElement spatialElement, Element boundingElement, IFCAnyHandle connectionGeometry)
        {
            IFCSpaceBoundaryType physOrVirt = IFCSpaceBoundaryType.Physical;
            if (boundingElement is CurveElement)
                physOrVirt = IFCSpaceBoundaryType.Virtual;
            else if (boundingElement is Autodesk.Revit.DB.Architecture.Room)
                physOrVirt = IFCSpaceBoundaryType.Undefined;

            bool isObjectExt = CategoryUtil.IsElementExternal(boundingElement);

            IFCSpaceBoundary spaceBoundary = IFCSpaceBoundary.Create(spatialElement.Id, boundingElement.Id, connectionGeometry, physOrVirt, isObjectExt);

            if (!ProcessIFCSpaceBoundary(exporterIFC, spaceBoundary, file))
                exporterIFC.RegisterIFCSpaceBoundary(spaceBoundary);
        }

        /// <summary>
        /// Get element from LinkElementId.
        /// </summary>
        /// <param name="document">
        /// The Revit document.
        /// </param>
        /// <param name="linkElementId">
        /// The link element id.
        /// </param>
        /// <returns>
        /// The element.
        /// </returns>
        static Element GetBoundaryElement(Document document, LinkElementId linkElementId)
        {
            ElementId elemId = linkElementId.LinkInstanceId;
            if (elemId == ElementId.InvalidElementId)
            {
                elemId = linkElementId.HostElementId;
            }
            return document.get_Element(elemId);
        }

        /// <summary>
        /// Create IFC connection surface geometry from a surface object.
        /// </summary>
        /// <param name="file">
        /// The IFC file.
        /// </param>
        /// <param name="surface">
        /// The EnergyAnalysisSurface.
        /// </param>
        /// <param name="openings">
        /// List of EnergyAnalysisOpenings.
        /// </param>
        /// <param name="offset">
        /// The offset of the geometry.
        /// </param>
        /// <returns>
        /// The connection geometry handle.
        /// </returns>
        static IFCAnyHandle CreateConnectionSurfaceGeometry(IFCFile file, EnergyAnalysisSurface surface, IList<EnergyAnalysisOpening> openings)
        {
            Polyloop outerLoop = surface.GetPolyloop();
            IList<XYZ> outerLoopPoints = outerLoop.GetPoints();

            IList<IList<XYZ>> innerLoopPoints = new List<IList<XYZ>>();
            foreach (EnergyAnalysisOpening opening in openings)
            {
                innerLoopPoints.Add(opening.GetPolyloop().GetPoints());
            }

            IFCAnyHandle hnd = file.CreateCurveBoundedPlane(outerLoopPoints, innerLoopPoints);

            IFCAnyHandle ifcOptionalHnd = IFCAnyHandle.Create();
            return file.CreateConnectionSurfaceGeometry(hnd, ifcOptionalHnd);
        }

        /// <summary>
        /// Create IFC connection surface geometry from an opening object.
        /// </summary>
        /// <param name="file">
        /// The IFC file.
        /// </param>
        /// <param name="opening">
        /// The EnergyAnalysisOpening.
        /// </param>
        /// <param name="offset">
        /// The offset of opening.
        /// </param>
        /// <returns>
        /// The connection surface geometry handle.
        /// </returns>
        static IFCAnyHandle CreateConnectionSurfaceGeometry(IFCFile file, EnergyAnalysisOpening opening)
        {
            Polyloop outerLoop = opening.GetPolyloop();
            IList<XYZ> outerLoopPoints = outerLoop.GetPoints();

            IList<IList<XYZ>> innerLoopPoints = new List<IList<XYZ>>();

            IFCAnyHandle hnd = file.CreateCurveBoundedPlane(outerLoopPoints, innerLoopPoints);

            IFCAnyHandle ifcOptionalHnd = IFCAnyHandle.Create();
            return file.CreateConnectionSurfaceGeometry(hnd, ifcOptionalHnd);
        }

        /// <summary>
        /// Check if the spatial element is gross interior.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="spatialElement">
        /// The spatial element.
        /// </param>
        /// <returns>
        /// True if the area is gross interior.
        /// </returns>
        static bool IsAreaGrossInterior(ExporterIFC exporterIFC, SpatialElement spatialElement)
        {
            Area area = spatialElement as Area;
            if (area != null)
            {
                double scale = exporterIFC.LinearScale;

                double dArea = 0.0;
                Parameter paramRoomArea = area.get_Parameter(BuiltInParameter.ROOM_AREA);
                if (paramRoomArea != null && paramRoomArea.StorageType == StorageType.Double)
                {
                    dArea = paramRoomArea.AsDouble();
                    dArea *= (scale * scale);
                }  // convert scale to export scale; area is scale squared.

                if (!MathUtil.IsAlmostZero(dArea) && area.IsGrossInterior)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Get the height of a spatial element.
        /// </summary>
        /// <param name="spatialElement">
        /// The spatial element.
        /// </param>
        /// <param name="scale">
        /// The scale value.
        /// </param>
        /// <param name="levelInfo">
        /// The level info.
        /// </param>
        /// <returns>
        /// The height.
        /// </returns>
        static double GetHeight(SpatialElement spatialElement, double scale, IFCLevelInfo levelInfo)
        {
            Document document = spatialElement.Document;

            double roomHeight = 0.0;
            double bottomOffset = 0.0;

            ElementId bottomLevelId = spatialElement.Level.Id;

            Parameter paramTopLevelId = spatialElement.get_Parameter(BuiltInParameter.ROOM_UPPER_LEVEL);
            ElementId topLevelId = paramTopLevelId != null ? paramTopLevelId.AsElementId() : ElementId.InvalidElementId;

            Parameter paramTopOffset = spatialElement.get_Parameter(BuiltInParameter.ROOM_UPPER_OFFSET);
            double topOffset = paramTopOffset != null ? paramTopOffset.AsDouble() : 0.0;

            Parameter paramBottomOffset = spatialElement.get_Parameter(BuiltInParameter.ROOM_LOWER_OFFSET);
            bottomOffset = paramBottomOffset != null ? paramBottomOffset.AsDouble() : 0.0;

            Level bottomLevel = document.get_Element(bottomLevelId) as Level;
            Level topLevel =
               (bottomLevelId == topLevelId) ? bottomLevel : document.get_Element(topLevelId) as Level;

            if (bottomLevel != null && topLevel != null)
            {
                roomHeight = (topLevel.Elevation - bottomLevel.Elevation) + (topOffset - bottomOffset);
                roomHeight *= scale;
            }

            if (MathUtil.IsAlmostZero(roomHeight))
            {
                roomHeight = levelInfo.DistanceToNextLevel * scale;
            }

            // For area spaces, we assign a dummy height (1'), as we are not allowed to export IfcSpaces without a volumetric representation.
            if (MathUtil.IsAlmostZero(roomHeight) && spatialElement is Area)
            {
                roomHeight = 1.0;
            }

            return roomHeight;
        }

        /// <summary>
        /// Create space boundary.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="boundary">
        /// The space boundary object.
        /// </param>
        /// <param name="file">
        /// The IFC file.
        /// </param>
        /// <returns>
        /// True if processed successfully, false otherwise.
        /// </returns>
        static bool ProcessIFCSpaceBoundary(ExporterIFC exporterIFC, IFCSpaceBoundary boundary, IFCFile file)
        {
            string spaceBoundaryName = String.Empty;
            if (exporterIFC.SpaceBoundaryLevel == 1)
                spaceBoundaryName = "1stLevel";
            else if (exporterIFC.SpaceBoundaryLevel == 2)
                spaceBoundaryName = "2ndLevel";
            IFCLabel spaceBoundaryNameLabel = IFCLabel.Create(spaceBoundaryName);

            IFCAnyHandle spatialElemHnd = exporterIFC.FindSpatialElementHandle(boundary.SpatialElementId);
            if (!spatialElemHnd.HasValue)
                return false;

            IFCSpaceBoundaryType boundaryType = boundary.SpaceBoundaryType;
            IFCAnyHandle buildingElemHnd = IFCAnyHandle.Create();
            if (boundaryType == IFCSpaceBoundaryType.Physical)
            {
                buildingElemHnd = exporterIFC.FindSpaceBoundingElementHandle(boundary.BuildingElementId);
                if (!buildingElemHnd.HasValue)
                    return false;
            }

            file.CreateRelSpaceBoundary(IFCLabel.CreateGUID(), exporterIFC.GetOwnerHistoryHandle(), spaceBoundaryNameLabel, IFCLabel.Create(),
               spatialElemHnd, buildingElemHnd, boundary.GetConnectionGeometry(), boundaryType, boundary.IsExternal);

            return true;
        }

        static private bool Use2DRoomBoundaryForRoomVolumeCalculation()
        {
            String use2DRoomBoundary = Environment.GetEnvironmentVariable("Use2DRoomBoundaryForRoomVolumeCalculationOnIFCExport");
            return (use2DRoomBoundary != null && use2DRoomBoundary == "1");
        }

        /// <summary>
        /// Create IFC room/space/area item, not include boundaries. 
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="spatialElement">
        /// The spatial element.
        /// </param>
        /// <param name="productWrapper">
        /// The IFCProductWrapper.
        /// </param>
        /// <param name="setter">
        /// The IFCPlacementSetter.
        /// </param>
        /// <returns>
        /// True if created successfully, false otherwise.
        /// </returns>
        static void CreateIFCSpace(ExporterIFC exporterIFC, SpatialElement spatialElement, IFCProductWrapper productWrapper, IFCPlacementSetter setter)
        {
            Autodesk.Revit.DB.Document document = spatialElement.Document;
            ElementId levelId = spatialElement.Level != null ? spatialElement.Level.Id : ElementId.InvalidElementId;
            double scale = exporterIFC.LinearScale;

            ElementId catId = spatialElement.Category != null ? spatialElement.Category.Id : ElementId.InvalidElementId;

            double dArea = 0.0;

            Parameter param = spatialElement.get_Parameter(BuiltInParameter.ROOM_AREA);
            if (param != null)
            {
                dArea = param.AsDouble();
                dArea *= (scale * scale);
            }


            SpatialElementBoundaryOptions options = ExporterIFCUtils.GetSpatialElementBoundaryOptions(exporterIFC, spatialElement);
            IList<CurveLoop> curveLoops = ExporterIFCUtils.GetRoomBoundaryAsCurveLoopArray(spatialElement, options, true);

            IFCLevelInfo levelInfo = exporterIFC.GetLevelInfo(levelId);

            string strSpaceNumber = null;
            string strSpaceName = null;
            string strSpaceDesc = null;

            bool isArea = spatialElement is Area;
            if (!isArea)
            {
                Parameter paramRoomNum = spatialElement.get_Parameter(BuiltInParameter.ROOM_NUMBER);
                if (paramRoomNum != null)
                {
                    strSpaceNumber = paramRoomNum.AsString();
                }

                Parameter paramRoomName = spatialElement.get_Parameter(BuiltInParameter.ROOM_NAME);
                if (paramRoomName != null)
                {
                    strSpaceName = paramRoomName.AsString();
                }

                Parameter paramRoomComm = spatialElement.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                if (paramRoomComm != null)
                {
                    strSpaceDesc = paramRoomComm.AsString();
                }
            }
            else
            {
                Element level = document.get_Element(levelId);
                if (level != null)
                {
                    strSpaceNumber = level.Name + " GSA Design Gross Area";
                }
            }

            //assign empty string if it is null
            if (strSpaceNumber == null) strSpaceNumber = "";
            if (strSpaceName == null) strSpaceName = "";
            if (strSpaceDesc == null) strSpaceDesc = "";
            IFCLabel name = IFCLabel.Create(strSpaceNumber);
            IFCLabel longName = IFCLabel.Create(strSpaceName);
            IFCLabel desc = IFCLabel.Create(strSpaceDesc);

            IFCFile file = exporterIFC.GetFile();

            IFCAnyHandle localPlacement = setter.GetPlacement();
            ElementType elemType = document.get_Element(spatialElement.GetTypeId()) as ElementType;
            bool isObjectExternal = CategoryUtil.IsElementExternal(spatialElement);
            IFCMeasureValue elevationWithFlooring = IFCMeasureValue.Create();

            double roomHeight = 0.0;

            roomHeight = GetHeight(spatialElement, scale, levelInfo);

            double bottomOffset = 0.0;
            Parameter paramBottomOffset = spatialElement.get_Parameter(BuiltInParameter.ROOM_LOWER_OFFSET);
            bottomOffset = paramBottomOffset != null ? paramBottomOffset.AsDouble() : 0.0;

            XYZ zDir = new XYZ(0, 0, 1);
            XYZ orig = new XYZ(0, 0, levelInfo.Elevation + bottomOffset);

            Plane plane = new Plane(zDir, orig); // room calculated as level offset.

            GeometryElement geomElem = null;
            if (spatialElement is Autodesk.Revit.DB.Architecture.Room)
            {
                Autodesk.Revit.DB.Architecture.Room room = spatialElement as Autodesk.Revit.DB.Architecture.Room;
                geomElem = room.ClosedShell;
            }
            else if (spatialElement is Autodesk.Revit.DB.Mechanical.Space)
            {
                Autodesk.Revit.DB.Mechanical.Space space = spatialElement as Autodesk.Revit.DB.Mechanical.Space;
                geomElem = space.ClosedShell;
            }

            IFCAnyHandle spaceHnd = null;
            IFCExtrusionCreationData extraParams = new IFCExtrusionCreationData();
            extraParams.SetLocalPlacement(localPlacement);
            extraParams.PossibleExtrusionAxes = IFCExtrusionAxes.TryZ;

            using (IFCTransaction tr2 = new IFCTransaction(file))
            {
                IFCAnyHandle repHnd = null;
                if (!(exporterIFC.ExportAs2x2 || Use2DRoomBoundaryForRoomVolumeCalculation()) && geomElem != null)
                {

                    IFCSolidMeshGeometryInfo solidMeshInfo = ExporterIFCUtils.GetSolidMeshGeometry(exporterIFC, geomElem, Transform.Identity);
                    IList<Solid> solids = solidMeshInfo.GetSolids();
                    IList<Mesh> polyMeshes = solidMeshInfo.GetMeshes();

                    bool tryToExportAsExtrusion = true;
                    if (solids.Count != 1 || polyMeshes.Count != 0)
                        tryToExportAsExtrusion = false;

                    IList<GeometryObject> geomObjects = new List<GeometryObject>();

                    foreach (Solid solid in solids)
                        geomObjects.Add(solid);

                    IFCAnyHandle shapeRep = BodyExporter.ExportBody(spatialElement.Document.Application, exporterIFC, catId, geomObjects, tryToExportAsExtrusion, extraParams);
                    IList<IFCAnyHandle> shapeReps = new List<IFCAnyHandle>();
                    shapeReps.Add(shapeRep);
                    repHnd = file.CreateProductDefinitionShape(IFCLabel.Create(), IFCLabel.Create(), shapeReps);
                }
                else
                {
                    IFCAnyHandle shapeRep = file.CreateExtrudedSolidFromCurveLoop(exporterIFC, catId, curveLoops, plane, zDir, roomHeight); //pScaledOrig?
                    HashSet<IFCAnyHandle> bodyItems = new HashSet<IFCAnyHandle>();
                    bodyItems.Add(shapeRep);
                    shapeRep = RepresentationUtil.CreateSweptSolidRep(exporterIFC, catId, exporterIFC.Get3DContextHandle(), bodyItems, IFCAnyHandle.Create());
                    IList<IFCAnyHandle> shapeReps = new List<IFCAnyHandle>();
                    shapeReps.Add(shapeRep);
                    repHnd = file.CreateProductDefinitionShape(IFCLabel.Create(), IFCLabel.Create(), shapeReps);
                }

                extraParams.ScaledHeight = roomHeight;
                extraParams.ScaledArea = dArea;

                spaceHnd = file.CreateSpace(IFCLabel.CreateGUID(spatialElement),
                                                  exporterIFC.GetOwnerHistoryHandle(),
                                                  NamingUtil.GetNameOverride(spatialElement, name),
                                                  NamingUtil.GetDescriptionOverride(spatialElement, desc),
                                                  NamingUtil.GetObjectTypeOverride(spatialElement, IFCLabel.Create()),
                                                  extraParams.GetLocalPlacement(), repHnd, longName, IFCElementComposition.Element
                                                  , isObjectExternal, elevationWithFlooring);
                tr2.Commit();
            }

            productWrapper.AddSpace(spaceHnd, levelInfo, extraParams, true);

            // Save room handle for later use/relationships
            exporterIFC.RegisterSpatialElementHandle(spatialElement.Id, spaceHnd);

            if (!MathUtil.IsAlmostZero(dArea) && !(exporterIFC.FileVersion == IFCVersion.IFCCOBIE))
            {
                ExporterIFCUtils.CreatePreCOBIEGSAQuantities(exporterIFC, spaceHnd, "GSA Space Areas", (isArea ? "GSA Design Gross Area" : "GSA BIM Area"), dArea);
            }

            // Export BaseQuantities for RoomElem
            if (exporterIFC.ExportBaseQuantities && !(exporterIFC.FileVersion == IFCVersion.IFCCOBIE))
            {
                ExporterIFCUtils.CreateNonCOBIERoomQuantities(exporterIFC, spaceHnd, spatialElement, dArea, roomHeight);
            }
        }
    }
}
