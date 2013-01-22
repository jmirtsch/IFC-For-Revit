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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB.Structure;
using BIM.IFC.Utility;
using BIM.IFC.Toolkit;
using BIM.IFC.Exporter.PropertySet;

namespace BIM.IFC.Exporter
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
        /// <param name="filterView">The view.</param>
        /// <param name="productWrapper">The product wrapper.</param>
        public static void Export(ExporterIFC exporterIFC,
           Element element, Autodesk.Revit.DB.View filterView, ProductWrapper productWrapper)
        {
            if (element is Rebar)
            {
                ExportRebar(exporterIFC, element, filterView, productWrapper);
            }
            else if (element is AreaReinforcement)
            {
                AreaReinforcement areaReinforcement = element as AreaReinforcement;
                IList<ElementId> rebarIds = areaReinforcement.GetRebarInSystemIds();

                Document doc = areaReinforcement.Document;
                foreach (ElementId id in rebarIds)
                {
                    Element rebarInSystem = doc.GetElement(id);
                    ExportRebar(exporterIFC, rebarInSystem, filterView, productWrapper);
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
                    ExportRebar(exporterIFC, rebarInSystem, filterView, productWrapper);
                }
            }
        }

        /// <summary>
        /// Exports a Rebar to IFC ReinforcingMesh.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="element">
        /// The element to be exported.
        /// </param>
        /// <param name="productWrapper">
        /// The ProductWrapper.
        /// </param>
        public static void ExportRebar(ExporterIFC exporterIFC,
           Element element, Autodesk.Revit.DB.View filterView, ProductWrapper productWrapper)
        {
            try
            {
                IFCFile file = exporterIFC.GetFile();

                using (IFCTransaction transaction = new IFCTransaction(file))
                {
                    using (IFCPlacementSetter setter = IFCPlacementSetter.Create(exporterIFC, element))
                    {
                        if (element is Rebar)
                        {
                            GeometryElement rebarGeometry = ExporterIFCUtils.GetRebarGeometry(element as Rebar, filterView);

                            // only options are: Not Export, BuildingElementProxy, or ReinforcingBar/Mesh, depending on layout.
                            // Not Export is handled previously, and ReinforcingBar vs Mesh will be determined below.
                            string ifcEnumType;
                            IFCExportType exportType = ExporterUtil.GetExportType(exporterIFC, element, out ifcEnumType);

                            if (exportType == IFCExportType.ExportBuildingElementProxy)
                            {
                                if (rebarGeometry != null)
                                {
                                    ProxyElementExporter.ExportBuildingElementProxy(exporterIFC, element, rebarGeometry, productWrapper);
                                    transaction.Commit();
                                }
                                return;
                            }
                        }

                        IFCAnyHandle prodRep = null;

                        double scale = exporterIFC.LinearScale;

                        double totalBarLengthUnscale = GetRebarTotalLength(element);
                        double volumeUnscale = GetRebarVolume(element);
                        double totalBarLength = totalBarLengthUnscale * scale;

                        if (MathUtil.IsAlmostZero(totalBarLength))
                            return;

                        ElementId typeId = element.GetTypeId();
                        RebarBarType elementType = element.Document.GetElement(element.GetTypeId()) as RebarBarType;
                        double diameter = (elementType == null ? 1.0 / 12.0 : elementType.BarDiameter) * scale;
                        double radius = diameter / 2.0;
                        double longitudinalBarNominalDiameter = diameter;
                        double longitudinalBarCrossSectionArea = (volumeUnscale / totalBarLengthUnscale) * scale * scale;
                        double barLength = totalBarLength / GetRebarQuantity(element);

                        IList<Curve> baseCurves = GetRebarCenterlineCurves(element, true, false, false);
                        int numberOfBarPositions = GetNumberOfBarPositions(element);
                        for (int i = 0; i < numberOfBarPositions; i++)
                        {
                            if (!DoesBarExistAtPosition(element, i))
                                continue;

                            string rebarGUID = ExporterIFCUtils.CreateGUID(element);
                            string rebarName = NamingUtil.GetNameOverride(element, NamingUtil.GetIFCName(element));
                            string rebarDescription = NamingUtil.GetDescriptionOverride(element, null);
                            string rebarObjectType = NamingUtil.GetObjectTypeOverride(element, NamingUtil.CreateIFCObjectName(exporterIFC, element));
                            string rebarTag = NamingUtil.GetTagOverride(element, NamingUtil.CreateIFCElementId(element));

                            Transform barTrf = GetBarPositionTransform(element, i);

                            IList<Curve> curves = new List<Curve>();
                            double endParam = 0.0;
                            foreach (Curve baseCurve in baseCurves)
                            {
                                if (baseCurve is Arc || baseCurve is Ellipse)
                                {
                                    if (baseCurve.IsBound)
                                        endParam += (baseCurve.get_EndParameter(1) - baseCurve.get_EndParameter(0)) * 180 / Math.PI;
                                    else
                                        endParam += (2 * Math.PI) * 180 / Math.PI;
                                }
                                else
                                    endParam += 1.0;
                                curves.Add(baseCurve.get_Transformed(barTrf));
                            }

                            IFCAnyHandle compositeCurve = GeometryUtil.CreateCompositeCurve(exporterIFC, curves);
                            IFCAnyHandle sweptDiskSolid = IFCInstanceExporter.CreateSweptDiskSolid(file, compositeCurve, radius, null, 0, endParam);
                            HashSet<IFCAnyHandle> bodyItems = new HashSet<IFCAnyHandle>();
                            bodyItems.Add(sweptDiskSolid);
                            ElementId categoryId = CategoryUtil.GetSafeCategoryId(element);
                            IFCAnyHandle shapeRep = RepresentationUtil.CreateSweptSolidRep(exporterIFC, element, categoryId, exporterIFC.Get3DContextHandle("Body"), bodyItems, null);
                            IList<IFCAnyHandle> shapeReps = new List<IFCAnyHandle>();
                            shapeReps.Add(shapeRep);
                            prodRep = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, shapeReps);

                            string steelGradeOpt = null;
                            IFCAnyHandle elemHnd = null;

                            IFCReinforcingBarRole role = IFCReinforcingBarRole.NotDefined;
                            elemHnd = IFCInstanceExporter.CreateReinforcingBar(file, rebarGUID, exporterIFC.GetOwnerHistoryHandle(),
                                rebarName, rebarDescription, rebarObjectType, setter.GetPlacement(),
                                prodRep, rebarTag, steelGradeOpt, longitudinalBarNominalDiameter, longitudinalBarCrossSectionArea,
                                barLength, role, null);

                            productWrapper.AddElement(elemHnd, setter.GetLevelInfo(), null, true);

                            PropertyUtil.CreateInternalRevitPropertySets(exporterIFC, element, productWrapper);
                        }
                    }
                    transaction.Commit();
                }

            }
            catch (Exception)
            {
                // It will throw exception at GetBarPositionTransform when exporting rebars with Revit 2013 UR1 and before versions, so we skip the export.
                // It should not come here and will export the rebars properly at Revit later versions.
            }
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
