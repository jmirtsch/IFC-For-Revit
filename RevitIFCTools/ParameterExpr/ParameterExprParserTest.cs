using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using IErrorNode = Antlr4.Runtime.Tree.IErrorNode;
using ITerminalNode = Antlr4.Runtime.Tree.ITerminalNode;
using IToken = Antlr4.Runtime.IToken;
using ParserRuleContext = Antlr4.Runtime.ParserRuleContext;
using Revit.IFC.Export.Utility;

namespace RevitIFCTools.ParameterExpr
{
   class EvalListener : ParamExprGrammarBaseListener
   {
      ParamExprGrammarParser parser;

      public EvalListener(ParamExprGrammarParser parser)
      {
         this.parser = parser;
      }

      public string visitMsg
      {
         set;
         get;
      }

      public override void VisitTerminal(ITerminalNode node)
      {
         //visitMsg = "\t{get node [Token: " + this.parser.TokenNames[node.Symbol.Type] + "] : " + node.Symbol.Text + "}\n";
         string nodeName = node.Symbol.ToString();
         visitMsg = "\t{Visiting node: [" + nodeName + "]\n";
         Logger.writeLog(visitMsg);
      }

      public override void EnterEveryRule(ParserRuleContext context)
      {
         Logger.writeLog("{get rule: " + this.parser.RuleNames[context.RuleIndex] + " : " + context.Start.Text + "}\n");
      }

      public override void ExitEveryRule(ParserRuleContext context)
      {
         Logger.writeLog("{end rule: " + this.parser.RuleNames[context.RuleIndex] + " : " + context.Start.Text + "}\n");
      }
   }
}
