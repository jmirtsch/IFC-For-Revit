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
           throw new InvalidOperationException("Unhandled code.");
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
                Importer.TheLog.LogNullError(IFCEntityType.IfcAdvancedFace);
                return null;
            }

            IFCEntity face;
            if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcAdvancedFace.StepId, out face))
                face = new IFCAdvancedFace(ifcAdvancedFace);
            return (face as IFCAdvancedFace);
        }
    }
}
