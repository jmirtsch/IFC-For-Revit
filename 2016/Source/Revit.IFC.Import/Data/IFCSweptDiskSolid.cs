//
// Revit IFC Import library: this library works with Autodesk(R) Revit(R) to import IFC files.
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
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
    public class IFCSweptDiskSolid : IFCSolidModel
    {
        IFCCurve m_Directrix = null;

        double m_Radius = 0.0;

        double? m_InnerRadius = null;

        double m_StartParam = 0.0;

        // although end param is not optional, we will still allow it to be null, to default to 
        // no trimming of the directrix.
        double? m_EndParam = null;

        /// <summary>
        /// The curve used for the sweep.
        /// </summary>
        public IFCCurve Directrix
        {
            get { return m_Directrix; }
            protected set { m_Directrix = value; }
        }

        /// <summary>
        /// The outer radius of the swept disk.
        /// </summary>
        public double Radius
        {
            get { return m_Radius; }
            protected set { m_Radius = value; }
        }

        /// <summary>
        /// The optional inner radius of the swept disk.
        /// </summary>
        public double? InnerRadius
        {
            get { return m_InnerRadius; }
            protected set { m_InnerRadius = value; }
        }

        /// <summary>
        /// The start parameter of the sweep, as measured along the length of the Directrix.
        /// </summary>
        /// <remarks>This is not optional in IFC, but we will default to 0.0 if not set.</remarks>
        public double StartParameter
        {
            get { return m_StartParam; }
            protected set { m_StartParam = value; }
        }

        /// <summary>
        /// The optional end parameter of the sweep, as measured along the length of the Directrix.
        /// </summary>
        /// <remarks>This is not optional in IFC, but we will default to ParametricLength(curve) if not set.</remarks>
        public double? EndParameter
        {
            get { return m_EndParam; }
            protected set { m_EndParam = value; }
        }
        
        protected IFCSweptDiskSolid()
        {
        }

        override protected void Process(IFCAnyHandle solid)
        {
            base.Process(solid);

            IFCAnyHandle directrix = IFCImportHandleUtil.GetRequiredInstanceAttribute(solid, "Directrix", true);
            Directrix = IFCCurve.ProcessIFCCurve(directrix);

            bool found = false;
            Radius = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(solid, "Radius", out found);
            if (!found || !Application.IsValidThickness(Radius))
                Importer.TheLog.LogError(solid.StepId, "IfcSweptDiskSolid radius is invalid, aborting.", true);

            double innerRadius = IFCImportHandleUtil.GetOptionalScaledLengthAttribute(solid, "InnerRadius", 0.0);
            if (Application.IsValidThickness(innerRadius))
            {
                if (!Application.IsValidThickness(Radius - innerRadius))
                   Importer.TheLog.LogError(solid.StepId, "IfcSweptDiskSolid inner radius is too large, aborting.", true);
                InnerRadius = innerRadius;
            }

            StartParameter = IFCImportHandleUtil.GetOptionalDoubleAttribute(solid, "StartParam", 0.0);
            if (StartParameter < MathUtil.Eps())
                StartParameter = 0.0;

            double endParameter = IFCImportHandleUtil.GetOptionalDoubleAttribute(solid, "EndParam", -1.0);
            if (!MathUtil.IsAlmostEqual(endParameter, -1.0))
            {
                if (endParameter < StartParameter + MathUtil.Eps())
                   Importer.TheLog.LogError(solid.StepId, "IfcSweptDiskSolid swept curve end parameter less than or equal to start parameter, aborting.", true);
                EndParameter = endParameter;
            }
        }
        
        /// <summary>
        /// Return geometry for a particular representation item.
        /// </summary>
        /// <param name="shapeEditScope">The geometry creation scope.</param>
        /// <param name="lcs">Local coordinate system for the geometry, without scale.</param>
        /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
        /// <param name="guid">The guid of an element for which represntation is being created.</param>
        /// <returns>Zero or more created geometries.</returns>
        protected override IList<GeometryObject> CreateGeometryInternal(
              IFCImportShapeEditScope shapeEditScope, Transform lcs, Transform scaledLcs, string guid)
        {
            Transform sweptDiskPosition = (lcs == null) ? Transform.Identity : lcs;

            CurveLoop baseProfileCurve = Directrix.GetCurveLoop();
            if (baseProfileCurve == null)
                return null;

            CurveLoop trimmedDirectrix = IFCGeometryUtil.TrimCurveLoop(baseProfileCurve, StartParameter, EndParameter);
            if (trimmedDirectrix == null)
                return null;

            CurveLoop trimmedDirectrixInLCS = IFCGeometryUtil.CreateTransformed(trimmedDirectrix, sweptDiskPosition);

            // Create the disk.
            Transform originTrf = null;
            double startParam = 0.0; // If the directrix isn't bound, this arbitrary parameter will do.
            foreach (Curve curve in trimmedDirectrixInLCS)
            {
                if (curve.IsBound)
                    startParam = curve.GetEndParameter(0);
                originTrf = curve.ComputeDerivatives(startParam, false);
                break;
            }

            if (originTrf == null)
                return null;

            // The X-dir of the transform of the start of the directrix will form the normal of the disk.
            Plane diskPlane = new Plane(originTrf.BasisX, originTrf.Origin);

            IList<CurveLoop> profileCurveLoops = new List<CurveLoop>();

            CurveLoop diskOuterCurveLoop = new CurveLoop();
            diskOuterCurveLoop.Append(Arc.Create(diskPlane, Radius, 0, Math.PI));
            diskOuterCurveLoop.Append(Arc.Create(diskPlane, Radius, Math.PI, 2.0 * Math.PI));
            profileCurveLoops.Add(diskOuterCurveLoop);
            
            if (InnerRadius.HasValue)
            {
                CurveLoop diskInnerCurveLoop = new CurveLoop();
                diskInnerCurveLoop.Append(Arc.Create(diskPlane, InnerRadius.Value, 0, Math.PI));
                diskInnerCurveLoop.Append(Arc.Create(diskPlane, InnerRadius.Value, Math.PI, 2.0 * Math.PI));
                profileCurveLoops.Add(diskInnerCurveLoop);
            }

            SolidOptions solidOptions = new SolidOptions(GetMaterialElementId(shapeEditScope), shapeEditScope.GraphicsStyleId);
            Solid sweptDiskSolid = GeometryCreationUtilities.CreateSweptGeometry(trimmedDirectrixInLCS, 0, startParam, profileCurveLoops,
                solidOptions);

            IList<GeometryObject> myObjs = new List<GeometryObject>();
            if (sweptDiskSolid != null)
                myObjs.Add(sweptDiskSolid);
            return myObjs;
        }

        /// <summary>
        /// Create geometry for a particular representation item.
        /// </summary>
        /// <param name="shapeEditScope">The geometry creation scope.</param>
        /// <param name="lcs">Local coordinate system for the geometry, without scale.</param>
        /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
        /// <param name="guid">The guid of an element for which represntation is being created.</param>
        protected override void CreateShapeInternal(IFCImportShapeEditScope shapeEditScope, Transform lcs, Transform scaledLcs, string guid)
        {
            base.CreateShapeInternal(shapeEditScope, lcs, scaledLcs, guid);

            IList<GeometryObject> sweptDiskGeometries = CreateGeometryInternal(shapeEditScope, lcs, scaledLcs, guid);
            if (sweptDiskGeometries != null)
            {
                foreach (GeometryObject sweptDiskGeometry in sweptDiskGeometries)
                {
                    shapeEditScope.AddGeometry(IFCSolidInfo.Create(Id, sweptDiskGeometry));
                }
            }
        }

        protected IFCSweptDiskSolid(IFCAnyHandle solid)
        {
            Process(solid);
        }

        /// <summary>
        /// Create an IFCSweptDiskSolid object from a handle of type IfcSweptDiskSolid.
        /// </summary>
        /// <param name="ifcSolid">The IFC handle.</param>
        /// <returns>The IFCSweptDiskSolid object.</returns>
        public static IFCSweptDiskSolid ProcessIFCSweptDiskSolid(IFCAnyHandle ifcSolid)
        {
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcSolid))
            {
                Importer.TheLog.LogNullError(IFCEntityType.IfcSweptDiskSolid);
                return null;
            }

            IFCEntity solid;
            if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcSolid.StepId, out solid))
                solid = new IFCSweptDiskSolid(ifcSolid);
            return (solid as IFCSweptDiskSolid); 
        }
    }
}
