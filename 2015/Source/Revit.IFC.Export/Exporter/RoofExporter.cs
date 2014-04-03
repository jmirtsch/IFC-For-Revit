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
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Exporter
{
    /// <summary>
    /// Provides methods to export Roof elements.
    /// </summary>
    class RoofExporter
    {
        /// <summary>
        /// Exports a roof to IfcRoof.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="ifcEnumType">The roof type.</param>
        /// <param name="roof">The roof element.</param>
        /// <param name="geometryElement">The geometry element.</param>
        /// <param name="productWrapper">The ProductWrapper.</param>
        public static void ExportRoof(ExporterIFC exporterIFC, string ifcEnumType, Element roof, GeometryElement geometryElement,
            ProductWrapper productWrapper)
        {
            if (roof == null || geometryElement == null)
                return;

            IFCFile file = exporterIFC.GetFile();

            using (IFCTransaction tr = new IFCTransaction(file))
            {
                using (PlacementSetter placementSetter = PlacementSetter.Create(exporterIFC, roof))
                {
                    using (IFCExtrusionCreationData ecData = new IFCExtrusionCreationData())
                    {
                        ecData.PossibleExtrusionAxes = IFCExtrusionAxes.TryZ;
                        ecData.AreInnerRegionsOpenings = true;
                        ecData.SetLocalPlacement(placementSetter.LocalPlacement);

                        ElementId categoryId = CategoryUtil.GetSafeCategoryId(roof);

                        BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true);
                        BodyData bodyData;
                        IFCAnyHandle representation = RepresentationUtil.CreateAppropriateProductDefinitionShape(exporterIFC, roof,
                            categoryId, geometryElement, bodyExporterOptions, null, ecData, out bodyData);

                        if (IFCAnyHandleUtil.IsNullOrHasNoValue(representation))
                        {
                            ecData.ClearOpenings();
                            return;
                        }

                        bool exportSlab = ecData.ScaledLength > MathUtil.Eps();

                        string guid = GUIDUtil.CreateGUID(roof);
                        IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();
                        string roofName = NamingUtil.GetNameOverride(roof, NamingUtil.GetIFCName(roof));
                        string roofDescription = NamingUtil.GetDescriptionOverride(roof, null);
                        string roofObjectType = NamingUtil.GetObjectTypeOverride(roof, NamingUtil.CreateIFCObjectName(exporterIFC, roof));
                        IFCAnyHandle localPlacement = ecData.GetLocalPlacement();
                        string elementTag = NamingUtil.GetTagOverride(roof, NamingUtil.CreateIFCElementId(roof));
                        string roofType = GetIFCRoofType(ifcEnumType);
                        roofType = IFCValidateEntry.GetValidIFCType(roof, ifcEnumType);

                        IFCAnyHandle roofHnd = IFCInstanceExporter.CreateRoof(file, guid, ownerHistory, roofName, roofDescription,
                            roofObjectType, localPlacement, exportSlab ? null : representation, elementTag, roofType);

                        productWrapper.AddElement(roof, roofHnd, placementSetter.LevelInfo, ecData, true);

                        // will export its host object materials later if it is a roof
                        if (!(roof is RoofBase))
                            CategoryUtil.CreateMaterialAssociations(exporterIFC, roofHnd, bodyData.MaterialIds);

                        if (exportSlab)
                        {
                            string slabGUID = GUIDUtil.CreateSubElementGUID(roof, (int)IFCRoofSubElements.RoofSlabStart);
                            string slabName = roofName + ":1";
                            IFCAnyHandle slabLocalPlacementHnd = ExporterUtil.CopyLocalPlacement(file, localPlacement);

                            IFCAnyHandle slabHnd = IFCInstanceExporter.CreateSlab(file, slabGUID, ownerHistory, slabName,
                               roofDescription, roofObjectType, slabLocalPlacementHnd, representation, elementTag, "ROOF");

                            Transform offsetTransform = (bodyData != null) ? bodyData.OffsetTransform : Transform.Identity;
                            OpeningUtil.CreateOpeningsIfNecessary(slabHnd, roof, ecData, offsetTransform,
                                exporterIFC, slabLocalPlacementHnd, placementSetter, productWrapper);

                            ExporterUtil.RelateObject(exporterIFC, roofHnd, slabHnd);

                            productWrapper.AddElement(null, slabHnd, placementSetter.LevelInfo, ecData, false);
                            CategoryUtil.CreateMaterialAssociations(exporterIFC, slabHnd, bodyData.MaterialIds);
                        }
                    }
                    tr.Commit();
                }
            }
        }
        
        /// <summary>
        /// Exports a roof to IfcRoof.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="roof">The roof element.</param>
        /// <param name="geometryElement">The geometry element.</param>
        /// <param name="productWrapper">The ProductWrapper.</param>
        public static void Export(ExporterIFC exporterIFC, RoofBase roof, GeometryElement geometryElement, ProductWrapper productWrapper)
        {
            IFCFile file = exporterIFC.GetFile();
            using (IFCTransaction tr = new IFCTransaction(file))
            {
                // export parts or not
                bool exportParts = PartExporter.CanExportParts(roof);
                bool exportAsCurtainRoof = CurtainSystemExporter.IsCurtainSystem(roof);

                if (exportParts)
                {
                    if (!PartExporter.CanExportElementInPartExport(roof, roof.LevelId, false))
                        return;
                    ExportRoofAsParts(exporterIFC, roof, geometryElement, productWrapper); // Right now, only flat roof could have parts.
                }
                else if (exportAsCurtainRoof)
                {
                    CurtainSystemExporter.ExportCurtainRoof(exporterIFC, roof, productWrapper);
                }
                else
                {
                    string ifcEnumType = ExporterUtil.GetIFCTypeFromExportTable(exporterIFC, roof);
                    
                    IFCAnyHandle roofHnd = ExportRoofAsContainer(exporterIFC, ifcEnumType, roof, 
                        geometryElement, productWrapper);
                    if (IFCAnyHandleUtil.IsNullOrHasNoValue(roofHnd))
                        ExportRoof(exporterIFC, ifcEnumType, roof, geometryElement, productWrapper);

                    // call for host objects; curtain roofs excused from call (no material information)
                    HostObjectExporter.ExportHostObjectMaterials(exporterIFC, roof, productWrapper.GetAnElement(),
                        geometryElement, productWrapper, ElementId.InvalidElementId, IFCLayerSetDirection.Axis3, null);
                }
                tr.Commit();
            }
        }

        /// <summary>
        ///  Exports a roof as a container of multiple roof slabs.  Returns the handle, if successful.
        /// </summary>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="ifcEnumType">The roof type.</param>
        /// <param name="element">The roof element.</param>
        /// <param name="geometry">The geometry of the element.</param>
        /// <param name="productWrapper">The product wrapper.</param>
        /// <returns>The roof handle.</returns>
        public static IFCAnyHandle ExportRoofAsContainer(ExporterIFC exporterIFC, string ifcEnumType, Element element, GeometryElement geometry, ProductWrapper productWrapper)
        {
            IFCFile file = exporterIFC.GetFile();

            if (!(element is ExtrusionRoof) && !(element is FootPrintRoof))
                return null;

            using (IFCTransaction transaction = new IFCTransaction(file))
            {
                using (PlacementSetter setter = PlacementSetter.Create(exporterIFC, element))
                {
                    IFCAnyHandle localPlacement = setter.LocalPlacement;
                    RoofComponents roofComponents = null;
                    try
                    {
                        roofComponents = ExporterIFCUtils.GetRoofComponents(exporterIFC, element as RoofBase);
                    }
                    catch
                    {
                        return null;
                    }

                    if (roofComponents == null)
                        return null;

                    try
                    {
                        using (IFCExtrusionCreationData extrusionCreationData = new IFCExtrusionCreationData())
                        {
                            IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();
                            extrusionCreationData.SetLocalPlacement(localPlacement);
                            extrusionCreationData.ReuseLocalPlacement = true;

                            using (TransformSetter trfSetter = TransformSetter.Create())
                            {
                                IList<GeometryObject> geometryList = new List<GeometryObject>();
                                geometryList.Add(geometry);
                                trfSetter.InitializeFromBoundingBox(exporterIFC, geometryList, extrusionCreationData);

                                IFCAnyHandle prodRepHnd = null;

                                string elementGUID = GUIDUtil.CreateGUID(element);
                                string elementName = NamingUtil.GetIFCName(element);
                                string elementDescription = NamingUtil.GetDescriptionOverride(element, null);
                                string elementObjectType = NamingUtil.GetObjectTypeOverride(element, exporterIFC.GetFamilyName());
                                string elementId = NamingUtil.CreateIFCElementId(element);
                                string roofType = IFCValidateEntry.GetValidIFCType(element, ifcEnumType);

                                IFCAnyHandle roofHandle = IFCInstanceExporter.CreateRoof(file, elementGUID, ownerHistory, elementName, elementDescription, 
                                    elementObjectType, localPlacement, prodRepHnd, elementId, roofType);

                                IList<IFCAnyHandle> elementHandles = new List<IFCAnyHandle>();
                                elementHandles.Add(roofHandle);

                                //only thing supported right now.
                                XYZ extrusionDir = new XYZ(0, 0, 1);

                                ElementId catId = CategoryUtil.GetSafeCategoryId(element);

                                IList<CurveLoop> roofCurveloops = roofComponents.GetCurveLoops();
                                IList<XYZ> planeDirs = roofComponents.GetPlaneDirections();
                                IList<XYZ> planeOrigins = roofComponents.GetPlaneOrigins();
                                IList<Face> loopFaces = roofComponents.GetLoopFaces();
                                double scaledDepth = roofComponents.ScaledDepth;
                                IList<double> areas = roofComponents.GetAreasOfCurveLoops();

                                IList<IFCAnyHandle> slabHandles = new List<IFCAnyHandle>();
                                using (IFCExtrusionCreationData slabExtrusionCreationData = new IFCExtrusionCreationData())
                                {
                                    slabExtrusionCreationData.SetLocalPlacement(extrusionCreationData.GetLocalPlacement());
                                    slabExtrusionCreationData.ReuseLocalPlacement = false;
                                    slabExtrusionCreationData.ForceOffset = true;

                                    for (int numLoop = 0; numLoop < roofCurveloops.Count; numLoop++)
                                    {
                                        trfSetter.InitializeFromBoundingBox(exporterIFC, geometryList, slabExtrusionCreationData);
                                        Plane plane = new Plane(planeDirs[numLoop], planeOrigins[numLoop]);
                                        IList<CurveLoop> curveLoops = new List<CurveLoop>();
                                        curveLoops.Add(roofCurveloops[numLoop]);
                                        double slope = Math.Abs(planeDirs[numLoop].Z);
                                        double scaledExtrusionDepth = scaledDepth * slope;
                                        IFCAnyHandle shapeRep = ExtrusionExporter.CreateExtrudedSolidFromCurveLoop(exporterIFC, null, curveLoops, plane, extrusionDir, scaledExtrusionDepth);
                                        if (IFCAnyHandleUtil.IsNullOrHasNoValue(shapeRep))
                                            return null;

                                        ElementId matId = HostObjectExporter.GetFirstLayerMaterialId(element as HostObject);
                                        BodyExporter.CreateSurfaceStyleForRepItem(exporterIFC, element.Document, shapeRep, matId);

                                        HashSet<IFCAnyHandle> bodyItems = new HashSet<IFCAnyHandle>();
                                        bodyItems.Add(shapeRep);
                                        shapeRep = RepresentationUtil.CreateSweptSolidRep(exporterIFC, element, catId, exporterIFC.Get3DContextHandle("Body"), bodyItems, null);
                                        IList<IFCAnyHandle> shapeReps = new List<IFCAnyHandle>();
                                        shapeReps.Add(shapeRep);

                                        IFCAnyHandle repHnd = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, shapeReps);

                                        // Allow support for up to 256 named IfcSlab components, as defined in IFCSubElementEnums.cs.
                                        string slabGUID = (numLoop <256) ? GUIDUtil.CreateSubElementGUID(element, (int)IFCRoofSubElements.RoofSlabStart + numLoop) : GUIDUtil.CreateGUID();

                                        IFCAnyHandle slabPlacement = ExporterUtil.CreateLocalPlacement(file, slabExtrusionCreationData.GetLocalPlacement(), null);
                                        IFCAnyHandle slabHnd = IFCInstanceExporter.CreateSlab(file, slabGUID, ownerHistory, elementName,
                                           elementDescription, elementObjectType, slabPlacement, repHnd, elementId, "ROOF");

                                        //slab quantities
                                        slabExtrusionCreationData.ScaledLength = scaledExtrusionDepth;
                                        slabExtrusionCreationData.ScaledArea = UnitUtil.ScaleArea(areas[numLoop]);
                                        slabExtrusionCreationData.ScaledOuterPerimeter = UnitUtil.ScaleLength(curveLoops[0].GetExactLength());
                                        slabExtrusionCreationData.Slope = UnitUtil.ScaleAngle(Math.Acos(Math.Abs(planeDirs[numLoop].Z)));

                                        productWrapper.AddElement(null, slabHnd, setter, slabExtrusionCreationData, false);
                                        elementHandles.Add(slabHnd);
                                        slabHandles.Add(slabHnd);
                                    }
                                }

                                productWrapper.AddElement(element, roofHandle, setter, extrusionCreationData, true);

                                ExporterUtil.RelateObjects(exporterIFC, null, roofHandle, slabHandles);

                                OpeningUtil.AddOpeningsToElement(exporterIFC, elementHandles, roofCurveloops, element, null, roofComponents.ScaledDepth,
                                    null, setter, localPlacement, productWrapper);

                                transaction.Commit();
                                return roofHandle;
                            }
                        }
                    }
                    finally
                    {
                        exporterIFC.ClearFaceWithElementHandleMap();
                    }
                }
            }
        }

        /// <summary>
        /// Export the roof to IfcRoof containing its parts.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="element">The roof element.</param>
        /// <param name="geometryElement">The geometry element.</param>
        /// <param name="productWrapper">The ProductWrapper.</param>
        public static void ExportRoofAsParts(ExporterIFC exporterIFC, Element element, GeometryElement geometryElement, ProductWrapper productWrapper)
        {
            IFCFile file = exporterIFC.GetFile();

            using (IFCTransaction transaction = new IFCTransaction(file))
            {
                using (PlacementSetter setter = PlacementSetter.Create(exporterIFC, element))
                {
                    IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();
                    IFCAnyHandle localPlacement = setter.LocalPlacement;

                    IFCAnyHandle prodRepHnd = null;

                    string elementGUID = GUIDUtil.CreateGUID(element);
                    string elementName = NamingUtil.GetNameOverride(element, NamingUtil.GetIFCName(element));
                    string elementDescription = NamingUtil.GetDescriptionOverride(element, null);
                    string elementObjectType = NamingUtil.GetObjectTypeOverride(element, exporterIFC.GetFamilyName());
                    string elementTag = NamingUtil.GetTagOverride(element, NamingUtil.CreateIFCElementId(element));

                    //need to convert the string to enum
                    string ifcEnumType = ExporterUtil.GetIFCTypeFromExportTable(exporterIFC, element);
                    ifcEnumType = IFCValidateEntry.GetValidIFCType(element, ifcEnumType);

                    IFCAnyHandle roofHandle = IFCInstanceExporter.CreateRoof(file, elementGUID, ownerHistory, elementName, elementDescription, 
                        elementObjectType, localPlacement, prodRepHnd, elementTag, ifcEnumType);

                    // Export the parts
                    PartExporter.ExportHostPart(exporterIFC, element, roofHandle, productWrapper, setter, localPlacement, null);

                    productWrapper.AddElement(element, roofHandle, setter, null, true);

                    transaction.Commit();
                }
            }
        }

        /// <summary>
        /// Gets IFCRoofType from roof type name.
        /// </summary>
        /// <param name="roofTypeName">The roof type name.</param>
        /// <returns>The IFCRoofType.</returns>
        public static string GetIFCRoofType(string roofTypeName)
        {
            string typeName = NamingUtil.RemoveSpacesAndUnderscores(roofTypeName);

            if (String.Compare(typeName, "ROOFTYPEENUM", true) == 0 ||
                String.Compare(typeName, "ROOFTYPEENUMFREEFORM", true) == 0)
                return "FREEFORM";
            if (String.Compare(typeName, "FLAT", true) == 0 ||
                String.Compare(typeName, "FLATROOF", true) == 0)
                return "FLAT_ROOF";
            if (String.Compare(typeName, "SHED", true) == 0 ||
                String.Compare(typeName, "SHEDROOF", true) == 0)
                return "SHED_ROOF";
            if (String.Compare(typeName, "GABLE", true) == 0 ||
                String.Compare(typeName, "GABLEROOF", true) == 0)
                return "GABLE_ROOF";
            if (String.Compare(typeName, "HIP", true) == 0 ||
                String.Compare(typeName, "HIPROOF", true) == 0)
                return "HIP_ROOF";
            if (String.Compare(typeName, "HIPPED_GABLE", true) == 0 ||
                String.Compare(typeName, "HIPPED_GABLEROOF", true) == 0)
                return "HIPPED_GABLE_ROOF";
            if (String.Compare(typeName, "MANSARD", true) == 0 ||
                String.Compare(typeName, "MANSARDROOF", true) == 0)
                return "MANSARD_ROOF";
            if (String.Compare(typeName, "BARREL", true) == 0 ||
                String.Compare(typeName, "BARRELROOF", true) == 0)
                return "BARREL_ROOF";
            if (String.Compare(typeName, "BUTTERFLY", true) == 0 ||
                String.Compare(typeName, "BUTTERFLYROOF", true) == 0)
                return "BUTTERFLY_ROOF";
            if (String.Compare(typeName, "PAVILION", true) == 0 ||
                String.Compare(typeName, "PAVILIONROOF", true) == 0)
                return "PAVILION_ROOF";
            if (String.Compare(typeName, "DOME", true) == 0 ||
                String.Compare(typeName, "DOMEROOF", true) == 0)
                return "DOME_ROOF";

            return typeName;        //return unchanged. Validation for ENUM will be done later specific to schema version
        }
    }
}
