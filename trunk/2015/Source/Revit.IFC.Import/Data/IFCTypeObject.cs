//
// Revit IFC Import library: this library works with Autodesk(R) Revit(R) to import IFC files.
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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
    /// <summary>
    /// Represents an IfcTypeObject.
    /// </summary>
    public class IFCTypeObject : IFCObjectDefinition
    {
        protected IDictionary<string, IFCPropertySetDefinition> m_IFCPropertySets;

        /// <summary>
        /// The property sets.
        /// </summary>
        public IDictionary<string, IFCPropertySetDefinition> PropertySets
        {
            get { return m_IFCPropertySets; }
        }

        protected IFCTypeObject()
        {
        }
        
        /// <summary>
        /// Constructs an IFCTypeObject from the IfcTypeObject handle.
        /// </summary>
        /// <param name="ifcTypeObject">The IfcTypeObject handle.</param>
        protected IFCTypeObject(IFCAnyHandle ifcTypeObject)
        {
            Process(ifcTypeObject);
        }

        /// <summary>
        /// Gets the shape type of the IfcTypeOject, depending on the file version and entity type.
        /// </summary>
        /// <param name="ifcObjectDefinition">The associated handle.</param>
        /// <returns>The shape type string, if any.</returns>
        protected override string GetShapeType(IFCAnyHandle ifcObjectDefinition)
        {
            // List of entities we explictly declare have no PredefinedType, to avoid exception.
            if (IFCImportFile.TheFile.SchemaVersion != IFCSchemaVersion.IFC4)
            {
                if (EntityType == IFCEntityType.IfcDoorStyle ||
                    EntityType == IFCEntityType.IfcFastenerType ||
                    EntityType == IFCEntityType.IfcFurnishingElementType ||
                    EntityType == IFCEntityType.IfcFurnitureType ||
                    EntityType == IFCEntityType.IfcWindowStyle)
                    return null;
            }

            // Most IfcTypeObjects have a PredefinedType.  Use try/catch for those that don't.
            try
            {
                return IFCAnyHandleUtil.GetEnumerationAttribute(ifcObjectDefinition, "PredefinedType");
            }
            catch
            {
            }

            return null;
        }

        /// <summary>
        /// Processes IfcTypeObject attributes.
        /// </summary>
        /// <param name="ifcTypeObject">The IfcTypeObject handle.</param>
        protected override void Process(IFCAnyHandle ifcTypeObject)
        {
            base.Process(ifcTypeObject);

            HashSet<IFCAnyHandle> propertySets = IFCAnyHandleUtil.GetAggregateInstanceAttribute
                <HashSet<IFCAnyHandle>>(ifcTypeObject, "HasPropertySets");

            if (propertySets != null)
            {
                m_IFCPropertySets = new Dictionary<string, IFCPropertySetDefinition>();

                foreach (IFCAnyHandle propertySet in propertySets)
                {
                    IFCPropertySetDefinition ifcPropertySetDefinition = IFCPropertySetDefinition.ProcessIFCPropertySetDefinition(propertySet);
                    if (ifcPropertySetDefinition != null)
                    {
                        string name = ifcPropertySetDefinition.Name;
                        if (string.IsNullOrWhiteSpace(name))
                            name = IFCAnyHandleUtil.GetEntityType(propertySet).ToString() + " " + propertySet.StepId;
                        m_IFCPropertySets[name] = ifcPropertySetDefinition;
                    }
                }
            }
        }

        /// <summary>
        /// Processes an IfcTypeObject.
        /// </summary>
        /// <param name="ifcTypeObject">The IfcTypeObject handle.</param>
        /// <returns>The IFCTypeObject object.</returns>
        public static IFCTypeObject ProcessIFCTypeObject(IFCAnyHandle ifcTypeObject)
        {
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcTypeObject))
            {
                IFCImportFile.TheLog.LogNullError(IFCEntityType.IfcTypeObject);
                return null;
            }

            IFCEntity typeObject;
            if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcTypeObject.StepId, out typeObject))
                return (typeObject as IFCTypeObject);

            if (IFCAnyHandleUtil.IsSubTypeOf(ifcTypeObject, IFCEntityType.IfcTypeProduct))
            {
                return IFCTypeProduct.ProcessIFCTypeProduct(ifcTypeObject);
            }
            
            return new IFCTypeObject(ifcTypeObject);
        }

        /// <summary>
        /// Creates or populates Revit elements based on the information contained in this class.
        /// </summary>
        /// <param name="doc">The document.</param>
        protected override void Create(Document doc)
        {
            
            string guid = GlobalId.ToString();
            DirectShapeType shapeType = Importer.TheCache.UseElementByGUID<DirectShapeType>(doc, guid);
            
            if (shapeType == null)
                shapeType = DirectShapeType.Create(doc, GetName("Direct Shape Type"), m_CategoryId);

            if (shapeType == null)
                throw new InvalidOperationException("Couldn't create DirectShapeType for IfcTypeObject.");

            m_CreatedElementId = shapeType.Id;

            base.Create(doc);

            TraverseSubElements(doc);
        }

        /// <summary>
        /// Create property sets for a given element.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="element">The element being created.</param>
        /// <param name="propertySetsCreated">A concatenated string of property sets created, used to filter schedules.</returns>
        public override void CreatePropertySets(Document doc, Element element, string propertySetsCreated)
        {
            if (PropertySets != null && PropertySets.Count > 0)
            {
                IFCParameterSetByGroup parameterGroupMap = IFCParameterSetByGroup.Create(element);
                foreach (IFCPropertySetDefinition propertySet in PropertySets.Values)
                {
                    string newPropertySetCreated = propertySet.CreatePropertySet(doc, element, parameterGroupMap);
                    if (propertySetsCreated == null)
                        propertySetsCreated = newPropertySetCreated;
                    else
                        propertySetsCreated += ";" + newPropertySetCreated;
                }
                Parameter propertySetList = element.LookupParameter("Type IfcPropertySetList");
                if (propertySetList != null)
                    propertySetList.Set(propertySetsCreated);
            }
        }

        /// <summary>
        /// Cleans out the IFCEntity to save memory.
        /// </summary>
        public override void CleanEntity()
        {
            // Don't do anything; IfcTypeObjects will be accessed multiple times.
        }
    }
}
