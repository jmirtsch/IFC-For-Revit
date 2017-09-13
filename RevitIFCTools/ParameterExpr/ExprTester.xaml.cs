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
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Revit.IFC.Export.Utility;

namespace RevitIFCTools.ParameterExpr
{
   /// <summary>
   /// Interaction logic for ExprTester.xaml
   /// </summary>
   public partial class ExprTester : Window
   {
      public ExprTester()
      {
         InitializeComponent();
      }

      private void button_Parse_Click(object sender, RoutedEventArgs e)
      {
         if (string.IsNullOrEmpty(textBox_Expr.Text))
            return;

         AntlrInputStream input = new AntlrInputStream(textBox_Expr.Text);
         ParamExprGrammarLexer lexer = new ParamExprGrammarLexer(input);
         CommonTokenStream tokens = new CommonTokenStream(lexer);
         ParamExprGrammarParser parser = new ParamExprGrammarParser(tokens);
         parser.RemoveErrorListeners();
         Logger.resetStream();

         parser.AddErrorListener(new ParamExprErrorListener());

         //IParseTree tree = parser.start_rule();
         IParseTree tree = parser.param_expr();
         ParseTreeWalker walker = new ParseTreeWalker();
         EvalListener eval = new EvalListener(parser);

         walker.Walk(eval, tree);

         // BIMRL_output.Text = tree.ToStringTree(parser);
         string toOutput = new string(Logger.getmStreamContent());
         textBox_Output.Text = tree.ToStringTree(parser) + '\n' + toOutput;
      }

      private void button_Close_Click(object sender, RoutedEventArgs e)
      {
         Close();
      }

      private void button_ClearOutput_Click(object sender, RoutedEventArgs e)
      {
         textBox_Output.Clear();
         textBox_Expr.Clear();
      }

      private void button_ClearAll_Click(object sender, RoutedEventArgs e)
      {
         textBox_Output.Clear();
      }
   }
}
