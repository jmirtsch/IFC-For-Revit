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
using System.Linq;
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using BIM.IFC.Utility;

namespace BIM.IFC.Exporter
{
    /// <summary>
    /// Provides methods to export geometries to body representation.
    /// </summary>
    class BodyExporter
    {
        /// <summary>
        /// Sets best material id for current export state.
        /// </summary>
        /// <param name="geometryObject">The geometry object to get the best material id.</param>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        public static void SetBestMaterialIdInExporter(GeometryObject geometryObject, ExporterIFC exporterIFC)
        {
            ElementId materialId = GetBestMaterialIdForGeometry(geometryObject, exporterIFC);

            if (materialId != ElementId.InvalidElementId)
                exporterIFC.SetMaterialIdForCurrentExportState(materialId);
        }

        /// <summary>
        /// Gets the best material id for the geometry.
        /// </summary>
        /// <remarks>
        /// The best material ID for a list of solid and meshes is not invalid if all solids and meshes with an ID have the same one.
        /// </remarks>
        /// <param name="solids">List of solids.</param>
        /// <param name="meshes">List of meshes.</param>
        /// <returns>The material id.</returns>
        public static ElementId GetBestMaterialIdForGeometry(IList<Solid> solids, IList<Mesh> meshes)
        {
            ElementId bestMaterialId = ElementId.InvalidElementId;
            int numSolids = solids.Count;
            int numMeshes = meshes.Count;
            if (numSolids + numMeshes == 0)
                return bestMaterialId;

            int currentMesh = 0;
            for (; (currentMesh < numMeshes); currentMesh++)
            {
                ElementId currentMaterialId = meshes[currentMesh].MaterialElementId;
                if (currentMaterialId != ElementId.InvalidElementId)
                {
                    bestMaterialId = currentMaterialId;
                    break;
                }
            }

            int currentSolid = 0;
            if (bestMaterialId == ElementId.InvalidElementId)
            {
                for (; (currentSolid < numSolids); currentSolid++)
                {
                    if (solids[currentSolid].Faces.Size > 0)
                    {
                        bestMaterialId = GetBestMaterialIdForGeometry(solids[currentSolid], null);
                        break;
                    }
                }
            }

            if (bestMaterialId != ElementId.InvalidElementId)
            {
                for (currentMesh++; (currentMesh < numMeshes); currentMesh++)
                {
                    ElementId currentMaterialId = meshes[currentMesh].MaterialElementId;
                    if (currentMaterialId != ElementId.InvalidElementId && currentMaterialId != bestMaterialId)
                    {
                        bestMaterialId = ElementId.InvalidElementId;
                        break;
                    }
                }
            }

            if (bestMaterialId != ElementId.InvalidElementId)
            {
                for (currentSolid++; (currentSolid < numSolids); currentSolid++)
                {
                    if (solids[currentSolid].Faces.Size > 0)
                        continue;

                    ElementId currentMaterialId = GetBestMaterialIdForGeometry(solids[currentSolid], null);
                    if (currentMaterialId != ElementId.InvalidElementId && currentMaterialId != bestMaterialId)
                    {
                        bestMaterialId = ElementId.InvalidElementId;
                        break;
                    }
                }
            }

            return bestMaterialId;
        }

        /// <summary>
        /// Gets the best material id for the geometry.
        /// </summary>
        /// <param name="geometryElement">The geometry object to get the best material id.</param>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="range">The range to get the clipped geometry.</param>
        /// <returns>The material id.</returns>
        public static ElementId GetBestMaterialIdForGeometry(GeometryElement geometryElement,
           ExporterIFC exporterIFC, UV range)
        {

            IFCSolidMeshGeometryInfo solidMeshInfo;
            if (range == null)
                solidMeshInfo = ExporterIFCUtils.GetSolidMeshGeometry(exporterIFC, geometryElement, Transform.Identity);
            else
                solidMeshInfo = ExporterIFCUtils.GetClippedSolidMeshGeometry(exporterIFC, range, geometryElement);

            IList<Solid> solids = solidMeshInfo.GetSolids();
            IList<Mesh> polyMeshes = solidMeshInfo.GetMeshes();

            ElementId id = GetBestMaterialIdForGeometry(solids, polyMeshes);

            return id;
        }

