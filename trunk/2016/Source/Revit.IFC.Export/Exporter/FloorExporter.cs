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
using System.Text;
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB.Structure;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Common.Utility;


namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Provides methods to export floor elements.
   /// </summary>
   class FloorExporter
   {
      /// <summary>
      /// Exports a generic element as an IfcSlab.</summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="floor">The floor element.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="ifcEnumType">The string value represents the IFC type.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      /// <returns>True if the floor is exported successfully, false otherwise.</returns>
      public static void ExportGenericSlab(ExporterIFC exporterIFC, Element slabElement, GeometryElement geometryElement, string ifcEnumType,
          ProductWrapper productWrapper)
      {
         if (geometryElement == null)
            return;

         IFCFile file = exporterIFC.GetFile();

         using (IFCTransaction tr = new IFCTransaction(file))
         {
            using (IFCTransformSetter transformSetter = IFCTransformSetter.Create())
            {
               using (PlacementSetter placementSetter = PlacementSetter.Create(exporterIFC, slabElement))
               {
                  using (IFCExtrusionCreationData ecData = new IFCExtrusionCreationData())
                  {
                     bool exportParts = PartExporter.CanExportParts(slabElement);

                     IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
                     IFCAnyHandle localPlacement = placementSetter.LocalPlacement;

                     IFCAnyHandle prodDefHnd = null;
                     bool isBRepSlabHnd = false;

                     if (!exportParts)
                     {
                        ecData.SetLocalPlacement(localPlacement);

                        ElementId catId = CategoryUtil.GetSafeCategoryId(slabElement);

                        BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true);
                        bodyExporterOptions.TessellationLevel = BodyExporter.GetTessellationLevel();
                        BodyData bodyData;
                        prodDefHnd = RepresentationUtil.CreateAppropriateProductDefinitionShape(exporterIFC,
                            slabElement, catId, geometryElement, bodyExporterOptions, null, ecData, out bodyData);
                        if (IFCAnyHandleUtil.IsNullOrHasNoValue(prodDefHnd))
                        {
                           ecData.ClearOpenings();
                           return;
                        }
                        isBRepSlabHnd = (bodyData.ShapeRepresentationType == ShapeRepresentationType.Brep);
                     }

                     // Create the slab from either the extrusion or the BRep information.
                     string ifcGUID = GUIDUtil.CreateGUID(slabElement);

                     string entityType = IFCValidateEntry.GetValidIFCType<IFCSlabType>(slabElement, ifcEnumType, "FLOOR");

                     string ifcName = NamingUtil.GetNameOverride(slabElement, NamingUtil.GetIFCName(slabElement));
                     string ifcDescription = NamingUtil.GetDescriptionOverride(slabElement, null);
                     string ifcObjectType = NamingUtil.GetObjectTypeOverride(slabElement, exporterIFC.GetFamilyName());
                     string ifcTag = NamingUtil.GetTagOverride(slabElement, NamingUtil.CreateIFCElementId(slabElement));

                     IFCAnyHandle slabHnd = IFCInstanceExporter.CreateSlab(file, ifcGUID, ownerHistory, ifcName,
                             ifcDescription, ifcObjectType, localPlacement, exportParts ? null : prodDefHnd,
                             ifcTag, entityType);

                     if (IFCAnyHandleUtil.IsNullOrHasNoValue(slabHnd))
                        return;

                     if (exportParts)
                        PartExporter.ExportHostPart(exporterIFC, slabElement, slabHnd, productWrapper, placementSetter, localPlacement, null);

                     productWrapper.AddElement(slabElement, slabHnd, placementSetter, ecData, true);

                     if (!exportParts)
                     {
                        if (slabElement is HostObject)
                        {
                           HostObject hostObject = slabElement as HostObject;

                           HostObjectExporter.ExportHostObjectMaterials(exporterIFC, hostObject, slabHnd,
                               geometryElement, productWrapper, ElementId.InvalidElementId, Toolkit.IFCLayerSetDirection.Axis3, isBRepSlabHnd);
                        }
                        else if (slabElement is FamilyInstance)
                        {
                           ElementId matId = BodyExporter.GetBestMaterialIdFromGeometryOrParameter(geometryElement, exporterIFC, slabElement);
                           Document doc = slabElement.Document;
                           CategoryUtil.CreateMaterialAssociation(exporterIFC, slabHnd, matId);
                        }

                        OpeningUtil.CreateOpeningsIfNecessary(slabHnd, slabElement, ecData, null,
                            exporterIFC, ecData.GetLocalPlacement(), placementSetter, productWrapper);
                     }
                  }
               }
               tr.Commit();

               return;
            }
         }
      }

      /// <summary>
      /// Exports a CeilingAndFloor element to IFC.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="floor">The floor element.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static void ExportCeilingAndFloorElement(ExporterIFC exporterIFC, CeilingAndFloor floorElement, GeometryElement geometryElement,
          ProductWrapper productWrapper)
      {
         if (geometryElement == null)
            return;

         // export parts or not
         bool exportParts = PartExporter.CanExportParts(floorElement);
         if (exportParts && !PartExporter.CanExportElementInPartExport(floorElement, floorElement.LevelId, false))
            return;

         IFCFile file = exporterIFC.GetFile();
         
         string ifcEnumType;
         IFCExportType exportType = ExporterUtil.GetExportType(exporterIFC, floorElement, out ifcEnumType);

         using (IFCTransaction tr = new IFCTransaction(file))
         {
            bool canExportAsContainerOrWithExtrusionAnalyzer = (!exportParts && (floorElement is Floor));
            
            if (canExportAsContainerOrWithExtrusionAnalyzer)
            {
               // Try to export the Floor slab as a container.  If that succeeds, we are done.
               // If we do export the floor as a container, it will take care of the local placement and transform there, so we need to leave
               // this out of the IFCTransformSetter and PlacementSetter scopes below, or else we'll get double transforms.
               IFCAnyHandle floorHnd = RoofExporter.ExportRoofOrFloorAsContainer(exporterIFC, ifcEnumType, floorElement, geometryElement, productWrapper);
               if (!IFCAnyHandleUtil.IsNullOrHasNoValue(floorHnd))
               {
                  tr.Commit();
                  return;
               }
            }

            IList<IFCAnyHandle> slabHnds = new List<IFCAnyHandle>();
            IList<IFCAnyHandle> brepSlabHnds = new List<IFCAnyHandle>();
            IList<IFCAnyHandle> nonBrepSlabHnds = new List<IFCAnyHandle>();

            using (IFCTransformSetter transformSetter = IFCTransformSetter.Create())
            {
               using (PlacementSetter placementSetter = PlacementSetter.Create(exporterIFC, floorElement))
               {
                  IFCAnyHandle localPlacement = placementSetter.LocalPlacement;
                  IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

                  // The routine ExportExtrudedSlabOpenings is called if exportedAsInternalExtrusion is true, and it requires having a valid level association.
                  // Disable calling ExportSlabAsExtrusion if we can't handle potential openings.
                  bool canExportAsInternalExtrusion = placementSetter.LevelInfo != null;
                  bool exportedAsInternalExtrusion = false;

                  ElementId catId = CategoryUtil.GetSafeCategoryId(floorElement);

                  IList<IFCAnyHandle> prodReps = new List<IFCAnyHandle>();
                  IList<ShapeRepresentationType> repTypes = new List<ShapeRepresentationType>();
                  IList<IList<CurveLoop>> extrusionLoops = new List<IList<CurveLoop>>();
                  IList<IFCExtrusionCreationData> loopExtraParams = new List<IFCExtrusionCreationData>();
                  Plane floorPlane = GeometryUtil.CreateDefaultPlane();

                  IList<IFCAnyHandle> localPlacements = new List<IFCAnyHandle>();

                  if (canExportAsContainerOrWithExtrusionAnalyzer)
                  {
                     Floor floor = floorElement as Floor;

                     // Next, try to use the ExtrusionAnalyzer for the limited cases it handles - 1 solid, no openings, end clippings only.
                     // Also limited to cases with line and arc boundaries.
                     //
                     SolidMeshGeometryInfo solidMeshInfo = GeometryUtil.GetSplitSolidMeshGeometry(geometryElement);
                     IList<Solid> solids = solidMeshInfo.GetSolids();
                     IList<Mesh> meshes = solidMeshInfo.GetMeshes();

                     if (solids.Count == 1 && meshes.Count == 0)
                     {
                        bool completelyClipped;
                        // floorExtrusionDirection is set to (0, 0, -1) because extrusionAnalyzerFloorPlane is computed from the top face of the floor
                        XYZ floorExtrusionDirection = new XYZ(0, 0, -1);
                        XYZ modelOrigin = XYZ.Zero;

                        XYZ floorOrigin = floor.GetVerticalProjectionPoint(modelOrigin, FloorFace.Top);
                        if (floorOrigin == null)
                        {
                           // GetVerticalProjectionPoint may return null if FloorFace.Top is an edited face that doesn't 
                           // go thruough te Revit model orgigin.  We'll try the midpoint of the bounding box instead.
                           BoundingBoxXYZ boundingBox = floorElement.get_BoundingBox(null);
                           modelOrigin = (boundingBox.Min + boundingBox.Max) / 2.0;
                           floorOrigin = floor.GetVerticalProjectionPoint(modelOrigin, FloorFace.Top);
                        }

                        if (floorOrigin != null)
                        {
                           XYZ floorDir = floor.GetNormalAtVerticalProjectionPoint(floorOrigin, FloorFace.Top);
                           Plane extrusionAnalyzerFloorPlane = new Plane(floorDir, floorOrigin);

                           HandleAndData floorAndProperties =
                               ExtrusionExporter.CreateExtrusionWithClippingAndProperties(exporterIFC, floorElement,
                               catId, solids[0], extrusionAnalyzerFloorPlane, floorExtrusionDirection, null, out completelyClipped);
                           if (completelyClipped)
                              return;
                           if (floorAndProperties.Handle != null)
                           {
                              IList<IFCAnyHandle> representations = new List<IFCAnyHandle>();
                              representations.Add(floorAndProperties.Handle);
                              IFCAnyHandle prodRep = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, representations);
                              prodReps.Add(prodRep);
                              repTypes.Add(ShapeRepresentationType.SweptSolid);

                              if (floorAndProperties.Data != null)
                                 loopExtraParams.Add(floorAndProperties.Data);
                           }
                        }
                     }
                  }

                  // Use internal routine as backup that handles openings.
                  if (prodReps.Count == 0 && canExportAsInternalExtrusion)
                  {
                     exportedAsInternalExtrusion = ExporterIFCUtils.ExportSlabAsExtrusion(exporterIFC, floorElement,
                         geometryElement, transformSetter, localPlacement, out localPlacements, out prodReps,
                         out extrusionLoops, out loopExtraParams, floorPlane);
                     for (int ii = 0; ii < prodReps.Count; ii++)
                     {
                        // all are extrusions
                        repTypes.Add(ShapeRepresentationType.SweptSolid);
                     }
                  }

                  if (prodReps.Count == 0)
                  {
                     using (IFCExtrusionCreationData ecData = new IFCExtrusionCreationData())
                     {
                        BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true);
                        bodyExporterOptions.TessellationLevel = BodyExporter.GetTessellationLevel();
                        BodyData bodyData;
                        IFCAnyHandle prodDefHnd = RepresentationUtil.CreateAppropriateProductDefinitionShape(exporterIFC,
                            floorElement, catId, geometryElement, bodyExporterOptions, null, ecData, out bodyData);
                        if (IFCAnyHandleUtil.IsNullOrHasNoValue(prodDefHnd))
                        {
                           ecData.ClearOpenings();
                           return;
                        }

                        prodReps.Add(prodDefHnd);
                        repTypes.Add(bodyData.ShapeRepresentationType);
                     }
                  }

                  // Create the slab from either the extrusion or the BRep information.
                  string ifcGUID = GUIDUtil.CreateGUID(floorElement);

                  int numReps = exportParts ? 1 : prodReps.Count;

                  string entityType = null;

                  switch (exportType)
                  {
                     case IFCExportType.IfcFooting:
                        if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
                           entityType = IFCValidateEntry.GetValidIFCType<Revit.IFC.Export.Toolkit.IFC4.IFCFootingType>(floorElement, ifcEnumType, null);
                        else
                           entityType = IFCValidateEntry.GetValidIFCType<IFCFootingType>(floorElement, ifcEnumType, null);
                        break;
                     case IFCExportType.IfcCovering:
                        entityType = IFCValidateEntry.GetValidIFCType<IFCCoveringType>(floorElement, ifcEnumType, "FLOORING");
                        break;
                     case IFCExportType.IfcRamp:
                        if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
                           entityType = IFCValidateEntry.GetValidIFCType<Revit.IFC.Export.Toolkit.IFC4.IFCRampType>(floorElement, ifcEnumType, null);
                        else
                           entityType = IFCValidateEntry.GetValidIFCType<IFCRampType>(floorElement, ifcEnumType, null);
                        break;
                     default:
                        bool isBaseSlab = false;
                        AnalyticalModel analyticalModel = floorElement.GetAnalyticalModel();
                        if (analyticalModel != null)
                        {
                           AnalyzeAs slabFoundationType = analyticalModel.GetAnalyzeAs();
                           isBaseSlab = (slabFoundationType == AnalyzeAs.SlabOnGrade) || (slabFoundationType == AnalyzeAs.Mat);
                        }
                        entityType = IFCValidateEntry.GetValidIFCType<IFCSlabType>(floorElement, ifcEnumType, isBaseSlab ? "BASESLAB" : "FLOOR");
                        break;
                  }

                  for (int ii = 0; ii < numReps; ii++)
                  {
                     string ifcName = NamingUtil.GetNameOverride(floorElement, NamingUtil.GetIFCNamePlusIndex(floorElement, ii == 0 ? -1 : ii + 1));
                     string ifcDescription = NamingUtil.GetDescriptionOverride(floorElement, null);
                     string ifcObjectType = NamingUtil.GetObjectTypeOverride(floorElement, exporterIFC.GetFamilyName());
                     string ifcTag = NamingUtil.GetTagOverride(floorElement, NamingUtil.CreateIFCElementId(floorElement));

                     string currentGUID = (ii == 0) ? ifcGUID : GUIDUtil.CreateGUID();
                     IFCAnyHandle localPlacementHnd = exportedAsInternalExtrusion ? localPlacements[ii] : localPlacement;

                     IFCAnyHandle slabHnd = null;

                     // TODO: replace with CreateGenericBuildingElement.
                     switch (exportType)
                     {
                        case IFCExportType.IfcFooting:
                           slabHnd = IFCInstanceExporter.CreateFooting(file, currentGUID, ownerHistory, ifcName,
                               ifcDescription, ifcObjectType, localPlacementHnd, exportParts ? null : prodReps[ii],
                               ifcTag, entityType);
                           break;
                        case IFCExportType.IfcCovering:
                           slabHnd = IFCInstanceExporter.CreateCovering(file, currentGUID, ownerHistory, ifcName,
                               ifcDescription, ifcObjectType, localPlacementHnd, exportParts ? null : prodReps[ii],
                               ifcTag, entityType);
                           break;
                        case IFCExportType.IfcRamp:
                           slabHnd = IFCInstanceExporter.CreateRamp(file, currentGUID, ownerHistory, ifcName,
                               ifcDescription, ifcObjectType, localPlacementHnd, exportParts ? null : prodReps[ii],
                               ifcTag, entityType);
                           break;
                        default:
                           slabHnd = IFCInstanceExporter.CreateSlab(file, currentGUID, ownerHistory, ifcName,
                               ifcDescription, ifcObjectType, localPlacementHnd, exportParts ? null : prodReps[ii],
                               ifcTag, entityType);
                           break;
                     }

                     if (IFCAnyHandleUtil.IsNullOrHasNoValue(slabHnd))
                        return;

                     if (exportParts)
                        PartExporter.ExportHostPart(exporterIFC, floorElement, slabHnd, productWrapper, placementSetter, localPlacementHnd, null);

                     slabHnds.Add(slabHnd);

                     if (!exportParts)
                     {
                        if (repTypes[ii] == ShapeRepresentationType.Brep)
                           brepSlabHnds.Add(slabHnd);
                        else
                           nonBrepSlabHnds.Add(slabHnd);
                     }
                  }

                  for (int ii = 0; ii < numReps; ii++)
                  {
                     IFCExtrusionCreationData loopExtraParam = ii < loopExtraParams.Count ? loopExtraParams[ii] : null;
                     productWrapper.AddElement(floorElement, slabHnds[ii], placementSetter, loopExtraParam, true);
                  }

                  // This call to the native function appears to create Brep opening also when appropriate. But the creation of the IFC instances is not
                  //   controllable from the managed code. Therefore in some cases BRep geometry for Opening will still be exported even in the Reference View
                  if (exportedAsInternalExtrusion)
                     ExporterIFCUtils.ExportExtrudedSlabOpenings(exporterIFC, floorElement, placementSetter.LevelInfo,
                        localPlacements[0], slabHnds, extrusionLoops, floorPlane, productWrapper.ToNative());
               }

               if (!exportParts)
               {
                  if (nonBrepSlabHnds.Count > 0)
                  {
                     HostObjectExporter.ExportHostObjectMaterials(exporterIFC, floorElement, nonBrepSlabHnds,
                         geometryElement, productWrapper, ElementId.InvalidElementId, Toolkit.IFCLayerSetDirection.Axis3, false);
                  }
                  if (brepSlabHnds.Count > 0)
                  {
                     HostObjectExporter.ExportHostObjectMaterials(exporterIFC, floorElement, brepSlabHnds,
                         geometryElement, productWrapper, ElementId.InvalidElementId, Toolkit.IFCLayerSetDirection.Axis3, true);
                  }
               }
            }
            tr.Commit();

            return;
         }
      }

      /// <summary>
      /// Gets IFCSlabType from slab type name.
      /// </summary>
      /// <param name="ifcEnumType">The slab type name.</param>
      /// <returns>The IFCSlabType.</returns>
      public static IFCSlabType GetIFCSlabType(string ifcEnumType)
      {
         if (String.IsNullOrEmpty(ifcEnumType))
            return IFCSlabType.Floor;

         string ifcEnumTypeWithoutSpaces = NamingUtil.RemoveSpacesAndUnderscores(ifcEnumType);

         if (String.Compare(ifcEnumTypeWithoutSpaces, "USERDEFINED", true) == 0)
            return IFCSlabType.UserDefined;
         if (String.Compare(ifcEnumTypeWithoutSpaces, "FLOOR", true) == 0)
            return IFCSlabType.Floor;
         if (String.Compare(ifcEnumTypeWithoutSpaces, "ROOF", true) == 0)
            return IFCSlabType.Roof;
         if (String.Compare(ifcEnumTypeWithoutSpaces, "LANDING", true) == 0)
            return IFCSlabType.Landing;
         if (String.Compare(ifcEnumTypeWithoutSpaces, "BASESLAB", true) == 0)
            return IFCSlabType.BaseSlab;

         return IFCSlabType.Floor;
      }
   }
}
