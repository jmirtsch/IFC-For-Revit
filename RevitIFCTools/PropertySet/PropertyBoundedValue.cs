using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitIFCTools.PropertySet
{
   class PropertyBoundedValue : PropertyDataType
   {
      public string DataType { get; set; }
      public override string ToString()
      {
         return DataType;
      }
   }
}
