//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2011  Autodesk, Inc.
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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;


namespace BIM.IFC.Utility
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
        /// Gets IFC type name of an element.
        /// </summary>
        /// <param name="element">
        /// The element.
        /// </param>
        /// <returns>
        /// The IFC type name.
        /// </returns>
        public static string GetIFCEnumTypeName(ExporterIFC exporterIFC, Element element)
        {
            string ifcEnumType = ExporterIFCUtils.GetIFCType(element, exporterIFC);

            return ifcEnumType;
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
                return material.Color;
            }
            else
            {
                Color color = category.LineColor;

                // Grey is returned in place of pure black.  For systems which default to a black background color, 
                // Grey is more of a contrast.  
                if (!color.IsValid || (color.Red == 0 && color.Green == 0 && color.Blue == 0))
                {
                    color = new Color(0x7f, 0x7f, 0x7f);
                }
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
        /// <param name="element">
        /// The element.
        /// </param>
        /// <returns>
        /// True if the element is external, false otherwise.
        /// </returns>
        public static bool IsElementExternal(Element element)
        {
            if (element == null)
                return false;

            // Look for a parameter "IsExternal"
            Document document = element.Document;
            String externalParamName = "IsExternal";
            Parameter instParameter = element.get_Parameter(externalParamName);
            bool isExternal = false;
            if (instParameter != null && instParameter.HasValue && instParameter.StorageType == StorageType.Integer)
            {
                isExternal = instParameter.AsInteger() != 0;
                return isExternal;
            }

            if (instParameter == null)
            {
                ElementType elementType = document.get_Element(element.GetTypeId()) as ElementType;

                if (elementType != null)
                {
                    Parameter typeParameter = elementType.get_Parameter(externalParamName);

                    if (typeParameter != null && typeParameter.HasValue && typeParameter.StorageType == StorageType.Integer)
                    {
                        isExternal = typeParameter.AsInteger() != 0;
                        return isExternal;
                    }
                }
            }

            // Specific element types that know if they are external or not 
            // Categories are used, and not types, to also support in-place families 

            // Roofs are always external
            ElementId categoryId = element.Category.Id;
            if (categoryId == new ElementId(BuiltInCategory.OST_Roofs))
                return true;

            // Wall types have the function parameter 
            if (categoryId == new ElementId(BuiltInCategory.OST_Walls))
            {
                ElementType wallType = document.get_Element(element.GetTypeId()) as ElementType;
                Parameter wallFunction = wallType.get_Parameter(BuiltInParameter.FUNCTION_PARAM);

                if (wallFunction != null)
                {
                    return (wallFunction.AsInteger() != (int)WallFunction.Interior);
                }
            }


            // Family instances may be hosted on an external element
            if (element is FamilyInstance)
            {
                FamilyInstance familyInstance = (FamilyInstance)element;
                Reference famInstanceHostRef = familyInstance.HostFace;

                if (famInstanceHostRef != null)
                {
                    Element famInstanceHost = document.GetElement(famInstanceHostRef);
                    return IsElementExternal(famInstanceHost);
                }
            }

            return false;
        }

        /// <summary>
        /// Creates an association between a material handle and an instance handle.
        /// </summary>
        /// <param name="document">
        /// The Revit document.
        /// </param>
        /// <param name="exporterIFC">
        /// The ExporterIFC object.
        /// </param>
        /// <param name="instanceHandle">
        /// The IFC instance handle.
        /// </param>
        /// <param name="materialId">
        /// The material id.
        /// </param>
        public static void CreateMaterialAssociation(Document doc, ExporterIFC exporterIFC, IFCAnyHandle instanceHandle, ElementId materialId)
        {
            // Create material association if any.
            if (materialId != ElementId.InvalidElementId)
            {
                IFCAnyHandle materialNameHnd = exporterIFC.FindMaterialHandle(materialId);
                if (!materialNameHnd.HasValue)
                {
                    Material material = doc.get_Element(materialId) as Material;
                    if (material != null)
                    {
                        IFCLabel materialName = IFCLabel.Create(material.Name);
                        materialNameHnd = exporterIFC.GetFile().CreateMaterial(materialName);
                        exporterIFC.RegisterMaterialHandle(materialId, materialNameHnd);
                    }
                }

                if (materialNameHnd.HasValue)
                {
                    exporterIFC.RegisterMaterialRelation(materialNameHnd, instanceHandle);
                }
            }
        }
    }
}
