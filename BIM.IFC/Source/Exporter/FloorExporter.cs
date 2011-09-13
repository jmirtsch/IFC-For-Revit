//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2011  Autodesk, Inc.
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
using BIM.IFC.Utility;


namespace BIM.IFC.Exporter
{
    /// <summary>
    /// Provides methods to export floor elements.
    /// </summary>
    class FloorExporter
    {
        /// <summary>
        /// Exports a floor to IFC slab.
        /// </summary>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="floor">
        /// The floor element.
        /// </param>
        /// <param name="geometryElement">
        /// The geometry element.
        /// </param>
        /// <param name="ifcEnumType">
        /// The string value represents the IFC type.
        /// </param>
        /// <param name="productWrapper">
        /// The IFCProductWrapper.
        /// </param>
        /// <returns>
        /// True if the floor is exported successfully, false otherwise.
        /// </returns>
        public static void ExportFloor(ExporterIFC exporterIFC, Floor floor, GeometryElement geometryElement, IFCProductWrapper productWrapper)
        {
            if (geometryElement == null)
                return;

            IFCFile file = exporterIFC.GetFile();

            using (IFCTransaction tr = new IFCTransaction(file))
            {
                using (IFCTransformSetter transformSetter = IFCTransformSetter.Create())
                {
                    using (IFCPlacementSetter placementSetter = IFCPlacementSetter.Create(exporterIFC, floor))
                    {
                        IFCAnyHandle localPlacement = placementSetter.GetPlacement();
                        IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();

                        string ifcEnumType = CategoryUtil.GetIFCEnumTypeName(exporterIFC, floor);

                        double scale = exporterIFC.LinearScale;
                        ElementId catId = CategoryUtil.GetSafeCategoryId(floor);

                        IList<IFCAnyHandle> reps = new List<IFCAnyHandle>();
                        IList<IList<CurveLoop>> extrusionLoops = new List<IList<CurveLoop>>();
                        IList<IFCExtrusionCreationData> loopExtraParams = new List<IFCExtrusionCreationData>();
                        Plane floorPlane = GeometryUtil.CreateDefaultPlane();

                        IList<IFCAnyHandle> localPlacements = new List<IFCAnyHandle>();

                        bool exportedAsExtrusion = ExporterIFCUtils.ExportSlabAsExtrusion(exporterIFC, floor, geometryElement,
                              transformSetter, localPlacement, out localPlacements, out reps, out extrusionLoops, out loopExtraParams, floorPlane);
                        // We will use the ExtrusionAnalyzer when it is ready.
                        //            XYZ extrusionDirection = { 0, 0, -1 };
                        //            XYZ modelOrigin = { 0, 0, 0 };
                        //            XYZ floorOrigin = floor.GetVerticalProjectionPoint(modelOrigin, FloorFace.Top);
                        //            XYZ floorDir = floor.GetNormalAtVerticalProjectionPoint(floorOrigin, FloorFace.Top);
                        //            Plane floorPlane(floorDir, floorOrigin);          
                        //            ExtrusionAnalyzer floorExtrusion = 
                        //                ExtrusionAnalyzer.Create(geometryElement, floorPlane, extrusionDirection);

                        if (!exportedAsExtrusion)
                        {
                            IFCAnyHandle bodyRep = null;
                            IList<GeometryObject> geomObjects = new List<GeometryObject>();
                            geomObjects.Add(geometryElement);
                            bodyRep = BodyExporter.ExportBody(floor.Document.Application, exporterIFC, catId, geomObjects, true, null);
                            if (bodyRep == null || !bodyRep.HasValue)
                            {
                                tr.Commit();
                                return;
                            }

                            IFCAnyHandle prodDefHnd = file.CreateProductDefinitionShape(bodyRep);
                            if (!prodDefHnd.HasValue)
                                return;

                            reps.Add(prodDefHnd);
                        }

                        // Create the slab from either the extrusion or the BRep information.
                        IFCLabel ifcGUID = IFCLabel.CreateGUID(floor);

                        IList<IFCAnyHandle> slabHnds = new List<IFCAnyHandle>();
                        int numReps = reps.Count;
                        for (int ii = 0; ii < numReps; ii++)
                        {
                            IFCLabel ifcName = NamingUtil.GetNameOverride(floor, NamingUtil.CreateIFCName(exporterIFC, ii == 0 ? -1 : ii + 1));
                            IFCLabel ifcDescription = NamingUtil.GetDescriptionOverride(floor, IFCLabel.Create());
                            IFCLabel ifcObjectType = NamingUtil.GetObjectTypeOverride(floor, exporterIFC.GetFamilyNameFromExportState());
                            IFCLabel ifcElemId = NamingUtil.CreateIFCElementId(floor);

                            IFCLabel currentGUID = (ii == 0) ? ifcGUID : IFCLabel.CreateGUID();
                            IFCAnyHandle localPlacementHnd = exportedAsExtrusion ? localPlacements[ii] : localPlacement;

                            IFCAnyHandle slabHnd = file.CreateSlab(currentGUID,
                               ownerHistory,
                               ifcName,
                               ifcDescription,
                               ifcObjectType,
                               localPlacementHnd,
                               ifcElemId,
                               ifcEnumType,
                               reps[ii]);

                            if (!slabHnd.HasValue)
                                return;

                            slabHnds.Add(slabHnd);
                        }

                        for (int ii = 0; ii < numReps; ii++)
                        {
                            IFCExtrusionCreationData loopExtraParam = ii < loopExtraParams.Count ? loopExtraParams[ii] : null;
                            productWrapper.AddElement(slabHnds[ii], placementSetter, loopExtraParam, true);
                        }

                        if (exportedAsExtrusion)
                            ExporterIFCUtils.ExportExtrudedSlabOpenings(exporterIFC, floor, placementSetter,
                               localPlacements[0], slabHnds, extrusionLoops, floorPlane, productWrapper);

                        ExporterIFCUtils.CreateGenericElementPropertySet(exporterIFC, floor, productWrapper);

                        ExporterIFCUtils.ExportHostObject(exporterIFC, floor, geometryElement, productWrapper);
                    }
                }
                tr.Commit();
            }
        }
    }
}
