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
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;

using GeometryGym.Ifc;

namespace Revit.IFC.Export.Exporter.PropertySet
{
    /// <summary>
    /// Provides static methods to create varies IFC properties.
    /// </summary>
    public class PositivePlaneAnglePropertyUtil : PropertyUtil
    {
        /// <summary>
        /// Create a label property.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IfcProperty CreatePositivePlaneAngleMeasureProperty(DatabaseIfc db, string propertyName, double value, PropertyValueType valueType)
        {
            // Ensure it is positive.  Don't throw, but should tell user.
            if (value <= MathUtil.Eps())
                return null;

            switch (valueType)
            {
                case PropertyValueType.EnumeratedValue:
                    return new IfcPropertyEnumeratedValue(db, propertyName, new IfcPositivePlaneAngleMeasure(value));
                case PropertyValueType.SingleValue:
                    return new IfcPropertySingleValue(db, propertyName, new IfcPositivePlaneAngleMeasure(value));
                default:
                    throw new InvalidOperationException("Missing case!");
            }
        }

        /// <summary>
        /// Create a label property, or retrieve from cache.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created or cached property handle.</returns>
        public static IfcProperty CreatePositivePlaneAngleMeasurePropertyFromCache(DatabaseIfc db, string propertyName, double value, PropertyValueType valueType)
        {
            // We have a partial cache here - we will only cache multiples of 15 degrees.
            bool canCache = false;
            double degreesDiv15 = Math.Floor(value / 15.0 + 0.5);
            double integerDegrees = degreesDiv15 * 15.0;
            if (MathUtil.IsAlmostEqual(value, integerDegrees))
            {
                canCache = true;
                value = integerDegrees;
            }

            IfcProperty propertyHandle;
            if (canCache)
            {
                propertyHandle = ExporterCacheManager.PropertyInfoCache.PositivePlaneAngleCache.Find(propertyName, value);
                if (propertyHandle != null)
                    return propertyHandle;
            }

            propertyHandle = CreatePositivePlaneAngleMeasureProperty(db, propertyName, value, valueType);

            if (canCache && propertyHandle != null)
            {
                ExporterCacheManager.PropertyInfoCache.PositivePlaneAngleCache.Add(propertyName, value, propertyHandle);
            }

            return propertyHandle;
        }

        /// <summary>
        /// Create a plane angle measure property from the element's or type's parameter.
        /// </summary>
        /// <param name="file">The IFC file.</param>
        /// <param name="elem">The Element.</param>
        /// <param name="revitParameterName">The name of the parameter.</param>
        /// <param name="ifcPropertyName">The name of the property.</param>
        /// <param name="valueType">The value type of the property.</param>
        /// <returns>The created property handle.</returns>
        public static IfcProperty CreatePositivePlaneAngleMeasurePropertyFromElementOrSymbol(DatabaseIfc db, Element elem, string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
        {
            double propertyValue;
            if (ParameterUtil.GetDoubleValueFromElement(elem, null, revitParameterName, out propertyValue) != null)
            {
                propertyValue = UnitUtil.ScaleAngle(propertyValue);
                return CreatePositivePlaneAngleMeasurePropertyFromCache(db, ifcPropertyName, propertyValue, valueType);
            }
            // For Symbol
            Document document = elem.Document;
            ElementId typeId = elem.GetTypeId();
            Element elemType = document.GetElement(typeId);
            if (elemType != null)
                return CreatePositivePlaneAngleMeasurePropertyFromElementOrSymbol(db, elemType, revitParameterName, ifcPropertyName, valueType);
            else
                return null;
        }
    }
}
