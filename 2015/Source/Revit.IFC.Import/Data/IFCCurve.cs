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
    public class IFCCurve : IFCRepresentationItem
    {
        // One of m_Curve or m_CurveLoop will be non-null.
        Curve m_Curve = null;

        CurveLoop m_CurveLoop = null;

        // In theory, some IfcCurves may use a non-unit length IfcVector to influence the parametrization of the underlying curve.
        // While in practice this is unlikely, we keep this value to be complete.
        double m_ParametericScaling = 1.0;

        /// <summary>
        /// Get the Curve representation of IFCCurve.  It could be null.
        /// </summary>
        public Curve Curve
        {
            get { return m_Curve; }
            protected set { m_Curve = value; }
        }

        /// <summary>
        /// Get the CurveLoop representation of IFCCurve.  It could be null.
        /// </summary>
        public CurveLoop CurveLoop
        {
            get { return m_CurveLoop; }
            protected set { m_CurveLoop = value; }
        }

        /// <summary>
        /// Get the curve or CurveLoop representation of IFCCurve, as a CurveLoop.  This will have a value, as long as Curve or CurveLoop do.
        /// </summary>
        public CurveLoop GetCurveLoop()
        {
            if (CurveLoop != null)
                return CurveLoop;
            if (Curve == null)
                return null;

            CurveLoop curveAsCurveLoop = new CurveLoop();
            curveAsCurveLoop.Append(Curve);
            return curveAsCurveLoop;
        }

        /// <summary>
        /// Calculates the normal of the plane of the curve or curve loop.
        /// </summary>
        /// <returns>The normal, or null if there is no curve or curve loop.</returns>
        public XYZ GetNormal()
        {
            if (Curve != null)
            {
                Transform transform = Curve.ComputeDerivatives(0, false);
                if (transform != null)
                    return transform.BasisZ;
            }
            else if (CurveLoop != null)
            {
                try
                {
                    Plane plane = CurveLoop.GetPlane();
                    if (plane != null)
                        return plane.Normal;
                }
                catch
                {
                }
            }
            
            return null;
        }

        protected IFCCurve()
        {
        }

        private void ProcessIFCLine(IFCAnyHandle ifcCurve)
        {
            IFCAnyHandle pnt = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcCurve, "Pnt", false);
            if (pnt == null)
                return;

            IFCAnyHandle dir = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcCurve, "Dir", false);
            if (dir == null)
                return;

            XYZ pntXYZ = IFCPoint.ProcessScaledLengthIFCCartesianPoint(pnt);
            XYZ dirXYZ = IFCUnitUtil.ScaleLength(IFCPoint.ProcessIFCVector(dir));
            m_ParametericScaling = dirXYZ.GetLength();
            if (MathUtil.IsAlmostZero(m_ParametericScaling))
            {
                IFCImportFile.TheLog.LogWarning(ifcCurve.StepId, "Line has zero length, ignoring.", false);
                return;
            }

            Curve = Line.CreateUnbound(pntXYZ, dirXYZ / m_ParametericScaling);
        }

        private void ProcessIFCCircle(IFCAnyHandle ifcCurve, Transform transform)
        {
            bool found = false;
            double radius = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(ifcCurve, "Radius", out found);
            if (!found)
                return;

            Curve = Arc.Create(transform.Origin, radius, 0, 2.0 * Math.PI, transform.BasisX, transform.BasisY);

        }

        private void ProcessIFCEllipse(IFCAnyHandle ifcCurve, Transform transform)
        {
            bool found = false;
            double radiusX = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(ifcCurve, "SemiAxis1", out found);
            if (!found)
                return;

            double radiusY = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(ifcCurve, "SemiAxis2", out found);
            if (!found)
                return;

            Curve = Ellipse.Create(transform.Origin, radiusX, radiusY, transform.BasisX, transform.BasisY, 0, 2.0 * Math.PI);
        }

        private void ProcessIFCConic(IFCAnyHandle ifcCurve)
        {
            IFCAnyHandle position = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcCurve, "Position", false);
            if (position == null)
                return;

            Transform transform = IFCLocation.ProcessIFCAxis2Placement(position);

            if (IFCAnyHandleUtil.IsSubTypeOf(ifcCurve, IFCEntityType.IfcCircle))
                ProcessIFCCircle(ifcCurve, transform);
            else if (IFCAnyHandleUtil.IsSubTypeOf(ifcCurve, IFCEntityType.IfcEllipse))
                ProcessIFCEllipse(ifcCurve, transform);
            else
                IFCImportFile.TheLog.LogUnhandledSubTypeError(ifcCurve, IFCEntityType.IfcConic, true);
        }

        private void ProcessIFCOffsetCurve2D(IFCAnyHandle ifcCurve)
        {
            IFCAnyHandle basisCurve = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcCurve, "BasisCurve", false);
            if (basisCurve == null)
                return;

            IFCAnyHandle dir = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcCurve, "RefDirection", false);
            
            bool found = false;
            double distance = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(ifcCurve, "Distance", out found);
            if (!found)
                distance = 0.0;

            IFCCurve ifcBasisCurve = IFCCurve.ProcessIFCCurve(basisCurve);
            XYZ dirXYZ = (dir == null) ? ifcBasisCurve.GetNormal() : IFCPoint.ProcessNormalizedIFCDirection(dir);

            try
            {
                if (ifcBasisCurve.Curve != null)
                    Curve = ifcBasisCurve.Curve.CreateOffset(distance, XYZ.BasisZ);
                else if (ifcBasisCurve.CurveLoop != null)
                    CurveLoop = CurveLoop.CreateViaOffset(ifcBasisCurve.CurveLoop, distance, XYZ.BasisZ);
            }
            catch
            {
                IFCImportFile.TheLog.LogError(ifcCurve.StepId, "Couldn't create offset curve.", false);
            }
        }

        private void ProcessIFCOffsetCurve3D(IFCAnyHandle ifcCurve)
        {
            IFCAnyHandle basisCurve = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcCurve, "BasisCurve", false);
            if (basisCurve == null)
                return;

            bool found = false;
            double distance = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(ifcCurve, "Distance", out found);
            if (!found)
                distance = 0.0;

            try
            {
                IFCCurve ifcBasisCurve = IFCCurve.ProcessIFCCurve(basisCurve);
                if (ifcBasisCurve.Curve != null)
                    Curve = ifcBasisCurve.Curve.CreateOffset(distance, XYZ.BasisZ);
                else if (ifcBasisCurve.CurveLoop != null)
                    CurveLoop = CurveLoop.CreateViaOffset(ifcBasisCurve.CurveLoop, distance, XYZ.BasisZ);
            }
            catch
            {
                IFCImportFile.TheLog.LogError(ifcCurve.StepId, "Couldn't create offset curve.", false);
            }
        }

        private void AddCurveToLoopInternal(CurveLoop curveLoop, Curve curve, bool initialReverse, bool tryReversed)
        {
            try
            {
                if (tryReversed != initialReverse)
                    curveLoop.Append(curve.CreateReversed());
                else
                    curveLoop.Append(curve);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("not contiguous"))
                {
                    if (!tryReversed)
                        AddCurveToLoopInternal(curveLoop, curve, initialReverse, true);
                    else
                        throw ex;
                }
            }
        }

        // We are going to try a few passes to add a curve to the CurveLoop, based on the fact that we can't always trust that the
        // orientation of the given curve loop is correct.  So we will:
        // 1. Try to add the curve, according to the orientation we believe is correct.
        // 2. Try to add the curve, reversing the orientation.
        // 3. Reverse the curve loop, and try steps 1-2 again.
        private void AddCurveToLoop(CurveLoop curveLoop, Curve curve, bool initialReverse, bool allowFlip)
        {
            try
            {
                AddCurveToLoopInternal(curveLoop, curve, initialReverse, false);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("not contiguous"))
                {
                    // One last attempt to solve the problem - flip the curve loop, try again.
                    if (allowFlip)
                    {
                        curveLoop.Flip();
                        AddCurveToLoop(curveLoop, curve, initialReverse, false);
                    }
                    else
                        throw ex;
                }
                else
                    throw ex;
            }
        }

        private void ProcessIFCCompositeCurveSegment(IFCAnyHandle ifcCurveSegment)
        {
            bool found = false;

            bool sameSense = IFCImportHandleUtil.GetRequiredBooleanAttribute(ifcCurveSegment, "SameSense", out found);
            if (!found)
                sameSense = true;

            IFCAnyHandle parentCurve = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcCurveSegment, "ParentCurve", true);
            IFCCurve ifcParentCurve = IFCCurve.ProcessIFCCurve(parentCurve);
            if (ifcParentCurve == null)
            {
                IFCImportFile.TheLog.LogError(ifcCurveSegment.StepId, "Error processing ParentCurve for IfcCompositeCurveSegment.", false);
                return;
            }

            bool hasCurve = (ifcParentCurve.Curve != null);
            bool hasCurveLoop = (ifcParentCurve.CurveLoop != null);
            if (!hasCurve && !hasCurveLoop)
            {
                IFCImportFile.TheLog.LogError(ifcCurveSegment.StepId, "Error processing ParentCurve for IfcCompositeCurveSegment.", false);
                return;
            }
            else
            {
                // The CurveLoop here is from the parent IfcCurve, which is the IfcCompositeCurve, which will correspond to a CurveLoop. 
                // IfcCompositiveCurveSegment may be either a curve or a curveloop, which we want to append to the parent curveloop.
                if (CurveLoop == null)
                    CurveLoop = new CurveLoop();
            }

            try
            {
                if (hasCurve)
                {
                    AddCurveToLoop(CurveLoop, ifcParentCurve.Curve, !sameSense, true);
                }
                else if (hasCurveLoop)
                {
                    foreach (Curve subCurve in ifcParentCurve.CurveLoop)
                    {
                        AddCurveToLoop(CurveLoop, subCurve, !sameSense, true);
                    }
                }
            }
            catch (Exception ex)
            {
                IFCImportFile.TheLog.LogError(ifcParentCurve.Id, ex.Message, true);
            }
        }

        private void ProcessIFCCompositeCurve(IFCAnyHandle ifcCurve)
        {
            IList<IFCAnyHandle> segments = IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(ifcCurve, "Segments");
            foreach (IFCAnyHandle segment in segments)
            {
                ProcessIFCCompositeCurveSegment(segment);
            }            
        }
         
        private void ProcessIFCPolyline(IFCAnyHandle ifcCurve)
        {
            IList<IFCAnyHandle> points = IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(ifcCurve, "Points");
            int numPoints = points.Count;
            if (numPoints < 2)
            {
                string msg = "IfcPolyLine had " + numPoints + ", expected at least 2, ignoring";
                IFCImportFile.TheLog.LogError(Id, msg, false);
                return;
            }

            IList<XYZ> pointXYZs = new List<XYZ>();
            foreach (IFCAnyHandle point in points)
            {
                XYZ pointXYZ = IFCPoint.ProcessScaledLengthIFCCartesianPoint(point);
                pointXYZs.Add(pointXYZ);
            }

            CurveLoop = IFCGeometryUtil.CreatePolyCurveLoop(pointXYZs, points, Id, false);
        }

        private double? GetTrimParameter(IFCData trim, IFCCurve basisCurve, IFCTrimmingPreference trimPreference, bool secondAttempt)
        {
            bool preferParam = !(trimPreference == IFCTrimmingPreference.Cartesian);
            if (secondAttempt)
                preferParam = !preferParam;
            double vertexEps = MathUtil.VertexEps;

            IFCAggregate trimAggregate = trim.AsAggregate();
            foreach (IFCData trimParam in trimAggregate)
            {
                if (!preferParam && (trimParam.PrimitiveType == IFCDataPrimitiveType.Instance))
                {
                    IFCAnyHandle trimParamInstance = trimParam.AsInstance();
                    XYZ trimParamPt = IFCPoint.ProcessScaledLengthIFCCartesianPoint(trimParamInstance);
                    if (trimParamPt == null)
                    {
                        IFCImportFile.TheLog.LogWarning(basisCurve.Id, "Invalid trim point for basis curve.", false);
                        continue;
                    }

                    try
                    {
                        IntersectionResult result = basisCurve.Curve.Project(trimParamPt);
                        if (result.Distance < vertexEps)
                            return result.Parameter;

                        IFCImportFile.TheLog.LogWarning(basisCurve.Id, "Cartesian value for trim point not on the basis curve.", false);
                    }
                    catch
                    {
                        IFCImportFile.TheLog.LogWarning(basisCurve.Id, "Cartesian value for trim point not on the basis curve.", false);
                    }
                }
                else if (preferParam && (trimParam.PrimitiveType == IFCDataPrimitiveType.Double))
                {
                    double trimParamDouble = trimParam.AsDouble();
                    if (basisCurve.Curve.IsCyclic)
                        trimParamDouble = IFCUnitUtil.ScaleAngle(trimParamDouble);
                    return trimParamDouble;
                }
            }

            // Try again with opposite preference.
            if (!secondAttempt)
                return GetTrimParameter(trim, basisCurve, trimPreference, true);

            return null;
        }

        private void GetTrimParameters(IFCData trim1, IFCData trim2, IFCCurve basisCurve, IFCTrimmingPreference trimPreference,
            out double param1, out double param2)
        {
            double? condParam1 = GetTrimParameter(trim1, basisCurve, trimPreference, false);
            if (!condParam1.HasValue)
                throw new InvalidOperationException("#" + basisCurve.Id + ": Couldn't apply first trimming parameter of IfcTrimmedCurve.");
            param1 = condParam1.Value;

            double? condParam2 = GetTrimParameter(trim2, basisCurve, trimPreference, false);
            if (!condParam2.HasValue)
                throw new InvalidOperationException("#" + basisCurve.Id + ": Couldn't apply second trimming parameter of IfcTrimmedCurve.");
            param2 = condParam2.Value;

            if (MathUtil.IsAlmostEqual(param1, param2))
            {
                // If we had a cartesian parameter as the trim preference, check if the parameter values are better.
                if (trimPreference == IFCTrimmingPreference.Cartesian)
                {
                    condParam1 = GetTrimParameter(trim1, basisCurve, IFCTrimmingPreference.Parameter, true);
                    if (!condParam1.HasValue)
                        throw new InvalidOperationException("#" + basisCurve.Id + ": Couldn't apply first trimming parameter of IfcTrimmedCurve.");
                    param1 = condParam1.Value;

                    condParam2 = GetTrimParameter(trim2, basisCurve, IFCTrimmingPreference.Parameter, true);
                    if (!condParam2.HasValue)
                        throw new InvalidOperationException("#" + basisCurve.Id + ": Couldn't apply second trimming parameter of IfcTrimmedCurve.");
                    param2 = condParam2.Value;
                }
                else
                    throw new InvalidOperationException("#" + basisCurve.Id + ": Ignoring 0 length curve.");
            }
        }

        private void ProcessIFCTrimmedCurve(IFCAnyHandle ifcCurve)
        {
            bool found = false;

            bool sameSense = IFCImportHandleUtil.GetRequiredBooleanAttribute(ifcCurve, "SenseAgreement", out found);
            if (!found)
                sameSense = true;

            IFCAnyHandle basisCurve = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcCurve, "BasisCurve", true);
            IFCCurve ifcBasisCurve = IFCCurve.ProcessIFCCurve(basisCurve);
            if (ifcBasisCurve == null || (ifcBasisCurve.Curve == null && ifcBasisCurve.CurveLoop == null))
            {
                // LOG: ERROR: Error processing BasisCurve # for IfcTrimmedCurve #.
                return;
            }
            if (ifcBasisCurve.Curve == null)
            {
                // LOG: ERROR: Expected a single curve, not a curve loop for BasisCurve # for IfcTrimmedCurve #.
                return;
            }

            IFCData trim1 = ifcCurve.GetAttribute("Trim1");
            if (trim1.PrimitiveType != IFCDataPrimitiveType.Aggregate)
            {
                // LOG: ERROR: Invalid data type for Trim1 attribute for IfcTrimmedCurve #.
                return;
            }
            IFCData trim2 = ifcCurve.GetAttribute("Trim2");
            if (trim2.PrimitiveType != IFCDataPrimitiveType.Aggregate)
            {
                // LOG: ERROR: Invalid data type for Trim1 attribute for IfcTrimmedCurve #.
                return;
            }

            string trimPreferenceAsString = IFCAnyHandleUtil.GetEnumerationAttribute(ifcCurve, "MasterRepresentation");
            IFCTrimmingPreference trimPreference = IFCTrimmingPreference.Parameter;
            if (trimPreferenceAsString != null)
                trimPreference = (IFCTrimmingPreference)Enum.Parse(typeof(IFCTrimmingPreference), trimPreferenceAsString, true);

            double param1 = 0.0, param2 = 0.0;
            try
            {
                GetTrimParameters(trim1, trim2, ifcBasisCurve, trimPreference, out param1, out param2);
            }
            catch (Exception ex)
            {
                IFCImportFile.TheLog.LogError(ifcCurve.StepId, ex.Message, false);
                return;
            }

            Curve baseCurve = ifcBasisCurve.Curve;
            if (baseCurve.IsCyclic)
            {
                if (!sameSense)
                    MathUtil.Swap(ref param1, ref param2);

                if (param2 < param1)
                    param2 = MathUtil.PutInRange(param2, param1 + Math.PI, 2 * Math.PI);
                
                if (param2 - param1 > 2.0 * Math.PI - MathUtil.Eps())
                {
                    // LOG: WARNING: #Id: IfcTrimmedCurve length is greater than 2*PI, leaving unbound.
                    Curve = baseCurve;
                    return;
                }

                Curve = baseCurve.Clone();
                Curve.MakeBound(param1, param2);
            }
            else
            {
                if (MathUtil.IsAlmostEqual(param1, param2))
                {
                    // LOG: ERROR: Param1 = Param2 for IfcTrimmedCurve #, ignoring.
                    return;
                }

                if (param1 >  param2 - MathUtil.Eps())
                {
                    // LOG: WARNING: Param1 > Param2 for IfcTrimmedCurve #, reversing.
                    MathUtil.Swap(ref param1, ref param2);
                    return;
                } 
                
                Curve copyCurve = baseCurve.Clone();
                copyCurve.MakeBound(param1, param2);
                if (sameSense)
                {
                    Curve = copyCurve;
                }
                else
                {
                    Curve = copyCurve.CreateReversed();
                }
            }
        }

        private void ProcessIFCBoundedCurve(IFCAnyHandle ifcCurve)
        {
            if (IFCAnyHandleUtil.IsSubTypeOf(ifcCurve, IFCEntityType.IfcCompositeCurve))
                ProcessIFCCompositeCurve(ifcCurve);
            else if (IFCAnyHandleUtil.IsSubTypeOf(ifcCurve, IFCEntityType.IfcPolyline))
                ProcessIFCPolyline(ifcCurve);
            else if (IFCAnyHandleUtil.IsSubTypeOf(ifcCurve, IFCEntityType.IfcTrimmedCurve))
                ProcessIFCTrimmedCurve(ifcCurve);
            else if (IFCAnyHandleUtil.IsSubTypeOf(ifcCurve, IFCEntityType.IfcBSplineCurve))
                IFCImportFile.TheLog.LogUnhandledSubTypeError(ifcCurve, IFCEntityType.IfcBoundedCurve, true);
            else
                IFCImportFile.TheLog.LogUnhandledSubTypeError(ifcCurve, IFCEntityType.IfcBoundedCurve, true);
        }

        override protected void Process(IFCAnyHandle ifcCurve)
        {
            base.Process(ifcCurve);

            if (IFCAnyHandleUtil.IsSubTypeOf(ifcCurve, IFCEntityType.IfcBoundedCurve))
                ProcessIFCBoundedCurve(ifcCurve);
            else if (IFCAnyHandleUtil.IsSubTypeOf(ifcCurve, IFCEntityType.IfcConic))
                ProcessIFCConic(ifcCurve);
            else if (IFCAnyHandleUtil.IsSubTypeOf(ifcCurve, IFCEntityType.IfcLine))
                ProcessIFCLine(ifcCurve);
            else if (IFCAnyHandleUtil.IsSubTypeOf(ifcCurve, IFCEntityType.IfcOffsetCurve2D))
                ProcessIFCOffsetCurve2D(ifcCurve);
            else if (IFCAnyHandleUtil.IsSubTypeOf(ifcCurve, IFCEntityType.IfcOffsetCurve3D))
                ProcessIFCOffsetCurve3D(ifcCurve);
            else
                IFCImportFile.TheLog.LogUnhandledSubTypeError(ifcCurve, IFCEntityType.IfcCurve, false);
        }

        protected IFCCurve(IFCAnyHandle profileDef)
        {
            Process(profileDef);
        }

        /// <summary>
        /// Create an IFCCurve object from a handle of type IfcCurve.
        /// </summary>
        /// <param name="ifcCurve">The IFC handle.</param>
        /// <returns>The IFCCurve object.</returns>
        public static IFCCurve ProcessIFCCurve(IFCAnyHandle ifcCurve)
        {
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcCurve))
            {
                IFCImportFile.TheLog.LogNullError(IFCEntityType.IfcCurve); 
                return null;
            }

            IFCEntity curve;
            if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcCurve.StepId, out curve))
                curve = new IFCCurve(ifcCurve);
            return (curve as IFCCurve); 
        }

        /// <summary>
        /// Create geometry for a particular representation item, and add to scope.
        /// </summary>
        /// <param name="shapeEditScope">The geometry creation scope.</param>
        /// <param name="lcs">Local coordinate system for the geometry, without scale.</param>
        /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
        /// <param name="forceSolid">Ignored for IFCCurve.</param>
        /// <param name="guid">The guid of an element for which represntation is being created.</param>
        /// <remarks>This currently assumes that we are create plan view curves.</remarks>
        protected override void CreateShapeInternal(IFCImportShapeEditScope shapeEditScope, Transform lcs, Transform scaledLcs, bool forceSolid, string guid)
        {
            base.CreateShapeInternal(shapeEditScope, lcs, scaledLcs, forceSolid, guid);

            // TODO: set graphics style.
            if (Curve != null)
            {
                Curve transformedCurve = Curve.CreateTransformed(lcs);
                shapeEditScope.Creator.FootprintCurves.Add(transformedCurve);
            }
            else if (CurveLoop != null)
            {
                foreach (Curve curve in CurveLoop)
                {
                    Curve transformedCurve = curve.CreateTransformed(lcs);
                    shapeEditScope.Creator.FootprintCurves.Add(transformedCurve);
                }
            }
        }
    }
}
