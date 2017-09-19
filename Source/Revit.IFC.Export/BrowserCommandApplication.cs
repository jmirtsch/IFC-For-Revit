using System;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;

using GeometryGym.Ifc;

namespace Revit.IFC.Export.UI
{
   public class PropertyBrowser : IExternalApplication
   {
      private ExternalEvent mExEvent;

      public Result OnStartup(UIControlledApplication a)
      {
         DockablePaneProviderData data = new DockablePaneProviderData();
         Browser browser = new Browser();
         data.FrameworkElement = browser as System.Windows.FrameworkElement;
         data.InitialState = new DockablePaneState();
         data.InitialState.DockPosition = DockPosition.Tabbed;

         DockablePaneId dpid = new DockablePaneId(new Guid("{C7C70722-1B9B-4454-A054-DFD142F23580}"));
         a.RegisterDockablePane(dpid, "IFC Properties", browser as IDockablePaneProvider);

        foreach (Autodesk.Windows.RibbonTab tab in Autodesk.Windows.ComponentManager.Ribbon.Tabs)
         {
            if (tab.Id == "Modify")
            {
               tab.PropertyChanged += PanelEvent;
               break;
            }
         }
         RequestHandler handler = new RequestHandler(browser);
         mExEvent = ExternalEvent.Create(handler);
         return Result.Succeeded;
      }

      public Result OnShutdown(UIControlledApplication a)
      {
         return Result.Succeeded;
      }

      void PanelEvent(object sender, System.ComponentModel.PropertyChangedEventArgs e)
      {
         if (e.PropertyName == "Title")
         {
            mExEvent.Raise();
         }
      }
   }

   public class RequestHandler : IExternalEventHandler
   {
      private Request mRequest = new Request();
      public Request Request { get { return mRequest; } }

      private Browser mBrowser = null;
      public RequestHandler(Browser browser)
      {
         mBrowser = browser;
      }

      public String GetName() { return "IFC Property Browser"; }
      public void Execute(UIApplication uiapp)
      {
         try
         {
            Document document = uiapp.ActiveUIDocument.Document;
            ICollection<ElementId> elementIds = uiapp.ActiveUIDocument.Selection.GetElementIds();
            if(elementIds.Count == 0)
            {
               mBrowser.mPropertyGrid.SelectedObject = null;
               return;
            }
            DatabaseIfc db = new DatabaseIfc(true, ReleaseVersion.IFC4A2);
            IfcBuilding building = new IfcBuilding(db, "Dummy");
            IfcProject project = new IfcProject(building, "Dummy");
            Utility.ExporterCacheManager.ExportOptionsCache = Utility.ExportOptionsCache.Create(null, document, null);
            Utility.ExporterCacheManager.Document = uiapp.ActiveUIDocument.Document;
            Exporter.Exporter exporter = new Revit.IFC.Export.Exporter.Exporter();
            exporter.InitializePropertySets();
            List<IfcProduct> products = new List<IfcProduct>();
            foreach (ElementId elid in elementIds)
            {
               Element e = document.GetElement(elid);
               IfcProduct product = new IfcWall(building, null, null);
               Utility.ExporterUtil.ExportRelatedProperties(e, product);
               HashSet<IfcObjectDefinition> objs = new HashSet<IfcObjectDefinition>();
               objs.Add(product);
               Exporter.PropertySet.PropertyUtil.CreateInternalRevitPropertySets(e, db, objs);
               products.Add(product);

            }
            
            if (products.Count < 1)
               return;
            mBrowser.mPropertyGrid.SelectedObject = products[0];
           
            
         }
         finally
         {

         }
         return;
      }
   }

   public class Request
   {
      //public string Take()
      //{
      //   string str = "";
      //   return Interlocked.Exchange(ref mJson, str);
      //}
      //public void Make(string json)
      //{
      //   Interlocked.Exchange(ref mJson, json);
      //}
   }
   [Transaction(TransactionMode.Manual)]
   public class RegisterDockableWindow : IExternalCommand
   {
      public Result Execute(
     ExternalCommandData commandData,
     ref string message,
     ElementSet elements)
      {
         
         return Result.Succeeded;
      }
   }
}

