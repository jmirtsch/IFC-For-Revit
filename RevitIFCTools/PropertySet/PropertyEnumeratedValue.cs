using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitIFCTools.PropertySet
{
   class PropertyEnumeratedValue : PropertyDataType
   {
      public string Name { get; set; }
      public IList<PropertyEnumItem> EnumDef { get; set; }
      public override string ToString()
      {
         string dt = Name;
         if (EnumDef != null)
         {
            foreach (PropertyEnumItem ei in EnumDef)
            {
               dt += "\n\tEnum:\t" + ei.EnumItem;
               foreach (NameAlias na in ei.Aliases)
               {
                  dt += "\n\t\tAliases:\tlang: " + na.lang + " :\t" + na.Alias;
               }
            }
         }
         return dt;
      }
   }
}
