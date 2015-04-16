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
    /// <summary>
    /// Class that represents IfcEdgeLoop entity
    /// </summary>
    public class IFCEdgeLoop : IFCLoop
    {
        IList<IFCOrientedEdge> m_EdgeList = null;

        /// <summary>
        /// A list of oriented edge entities which are concatenated together to form this path.
        /// </summary>
        public IList<IFCOrientedEdge> EdgeList
        {
            get 
            {
                if (m_EdgeList == null) 
                {
                    m_EdgeList = new List<IFCOrientedEdge>();
                }
                return m_EdgeList;
            }
            set { m_EdgeList = value; }
        }

        protected IFCEdgeLoop()
        {
        }

        protected IFCEdgeLoop(IFCAnyHandle ifcEdgeLoop)
        {
            Process(ifcEdgeLoop);
        }

        override protected void Process(IFCAnyHandle ifcEdgeLoop)
        {
            base.Process(ifcEdgeLoop);

            // TODO in REVIT-61368: checks that edgeList is closed and continuous
            IList<IFCAnyHandle> edgeList = IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(ifcEdgeLoop, "EdgeList");
            if (edgeList == null) 
            {
                IFCImportFile.TheLog.LogError(Id, "Cannot find the EdgeList of this loop", true);
            }
            IFCOrientedEdge orientedEdge = null;
            foreach(IFCAnyHandle edge in edgeList) 
            {
                orientedEdge = IFCOrientedEdge.ProcessIFCOrientedEdge(edge);
                EdgeList.Add(orientedEdge);
            }
        }

        protected override CurveLoop GenerateLoop()
        {
            CurveLoop curveLoop = new CurveLoop();
            foreach (IFCOrientedEdge edge in EdgeList) 
            {
                if (edge != null)
                    curveLoop.Append(edge.GetGeometry());
            }
            return curveLoop;
        }

        protected override IList<XYZ> GenerateLoopVertices() 
        {
            return null;
        }

        /// <summary>
        /// Create an IFCEdgeLoop object from a handle of type IfcEdgeLoop.
        /// </summary>
        /// <param name="ifcEdgeLoop">The IFC handle.</param>
        /// <returns>The IFCEdgeLoop object.</returns>
        public static IFCEdgeLoop ProcessIFCEdgeLoop(IFCAnyHandle ifcEdgeLoop)
        {
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcEdgeLoop))
            {
                IFCImportFile.TheLog.LogNullError(IFCEntityType.IfcFace);
                return null;
            }

            IFCEntity edgeLoop;
            if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcEdgeLoop.StepId, out edgeLoop))
                edgeLoop = new IFCEdgeLoop(ifcEdgeLoop);
            return (edgeLoop as IFCEdgeLoop);
        }

        protected override void CreateShapeInternal(IFCImportShapeEditScope shapeEditScope, Transform lcs, Transform scaledLcs, string guid)
        {
            // this code only works for IFC 4 for now
            if (IFCImportFile.TheFile.SchemaVersion > IFCSchemaVersion.IFC2x3)
            {
                foreach (IFCOrientedEdge edge in EdgeList)
                {
                    if (edge == null || edge.EdgeStart == null || edge.EdgeEnd == null) 
                    {
                        IFCImportFile.TheLog.LogError(Id, "Invalid edge loop", true);
                        return;
                    }

                    edge.CreateShape(shapeEditScope, lcs, scaledLcs, guid);

                    if (lcs == null)
                        lcs = Transform.Identity;

                    IFCEdge edgeElement = edge.EdgeElement;
                    Curve edgeGeometry = null;
                    if (edgeElement is IFCEdgeCurve)
                    {
                        edgeGeometry = edgeElement.GetGeometry();
                    }
                    else 
                    {
                        edgeGeometry = null;
                    }
                    XYZ edgeStart = edgeElement.EdgeStart.GetCoordinate();
                    XYZ edgeEnd = edgeElement.EdgeEnd.GetCoordinate();

                    if (edgeStart == null || edgeEnd == null)
                    {
                        IFCImportFile.TheLog.LogError(Id, "Invalid start or end vertices", true);
                    }
                    bool orientation = lcs.HasReflection ? !edge.Orientation : edge.Orientation;
                    shapeEditScope.AddOrientedEdgeToTheBoundary(edgeElement.Id, edgeGeometry.CreateTransformed(lcs), lcs.OfPoint(edgeStart), lcs.OfPoint(edgeEnd), edge.Orientation);
                }
            }
            base.CreateShapeInternal(shapeEditScope, lcs, scaledLcs, guid);
        }
    }
}
