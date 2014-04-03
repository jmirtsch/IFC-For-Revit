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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
    /// <summary>
    /// Represents an IfcSite.
    /// </summary>
    public class IFCSite : IFCSpatialStructureElement
    {
        double? m_RefLatitude = null;
        double? m_RefLongitude = null;
        double m_RefElevation = 0.0;
        string m_LandTitleNumber = null;
        // TODO: handle SiteAddress.

        /// <summary>
        /// Constructs an IFCSite from the IfcSite handle.
        /// </summary>
        /// <param name="ifcIFCSite">The IfcSite handle.</param>
        protected IFCSite(IFCAnyHandle ifcIFCSite)
        {
            Process(ifcIFCSite);
        }

        /// <summary>
        /// Cleans out the IFCEntity to save memory.
        /// </summary>
        public override void CleanEntity()
        {
            base.CleanEntity();

            m_LandTitleNumber = null;
        }

        /// <summary>
        /// Processes IfcSite attributes.
        /// </summary>
        /// <param name="ifcIFCSite">The IfcSite handle.</param>
        protected override void Process(IFCAnyHandle ifcIFCSite)
        {
            base.Process(ifcIFCSite);
            
            RefElevation = IFCImportHandleUtil.GetOptionalScaledLengthAttribute(ifcIFCSite, "RefElevation", 0.0);

            IList<int> refLatitudeList = IFCAnyHandleUtil.GetAggregateIntAttribute<List<int>>(ifcIFCSite, "RefLatitude");
            IList<int> refLongitudeList = IFCAnyHandleUtil.GetAggregateIntAttribute<List<int>>(ifcIFCSite, "RefLongitude");

            if (refLatitudeList != null)
            {
                m_RefLatitude = 0.0;
                double latLongScaler = 1.0;
                foreach (double latVal in refLatitudeList)
                {
                    m_RefLatitude += ((double)latVal) / latLongScaler;
                    latLongScaler *= 60.0;
                }
            }
                
            if (refLongitudeList != null)
            {
                m_RefLongitude = 0.0;
                double latLongScaler = 1.0;
                foreach (double longVal in refLongitudeList)
                {
                    m_RefLongitude += ((double)longVal) / latLongScaler;
                    latLongScaler *= 60.0;
                }
            }

            m_LandTitleNumber = IFCAnyHandleUtil.GetStringAttribute(ifcIFCSite, "LandTitleNumber");
        }

        /// <summary>
        /// The site elevation, in Revit internal units.
        /// </summary>
        public double RefElevation
        {
            get { return m_RefElevation; }
            protected set { m_RefElevation = value; }
        }

        /// <summary>
        /// The site latitude, in degrees.
        /// </summary>
        public double? RefLatitude
        {
            get { return m_RefLatitude; }
        }

        /// <summary>
        /// The site longitude, in degrees.
        /// </summary>
        public double? RefLongitude
        {
            get { return m_RefLongitude; }
        }

        /// <summary>
        /// The Land Title number.
        /// </summary>
        public string LandTitleNumber
        {
            get { return m_LandTitleNumber; }
        }

        /// <summary>
        /// Processes an IfcSite object.
        /// </summary>
        /// <param name="ifcSite">The IfcSite handle.</param>
        /// <returns>The IFCSite object.</returns>
        public static IFCSite ProcessIFCSite(IFCAnyHandle ifcSite)
        {
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcSite))
            {
                IFCImportFile.TheLog.LogNullError(IFCEntityType.IfcSite);
                return null;
            }

            IFCEntity site;
            if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcSite.StepId, out site))
                return (site as IFCSite);

            return new IFCSite(ifcSite);
        }

        /// <summary>
        /// Creates or populates Revit elements based on the information contained in this class.
        /// </summary>
        /// <param name="doc">The document.</param>
        protected override void Create(Document doc)
        {
            // Only set the project location for the site that contains the building.
            bool hasBuilding = false;
            foreach (IFCObjectDefinition objectDefinition in ComposedObjectDefinitions)
            {
                if (objectDefinition is IFCBuilding)
                {
                    hasBuilding = true;
                    break;
                }
            }

            if (hasBuilding)
            {
                ProjectLocation projectLocation = doc.ActiveProjectLocation;
                if (projectLocation != null)
                {
                    SiteLocation siteLocation = projectLocation.SiteLocation;
                    if (siteLocation != null)
                    {
                        if (RefLatitude.HasValue)
                            siteLocation.Latitude = RefLatitude.Value * Math.PI / 180.0;
                        if (RefLongitude.HasValue)
                            siteLocation.Longitude = RefLongitude.Value * Math.PI / 180.0;
                    }

                    if (ObjectLocation != null)
                    {
                        XYZ projectLoc = (ObjectLocation.RelativeTransform != null) ? ObjectLocation.RelativeTransform.Origin : XYZ.Zero;

                        // Get true north from IFCProject.
                        double trueNorth = 0.0;
                        IList<double> trueNorthList = IFCImportFile.TheFile.IFCProject.TrueNorthDirection;
                        if (trueNorthList != null && trueNorthList.Count >= 2)
                            trueNorth = Math.Atan2(trueNorthList[1], trueNorthList[0]);

                        ProjectPosition projectPosition = new ProjectPosition(projectLoc.X, projectLoc.Y, RefElevation, trueNorth);

                        XYZ origin = new XYZ(0, 0, 0);
                        projectLocation.set_ProjectPosition(origin, projectPosition);

                        // Now that we've set the project position, remove the site relative transform.
                        IFCLocation.RemoveRelativeTransformForSite(this);
                    }
                }
            }

            base.Create(doc);
        }

        /// <summary>
        /// Creates or populates Revit element params based on the information contained in this class.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="element">The element.</param>
        protected override void CreateParametersInternal(Document doc, Element element)
        {
            base.CreateParametersInternal(doc, element);
            string parameterName = "LandTitleNumber";

            if (element == null)
            {
                element = doc.ProjectInformation;
                parameterName = "Site " + parameterName;
            }

            if (element != null)
            {
                string landTitleNumber = LandTitleNumber;
                if (!string.IsNullOrWhiteSpace(landTitleNumber))
                    IFCPropertySet.AddParameterString(doc, element, parameterName, landTitleNumber, Id);
            }
        }
    }
}
