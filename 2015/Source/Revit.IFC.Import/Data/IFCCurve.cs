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
        /// Get the curve or CurveLoop representation of IFCCurve, as a list of 0 or more curves.
        /// </summary>
        public IList<Curve> GetCurves()
        {
            IList<Curve> curves = new List<Curve>();

            if (Curve != null)
                curves.Add(Curve);
            else if (CurveLoop != null)
            {
                foreach (Curve curve in CurveLoop)
                    curves.Add(curve);
            }

            return curves;
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

        private IList<Curve> ProcessIFCCompositeCurveSegment(IFCAnyHandle ifcCurveSegment)
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
                return null;
            }

            bool hasCurve = (ifcParentCurve.Curve != null);
            bool hasCurveLoop = (ifcParentCurve.CurveLoop != null);
            if (!hasCurve && !hasCurveLoop)
            {
                IFCImportFile.TheLog.LogError(ifcCurveSegment.StepId, "Error processing ParentCurve for IfcCompositeCurveSegment.", false);
                return null;
            }

            IList<Curve> curveSegments = new List<Curve>();
            if (hasCurve)
            {
                curveSegments.Add(ifcParentCurve.Curve);
            }
            else if (hasCurveLoop)
            {
                foreach (Curve subCurve in ifcParentCurve.CurveLoop)
                {
                    curveSegments.Add(subCurve);
                }
            }

            return curveSegments;
        }

        private Line RepairLineAndReport(int id, XYZ startPoint, XYZ endPoint, double gap)
        {
            string gapAsString = UnitFormatUtils.Format(IFCImportFile.TheFile.Document.GetUnits(), UnitType.UT_Length, gap, true, false);
            IFCImportFile.TheLog.LogWarning(id, "Repaired gap of size " + gapAsString + " in IfcCompositeCurve.", false);
            return Line.CreateBound(startPoint, endPoint);
        }

        private void ProcessIFCCompositeCurve(IFCAnyHandle ifcCurve)
        {
            // We are going to attempt minor repairs for small but reasonable gaps between Line/Line and Line/Arc pairs.  As such, we want to collect the
            // curves before we create the curve loop.

            IList<IFCAnyHandle> segments = IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(ifcCurve, "Segments");
            if (segments == null)
                IFCImportFile.TheLog.LogError(Id, "Invalid IfcCompositeCurve with no segments.", true);

            // need List<> so that we can AddRange later.
            List<Curve> curveSegments = new List<Curve>();
            foreach (IFCAnyHandle segment in segments)
            {
                IList<Curve> currCurve = ProcessIFCCompositeCurveSegment(segment);
                if (currCurve != null && currCurve.Count != 0)
                    curveSegments.AddRange(currCurve);
            }

            int numSegments = curveSegments.Count;
            if (numSegments == 0)
                IFCImportFile.TheLog.LogError(Id, "Invalid IfcCompositeCurve with no segments.", true);

            if (CurveLoop == null) 
                CurveLoop = new CurveLoop();

            try
            {
                // We are going to try to reverse or tweak segments as necessary to make the CurveLoop.
                // NOTE: we do not do any checks yet to repair the endpoints of the curveloop to make them closed.
                // NOTE: this is not expected to be perfect with dirty data, but is expected to not change already valid data.
                XYZ startPoint = curveSegments[0].GetEndPoint(0);
                XYZ endPoint = curveSegments[0].GetEndPoint(1);

                double vertexEps = MathUtil.VertexEps;
                double gapVertexEps = vertexEps * 5.0;  // This is the largest gap we'll repair.

                bool firstCurveIsLine = (curveSegments[0] is Line);
                for (int ii = 1; ii < numSegments; ii++)
                {
                    XYZ nextStartPoint = curveSegments[ii].GetEndPoint(0);
                    XYZ nextEndPoint = curveSegments[ii].GetEndPoint(1);

                    for (int jj = 0; jj < 2; jj++)
                    {
                        bool doRepair = (jj == 1);
                        double currVertexEps = (jj == 0) ? vertexEps : gapVertexEps;

                        // Check that we can repair.  At least one of the two segments must be a line.
                        bool canRepairFirst = doRepair && (curveSegments[ii - 1] is Line);
                        bool canRepairSecond = doRepair && (curveSegments[ii] is Line);
                        if (doRepair && !canRepairFirst && !canRepairSecond)
                            IFCImportFile.TheLog.LogError(Id, "IfcCompositeCurve contains a gap that cannot be repaired.", true);
                                                
                        // Trivial case: endPoint is almost equal to nextStartPoint.  Update the end point and continue.
                        double dist1 = endPoint.DistanceTo(nextStartPoint);
                        double dist2 = endPoint.DistanceTo(nextEndPoint);

                        // If we are repairing, and the next segment is short but valid, fix the gap to the closer endpoint.
                        if (dist1 < currVertexEps && (!(doRepair && (dist1 > dist2))))
                        {
                            endPoint = nextEndPoint;
                            if (doRepair)
                            {
                                if (canRepairFirst)
                                    curveSegments[ii - 1] = RepairLineAndReport(Id, curveSegments[ii - 1].GetEndPoint(0), nextStartPoint, dist1);
                                else
                                    curveSegments[ii] = RepairLineAndReport(Id, curveSegments[ii - 1].GetEndPoint(1), nextEndPoint, dist1);
                            }
                            break;
                        }

                        // Now we try to repair.  Next case - next segment is reversed.
                        if (dist2 < currVertexEps)
                        {
                            curveSegments[ii] = curveSegments[ii].CreateReversed();
                            endPoint = nextStartPoint;
                            if (doRepair)
                            {
                                if (canRepairFirst)
                                    curveSegments[ii - 1] = RepairLineAndReport(Id, curveSegments[ii - 1].GetEndPoint(0), nextEndPoint, dist2);
                                else
                                    curveSegments[ii] = RepairLineAndReport(Id, curveSegments[ii - 1].GetEndPoint(1), nextStartPoint, dist2);
                            }
                            break;
                        }

                        // We will now be trying to append ot the start of the loop.  We can only do repairs if either the start curve or the current curve
                        // is a line.
                        if (doRepair && !firstCurveIsLine && !canRepairSecond)
                            IFCImportFile.TheLog.LogError(Id, "IfcCompositeCurve contains a gap that cannot be repaired.", true);

                        // Next case: the curve needs to be appended to the front, not the back, of the curve loop.
                        dist1 = startPoint.DistanceTo(nextEndPoint);
                        dist2 = startPoint.DistanceTo(nextStartPoint);

                        // If we are repairing, and the next segment is short but valid, fix the gap to the closer endpoint.
                        if (dist1 < currVertexEps && (!(doRepair && (dist1 > dist2))))
                        {
                            Curve tmpCurve = curveSegments[ii];
                            curveSegments.RemoveAt(ii);
                            curveSegments.Insert(0, tmpCurve);
                            startPoint = nextStartPoint;
                            firstCurveIsLine = canRepairSecond;
                            if (doRepair)
                            {
                                if (firstCurveIsLine)   // The former first curve is now the second curve.
                                    curveSegments[1] = RepairLineAndReport(Id, nextEndPoint, curveSegments[1].GetEndPoint(1), dist1);
                                else
                                    curveSegments[0] = RepairLineAndReport(Id, curveSegments[0].GetEndPoint(0), curveSegments[1].GetEndPoint(0), dist1);
                            }
                            break;
                        }

                        // Last simple case: the curve needs to be appended to the front, and it is reversed.
                        if (dist2 < currVertexEps)
                        {
                            Curve tmpCurve = curveSegments[ii].CreateReversed();
                            curveSegments.RemoveAt(ii);
                            curveSegments.Insert(0, tmpCurve);
                            startPoint = nextEndPoint;
                            firstCurveIsLine = canRepairSecond;
                            if (doRepair)
                            {
                                if (firstCurveIsLine)   // The former first curve is now the second curve.
                                    curveSegments[1] = RepairLineAndReport(Id, nextStartPoint, curveSegments[1].GetEndPoint(1), dist2);
                                else
                                    curveSegments[0] = RepairLineAndReport(Id, curveSegments[0].GetEndPoint(0), curveSegments[1].GetEndPoint(0), dist2);
                            }
                            break;
                        }
                    }
                }

                foreach (Curve curveSegment in curveSegments)
                {
                    CurveLoop.Append(curveSegment);
                }
            }
            catch (Exception ex)
            {
                IFCImportFile.TheLog.LogError(Id, ex.Message, true);
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
                    else
                        trimParamDouble = IFCUnitUtil.ScaleLength(trimParamDouble);
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

            IFCTrimmingPreference trimPreference = IFCEnums.GetSafeEnumerationAttribute<IFCTrimmingPreference>(ifcCurve, "MasterRepresentation", IFCTrimmingPreference.Parameter);

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
                    IFCImportFile.TheLog.LogError(Id, "Param1 = Param2 for IfcTrimmedCurve #, ignoring.", false);
                    return;
                }

                if (param1 >  param2 - MathUtil.Eps())
                {
                    IFCImportFile.TheLog.LogWarning(Id, "Param1 > Param2 for IfcTrimmedCurve #, reversing.", false);
                    MathUtil.Swap(ref param1, ref param2);
                    return;
                } 
                
                Curve copyCurve = baseCurve.Clone();

                if (param2 - param1 <= IFCImportFile.TheFile.Document.Application.ShortCurveTolerance)
                {
                    string lengthAsString = UnitFormatUtils.Format(IFCImportFile.TheFile.Document.GetUnits(), UnitType.UT_Length, param2 - param1, true, false);
                    IFCImportFile.TheLog.LogError(Id, "curve length of " + lengthAsString + " is invalid, ignoring.", false);
                    return;
                }

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

        private Curve CreateTransformedCurve(Curve baseCurve, IFCRepresentation parentRep, Transform lcs)
        {
            Curve transformedCurve = (baseCurve != null) ? baseCurve.CreateTransformed(lcs) : null;
            if (transformedCurve == null)
            {
                IFCImportFile.TheLog.LogWarning(Id, "couldn't create curve for " +
                    ((parentRep == null) ? "" : parentRep.Identifier.ToString()) +
                    " representation.", false);
            }

            return transformedCurve;
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
            // Reject Axis curves - not yet supported.
            IFCRepresentation parentRep = shapeEditScope.ContainingRepresentation;

            IFCRepresentationIdentifier repId = (parentRep == null) ? IFCRepresentationIdentifier.Unhandled : parentRep.Identifier;
            bool createModelGeometry = (repId == IFCRepresentationIdentifier.Axis);
            if (createModelGeometry)
            {
                IFCImportFile.TheLog.LogWarning(Id, "Can't process Axis representation, ignoring.", true);
                return;
            } 
            
            base.CreateShapeInternal(shapeEditScope, lcs, scaledLcs, guid);

            IList<Curve> transformedCurves = new List<Curve>();
            if (Curve != null)
            {
                Curve transformedCurve = CreateTransformedCurve(Curve, parentRep, lcs);
                if (transformedCurve != null)
                    transformedCurves.Add(transformedCurve);
            }
            else if (CurveLoop != null)
            {
                foreach (Curve curve in CurveLoop)
                {
                    Curve transformedCurve = CreateTransformedCurve(curve, parentRep, lcs);
                    if (transformedCurve != null)
                        transformedCurves.Add(transformedCurve);
                }
            }

            // TODO: set graphics style for footprint curves.
            ElementId gstyleId = ElementId.InvalidElementId;
            foreach (Curve curve in transformedCurves)
            {
                // Default: assume a plan view curve.
                shapeEditScope.AddFootprintCurve(curve);
            }
        }
    }
}
