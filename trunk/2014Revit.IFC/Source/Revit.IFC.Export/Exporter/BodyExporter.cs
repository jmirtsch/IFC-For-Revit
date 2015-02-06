//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2013  Autodesk, Inc.
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
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Exporter
{
    /// <summary>
    /// Provides methods to export geometries to body representation.
    /// </summary>
    class BodyExporter
    {
        /// <summary>
        /// Get the tessellation level set by the default export options.
        /// Used by floors, railings, ramps, spaces and stairs.
        /// </summary>
        /// <returns></returns>
        public static BodyExporterOptions.BodyTessellationLevel GetTessellationLevel()
        {
            if (ExporterCacheManager.ExportOptionsCache.UseCoarseTessellation)
                return BodyExporterOptions.BodyTessellationLevel.Coarse;
            return BodyExporterOptions.BodyTessellationLevel.Default;
        }

        /// <summary>
        /// Sets best material id for current export state.
        /// </summary>
        /// <param name="geometryObject">The geometry object to get the best material id.</param>
        /// <param name="element">The element to get its structual material if no material found in its geometry.</param>
        /// <param name="overrideMaterialId">The material id to override the one gets from geometry object.</param>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <returns>The material id.</returns>
        public static ElementId SetBestMaterialIdInExporter(GeometryObject geometryObject, Element element, ElementId overrideMaterialId, ExporterIFC exporterIFC)
        {
            ElementId materialId = overrideMaterialId != ElementId.InvalidElementId ? overrideMaterialId :
                GetBestMaterialIdFromGeometryOrParameter(geometryObject, exporterIFC, element);

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

        private static ElementId GetBestMaterialIdFromParameter(Element element)
        {
            ElementId systemTypeId = ElementId.InvalidElementId;
            if (element is Duct)
                ParameterUtil.GetElementIdValueFromElement(element, BuiltInParameter.RBS_DUCT_SYSTEM_TYPE_PARAM, out systemTypeId);
            else if (element is Pipe)
                ParameterUtil.GetElementIdValueFromElement(element, BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM, out systemTypeId);

            ElementId matId = ElementId.InvalidElementId;
            if (systemTypeId != ElementId.InvalidElementId)
            {
                Element systemType = element.Document.GetElement(systemTypeId);
                if (systemType != null)
                    return GetBestMaterialIdFromParameter(systemType);
            }
            else if (element is DuctLining || element is MEPSystemType)
                ParameterUtil.GetElementIdValueFromElementOrSymbol(element, BuiltInParameter.MATERIAL_ID_PARAM, out matId);
            else
                ParameterUtil.GetElementIdValueFromElementOrSymbol(element, BuiltInParameter.STRUCTURAL_MATERIAL_PARAM, out matId);
            return matId;
        }
        
        /// <summary>
        /// Gets the best material id from the geometry or its structural material parameter.
        /// </summary>
        /// <param name="solids">List of solids.</param>
        /// <param name="meshes">List of meshes.</param>
        /// <param name="element">The element.</param>
        /// <returns>The material id.</returns>
        public static ElementId GetBestMaterialIdFromGeometryOrParameter(IList<Solid> solids, IList<Mesh> meshes, Element element)
        {
            ElementId matId = GetBestMaterialIdForGeometry(solids, meshes);
            if (matId == ElementId.InvalidElementId && element != null)
                matId = GetBestMaterialIdFromParameter(element);
            return matId;
        }

        /// <summary>
        /// Gets the best material id from the geometry or its structural material parameter.
        /// </summary>
        /// <param name="geometryElement">The geometry element.</param>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="range">The range to get the clipped geometry.</param>
        /// <param name="element">The element.</param>
        /// <returns>The material id.</returns>
        public static ElementId GetBestMaterialIdFromGeometryOrParameter(GeometryElement geometryElement, 
           ExporterIFC exporterIFC, IFCRange range, Element element)
        {
            ElementId matId = GetBestMaterialIdForGeometry(geometryElement, exporterIFC, range);
            if (matId == ElementId.InvalidElementId && element != null)
                matId = GetBestMaterialIdFromParameter(element);
            return matId;
        }

        /// <summary>
        /// Gets the best material id from the geometry or its structural material parameter.
        /// </summary>
        /// <param name="geometryObject">The geometry object.</param>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="element">The element.</param>
        /// <returns>The material id.</returns>
        public static ElementId GetBestMaterialIdFromGeometryOrParameter(GeometryObject geometryObject, ExporterIFC exporterIFC, Element element)
        {
            ElementId matId = GetBestMaterialIdForGeometry(geometryObject, exporterIFC);
            if (matId == ElementId.InvalidElementId && element != null)
                matId = GetBestMaterialIdFromParameter(element);
            return matId;
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
                !IFCAnyHandleUtil.IsSubTypeOf(repItemHnd, IFCEntityType.IfcSurface) &&
                !IFCAnyHandleUtil.IsSubTypeOf(repItemHnd, IFCEntityType.IfcTessellatedItem))
            {
                throw new InvalidOperationException("Attempting to set surface style for unknown item.");
            }

            IFCFile file = exporterIFC.GetFile();

            ElementId materialId = (overrideMatId != ElementId.InvalidElementId) ? overrideMatId : exporterIFC.GetMaterialIdForCurrentExportState();
            if (materialId == ElementId.InvalidElementId)
                return null;

            IFCAnyHandle presStyleHnd = ExporterCacheManager.PresentationStyleAssignmentCache.Find(materialId);
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(presStyleHnd))
            {
                IFCAnyHandle surfStyleHnd = CategoryUtil.GetOrCreateMaterialStyle(document, exporterIFC, materialId);
                if (IFCAnyHandleUtil.IsNullOrHasNoValue(surfStyleHnd))
                    return null;

                ISet<IFCAnyHandle> styles = new HashSet<IFCAnyHandle>();
                styles.Add(surfStyleHnd);

                presStyleHnd = IFCInstanceExporter.CreatePresentationStyleAssignment(file, styles);
                ExporterCacheManager.PresentationStyleAssignmentCache.Register(materialId, presStyleHnd);
            }

            HashSet<IFCAnyHandle> presStyleSet = new HashSet<IFCAnyHandle>();
            presStyleSet.Add(presStyleHnd);

            return IFCInstanceExporter.CreateStyledItem(file, repItemHnd, presStyleSet, null);
        }

        /// <summary>
        /// Creates the related IfcCurveStyle for a representation item.
        /// </summary>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="repItemHnd">The representation item.</param>
        /// <param name="curveWidth">The curve width.</param>
        /// <param name="colorHnd">The curve color handle.</param>
        /// <returns>The IfcCurveStyle handle.</returns>
        public static IFCAnyHandle CreateCurveStyleForRepItem(ExporterIFC exporterIFC, IFCAnyHandle repItemHnd, IFCData curveWidth, IFCAnyHandle colorHnd)
        {
            if (repItemHnd == null)
                return null;

            IFCAnyHandle presStyleHnd = null;
            IFCFile file = exporterIFC.GetFile();

            IFCAnyHandle curveStyleHnd = IFCInstanceExporter.CreateCurveStyle(file, null, null, curveWidth, colorHnd);
            ISet<IFCAnyHandle> styles = new HashSet<IFCAnyHandle>();
            styles.Add(curveStyleHnd);

            presStyleHnd = IFCInstanceExporter.CreatePresentationStyleAssignment(file, styles);
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
        public static bool CanCreateClosedShell(Mesh mesh)
        {
            int numFaces = mesh.NumTriangles;

            // Do simple checks first.
            if (numFaces < 4)
                return false;

            // Try to match up edges.
            IDictionary<uint, IList<uint>> unmatchedEdges = new Dictionary<uint, IList<uint>>();
            int unmatchedEdgeSz = 0;

            for (int ii = 0; ii < numFaces; ii++)
            {
                MeshTriangle meshTriangle = mesh.get_Triangle(ii);
                for (int jj = 0; jj < 3; jj++)
                {
                    uint pt1 = meshTriangle.get_Index(jj);
                    uint pt2 = meshTriangle.get_Index((jj + 1) % 3);

                    IList<uint> unmatchedEdgesPt2 = null;
                    if (unmatchedEdges.TryGetValue(pt2, out unmatchedEdgesPt2) && unmatchedEdgesPt2.Contains(pt1))
                    {
                        unmatchedEdgesPt2.Remove(pt1);
                        unmatchedEdgeSz--;
                    }
                    else
                    {
                        IList<uint> unmatchedEdgesPt1 = null;
                        if (unmatchedEdges.TryGetValue(pt1, out unmatchedEdgesPt1) && unmatchedEdgesPt1.Contains(pt2))
                        {
                            // An edge with the same orientation exists twice; can't create solid.
                            return false;
                        }

                        if (unmatchedEdgesPt1 == null)
                        {
                            unmatchedEdgesPt1 = new List<uint>();
                            unmatchedEdges[pt1] = unmatchedEdgesPt1;
                        }

                        unmatchedEdgesPt1.Add(pt2);
                        unmatchedEdgeSz++;
                    }
                }
            }

            return (unmatchedEdgeSz == 0);
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
            
            // Do simple checks first.
            if (numFaces < 4)
                return false;

            foreach (IFCAnyHandle face in faceSet)
            {
                if (IFCAnyHandleUtil.IsNullOrHasNoValue(face))
                    return false;
            }

            // Try to match up edges.
            IDictionary<int, IList<int>> unmatchedEdges = new Dictionary<int, IList<int>>();
            int unmatchedEdgeSz = 0;

            foreach (IFCAnyHandle face in faceSet)
            {
                HashSet<IFCAnyHandle> currFaceBounds = GeometryUtil.GetFaceBounds(face);
                foreach (IFCAnyHandle boundary in currFaceBounds)
                {
                    if (IFCAnyHandleUtil.IsNullOrHasNoValue(boundary))
                        return false;

                    IList<IFCAnyHandle> points = GeometryUtil.GetBoundaryPolygon(boundary);
                    int sizeOfBoundary = points.Count;
                    if (sizeOfBoundary < 3)
                        return false;

                    bool reverse = !GeometryUtil.BoundaryHasSameSense(boundary);
                    for (int ii = 0; ii < sizeOfBoundary; ii++)
                    {
                        int pt1 = reverse ? points[(ii + 1) % sizeOfBoundary].Id : points[ii].Id;
                        int pt2 = reverse ? points[ii].Id : points[(ii + 1) % sizeOfBoundary].Id;

                        IList<int> unmatchedEdgesPt2 = null;
                        if (unmatchedEdges.TryGetValue(pt2, out unmatchedEdgesPt2) && unmatchedEdgesPt2.Contains(pt1))
                        {
                            unmatchedEdgesPt2.Remove(pt1);
                            unmatchedEdgeSz--;
                        }
                        else
                        {
                            IList<int> unmatchedEdgesPt1 = null;
                            if (unmatchedEdges.TryGetValue(pt1, out unmatchedEdgesPt1) && unmatchedEdgesPt1.Contains(pt2))
                            {
                                // An edge with the same orientation exists twice; can't create solid.
                                return false;
                            }

                            if (unmatchedEdgesPt1 == null)
                            {
                                unmatchedEdgesPt1 = new List<int>();
                                unmatchedEdges[pt1] = unmatchedEdgesPt1;
                            }

                            unmatchedEdgesPt1.Add(pt2);
                            unmatchedEdgeSz++;
                        }
                    }
                }
            }

            return (unmatchedEdgeSz == 0);
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

            Document doc = element.Document;
            Group group = doc.GetElement(element.GroupId) as Group;
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
                            ISet<IFCAnyHandle> groupBodyItems = new HashSet<IFCAnyHandle>();
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
            ISet<IFCAnyHandle> currBodyItems = new HashSet<IFCAnyHandle>();
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

            ISet<IFCAnyHandle> currFaceSetItems = new HashSet<IFCAnyHandle>();
            IFCAnyHandle currSurfaceModelHnd = IFCInstanceExporter.CreateFaceBasedSurfaceModel(file, currFaceSet);
            currFaceSetItems.Add(currSurfaceModelHnd);
            IFCAnyHandle currRepHnd = RepresentationUtil.CreateSurfaceRep(exporterIFC, element, categoryId, contextOfItems,
                currFaceSetItems, false, null);

            IFCAnyHandle currOrigin = ExporterUtil.CreateAxis2Placement3D(file);
            IFCAnyHandle currMappedRepHnd = IFCInstanceExporter.CreateRepresentationMap(file, currOrigin, currRepHnd);
            return currMappedRepHnd;
        }

        // This is a simplified routine for solids that are composed of planar faces with polygonal edges.  This
        // allows us to use the edges as the boundaries of the faces.
        private static bool ExportPlanarBodyIfPossible(ExporterIFC exporterIFC, Solid solid,
            IList<HashSet<IFCAnyHandle>> currentFaceHashSetList)
        {
            IFCFile file = exporterIFC.GetFile();

            foreach (Face face in solid.Faces)
            {
                if (!(face is PlanarFace))
                    return false;
            }

            IList<IFCAnyHandle> vertexHandles = new List<IFCAnyHandle>();
            HashSet<IFCAnyHandle> currentFaceSet = new HashSet<IFCAnyHandle>();
            IDictionary<XYZ, IFCAnyHandle> vertexCache = new Dictionary<XYZ, IFCAnyHandle>(new GeometryUtil.XYZComparer());
            IDictionary<Edge, IList<IFCAnyHandle>> edgeCache = new Dictionary<Edge, IList<IFCAnyHandle>>();

            foreach (Face face in solid.Faces)
            {
                HashSet<IFCAnyHandle> faceBounds = new HashSet<IFCAnyHandle>();
                EdgeArrayArray edgeArrayArray = face.EdgeLoops;

                int edgeArraySize = edgeArrayArray.Size;
                IList<IList<IFCAnyHandle>> edgeArrayVertices = new List<IList<IFCAnyHandle>>();

                int outerEdgeArrayIndex = 0;
                double maxArea = 0.0;
                XYZ faceNormal = (face as PlanarFace).Normal;

                foreach (EdgeArray edgeArray in edgeArrayArray)
                {
                    IList<IFCAnyHandle> vertices = new List<IFCAnyHandle>();
                    IList<XYZ> vertexXYZs = new List<XYZ>();
                    
                    foreach (Edge edge in edgeArray)
                    {
                        IList<IFCAnyHandle> edgeVertices = null;
                        if (!edgeCache.TryGetValue(edge, out edgeVertices))
                        {
                            edgeVertices = new List<IFCAnyHandle>();
                            Curve curve = edge.AsCurveFollowingFace(face);

                            IList<XYZ> curvePoints = curve.Tessellate();
                            foreach (XYZ curvePoint in curvePoints)
                                vertexXYZs.Add(curvePoint);

                            int numPoints = curvePoints.Count;

                            // Don't add last point to vertives, as this will be added in the next edge, but we do want it
                            // in the vertex cache and the edge vertex cache.
                            for (int idx = 0; idx < numPoints; idx++)
                            {
                                IFCAnyHandle pointHandle = null;

                                if (!vertexCache.TryGetValue(curvePoints[idx], out pointHandle))
                                {
                                    XYZ pointScaled = ExporterIFCUtils.TransformAndScalePoint(exporterIFC, curvePoints[idx]);
                                    pointHandle = ExporterUtil.CreateCartesianPoint(file, pointScaled);
                                    vertexCache[curvePoints[idx]] = pointHandle;
                                    edgeVertices.Add(pointHandle);
                                }

                                if (idx != numPoints - 1)
                                    vertices.Add(pointHandle);
                            }
                        }
                        else
                        {
                            int numEdgePoints = edgeVertices.Count;
                            for (int idx = numEdgePoints - 1; idx > 0; idx--)
                            {
                                vertices.Add(edgeVertices[idx]);
                            }
                        }
                    }

                    double currArea = Math.Abs(GeometryUtil.ComputePolygonalLoopArea(vertexXYZs, faceNormal, vertexXYZs[0]));
                    if (currArea > maxArea)
                    {
                        outerEdgeArrayIndex = edgeArrayVertices.Count;
                        maxArea = currArea;
                    }

                    edgeArrayVertices.Add(vertices);
                }

                for (int ii = 0; ii < edgeArraySize; ii++)
                {
                    if (edgeArrayVertices[ii].Count < 3)
                        continue;

                    IFCAnyHandle faceLoop = IFCInstanceExporter.CreatePolyLoop(file, edgeArrayVertices[ii]);
                    IFCAnyHandle faceBound = (ii == outerEdgeArrayIndex) ?
                        IFCInstanceExporter.CreateFaceOuterBound(file, faceLoop, true) :
                        IFCInstanceExporter.CreateFaceBound(file, faceLoop, true);

                    faceBounds.Add(faceBound);
                }

                IFCAnyHandle currFace = IFCInstanceExporter.CreateFace(file, faceBounds);
                currentFaceSet.Add(currFace);
            }

            currentFaceHashSetList.Add(currentFaceSet);
            return true;
        }

        // This class allows us to merge points that are equal within a small tolerance.
        private class FuzzyPoint
        {
            XYZ m_Point;

            public FuzzyPoint(XYZ point)
            {
                m_Point = point;
            }

            public XYZ Point
            {
                get { return m_Point; }
                set { m_Point = value; }
            }

            static public bool operator ==(FuzzyPoint first, FuzzyPoint second)
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

                if (!first.Point.IsAlmostEqualTo(second.Point))
                    return false;

                return true;
            }

            static public bool operator !=(FuzzyPoint first, FuzzyPoint second)
            {
                return !(first ==second);
            }

            public override bool Equals(object obj)
            {
                if (obj == null)
                    return false;

                FuzzyPoint second = obj as FuzzyPoint;
                return (this == second);
            }

            public override int GetHashCode()
            {
                double total = Point.X + Point.Y + Point.Z;
                return Math.Floor(total * 10000.0 + 0.3142).GetHashCode();
            }
        }
        
        // This class allows us to merge Planes that have normals and origins that are equal within a small tolerance.
        private class PlanarKey
        {
            FuzzyPoint m_Norm;
            FuzzyPoint m_Origin;

            public PlanarKey(XYZ norm, XYZ origin)
            {
                m_Norm = new FuzzyPoint(norm);
                m_Origin = new FuzzyPoint(origin);
            }

            public XYZ Norm
            {
                get { return m_Norm.Point; }
                set { m_Norm.Point = value; }
            }

            public XYZ Origin
            {
                get { return m_Origin.Point; }
                set { m_Origin.Point = value; }
            }

            static public bool operator ==(PlanarKey first, PlanarKey second)
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

                if (first.m_Origin != second.m_Origin)
                    return false;

                if (first.m_Norm != second.m_Norm)
                    return false;

                return true;
            }

            static public bool operator !=(PlanarKey first, PlanarKey second)
            {
                return !(first ==second);
            }

            public override bool Equals(object obj)
            {
                if (obj == null)
                    return false;

                PlanarKey second = obj as PlanarKey;
                return (this == second);
            }

            public override int GetHashCode()
            {
                return m_Origin.GetHashCode() + m_Norm.GetHashCode();
            }
        }

        // This class contains a listing of the indices of the triangles on the plane, and some simple
        // connection information to speed up sewing.
        private class PlanarInfo
        {
            public IList<int> TriangleList = new List<int>();

            public Dictionary<int, HashSet<int>> TrianglesAtVertexList = new Dictionary<int, HashSet<int>>();

            public void AddTriangleIndexToVertexGrouping(int triangleIndex, int vertex)
            {
                HashSet<int> trianglesAtVertex;
                if (TrianglesAtVertexList.TryGetValue(vertex, out trianglesAtVertex))
                    trianglesAtVertex.Add(triangleIndex);
                else
                {
                    trianglesAtVertex = new HashSet<int>();
                    trianglesAtVertex.Add(triangleIndex);
                    TrianglesAtVertexList[vertex] = trianglesAtVertex;
                }
            }
        }

        // This routine is inefficient, so we will cap how much work we allow it to do.
        private static IList<LinkedList<int>> ConvertTrianglesToPlanarFacets(TriangulatedShellComponent component)
        {
            IList<LinkedList<int>> facets = new List<LinkedList<int>>();
            
            int numTriangles = component.TriangleCount;

            // sort triangles by normals.

            // This is a list of triangles whose planes are difficult to calculate, so we won't try to optimize them.
            IList<int> sliverTriangles = new List<int>();

            // PlanarKey allows for planes with almost equal normals and origins to be merged.
            Dictionary<PlanarKey, PlanarInfo> planarGroupings = new Dictionary<PlanarKey, PlanarInfo>();

            for (int ii = 0; ii < numTriangles; ii++)
            {
                TriangleInShellComponent currTriangle = component.GetTriangle(ii);

                // Normalize fails if the length is less than 1e-8 or so.  As such, normalilze the vectors
                // along the way to make sure the CrossProduct length isn't too small. 
                int vertex0 = currTriangle.VertexIndex0;
                int vertex1 = currTriangle.VertexIndex1;
                int vertex2 = currTriangle.VertexIndex2;

                XYZ pt1 = component.GetVertex(vertex0);
                XYZ pt2 = component.GetVertex(vertex1);
                XYZ pt3 = component.GetVertex(vertex2);
                XYZ norm = null;

                try
                {
                    XYZ xDir = (pt2 - pt1).Normalize();
                    norm = xDir.CrossProduct((pt3 - pt1).Normalize());
                    norm = norm.Normalize();
                }
                catch
                {
                    sliverTriangles.Add(ii);
                    continue;
                }

                double distToOrig = norm.DotProduct(pt1);
                XYZ origin = new XYZ(norm.X * distToOrig, norm.Y * distToOrig, norm.Z * distToOrig);

                // Go through map of existing planes and add triangle.
                PlanarInfo planarGrouping = null;
                
                PlanarKey currKey = new PlanarKey(norm, origin);
                if (planarGroupings.TryGetValue(currKey, out planarGrouping))
                {
                    planarGrouping.TriangleList.Add(ii);
                }
                else
                {
                    planarGrouping = new PlanarInfo();
                    planarGrouping.TriangleList.Add(ii);
                    planarGroupings[currKey] = planarGrouping;
                }

                planarGrouping.AddTriangleIndexToVertexGrouping(ii, vertex0);
                planarGrouping.AddTriangleIndexToVertexGrouping(ii, vertex1);
                planarGrouping.AddTriangleIndexToVertexGrouping(ii, vertex2);
            }

            foreach (PlanarInfo planarGroupingInfo in planarGroupings.Values)
            {
                IList<int> planarGrouping = planarGroupingInfo.TriangleList;

                HashSet<int> visitedTriangles = new HashSet<int>();
                int numCurrTriangles = planarGrouping.Count;

                for (int ii = 0; ii < numCurrTriangles; ii++)
                {
                    int idx = planarGrouping[ii];
                    if (visitedTriangles.Contains(idx))
                        continue;

                    TriangleInShellComponent currTriangle = component.GetTriangle(idx);

                    LinkedList<int> currFacet = new LinkedList<int>();
                    currFacet.AddLast(currTriangle.VertexIndex0);
                    currFacet.AddLast(currTriangle.VertexIndex1);
                    currFacet.AddLast(currTriangle.VertexIndex2);

                    // If only one triangle, a common case, add the facet and break out.
                    if (numCurrTriangles == 1)
                    {
                        facets.Add(currFacet);
                        break;
                    }

                    // If there are too many triangles, we won't try to be fancy until this routine is optimized.
                    if (numCurrTriangles > 150)
                    {
                        facets.Add(currFacet);
                        continue;
                    }
                    
                    HashSet<int> currFacetVertices = new HashSet<int>();
                    currFacetVertices.Add(currTriangle.VertexIndex0);
                    currFacetVertices.Add(currTriangle.VertexIndex1);
                    currFacetVertices.Add(currTriangle.VertexIndex2);

                    visitedTriangles.Add(idx);

                    bool foundTriangle;
                    do
                    {
                        foundTriangle = false;

                        // For each pair of adjacent vertices in the triangle, see if there is a triangle that shares that edge.
                        int sizeOfCurrBoundary = currFacet.Count;
                        foreach (int currVertexIndex in currFacet)
                        {
                            HashSet<int> trianglesAtCurrVertex = planarGroupingInfo.TrianglesAtVertexList[currVertexIndex];
                            foreach (int potentialNeighbor in trianglesAtCurrVertex)
                            {
                                if (visitedTriangles.Contains(potentialNeighbor))
                                    continue;

                                TriangleInShellComponent candidateTriangle = component.GetTriangle(potentialNeighbor);
                                int oldVertex = -1, newVertex = -1;

                                // Same normal, unvisited face - see if we have a matching edge.
                                if (currFacetVertices.Contains(candidateTriangle.VertexIndex0))
                                {
                                    if (currFacetVertices.Contains(candidateTriangle.VertexIndex1))
                                    {
                                        oldVertex = candidateTriangle.VertexIndex1;
                                        newVertex = candidateTriangle.VertexIndex2;
                                    }
                                    else if (currFacetVertices.Contains(candidateTriangle.VertexIndex2))
                                    {
                                        oldVertex = candidateTriangle.VertexIndex0;
                                        newVertex = candidateTriangle.VertexIndex1;
                                    }
                                }
                                else if (currFacetVertices.Contains(candidateTriangle.VertexIndex1))
                                {
                                    if (currFacetVertices.Contains(candidateTriangle.VertexIndex2))
                                    {
                                        oldVertex = candidateTriangle.VertexIndex2;
                                        newVertex = candidateTriangle.VertexIndex0;
                                    }
                                }

                                if (oldVertex == -1 || newVertex == -1)
                                    continue;

                                // Found a matching edge, insert it into the existing list.
                                LinkedListNode<int> newPosition = currFacet.Find(oldVertex);
                                currFacet.AddAfter(newPosition, newVertex);

                                foundTriangle = true;
                                visitedTriangles.Add(potentialNeighbor);
                                currFacetVertices.Add(newVertex);

                                break;
                            }

                            if (foundTriangle)
                                break;
                        }
                    } while (foundTriangle);

                    // Check the validity of the facets.  For now, if we have a duplicated vertex,
                    // revert to the original triangles.  TODO: split the facet into outer and inner
                    // loops and remove unnecessary edges.
                    if (currFacet.Count == currFacetVertices.Count)
                        facets.Add(currFacet);
                    else
                    {
                        foreach (int visitedIdx in visitedTriangles)
                        {
                            TriangleInShellComponent visitedTriangle = component.GetTriangle(visitedIdx);

                            LinkedList<int> visitedFacet = new LinkedList<int>();
                            visitedFacet.AddLast(visitedTriangle.VertexIndex0);
                            visitedFacet.AddLast(visitedTriangle.VertexIndex1);
                            visitedFacet.AddLast(visitedTriangle.VertexIndex2);

                            facets.Add(visitedFacet);
                        }
                    }
                }
            }

            // Add in slivery triangles.
            foreach (int sliverIdx in sliverTriangles)
            {
                TriangleInShellComponent currTriangle = component.GetTriangle(sliverIdx);

                LinkedList<int> currFacet = new LinkedList<int>();
                currFacet.AddLast(currTriangle.VertexIndex0);
                currFacet.AddLast(currTriangle.VertexIndex1);
                currFacet.AddLast(currTriangle.VertexIndex2);

                facets.Add(currFacet);
            }

            return facets;
        }

        /// <summary>
        /// Create an IfcFace with one outer loop whose vertices are defined by the vertices array.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="vertices">The vertices.</param>
        /// <returns>An IfcFace handle.</returns>
        public static IFCAnyHandle CreateFaceFromVertexList(IFCFile file, IList<IFCAnyHandle> vertices)
        {
            IFCAnyHandle faceOuterLoop = IFCInstanceExporter.CreatePolyLoop(file, vertices);
            IFCAnyHandle faceOuterBound = IFCInstanceExporter.CreateFaceOuterBound(file, faceOuterLoop, true);
            HashSet<IFCAnyHandle> faceBounds = new HashSet<IFCAnyHandle>();
            faceBounds.Add(faceOuterBound);
            return IFCInstanceExporter.CreateFace(file, faceBounds);
        }

        private static bool ExportPlanarFacetsIfPossible(IFCFile file, TriangulatedShellComponent component, IList<IFCAnyHandle> vertexHandles, HashSet<IFCAnyHandle> currentFaceSet)
        {
            IList<LinkedList<int>> facets = null;
            try
            {
                facets = ConvertTrianglesToPlanarFacets(component);
            }
            catch
            {
                return false;
            }

            if (facets == null)
                return false;

            foreach (LinkedList<int> facet in facets)
            {
                IList<IFCAnyHandle> vertices = new List<IFCAnyHandle>();
                int numVertices = facet.Count;
                if (numVertices < 3)
                    continue;
                foreach (int vertexIndex in facet)
                {
                    vertices.Add(vertexHandles[vertexIndex]);
                }

                IFCAnyHandle face = CreateFaceFromVertexList(file, vertices);
                currentFaceSet.Add(face);
            }

            return true;
        }

        private static IFCAnyHandle CreateEdgeCurveFromCurve(IFCFile file, ExporterIFC exporterIFC, Curve currCurve, IFCAnyHandle edgeStart, IFCAnyHandle edgeEnd, bool sameSense)
        {
            IFCAnyHandle baseCurve = null;

            // if the Curve is a line, do the following
            if (currCurve is Line)
            {
                // Unbounded line doesn't make sense, skip if somehow it is 
                if (!currCurve.IsBound)
                {
                    Line curveLine = currCurve as Line;
                    IFCAnyHandle point = XYZtoIfcCartesianPoint(exporterIFC, curveLine.GetEndPoint(0), true);
                    IList<double> direction = new List<double>();
                    direction.Add(UnitUtil.ScaleLength(curveLine.Direction.X));
                    direction.Add(UnitUtil.ScaleLength(curveLine.Direction.Y));
                    direction.Add(UnitUtil.ScaleLength(curveLine.Direction.Z));
                    IFCAnyHandle vector = IFCInstanceExporter.CreateVector(file, direction, curveLine.Direction.GetLength());
                    baseCurve = IFCInstanceExporter.CreateLine(file, point, vector);
                }
            }
            // if the Curve is an Arc do following
            else if (currCurve is Arc)
            {
                Arc curveArc = currCurve as Arc;
                IFCAnyHandle location = XYZtoIfcCartesianPoint2D(exporterIFC, curveArc.Center, true);
                IList<double> direction = new List<double>();
                direction.Add(UnitUtil.ScaleLength(curveArc.XDirection.X));
                direction.Add(UnitUtil.ScaleLength(curveArc.YDirection.Y));
                IFCAnyHandle dir = IFCInstanceExporter.CreateDirection(file, direction);
                IFCAnyHandle position = IFCInstanceExporter.CreateAxis2Placement2D(file, location, null, dir);
                baseCurve = IFCInstanceExporter.CreateCircle(file, position, UnitUtil.ScaleLength(curveArc.Radius));
            }
            // If curve is an ellipse or elliptical Arc type
            else if (currCurve is Ellipse)
            {
                Ellipse curveEllipse = currCurve as Ellipse;
                IFCAnyHandle location = XYZtoIfcCartesianPoint2D(exporterIFC, curveEllipse.Center, true);
                IList<double> direction = new List<double>();
                direction.Add(UnitUtil.ScaleLength(curveEllipse.XDirection.X));
                direction.Add(UnitUtil.ScaleLength(curveEllipse.YDirection.Y));
                IFCAnyHandle dir = IFCInstanceExporter.CreateDirection(file, direction);
                IFCAnyHandle position = IFCInstanceExporter.CreateAxis2Placement2D(file, location, null, dir);
                baseCurve = IFCInstanceExporter.CreateEllipse(file, position, UnitUtil.ScaleLength(curveEllipse.RadiusX), UnitUtil.ScaleLength(curveEllipse.RadiusY));
            }
            // if the Curve is of any other type, tessellate it and use polyline to represent it
            else
            {
                // any other curve is not supported, we will tessellate it
                IList<XYZ> tessCurve = currCurve.Tessellate();
                IList<IFCAnyHandle> polylineVertices = new List<IFCAnyHandle>();
                foreach (XYZ vertex in tessCurve)
                {
                    IFCAnyHandle ifcVert = XYZtoIfcCartesianPoint(exporterIFC, vertex, true);
                    polylineVertices.Add(ifcVert);
                }
                baseCurve = IFCInstanceExporter.CreatePolyline(file, polylineVertices);
            }

            if (IFCAnyHandleUtil.IsNullOrHasNoValue(baseCurve))
                return null;

            IFCAnyHandle edgeCurve = IFCInstanceExporter.CreateEdgeCurve(file, edgeStart, edgeEnd, baseCurve, sameSense);
            return edgeCurve;
        }

        /// <summary>
        /// Local implementation of the Revit 2015 API function in .NET for Revit 2014.
        /// </summary>
        /// <param name="face">The face.</param>
        /// <returns>The list of CurveLoops representing the ordered face edges.</returns>
        static private IList<CurveLoop> GetEdgesAsCurveLoops(Face face)
        {
            if (face == null)
                return null;

            IList<CurveLoop> edgesAsCurveLoops = new List<CurveLoop>();

            EdgeArrayArray edgeArrayArray = face.EdgeLoops;
            foreach (EdgeArray edgeArray in edgeArrayArray)
            {
                CurveLoop edgesAsCurveLoop = new CurveLoop();
                foreach (Edge edge in edgeArray)
                {
                    Curve curve = edge.AsCurveFollowingFace(face);
                    edgesAsCurveLoop.Append(curve);
                }
                edgesAsCurveLoops.Add(edgesAsCurveLoop);
            }

            return edgesAsCurveLoops;
        }
                        
        /// <summary>
        /// Returns a handle for creation of an AdvancedBrep with AdvancedFace and assigns it to the file
        /// </summary>
        /// <param name="exporterIFC">exporter IFC</param>
        /// <param name="element">the element</param>
        /// <param name="options">exporter option</param>
        /// <param name="geomObject">the geometry object</param>
        /// <returns>the handle</returns>
        public static IFCAnyHandle ExportBodyAsAdvancedBrep(ExporterIFC exporterIFC, Element element, BodyExporterOptions options,
            GeometryObject geomObject)
        {
            IFCFile file = exporterIFC.GetFile();
            Document document = element.Document;

            IFCAnyHandle advancedBrep = null;

            try
            {
                if (geomObject is Solid)
                {
                    IList<IFCAnyHandle> edgeLoopList = new List<IFCAnyHandle>();
                    HashSet<IFCAnyHandle> cfsFaces = new HashSet<IFCAnyHandle>();
                    Solid geomSolid = geomObject as Solid;
                    FaceArray faces = geomSolid.Faces;
                    foreach (Face face in faces)
                    {
                        IList<IFCAnyHandle> orientedEdgeList = new List<IFCAnyHandle>();
                        IFCAnyHandle surface = null;

                        // Use SortCurveLoops to collect the outerbound(s) with its list of innerbounds
                        IList<IList<CurveLoop>> curveloopList = ExporterIFCUtils.SortCurveLoops(GetEdgesAsCurveLoops(face));
                        IList<HashSet<IFCAnyHandle>> boundsCollection = new List<HashSet<IFCAnyHandle>>();

                        // loop for each outerloop (and its list of innerloops
                        foreach (IList<CurveLoop> curveloops in curveloopList)
                        {
                            foreach (CurveLoop curveloop in curveloops)
                            {
                                CurveLoopIterator curveloopIter = curveloop.GetCurveLoopIterator();
                                XYZ lastPoint = new XYZ();
                                bool first = true;
                                bool unbounded = false;
                                while (curveloopIter.MoveNext())
                                {
                                    IFCAnyHandle edgeCurve = null;
                                    bool orientation = true;
                                    bool sameSense = true;
                                    Curve currCurve = curveloopIter.Current;
                                    IFCAnyHandle edgeStart = null;
                                    IFCAnyHandle edgeEnd = null;

                                    if (currCurve.IsBound)
                                    {
                                        IFCAnyHandle edgeStartCP = XYZtoIfcCartesianPoint(exporterIFC, currCurve.GetEndPoint(0), true);
                                        edgeStart = IFCInstanceExporter.CreateVertexPoint(file, edgeStartCP);
                                        IFCAnyHandle edgeEndCP = XYZtoIfcCartesianPoint(exporterIFC, currCurve.GetEndPoint(1), true);
                                        edgeEnd = IFCInstanceExporter.CreateVertexPoint(file, edgeEndCP);
                                    }
                                    else
                                    {
                                        unbounded = true;
                                    }

                                    // Detect the sense direction by the continuity of the last point in the previous curve to the first point of the current curve
                                    if (!unbounded)
                                    {
                                        if (first)
                                        {
                                            lastPoint = currCurve.GetEndPoint(1);
                                            sameSense = true;
                                            first = false;
                                        }
                                        else
                                        {
                                            if (lastPoint.IsAlmostEqualTo(currCurve.GetEndPoint(1)))
                                            {
                                                sameSense = false;
                                                lastPoint = currCurve.GetEndPoint(0);
                                            }
                                            else
                                            {
                                                sameSense = true;
                                                lastPoint = currCurve.GetEndPoint(1);
                                            }
                                        }
                                    }

                                    // if the Curve is a line, do the following
                                    edgeCurve = CreateEdgeCurveFromCurve(file, exporterIFC, currCurve, edgeStart, edgeEnd, sameSense);
                                    if (IFCAnyHandleUtil.IsNullOrHasNoValue(edgeCurve))
                                        continue;

                                    IFCAnyHandle orientedEdge = IFCInstanceExporter.CreateOrientedEdge(file, edgeCurve, orientation);
                                    orientedEdgeList.Add(orientedEdge);
                                }

                                IFCAnyHandle edgeLoop = IFCInstanceExporter.CreateEdgeLoop(file, orientedEdgeList);
                                edgeLoopList.Add(edgeLoop);     // The list may contain outer edges and inner edges
                            }
                       
                            // Process the edgelooplist
                            bool orientationFB = true; //temp
                            HashSet<IFCAnyHandle> bounds = new HashSet<IFCAnyHandle>();
                            IFCAnyHandle faceOuterBound = IFCInstanceExporter.CreateFaceOuterBound(file, edgeLoopList[0], orientationFB);
                            bounds.Add(faceOuterBound);
                            if (edgeLoopList.Count > 1)
                            {
                                orientationFB = false; //temp
                                // Process inner bound
                                for (int ii = 1; ii < edgeLoopList.Count; ++ii)
                                {
                                    IFCAnyHandle faceBound = IFCInstanceExporter.CreateFaceBound(file, edgeLoopList[ii], orientationFB);
                                    bounds.Add(faceBound);
                                }
                            }
                            // We collect the list of Outerbounds (plus their innerbounds) here 
                            //    to create multiple AdvancedFaces from the same face if there are multiple faceouterbound
                            boundsCollection.Add(bounds);
                        }

                        // process the face now
                        if (face is PlanarFace)
                        {
                            PlanarFace plFace = face as PlanarFace;
                            XYZ plFaceNormal = plFace.Normal;

                            IFCAnyHandle location = XYZtoIfcCartesianPoint(exporterIFC, plFace.Origin, true);
                            IList<double> zaxis = new List<double>();
                            IList<double> refdir = new List<double>();
                            zaxis.Add(0.0);
                            zaxis.Add(0.0);
                            zaxis.Add(UnitUtil.ScaleLength(plFaceNormal.Z));
                            IFCAnyHandle axis = IFCInstanceExporter.CreateDirection(file, zaxis);
                            refdir.Add(UnitUtil.ScaleLength(plFaceNormal.X));
                            refdir.Add(0.0);
                            refdir.Add(0.0);
                            IFCAnyHandle refDirection = IFCInstanceExporter.CreateDirection(file, refdir);
                            IFCAnyHandle position = IFCInstanceExporter.CreateAxis2Placement3D(file, location, axis, refDirection);
                            surface = IFCInstanceExporter.CreatePlane(file, position);
                        }
                        else if (face is CylindricalFace)
                        {
                            CylindricalFace cylFace = face as CylindricalFace;
                            // get radius-x and radius-y and the position of the origin
                            XYZ rad = UnitUtil.ScaleLength(cylFace.get_Radius(0));
                            double radius = rad.GetLength();
                            IFCAnyHandle location = XYZtoIfcCartesianPoint(exporterIFC, cylFace.Origin, true);

                            IList<double> zaxis = new List<double>();
                            IList<double> refdir = new List<double>();
                            zaxis.Add(0.0);
                            zaxis.Add(0.0);
                            zaxis.Add(UnitUtil.ScaleLength(cylFace.Axis.Z));
                            IFCAnyHandle axis = IFCInstanceExporter.CreateDirection(file, zaxis);
                            refdir.Add(UnitUtil.ScaleLength(cylFace.Axis.X));
                            refdir.Add(0.0);
                            refdir.Add(0.0);
                            IFCAnyHandle refDirection = IFCInstanceExporter.CreateDirection(file, refdir);
                            IFCAnyHandle position = IFCInstanceExporter.CreateAxis2Placement3D(file, location, axis, refDirection);
                            surface = IFCInstanceExporter.CreateCylindricalSurface(file, position, radius);
                        }

                        else if (face is RevolvedFace)
                        {
                            RevolvedFace revFace = face as RevolvedFace;
                            IFCAnyHandle sweptCurve;

                            IList<double> zaxis = new List<double>();
                            IList<double> refdir = new List<double>();
                            zaxis.Add(0.0);
                            zaxis.Add(0.0);
                            zaxis.Add(UnitUtil.ScaleLength(revFace.Axis.Z));
                            IFCAnyHandle axis = IFCInstanceExporter.CreateDirection(file, zaxis);

                            refdir.Add(UnitUtil.ScaleLength(revFace.Axis.X));
                            refdir.Add(0.0);
                            refdir.Add(0.0);

                            IFCAnyHandle location = XYZtoIfcCartesianPoint(exporterIFC, revFace.Origin, true);

                            Curve curve = revFace.Curve;
                            // If the base curve is an Arc
                            if (curve is Arc)
                            {
                                Arc curveArc = curve as Arc;

                                IFCAnyHandle curveLoc = XYZtoIfcCartesianPoint2D(exporterIFC, curveArc.Center, true);
                                IList<double> direction = new List<double>();
                                direction.Add(UnitUtil.ScaleLength(curveArc.XDirection.X));
                                direction.Add(UnitUtil.ScaleLength(curveArc.YDirection.Y));
                                IFCAnyHandle dir = IFCInstanceExporter.CreateDirection(file, direction);
                                IFCAnyHandle position = IFCInstanceExporter.CreateAxis2Placement2D(file, curveLoc, null, dir);
                                IFCAnyHandle circle = IFCInstanceExporter.CreateCircle(file, position, UnitUtil.ScaleLength(curveArc.Radius));
                                bool unbounded = false;
                                IFCAnyHandle edgeStart = null;
                                IFCAnyHandle edgeEnd = null;
                                if (curve.IsBound)
                                {
                                    edgeStart = XYZtoIfcCartesianPoint(exporterIFC, curveArc.GetEndPoint(0), true);
                                    edgeEnd = XYZtoIfcCartesianPoint(exporterIFC, curveArc.GetEndPoint(1), true);
                                    if (curveArc.GetEndPoint(0).IsAlmostEqualTo(curveArc.GetEndPoint(1)))
                                        unbounded = true;
                                }
                                else
                                {
                                    unbounded = true;
                                }

                                IFCAnyHandle ifcCurve;
                                string name = "ArcCurveBaseProfile";
                                if (unbounded)
                                    sweptCurve = IFCInstanceExporter.CreateArbitraryClosedProfileDef(file, IFCProfileType.Curve, name, circle);
                                else
                                {
                                    IFCData trim1data = IFCData.CreateIFCAnyHandle(edgeStart);
                                    HashSet<IFCData> trim1 = new HashSet<IFCData>();
                                    trim1.Add(trim1data);
                                    IFCData trim2data = IFCData.CreateIFCAnyHandle(edgeEnd);
                                    HashSet<IFCData> trim2 = new HashSet<IFCData>();
                                    trim2.Add(trim2data);
                                    bool senseAgreement = true;
                                    ifcCurve = IFCInstanceExporter.CreateTrimmedCurve(file, circle, trim1, trim2, senseAgreement, IFCTrimmingPreference.Cartesian);
                                    sweptCurve = IFCInstanceExporter.CreateArbitraryClosedProfileDef(file, IFCProfileType.Curve, name, ifcCurve);
                                }

                            }
                            // If the base curve is an Ellipse
                            else if (curve is Ellipse)
                            {
                                Ellipse curveEllipse = curve as Ellipse;

                                IFCAnyHandle curveLoc = XYZtoIfcCartesianPoint2D(exporterIFC, curveEllipse.Center, true);
                                IList<double> direction = new List<double>();
                                direction.Add(UnitUtil.ScaleLength(curveEllipse.XDirection.X));
                                direction.Add(UnitUtil.ScaleLength(curveEllipse.YDirection.Y));
                                IFCAnyHandle dir = IFCInstanceExporter.CreateDirection(file, direction);
                                IFCAnyHandle position = IFCInstanceExporter.CreateAxis2Placement2D(file, curveLoc, null, dir);
                                IFCAnyHandle ellipse = IFCInstanceExporter.CreateEllipse(file, position, UnitUtil.ScaleLength(curveEllipse.RadiusX), UnitUtil.ScaleLength(curveEllipse.RadiusY));
                                bool unbounded = false;
                                IFCAnyHandle edgeStart = null;
                                IFCAnyHandle edgeEnd = null;
                                try
                                {
                                    edgeStart = XYZtoIfcCartesianPoint(exporterIFC, curveEllipse.GetEndPoint(0), true);
                                    edgeEnd = XYZtoIfcCartesianPoint(exporterIFC, curveEllipse.GetEndPoint(1), true);
                                    if (curveEllipse.GetEndPoint(0).IsAlmostEqualTo(curveEllipse.GetEndPoint(1)))
                                        unbounded = true;
                                }
                                catch (Exception e)
                                {
                                    if (e is Autodesk.Revit.Exceptions.ArgumentException)
                                    {
                                        unbounded = true;
                                    }
                                    else
                                        throw new ArgumentException("Edge Vertex");
                                }
                                IFCAnyHandle ifcCurve;
                                string name = "EllipseCurveBaseProfile";
                                if (unbounded)
                                    sweptCurve = IFCInstanceExporter.CreateArbitraryClosedProfileDef(file, IFCProfileType.Curve, name, ellipse);
                                else
                                {
                                    IFCData trim1data = IFCData.CreateIFCAnyHandle(edgeStart);
                                    HashSet<IFCData> trim1 = new HashSet<IFCData>();
                                    trim1.Add(trim1data);
                                    IFCData trim2data = IFCData.CreateIFCAnyHandle(edgeEnd);
                                    HashSet<IFCData> trim2 = new HashSet<IFCData>();
                                    trim2.Add(trim2data);
                                    bool senseAgreement = true;
                                    ifcCurve = IFCInstanceExporter.CreateTrimmedCurve(file, ellipse, trim1, trim2, senseAgreement, IFCTrimmingPreference.Cartesian);
                                    sweptCurve = IFCInstanceExporter.CreateArbitraryClosedProfileDef(file, IFCProfileType.Curve, name, ifcCurve);
                                }
                            }
                            // Any other type will be tessellated and is approximated using Polyline
                            else
                            {
                                IList<XYZ> tessCurve = curve.Tessellate();
                                IList<IFCAnyHandle> polylineVertices = new List<IFCAnyHandle>();
                                foreach (XYZ vertex in tessCurve)
                                {
                                    IFCAnyHandle ifcVert = XYZtoIfcCartesianPoint(exporterIFC, vertex, true);
                                    polylineVertices.Add(ifcVert);
                                }
                                string name = "TeseellatedBaseCurveProfile";
                                IFCAnyHandle polyLine = IFCInstanceExporter.CreatePolyline(file, polylineVertices);
                                sweptCurve = IFCInstanceExporter.CreateArbitraryClosedProfileDef(file, IFCProfileType.Curve, name, polyLine);
                            }

                            IFCAnyHandle axisPosition = IFCInstanceExporter.CreateAxis1Placement(file, location, axis);
                            surface = IFCInstanceExporter.CreateSurfaceOfRevolution(file, sweptCurve, null, axisPosition);
                        }
                        else if (face is ConicalFace)
                        {
                            ConicalFace conFace = face as ConicalFace;
                            return null;        // currently does not support this type of face
                        }
                        else if (face is RuledFace)
                        {
                            RuledFace ruledFace = face as RuledFace;
                            return null;        // currently does not support this type of face
                        }
                        else if (face is HermiteFace)
                        {
                            HermiteFace hermFace = face as HermiteFace;
                            return null;        // currently does not support this type of face
                        }

                        // create advancedFace
                        bool sameSenseAF = true; // temp
                        foreach (HashSet<IFCAnyHandle> theFaceBounds in boundsCollection)
                        {
                            IFCAnyHandle advancedFace = IFCInstanceExporter.CreateAdvancedFace(file, theFaceBounds, surface, sameSenseAF);
                            cfsFaces.Add(advancedFace);
                        }
                    }
                    // create advancedBrep
                    IFCAnyHandle closedShell = IFCInstanceExporter.CreateClosedShell(file, cfsFaces);
                    advancedBrep = IFCInstanceExporter.CreateAdvancedBrep(file, closedShell);
                }
                return advancedBrep;
            }
            catch
            {
                return null;
            }
        }

        private static IFCAnyHandle XYZtoIfcCartesianPoint(ExporterIFC exporterIFC, XYZ thePoint, bool applyUnitScale)
        {
            IFCFile file = exporterIFC.GetFile();
            XYZ vertexScaled = thePoint;
            if (applyUnitScale)
                vertexScaled = ExporterIFCUtils.TransformAndScalePoint(exporterIFC, thePoint);

            IList<double> coordPoint = new List<double>();
            coordPoint.Add(vertexScaled.X);
            coordPoint.Add(vertexScaled.Y);
            coordPoint.Add(vertexScaled.Z);
            IFCAnyHandle cartesianPoint = IFCInstanceExporter.CreateCartesianPoint(file, coordPoint);
            return cartesianPoint;
        }

        private static IFCAnyHandle XYZtoIfcCartesianPoint2D(ExporterIFC exporterIFC, XYZ thePoint, bool applyUnitScale)
        {
            IFCFile file = exporterIFC.GetFile();
            XYZ vertexScaled = thePoint;
            if (applyUnitScale)
                vertexScaled = ExporterIFCUtils.TransformAndScalePoint(exporterIFC, thePoint);

            IList<double> coordPoint = new List<double>();
            coordPoint.Add(vertexScaled.X);
            coordPoint.Add(vertexScaled.Y);
            IFCAnyHandle cartesianPoint = IFCInstanceExporter.CreateCartesianPoint(file, coordPoint);
            return cartesianPoint;
        }

        /// <summary>
        /// Export Geometry in IFC4 Triangulated tessellation
        /// </summary>
        /// <param name="exporterIFC">the exporter</param>
        /// <param name="element">the element</param>
        /// <param name="options">the options</param>
        /// <param name="geomObject">geometry objects</param>
        /// <returns>returns a handle</returns>
        public static IFCAnyHandle ExportBodyAsTriangulatedFaceSet(ExporterIFC exporterIFC, Element element, BodyExporterOptions options,
                    GeometryObject geomObject)
        {
            IFCFile file = exporterIFC.GetFile();
            Document document = element.Document;

            IFCAnyHandle triangulatedBody = null;

            if (geomObject is Solid)
            {
                try
                {
                    Solid solid = geomObject as Solid;

                    SolidOrShellTessellationControls tessellationControls = options.TessellationControls;
                    tessellationControls.LevelOfDetail = ExporterCacheManager.ExportOptionsCache.TessellationLevelOfDetail;

                    TriangulatedSolidOrShell solidFacetation =
                        SolidUtils.TessellateSolidOrShell(solid, tessellationControls);

                    // Only handle one solid or shell.
                    if (solidFacetation.ShellComponentCount == 1)
                    {
                        TriangulatedShellComponent component = solidFacetation.GetShellComponent(0);
                        int numberOfTriangles = component.TriangleCount;
                        int numberOfVertices = component.VertexCount;

                        // We are going to limit the number of triangles to 50,000.  This is a arbitrary number
                        // that should prevent the solid faceter from creating too many extra triangles to sew the surfaces.
                        // We may evaluate this number over time.
                        if ((numberOfTriangles > 0 && numberOfVertices > 0) && (numberOfTriangles < 50000))
                        {
                            List<List<double>> coordList = new List<List<double>>();
                            List<List<int>> coordIdx = new List<List<int>>();

                            // create list of vertices first.
                            for (int i = 0; i < numberOfVertices; i++)
                            {
                                List<double> vertCoord = new List<double>();

                                XYZ vertex = component.GetVertex(i);
                                XYZ vertexScaled = ExporterIFCUtils.TransformAndScalePoint(exporterIFC, vertex);
                                vertCoord.Add(vertexScaled.X);
                                vertCoord.Add(vertexScaled.Y);
                                vertCoord.Add(vertexScaled.Z);
                                coordList.Add(vertCoord);
                            }
                            // Create the entity IfcCartesianPointList3D from the List of List<double> and assign it to attribute Coordinates of IfcTriangulatedFaceSet

                            // Export all of the triangles
                            for (int i = 0; i < numberOfTriangles; i++)
                            {
                                List<int> vertIdx = new List<int>();

                                TriangleInShellComponent triangle = component.GetTriangle(i);
                                vertIdx.Add(triangle.VertexIndex0 + 1);     // IFC uses index that starts with 1 instead of 0 (following similar standard in X3D)
                                vertIdx.Add(triangle.VertexIndex1 + 1);
                                vertIdx.Add(triangle.VertexIndex2 + 1);
                                coordIdx.Add(vertIdx);
                            }

                            // Create attribute CoordIndex from the List of List<int> of the IfcTriangulatedFaceSet
                            
                            IFCAnyHandle coordPointLists = IFCAnyHandleUtil.CreateInstance(file, IFCEntityType.IfcCartesianPointList3D);
                            IFCAnyHandleUtil.SetAttribute(coordPointLists, "CoordList", coordList, 1, null, 3, 3);

                            triangulatedBody = IFCAnyHandleUtil.CreateInstance(file, IFCEntityType.IfcTriangulatedFaceSet);
                            IFCAnyHandleUtil.SetAttribute(triangulatedBody, "Coordinates", coordPointLists);
                            IFCAnyHandleUtil.SetAttribute(triangulatedBody, "CoordIndex", coordIdx, 1, null, 3, 3);
                        }
                    }
                }
                catch
                {
                    // Failed! Likely because of the tessellation failed. Try to create from the faceset instead
                    return ExportSurfaceAsTriangulatedFaceSet(exporterIFC, element, options, geomObject);
                }
            }
            else
            {
                // It is not from Solid, so we will use the faces to export. It works for Surface export too
                triangulatedBody = ExportSurfaceAsTriangulatedFaceSet(exporterIFC, element, options, geomObject);
            }
            return triangulatedBody;
        }

        /// <summary>
        /// Return a triangulated face set from the list of faces
        /// </summary>
        /// <param name="exporterIFC">exporter IFC</param>
        /// <param name="element">the element</param>
        /// <param name="options">the body export options</param>
        /// <param name="geomObject">the geometry object</param>
        /// <returns>returns the handle</returns>
        private static IFCAnyHandle ExportSurfaceAsTriangulatedFaceSet(ExporterIFC exporterIFC, Element element, BodyExporterOptions options,
                    GeometryObject geomObject)
        {
            IFCFile file = exporterIFC.GetFile();

            //IFCAnyHandle triangulatedBody = null;

            //bool isCoarse = (options.TessellationLevel == BodyExporterOptions.BodyTessellationLevel.Coarse);
            //double eps = UnitUtil.ScaleLength(element.Document.Application.VertexTolerance);

            List<List<XYZ>> triangleList = new List<List<XYZ>>();

            if (geomObject is Solid)
            {
                Solid geomSolid = geomObject as Solid;
                FaceArray faces = geomSolid.Faces;
                foreach (Face face in faces)
                {
                    double tessellationLevel = options.TessellationControls.LevelOfDetail;
                    Mesh faceTriangulation = face.Triangulate(tessellationLevel);
                    for (int i=0; i<faceTriangulation.NumTriangles; ++i)
                    {
                        List<XYZ> triangleVertices = new List<XYZ>();
                        MeshTriangle triangle = faceTriangulation.get_Triangle(i);
                        for (int tri=0; tri<3; ++tri)
                        {
                            XYZ vert = triangle.get_Vertex(tri);
                            triangleVertices.Add(vert);
                        }
                        triangleList.Add(triangleVertices);
                    }
                }
            }
            if (geomObject is Mesh)
            {
                Mesh geomMesh = geomObject as Mesh;
                for (int i = 0; i < geomMesh.NumTriangles; ++i)
                {
                    List<XYZ> triangleVertices = new List<XYZ>();
                    MeshTriangle triangle = geomMesh.get_Triangle(i);
                    for (int tri = 0; tri < 3; ++tri)
                    {
                        XYZ vert = triangle.get_Vertex(tri);
                        triangleVertices.Add(vert);
                    }
                    triangleList.Add(triangleVertices);
                }
            }
            return GeometryUtil.GetIndexedTriangles(file, triangleList);


            //// CollectGeometryInfo or faceListInfo.GetFaces call the native code and will create IfcFace instances. Currently this may result in orphaned IfcFace instances
            ////   in the IFC file. They are harmless, they only take up extra spaces
            //IFCGeometryInfo faceListInfo = IFCGeometryInfo.CreateFaceGeometryInfo(eps, isCoarse);
            //ExporterIFCUtils.CollectGeometryInfo(exporterIFC, faceListInfo, geomObject, XYZ.Zero, false);

            //IList<ICollection<IFCAnyHandle>> faceSetList = faceListInfo.GetFaces();
            //if (faceSetList.Count == 0)
            //    return null;
            //triangulatedBody = GeometryUtil.GetIndexedTriangles(file, faceSetList);
            //if (triangulatedBody == null)
            //    return null;   // something wrong, cannot get triangles, return null
            //return triangulatedBody;
        }

        private static bool ExportBodyAsSolid(ExporterIFC exporterIFC, Element element, BodyExporterOptions options,
            IList<HashSet<IFCAnyHandle>> currentFaceHashSetList, GeometryObject geomObject)
        {
            IFCFile file = exporterIFC.GetFile();
            Document document = element.Document;
            bool exportedAsSolid = false;


            try
            {
                if (geomObject is Solid)
                {
                    Solid solid = geomObject as Solid;
                    exportedAsSolid = ExportPlanarBodyIfPossible(exporterIFC, solid, currentFaceHashSetList);
                    if (exportedAsSolid)
                        return exportedAsSolid;

                    SolidOrShellTessellationControls tessellationControls = options.TessellationControls;

                    TriangulatedSolidOrShell solidFacetation =
                        SolidUtils.TessellateSolidOrShell(solid, tessellationControls);

                    // Only handle one solid or shell.
                    if (solidFacetation.ShellComponentCount == 1)
                    {
                        TriangulatedShellComponent component = solidFacetation.GetShellComponent(0);
                        int numberOfTriangles = component.TriangleCount;
                        int numberOfVertices = component.VertexCount;

                        // We are going to limit the number of triangles to 50,000.  This is a arbitrary number
                        // that should prevent the solid faceter from creating too many extra triangles to sew the surfaces.
                        // We may evaluate this number over time.
                        if ((numberOfTriangles > 0 && numberOfVertices > 0) && (numberOfTriangles < 50000))
                        {
                            IList<IFCAnyHandle> vertexHandles = new List<IFCAnyHandle>();
                            HashSet<IFCAnyHandle> currentFaceSet = new HashSet<IFCAnyHandle>();

                            if (ExporterUtil.IsReferenceView())
                            {
                                List<List<double>> coordList = new List<List<double>>();

                                // create list of vertices first.
                                for (int i = 0; i < numberOfVertices; i++)
                                {
                                    List<double> vertCoord = new List<double>();

                                    XYZ vertex = component.GetVertex(i);
                                    XYZ vertexScaled = ExporterIFCUtils.TransformAndScalePoint(exporterIFC, vertex);
                                    vertCoord.Add(vertexScaled.X);
                                    vertCoord.Add(vertexScaled.Y);
                                    vertCoord.Add(vertexScaled.Z);
                                    coordList.Add(vertCoord);
                                }

                            }
                            else
                            {
                                // create list of vertices first.
                                for (int ii = 0; ii < numberOfVertices; ii++)
                                {
                                    XYZ vertex = component.GetVertex(ii);
                                    XYZ vertexScaled = ExporterIFCUtils.TransformAndScalePoint(exporterIFC, vertex);
                                    IFCAnyHandle vertexHandle = ExporterUtil.CreateCartesianPoint(file, vertexScaled);
                                    vertexHandles.Add(vertexHandle);
                                }

                                if (!ExportPlanarFacetsIfPossible(file, component, vertexHandles, currentFaceSet))
                                {
                                    // Export all of the triangles instead.
                                    for (int ii = 0; ii < numberOfTriangles; ii++)
                                    {
                                        TriangleInShellComponent triangle = component.GetTriangle(ii);
                                        IList<IFCAnyHandle> vertices = new List<IFCAnyHandle>();
                                        vertices.Add(vertexHandles[triangle.VertexIndex0]);
                                        vertices.Add(vertexHandles[triangle.VertexIndex1]);
                                        vertices.Add(vertexHandles[triangle.VertexIndex2]);

                                        IFCAnyHandle face = CreateFaceFromVertexList(file, vertices);
                                        currentFaceSet.Add(face);
                                    }
                                }

                                currentFaceHashSetList.Add(currentFaceSet);
                                exportedAsSolid = true;
                            }
                        }
                    }
                }
                return exportedAsSolid;
            }
            catch
            {
                string errMsg = String.Format("TessellateSolidOrShell failed in IFC export for element \"{0}\" with id {1}", element.Name, element.Id);
                document.Application.WriteJournalComment(errMsg, false/*timestamp*/);
                return false;
            }
        }

        // NOTE: the useMappedGeometriesIfPossible and useGroupsIfPossible options are experimental and do not yet work well.
        // In shipped code, these are always false, and should be kept false until API support routines are proved to be reliable.
        private static BodyData ExportBodyAsBRep(ExporterIFC exporterIFC, IList<GeometryObject> splitGeometryList, 
            IList<KeyValuePair<int, SimpleSweptSolidAnalyzer>> exportAsBRep, IList<IFCAnyHandle> bodyItems,
            Element element, ElementId categoryId, ElementId overrideMaterialId, IFCAnyHandle contextOfItems, double eps, 
            BodyExporterOptions options, BodyData bodyDataIn)
        {
            bool exportAsBReps = true;
            bool hasTriangulatedGeometry = false;
            bool hasAdvancedBrepGeometry = false;
            IFCFile file = exporterIFC.GetFile();
            Document document = element.Document;

            // Can't use the optimization functions below if we already have partially populated our body items with extrusions.
            int numExtrusions = bodyItems.Count;
            bool useMappedGeometriesIfPossible = options.UseMappedGeometriesIfPossible && (numExtrusions != 0);
            bool useGroupsIfPossible = options.UseGroupsIfPossible && (numExtrusions != 0);

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
            
            int numBRepsToExport = exportAsBRep.Count;
            bool selectiveBRepExport = (numBRepsToExport > 0);
            int numGeoms = selectiveBRepExport ? numBRepsToExport : splitGeometryList.Count;
            
            for (int index = 0; index < numGeoms; index++)
            {
                int brepIndex = selectiveBRepExport ? exportAsBRep[index].Key : index;
                SimpleSweptSolidAnalyzer currAnalyzer = selectiveBRepExport ? exportAsBRep[index].Value : null;

                GeometryObject geomObject = selectiveBRepExport ? splitGeometryList[brepIndex] : splitGeometryList[index];
                
                // A simple test to see if the geometry is a valid solid.  This will save a lot of time in CanCreateClosedShell later.
                if (exportAsBReps && (geomObject is Solid))
                try
                {
                    // We don't care what the value is here.  What we care about is whether or not it can be calculated.  If it can't be calculated,
                    // it is probably not a valid solid.
                    double volume = (geomObject as Solid).Volume;

                    // Current code should already prevent 0 volume solids from coming here, but may as well play it safe.
                    if (volume <= MathUtil.Eps())
                        exportAsBReps = false;
                }
                catch
                {
                    exportAsBReps = false;
                }

                startIndexForObject.Add(currentFaceHashSetList.Count);

                ElementId materialId = SetBestMaterialIdInExporter(geomObject, element, overrideMaterialId, exporterIFC);
                materialIds.Add(materialId);
                bodyData.AddMaterial(materialId);

                bool alreadyExported = false;

                if (exportAsBReps && (currAnalyzer != null))
                {
                    SweptSolidExporter sweptSolidExporter = SweptSolidExporter.Create(exporterIFC, element, currAnalyzer, geomObject);
                    HashSet<IFCAnyHandle> facetHnds = (sweptSolidExporter != null) ? sweptSolidExporter.Facets : null;
                    if (facetHnds != null && facetHnds.Count != 0)
                    {
                        currentFaceHashSetList.Add(facetHnds);
                        alreadyExported = true;
                    }
                }

                if (!alreadyExported && ExporterUtil.IsReferenceView())
                {
                    IFCAnyHandle triangulatedBodyItem = ExportBodyAsTriangulatedFaceSet(exporterIFC, element, options, geomObject);
                    if (!IFCAnyHandleUtil.IsNullOrHasNoValue(triangulatedBodyItem))
                    {
                        bodyItems.Add(triangulatedBodyItem);
                        alreadyExported = true;
                        hasTriangulatedGeometry = true;
                    }
                }
                else if (!alreadyExported && ExporterUtil.IsDesignTransferView())
                {
                    IFCAnyHandle advancedBrepBodyItem = ExportBodyAsAdvancedBrep(exporterIFC, element, options, geomObject);
                    if (!IFCAnyHandleUtil.IsNullOrHasNoValue(advancedBrepBodyItem))
                    {
                        bodyItems.Add(advancedBrepBodyItem);
                        alreadyExported = true;
                        hasAdvancedBrepGeometry = true;
                    }
                }
                // If both the above options do not generate any body, do the traditional step for Brep
                if (!alreadyExported && (exportAsBReps || isCoarse))
                    alreadyExported = ExportBodyAsSolid(exporterIFC, element, options, currentFaceHashSetList, geomObject);
                
                if (!alreadyExported)
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
                            bool canExportAsClosedShell = (currentFaceSet.Count >= 4);
                            if (canExportAsClosedShell)
                            {
                                if ((geomObject is Mesh) && (numBReps == 1))
                                {
                                    // use optimized version.
                                    canExportAsClosedShell = CanCreateClosedShell(geomObject as Mesh);
                                }
                                else
                                {
                                    canExportAsClosedShell = CanCreateClosedShell(currentFaceSet);
                                }
                            }

                            if (!canExportAsClosedShell)
                            {
                                exportAsBReps = false;

                                // We'll need to invalidate the extrusions we created and replace them with BReps.
                                if (selectiveBRepExport && (numGeoms != splitGeometryList.Count))
                                {
                                    for (int fixIndex = 0; fixIndex < numExtrusions; fixIndex++)
                                    {
                                        bodyItems[0].Delete();
                                        bodyItems.RemoveAt(0);
                                    }
                                    numExtrusions = 0;
                                    numGeoms = splitGeometryList.Count;
                                    int currBRepIndex = 0;
                                    for (int fixIndex = 0; fixIndex < numGeoms; fixIndex++)
                                    {
                                        bool outOfRange = (currBRepIndex >= numBRepsToExport);
                                        if (!outOfRange && (exportAsBRep[currBRepIndex].Key == fixIndex))
                                        {
                                            currBRepIndex++;
                                            continue;
                                        }
                                        SimpleSweptSolidAnalyzer fixAnalyzer = outOfRange ? null : exportAsBRep[currBRepIndex].Value;
                                        exportAsBRep.Add(new KeyValuePair<int, SimpleSweptSolidAnalyzer>(fixIndex, fixAnalyzer));
                                    }
                                    numBRepsToExport = exportAsBRep.Count;
                                }
                            }
                        }

                        currentFaceHashSetList.Add(new HashSet<IFCAnyHandle>(currentFaceSet));
                    }
                }
            }

            if (ExporterUtil.IsReferenceView() && hasTriangulatedGeometry)
            {
                HashSet<IFCAnyHandle> bodyItemSet = new HashSet<IFCAnyHandle>();
                bodyItemSet.UnionWith(bodyItems);
                if (bodyItemSet.Count > 0)
                {
                    bodyData.RepresentationHnd = RepresentationUtil.CreateTessellatedRep(exporterIFC, element, categoryId, contextOfItems, bodyItemSet, null);
                    bodyData.ShapeRepresentationType = ShapeRepresentationType.Tessellation;
                }
            }
            else if (ExporterUtil.IsDesignTransferView() && hasAdvancedBrepGeometry)
            {
                HashSet<IFCAnyHandle> bodyItemSet = new HashSet<IFCAnyHandle>();
                bodyItemSet.UnionWith(bodyItems);
                if (bodyItemSet.Count > 0)
                {
                    bodyData.RepresentationHnd = RepresentationUtil.CreateAdvancedBRepRep(exporterIFC, element, categoryId, contextOfItems, bodyItemSet, null);
                    bodyData.ShapeRepresentationType = ShapeRepresentationType.AdvancedBrep;
                }
            }
            else
            {
                startIndexForObject.Add(currentFaceHashSetList.Count);  // end index for last object.

                IList<IFCAnyHandle> repMapItems = new List<IFCAnyHandle>();

                int size = currentFaceHashSetList.Count;
                if (exportAsBReps)
                {
                    int matToUse = -1;
                    for (int ii = 0; ii < size; ii++)
                    {
                        if (startIndexForObject[matToUse + 1] == ii)
                            matToUse++;
                        HashSet<IFCAnyHandle> currentFaceHashSet = currentFaceHashSetList[ii];
                        ElementId currMatId = materialIds[matToUse];

                        IFCAnyHandle faceOuter = IFCInstanceExporter.CreateClosedShell(file, currentFaceHashSet);
                        IFCAnyHandle brepHnd = RepresentationUtil.CreateFacetedBRep(exporterIFC, document, faceOuter, currMatId);

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
                        if (startIndexForObject[matToUse + 1] == ii)
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

                HashSet<IFCAnyHandle> bodyItemSet = new HashSet<IFCAnyHandle>();
                bodyItemSet.UnionWith(bodyItems);
                if (useMappedGeometriesIfPossible)
                {
                    bodyData.RepresentationHnd = RepresentationUtil.CreateBodyMappedItemRep(exporterIFC, element, categoryId, contextOfItems, bodyItemSet);
                }
                else if (exportAsBReps)
                {
                    if (numExtrusions > 0)
                        bodyData.RepresentationHnd = RepresentationUtil.CreateSolidModelRep(exporterIFC, element, categoryId, contextOfItems, bodyItemSet);
                    else
                        bodyData.RepresentationHnd = RepresentationUtil.CreateBRepRep(exporterIFC, element, categoryId, contextOfItems, bodyItemSet);
                }
                else
                    bodyData.RepresentationHnd = RepresentationUtil.CreateSurfaceRep(exporterIFC, element, categoryId, contextOfItems, bodyItemSet, false, null);

                if (useGroupsIfPossible && (groupKey != null) && (groupData != null))
                {
                    groupData.Handles = repMapItems;
                    ExporterCacheManager.GroupElementGeometryCache.Register(groupKey, groupData);
                }

                bodyData.ShapeRepresentationType = ShapeRepresentationType.Brep;
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
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="categoryId">The category id.</param>
        /// <param name="geometryListIn">The geometry list.</param>
        /// <param name="options">The settings for how to export the body.</param>
        /// <param name="exportBodyParams">The extrusion creation data.</param>
        /// <returns>The BodyData containing the handle, offset and material ids.</returns>
        public static BodyData ExportBody(ExporterIFC exporterIFC,
            Element element, 
            ElementId categoryId,
            ElementId overrideMaterialId,
            IList<GeometryObject> geometryList,
            BodyExporterOptions options,
            IFCExtrusionCreationData exportBodyParams) 
        {
            BodyData bodyData = new BodyData();
            if (geometryList.Count == 0)
                return bodyData;

            Document document = element.Document;
            bool tryToExportAsExtrusion = options.TryToExportAsExtrusion;
            bool canExportSolidModelRep = tryToExportAsExtrusion && ExporterCacheManager.ExportOptionsCache.CanExportSolidModelRep;
            
            bool useCoarseTessellation = ExporterCacheManager.ExportOptionsCache.UseCoarseTessellation;
            bool allowAdvancedBReps = ExporterCacheManager.ExportOptionsCache.ExportAs4 && !ExporterUtil.IsReferenceView();

            // We will try to export as a swept solid if the option is set, and we are either exporting to a schema that allows it,
            // or we are using a coarse tessellation, in which case we will export the swept solid as an optimzed BRep.
            bool tryToExportAsSweptSolid = options.TryToExportAsSweptSolid && (allowAdvancedBReps || useCoarseTessellation);

            // We will allow exporting swept solids as BReps or TriangulatedFaceSet if we are exporting to a schema before IFC4, or to a Reference View MVD, 
            // and we allow coarse representations.  In the future, we may allow more control here.
            // Note that we disable IFC4 because in IFC4, we will export it as a true swept solid instead, except for the Reference View MVD.
            bool tryToExportAsSweptSolidAsTessellation = tryToExportAsSweptSolid && useCoarseTessellation && !allowAdvancedBReps;

            IFCFile file = exporterIFC.GetFile();
            IFCAnyHandle contextOfItems = exporterIFC.Get3DContextHandle("Body");

            double eps = UnitUtil.ScaleLength(element.Document.Application.VertexTolerance);

            bool allFaces = true;
            foreach (GeometryObject geomObject in geometryList)
            {
                if (!(geomObject is Face))
                {
                    allFaces = false;
                    break;
                }
            }

            IList<IFCAnyHandle> bodyItems = new List<IFCAnyHandle>();
            IList<ElementId> materialIdsForExtrusions = new List<ElementId>();

            // This is a list of geometries that can be exported using the coarse facetation of the SweptSolidExporter.
            IList<KeyValuePair<int, SimpleSweptSolidAnalyzer>> exportAsBRep = new List<KeyValuePair<int, SimpleSweptSolidAnalyzer>>();
            
            IList<int> exportAsSweptSolid = new List<int>();
            IList<int> exportAsExtrusion = new List<int>();

            bool hasExtrusions = false;
            bool hasSweptSolids = false;
            bool hasSweptSolidsAsBReps = false;
            ShapeRepresentationType hasRepresentationType = ShapeRepresentationType.Undefined;

            BoundingBoxXYZ bbox = GeometryUtil.GetBBoxOfGeometries(geometryList);
            XYZ unscaledTrfOrig = new XYZ();

            int numItems = geometryList.Count;
            bool tryExtrusionAnalyzer = tryToExportAsExtrusion && (options.ExtrusionLocalCoordinateSystem != null) && (numItems == 1) && (geometryList[0] is Solid);
            bool supportOffsetTransformForExtrusions = !(tryExtrusionAnalyzer || tryToExportAsSweptSolidAsTessellation);
            bool useOffsetTransformForExtrusions = (options.AllowOffsetTransform && supportOffsetTransformForExtrusions && (exportBodyParams != null));

            using (IFCTransaction tr = new IFCTransaction(file))
            {
                // generate "bottom corner" of bbox; create new local placement if passed in.
                // need to transform, but not scale, this point to make it the new origin.
                using (TransformSetter transformSetter = TransformSetter.Create())
                {
                    if (useOffsetTransformForExtrusions)
                        bodyData.OffsetTransform = transformSetter.InitializeFromBoundingBox(exporterIFC, bbox, exportBodyParams, out unscaledTrfOrig);

                    // If we passed in an ExtrusionLocalCoordinateSystem, and we have 1 Solid, we will try to create an extrusion using the ExtrusionAnalyzer.
                    // If we succeed, we will skip the rest of the routine, otherwise we will try with the backup extrusion method.
                    // This doesn't yet create fallback information for solid models that are hybrid extrusions and BReps.
                    if (tryToExportAsExtrusion)
                    {
                        if (tryExtrusionAnalyzer)
                        {
                            using (IFCTransaction extrusionTransaction = new IFCTransaction(file))
                            {
                                Plane extrusionPlane = new Plane(options.ExtrusionLocalCoordinateSystem.BasisY,
                                        options.ExtrusionLocalCoordinateSystem.BasisZ,
                                        options.ExtrusionLocalCoordinateSystem.Origin);
                                XYZ extrusionDirection = options.ExtrusionLocalCoordinateSystem.BasisX;

                                bool completelyClipped;
                                HandleAndData extrusionData = ExtrusionExporter.CreateExtrusionWithClippingAndProperties(exporterIFC, element,
                                    CategoryUtil.GetSafeCategoryId(element), geometryList[0] as Solid, extrusionPlane,
                                    extrusionDirection, null, out completelyClipped);
                                if (!completelyClipped && !IFCAnyHandleUtil.IsNullOrHasNoValue(extrusionData.Handle) && extrusionData.BaseExtrusions != null && extrusionData.BaseExtrusions.Count == 1)
                                {
                                    HashSet<ElementId> materialIds = extrusionData.MaterialIds;

                                    // We skip setting and getting the material id from the exporter as unnecessary.
                                    ElementId matIdFromGeom = (materialIds != null && materialIds.Count > 0) ? materialIds.First() : ElementId.InvalidElementId;
                                    ElementId matId = (overrideMaterialId != ElementId.InvalidElementId) ? overrideMaterialId : matIdFromGeom;

                                    bodyItems.Add(extrusionData.BaseExtrusions[0]);
                                    materialIdsForExtrusions.Add(matId);
                                    if (matId != ElementId.InvalidElementId)
                                        bodyData.AddMaterial(matId);
                                    bodyData.RepresentationHnd = extrusionData.Handle;
                                    bodyData.ShapeRepresentationType = extrusionData.ShapeRepresentationType;

                                    if (exportBodyParams != null && extrusionData.Data != null)
                                    {
                                        exportBodyParams.Slope = extrusionData.Data.Slope;
                                        exportBodyParams.ScaledLength = extrusionData.Data.ScaledLength;
                                        exportBodyParams.ExtrusionDirection = extrusionData.Data.ExtrusionDirection;

                                        exportBodyParams.ScaledHeight = extrusionData.Data.ScaledHeight;
                                        exportBodyParams.ScaledWidth = extrusionData.Data.ScaledWidth;

                                        exportBodyParams.ScaledArea = extrusionData.Data.ScaledArea;
                                        exportBodyParams.ScaledInnerPerimeter = extrusionData.Data.ScaledInnerPerimeter;
                                        exportBodyParams.ScaledOuterPerimeter = extrusionData.Data.ScaledOuterPerimeter;
                                    }

                                    hasExtrusions = true;
                                    extrusionTransaction.Commit();
                                }
                                else
                                {
                                    extrusionTransaction.RollBack();
                                }
                            }
                        }

                        // Only try if ExtrusionAnalyzer wasn't called, or failed.
                        if (!hasExtrusions)
                        {
                            // Check to see if we have Geometries or GFaces.
                            // We will have the specific all GFaces case and then the generic case.
                            IList<Face> faces = null;

                            if (allFaces)
                            {
                                faces = new List<Face>();
                                foreach (GeometryObject geometryObject in geometryList)
                                {
                                    faces.Add(geometryObject as Face);
                                }
                            }

                            // Options used if we try to export extrusions.
                            IFCExtrusionAxes axesToExtrudeIn = exportBodyParams != null ? exportBodyParams.PossibleExtrusionAxes : IFCExtrusionAxes.TryDefault;
                            XYZ directionToExtrudeIn = XYZ.Zero;
                            if (exportBodyParams != null && exportBodyParams.HasCustomAxis)
                                directionToExtrudeIn = exportBodyParams.CustomAxis;

                            double lengthScale = UnitUtil.ScaleLengthForRevitAPI();
                            IFCExtrusionCalculatorOptions extrusionOptions =
                               new IFCExtrusionCalculatorOptions(exporterIFC, axesToExtrudeIn, directionToExtrudeIn, lengthScale);

                            int numExtrusionsToCreate = allFaces ? 1 : geometryList.Count;

                            IList<IList<IFCExtrusionData>> extrusionLists = new List<IList<IFCExtrusionData>>();
                            for (int ii = 0; ii < numExtrusionsToCreate; ii++)
                            {
                                IList<IFCExtrusionData> extrusionList = new List<IFCExtrusionData>();

                                if (tryToExportAsExtrusion)
                                {
                                    if (allFaces)
                                        extrusionList = IFCExtrusionCalculatorUtils.CalculateExtrusionData(extrusionOptions, faces);
                                    else
                                        extrusionList = IFCExtrusionCalculatorUtils.CalculateExtrusionData(extrusionOptions, geometryList[ii]);
                                }

                                if (extrusionList.Count == 0)
                                {
                                    // If we are trying to create swept solids, we will keep going, but we won't try to create more extrusions unless we are also exporting a solid model.
                                    if (tryToExportAsSweptSolid)
                                    {
                                        if (!canExportSolidModelRep) 
                                            tryToExportAsExtrusion = false; 
                                        exportAsSweptSolid.Add(ii);
                                    }
                                    else if (!canExportSolidModelRep)
                                    {
                                        tryToExportAsExtrusion = false;
                                        break;
                                    }
                                    else
                                        exportAsBRep.Add(new KeyValuePair<int, SimpleSweptSolidAnalyzer>(ii, null));
                                }
                                else
                                {
                                    extrusionLists.Add(extrusionList);
                                    exportAsExtrusion.Add(ii);
                                }
                            }

                            int numCreatedExtrusions = extrusionLists.Count;
                            for (int ii = 0; ii < numCreatedExtrusions && tryToExportAsExtrusion; ii++)
                            {
                                int geomIndex = exportAsExtrusion[ii];

                                ElementId matId = SetBestMaterialIdInExporter(geometryList[geomIndex], element, overrideMaterialId, exporterIFC);
                                if (matId != ElementId.InvalidElementId)
                                    bodyData.AddMaterial(matId);

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

                                bool exportedAsExtrusion = false;
                                IFCExtrusionBasis whichBasis = extrusionLists[ii][0].ExtrusionBasis;
                                if (whichBasis >= 0)
                                {
                                    IFCAnyHandle extrusionHandle = ExtrusionExporter.CreateExtrudedSolidFromExtrusionData(exporterIFC, element, extrusionLists[ii][0]);
                                    if (!IFCAnyHandleUtil.IsNullOrHasNoValue(extrusionHandle))
                                    {
                                        bodyItems.Add(extrusionHandle);
                                        materialIdsForExtrusions.Add(exporterIFC.GetMaterialIdForCurrentExportState());

                                        IList<CurveLoop> curveLoops = extrusionLists[ii][0].GetLoops();
                                        XYZ extrusionDirection = extrusionLists[ii][0].ExtrusionDirection;

                                        if (exportBodyParams != null)
                                        {
                                            exportBodyParams.Slope = GeometryUtil.GetSimpleExtrusionSlope(extrusionDirection, whichBasis);
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
                                                exportBodyParams.ScaledHeight = UnitUtil.ScaleLength(height);
                                                exportBodyParams.ScaledWidth = UnitUtil.ScaleLength(width);
                                            }

                                            double area = ExporterIFCUtils.ComputeAreaOfCurveLoops(curveLoops);
                                            if (area > 0.0)
                                            {
                                                exportBodyParams.ScaledArea = UnitUtil.ScaleArea(area);
                                            }

                                            double innerPerimeter = ExtrusionExporter.ComputeInnerPerimeterOfCurveLoops(curveLoops);
                                            double outerPerimeter = ExtrusionExporter.ComputeOuterPerimeterOfCurveLoops(curveLoops);
                                            if (innerPerimeter > 0.0)
                                                exportBodyParams.ScaledInnerPerimeter = UnitUtil.ScaleLength(innerPerimeter);
                                            if (outerPerimeter > 0.0)
                                                exportBodyParams.ScaledOuterPerimeter = UnitUtil.ScaleLength(outerPerimeter);
                                        }
                                        exportedAsExtrusion = true;
                                        hasExtrusions = true;
                                    }
                                }

                                if (!exportedAsExtrusion)
                                {
                                    if (tryToExportAsSweptSolid)
                                        exportAsSweptSolid.Add(ii);
                                    else if (!canExportSolidModelRep)
                                    {
                                        tryToExportAsExtrusion = false;
                                        break;
                                    }
                                    else
                                        exportAsBRep.Add(new KeyValuePair<int, SimpleSweptSolidAnalyzer>(ii, null));
                                }
                            }
                        }
                    }

                    if (tryToExportAsSweptSolid)
                    {
                        int numCreatedSweptSolids = exportAsSweptSolid.Count;
                        for (int ii = 0; (ii < numCreatedSweptSolids) && tryToExportAsSweptSolid; ii++)
                        {
                            bool exported = false;
                            int geomIndex = exportAsSweptSolid[ii];
                            Solid solid = geometryList[geomIndex] as Solid;
                            SimpleSweptSolidAnalyzer simpleSweptSolidAnalyzer = null;
                            // TODO: allFaces to SweptSolid
                            if (solid != null)
                            {
                                // TODO: give normal hint below if we have an idea.
                                simpleSweptSolidAnalyzer = SweptSolidExporter.CanExportAsSweptSolid(exporterIFC, solid, null);
                                
                                // If we are exporting as a BRep, we will keep the analyzer for later, if it isn't null.
                                if (simpleSweptSolidAnalyzer != null)
                                {
                                    if (!tryToExportAsSweptSolidAsTessellation)
                                    {
                                        SweptSolidExporter sweptSolidExporter = SweptSolidExporter.Create(exporterIFC, element, simpleSweptSolidAnalyzer, solid);
                                        IFCAnyHandle sweptHandle = (sweptSolidExporter != null) ? sweptSolidExporter.RepresentationItem : null;
                                        
                                        if (!IFCAnyHandleUtil.IsNullOrHasNoValue(sweptHandle))
                                        {
                                            bodyItems.Add(sweptHandle);
                                            materialIdsForExtrusions.Add(exporterIFC.GetMaterialIdForCurrentExportState());
                                            exported = true;
                                            hasRepresentationType = sweptSolidExporter.RepresentationType;

                                            // These are the only two valid cases for true sweep export: either an extrusion or a sweep.
                                            // We don't expect regular BReps or triangulated face sets here.
                                            if (sweptSolidExporter.isSpecificRepresentationType(ShapeRepresentationType.SweptSolid))
                                                hasExtrusions = true;
                                            else if (sweptSolidExporter.isSpecificRepresentationType(ShapeRepresentationType.AdvancedSweptSolid))
                                                hasSweptSolids = true;
                                        }
                                        else
                                            simpleSweptSolidAnalyzer = null;    // Didn't work for some reason.
                                    }
                                }
                            }

                            if (!exported)
                            {
                                exportAsBRep.Add(new KeyValuePair<int, SimpleSweptSolidAnalyzer>(geomIndex, simpleSweptSolidAnalyzer));
                                hasSweptSolidsAsBReps |= (simpleSweptSolidAnalyzer != null);
                            }
                        }
                    }

                    bool exportSucceeded = (exportAsBRep.Count == 0) && (tryToExportAsExtrusion || tryToExportAsSweptSolid)
                                && (hasExtrusions || hasSweptSolids || hasRepresentationType != ShapeRepresentationType.Undefined);
                    if (exportSucceeded || canExportSolidModelRep)
                    {
                        int sz = bodyItems.Count();
                        for (int ii = 0; ii < sz; ii++)
                            BodyExporter.CreateSurfaceStyleForRepItem(exporterIFC, document, bodyItems[ii], materialIdsForExtrusions[ii]);

                        if (exportSucceeded)
                        {
                            if (bodyData.RepresentationHnd == null)
                            {
                                HashSet<IFCAnyHandle> bodyItemSet = new HashSet<IFCAnyHandle>();
                                bodyItemSet.UnionWith(bodyItems);
                                if (hasExtrusions && !hasSweptSolids)
                                {
                                    bodyData.RepresentationHnd =
                                        RepresentationUtil.CreateSweptSolidRep(exporterIFC, element, categoryId, contextOfItems, bodyItemSet, bodyData.RepresentationHnd);
                                    bodyData.ShapeRepresentationType = ShapeRepresentationType.SweptSolid;
                                }
                                else if (hasSweptSolids && !hasExtrusions)
                                {
                                    bodyData.RepresentationHnd =
                                        RepresentationUtil.CreateAdvancedSweptSolidRep(exporterIFC, element, categoryId, contextOfItems, bodyItemSet, bodyData.RepresentationHnd);
                                    bodyData.ShapeRepresentationType = ShapeRepresentationType.AdvancedSweptSolid;
                                }
                                else if (hasRepresentationType == ShapeRepresentationType.Tessellation)
                                {
                                    bodyData.RepresentationHnd =
                                        RepresentationUtil.CreateTessellatedRep(exporterIFC, element, categoryId, contextOfItems, bodyItemSet, bodyData.RepresentationHnd);
                                    bodyData.ShapeRepresentationType = ShapeRepresentationType.Tessellation;
                                }
                                else
                                {
                                    bodyData.RepresentationHnd =
                                        RepresentationUtil.CreateSolidModelRep(exporterIFC, element, categoryId, contextOfItems, bodyItemSet);
                                    bodyData.ShapeRepresentationType = ShapeRepresentationType.SolidModel;
                                }
                            }

                            // TODO: include BRep, CSG, Clipping

                            XYZ lpOrig = ((bodyData != null) && (bodyData.OffsetTransform != null)) ? bodyData.OffsetTransform.Origin : new XYZ();
                            transformSetter.CreateLocalPlacementFromOffset(exporterIFC, bbox, exportBodyParams, lpOrig, unscaledTrfOrig);
                            tr.Commit();
                            return bodyData;
                        }
                    }

                    // If we are going to export a solid model, keep the created items.
                    if (!canExportSolidModelRep)
                        tr.RollBack();
                    else
                        tr.Commit();
                }

                // We couldn't export it as an extrusion; export as a solid, brep, or a surface model.
                if (!canExportSolidModelRep)
                {
                    exportAsExtrusion.Clear();
                    bodyItems.Clear();
                    if (exportBodyParams != null)
                        exportBodyParams.ClearOpenings();
                }

                if (exportAsExtrusion.Count == 0)
                {
                    // We used to clear exportAsBRep, but we need the SimpleSweptSolidAnalyzer information, so we will fill out the rest.
                    int numGeoms = geometryList.Count;
                    IList<KeyValuePair<int, SimpleSweptSolidAnalyzer>> newExportAsBRep = new List<KeyValuePair<int, SimpleSweptSolidAnalyzer>>(numGeoms);
                    int exportAsBRepCount = exportAsBRep.Count;
                    int currIndex = 0;
                    for (int ii = 0; ii < numGeoms; ii++)
                    {
                        if ((currIndex < exportAsBRepCount) && (ii == exportAsBRep[currIndex].Key))
                        {
                            newExportAsBRep.Add(exportAsBRep[currIndex]);
                            currIndex++;
                        }
                        else
                            newExportAsBRep.Add(new KeyValuePair<int, SimpleSweptSolidAnalyzer>(ii, null));
                    }
                    exportAsBRep = newExportAsBRep;
                }
            }

            // If we created some extrusions that we are using (e.g., creating a solid model), and we didn't use an offset transform for the extrusions, don't do it here either.
            bool supportOffsetTransformForBreps = !hasSweptSolidsAsBReps;
            bool disallowOffsetTransformForBreps = (exportAsExtrusion.Count > 0) && !useOffsetTransformForExtrusions;
            bool useOffsetTransformForBReps = options.AllowOffsetTransform && supportOffsetTransformForBreps && !disallowOffsetTransformForBreps;

            using (IFCTransaction tr = new IFCTransaction(file))
            {
                using (TransformSetter transformSetter = TransformSetter.Create())
                {
                    // Need to do extra work to support offset transforms if we are using the sweep analyzer.
                    if (useOffsetTransformForBReps)
                        bodyData.OffsetTransform = transformSetter.InitializeFromBoundingBox(exporterIFC, bbox, exportBodyParams, out unscaledTrfOrig);

                    BodyData brepBodyData = 
                        ExportBodyAsBRep(exporterIFC, geometryList, exportAsBRep, bodyItems, element, categoryId, overrideMaterialId, contextOfItems, eps, options, bodyData);
                    if (brepBodyData == null)
                        tr.RollBack();
                    else
                    {
                        XYZ lpOrig = ((bodyData != null) && (bodyData.OffsetTransform != null)) ? bodyData.OffsetTransform.Origin : new XYZ();
                        transformSetter.CreateLocalPlacementFromOffset(exporterIFC, bbox, exportBodyParams, lpOrig, unscaledTrfOrig);
                        tr.Commit();
                    }
                    return brepBodyData;
                }
            }
        }

        /// <summary>
        /// Exports list of solids and meshes to IFC body representation.
        /// </summary>
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
        public static BodyData ExportBody(ExporterIFC exporterIFC, 
            Element element, 
            ElementId categoryId,
            ElementId overrideMaterialId, 
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

            return ExportBody(exporterIFC, element, categoryId, overrideMaterialId, objects, options, exportBodyParams);
        }

        /// <summary>
        /// Exports a geometry object to IFC body representation.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="categoryId">The category id.</param>
        /// <param name="geometryObject">The geometry object.</param>
        /// <param name="options">The settings for how to export the body.</param>
        /// <param name="exportBodyParams">The extrusion creation data.</param>
        /// <returns>The body data.</returns>
        public static BodyData ExportBody(ExporterIFC exporterIFC,
           Element element, ElementId categoryId, ElementId overrideMaterialId,
           GeometryObject geometryObject, BodyExporterOptions options,
           IFCExtrusionCreationData exportBodyParams)
        {
            IList<GeometryObject> geomList = new List<GeometryObject>();
            if (geometryObject is Solid)
            {
                IList<Solid> splitVolumes = GeometryUtil.SplitVolumes(geometryObject as Solid);
                foreach (Solid solid in splitVolumes)
                    geomList.Add(solid);
            }
            else
                geomList.Add(geometryObject);
            return ExportBody(exporterIFC, element, categoryId, overrideMaterialId, geomList, options, exportBodyParams);
        }

        /// <summary>
        /// Exports a geometry object to IFC body representation.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="categoryId">The category id.</param>
        /// <param name="geometryElement">The geometry element.</param>
        /// <param name="options">The settings for how to export the body.</param>
        /// <param name="exportBodyParams">The extrusion creation data.</param>
        /// <param name="offsetTransform">The transform used to shift the body to the local origin.</param>
        /// <returns>The body data.</returns>
        public static BodyData ExportBody(ExporterIFC exporterIFC,
           Element element, ElementId categoryId, ElementId overrideMaterialId, 
           GeometryElement geometryElement, BodyExporterOptions options,
           IFCExtrusionCreationData exportBodyParams)
        {
            SolidMeshGeometryInfo info = null;
            IList<GeometryObject> geomList = new List<GeometryObject>();

            if (!ExporterCacheManager.ExportOptionsCache.ExportAs2x2)
            {
                info = GeometryUtil.GetSplitSolidMeshGeometry(geometryElement);
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

            return ExportBody(exporterIFC, element, categoryId, overrideMaterialId, geomList, 
                options, exportBodyParams);
        }
    }
}