        /// <summary>
        /// Gets the best material id for the geometry.
        /// </summary>
        /// <param name="geometryObject">The geometry object to get the best material id.</param>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <returns>The material id.</returns>
        public static ElementId GetBestMaterialIdForGeometry(GeometryObject geometryObject, ExporterIFC exporterIFC)
        {
            if (geometryObject is GeometryElement)
                return GetBestMaterialIdForGeometry(geometryObject as GeometryElement, exporterIFC, null);

            if (!(geometryObject is Solid))
                return ElementId.InvalidElementId;
            Solid solid = geometryObject as Solid;

            // We need to figure out the most common material id for the internal faces.
            // Other faces will override this.

            IDictionary<ElementId, int> countMap = new Dictionary<ElementId, int>();
            ElementId mostPopularId = ElementId.InvalidElementId;
            int numMostPopular = 0;

            foreach (Face face in solid.Faces)
            {
                if (face == null)
                    continue;

                ElementId currentMaterialId = face.MaterialElementId;
                if (currentMaterialId == ElementId.InvalidElementId)
                    continue;

                int currentCount = 0;
                if (countMap.ContainsKey(currentMaterialId))
                {
                    countMap[currentMaterialId]++;
                    currentCount = countMap[currentMaterialId];
                }
                else
                {
                    countMap[currentMaterialId] = 1;
                    currentCount = 1;
                }

                if (currentCount > numMostPopular)
                {
                    mostPopularId = currentMaterialId;
                    numMostPopular = currentCount;
                }
            }

            return mostPopularId;
        }

        /// <summary>
        /// Creates extruded solid from extrusion data.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="categoryId">The category id.</param>
        /// <param name="extrusionData">The extrusion data.</param>
        /// <returns>The IfcExtrudedAreaSolid handle.</returns>
        public static IFCAnyHandle CreateExtrudedSolidFromExtrusionData(ExporterIFC exporterIFC, ElementId categoryId,
           IFCExtrusionData extrusionData)
        {
            if (!extrusionData.IsValid())
                return IFCAnyHandle.Create();

            Plane plane = GeometryUtil.CreateDefaultPlane();

            IList<CurveLoop> extrusionLoops = extrusionData.GetLoops();
            if (extrusionLoops != null)
            {
                XYZ extrusionDir = extrusionData.ExtrusionDirection;
                double extrusionSize = extrusionData.ScaledExtrusionLength;

                if (ExporterIFCUtils.CorrectCurveLoopOrientation(extrusionLoops, plane, extrusionDir))
                {
                    IFCFile file = exporterIFC.GetFile();
                    IFCAnyHandle extrudedSolid = file.CreateExtrudedSolidFromCurveLoop(exporterIFC, categoryId,
                       extrusionLoops, plane, extrusionDir, extrusionSize);
                    if (!extrudedSolid.HasValue)
                        return IFCAnyHandle.Create();

                    return extrudedSolid;
                }
            }

            return IFCAnyHandle.Create();
        }

