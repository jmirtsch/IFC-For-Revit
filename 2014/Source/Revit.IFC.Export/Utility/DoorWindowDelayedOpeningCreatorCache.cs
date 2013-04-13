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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;

namespace Revit.IFC.Export.Utility
{
    /// <summary>
    /// Used to keep a cache to create door window openings.
    /// </summary>
    public class DoorWindowDelayedOpeningCreatorCache
    {
        Dictionary<ElementId, DoorWindowDelayedOpeningCreator> m_DelayedOpeningCreators = new Dictionary<ElementId, DoorWindowDelayedOpeningCreator>();

        /// <summary>
        /// Adds a new DoorWindowDelayedOpeningCreator.
        /// </summary>
        /// <param name="creator">The creator.</param>
        public void Add(DoorWindowDelayedOpeningCreator creator)
        {
            if (creator == null)
                return;

            DoorWindowDelayedOpeningCreator oldCreator = null;
            if (m_DelayedOpeningCreators.TryGetValue(creator.InsertId, out oldCreator))
            {
                // from DoorWindowInfo has higher priority
                if (oldCreator.CreatedFromDoorWindowInfo)
                {
                    if (!oldCreator.HasValidGeometry && creator.HasValidGeometry)
                    {
                        oldCreator.CopyGeometry(creator);
                    }
                }
                else if (creator.CreatedFromDoorWindowInfo)
                {
                    if (!creator.HasValidGeometry && oldCreator.HasValidGeometry)
                    {
                        creator.CopyGeometry(oldCreator);
                    }
                    m_DelayedOpeningCreators[creator.InsertId] = creator;
                }
            }
            else
                m_DelayedOpeningCreators[creator.InsertId] = creator;
        }

        /// <summary>
        /// Executes all opening creators in this cache.
        /// </summary>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="doc">The document.</param>
        public void ExecuteCreators(ExporterIFC exporterIFC, Document doc)
        {
            foreach (DoorWindowDelayedOpeningCreator creator in m_DelayedOpeningCreators.Values)
            {
                creator.Execute(exporterIFC, doc);
            }
        }
    }
}
