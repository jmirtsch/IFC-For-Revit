using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
    /// <summary>
    /// Class that represents IfcAdvancedFace entity
    /// </summary>
    public class IFCAdvancedFace : IFCFaceSurface
    {
        protected IFCAdvancedFace()
        { 
        }

        protected IFCAdvancedFace(IFCAnyHandle ifcAdvancedFace) 
        {
            Process(ifcAdvancedFace);
        }

        protected override void Process(IFCAnyHandle ifcAdvancedFace)
        {
            base.Process(ifcAdvancedFace);
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
            Transform localTransform = lcs != null ? lcs : Transform.Identity;
            Surface surface = IFCGeometryUtil.GetTransformedSurface(FaceSurface.GetSurface(), localTransform);
            shapeEditScope.StartCollectingFaceForBrepBuilder(surface, SameSense);

            foreach (IFCFaceBound faceBound in Bounds)
            {
                shapeEditScope.InitializeNewLoopForBrepBuilder();
                faceBound.CreateShape(shapeEditScope, lcs, scaledLcs, guid);

                //If we can't create the outer face boundary, we will abort the creation of this face and throw an exception
                if (!shapeEditScope.HaveActiveFace())
                {
                    throw new InvalidOperationException("Invalid face");
                }

            }
            shapeEditScope.StopCollectingFaceForBrepBuilder();
        }

        /// <summary>
        /// Create an IFCAdvancedFace object from a handle of type IfcAdvancedFace.
        /// </summary>
        /// <param name="ifcAdvancedFace">The IFC handle.</param>
        /// <returns>The IFCAdvancedFace object.</returns>
        public static IFCAdvancedFace ProcessIFCAdvancedFace(IFCAnyHandle ifcAdvancedFace)
        {
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcAdvancedFace))
            {
                IFCImportFile.TheLog.LogNullError(IFCEntityType.IfcAdvancedFace);
                return null;
            }

            IFCEntity face;
            if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcAdvancedFace.StepId, out face))
                face = new IFCAdvancedFace(ifcAdvancedFace);
            return (face as IFCAdvancedFace);
        }
    }
}
