//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2011  Autodesk, Inc.
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

namespace BIM.IFC.Utility
{
    /// <summary>
    /// Used to keep a cache of the curve style handles mapping to curve annotations.
    /// </summary>
    class CurveAnnotationCache
    {
        /// <summary>
        /// Used as a key for a dictionary.
        /// </summary>
        struct CurveAnnotationKey
        {
            /// <summary>
            /// The sketch plane id.
            /// </summary>
            public ElementId SketchPlaneId;
            /// <summary>
            /// The curve style handle.
            /// </summary>
            public IFCAnyHandle CurveStyleHandle;
        }


        /// <summary>
        /// The dictionary mapping from CurveAnnotationKey to curve annotation handle. 
        /// </summary>
        Dictionary<CurveAnnotationKey, IFCAnyHandle> m_AnnotationMap;

        /// <summary>
        /// Constructs a default CurveAnnotationCache object.
        /// </summary>
        public CurveAnnotationCache()
        {
            m_AnnotationMap = new Dictionary<CurveAnnotationKey, IFCAnyHandle>();
        }

        /// <summary>
        /// Gets the curve annotation handle from the dictionary.
        /// </summary>
        /// <param name="sketchPlaneId">
        /// The sketch plane id.
        /// </param>
        /// <param name="curveStyleHandle">
        /// The curve style handle.
        /// </param>
        /// <returns>
        /// The curve annotation handle.
        /// </returns>
        public IFCAnyHandle GetAnnotation(ElementId sketchPlaneId, IFCAnyHandle curveStyleHandle)
        {
            IFCAnyHandle ret;
            CurveAnnotationKey key = new CurveAnnotationKey();
            key.SketchPlaneId = sketchPlaneId;
            key.CurveStyleHandle = curveStyleHandle;
            if (m_AnnotationMap.TryGetValue(key, out ret))
            {
                return ret;
            }
            else
            {
                return IFCAnyHandle.Create();
            }
        }

        /// <summary>
        /// Adds a curve annotation handle to the dictionary.
        /// </summary>
        /// <param name="sketchPlaneId">
        /// The sketch plane id.
        /// </param>
        /// <param name="curveStyleHandle">
        /// The curve style handle.
        /// </param>
        /// <param name="curveAnnotation">
        /// The curve annotation handle.
        /// </param>
        public void AddAnnotation(ElementId sketchPlaneId, IFCAnyHandle curveStyleHandle, IFCAnyHandle curveAnnotation)
        {
            CurveAnnotationKey key = new CurveAnnotationKey();
            key.SketchPlaneId = sketchPlaneId;
            key.CurveStyleHandle = curveStyleHandle;

            if (m_AnnotationMap.ContainsKey(key))
            {
                throw new Exception("CurveAnnotationCache already contains this key");
            }

            m_AnnotationMap[key] = curveAnnotation;
        }
    }
}
