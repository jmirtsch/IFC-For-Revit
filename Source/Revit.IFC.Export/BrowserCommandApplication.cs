using System;
using System.Reflection;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Autodesk.Windows;

using GeometryGym.Ifc;

namespace Revit.IFC.Export.UI
{
   public class PropertyBrowser : IExternalApplication
   {
      private ExternalEvent mExEvent;
      internal static DockablePaneId mPropertyPanel = null;
      public Result OnStartup(UIControlledApplication a)
      {
         string tabName = "IFC";
         RibbonControl myRibbon = ComponentManager.Ribbon;
         RibbonTab ggTab = null;

         foreach (RibbonTab tab in myRibbon.Tabs)
         {
            if (string.Compare(tab.Id, tabName, true) == 0)
            {
               ggTab = tab;
               break;
            }
         }
         if (ggTab == null)
            a.CreateRibbonTab(tabName);
         Autodesk.Revit.UI.RibbonPanel rp = a.CreateRibbonPanel(tabName, "Browser");
         PushButtonData pbd = new PushButtonData("propBrowser", "Ifc Property Browser", Assembly.GetExecutingAssembly().Location, "Revit.IFC.Export.UI.ShowBrowser");
         pbd.ToolTip = "Show Property Browser";

         rp.AddItem(pbd);
         DockablePaneProviderData data = new DockablePaneProviderData();
         Browser browser = new Browser();
         data.FrameworkElement = browser as System.Windows.FrameworkElement;
         data.InitialState = new DockablePaneState();
         data.InitialState.DockPosition = DockPosition.Tabbed;

         mPropertyPanel = new DockablePaneId(new Guid("{C7C70722-1B9B-4454-A054-DFD142F23580}"));
         a.RegisterDockablePane(mPropertyPanel, "IFC Properties", browser as IDockablePaneProvider);


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

      void PanelEvent(object sender, PropertyChangedEventArgs e)
      {
         if (e.PropertyName == "Title")
         {
            mExEvent.Raise();
         }
      }
   }
   [Transaction(TransactionMode.Manual)]
   public class ShowBrowser : IExternalCommand
   {
      public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
      {
         DockablePane dp = commandData.Application.GetDockablePane(PropertyBrowser.mPropertyPanel);
         dp.Show();
         return Result.Succeeded;
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
            if (elementIds.Count == 0)
            {
               mBrowser.mPropertyGrid.SelectedObject = null;
               return;
            }
            DatabaseIfc db = new DatabaseIfc(true, ReleaseVersion.IFC4A2);
            IfcBuilding building = new IfcBuilding(db, "Dummy");
            IfcProject project = new IfcProject(building, "Dummy");
            Utility.ExporterCacheManager.ExportOptionsCache = Utility.ExportOptionsCache.Create(null, document, null);
            Utility.ExporterCacheManager.Document = uiapp.ActiveUIDocument.Document;
            Exporter.Exporter exporter = new Exporter.Exporter();
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
            {
               mBrowser.mPropertyGrid.SelectedObject = null;
               return;
            }
            mBrowser.mPropertyGrid.SelectedObject = new ProductPropertyGridAdapter(products[0]);


         }
         finally
         {

         }
         return;
      }
   }
   [DisplayName("Ifc Properties")]
   class ProductPropertyGridAdapter : ICustomTypeDescriptor
   {
      private IfcProduct mProduct = null;

      public ProductPropertyGridAdapter(IfcProduct product)
      {
         mProduct = product;
         
      }

      public string GetComponentName()
      {
         return TypeDescriptor.GetComponentName(mProduct, true);
      }

      public EventDescriptor GetDefaultEvent()
      {
         return TypeDescriptor.GetDefaultEvent(this, true);
      }
      
      public string GetClassName()
      {
         return TypeDescriptor.GetClassName(mProduct, true);
      }

      public EventDescriptorCollection GetEvents(Attribute[] attributes)
      {
         return TypeDescriptor.GetEvents(this, attributes, true);
      }

      EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
      {
         return TypeDescriptor.GetEvents(this, true);
      }

      public TypeConverter GetConverter()
      {
         return TypeDescriptor.GetConverter(this, true);
      }

      public object GetPropertyOwner(PropertyDescriptor pd)
      {
         return this;
      }

      public AttributeCollection GetAttributes()
      {
         return TypeDescriptor.GetAttributes(this, true);
      }

      public object GetEditor(Type editorBaseType)
      {
         return TypeDescriptor.GetEditor(this, editorBaseType, true);
      }

      public PropertyDescriptor GetDefaultProperty()
      {
         return null;
      }

      PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
      {
         return ((ICustomTypeDescriptor)this).GetProperties(new Attribute[0]);
      }

      public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
      {
         List<PropertyDescriptor> properties = new List<PropertyDescriptor>();

         foreach (IfcRelDefinesByProperties rdp in mProduct.IsDefinedBy)
         {
            IfcPropertySetDefinition psetdef = rdp.RelatingPropertyDefinition;
            IfcPropertySet pset = psetdef as IfcPropertySet;
            if (pset != null)
            {
               foreach (IfcProperty property in pset.HasProperties.Values)
               {
                  IfcPropertySingleValue psv = property as IfcPropertySingleValue;
                  if (psv != null)
                  {
                     IfcValue value = psv.NominalValue;
                     if (value != null)
                     {
                        properties.Add(new PropertySingleValueDescriptor(psv, psetdef.Name));
                     }
                  }
               }
            }
         }
         return new PropertyDescriptorCollection(properties.ToArray());
      }
   }

   public class PropertySingleValueDescriptor : PropertyDescriptor
   {
      protected IfcPropertySingleValue mProperty = null;
      protected string mCategory = "";

      internal PropertySingleValueDescriptor(IfcPropertySingleValue psv, string category)
          : base(psv.Name, null)
      {
         mProperty = psv;
         mCategory = category;
      }

      public override bool IsReadOnly
      {
         get { return false; }
      }

      public override Type ComponentType
      {
         get { return null; }
      }

      public override bool CanResetValue(object component)
      {
         return false;
      }

      public override void ResetValue(object component)
      {
      }

      public override bool ShouldSerializeValue(object component)
      {
         return false;
      }
      public override string Category => mCategory;
      public override string Description => mProperty.Description;
      public override object GetValue(object component)
      {
         return mProperty.NominalValue.Value;
      }
      public override Type PropertyType { get { return mProperty.NominalValue.ValueType; } }
      public override void SetValue(object component, object value)
      {
         mProperty.NominalValue.Value = value;
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

