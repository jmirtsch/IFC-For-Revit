//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2012  Autodesk, Inc.
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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Utility;

namespace Revit.IFC.Export.Exporter.PropertySet
{
   /// <summary>
   /// A description mapping of a group of Revit parameters and/or calculated values to an IfcPropertySet.
   /// </summary>
   /// <remarks>
   /// The mapping includes: the name of the IFC property set, the entity type this property to which this set applies,
   /// and an array of property set entries.  A property set description is valid for only one entity type.
   /// </remarks>
   public class PropertySetDescription : Description
   {
      /// <summary>
      /// The entries stored in this property set description.
      /// </summary>
      IList<PropertySetEntry> m_Entries = new List<PropertySetEntry>();

      /// <summary>
      /// The entries stored in this property set description.
      /// </summary>
      public void AddEntry(PropertySetEntry entry)
      {
         //if the PropertySetDescription name and PropertySetEntry name are in the dictionary, 
         Tuple<string, string> key = new Tuple<string, string>(this.Name, entry.PropertyName);
         if (ExporterCacheManager.PropertyMapCache.ContainsKey(new Tuple<string, string>(this.Name, entry.PropertyName)))
         {
            //replace the PropertySetEntry.RevitParameterName by the value in the cache.
            entry.RevitParameterName = ExporterCacheManager.PropertyMapCache[key];
         }

         entry.UpdateEntry();
         m_Entries.Add(entry);
      }

      private string UsablePropertyName(IFCAnyHandle propHnd, IDictionary<string, IFCAnyHandle> propertiesByName)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return null;

         string currPropertyName = IFCAnyHandleUtil.GetStringAttribute(propHnd, "Name");
         if (string.IsNullOrWhiteSpace(currPropertyName))
            return null;   // This shouldn't be posssible.

         // Don't override if the new value is empty.
         if (propertiesByName.ContainsKey(currPropertyName))
         {
            try
            {
               // Only IfcSimplePropertyValue has the NominalValue attribute; any other type of property will throw.
               IFCData currPropertyValue = propHnd.GetAttribute("NominalValue");
               if (currPropertyValue.PrimitiveType == IFCDataPrimitiveType.String && string.IsNullOrWhiteSpace(currPropertyValue.AsString()))
                  return null;
            }
            catch
            {
               // Not an IfcSimplePropertyValue - no need to verify.
            }
         }

         return currPropertyName;
      }

      /// <summary>
      /// Creates handles for the properties.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC class.</param>
      /// <param name="ifcParams">The extrusion creation data, used to get extra parameter information.</param>
      /// <param name="elementToUse">The base element.</param>
      /// <param name="elemTypeToUse">The base element type.</param>
      /// <returns>A set of property handles.</returns>
      public ISet<IFCAnyHandle> ProcessEntries(IFCFile file, ExporterIFC exporterIFC, IFCExtrusionCreationData ifcParams, Element elementToUse, ElementType elemTypeToUse)
      {
         // We need to ensure that we don't have the same property name twice in the same property set.
         // By convention, we will keep the last property with the same name.  This allows for a user-defined
         // property set to look at both the type and the instance for a property value, if the type and instance properties
         // have different names.
         IDictionary<string, IFCAnyHandle> propertiesByName = new SortedDictionary<string, IFCAnyHandle>();

         foreach (PropertySetEntry entry in m_Entries)
         {
            IFCAnyHandle propHnd = entry.ProcessEntry(file, exporterIFC, ifcParams, elementToUse, elemTypeToUse);

            string currPropertyName = UsablePropertyName(propHnd, propertiesByName);
            if (currPropertyName != null)
               propertiesByName[currPropertyName] = propHnd;
         }

         ISet<IFCAnyHandle> props = new HashSet<IFCAnyHandle>(propertiesByName.Values);
         return props;
      }
   }
}
