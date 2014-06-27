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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
    public class IFCShellBasedSurfaceModel : IFCRepresentationItem
    {
        // Only IfcOpenShell and IfcClosedShell will be permitted.
        ISet<IFCConnectedFaceSet> m_Shells = null;

        /// <summary>
        /// The shells of the surface model.
        /// </summary>
        public ISet<IFCConnectedFaceSet> Shells
        {
            get 
            { 
                if (m_Shells == null)
                    m_Shells = new HashSet<IFCConnectedFaceSet>();
                return m_Shells; 
            }
        }

        protected IFCShellBasedSurfaceModel()
        {
        }

        /// <summary>
        /// Return geometry for a particular representation item.
        /// </summary>
        /// <param name="shapeEditScope">The shape edit scope.</param>
        /// <param name="lcs">Local coordinate system for the geometry, without scale.</param>
        /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
        /// <param name="forceSolid">True if we only allow solid output.</param>
        /// <param name="guid">The guid of an element for which represntation is being created.</param>
        /// <returns>The created geometry.</returns>
        /// <remarks>As this doesn't inherit from IfcSolidModel, this is a non-virtual CreateSolid function.</remarks>
        protected IList<GeometryObject> CreateGeometry(
              IFCImportShapeEditScope shapeEditScope, Transform lcs, Transform scaledLcs, bool forceSolid, string guid)
        {
            if (Shells.Count == 0)
                return null;

            shapeEditScope.StartCollectingFaceSet();

            foreach (IFCConnectedFaceSet faceSet in Shells)
                faceSet.CreateShape(shapeEditScope, lcs, scaledLcs, forceSolid, guid);

            IList<GeometryObject> geomObjs = null;

            if (forceSolid)
            {
                geomObjs = new List<GeometryObject>();
                GeometryObject geomObj = shapeEditScope.CreateClosedSolid(guid);
                if (geomObj != null)
                    geomObjs.Add(geomObj);
            }
            else
            {
                geomObjs = shapeEditScope.CreateSolidOrMesh(guid);
            }

            if (geomObjs == null || geomObjs.Count == 0)
               return null;

            return geomObjs;
        }
        
        override protected void Process(IFCAnyHandle ifcShellBasedSurfaceModel)
        {
            base.Process(ifcShellBasedSurfaceModel);

            ISet<IFCAnyHandle> ifcShells = IFCAnyHandleUtil.GetAggregateInstanceAttribute<HashSet<IFCAnyHandle>>(ifcShellBasedSurfaceModel, "SbsmBoundary");
            foreach (IFCAnyHandle ifcShell in ifcShells)
            {
                IFCConnectedFaceSet shell = IFCConnectedFaceSet.ProcessIFCConnectedFaceSet(ifcShell);
                if (shell != null)
                {
                    shell.AllowInvalidFace = true;
                    Shells.Add(shell);
                }
            }
        }

        /// <summary>
        /// Create geometry for a particular representation item.
        /// </summary>
        /// <param name="shapeEditScope">The geometry creation scope.</param>
        /// <param name="lcs">Local coordinate system for the geometry, without scale.</param>
        /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
        /// <param name="forceSolid">True if we force the output to be a Solid.</param>
        /// <param name="guid">The guid of an element for which represntation is being created.</param>
        protected override void CreateShapeInternal(IFCImportShapeEditScope shapeEditScope, Transform lcs, Transform scaledLcs, bool forceSolid, string guid)
        {
            base.CreateShapeInternal(shapeEditScope, lcs, scaledLcs, forceSolid, guid);

            // Ignoring Inner shells for now.
            if (Shells.Count != 0)
            {
               IList<GeometryObject> createdGeometries = CreateGeometry(shapeEditScope, lcs, scaledLcs, forceSolid, guid);
               if (createdGeometries != null)
               {
                   foreach (GeometryObject createdGeometry in createdGeometries)
                   {
                       shapeEditScope.AddGeometry(IFCSolidInfo.Create(Id, createdGeometry));
                   }
               }
            }
        }

        protected IFCShellBasedSurfaceModel(IFCAnyHandle item)
        {
            Process(item);
        }

        /// <summary>
        /// Create an IFCShellBasedSurfaceModel object from a handle of type IfcShellBasedSurfaceModel.
        /// </summary>
        /// <param name="ifcShellBasedSurfaceModel">The IFC handle.</param>
        /// <returns>The IFCShellBasedSurfaceModel object.</returns>
        public static IFCShellBasedSurfaceModel ProcessIFCShellBasedSurfaceModel(IFCAnyHandle ifcShellBasedSurfaceModel)
        {
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcShellBasedSurfaceModel))
            {
                IFCImportFile.TheLog.LogNullError(IFCEntityType.IfcShellBasedSurfaceModel);
                return null;
            }

            IFCEntity shellBasedSurfaceModel;
            if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcShellBasedSurfaceModel.StepId, out shellBasedSurfaceModel))
                shellBasedSurfaceModel = new IFCShellBasedSurfaceModel(ifcShellBasedSurfaceModel);
            return (shellBasedSurfaceModel as IFCShellBasedSurfaceModel);
        }
    }
}
