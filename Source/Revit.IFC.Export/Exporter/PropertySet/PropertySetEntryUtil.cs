//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2013  Autodesk, Inc.
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Exporter.PropertySet.Calculators;

namespace Revit.IFC.Export.Exporter.PropertySet
{
    /// <summary>
    /// Provides static methods to create varies IFC PropertySetEntries.
    /// </summary>
    public class PropertySetEntryUtil
    {
        /// <summary>
        /// Create the PropertySetEntry for "AcousticRating", which is used by many property sets.
        /// </summary>
        /// <returns>The correct PropertySetEntry.</returns>
        public static PropertySetEntry CreateAcousticRatingEntry()
        {
			string name = "AcousticRating";
            PropertySetEntryMap ifcPSE = new PropertySetEntryMap(name);
            ifcPSE.AddLocalizedParameterName(LanguageType.Chinese_Simplified, "隔音等级");
            ifcPSE.AddLocalizedParameterName(LanguageType.French, "IsolationAcoustique");
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Schallschutzklasse");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "遮音等級");
            return new PropertySetEntry(PropertyType.Label, name, ifcPSE);
        }

        /// <summary>
        /// Create the PropertySetEntry for "Compartmentation", which is used by some property sets.
        /// </summary>
        /// <returns>The correct PropertySetEntry.</returns>
        public static PropertySetEntry CreateCompartmentationEntry()
        {
			string name = "Compartmentation";
			PropertySetEntryMap ifcPSE = new PropertySetEntryMap(name);
            ifcPSE.AddLocalizedParameterName(LanguageType.French, "Compartimentage");
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "BrandabschnittsdefinierendesBauteil");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "防火区画");
            return new PropertySetEntry(PropertyType.Boolean, name, ifcPSE);
        }
        
        /// <summary>
        /// Create the PropertySetEntry for "Combustible", which is used by many property sets.
        /// </summary>
        /// <returns>The correct PropertySetEntry.</returns>
        public static PropertySetEntry CreateCombustibleEntry()
        {
			string name = "Combustible";
            PropertySetEntryMap ifcPSE = new PropertySetEntryMap(name);
            ifcPSE.AddLocalizedParameterName(LanguageType.Chinese_Simplified, "是否可燃");
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "BrennbaresMaterial");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "可燃性区分");
            return new PropertySetEntry(PropertyType.Boolean, name, ifcPSE);
        }

        /// <summary>
        /// Create the PropertySetEntry for "ExtendToStructure".
        /// </summary>
        /// <returns>The correct PropertySetEntry.</returns>
        public static PropertySetEntry CreateExtendToStructureEntry()
        {
			string name = "ExtendToStructure";
			PropertySetEntryMap ifcPSE = new PropertySetEntryMap(name);
            ifcPSE.AddLocalizedParameterName(LanguageType.French, "ExtensionStructure");
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "RaumhoheWand");
            ifcPSE.PropertyCalculator = ExtendToStructureCalculator.Instance;
            return new PropertySetEntry(PropertyType.Boolean, name, ifcPSE);
        }

        /// <summary>
        /// Create the PropertySetEntry for "FireRating", which is used by many property sets.
        /// </summary>
        /// <returns>The correct PropertySetEntry.</returns>
        public static PropertySetEntry CreateFireRatingEntry()
        {
			string name = "FireRating";
            PropertySetEntryMap ifcPSE = new PropertySetEntryMap(name);
            ifcPSE.AddLocalizedParameterName(LanguageType.Chinese_Simplified, "防火等级");
            ifcPSE.AddLocalizedParameterName(LanguageType.French, "ResistanceAuFeu");
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Feuerwiderstandsklasse");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "耐火等級");
            ifcPSE.RevitBuiltInParameter = BuiltInParameter.FIRE_RATING;
			return new PropertySetEntry(PropertyType.Label, name, ifcPSE);
        }

        /// <summary>
        /// Gets the localized version of the "IsExternal" parameter name.
        /// </summary>
        /// <param name="language">The current language.</param>
        /// <returns>The string containing the localized value, or "IsExternal" as default.</returns>
        public static string GetLocalizedIsExternal(LanguageType language)
        {
            switch (language)
            {
                case LanguageType.English_USA:
                    return "IsExternal";
                case LanguageType.Chinese_Simplified:
                    return "是否外部构件";
                case LanguageType.French:
                    return "EstExterieur";
                case LanguageType.German:
                    return "Außenbauteil";
                case LanguageType.Japanese:
                    return "外部区分";
            }
            return null;
        }

        /// <summary>
        /// Create the PropertySetEntry for "IsExternal", which is used by many property sets.
        /// </summary>
        /// <returns>The correct PropertySetEntry.</returns>
        public static PropertySetEntry CreateIsExternalEntry()
        {
			string name = "IsExternal";
            PropertySetEntryMap ifcPSE = new PropertySetEntryMap(name);
            ifcPSE.AddLocalizedParameterName(LanguageType.Chinese_Simplified, GetLocalizedIsExternal(LanguageType.Chinese_Simplified));
            ifcPSE.AddLocalizedParameterName(LanguageType.French, GetLocalizedIsExternal(LanguageType.French));
            ifcPSE.AddLocalizedParameterName(LanguageType.German, GetLocalizedIsExternal(LanguageType.German));
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, GetLocalizedIsExternal(LanguageType.Japanese));
            ifcPSE.PropertyCalculator = ExternalCalculator.Instance;
            return new PropertySetEntry(PropertyType.Boolean, name, ifcPSE);
        }

        /// <summary>
        /// Create the PropertySetEntry for "LoadBearing", which is used by many property sets.
        /// </summary>
        /// <param name="calc">The appropriate calculator for the element type associated with this entry.</param>
        /// <returns>The correct PropertySetEntry.</returns>
        public static PropertySetEntry CreateLoadBearingEntry(PropertyCalculator calc)
        {
			string name = "LoadBearing";
			PropertySetEntryMap ifcPSE = new PropertySetEntryMap(name);
            ifcPSE.AddLocalizedParameterName(LanguageType.Chinese_Simplified, "是否承重");
            ifcPSE.AddLocalizedParameterName(LanguageType.French, "Porteur");
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "TragendesBauteil");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "耐力部材");
            ifcPSE.PropertyCalculator = calc;
            return new PropertySetEntry(PropertyType.Boolean, name, ifcPSE);
        }

        /// <summary>
        /// Create the PropertySetEntry for "Reference", which is used by many property sets.
        /// </summary>
        /// <returns>The correct PropertySetEntry.</returns>
        public static PropertySetEntry CreateReferenceEntry()
        {
			string name = "Reference";
            PropertySetEntryMap ifcPSE = new PropertySetEntryMap(name);
            ifcPSE.AddLocalizedParameterName(LanguageType.Chinese_Simplified, "参考号");
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Bauteiltyp");
            ifcPSE.AddLocalizedParameterName(LanguageType.French, "Reference");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "参照記号");
            ifcPSE.AddLocalizedParameterName(LanguageType.Korean, "참조 ID");
            ifcPSE.PropertyCalculator = ReferenceCalculator.Instance;
            return new PropertySetEntry(PropertyType.Identifier, name, ifcPSE);
        }

        /// <summary>
        /// Create the PropertySetEntry for "SurfaceSpreadOfFlame", which is used by many property sets.
        /// </summary>
        /// <returns>The correct PropertySetEntry.</returns>
        public static PropertySetEntry CreateSurfaceSpreadOfFlameEntry()
        {
			string name = "SurfaceSpreadOfFlame";
			PropertySetEntryMap ifcPSE = new PropertySetEntryMap(name);
            ifcPSE.AddLocalizedParameterName(LanguageType.French, "SurfacePropagationFlamme");
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Brandverhalten");
            return new PropertySetEntry(PropertyType.Label, name, ifcPSE);
        }

        /// <summary>
        /// Create the PropertySetEntry for "ThermalTransmittance", which is used by many property sets.
        /// </summary>
        /// <returns>The correct PropertySetEntry.</returns>
        public static PropertySetEntry CreateThermalTransmittanceEntry()
        {
			string name = "ThermalTransmittance";
			PropertySetEntryMap ifcPSE = new PropertySetEntryMap(name);
            ifcPSE.RevitBuiltInParameter = BuiltInParameter.ANALYTICAL_HEAT_TRANSFER_COEFFICIENT;
            ifcPSE.AddLocalizedParameterName(LanguageType.Chinese_Simplified, "导热系数");
            ifcPSE.AddLocalizedParameterName(LanguageType.French, "TransmissionThermique");
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "U-Wert");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "熱貫流率");
            return new PropertySetEntry(PropertyType.ThermalTransmittance, name, ifcPSE);
        }

        /// <summary>
        /// Create PropertySetEntry for "Status", which is used by many property sets
        /// </summary>
        /// <returns></returns>
        public static PropertySetEntry CreateStatusEntry()
        {
			string name = "Status";
			PropertySetEntryMap ifcPSE = new PropertySetEntryMap(name); 
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Status");
            ifcPSE.AddLocalizedParameterName(LanguageType.French, "Statut");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "状態");
            return PropertySetEntry.CreateEnumeratedValue(name, PropertyType.Label,
				typeof(Toolkit.IFC4.PsetElementStatus), ifcPSE); 
        }

        /// <summary>
        /// Create PropertySetEntry for "HandicapAccessible", which is used by many property sets
        /// </summary>
        /// <returns></returns>
        public static PropertySetEntry CreateHandicapAccessibleEntry()
        {
			string name = "HandicapAccessible";
			PropertySetEntryMap ifcPSE = new PropertySetEntryMap(name);
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Behindertengerecht");
            ifcPSE.AddLocalizedParameterName(LanguageType.French, "AccessibleHandicapes");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "ハンディキャップアクセス可能性");
            ifcPSE.AddLocalizedParameterName(LanguageType.Chinese_Simplified, "是否为无障碍设施");
            return new PropertySetEntry(PropertyType.Boolean, name, ifcPSE);
        }

        public static PropertySetEntry CreateSpanEntry(PropertyCalculator propertyCalculator)
        {
			string name = "Span";
			PropertySetEntryMap ifcPSE = new PropertySetEntryMap(name) { PropertyCalculator = propertyCalculator };
           ifcPSE.AddLocalizedParameterName(LanguageType.German, "Spannweite");
           ifcPSE.AddLocalizedParameterName(LanguageType.French, "PorteeLibre");
           ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "全長");
           ifcPSE.AddLocalizedParameterName(LanguageType.Chinese_Simplified, "跨度");
           return new PropertySetEntry(PropertyType.PositiveLength, name, ifcPSE);
        }

      public static PropertySetEntry CreateSlopeEntry(PropertyCalculator propertyCalculator)
      {
		 string name = "Slope";
         PropertySetEntryMap ifcPSE = new PropertySetEntryMap(name) { PropertyCalculator = propertyCalculator };
         ifcPSE.AddLocalizedParameterName(LanguageType.German, "Neigungswinkel");
         ifcPSE.AddLocalizedParameterName(LanguageType.French, "Inclinaison");
         ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "傾斜");
         ifcPSE.AddLocalizedParameterName(LanguageType.Chinese_Simplified, "坡度");
         return new PropertySetEntry(PropertyType.PlaneAngle, name, ifcPSE);
      }

      public static PropertySetEntry CreateRollEntry(PropertyCalculator propertyCalculator)
		{
			string name = "Roll";
			PropertySetEntryMap ifcPSE = new PropertySetEntryMap(name) { PropertyCalculator = propertyCalculator };
			ifcPSE.AddLocalizedParameterName(LanguageType.German, "Kippwinkel");
         ifcPSE.AddLocalizedParameterName(LanguageType.French, "RotationAutourAxeLongitudinal");
         ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "回転");
         ifcPSE.AddLocalizedParameterName(LanguageType.Chinese_Simplified, "转角");
         return new PropertySetEntry(PropertyType.PlaneAngle, name, ifcPSE);
      }
   }
}
