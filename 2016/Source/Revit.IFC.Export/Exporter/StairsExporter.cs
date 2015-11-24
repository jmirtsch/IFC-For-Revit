//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
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
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Provides methods to export stairs
   /// </summary>
   class StairsExporter
   {
      /// <summary>
      /// The IfcMemberType shared by all stringers to keep their type.  This is a placeholder IfcMemberType.
      /// </summary>
      public static IFCAnyHandle GetMemberTypeHandle(ExporterIFC exporterIFC, Element stringer)
      {
         Element stringerType = stringer.Document.GetElement(stringer.GetTypeId());
         IFCAnyHandle memberType = ExporterCacheManager.ElementToHandleCache.Find(stringerType.Id);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(memberType))
         {
            IFCFile file = exporterIFC.GetFile();
            IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

            string stringerTypeGUID = GUIDUtil.CreateGUID(stringerType);
            string stringerTypeName = NamingUtil.GetNameOverride(stringerType, NamingUtil.GetIFCName(stringerType));
            string stringerTypeDescription = NamingUtil.GetDescriptionOverride(stringerType, null);
            string stringerTypeTag = NamingUtil.GetTagOverride(stringerType, NamingUtil.CreateIFCElementId(stringerType));
            string stringerApplicableOccurence = NamingUtil.GetOverrideStringValue(stringerType, "IfcApplicableOccurence", null);
            string stringerElementType = NamingUtil.GetOverrideStringValue(stringerType, "IfcElementType", null);

            memberType = IFCInstanceExporter.CreateMemberType(file, stringerTypeGUID,
                ownerHistory, stringerTypeName, stringerTypeDescription, stringerApplicableOccurence, null, null, stringerTypeTag,
                stringerElementType, IFCMemberType.Stringer);
            ExporterCacheManager.ElementToHandleCache.Register(stringerType.Id, memberType);
         }
         return memberType;
      }


      /// <summary>
      /// Determines if an element is a legacy (created in R2012 or before) Stairs element.
      /// </summary>
      /// <param name="element">
      /// The element.
      /// </param>
      /// <returns>
      /// Returns true if the element is a legacy (created in R2012 or before) Stairs element, false otherwise.
      /// </returns>
      static public bool IsLegacyStairs(Element element)
      {
         if (CategoryUtil.GetSafeCategoryId(element) != new ElementId(BuiltInCategory.OST_Stairs))
            return false;

         return !(element is Stairs) && !(element is FamilyInstance) && !(element is DirectShape);
      }

      static private double GetDefaultHeightForLegacyStair(Document doc)
      {
         // The default height for legacy stairs are either 12' or 3.5m.  Figure it out based on the scale of the export, and convert to feet.
         return (doc.DisplayUnitSystem == DisplayUnit.IMPERIAL) ? 12.0 : 3.5 * (100 / (12 * 2.54));
      }

      /// <summary>
      /// Gets the stairs height for a legacy (R2012 or before) stairs.
      /// </summary>
      /// <param name="exporterIFC">
      /// The exporter.
      /// </param>
      /// <param name="element">
      /// The element.
      /// </param>
      /// <param name="defaultHeight">
      /// The default height of the stair, in feet.
      /// </param>
      /// <returns>
      /// The unscaled height.
      /// </returns>
      static public double GetStairsHeightForLegacyStair(ExporterIFC exporterIFC, Element element, double defaultHeight)
      {
         ElementId baseLevelId;
         if (ParameterUtil.GetElementIdValueFromElement(element, BuiltInParameter.STAIRS_BASE_LEVEL_PARAM, out baseLevelId) == null)
            return 0.0;

         Level bottomLevel = element.Document.GetElement(baseLevelId) as Level;
         if (bottomLevel == null)
            return 0.0;
         double bottomLevelElev = bottomLevel.Elevation;

         ElementId topLevelId;
         Level topLevel = null;
         if ((ParameterUtil.GetElementIdValueFromElement(element, BuiltInParameter.STAIRS_TOP_LEVEL_PARAM, out topLevelId) != null) &&
             (topLevelId != ElementId.InvalidElementId))
            topLevel = element.Document.GetElement(topLevelId) as Level;

         double bottomLevelOffset;
         ParameterUtil.GetDoubleValueFromElement(element, BuiltInParameter.STAIRS_BASE_OFFSET, out bottomLevelOffset);

         double topLevelOffset;
         ParameterUtil.GetDoubleValueFromElement(element, BuiltInParameter.STAIRS_TOP_OFFSET, out topLevelOffset);

         double minHeight = bottomLevelElev + bottomLevelOffset;
         double maxHeight = (topLevel != null) ? topLevel.Elevation + topLevelOffset : minHeight + defaultHeight;

         double stairsHeight = maxHeight - minHeight;
         return stairsHeight;
      }

      /// <summary>
      /// Gets the number of flights of a multi-story staircase for a legacy (R2012 or before) stairs.
      /// </summary>
      /// <param name="exporterIFC">
      /// The exporter.
      /// </param>
      /// <param name="element">
      /// The element.
      /// </param>
      /// <param name="defaultHeight">
      /// The default height.
      /// </param>
      /// <returns>
      /// The number of flights (at least 1.)
      /// </returns>
      static public int GetNumFlightsForLegacyStair(ExporterIFC exporterIFC, Element element, double defaultHeight)
      {
         ElementId multistoryTopLevelId;
         if ((ParameterUtil.GetElementIdValueFromElement(element, BuiltInParameter.STAIRS_MULTISTORY_TOP_LEVEL_PARAM, out multistoryTopLevelId) == null) ||
             (multistoryTopLevelId == ElementId.InvalidElementId))
            return 1;

         ElementId baseLevelId;
         if ((ParameterUtil.GetElementIdValueFromElement(element, BuiltInParameter.STAIRS_BASE_LEVEL_PARAM, out baseLevelId) == null) ||
             (baseLevelId == ElementId.InvalidElementId))
            return 1;

         Level bottomLevel = element.Document.GetElement(baseLevelId) as Level;
         if (bottomLevel == null)
            return 1;
         double bottomLevelElev = bottomLevel.Elevation;

         Level multistoryTopLevel = element.Document.GetElement(multistoryTopLevelId) as Level;
         double multistoryLevelElev = multistoryTopLevel.Elevation;

         Level topLevel = null;
         ElementId topLevelId;
         if ((ParameterUtil.GetElementIdValueFromElement(element, BuiltInParameter.STAIRS_TOP_LEVEL_PARAM, out topLevelId) != null) &&
             (topLevelId != ElementId.InvalidElementId))
            topLevel = element.Document.GetElement(topLevelId) as Level;

         double bottomLevelOffset;
         ParameterUtil.GetDoubleValueFromElement(element, BuiltInParameter.STAIRS_BASE_OFFSET, out bottomLevelOffset);

         double topLevelOffset;
         ParameterUtil.GetDoubleValueFromElement(element, BuiltInParameter.STAIRS_TOP_OFFSET, out topLevelOffset);

         double minHeight = bottomLevelElev + bottomLevelOffset;
         double maxHeight = (topLevel != null) ? topLevel.Elevation + topLevelOffset : minHeight + defaultHeight;
         double unconnectedHeight = maxHeight;

         double stairsHeight = GetStairsHeightForLegacyStair(exporterIFC, element, defaultHeight);

         double topElev = (topLevel != null) ? topLevel.Elevation : unconnectedHeight;

         if ((topElev + MathUtil.Eps() > multistoryLevelElev) || (bottomLevelElev + MathUtil.Eps() > multistoryLevelElev))
            return 1;

         double multistoryHeight = multistoryLevelElev - bottomLevelElev;
         double oneStairHeight = stairsHeight;
         double currentHeight = oneStairHeight;

         if (oneStairHeight < MathUtil.Eps())
            return 1;

         int flightNumber = 0;
         for (; currentHeight < multistoryHeight + MathUtil.Eps() * flightNumber;
             currentHeight += oneStairHeight, flightNumber++)
         {
            // Fail if we reach some arbitrarily huge number.
            if (flightNumber > 100000)
               return 1;
         }

         return (flightNumber > 0) ? flightNumber : 1;
      }

      static private double GetStairsHeight(ExporterIFC exporterIFC, Element stair)
      {
         if (IsLegacyStairs(stair))
         {
            // The default height for legacy stairs are either 12' or 3.5m.  Figure it out based on the scale of the export, and convert to feet.
            double defaultHeight = GetDefaultHeightForLegacyStair(stair.Document);
            return GetStairsHeightForLegacyStair(exporterIFC, stair, defaultHeight);
         }

         if (stair is Stairs)
         {
            return (stair as Stairs).Height;
         }

         return 0.0;
      }

      /// <summary>
      /// Gets IFCStairType from stair type name.
      /// </summary>
      /// <param name="stairTypeName">The stair type name.</param>
      /// <returns>The IFCStairType.</returns>
      public static string GetIFCStairType(string stairTypeName)
      {
         string typeName = NamingUtil.RemoveSpacesAndUnderscores(stairTypeName);

         if (String.Compare(typeName, "StraightRun", true) == 0 ||
             String.Compare(typeName, "StraightRunStair", true) == 0)
            return "Straight_Run_Stair";
         if (String.Compare(typeName, "QuarterWinding", true) == 0 ||
             String.Compare(typeName, "QuarterWindingStair", true) == 0)
            return "Quarter_Winding_Stair";
         if (String.Compare(typeName, "QuarterTurn", true) == 0 ||
             String.Compare(typeName, "QuarterTurnStair", true) == 0)
            return "Quarter_Turn_Stair";
         if (String.Compare(typeName, "HalfWinding", true) == 0 ||
             String.Compare(typeName, "HalfWindingStair", true) == 0)
            return "Half_Winding_Stair";
         if (String.Compare(typeName, "HalfTurn", true) == 0 ||
             String.Compare(typeName, "HalfTurnStair", true) == 0)
            return "Half_Turn_Stair";
         if (String.Compare(typeName, "TwoQuarterWinding", true) == 0 ||
             String.Compare(typeName, "TwoQuarterWindingStair", true) == 0)
            return "Two_Quarter_Winding_Stair";
         if (String.Compare(typeName, "TwoStraightRun", true) == 0 ||
             String.Compare(typeName, "TwoStraightRunStair", true) == 0)
            return "Two_Straight_Run_Stair";
         if (String.Compare(typeName, "TwoQuarterTurn", true) == 0 ||
             String.Compare(typeName, "TwoQuarterTurnStair", true) == 0)
            return "Two_Quarter_Turn_Stair";
         if (String.Compare(typeName, "ThreeQuarterWinding", true) == 0 ||
             String.Compare(typeName, "ThreeQuarterWindingStair", true) == 0)
            return "Three_Quarter_Winding_Stair";
         if (String.Compare(typeName, "ThreeQuarterTurn", true) == 0 ||
             String.Compare(typeName, "ThreeQuarterTurnStair", true) == 0)
            return "Three_Quarter_Turn_Stair";
         if (String.Compare(typeName, "Spiral", true) == 0 ||
             String.Compare(typeName, "SpiralStair", true) == 0)
            return "Spiral_Stair";
         if (String.Compare(typeName, "DoubleReturn", true) == 0 ||
             String.Compare(typeName, "DoubleReturnStair", true) == 0)
            return "Double_Return_Stair";
         if (String.Compare(typeName, "CurvedRun", true) == 0 ||
             String.Compare(typeName, "CurvedRunStair", true) == 0)
            return "Curved_Run_Stair";
         if (String.Compare(typeName, "TwoCurvedRun", true) == 0 ||
             String.Compare(typeName, "TwoCurvedRunStair", true) == 0)
            return "Two_Curved_Run_Stair";
         if (String.Compare(typeName, "UserDefined", true) == 0)
            return "UserDefined";

         return "NotDefined";
      }

      /// <summary>
      /// Exports the top stories of a multistory staircase.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="stair">The stairs element.</param>
      /// <param name="numFlights">The number of flights for a multistory staircase.</param>
      /// <param name="stairHnd">The stairs container handle.</param>
      /// <param name="components">The components handles.</param>
      /// <param name="ecData">The extrusion creation data.</param>
      /// <param name="componentECData">The extrusion creation data for the components.</param>
      /// <param name="placementSetter">The placement setter.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static void ExportMultistoryStair(ExporterIFC exporterIFC, Element stair, int numFlights,
          IFCAnyHandle stairHnd, IList<IFCAnyHandle> components, IList<IFCExtrusionCreationData> componentECData,
          PlacementSetter placementSetter, ProductWrapper productWrapper)
      {
         if (numFlights < 2)
            return;

         double heightNonScaled = GetStairsHeight(exporterIFC, stair);
         if (heightNonScaled < MathUtil.Eps())
            return;

         if (IFCAnyHandleUtil.IsNullOrHasNoValue(stairHnd))
            return;

         IFCAnyHandle localPlacement = IFCAnyHandleUtil.GetObjectPlacement(stairHnd);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(localPlacement))
            return;

         IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

         IFCFile file = exporterIFC.GetFile();

         IFCAnyHandle relPlacement = GeometryUtil.GetRelativePlacementFromLocalPlacement(localPlacement);
         IFCAnyHandle ptHnd = IFCAnyHandleUtil.GetLocation(relPlacement);
         IList<double> origCoords = IFCAnyHandleUtil.GetCoordinates(ptHnd);

         ICollection<ElementId> runIds = null;
         ICollection<ElementId> landingIds = null;
         ICollection<ElementId> supportIds = null;

         if (stair is Stairs)
         {
            Stairs stairAsStairs = stair as Stairs;
            runIds = stairAsStairs.GetStairsRuns();
            landingIds = stairAsStairs.GetStairsLandings();
            supportIds = stairAsStairs.GetStairsSupports();
         }

         IList<IFCAnyHandle> stairLocalPlacementHnds = new List<IFCAnyHandle>();
         IList<IFCLevelInfo> levelInfos = new List<IFCLevelInfo>();
         for (int ii = 0; ii < numFlights - 1; ii++)
         {
            IFCAnyHandle newLevelHnd = null;

            // We are going to avoid internal scaling routines, and instead scale in .NET.
            double newOffsetUnscaled = 0.0;
            IFCLevelInfo currLevelInfo =
                placementSetter.GetOffsetLevelInfoAndHandle(heightNonScaled * (ii + 1), 1.0, stair.Document, out newLevelHnd, out newOffsetUnscaled);
            double newOffsetScaled = UnitUtil.ScaleLength(newOffsetUnscaled);

            if (currLevelInfo != null)
               levelInfos.Add(currLevelInfo);
            else
               levelInfos.Add(placementSetter.LevelInfo);

            XYZ orig;
            if (ptHnd.HasValue)
            {
               orig = new XYZ(origCoords[0], origCoords[1], newOffsetScaled);
            }
            else
            {
               orig = new XYZ(0.0, 0.0, newOffsetScaled);
            }
            stairLocalPlacementHnds.Add(ExporterUtil.CreateLocalPlacement(file, newLevelHnd, orig, null, null));
         }

         IList<List<IFCAnyHandle>> newComponents = new List<List<IFCAnyHandle>>();
         for (int ii = 0; ii < numFlights - 1; ii++)
            newComponents.Add(new List<IFCAnyHandle>());

         int compIdx = 0;
         IEnumerator<ElementId> runIter = null;
         if (runIds != null)
         {
            runIter = runIds.GetEnumerator();
            runIter.MoveNext();
         }
         IEnumerator<ElementId> landingIter = null;
         if (landingIds != null)
         {
            landingIter = landingIds.GetEnumerator();
            landingIter.MoveNext();
         }
         IEnumerator<ElementId> supportIter = null;
         if (supportIds != null)
         {
            supportIter = supportIds.GetEnumerator();
            supportIter.MoveNext();
         }

         foreach (IFCAnyHandle component in components)
         {
            string componentName = IFCAnyHandleUtil.GetStringAttribute(component, "Name");
            string componentDescription = IFCAnyHandleUtil.GetStringAttribute(component, "Description");
            string componentObjectType = IFCAnyHandleUtil.GetStringAttribute(component, "ObjectType");
            string componentElementTag = IFCAnyHandleUtil.GetStringAttribute(component, "Tag");
            IFCAnyHandle componentProdRep = IFCAnyHandleUtil.GetInstanceAttribute(component, "Representation");

            IList<string> localComponentNames = new List<string>();
            IList<IFCAnyHandle> componentPlacementHnds = new List<IFCAnyHandle>();

            IFCAnyHandle localLocalPlacement = IFCAnyHandleUtil.GetObjectPlacement(component);
            IFCAnyHandle localRelativePlacement =
                (localLocalPlacement == null) ? null : IFCAnyHandleUtil.GetInstanceAttribute(localLocalPlacement, "RelativePlacement");

            bool isSubStair = component.IsSubTypeOf(IFCEntityType.IfcStair.ToString());
            for (int ii = 0; ii < numFlights - 1; ii++)
            {
               localComponentNames.Add((componentName == null) ? (ii + 2).ToString() : (componentName + ":" + (ii + 2)));
               if (isSubStair)
                  componentPlacementHnds.Add(ExporterUtil.CopyLocalPlacement(file, stairLocalPlacementHnds[ii]));
               else
                  componentPlacementHnds.Add(IFCInstanceExporter.CreateLocalPlacement(file, stairLocalPlacementHnds[ii], localRelativePlacement));
            }

            IList<IFCAnyHandle> localComponentHnds = new List<IFCAnyHandle>();
            if (isSubStair)
            {
               string componentType = IFCAnyHandleUtil.GetEnumerationAttribute(component, ExporterCacheManager.ExportOptionsCache.ExportAs4 ? "PredefinedType" : "ShapeType");
               string localStairType = GetIFCStairType(componentType);

               ElementId catId = CategoryUtil.GetSafeCategoryId(stair);

               for (int ii = 0; ii < numFlights - 1; ii++)
               {
                  IFCAnyHandle representationCopy =
                      ExporterUtil.CopyProductDefinitionShape(exporterIFC, stair, catId, componentProdRep);

                  localComponentHnds.Add(IFCInstanceExporter.CreateStair(file, GUIDUtil.CreateGUID(), ownerHistory,
                      localComponentNames[ii], componentDescription, componentObjectType, componentPlacementHnds[ii], representationCopy,
                      componentElementTag, localStairType));
               }
            }
            else if (IFCAnyHandleUtil.IsSubTypeOf(component, IFCEntityType.IfcStairFlight))
            {
               Element runElem = (runIter == null) ? stair : stair.Document.GetElement(runIter.Current);
               Element runElemToUse = (runElem == null) ? stair : runElem;
               ElementId catId = CategoryUtil.GetSafeCategoryId(runElemToUse);

               int? numberOfRiser = IFCAnyHandleUtil.GetIntAttribute(component, "NumberOfRiser");
               int? numberOfTreads = IFCAnyHandleUtil.GetIntAttribute(component, "NumberOfTreads");
               double? riserHeight = IFCAnyHandleUtil.GetDoubleAttribute(component, "RiserHeight");
               double? treadLength = IFCAnyHandleUtil.GetDoubleAttribute(component, "TreadLength");

               for (int ii = 0; ii < numFlights - 1; ii++)
               {
                  IFCAnyHandle representationCopy =
                      ExporterUtil.CopyProductDefinitionShape(exporterIFC, runElemToUse, catId, componentProdRep);

                  localComponentHnds.Add(IFCInstanceExporter.CreateStairFlight(file, GUIDUtil.CreateGUID(), ownerHistory,
                      localComponentNames[ii], componentDescription, componentObjectType, componentPlacementHnds[ii], representationCopy,
                      componentElementTag, numberOfRiser, numberOfTreads, riserHeight, treadLength, "NOTDEFINED"));
               }
               runIter.MoveNext();
            }
            else if (IFCAnyHandleUtil.IsSubTypeOf(component, IFCEntityType.IfcSlab))
            {
               Element landingElem = (landingIter == null) ? stair : stair.Document.GetElement(landingIter.Current);
               Element landingElemToUse = (landingElem == null) ? stair : landingElem;
               ElementId catId = CategoryUtil.GetSafeCategoryId(landingElemToUse);

               string componentType = IFCValidateEntry.GetValidIFCType(landingElemToUse, IFCAnyHandleUtil.GetEnumerationAttribute(component, "PredefinedType"));
               // IFCSlabType localLandingType = FloorExporter.GetIFCSlabType(componentType);

               for (int ii = 0; ii < numFlights - 1; ii++)
               {
                  IFCAnyHandle representationCopy =
                      ExporterUtil.CopyProductDefinitionShape(exporterIFC, landingElemToUse, catId, componentProdRep);

                  localComponentHnds.Add(IFCInstanceExporter.CreateSlab(file, GUIDUtil.CreateGUID(), ownerHistory,
                      localComponentNames[ii], componentDescription, componentObjectType, componentPlacementHnds[ii], representationCopy,
                      componentElementTag, componentType));
               }

               landingIter.MoveNext();
            }
            else if (IFCAnyHandleUtil.IsSubTypeOf(component, IFCEntityType.IfcMember))
            {
               Element supportElem = (supportIter == null) ? stair : stair.Document.GetElement(supportIter.Current);
               Element supportElemToUse = (supportElem == null) ? stair : supportElem;
               ElementId catId = CategoryUtil.GetSafeCategoryId(supportElemToUse);

               IFCAnyHandle memberType = (supportElemToUse != stair) ? GetMemberTypeHandle(exporterIFC, supportElemToUse) : null;

               for (int ii = 0; ii < numFlights - 1; ii++)
               {
                  IFCAnyHandle representationCopy =
                  ExporterUtil.CopyProductDefinitionShape(exporterIFC, supportElemToUse, catId, componentProdRep);

                  localComponentHnds.Add(IFCInstanceExporter.CreateMember(file, GUIDUtil.CreateGUID(), ownerHistory,
                      localComponentNames[ii], componentDescription, componentObjectType, componentPlacementHnds[ii], representationCopy,
                      componentElementTag, "STRINGER"));

                  if (memberType != null)
                     ExporterCacheManager.TypeRelationsCache.Add(memberType, localComponentHnds[ii]);
               }

               supportIter.MoveNext();
            }

            for (int ii = 0; ii < numFlights - 1; ii++)
            {
               if (localComponentHnds[ii] != null)
               {
                  newComponents[ii].Add(localComponentHnds[ii]);
                  productWrapper.AddElement(null, localComponentHnds[ii], levelInfos[ii], componentECData[compIdx], false);
               }
            }
            compIdx++;
         }

         // finally add a copy of the container.
         IList<IFCAnyHandle> stairCopyHnds = new List<IFCAnyHandle>();
         for (int ii = 0; ii < numFlights - 1; ii++)
         {
            string stairName = IFCAnyHandleUtil.GetStringAttribute(stairHnd, "Name");
            string stairObjectType = IFCAnyHandleUtil.GetStringAttribute(stairHnd, "ObjectType");
            string stairDescription = IFCAnyHandleUtil.GetStringAttribute(stairHnd, "Description");
            string stairElementTag = IFCAnyHandleUtil.GetStringAttribute(stairHnd, "Tag");
            string stairTypeAsString = null;
            if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
               stairTypeAsString = IFCAnyHandleUtil.GetEnumerationAttribute(stairHnd, "PredefinedType");
            else
               stairTypeAsString = IFCAnyHandleUtil.GetEnumerationAttribute(stairHnd, "ShapeType");
            string stairType = GetIFCStairType(stairTypeAsString);

            string containerStairName = stairName + ":" + (ii + 2);
            stairCopyHnds.Add(IFCInstanceExporter.CreateStair(file, GUIDUtil.CreateGUID(), ownerHistory,
                containerStairName, stairDescription, stairObjectType, stairLocalPlacementHnds[ii], null, stairElementTag, stairType));

            productWrapper.AddElement(stair, stairCopyHnds[ii], levelInfos[ii], null, true);
         }

         for (int ii = 0; ii < numFlights - 1; ii++)
         {
            StairRampContainerInfo stairRampInfo = new StairRampContainerInfo(stairCopyHnds[ii], newComponents[ii],
                stairLocalPlacementHnds[ii]);
            ExporterCacheManager.StairRampContainerInfoCache.AppendStairRampContainerInfo(stair.Id, stairRampInfo);
         }
      }

      /// <summary>
      /// Exports a staircase to IfcStair, without decomposing into separate runs and landings.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="ifcEnumType">The stairs type.</param>
      /// <param name="stair">The stairs element.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="numFlights">The number of flights for a multistory staircase.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static void ExportStairAsSingleGeometry(ExporterIFC exporterIFC, string ifcEnumType, Element stair, GeometryElement geometryElement,
          int numFlights, ProductWrapper productWrapper)
      {
         if (stair == null || geometryElement == null)
            return;

         IFCFile file = exporterIFC.GetFile();

         using (IFCTransaction tr = new IFCTransaction(file))
         {
            using (PlacementSetter placementSetter = PlacementSetter.Create(exporterIFC, stair))
            {
               using (IFCExtrusionCreationData ecData = new IFCExtrusionCreationData())
               {
                  ecData.SetLocalPlacement(placementSetter.LocalPlacement);
                  ecData.ReuseLocalPlacement = false;

                  GeometryElement stairsGeom = GeometryUtil.GetOneLevelGeometryElement(geometryElement, numFlights);

                  BodyData bodyData;
                  ElementId categoryId = CategoryUtil.GetSafeCategoryId(stair);

                  BodyExporterOptions bodyExporterOptions = new BodyExporterOptions();
                  IFCAnyHandle representation = RepresentationUtil.CreateAppropriateProductDefinitionShape(exporterIFC,
                      stair, categoryId, stairsGeom, bodyExporterOptions, null, ecData, out bodyData);

                  if (IFCAnyHandleUtil.IsNullOrHasNoValue(representation))
                  {
                     ecData.ClearOpenings();
                     return;
                  }

                  string containedStairGuid = GUIDUtil.CreateSubElementGUID(stair, (int)IFCStairSubElements.ContainedStair);
                  IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
                  string stairName = NamingUtil.GetNameOverride(stair, NamingUtil.GetIFCName(stair));
                  string stairDescription = NamingUtil.GetDescriptionOverride(stair, null);
                  string stairObjectType = NamingUtil.GetObjectTypeOverride(stair, NamingUtil.CreateIFCObjectName(exporterIFC, stair));
                  IFCAnyHandle containedStairLocalPlacement = ExporterUtil.CreateLocalPlacement(file, ecData.GetLocalPlacement(), null);
                  string elementTag = NamingUtil.GetTagOverride(stair, NamingUtil.CreateIFCElementId(stair));
                  string stairType = GetIFCStairType(ifcEnumType);

                  List<IFCAnyHandle> components = new List<IFCAnyHandle>();
                  IList<IFCExtrusionCreationData> componentExtrusionData = new List<IFCExtrusionCreationData>();
                  IFCAnyHandle containedStairHnd = IFCInstanceExporter.CreateStair(file, containedStairGuid, ownerHistory, stairName,
                      stairDescription, stairObjectType, containedStairLocalPlacement, representation, elementTag, stairType);
                  components.Add(containedStairHnd);
                  componentExtrusionData.Add(ecData);
                  //productWrapper.AddElement(containedStairHnd, placementSetter.LevelInfo, ecData, false);
                  CategoryUtil.CreateMaterialAssociations(exporterIFC, containedStairHnd, bodyData.MaterialIds);

                  string guid = GUIDUtil.CreateGUID(stair);
                  IFCAnyHandle localPlacement = ecData.GetLocalPlacement();

                  IFCAnyHandle stairHnd = IFCInstanceExporter.CreateStair(file, guid, ownerHistory, stairName,
                      stairDescription, stairObjectType, localPlacement, null, elementTag, stairType);

                  productWrapper.AddElement(stair, stairHnd, placementSetter.LevelInfo, ecData, true);

                  StairRampContainerInfo stairRampInfo = new StairRampContainerInfo(stairHnd, components, localPlacement);
                  ExporterCacheManager.StairRampContainerInfoCache.AddStairRampContainerInfo(stair.Id, stairRampInfo);

                  ExportMultistoryStair(exporterIFC, stair, numFlights, stairHnd, components,
                      componentExtrusionData, placementSetter, productWrapper);
               }
               tr.Commit();
            }
         }
      }

      /// <summary>
      /// Exports a staircase to IfcStair, composing into separate runs and landings.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="ifcEnumType">The stairs type.</param>
      /// <param name="stair">The stairs element.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="numFlights">The number of flights for a multistory staircase.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static void ExportStairsAsContainer(ExporterIFC exporterIFC, string ifcEnumType, Stairs stair, GeometryElement geometryElement,
          int numFlights, ProductWrapper productWrapper)
      {
         if (stair == null || geometryElement == null)
            return;

         Document doc = stair.Document;
         IFCFile file = exporterIFC.GetFile();
         Options geomOptions = GeometryUtil.GetIFCExportGeometryOptions();
         ElementId categoryId = CategoryUtil.GetSafeCategoryId(stair);

         using (IFCTransaction tr = new IFCTransaction(file))
         {
            using (PlacementSetter placementSetter = PlacementSetter.Create(exporterIFC, stair))
            {
               List<IFCAnyHandle> componentHandles = new List<IFCAnyHandle>();
               IList<IFCExtrusionCreationData> componentExtrusionData = new List<IFCExtrusionCreationData>();

               IFCAnyHandle contextOfItemsFootPrint = exporterIFC.Get3DContextHandle("FootPrint");
               IFCAnyHandle contextOfItemsAxis = exporterIFC.Get3DContextHandle("Axis");

               Transform trf = ExporterIFCUtils.GetUnscaledTransform(exporterIFC, placementSetter.LocalPlacement);
               Plane boundaryPlane = new Plane(trf.BasisX, trf.BasisY, trf.Origin);
               XYZ boundaryProjDir = trf.BasisZ;

               IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
               string stairGUID = GUIDUtil.CreateGUID(stair);
               string stairName = NamingUtil.GetNameOverride(stair, NamingUtil.GetIFCName(stair));
               string stairDescription = NamingUtil.GetDescriptionOverride(stair, null);
               string stairObjectType = NamingUtil.GetObjectTypeOverride(stair, NamingUtil.CreateIFCObjectName(exporterIFC, stair));
               IFCAnyHandle stairLocalPlacement = placementSetter.LocalPlacement;
               string stairElementTag = NamingUtil.GetTagOverride(stair, NamingUtil.CreateIFCElementId(stair));
               string stairType = GetIFCStairType(ifcEnumType);

               IFCAnyHandle stairContainerHnd = IFCInstanceExporter.CreateStair(file, stairGUID, ownerHistory, stairName,
                   stairDescription, stairObjectType, stairLocalPlacement, null, stairElementTag, stairType);

               productWrapper.AddElement(stair, stairContainerHnd, placementSetter.LevelInfo, null, true);

               // Get List of runs to export their geometry.
               ICollection<ElementId> runIds = stair.GetStairsRuns();
               int index = 0;
               foreach (ElementId runId in runIds)
               {
                  index++;
                  StairsRun run = doc.GetElement(runId) as StairsRun;

                  using (IFCExtrusionCreationData ecData = new IFCExtrusionCreationData())
                  {
                     ecData.AllowVerticalOffsetOfBReps = false;
                     ecData.SetLocalPlacement(ExporterUtil.CreateLocalPlacement(file, placementSetter.LocalPlacement, null));
                     ecData.ReuseLocalPlacement = true;

                     GeometryElement runGeometryElement = run.get_Geometry(geomOptions);

                     BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true);
                     BodyData bodyData = BodyExporter.ExportBody(exporterIFC, run, categoryId, ElementId.InvalidElementId, runGeometryElement,
                         bodyExporterOptions, ecData);

                     IFCAnyHandle bodyRep = bodyData.RepresentationHnd;
                     if (IFCAnyHandleUtil.IsNullOrHasNoValue(bodyRep))
                     {
                        ecData.ClearOpenings();
                        continue;
                     }

                     IList<IFCAnyHandle> reps = new List<IFCAnyHandle>();
                     reps.Add(bodyRep);

                     if (!ExporterCacheManager.ExportOptionsCache.ExportAsCoordinationView2)
                     {
                        Transform runBoundaryTrf = (bodyData.OffsetTransform == null) ? trf : trf.Multiply(bodyData.OffsetTransform);
                        Plane runBoundaryPlane = new Plane(runBoundaryTrf.BasisX, runBoundaryTrf.BasisY, runBoundaryTrf.Origin);
                        XYZ runBoundaryProjDir = runBoundaryTrf.BasisZ;

                        CurveLoop boundary = run.GetFootprintBoundary();
                        IFCAnyHandle boundaryHnd = GeometryUtil.CreateIFCCurveFromCurveLoop(exporterIFC, boundary,
                            runBoundaryPlane, runBoundaryProjDir);
                        if (!IFCAnyHandleUtil.IsNullOrHasNoValue(boundaryHnd))
                        {
                           HashSet<IFCAnyHandle> geomSelectSet = new HashSet<IFCAnyHandle>();
                           geomSelectSet.Add(boundaryHnd);

                           HashSet<IFCAnyHandle> boundaryItems = new HashSet<IFCAnyHandle>();
                           boundaryItems.Add(IFCInstanceExporter.CreateGeometricSet(file, geomSelectSet));

                           IFCAnyHandle boundaryRep = RepresentationUtil.CreateGeometricSetRep(exporterIFC, run, categoryId, "FootPrint",
                               contextOfItemsFootPrint, boundaryItems);
                           reps.Add(boundaryRep);
                        }

                        CurveLoop walkingLine = run.GetStairsPath();
                        IFCAnyHandle walkingLineHnd = GeometryUtil.CreateIFCCurveFromCurveLoop(exporterIFC, walkingLine,
                            runBoundaryPlane, runBoundaryProjDir);
                        if (!IFCAnyHandleUtil.IsNullOrHasNoValue(walkingLineHnd))
                        {
                           HashSet<IFCAnyHandle> geomSelectSet = new HashSet<IFCAnyHandle>();
                           geomSelectSet.Add(walkingLineHnd);

                           HashSet<IFCAnyHandle> walkingLineItems = new HashSet<IFCAnyHandle>();
                           walkingLineItems.Add(IFCInstanceExporter.CreateGeometricSet(file, geomSelectSet));

                           IFCAnyHandle walkingLineRep = RepresentationUtil.CreateGeometricSetRep(exporterIFC, run, categoryId, "Axis",
                               contextOfItemsAxis, walkingLineItems);
                           reps.Add(walkingLineRep);
                        }
                     }

                     Transform boundingBoxTrf = (bodyData.OffsetTransform == null) ? Transform.Identity : bodyData.OffsetTransform.Inverse;
                     IFCAnyHandle boundingBoxRep = BoundingBoxExporter.ExportBoundingBox(exporterIFC, runGeometryElement, boundingBoxTrf);
                     if (boundingBoxRep != null)
                        reps.Add(boundingBoxRep);

                     IFCAnyHandle representation = IFCInstanceExporter.CreateProductDefinitionShape(exporterIFC.GetFile(), null, null, reps);

                     string runGUID = GUIDUtil.CreateGUID(run);
                     string origRunName = stairName + " Run " + index;
                     string runName = NamingUtil.GetNameOverride(run, origRunName);
                     string runDescription = NamingUtil.GetDescriptionOverride(run, stairDescription);
                     string runObjectType = NamingUtil.GetObjectTypeOverride(run, stairObjectType);
                     IFCAnyHandle runLocalPlacement = ecData.GetLocalPlacement();
                     string runElementTag = NamingUtil.GetTagOverride(run, NamingUtil.CreateIFCElementId(run));

                     IFCAnyHandle stairFlightHnd = IFCInstanceExporter.CreateStairFlight(file, runGUID, ownerHistory,
                         runName, runDescription, runObjectType, runLocalPlacement, representation, runElementTag,
                         run.ActualRisersNumber, run.ActualTreadsNumber, stair.ActualRiserHeight, stair.ActualTreadDepth, "NOTDEFINED");

                     componentHandles.Add(stairFlightHnd);
                     componentExtrusionData.Add(ecData);

                     CategoryUtil.CreateMaterialAssociations(exporterIFC, stairFlightHnd, bodyData.MaterialIds);

                     productWrapper.AddElement(run, stairFlightHnd, placementSetter.LevelInfo, ecData, false);

                     ExporterCacheManager.HandleToElementCache.Register(stairFlightHnd, run.Id);
                  }
               }

               // Get List of landings to export their geometry.
               ICollection<ElementId> landingIds = stair.GetStairsLandings();
               index = 0;
               foreach (ElementId landingId in landingIds)
               {
                  index++;
                  StairsLanding landing = doc.GetElement(landingId) as StairsLanding;

                  using (IFCExtrusionCreationData ecData = new IFCExtrusionCreationData())
                  {
                     ecData.AllowVerticalOffsetOfBReps = false;
                     ecData.SetLocalPlacement(ExporterUtil.CreateLocalPlacement(file, placementSetter.LocalPlacement, null));
                     ecData.ReuseLocalPlacement = true;

                     GeometryElement landingGeometryElement = landing.get_Geometry(geomOptions);

                     BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true);
                     BodyData bodyData = BodyExporter.ExportBody(exporterIFC, landing, categoryId, ElementId.InvalidElementId, landingGeometryElement,
                         bodyExporterOptions, ecData);

                     IFCAnyHandle bodyRep = bodyData.RepresentationHnd;
                     if (IFCAnyHandleUtil.IsNullOrHasNoValue(bodyRep))
                     {
                        ecData.ClearOpenings();
                        continue;
                     }

                     // create Boundary rep.
                     IList<IFCAnyHandle> reps = new List<IFCAnyHandle>();
                     reps.Add(bodyRep);

                     if (!ExporterCacheManager.ExportOptionsCache.ExportAsCoordinationView2)
                     {
                        Transform landingBoundaryTrf = (bodyData.OffsetTransform == null) ? trf : trf.Multiply(bodyData.OffsetTransform);
                        Plane landingBoundaryPlane = new Plane(landingBoundaryTrf.BasisX, landingBoundaryTrf.BasisY, landingBoundaryTrf.Origin);
                        XYZ landingBoundaryProjDir = landingBoundaryTrf.BasisZ;

                        CurveLoop boundary = landing.GetFootprintBoundary();
                        IFCAnyHandle boundaryHnd = GeometryUtil.CreateIFCCurveFromCurveLoop(exporterIFC, boundary,
                            landingBoundaryPlane, landingBoundaryProjDir);
                        if (!IFCAnyHandleUtil.IsNullOrHasNoValue(boundaryHnd))
                        {
                           HashSet<IFCAnyHandle> geomSelectSet = new HashSet<IFCAnyHandle>();
                           geomSelectSet.Add(boundaryHnd);

                           HashSet<IFCAnyHandle> boundaryItems = new HashSet<IFCAnyHandle>();
                           boundaryItems.Add(IFCInstanceExporter.CreateGeometricSet(file, geomSelectSet));

                           IFCAnyHandle boundaryRep = RepresentationUtil.CreateGeometricSetRep(exporterIFC, landing, categoryId, "FootPrint",
                               contextOfItemsFootPrint, boundaryItems);
                           reps.Add(boundaryRep);
                        }

                        CurveLoop walkingLine = landing.GetStairsPath();
                        IFCAnyHandle walkingLineHnd = GeometryUtil.CreateIFCCurveFromCurveLoop(exporterIFC, walkingLine,
                            landingBoundaryPlane, landingBoundaryProjDir);
                        if (!IFCAnyHandleUtil.IsNullOrHasNoValue(walkingLineHnd))
                        {
                           HashSet<IFCAnyHandle> geomSelectSet = new HashSet<IFCAnyHandle>();
                           geomSelectSet.Add(walkingLineHnd);

                           HashSet<IFCAnyHandle> walkingLineItems = new HashSet<IFCAnyHandle>();
                           walkingLineItems.Add(IFCInstanceExporter.CreateGeometricSet(file, geomSelectSet));

                           IFCAnyHandle walkingLineRep = RepresentationUtil.CreateGeometricSetRep(exporterIFC, landing, categoryId, "Axis",
                               contextOfItemsAxis, walkingLineItems);
                           reps.Add(walkingLineRep);
                        }
                     }

                     Transform boundingBoxTrf = (bodyData.OffsetTransform == null) ? Transform.Identity : bodyData.OffsetTransform.Inverse;
                     IFCAnyHandle boundingBoxRep = BoundingBoxExporter.ExportBoundingBox(exporterIFC, landingGeometryElement, boundingBoxTrf);
                     if (boundingBoxRep != null)
                        reps.Add(boundingBoxRep);

                     string landingGUID = GUIDUtil.CreateGUID(landing);
                     string origLandingName = stairName + " Landing " + index;
                     string landingName = NamingUtil.GetNameOverride(landing, origLandingName);
                     string landingDescription = NamingUtil.GetDescriptionOverride(landing, stairDescription);
                     string landingObjectType = NamingUtil.GetObjectTypeOverride(landing, stairObjectType);
                     IFCAnyHandle landingLocalPlacement = ecData.GetLocalPlacement();
                     string landingElementTag = NamingUtil.GetTagOverride(landing, NamingUtil.CreateIFCElementId(landing));

                     IFCAnyHandle representation = IFCInstanceExporter.CreateProductDefinitionShape(exporterIFC.GetFile(), null, null, reps);

                     IFCAnyHandle landingHnd = IFCInstanceExporter.CreateSlab(file, landingGUID, ownerHistory,
                         landingName, landingDescription, landingObjectType, landingLocalPlacement, representation, landingElementTag,
                         "LANDING");

                     componentHandles.Add(landingHnd);
                     componentExtrusionData.Add(ecData);

                     CategoryUtil.CreateMaterialAssociations(exporterIFC, landingHnd, bodyData.MaterialIds);

                     productWrapper.AddElement(landing, landingHnd, placementSetter.LevelInfo, ecData, false);
                     ExporterCacheManager.HandleToElementCache.Register(landingHnd, landing.Id);
                  }
               }

               // Get List of supports to export their geometry.  Supports are not exposed to API, so export as generic Element.
               ICollection<ElementId> supportIds = stair.GetStairsSupports();
               index = 0;
               foreach (ElementId supportId in supportIds)
               {
                  index++;
                  Element support = doc.GetElement(supportId);

                  using (IFCExtrusionCreationData ecData = new IFCExtrusionCreationData())
                  {
                     ecData.SetLocalPlacement(ExporterUtil.CreateLocalPlacement(file, placementSetter.LocalPlacement, null));
                     ecData.ReuseLocalPlacement = true;

                     GeometryElement supportGeometryElement = support.get_Geometry(geomOptions);
                     BodyData bodyData;
                     BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true);
                     IFCAnyHandle representation = RepresentationUtil.CreateAppropriateProductDefinitionShape(exporterIFC,
                         support, categoryId, supportGeometryElement, bodyExporterOptions, null, ecData, out bodyData);

                     if (IFCAnyHandleUtil.IsNullOrHasNoValue(representation))
                     {
                        ecData.ClearOpenings();
                        continue;
                     }

                     string supportGUID = GUIDUtil.CreateGUID(support);
                     string origSupportName = stairName + " Stringer " + index;
                     string supportName = NamingUtil.GetNameOverride(support, origSupportName);
                     string supportDescription = NamingUtil.GetDescriptionOverride(support, stairDescription);
                     string supportObjectType = NamingUtil.GetObjectTypeOverride(support, stairObjectType);
                     IFCAnyHandle supportLocalPlacement = ecData.GetLocalPlacement();
                     string supportElementTag = NamingUtil.GetTagOverride(support, NamingUtil.CreateIFCElementId(support));

                     IFCAnyHandle type = GetMemberTypeHandle(exporterIFC, support);

                     IFCAnyHandle supportHnd = IFCInstanceExporter.CreateMember(file, supportGUID, ownerHistory,
                         supportName, supportDescription, supportObjectType, supportLocalPlacement, representation, supportElementTag, "STRINGER");

                     componentHandles.Add(supportHnd);
                     componentExtrusionData.Add(ecData);

                     CategoryUtil.CreateMaterialAssociations(exporterIFC, supportHnd, bodyData.MaterialIds);

                     productWrapper.AddElement(support, supportHnd, placementSetter.LevelInfo, ecData, false);

                     ExporterCacheManager.TypeRelationsCache.Add(type, supportHnd);
                  }
               }

               StairRampContainerInfo stairRampInfo = new StairRampContainerInfo(stairContainerHnd, componentHandles, stairLocalPlacement);
               ExporterCacheManager.StairRampContainerInfoCache.AddStairRampContainerInfo(stair.Id, stairRampInfo);

               ExportMultistoryStair(exporterIFC, stair, numFlights, stairContainerHnd, componentHandles, componentExtrusionData,
                   placementSetter, productWrapper);
            }
            tr.Commit();
         }
      }

      /// <summary>
      /// Exports a legacy staircase or ramp to IfcStair or IfcRamp, composing into separate runs and landings.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="ifcEnumType">>The ifc type.</param>
      /// <param name="legacyStair">The legacy stairs or ramp element.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static void ExportLegacyStairOrRampAsContainer(ExporterIFC exporterIFC, string ifcEnumType, Element legacyStair, GeometryElement geometryElement,
          ProductWrapper productWrapper)
      {
         IFCFile file = exporterIFC.GetFile();
         ElementId categoryId = CategoryUtil.GetSafeCategoryId(legacyStair);

         using (IFCTransaction tr = new IFCTransaction(file))
         {
            using (PlacementSetter placementSetter = PlacementSetter.Create(exporterIFC, legacyStair))
            {
               IFCLegacyStairOrRamp legacyStairOrRamp = null;
               try
               {
                  legacyStairOrRamp = ExporterIFCUtils.GetLegacyStairOrRampComponents(exporterIFC, legacyStair);
               }
               catch
               {
                  legacyStairOrRamp = null;
               }

               if (legacyStairOrRamp == null)
                  return;

               bool isRamp = legacyStairOrRamp.IsRamp;

               using (IFCExtrusionCreationData ifcECData = new IFCExtrusionCreationData())
               {
                  ifcECData.SetLocalPlacement(placementSetter.LocalPlacement);

                  string stairDescription = NamingUtil.GetDescriptionOverride(legacyStair, null);
                  string stairObjectType = NamingUtil.GetObjectTypeOverride(legacyStair, NamingUtil.CreateIFCObjectName(exporterIFC, legacyStair));
                  string stairElementTag = NamingUtil.GetTagOverride(legacyStair, NamingUtil.CreateIFCElementId(legacyStair));

                  double defaultHeight = GetDefaultHeightForLegacyStair(legacyStair.Document);
                  double stairHeight = GetStairsHeightForLegacyStair(exporterIFC, legacyStair, defaultHeight);
                  int numFlights = GetNumFlightsForLegacyStair(exporterIFC, legacyStair, defaultHeight);

                  List<IFCLevelInfo> localLevelInfoForFlights = new List<IFCLevelInfo>();
                  List<IFCAnyHandle> localPlacementForFlights = new List<IFCAnyHandle>();
                  List<List<IFCAnyHandle>> components = new List<List<IFCAnyHandle>>();

                  components.Add(new List<IFCAnyHandle>());

                  if (numFlights > 1)
                  {
                     XYZ zDir = new XYZ(0.0, 0.0, 1.0);
                     XYZ xDir = new XYZ(1.0, 0.0, 0.0);
                     for (int ii = 1; ii < numFlights; ii++)
                     {
                        components.Add(new List<IFCAnyHandle>());
                        IFCAnyHandle newLevelHnd = null;

                        // We are going to avoid internal scaling routines, and instead scale in .NET.
                        double newOffsetUnscaled = 0.0;
                        IFCLevelInfo currLevelInfo =
                            placementSetter.GetOffsetLevelInfoAndHandle(stairHeight * ii, 1.0, legacyStair.Document, out newLevelHnd, out newOffsetUnscaled);
                        double newOffsetScaled = UnitUtil.ScaleLength(newOffsetUnscaled);

                        localLevelInfoForFlights.Add(currLevelInfo);

                        XYZ orig = new XYZ(0.0, 0.0, newOffsetScaled);
                        localPlacementForFlights.Add(ExporterUtil.CreateLocalPlacement(file, newLevelHnd, orig, zDir, xDir));
                     }
                  }

                  IList<IFCAnyHandle> walkingLineReps = CreateWalkLineReps(exporterIFC, legacyStairOrRamp, legacyStair);
                  IList<IFCAnyHandle> boundaryReps = CreateBoundaryLineReps(exporterIFC, legacyStairOrRamp, legacyStair);
                  IList<IList<GeometryObject>> geometriesOfRuns = legacyStairOrRamp.GetRunGeometries();
                  IList<int> numRisers = legacyStairOrRamp.GetNumberOfRisers();
                  IList<int> numTreads = legacyStairOrRamp.GetNumberOfTreads();
                  IList<double> treadsLength = legacyStairOrRamp.GetTreadsLength();
                  double riserHeight = legacyStairOrRamp.RiserHeight;

                  int runCount = geometriesOfRuns.Count;
                  int walkingLineCount = walkingLineReps.Count;
                  int boundaryRepCount = boundaryReps.Count;

                  for (int ii = 0; ii < runCount; ii++)
                  {
                     BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true);
                     bodyExporterOptions.TessellationLevel = BodyExporter.GetTessellationLevel();

                     IList<GeometryObject> geometriesOfARun = geometriesOfRuns[ii];
                     BodyData bodyData = BodyExporter.ExportBody(exporterIFC, legacyStair, categoryId, ElementId.InvalidElementId, geometriesOfARun,
                         bodyExporterOptions, null);

                     IFCAnyHandle bodyRep = bodyData.RepresentationHnd;
                     if (IFCAnyHandleUtil.IsNullOrHasNoValue(bodyRep))
                        continue;
                     
                     HashSet<IFCAnyHandle> flightHnds = new HashSet<IFCAnyHandle>();
                     List<IFCAnyHandle> representations = new List<IFCAnyHandle>();
                     if ((ii < walkingLineCount) && !IFCAnyHandleUtil.IsNullOrHasNoValue(walkingLineReps[ii]))
                        representations.Add(walkingLineReps[ii]);

                     if ((ii < boundaryRepCount) && !IFCAnyHandleUtil.IsNullOrHasNoValue(boundaryReps[ii]))
                        representations.Add(boundaryReps[ii]);

                     representations.Add(bodyRep);

                     IFCAnyHandle boundingBoxRep = BoundingBoxExporter.ExportBoundingBox(exporterIFC, geometriesOfARun, Transform.Identity);
                     if (boundingBoxRep != null)
                        representations.Add(boundingBoxRep);

                     IFCAnyHandle flightRep = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, representations);
                     IFCAnyHandle flightLocalPlacement = ExporterUtil.CreateLocalPlacement(file, placementSetter.LocalPlacement, null);

                     IFCAnyHandle flightHnd;
                     string stairName = NamingUtil.GetNameOverride(legacyStair, NamingUtil.GetIFCNamePlusIndex(legacyStair, ii + 1));

                     if (isRamp)
                     {
                        flightHnd = IFCInstanceExporter.CreateRampFlight(file, GUIDUtil.CreateGUID(), ExporterCacheManager.OwnerHistoryHandle,
                            stairName, stairDescription, stairObjectType, flightLocalPlacement, flightRep, stairElementTag, "NOTDEFINED");
                        flightHnds.Add(flightHnd);
                        productWrapper.AddElement(null, flightHnd, placementSetter.LevelInfo, null, false);
                     }
                     else
                     {
                        flightHnd = IFCInstanceExporter.CreateStairFlight(file, GUIDUtil.CreateGUID(), ExporterCacheManager.OwnerHistoryHandle,
                            stairName, stairDescription, stairObjectType, flightLocalPlacement, flightRep, stairElementTag, numRisers[ii], numTreads[ii],
                            riserHeight, treadsLength[ii], "NOTDEFINED");
                        flightHnds.Add(flightHnd);
                        productWrapper.AddElement(null, flightHnd, placementSetter.LevelInfo, null, false);
                     }
                     CategoryUtil.CreateMaterialAssociations(exporterIFC, flightHnd, bodyData.MaterialIds);

                     components[0].Add(flightHnd);
                     for (int compIdx = 1; compIdx < numFlights; compIdx++)
                     {
                        if (isRamp)
                        {
                           IFCAnyHandle newLocalPlacement = ExporterUtil.CreateLocalPlacement(file, localPlacementForFlights[compIdx - 1], null);
                           IFCAnyHandle newProdRep = ExporterUtil.CopyProductDefinitionShape(exporterIFC, legacyStair, categoryId, IFCAnyHandleUtil.GetRepresentation(flightHnd));
                           flightHnd = IFCInstanceExporter.CreateRampFlight(file, GUIDUtil.CreateGUID(), ExporterCacheManager.OwnerHistoryHandle,
                               stairName, stairDescription, stairObjectType, newLocalPlacement, newProdRep, stairElementTag, "NOTDEFINED");
                           components[compIdx].Add(flightHnd);
                        }
                        else
                        {
                           IFCAnyHandle newLocalPlacement = ExporterUtil.CreateLocalPlacement(file, localPlacementForFlights[compIdx - 1], null);
                           IFCAnyHandle newProdRep = ExporterUtil.CopyProductDefinitionShape(exporterIFC, legacyStair, categoryId, IFCAnyHandleUtil.GetRepresentation(flightHnd));

                           flightHnd = IFCInstanceExporter.CreateStairFlight(file, GUIDUtil.CreateGUID(), ExporterCacheManager.OwnerHistoryHandle,
                               stairName, stairDescription, stairObjectType, newLocalPlacement, newProdRep, stairElementTag,
                               numRisers[ii], numTreads[ii], riserHeight, treadsLength[ii], "NOTDEFINED");
                           components[compIdx].Add(flightHnd);
                        }

                        productWrapper.AddElement(null, flightHnd, placementSetter.LevelInfo, null, false);
                        CategoryUtil.CreateMaterialAssociations(exporterIFC, flightHnd, bodyData.MaterialIds);
                        flightHnds.Add(flightHnd);
                     }
                  }

                  IList<IList<GeometryObject>> geometriesOfLandings = legacyStairOrRamp.GetLandingGeometries();
                  for (int ii = 0; ii < geometriesOfLandings.Count; ii++)
                  {
                     using (IFCExtrusionCreationData ecData = new IFCExtrusionCreationData())
                     {
                        BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true);
                        bodyExporterOptions.TessellationLevel = BodyExporterOptions.BodyTessellationLevel.Coarse;
                        IList<GeometryObject> geometriesOfALanding = geometriesOfLandings[ii];
                        BodyData bodyData = BodyExporter.ExportBody(exporterIFC, legacyStair, categoryId, ElementId.InvalidElementId, geometriesOfALanding,
                            bodyExporterOptions, ecData);

                        IFCAnyHandle bodyRep = bodyData.RepresentationHnd;
                        if (IFCAnyHandleUtil.IsNullOrHasNoValue(bodyRep))
                        {
                           ecData.ClearOpenings();
                           continue;
                        }

                        List<IFCAnyHandle> representations = new List<IFCAnyHandle>();
                        if (((ii + runCount) < walkingLineCount) && !IFCAnyHandleUtil.IsNullOrHasNoValue(walkingLineReps[ii + runCount]))
                           representations.Add(walkingLineReps[ii + runCount]);

                        if (((ii + runCount) < boundaryRepCount) && !IFCAnyHandleUtil.IsNullOrHasNoValue(boundaryReps[ii + runCount]))
                           representations.Add(boundaryReps[ii + runCount]);

                        representations.Add(bodyRep);

                        IFCAnyHandle boundingBoxRep = BoundingBoxExporter.ExportBoundingBox(exporterIFC, geometriesOfALanding, Transform.Identity);
                        if (boundingBoxRep != null)
                           representations.Add(boundingBoxRep);

                        IFCAnyHandle shapeHnd = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, representations);
                        IFCAnyHandle landingLocalPlacement = ExporterUtil.CreateLocalPlacement(file, placementSetter.LocalPlacement, null);
                        string stairName = NamingUtil.GetIFCNamePlusIndex(legacyStair, ii + 1);

                        IFCAnyHandle slabHnd = IFCInstanceExporter.CreateSlab(file, GUIDUtil.CreateGUID(), ExporterCacheManager.OwnerHistoryHandle,
                            stairName, stairDescription, stairObjectType, landingLocalPlacement, shapeHnd, stairElementTag, "LANDING");
                        productWrapper.AddElement(null, slabHnd, placementSetter.LevelInfo, ecData, false);
                        CategoryUtil.CreateMaterialAssociations(exporterIFC, slabHnd, bodyData.MaterialIds);

                        components[0].Add(slabHnd);
                        for (int compIdx = 1; compIdx < numFlights; compIdx++)
                        {
                           IFCAnyHandle newLocalPlacement = ExporterUtil.CreateLocalPlacement(file, localPlacementForFlights[compIdx - 1], null);
                           IFCAnyHandle newProdRep = ExporterUtil.CopyProductDefinitionShape(exporterIFC, legacyStair, categoryId, IFCAnyHandleUtil.GetRepresentation(slabHnd));

                           IFCAnyHandle newSlabHnd = IFCInstanceExporter.CreateSlab(file, GUIDUtil.CreateGUID(), ExporterCacheManager.OwnerHistoryHandle,
                               stairName, stairDescription, stairObjectType, newLocalPlacement, newProdRep, stairElementTag, "LANDING");
                           CategoryUtil.CreateMaterialAssociations(exporterIFC, slabHnd, bodyData.MaterialIds);
                           components[compIdx].Add(newSlabHnd);
                           productWrapper.AddElement(null, newSlabHnd, placementSetter.LevelInfo, ecData, false);
                        }
                     }
                  }

                  IList<GeometryObject> geometriesOfStringer = legacyStairOrRamp.GetStringerGeometries();
                  for (int ii = 0; ii < geometriesOfStringer.Count; ii++)
                  {
                     using (IFCExtrusionCreationData ecData = new IFCExtrusionCreationData())
                     {
                        BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true);
                        bodyExporterOptions.TessellationLevel = BodyExporterOptions.BodyTessellationLevel.Coarse;
                        GeometryObject geometryOfStringer = geometriesOfStringer[ii];
                        BodyData bodyData = BodyExporter.ExportBody(exporterIFC, legacyStair, categoryId, ElementId.InvalidElementId, geometryOfStringer,
                            bodyExporterOptions, ecData);

                        IFCAnyHandle bodyRep = bodyData.RepresentationHnd;
                        if (IFCAnyHandleUtil.IsNullOrHasNoValue(bodyRep))
                        {
                           ecData.ClearOpenings();
                           continue;
                        }

                        List<IFCAnyHandle> representations = new List<IFCAnyHandle>();
                        representations.Add(bodyRep);

                        IFCAnyHandle boundingBoxRep = BoundingBoxExporter.ExportBoundingBox(exporterIFC, geometriesOfStringer, Transform.Identity);
                        if (boundingBoxRep != null)
                           representations.Add(boundingBoxRep);

                        IFCAnyHandle stringerRepHnd = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, representations);
                        IFCAnyHandle stringerLocalPlacement = ExporterUtil.CreateLocalPlacement(file, placementSetter.LocalPlacement, null);
                        string stairName = NamingUtil.GetIFCNamePlusIndex(legacyStair, ii + 1);

                        IFCAnyHandle memberHnd = IFCInstanceExporter.CreateMember(file, GUIDUtil.CreateGUID(), ExporterCacheManager.OwnerHistoryHandle,
                            stairName, stairDescription, stairObjectType, stringerLocalPlacement, stringerRepHnd, stairElementTag, "STRINGER");

                        productWrapper.AddElement(null, memberHnd, placementSetter.LevelInfo, ecData, false);
                        PropertyUtil.CreateBeamColumnMemberBaseQuantities(exporterIFC, memberHnd, null, ecData);
                        CategoryUtil.CreateMaterialAssociations(exporterIFC, memberHnd, bodyData.MaterialIds);

                        components[0].Add(memberHnd);
                        for (int compIdx = 1; compIdx < numFlights; compIdx++)
                        {
                           IFCAnyHandle newLocalPlacement = ExporterUtil.CreateLocalPlacement(file, localPlacementForFlights[compIdx - 1], null);
                           IFCAnyHandle newProdRep = ExporterUtil.CopyProductDefinitionShape(exporterIFC, legacyStair, categoryId, IFCAnyHandleUtil.GetRepresentation(memberHnd));

                           IFCAnyHandle newMemberHnd = IFCInstanceExporter.CreateMember(file, GUIDUtil.CreateGUID(), ExporterCacheManager.OwnerHistoryHandle,
                               stairName, stairDescription, stairObjectType, newLocalPlacement, newProdRep, stairElementTag, "STRINGER");
                           CategoryUtil.CreateMaterialAssociations(exporterIFC, memberHnd, bodyData.MaterialIds);
                           components[compIdx].Add(newMemberHnd);
                           productWrapper.AddElement(null, newMemberHnd, placementSetter.LevelInfo, ecData, false);
                        }
                     }
                  }

                  List<IFCAnyHandle> createdStairs = new List<IFCAnyHandle>();
                  if (isRamp)
                  {
                     string rampType = RampExporter.GetIFCRampType(ifcEnumType);
                     string stairName = NamingUtil.GetIFCName(legacyStair);
                     IFCAnyHandle containedRampHnd = IFCInstanceExporter.CreateRamp(file, GUIDUtil.CreateGUID(legacyStair), ExporterCacheManager.OwnerHistoryHandle,
                         stairName, stairDescription, stairObjectType, placementSetter.LocalPlacement, null, stairElementTag, rampType);
                     productWrapper.AddElement(legacyStair, containedRampHnd, placementSetter.LevelInfo, ifcECData, true);
                     createdStairs.Add(containedRampHnd);
                  }
                  else
                  {
                     string stairType = GetIFCStairType(ifcEnumType);
                     string stairName = NamingUtil.GetIFCName(legacyStair);
                     IFCAnyHandle containedStairHnd = IFCInstanceExporter.CreateStair(file, GUIDUtil.CreateGUID(legacyStair), ExporterCacheManager.OwnerHistoryHandle,
                         stairName, stairDescription, stairObjectType, placementSetter.LocalPlacement, null, stairElementTag, stairType);
                     productWrapper.AddElement(legacyStair, containedStairHnd, placementSetter.LevelInfo, ifcECData, true);
                     createdStairs.Add(containedStairHnd);
                  }

                  // multi-story stairs.
                  if (numFlights > 1)
                  {
                     IFCAnyHandle localPlacement = placementSetter.LocalPlacement;
                     IFCAnyHandle relPlacement = GeometryUtil.GetRelativePlacementFromLocalPlacement(localPlacement);
                     IFCAnyHandle ptHnd = IFCAnyHandleUtil.GetLocation(relPlacement);
                     IList<double> origCoords = null;
                     if (!IFCAnyHandleUtil.IsNullOrHasNoValue(ptHnd))
                        origCoords = IFCAnyHandleUtil.GetCoordinates(ptHnd);

                     for (int ii = 1; ii < numFlights; ii++)
                     {
                        IFCLevelInfo levelInfo = localLevelInfoForFlights[ii - 1];
                        if (levelInfo == null)
                           levelInfo = placementSetter.LevelInfo;

                        localPlacement = localPlacementForFlights[ii - 1];

                        // relate to bottom stair or closest level?  For code checking, we need closest level, and
                        // that seems good enough for the general case.
                        if (!IFCAnyHandleUtil.IsNullOrHasNoValue(ptHnd))
                        {
                           IFCAnyHandle relPlacement2 = GeometryUtil.GetRelativePlacementFromLocalPlacement(localPlacement);
                           IFCAnyHandle newPt = IFCAnyHandleUtil.GetLocation(relPlacement2);

                           List<double> newCoords = new List<double>();
                           newCoords.Add(origCoords[0]);
                           newCoords.Add(origCoords[1]);
                           newCoords.Add(origCoords[2]);
                           if (!IFCAnyHandleUtil.IsNullOrHasNoValue(newPt))
                           {
                              IList<double> addToCoords;
                              addToCoords = IFCAnyHandleUtil.GetCoordinates(newPt);
                              newCoords[0] += addToCoords[0];
                              newCoords[1] += addToCoords[1];
                              newCoords[2] = addToCoords[2];
                           }

                           IFCAnyHandle locPt = ExporterUtil.CreateCartesianPoint(file, newCoords);
                           IFCAnyHandleUtil.SetAttribute(relPlacement2, "Location", locPt);
                        }

                        if (isRamp)
                        {
                           string rampType = RampExporter.GetIFCRampType(ifcEnumType);
                           string stairName = NamingUtil.GetIFCName(legacyStair);
                           IFCAnyHandle containedRampHnd = IFCInstanceExporter.CreateRamp(file, GUIDUtil.CreateGUID(legacyStair), ExporterCacheManager.OwnerHistoryHandle,
                               stairName, stairDescription, stairObjectType, localPlacement, null, stairElementTag, rampType);
                           productWrapper.AddElement(legacyStair, containedRampHnd, levelInfo, ifcECData, true);
                           //createdStairs.Add(containedRampHnd) ???????????????????????
                        }
                        else
                        {
                           string stairType = GetIFCStairType(ifcEnumType);
                           string stairName = NamingUtil.GetIFCName(legacyStair);
                           IFCAnyHandle containedStairHnd = IFCInstanceExporter.CreateStair(file, GUIDUtil.CreateGUID(legacyStair), ExporterCacheManager.OwnerHistoryHandle,
                               stairName, stairDescription, stairObjectType, localPlacement, null, stairElementTag, stairType);
                           productWrapper.AddElement(legacyStair, containedStairHnd, levelInfo, ifcECData, true);
                           createdStairs.Add(containedStairHnd);
                        }
                     }
                  }

                  localPlacementForFlights.Insert(0, placementSetter.LocalPlacement);

                  StairRampContainerInfo stairRampInfo = new StairRampContainerInfo(createdStairs, components, localPlacementForFlights);
                  ExporterCacheManager.StairRampContainerInfoCache.AddStairRampContainerInfo(legacyStair.Id, stairRampInfo);
               }
            }

            tr.Commit();
         }
      }

      /// <summary>
      /// Exports a staircase to IfcStair.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="element">The stairs element.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static void Export(ExporterIFC exporterIFC, Element element, GeometryElement geometryElement, ProductWrapper productWrapper)
      {
         string ifcEnumType = ExporterUtil.GetIFCTypeFromExportTable(exporterIFC, element);
         IFCFile file = exporterIFC.GetFile();

         using (IFCTransaction tr = new IFCTransaction(file))
         {
            if (element is Stairs)
            {
               Stairs stair = element as Stairs;
               int numFlights = stair.NumberOfStories;
               if (numFlights > 0)
               {
                  ExportStairsAsContainer(exporterIFC, ifcEnumType, stair, geometryElement, numFlights, productWrapper);
                  if (IFCAnyHandleUtil.IsNullOrHasNoValue(productWrapper.GetAnElement()))
                     ExportStairAsSingleGeometry(exporterIFC, ifcEnumType, element, geometryElement, numFlights, productWrapper);
               }
            }
            else
            {
               // If we didn't create a handle here, then the element wasn't a "native" legacy Stairs, and is likely a FamilyInstance or a DirectShape.
               ExportLegacyStairOrRampAsContainer(exporterIFC, ifcEnumType, element, geometryElement, productWrapper);
               if (IFCAnyHandleUtil.IsNullOrHasNoValue(productWrapper.GetAnElement()))
               {
                  double defaultHeight = GetDefaultHeightForLegacyStair(element.Document);
                  int numFlights = GetNumFlightsForLegacyStair(exporterIFC, element, defaultHeight);
                  if (numFlights > 0)
                     ExportStairAsSingleGeometry(exporterIFC, ifcEnumType, element, geometryElement, numFlights, productWrapper);
               }
            }

            tr.Commit();
         }
      }

      /// <summary>
      /// Creates boundary line representations from stair boundary lines.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="legacyStair">The stair.</param>
      /// <param name="legacyStairElem">The stair element.</param>
      /// <returns>Boundary line representations.</returns>
      static IList<IFCAnyHandle> CreateBoundaryLineReps(ExporterIFC exporterIFC, IFCLegacyStairOrRamp legacyStair, Element legacyStairElem)
      {
         IFCAnyHandle contextOfItemsBoundary = exporterIFC.Get3DContextHandle("FootPrint");

         IList<IFCAnyHandle> boundaryLineReps = new List<IFCAnyHandle>();

         IFCFile file = exporterIFC.GetFile();
         ElementId cateId = CategoryUtil.GetSafeCategoryId(legacyStairElem);

         HashSet<IFCAnyHandle> curveSet = new HashSet<IFCAnyHandle>();
         IList<CurveLoop> boundaryLines = legacyStair.GetBoundaryLines();
         foreach (CurveLoop curveLoop in boundaryLines)
         {
            Plane plane = new Plane(XYZ.BasisX, XYZ.BasisY, XYZ.Zero);
            foreach (Curve curve in curveLoop)
            {
               IFCGeometryInfo info = IFCGeometryInfo.CreateCurveGeometryInfo(exporterIFC, plane, XYZ.BasisZ, false);
               ExporterIFCUtils.CollectGeometryInfo(exporterIFC, info, curve, XYZ.Zero, false);
               IList<IFCAnyHandle> curves = info.GetCurves();
               if (curves.Count == 1 && !IFCAnyHandleUtil.IsNullOrHasNoValue(curves[0]))
               {
                  curveSet.Add(curves[0]);
               }
            }
            IFCAnyHandle curveRepresentationItem = IFCInstanceExporter.CreateGeometricSet(file, curveSet);
            HashSet<IFCAnyHandle> bodyItems = new HashSet<IFCAnyHandle>();
            bodyItems.Add(curveRepresentationItem);
            IFCAnyHandle boundaryLineRep = RepresentationUtil.CreateGeometricSetRep(exporterIFC, legacyStairElem, cateId, "FootPrint",
               contextOfItemsBoundary, bodyItems);
            boundaryLineReps.Add(boundaryLineRep);
         }
         return boundaryLineReps;
      }

      /// <summary>
      /// Creates walk line representations from stair walk lines.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="legacyStair">The stair.</param>
      /// <param name="legacyStairElem">The stair element.</param>
      /// <returns>The walk line representation handles.  Some of them may be null.</returns>
      static IList<IFCAnyHandle> CreateWalkLineReps(ExporterIFC exporterIFC, IFCLegacyStairOrRamp legacyStair, Element legacyStairElem)
      {
         IList<IFCAnyHandle> walkLineReps = new List<IFCAnyHandle>();
         IFCAnyHandle contextOfItemsWalkLine = exporterIFC.Get3DContextHandle("Axis");

         ElementId cateId = CategoryUtil.GetSafeCategoryId(legacyStairElem);
         Plane plane = new Plane(XYZ.BasisX, XYZ.BasisY, XYZ.Zero);
         XYZ projDir = XYZ.BasisZ;

         IList<IList<Curve>> curvesArr = legacyStair.GetWalkLines();
         foreach (IList<Curve> curves in curvesArr)
         {
            IFCAnyHandle curve = GeometryUtil.CreateIFCCurveFromCurves(exporterIFC, curves, plane, projDir);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(curve))
            {
               HashSet<IFCAnyHandle> bodyItems = new HashSet<IFCAnyHandle>();
               bodyItems.Add(curve);
               walkLineReps.Add(RepresentationUtil.CreateShapeRepresentation(exporterIFC, legacyStairElem, cateId,
                   contextOfItemsWalkLine, "Axis", "Curve2D", bodyItems));
            }
            else
               walkLineReps.Add(null);
         }
         return walkLineReps;
      }
   }
}
