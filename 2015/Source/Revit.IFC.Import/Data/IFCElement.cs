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
using Revit.IFC.Common.Enums;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
    /// <summary>
    /// Represents an IfcElement.
    /// </summary>
    public class IFCElement : IFCProduct
    {
        protected string m_Tag = null;

        protected ICollection<IFCFeatureElementSubtraction> m_Openings = null;

        protected IFCFeatureElementSubtraction m_FillsOpening = null;

        public string Tag
        {
            get { return m_Tag; }
        }

        public ICollection<IFCFeatureElementSubtraction> Openings
        {
            get 
            {
                if (m_Openings == null)
                    m_Openings = new List<IFCFeatureElementSubtraction>();
                return m_Openings; 
            }
        }

        public IFCFeatureElementSubtraction FillsOpening
        {
            get { return m_FillsOpening; }
            set { m_FillsOpening = value; }
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        protected IFCElement()
        {

        }

        /// <summary>
        /// Constructs an IFCElement from the IfcElement handle.
        /// </summary>
        /// <param name="ifcElement">The IfcElement handle.</param>
        protected IFCElement(IFCAnyHandle ifcElement)
        {
            Process(ifcElement);
        }

        /// <summary>
        /// Processes IfcElement attributes.
        /// </summary>
        /// <param name="ifcElement">The IfcElement handle.</param>
        protected override void Process(IFCAnyHandle ifcElement)
        {
            base.Process(ifcElement);
            
            m_Tag = IFCAnyHandleUtil.GetStringAttribute(ifcElement, "Tag");

            ICollection<IFCAnyHandle> hasOpenings = IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(ifcElement, "HasOpenings");
            if (hasOpenings != null)
            {
                foreach (IFCAnyHandle hasOpening in hasOpenings)
                {
                    IFCAnyHandle relatedOpeningElement = IFCAnyHandleUtil.GetInstanceAttribute(hasOpening, "RelatedOpeningElement");
                    if (IFCAnyHandleUtil.IsNullOrHasNoValue(relatedOpeningElement))
                        continue;

                    IFCFeatureElementSubtraction opening = IFCFeatureElementSubtraction.ProcessIFCFeatureElementSubtraction(relatedOpeningElement);
                    if (opening != null)
                    {
                        opening.VoidsElement = this;
                        Openings.Add(opening);
                    }
                }
            }
        }

        /// <summary>
        /// Creates or populates Revit element params based on the information contained in this class.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="element">The element.</param>
        protected override void CreateParametersInternal(Document doc, Element element)
        {
            base.CreateParametersInternal(doc, element);

            if (element != null)
            {
                // Set "Tag" parameter.
                string ifcTag = Tag;
                if (!string.IsNullOrWhiteSpace(ifcTag))
                    IFCPropertySet.AddParameterString(doc, element, "IfcTag", ifcTag, Id);
            }
        }

        /// <summary>
        /// Creates or populates Revit elements based on the information contained in this class.
        /// </summary>
        /// <param name="doc">The document.</param>
        protected override void Create(Document doc)
        {
            foreach (IFCFeatureElement opening in Openings)
            {
                try
                {
                    // Don't clean the element until after we get the solid information.
                    using (DelayedCleanEntity dce = new DelayedCleanEntity(opening))
                    {
                        CreateElement(doc, opening);
                        foreach (IFCSolidInfo voidGeom in opening.Solids)
                            Voids.Add(voidGeom);
                    }
                }
                catch (Exception ex)
                {
                    IFCImportFile.TheLog.LogError(opening.Id, ex.Message, false);
                }
            }

            base.Create(doc);
        }

        /// <summary>
        /// Cleans out the IFCEntity to save memory.
        /// </summary>
        public override void CleanEntity()
        {
            base.CleanEntity();
        
            m_Tag = null;

            m_Openings = null;

            m_FillsOpening = null;
        }
            
        /// <summary>
        /// Processes an IfcElement object.
        /// </summary>
        /// <param name="ifcElement">The IfcElement handle.</param>
        /// <returns>The IFCElement object.</returns>
        public static IFCElement ProcessIFCElement(IFCAnyHandle ifcElement)
        {
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcElement))
            {
                IFCImportFile.TheLog.LogNullError(IFCEntityType.IfcElement);
                return null;
            }

            IFCEntity cachedIFCElement;
            IFCImportFile.TheFile.EntityMap.TryGetValue(ifcElement.StepId, out cachedIFCElement);
            if (cachedIFCElement != null)
                return (cachedIFCElement as IFCElement);

            IFCElement newIFCElement = null;
            // other subclasses not handled yet!
            if (IFCAnyHandleUtil.IsSubTypeOf(ifcElement, IFCEntityType.IfcFeatureElement))
                newIFCElement = IFCFeatureElement.ProcessIFCFeatureElement(ifcElement);
            else
                newIFCElement = new IFCElement(ifcElement);
            return newIFCElement;
        }
    }
}
