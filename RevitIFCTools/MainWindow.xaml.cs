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
using System.Windows.Shapes;

namespace RevitIFCTools
{
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window
   {
      public MainWindow()
      {
         InitializeComponent();
      }

      private void button_GenerateIFCEntityList_Click(object sender, RoutedEventArgs e)
      {
         RevitIFCTools.IFCEntityListWin ifcEntWnd = new RevitIFCTools.IFCEntityListWin();
         ifcEntWnd.ShowDialog();
      }

      private void button_GeneratePsetDefs_Click(object sender, RoutedEventArgs e)
      {
         RevitIFCTools.GeneratePsetDefWin psetWin = new RevitIFCTools.GeneratePsetDefWin();
         psetWin.ShowDialog();
      }

      private void button_Cancel_Click(object sender, RoutedEventArgs e)
      {
         Close();
      }

      private void button_paramexpr_Click(object sender, RoutedEventArgs e)
      {
         RevitIFCTools.ParameterExpr.ExprTester exprTest = new ParameterExpr.ExprTester();
         exprTest.ShowDialog();
      }
   }
}
