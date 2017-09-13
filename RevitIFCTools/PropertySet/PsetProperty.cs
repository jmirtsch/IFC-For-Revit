using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitIFCTools.PropertySet
{
   class PsetProperty
   {
      public string IfdGuid { get; set; }
      public string Name { get; set; }
      public PropertyDataType PropertyType { get; set; }
      public IList<NameAlias> NameAliases { get; set; }
      public override string ToString()
      {
         string propStr = "\n\tPropertyName:\t" + Name;
         if (!string.IsNullOrEmpty(IfdGuid))
            propStr += "\n\tIfdGuid:\t" + IfdGuid;
         if (NameAliases != null)
         {
            foreach (NameAlias na in NameAliases)
            {
               propStr += "\n\t\tAliases:\tlang: " + na.lang + " :\t" + na.Alias;
            }
         }
         if (PropertyType != null)
            propStr += "\n\tPropertyType:\t" + PropertyType.ToString();
         return propStr;
      }
   }
}
