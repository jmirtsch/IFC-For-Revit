//
// BIM IFC export alternate UI library: this library works with Autodesk(R) Revit(R) to provide an alternate user interface for the export of IFC files from Revit.
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
        // old schema
        private Schema m_schemaV1 = null;
        private static Guid s_schemaIdV1 = new Guid("2CC3F098-1D06-4771-815D-D39128193A14");
        
        private Schema m_schema = null;
        private static Guid s_schemaId = new Guid("9A5A28C2-DDAC-4828-8B8A-3EE97118017A");

        private const String s_ClassificationName = "ClassificationName";
        private const String s_ClassificationSource = "ClassificationSource";
        private const String s_ClassificationEdition = "ClassificationEdition";
        private const String s_ClassificationEditionDate_Day = "ClassificationEditionDate_Day";
        private const String s_ClassificationEditionDate_Month = "ClassificationEditionDate_Month";
        private const String s_ClassificationEditionDate_Year = "ClassificationEditionDate_Year";
        private const String s_ClassificationLocation = "ClassificationLocation";
        // Not in v1.
        private const String s_ClassificationFieldName = "ClassificationFieldName";

        private Schema Schema
        {
            get
            {
                if (m_schema == null)
                    m_schema = Schema.Lookup(s_schemaId);
                return m_schema;
            }
        }

        private Schema SchemaV1
        {
            get
            {
                if (m_schemaV1 == null)
                    m_schemaV1 = Schema.Lookup(s_schemaIdV1);
                return m_schemaV1;
            }
        }

        /// <summary>
        /// the IFC Classification Manager
        /// </summary>
        /// <param name="document">The document.</param>
        public IFCClassificationMgr(Document document)
        {
            if (Schema == null)
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
                classificationBuilder.AddSimpleField(s_ClassificationFieldName, typeof(string));
                m_schema = classificationBuilder.Finish();
            }

            // Potentially delete obsolete schema.
            if (SchemaV1 != null)
            {
                IList<IFCClassification> classifications;
                bool hasOldClassifications = GetSavedClassifications(document, SchemaV1, out classifications);
                if (hasOldClassifications)
                {
                    Transaction transaction = new Transaction(document, "Upgrade saved IFC classification");
                    transaction.Start();

                    IList<DataStorage> oldSchemaData = GetClassificationInStorage(document, SchemaV1);
                    IList<ElementId> oldDataToDelete = new List<ElementId>();

                    foreach (DataStorage oldData in oldSchemaData)
                    {
                        Entity savedClassification = oldData.GetEntity(SchemaV1);

                        DataStorage classificationStorage = DataStorage.Create(document);
                        Entity entIFCClassification = new Entity(Schema);
                        string classificationName = savedClassification.Get<string>(s_ClassificationName);
                        if (classificationName != null) entIFCClassification.Set<string>(s_ClassificationName, classificationName);

                        string classificationSource = savedClassification.Get<string>(s_ClassificationSource);
                        if (classificationSource != null) entIFCClassification.Set<string>(s_ClassificationSource, classificationSource);

                        string classificationEdition = savedClassification.Get<string>(s_ClassificationEdition);
                        if (classificationEdition != null) entIFCClassification.Set<string>(s_ClassificationEdition, classificationEdition);

                        Int32 classificationEditionDateDay = savedClassification.Get<Int32>(s_ClassificationEditionDate_Day);
                        Int32 classificationEditionDateMonth = savedClassification.Get<Int32>(s_ClassificationEditionDate_Month);
                        Int32 classificationEditionDateYear = savedClassification.Get<Int32>(s_ClassificationEditionDate_Year);

                        entIFCClassification.Set<Int32>(s_ClassificationEditionDate_Day, classificationEditionDateDay);
                        entIFCClassification.Set<Int32>(s_ClassificationEditionDate_Month, classificationEditionDateMonth);
                        entIFCClassification.Set<Int32>(s_ClassificationEditionDate_Year, classificationEditionDateYear);

                        string classificationLocation = savedClassification.Get<string>(s_ClassificationLocation);
                        if (classificationLocation != null) entIFCClassification.Set<string>(s_ClassificationLocation, classificationLocation);
                        
                        classificationStorage.SetEntity(entIFCClassification);
                        oldDataToDelete.Add(oldData.Id);
                    }

                    if (oldDataToDelete.Count > 0)
                        document.Delete(oldDataToDelete);
                    transaction.Commit();
                }
            }
        }

        /// <summary>
        /// Get IFC Classification Information from the Extensible Storage in Revit document.
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
        /// Update the IFC Classification (from the UI) into the document.
        /// </summary>
        /// <param name="document">The document storing the saved Classification.</param>
        /// <param name="fileHeaderItem">The Classification item to save.</param>
        public void UpdateClassification(Document document, IFCClassification classification)
        {
            // TO DO: To handle individual item and not the whole since in the future we may support multiple classification systems!!!
            if (Schema != null)
            {
                IList<DataStorage> oldSavedClassification = GetClassificationInStorage(document, Schema);
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
            if (Schema != null)
            {
                Transaction transaction = new Transaction(document, "Update saved IFC classification");
                transaction.Start();

                DataStorage classificationStorage = DataStorage.Create(document);

                Entity entIFCClassification = new Entity(Schema);
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
                if (classification.ClassificationFieldName != null) entIFCClassification.Set<string>(s_ClassificationFieldName, classification.ClassificationFieldName.ToString());
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

            if (Schema != null)
            {
                IList<DataStorage> classificationStorage = GetClassificationInStorage(document, Schema);

                for (int noClass=0; noClass < classificationStorage.Count; noClass++)
                {
                    // expected only one File Header information in the storage
                    Entity savedClassification = classificationStorage[noClass].GetEntity(Schema);

                    ifcClassificationSaved.ClassificationName = savedClassification.Get<string>(Schema.GetField(s_ClassificationName));
                    // Get the details only if the name matches
                    if (String.Compare(classificationName, ifcClassificationSaved.ClassificationName) == 0)
                    {
                        ifcClassificationSaved.ClassificationSource = savedClassification.Get<string>(Schema.GetField(s_ClassificationSource));
                        ifcClassificationSaved.ClassificationEdition = savedClassification.Get<string>(Schema.GetField(s_ClassificationEdition));

                        Int32 cldateDay = savedClassification.Get<Int32>(Schema.GetField(s_ClassificationEditionDate_Day));
                        Int32 cldateMonth = savedClassification.Get<Int32>(Schema.GetField(s_ClassificationEditionDate_Month));
                        Int32 cldateYear = savedClassification.Get<Int32>(Schema.GetField(s_ClassificationEditionDate_Year));
                        ifcClassificationSaved.ClassificationEditionDate = new DateTime(cldateYear, cldateMonth, cldateDay);

                        ifcClassificationSaved.ClassificationLocation = savedClassification.Get<string>(Schema.GetField(s_ClassificationLocation));

                        ifcClassificationSaved.ClassificationFieldName = savedClassification.Get<string>(Schema.GetField(s_ClassificationFieldName));
                        
                        classification = ifcClassificationSaved;
                        return true;
                    }
                }
            }

            classification = ifcClassificationSaved;
            return false;
        }

        /// <summary>
        /// Get the List of Classifications saved in the Extensible Storage
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="schema">The schema.  If null is passed in, the default schema is used.</param>
        /// <param name="classifications">The list of classifications.</param>
        /// <returns>True if any classifications were found.<returns>
        public bool GetSavedClassifications(Document document, Schema schema, out IList<IFCClassification> classifications)
        {
            IList<IFCClassification> ifcClassificationSaved = new List<IFCClassification>();
            Boolean ret = false;

            if (schema == null)
                schema = Schema;

            if (schema != null)
            {
                // This section handles multiple definitions of Classifications, but at the moment not in the UI
                IList<DataStorage> classificationStorage = GetClassificationInStorage(document, schema);

                for (int noClass = 0; noClass < classificationStorage.Count; noClass++)
                {
                    Entity savedClassification = classificationStorage[noClass].GetEntity(schema);

                    ifcClassificationSaved.Add(new IFCClassification());

                    ifcClassificationSaved[noClass].ClassificationName = savedClassification.Get<string>(schema.GetField(s_ClassificationName));
                    ifcClassificationSaved[noClass].ClassificationSource = savedClassification.Get<string>(schema.GetField(s_ClassificationSource));
                    ifcClassificationSaved[noClass].ClassificationEdition = savedClassification.Get<string>(schema.GetField(s_ClassificationEdition));

                    Int32 cldateDay = savedClassification.Get<Int32>(schema.GetField(s_ClassificationEditionDate_Day));
                    Int32 cldateMonth = savedClassification.Get<Int32>(schema.GetField(s_ClassificationEditionDate_Month));
                    Int32 cldateYear = savedClassification.Get<Int32>(schema.GetField(s_ClassificationEditionDate_Year));
                    try
                    {
                        ifcClassificationSaved[noClass].ClassificationEditionDate = new DateTime(cldateYear, cldateMonth, cldateDay);
                    }
                    catch
                    {
                        ifcClassificationSaved[noClass].ClassificationEditionDate = DateTime.Now;
                    }

                    ifcClassificationSaved[noClass].ClassificationLocation = savedClassification.Get<string>(schema.GetField(s_ClassificationLocation));
                    // Only for newest schema.
                    if (schema != SchemaV1)
                        ifcClassificationSaved[noClass].ClassificationFieldName = savedClassification.Get<string>(schema.GetField(s_ClassificationFieldName));
                    ret = true;
                }
            }

            // Create at least one new Classification in the List, otherwise caller may fail
            if (ifcClassificationSaved.Count == 0)
                ifcClassificationSaved.Add(new IFCClassification());

            classifications = ifcClassificationSaved;
            return ret;
        }

        /// <summary>
        /// Function to delete the classification in the schema
        /// </summary>
        /// <param name="document"></param>
        public void DeleteClassification(Document document)
        {
            // TO DO: To handle individual item and not the whole since in the future we may support multiple classification systems!!!
            if (Schema != null)
            {
                IList<DataStorage> oldSavedClassification = GetClassificationInStorage(document, Schema);
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
