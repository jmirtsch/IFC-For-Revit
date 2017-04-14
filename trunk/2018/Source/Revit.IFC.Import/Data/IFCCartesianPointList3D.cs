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
    /// Class that represents an IfcCartesianPoint3D.
    /// </summary>
    public class IFCCartesianPointList3D : IFCRepresentationItem
    {
        IList<IList<double>> m_CoordList = null;

        protected IFCCartesianPointList3D()
        {
        }

        /// <summary>
        /// The list of vertices, where the vertices are represented as an IList of doubles.
        /// </summary>
        public IList<IList<double>> CoordList
        {
            get { return m_CoordList; }
            protected set { m_CoordList = value; }
        }

        /// <summary>
        /// Create IFCCartesianPointList3D instance
        /// </summary>
        /// <param name="item">The handle</param>
        protected IFCCartesianPointList3D(IFCAnyHandle item)
        {
            Process(item);
        }

        /// <summary>
        /// Process the IfcCartesianPointList3D handle.
        /// </summary>
        /// <param name="item">The handle</param>
        protected override void Process(IFCAnyHandle item)
        {
            base.Process(item);

            IList<IList<double>> coordListAttrbute = IFCImportHandleUtil.GetListOfListOfDoubleAttribute(item, "CoordList");
            if (coordListAttrbute != null)
                m_CoordList = coordListAttrbute;
        }

        /// <summary>
        /// Accept the handle for IFCCartesianPointList3D and return the instance (creating it if not yet created)
        /// </summary>
        /// <param name="ifcCartesianPointList3D">The handle.</param>
        /// <returns>The associated IFCCartesianPointList3D class.</returns>
        public static IFCCartesianPointList3D processIFCCartesianPointList3D(IFCAnyHandle ifcCartesianPointList3D)
        {
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcCartesianPointList3D))
            {
                Importer.TheLog.LogNullError(IFCEntityType.IfcCartesianPointList3D);
                return null;
            }

            IFCEntity cartesianPointList3D;
            if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcCartesianPointList3D.StepId, out cartesianPointList3D))
                cartesianPointList3D = new IFCCartesianPointList3D(ifcCartesianPointList3D);
            return (cartesianPointList3D as IFCCartesianPointList3D);
        }
    }
}
