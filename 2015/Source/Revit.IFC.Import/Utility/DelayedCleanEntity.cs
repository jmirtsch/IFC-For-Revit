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
using Revit.IFC.Import.Data;

namespace Revit.IFC.Import.Utility
{
    public class DelayedCleanEntity : IDisposable
    {
        IFCObjectDefinition m_ObjectDefinition = null;

        /// <summary>
        /// The public constructor for this class.
        /// </summary>
        /// <param name="objectDefinition">The entity for which to delay cleaning, if it is enabled.</param>
        public DelayedCleanEntity(IFCObjectDefinition objectDefinition)
        {
            if (objectDefinition != null)
            {
                m_ObjectDefinition = objectDefinition;
                m_ObjectDefinition.DelayCleanEntity = true;
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (m_ObjectDefinition != null)
            {
                m_ObjectDefinition.DelayCleanEntity = false;
                m_ObjectDefinition.CleanEntity();
            }
        }

        #endregion
    }
}
