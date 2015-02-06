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
    public class IFCManifoldSolidBrep : IFCSolidModel, IIFCBooleanOperand
    {
        IFCClosedShell m_Outer = null;

        ISet<IFCClosedShell> m_Inners = null;

        /// <summary>
        /// The outer shell of the solid.
        /// </summary>
        public IFCClosedShell Outer
        {
            get { return m_Outer; }
            protected set { m_Outer = value; }
        }

        /// <summary>
        /// The list of optional voids of the solid.
        /// </summary>
        public ISet<IFCClosedShell> Inners
        {
            get 
            { 
                if (m_Inners == null)
                    m_Inners = new HashSet<IFCClosedShell>();
                return m_Inners; 
            }
        }
        
        protected IFCManifoldSolidBrep()
        {
        }

        /// <summary>
        /// Return geometry for a particular representation item.
        /// </summary>
        /// <param name="shapeEditScope">The shape edit scope.</param>
        /// <param name="lcs">Local coordinate system for the geometry, without scale.</param>
        /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
        /// <param name="guid">The guid of an element for which represntation is being created.</param>
        /// <returns>The created geometry.</returns>
        protected override IList<GeometryObject> CreateGeometryInternal(
           IFCImportShapeEditScope shapeEditScope, Transform lcs, Transform scaledLcs, string guid)
        {
            if (Outer == null || Outer.Faces.Count == 0)
                return null;

            IList<GeometryObject> geomObjs = null;
            
            shapeEditScope.StartCollectingFaceSet();
            Outer.CreateShape(shapeEditScope, lcs, scaledLcs, guid);
            
            bool closedFaceSet = false;
            if (shapeEditScope.CreatedFacesCount == Outer.Faces.Count)
            {
                geomObjs = shapeEditScope.CreateGeometry(guid);
                closedFaceSet = true;
            }

            if (geomObjs == null || geomObjs.Count == 0)
            {
                // Let's see if we can loosen the requirements a bit, and try again.
                if (shapeEditScope.TargetGeometry == TessellatedShapeBuilderTarget.AnyGeometry &&
                    shapeEditScope.FallbackGeometry == TessellatedShapeBuilderFallback.Mesh)
                {
                    using (IFCImportShapeEditScope.IFCTargetSetter targetSetter =
                        new IFCImportShapeEditScope.IFCTargetSetter(shapeEditScope, TessellatedShapeBuilderTarget.Mesh, TessellatedShapeBuilderFallback.Salvage))
                    {
                        if (closedFaceSet)
                            shapeEditScope.StartCollectingFaceSet();
                        else
                            shapeEditScope.ResetCreatedFacesCount();
                        
                        Outer.CreateShape(shapeEditScope, lcs, scaledLcs, guid);

                        // This needs to be in scope so that we keep the mesh tolerance for vertices.
                        if (shapeEditScope.CreatedFacesCount != 0)
                        {
                            if (shapeEditScope.CreatedFacesCount != Outer.Faces.Count)
                                IFCImportFile.TheLog.LogWarning(Outer.Id, "Processing " + shapeEditScope.CreatedFacesCount + " valid faces out of " + Outer.Faces.Count + " total.", false);

                            geomObjs = shapeEditScope.CreateGeometry(guid);
                        }
                    }
                }
            }
            
            if (geomObjs == null || geomObjs.Count == 0)
            {
                // Couldn't use fallback, or fallback didn't work.
                IFCImportFile.TheLog.LogWarning(Id, "Couldn't create any geometry.", false);
               return null;
            }

            return geomObjs;
        }
        
        override protected void Process(IFCAnyHandle ifcManifoldSolidBrep)
        {
            base.Process(ifcManifoldSolidBrep);

            // We will not fail if the transform is not given, but instead assume it to be the identity.
            IFCAnyHandle ifcOuter = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcManifoldSolidBrep, "Outer", true);
            Outer = IFCClosedShell.ProcessIFCClosedShell(ifcOuter);

            if (IFCAnyHandleUtil.IsSubTypeOf(ifcManifoldSolidBrep, IFCEntityType.IfcFacetedBrepWithVoids))
            {
                HashSet<IFCAnyHandle> ifcVoids = 
                    IFCAnyHandleUtil.GetAggregateInstanceAttribute<HashSet<IFCAnyHandle>>(ifcManifoldSolidBrep, "Voids");
                if (ifcVoids != null)
                {
                    foreach (IFCAnyHandle ifcVoid in ifcVoids)
                    {
                        try
                        {
                            Inners.Add(IFCClosedShell.ProcessIFCClosedShell(ifcVoid));
                        }
                        catch
                        {
                            // LOG: WARNING: #: Invalid inner shell ifcVoid.StepId, ignoring.
                        }
                    }
                }
            }
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
            base.CreateShapeInternal(shapeEditScope, scaledLcs, lcs, guid);

            // Ignoring Inner shells for now.
            if (Outer != null)
            {
                try
                {
                    IList<GeometryObject> solids = CreateGeometry(shapeEditScope, scaledLcs, lcs, guid);
                    if (solids != null)
                    {
                        foreach (GeometryObject solid in solids)
                        {
                            shapeEditScope.AddGeometry(IFCSolidInfo.Create(Id, solid));
                        }
                    }
                    else
                        IFCImportFile.TheLog.LogError(Outer.Id, "cannot create valid solid, ignoring.", false);
                }
                catch (Exception ex)
                {
                    IFCImportFile.TheLog.LogError(Outer.Id, ex.Message, false);
                }
            }
        }

        protected IFCManifoldSolidBrep(IFCAnyHandle item)
        {
            Process(item);
        }

        /// <summary>
        /// Create an IFCManifoldSolidBrep object from a handle of type IfcManifoldSolidBrep.
        /// </summary>
        /// <param name="ifcManifoldSolidBrep">The IFC handle.</param>
        /// <returns>The IFCManifoldSolidBrep object.</returns>
        public static IFCManifoldSolidBrep ProcessIFCManifoldSolidBrep(IFCAnyHandle ifcManifoldSolidBrep)
        {
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcManifoldSolidBrep))
            {
                IFCImportFile.TheLog.LogNullError(IFCEntityType.IfcManifoldSolidBrep);
                return null;
            }

            IFCEntity manifoldSolidBrep;
            if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcManifoldSolidBrep.StepId, out manifoldSolidBrep))
                manifoldSolidBrep = new IFCManifoldSolidBrep(ifcManifoldSolidBrep);
            return (manifoldSolidBrep as IFCManifoldSolidBrep);
        }
    }
}
