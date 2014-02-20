﻿//
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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB.Structure;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Export.Exporter.PropertySet;

namespace Revit.IFC.Export.Exporter
{
    /// <summary>
    /// Provides methods to export a Revit element as IfcElementAssembly.
    /// </summary>
    class AssemblyInstanceExporter
    {
        private static IFCElementAssemblyType GetPredefinedTypeFromObjectType(string objectType)
        {
            if (String.IsNullOrEmpty(objectType))
                return IFCElementAssemblyType.NotDefined;

            foreach (IFCElementAssemblyType val in Enum.GetValues(typeof(IFCElementAssemblyType)))
            {
                if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(objectType, val.ToString()))
                    return val;
            }

            return IFCElementAssemblyType.UserDefined;
        }

        /// <summary>
        /// Exports an element as an IFC assembly.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="element">The element.</param>
        /// <param name="productWrapper">The ProductWrapper.</param>
        /// <returns>True if exported successfully, false otherwise.</returns>
        public static bool ExportAssemblyInstanceElement(ExporterIFC exporterIFC, AssemblyInstance element,
            ProductWrapper productWrapper)
        {
            if (element == null)
                return false;

            IFCFile file = exporterIFC.GetFile();

            using (IFCTransaction tr = new IFCTransaction(file))
            {
                IFCAnyHandle assemblyInstanceHnd = null;

                string guid = GUIDUtil.CreateGUID(element);
                IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();
                string name = NamingUtil.GetNameOverride(element, NamingUtil.GetIFCName(element));
                string description = NamingUtil.GetDescriptionOverride(element, null);
                string objectType = NamingUtil.GetObjectTypeOverride(element, exporterIFC.GetFamilyName());
                IFCAnyHandle localPlacement = null;
                PlacementSetter placementSetter = null;
                IFCLevelInfo levelInfo = null;

                string ifcEnumType;
                IFCExportType exportAs = ExporterUtil.GetExportType(exporterIFC, element, out ifcEnumType);
                if (exportAs == IFCExportType.IfcSystem)
                {
                    assemblyInstanceHnd = IFCInstanceExporter.CreateSystem(file, guid,
                        ownerHistory, name, description, objectType);

                    // Create classification reference when System has classification filed name assigned to it
                    ClassificationUtil.CreateClassification(exporterIFC, file, element, assemblyInstanceHnd);

                    HashSet<IFCAnyHandle> relatedBuildings = new HashSet<IFCAnyHandle>();
                    relatedBuildings.Add(ExporterCacheManager.BuildingHandle);

                    IFCAnyHandle relServicesBuildings = IFCInstanceExporter.CreateRelServicesBuildings(file, GUIDUtil.CreateGUID(),
                        exporterIFC.GetOwnerHistoryHandle(), null, null, assemblyInstanceHnd, relatedBuildings);
                }
                else
                {
                    using (placementSetter = PlacementSetter.Create(exporterIFC, element))
                    {
                        string elementTag = NamingUtil.GetTagOverride(element, NamingUtil.CreateIFCElementId(element));
                        IFCAnyHandle representation = null;

                        // We have limited support for exporting assemblies as other container types.
                        localPlacement = placementSetter.LocalPlacement;
                        levelInfo = placementSetter.LevelInfo;
                        ifcEnumType = IFCValidateEntry.GetValidIFCType(element, ifcEnumType);

                        switch (exportAs)
                        {
                            case IFCExportType.IfcCurtainWall:
                                assemblyInstanceHnd = IFCInstanceExporter.CreateCurtainWall(file, guid,
                                    ownerHistory, name, description, objectType, localPlacement, representation, elementTag);
                                break;
                            case IFCExportType.IfcRamp:
                                string rampPredefinedType = RampExporter.GetIFCRampType(ifcEnumType);
                                assemblyInstanceHnd = IFCInstanceExporter.CreateRamp(file, guid,
                                    ownerHistory, name, description, objectType, localPlacement, representation, elementTag,
                                    rampPredefinedType);
                                break;
                            case IFCExportType.IfcRoof:
                                assemblyInstanceHnd = IFCInstanceExporter.CreateRoof(file, guid,
                                    ownerHistory, name, description, objectType, localPlacement, representation, elementTag,
                                    ifcEnumType);
                                break;
                            case IFCExportType.IfcStair:
                                string stairPredefinedType = StairsExporter.GetIFCStairType(ifcEnumType);
                                assemblyInstanceHnd = IFCInstanceExporter.CreateStair(file, guid,
                                    ownerHistory, name, description, objectType, localPlacement, representation, elementTag,
                                    stairPredefinedType);
                                break;
                            case IFCExportType.IfcWall:
                                assemblyInstanceHnd = IFCInstanceExporter.CreateWall(file, guid,
                                    ownerHistory, name, description, objectType, localPlacement, representation, elementTag, 
                                    ifcEnumType);
                                break;
                            default:
                                IFCElementAssemblyType assemblyPredefinedType = GetPredefinedTypeFromObjectType(objectType);
                                assemblyInstanceHnd = IFCInstanceExporter.CreateElementAssembly(file, guid,
                                    ownerHistory, name, description, objectType, localPlacement, representation, elementTag,
                                    IFCAssemblyPlace.NotDefined, assemblyPredefinedType);
                                break;
                        }
                    }
                }

                if (assemblyInstanceHnd == null)
                    return false;

                bool relateToLevel = (levelInfo != null);
                productWrapper.AddElement(element, assemblyInstanceHnd, levelInfo, null, relateToLevel);

                ExporterCacheManager.AssemblyInstanceCache.RegisterAssemblyInstance(element.Id, assemblyInstanceHnd);

                tr.Commit();
                return true;
            }
        }

