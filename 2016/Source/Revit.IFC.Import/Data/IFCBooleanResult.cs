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
    public class IFCBooleanResult : IFCRepresentationItem, IIFCBooleanOperand
    {
        IFCBooleanOperator? m_BooleanOperator = null;

        IIFCBooleanOperand m_FirstOperand;

        IIFCBooleanOperand m_SecondOperand;

        /// <summary>
        /// The boolean operator.
        /// </summary>
        public IFCBooleanOperator? BooleanOperator
        {
            get { return m_BooleanOperator; }
            protected set { m_BooleanOperator = value; }
        }

        /// <summary>
        /// The first boolean operand.
        /// </summary>
        public IIFCBooleanOperand FirstOperand
        {
            get { return m_FirstOperand; }
            protected set { m_FirstOperand = value; }
        }

        /// <summary>
        /// The second boolean operand.
        /// </summary>
        public IIFCBooleanOperand SecondOperand
        {
            get { return m_SecondOperand; }
            protected set { m_SecondOperand = value; }
        }
        
        protected IFCBooleanResult()
        {
        }

        override protected void Process(IFCAnyHandle item)
        {
            base.Process(item);

            IFCBooleanOperator? booleanOperator = IFCEnums.GetSafeEnumerationAttribute<IFCBooleanOperator>(item, "Operator");
            if (booleanOperator.HasValue)
                BooleanOperator = booleanOperator.Value;

            IFCAnyHandle firstOperand = IFCImportHandleUtil.GetRequiredInstanceAttribute(item, "FirstOperand", true);
            FirstOperand = IFCBooleanOperand.ProcessIFCBooleanOperand(firstOperand);
                
            IFCAnyHandle secondOperand = IFCImportHandleUtil.GetRequiredInstanceAttribute(item, "SecondOperand", true);


            // We'll allow a solid to be created even if the second operand can't be properly handled.
            try
            {
                SecondOperand = IFCBooleanOperand.ProcessIFCBooleanOperand(secondOperand);
            }
            catch (Exception ex)
            {
                SecondOperand = null;
                IFCImportFile.TheLog.LogError(secondOperand.StepId, ex.Message, false);
            }
        }

        /// <summary>
        /// Return geometry for a particular representation item.
        /// </summary>
        /// <param name="shapeEditScope">The geometry creation scope.</param>
        /// <param name="lcs">Local coordinate system for the geometry, without scale.</param>
        /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
        /// <param name="guid">The guid of an element for which represntation is being created.</param>
        /// <returns>The created geometry.</returns>
        public IList<GeometryObject> CreateGeometry(
              IFCImportShapeEditScope shapeEditScope, Transform lcs, Transform scaledLcs, string guid)
        {
            IList<GeometryObject> firstSolids = FirstOperand.CreateGeometry(shapeEditScope, lcs, scaledLcs, guid);
            if (firstSolids != null)
            {
                foreach (GeometryObject potentialSolid in firstSolids)
                {
                    if (!(potentialSolid is Solid))
                    {
                        IFCImportFile.TheLog.LogError((FirstOperand as IFCRepresentationItem).Id, "Can't perform Boolean operation on a Mesh.", false);
                        return firstSolids;
                    }
                }
            }

            IList<GeometryObject> secondSolids = null;
            if (SecondOperand != null)
            {
                try
                {
                    using (IFCImportShapeEditScope.IFCTargetSetter setter =
                        new IFCImportShapeEditScope.IFCTargetSetter(shapeEditScope, TessellatedShapeBuilderTarget.Solid, TessellatedShapeBuilderFallback.Abort))
                    {
                        secondSolids = SecondOperand.CreateGeometry(shapeEditScope, lcs, scaledLcs, guid);
                    }
                }
                catch (Exception ex)
                {
                    // We will allow something to be imported, in the case where the second operand is invalid.
                    // If the first (base) operand is invalid, we will still fail the import of this solid.
                    if (SecondOperand is IFCRepresentationItem)
                        IFCImportFile.TheLog.LogError((SecondOperand as IFCRepresentationItem).Id, ex.Message, false);
                    else
                        throw ex;
                    secondSolids = null;
                }
            }

            IList<GeometryObject> resultSolids = null;
            if (firstSolids == null)
            {
                resultSolids = secondSolids;
            }
            else if (secondSolids == null || BooleanOperator == null)
            {
                if (BooleanOperator == null)
                    IFCImportFile.TheLog.LogError(Id, "Invalid BooleanOperationsType.", false);
                resultSolids = firstSolids;
            }
            else
            {
                BooleanOperationsType booleanOperationsType = BooleanOperationsType.Difference;
                switch (BooleanOperator)
                {
                    case IFCBooleanOperator.Difference:
                        booleanOperationsType = BooleanOperationsType.Difference;
                        break;
                    case IFCBooleanOperator.Intersection:
                        booleanOperationsType = BooleanOperationsType.Intersect;
                        break;
                    case IFCBooleanOperator.Union:
                        booleanOperationsType = BooleanOperationsType.Union;
                        break;
                    default:
                        IFCImportFile.TheLog.LogError(Id, "Invalid BooleanOperationsType.", true);
                        break;
                }

                resultSolids = new List<GeometryObject>();
                foreach (GeometryObject firstSolid in firstSolids)
                {
                    Solid resultSolid = (firstSolid as Solid);

                    int secondId = (SecondOperand == null) ? -1 : (SecondOperand as IFCRepresentationItem).Id;
                    foreach (GeometryObject secondSolid in secondSolids)
                    {
                        resultSolid = IFCGeometryUtil.ExecuteSafeBooleanOperation(Id, secondId, resultSolid, secondSolid as Solid, booleanOperationsType);
                        if (resultSolid == null)
                            break;
                    }

                    if (resultSolid != null)
                        resultSolids.Add(resultSolid);
                }
            }

            return resultSolids;
        }

        /// <summary>
        /// Create geometry for a particular representation item, and add to scope.
        /// </summary>
        /// <param name="shapeEditScope">The geometry creation scope.</param>
        /// <param name="lcs">Local coordinate system for the geometry, without scale.</param>
        /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
        /// <param name="guid">The guid of an element for which represntation is being created.</param>
        protected override void CreateShapeInternal(IFCImportShapeEditScope shapeEditScope, Transform lcs, Transform scaledLcs, string guid)
        {
            base.CreateShapeInternal(shapeEditScope, lcs, scaledLcs, guid);

            IList<GeometryObject> resultGeometries = CreateGeometry(shapeEditScope, lcs, scaledLcs, guid);
            if (resultGeometries != null)
            {
                foreach (GeometryObject resultGeometry in resultGeometries)
                {
                    shapeEditScope.AddGeometry(IFCSolidInfo.Create(Id, resultGeometry));
                }
            }
        }

        protected IFCBooleanResult(IFCAnyHandle item)
        {
            Process(item);
        }

        /// <summary>
        /// Create an IFCBooleanResult object from a handle of type IfcBooleanResult.
        /// </summary>
        /// <param name="ifcBooleanResult">The IFC handle.</param>
        /// <returns>The IFCBooleanResult object.</returns>
        public static IFCBooleanResult ProcessIFCBooleanResult(IFCAnyHandle ifcBooleanResult)
        {
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcBooleanResult))
            {
                IFCImportFile.TheLog.LogNullError(IFCEntityType.IfcBooleanResult);
                return null;
            }

            IFCEntity booleanResult;
            if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcBooleanResult.StepId, out booleanResult))
                booleanResult = new IFCBooleanResult(ifcBooleanResult);
            return (booleanResult as IFCBooleanResult);
        }
    }
}
