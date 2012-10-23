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
        /// Exports a Rebar to IFC ReinforcingMesh.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="element">
        /// The element to be exported.
        /// </param>
        /// <param name="productWrapper">
        /// The IFCProductWrapper.
        /// </param>
        public static void ExportRebar(ExporterIFC exporterIFC,
           Rebar element, Autodesk.Revit.DB.View filterView, IFCProductWrapper productWrapper)
        {
            try
            {
                IFCFile file = exporterIFC.GetFile();

                using (IFCTransaction transaction = new IFCTransaction(file))
                {
                    using (IFCPlacementSetter setter = IFCPlacementSetter.Create(exporterIFC, element))
                    {
                        GeometryElement rebarGeometry = ExporterIFCUtils.GetRebarGeometry(element, filterView);

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

                        IFCAnyHandle prodRep = null;

                        double scale = exporterIFC.LinearScale;

                        double totalBarLength = element.TotalLength * scale;

                        if (MathUtil.IsAlmostZero(totalBarLength))
                            return;

                        ElementId typeId = element.GetTypeId();
                        RebarBarType elementType = element.Document.GetElement(element.GetTypeId()) as RebarBarType;
                        double diameter = (elementType == null ? 1.0 / 12.0 : elementType.BarDiameter) * scale;
                        double radius = diameter / 2.0;
                        double longitudinalBarNominalDiameter = diameter;
                        double longitudinalBarCrossSectionArea = (element.Volume * scale * scale * scale) / totalBarLength;
                        double barLength = totalBarLength / element.Quantity;

                        IList<Curve> baseCurves = element.GetCenterlineCurves(true, false, false);
                        int numberOfBarPositions = element.NumberOfBarPositions;
                        for (int i = 0; i < numberOfBarPositions; i++)
                        {
                            if (!element.DoesBarExistAtPosition(i))
                                continue;

                            Transform barTrf = element.GetBarPositionTransform(i);

                            IList<Curve> curves = new List<Curve>();
                            foreach (Curve baseCurve in baseCurves)
                            {
                                curves.Add(baseCurve.get_Transformed(barTrf));
                            }

                            IFCAnyHandle compositeCurve = GeometryUtil.CreateCompositeCurve(exporterIFC, curves);
                            IFCAnyHandle sweptDiskSolid = IFCInstanceExporter.CreateSweptDiskSolid(file, compositeCurve, radius, null, 0, 1);
                            HashSet<IFCAnyHandle> bodyItems = new HashSet<IFCAnyHandle>();
                            bodyItems.Add(sweptDiskSolid);
                            ElementId categoryId = CategoryUtil.GetSafeCategoryId(element);
                            IFCAnyHandle shapeRep = RepresentationUtil.CreateSweptSolidRep(exporterIFC, element, categoryId, exporterIFC.Get3DContextHandle("Body"), bodyItems, null);
                            IList<IFCAnyHandle> shapeReps = new List<IFCAnyHandle>();
                            shapeReps.Add(shapeRep);
                            prodRep = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, shapeReps);

                            string steelGradeOpt = null;
                            IFCAnyHandle elemHnd = null;

                            string rebarGUID = ExporterIFCUtils.CreateGUID(element);
                            string rebarName = NamingUtil.GetIFCName(element);
                            string rebarDescription = NamingUtil.GetDescriptionOverride(element, null);
                            string rebarObjectType = NamingUtil.GetObjectTypeOverride(element, NamingUtil.CreateIFCObjectName(exporterIFC, element));
                            string rebarElemId = NamingUtil.CreateIFCElementId(element);

                            IFCReinforcingBarRole role = IFCReinforcingBarRole.NotDefined;
                            elemHnd = IFCInstanceExporter.CreateReinforcingBar(file, rebarGUID, exporterIFC.GetOwnerHistoryHandle(),
                                rebarName, rebarDescription, rebarObjectType, setter.GetPlacement(),
                                prodRep, rebarElemId, steelGradeOpt, longitudinalBarNominalDiameter, longitudinalBarCrossSectionArea,
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

    }
}
