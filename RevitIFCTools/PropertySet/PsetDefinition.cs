using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitIFCTools.PropertySet
{
   class PsetDefinition
   {
      public string Name { get; set; }
      public string IfcVersion { get; set; }
      public string IfdGuid { get; set; }
      public IList<string> ApplicableClasses { get; set; }
      public IList<PsetProperty> properties { get; set; }
      public override string ToString()
      {
         string psetDefStr = "";
         psetDefStr = "\nPsetName:\t" + Name
                     + "\nIfcVersion:\t" + IfcVersion;
         if (!string.IsNullOrEmpty(IfdGuid))
            psetDefStr += "\nifdguid:\t" + IfdGuid;
         string appCl = "";
         foreach (string cl in ApplicableClasses)
         {
            if (!string.IsNullOrEmpty(appCl))
               appCl += ", ";
            appCl += cl;
         }
         psetDefStr += "\nApplicableClasses:\t(" + appCl + ")";
         psetDefStr += "\nProperties:";
         foreach (PsetProperty p in properties)
         {
            psetDefStr += p.ToString() + "\n";
         }
         return psetDefStr;
      }
   }
}
