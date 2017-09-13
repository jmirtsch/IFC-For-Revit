using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitIFCTools.PropertySet
{
   class PropertyEnumItem
   {
      public string EnumItem { get; set; }
      public IList<NameAlias> Aliases { get; set; }
   }
}