        /// <summary>
        /// Update the local placements of the members of an assembly relative to the assembly.
        /// </summary>
        /// <param name="exporterIFC">The ExporerIFC.</param>
        /// <param name="assemblyPlacement">The assembly local placement handle.</param>
        /// <param name="elementPlacements">The member local placement handles.</param>
        public static void SetLocalPlacementsRelativeToAssembly(ExporterIFC exporterIFC, IFCAnyHandle assemblyPlacement, ICollection<IFCAnyHandle> elementPlacements)
        {
            foreach (IFCAnyHandle elementHandle in elementPlacements)
            {
                IFCAnyHandle elementPlacement = IFCAnyHandleUtil.GetObjectPlacement(elementHandle);

                Transform origTrf = ExporterIFCUtils.GetUnscaledTransform(exporterIFC, assemblyPlacement);
                if (!origTrf.IsIdentity)
                {
                    Transform relTrf = ExporterIFCUtils.GetRelativeLocalPlacementOffsetTransform(assemblyPlacement, elementPlacement);
                    Transform inverseTrf = relTrf.Inverse;

                    IFCFile file = exporterIFC.GetFile();
                    IFCAnyHandle relLocalPlacement = ExporterUtil.CreateAxis2Placement3D(file, inverseTrf.Origin, inverseTrf.BasisZ, inverseTrf.BasisX);
                    
                    // NOTE: caution that old IFCAXIS2PLACEMENT3D may be unused as the new one replace it. 
                    // But we cannot delete it safely yet because we don't know if any handle is referencing it.
                    GeometryUtil.SetRelativePlacement(elementPlacement, relLocalPlacement);
                }
                GeometryUtil.SetPlacementRelTo(elementPlacement, assemblyPlacement);
            }
        }

        /// <summary>
        /// Exports a truss as an IFC assembly.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="truss">The truss element.</param>
        /// <param name="productWrapper">The ProductWrapper.</param>
        public static void ExportTrussElement(ExporterIFC exporterIFC, Truss truss,
            ProductWrapper productWrapper)
        {
            if (truss == null)
                return;

            ICollection<ElementId> trussMemberIds = truss.Members;
            ExportAssemblyInstanceWithMembers(exporterIFC, truss, trussMemberIds, IFCElementAssemblyType.Truss, productWrapper);
        }

        /// <summary>
        /// Exports a beam system as an IFC assembly.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="beamSystem">The beam system.</param>
        /// <param name="productWrapper">The ProductWrapper.</param>
        public static void ExportBeamSystem(ExporterIFC exporterIFC, BeamSystem beamSystem,
            ProductWrapper productWrapper)
        {
            if (beamSystem == null)
                return;

            ICollection<ElementId> beamMemberIds = beamSystem.GetBeamIds();
            ExportAssemblyInstanceWithMembers(exporterIFC, beamSystem, beamMemberIds, IFCElementAssemblyType.Beam_Grid, productWrapper);
        }

        /// <summary>
        /// Exports an element as an IFC assembly with its members.
        /// </summary>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="assemblyElem">The element to be exported as IFC assembly.</param>
        /// <param name="memberIds">The member element ids.</param>
        /// <param name="assemblyType">The IFC assembly type.</param>
        /// <param name="productWrapper">The ProductWrapper.</param>
        static void ExportAssemblyInstanceWithMembers(ExporterIFC exporterIFC, Element assemblyElem,
            ICollection<ElementId> memberIds, IFCElementAssemblyType assemblyType, ProductWrapper productWrapper)
        {
            HashSet<IFCAnyHandle> memberHnds = new HashSet<IFCAnyHandle>();

            foreach (ElementId memberId in memberIds)
            {
                IFCAnyHandle memberHnd = ExporterCacheManager.ElementToHandleCache.Find(memberId);
                if (!IFCAnyHandleUtil.IsNullOrHasNoValue(memberHnd))
                    memberHnds.Add(memberHnd);
            }

            if (memberHnds.Count == 0)
                return;

            IFCFile file = exporterIFC.GetFile();
            using (IFCTransaction tr = new IFCTransaction(file))
            {
                using (PlacementSetter placementSetter = PlacementSetter.Create(exporterIFC, assemblyElem))
                {
                    IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();
                    IFCAnyHandle localPlacement = placementSetter.LocalPlacement;

                    string guid = GUIDUtil.CreateGUID(assemblyElem);
                    string name = NamingUtil.GetIFCName(assemblyElem);
                    string description = NamingUtil.GetDescriptionOverride(assemblyElem, null);
                    string objectType = NamingUtil.GetObjectTypeOverride(assemblyElem, exporterIFC.GetFamilyName());
                    string elementTag = NamingUtil.CreateIFCElementId(assemblyElem);

                    IFCAnyHandle assemblyInstanceHnd = IFCInstanceExporter.CreateElementAssembly(file, guid,
                        ownerHistory, name, description, objectType, localPlacement, null, elementTag,
                        IFCAssemblyPlace.NotDefined, assemblyType);

                    productWrapper.AddElement(assemblyElem, assemblyInstanceHnd, placementSetter.LevelInfo, null, true);

                    string aggregateGuid = GUIDUtil.CreateSubElementGUID(assemblyElem, (int)IFCAssemblyInstanceSubElements.RelAggregates);
                    IFCInstanceExporter.CreateRelAggregates(file, aggregateGuid, ownerHistory, null, null, assemblyInstanceHnd, memberHnds);

                    ExporterCacheManager.ElementsInAssembliesCache.UnionWith(memberHnds);

                    // Update member local placements to be relative to the assembly.
                    SetLocalPlacementsRelativeToAssembly(exporterIFC, localPlacement, memberHnds);
                }
                tr.Commit();
            }
        }
    }
}
