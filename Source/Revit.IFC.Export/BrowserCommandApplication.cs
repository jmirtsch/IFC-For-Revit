using System;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;

namespace Revit.IFC.Export.UI
{
   public class PropertyBrowser : IExternalApplication
   {
      private RequestHandler mHandler;
      Browser m_MyDockableWindow = null;
      private ExternalEvent mExEvent;

      public Result OnStartup(UIControlledApplication a)
      {
         DockablePaneProviderData data = new DockablePaneProviderData();
         m_MyDockableWindow = new Browser();
         data.FrameworkElement = m_MyDockableWindow as System.Windows.FrameworkElement;
         data.InitialState = new DockablePaneState();
         data.InitialState.DockPosition = DockPosition.Tabbed;

         DockablePaneId dpid = new DockablePaneId(new Guid("{C7C70722-1B9B-4454-A054-DFD142F23580}"));
         a.RegisterDockablePane(dpid, "IFC Properties", m_MyDockableWindow as IDockablePaneProvider);

        foreach (Autodesk.Windows.RibbonTab tab in Autodesk.Windows.ComponentManager.Ribbon.Tabs)
         {
            if (tab.Id == "Modify")
            {
               tab.PropertyChanged += PanelEvent;
               break;
            }
         }
         //mHandler = new RequestHandler(this);
         mExEvent = ExternalEvent.Create(mHandler);
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

         }
      }

      private void MakeRequest(string json)
      {
         mExEvent.Raise();
      }
   }

   public class RequestHandler : IExternalEventHandler
   {
      private Request mRequest = new Request();
      public Request Request { get { return mRequest; } }

      //private 
      public RequestHandler(Form form)
      {
         //mForm = form;
      }

      public String GetName() { return "BimForce UOB"; }
      public void Execute(UIApplication uiapp)
      {
         try
         {
            ICollection<ElementId> elementIds = uiapp.ActiveUIDocument.Selection.GetElementIds(); 

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

