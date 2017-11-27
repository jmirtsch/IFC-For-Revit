using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Export.Exporter.PropertySet.Calculators;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Exporter
{
   class PropertySetsInitialization
   {
      /// <summary>
      /// Initializes common door property sets.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPropertySetDoorCommon(IList<PropertySetDescription> commonPropertySets)
      {
         //property set wall common
         PropertySetDescription propertySetDoorCommon = new PropertySetDescription();
         propertySetDoorCommon.Name = "Pset_DoorCommon";
         propertySetDoorCommon.SubElementIndex = (int)IFCCommonPSets.PSetDoorCommon;

         propertySetDoorCommon.EntityTypes.Add(IFCEntityType.IfcDoor);

         PropertySetEntry ifcPSE = PropertySetEntryUtil.CreateReferenceEntry();
         propertySetDoorCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateIsExternalEntry();
         propertySetDoorCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateFireRatingEntry();
         propertySetDoorCommon.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntryUtil.CreateThermalTransmittanceEntry();
         propertySetDoorCommon.AddEntry(ifcPSE);

         propertySetDoorCommon.AddEntry(PropertySetEntryUtil.CreateAcousticRatingEntry());
         propertySetDoorCommon.AddEntry(PropertySetEntry.CreateLabel("SecurityRating"));
         propertySetDoorCommon.AddEntry(PropertySetEntryUtil.CreateHandicapAccessibleEntry());
         propertySetDoorCommon.AddEntry(PropertySetEntry.CreateBoolean("FireExit"));
         propertySetDoorCommon.AddEntry(PropertySetEntry.CreateBoolean("SelfClosing"));
         propertySetDoorCommon.AddEntry(PropertySetEntry.CreateBoolean("SmokeStop"));
         propertySetDoorCommon.AddEntry(PropertySetEntry.CreateReal("GlazingAreaFraction"));
         propertySetDoorCommon.AddEntry(PropertySetEntry.CreateVolumetricFlowRate("Infiltration"));

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            propertySetDoorCommon.AddEntry(PropertySetEntryUtil.CreateStatusEntry());
            propertySetDoorCommon.AddEntry(PropertySetEntry.CreateLabel("DurabilityRating"));
            propertySetDoorCommon.AddEntry(PropertySetEntry.CreateLabel("HygrothermalRating"));
            propertySetDoorCommon.AddEntry(PropertySetEntry.CreateBoolean("HasDrive"));
         }

         commonPropertySets.Add(propertySetDoorCommon);
      }
   }
}
