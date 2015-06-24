//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
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
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Utility
{
    /// <summary>
    /// This geometry utility allows you to attempt to “fit” a given piece of geometry into
    /// the shape of a simple swept solid.
    /// </summary>
    /// <remarks>
    /// It now only supports an open sweep with no opening or clippings and with one path curve of a line or arc.
    /// </remarks>
    class SimpleSweptSolidAnalyzer
    {
        PlanarFace m_ProfileFace;

        Curve m_PathCurve;

        XYZ m_ReferencePlaneNormal;

        List<Face> m_UnalignedFaces;

        /// <summary>
        /// The face that represents the profile of the swept solid.
        /// </summary>
        public PlanarFace ProfileFace
        {
            get { return m_ProfileFace; }
        }

        /// <summary>
        /// The edge that represents the path of the swept solid.
        /// </summary>
        public Curve PathCurve
        {
            get { return m_PathCurve; }
        }
        
        /// <summary>
        /// The normal of the reference plane that the path lies on.
        /// </summary>
        public XYZ ReferencePlaneNormal
        {
            get { return m_ReferencePlaneNormal; }
        }

        /// <summary>
        /// The unaligned faces, maybe openings or recesses.
        /// </summary>
        public List<Face> UnalignedFaces
        {
            get { return m_UnalignedFaces; }
        }

        /// <summary>
        /// Creates a SimpleSweptSolidAnalyzer and computes the swept solid.
        /// </summary>
        /// <param name="solid">The solid geometry.</param>
        /// <param name="normal">The normal of the reference plane that a path might lie on.  If it is null, try to guess based on the geometry.</param>
        /// <returns>The analyzer.</returns>
        public static SimpleSweptSolidAnalyzer Create(Solid solid, XYZ normal)
        {
            if (solid == null)
                throw new ArgumentNullException();

            ICollection<Face> faces = new List<Face>();
            foreach (Face face in solid.Faces)
            {
                faces.Add(face);
            }
            return Create(faces, normal);
        }

        /// <summary>
        /// Creates a SimpleSweptSolidAnalyzer and computes the swept solid.
        /// </summary>
        /// <param name="faces">The faces of a solid.</param>
        /// <param name="normal">The normal of the reference plane that a path might lie on.  If it is null, try to guess based on the geometry.</param>
        /// <returns>The analyzer.</returns>
        /// <remarks>This is a simple analyzer, and is not intended to be general - it works in some simple, real-world cases.</remarks>
        public static SimpleSweptSolidAnalyzer Create(ICollection<Face> faces, XYZ normal)
        {
            if (faces == null || faces.Count < 3)
                throw new ArgumentException("Invalid faces.", "faces");
            
            if (normal == null)
            {
                foreach (Face face in faces)
                {
                    if (face is RevolvedFace)
                    {
                        XYZ faceNormal = (face as RevolvedFace).Axis;
                        if (normal == null)
                            normal = faceNormal;
                        else if (!MathUtil.VectorsAreParallel(normal, faceNormal))
                            throw new InvalidOperationException("Couldn't calculate swept solid normal.");
                    }
                }
            }

            // find potential profile faces, their normal vectors must be orthogonal to the input normal
            List<PlanarFace> potentialSweepEndFaces = new List<PlanarFace>();
            foreach (Face face in faces)
            {
                PlanarFace planarFace =  face as PlanarFace;
                if (planarFace == null)
                    continue;
                if (MathUtil.VectorsAreOrthogonal(normal, planarFace.FaceNormal))
                    potentialSweepEndFaces.Add(planarFace);
            }

            if (potentialSweepEndFaces.Count < 2)
                throw new InvalidOperationException("Can't find enough potential end faces.");

            int i = 0;
            PlanarFace candidateProfileFace = null; // the potential profile face for the swept solid
            PlanarFace candidateProfileFace2 = null;
            Edge candidatePathEdge = null;
            bool foundCandidateFace = false;
            do 
            {
                candidateProfileFace = potentialSweepEndFaces[i++];

                // find edges on the candidate profile face and the side faces with the edges
                // later find edges on the other candidate profile face with same side faces
                // they will be used to compare if the edges are congruent
                // to make sure the two faces are the potential profile faces

                Dictionary<Face, Edge> sideFacesWithCandidateEdges = new Dictionary<Face, Edge>();
                EdgeArrayArray candidateFaceEdgeLoops = candidateProfileFace.EdgeLoops;
                foreach (EdgeArray edgeArray in candidateFaceEdgeLoops)
                {
                    foreach (Edge candidateEdge in edgeArray)
                    {
                        Face sideFace = candidateEdge.GetFace(0);
                        if (sideFace == candidateProfileFace)
                            sideFace = candidateEdge.GetFace(1);

                        if (sideFacesWithCandidateEdges.ContainsKey(sideFace)) // should not happen
                            throw new InvalidOperationException("Failed");

                        sideFacesWithCandidateEdges[sideFace] = candidateEdge;
                    }
                }

                double candidateProfileFaceArea = candidateProfileFace.Area;
                foreach (PlanarFace theOtherCandidateFace in potentialSweepEndFaces)
                {
                    if (theOtherCandidateFace.Equals(candidateProfileFace))
                        continue;

                    if (!MathUtil.IsAlmostEqual(candidateProfileFaceArea, theOtherCandidateFace.Area))
                        continue;

                    EdgeArrayArray theOtherCandidateFaceEdgeLoops = theOtherCandidateFace.EdgeLoops;

                    bool failToFindTheOtherCandidateFace = false;
                    Dictionary<Face, Edge> sideFacesWithTheOtherCandidateEdges = new Dictionary<Face, Edge>();
                    foreach (EdgeArray edgeArray in theOtherCandidateFaceEdgeLoops)
                    {
                        foreach (Edge theOtherCandidateEdge in edgeArray)
                        {
                            Face sideFace = theOtherCandidateEdge.GetFace(0);
                            if (sideFace == theOtherCandidateFace)
                                sideFace = theOtherCandidateEdge.GetFace(1);

                            if (!sideFacesWithCandidateEdges.ContainsKey(sideFace)) // should already have
                            {
                                failToFindTheOtherCandidateFace = true;
                                break;
                            }

                            if (sideFacesWithTheOtherCandidateEdges.ContainsKey(sideFace)) // should not happen
                                throw new InvalidOperationException("Failed");

                            sideFacesWithTheOtherCandidateEdges[sideFace] = theOtherCandidateEdge;
                        }
                    }

                    if (failToFindTheOtherCandidateFace)
                        continue;

                    if (sideFacesWithCandidateEdges.Count != sideFacesWithTheOtherCandidateEdges.Count)
                        continue;

                    // side faces with candidate profile face edges
                    Dictionary<Face, List<Edge>> sideFacesWithEdgesDic = new Dictionary<Face, List<Edge>>();
                    foreach (Face sideFace in sideFacesWithCandidateEdges.Keys)
                    {
                        sideFacesWithEdgesDic[sideFace] = new List<Edge>();
                        sideFacesWithEdgesDic[sideFace].Add(sideFacesWithCandidateEdges[sideFace]);
                        sideFacesWithEdgesDic[sideFace].Add(sideFacesWithTheOtherCandidateEdges[sideFace]);
                    }

                    if (!AreFacesSimpleCongruent(sideFacesWithEdgesDic))
                        continue;

                    // find candidate path edges
                    Dictionary<Face, List<Edge>> candidatePathEdgesWithFace = new Dictionary<Face, List<Edge>>();
                    foreach (KeyValuePair<Face, List<Edge>> sideFaceAndEdges in sideFacesWithEdgesDic)
                    {
                        List<Edge> pathEdges = FindCandidatePathEdge(sideFaceAndEdges.Key, sideFaceAndEdges.Value[0], sideFaceAndEdges.Value[1]);
                        // maybe we found two faces of an opening or a recess on the swept solid, skip in this case
                        if (pathEdges.Count < 2)
                        {
                            failToFindTheOtherCandidateFace = true;
                            break;
                        }
                        candidatePathEdgesWithFace[sideFaceAndEdges.Key] = pathEdges;
                    }

                    if (failToFindTheOtherCandidateFace)
                        continue;

                    // check if these edges are congruent
                    if (!AreEdgesSimpleCongruent(candidatePathEdgesWithFace))
                        continue;

                    candidatePathEdge = candidatePathEdgesWithFace.Values.ElementAt(0).ElementAt(0);

                    foundCandidateFace = true;
                    candidateProfileFace2 = theOtherCandidateFace;
                    break;
                }

                if (foundCandidateFace)
                    break;
            } while (i < potentialSweepEndFaces.Count);

            SimpleSweptSolidAnalyzer simpleSweptSolidAnalyzer = null;

            if (foundCandidateFace)
            {
                simpleSweptSolidAnalyzer = new SimpleSweptSolidAnalyzer();
                Curve pathCurve = candidatePathEdge.AsCurve();
                XYZ endPoint0 = pathCurve.GetEndPoint(0);

                bool foundProfileFace = false;
                List<PlanarFace> profileFaces = new List<PlanarFace>();
                profileFaces.Add(candidateProfileFace);
                profileFaces.Add(candidateProfileFace2);

                foreach (PlanarFace profileFace in profileFaces)
                {
                    IntersectionResultArray intersectionResults;
                    profileFace.Intersect(pathCurve, out intersectionResults);
                    if (intersectionResults != null)
                    {
                        foreach (IntersectionResult intersectoinResult in intersectionResults)
                        {
                            XYZ intersectPoint = intersectoinResult.XYZPoint;
                            if (intersectPoint.IsAlmostEqualTo(endPoint0))
                            {
                                simpleSweptSolidAnalyzer.m_ProfileFace = profileFace;
                                foundProfileFace = true;
                                break;
                            }
                        }
                    }

                    if (foundProfileFace)
                        break;
                }

                if (!foundProfileFace)
                    throw new InvalidOperationException("Failed to find profile face.");

                // TODO: consider one profile face has an opening extrusion inside while the other does not
                List<Face> alignedFaces = FindAlignedFaces(profileFaces.ToList<Face>());

                List<Face> unalignedFaces = new List<Face>();
                foreach (Face face in faces)
                {
                    if (profileFaces.Contains(face) || alignedFaces.Contains(face))
                        continue;

                    unalignedFaces.Add(face);
                }

                simpleSweptSolidAnalyzer.m_UnalignedFaces = unalignedFaces;

                simpleSweptSolidAnalyzer.m_PathCurve = pathCurve;
                simpleSweptSolidAnalyzer.m_ReferencePlaneNormal = normal;
            }

            return simpleSweptSolidAnalyzer;
        }

        /// <summary>
        /// Finds faces aligned with the swept solid.
        /// </summary>
        /// <param name="profileFaces">The profile faces.</param>
        /// <returns>The aligned faces.</returns>
        private static List<Face> FindAlignedFaces(ICollection<Face> profileFaces)
        {
            List<Face> alignedFaces = new List<Face>();
            foreach (Face face in profileFaces)
            {
                EdgeArrayArray edgeLoops = face.EdgeLoops;
                int edgeLoopCount = edgeLoops.Size;

                for (int i = 0; i < edgeLoopCount; i++)
                {
                    foreach (Edge edge in edgeLoops.get_Item(i))
                    {
                        Face alignedFace = edge.GetFace(0);
                        if (alignedFace == face)
                            alignedFace = edge.GetFace(1);
                        alignedFaces.Add(alignedFace);
                    }
                }
            }
            return alignedFaces;
        }

        /// <summary>
        /// Checks if two faces are congruent.
        /// </summary>
        /// <param name="faceEdgeDic">The collection contains edges on the candidate faces combined with same side faces.</param>
        /// <returns>True if congruent, false otherwise.</returns>
        private static bool AreFacesSimpleCongruent(Dictionary<Face, List<Edge>> faceEdgeDic)
        {
            if (faceEdgeDic == null || faceEdgeDic.Count == 0)
                return false;

            foreach (Face face in faceEdgeDic.Keys)
            {
                List<Edge> edges = faceEdgeDic[face];
                if (edges.Count != 2)
                    return false;

                Curve curve0 = edges[0].AsCurve();
                Curve curve1 = edges[1].AsCurve();

                if (curve0 is Line)
                {
                    if (!(curve1 is Line))
                        return false;
                    if (!MathUtil.IsAlmostEqual(curve0.Length, curve1.Length))
                        return false;
                    continue;
                }
                else if (curve0 is Arc)
                {
                    if (!(curve1 is Arc))
                        return false;
                    if (!MathUtil.IsAlmostEqual(curve0.Length, curve1.Length))
                        return false;
                    if (!MathUtil.IsAlmostEqual((curve0 as Arc).Radius, (curve1 as Arc).Radius))
                        return false;
                    continue;
                }

                // not support other types of curves for now
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if edges are congruent.
        /// </summary>
        /// <param name="faceEdgeDic">The collection contains potential path edges on the side faces.</param>
        /// <returns>True if congruent, false otherwise.</returns>
        private static bool AreEdgesSimpleCongruent(Dictionary<Face, List<Edge>> faceEdgeDic)
        {
            if (faceEdgeDic == null || faceEdgeDic.Count == 0)
                return false;

            foreach (Face face in faceEdgeDic.Keys)
            {
                List<Edge> edges = faceEdgeDic[face];
                if (edges.Count != 2)
                    return false;

                Curve curve0 = edges[0].AsCurveFollowingFace(face);
                Curve curve1 = edges[1].AsCurveFollowingFace(face);

                if (curve0 is Line)
                {
                    if (!(curve1 is Line))
                        return false;
                    XYZ moveDir = curve1.GetEndPoint(1) - curve0.GetEndPoint(0);
                    Curve movedCurve = GeometryUtil.MoveCurve(curve0, moveDir);
                    if (movedCurve.Intersect(curve1) != SetComparisonResult.Equal)
                        return false;
                    continue;
                }
                else if (curve0 is Arc)
                {
                    if (!(curve1 is Arc))
                        return false;
                    Arc arc0 = curve0 as Arc;
                    XYZ offsetVec = curve1.GetEndPoint(1) - curve0.GetEndPoint(0);
                    Arc offsetArc = OffsetArc(arc0, curve0.GetEndPoint(0), offsetVec);
                    if (offsetArc.Intersect(curve1) != SetComparisonResult.Equal)
                        return false;
                    continue;
                }

                // not support other types of curves for now
                return false;
            }

            return true;
        }

        /// <summary>
        /// Offsets an arc along the offset direction from the point on the arc.
        /// </summary>
        /// <param name="arc">The arc.</param>
        /// <param name="offsetPntOnArc">The point on the arc.</param>
        /// <param name="offset">The offset vector.</param>
        /// <returns>The offset Arc.</returns>
        private static Arc OffsetArc(Arc arc, XYZ offsetPntOnArc, XYZ offset)
        {
            if (arc == null || offset == null)
                throw new ArgumentNullException();

            if (offset.IsZeroLength())
                return arc;

            XYZ axis = arc.Normal.Normalize();

            XYZ offsetAlongAxis = axis.Multiply(offset.DotProduct(axis));
            XYZ offsetOrthAxis = offset - offsetAlongAxis;
            XYZ offsetPntToCenter = (arc.Center - offsetPntOnArc).Normalize();

            double signedOffsetLengthTowardCenter = offsetOrthAxis.DotProduct(offsetPntToCenter);
            double newRadius = arc.Radius - signedOffsetLengthTowardCenter; // signedOffsetLengthTowardCenter > 0, minus, < 0, add

            Arc offsetArc = Arc.Create(arc.Center, newRadius, arc.GetEndParameter(0), arc.GetEndParameter(1), arc.XDirection, arc.YDirection);

            offsetArc = GeometryUtil.MoveCurve(offsetArc, offsetAlongAxis) as Arc;

            return offsetArc;
        }

        /// <summary>
        /// Finds candidate path edges from a side face with two edges on the profile face.
        /// </summary>
        /// <param name="face">The side face.</param>
        /// <param name="edge0">The edge on the profile face and the side face.</param>
        /// <param name="edge1">The edge on the profile face and the side face.</param>
        /// <returns>The potential path edges. Should at least have two path on one face</returns>
        private static List<Edge> FindCandidatePathEdge(Face face, Edge edge0, Edge edge1)
        {
           double vertexEps = ExporterCacheManager.Document.Application.VertexTolerance;

            Curve curve0 = edge0.AsCurveFollowingFace(face);
            Curve curve1 = edge1.AsCurveFollowingFace(face);

            XYZ[,] endPoints = new XYZ[2, 2] { { curve0.GetEndPoint(0), curve1.GetEndPoint(1) }, { curve0.GetEndPoint(1), curve1.GetEndPoint(0) } };

            List<Edge> candidatePathEdges = new List<Edge>();
            EdgeArray outerEdgeLoop = face.EdgeLoops.get_Item(0);
            foreach (Edge edge in outerEdgeLoop)
            {
                XYZ endPoint0 = edge.Evaluate(0);
                XYZ endPoint1 = edge.Evaluate(1);

                for (int i = 0; i < 2; i++)
                {
                    bool found = false;
                    for (int j = 0; j < 2; j++)
                    {
                        if (endPoint0.IsAlmostEqualTo(endPoints[i, j], vertexEps))
                        {
                            int k = 1 - j;
                            if (endPoint1.IsAlmostEqualTo(endPoints[i, k], vertexEps))
                            {
                                candidatePathEdges.Add(edge);
                                found = true;
                                break;
                            }
                        }
                    }
                    if (found)
                        break;
                }
            }

            return candidatePathEdges;
        }
    }
}
