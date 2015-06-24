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
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Exporter
{
    /// <summary>
    /// Provides methods to export ceilings.
    /// </summary>
    class RailingExporter
    {
        private static Toolkit.IFCRailingType GetIFCRailingTypeFromString(string value)
        {
            if (String.IsNullOrEmpty(value))
                return Toolkit.IFCRailingType.NotDefined;

            if (String.Compare(value, "USERDEFINED", true) == 0)
                return Toolkit.IFCRailingType.UserDefined;
            if (String.Compare(value, "HANDRAIL", true) == 0)
                return Toolkit.IFCRailingType.HandRail;
            if (String.Compare(value, "GUARDRAIL", true) == 0)
                return Toolkit.IFCRailingType.GuardRail;
            if (String.Compare(value, "BALUSTRADE", true) == 0)
                return Toolkit.IFCRailingType.Balustrade;

            return Toolkit.IFCRailingType.NotDefined;
        }

        /// <summary>
        /// Gets IFC railing type for an element.
        /// </summary>
        /// <param name="element">
        /// The element.
        /// </param>
        /// <param name="typeName">
        /// The type name.
        /// </param>
        private static Toolkit.IFCRailingType GetIFCRailingType(Element element, string typeName)
        {
            string value = null;
            if (ParameterUtil.GetStringValueFromElementOrSymbol(element, "IfcType", out value) == null)
                value = typeName;

            if (String.IsNullOrEmpty(value))
                return Toolkit.IFCRailingType.NotDefined;

            string newValue = NamingUtil.RemoveSpacesAndUnderscores(value);
            return GetIFCRailingTypeFromString(newValue);
        }

        private static ElementId GetStairOrRampHostId(ExporterIFC exporterIFC, Railing railingElem)
        {
            ElementId returnHostId = ElementId.InvalidElementId;
            
            if (railingElem == null)
                return returnHostId;

            ElementId hostId = railingElem.HostId;
            if (hostId == ElementId.InvalidElementId)
                return returnHostId;

            if (!ExporterCacheManager.StairRampContainerInfoCache.ContainsStairRampContainerInfo(hostId))
                return returnHostId;

            Element host = railingElem.Document.GetElement(hostId);
            if (host == null)
                return returnHostId;

            if (!(host is Stairs) && !StairsExporter.IsLegacyStairs(host) && !RampExporter.IsRamp(host))
                return returnHostId;

            returnHostId = hostId;
            return returnHostId;
        }

        private static IFCAnyHandle CopyRailingHandle(ExporterIFC exporterIFC, Element elem, ElementId catId, IFCAnyHandle origLocalPlacement,
            IFCAnyHandle origRailing)
        {
            IFCFile file = exporterIFC.GetFile();

            IFCAnyHandle origRailingObjectPlacement = IFCAnyHandleUtil.GetObjectPlacement(origRailing);
            IFCAnyHandle railingRelativePlacement = IFCAnyHandleUtil.GetInstanceAttribute(origRailingObjectPlacement, "RelativePlacement");
            IFCAnyHandle parentRelativePlacement = IFCAnyHandleUtil.GetInstanceAttribute(origLocalPlacement, "RelativePlacement");

            IFCAnyHandle newRelativePlacement = null;
            IFCAnyHandle parentRelativeOrig = IFCAnyHandleUtil.GetInstanceAttribute(parentRelativePlacement, "Location");

            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(parentRelativeOrig))
            {
                IList<double> parentVec = IFCAnyHandleUtil.GetCoordinates(parentRelativeOrig);
                IFCAnyHandle railingRelativeOrig = IFCAnyHandleUtil.GetInstanceAttribute(railingRelativePlacement, "Location");
                if (!IFCAnyHandleUtil.IsNullOrHasNoValue(railingRelativeOrig))
                {
                    IList<double> railingVec = IFCAnyHandleUtil.GetCoordinates(railingRelativeOrig);

                    IList<double> newMeasure = new List<double>();
                    newMeasure.Add(railingVec[0] - parentVec[0]);
                    newMeasure.Add(railingVec[1] - parentVec[1]);
                    newMeasure.Add(railingVec[2]);

                    IFCAnyHandle locPtHnd = ExporterUtil.CreateCartesianPoint(file, newMeasure);
                    newRelativePlacement = IFCInstanceExporter.CreateAxis2Placement3D(file, locPtHnd, null, null);
                }
                else
                {
                    IList<double> railingMeasure = new List<double>();
                    railingMeasure.Add(-parentVec[0]);
                    railingMeasure.Add(-parentVec[1]);
                    railingMeasure.Add(0.0);
                    IFCAnyHandle locPtHnd = ExporterUtil.CreateCartesianPoint(file, railingMeasure);
                    newRelativePlacement = IFCInstanceExporter.CreateAxis2Placement3D(file, locPtHnd, null, null);
                }
            }

            IFCAnyHandle newLocalPlacement = IFCInstanceExporter.CreateLocalPlacement(file, origLocalPlacement, newRelativePlacement);
            IFCAnyHandle origRailingRep = IFCAnyHandleUtil.GetInstanceAttribute(origRailing, "Representation");
            IFCAnyHandle newProdRep = ExporterUtil.CopyProductDefinitionShape(exporterIFC, elem, catId, origRailingRep);

            string ifcEnumTypeAsString = IFCAnyHandleUtil.GetEnumerationAttribute(origRailing, "PredefinedType");

            string copyGUID = GUIDUtil.CreateGUID();
            IFCAnyHandle copyOwnerHistory = IFCAnyHandleUtil.GetInstanceAttribute(origRailing, "OwnerHistory");
            string copyName = IFCAnyHandleUtil.GetStringAttribute(origRailing, "Name");
            string copyDescription = IFCAnyHandleUtil.GetStringAttribute(origRailing, "Description");
            string copyObjectType = IFCAnyHandleUtil.GetStringAttribute(origRailing, "ObjectType");
            string copyElemId = IFCAnyHandleUtil.GetStringAttribute(origRailing, "Tag");

            return IFCInstanceExporter.CreateRailing(file, copyGUID, copyOwnerHistory, copyName, copyDescription, copyObjectType,
                newLocalPlacement, newProdRep,
                copyElemId, ifcEnumTypeAsString);
        }

        /// <summary>
        /// Exports a railing to IFC railing
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="railing">
        /// The ceiling element to be exported.
        /// </param>
        /// <param name="geomElement">
        /// The geometry element.
        /// </param>
        /// <param name="productWrapper">
        /// The ProductWrapper.
        /// </param>
        public static void ExportRailingElement(ExporterIFC exporterIFC, Railing railing, ProductWrapper productWrapper)
        {
            if (railing == null)
                return;

            Options geomOptions = GeometryUtil.GetIFCExportGeometryOptions();
            GeometryElement geomElement = GeometryUtil.GetOneLevelGeometryElement(railing.get_Geometry(geomOptions), 0);

            // If this is a multistory railing, the geometry will contain all of the levels of railing.  We only want one.
            if (geomElement == null)
                return;

            string ifcEnumType = ExporterUtil.GetIFCTypeFromExportTable(exporterIFC, railing);
            ExportRailing(exporterIFC, railing, geomElement, ifcEnumType, productWrapper);
        }

        private static IList<ElementId> CollectSubElements(Railing railingElem)
        {
            IList<ElementId> subElementIds = new List<ElementId>();
            if (railingElem != null)
            {
                ElementId topRailId = railingElem.TopRail;
                if (topRailId != ElementId.InvalidElementId)
                    subElementIds.Add(topRailId);
                IList<ElementId> handRailIds = railingElem.GetHandRails();
                if (handRailIds != null)
                {
                    foreach (ElementId handRailId in handRailIds)
                    {
                        HandRail handRail = railingElem.Document.GetElement(handRailId) as HandRail;
                        if (handRail != null)
                        {
                            subElementIds.Add(handRailId);
                            IList<ElementId> supportIds = handRail.GetSupports();
                            foreach (ElementId supportId in supportIds)
                            {
                                subElementIds.Add(supportId);
                            }
                        }
                    }
                }
            }
            return subElementIds;
        }

        /// <summary>
        /// Collects the sub-elements of a Railing, to prevent double export.
        /// </summary>
        /// <param name="railingElem">
        /// The railing.
        /// </param>
        public static void AddSubElementsToCache(Railing railingElem)
        {
            IList<ElementId> subElementIds = CollectSubElements(railingElem);
            foreach (ElementId subElementId in subElementIds)
                ExporterCacheManager.RailingSubElementCache.Add(subElementId);
        }

        /// <summary>
        /// Exports an element as IFC railing.
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
        /// The ProductWrapper.
        /// </param>
        public static void ExportRailing(ExporterIFC exporterIFC, Element element, GeometryElement geomElem, string ifcEnumType, ProductWrapper productWrapper)
        {
            ElementType elemType = element.Document.GetElement(element.GetTypeId()) as ElementType;
            IFCFile file = exporterIFC.GetFile();
            Options geomOptions = GeometryUtil.GetIFCExportGeometryOptions();

            using (IFCTransaction transaction = new IFCTransaction(file))
            {
                using (PlacementSetter setter = PlacementSetter.Create(exporterIFC, element))
                {
                    using (IFCExtrusionCreationData ecData = new IFCExtrusionCreationData())
                    {
                        IFCAnyHandle localPlacement = setter.LocalPlacement;
                        StairRampContainerInfo stairRampInfo = null;
                        ElementId hostId = GetStairOrRampHostId(exporterIFC, element as Railing);
                        Transform inverseTrf = Transform.Identity;
                        if (hostId != ElementId.InvalidElementId)
                        {
                            stairRampInfo = ExporterCacheManager.StairRampContainerInfoCache.GetStairRampContainerInfo(hostId);
                            IFCAnyHandle stairRampLocalPlacement = stairRampInfo.LocalPlacements[0];
                            Transform relTrf = ExporterIFCUtils.GetRelativeLocalPlacementOffsetTransform(stairRampLocalPlacement, localPlacement);
                            inverseTrf = relTrf.Inverse;

                            IFCAnyHandle railingLocalPlacement = ExporterUtil.CreateLocalPlacement(file, stairRampLocalPlacement,
                                inverseTrf.Origin, inverseTrf.BasisZ, inverseTrf.BasisX);
                            localPlacement = railingLocalPlacement;
                        }
                        ecData.SetLocalPlacement(localPlacement);

                        SolidMeshGeometryInfo solidMeshInfo = GeometryUtil.GetSplitSolidMeshGeometry(geomElem);
                        IList<Solid> solids = solidMeshInfo.GetSolids();
                        IList<Mesh> meshes = solidMeshInfo.GetMeshes();

                        Railing railingElem = element as Railing;
                        IList<ElementId> subElementIds = CollectSubElements(railingElem);

                        foreach (ElementId subElementId in subElementIds)
                        {
                            Element subElement = railingElem.Document.GetElement(subElementId);
                            if (subElement != null)
                            {
                                GeometryElement subElementGeom = GeometryUtil.GetOneLevelGeometryElement(subElement.get_Geometry(geomOptions), 0);

                                SolidMeshGeometryInfo subElementSolidMeshInfo = GeometryUtil.GetSplitSolidMeshGeometry(subElementGeom);
                                IList<Solid> subElementSolids = subElementSolidMeshInfo.GetSolids();
                                IList<Mesh> subElementMeshes = subElementSolidMeshInfo.GetMeshes();
                                foreach (Solid subElementSolid in subElementSolids)
                                    solids.Add(subElementSolid);
                                foreach (Mesh subElementMesh in subElementMeshes)
                                    meshes.Add(subElementMesh);
                            }
                        }

                        ElementId catId = CategoryUtil.GetSafeCategoryId(element);
                        BodyData bodyData = null;
                        BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true);
                        bodyExporterOptions.TessellationLevel = BodyExporter.GetTessellationLevel();
                        //bodyExporterOptions.UseGroupsIfPossible = true;
                        //bodyExporterOptions.UseMappedGeometriesIfPossible = true;

                        if (solids.Count > 0 || meshes.Count > 0)
                        {
                            bodyData = BodyExporter.ExportBody(exporterIFC, element, catId, ElementId.InvalidElementId, solids, meshes, bodyExporterOptions, ecData);
                        }
                        else
                        {
                            IList<GeometryObject> geomlist = new List<GeometryObject>();
                            geomlist.Add(geomElem);
                            bodyData = BodyExporter.ExportBody(exporterIFC, element, catId, ElementId.InvalidElementId, geomlist, bodyExporterOptions, ecData);
                        }

                        IFCAnyHandle bodyRep = bodyData.RepresentationHnd;
                        if (IFCAnyHandleUtil.IsNullOrHasNoValue(bodyRep))
                        {
                            if (ecData != null)
                                ecData.ClearOpenings();
                            return;
                        }

                        IList<IFCAnyHandle> representations = new List<IFCAnyHandle>();
                        representations.Add(bodyRep);

                        IList<GeometryObject> geomObjects = new List<GeometryObject>();
                        foreach (Solid solid in solids)
                            geomObjects.Add(solid);
                        foreach (Mesh mesh in meshes)
                            geomObjects.Add(mesh);

                        Transform boundingBoxTrf = (bodyData.OffsetTransform != null) ? bodyData.OffsetTransform.Inverse : Transform.Identity;
                        boundingBoxTrf = inverseTrf.Multiply(boundingBoxTrf);
                        IFCAnyHandle boundingBoxRep = BoundingBoxExporter.ExportBoundingBox(exporterIFC, geomObjects, boundingBoxTrf);
                        if (boundingBoxRep != null)
                            representations.Add(boundingBoxRep);

                        IFCAnyHandle prodRep = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, representations);

                        IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

                        string instanceGUID = GUIDUtil.CreateGUID(element);
                        string instanceName = NamingUtil.GetNameOverride(element, NamingUtil.GetIFCName(element));
                        string instanceDescription = NamingUtil.GetDescriptionOverride(element, null);
                        string instanceObjectType = NamingUtil.GetObjectTypeOverride(element, exporterIFC.GetFamilyName());
                        string instanceTag = NamingUtil.GetTagOverride(element, NamingUtil.CreateIFCElementId(element));

                        string railingType = IFCValidateEntry.GetValidIFCType(element, ifcEnumType);

                        IFCAnyHandle railing = IFCInstanceExporter.CreateRailing(file, instanceGUID, ownerHistory,
                            instanceName, instanceDescription, instanceObjectType, ecData.GetLocalPlacement(),
                            prodRep, instanceTag, railingType);

                        bool associateToLevel = (hostId == ElementId.InvalidElementId);

                        productWrapper.AddElement(element, railing, setter, ecData, associateToLevel);
                        OpeningUtil.CreateOpeningsIfNecessary(railing, element, ecData, bodyData.OffsetTransform,
                            exporterIFC, ecData.GetLocalPlacement(), setter, productWrapper);

                        CategoryUtil.CreateMaterialAssociations(exporterIFC, railing, bodyData.MaterialIds);

                        // Create multi-story duplicates of this railing.
                        if (stairRampInfo != null)
                        {
                            stairRampInfo.AddComponent(0, railing);

                            List<IFCAnyHandle> stairHandles = stairRampInfo.StairOrRampHandles;
                            for (int ii = 1; ii < stairHandles.Count; ii++)
                            {
                                IFCAnyHandle railingLocalPlacement = stairRampInfo.LocalPlacements[ii];
                                if (!IFCAnyHandleUtil.IsNullOrHasNoValue(railingLocalPlacement))
                                {
                                    IFCAnyHandle railingHndCopy = CopyRailingHandle(exporterIFC, element, catId, railingLocalPlacement, railing);
                                    stairRampInfo.AddComponent(ii, railingHndCopy);
                                    productWrapper.AddElement(element, railingHndCopy, (IFCLevelInfo)null, ecData, false);
                                    CategoryUtil.CreateMaterialAssociations(exporterIFC, railingHndCopy, bodyData.MaterialIds);
                                }
                            }

                            ExporterCacheManager.StairRampContainerInfoCache.AddStairRampContainerInfo(hostId, stairRampInfo);
                        }
                    }
                    transaction.Commit();
                }
            }
        }
    }
}
