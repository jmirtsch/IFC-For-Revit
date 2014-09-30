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
        static public double IFCFuzzyXYZEpsilon
        {
            get;
            protected set;
        }

        /// <summary>
        /// A class to allow comparison of XYZ values based on a static epsilon value contained in the IFCImportShapeEditScope class.
        /// The static epsilon value should be set before using these values.
        /// </summary>
        public class IFCFuzzyXYZ : XYZ, IComparable
        {
            public IFCFuzzyXYZ() : base() { }

            public IFCFuzzyXYZ(XYZ xyz) : base(xyz.X, xyz.Y, xyz.Z) { }

            /// <summary>
            /// Compare an IFCFuzzyXYZ with an XYZ value by checking that their individual X,Y, and Z components are within an epsilon value of each other.
            /// </summary>
            /// <param name="obj">The other value, an XYZ or IFCFuzzyXYZ.</param>
            /// <returns>0 if they are equal, -1 if this is smaller than obj, and 1 if this is larger than obj.</returns>
            public int CompareTo(Object obj)
            {
                if (obj == null || (!(obj is XYZ)))
                    return -1;

                XYZ otherXYZ = obj as XYZ;
                for (int ii = 0; ii < 3; ii++)
                {
                    if (this[ii] < otherXYZ[ii] - IFCFuzzyXYZEpsilon)
                        return -1;
                    if (this[ii] > otherXYZ[ii] + IFCFuzzyXYZEpsilon)
                        return 1;
                }
                
                return 0;
            }
        }

        private Document m_Document = null;

        private IFCProduct m_Creator = null;

        private IFCRepresentation m_ContainingRepresentation = null;

        private ElementId m_GraphicsStyleId = ElementId.InvalidElementId;

        private ElementId m_CategoryId = ElementId.InvalidElementId;

        /// <summary>
        /// The names of the associated IfcPresentationLayerWithStyles
        /// </summary>
        private ISet<string> m_PresentationLayerNames = null;

        /// <summary>
        /// A stack of material element id from IFCStyledItems and IFCPresentationLayerWithStyles.  The "current" material id should generally be used.
        /// </summary>
        private IList<ElementId> m_MaterialIdList = null;

        // store all curves for 2D plan representation.  
        private ViewShapeBuilder m_ViewShapeBuilder = null;

        // stores all faces from the face set which will be built
        private TessellatedShapeBuilder m_TessellatedShapeBuilder = null;

        /// <summary>
        /// A map of IFCFuzzyXYZ to XYZ values.  In practice, the two values will be the same, but this allows us to
        /// "look up" an XYZ value and get the fuzzy equivalent.  Internally, this is represented by a SortedDictionary.
        /// </summary>
        private IDictionary<IFCFuzzyXYZ, XYZ> m_TessellatedFaceVertices = null;

        // stores the current face being input. After the face will be
        // completely set, it will be inserted into the resident shape builder.
        private IList<IList<XYZ>> m_TessellatedFaceBoundary = null;

        // material of the current face - see 'm_TessellatedFaceBoundary'
        private ElementId m_faceMaterialId = ElementId.InvalidElementId;

        // The target geometry being created.  This may affect tolerances used to include or exclude vertices that are very close to one another,
        // or potentially degenerate faces.
        private TessellatedShapeBuilderTarget m_TargetGeometry = TessellatedShapeBuilderTarget.AnyGeometry;

        // The fallback geometry being created.
        private TessellatedShapeBuilderFallback m_FallbackGeometry = TessellatedShapeBuilderFallback.Mesh;

        // The target geometry being created.  This may affect tolerances used to include or exclude vertices that are very 
        // close to one another, or potentially degenerate faces.
        public TessellatedShapeBuilderTarget TargetGeometry
        {
            get { return m_TargetGeometry; }
            private set { m_TargetGeometry = value; }
        }

        // The fallback geometry that will be created if we can't make the target geometry.
        public TessellatedShapeBuilderFallback FallbackGeometry
        {
            get { return m_FallbackGeometry; }
            private set { m_FallbackGeometry = value; }
        }

        private IList<ElementId> MaterialIdList
        {
            get
            {
                if (m_MaterialIdList == null)
                    m_MaterialIdList = new List<ElementId>();
                return m_MaterialIdList;
            }
        }

        private void PushMaterialId(ElementId materialId)
        {
            MaterialIdList.Add(materialId);
        }

        private void PopMaterialId()
        {
            int count = MaterialIdList.Count;
            if (count > 0)
                MaterialIdList.RemoveAt(count - 1);
        }

        /// <summary>
        /// The material id associated with the representation item currently being processed.
        /// </summary>
        /// <returns></returns>
        public ElementId GetCurrentMaterialId()
        {
            int count = MaterialIdList.Count;
            if (count == 0)
                return ElementId.InvalidElementId;
            return MaterialIdList[count - 1];
        }

        /// <summary>
        /// A class to responsibly set - and unset - ContainingRepresentation.  
        /// Intended to be used with the "using" keyword.
        /// </summary>
        public class IFCContainingRepresentationSetter : IDisposable
        {
            private IFCImportShapeEditScope m_Scope = null;
            private IFCRepresentation m_OldRepresentation = null;

            /// <summary>
            /// The constructor.
            /// </summary>
            /// <param name="scope">The associated shape edit scope.</param>
            /// <param name="item">The current styled item.</param>
            public IFCContainingRepresentationSetter(IFCImportShapeEditScope scope, IFCRepresentation containingRepresentation)
            {
                if (scope != null)
                {
                    m_Scope = scope;
                    m_OldRepresentation = scope.ContainingRepresentation;
                    scope.ContainingRepresentation = containingRepresentation;
                }
            }

            #region IDisposable Members

            public void Dispose()
            {
                if (m_Scope != null)
                    m_Scope.ContainingRepresentation = m_OldRepresentation;
            }

            #endregion
        }

        /// <summary>
        /// A class to allow temporarily changing the required geometric output of IFCImportShapeEditScope.
        /// </summary>
        public class IFCTargetSetter : IDisposable
        {
            private IFCImportShapeEditScope m_Scope = null;
            private TessellatedShapeBuilderTarget m_TargetGeometry = TessellatedShapeBuilderTarget.AnyGeometry;
            private TessellatedShapeBuilderFallback m_FallbackGeometry = TessellatedShapeBuilderFallback.Mesh;

            /// <summary>
            /// The constructor.
            /// </summary>
            /// <param name="scope">The associated shape edit scope.</param>
            /// <param name="targetGeometry">The current target geometry.</param>
            /// <param name="fallbackGeometry">The current fallback geometry.</param>
            public IFCTargetSetter(IFCImportShapeEditScope scope, TessellatedShapeBuilderTarget targetGeometry, TessellatedShapeBuilderFallback fallbackGeometry)
            {
                if (scope != null)
                {
                    m_Scope = scope;

                    m_TargetGeometry = m_Scope.TargetGeometry;
                    m_FallbackGeometry = m_Scope.FallbackGeometry;

                    m_Scope.SetTargetAndFallbackGeometry(targetGeometry, fallbackGeometry);
                }
            }

            #region IDisposable Members

            public void Dispose()
            {
                if (m_Scope != null)
                    m_Scope.SetTargetAndFallbackGeometry(m_TargetGeometry, m_FallbackGeometry);
            }

            #endregion
        }

        /// <summary>
        /// The class containing all of the IfcStyledItems currently active.
        /// </summary>
        public class IFCMaterialStack : IDisposable
        {
            private IFCImportShapeEditScope m_Scope = null;
            private ElementId m_MaterialElementId = ElementId.InvalidElementId;
            
            /// <summary>
            /// The constructor.
            /// </summary>
            /// <param name="scope">The associated shape edit scope.</param>
            /// <param name="item">The current styled item.</param>
            public IFCMaterialStack(IFCImportShapeEditScope scope, IFCStyledItem styledItem, IFCPresentationLayerAssignment layerAssignment)
            {
                m_Scope = scope;
                if (styledItem != null)
                    m_MaterialElementId = styledItem.GetMaterialElementId(scope);
                else if (layerAssignment != null)
                    m_MaterialElementId = layerAssignment.GetMaterialElementId(scope);

                if (m_MaterialElementId  != ElementId.InvalidElementId)
                    m_Scope.PushMaterialId(m_MaterialElementId);
            }

            #region IDisposable Members

            public void Dispose()
            {
                if (m_MaterialElementId != ElementId.InvalidElementId)
                    m_Scope.PopMaterialId();
            }

            #endregion
        }

        /// <summary>
        /// The names of the presentation layers created in this scope.
        /// </summary>
        public ISet<string> PresentationLayerNames
        {
            get
            {
                if (m_PresentationLayerNames == null)
                    m_PresentationLayerNames = new SortedSet<string>();
                return m_PresentationLayerNames;
            }
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

        /// <summary>
        /// The IFCRepresentation that contains the currently processed IFC entity.
        /// </summary>
        public IFCRepresentation ContainingRepresentation
        {
            get { return m_ContainingRepresentation; }
            protected set { m_ContainingRepresentation = value; }
        }

        /// <summary>
        /// Set the target and fallback geometry for this scope.
        /// </summary>
        /// <param name="targetGeometry">The target geometry.</param>
        /// <param name="fallbackGeometry">The fallback geometry.</param>
        /// <remarks>This should not be directly called, but instead set with the IFCTargetSetter and the "using" scope.</remarks>
        public void SetTargetAndFallbackGeometry(TessellatedShapeBuilderTarget targetGeometry, TessellatedShapeBuilderFallback fallbackGeometry)
        {
            TargetGeometry = targetGeometry;
            FallbackGeometry = fallbackGeometry;
        }

        /// <summary>
        /// Start collecting faces to create a BRep solid.
        /// </summary>
        public void StartCollectingFaceSet()
        {
            if (m_TessellatedShapeBuilder == null)
                m_TessellatedShapeBuilder = new TessellatedShapeBuilder();

            m_TessellatedShapeBuilder.OpenConnectedFaceSet(false);

            if (m_TessellatedFaceVertices != null)
                m_TessellatedFaceVertices.Clear();

            if (m_TessellatedFaceBoundary != null)
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

            if (m_TessellatedFaceVertices != null)
                m_TessellatedFaceVertices.Clear();

            m_faceMaterialId = ElementId.InvalidElementId;
        }

        /// <summary>
        /// Start collecting edges for a face to create a BRep solid.
        /// </summary>
        public void StartCollectingFace(ElementId materialId)
        {
            if (m_TessellatedShapeBuilder == null)
                throw new InvalidOperationException("StartCollectingFaceSet has not been called.");

            if (m_TessellatedFaceBoundary == null)
                m_TessellatedFaceBoundary = new List<IList<XYZ>>();
            else
                m_TessellatedFaceBoundary.Clear();

            if (m_TessellatedFaceVertices == null)
                m_TessellatedFaceVertices = new SortedDictionary<IFCFuzzyXYZ, XYZ>();

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
        /// Check if we have started building a face.
        /// </summary>
        /// <returns>True if we have collected at least one face boundary, false otherwise.
        public bool HaveActiveFace()
        {
            return (m_TessellatedFaceBoundary != null && m_TessellatedFaceBoundary.Count > 0);
        }

        /// <summary>
        /// Remove the current invalid face from the list of faces to create a BRep solid.
        /// </summary>
        public void AbortCurrentFace()
        {
            if (m_TessellatedFaceBoundary != null)
                m_TessellatedFaceBoundary.Clear();

            m_faceMaterialId = ElementId.InvalidElementId;
        }

        /// <summary>
        /// Add one loop of vertices that will define a boundary loop of the current face.
        /// </summary>
        public void AddLoopVertices(IList<XYZ> loopVertices)
        {
            int vertexCount = (loopVertices == null) ? 0 : loopVertices.Count;
            if (vertexCount < 3)
                throw new InvalidOperationException("Too few distinct loop vertices, ignoring.");

            IList<XYZ> adjustedLoopVertices = new List<XYZ>();
            IDictionary<IFCFuzzyXYZ, int> createdVertices = new SortedDictionary<IFCFuzzyXYZ, int>();

            int numCreated = 0;
            for (int ii = 0; ii < vertexCount; ii++)
            {
                IFCFuzzyXYZ fuzzyXYZ = new IFCFuzzyXYZ(loopVertices[ii]);

                int createdVertexIndex = -1;
                if (createdVertices.TryGetValue(fuzzyXYZ, out createdVertexIndex))
                {
                    // We will allow the first and last point to be equivalent, or the current and last point.  Otherwise we will throw.
                    if (((createdVertexIndex == 0) && (ii == vertexCount - 1)) || (createdVertexIndex == numCreated-1))
                        continue;

                    throw new InvalidOperationException("Loop is self-intersecting, ignoring.");
                }

                XYZ adjustedXYZ;
                if (!m_TessellatedFaceVertices.TryGetValue(fuzzyXYZ, out adjustedXYZ))
                    adjustedXYZ = m_TessellatedFaceVertices[fuzzyXYZ] = loopVertices[ii];
                
                adjustedLoopVertices.Add(adjustedXYZ);
                createdVertices[new IFCFuzzyXYZ(adjustedXYZ)] = numCreated;
                numCreated++;
            }

            // Checking start and end points should be covered above.
            if (numCreated < 3)
                throw new InvalidOperationException("Loop has less than 3 distinct vertices, ignoring.");

            m_TessellatedFaceBoundary.Add(adjustedLoopVertices);
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
        private IList<GeometryObject> CreateGeometryObjects(string guid,
           out bool hasInvalidData, out TessellatedShapeBuilderOutcome outcome)
        {
            m_TessellatedShapeBuilder.CloseConnectedFaceSet();

            // The OwnerInfo is currently unused; the value doesn't really matter.
            m_TessellatedShapeBuilder.LogString = IFCImportFile.TheFileName;
            m_TessellatedShapeBuilder.LogInteger = IFCImportFile.TheBrepCounter;
            m_TessellatedShapeBuilder.OwnerInfo = guid != null ? guid : "Temporary Element";
            TessellatedShapeBuilderResult result = m_TessellatedShapeBuilder.Build(
                TargetGeometry, FallbackGeometry, GraphicsStyleId);

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
        private IList<GeometryObject> CreateClosedSolid(string guid)
        {
            if (TargetGeometry != TessellatedShapeBuilderTarget.Solid || FallbackGeometry != TessellatedShapeBuilderFallback.Abort)
                throw new ArgumentException("CreateClosedSolid expects TessellatedShapeBuilderTarget.Solid and TessellatedShapeBuilderFallback.Abort.");

            bool invalidData;
            TessellatedShapeBuilderOutcome outcome;
            IList<GeometryObject> geomObjs = CreateGeometryObjects(guid, out invalidData, out outcome);

            if (geomObjs == null || geomObjs.Count != 1 || geomObjs[0] == null)
                return new List<GeometryObject>();

            if (geomObjs[0] is Solid)
                return geomObjs;

            // TessellatedShapeBuilder is only allowed to return a Solid, or nothing in this case.  If it returns something else, throw.
            throw new InvalidOperationException("Unexpected object was created");
        }

        /// <summary>
        /// Indicates whether we are required to create a Solid.
        /// </summary>
        /// <returns>True if we are required to create a Solid, false otherwise.</returns>
        public bool MustCreateSolid()
        {
            return (TargetGeometry == TessellatedShapeBuilderTarget.Solid &&
                FallbackGeometry == TessellatedShapeBuilderFallback.Abort);
        }

        /// <summary>
        /// Indicates whether we are attempting to create a Solid as our primary target.
        /// </summary>
        /// <returns>True if we are first trying to create a Solid, false otherwise.</returns>
        public bool TryToCreateSolid()
        {
            return (TargetGeometry == TessellatedShapeBuilderTarget.AnyGeometry ||
                TargetGeometry == TessellatedShapeBuilderTarget.Solid);
        }

        /// <summary>
        /// If possible, create a Solid representing all faces in all face sets. If a Solid can't be created,
        /// then a Mesh is created instead.</summary>
        /// <returns>null or an IList containing 1 or more GeometryObjects of type Solid or Mesh.</returns>
        /// <remarks>Usually a single-element IList is returned, but multiple GeometryObjects may be created.</remarks>
        private IList<GeometryObject> CreateSolidOrMesh(string guid)
        {
            if (TargetGeometry != TessellatedShapeBuilderTarget.AnyGeometry || FallbackGeometry != TessellatedShapeBuilderFallback.Mesh)
                throw new ArgumentException("CreateMesh expects TessellatedShapeBuilderTarget.AnyGeometry and TessellatedShapeBuilderFallback.Mesh.");

            bool invalidData;
            TessellatedShapeBuilderOutcome outcome;
            IList<GeometryObject> geomObjects = CreateGeometryObjects(guid, out invalidData, out outcome);
            if (outcome == TessellatedShapeBuilderOutcome.Nothing)
                IFCImportFile.TheLog.LogWarning(Creator.Id, "Couldn't create any geometry.", false);

            return geomObjects;
        }

        /// <summary>
        /// Create geometry with the TessellatedShapeBuilder based on already existing settings.
        /// </summary>
        /// <param name="guid">The Guid associated with the geometry.</param>
        /// <returns>A list of GeometryObjects, possibly empty.</returns>
        public IList<GeometryObject> CreateGeometry(string guid)
        {
            if (TargetGeometry == TessellatedShapeBuilderTarget.AnyGeometry && FallbackGeometry == TessellatedShapeBuilderFallback.Mesh)
                return CreateSolidOrMesh(guid);

            if (TargetGeometry == TessellatedShapeBuilderTarget.Solid && FallbackGeometry == TessellatedShapeBuilderFallback.Abort)
                return CreateClosedSolid(guid);

            throw new ArgumentException("Unhandled TessellatedShapeBuilderTarget and TessellatedShapeBuilderFallback for CreateGeometry.");
        }

        // End temporary classes for holding BRep information.

        protected IFCImportShapeEditScope(Document doc, IFCProduct creator)
        {
            Document = doc;
            Creator = creator;

            // Note that this tolerance is larger than required for meshes, and slightly larger than
            // required for BReps, as it is a cube instead of a sphere of equivalence.  However, we are
            // generally trying to create Solids over Meshes, and as such we try for Solid tolerances.
            IFCFuzzyXYZEpsilon = IFCImportFile.TheFile.Document.Application.ShortCurveTolerance;
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
        /// Add a curve to the Footprint reprensentation of the object in scope.
        /// </summary>
        /// <param name="curve">The curve.</param>
        public void AddFootprintCurve(Curve curve)
        {
            if (curve == null)
                return;

            Creator.FootprintCurves.Add(curve);
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
                        // We will move the origin to Z=0 if necessary, since the VSB requires all curves to be in the Z=0 plane.
                        IntersectionResult result = curve.Project(XYZ.Zero);
                        if (result != null && result.XYZPoint != null && !MathUtil.IsAlmostZero(result.XYZPoint.Z))
                        {
                            try
                            {
                                Transform offsetTransform = Transform.CreateTranslation(-result.XYZPoint.Z * XYZ.BasisZ);
                                Curve projectedCurve = curve.CreateTransformed(offsetTransform);
                                if (projectedCurve != null && m_ViewShapeBuilder.ValidateCurve(projectedCurve))
                                {
                                    m_ViewShapeBuilder.AddCurve(projectedCurve);
                                    continue;
                                }
                            }
                            catch
                            {
                            }
                        }

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
