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

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;

using Revit.IFC.Export.Toolkit;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Utility
{
    /// <summary>
    /// Manages caches necessary for IFC export.
    /// </summary>
    public class ExporterCacheManager
    {
        /// <summary>
        /// The AssemblyInstanceCache object.
        /// </summary>
        static AllocatedGeometryObjectCache m_AllocatedGeometryObjectCache;

        /// <summary>
        /// The AssemblyInstanceCache object.
        /// </summary>
        static AssemblyInstanceCache m_AssemblyInstanceCache;

        /// <summary>
        /// The ClassificationCache object.
        /// Keeps track of created IfcClassifications for re-use.
        /// </summary>
        static ClassificationCache m_ClassificationCache;

        /// <summary>
        /// The Classification location cache.
        /// </summary>
        static ClassificationLocationCache m_ClassificationLocationCache;

        /// <summary>
        /// The ContainmentCache object.
        /// </summary>
        static ContainmentCache m_ContainmentCache;

        /// <summary>
        /// The CurveAnnotationCache object.
        /// </summary>
        static CurveAnnotationCache m_CurveAnnotationCache;

        /// <summary>
        /// The db views to export.
        /// </summary>
        static IDictionary<ElementId, ElementId> m_DBViewsToExport;

        /// <summary>
        /// The Document object.
        /// </summary>
        static Document m_Document;

        /// <summary>
        /// The collection of openings needed for created doors and windows.
        /// </summary>
        static DoorWindowDelayedOpeningCreatorCache m_DoorWindowDelayedOpeningCreatorCache;

        ///<summary>
        /// The ElementToHandleCache cache.
        /// </summary>
        static ElementToHandleCache m_ElementToHandleCache;

        ///<summary>
        /// The ExportOptions cache.
        /// </summary>
        static ExportOptionsCache m_ExportOptionsCache;

        /// <summary>
        /// The GroupElementGeometryCache cache.
        /// </summary>
        static GroupElementGeometryCache m_GroupElementGeometryCache;

        /// <summary>
        /// The GUID cache.
        /// </summary>
        static HashSet<string> m_GUIDCache;

        /// <summary>
        /// The HandleToElementCache cache.
        /// This maps an IFC handle to the Element that created it.
        /// This is used to identify which element should be used for properties, for elements (e.g. Stairs) that contain other elements.
        /// </summary>
        static HandleToElementCache m_HandleToElementCache;

        /// <summary>
        /// The language of the current Revit document.
        /// </summary>
        static LanguageType m_LanguageType;

        /// <summary>
        /// The LevelInfoCache object.  This contains extra information on top of
        /// IFCLevelInfo, and will eventually replace it.
        /// </summary>
        static LevelInfoCache m_LevelInfoCache;

        /// <summary>
        /// The MaterialHandleCache object.
        /// </summary>
        static MaterialHandleCache m_MaterialHandleCache;

        /// <summary>
        /// The MaterialLayerRelationsCache object.
        /// </summary>
        static MaterialLayerRelationsCache m_MaterialLayerRelationsCache;

        /// <summary>
        /// The MaterialLayerSetCache object.
        /// </summary>
        static MaterialLayerSetCache m_MaterialLayerSetCache;

        /// <summary>
        /// The MEPCache object.
        /// </summary>
        static MEPCache m_MEPCache;

        /// <summary>
        /// The MaterialRelationsCache object.
        /// </summary>
        static MaterialRelationsCache m_MaterialRelationsCache;

        /// <summary>
        /// The ParameterCache object.
        /// </summary>
        static ParameterCache m_ParameterCache;

        /// <summary>
        /// The PartExportedCache object.
        /// </summary>
        static PartExportedCache m_PartExportedCache;

        /// <summary>
        /// The PresentationLayerSetCache object.
        /// </summary>
        static PresentationLayerSetCache m_PresentationLayerSetCache;

        /// <summary>
        /// The PresentationStyleAssignmentCache object.
        /// </summary>
        static PresentationStyleAssignmentCache m_PresentationStyleCache;

        ///<summary>
        /// The RailingCache cache.
        /// This keeps track of all of the railings in the document, to export them last.
        /// </summary>
        static HashSet<ElementId> m_RailingCache;

        ///<summary>
        /// The RailingSubElementCache cache.
        /// This keeps track of all of the sub-elements of railings in the document, to not export them twice.
        /// </summary>
        static HashSet<ElementId> m_RailingSubElementCache;

        /// <summary>
        /// The SpaceBoundaryCache object.
        /// </summary>
        static SpaceBoundaryCache m_SpaceBoundaryCache;

        /// <summary>
        /// The SpaceOccupantInfoCache object.
        /// </summary>
        static SpaceOccupantInfoCache m_SpaceOccupantInfoCache;

        /// <summary>
        /// The SystemsCache object.
        /// </summary>
        static SystemsCache m_SystemsCache;

        ///<summary>
        /// The Truss cache.
        /// This keeps track of all of the truss in the document, to export after all beams and members.
        /// </summary>
        static HashSet<ElementId> m_TrussCache;

        ///<summary>
        /// The AreaScheme cache.
        /// This keeps track of all of the area schemes in the document, to export them after all areas.
        /// </summary>
        static Dictionary<ElementId, HashSet<IFCAnyHandle>> m_AreaSchemeCache;

        ///<summary>
        /// The BeamSystem cache.
        /// This keeps track of all of the beam systems in the document, to export after all beams.
        /// </summary>
        static HashSet<ElementId> m_BeamSystemCache;

        ///<summary>
        /// The Zone cache.
        /// This keeps track of all of the zone in the document, to export them after all spaces.
        /// </summary>
        static HashSet<ElementId> m_ZoneCache;

        /// <summary>
        /// The TypeRelationsCache object.
        /// </summary>
        static TypeRelationsCache m_TypeRelationsCache;

        /// <summary>
        /// The TypeObjectsCache object.
        /// </summary>
        static TypeObjectsCache m_TypeObjectsCache;

        /// <summary>
        /// The WallConnectionDataCache object.
        /// </summary>
        static WallConnectionDataCache m_WallConnectionDataCache;

        /// <summary>
        /// The UnitsCache object.
        /// Keeps track of created IfcUnits for re-use.
        /// </summary>
        static UnitsCache m_UnitsCache;

        /// <summary>
        /// The ZoneInfoCache object.
        /// </summary>
        static ZoneInfoCache m_ZoneInfoCache;

        /// <summary>
        /// The TypePropertyInfoCache object.
        /// </summary>
        static TypePropertyInfoCache m_TypePropertyInfoCache;

        /// <summary>
        /// The PropertyInfoCache object.
        /// </summary>
        static PropertyInfoCache m_PropertyInfoCache;

        /// <summary>
        /// The common property sets to be exported for an entity type, regardless of Object Type.
        /// </summary>
        static IDictionary<IFCEntityType, IList<PropertySetDescription>> m_PropertySetsForTypeCache;

        /// <summary>
        /// The common property sets to be exported for an entity type, conditional on the Object Type of the
        /// entity matching that of the PropertySetDescription.
        /// </summary>
        static IDictionary<IFCEntityType, IList<PropertySetDescription>> m_ConditionalPropertySetsForTypeCache;

        /// <summary>
        /// The material id to style handle cache.
        /// </summary>
        static ElementToHandleCache m_MaterialIdToStyleHandleCache;

        /// <summary>
        /// A list of elements contained in assemblies, to be removed from the level spatial structure.
        /// </summary>
        static ISet<IFCAnyHandle> m_ElementsInAssembliesCache;
        
        /// <summary>
        /// The default IfcCartesianTransformationOperator3D, scale 1.0 and origin =  { 0., 0., 0. };
        /// </summary>
        static IFCAnyHandle m_DefaultCartesianTransformationOperator3D;

        /// The HostPartsCache object.
        /// </summary>
        static HostPartsCache m_HostPartsCache;

        /// <summary>
        /// The DummyHostCache object.
        /// </summary>
        static DummyHostCache m_DummyHostCache;

        /// <summary>
        /// The StairRampContainerInfoCache object.
        /// </summary>
        static StairRampContainerInfoCache m_StairRampContainerInfoCache;

        /// <summary>
        /// The GridCache object.
        /// </summary>
        static List<Element> m_GridCache;

        /// <summary>
        /// This contains the mapping from Level element id to index in the IList returned by GetHostObjects.
        /// This is redundant with a native list that is being deprecated, which has inadequate API access.
        /// </summary>
        static IDictionary<ElementId, int> m_HostObjectsLevelIndex;
        
        /// <summary>
        /// The WallType cache that maps Revit wall type id to the IFC wall type handle.
        /// </summary>
        static IDictionary<ElementId, IFCAnyHandle> m_WallTypeCache;

        /// <summary>
        /// Keeps relationship of Ceiling to the Space(s) where it belongs to. Used to determine Space containment for Ceiling object that is fully contained in Space (for FMHandOverView)
        /// </summary>
        static IDictionary<ElementId, IList<ElementId>> m_CeilingSpaceRelCache;

        /// <summary>
        /// The SpaceInfo cache that maps Revit SpatialElement id to the SpaceInfo.
        /// </summary>
        static SpaceInfoCache m_SpaceInfoCache;

        /// <summary>
        /// The FabricArea id to FabricSheet handle cache.
        /// </summary>
        static IDictionary<ElementId, HashSet<IFCAnyHandle>> m_FabricAreaHandleCache;

        /// <summary>
        /// The ParameterCache object.
        /// </summary>
        public static AllocatedGeometryObjectCache AllocatedGeometryObjectCache
        {
            get
            {
                if (m_AllocatedGeometryObjectCache == null)
                    m_AllocatedGeometryObjectCache = new AllocatedGeometryObjectCache();
                return m_AllocatedGeometryObjectCache;
            }
        }

        /// <summary>
        /// The AssemblyInstanceCache object.
        /// </summary>
        public static AssemblyInstanceCache AssemblyInstanceCache
        {
            get
            {
                if (m_AssemblyInstanceCache == null)
                    m_AssemblyInstanceCache = new AssemblyInstanceCache();
                return m_AssemblyInstanceCache;
            }
        }

        /// <summary>
        /// The GroupElementGeometryCache object.
        /// </summary>
        public static GroupElementGeometryCache GroupElementGeometryCache
        {
            get
            {
                if (m_GroupElementGeometryCache == null)
                    m_GroupElementGeometryCache = new GroupElementGeometryCache();
                return m_GroupElementGeometryCache;
            }
        }

        /// <summary>
        /// The GUIDCache object.
        /// </summary>
        public static HashSet<string> GUIDCache
        {
            get
            {
                if (m_GUIDCache == null)
                    m_GUIDCache = new HashSet<string>();
                return m_GUIDCache;
            }
        }

        /// <summary>
        /// The HandleToElementCache object.
        /// </summary>
        public static HandleToElementCache HandleToElementCache
        {
            get
            {
                if (m_HandleToElementCache == null)
                    m_HandleToElementCache = new HandleToElementCache();
                return m_HandleToElementCache;
            }
        }

        /// <summary>
        /// The language of the current Revit document.
        /// </summary>
        public static LanguageType LanguageType
        {
            get { return m_LanguageType; }
            set { m_LanguageType = value; }
        }

        /// <summary>
        /// The ParameterCache object.
        /// </summary>
        public static ParameterCache ParameterCache
        {
            get
            {
                if (m_ParameterCache == null)
                    m_ParameterCache = new ParameterCache();
                return m_ParameterCache;
            }
        }

        /// <summary>
        /// The PartExportedCache object.
        /// </summary>
        public static PartExportedCache PartExportedCache
        {
            get
            {
                if (m_PartExportedCache == null)
                    m_PartExportedCache = new PartExportedCache();
                return m_PartExportedCache;
            }
        }
        /// <summary>
        /// The Document object passed to the Exporter class.
        /// </summary>
        public static Autodesk.Revit.DB.Document Document
        {
            get
            {
                if (m_Document == null)
                {
                    throw new InvalidOperationException("doc is null");
                }
                return m_Document;
            }
            set
            {
                m_Document = value;
            }
        }

        /// <summary>
        /// The PresentationLayerSetCache object.
        /// </summary>
        public static PresentationLayerSetCache PresentationLayerSetCache
        {
            get
            {
                if (m_PresentationLayerSetCache == null)
                    m_PresentationLayerSetCache = new PresentationLayerSetCache();
                return m_PresentationLayerSetCache;
            }
        }

        /// <summary>
        /// The PresentationStyleAssignmentCache object.
        /// </summary>
        public static PresentationStyleAssignmentCache PresentationStyleAssignmentCache
        {
            get
            {
                if (m_PresentationStyleCache == null)
                    m_PresentationStyleCache = new PresentationStyleAssignmentCache();
                return m_PresentationStyleCache;
            }
        }

        /// <summary>
        /// The CurveAnnotationCache object.
        /// </summary>
        public static CurveAnnotationCache CurveAnnotationCache
        {
            get
            {
                if (m_CurveAnnotationCache == null)
                    m_CurveAnnotationCache = new CurveAnnotationCache();
                return m_CurveAnnotationCache;
            }
        }

        /// <summary>
        /// The CurveAnnotationCache object.
        /// </summary>
        public static IDictionary<ElementId, ElementId> DBViewsToExport
        {
            get
            {
                if (m_DBViewsToExport == null)
                    m_DBViewsToExport = new Dictionary<ElementId, ElementId>();
                return m_DBViewsToExport;
            }
        }

        /// <summary>
        /// The cache containing the openings that need to be created for doors and windows.
        /// </summary>
        public static DoorWindowDelayedOpeningCreatorCache DoorWindowDelayedOpeningCreatorCache
        {
            get
            {
                if (m_DoorWindowDelayedOpeningCreatorCache == null)
                    m_DoorWindowDelayedOpeningCreatorCache = new DoorWindowDelayedOpeningCreatorCache();
                return m_DoorWindowDelayedOpeningCreatorCache;
            }
        }

        /// <summary>
        /// The MaterialLayerSetCache object.
        /// </summary>
        public static MaterialLayerSetCache MaterialLayerSetCache
        {
            get
            {
                if (m_MaterialLayerSetCache == null)
                    m_MaterialLayerSetCache = new MaterialLayerSetCache();
                return m_MaterialLayerSetCache;
            }
        }

        /// <summary>
        /// The MEPCache object.
        /// </summary>
        public static MEPCache MEPCache
        {
            get
            {
                if (m_MEPCache == null)
                    m_MEPCache = new MEPCache();
                return m_MEPCache;
            }
        }
        

        /// <summary>
        /// The SpaceBoundaryCache object.
        /// </summary>
        public static SpaceBoundaryCache SpaceBoundaryCache
        {
            get
            {
                if (m_SpaceBoundaryCache == null)
                    m_SpaceBoundaryCache = new SpaceBoundaryCache();
                return m_SpaceBoundaryCache;
            }
        }

        /// <summary>
        /// The SpaceInfo cache that maps Revit SpatialElement id to the SpaceInfo.
        /// </summary>
        public static SpaceInfoCache SpaceInfoCache
        {
            get
            {
                if (m_SpaceInfoCache == null)
                    m_SpaceInfoCache = new SpaceInfoCache();
                return m_SpaceInfoCache;
            }
        }

        /// <summary>
        /// The SystemsCache object.
        /// </summary>
        public static SystemsCache SystemsCache
        {
            get
            {
                if (m_SystemsCache == null)
                    m_SystemsCache = new SystemsCache();
                return m_SystemsCache;
            }
        }

        /// <summary>
        /// The MaterialHandleCache object.
        /// </summary>
        public static MaterialHandleCache MaterialHandleCache
        {
            get
            {
                if (m_MaterialHandleCache == null)
                    m_MaterialHandleCache = new MaterialHandleCache();
                return m_MaterialHandleCache;
            }
        }

        /// <summary>
        /// The MaterialRelationsCache object.
        /// </summary>
        public static MaterialRelationsCache MaterialRelationsCache
        {
            get
            {
                if (m_MaterialRelationsCache == null)
                    m_MaterialRelationsCache = new MaterialRelationsCache();
                return m_MaterialRelationsCache;
            }
        }

        /// <summary>
        /// The MaterialLayerRelationsCache object.
        /// </summary>
        public static MaterialLayerRelationsCache MaterialLayerRelationsCache
        {
            get
            {
                if (m_MaterialLayerRelationsCache == null)
                    m_MaterialLayerRelationsCache = new MaterialLayerRelationsCache();
                return m_MaterialLayerRelationsCache;
            }
        }

        /// <summary>
        /// The RailingCache object.
        /// </summary>
        public static HashSet<ElementId> RailingCache
        {
            get
            {
                if (m_RailingCache == null)
                    m_RailingCache = new HashSet<ElementId>();
                return m_RailingCache;
            }
        }

        /// <summary>
        /// The TrussCache object.
        /// </summary>
        public static HashSet<ElementId> TrussCache
        {
            get
            {
                if (m_TrussCache == null)
                    m_TrussCache = new HashSet<ElementId>();
                return m_TrussCache;
            }
        }

        /// <summary>
        /// The BeamSystemCache object.
        /// </summary>
        public static HashSet<ElementId> BeamSystemCache
        {
            get
            {
                if (m_BeamSystemCache == null)
                    m_BeamSystemCache = new HashSet<ElementId>();
                return m_BeamSystemCache;
            }
        }

        /// <summary>
        /// The AreaSchemeCache object.
        /// </summary>
        public static Dictionary<ElementId, HashSet<IFCAnyHandle>> AreaSchemeCache
        {
            get
            {
                if (m_AreaSchemeCache == null)
                    m_AreaSchemeCache = new Dictionary<ElementId, HashSet<IFCAnyHandle>>();
                return m_AreaSchemeCache;
            }
        }

        /// <summary>
        /// The ZoneCache object.
        /// </summary>
        public static HashSet<ElementId> ZoneCache
        {
            get
            {
                if (m_ZoneCache == null)
                    m_ZoneCache = new HashSet<ElementId>();
                return m_ZoneCache;
            }
        }

        /// <summary>
        /// The RailingSubElementCache object.  This ensures that we don't export sub-elements of railings (e.g. supports) separately.
        /// </summary>
        public static HashSet<ElementId> RailingSubElementCache
        {
            get
            {
                if (m_RailingSubElementCache == null)
                    m_RailingSubElementCache = new HashSet<ElementId>();
                return m_RailingSubElementCache;
            }
        }

        /// <summary>
        /// The TypeRelationsCache object.
        /// </summary>
        public static TypeRelationsCache TypeRelationsCache
        {
            get
            {
                if (m_TypeRelationsCache == null)
                    m_TypeRelationsCache = new TypeRelationsCache();
                return m_TypeRelationsCache;
            }
        }

        /// <summary>
        /// The TypeObjectsCache object.
        /// </summary>
        public static TypeObjectsCache TypeObjectsCache
        {
            get
            {
                if (m_TypeObjectsCache == null)
                    m_TypeObjectsCache = new TypeObjectsCache();
                return m_TypeObjectsCache;
            }
        }

        /// <summary>
        /// The ZoneInfoCache object.
        /// </summary>
        public static ZoneInfoCache ZoneInfoCache
        {
            get
            {
                if (m_ZoneInfoCache == null)
                    m_ZoneInfoCache = new ZoneInfoCache();
                return m_ZoneInfoCache;
            }
        }

        /// <summary>
        /// The SpaceOccupantInfoCache object.
        /// </summary>
        public static SpaceOccupantInfoCache SpaceOccupantInfoCache
        {
            get
            {
                if (m_SpaceOccupantInfoCache == null)
                    m_SpaceOccupantInfoCache = new SpaceOccupantInfoCache();
                return m_SpaceOccupantInfoCache;
            }
        }

        /// <summary>
        /// The WallConnectionDataCache object.
        /// </summary>
        public static WallConnectionDataCache WallConnectionDataCache
        {
            get
            {
                if (m_WallConnectionDataCache == null)
                    m_WallConnectionDataCache = new WallConnectionDataCache();
                return m_WallConnectionDataCache;
            }
        }

        /// <summary>
        /// The ExportOptionsCache object.
        /// </summary>
        public static ElementToHandleCache ElementToHandleCache
        {
            get
            {
                if (m_ElementToHandleCache == null)
                    m_ElementToHandleCache = new ElementToHandleCache();
                return m_ElementToHandleCache;
            }
        }

        /// <summary>
        /// The ExportOptionsCache object.
        /// </summary>
        public static ExportOptionsCache ExportOptionsCache
        {
            get { return m_ExportOptionsCache; }
            set { m_ExportOptionsCache = value; }
        }

        /// <summary>
        /// The ContainmentCache object.
        /// </summary>
        public static ContainmentCache ContainmentCache
        {
            get
            {
                if (m_ContainmentCache == null)
                    m_ContainmentCache = new ContainmentCache();
                return m_ContainmentCache;
            }
            set { m_ContainmentCache = value; }
        }

        /// <summary>
        /// The ClassificationCache object.
        /// </summary>
        public static ClassificationCache ClassificationCache
        {
            get
            {
                if (m_ClassificationCache == null)
                    m_ClassificationCache = new ClassificationCache();
                return m_ClassificationCache;
            }
            set { m_ClassificationCache = value; }
        }

        public static ClassificationLocationCache ClassificationLocationCache
        {
            get
            {
                if (m_ClassificationLocationCache == null)
                    m_ClassificationLocationCache = new ClassificationLocationCache();
                return m_ClassificationLocationCache;
            }
            set { m_ClassificationLocationCache = value; }
        }

        /// <summary>
        /// The UnitsCache object.
        /// </summary>
        public static UnitsCache UnitsCache
        {
            get
            {
                if (m_UnitsCache == null)
                    m_UnitsCache = new UnitsCache();
                return m_UnitsCache;
            }
            set { m_UnitsCache = value; }
        }

        /// <summary>
        /// The HostPartsCache object.
        /// </summary>
        public static HostPartsCache HostPartsCache
        {
            get
            {
                if (m_HostPartsCache == null)
                    m_HostPartsCache = new HostPartsCache();
                return m_HostPartsCache;
            }
        }

        /// <summary>
        /// The DummyHostCache object.
        /// </summary>
        public static DummyHostCache DummyHostCache
        {
            get
            {
                if (m_DummyHostCache == null)
                    m_DummyHostCache = new DummyHostCache();
                return m_DummyHostCache;
            }
        }

        /// <summary>
        /// The LevelInfoCache object.
        /// </summary>
        public static LevelInfoCache LevelInfoCache
        {
            get
            {
                if (m_LevelInfoCache == null)
                    m_LevelInfoCache = new LevelInfoCache();
                return m_LevelInfoCache;
            }
        }

        /// <summary>
        /// The TypePropertyInfoCache object.
        /// </summary>
        public static TypePropertyInfoCache TypePropertyInfoCache
        {
            get
            {
                if (m_TypePropertyInfoCache == null)
                    m_TypePropertyInfoCache = new TypePropertyInfoCache();
                return m_TypePropertyInfoCache;
            }
        }

        /// <summary>
        /// The PropertyInfoCache object.
        /// </summary>
        public static PropertyInfoCache PropertyInfoCache
        {
            get
            {
                if (m_PropertyInfoCache == null)
                    m_PropertyInfoCache = new PropertyInfoCache();
                return m_PropertyInfoCache;
            }
        }
        
        /// <summary>
        /// The common property sets to be exported for an entity type, regardless of Object Type.
        /// </summary>
        public static IDictionary<IFCEntityType, IList<PropertySetDescription>> PropertySetsForTypeCache
        {
            get
            {
                if (m_PropertySetsForTypeCache == null)
                    m_PropertySetsForTypeCache = new Dictionary<IFCEntityType, IList<PropertySetDescription>>();
                return m_PropertySetsForTypeCache;
            }
        }

        /// <summary>
        /// The common property sets to be exported for an entity type, conditional on the Object Type of the
        /// entity matching that of the PropertySetDescription.
        /// </summary>
        public static IDictionary<IFCEntityType, IList<PropertySetDescription>> ConditionalPropertySetsForTypeCache
        {
            get
            {
                if (m_ConditionalPropertySetsForTypeCache == null)
                    m_ConditionalPropertySetsForTypeCache = new Dictionary<IFCEntityType, IList<PropertySetDescription>>();
                return m_ConditionalPropertySetsForTypeCache;
            }
        }

        /// <summary>
        /// The material id to style handle cache.
        /// </summary>
        public static ElementToHandleCache MaterialIdToStyleHandleCache
        {
            get
            {
                if (m_MaterialIdToStyleHandleCache == null)
                    m_MaterialIdToStyleHandleCache = new ElementToHandleCache();
                return m_MaterialIdToStyleHandleCache;
            }
        }

        /// <summary>
        /// The elements in assemblies cache.
        /// </summary>
        public static ISet<IFCAnyHandle> ElementsInAssembliesCache
        {
            get
            {
                if (m_ElementsInAssembliesCache == null)
                    m_ElementsInAssembliesCache = new HashSet<IFCAnyHandle>();
                return m_ElementsInAssembliesCache;
            }
        }

        public static IFCAnyHandle GetDefaultCartesianTransformationOperator3D(IFCFile file)
        {
            if (m_DefaultCartesianTransformationOperator3D == null)
            {
                XYZ orig = new XYZ();
                IFCAnyHandle origHnd = ExporterUtil.CreateCartesianPoint(file, orig);
                m_DefaultCartesianTransformationOperator3D = IFCInstanceExporter.CreateCartesianTransformationOperator3D(file, null, null, origHnd, 1.0, null);
            }
            return m_DefaultCartesianTransformationOperator3D;
        }

        /// <summary>
        /// The StairRampContainerInfoCache object.
        /// </summary>
        public static StairRampContainerInfoCache StairRampContainerInfoCache
        {
            get
            {
                if (m_StairRampContainerInfoCache == null)
                    m_StairRampContainerInfoCache = new StairRampContainerInfoCache();
                return m_StairRampContainerInfoCache;
            }
        }

        /// <summary>
        /// The GridCache object.
        /// </summary>
        public static List<Element> GridCache
        {
            get
            {
                if (m_GridCache == null)
                    m_GridCache = new List<Element>();
                return m_GridCache;
            }
        }

        /// <summary>
        /// This contains the mapping from Level element id to index in the IList returned by GetHostObjects.
        /// This is redundant with a native list that is being deprecated, which has inadequate API access.
        /// </summary>
        public static IDictionary<ElementId, int> HostObjectsLevelIndex
        {
            get
            {
                if (m_HostObjectsLevelIndex == null)
                    m_HostObjectsLevelIndex = new Dictionary<ElementId, int>();
                return m_HostObjectsLevelIndex;
            }
        }

        /// <summary>
        /// The WallType cache that maps Revit wall type id to the IFC wall type handle.
        /// </summary>
        public static IDictionary<ElementId, IFCAnyHandle> WallTypeCache
        {
            get
            {
                if (m_WallTypeCache == null)
                    m_WallTypeCache = new Dictionary<ElementId, IFCAnyHandle>();
                return m_WallTypeCache;
            }
        }

        /// <summary>
        /// Ceiling and Space relationship cache. We need it to check whether a Ceiling should be contained in a Space later on when exporting Ceiling
        /// </summary>
        public static IDictionary<ElementId, IList<ElementId>> CeilingSpaceRelCache
        {
            get
            {
                if (m_CeilingSpaceRelCache == null)
                    m_CeilingSpaceRelCache = new Dictionary<ElementId, IList<ElementId>>();
                return m_CeilingSpaceRelCache;
            }
        }

        /// <summary>
        /// The FabricArea id to FabricSheet handle cache.
        /// </summary>
        public static IDictionary<ElementId, HashSet<IFCAnyHandle>> FabricAreaHandleCache
        {
            get
            {
                if (m_FabricAreaHandleCache == null)
                    m_FabricAreaHandleCache = new Dictionary<ElementId, HashSet<IFCAnyHandle>>();
                return m_FabricAreaHandleCache;
            }
        }

        
        /// <summary>
        /// Clear all caches contained in this manager.
        /// </summary>
        public static void Clear()
        {
            if (m_AllocatedGeometryObjectCache != null)
                m_AllocatedGeometryObjectCache.DisposeCache();
            ParameterUtil.ClearParameterCache();

            m_AllocatedGeometryObjectCache = null;
            m_AreaSchemeCache = null;
            m_AssemblyInstanceCache = null;
            m_BeamSystemCache = null;
            m_CeilingSpaceRelCache = null;
            m_ClassificationCache = null;
            m_ClassificationLocationCache = null;
            m_ConditionalPropertySetsForTypeCache = null;
            m_ContainmentCache = null;
            m_CurveAnnotationCache = null;
            m_DBViewsToExport = null;
            m_DefaultCartesianTransformationOperator3D = null;
            m_DoorWindowDelayedOpeningCreatorCache = null;
            m_DummyHostCache = null;
            m_ElementToHandleCache = null;
            m_ElementsInAssembliesCache = null;
            m_ExportOptionsCache = null;
            m_FabricAreaHandleCache = null;
            m_GridCache = null;
            m_GroupElementGeometryCache = null;
            m_GUIDCache = null;
            m_HandleToElementCache = null;
            m_HostObjectsLevelIndex = null;
            m_HostPartsCache = null;
            m_LevelInfoCache = null;
            m_MaterialIdToStyleHandleCache = null;
            m_MaterialLayerRelationsCache = null;
            m_MaterialLayerSetCache = null;
            m_MaterialHandleCache = null;
            m_MaterialRelationsCache = null;
            m_MEPCache = null;
            m_ParameterCache = null;
            m_PartExportedCache = null;
            m_PresentationLayerSetCache = null;
            m_PresentationStyleCache = null;
            m_PropertyInfoCache = null;
            m_PropertySetsForTypeCache = null;
            m_RailingCache = null;
            m_RailingSubElementCache = null;
            m_SpaceBoundaryCache = null;
            m_SpaceInfoCache = null;
            m_SpaceOccupantInfoCache = null;
            m_StairRampContainerInfoCache = null;
            m_SystemsCache = null;
            m_TrussCache = null;
            m_TypeObjectsCache = null;
            m_TypePropertyInfoCache = null;
            m_TypeRelationsCache = null;
            m_WallConnectionDataCache = null;
            m_WallTypeCache = null;
            m_UnitsCache = null;
            m_ZoneCache = null;
            m_ZoneInfoCache = null;
        }
    }
}
