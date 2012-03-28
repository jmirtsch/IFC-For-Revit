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
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using BIM.IFC.Utility;
using BIM.IFC.Toolkit;

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
        public static ElementId SetBestMaterialIdInExporter(GeometryObject geometryObject, ExporterIFC exporterIFC)
        {
            ElementId materialId = GetBestMaterialIdForGeometry(geometryObject, exporterIFC);

            if (materialId != ElementId.InvalidElementId)
                exporterIFC.SetMaterialIdForCurrentExportState(materialId);

            return materialId;
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
           ExporterIFC exporterIFC, IFCRange range)
        {
            SolidMeshGeometryInfo solidMeshCapsule = null;

            if (range == null)
            {
                solidMeshCapsule = GeometryUtil.GetSolidMeshGeometry(geometryElement, Transform.Identity);
            }
            else
            {
                solidMeshCapsule = GeometryUtil.GetClippedSolidMeshGeometry(geometryElement, range);
            }

            IList<Solid> solids = solidMeshCapsule.GetSolids();
            IList<Mesh> polyMeshes = solidMeshCapsule.GetMeshes();

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
        /// Creates the related IfcSurfaceStyle for a representation item.
        /// </summary>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="document">The document.</param>
        /// <param name="repItemHnd">The representation item.</param>
        /// <param name="overrideMatId">The material id to use instead of the one in the exporter, if provided.</param>
        /// <returns>The handle.</returns>
        public static IFCAnyHandle CreateSurfaceStyleForRepItem(ExporterIFC exporterIFC, Document document, IFCAnyHandle repItemHnd, 
            ElementId overrideMatId)
        {
            if (repItemHnd == null || ExporterCacheManager.ExportOptionsCache.ExportAs2x2)
                return null;

            // Restrict material to proper subtypes.
            if (!IFCAnyHandleUtil.IsSubTypeOf(repItemHnd, IFCEntityType.IfcSolidModel) &&
                !IFCAnyHandleUtil.IsSubTypeOf(repItemHnd, IFCEntityType.IfcFaceBasedSurfaceModel) &&
                !IFCAnyHandleUtil.IsSubTypeOf(repItemHnd, IFCEntityType.IfcShellBasedSurfaceModel) &&
                !IFCAnyHandleUtil.IsSubTypeOf(repItemHnd, IFCEntityType.IfcSurface))
            {
                throw new InvalidOperationException("Attempting to set surface style for unknown item.");
            }

            IFCFile file = exporterIFC.GetFile();

            ElementId materialId = (overrideMatId != ElementId.InvalidElementId) ? overrideMatId : exporterIFC.GetMaterialIdForCurrentExportState();
            if (materialId == ElementId.InvalidElementId)
                return null;

            IFCAnyHandle presStyleHnd = ExporterCacheManager.PresentationStyleAssignmentCache.Find(materialId);
            if (presStyleHnd == null)
            {
                IFCAnyHandle surfStyleHnd = CategoryUtil.GetOrCreateMaterialStyle(document, exporterIFC, materialId);
                if (surfStyleHnd == null)
                    return null;

                ICollection<IFCAnyHandle> styles = new HashSet<IFCAnyHandle>();
                styles.Add(surfStyleHnd);

                presStyleHnd = IFCInstanceExporter.CreatePresentationStyleAssignment(file, styles);
                ExporterCacheManager.PresentationStyleAssignmentCache.Register(materialId, presStyleHnd);
            }

            HashSet<IFCAnyHandle> presStyleSet = new HashSet<IFCAnyHandle>();
            presStyleSet.Add(presStyleHnd);

            return IFCInstanceExporter.CreateStyledItem(file, repItemHnd, presStyleSet, null);
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
                if (IFCAnyHandleUtil.IsNullOrHasNoValue(face))
                    return false;

                HashSet<IFCAnyHandle> currFaceBounds = GeometryUtil.GetFaceBounds(face);
                foreach (IFCAnyHandle boundary in currFaceBounds)
                {
                    if (IFCAnyHandleUtil.IsNullOrHasNoValue(boundary))
                        return false;

                    bool reverse = !GeometryUtil.BoundaryHasSameSense(boundary);
                    IList<IFCAnyHandle> points = GeometryUtil.GetBoundaryPolygon(boundary);

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

        private static bool GatherMappedGeometryGroupings(IList<GeometryObject> geomList, 
            out IList<GeometryObject> newGeomList,
            out IDictionary<SolidMetrics, HashSet<Solid>> solidMappingGroups,
            out IList<KeyValuePair<int, Transform>> solidMappings)
        {
            bool useMappedGeometriesIfPossible = true;
            solidMappingGroups = new Dictionary<SolidMetrics, HashSet<Solid>>();
            solidMappings = new List<KeyValuePair<int, Transform>>();
            newGeomList = null;

            foreach (GeometryObject geometryObject in geomList)
            {
                Solid currSolid = geometryObject as Solid;
                SolidMetrics metrics = new SolidMetrics(currSolid);
                HashSet<Solid> currValues = null;
                if (solidMappingGroups.TryGetValue(metrics, out currValues))
                    currValues.Add(currSolid);
                else
                {
                    currValues = new HashSet<Solid>();
                    currValues.Add(currSolid);
                    solidMappingGroups[metrics] = currValues;
                }
            }

            useMappedGeometriesIfPossible = false;
            if (solidMappingGroups.Count != geomList.Count)
            {
                newGeomList = new List<GeometryObject>();
                int solidIndex = 0;
                foreach (KeyValuePair<SolidMetrics, HashSet<Solid>> solidKey in solidMappingGroups)
                {
                    Solid firstSolid = null;

                    // Check the rest of the list, to see if it matches the first item
                    foreach (Solid currSolid in solidKey.Value)
                    {
                        if (firstSolid == null)
                        {
                            firstSolid = currSolid;
                            newGeomList.Add(firstSolid);
                            solidIndex++;
                        }
                        else
                        {
                            Transform offsetTransform;
                            if (ExporterIFCUtils.AreSolidsEqual(firstSolid, currSolid, out offsetTransform))
                            {
                                useMappedGeometriesIfPossible = true;
                                solidMappings.Add(new KeyValuePair<int, Transform>(solidIndex - 1, offsetTransform));
                            }
                            else
                            {
                                newGeomList.Add(currSolid);
                                solidIndex++;
                            }
                        }
                    }
                }
            }
            return useMappedGeometriesIfPossible;
        }

        private static bool ProcessGroupMembership(ExporterIFC exporterIFC, IFCFile file, Element element, ElementId categoryId, IFCAnyHandle contextOfItems, 
            IList<GeometryObject> geomList, BodyData bodyDataIn,
            out BodyGroupKey groupKey, out BodyGroupData groupData, out BodyData bodyData)
        {
            // Set back to true if all checks are passed.
            bool useGroupsIfPossible = false;

            groupKey = null;
            groupData = null;
            bodyData = null;

            Group group = element.Group;
            if (group != null)
            {
                ElementId elementId = element.Id;

                bool pristineGeometry = true;
                foreach (GeometryObject geomObject in geomList)
                {
                    try
                    {
                        ICollection<ElementId> generatingElementIds = element.GetGeneratingElementIds(geomObject);
                        int numGeneratingElements = generatingElementIds.Count;
                        if ((numGeneratingElements > 1) || (numGeneratingElements == 1 && (generatingElementIds.First() != elementId)))
                        {
                            pristineGeometry = false;
                            break;
                        }
                    }
                    catch
                    {
                        pristineGeometry = false;
                        break;
                    }
                }

                if (pristineGeometry)
                {
                    groupKey = new BodyGroupKey();

                    IList<ElementId> groupMemberIds = group.GetMemberIds();
                    int numMembers = groupMemberIds.Count;
                    for (int idx = 0; idx < numMembers; idx++)
                    {
                        if (groupMemberIds[idx] == elementId)
                        {
                            groupKey.GroupMemberIndex = idx;
                            break;
                        }
                    }
                    if (groupKey.GroupMemberIndex >= 0)
                    {
                        groupKey.GroupTypeId = group.GetTypeId();

                        groupData = ExporterCacheManager.GroupElementGeometryCache.Find(groupKey);
                        if (groupData == null)
                        {
                            groupData = new BodyGroupData();
                            useGroupsIfPossible = true;
                        }
                        else
                        {
                            IList<IFCAnyHandle> groupBodyItems = new List<IFCAnyHandle>();
                            foreach (IFCAnyHandle mappedRepHnd in groupData.Handles)
                            {
                                IFCAnyHandle mappedItemHnd = ExporterUtil.CreateDefaultMappedItem(file, mappedRepHnd);
                                groupBodyItems.Add(mappedItemHnd);
                            }

                            bodyData = new BodyData(bodyDataIn);
                            bodyData.RepresentationHnd = RepresentationUtil.CreateBodyMappedItemRep(exporterIFC, element, categoryId, contextOfItems, groupBodyItems);
                            return true;
                        }
                    }
                }
            }
            return useGroupsIfPossible;
        }

        private static IFCAnyHandle CreateBRepRepresentationMap(ExporterIFC exporterIFC, IFCFile file, Element element, ElementId categoryId, 
            IFCAnyHandle contextOfItems, IFCAnyHandle brepHnd)
        {
            IList<IFCAnyHandle> currBodyItems = new List<IFCAnyHandle>();
            currBodyItems.Add(brepHnd);
            IFCAnyHandle currRepHnd = RepresentationUtil.CreateBRepRep(exporterIFC, element, categoryId,
                contextOfItems, currBodyItems);

            IFCAnyHandle currOrigin = ExporterUtil.CreateAxis2Placement3D(file);
            IFCAnyHandle currMappedRepHnd = IFCInstanceExporter.CreateRepresentationMap(file, currOrigin, currRepHnd);
            return currMappedRepHnd;
        }

        private static IFCAnyHandle CreateSurfaceRepresentationMap(ExporterIFC exporterIFC, IFCFile file, Element element, ElementId categoryId, 
            IFCAnyHandle contextOfItems, IFCAnyHandle faceSetHnd)
        {
            HashSet<IFCAnyHandle> currFaceSet = new HashSet<IFCAnyHandle>();
            currFaceSet.Add(faceSetHnd);

            IList<IFCAnyHandle> currFaceSetItems = new List<IFCAnyHandle>();
            IFCAnyHandle currSurfaceModelHnd = IFCInstanceExporter.CreateFaceBasedSurfaceModel(file, currFaceSet);
            currFaceSetItems.Add(currSurfaceModelHnd);
            IFCAnyHandle currRepHnd = RepresentationUtil.CreateSurfaceRep(exporterIFC, element, categoryId, contextOfItems,
                currFaceSetItems, false, null);

            IFCAnyHandle currOrigin = ExporterUtil.CreateAxis2Placement3D(file);
            IFCAnyHandle currMappedRepHnd = IFCInstanceExporter.CreateRepresentationMap(file, currOrigin, currRepHnd);
            return currMappedRepHnd;
        }

        private static BodyData ExportBodyAsBRep(ExporterIFC exporterIFC, IList<GeometryObject> splitGeometryList, Element element, 
            ElementId categoryId, IFCAnyHandle contextOfItems, double eps, BodyExporterOptions options, BodyData bodyDataIn)
        {
            bool exportAsBReps = true;
            IFCFile file = exporterIFC.GetFile();
            Document document = element.Document;

            bool useMappedGeometriesIfPossible = options.UseMappedGeometriesIfPossible;
            bool useGroupsIfPossible = options.UseGroupsIfPossible;

            IList<HashSet<IFCAnyHandle>> currentFaceHashSetList = new List<HashSet<IFCAnyHandle>>();
            IList<int> startIndexForObject = new List<int>();
            
            BodyData bodyData = new BodyData(bodyDataIn);

            IDictionary<SolidMetrics, HashSet<Solid>> solidMappingGroups = null;
            IList<KeyValuePair<int, Transform>> solidMappings = null;
            IList<ElementId> materialIds = new List<ElementId>();

            if (useMappedGeometriesIfPossible)
            {
                IList<GeometryObject> newGeometryList = null;
                useMappedGeometriesIfPossible = GatherMappedGeometryGroupings(splitGeometryList, out newGeometryList, out solidMappingGroups, out solidMappings);
                if (useMappedGeometriesIfPossible && (newGeometryList != null))
                    splitGeometryList = newGeometryList;
            }

            BodyGroupKey groupKey = null;
            BodyGroupData groupData = null;
            if (useGroupsIfPossible)
            {
                BodyData bodyDataOut = null;
                useGroupsIfPossible = ProcessGroupMembership(exporterIFC, file, element, categoryId, contextOfItems, splitGeometryList, bodyData,
                    out groupKey, out groupData, out bodyDataOut);
                if (bodyDataOut != null)
                    return bodyDataOut;
                if (useGroupsIfPossible)
                    useMappedGeometriesIfPossible = true;
            }

            bool isCoarse = (options.TessellationLevel == BodyExporterOptions.BodyTessellationLevel.Coarse);
            foreach (GeometryObject geomObject in splitGeometryList)
            {
                startIndexForObject.Add(currentFaceHashSetList.Count);
                ElementId materialId = SetBestMaterialIdInExporter(geomObject, exporterIFC);
                materialIds.Add(materialId);
                bodyData.AddMaterial(materialId);

                bool exportedAsSolid = false;
                if (exportAsBReps || isCoarse)
                {
                    try
                    {
                        if (geomObject is Solid)
                        {
                            Solid solid = geomObject as Solid;

                            SolidOrShellTessellationControls tessellationControls = options.TessellationControls;
                            
                            TriangulatedSolidOrShell solidFacetation =
                                SolidUtils.TessellateSolidOrShell(solid, tessellationControls);
                            if (solidFacetation.ShellComponentCount == 1)
                            {
                                TriangulatedShellComponent component = solidFacetation.GetShellComponent(0);
                                int numberOfTriangles = component.TriangleCount;
                                int numberOfVertices = component.VertexCount;
                                if (numberOfTriangles > 0 && numberOfVertices > 0)
                                {
                                    IList<IFCAnyHandle> vertexHandles = new List<IFCAnyHandle>();
                                    HashSet<IFCAnyHandle> currentFaceSet = new HashSet<IFCAnyHandle>();

                                    // create list of vertices first.
                                    for (int ii = 0; ii < numberOfVertices; ii++)
                                    {
                                        XYZ vertex = component.GetVertex(ii);
                                        XYZ vertexScaled = ExporterIFCUtils.TransformAndScalePoint(exporterIFC, vertex);
                                        IFCAnyHandle vertexHandle = ExporterUtil.CreateCartesianPoint(file, vertexScaled);
                                        vertexHandles.Add(vertexHandle);
                                    }

                                    try
                                    {
                                        TriangulationInterfaceForTriangulatedShellComponent componentInterface =
                                            new TriangulationInterfaceForTriangulatedShellComponent(component);
                                        IList<TriOrQuadFacet> facets = FacetingUtils.ConvertTrianglesToQuads(componentInterface);
                                        foreach (TriOrQuadFacet facet in facets)
                                        {
                                            IList<IFCAnyHandle> vertices = new List<IFCAnyHandle>();
                                            int numVertices = facet.NumberOfVertices;
                                            if (numVertices < 3)
                                                continue;
                                            for (int jj = 0; jj < facet.NumberOfVertices; jj++)
                                            {
                                                vertices.Add(vertexHandles[facet.GetVertexIndex(jj)]);
                                            }
                                            IFCAnyHandle faceOuterLoop = IFCInstanceExporter.CreatePolyLoop(file, vertices);
                                            IFCAnyHandle faceOuterBound = IFCInstanceExporter.CreateFaceOuterBound(file, faceOuterLoop, true);
                                            HashSet<IFCAnyHandle> faceBounds = new HashSet<IFCAnyHandle>();
                                            faceBounds.Add(faceOuterBound);
                                            IFCAnyHandle face = IFCInstanceExporter.CreateFace(file, faceBounds);
                                            currentFaceSet.Add(face);
                                        }
                                    }
                                    catch
                                    {
                                        for (int ii = 0; ii < numberOfTriangles; ii++)
                                        {
                                            TriangleInShellComponent triangle = component.GetTriangle(ii);
                                            IList<IFCAnyHandle> vertices = new List<IFCAnyHandle>();
                                            vertices.Add(vertexHandles[triangle.VertexIndex0]);
                                            vertices.Add(vertexHandles[triangle.VertexIndex1]);
                                            vertices.Add(vertexHandles[triangle.VertexIndex2]);
                                            IFCAnyHandle faceOuterLoop = IFCInstanceExporter.CreatePolyLoop(file, vertices);
                                            IFCAnyHandle faceOuterBound = IFCInstanceExporter.CreateFaceOuterBound(file, faceOuterLoop, true);
                                            HashSet<IFCAnyHandle> faceBounds = new HashSet<IFCAnyHandle>();
                                            faceBounds.Add(faceOuterBound);
                                            IFCAnyHandle face = IFCInstanceExporter.CreateFace(file, faceBounds);
                                            currentFaceSet.Add(face);
                                        }
                                    }
                                    finally
                                    {
                                        currentFaceHashSetList.Add(currentFaceSet);
                                        exportedAsSolid = true;
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        string errMsg = String.Format("TessellateSolidOrShell failed in IFC export for element \"{0}\" with id {1}", element.Name, element.Id);
                        document.Application.WriteJournalComment(errMsg, false/*timestamp*/);
                    }
                }
               
                if (!exportedAsSolid)
                {
                    IFCGeometryInfo faceListInfo = IFCGeometryInfo.CreateFaceGeometryInfo(eps, isCoarse);
                    ExporterIFCUtils.CollectGeometryInfo(exporterIFC, faceListInfo, geomObject, XYZ.Zero, false);

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
                                exportAsBReps = false;
                            }
                        }

                        currentFaceHashSetList.Add(new HashSet<IFCAnyHandle>(currentFaceSet));
                    }
                }
            }
            startIndexForObject.Add(currentFaceHashSetList.Count);  // end index for last object.

            IList<IFCAnyHandle> repMapItems = new List<IFCAnyHandle>();
            IList<IFCAnyHandle> bodyItems = new List<IFCAnyHandle>();

            int size = currentFaceHashSetList.Count;
            if (exportAsBReps)
            {
                int matToUse = -1;
                for (int ii = 0; ii < size; ii++)
                {
                    if (startIndexForObject[matToUse + 1] == ii)
                        matToUse++;
                    HashSet<IFCAnyHandle> currentFaceHashSet = currentFaceHashSetList[ii];

                    IFCAnyHandle faceOuter = IFCInstanceExporter.CreateClosedShell(file, currentFaceHashSet);
                    IFCAnyHandle brepHnd = RepresentationUtil.CreateFacetedBRep(exporterIFC, document, faceOuter, materialIds[matToUse]);
                    
                    if (!IFCAnyHandleUtil.IsNullOrHasNoValue(brepHnd))
                    {
                        if (useMappedGeometriesIfPossible)
                        {
                            IFCAnyHandle currMappedRepHnd = CreateBRepRepresentationMap(exporterIFC, file, element, categoryId, contextOfItems, brepHnd);
                            repMapItems.Add(currMappedRepHnd);
                            
                            IFCAnyHandle mappedItemHnd = ExporterUtil.CreateDefaultMappedItem(file, currMappedRepHnd);
                            bodyItems.Add(mappedItemHnd);
                        }
                        else
                            bodyItems.Add(brepHnd);
                    }
                }
            }
            else
            {
                IDictionary<ElementId, HashSet<IFCAnyHandle>> faceSets = new Dictionary<ElementId, HashSet<IFCAnyHandle>>();
                int matToUse = -1;
                for (int ii = 0; ii < size; ii++)
                {
                    HashSet<IFCAnyHandle> currentFaceHashSet = currentFaceHashSetList[ii];
                    if (startIndexForObject[matToUse+1] == ii)
                        matToUse++;

                    IFCAnyHandle faceSetHnd = IFCInstanceExporter.CreateConnectedFaceSet(file, currentFaceHashSet);
                    if (useMappedGeometriesIfPossible)
                    {
                        IFCAnyHandle currMappedRepHnd = CreateSurfaceRepresentationMap(exporterIFC, file, element, categoryId, contextOfItems, faceSetHnd);
                        repMapItems.Add(currMappedRepHnd);

                        IFCAnyHandle mappedItemHnd = ExporterUtil.CreateDefaultMappedItem(file, currMappedRepHnd);
                        bodyItems.Add(mappedItemHnd);
                    }
                    else
                    {
                        HashSet<IFCAnyHandle> surfaceSet = null;
                        if (faceSets.TryGetValue(materialIds[matToUse], out surfaceSet))
                        {
                            surfaceSet.Add(faceSetHnd);
                        }
                        else
                        {
                            surfaceSet = new HashSet<IFCAnyHandle>();
                            surfaceSet.Add(faceSetHnd);
                            faceSets[materialIds[matToUse]] = surfaceSet;
                        }
                    }
                }

                if (faceSets.Count > 0)
                {
                    foreach (KeyValuePair<ElementId, HashSet<IFCAnyHandle>> faceSet in faceSets)
                    {
                        IFCAnyHandle surfaceModel = IFCInstanceExporter.CreateFaceBasedSurfaceModel(file, faceSet.Value);
                        BodyExporter.CreateSurfaceStyleForRepItem(exporterIFC, document, surfaceModel, faceSet.Key);

                        bodyItems.Add(surfaceModel);
                    }
                }
            }

            if (bodyItems.Count == 0)
                return bodyData;

            // Add in mapped items.
            if (useMappedGeometriesIfPossible && (solidMappings != null))
            {
                foreach (KeyValuePair<int, Transform> mappedItem in solidMappings)
                {
                    for (int idx = startIndexForObject[mappedItem.Key]; idx < startIndexForObject[mappedItem.Key + 1]; idx++)
                    {
                        IFCAnyHandle mappedItemHnd = ExporterUtil.CreateMappedItemFromTransform(file, repMapItems[idx], mappedItem.Value);
                        bodyItems.Add(mappedItemHnd);
                    }
                }
            }

            if (useMappedGeometriesIfPossible)
                bodyData.RepresentationHnd = RepresentationUtil.CreateBodyMappedItemRep(exporterIFC, element, categoryId, contextOfItems, bodyItems);
            else if (exportAsBReps)
                bodyData.RepresentationHnd = RepresentationUtil.CreateBRepRep(exporterIFC, element, categoryId, contextOfItems, bodyItems);
            else
                bodyData.RepresentationHnd = RepresentationUtil.CreateSurfaceRep(exporterIFC, element, categoryId, contextOfItems, bodyItems, false, null);

            if (useGroupsIfPossible && (groupKey != null) && (groupData != null))
            {
                groupData.Handles = repMapItems;
                ExporterCacheManager.GroupElementGeometryCache.Register(groupKey, groupData);
            }

            return bodyData;
        }

        private class SolidMetrics
        {
            int m_NumEdges;
            int m_NumFaces;
            double m_SurfaceArea;
            double m_Volume;

            public SolidMetrics(Solid solid)
            {
                NumEdges = solid.Edges.Size;
                NumFaces = solid.Faces.Size;
                SurfaceArea = solid.SurfaceArea;
                Volume = solid.Volume;
            }

            public int NumEdges
            {
                get { return m_NumEdges; }
                set { m_NumEdges = value; }
            }

            public int NumFaces
            {
                get { return m_NumFaces; }
                set { m_NumFaces = value; }
            }

            public double SurfaceArea
            {
                get { return m_SurfaceArea; }
                set { m_SurfaceArea = value; }
            }

            public double Volume
            {
                get { return m_Volume; }
                set { m_Volume = value; }
            }

            static public bool operator ==(SolidMetrics first, SolidMetrics second)
            {
                Object lhsObject = first;
                Object rhsObject = second;
                if (null == lhsObject)
                {
                    if (null == rhsObject)
                        return true;
                    return false;
                }
                if (null == rhsObject)
                    return false;

                if (first.NumEdges != second.NumEdges)
                    return false;

                if (first.NumFaces != second.NumFaces)
                    return false;

                if (!MathUtil.IsAlmostEqual(first.SurfaceArea, second.SurfaceArea))
                {
                    return false;
                }

                if (!MathUtil.IsAlmostEqual(first.Volume, second.Volume))
                {
                    return false;
                }

                return true;
            }

            static public bool operator !=(SolidMetrics first, SolidMetrics second)
            {
                return !(first ==second);
            }

            public override bool Equals(object obj)
            {
                if (obj == null)
                    return false;

                SolidMetrics second = obj as SolidMetrics;
                return (this == second);
            }

            public override int GetHashCode()
            {
                double total = NumFaces + NumEdges + SurfaceArea + Volume;
                return (Math.Floor(total) * 100.0).GetHashCode();
            }
        }

        /// <summary>
        /// Exports list of geometries to IFC body representation.
        /// </summary>
        /// <param name="application">The Revit application.</param>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="categoryId">The category id.</param>
        /// <param name="geometryListIn">The geometry list.</param>
        /// <param name="options">The settings for how to export the body.</param>
        /// <param name="exportBodyParams">The extrusion creation data.</param>
        /// <returns>The BodyData containing the handle, offset and material ids.</returns>
        public static BodyData ExportBody(Autodesk.Revit.ApplicationServices.Application application, 
            ExporterIFC exporterIFC,
            Element element, 
            ElementId categoryId,
            IList<GeometryObject> geometryListIn,
            BodyExporterOptions options,
            IFCExtrusionCreationData exportBodyParams) 
        {
            BodyData bodyData = new BodyData();
            if (geometryListIn.Count == 0)
                return bodyData;

            Document document = element.Document;
            bool tryToExportAsExtrusion = options.TryToExportAsExtrusion;

            IFCFile file = exporterIFC.GetFile();
            IFCAnyHandle contextOfItems = exporterIFC.Get3DContextHandle("Body");
            double scale = exporterIFC.LinearScale;

            double eps = application.VertexTolerance * scale;

            IList<GeometryObject> splitGeometryList = new List<GeometryObject>();

            bool allFaces = true;
            foreach (GeometryObject geomObject in geometryListIn)
            {
                try
                {
                    bool split = false;
                    if (geomObject is Solid)
                    {
                        Solid solid = geomObject as Solid;
                        IList<Solid> splitVolumes = SolidUtils.SplitVolumes(solid);
                        allFaces = false;

                        if (splitVolumes != null && splitVolumes.Count != 0)
                        {
                            split = true;
                            foreach (Solid currSolid in splitVolumes)
                            {
                                splitGeometryList.Add(currSolid);
                                // The geometry element created by SplitVolumesis a copy which will have its own allocated
                                // membership - this needs to be stored and disposed of (see AllocatedGeometryObjectCache
                                // for details)
                                ExporterCacheManager.AllocatedGeometryObjectCache.AddGeometryObject(currSolid);
                            }
                        }
                    }
                    else if (allFaces && !(geomObject is Face))
                        allFaces = false;

                    if (!split)
                        splitGeometryList.Add(geomObject);
                }
                catch
                {
                    splitGeometryList.Add(geomObject);
                }
            }

            IList<IFCAnyHandle> bodyItems = new List<IFCAnyHandle>();
            IList<ElementId> materialIdsForExtrusions = new List<ElementId>();

            using (IFCTransaction tr = new IFCTransaction(file))
            {
                if (tryToExportAsExtrusion)
                {
                    // Check to see if we have Geometries or GFaces.
                    // We will have the specific all GFaces case and then the generic case.
                    IList<Face> faces = null;

                    if (allFaces)
                    {
                        faces = new List<Face>();
                        foreach (GeometryObject geometryObject in splitGeometryList)
                        {
                            faces.Add(geometryObject as Face);
                        }
                    }

                    int numExtrusionsToCreate = allFaces ? 1 : splitGeometryList.Count;

                    IList<IList<IFCExtrusionData>> extrusionLists = new List<IList<IFCExtrusionData>>();
                    for (int ii = 0; ii < numExtrusionsToCreate && tryToExportAsExtrusion; ii++)
                    {
                        IList<IFCExtrusionData> extrusionList = new List<IFCExtrusionData>();

                        IFCExtrusionAxes axesToExtrudeIn = exportBodyParams != null ? exportBodyParams.PossibleExtrusionAxes : IFCExtrusionAxes.TryDefault;
                        XYZ directionToExtrudeIn = XYZ.Zero;
                        if (exportBodyParams != null && exportBodyParams.HasCustomAxis)
                            directionToExtrudeIn = exportBodyParams.CustomAxis;

                        IFCExtrusionCalculatorOptions extrusionOptions =
                           new IFCExtrusionCalculatorOptions(exporterIFC, axesToExtrudeIn, directionToExtrudeIn, scale);

                        if (allFaces)
                            extrusionList = IFCExtrusionCalculatorUtils.CalculateExtrusionData(extrusionOptions, faces);
                        else
                            extrusionList = IFCExtrusionCalculatorUtils.CalculateExtrusionData(extrusionOptions, splitGeometryList[ii]);

                        if (extrusionList.Count == 0)
                            tryToExportAsExtrusion = false;
                        else
                            extrusionLists.Add(extrusionList);
                    }

                    for (int ii = 0; ii < numExtrusionsToCreate && tryToExportAsExtrusion; ii++)
                    {
                        bodyData.AddMaterial(SetBestMaterialIdInExporter(splitGeometryList[ii], exporterIFC));

                        if (exportBodyParams != null && exportBodyParams.AreInnerRegionsOpenings)
                        {
                            IList<CurveLoop> curveLoops = extrusionLists[ii][0].GetLoops();
                            XYZ extrudedDirection = extrusionLists[ii][0].ExtrusionDirection;

                            int numLoops = curveLoops.Count;
                            for (int jj = numLoops - 1; jj > 0; jj--)
                            {
                                ExtrusionExporter.AddOpeningData(exportBodyParams, extrusionLists[ii][0], curveLoops[jj]);
                                extrusionLists[ii][0].RemoveLoopAt(jj);
                            }
                        }

                        tryToExportAsExtrusion = false;
                        IFCAnyHandle extrusionHandle = ExtrusionExporter.CreateExtrudedSolidFromExtrusionData(exporterIFC, element, extrusionLists[ii][0]);
                        if (!IFCAnyHandleUtil.IsNullOrHasNoValue(extrusionHandle))
                        {
                            bodyItems.Add(extrusionHandle);
                            materialIdsForExtrusions.Add(exporterIFC.GetMaterialIdForCurrentExportState());

                            IFCExtrusionBasis whichBasis = extrusionLists[ii][0].ExtrusionBasis;
                            IList<CurveLoop> curveLoops = extrusionLists[ii][0].GetLoops();
                            XYZ extrusionDirection = extrusionLists[ii][0].ExtrusionDirection;

                            tryToExportAsExtrusion = (whichBasis >= 0);

                            if (exportBodyParams != null)
                            {
                                double zOff = (whichBasis == IFCExtrusionBasis.BasisZ) ? (1.0 - Math.Abs(extrusionDirection[2])) : Math.Abs(extrusionDirection[2]);
                                double scaledAngle = Math.Asin(zOff) * 180 / Math.PI;
                                exportBodyParams.Slope = scaledAngle;
                                exportBodyParams.ScaledLength = extrusionLists[ii][0].ScaledExtrusionLength;
                                exportBodyParams.ExtrusionDirection = extrusionDirection;
                                for (int kk = 1; kk < extrusionLists[ii].Count; kk++)
                                {
                                    ExtrusionExporter.AddOpeningData(exportBodyParams, extrusionLists[ii][kk]);
                                }

                                Plane plane = null;
                                double height = 0.0, width = 0.0;
                                if (ExtrusionExporter.ComputeHeightWidthOfCurveLoop(curveLoops[0], plane, out height, out width))
                                {
                                    exportBodyParams.ScaledHeight = height * scale;
                                    exportBodyParams.ScaledWidth = width * scale;
                                }

                                double area = ExporterIFCUtils.ComputeAreaOfCurveLoops(curveLoops);
                                if (area > 0.0)
                                {
                                    exportBodyParams.ScaledArea = area * scale * scale;
                                }

                                double innerPerimeter = ExtrusionExporter.ComputeInnerPerimeterOfCurveLoops(curveLoops);
                                double outerPerimeter = ExtrusionExporter.ComputeOuterPerimeterOfCurveLoops(curveLoops);
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
                }

                if (tryToExportAsExtrusion)
                {
                    int sz = bodyItems.Count();
                    for (int ii = 0; ii < sz; ii++)
                        BodyExporter.CreateSurfaceStyleForRepItem(exporterIFC, document, bodyItems[ii], materialIdsForExtrusions[ii]);

                    bodyData.RepresentationHnd =
                        RepresentationUtil.CreateSweptSolidRep(exporterIFC, element, categoryId, contextOfItems, bodyItems, bodyData.RepresentationHnd);

                    tr.Commit();
                    return bodyData;
                }

                tr.RollBack();
            }
            
            // We couldn't export it as an extrusion; export as a faceted solid or a surface model.
            bodyItems.Clear();

            // generate "bottom corner" of bbox; create new local placement if passed in.
            // need to transform, but not scale, this point to make it the new origin.
            using (IFCTransformSetter transformSetter = IFCTransformSetter.Create())
            {
                if (exportBodyParams != null)
                    bodyData.BrepOffsetTransform = transformSetter.InitializeFromBoundingBox(exporterIFC, splitGeometryList, exportBodyParams);

                return ExportBodyAsBRep(exporterIFC, splitGeometryList, element, categoryId, contextOfItems, eps, options, bodyData);
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
        /// <param name="options">The settings for how to export the body.</param>
        /// <param name="useMappedGeometriesIfPossible">If extrusions are not possible, and there is redundant geometry, 
        /// use a MappedRepresentation.</param>
        /// <param name="useGroupsIfPossible">If extrusions are not possible, and the element is part of a group, 
        /// use the cached version if it exists, or create it.</param>
        /// <param name="exportBodyParams">The extrusion creation data.</param>
        /// <returns>The body data.</returns>
        public static BodyData ExportBody(Autodesk.Revit.ApplicationServices.Application application, 
            ExporterIFC exporterIFC, 
            Element element, 
            ElementId categoryId,
            IList<Solid> solids, 
            IList<Mesh> meshes,
            BodyExporterOptions options,
            IFCExtrusionCreationData exportBodyParams)
        {
            IList<GeometryObject> objects = new List<GeometryObject>();
            foreach (Solid solid in solids)
                objects.Add(solid);
            foreach (Mesh mesh in meshes)
                objects.Add(mesh);

            return ExportBody(application, exporterIFC, element, categoryId, objects, options, exportBodyParams);
        }

        /// <summary>
        /// Exports a geometry object to IFC body representation.
        /// </summary>
        /// <param name="application">The Revit application.</param>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="categoryId">The category id.</param>
        /// <param name="geometryObject">The geometry object.</param>
        /// <param name="options">The settings for how to export the body.</param>
        /// <param name="exportBodyParams">The extrusion creation data.</param>
        /// <returns>The body data.</returns>
        public static BodyData ExportBody(Autodesk.Revit.ApplicationServices.Application application, ExporterIFC exporterIFC, 
           Element element, ElementId categoryId,
           GeometryObject geometryObject, BodyExporterOptions options,
           IFCExtrusionCreationData exportBodyParams)
        {
            IList<GeometryObject> geomList = new List<GeometryObject>();
            geomList.Add(geometryObject);
            return ExportBody(application, exporterIFC, element, categoryId, geomList, options, exportBodyParams);
        }

        /// <summary>
        /// Exports a geometry object to IFC body representation.
        /// </summary>
        /// <param name="application">The Revit application.</param>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="categoryId">The category id.</param>
        /// <param name="geometryElement">The geometry element.</param>
        /// <param name="options">The settings for how to export the body.</param>
        /// <param name="exportBodyParams">The extrusion creation data.</param>
        /// <param name="brepOffsetTransform">If the body is exported as a BRep or surface model, the transform used to shift it to the local origin.</param>
        /// <returns>The body data.</returns>
        public static BodyData ExportBody(Autodesk.Revit.ApplicationServices.Application application, ExporterIFC exporterIFC,
           Element element, ElementId categoryId,
           GeometryElement geometryElement, BodyExporterOptions options,
           IFCExtrusionCreationData exportBodyParams)
        {
            SolidMeshGeometryInfo info = null;
            IList<GeometryObject> geomList = new List<GeometryObject>();

            if (!exporterIFC.ExportAs2x2)
            {
                info = GeometryUtil.GetSolidMeshGeometry(geometryElement, Transform.Identity);
                IList<Mesh> meshes = info.GetMeshes();
                if (meshes.Count == 0)
                {
                    IList<Solid> solidList = info.GetSolids();
                    foreach (Solid solid in solidList)
                    {
                        geomList.Add(solid);
                    }
                }
            }

            if (geomList.Count == 0)
                geomList.Add(geometryElement);

            return ExportBody(application, exporterIFC, element, categoryId, geomList, 
                options, exportBodyParams);
        }
    }
}
