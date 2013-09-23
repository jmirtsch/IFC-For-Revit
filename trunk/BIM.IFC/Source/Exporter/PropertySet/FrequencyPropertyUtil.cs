﻿//
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
using BIM.IFC.Utility;
using BIM.IFC.Toolkit;

namespace BIM.IFC.Exporter.PropertySet
{
    /// <summary>
    /// Provides static methods to create varies IFC properties.  Inherit from PropertyUtil for protected helper functions.
    /// </summary>
    public class FrequencyPropertyUtil : PropertyUtil
    {
        /// <summary>Create a FrequencyMeasure property.</summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateFrequencyProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
        {
            switch (valueType)
            {
                case PropertyValueType.EnumeratedValue:
                    {
                        IList<IFCData> valueList = new List<IFCData>();
                        valueList.Add(IFCDataUtil.CreateAsFrequencyMeasure(value));
                        return IFCInstanceExporter.CreatePropertyEnumeratedValue(file, propertyName, null, valueList, null);
                    }
                case PropertyValueType.SingleValue:
                    return IFCInstanceExporter.CreatePropertySingleValue(file, propertyName, null, IFCDataUtil.CreateAsFrequencyMeasure(value), null);
                default:
                    throw new InvalidOperationException("Missing case!");
            }
        }

        /// <summary>
        /// Create a Frequency measure property from the element's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateFrequencyPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
            string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
        {
            double propertyValue;
            if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue) != null)
                return CreateFrequencyProperty(file, ifcPropertyName, propertyValue, valueType);
            return null;
        }

        /// <summary>
        /// Create a Frequency measure property from the element's or type's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IFCAnyHandle CreateFrequencyPropertyFromElementOrSymbol(IFCFile file, ExporterIFC exporterIFC, Element elem,
            string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
        {
            IFCAnyHandle propHnd = CreateFrequencyPropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                return propHnd;

            if (revitBuiltInParam != BuiltInParameter.INVALID)
            {
                string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
                propHnd = CreateFrequencyPropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
                if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                    return propHnd;
            }

            // For Symbol
            Document document = elem.Document;
            ElementId typeId = elem.GetTypeId();
            Element elemType = document.GetElement(typeId);
            if (elemType != null)
                return CreateFrequencyPropertyFromElementOrSymbol(file, exporterIFC, elemType, revitParameterName, revitBuiltInParam, ifcPropertyName, valueType);
            else
                return null;
        }
    }
}
