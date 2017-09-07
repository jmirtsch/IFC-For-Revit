//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2012-2016  Autodesk, Inc.
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using System.Diagnostics;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// This is a special class to be used to merge triangles into polygonal faces (only work for planar faces of course)
   /// </summary>
   public class TriangleMergeUtil
   {
      static TriangulatedShellComponent _geom;
      HashSet<int> _mergedFaceList = new HashSet<int>();

      IDictionary<int, IndexFace> facesColl = new Dictionary<int, IndexFace>();

      // These two must be hand in hand
      static double _tol = 1e-6;
      static int _tolNoDecPrecision = 6;
      // ------

      IDictionary<int, HashSet<int>> sortedFVert = new Dictionary<int, HashSet<int>>();

      /// <summary>
      /// Constructor for the class, accepting the TriangulatedShellComponent from the result of body tessellation
      /// </summary>
      /// <param name="triangulatedBody"></param>
      public TriangleMergeUtil (TriangulatedShellComponent triangulatedBody)
      {
         _geom = triangulatedBody;
      }

      /// <summary>
      /// Custom IEqualityComparer for a vector with tolerance for use with Dictionary
      /// </summary>
      public class vectorCompare : IEqualityComparer<XYZ>
      {
         public bool Equals(XYZ o1, XYZ o2)
         {
            bool xdiff = Math.Abs((o1 as XYZ).X - (o2 as XYZ).X) < _tol;
            bool ydiff = Math.Abs((o1 as XYZ).Y - (o2 as XYZ).Y) < _tol;
            bool zdiff = Math.Abs((o1 as XYZ).Z - (o2 as XYZ).Z) < _tol;
            if (xdiff && ydiff && zdiff)
               return true;
            else
               return false;
         }

         public int GetHashCode(XYZ obj)
         {
            // Uses the precision set in MathUtils to round the values so that the HashCode will be consistent with the Equals method
            double X = Math.Round(obj.X, _tolNoDecPrecision);
            double Y = Math.Round(obj.Y, _tolNoDecPrecision);
            double Z = Math.Round(obj.Z, _tolNoDecPrecision);
            
            return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
         }
      }

      /// <summary>
      /// Custom IEqualityComparer for a lighweight line segment used in this merge utility
      /// </summary>
      public class SegmentCompare : IEqualityComparer<IndexSegment>
      {
         public bool Equals(IndexSegment o1, IndexSegment o2)
         {
            if (o1.coincide(o2))
               return true;
            else
               return false;
         }

         public int GetHashCode(IndexSegment obj)
         {
            int hash = 23;
            hash = hash * 31 + obj.startPindex;
            hash = hash * 31 + obj.endPIndex;
            return hash;
         }
      }

      /// <summary>
      /// Private class that defines a line segment defined by index to the vertices
      /// </summary>
      public class IndexSegment
      {
         /// <summary>
         /// Vertex index of the starting point
         /// </summary>
         public int startPindex { get; set; }
         /// <summary>
         /// Vertex index of the end point
         /// </summary>
         public int endPIndex { get; set; }
         /// <summary>
         /// Constructor for generating the class
         /// </summary>
         /// <param name="startIndex">Vertex index of the starting point</param>
         /// <param name="endIndex">Vertex index of the end point</param>
         public IndexSegment(int startIndex, int endIndex)
         {
            startPindex = startIndex;
            endPIndex = endIndex;
         }

         /// <summary>
         /// Extent size (length) of the line segment
         /// </summary>
         public double extent
         {
            get
            {
               return _geom.GetVertex(startPindex).DistanceTo(_geom.GetVertex(endPIndex));
            }
         }

         /// <summary>
         /// Test whether a line segment coincides with this one (must be exactly the same start - end, or end - start) 
         /// </summary>
         /// <param name="inputSegment"></param>
         /// <returns></returns>
         public bool coincide(IndexSegment inputSegment)
         {
            return ((startPindex == inputSegment.startPindex && endPIndex == inputSegment.endPIndex) 
               || (endPIndex == inputSegment.startPindex && startPindex == inputSegment.endPIndex));
         }

         /// <summary>
         /// Reverse the order of the line segment (end to start)
         /// </summary>
         /// <returns></returns>
         public IndexSegment reverse()
         {
            return new IndexSegment(endPIndex, startPindex);
         }
      }

      /// <summary>
      /// Private class to creates a face based on the vertex indices
      /// </summary>
      class IndexFace
      {
         /// <summary>
         /// Vertex indices for the outer boundary of the face
         /// </summary>
         public IList<int> indexOuterBoundary { get; set; }
         /// <summary>
         /// List of vertex indices for the inner boundaries
         /// </summary>
         public IList<IList<int>> indexedInnerBoundaries { get; set; }
         /// <summary>
         /// Collection of all the vertices (outer and inner)
         /// </summary>
         public IList<IndexSegment> outerAndInnerBoundaries { get; set; }
         IDictionary<IndexSegment, int> boundaryLinesDict;
         /// <summary>
         /// The normal vector of the face
         /// </summary>
         public XYZ normal { get; set; }

         /// <summary>
         /// Constructor taking a list of vertex indices (face without hole) 
         /// </summary>
         /// <param name="vertxIndices">the list of vertex indices (face without hole)</param>
         public IndexFace(IList<int> vertxIndices)
         {
            indexOuterBoundary = vertxIndices;
            outerAndInnerBoundaries = setupEdges(indexOuterBoundary);

            IList<XYZ> vertices = new List<XYZ>();
            for (int i = 0; i < indexOuterBoundary.Count; ++i)
            {
               vertices.Add(_geom.GetVertex(indexOuterBoundary[i]));
            }
            normal = normalByNewellMethod(vertices);
        }

         /// <summary>
         /// Constructor taking in List of List of vertices. The first list will be the outer boundary and the rest are the inner boundaries
         /// </summary>
         /// <param name="vertxIndices">List of List of vertices. The first list will be the outer boundary and the rest are the inner boundaries</param>
         public IndexFace(IList<IList<int>> vertxIndices)
         {
            if (vertxIndices == null)
               return;
            if (vertxIndices.Count == 0)
               return;

            indexOuterBoundary = vertxIndices[0];
            vertxIndices.RemoveAt(0);
            outerAndInnerBoundaries = setupEdges(indexOuterBoundary);

            if (vertxIndices.Count != 0)
            {
               indexedInnerBoundaries = vertxIndices;

               List<IndexSegment> innerBIndexList = new List<IndexSegment>();
               foreach (IList<int> innerBound in indexedInnerBoundaries)
               {
                  innerBIndexList.AddRange(setupEdges(innerBound));
               }

               foreach (IndexSegment seg in innerBIndexList)
                  outerAndInnerBoundaries.Add(seg);
            }

            // Create normal from only the outer boundary
            IList<XYZ> vertices = new List<XYZ>();
            for (int i = 0; i < indexOuterBoundary.Count; ++i)
            {
               vertices.Add(_geom.GetVertex(indexOuterBoundary[i]));
            }
            normal = normalByNewellMethod(vertices);
         }

         /// <summary>
         /// Reverse the order of the vertices. Only operates on the outer boundary 
         /// </summary>
         public void Reverse()
         {
            // This is used in the process of combining triangles and therefore will work only with the face without holes
            indexOuterBoundary.Reverse();
            outerAndInnerBoundaries.Clear();
            boundaryLinesDict.Clear();
            outerAndInnerBoundaries = setupEdges(indexOuterBoundary);
         }

         /// <summary>
         /// Find matched line segment in the face boundaries
         /// </summary>
         /// <param name="inpSeg">Input line segment as vertex indices</param>
         /// <returns></returns>
         public int findMatechedIndexSegment(IndexSegment inpSeg)
         {
            int idx;
            if (boundaryLinesDict.TryGetValue(inpSeg, out idx))
               return idx;
            else
               return -1;
         }

         IList<IndexSegment> setupEdges(IList<int> vertxIndices)
         {
            IList<IndexSegment> indexList = new List<IndexSegment>();
            int boundLinesDictOffset = 0;

            if (boundaryLinesDict == null)
            {
                IEqualityComparer<IndexSegment> segCompare = new SegmentCompare();
                boundaryLinesDict = new Dictionary<IndexSegment, int>(segCompare);
            }
            else
                boundLinesDictOffset = boundaryLinesDict.Count();

            for (int i = 0; i < vertxIndices.Count; ++i)
            {
               IndexSegment segm; 
               if (i == vertxIndices.Count - 1)
               {
                  segm = new IndexSegment(vertxIndices[i], vertxIndices[0]);

               }
               else
               {
                  segm = new IndexSegment(vertxIndices[i], vertxIndices[i + 1]);
               }
               indexList.Add(segm);
               boundaryLinesDict.Add(segm, i + boundLinesDictOffset);       // boundaryLinesDict is a dictionary for the combined outer and inner boundaries, the values should be sequential
            }

            return indexList;
         }
      }

      /// <summary>
      /// Number of faces in this merged faces
      /// </summary>
      public int NoOfFaces
      {
         get { return _mergedFaceList.Count; }
      }

      /// <summary>
      /// Get the specific outer boundary (index of vertices)
      /// </summary>
      /// <param name="fIdx">the index face</param>
      /// <returns>return index of vertices</returns>
      public IList<int> IndexOuterboundOfFaceAt(int fIdx)
      {
         return facesColl[_mergedFaceList.ElementAt(fIdx)].indexOuterBoundary;
      }

      /// <summary>
      /// Number of holes in a specific face
      /// </summary>
      /// <param name="fIdx">index of the face</param>
      /// <returns>number of the holes in the face</returns>
      public int NoOfHolesInFace(int fIdx)
      {
         if (facesColl[_mergedFaceList.ElementAt(fIdx)].indexedInnerBoundaries == null)
            return 0;
         if (facesColl[_mergedFaceList.ElementAt(fIdx)].indexedInnerBoundaries.Count == 0)
            return 0;
         return facesColl[_mergedFaceList.ElementAt(fIdx)].indexedInnerBoundaries.Count;
      }

      /// <summary>
      /// Get the inner boundaries of the merged faces for a specific face
      /// </summary>
      /// <param name="fIdx">the index of the face</param>
      /// <return>List of list of the inner boundaries</returns>
      public IList<IList<int>> IndexInnerBoundariesOfFaceAt(int fIdx)
      {
         return facesColl[_mergedFaceList.ElementAt(fIdx)].indexedInnerBoundaries;
      }

      void sortVertAndFaces(int vIndex, int fIndex)
      {
         HashSet<int> facesOfVert;
         if (!sortedFVert.TryGetValue(vIndex, out facesOfVert))
         {
            facesOfVert = new HashSet<int>();
            facesOfVert.Add(fIndex);
            sortedFVert.Add(vIndex, facesOfVert);
         }
         else
         {
            // Dict already contains the point, update the HashSet with this new face
            facesOfVert.Add(fIndex);
         }
      }

      static XYZ normalByNewellMethod(IList<XYZ> vertices)
      {
         XYZ normal;
         if (vertices.Count == 3)
         {
            // If there are only 3 vertices, which is definitely a plannar face, we will use directly 2 vectors and calculate the cross product for the normal vector
            XYZ v1 = vertices[1] - vertices[0];
            XYZ v2 = vertices[2] - vertices[1];
            normal = v1.CrossProduct(v2);
         }
         else
         {
            double normX = 0;
            double normY = 0;
            double normZ = 0;

            // Use Newell algorithm only when there are more than 3 vertices to handle non-convex face and colinear edges
            for (int i = 0; i < vertices.Count; i++)
            {
               if (i == vertices.Count - 1)
               {
                  //The last vertex
                  normX += (vertices[i].Y - vertices[0].Y) * (vertices[i].Z + vertices[0].Z);
                  normY += (vertices[i].Z - vertices[0].Z) * (vertices[i].X + vertices[0].X);
                  normZ += (vertices[i].X - vertices[0].X) * (vertices[i].Y + vertices[0].Y);
               }
               else
               {
                  normX += (vertices[i].Y - vertices[i + 1].Y) * (vertices[i].Z + vertices[i + 1].Z);
                  normY += (vertices[i].Z - vertices[i + 1].Z) * (vertices[i].X + vertices[i + 1].X);
                  normZ += (vertices[i].X - vertices[i + 1].X) * (vertices[i].Y + vertices[i + 1].Y);
               }
            }
            normal = new XYZ(normX, normY, normZ);
         }
         return normal.Normalize();
      }

      /// <summary>
      /// Combine coplanar triangles from the faceted body if they share the edge. From this process, polygonal faces (with or without holes) will be created
      /// </summary>
      public void simplifyAndMergeFaces()
      {
         int noTriangle = _geom.TriangleCount;
         int noVertices = _geom.VertexCount;

         for (int ef = 0; ef<noTriangle; ++ef)
         {
            TriangleInShellComponent f = _geom.GetTriangle(ef);
            IList<int> vertIndex = new List<int>();
            vertIndex.Add(f.VertexIndex0);
            vertIndex.Add(f.VertexIndex1);
            vertIndex.Add(f.VertexIndex2);

            IndexFace intF = new IndexFace(vertIndex);
            facesColl.Add(ef, intF);         // Keep faces in a dictionary and assigns ID
            sortVertAndFaces(f.VertexIndex0, ef);
            sortVertAndFaces(f.VertexIndex1, ef);
            sortVertAndFaces(f.VertexIndex2, ef);
         }

         // After the above, we have a sorted polyhedron vertices that contains hashset of faces it belongs to
         // Loop through the dictionary to merge faces that have the same normal (on the same plane)
         foreach (KeyValuePair<int, HashSet<int>> dictItem in sortedFVert)
         {
            IEqualityComparer<XYZ> normalComparer = new vectorCompare();
            Dictionary<XYZ, List<int>> faceSortedByNormal = new Dictionary<XYZ, List<int>>(normalComparer);
            List<int> fIDList;

            foreach (int fID in dictItem.Value)
            {
               IndexFace f = facesColl[fID];

               if (!faceSortedByNormal.TryGetValue(f.normal, out fIDList))
               {
                  fIDList = new List<int>();
                  fIDList.Add(fID);
                  faceSortedByNormal.Add(f.normal, fIDList);
               }
               else
               {
                  if (!fIDList.Contains(fID))
                  {
                     fIDList.Add(fID);
                  }
               }
            }

            foreach (KeyValuePair<XYZ, List<int>> fListDict in faceSortedByNormal)
            {
               List<int> mergedFaceList = null;
               if (fListDict.Value.Count > 1)
               {
                  tryMergeFaces(fListDict.Value, out mergedFaceList);
                  if (mergedFaceList != null && mergedFaceList.Count > 0)
                  {
                     // insert only new face indexes as the mergedlist from different vertices can be duplicated
                     foreach (int fIdx in mergedFaceList)
                        if (!_mergedFaceList.Contains(fIdx))
                           _mergedFaceList.Add(fIdx);
                  }
               }
               else
                     if (!_mergedFaceList.Contains(fListDict.Value[0]))
                  _mergedFaceList.Add(fListDict.Value[0]);    // No pair face, add it into the mergedList
            }
         }
      }

      /// <summary>
      /// Go through the input face list that share the same vertex and has the same normal (coplannar).
      /// </summary>
      /// <param name="inputFaceList"></param>
      /// <param name="outputFaceList"></param>
      /// <returns></returns>
      bool tryMergeFaces(List<int> inputFaceList, out List<int> outputFaceList)
      {
         outputFaceList = new List<int>();
         IndexFace firstF = facesColl[inputFaceList[0]];
         int prevFirstFIdx = 0;
         HashSet<int> mergedFacesIdxList = new HashSet<int>();
         mergedFacesIdxList.Add(inputFaceList[0]);

         inputFaceList.RemoveAt(0);  // remove the first face from the list
         int currEdgeIdx = 0;
         bool merged = false;

         while (currEdgeIdx < firstF.outerAndInnerBoundaries.Count && inputFaceList.Count > 0)
         {
            IndexSegment reversedEdge = firstF.outerAndInnerBoundaries[currEdgeIdx].reverse();
            int currFaceIdx = 0;
            while (currFaceIdx < inputFaceList.Count && currEdgeIdx < firstF.outerAndInnerBoundaries.Count)
            {
               IndexFace currFace = facesColl[inputFaceList[currFaceIdx]];
               int idx = currFace.findMatechedIndexSegment(reversedEdge);       // Test reversedEdge first as it is the most likely one in our data
               if (idx < 0)
               {
                  idx = currFace.findMatechedIndexSegment(firstF.outerAndInnerBoundaries[currEdgeIdx]);
                  if (idx >= 0)
                  {
                     // Found match, we need to reversed the order of the data in this face
                     currFace.Reverse();
                     idx = currFace.findMatechedIndexSegment(reversedEdge);
                  }
               }
               if (idx < 0)
               {
                  currFaceIdx++;
                  merged = false;
                  continue;   // not found
               }

               // Now we need to check other edges of this face whether there is other coincide edge (this is in the case of hole(s))
               List<int> fFaceIdxList = new List<int>();
               List<int> currFaceIdxList = new List<int>();
               for (int ci = 0; ci < currFace.outerAndInnerBoundaries.Count; ci++)
               {
                  if (ci == idx)
                     continue;   // skip already known coincide edge
                  int ffIdx = -1;
                  IndexSegment reL = new IndexSegment(currFace.outerAndInnerBoundaries[ci].endPIndex, currFace.outerAndInnerBoundaries[ci].startPindex);
                  ffIdx = firstF.findMatechedIndexSegment(reL);
                  if (ffIdx > 0)
                  {
                     fFaceIdxList.Add(ffIdx);        // List of edges to skip when merging
                     currFaceIdxList.Add(ci);        // List of edges to skip when merging
                  }
               }

               // Now we will remove the paired edges and merge the faces
               List<IndexSegment> newFaceEdges = new List<IndexSegment>();
               for (int i = 0; i < currEdgeIdx; i++)
               {
                  bool toSkip = false;
                  if (fFaceIdxList.Count > 0)
                     toSkip = fFaceIdxList.Contains(i);
                  if (!toSkip)
                     newFaceEdges.Add(firstF.outerAndInnerBoundaries[i]);     // add the previous edges from the firstF faces first. This will skip the currEdge
               }

               // Add the next-in-sequence edges from the second face
               for (int i = idx + 1; i < currFace.outerAndInnerBoundaries.Count; i++)
               {
                  bool toSkip = false;
                  if (currFaceIdxList.Count > 0)
                     toSkip = currFaceIdxList.Contains(i);
                  if (!toSkip)
                     newFaceEdges.Add(currFace.outerAndInnerBoundaries[i]);
               }
               for (int i = 0; i < idx; i++)
               {
                  bool toSkip = false;
                  if (currFaceIdxList.Count > 0)
                     toSkip = currFaceIdxList.Contains(i);
                  if (!toSkip)
                     newFaceEdges.Add(currFace.outerAndInnerBoundaries[i]);
               }

               for (int i = currEdgeIdx + 1; i < firstF.outerAndInnerBoundaries.Count; i++)
               {
                  bool toSkip = false;
                  if (fFaceIdxList.Count > 0)
                     toSkip = fFaceIdxList.Contains(i);
                  if (!toSkip)
                     newFaceEdges.Add(firstF.outerAndInnerBoundaries[i]);
               }

               // Build a new face
               // Important to note that the list of edges may not be continuous if there is a hole. We need to go through the list here to identify whether there is any such
               //   discontinuity and collect the edges into their respective loops
               List<List<IndexSegment>> loops = new List<List<IndexSegment>>();

               List<IndexSegment> loopEdges = new List<IndexSegment>();
               loops.Add(loopEdges);
               for (int i = 0; i < newFaceEdges.Count; i++)
               {
                  if (i == 0)
                  {
                     loopEdges.Add(newFaceEdges[i]);
                  }
                  else
                  {
                     if (newFaceEdges[i].startPindex == newFaceEdges[i - 1].endPIndex)
                        loopEdges.Add(newFaceEdges[i]);
                     else
                     {
                        // Discontinuity detected
                        loopEdges = new List<IndexSegment>();   // start new loop
                        loops.Add(loopEdges);
                        loopEdges.Add(newFaceEdges[i]);
                     }
                  }
               }

               List<List<IndexSegment>> finalLoops = new List<List<IndexSegment>>();
               {
                  while (loops.Count > 1)
                  {
                     // There are more than 1 loops, need to consolidate if there are fragments to combine due to their continuity between the fragments
                     int toDelIdx = -1;
                     for (int i = 1; i < loops.Count; i++)
                     {
                        if (loops[0][loops[0].Count - 1].endPIndex == loops[i][0].startPindex)
                        {
                           // found continuity, merge the loops
                           List<IndexSegment> newLoop = new List<IndexSegment>(loops[0]);
                           newLoop.AddRange(loops[i]);
                           finalLoops.Add(newLoop);
                           toDelIdx = i;
                           break;
                        }
                     }
                     if (toDelIdx > 0)
                     {
                        loops.RemoveAt(toDelIdx);   // !!!! Important to remove the later member first before removing the first one 
                        loops.RemoveAt(0);
                     }
                     else
                     {
                        // No continuity found, copy the first loop to the final loop
                        List<IndexSegment> newLoop = new List<IndexSegment>(loops[0]);
                        finalLoops.Add(newLoop);
                        loops.RemoveAt(0);
                     }
                  }
                  if (loops.Count > 0)
                  {
                     // Add remaining list into the final loops
                     finalLoops.AddRange(loops);
                  }
               }

               if (finalLoops.Count > 1)
               {
                  // Find the largest loop and put it in the first position signifying the outer loop and the rest are the inner loops
                  int largestPerimeterIdx = 0;
                  double largestPerimeter = 0.0;
                  for (int i = 0; i < finalLoops.Count; i++)
                  {
                     double loopPerimeter = 0.0;
                     foreach (IndexSegment line in finalLoops[i])
                        loopPerimeter += line.extent;
                     if (loopPerimeter > largestPerimeter)
                     {
                        largestPerimeter = loopPerimeter;
                        largestPerimeterIdx = i;
                     }
                  }
                  // We need to move the largest loop into the head if it is not
                  if (largestPerimeterIdx > 0)
                  {
                     List<IndexSegment> largestLoop = new List<IndexSegment>(finalLoops[largestPerimeterIdx]);
                     finalLoops.RemoveAt(largestPerimeterIdx);
                     finalLoops.Insert(0, largestLoop);
                  }
               }

               // Collect the vertices from the list of Edges into list of list of vertices starting with the outer loop (largest loop) following the finalLoop
               IList<IList<int>> newFaceVertsLoops = new List<IList<int>>();
               foreach (List<IndexSegment> loop in finalLoops)
               {
                  IList<int> newFaceVerts = new List<int>();
                  for (int i = 0; i < loop.Count; i++)
                  {
                     if (i == 0)
                     {
                        newFaceVerts.Add(loop[i].startPindex);
                        newFaceVerts.Add(loop[i].endPIndex);
                     }
                     else if (i == loop.Count - 1)   // Last
                     {
                        // Add nothing as the last segment ends at the first vertex
                     }
                     else
                     {
                        newFaceVerts.Add(loop[i].endPIndex);
                     }
                  }
                  // close the loop with end point from the starting point (it is important to mark the end of loop and if there is any other vertex follow, they start the inner loop)
                  if (newFaceVerts.Count > 0)
                  {
                     if (newFaceVerts.Count >= 3)
                        newFaceVertsLoops.Add(newFaceVerts);
                     else
                     {
                        // Something wrong, a face cannot have less than 3 vertices
                        Debug.WriteLine("Something went wrong when merging faces resulting with a loop that has less than 3 vertices");
                     }
                  }
               }

               firstF = new IndexFace(newFaceVertsLoops);

               currEdgeIdx = 0;
               reversedEdge = new IndexSegment(firstF.outerAndInnerBoundaries[currEdgeIdx].endPIndex, firstF.outerAndInnerBoundaries[currEdgeIdx].startPindex);

               mergedFacesIdxList.Add(inputFaceList[currFaceIdx]);
               inputFaceList.RemoveAt(currFaceIdx);
               currFaceIdx = 0;
               merged = true;
            }

            if (!merged)
            {
               currEdgeIdx++;
            }
            if (merged || currEdgeIdx == firstF.outerAndInnerBoundaries.Count)
            {
               int lastFaceID = facesColl.Count;   // The new index is always the next one in the collection was inserted based on the seq order

               facesColl.Add(lastFaceID, firstF);
               prevFirstFIdx = lastFaceID;
               outputFaceList.Add(lastFaceID);

               // Now loop through all the dictionary of the sortedVert and replace all merged face indexes with the new one
               foreach (KeyValuePair<int, HashSet<int>> v in sortedFVert)
               {
                  HashSet<int> fIndexes = v.Value;
                  bool replaced = false;
                  foreach (int Idx in mergedFacesIdxList)
                  {
                     replaced |= fIndexes.Remove(Idx);
                     _mergedFaceList.Remove(Idx);        // Remove the idx face also from _mergeFaceList as some faces might be left unmerged in the previous step(s)

                  }
                  if (replaced)
                     fIndexes.Add(lastFaceID);   // replace the merged face indexes with the new merged face index
               }

               if (inputFaceList.Count > 0)
               {
                  firstF = facesColl[inputFaceList[0]];
                  mergedFacesIdxList.Clear();
                  mergedFacesIdxList.Add(inputFaceList[0]);
                  inputFaceList.RemoveAt(0);  // remove the first face from the list
                  currEdgeIdx = 0;
                  merged = false;
               }
            }
         }

         return merged;
      }
   }
}


