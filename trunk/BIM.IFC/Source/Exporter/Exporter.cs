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
using System.Text;
using System.IO;
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using System.Collections.ObjectModel;
using BIM.IFC.Utility;

namespace BIM.IFC.Exporter
{
    /// <summary>
    /// This class implements the method of interface IExporterIFC to perform an export to IFC. 
    /// It also implements the methods of interface IExternalDBApplication to register the IFC export client to Autodesk Revit.
    /// </summary>
    class Exporter : IExporterIFC, IExternalDBApplication
    {
        /// <summary>
        /// Used for debugging tool "WriteIFCExportedElements"
        /// </summary>
        private StreamWriter m_Writer;

        /// <summary>
        /// Parameter cache.
        /// </summary>
        private ParameterCache m_ParameterCache;

        /// <summary>
        /// Presentation style cache.
        /// </summary>
        private PresentationStyleAssignmentCache m_PresentationStyleCache;

        /// <summary>
        /// Curve annotation cache.
        /// </summary>
        private CurveAnnotationCache m_CurveAnnotationCache;

        #region IExternalDBApplication Members

        /// <summary>
        /// The method called when Autodesk Revit exits.
        /// </summary>
        /// <param name="application">Controlled application to be shutdown.</param>
        /// <returns>Return the status of the external application.</returns>
        public ExternalDBApplicationResult OnShutdown(Autodesk.Revit.ApplicationServices.ControlledApplication application)
        {
            return ExternalDBApplicationResult.Succeeded;
        }

        /// <summary>
        /// The method called when Autodesk Revit starts.
        /// </summary>
        /// <param name="application">Controlled application to be loaded to Autodesk Revit process.</param>
        /// <returns>Return the status of the external application.</returns>
        public ExternalDBApplicationResult OnStartup(Autodesk.Revit.ApplicationServices.ControlledApplication application)
        {
            ExporterIFCRegistry.RegisterIFCExporter(this);
            return ExternalDBApplicationResult.Succeeded;
        }

        #endregion

        #region IExporterIFC Members

        /// <summary>
        /// Implements the method that Autodesk Revit will invoke to perform an export to IFC.
        /// </summary>
        /// <param name="document">The document to export.</param>
        /// <param name="exporterIFC">The IFC exporter object.</param>
        /// <param name="view">The 3D view that is being exported.</param>
        /// <param name="filterView">The view whose filter visibility settings govern the export.</param>
        public void ExportIFC(Autodesk.Revit.DB.Document document, ExporterIFC exporterIFC, Autodesk.Revit.DB.View view, Autodesk.Revit.DB.View filterView)
        {
            try
            {
                String writeIFCExportedElementsVar = Environment.GetEnvironmentVariable("WriteIFCExportedElements");
                if (writeIFCExportedElementsVar != null && writeIFCExportedElementsVar.Length > 0)
                {
                    m_Writer = new StreamWriter(@"c:\ifc-output-filters.txt");
                }

                //init common properties
                m_ParameterCache = new ParameterCache();
                ExporterInitializer.InitPropertySets(m_ParameterCache, exporterIFC.FileVersion);
                ExporterInitializer.InitQuantities(m_ParameterCache, exporterIFC.FileVersion);

                m_PresentationStyleCache = new PresentationStyleAssignmentCache();

                m_CurveAnnotationCache = new CurveAnnotationCache();

                //export spatial element - none or 1st level room boundaries
                if (exporterIFC.SpaceBoundaryLevel == 0 || exporterIFC.SpaceBoundaryLevel == 1)
                {
                    SpatialElementExporter.InitializeSpatialElementGeometryCalculator(document, exporterIFC);
                    ElementFilter spatialElementFilter = ElementFilteringUtil.GetSpatialElementFilter(document, exporterIFC);
                    FilteredElementCollector collector = new FilteredElementCollector(document);
                    collector.WherePasses(spatialElementFilter);
                    foreach (Element element in collector)
                    {
                        ExportElement(exporterIFC, filterView, element);
                    }
                }
                else if (exporterIFC.SpaceBoundaryLevel == 2)
                {
                    SpatialElementExporter.ExportSpatialElement2ndLevel(this, exporterIFC, document, filterView);
                }

                //export other elements
                ElementFilter nonSpatialElementFilter = ElementFilteringUtil.GetNonSpatialElementFilter(document, exporterIFC);
                FilteredElementCollector collector2 = new FilteredElementCollector(document);
                collector2.WherePasses(nonSpatialElementFilter);
                foreach (Element element in collector2)
                {
                    ExportElement(exporterIFC, filterView, element);
                }


                // These elements are created internally, but we allow custom property sets for them.  Create them here.
                using (IFCProductWrapper productWrapper = IFCProductWrapper.Create(exporterIFC, true))
                {
                    // This allows for custom property sets for buildings. There are currently no custom quantities, 
                    // but the code could be added here at a later date.
                    IFCAnyHandle buildingHnd = ExporterIFCUtils.GetBuilding(exporterIFC);
                    productWrapper.AddBuilding(buildingHnd);
                    ExportElementProperties(exporterIFC, document.ProjectInformation, productWrapper);
                }
            }
            finally
            {
                if (m_Writer != null)
                    m_Writer.Close();
            }

        }

