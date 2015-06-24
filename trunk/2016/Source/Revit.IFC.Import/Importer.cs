﻿//
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
using System.IO;
using System.Collections.Generic;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExternalService;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Import.Data;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Utility;
using IFCImportOptions = Revit.IFC.Import.Utility.IFCImportOptions;

namespace Revit.IFC.Import
{
   /// <summary>
   /// This class implements the method of interface IExternalDBApplication to register the IFC import client to Autodesk Revit.
   /// </summary>
   class ImporterApplication : IExternalDBApplication
   {
      #region IExternalDBApplication Members

      public static Autodesk.Revit.ApplicationServices.ControlledApplication RevitApplication { get; protected set; }

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
         // As an ExternalServer, the importer cannot be registered until full application initialization. Setup an event callback to do this
         // at the appropriate time.
         RevitApplication = application;
         application.ApplicationInitialized += OnApplicationInitialized;
         return ExternalDBApplicationResult.Succeeded;
      }

      #endregion

      /// <summary>
      /// The action taken on application initialization.
      /// </summary>
      /// <param name="sender">The sender.</param>
      /// <param name="eventArgs">The event args.</param>
      private void OnApplicationInitialized(object sender, EventArgs eventArgs)
      {
         SingleServerService service = ExternalServiceRegistry.GetService(ExternalServices.BuiltInExternalServices.IFCImporterService) as SingleServerService;
         if (service != null)
         {
            Importer importer = new Importer();
            service.AddServer(importer);
            service.SetActiveServer(importer.GetServerId());
         }
         else
            throw new InvalidOperationException("Failed to get IFC importer service.");
      }
   }

   /// <summary>
   /// This class implements the method of interface IIFCImporterServer to perform an import from IFC. 
   /// </summary>
   class Importer : IIFCImporterServer
   {
      #region IIFCImporterServer Members

      static Importer m_TheImporter = null;

      IFCImportOptions m_ImportOptions = null;

      IFCImportCache m_ImportCache = null;

      IFCImportLog m_ImportLog = null;

      private static HashSet<FailureDefinitionId> m_ImportPostedErrors = null;

      /// <summary>
      /// Add an error or warning that will be posted to a document in a future link transaction.
      /// </summary>
      /// <param name="failureDefinitionEnum">The error type.</param>
      public static void AddDelayedLinkError(FailureDefinitionId failureDefinitionId)
      {
         if (m_ImportPostedErrors == null)
            m_ImportPostedErrors = new HashSet<FailureDefinitionId>();

         m_ImportPostedErrors.Add(failureDefinitionId);
      }

      /// <summary>
      /// Post any delayed errors or warnings to the current document.
      /// </summary>
      /// <remarks>Needs to occur inside of a transaction.</remarks>
      public static void PostDelayedLinkErrors(Document doc)
      {
         if (m_ImportPostedErrors == null)
            return;

         try
         {
            foreach (FailureDefinitionId failureDefId in m_ImportPostedErrors)
            {
               FailureMessage fm = new FailureMessage(failureDefId);
               doc.PostFailure(fm);
            }
         }
         catch
         {
         }
         finally
         {
            m_ImportPostedErrors = null;
         }
      }

      /// <summary>
      /// The one  Importer class for this import process.
      /// </summary>
      static public Importer TheImporter
      {
         get { return m_TheImporter; }
         protected set { m_TheImporter = value; }
      }

      /// <summary>
      /// The Import cache used for this import process.
      /// </summary>
      static public IFCImportCache TheCache
      {
         get { return m_TheImporter.m_ImportCache; }
      }

      /// <summary>
      /// The log file used for this import process.
      /// </summary>
      static public IFCImportLog TheLog
      {
         get { return m_TheImporter.m_ImportLog; }
      }

      static IFCImportOptions m_TheOptions = null;

      /// <summary>
      /// The IFC import options used for this import process.
      /// </summary>
      public static IFCImportOptions TheOptions
      {
         get { return m_TheOptions; }
         protected set { m_TheOptions = value; }
      }

      private Document LoadLinkDoument(Document originalDocument, string linkedFileName)
      {
         if (!File.Exists(linkedFileName))
            return null;

         Application application = originalDocument.Application;

         // We won't catch any exceptions here, yet.
         // There could be a number of reasons why this fails, to be investigated.
         return application.OpenDocumentFile(linkedFileName);
      }

      private Document CreateLinkDocument(Document originalDocument)
      {
         Document ifcDocument = null;

         // We will attempt to create a new document up to two times:
         // 1st attempt: using IFC project template file.
         // 2nd attempt: using default project template file.
         Application application = originalDocument.Application;

         string defaultIFCTemplate = application.DefaultIFCProjectTemplate;
         string defaultProjectTemplate = application.DefaultProjectTemplate;

         // We can't use the IFC template if it doesn't exist on disk.
         bool noIFCTemplate = (string.IsNullOrEmpty(defaultIFCTemplate) || !File.Exists(defaultIFCTemplate));
         bool noProjectTemplate = (string.IsNullOrEmpty(defaultProjectTemplate) || !File.Exists(defaultProjectTemplate));
         bool noTemplate = (noIFCTemplate && noProjectTemplate);
         if (noTemplate)
         {
            Importer.TheLog.LogError(-1,
                "Both the IFC template file given in the IFC options and the default project template file listed below are either not given, or not found.  Creating the cache file with no template instead.<br>(1) IFC template: " +
                defaultIFCTemplate + "<br>(2) Default project template: " + defaultProjectTemplate, false);
            return null;
         }

         string defaultTemplate = noIFCTemplate ? defaultProjectTemplate : defaultIFCTemplate;

         // templatesDifferent returns false if there is no IFC template; it only returns true if there are 2 potential templates to use, and
         // they are different.
         bool templatesDifferent = noIFCTemplate ? false : (string.Compare(defaultTemplate, defaultProjectTemplate, true) != 0);
         bool canUseDefault = templatesDifferent;

         while (ifcDocument == null)
         {
            try
            {
               ifcDocument = application.NewProjectDocument(defaultTemplate);
            }
            catch
            {
               ifcDocument = null;

               if (canUseDefault)
               {
                  defaultTemplate = defaultProjectTemplate;
                  canUseDefault = false;
                  continue;
               }
               else
               {
                  string projectFilesUsed = templatesDifferent ? defaultIFCTemplate + ", " + defaultProjectTemplate : defaultTemplate;

                  if (string.Compare(defaultTemplate, defaultProjectTemplate, true) != 0)
                     projectFilesUsed += ", " + defaultProjectTemplate;
                  Importer.TheLog.LogError(-1, "Can't open template file(s) " + projectFilesUsed + " to create link document, aborting import.", false);
                  return null;
               }
            }

            break;
         }

         return ifcDocument;
      }

      private Document LoadOrCreateLinkDocument(Document originalDocument, string linkedFileName)
      {
         Document ifcDocument = null;

         try
         {
            // Check to see if the Revit file already exists; if so, we will re-use it.
            ifcDocument = LoadLinkDoument(originalDocument, linkedFileName);

            // If it doesn't exist, create a new document.
            if (ifcDocument == null)
               ifcDocument = CreateLinkDocument(originalDocument);
         }
         catch
         {
            return null;
         }

         return ifcDocument;
      }

      /// <summary>
      /// Retuns the GUID associated with the importer, for DirectShape creation.
      /// </summary>
      static public string ImportAppGUID()
      {
         return "88743F28-A2E1-4935-949D-4DB7A724A150";
      }

      /// <summary>
      /// Quick reject based on IFC file info and existence of Revit file.
      /// </summary>
      /// <param name="ifcFileName">The IFC file name.</param>
      /// <param name="revitFileName">The Revit cached file name.</param>
      /// <returns>True if we need a reload; false if nothing has changed.</returns>
      private bool NeedsReload(string ifcFileName, string revitFileName)
      {
         // If the RVT file doesn't exist, we'll reload.  Otherwise, look at saved file size and timestamp.
         if (!File.Exists(revitFileName))
            return true;

         FileInfo infoIFC = null;
         try
         {
            infoIFC = new FileInfo(ifcFileName);
         }
         catch
         {
            return true;
         }

         long ifcFileLength = infoIFC.Length;
         if ((TheOptions.OriginalFileSize != 0) && (ifcFileLength != TheOptions.OriginalFileSize))
            return true;

         // Ignore ticks - only needs to be accurate to the second, or 10,000,000 ticks.
         Int64 diffTicks = infoIFC.LastWriteTimeUtc.Ticks - TheOptions.OriginalTimeStamp.Ticks;
         if (diffTicks < 0 || diffTicks >= 10000000)
            return true;

         return false;
      }

      private bool DocumentUpToDate(Document doc, string ifcFileName)
      {
         FileInfo infoIFC = null;
         try
         {
            infoIFC = new FileInfo(ifcFileName);
         }
         catch
         {
            return false;
         }

         ProjectInfo projInfo = doc.ProjectInformation;
         if (projInfo == null)
            return false;

         Parameter originalFileName = projInfo.LookupParameter("Original IFC File Name");
         if (originalFileName == null || originalFileName.StorageType != StorageType.String)
            return false;

         Parameter originalFileSizeParam = projInfo.LookupParameter("Original IFC File Size");
         if (originalFileSizeParam == null || originalFileSizeParam.StorageType != StorageType.String)
            return false;

         Parameter revitImporterVersion = projInfo.LookupParameter("Revit Importer Version");
         if (revitImporterVersion == null || revitImporterVersion.StorageType != StorageType.String)
            return false;

         // Stored as string to contain Int64 value
         Parameter originalTimeStampParam = projInfo.LookupParameter("Revit File Last Updated");
         if (originalTimeStampParam == null || originalTimeStampParam.StorageType != StorageType.String)
            return false;

         if (string.Compare(originalFileName.AsString(), ifcFileName, true) != 0)
            return false;

         Int64 originalTimeStampInTicks = 0;
         try
         {
            originalTimeStampInTicks = Int64.Parse(originalTimeStampParam.AsString());
         }
         catch
         {
            return false;
         }

         long originalFileSize = 0;
         try
         {
            originalFileSize = long.Parse(originalFileSizeParam.AsString());
         }
         catch
         {
            return false;
         }

         long ifcFileLength = infoIFC.Length;
         if ((originalFileSize != 0) && (ifcFileLength != originalFileSize))
            return false;

         // Ignore ticks - only needs to be accurate to the second, or 10,000,000 ticks.
         Int64 diffTicks = infoIFC.LastWriteTimeUtc.Ticks - originalTimeStampInTicks;
         if (diffTicks < 0 || diffTicks >= 10000000)
            return false;

         // If the importer has been updated, update the cached file also.
         if (string.Compare(revitImporterVersion.AsString(), IFCImportOptions.ImporterVersion, true) != 0)
            return false;

         return true;
      }

      /// <summary>
      /// Import an IFC file into a given document for Reference only.
      /// </summary>
      /// <param name="document">The host document for the import.</param>
      /// <param name="fullFileName">The full file name of the document.</param>
      /// <param name="options">The list of configurable options for this import.</param>
      public void ReferenceIFC(Document document, string fullFileName, IDictionary<String, String> options)
      {
         // An early check, based on the options set - if we are allowed to use an up-to-date existing file on disk, use it.
         m_ImportLog = IFCImportLog.CreateLog(fullFileName, "log.html");

         Document originalDocument = document;
         Document ifcDocument = null;

         if (TheOptions.Action == IFCImportAction.Link)
         {
            string linkedFileName = IFCImportFile.GetRevitFileName(fullFileName);

            ifcDocument = LoadOrCreateLinkDocument(originalDocument, linkedFileName);
            if (ifcDocument == null)
               return;
         }
         else
            ifcDocument = originalDocument;

         bool useCachedRevitFile = DocumentUpToDate(ifcDocument, fullFileName);

         // In the case where the document is already opened as a link, but it has been updated on disk,
         // give the user a warning and use the cached value.
         if (!useCachedRevitFile && ifcDocument.IsLinked)
         {
            useCachedRevitFile = true;
            Importer.AddDelayedLinkError(BuiltInFailures.ImportFailures.IFCCantUpdateLinkedFile);
         }

         if (!useCachedRevitFile)
         {
            m_ImportCache = IFCImportCache.Create(ifcDocument, fullFileName);

            // Limit creating the cache to Link, but may either remove limiting or make it more restrict (reload only) later.
            if (TheOptions.Action == IFCImportAction.Link)
               TheCache.CreateExistingElementMaps(ifcDocument);

            // TheFile will contain the same value as the return value for this function.
            IFCImportFile.Create(fullFileName, m_ImportOptions, ifcDocument);
         }

         if (useCachedRevitFile || IFCImportFile.TheFile != null)
         {
            IFCImportFile theFile = IFCImportFile.TheFile;
            if (theFile != null)
            {
               if (theFile.IFCProject != null)
                  IFCObjectDefinition.CreateElement(ifcDocument, theFile.IFCProject);

               // Also process any other entities to create.
               foreach (IFCObjectDefinition objDef in IFCImportFile.TheFile.OtherEntitiesToCreate)
                  IFCObjectDefinition.CreateElement(ifcDocument, objDef);

               theFile.EndImport(ifcDocument, fullFileName);
            }

            if (TheOptions.Action == IFCImportAction.Link)
            {
               // If we have an original Revit link file name, don't create a new RevitLinkType - 
               // we will use the existing one.
               bool useExistingType = (TheOptions.RevitLinkFileName != null);
               ElementId revitLinkTypeId = IFCImportFile.LinkInFile(fullFileName, ifcDocument, originalDocument, useExistingType, !useCachedRevitFile);
            }
         }

         if (m_ImportCache != null)
            m_ImportCache.Reset(ifcDocument);
      }

      /// <summary>
      /// The main entry point into the .NET IFC import code
      /// </summary>
      /// <param name="importer">The internal ImporterIFC class that contains information necessary for the import process.</param>
      public void ImportIFC(ImporterIFC importer)
      {
         TheImporter = this;

         IDictionary<String, String> options = importer.GetOptions();
         TheOptions = m_ImportOptions = IFCImportOptions.Create(options);

         // An early check, based on the options set - if we are allowed to use an up-to-date existing file on disk, use it.
         try
         {
            string fullFileName = importer.FullFileName;

            string revitFileName = IFCImportFile.GetRevitFileName(fullFileName);
            if (!TheOptions.ForceImport && !NeedsReload(fullFileName, revitFileName))
               return;

            // Clear the category mapping table, to force reload of options.
            IFCCategoryUtil.Clear();

            if (TheOptions.Intent != IFCImportIntent.Reference)
            {
               IFCImportFile.Import(importer);
            }
            else
            {
               ReferenceIFC(importer.Document, fullFileName, options);
            }
         }
         catch (Exception ex)
         {
            if (Importer.TheLog != null)
               Importer.TheLog.LogError(-1, ex.Message, false);
         }
         finally
         {
            if (Importer.TheLog != null)
               Importer.TheLog.Close();
            if (IFCImportFile.TheFile != null)
               IFCImportFile.TheFile.Close();
         }
      }

      #endregion

      #region IExternalServer Members

      public string GetDescription()
      {
         return "IFC open source importer";
      }

      public string GetName()
      {
         return "IFC importer";
      }

      public System.Guid GetServerId()
      {
         return new Guid("88743F28-A2E1-4935-949D-4DB7A724A150");
      }

      public Autodesk.Revit.DB.ExternalService.ExternalServiceId GetServiceId()
      {
         return Autodesk.Revit.DB.ExternalService.ExternalServices.BuiltInExternalServices.IFCImporterService;
      }

      public string GetVendorId()
      {
         return "IFCX";
      }

      #endregion
   }
}
