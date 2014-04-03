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
using Revit.IFC.Import.Data;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Geometry
{
    /// <summary>
    /// Provides methods to work on Revit geometric objects.
    /// </summary>
    public class IFCGeometryUtil
    {
        private static DirectShape m_SolidValidator = null;

        /// <summary>
        /// Create a DirectShape element that will act to validate solids.
        /// </summary>
        /// <remarks> 
        /// Because there is no API that allows validation of solids to be added to DirectShapeTypes, 
        /// we take advantage of the fact that there is not yet an actual check for the category of the DirectShape when doing 
        /// solid validation.  As such, we can set up a dummy Generic Models DirectShape, that we can globally use to 
        /// verify that our geometry is valid before attempting to add it to a DirectShape or a DirectShapeType.
        /// </remarks>
        private static DirectShape SolidValidator
        {
            get
            {
                if (m_SolidValidator == null)
                {
                    m_SolidValidator = DirectShape.CreateElement(IFCImportFile.TheFile.Document,
                        new ElementId(BuiltInCategory.OST_GenericModel),
                        Importer.ImportAppGUID(),
                        "(SolidValidator)");
                }
                return m_SolidValidator;
            }
        }

        /// <summary>
        /// Create a copy of a curve loop with a given transformation applied.
        /// </summary>
        /// <param name="origLoop">The original curve loop.</param>
        /// <param name="trf">The transform.</param>
        /// <returns>The transformed loop.</returns>
        public static CurveLoop CreateTransformed(CurveLoop origLoop, Transform trf)
        {
            if (origLoop == null)
                return null;

            CurveLoop newLoop = new CurveLoop();
            foreach (Curve curve in origLoop)
            {
                newLoop.Append(curve.CreateTransformed(trf));
            }
            return newLoop;
        }

        private static double UnscaleSweptSolidCurveParam(Curve curve, double param)
        {
            if (curve.IsCyclic)
                return param * (Math.PI / 180);
            return param * (curve.GetEndParameter(1) - curve.GetEndParameter(0));
        }

        private static double ScaleCurveLengthForSweptSolid(Curve curve, double param)
        {
            if (curve.IsCyclic)
                return param * (180 / Math.PI);
            return 1.0;
        }

        /// <summary>
        /// Returns true if the line segment from pt1 to pt2 is less than the short curve tolerance.
        /// </summary>
        /// <param name="pt1">The first point of the line segment.</param>
        /// <param name="pt2">The final point of the line segment.</param>
        /// <returns>True if it is too short, false otherwise.</returns>
        public static bool LineSegmentIsTooShort(XYZ pt1, XYZ pt2)
        {
            double dist = pt1.DistanceTo(pt2);
            return (dist < IFCImportFile.TheFile.Document.Application.ShortCurveTolerance + MathUtil.Eps());
        }

        /// <summary>
        /// Creates an open or closed CurveLoop from a list of vertices.
        /// </summary>
        /// <param name="pointXYZs">The list of vertices.</param>
        /// <param name="points">The optional list of IFCAnyHandles that generated the vertices, used solely for error reporting.</param>
        /// <param name="id">The id of the IFCAnyHandle associated with the CurveLoop.</param>
        /// <param name="isClosedLoop">True if the vertices represent a closed loop, false if not.</param>
        /// <returns>The new curve loop.</returns>
        /// <remarks>If isClosedLoop is true, there will be pointsXyz.Count line segments.  Otherwise, there will be pointsXyz.Count-1.</remarks>
        public static CurveLoop CreatePolyCurveLoop(IList<XYZ> pointXYZs, IList<IFCAnyHandle> points, int id, bool isClosedLoop)
        {
            int numPoints = pointXYZs.Count;
            if (numPoints < 2)
                return null;

            IList<int> badIds = new List<int>();

            int numMinPoints = isClosedLoop ? 3 : 2;
            
            // Check distance between points; remove too-close points, and warn if result is non-collinear.
            // Always include first point.
            IList<XYZ> finalPoints = new List<XYZ>();
            finalPoints.Add(pointXYZs[0]);
            int numNewPoints = 1;
            for (int ii = 1; ii < numPoints; ii++)
            {
                if (IFCGeometryUtil.LineSegmentIsTooShort(finalPoints[numNewPoints - 1], pointXYZs[ii]))
                {
                    if (points != null)
                        badIds.Add(points[ii].StepId);
                    else
                        badIds.Add(ii+1);
                }
                else
                {
                    finalPoints.Add(pointXYZs[ii]);
                    numNewPoints++;
                }
            }

            // Check final segment; if too short, delete 2nd to last point.
            if (isClosedLoop)
            {
                if (IFCGeometryUtil.LineSegmentIsTooShort(finalPoints[numNewPoints - 1], pointXYZs[0]))
                {
                    finalPoints.RemoveAt(numNewPoints - 1);
                    numNewPoints--;
                }
            }

            // This can be a very common warning, so we will restrict to verbose logging.
            if (Importer.TheOptions.VerboseLogging)
            {
                if (badIds.Count > 0)
                {
                    int count = badIds.Count;
                    string msg = null;
                    if (count == 1)
                    {
                        msg = "Polyline had 1 point that was too close to one of its neighbors, removing point: #" + badIds[0] + ".";
                    }
                    else
                    {
                        msg = "Polyline had " + count + " points that were too close to one of their neighbors, removing points:";
                        foreach (int badId in badIds)
                            msg += " #" + badId;
                        msg += ".";
                    }
                    IFCImportFile.TheLog.LogWarning(id, msg, false);
                }
            }

            if (numNewPoints < numMinPoints)
            {
                if (Importer.TheOptions.VerboseLogging)
                {
                    string msg = "PolyCurve had " + numNewPoints + " point(s) after removing points that were too close, expected at least " + numMinPoints + ", ignoring.";
                    IFCImportFile.TheLog.LogWarning(id, msg, false);
                }
                return null;
            }

            CurveLoop curveLoop = new CurveLoop();
            for (int ii = 0; ii < numNewPoints - 1; ii++)
                curveLoop.Append(Line.CreateBound(finalPoints[ii], finalPoints[ii + 1]));
            if (isClosedLoop)
                curveLoop.Append(Line.CreateBound(finalPoints[numNewPoints - 1], finalPoints[0]));

            return curveLoop;
        }

        /// <summary>
        /// Returns a CurveLoop that has potentially been trimmed.
        /// </summary>
        /// <param name="origCurveLoop">The original curve loop.</param>
        /// <param name="startVal">The starting trim parameter.</param>
        /// <param name="origEndVal">The optional end trim parameter.  If not supplied, assume no end trim.</param>
        /// <returns>The original curve loop, if no trimming has been done, otherwise a trimmed copy.</returns>
        public static CurveLoop TrimCurveLoop(CurveLoop origCurveLoop, double startVal, double? origEndVal)
        {
            // Trivial case: no trimming.
            if (!origEndVal.HasValue && MathUtil.IsAlmostZero(startVal))
                return origCurveLoop;

            IList<double> curveLengths = new List<double>();
            IList<Curve> loopCurves = new List<Curve>();

            double totalParamLength = 0.0;
            foreach (Curve curve in origCurveLoop)
            {
                double currLength = ScaleCurveLengthForSweptSolid(curve, curve.GetEndParameter(1) - curve.GetEndParameter(0));
                loopCurves.Add(curve);
                curveLengths.Add(currLength);
                totalParamLength += currLength;
            }

            double endVal = origEndVal.HasValue ? origEndVal.Value : totalParamLength;

            // This check allows for some leniency in the setting of startVal and endVal; we assume that if the parameter range
            // is equal, that an offset value is OK.
            if (MathUtil.IsAlmostEqual(endVal - startVal, totalParamLength))
                return origCurveLoop;

            int numCurves = loopCurves.Count;
            double currentPosition = 0.0;
            int currCurve = 0;

            IList<Curve> newLoopCurves = new List<Curve>();

            if (startVal > MathUtil.Eps())
            {
                for (; currCurve < numCurves; currCurve++)
                {
                    if (currentPosition + curveLengths[currCurve] < startVal + MathUtil.Eps())
                    {
                        currentPosition += curveLengths[currCurve];
                        continue;
                    }

                    Curve newCurve = loopCurves[currCurve].Clone();
                    if (!MathUtil.IsAlmostEqual(currentPosition, startVal))
                        newCurve.MakeBound(UnscaleSweptSolidCurveParam(loopCurves[currCurve], startVal - currentPosition), newCurve.GetEndParameter(1));

                    newLoopCurves.Add(newCurve);
                    break;
                }
            }

            if (endVal < totalParamLength - MathUtil.Eps())
            {
                for (; currCurve < numCurves; currCurve++)
                {
                    if (currentPosition + curveLengths[currCurve] < endVal - MathUtil.Eps())
                    {
                        currentPosition += curveLengths[currCurve];
                        newLoopCurves.Add(loopCurves[currCurve]);
                        continue;
                    }

                    Curve newCurve = loopCurves[currCurve].Clone();
                    if (!MathUtil.IsAlmostEqual(currentPosition + curveLengths[currCurve], endVal))
                        newCurve.MakeBound(newCurve.GetEndParameter(0), UnscaleSweptSolidCurveParam(loopCurves[currCurve], endVal - currentPosition));

                    newLoopCurves.Add(newCurve);
                    break;
                }
            }

            CurveLoop trimmedCurveLoop = new CurveLoop();
            foreach (Curve curve in loopCurves)
                trimmedCurveLoop.Append(curve);
            return trimmedCurveLoop;
        }

        /// <summary>
        /// Execute a Boolean operation, and catch the exception.
        /// </summary>
        /// <param name="id">The id of the object demanding the Boolean operation.</param>
        /// <param name="secondId">The id of the object providing the second solid.</param>
        /// <param name="firstSolid">The first solid parameter to ExecuteBooleanOperation.</param>
        /// <param name="secondSolid">The second solid parameter to ExecuteBooleanOperation.</param>
        /// <param name="opType">The Boolean operation type.</param>
        /// <returns>The result of the Boolean operation, or the first solid if the operation fails.</returns>
        public static Solid ExecuteSafeBooleanOperation(int id, int secondId, Solid firstSolid, Solid secondSolid, BooleanOperationsType opType)
        {
            if (firstSolid == null || secondSolid == null)
            {
                if (firstSolid == null && secondSolid == null)
                    return null;

                if (opType == BooleanOperationsType.Union)
                {
                    if (firstSolid == null)
                        return secondSolid;

                    return firstSolid;
                }

                if (opType == BooleanOperationsType.Difference)
                {
                    if (firstSolid == null)
                        return null;

                    return firstSolid;
                }

                // for .Intersect
                return null;
            }

            Solid resultSolid = null;

            try
            {
                resultSolid = BooleanOperationsUtils.ExecuteBooleanOperation(firstSolid, secondSolid, opType);
            }
            catch (Exception ex)
            {
                IFCImportFile.TheLog.LogError(id, ex.Message, false);
                resultSolid = firstSolid;
            }

            if (SolidValidator.IsValidGeometry(resultSolid))
                return resultSolid;

            IFCImportFile.TheLog.LogError(id, opType.ToString() + " operation failed with void from #" + secondId.ToString(), false);
            return firstSolid;
        }

        /// <summary>
        /// Creates a list of meshes out a solid by triangulating the faces.
        /// </summary>
        /// <param name="solid">The original solid.</param>
        /// <returns>A list of meshes created from the triangulation of the solid's faces.</returns>
        public static IList<GeometryObject> CreateMeshesFromSolid(Solid solid)
        {
            IList<GeometryObject> triangulations = new List<GeometryObject>();

            foreach (Face face in solid.Faces)
            {
                Mesh faceMesh = face.Triangulate();
                if (faceMesh != null && faceMesh.NumTriangles > 0)
                    triangulations.Add(faceMesh);
            }

            return triangulations;
        }

        /// <summary>
        /// Checks if a Solid is valid for use in a generic DirectShape or DirecShapeType.
        /// </summary>
        /// <param name="solid"></param>
        /// <returns></returns>
        public static bool ValidateGeometry(Solid solid)
        {
            return SolidValidator.IsValidGeometry(solid);
        }

        /// <summary>
        /// Delete the element used for solid validation, if it exists.
        /// </summary>
        public static void DeleteSolidValidator()
        {
            if (m_SolidValidator != null)
            {
                IFCImportFile.TheFile.Document.Delete(SolidValidator.Id);
                m_SolidValidator = null;
            }
        }
    }
}