        #endregion

        /// <summary>
        /// Performs the export of elements, including spatial and non-spatial elements.
        /// </summary>
        /// <param name="exporterIFC">The IFC exporter object.</param>
        /// <param name="filterView">The view whose filter visibility settings govern the export.</param>
        /// <param name="element ">The element to export.</param>
        internal void ExportElement(ExporterIFC exporterIFC, Autodesk.Revit.DB.View filterView, Autodesk.Revit.DB.Element element)
        {
            //current view only
            if (filterView != null && !ElementFilteringUtil.IsElementVisible(filterView, element))
                return;
            //

            if (!ElementFilteringUtil.ShouldCategoryBeExported(exporterIFC, element))
                return;

            //WriteIFCExportedElements
            if (m_Writer != null)
            {
                Category category = element.Category;
                m_Writer.WriteLine(String.Format("{0},{1},{2}", element.Id, category == null ? "null" : category.Name, element.GetType().Name));
            }

            try
            {
                using (IFCProductWrapper productWrapper = IFCProductWrapper.Create(exporterIFC, true))
                {
                    if (IsElementSupportedExternally(exporterIFC, element))
                    {
                        ExportElementImpl(exporterIFC, element, productWrapper);
                    }
                    else
                        ExporterIFCUtils.ExportElementInternal(exporterIFC, element, productWrapper);

                    ExportElementProperties(exporterIFC, element, productWrapper);
                    ExportElementQuantities(exporterIFC, element, productWrapper);
                }
            }
            catch (System.Exception ex)
            {
                string errMsg = String.Format("IFC error: Exporting element \"{0}\",{1} - {2}", element.Name, element.Id, ex.ToString());
                element.Document.Application.WriteJournalComment(errMsg, true);
            }
        }

        /// <summary>
        /// Checks if the element is MEP type.
        /// </summary>
        /// <param name="exporterIFC">The IFC exporter object.</param>
        /// <param name="element">The element to check.</param>
        /// <returns>True for MEP type of elements.</returns>
        private bool IsMEPType(ExporterIFC exporterIFC, Element element)
        {
            string ifcEnumType;
            IFCExportType exportType = ExporterUtil.GetExportType(exporterIFC, element, out ifcEnumType);

            return (exportType >= IFCExportType.ExportDistributionElement && exportType <= IFCExportType.ExportWasteTerminalType);
        }

        /// <summary>
        /// Checks if the element is supported externally.
        /// </summary>
        /// <param name="exporterIFC">The IFC exporter object.</param>
        /// <param name="element ">The element to check.</param>
        /// <returns>True if element is externally supported.</returns>
        private bool IsElementSupportedExternally(ExporterIFC exporterIFC, Element element)
        {
            // FilledRegion is still handled by internal Revit code, but the get_Geometry call makes sure
            // that the Owner view is clean before we get the region's GeometryElement.
            return ((element is CurveElement) ||
                (element is FamilyInstance) ||
                (element is FilledRegion) ||
                //(element is Wall) ||   
                (element is TextNote) ||
                (element is Floor) ||
                (element is SpatialElement) ||
                (IsMEPType(exporterIFC, element)));
        }

