using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitIFCTools.PropertySet
{
   class ComplexProperty : PropertyDataType
   {
      public string Name { get; set; }
      public IList<PsetProperty> Properties { get; set; }
      public override string ToString()
      {
         string compT = Name;
         foreach (PsetProperty p in Properties)
            compT += "\n" + p.ToString();
         return compT;
      }
   }
}
