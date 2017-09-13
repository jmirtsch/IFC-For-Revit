using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;

namespace Revit.IFC.Export.Utility
{
   class ParamExprErrorListener : BaseErrorListener
   {
      public override void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
      {
         string stackList = null;
         IList<string> stack = ((Parser)recognizer).GetRuleInvocationStack();
         stack.Reverse();
         for (int i = 0; i < stack.Count(); i++)
         {
            if (i == 0) stackList = "[";
            stackList = stackList + " " + stack[i];
         }
         stackList = stackList + "]";
         ParamExprLogger.writeLog("\t\t-> rule stack: " + stackList + "\n");
         ParamExprLogger.writeLog("\t\t-> line " + line + ":" + charPositionInLine + " at " + offendingSymbol + ": " + msg + "\n");
      }
   }
}
