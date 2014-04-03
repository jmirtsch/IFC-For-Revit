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
    public class IFCFace : IFCRepresentationItem
    {
        ISet<IFCFaceBound> m_Bounds = null;

        /// <summary>
        /// Return the bounding loops of the face.
        /// </summary>
        public ISet<IFCFaceBound> Bounds
        {
            get
            {
                if (m_Bounds == null)
                    m_Bounds = new HashSet<IFCFaceBound>();
                return m_Bounds;
            }
        }

        protected IFCFace()
        {
        }

        override protected void Process(IFCAnyHandle ifcFace)
        {
            if (IFCAnyHandleUtil.IsSubTypeOf(ifcFace, IFCEntityType.IfcFaceSurface))
            {
                // Only allow IfcFaceSurface is the surface is a plane.
                IFCAnyHandle faceSurface = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcFace, "FaceSurface", false);
                if (!IFCAnyHandleUtil.IsNullOrHasNoValue(faceSurface))
                {
                    IFCSurface surface = IFCSurface.ProcessIFCSurface(faceSurface);
                    if (!(surface is IFCPlane))
                        IFCImportFile.TheLog.LogError(ifcFace.StepId,
                            "cannot handle IfcFaceSurface with FaceSurface of type " + IFCAnyHandleUtil.GetEntityType(faceSurface).ToString(), true);
                }
            }

            base.Process(ifcFace);

            HashSet<IFCAnyHandle> ifcBounds =
                IFCAnyHandleUtil.GetAggregateInstanceAttribute<HashSet<IFCAnyHandle>>(ifcFace, "Bounds");
            if (ifcBounds == null || ifcBounds.Count == 0)
                throw new InvalidOperationException("#" + ifcFace.StepId + ": no face boundaries, aborting.");

            foreach (IFCAnyHandle ifcBound in ifcBounds)
            {
                try
                {
                    Bounds.Add(IFCFaceBound.ProcessIFCFaceBound(ifcBound));
                }
                catch
                {
                    // LOG: WARNING: #: Invalid face boundary ifcBound.StepId, ignoring.
                }
            }

            if (Bounds.Count == 0)
                throw new InvalidOperationException("#" + ifcFace.StepId + ": no face boundaries, aborting.");

            // Give warning if too many outer bounds.  We won't care how they are designated, regardless.
            bool hasOuter = false;
            foreach (IFCFaceBound faceBound in Bounds)
            {
                if (faceBound.IsOuter)
                {
                    if (hasOuter)
                    {
                        // LOG: WARNING: #: Too many outer boundary loops for IfcFace.
                        break;
                    }
                    hasOuter = true;
                }
            }
        }

        /// <summary>
        /// Create geometry for a particular representation item.
        /// </summary>
        /// <param name="shapeEditScope">The geometry creation scope.</param>
        /// <param name="lcs">Local coordinate system for the geometry, without scale.</param>
        /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
        /// <param name="forceSolid">True if we require a solid.</param>
        /// <param name="guid">The guid of an element for which represntation is being created.</param>
        protected override void CreateShapeInternal(IFCImportShapeEditScope shapeEditScope, Transform lcs, Transform scaledLcs, bool forceSolid, string guid)
        {
            base.CreateShapeInternal(shapeEditScope, lcs, scaledLcs, forceSolid, guid);

            shapeEditScope.StartCollectingFace(GetMaterialElementId(shapeEditScope));
            foreach (IFCFaceBound faceBound in Bounds)
            {
                faceBound.CreateShape(shapeEditScope, lcs, scaledLcs, forceSolid, guid);
            }
            shapeEditScope.StopCollectingFace();
        }

        protected IFCFace(IFCAnyHandle ifcFace)
        {
            Process(ifcFace);
        }

        /// <summary>
        /// Create an IFCFace object from a handle of type IfcFace.
        /// </summary>
        /// <param name="ifcFace">The IFC handle.</param>
        /// <returns>The IFCFace object.</returns>
        public static IFCFace ProcessIFCFace(IFCAnyHandle ifcFace)
        {
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcFace))
            {
                IFCImportFile.TheLog.LogNullError(IFCEntityType.IfcFace); 
                return null;
            }

            IFCEntity face;
            if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcFace.StepId, out face))
                face = new IFCFace(ifcFace);
            return (face as IFCFace);
        }
    }
}
