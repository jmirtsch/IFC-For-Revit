using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitIFCTools.PropertySet
{
   class PropertyTableValue : PropertyDataType
   {
      public string Expression { get; set; }
      public string DefiningValueType { get; set; }
      public string DefinedValueType { get; set; }
      public override string ToString()
      {
         string tv = "";
         if (!string.IsNullOrEmpty(Expression))
         {
            tv += "Expression:\t" + Expression;
         }
         tv += "\nDefiningValueType:\t" + DefiningValueType;
         tv += "\nDefinedValueType:\t" + DefinedValueType;
         return tv;
      }
   }
}
