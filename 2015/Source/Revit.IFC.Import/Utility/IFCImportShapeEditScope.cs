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
using System.IO;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Data;
using UnitSystem = Autodesk.Revit.DB.DisplayUnit;
using UnitName = Autodesk.Revit.DB.DisplayUnitType;

namespace Revit.IFC.Import.Utility
{
    /// <summary>
    /// Provides methods to manage creation of DirectShape elements.
    /// </summary>
    public class IFCImportShapeEditScope: IDisposable
    {
        private Document m_Document = null;

        private IFCProduct m_Creator = null;

        private ElementId m_GraphicsStyleId = ElementId.InvalidElementId;

        private ElementId m_CategoryId = ElementId.InvalidElementId;

        /// <summary>
        /// A stack of created styles.  The "current" style should generally be used.
        /// </summary>
        private IList<IFCStyledItem> m_StyledItemList = null;

        private IList<IFCStyledItem> StyledItemList
        {
            get
            {
                if (m_StyledItemList == null)
                    m_StyledItemList = new List<IFCStyledItem>();
                return m_StyledItemList;
            }
        }

        private void PushStyledItem(IFCStyledItem item)
        {
            StyledItemList.Add(item);
        }

        private void PopStyledItem()
        {
            int count = StyledItemList.Count;
            if (count > 0)
                StyledItemList.RemoveAt(count - 1);
        }

        /// <summary>
        /// The id of the associated graphics style, if any.
        /// </summary>
        public ElementId GraphicsStyleId
        {
            get { return m_GraphicsStyleId; }
            set { m_GraphicsStyleId = value; }
        }

        /// <summary>
        /// The id of the associated category.
        /// </summary>
        public ElementId CategoryId
        {
           get { return m_CategoryId; }
           set { m_CategoryId = value; }
        }

        /// <summary>
        /// The IfcStyledItem associated with the representation item currently being processed.
        /// </summary>
        /// <returns></returns>
        public IFCStyledItem GetCurrentStyledItem()
        {
            int count = StyledItemList.Count;
            if (count == 0)
                return null;
            return StyledItemList[count - 1];
        }

        /// <summary>
        /// The class containing all of the IfcStyledItems currently active.
        /// </summary>
        public class IFCStyledItemStack : IDisposable
        {
            private IFCImportShapeEditScope m_Scope = null;
            private IFCStyledItem m_Item = null;

            /// <summary>
            /// The constructor.
            /// </summary>
            /// <param name="scope">The associated shape edit scope.</param>
            /// <param name="item">The current styled item.</param>
            public IFCStyledItemStack(IFCImportShapeEditScope scope, IFCStyledItem item)
            {
                m_Scope = scope;
                m_Item = item;
                if (m_Item != null)
                    m_Scope.PushStyledItem(m_Item);
            }

            #region IDisposable Members

            public void Dispose()
            {
                if (m_Item != null) 
                    m_Scope.PopStyledItem();
            }

            #endregion
        }

        /// <summary>
        /// The document associated with this element.
        /// </summary>
        public Document Document
        {
            get { return m_Document; }
            protected set { m_Document = value; }
        }

        /// <summary>
        /// Get the top-level IFC entity associated with this shape.
        /// </summary>
        public IFCProduct Creator
        {
            get { return m_Creator; }
            protected set { m_Creator = value; }
        }

        // store all curves for 2D plan representation.  
        private ViewShapeBuilder m_ViewShapeBuilder = null;

        // stores all faces from the face set which will be built
        private TessellatedShapeBuilder m_TessellatedShapeBuilder = null;

        // stores the current face being input. After the face will be
        // completely set, it will be inserted into the resident shape builder.
        private IList<IList<XYZ>> m_TessellatedFaceBoundary = null;

        // material of the current face - see 'm_TessellatedFaceBoundary'
        private ElementId m_faceMaterialId = ElementId.InvalidElementId;

        /// <summary>
        /// Start collecting faces to create a BRep solid.
        /// </summary>
        public void StartCollectingFaceSet()
        {
           if(m_TessellatedShapeBuilder == null)
               m_TessellatedShapeBuilder = new TessellatedShapeBuilder();

           m_TessellatedShapeBuilder.OpenConnectedFaceSet(false);

           if(m_TessellatedFaceBoundary != null)
              m_TessellatedFaceBoundary.Clear();

           m_faceMaterialId = ElementId.InvalidElementId;
        }