        /// <summary>
        /// Implements the export of element.
        /// </summary>
        /// <param name="exporterIFC">The IFC exporter object.</param>
        /// <param name="element ">The element to export.</param>
        /// <param name="productWrapper">The IFCProductWrapper object.</param>
        private void ExportElementImpl(ExporterIFC exporterIFC, Element element, IFCProductWrapper productWrapper)
        {
            Options options = new Options();
            View ownerView = element.Document.get_Element(element.OwnerViewId) as View;
            if (ownerView != null)
                options.View = ownerView;
            GeometryElement geomElem = element.get_Geometry(options);

            try
            {
                exporterIFC.PushExportState(element, geomElem);

                using (SubTransaction st = new SubTransaction(element.Document))
                {
                    st.Start();

                    if (element is CurveElement)
                    {
                        CurveElement curveElem = element as CurveElement;
                        CurveElementExporter.ExportCurveElement(exporterIFC, curveElem, geomElem, productWrapper, m_CurveAnnotationCache);
                    }
                    else if (element is FamilyInstance)
                    {
                        FamilyInstance familyInstanceElem = element as FamilyInstance;
                        FamilyInstanceExporter.ExportFamilyInstanceElement(exporterIFC, familyInstanceElem, geomElem, productWrapper);
                    }
                    else if (element is Floor)
                    {
                        Floor floorElem = element as Floor;
                        FloorExporter.ExportFloor(exporterIFC, floorElem, geomElem, productWrapper);
                    }
                    else if (element is SpatialElement)
                    {
                        SpatialElement spatialElem = element as SpatialElement;
                        SpatialElementExporter.ExportSpatialElement(exporterIFC, spatialElem, productWrapper);
                    }
                    else if (element is TextNote)
                    {
                        TextNote textNote = element as TextNote;
                        TextNoteExporter.Export(exporterIFC, textNote, productWrapper, m_PresentationStyleCache);
                    }
                    else if (element is Wall)
                    {
                        Wall wallElem = element as Wall;
                        WallExporter.Export(exporterIFC, wallElem, geomElem, productWrapper);
                    }
                    else if (IsMEPType(exporterIFC, element))
                    {
                        GenericMEPExporter.Export(exporterIFC, element, geomElem, productWrapper);
                    }
                    else if (elem is FilledRegion)
                    {
                        // FilledRegion is still handled by internal Revit code, but the get_Geometry call makes sure
                        // that the Owner view is clean before we get the region's GeometryElement.
                        ExporterIFCUtils.ExportElementInternal(exporterIFC, elem, productWrapper);
                    }

                    st.RollBack();
                }
            }
            finally
            {
                exporterIFC.PopExportState();
            }
        }

        /// <summary>
        /// Exports the element properties.
        /// </summary>
        /// <param name="exporterIFC">The IFC exporter object.</param>
        /// <param name="element ">The element which properties are exported.</param>
        /// <param name="productWrapper">The IFCProductWrapper object.</param>
        internal void ExportElementProperties(ExporterIFC exporterIFC, Element element, IFCProductWrapper productWrapper)
        {
            if (productWrapper.Count == 0)
                return;

            IFCFile ifcFile = exporterIFC.GetFile();
            using (IFCTransaction tr = new IFCTransaction(ifcFile))
            {
                Document doc = element.Document;

                ElementType elemType = doc.get_Element(element.GetTypeId()) as ElementType;

                IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();

                ICollection<IFCAnyHandle> productSet = productWrapper.GetAllObjects();
                IList<IList<IFCPropertySetDescription>> psetsToCreate = m_ParameterCache.PropertySets;

                foreach (IList<IFCPropertySetDescription> currStandard in psetsToCreate)
                {
                    foreach (IFCPropertySetDescription currDesc in currStandard)
                    {
                        foreach (IFCAnyHandle prodHnd in productSet)
                        {
                            if (currDesc.IsAppropriateType(prodHnd))
                            {
                                IFCExtrusionCreationData ifcParams = productWrapper.FindExtrusionCreationParameters(prodHnd);

                                List<IFCAnyHandle> props = new List<IFCAnyHandle>();
                                IList<IFCPropertySetEntry> entries = currDesc.GetEntries();
                                foreach (IFCPropertySetEntry entry in entries)
                                {
                                    props.AddRange(entry.ProcessEntry(ifcFile, exporterIFC, ifcParams, element, elemType));
                                }

                                IFCLabel paramSetName = IFCLabel.Create(currDesc.Name);

                                if (props.Count > 0)
                                {
                                    IFCAnyHandle propertySet = ifcFile.CreatePropertySet(IFCLabel.CreateGUID(), ownerHistory, paramSetName, IFCLabel.Create(), props);
                                    IFCAnyHandle prodHndToUse = prodHnd;
                                    IFCRedirectDescriptionCalculator ifcRDC = currDesc.GetRedirectDescriptionCalculator();
                                    if (ifcRDC != null)
                                    {
                                        IFCAnyHandle overrideHnd = ifcRDC.RedirectDescription(exporterIFC, element);
                                        if (overrideHnd.HasValue)
                                            prodHndToUse = overrideHnd;
                                    }
                                    ifcFile.CreateRelDefinesByProperties(IFCLabel.CreateGUID(), ownerHistory, propertySet, prodHndToUse);
                                }

                            }
                        }
                    }
                }
                tr.Commit();
            }

            if (exporterIFC.ExportAs2x2)
                ExportPsetDraughtingFor2x2(exporterIFC, element, productWrapper);
        }

