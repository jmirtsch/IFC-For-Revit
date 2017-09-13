using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitIFCTools.PropertySet
{
   class PropertyReferenceValue : PropertyDataType
   {
      public string RefEntity { get; set; }
      public override string ToString()
      {
         return RefEntity;
      }
   }
}
