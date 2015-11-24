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
               m_SolidValidator = IFCElementUtil.CreateElement(IFCImportFile.TheFile.Document,
                   new ElementId(BuiltInCategory.OST_GenericModel),
                   "(SolidValidator)",
                   null);
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
      /// Determines if (firstPt, midPt) and (midPt, finalPt) overlap at more than one point.
      /// </summary>
      /// <param name="firstPt">The start point of the first line segment.</param>
      /// <param name="midPt">The end point of the first line segment (and start point of the second).</param>
      /// <param name="finalPt">The end point of the second line segment.</param>
      /// <returns>True if the line segments overlap at more than one point.</returns>
      private static bool LineSegmentsOverlap(XYZ firstPt, XYZ midPt, XYZ finalPt)
      {
         XYZ firstDir = midPt - firstPt;
         XYZ secondDir = finalPt - midPt;
         return MathUtil.VectorsAreParallel2(firstDir, secondDir) == -1;
      }

      /// <summary>
      /// Creates an open or closed CurveLoop from a list of vertices.
      /// </summary>
      /// <param name="pointXYZs">The list of vertices.</param>
      /// <param name="points">The optional list of IFCAnyHandles that generated the vertices, used solely for error reporting.</param>
      /// <param name="id">The id of the IFCAnyHandle associated with the CurveLoop.</param>
      /// <param name="closeCurve">True if the loop needs a segment between the last point and the first point.</param>
      /// <returns>The new curve loop.</returns>
      /// <remarks>If closeCurve is true, there will be pointsXyz.Count line segments.  Otherwise, there will be pointsXyz.Count-1.</remarks>
      public static CurveLoop CreatePolyCurveLoop(IList<XYZ> pointXYZs, IList<IFCAnyHandle> points, int id, bool closeCurve)
      {
         int numPoints = pointXYZs.Count;
         if (numPoints < 2)
         {
            // TODO: log warning
            return null;
         }

         IList<int> badIds = new List<int>();

         // The input polycurve loop may or may not repeat the start/end point.
         // wasAlreadyClosed checks if the point was repeated.
         bool wasAlreadyClosed = pointXYZs[0].IsAlmostEqualTo(pointXYZs[numPoints - 1]);

         bool wasClosed = closeCurve ? true : wasAlreadyClosed;

         // We expect at least 3 points if the curve is closed, 2 otherwise.
         int numMinPoints = wasAlreadyClosed ? 4 : (closeCurve ? 3 : 2);
         if (numPoints < numMinPoints)
         {
            // TODO: log warning
            return null;
         }

         // Check distance between points; remove too-close points, and warn if result is non-collinear.
         // Always include first point.
         IList<XYZ> finalPoints = new List<XYZ>();
         finalPoints.Add(pointXYZs[0]);
         int numNewPoints = 1;

         int numPointsToCheck = closeCurve ? numPoints + 1 : numPoints;
         for (int ii = 1; ii < numPointsToCheck; ii++)
         {
            int nextIndex = (ii % numPoints);
            int nextNextIndex = (nextIndex == numPoints-1 && wasAlreadyClosed) ? 1 : ((ii + 1) % numPoints);
            
            // Only check if the last segment overlaps the first segment if we have a closed curve.
            bool doSegmentOverlapCheck = (ii < numPointsToCheck - 1) || wasClosed;
            if (LineSegmentIsTooShort(finalPoints[numNewPoints - 1], pointXYZs[nextIndex]) || 
               (doSegmentOverlapCheck && LineSegmentsOverlap(finalPoints[numNewPoints - 1], pointXYZs[nextIndex], pointXYZs[nextNextIndex])))
            {
               if (points != null)
                  badIds.Add(points[nextIndex].StepId);
               else
                  badIds.Add(nextIndex + 1);
            }
            else
            {
               finalPoints.Add(pointXYZs[nextIndex]);
               numNewPoints++;
            }
         }

         // Check final segment; if too short, delete 2nd to last point instead of the last.
         if (wasClosed)
         {
            if (numNewPoints < 4)
               return null;

            bool isClosed = finalPoints[numNewPoints - 1].IsAlmostEqualTo(finalPoints[0]);  // Do we have a closed loop now?
            if (wasClosed && !isClosed)   // If we had a closed loop, and now we don't, fix it up.
            {
               // Presumably, the second-to-last point had to be very close to the last point, or we wouldn't have removed the last point.
               // So instead of creating a too-short segment, we replace the last point of the new point list with the last point of the original point list.
               finalPoints[numNewPoints - 1] = pointXYZs[numPoints - 1];

               // Now we have to check that we didn't inadvertently make a "too-short" segment.
               for (int ii = numNewPoints - 1; ii > 0; ii--)
               {
                  if (IFCGeometryUtil.LineSegmentIsTooShort(finalPoints[ii], finalPoints[ii - 1]))
                  {
                     // TODO: log this removal.
                     finalPoints.RemoveAt(ii - 1); // Remove the intermediate point, not the last point.
                     numNewPoints--;
                  }
                  else
                     break;   // We are in the clear, unless we removed too many points - we've already checked the rest of the loop.
               }
            }

            if (numNewPoints < 4)
               return null;
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
               Importer.TheLog.LogWarning(id, msg, false);
            }
         }

         if (numNewPoints < numMinPoints)
         {
            if (Importer.TheOptions.VerboseLogging)
            {
               string msg = "PolyCurve had " + numNewPoints + " point(s) after removing points that were too close, expected at least " + numMinPoints + ", ignoring.";
               Importer.TheLog.LogWarning(id, msg, false);
            }
            return null;
         }

         CurveLoop curveLoop = new CurveLoop();
         for (int ii = 0; ii < numNewPoints - 1; ii++)
            curveLoop.Append(Line.CreateBound(finalPoints[ii], finalPoints[ii + 1]));

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
      /// <param name="suggestedShiftDirection">If the Boolean operation fails, a unit vector used to retry with a small shift.  Can be null.</param>
      /// <returns>The result of the Boolean operation, or the first solid if the operation fails.</returns>
      public static Solid ExecuteSafeBooleanOperation(int id, int secondId, Solid firstSolid, Solid secondSolid, BooleanOperationsType opType, XYZ suggestedShiftDirection)
      {
         const double footToMillimeter = 1.0 / 304.8;

         // Perform default operations if one of the arguments is null.
         if (firstSolid == null || secondSolid == null)
         {
            if (firstSolid == null && secondSolid == null)
               return null;

            switch (opType)
            {
               case BooleanOperationsType.Union:
                  {
               if (firstSolid == null)
                  return secondSolid;

               return firstSolid;
            }
               case BooleanOperationsType.Difference:
            {
               if (firstSolid == null)
                  return null;

               return firstSolid;
            }
               default:
            // for .Intersect
            return null;
         }
         }

         Solid resultSolid = null;
         bool failedAllAttempts = true;

         // We will attempt to do the Boolean operation 3 times:
         // 1st pass: With the passed-in arguments.
         // 2nd pass: With a 1mm shift in a direction in suggestedShiftDirection, or +Z if suggestedShiftDirection is null
         // 3rd pass: With a 1mm shift in a direction in -suggestedShiftDirection, or -Z if suggestedShiftDirection is null
         for (int ii = 0; ii < 3; ii++)
         {
         try
         {
               resultSolid = null;

               Solid secondOperand = secondSolid;
               if (ii > 0)
               {
                  // 1 mm shift.
                  XYZ shiftDirection = (suggestedShiftDirection == null) ? new XYZ(0, 0, 1) : suggestedShiftDirection;
                  Transform secondSolidShift = Transform.CreateTranslation(shiftDirection * ((ii == 1) ? footToMillimeter : -footToMillimeter));
                  secondOperand = SolidUtils.CreateTransformed(secondOperand, secondSolidShift);
         }

               resultSolid = BooleanOperationsUtils.ExecuteBooleanOperation(firstSolid, secondOperand, opType);
               failedAllAttempts = false;
            }
         catch (Exception ex)
         {
               if (ii < 2)
                  continue;
               
            Importer.TheLog.LogError(id, ex.Message, false);
            resultSolid = firstSolid;
         }

         if (SolidValidator.IsValidGeometry(resultSolid))
            {
               // If we got here not on out first attempt, generate a warning, unless we got here because we gave up on our 3rd attempt.
               if (ii > 0 && !failedAllAttempts)
                  Importer.TheLog.LogWarning(id, "The second argument in the Boolean " + 
                     opType.ToString() + 
                     " operation was shifted by 1mm to allow the operation to succeed.  This may result in a very small difference in appearance.", false);
            return resultSolid;
            }
         }

         Importer.TheLog.LogError(id, opType.ToString() + " operation failed with void from #" + secondId.ToString(), false);
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
      /// Return a solid corresponding to the volume represented by boundingBoxXYZ. 
      /// </summary>
      /// <param name="lcs">The local coordinate system of the bounding box; if null, assume the Identity transform.</param>
      /// <param name="boundingBoxXYZ">The bounding box.</param>
      /// <param name="solidOptions">The options for creating the solid.  Allow null to mean default.</param>
      /// <returns>A solid of the same size and orientation as boundingBoxXYZ, or null if boundingBoxXYZ is invalid or null.</returns>
      /// <remarks>We don't do any checking on the input transform, which could have non-uniform scaling and/or mirroring.
      /// This could potentially lead to unexpected results, which we can examine if and when such cases arise.</remarks>
      public static Solid CreateSolidFromBoundingBox(Transform lcs, BoundingBoxXYZ boundingBoxXYZ, SolidOptions solidOptions)
      {
         // Check that the bounding box is valid.
         if (boundingBoxXYZ == null || !boundingBoxXYZ.Enabled)
            return null;

         try
         {
            // Create a transform based on the incoming local coordinate system and the bounding box coordinate system.
            Transform bboxTransform = (lcs == null) ? boundingBoxXYZ.Transform : lcs.Multiply(boundingBoxXYZ.Transform);

            XYZ[] profilePts = new XYZ[4];
            profilePts[0] = bboxTransform.OfPoint(boundingBoxXYZ.Min);
            profilePts[1] = bboxTransform.OfPoint(new XYZ(boundingBoxXYZ.Max.X, boundingBoxXYZ.Min.Y, boundingBoxXYZ.Min.Z));
            profilePts[2] = bboxTransform.OfPoint(new XYZ(boundingBoxXYZ.Max.X, boundingBoxXYZ.Max.Y, boundingBoxXYZ.Min.Z));
            profilePts[3] = bboxTransform.OfPoint(new XYZ(boundingBoxXYZ.Min.X, boundingBoxXYZ.Max.Y, boundingBoxXYZ.Min.Z));

            XYZ upperRightXYZ = bboxTransform.OfPoint(boundingBoxXYZ.Max);

            // If we assumed that the transforms had no scaling, 
            // then we could simply take boundingBoxXYZ.Max.Z - boundingBoxXYZ.Min.Z.
            // This code removes that assumption.
            XYZ origExtrusionVector = new XYZ(boundingBoxXYZ.Min.X, boundingBoxXYZ.Min.Y, boundingBoxXYZ.Max.Z) - boundingBoxXYZ.Min;
            XYZ extrusionVector = bboxTransform.OfVector(origExtrusionVector);

            double extrusionDistance = extrusionVector.GetLength();
            XYZ extrusionDirection = extrusionVector.Normalize();

            CurveLoop baseLoop = new CurveLoop();

            for (int ii = 0; ii < 4; ii++)
            {
               baseLoop.Append(Line.CreateBound(profilePts[ii], profilePts[(ii + 1) % 4]));
            }

            IList<CurveLoop> baseLoops = new List<CurveLoop>();
            baseLoops.Add(baseLoop);

            if (solidOptions == null)
               return GeometryCreationUtilities.CreateExtrusionGeometry(baseLoops, extrusionDirection, extrusionDistance);
            else
               return GeometryCreationUtilities.CreateExtrusionGeometry(baseLoops, extrusionDirection, extrusionDistance, solidOptions);
         }
         catch
         {
            return null;
         }
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

      /// <summary>
      /// Check for any occurence where distance of two vertices are too narrow (within the tolerance)
      /// </summary>
      /// <param name="entityId">The integer number representing the current IFC entity Id</param>
      /// <param name="shapeEditScope">the shapeEditScope</param>
      /// <param name="inputVerticesList">Input list of the vertices</param>
      /// <param name="outputVerticesList">Output List of the valid vertices, i.e. not vertices that are too close to each other</param>
      /// <returns></returns>
      public static void CheckAnyDistanceVerticesWithinTolerance(int entityId, IFCImportShapeEditScope shapeEditScope, IList<XYZ> inputVerticesList, out IList<XYZ> outputVerticesList)
      {
         // Check triangle that is too narrow (2 vertices are within the tolerance
         double shortSegmentTolerance = shapeEditScope.TryToCreateSolid() ?
                                         shapeEditScope.Document.Application.ShortCurveTolerance :
                                         shapeEditScope.Document.Application.VertexTolerance;

         int lastVertex = 0;
         IList<XYZ> vertList = new List<XYZ>();
         outputVerticesList = vertList;
         for (int ii = 1; ii <= inputVerticesList.Count; ii++)
         {
            int currIdx = (ii % inputVerticesList.Count);

            double dist = inputVerticesList[lastVertex].DistanceTo(inputVerticesList[currIdx]);
            if (dist >= shortSegmentTolerance)
            {
               vertList.Add(inputVerticesList[lastVertex]);
               lastVertex = currIdx;
            }
            else
            {
               string distAsString = IFCUnitUtil.FormatLengthAsString(dist);
               string shortDistAsString = IFCUnitUtil.FormatLengthAsString(shortSegmentTolerance);
               string warningString = "Distance between vertices " + lastVertex + " and " + currIdx +
                                       " is " + distAsString + ", which is less than the minimum " + (shapeEditScope.TryToCreateSolid() ? "Solid" : "Mesh") +
                                       " distance of " + shortDistAsString + ", removing second point.";

               Importer.TheLog.LogComment(entityId, warningString, false);
            }
         }
      }

      /// <summary>
   }
}
