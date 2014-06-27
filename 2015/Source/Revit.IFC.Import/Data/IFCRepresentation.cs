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
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
    /// <summary>
    /// Represents an IfcRepresentation.
    /// </summary>
    public class IFCRepresentation : IFCEntity
    {
        protected IFCRepresentationContext m_RepresentationContext = null;

        protected string m_RepresentationIdentifier = null;

        protected string m_RepresentationType = null;

        protected IList<IFCRepresentationItem> m_RepresentationItems = null;

        // Special holder for "Box" representation type only.
        protected BoundingBoxXYZ m_BoundingBox = null;

        protected IFCPresentationLayerAssignment m_LayerAssignment = null;

        /// <summary>
        /// The related IfcRepresentationContext.
        /// </summary>
        public IFCRepresentationContext Context
        {
            get { return m_RepresentationContext; }
            protected set { m_RepresentationContext = value; }
        }

        /// <summary>
        /// The optional representation identifier.
        /// </summary>
        public string Identifier
        {
            get { return m_RepresentationIdentifier; }
            protected set { m_RepresentationIdentifier = value; }
        }

        /// <summary>
        /// The optional representation type.
        /// </summary>
        public string Type
        {
            get { return m_RepresentationType; }
            protected set { m_RepresentationType = value; }
        }
        
        /// <summary>
        /// The bounding box, only valid for "Box" representation type.
        /// </summary>
        public BoundingBoxXYZ BoundingBox
        {
            get { return m_BoundingBox; }
            protected set { m_BoundingBox = value; }
        }

        /// <summary>
        /// The representations of the product.
        /// </summary>
        public IList<IFCRepresentationItem> RepresentationItems
        {
            get
            {
                if (m_RepresentationItems == null)
                    m_RepresentationItems = new List<IFCRepresentationItem>();
                return m_RepresentationItems;
            }
        }

        /// <summary>
        /// The associated layer assignment of the representation item, if any.
        /// </summary>
        public IFCPresentationLayerAssignment LayerAssignment
        {
            get { return m_LayerAssignment; }
            protected set { m_LayerAssignment = value; }
        }
        
        /// <summary>
        /// Default constructor.
        /// </summary>
        protected IFCRepresentation()
        {

        }

        // Check current ("FootPrint") and old ("Annotation", "Plan") labels that all mean the same thing.
        private bool IsFootprintRep(string identifier)
        {
            return ((string.Compare(Identifier, "FootPrint", true) == 0) ||
                (string.Compare(Identifier, "Annotation", true) == 0) ||
                (string.Compare(Identifier, "Plan", true) == 0));
        }

        /// <summary>
        /// Determines if a representation is a 2D footprint.
        /// </summary>
        /// <returns>True if it is, false otherwise.</returns>
        public bool IsFootprintRepresentation()
        {
            return IsFootprintRep(Identifier);
        }

        private bool NotAllowedInPlan(IFCAnyHandle item)
        {
            return !(IFCAnyHandleUtil.IsSubTypeOf(item, IFCEntityType.IfcCurve) || 
                IFCAnyHandleUtil.IsSubTypeOf(item, IFCEntityType.IfcGeometricSet) ||
                IFCAnyHandleUtil.IsSubTypeOf(item, IFCEntityType.IfcMappedItem));
        }

        private bool AllowedOnlyInPlan(IFCAnyHandle item)
        {
            return (IFCAnyHandleUtil.IsSubTypeOf(item, IFCEntityType.IfcCurve) || IFCAnyHandleUtil.IsSubTypeOf(item, IFCEntityType.IfcGeometricSet));
        }

        /// <summary>
        /// Processes IfcRepresentation attributes.
        /// </summary>
        /// <param name="ifcRepresentation">The IfcRepresentation handle.</param>
        override protected void Process(IFCAnyHandle ifcRepresentation)
        {
            base.Process(ifcRepresentation);

            IFCAnyHandle representationContext = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcRepresentation, "ContextOfItems", false);
            if (representationContext != null)
                Context = IFCRepresentationContext.ProcessIFCRepresentationContext(representationContext);

            Identifier = IFCImportHandleUtil.GetOptionalStringAttribute(ifcRepresentation, "RepresentationIdentifier", null);

            Type = IFCImportHandleUtil.GetOptionalStringAttribute(ifcRepresentation, "RepresentationType", null);

            HashSet<IFCAnyHandle> items =
                IFCAnyHandleUtil.GetAggregateInstanceAttribute<HashSet<IFCAnyHandle>>(ifcRepresentation, "Items");

            LayerAssignment = IFCPresentationLayerAssignment.GetTheLayerAssignment(ifcRepresentation);

            if (string.Compare(Identifier, "Box", true) == 0)
            {
                if (!IFCImportFile.TheFile.Options.ProcessBoundingBoxGeometry)
                    throw new InvalidOperationException("BoundingBox not imported with ProcessBoundingBoxGeometry=false");

                foreach (IFCAnyHandle item in items)
                {
                    if (BoundingBox != null)
                    {
                        // LOG: WARNING: #: Found more than 1 Bounding Box, #, ignoring.
                        return;
                    }

                    if (IFCAnyHandleUtil.IsSubTypeOf(item, IFCEntityType.IfcBoundingBox))
                        BoundingBox = ProcessBoundingBox(item);
                    else
                    {
                        // LOG: ERROR: #: Expected IfcBoundingBox, found {type}, ignoring.
                    }
                }
            }
            else
            {
                bool isFootprintRep = IsFootprintRep(Identifier);

                foreach (IFCAnyHandle item in items)
                {
                    IFCRepresentationItem repItem = null;
                    try
                    {
                        // Ignore curves outside of FootPrint representation for now.
                        if (!isFootprintRep && AllowedOnlyInPlan(item))
                        {
                            IFCImportFile.TheLog.LogWarning(item.StepId, "Ignoring unhandled curves in " + Identifier + " representation.", true);
                            continue;
                        }
                        else if (isFootprintRep && NotAllowedInPlan(item))
                        {
                            IFCEntityType entityType = IFCAnyHandleUtil.GetEntityType(item);
                            IFCImportFile.TheLog.LogWarning(item.StepId, "Only curves handled in 'FootPrint' representation, ignoring " + entityType.ToString(), true);
                            continue;
                        }
                        repItem = IFCRepresentationItem.ProcessIFCRepresentationItem(item);
                    }
                    catch (Exception ex)
                    {
                        IFCImportFile.TheLog.LogError(item.StepId, ex.Message, false);
                    }
                    if (repItem != null)
                        RepresentationItems.Add(repItem);
                }
            }
        }

        /// <summary>
        /// Deal with missing "LayerAssignments" in IFC2x3 EXP file.
        /// </summary>
        /// <param name="layerAssignment">The layer assignment to add to this representation.</param>
        public void PostProcessLayerAssignment(IFCPresentationLayerAssignment layerAssignment)
        {
            if (LayerAssignment == null)
                LayerAssignment = layerAssignment;
            else
                IFCImportDataUtil.CheckLayerAssignmentConsistency(LayerAssignment, layerAssignment, Id);
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        protected IFCRepresentation(IFCAnyHandle representation)
        {
            Process(representation);
        }

        /// <summary>
        /// Create geometry for a particular representation.
        /// </summary>
        /// <param name="shapeEditScope">The geometry creation scope.</param>
        /// <param name="lcs">Local coordinate system for the geometry, without scale.</param>
        /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
        /// <param name="guid">The guid of an element for which represntation is being created.</param>
        public void CreateShape(IFCImportShapeEditScope shapeEditScope, Transform lcs, Transform scaledLcs, string guid)
        {
            if (LayerAssignment != null)
                LayerAssignment.Create(shapeEditScope);

            // There is an assumption here that Process() weeded out any items that are invalid for this representation.
            using (IFCImportShapeEditScope.IFCMaterialStack stack = new IFCImportShapeEditScope.IFCMaterialStack(shapeEditScope, null, LayerAssignment))
            {
                using (IFCImportShapeEditScope.IFCContainingRepresentationSetter repSetter = new IFCImportShapeEditScope.IFCContainingRepresentationSetter(shapeEditScope, this))
                {
                    foreach (IFCRepresentationItem representationItem in RepresentationItems)
                    {
                        representationItem.CreateShape(shapeEditScope, lcs, scaledLcs, false, guid);
                    }
                }
            }
        }

        static private BoundingBoxXYZ ProcessBoundingBox(IFCAnyHandle boundingBoxHnd)
        {
            IFCAnyHandle lowerLeftHnd = IFCAnyHandleUtil.GetInstanceAttribute(boundingBoxHnd, "Corner");
            XYZ minXYZ = IFCPoint.ProcessScaledLengthIFCCartesianPoint(lowerLeftHnd);

            double xDim = IFCAnyHandleUtil.GetDoubleAttribute(boundingBoxHnd, "XDim").Value;
            double yDim = IFCAnyHandleUtil.GetDoubleAttribute(boundingBoxHnd, "YDim").Value;
            double zDim = IFCAnyHandleUtil.GetDoubleAttribute(boundingBoxHnd, "ZDim").Value;

            XYZ maxXYZ = new XYZ(minXYZ.X + xDim, minXYZ.Y + yDim, minXYZ.Z + zDim);
            BoundingBoxXYZ boundingBox = new BoundingBoxXYZ();
            boundingBox.set_Bounds(0, minXYZ);
            boundingBox.set_Bounds(1, maxXYZ);
            return boundingBox;
        }

        /// <summary>
        /// Processes an IfcRepresentation object.
        /// </summary>
        /// <param name="ifcRepresentation">The IfcRepresentation handle.</param>
        /// <returns>The IFCRepresentation object.</returns>
        public static IFCRepresentation ProcessIFCRepresentation(IFCAnyHandle ifcRepresentation)
        {
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcRepresentation))
            {
                IFCImportFile.TheLog.LogNullError(IFCEntityType.IfcRepresentation);
                return null;
            }

            IFCEntity representation;
            if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcRepresentation.StepId, out representation))
                return (representation as IFCRepresentation);

            return new IFCRepresentation(ifcRepresentation);
        }
    }
}