        /// <summary>
        /// Stop collecting faces to create a BRep solid.
        /// </summary>
        /// 
        public void StopCollectingFaceSet()
        {
           if (m_TessellatedShapeBuilder == null)
              throw new InvalidOperationException("StartCollectingFaceSet has not been called.");

           m_TessellatedShapeBuilder.CloseConnectedFaceSet();

           if (m_TessellatedFaceBoundary != null)
              m_TessellatedFaceBoundary.Clear();

           m_faceMaterialId = ElementId.InvalidElementId;
        }

        /// <summary>
        /// Start collecting edges for a face to create a BRep solid.
        /// </summary>
        public void StartCollectingFace(ElementId materialId)
        {
           if(m_TessellatedShapeBuilder == null)
              throw new InvalidOperationException("StartCollectingFaceSet has not been called.");

           if(m_TessellatedFaceBoundary == null)
              m_TessellatedFaceBoundary = new List<IList<XYZ>>();
           else
              m_TessellatedFaceBoundary.Clear();

           m_faceMaterialId = materialId;
        }

        /// <summary>
        /// Stop collecting edges for a face to create a BRep solid.
        /// </summary>
        public void StopCollectingFace()
        {
           if (m_TessellatedShapeBuilder == null || m_TessellatedFaceBoundary == null)
              throw new InvalidOperationException("StartCollectingFace has not been called.");

            TessellatedFace theFace = new TessellatedFace(m_TessellatedFaceBoundary, m_faceMaterialId);
            m_TessellatedShapeBuilder.AddFace(theFace);

            m_TessellatedFaceBoundary.Clear();

            m_faceMaterialId = ElementId.InvalidElementId;
        }

        /// <summary>
        /// Remove the current invalid face from the list of faces to create a BRep solid.
        /// </summary>
        public void AbortCurrentFace()
        {
           if(m_TessellatedFaceBoundary != null)
              m_TessellatedFaceBoundary.Clear();

           m_faceMaterialId = ElementId.InvalidElementId;
        }

        /// <summary>
        /// Add one loop of vertexes that will define a boundary loop of the current face.
        /// </summary>
        public void AddLoopVertexes(IList<XYZ> loopVertexes)
        {
           if (loopVertexes == null || loopVertexes.Count < 3)
           {
              throw new InvalidOperationException("In AddLoopVertexes, loopVertexes is null or has less than 3 vertexes.");
           }

           m_TessellatedFaceBoundary.Add(loopVertexes);
        }

        /// <summary>
        /// Create a geometry object(s) described by stored face sets, if possible.
        /// Usually a single-element IList conatining either Solid or Mesh is returned.
        /// A two-elemant IList containing a Solid as the 1st element and a Mesh as
        /// the 2nd is returned if while building multiple face sets, a fallback
        /// was used for some but not all sets.
        /// </summary>
        /// <returns>The IList created, or null. The IList can contain a Solid and/or a Mesh.
        /// If Solid is present, it always the 1st element.</returns>
        private IList<GeometryObject> CreateGeometryObjects(
           TessellatedShapeBuilderTarget target, TessellatedShapeBuilderFallback fallback, string guid,
           out bool hasInvalidData, out TessellatedShapeBuilderOutcome outcome)
        {
            m_TessellatedShapeBuilder.CloseConnectedFaceSet();

            m_TessellatedShapeBuilder.LogString = IFCImportFile.TheFileName;
            m_TessellatedShapeBuilder.LogInteger = IFCImportFile.TheBrepCounter;
            m_TessellatedShapeBuilder.OwnerInfo = guid;
            TessellatedShapeBuilderResult result = m_TessellatedShapeBuilder.Build(
                target, fallback, GraphicsStyleId);

            // It is important that we clear the TSB after we build above, otherwise we will "collect" geometries
            // in the DirectShape and create huge files with redundant data.
            m_TessellatedShapeBuilder.Clear();
            hasInvalidData = result.HasInvalidData;
            outcome = result.Outcome;
            return result.GetGeometricalObjects();
        }
        

        /// <summary>
        /// Create a closed Solid if possible. If the face sets have unusable faces
        /// or describe an open Solid, then nothing is created.
        /// </summary>
        /// <returns>The Solid created, or null.</returns>
        public Solid CreateClosedSolid(string guid)
        {
           bool invalidData;
           TessellatedShapeBuilderOutcome outcome;
           IList<GeometryObject> geomObjs = CreateGeometryObjects(
              TessellatedShapeBuilderTarget.Solid, TessellatedShapeBuilderFallback.Abort, guid,
              out invalidData, out outcome);

           if (geomObjs == null || geomObjs.Count != 1 || geomObjs[0] == null)
              return null;

           if (geomObjs[0] is Solid)
           {
              Solid solid = geomObjs[0] as Solid;
              return solid;
           }

           throw new InvalidOperationException("Unexpected object was created");
        }

