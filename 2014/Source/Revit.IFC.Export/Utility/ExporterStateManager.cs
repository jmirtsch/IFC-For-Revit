//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
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

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;

using Revit.IFC.Export.Toolkit;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Utility
{
    /// <summary>
    /// Manages state information for the current export session.  Intended to eventually replace ExporterIFC for most state operations.
    /// </summary>
    public class ExporterStateManager
    {
        static IList<string> m_CADLayerOverrides;

        static int m_RangeIndex;

        /// <summary>
        /// A utility class that manages keeping track of a sub-element index for ranges for splitting walls and columns.  
        /// Intended to be using with "using" keyword.
        /// </summary>
        public class RangeIndexSetter : IDisposable
        {
            /// <summary>
            /// Increment the range index.
            /// </summary>
            public void IncreaseRangeIndex()
            {
                m_RangeIndex++;
            }

            /// <summary>
            /// Return the maximum allowed number of stable GUIDs for elements split by range.
            /// </summary>
            /// <returns>The maximum allowed number of stable GUIDs for elements split by range. </returns>
            static public int GetMaxStableGUIDs()
            {
                const int maxSplitIndices = IFCGenericSubElements.SplitInstanceEnd - IFCGenericSubElements.SplitInstanceStart + 1;
                return maxSplitIndices;
            }

            #region IDisposable Members

            /// <summary>
            /// Reset the range index.
            /// </summary>
            public void Dispose()
            {
                m_RangeIndex = 0;
            }

            #endregion
        }

        /// <summary>
        /// A utility class that manages pushing and popping CAD layer overrides for containers.  Intended to be using with "using" keyword.
        /// </summary>
        public class CADLayerOverrideSetter : IDisposable
        {
            bool m_ValidString = false;

            /// <summary>
            /// The constructor that sets the current CAD layer override string.  Will do nothing if the string in invalid or null.
            /// </summary>
            /// <param name="overrideString">The value.</param>
            public CADLayerOverrideSetter(string overrideString)
            {
                if (!string.IsNullOrWhiteSpace(overrideString))
                {
                    ExporterStateManager.PushCADLayerOverride(overrideString);
                    m_ValidString = true;
                }
            }

            #region IDisposable Members

            /// <summary>
            /// Pop the current CAD layer override string, if valid.
            /// </summary>
            public void Dispose()
            {
                if (m_ValidString)
                {
                    ExporterStateManager.PopCADLayerOverride();
                }
            }

            #endregion
        }

        static private IList<string> CADLayerOverrides
        {
            get
            {
                if (m_CADLayerOverrides == null)
                    m_CADLayerOverrides = new List<string>();
                return m_CADLayerOverrides;
            }
        }
        
        static private void PushCADLayerOverride(string overrideString)
        {
            CADLayerOverrides.Add(overrideString);
        }

        static private void PopCADLayerOverride()
        {
            int size = CADLayerOverrides.Count;
            if (size > 0)
                CADLayerOverrides.RemoveAt(size - 1);
        }

        /// <summary>
        /// Get the current CAD layer override string.
        /// </summary>
        /// <returns>The CAD layer override string, or null if not set.</returns>
        static public string GetCurrentCADLayerOverride()
        {
            if (CADLayerOverrides.Count > 0)
                return CADLayerOverrides[0];
            return null;
        }

        /// <summary>
        /// Get the current range index.
        /// </summary>
        /// <returns>The current range index, or 0 if there are no ranges.
        static public int GetCurrentRangeIndex()
        {
            return m_RangeIndex;
        }

        /// <summary>
        /// Resets the state manager.
        /// </summary>
        static public void Clear()
        {
            m_CADLayerOverrides = null;
            m_RangeIndex = 0;
        }
    }
}
