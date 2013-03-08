//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2012  Autodesk, Inc.
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

using BIM.IFC.Toolkit;
using BIM.IFC.Exporter.PropertySet;

namespace BIM.IFC.Utility
{
    /// <summary>
    /// Manages state information for the current export session.  Intended to eventually replace ExporterIFC for most state operations.
    /// </summary>
    public class ExporterStateManager
    {
        static IList<string> m_CADLayerOverrides;

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
        /// Resets the state manager.
        /// </summary>
        static public void Clear()
        {
            m_CADLayerOverrides = null;
        }
    }
}