        /// <summary>
        /// Exports Pset_Draughting for IFC 2x2 standard.
        /// </summary>
        /// <param name="exporterIFC">The IFC exporter object.</param>
        /// <param name="element ">The element which properties are exported.</param>
        /// <param name="productWrapper">The IFCProductWrapper object.</param>
        void ExportPsetDraughtingFor2x2(ExporterIFC exporterIFC, Element element, IFCProductWrapper productWrapper)
        {
            IFCFile ifcFile = exporterIFC.GetFile();
            using (IFCTransaction tr = new IFCTransaction(ifcFile))
            {
                IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();

                string catName = CategoryUtil.GetCategoryName(element);
                Color color = CategoryUtil.GetElementColor(element);


                ICollection<IFCAnyHandle> nameAndColorProps = new Collection<IFCAnyHandle>();

                nameAndColorProps.Add(ifcFile.CreateStringProperty("Layername", catName));

                //color
                {
                    ICollection<IFCAnyHandle> colorProps = new Collection<IFCAnyHandle>();
                    colorProps.Add(ifcFile.CreateIntegerProperty(null, "Red", color.Red));
                    colorProps.Add(ifcFile.CreateIntegerProperty(null, "Green", color.Green));
                    colorProps.Add(ifcFile.CreateIntegerProperty(null, "Blue", color.Blue));

                    nameAndColorProps.Add(ifcFile.CreateComplexProperty("Color", colorProps));
                }

                IFCLabel name = IFCLabel.Create("Pset_Draughting");   // IFC 2x2 standard
                IFCAnyHandle propertySet2 = ifcFile.CreatePropertySet(IFCLabel.CreateGUID(), ownerHistory, name, IFCLabel.Create(), nameAndColorProps);

                ifcFile.CreateRelDefinesByProperties(IFCLabel.CreateGUID(), ownerHistory, propertySet2, productWrapper.GetAllObjects());

                tr.Commit();
            }
        }

        /// <summary>
        /// Exports the IFC element quantities.
        /// </summary>
        /// <param name="exporterIFC">The IFC exporter object.</param>
        /// <param name="element ">The element which quantities are exported.</param>
        /// <param name="productWrapper">The IFCProductWrapper object.</param>
        internal void ExportElementQuantities(ExporterIFC exporterIFC, Element element, IFCProductWrapper productWrapper)
        {
            if (productWrapper.Count == 0)
                return;

            IFCFile ifcFile = exporterIFC.GetFile();
            using (IFCTransaction tr = new IFCTransaction(ifcFile))
            {
                Document doc = element.Document;

                ElementType elemType = doc.get_Element(element.GetTypeId()) as ElementType;

                IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();

                ICollection<IFCAnyHandle> productSet = productWrapper.GetAllObjects();
                IList<IList<IFCQuantityDescription>> quantitiesToCreate = m_ParameterCache.Quantities;

                foreach (IList<IFCQuantityDescription> currStandard in quantitiesToCreate)
                {
                    foreach (IFCQuantityDescription currDesc in currStandard)
                    {
                        foreach (IFCAnyHandle prodHnd in productSet)
                        {
                            if (currDesc.IsAppropriateType(prodHnd))
                            {
                                IFCExtrusionCreationData ifcParams = productWrapper.FindExtrusionCreationParameters(prodHnd);

                                List<IFCAnyHandle> quantities = new List<IFCAnyHandle>();
                                IList<IFCQuantityEntry> entries = currDesc.GetEntries();
                                foreach (IFCQuantityEntry entry in entries)
                                {
                                    quantities.AddRange(entry.ProcessEntry(ifcFile, exporterIFC, ifcParams, element, elemType));
                                }

                                IFCLabel paramSetName = IFCLabel.Create(currDesc.Name);

                                IFCLabel methodName = IFCLabel.Create(currDesc.MethodOfMeasurement);

                                if (quantities.Count > 0)
                                {
                                    IFCAnyHandle propertySet = ifcFile.CreateElementQuantity(IFCLabel.CreateGUID(), ownerHistory, paramSetName, methodName, IFCLabel.Create(), quantities);
                                    IFCAnyHandle prodHndToUse = prodHnd;
                                    IFCRedirectDescriptionCalculator ifcRDC = currDesc.GetRedirectDescriptionCalculator();
                                    if (ifcRDC != null)
                                    {
                                        IFCAnyHandle overrideHnd = ifcRDC.RedirectDescription(exporterIFC, element);
                                        if (overrideHnd.HasValue)
                                            prodHndToUse = overrideHnd;
                                    }
                                    ifcFile.CreateRelDefinesByProperties(IFCLabel.CreateGUID(), ownerHistory, propertySet, prodHndToUse);
                                }
                            }
                        }
                    }
                }
                tr.Commit();
            }
        }
    }
}
