//
// BIM IFC export alternate UI library: this library works with Autodesk(R) Revit(R) to provide an alternate user interface for the export of IFC files from Revit.
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB.ExtensibleStorage;


namespace Revit.IFC.Common.Extensions
{
    public class IFCClassificationMgr
    {
        private Schema m_schema = null;
        private static Guid s_schemaId = new Guid("2CC3F098-1D06-4771-815D-D39128193A14");

        private const String s_ClassificationName = "ClassificationName";
        private const String s_ClassificationSource = "ClassificationSource";
        private const String s_ClassificationEdition = "ClassificationEdition";
        private const String s_ClassificationEditionDate_Day = "ClassificationEditionDate_Day";
        private const String s_ClassificationEditionDate_Month = "ClassificationEditionDate_Month";
        private const String s_ClassificationEditionDate_Year = "ClassificationEditionDate_Year";
        private const String s_ClassificationLocation = "ClassificationLocation";


        /// <summary>
        /// the IFC Classification Manager
        /// </summary>
        public IFCClassificationMgr()
        {
            if (m_schema == null)
            {
                m_schema = Schema.Lookup(s_schemaId);
            }
            if (m_schema == null)
            {
                SchemaBuilder classificationBuilder = new SchemaBuilder(s_schemaId);
                classificationBuilder.SetSchemaName("IFCClassification");
                classificationBuilder.AddSimpleField(s_ClassificationName, typeof(string));
                classificationBuilder.AddSimpleField(s_ClassificationSource, typeof(string));
                classificationBuilder.AddSimpleField(s_ClassificationEdition, typeof(string));
                classificationBuilder.AddSimpleField(s_ClassificationEditionDate_Day, typeof(Int32));
                classificationBuilder.AddSimpleField(s_ClassificationEditionDate_Month, typeof(Int32));
                classificationBuilder.AddSimpleField(s_ClassificationEditionDate_Year, typeof(Int32));
                classificationBuilder.AddSimpleField(s_ClassificationLocation, typeof(string));
                m_schema = classificationBuilder.Finish();
            }
        }

        /// <summary>
        /// Get IFC Classification Information from the Extensible Storage in Revit document. .
        /// </summary>
        /// <param name="document">The document storing the Classification.</param>
        /// <param name="schema">The schema for storing Classification.</param>
        /// <returns>Returns list of IFC Classification in the storage.</returns>
        private IList<DataStorage> GetClassificationInStorage(Document document, Schema schema)
        {
            FilteredElementCollector collector = new FilteredElementCollector(document);
            collector.OfClass(typeof(DataStorage));
            Func<DataStorage, bool> hasTargetData = ds => (ds.GetEntity(schema) != null && ds.GetEntity(schema).IsValid());

            return collector.Cast<DataStorage>().Where<DataStorage>(hasTargetData).ToList<DataStorage>();
        }

        /// <summary>
        /// Update the IFC Classification (from the UI) into the document 
        /// </summary>
        /// <param name="document">The document storing the saved Classification.</param>
        /// <param name="fileHeaderItem">The Classification item to save.</param>
        public void UpdateClassification (Document document, IFCClassification classification)
        {
            // TO DO: To handle individual item and not the whole since in the future we may support multiple classification systems!!!
            if (m_schema == null)
            {
                m_schema = Schema.Lookup(s_schemaId);
            }
            if (m_schema != null)
            {
                IList<DataStorage> oldSavedClassification = GetClassificationInStorage(document, m_schema);
                if (oldSavedClassification.Count > 0)
                {
                    Transaction deleteTransaction = new Transaction(document, "Delete old IFC Classification");
                    deleteTransaction.Start();
                    List<ElementId> dataStorageToDelete = new List<ElementId>();
                    foreach (DataStorage dataStorage in oldSavedClassification)
                    {
                        dataStorageToDelete.Add(dataStorage.Id);
                    }
                    document.Delete(dataStorageToDelete);
                    deleteTransaction.Commit();
                }
            }

            // Update the address using the new information
            if (m_schema == null)
            {
                m_schema = Schema.Lookup(s_schemaId);
            }
            if (m_schema != null)
            {
                Transaction transaction = new Transaction(document, "Update saved IFC classification");
                transaction.Start();

                DataStorage classificationStorage = DataStorage.Create(document);

                Entity entIFCClassification = new Entity(m_schema);
                if (classification.ClassificationName != null) entIFCClassification.Set<string>(s_ClassificationName, classification.ClassificationName.ToString());
                if (classification.ClassificationSource != null) entIFCClassification.Set<string>(s_ClassificationSource, classification.ClassificationSource.ToString());
                if (classification.ClassificationEdition != null) entIFCClassification.Set<string>(s_ClassificationEdition, classification.ClassificationEdition.ToString());
                if (classification.ClassificationEditionDate != null)
                {
                    entIFCClassification.Set<Int32>(s_ClassificationEditionDate_Day, classification.ClassificationEditionDate.Day);
                    entIFCClassification.Set<Int32>(s_ClassificationEditionDate_Month, classification.ClassificationEditionDate.Month);
                    entIFCClassification.Set<Int32>(s_ClassificationEditionDate_Year, classification.ClassificationEditionDate.Year);
                }
                if (classification.ClassificationLocation != null) entIFCClassification.Set<string>(s_ClassificationLocation, classification.ClassificationLocation.ToString());
                classificationStorage.SetEntity(entIFCClassification);
                transaction.Commit();
            }
        }

