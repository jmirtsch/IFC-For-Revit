//
// Revit IFC Import library: this library works with Autodesk(R) Revit(R) to import IFC files.
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
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
    public class IFCExtrudedAreaSolid : IFCSweptAreaSolid
    {
        XYZ m_Direction = null;

        double m_Depth = 0.0;

        /// <summary>
        /// The direction of the extrusion in the local coordinate system.
        /// </summary>
        public XYZ Direction
        {
            get { return m_Direction; }
            protected set { m_Direction = value; }
        }

        /// <summary>
        /// The depth of the extrusion, along the extrusion direction.
        /// </summary>
        public double Depth
        {
            get { return m_Depth; }
            protected set { m_Depth = value; }
        }

        protected IFCExtrudedAreaSolid()
        {
        }

        override protected void Process(IFCAnyHandle solid)
        {
            base.Process(solid);

            // We will not fail if the direction is not given, but instead assume it to be normal to the swept area.
            IFCAnyHandle direction = IFCImportHandleUtil.GetRequiredInstanceAttribute(solid, "ExtrudedDirection", false);
            if (direction != null)
                Direction = IFCPoint.ProcessNormalizedIFCDirection(direction);
            else
                Direction = XYZ.BasisZ;

            bool found = false;
            Depth = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(solid, "Depth", out found);
            if (found && Depth < 0.0)
            {
                // Reverse depth and orientation.
                if (Application.IsValidThickness(-Depth))
                {
                    Depth = -Depth;
                    Direction = -Direction;
                    IFCImportFile.TheLog.LogWarning(solid.StepId, "negative extrusion depth is invalid, reversing direction.", false);
                }
            }

            if (!found || !Application.IsValidThickness(Depth))
            {
                string depthAsString = UnitFormatUtils.Format(IFCImportFile.TheFile.Document.GetUnits(), UnitType.UT_Length, Depth,
                    true, false);
                IFCImportFile.TheLog.LogError(solid.StepId, "extrusion depth of " + depthAsString + " is invalid, aborting.", true);
            }
        }
        
        /// <summary>
        /// Return geometry for a particular representation item.
        /// </summary>
        /// <param name="shapeEditScope">The shape edit scope.</param>
        /// <param name="lcs">Local coordinate system for the geometry.</param>
        /// <param name="forceSolid">True if we require a Solid.</param>
        /// <param name="guid">The guid of an element for which represntation is being created.</param>
        /// <returns>The created geometry.</returns>
        /// <remarks>The scaledLcs is only partially supported in this routine; it allows scaling the depth of the extrusion,
        /// which is commonly found in ACA files.</remarks>
        protected override GeometryObject CreateGeometryInternal(
              IFCImportShapeEditScope shapeEditScope, Transform lcs, Transform scaledLcs, bool forceSolid, string guid)
        {
            Transform origLCS = (lcs == null) ? Transform.Identity : lcs;
            Transform origScaledLCS = (scaledLcs == null) ? Transform.Identity : scaledLcs;

            Transform extrusionPosition = (Position == null) ? origLCS : origLCS.Multiply(Position);
            Transform scaledExtrusionPosition = (Position == null) ? origScaledLCS : origScaledLCS.Multiply(Position);

            XYZ extrusionDirection = extrusionPosition.OfVector(Direction);

            IList<CurveLoop> loops = GetTransformedCurveLoops(extrusionPosition);

            GeometryObject myObj = null;
            if (loops != null && loops.Count() != 0)
            {
                SolidOptions solidOptions = new SolidOptions(GetMaterialElementId(shapeEditScope), shapeEditScope.GraphicsStyleId);
                XYZ scaledDirection = scaledExtrusionPosition.OfVector(Direction);
                double currDepth = Depth * scaledDirection.GetLength();

                try
                {
                    myObj = GeometryCreationUtilities.CreateExtrusionGeometry(loops, extrusionDirection, currDepth, solidOptions);
                }
                catch (Exception ex)
                {
                   if (forceSolid)
                      throw ex;

                   MeshFromGeometryOperationResult meshResult = TessellatedShapeBuilder.CreateMeshByExtrusion(
                      loops, extrusionDirection, currDepth, GetMaterialElementId(shapeEditScope));

                      // will throw if mesh is not available
                   myObj = meshResult.GetMesh();
                }
            }

            return myObj;
        }

        /// <summary>
        /// Create geometry for a particular representation item.
        /// </summary>
        /// <param name="shapeEditScope">The geometry creation scope.</param>
        /// <param name="lcs">Local coordinate system for the geometry, without scale.</param>
        /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
        /// <param name="forceSolid">True if we require a Solid.</param>
        /// <param name="guid">The guid of an element for which represntation is being created.</param>
        protected override void CreateShapeInternal(IFCImportShapeEditScope shapeEditScope, Transform lcs, Transform scaledLcs, bool forceSolid, string guid)
        {
            base.CreateShapeInternal(shapeEditScope, lcs, scaledLcs, forceSolid, guid);

            GeometryObject extrudedGeometry = CreateGeometryInternal(shapeEditScope, lcs, scaledLcs, forceSolid, guid);
            if (extrudedGeometry != null)
                shapeEditScope.AddGeometry(IFCSolidInfo.Create(Id, extrudedGeometry));
        }

        protected IFCExtrudedAreaSolid(IFCAnyHandle solid)
        {
            Process(solid);
        }

        /// <summary>
        /// Create an IFCExtrudedAreaSolid object from a handle of type IfcExtrudedAreaSolid.
        /// </summary>
        /// <param name="ifcSolid">The IFC handle.</param>
        /// <returns>The IFCExtrudedAreaSolid object.</returns>
        public static IFCExtrudedAreaSolid ProcessIFCExtrudedAreaSolid(IFCAnyHandle ifcSolid)
        {
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcSolid))
            {
                IFCImportFile.TheLog.LogNullError(IFCEntityType.IfcExtrudedAreaSolid);
                return null;
            }

            IFCEntity solid;
            if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcSolid.StepId, out solid))
                solid = new IFCExtrudedAreaSolid(ifcSolid);
            return (solid as IFCExtrudedAreaSolid); 
        }
    }
}
