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
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
    public class IFCCartesianTransformOperator : IFCRepresentationItem
    {
        Transform m_BaseTransform = Transform.Identity;

        double m_Scale = 1.0;

        double? m_ScaleY = null;

        double? m_ScaleZ = null;

        /// <summary>
        /// The transform associated with the IfcCartesianTransformOperator
        /// </summary>
        public Transform Transform
        {
            get { return m_BaseTransform; }
            protected set { m_BaseTransform = value; }
        }

        /// <summary>
        /// The base scale for all dimensions, if ScaleY and ScaleZ are not set; otherwise scale in X direction.
        /// </summary>
        public double Scale
        {
            get { return m_Scale; }
            protected set { m_Scale = value; }
        }

        /// <summary>
        /// The optional base scale for Y direction.
        /// </summary>
        public double? ScaleY
        {
            get { return m_ScaleY; }
            protected set { m_ScaleY = value; }
        }

        /// <summary>
        /// The optional base scale for Z direction.
        /// </summary>
        public double? ScaleZ
        {
            get { return m_ScaleZ; }
            protected set { m_ScaleZ = value; }
        }
        
        protected IFCCartesianTransformOperator()
        {
        }

        override protected void Process(IFCAnyHandle item)
        {
            base.Process(item);

            IFCAnyHandle localOrigin = IFCImportHandleUtil.GetRequiredInstanceAttribute(item, "LocalOrigin", false);
            XYZ origin = null;
            if (localOrigin != null)
                origin = IFCPoint.ProcessScaledLengthIFCCartesianPoint(localOrigin);
            else
                origin = XYZ.Zero;

            IFCAnyHandle axis1 = IFCImportHandleUtil.GetOptionalInstanceAttribute(item, "Axis1");
            XYZ xAxis = null;
            if (axis1 != null)
                xAxis = IFCPoint.ProcessNormalizedIFCDirection(axis1);

            IFCAnyHandle axis2 = IFCImportHandleUtil.GetOptionalInstanceAttribute(item, "Axis2");
            XYZ yAxis = null;
            if (axis2 != null)
                yAxis = IFCPoint.ProcessNormalizedIFCDirection(axis2);

            Scale = IFCImportHandleUtil.GetOptionalRealAttribute(item, "Scale", 1.0);

            XYZ zAxis = null;

            if (IFCAnyHandleUtil.IsSubTypeOf(item, IFCEntityType.IfcCartesianTransformationOperator2DnonUniform))
                ScaleY = IFCImportHandleUtil.GetOptionalRealAttribute(item, "Scale2", Scale);
            else if (IFCAnyHandleUtil.IsSubTypeOf(item, IFCEntityType.IfcCartesianTransformationOperator3D))
            {
                IFCAnyHandle axis3 = IFCImportHandleUtil.GetOptionalInstanceAttribute(item, "Axis3");
                if (axis3 != null)
                    zAxis = IFCPoint.ProcessNormalizedIFCDirection(axis3);
                if (IFCAnyHandleUtil.IsSubTypeOf(item, IFCEntityType.IfcCartesianTransformationOperator3DnonUniform))
                {
                    ScaleY = IFCImportHandleUtil.GetOptionalRealAttribute(item, "Scale2", Scale);
                    ScaleZ = IFCImportHandleUtil.GetOptionalRealAttribute(item, "Scale3", Scale);
                }
            }

            // Set the axes based on what is specified.
            // If all three axes are set, ensure they are truly orthogonal.
            // If two axes are set, ensure they are orthogonal and set the 3rd axis to be the cross product.
            // If one axis is set, arbitrarily set the next axis to be the basis vector which 
            // If no axes are set, use identity transform.
            if (xAxis == null)
            {
                if (yAxis == null)
                {
                    if (zAxis == null)
                    {
                        xAxis = XYZ.BasisX;
                        yAxis = XYZ.BasisY;
                        zAxis = XYZ.BasisZ;
                    }
                }
                else if (zAxis == null)
                {
                    // Special case - Y axis is in XY plane.
                    if (MathUtil.IsAlmostZero(yAxis[2]))
                    {
                        xAxis = new XYZ(yAxis[1], -yAxis[0], 0.0);
                        zAxis = XYZ.BasisZ;
                    }
                    else
                        throw new InvalidOperationException("#" + item.StepId + ": IfcCartesianTransformOperator has only y axis defined, ignoring.");
                }
                else
                {
                    xAxis = yAxis.CrossProduct(zAxis);
                }
            }
            else if (yAxis == null)
            {
                if (zAxis == null)
                {
                    // Special case - X axis is in XY plane.
                    if (MathUtil.IsAlmostZero(xAxis[2]))
                    {
                        yAxis = new XYZ(xAxis[1], -xAxis[0], 0.0);
                        zAxis = XYZ.BasisZ;
                    }
                    else
                        throw new InvalidOperationException("#" + item.StepId + ": IfcCartesianTransformOperator has only x axis defined, ignoring.");
                }
                else
                {
                    yAxis = zAxis.CrossProduct(xAxis);
                }
            }
            else if (zAxis == null)
            {
                zAxis = xAxis.CrossProduct(yAxis);
            }

            // Make sure that the axes are really orthogonal.
            if (!MathUtil.IsAlmostZero(xAxis.DotProduct(zAxis)))
                zAxis = xAxis.CrossProduct(yAxis);
            if (!MathUtil.IsAlmostZero(xAxis.DotProduct(yAxis)))
                yAxis = zAxis.CrossProduct(xAxis);

            Transform = Transform.CreateTranslation(origin);
            Transform.set_Basis(0, xAxis);
            Transform.set_Basis(1, yAxis);
            Transform.set_Basis(2, zAxis);
        }

        protected IFCCartesianTransformOperator(IFCAnyHandle item)
        {
            Process(item);
        }

        /// <summary>
        /// Allows creation of "identity" IFCCartesianTransformOperator if no value provided.
        /// </summary>
        /// <returns>The IFCCartesianTransformOperator.</returns>
        public static IFCCartesianTransformOperator ProcessIFCCartesianTransformOperator()
        {
            return new IFCCartesianTransformOperator();
        }

        /// <summary>
        /// Creates an IFCCartesianTransformOperator corresponding to an IFC handle.
        /// </summary>
        /// <param name="item">The handle.</param>
        /// <returns>The IFCCartesianTransformOperator.</returns>
        public static IFCCartesianTransformOperator ProcessIFCCartesianTransformOperator(IFCAnyHandle ifcTransformOperator)
        {
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcTransformOperator))
            {
                Importer.TheLog.LogNullError(IFCEntityType.IfcCartesianTransformationOperator);
                return null;
            }

            IFCEntity transformOperator;
            if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcTransformOperator.StepId, out transformOperator))
                transformOperator = new IFCCartesianTransformOperator(ifcTransformOperator);
            return (transformOperator as IFCCartesianTransformOperator); 
        }
    }
}