        /// <summary>
        /// Get saved IFC Classifications
        /// </summary>
        /// <param name="document">The document where Classification information is stored.</param>
        /// <param name="fileHeader">Output of the saved Classification from the extensible storage.</param>
        /// <returns>Status whether there is existing saved Classification.</returns>
        public bool GetSavedClassificationByName (Document document, string classificationName, out IFCClassification classification)
        {
            IFCClassification ifcClassificationSaved = new IFCClassification();

            if (m_schema == null)
            {
                m_schema = Schema.Lookup(s_schemaId);
            }
            if (m_schema != null)
            {
                IList<DataStorage> classificationStorage = GetClassificationInStorage(document, m_schema);

                for (int noClass=0; noClass < classificationStorage.Count; noClass++)
                {

                    // expected only one File Header information in the storage
                    Entity savedClassification = classificationStorage[noClass].GetEntity(m_schema);

                    ifcClassificationSaved.ClassificationName = savedClassification.Get<string>(m_schema.GetField(s_ClassificationName));
                    // Get the details only if the name matches
                    if (String.Compare(classificationName, ifcClassificationSaved.ClassificationName) == 0)
                    {
                        ifcClassificationSaved.ClassificationSource = savedClassification.Get<string>(m_schema.GetField(s_ClassificationSource));
                        ifcClassificationSaved.ClassificationEdition = savedClassification.Get<string>(m_schema.GetField(s_ClassificationEdition));

                        Int32 cldateDay = savedClassification.Get<Int32>(m_schema.GetField(s_ClassificationEditionDate_Day));
                        Int32 cldateMonth = savedClassification.Get<Int32>(m_schema.GetField(s_ClassificationEditionDate_Month));
                        Int32 cldateYear = savedClassification.Get<Int32>(m_schema.GetField(s_ClassificationEditionDate_Year));
                        ifcClassificationSaved.ClassificationEditionDate = new DateTime(cldateYear, cldateMonth, cldateDay);

                        ifcClassificationSaved.ClassificationLocation = savedClassification.Get<string>(m_schema.GetField(s_ClassificationLocation));

                        classification = ifcClassificationSaved;
                        return true;
                    }
                }
            }

            classification = ifcClassificationSaved;
            return false;
        }

        /// <summary>
        /// Get the List of Classification saved in the Extensible Storage
        /// </summary>
        /// <param name="document"></param>
        /// <param name="classification"></param>
        /// <returns></returns>
        public bool GetSavedClassification(Document document, out List<IFCClassification> classification)
        {
            List<IFCClassification> ifcClassificationSaved = new List<IFCClassification>();
            Boolean ret = false;

            if (m_schema == null)
            {
                m_schema = Schema.Lookup(s_schemaId);
            }
            if (m_schema != null)
            {
                // This section handles multiple definitions of Classifications, but at the moment not in the UI
                IList<DataStorage> classificationStorage = GetClassificationInStorage(document, m_schema);

                for (int noClass = 0; noClass < classificationStorage.Count; noClass++)
                {
                    Entity savedClassification = classificationStorage[noClass].GetEntity(m_schema);

                    ifcClassificationSaved.Add(new IFCClassification());

                    ifcClassificationSaved[noClass].ClassificationName = savedClassification.Get<string>(m_schema.GetField(s_ClassificationName));
                    ifcClassificationSaved[noClass].ClassificationSource = savedClassification.Get<string>(m_schema.GetField(s_ClassificationSource));
                    ifcClassificationSaved[noClass].ClassificationEdition = savedClassification.Get<string>(m_schema.GetField(s_ClassificationEdition));

                    Int32 cldateDay = savedClassification.Get<Int32>(m_schema.GetField(s_ClassificationEditionDate_Day));
                    Int32 cldateMonth = savedClassification.Get<Int32>(m_schema.GetField(s_ClassificationEditionDate_Month));
                    Int32 cldateYear = savedClassification.Get<Int32>(m_schema.GetField(s_ClassificationEditionDate_Year));
                    ifcClassificationSaved[noClass].ClassificationEditionDate = new DateTime(cldateYear, cldateMonth, cldateDay);

                    ifcClassificationSaved[noClass].ClassificationLocation = savedClassification.Get<string>(m_schema.GetField(s_ClassificationLocation));
                    classification = ifcClassificationSaved;
                    ret = true;
                }
            }

            // Create at least one new Classification in the List, otherwise caller may fail
            if (ifcClassificationSaved.Count == 0)
                ifcClassificationSaved.Add(new IFCClassification());

            classification = ifcClassificationSaved;
            return ret;
        }

        /// <summary>
        /// Function to delete the classification in the schema
        /// </summary>
        /// <param name="document"></param>
        public void DeleteClassification(Document document)
        {
            // TO DO: To handle individual item and not the whole since in the future we may support multiple classification systems!!!
            if (m_schema == null)
            {
                m_schema = Schema.Lookup(s_schemaId);
            }
            if (m_schema != null)
            {
                IList<DataStorage> oldSavedClassification = GetClassificationInStorage(document, m_schema);
                if (oldSavedClassification.Count > 0)
                {
                    Transaction deleteTransaction = new Transaction(document, "Delete old IFC Classification");
                    deleteTransaction.Start();
                    List<ElementId> dataStorageToDelete = new List<ElementId>();
                    foreach (DataStorage dataStorage in oldSavedClassification)
                    {
                        dataStorageToDelete.Add(dataStorage.Id);
                    }
                    document.Delete(dataStorageToDelete);
                    deleteTransaction.Commit();
                }
            }
        }
    }
}
