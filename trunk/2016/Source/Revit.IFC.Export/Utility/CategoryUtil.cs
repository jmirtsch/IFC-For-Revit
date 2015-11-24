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
using System.Text;
using Autodesk.Revit;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Export.Toolkit;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Provides static methods for category related manipulations.
   /// </summary>
   class CategoryUtil
   {
      /// <summary>
      /// Gets category id of an element.
      /// </summary>
      /// <remarks>
      /// Returns InvalidElementId when argument is null.
      /// </remarks>
      /// <param name="element">
      /// The element.
      /// </param>
      /// <returns>
      /// The category id.
      /// </returns>
      public static ElementId GetSafeCategoryId(Element element)
      {
         if (element == null)
            return ElementId.InvalidElementId;
         return element.Category.Id;
      }

      /// <summary>
      /// Gets category name of an element.
      /// </summary>
      /// <param name="element">
      /// The element.
      /// </param>
      /// <returns>
      /// The category name.
      /// </returns>
      public static String GetCategoryName(Element element)
      {
         Category category = element.Category;

         if (category == null)
         {
            throw new Exception("Unable to obtain category for element id " + element.Id.IntegerValue);
         }
         return category.Name;
      }

      /// <summary>
      /// Gets material id of the category of an element.
      /// </summary>
      /// <remarks>
      /// Returns the material id of the parent category when the category of the element has no material.
      /// </remarks>
      /// <param name="element">
      /// The element.
      /// </param>
      /// <returns>
      /// The material id.
      /// </returns>
      public static ElementId GetBaseMaterialIdForElement(Element element)
      {
         ElementId baseMaterialId = ElementId.InvalidElementId;
         Category category = element.Category;
         if (category != null)
         {
            Material baseMaterial = category.Material;
            if (baseMaterial != null)
               baseMaterialId = baseMaterial.Id;
            else
            {
               category = category.Parent;
               if (category != null)
               {
                  baseMaterial = category.Material;
                  if (baseMaterial != null)
                     baseMaterialId = baseMaterial.Id;
               }
            }
         }
         return baseMaterialId;
      }

      /// <summary>
      /// Returns the original color if it is valid, or a default color (grey) if it isn't.
      /// </summary>
      /// <param name="originalColor">The original color.</param>
      /// <returns>The original color if it is valid, or a default color (grey) if it isn't.</returns>
      public static Color GetSafeColor(Color originalColor)
      {
         if (originalColor.IsValid)
            return originalColor;

         // Default color is grey.
         return new Color(0x7f, 0x7f, 0x7f);
      }

      /// <summary>
      /// Gets the color of the material of the category of an element.
      /// </summary>
      /// <remarks>
      /// Returns the line color of the category when the category of the element has no material.
      /// </remarks>
      /// <param name="element">
      /// The element.
      /// </param>
      /// <returns>
      /// The color of the element.
      /// </returns>
      public static Autodesk.Revit.DB.Color GetElementColor(Element element)
      {
         Category category = element.Category;

         if (category == null)
         {
            throw new Exception("Unable to obtain category for element id " + element.Id.IntegerValue);
         }

         Material material = category.Material;

         if (material != null)
         {
            return GetSafeColor(material.Color);
         }
         else
         {
            Color color = GetSafeColor(category.LineColor);

            // Grey is returned in place of pure black.  For systems which default to a black background color, 
            // Grey is more of a contrast.  
            if (color.Red == 0 && color.Green == 0 && color.Blue == 0)
               color = new Color(0x7f, 0x7f, 0x7f);

            return color;
         }
      }

      /// <summary>
      /// Checks if element is external.
      /// </summary>
      /// <remarks>
      /// An element is considered external if either:
      ///   <li> A special Yes/No parameter "IsExternal" is applied to it or its type and it's value is set to "yes".</li>
      ///   <li> The element itself has information about being an external element.</li>
      /// All other elements are internal.
      /// </remarks>
      /// <param name="element">The element.</param>
      /// <returns>True if the element is external, false otherwise.</returns>
      public static bool IsElementExternal(Element element)
      {
         if (element == null)
            return false;

         Document document = element.Document;

         // Look for a parameter "IsExternal", potentially localized.
         {
            ElementId elementId = element.Id;

            bool? maybeIsExternal = null;
            if (!ExporterCacheManager.IsExternalParameterValueCache.TryGetValue(elementId, out maybeIsExternal))
            {
               int intIsExternal = 0;
               string localExternalParamName = PropertySetEntryUtil.GetLocalizedIsExternal(ExporterCacheManager.LanguageType);
               if ((localExternalParamName != null) && (ParameterUtil.GetIntValueFromElementOrSymbol(element, localExternalParamName, out intIsExternal) != null))
                  maybeIsExternal = (intIsExternal != 0);

               if (!maybeIsExternal.HasValue && (ExporterCacheManager.LanguageType != LanguageType.English_USA))
               {
                  string externalParamName = PropertySetEntryUtil.GetLocalizedIsExternal(LanguageType.English_USA);
                  if (ParameterUtil.GetIntValueFromElementOrSymbol(element, externalParamName, out intIsExternal) != null)
                     maybeIsExternal = (intIsExternal != 0);
               }

               ExporterCacheManager.IsExternalParameterValueCache.Add(new KeyValuePair<ElementId, bool?>(elementId, maybeIsExternal));
            }

            if (maybeIsExternal.HasValue)
               return maybeIsExternal.Value;
         }

         // Many element types have the FUNCTION_PARAM parameter.  If this is set, use its value.
         ElementType elementType = document.GetElement(element.GetTypeId()) as ElementType;
         int elementFunction;
         if ((elementType != null) && ParameterUtil.GetIntValueFromElement(elementType, BuiltInParameter.FUNCTION_PARAM, out elementFunction) != null)
         {
            // Note that the WallFunction enum value is the same for many different kinds of objects.
            return elementFunction != ((int)WallFunction.Interior);
         }

         // Specific element types that know if they are external or not if the built-in parameter isn't set.
         // Categories are used, and not types, to also support in-place families 

         // Roofs are always external
         ElementId categoryId = element.Category.Id;
         if (categoryId == new ElementId(BuiltInCategory.OST_Roofs) ||
             categoryId == new ElementId(BuiltInCategory.OST_MassExteriorWall))
            return true;

         // Mass interior walls are always internal
         if (categoryId == new ElementId(BuiltInCategory.OST_MassInteriorWall))
            return false;

         // Family instances may be hosted on an external element
         if (element is FamilyInstance)
         {
            FamilyInstance familyInstance = element as FamilyInstance;
            Element familyInstanceHost = familyInstance.Host;
            if (familyInstanceHost == null)
            {
               Reference familyInstanceHostReference = familyInstance.HostFace;
               if (familyInstanceHostReference != null)
                  familyInstanceHost = document.GetElement(familyInstanceHostReference);
            }

            if (familyInstanceHost != null)
               return IsElementExternal(familyInstanceHost);
         }

         return false;
      }

      /// <summary>
      /// Creates an association between a material handle and an instance handle.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="instanceHandle">The IFC instance handle.</param>
      /// <param name="materialId">The material id.</param>
      public static void CreateMaterialAssociation(ExporterIFC exporterIFC, IFCAnyHandle instanceHandle, ElementId materialId)
      {
         // Create material association if any.
         if (materialId != ElementId.InvalidElementId)
         {
            IFCAnyHandle materialNameHandle = GetOrCreateMaterialHandle(exporterIFC, materialId);

            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(materialNameHandle))
               ExporterCacheManager.MaterialRelationsCache.Add(materialNameHandle, instanceHandle);
         }
      }

      /// <summary>
      /// Creates an association between a list of material handles and an instance handle.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="instanceHandle">The IFC instance handle.</param>
      /// <param name="materialId">The list of material ids.</param>
      public static void CreateMaterialAssociations(ExporterIFC exporterIFC, IFCAnyHandle instanceHandle, ICollection<ElementId> materialList)
      {
         // Create material association if any.
         IList<IFCAnyHandle> materials = new List<IFCAnyHandle>();
         foreach (ElementId materialId in materialList)
         {
            if (materialId != ElementId.InvalidElementId)
            {
               IFCAnyHandle matHnd = GetOrCreateMaterialHandle(exporterIFC, materialId);
               if (!IFCAnyHandleUtil.IsNullOrHasNoValue(matHnd))
                  materials.Add(matHnd);
            }
         }

         if (materials.Count == 0)
            return;

         if (materials.Count == 1)
         {
            ExporterCacheManager.MaterialRelationsCache.Add(materials[0], instanceHandle);
            return;
         }

         IFCAnyHandle materialListHnd = IFCInstanceExporter.CreateMaterialList(exporterIFC.GetFile(), materials);
         ExporterCacheManager.MaterialRelationsCache.Add(materialListHnd, instanceHandle);
      }

      public static IFCAnyHandle GetOrCreateMaterialStyle(Document document, ExporterIFC exporterIFC, ElementId materialId)
      {
         IFCAnyHandle styleHnd = ExporterCacheManager.MaterialIdToStyleHandleCache.Find(materialId);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(styleHnd))
         {
            Material material = document.GetElement(materialId) as Material;
            if (material == null)
               return null;

            string matName = material.Name;

            Color color = GetSafeColor(material.Color);
            double blueVal = color.Blue / 255.0;
            double greenVal = color.Green / 255.0;
            double redVal = color.Red / 255.0;

            IFCFile file = exporterIFC.GetFile();
            IFCAnyHandle colorHnd = IFCInstanceExporter.CreateColourRgb(file, null, redVal, greenVal, blueVal);

            double transparency = ((double)material.Transparency) / 100.0;
            IFCData smoothness = IFCDataUtil.CreateAsNormalisedRatioMeasure(((double)material.Smoothness) / 100.0);

            IFCData specularExp = IFCDataUtil.CreateAsSpecularExponent(material.Shininess);

            IFCReflectanceMethod method = IFCReflectanceMethod.NotDefined;

            IFCAnyHandle renderingHnd = IFCInstanceExporter.CreateSurfaceStyleRendering(file, colorHnd, transparency,
                null, null, null, null, smoothness, specularExp, method);

            ISet<IFCAnyHandle> surfStyles = new HashSet<IFCAnyHandle>();
            surfStyles.Add(renderingHnd);

            IFCSurfaceSide surfSide = IFCSurfaceSide.Both;
            styleHnd = IFCInstanceExporter.CreateSurfaceStyle(file, matName, surfSide, surfStyles);
            ExporterCacheManager.MaterialIdToStyleHandleCache.Register(materialId, styleHnd);
         }

         return styleHnd;
      }

      /// <summary>
      /// Gets material handle from material id or creates one if there is none.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="materialId">The material id.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle GetOrCreateMaterialHandle(ExporterIFC exporterIFC, ElementId materialId)
      {
         Document document = ExporterCacheManager.Document;
         IFCAnyHandle materialNameHandle = ExporterCacheManager.MaterialHandleCache.Find(materialId);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(materialNameHandle))
         {
            string materialName = " <Unnamed>";
            if (materialId != ElementId.InvalidElementId)
            {
               Material material = document.GetElement(materialId) as Material;
               if (material != null)
               {
                  materialName = material.Name;
               }
            }
            materialNameHandle = IFCInstanceExporter.CreateMaterial(exporterIFC.GetFile(), materialName);

            ExporterCacheManager.MaterialHandleCache.Register(materialId, materialNameHandle);

            // associate Material with SurfaceStyle if necessary.
            IFCFile file = exporterIFC.GetFile();
            if (materialId != ElementId.InvalidElementId && !ExporterCacheManager.ExportOptionsCache.ExportAs2x2 && materialNameHandle.HasValue)
            {
               HashSet<IFCAnyHandle> matRepHandles = IFCAnyHandleUtil.GetHasRepresentation(materialNameHandle);
               if (matRepHandles.Count == 0)
               {
                  Material matElem = document.GetElement(materialId) as Material;

                  ElementId fillPatternId = (matElem != null) ? matElem.CutPatternId : ElementId.InvalidElementId;
                  Autodesk.Revit.DB.Color color = (matElem != null) ? GetSafeColor(matElem.CutPatternColor) : new Color(0, 0, 0);

                  double planScale = 100.0;

                  HashSet<IFCAnyHandle> styles = new HashSet<IFCAnyHandle>();

                  bool hasFill = false;

                  IFCAnyHandle styledRepItem = null;
                  IFCAnyHandle matStyleHnd = CategoryUtil.GetOrCreateMaterialStyle(document, exporterIFC, materialId);
                  if (!IFCAnyHandleUtil.IsNullOrHasNoValue(matStyleHnd))
                  {
                     styles.Add(matStyleHnd);

                     bool supportCutStyles = !ExporterCacheManager.ExportOptionsCache.ExportAsCoordinationView2;
                     if (fillPatternId != ElementId.InvalidElementId && supportCutStyles)
                     {
                        IFCAnyHandle cutStyleHnd = exporterIFC.GetOrCreateFillPattern(fillPatternId, color, planScale);
                        if (cutStyleHnd.HasValue)
                        {
                           styles.Add(cutStyleHnd);
                           hasFill = true;
                        }
                     }

                     IFCAnyHandle presStyleHnd = IFCInstanceExporter.CreatePresentationStyleAssignment(file, styles);

                     HashSet<IFCAnyHandle> presStyleSet = new HashSet<IFCAnyHandle>();
                     presStyleSet.Add(presStyleHnd);

                     IFCAnyHandle styledItemHnd = IFCInstanceExporter.CreateStyledItem(file, styledRepItem, presStyleSet, null);

                     IFCAnyHandle contextOfItems = exporterIFC.Get3DContextHandle("");

                     string repId = "Style";
                     string repType = (hasFill) ? "Material and Cut Pattern" : "Material";
                     HashSet<IFCAnyHandle> repItems = new HashSet<IFCAnyHandle>();
                     repItems.Add(styledItemHnd);

                     IFCAnyHandle styleRepHnd = IFCInstanceExporter.CreateStyledRepresentation(file, contextOfItems, repId, repType, repItems);

                     List<IFCAnyHandle> repList = new List<IFCAnyHandle>();
                     repList.Add(styleRepHnd);

                     IFCAnyHandle matDefRepHnd = IFCInstanceExporter.CreateMaterialDefinitionRepresentation(file, null, null, repList, materialNameHandle);
                  }
               }
            }
         }
         return materialNameHandle;
      }
   }
}
