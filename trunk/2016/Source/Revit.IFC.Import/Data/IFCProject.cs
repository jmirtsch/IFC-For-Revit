//
// Revit IFC Import library: this library works with Autodesk(R) Revit(R) to import IFC files.
// Copyright (C) 2015  Autodesk, Inc.
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
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IfcProject.
   /// </summary>
   public class IFCProject : IFCObject
   {
      private IList<double> m_TrueNorthDirection = null;

      private ISet<IFCUnit> m_UnitsInContext = null;

      private IDictionary<string, IFCGridAxis> m_GridAxes = null;

      /// <summary>
      /// The true north direction of the project.
      /// </summary>
      public IList<double> TrueNorthDirection
      {
         get { return m_TrueNorthDirection; }
      }

      /// <summary>
      /// The units in the project.
      /// </summary>
      public ISet<IFCUnit> UnitsInContext
      {
         get { return m_UnitsInContext; }
      }

      /// <summary>
      /// Constructs an IFCProject from the IfcProject handle.
      /// </summary>
      /// <param name="ifcProject">The IfcProject handle.</param>
      protected IFCProject(IFCAnyHandle ifcProject)
      {
         IFCImportFile.TheFile.IFCProject = this;
         Process(ifcProject);
      }

      /// <summary>
      /// Returns true if sub-elements should be grouped; false otherwise.
      /// </summary>
      public override bool GroupSubElements()
      {
         return false;
      }

      /// <summary>
      /// Processes IfcProject attributes.
      /// </summary>
      /// <param name="ifcProjectHandle">The IfcProject handle.</param>
      protected override void Process(IFCAnyHandle ifcProjectHandle)
      {
         IFCAnyHandle unitsInContext = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcProjectHandle, "UnitsInContext", false);

         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(unitsInContext))
         {
            IList<IFCAnyHandle> units = IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(unitsInContext, "Units");

            if (units != null)
            {
               m_UnitsInContext = new HashSet<IFCUnit>();

               foreach (IFCAnyHandle unit in units)
               {
                  IFCUnit ifcUnit = IFCImportFile.TheFile.IFCUnits.ProcessIFCProjectUnit(unit);
                  if (!IFCUnit.IsNullOrInvalid(ifcUnit))
                     m_UnitsInContext.Add(ifcUnit);
               }
            }
            else
            {
               Importer.TheLog.LogMissingRequiredAttributeError(unitsInContext, "Units", false);
            }
         }

         // We need to process the units before we process the rest of the file, since we will scale values as we go along.
         base.Process(ifcProjectHandle);

         // process true north - take the first valid representation context that has a true north value.
         HashSet<IFCAnyHandle> repContexts = IFCAnyHandleUtil.GetAggregateInstanceAttribute<HashSet<IFCAnyHandle>>(ifcProjectHandle, "RepresentationContexts");

         if (repContexts != null)
         {
            foreach (IFCAnyHandle geomRepContextHandle in repContexts)
            {
               if (!IFCAnyHandleUtil.IsNullOrHasNoValue(geomRepContextHandle) &&
                   IFCAnyHandleUtil.IsSubTypeOf(geomRepContextHandle, IFCEntityType.IfcGeometricRepresentationContext))
               {
                  IFCAnyHandle trueNorthHandle = IFCAnyHandleUtil.GetInstanceAttribute(geomRepContextHandle, "TrueNorth");
                  if (!IFCAnyHandleUtil.IsNullOrHasNoValue(trueNorthHandle))
                  {
                     List<double> trueNorthDir = IFCAnyHandleUtil.GetAggregateDoubleAttribute<List<double>>(trueNorthHandle, "DirectionRatios");
                     if (trueNorthDir != null && trueNorthDir.Count >= 2)
                     {
                        m_TrueNorthDirection = trueNorthDir;
                        break;
                     }
                  }
               }
            }
         }

         // Special read of IfcPresentationLayerAssignment if the INVERSE flag isn't properly set in the EXPRESS file.
         IFCPresentationLayerAssignment.ProcessAllLayerAssignments();
      }

      /// <summary>
      /// The list of grid axes in this IFCProject, sorted by Revit name.
      /// </summary>
      public IDictionary<string, IFCGridAxis> GridAxes
      {
         get
         {
            if (m_GridAxes == null)
               m_GridAxes = new Dictionary<string, IFCGridAxis>();
            return m_GridAxes;
         }
      }

      /// <summary>
      /// Allow for override of IfcObjectDefinition shared parameter names.
      /// </summary>
      /// <param name="name">The enum corresponding of the shared parameter.</param>
      /// <returns>The name appropriate for this IfcObjectDefinition.</returns>
      public override string GetSharedParameterName(IFCSharedParameters name)
      {
         switch (name)
         {
            case IFCSharedParameters.IfcName:
               return "IfcProject Name";
            case IFCSharedParameters.IfcDescription:
               return "IfcProject Description";
            default:
               return base.GetSharedParameterName(name);
         }
      }

      /// <summary>
      /// Get the element ids created for this entity, for summary logging.
      /// </summary>
      /// <param name="createdElementIds">The creation list.</param>
      /// <remarks>May contain InvalidElementId; the caller is expected to remove it.</remarks>
      public override void GetCreatedElementIds(ISet<ElementId> createdElementIds)
      {
         // If we used ProjectInformation, don't report that.
         if (CreatedElementId != ElementId.InvalidElementId && CreatedElementId != Importer.TheCache.ProjectInformationId)
         {
            createdElementIds.Add(CreatedElementId);
         }
      }

      /// <summary>
      /// Creates or populates Revit elements based on the information contained in this class.
      /// </summary>
      /// <param name="doc">The document.</param>
      protected override void Create(Document doc)
      {
         Units documentUnits = new Units(doc.DisplayUnitSystem == DisplayUnit.METRIC ?
             UnitSystem.Metric : UnitSystem.Imperial);
         foreach (IFCUnit unit in UnitsInContext)
         {
            if (!IFCUnit.IsNullOrInvalid(unit))
            {
               try
               {
                  FormatOptions formatOptions = new FormatOptions(unit.UnitName);
                  formatOptions.UnitSymbol = unit.UnitSymbol;
                  documentUnits.SetFormatOptions(unit.UnitType, formatOptions);
               }
               catch (Exception ex)
               {
                  Importer.TheLog.LogError(unit.Id, ex.Message, false);
               }
            }
         }
         doc.SetUnits(documentUnits);

         // We will randomize unused grid names so that they don't conflict with new entries with the same name.
         // This is only for relink.
         foreach (ElementId gridId in Importer.TheCache.GridNameToElementMap.Values)
         {
            Grid grid = doc.GetElement(gridId) as Grid;
            if (grid == null)
               continue;

            grid.Name = new Guid().ToString();
         }

         base.Create(doc);

         // IfcProject usually won't create an element, as it contains no geometry.
         // If it doesn't, use the ProjectInfo element in the document to store its parameters.
         if (CreatedElementId == ElementId.InvalidElementId)
            CreatedElementId = Importer.TheCache.ProjectInformationId;
      }

      /// <summary>
      /// Processes an IfcProject object.
      /// </summary>
      /// <param name="ifcProject">The IfcProject handle.</param>
      /// <returns>The IFCProject object.</returns>
      public static IFCProject ProcessIFCProject(IFCAnyHandle ifcProject)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcProject))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcProject);
            return null;
         }

         IFCEntity project;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcProject.StepId, out project))
            return (project as IFCProject);

         if (IFCAnyHandleUtil.IsSubTypeOf(ifcProject, IFCEntityType.IfcProject))
         {
            return new IFCProject(ifcProject);
         }

         //LOG: ERROR: Not processed project.
         return null;
      }
   }
}