        /// <summary>
        /// Create a Mesh if possible.
        /// The function creates a mesh representing as many faces from the original
        /// face sets as possible, while skipping unusable faces.
        /// </summary>
        /// <returns>The Mesh created, or null.</returns>
        public Mesh CreateMesh(string guid)
        {
           bool invalidData;
           TessellatedShapeBuilderOutcome outcome;
           IList<GeometryObject> geomObjs = CreateGeometryObjects(
              TessellatedShapeBuilderTarget.Mesh, TessellatedShapeBuilderFallback.Salvage, guid,
              out invalidData, out outcome);

           if (geomObjs == null || geomObjs.Count != 1 || geomObjs[0] == null)
              return null;

           if (geomObjs[0] is Mesh)
           {
              Mesh mesh = geomObjs[0] as Mesh;
              return mesh;
           }

           throw new InvalidOperationException("Unexpected object was created");
        }

        /// <summary>
        /// If posible function creates a Solid representing all faces in all face
        /// sets. If some faces in some sets are unusable, then a Mesh is created
        /// instead. All faces from all face sets, which contain some unusable faces.
        /// will be represented by a Mesh </summary>
        /// <returns>Null or an IList conatining GeometryObjects of type Solid or Mesh, or null.
        /// Usually a single-element IList is returned, but if multiple face sets are being built
        /// and only some of them can be built as Solids, then a two-element IList containing
        /// a Solid as 1st element and a Mesh as 2nd is returned.</returns>
        public IList<GeometryObject> CreateSolidOrMesh(string guid)
        {
           bool invalidData;
           TessellatedShapeBuilderOutcome outcome;
           return CreateGeometryObjects(
              TessellatedShapeBuilderTarget.AnyGeometry, TessellatedShapeBuilderFallback.Mesh, guid,
              out invalidData, out outcome);
        }

        // End temporary classes for holding BRep information.

        protected IFCImportShapeEditScope(Document doc, IFCProduct creator)
        {
            Document = doc;
            Creator = creator;
        }

        /// <summary>
        /// Add a Solid to the current DirectShape element.
        /// </summary>
        /// <param name="solidInfo">The IFCSolidInfo class describing the solid.</param>
        public void AddGeometry(IFCSolidInfo solidInfo)
        {
            if (solidInfo == null || solidInfo.GeometryObject == null)
                return;

            Creator.Solids.Add(solidInfo);
        }

        /// <summary>
        /// Add curves to represent the plan view of the created object.
        /// </summary>
        /// <param name="curves">The list of curves, to be validated.</param>
        /// <param name="id">The id of the object being created, for error logging.</param>
        /// <returns>True if any curves were added to the plan view representation.</returns>
        public bool AddPlanViewCurves(IList<Curve> curves, int id)
        {
            m_ViewShapeBuilder = null;
            int numCurves = curves.Count;
            if (numCurves > 0)
            {
                m_ViewShapeBuilder = new ViewShapeBuilder(DirectShapeTargetViewType.Plan);
                foreach (Curve curve in curves)
                {
                    if (m_ViewShapeBuilder.ValidateCurve(curve))
                        m_ViewShapeBuilder.AddCurve(curve);
                    else
                    {
                        IFCImportFile.TheLog.LogError(id, "Invalid curve in FootPrint representation, ignoring.", false);
                        numCurves--;
                    }
                }

                if (numCurves == 0)
                    m_ViewShapeBuilder = null;
            }

            return (m_ViewShapeBuilder != null);
        }

        /// <summary>
        /// Set the plan view representation of the given DirectShape or DirectShapeType given the information created by AddPlanViewCurves.
        /// </summary>
        /// <param name="shape">The DirectShape or DirectShapeType.</param>
        public void SetPlanViewRep(Element shape)
        {
            if (m_ViewShapeBuilder != null)
            {
                if (shape is DirectShape)
                    m_ViewShapeBuilder.SetShape(shape as DirectShape);
                else if (shape is DirectShapeType)
                    m_ViewShapeBuilder.SetShape(shape as DirectShapeType);
                else
                    throw new ArgumentException("SetPlanViewRep only works on DirectShape and DirectShapeType.");
            }
        }

        /// <summary>
        /// Create a new edit scope.  Intended to be used with the "using" keyword.
        /// </summary>
        /// <param name="doc">The import document.</param>
        /// <param name="action">The name of the current action.</param>
        /// <param name="creator">The entity being processed.</param>
        /// <returns>The new edit scope.</returns>
        static public IFCImportShapeEditScope Create(Document doc, IFCProduct creator)
        {
            return new IFCImportShapeEditScope(doc, creator);
        }

        #region IDisposable Members

        public void Dispose()
        {
        }

        #endregion
    }
}
