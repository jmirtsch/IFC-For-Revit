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
using Revit.IFC.Import.Data;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Import.Utility
{
    /// <summary>
    /// Utilities for IFCElement
    /// </summary>
    public class IFCElementUtil
    {
        /// <summary>
        /// Gets the host of a hosted element, if any.
        /// </summary>
        /// <param name="hostedElement">The hosted element.</param>
        /// <returns>The host, or null.</returns>
        static public IFCElement GetHost(IFCElement hostedElement)
        {
            if (hostedElement == null)
                return null;

            IFCFeatureElementSubtraction fillsOpening = hostedElement.FillsOpening;
            if (fillsOpening == null)
                return null;

            return fillsOpening.VoidsElement;
        }

        /// <summary>
        /// Gets the elements hosted by this host element, if any.
        /// </summary>
        /// <param name="hostElement">The host element.</param>
        /// <returns>The hosted elements, or null.  An unfilled opening counts as a hosted element.</returns>
        static public IList<IFCElement> GetHostedElements(IFCElement hostElement)
        {
            if (hostElement == null)
                return null;

            ICollection<IFCFeatureElementSubtraction> openings = hostElement.Openings;
            if (openings == null || (openings.Count == 0))
                return null;

            IList<IFCElement> hostedElements = new List<IFCElement>();
            foreach (IFCFeatureElementSubtraction opening in openings)
            {
                if (!(opening is IFCOpeningElement))
                    hostedElements.Add(opening);
                else
                {
                    IFCOpeningElement openingElement = opening as IFCOpeningElement;
                    if (openingElement.FilledByElement != null)
                        hostedElements.Add(openingElement.FilledByElement);
                    else
                        hostedElements.Add(openingElement);
                }
            }

            return hostedElements;
        }

        /// <summary>
        /// Create a DirectShape, and set its options accordingly.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="categoryId">The category of the DirectShape.</param>
        /// <param name="dataGUID">The GUID of the data creating the DirectShape.</param>
        /// <returns>The DirectShape.</returns>
        static public DirectShape CreateElement(Document doc, ElementId categoryId, string dataGUID, IList<GeometryObject> geomObjs)
        {
            string appGUID = Importer.ImportAppGUID();
            DirectShape directShape = DirectShape.CreateElement(doc, categoryId, appGUID, dataGUID);
            
            // Note: we use the standard options for the DirectShape that is created.  This includes but is not limited to:
            // Referenceable: true.
            // Room Bounding: if applicable, user settable.

            if (directShape != null && geomObjs != null)
                directShape.SetShape(geomObjs);

            return directShape;
        }
    }
}
