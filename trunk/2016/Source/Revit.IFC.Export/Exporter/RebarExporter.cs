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
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB.Structure;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Exporter
{
    /// <summary>
    /// Provides methods to export beams.
    /// </summary>
    class RebarExporter
    {
        /// <summary>
        /// Exports a Rebar, AreaReinforcement or PathReinforcement to IFC ReinforcingBar.
        /// </summary>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="element">The element.</param>
        /// <param name="productWrapper">The product wrapper.</param>
        public static void Export(ExporterIFC exporterIFC, Element element, ProductWrapper productWrapper)
        {
            ISet<IFCAnyHandle> createdRebars = null;

            if (element is Rebar)
            {
                ExportRebar(exporterIFC, element, productWrapper);
            }
            else if (element is AreaReinforcement)
            {
                AreaReinforcement areaReinforcement = element as AreaReinforcement;
                IList<ElementId> rebarIds = areaReinforcement.GetRebarInSystemIds();

                Document doc = areaReinforcement.Document;
                foreach (ElementId id in rebarIds)
                {
                    Element rebarInSystem = doc.GetElement(id);
                    createdRebars = ExportRebar(exporterIFC, rebarInSystem, productWrapper);
                }
            }
            else if (element is PathReinforcement)
            {
                PathReinforcement pathReinforcement = element as PathReinforcement;
                IList<ElementId> rebarIds = pathReinforcement.GetRebarInSystemIds();

                Document doc = pathReinforcement.Document;
                foreach (ElementId id in rebarIds)
                {
                    Element rebarInSystem = doc.GetElement(id);
                    createdRebars = ExportRebar(exporterIFC, rebarInSystem, productWrapper);
                }
            }

            if (createdRebars != null && createdRebars.Count > 1)
            {
                IFCFile file = exporterIFC.GetFile();
                using (IFCTransaction tr = new IFCTransaction(file))
                {
                    string guid = GUIDUtil.CreateGUID(element);
                    IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
                    string revitObjectType = exporterIFC.GetFamilyName();
                    string name = NamingUtil.GetNameOverride(element, revitObjectType);
                    string description = NamingUtil.GetDescriptionOverride(element, null);
                    string objectType = NamingUtil.GetObjectTypeOverride(element, revitObjectType);

                    IFCAnyHandle rebarGroup = IFCInstanceExporter.CreateGroup(file, guid,
                        ownerHistory, name, description, objectType);

                    productWrapper.AddElement(element, rebarGroup);

                    IFCInstanceExporter.CreateRelAssignsToGroup(file, GUIDUtil.CreateGUID(), ownerHistory,
                        null, null, createdRebars, null, rebarGroup);

                    tr.Commit();
                }
            }
        }

        private static IFCReinforcingBarRole GetReinforcingBarRole(string role)
        {
            if (String.IsNullOrWhiteSpace(role))
                return IFCReinforcingBarRole.NotDefined;

            if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(role, "Main"))
                return IFCReinforcingBarRole.Main;
            if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(role, "Shear"))
                return IFCReinforcingBarRole.Shear;
            if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(role, "Ligature"))
                return IFCReinforcingBarRole.Ligature;
            if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(role, "Stud"))
                return IFCReinforcingBarRole.Stud;
            if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(role, "Punching"))
                return IFCReinforcingBarRole.Punching;
            if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(role, "Edge"))
                return IFCReinforcingBarRole.Edge;
            if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(role, "Ring"))
                return IFCReinforcingBarRole.Ring;
            return IFCReinforcingBarRole.UserDefined;
        }

        /// <summary>
        /// Exports a Rebar to IFC ReinforcingBar.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="element">The element to be exported.</param>
        /// <param name="productWrapper">The ProductWrapper.</param>
        /// <returns>The list of IfcReinforcingBar handles created.</returns>
        public static ISet<IFCAnyHandle> ExportRebar(ExporterIFC exporterIFC, Element element, ProductWrapper productWrapper)
        {
            IFCFile file = exporterIFC.GetFile();
            HashSet<IFCAnyHandle> createdRebars = new HashSet<IFCAnyHandle>();
            
            using (IFCTransaction transaction = new IFCTransaction(file))
            {
                using (PlacementSetter setter = PlacementSetter.Create(exporterIFC, element))
                {
                    if (element is Rebar && ExporterCacheManager.ExportOptionsCache.FilterViewForExport != null)
                    {
                        // only options are: Not Export, BuildingElementProxy, or ReinforcingBar/Mesh, depending on layout.
                        // Not Export is handled previously, and ReinforcingBar vs Mesh will be determined below.
                        string ifcEnumType;
                        IFCExportType exportType = ExporterUtil.GetExportType(exporterIFC, element, out ifcEnumType);
                        
                        if (exportType == IFCExportType.IfcBuildingElementProxy ||
                            exportType == IFCExportType.IfcBuildingElementProxyType)
                        {
                            GeometryElement rebarGeometry = ExporterIFCUtils.GetRebarGeometry(element as Rebar, ExporterCacheManager.ExportOptionsCache.FilterViewForExport);

                            if (rebarGeometry != null)
                            {
                                ProxyElementExporter.ExportBuildingElementProxy(exporterIFC, element, rebarGeometry, productWrapper);
                                transaction.Commit();
                            }
                            return null;
                        }
                    }
                    
                    IFCAnyHandle prodRep = null;

                    double totalBarLengthUnscale = GetRebarTotalLength(element);
                    double volumeUnscale = GetRebarVolume(element);
                    double totalBarLength = UnitUtil.ScaleLength(totalBarLengthUnscale);

                    if (MathUtil.IsAlmostZero(totalBarLength))
                        return null;

                    ElementId materialId = ElementId.InvalidElementId;
                    ParameterUtil.GetElementIdValueFromElementOrSymbol(element, BuiltInParameter.MATERIAL_ID_PARAM, out materialId);

                    Document doc = element.Document;
                    ElementId typeId = element.GetTypeId();
                    RebarBarType elementType = doc.GetElement(element.GetTypeId()) as RebarBarType;
                    double diameter = UnitUtil.ScaleLength(elementType == null ? 1.0 / 12.0 : elementType.BarDiameter);
                    double radius = diameter / 2.0;
                    double longitudinalBarNominalDiameter = diameter;
                    double longitudinalBarCrossSectionArea = UnitUtil.ScaleArea(volumeUnscale / totalBarLengthUnscale);
                    double barLength = totalBarLength / GetRebarQuantity(element);

                    IList<Curve> baseCurves = GetRebarCenterlineCurves(element, true, false, false);
                    int numberOfBarPositions = GetNumberOfBarPositions(element);

                    string steelGrade = NamingUtil.GetOverrideStringValue(element, "SteelGrade", null);
                    
                    // Allow use of IFC2x3 or IFC4 naming.
                    string predefinedType = NamingUtil.GetOverrideStringValue(element, "BarRole", null);
                    if (string.IsNullOrWhiteSpace(predefinedType))
                        predefinedType = NamingUtil.GetOverrideStringValue(element, "PredefinedType", null);
                    IFCReinforcingBarRole role = GetReinforcingBarRole(predefinedType);

                    string origRebarName = NamingUtil.GetNameOverride(element, NamingUtil.GetIFCName(element));
                    string rebarDescription = NamingUtil.GetDescriptionOverride(element, null);
                    string rebarObjectType = NamingUtil.GetObjectTypeOverride(element, NamingUtil.CreateIFCObjectName(exporterIFC, element));
                    string rebarTag = NamingUtil.GetTagOverride(element, NamingUtil.CreateIFCElementId(element));
                    
                    const int maxBarGUIDS = IFCReinforcingBarSubElements.BarEnd - IFCReinforcingBarSubElements.BarStart + 1;
                    ElementId categoryId = CategoryUtil.GetSafeCategoryId(element);

                    IFCAnyHandle originalPlacement = setter.LocalPlacement;

                    for (int i = 0; i < numberOfBarPositions; i++)
                    {
                        if (!DoesBarExistAtPosition(element, i))
                            continue;

                        string rebarName = NamingUtil.GetNameOverride(element, origRebarName + ": " + i);

                        Transform barTrf = GetBarPositionTransform(element, i);

                        IList<Curve> curves = new List<Curve>();
                        double endParam = 0.0;
                        foreach (Curve baseCurve in baseCurves)
                        {
                            if (baseCurve is Arc || baseCurve is Ellipse)
                            {
                                if (baseCurve.IsBound)
                                    endParam += UnitUtil.ScaleAngle(baseCurve.GetEndParameter(1) - baseCurve.GetEndParameter(0));
                                else
                                    endParam += UnitUtil.ScaleAngle(2 * Math.PI);
                            }
                            else
                                endParam += 1.0;
                            curves.Add(baseCurve.CreateTransformed(barTrf));
                        }

                        IFCAnyHandle compositeCurve = GeometryUtil.CreateCompositeCurve(exporterIFC, curves);
                        IFCAnyHandle sweptDiskSolid = IFCInstanceExporter.CreateSweptDiskSolid(file, compositeCurve, radius, null, 0, endParam);
                        HashSet<IFCAnyHandle> bodyItems = new HashSet<IFCAnyHandle>();
                        bodyItems.Add(sweptDiskSolid);

                        IFCAnyHandle shapeRep = RepresentationUtil.CreateAdvancedSweptSolidRep(exporterIFC, element, categoryId, exporterIFC.Get3DContextHandle("Body"), bodyItems, null);
                        IList<IFCAnyHandle> shapeReps = new List<IFCAnyHandle>();
                        shapeReps.Add(shapeRep);
                        prodRep = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, shapeReps);

                        IFCAnyHandle copyLevelPlacement = (i == 0) ? originalPlacement : ExporterUtil.CopyLocalPlacement(file, originalPlacement);

                        string rebarGUID = (i < maxBarGUIDS) ?
                            GUIDUtil.CreateSubElementGUID(element, i + (int)IFCReinforcingBarSubElements.BarStart) :
                            GUIDUtil.CreateGUID();
                        IFCAnyHandle elemHnd = IFCInstanceExporter.CreateReinforcingBar(file, rebarGUID, ExporterCacheManager.OwnerHistoryHandle,
                            rebarName, rebarDescription, rebarObjectType, copyLevelPlacement,
                            prodRep, rebarTag, steelGrade, longitudinalBarNominalDiameter, longitudinalBarCrossSectionArea,
                            barLength, role, null);
                        createdRebars.Add(elemHnd);

                        productWrapper.AddElement(element, elemHnd, setter.LevelInfo, null, true);
                        ExporterCacheManager.HandleToElementCache.Register(elemHnd, element.Id);

                        CategoryUtil.CreateMaterialAssociation(exporterIFC, elemHnd, materialId);
                    }
                }
                transaction.Commit();
            }
            return createdRebars;
        }

        /// <summary>
        /// Gets total length of a rebar.
        /// </summary>
        /// <param name="element">The rebar element.</param>
        /// <returns>The length.</returns>
        static double GetRebarTotalLength(Element element)
        {
            if (element is Rebar)
            {
                return (element as Rebar).TotalLength;
            }
            else if (element is RebarInSystem)
            {
                return (element as RebarInSystem).TotalLength;
            }
            else
                throw new ArgumentException("Not a rebar.");
        }


        /// <summary>
        /// Gets volume of a rebar.
        /// </summary>
        /// <param name="element">The rebar element.</param>
        /// <returns>The volume.</returns>
        static double GetRebarVolume(Element element)
        {
            if (element is Rebar)
            {
                return (element as Rebar).Volume;
            }
            else if (element is RebarInSystem)
            {
                return (element as RebarInSystem).Volume;
            }
            else
                throw new ArgumentException("Not a rebar.");
        }

        /// <summary>
        /// Gets quantity of a rebar.
        /// </summary>
        /// <param name="element">The rebar element.</param>
        /// <returns>The number.</returns>
        static int GetRebarQuantity(Element element)
        {
            if (element is Rebar)
            {
                return (element as Rebar).Quantity;
            }
            else if (element is RebarInSystem)
            {
                return (element as RebarInSystem).Quantity;
            }
            else
                throw new ArgumentException("Not a rebar.");
        }

        /// <summary>
        /// Gets center line curves of a rebar.
        /// </summary>
        /// <param name="element">The rebar element.</param>
        /// <param name="adjustForSelfIntersection">Identifies if curves should be adjusted to avoid intersection.</param>
        /// <param name="suppressHooks">Identifies if the chain will include hooks curves.</param>
        /// <param name="suppressBendRadius">Identifies if the connected chain will include unfilled curves.</param>
        /// <returns></returns>
        static IList<Curve> GetRebarCenterlineCurves(Element element, bool adjustForSelfIntersection, bool suppressHooks, bool suppressBendRadius)
        {
            if (element is Rebar)
            {
                return (element as Rebar).GetCenterlineCurves(adjustForSelfIntersection, suppressHooks, suppressBendRadius);
            }
            else if (element is RebarInSystem)
            {
                return (element as RebarInSystem).GetCenterlineCurves(adjustForSelfIntersection, suppressHooks, suppressBendRadius);
            }
            else
                throw new ArgumentException("Not a rebar.");
        }

        /// <summary>
        /// Gets number of rebar positions.
        /// </summary>
        /// <param name="element">The rebar element.</param>
        /// <returns>The number.</returns>
        static int GetNumberOfBarPositions(Element element)
        {
            if (element is Rebar)
            {
                return (element as Rebar).NumberOfBarPositions;
            }
            else if (element is RebarInSystem)
            {
                return (element as RebarInSystem).NumberOfBarPositions;
            }
            else
                throw new ArgumentException("Not a rebar.");
        }

        /// <summary>
        /// Identifies if rebar exists at a certain position.
        /// </summary>
        /// <param name="element">The rebar element.</param>
        /// <param name="barPosition">The bar position.</param>
        /// <returns>True if it exists.</returns>
        static bool DoesBarExistAtPosition(Element element, int barPosition)
        {
            if (element is Rebar)
            {
                return (element as Rebar).DoesBarExistAtPosition(barPosition);
            }
            else if (element is RebarInSystem)
            {
                return (element as RebarInSystem).DoesBarExistAtPosition(barPosition);
            }
            else
                throw new ArgumentException("Not a rebar.");
        }

        /// <summary>
        /// Gets bar position transform.
        /// </summary>
        /// <param name="element">The rebar element.</param>
        /// <param name="barPositionIndex">The bar position.</param>
        /// <returns>The transform.</returns>
        static Transform GetBarPositionTransform(Element element, int barPositionIndex)
        {
            if (element is Rebar)
            {
                return (element as Rebar).GetBarPositionTransform(barPositionIndex);
            }
            else if (element is RebarInSystem)
            {
                return (element as RebarInSystem).GetBarPositionTransform(barPositionIndex);
            }
            else
                throw new ArgumentException("Not a rebar.");
        }
    }
}