        /// <summary>
        /// Computes height and width of a curve loop with respect to the projection plane.
        /// </summary>
        /// <param name="curveLoop">The curve loop.</param>
        /// <param name="plane">The projection plane.</param>
        /// <param name="height">The height.</param>
        /// <param name="width">The width.</param>
        /// <returns>True if success, false if fail.</returns>
        public static bool ComputeHeightWidthOfCurveLoop(CurveLoop curveLoop, Plane plane, out double height, out double width)
        {
            height = 0.0;
            width = 0.0;

            Plane localPlane = plane;
            if (localPlane == null)
            {
                try
                {
                    localPlane = curveLoop.GetPlane();
                }
                catch
                {
                    return false;
                }
            }

            if (curveLoop.IsRectangular(localPlane))
            {
                height = curveLoop.GetRectangularHeight(localPlane);
                width = curveLoop.GetRectangularWidth(localPlane);
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Computes the outer length of curve loops.
        /// </summary>
        /// <param name="curveLoops">List of curve loops.</param>
        /// <returns>The length.</returns>
        public static double ComputeOuterPerimeterOfCurveLoops(IList<CurveLoop> curveLoops)
        {
            int numCurveLoops = curveLoops.Count;
            if (numCurveLoops == 0)
                return 0.0;

            if (curveLoops[0].IsOpen())
                return 0.0;

            return curveLoops[0].GetExactLength();
        }

        /// <summary>
        /// Computes the inner length of curve loops.
        /// </summary>
        /// <param name="curveLoops">List of curve loops.</param>
        /// <returns>The length.</returns>
        public static double ComputeInnerPerimeterOfCurveLoops(IList<CurveLoop> curveLoops)
        {
            double innerPerimeter = 0.0;

            int numCurveLoops = curveLoops.Count;
            if (numCurveLoops == 0)
                return 0.0;

            for (int ii = 1; ii < numCurveLoops; ii++)
            {
                if (curveLoops[ii].IsOpen())
                    return 0.0;
                innerPerimeter += curveLoops[ii].GetExactLength();
            }

            return innerPerimeter;
        }

        /// <summary>
        /// Checks if the faces can create a closed shell.
        /// </summary>
        /// <remarks>
        /// Limitation: This could let through an edge shared an even number of times greater than 2.
        /// </remarks>
        /// <param name="faceSet">The collection of face handles.</param>
        /// <returns>True if can, false if can't.</returns>
        public static bool CanCreateClosedShell(ICollection<IFCAnyHandle> faceSet)
        {
            int numFaces = faceSet.Count;
            if (numFaces < 4)
                return false;

            HashSet<KeyValuePair<int, int>> unmatchedEdges = new HashSet<KeyValuePair<int, int>>();
            bool someReversed = false;
            foreach (IFCAnyHandle face in faceSet)
            {
                if (!face.HasValue)
                    return false;

                HashSet<IFCAnyHandle> currFaceBounds = new HashSet<IFCAnyHandle>(IFCGeometryUtils.GetFaceBounds(face));
                foreach (IFCAnyHandle boundary in currFaceBounds)
                {
                    if (!boundary.HasValue)
                        return false;

                    bool reverse = !IFCGeometryUtils.BoundaryHasSameSense(boundary);
                    IList<IFCAnyHandle> points = IFCGeometryUtils.GetBoundaryPolygon(boundary);

                    int sizeOfBoundary = points.Count;
                    if (sizeOfBoundary < 3)
                        return false;

                    for (int ii = 0; ii < sizeOfBoundary; ii++)
                    {
                        int pt1 = points[ii].Id;
                        int pt2 = points[(ii + 1) % sizeOfBoundary].Id;

                        KeyValuePair<int, int> reverseEdge =
                           reverse ? new KeyValuePair<int, int>(pt1, pt2) : new KeyValuePair<int, int>(pt2, pt1);
                        if (unmatchedEdges.Contains(reverseEdge))
                        {
                            unmatchedEdges.Remove(reverseEdge);
                        }
                        else
                        {
                            KeyValuePair<int, int> currEdge =
                               reverse ? new KeyValuePair<int, int>(pt2, pt1) : new KeyValuePair<int, int>(pt1, pt2);
                            if (unmatchedEdges.Contains(currEdge))
                            {
                                unmatchedEdges.Remove(currEdge);
                                someReversed = true;
                            }
                            else
                            {
                                unmatchedEdges.Add(currEdge);
                            }
                        }
                    }
                }
            }

            bool allMatched = (unmatchedEdges.Count == 0);
            return (allMatched && !someReversed);
        }

        /// <summary>
        /// Deletes set of handles.
        /// </summary>
        /// <param name="handles">Set of handles.</param>
        public static void DeleteHandles(HashSet<IFCAnyHandle> handles)
        {
            foreach (IFCAnyHandle hnd in handles)
            {
                hnd.Delete();
            }
        }

        /// <summary>
        /// Adds a new opening to extrusion creation data from curve loop and extrusion data.
        /// </summary>
        /// <param name="creationData">The extrusion creation data.</param>
        /// <param name="from">The extrusion data.</param>
        /// <param name="curveLoop">The curve loop.</param>
        private static void AddOpeningData(IFCExtrusionCreationData creationData, IFCExtrusionData from, CurveLoop curveLoop)
        {
            List<CurveLoop> curveLoops = new List<CurveLoop>();
            curveLoops.Add(curveLoop);
            AddOpeningData(creationData, from, curveLoops);
        }

        /// <summary>
        /// Adds a new opening to extrusion creation data from extrusion data.
        /// </summary>
        /// <param name="creationData">The extrusion creation data.</param>
        /// <param name="from">The extrusion data.</param>
        private static void AddOpeningData(IFCExtrusionCreationData creationData, IFCExtrusionData from)
        {
            AddOpeningData(creationData, from, from.GetLoops());
        }

        /// <summary>
        /// Adds a new opening to extrusion creation data from curve loops and extrusion data.
        /// </summary>
        /// <param name="creationData">The extrusion creation data.</param>
        /// <param name="from">The extrusion data.</param>
        /// <param name="curveLoops">The curve loops.</param>
        private static void AddOpeningData(IFCExtrusionCreationData creationData, IFCExtrusionData from, ICollection<CurveLoop> curveLoops)
        {
            IFCExtrusionData newData = new IFCExtrusionData();
            foreach (CurveLoop curveLoop in curveLoops)
                newData.AddLoop(curveLoop);
            newData.ScaledExtrusionLength = from.ScaledExtrusionLength;
            newData.ExtrusionBasis = from.ExtrusionBasis;

            newData.ExtrusionDirection = from.ExtrusionDirection;
            creationData.AddOpening(newData);
        }

        /// <summary>
        /// Exports list of geometries to IFC body representation.
        /// </summary>
        /// <param name="application">The Revit application.</param>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="categoryId">The category id.</param>
        /// <param name="geometryList">The geometry list.</param>
        /// <param name="tryToExportAsExtrusion">True if try to export as extrusion.</param>
        /// <param name="exportBodyParams">The extrusion creation data.</param>
        /// <returns>The representation handle.</returns>
        public static IFCAnyHandle ExportBody(Autodesk.Revit.ApplicationServices.Application application, ExporterIFC exporterIFC, ElementId categoryId,
           IList<GeometryObject> geometryList, bool tryToExportAsExtrusion,
           IFCExtrusionCreationData exportBodyParams)
        {
            IFCAnyHandle body = IFCAnyHandle.Create();

            int sizeOfGeometry = geometryList.Count;
            if (sizeOfGeometry == 0)
                return body;

            IFCFile file = exporterIFC.GetFile();
            IFCAnyHandle contextOfItems = exporterIFC.Get3DContextHandle();
            double scale = exporterIFC.LinearScale;

            double eps = application.VertexTolerance * scale;

            HashSet<IFCAnyHandle> bodyItems = new HashSet<IFCAnyHandle>();
            if (tryToExportAsExtrusion)
            {
                // Check to see if we have Geometries or GFaces.
                // We will have the specific all GFaces case and then the generic case.
                IList<Face> faces = new List<Face>();
                bool allFaces = true;
                foreach (GeometryObject geometryObject in geometryList)
                {
                    if (geometryObject is Face)
                        faces.Add(geometryObject as Face);
                    else
                    {
                        allFaces = false;
                        break;
                    }
                }

                int numExtrusionsToCreate = allFaces ? 1 : sizeOfGeometry;

                try
                {
                    for (int ii = 0; ii < numExtrusionsToCreate && tryToExportAsExtrusion; ii++)
                    {
                        SetBestMaterialIdInExporter(geometryList[ii], exporterIFC);

                        IList<IFCExtrusionData> extrusionList = new List<IFCExtrusionData>();

                        IFCExtrusionAxes axesToExtrudeIn = exportBodyParams != null ? exportBodyParams.PossibleExtrusionAxes : IFCExtrusionAxes.TryDefault;
                        XYZ directionToExtrudeIn = XYZ.Zero;
                        if (exportBodyParams != null && exportBodyParams.HasCustomAxis)
                            directionToExtrudeIn = exportBodyParams.CustomAxis;

                        IFCExtrusionCalculatorOptions extrusionOptions =
                           new IFCExtrusionCalculatorOptions(exporterIFC, axesToExtrudeIn, directionToExtrudeIn,
                              scale);

                        bool canExportAsExtrusion = false;
                        if (allFaces)
                            extrusionList = IFCExtrusionCalculatorUtils.CalculateExtrusionData(extrusionOptions, faces);
                        else
                            extrusionList = IFCExtrusionCalculatorUtils.CalculateExtrusionData(extrusionOptions, geometryList[ii]);

                        if (extrusionList.Count > 0)
                        {
                            if (exportBodyParams != null && exportBodyParams.AreInnerRegionsOpenings)
                            {
                                IList<CurveLoop> curveLoops = extrusionList[0].GetLoops();
                                XYZ extrudedDirection = extrusionList[0].ExtrusionDirection;

                                int numLoops = curveLoops.Count;
                                for (int jj = numLoops - 1; jj > 0; jj--)
                                {
                                    AddOpeningData(exportBodyParams, extrusionList[0], curveLoops[jj]);
                                }
                                extrusionList[0].ClearLoops();
                            }

                            canExportAsExtrusion = false;
                            IFCAnyHandle extrusionHandle = CreateExtrudedSolidFromExtrusionData(exporterIFC, categoryId, extrusionList[0]);
                            if (extrusionHandle.HasValue)
                            {
                                bodyItems.Add(extrusionHandle);

                                IFCExtrusionBasis whichBasis = extrusionList[0].ExtrusionBasis;
                                IList<CurveLoop> curveLoops = extrusionList[0].GetLoops();
                                XYZ extrusionDirection = extrusionList[0].ExtrusionDirection;

                                canExportAsExtrusion = (whichBasis >= 0);

                                if (exportBodyParams != null)
                                {
                                    double zOff = (whichBasis == IFCExtrusionBasis.BasisZ) ? (1.0 - Math.Abs(extrusionDirection[2])) : Math.Abs(extrusionDirection[2]);
                                    double scaledAngle = Math.Asin(zOff) * 180 / Math.PI;
                                    exportBodyParams.Slope = scaledAngle;
                                    exportBodyParams.ScaledLength = extrusionList[0].ScaledExtrusionLength;
                                    exportBodyParams.CustomAxis = extrusionDirection;
                                    for (int kk = 1; kk < extrusionList.Count; kk++)
                                    {
                                        AddOpeningData(exportBodyParams, extrusionList[kk]);
                                    }

                                    Plane plane = null;
                                    double height = 0.0, width = 0.0;
                                    if (ComputeHeightWidthOfCurveLoop(curveLoops[0], plane, out height, out width))
                                    {
                                        exportBodyParams.ScaledHeight = height * scale;
                                        exportBodyParams.ScaledWidth = width * scale;
                                    }

                                    double area = ExporterIFCUtils.ComputeAreaOfCurveLoops(curveLoops);
                                    if (area > 0.0)
                                    {
                                        exportBodyParams.ScaledArea = area * scale * scale;
                                    }

                                    double innerPerimeter = ComputeInnerPerimeterOfCurveLoops(curveLoops);
                                    double outerPerimeter = ComputeOuterPerimeterOfCurveLoops(curveLoops);
                                    if (innerPerimeter > 0.0)
                                        exportBodyParams.ScaledInnerPerimeter = innerPerimeter * scale;
                                    if (outerPerimeter > 0.0)
                                        exportBodyParams.ScaledOuterPerimeter = outerPerimeter * scale;
                                }
                            }
                            else
                            {
                                if (exportBodyParams != null)
                                    exportBodyParams.ClearOpenings();
                            }
                        }
                        else
                        {
                            tryToExportAsExtrusion = false;
                        }
                    }
                }
                catch
                {
                    tryToExportAsExtrusion = false;
                }

                if (tryToExportAsExtrusion)
                {
                    body = RepresentationUtil.CreateSweptSolidRep(exporterIFC, categoryId, contextOfItems, bodyItems, body);
                    return body;
                }
            }

            // We couldn't export it as an extrusion; export as a faceted solid or a surface model.
            DeleteHandles(bodyItems);
            bodyItems.Clear();

            // generate "bottom corner" of bbox; create new local placement if passed in.
            // need to transform, but not scale, this point to make it the new origin.
            using (IFCTransformSetter transformSetter = IFCTransformSetter.Create())
            {
                if (exportBodyParams != null)
                    transformSetter.InitializeFromBoundingBox(exporterIFC, geometryList, exportBodyParams);

                HashSet<IFCAnyHandle> faceSet = new HashSet<IFCAnyHandle>(); // for export as surface.
                bool exportAsBReps = true;

                foreach (GeometryObject geometryObject in geometryList)
                {
                    SetBestMaterialIdInExporter(geometryObject, exporterIFC);

                    IFCGeometryInfo faceListInfo = IFCGeometryInfo.CreateFaceGeometryInfo(eps);
                    ExporterIFCUtils.CollectGeometryInfo(exporterIFC, faceListInfo, geometryObject, XYZ.Zero, false);

                    IList<ICollection<IFCAnyHandle>> faceSetList = faceListInfo.GetFaces();

                    int numBReps = faceSetList.Count;
                    if (numBReps == 0)
                        continue;

                    foreach (ICollection<IFCAnyHandle> currentFaceSet in faceSetList)
                    {
                        if (currentFaceSet.Count == 0)
                            continue;

                        if (exportAsBReps)
                        {
                            if ((currentFaceSet.Count < 4) || !CanCreateClosedShell(currentFaceSet))
                            {
                                // fix all previous bodyItems.
                                exportAsBReps = false;
                                foreach (IFCAnyHandle bodyItem in bodyItems)
                                {
                                    if (bodyItem.HasValue)
                                    {
                                        IFCAnyHandle closedShellHnd = IFCGeometryUtils.GetFaceOuterBoundary(bodyItem);
                                        if (closedShellHnd.HasValue)
                                        {
                                            ICollection<IFCAnyHandle> faces = IFCGeometryUtils.GetClosedShellFaces(closedShellHnd);
                                            IFCAnyHandle faceSetHnd = file.CreateConnectedFaceSet(faces);
                                            faceSet.Add(faceSetHnd); // only one item.
                                            closedShellHnd.Delete();
                                        }
                                        bodyItem.Delete();
                                    }
                                }
                                bodyItems.Clear();
                            }
                        }

                        if (exportAsBReps)
                        {
                            IFCAnyHandle faceOuter = file.CreateClosedShell(currentFaceSet);
                            IFCAnyHandle brepHnd = RepresentationUtil.CreateFacetedBRep(exporterIFC, faceOuter);
                            if (brepHnd.HasValue)
                                bodyItems.Add(brepHnd); // only one item.
                        }
                        else
                        {
                            // TODO: add layer assignment info.
                            IFCAnyHandle faceSetHnd = file.CreateConnectedFaceSet(currentFaceSet);
                            faceSet.Add(faceSetHnd); // only one item.
                        }
                    }
                }

                if (faceSet.Count > 0)
                {
                    IFCAnyHandle surfaceModel = file.CreateFaceBasedSurfaceModel(faceSet);
                    bodyItems.Add(surfaceModel); // only one item.
                }

                if (bodyItems.Count == 0)
                    return body;

                if (exportAsBReps)
                    body = RepresentationUtil.CreateBRepRep(exporterIFC, categoryId, contextOfItems, bodyItems);
                else
                    body = RepresentationUtil.CreateSurfaceRep(exporterIFC, categoryId, contextOfItems, bodyItems, false);

                return body;
            }
        }

        /// <summary>
        /// Exports list of solids and meshes to IFC body representation.
        /// </summary>
        /// <param name="application">The Revit application.</param>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="categoryId">The category id.</param>
        /// <param name="solids">The solids.</param>
        /// <param name="meshes">The meshes.</param>
        /// <param name="tryToExportAsExtrusion">True if try to export as extrusion.</param>
        /// <param name="exportBodyParams">The extrusion creation data.</param>
        /// <returns>The representation handle.</returns>
        public static IFCAnyHandle ExportBody(Autodesk.Revit.ApplicationServices.Application application, ExporterIFC exporterIFC, ElementId categoryId,
           IList<Solid> solids, IList<Mesh> meshes, bool tryToExportAsExtrusion,
           IFCExtrusionCreationData exportBodyParams)
        {
            IList<GeometryObject> objects = new List<GeometryObject>();
            foreach (Solid solid in solids)
                objects.Add(solid);
            foreach (Mesh mesh in meshes)
                objects.Add(mesh);
            return ExportBody(application, exporterIFC, categoryId, objects, tryToExportAsExtrusion, exportBodyParams);
        }
    }
}
