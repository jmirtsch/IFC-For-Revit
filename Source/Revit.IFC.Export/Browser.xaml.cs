using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit.PropertyGrid;

using Autodesk.Revit.UI;

namespace Revit.IFC.Export
{
   /// <summary>
   /// Interaction logic for Browser.xaml
   /// </summary>
   public partial class Browser : Page, IDockablePaneProvider
   {
      public Browser()
      {
         InitializeComponent();
			Menu menu = new Menu();
      }

      public void SetupDockablePane(DockablePaneProviderData data)
      {
         data.FrameworkElement = this as FrameworkElement;
         data.InitialState = new DockablePaneState();
         data.InitialState.DockPosition = DockPosition.Tabbed;
         //DockablePaneId targetPane;
         //if (m_targetGuid == Guid.Empty)
         //    targetPane = null;
         //else targetPane = new DockablePaneId(m_targetGuid);
         //if (m_position == DockPosition.Tabbed)

         data.InitialState.TabBehind = Autodesk.Revit.UI.DockablePanes.BuiltInDockablePanes.ProjectBrowser;
         //if (m_position == DockPosition.Floating)
         //{
         //data.InitialState.SetFloatingRectangle(new Autodesk.Revit.UI.Rectangle(10, 710, 10, 710));
         //data.InitialState.DockPosition = DockPosition.Tabbed;
         //}
         //Log.Message("***Intial docking parameters***");
         //Log.Message(APIUtility.GetDockStateSummary(data.InitialState));
      }

		private void MenuItem_Click(object sender, RoutedEventArgs e)
		{

		}
	}
}
