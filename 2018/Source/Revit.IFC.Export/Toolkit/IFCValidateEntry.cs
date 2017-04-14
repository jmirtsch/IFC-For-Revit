using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;

using Revit.IFC.Export.Utility;

namespace Revit.IFC.Export.Toolkit
{
    public static class IFCValidateEntry
    {
        /// <summary>
        /// Get the IFC type from shared parameters, or from a type name.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="typeName">The original value.</param>
        /// <returns>The found value.</returns>
        public static string GetValidIFCType(Element element, string typeName)
        {
            return GetValidIFCType(element, typeName, null);
        }

        /// <summary>
        /// Get the IFC type from shared parameters, from a type name, or from a default value.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="typeName">The type value.</param>
        /// <param name="defaultValue">A default value that can be null.</param>
        /// <returns>The found value.</returns>
        public static string GetValidIFCType(Element element, string typeName, string defaultValue)
        {
            string value = null;
            if ((ParameterUtil.GetStringValueFromElementOrSymbol(element, "IfcExportType", out value) == null) && // change IFCType to consistent parameter of IfcExportType
                (ParameterUtil.GetStringValueFromElementOrSymbol(element, "IfcType", out value) == null))  // support IFCType for legacy support
                value = typeName;

            if (String.IsNullOrEmpty(value))
            {
                if (!String.IsNullOrEmpty(defaultValue))
                    return defaultValue;
                return "NotDefined";
            }
            return value;
        }

        /// <summary>
        /// Get the IFC type from shared parameters, from a type name, or from a default value.
        /// </summary>
        /// <typeparam name="TEnum">The type of Enum.</typeparam>
        /// <param name="element">The element.</param>
        /// <param name="typeName">The type value.</param>
        /// <param name="defaultValue">A default value that can be null.</param>
        /// <returns>The found value, or null.</returns>
        public static string GetValidIFCType<TEnum>(Element element, string typeName, string defaultValue) where TEnum : struct
        {
            string value = null;
            bool canUseTypeName = true;
            if ((ParameterUtil.GetStringValueFromElementOrSymbol(element, "IfcExportType", out value) == null) && // change IFCType to consistent parameter of IfcExportType
                (ParameterUtil.GetStringValueFromElementOrSymbol(element, "IfcType", out value) == null))  // support IFCType for legacy support
            {
                canUseTypeName = false;
                value = typeName;
            }

            if (ValidateStrEnum<TEnum>(value) == null && value != "NotDefined")
            {
                if (canUseTypeName)
                {
                    value = typeName;
                    if (ValidateStrEnum<TEnum>(value) == null)
                        value = null;
                }
                else
                    value = null;
            }

            if (String.IsNullOrEmpty(value))
            {
                if (!String.IsNullOrEmpty(defaultValue))
                    return defaultValue;

                // We used to return "NotDefined" here.  However, that assumed that all types had "NotDefined" as a value.
                // It is better to return null.
                return null;
            }

            return value;
        }

        /// <summary>
        /// Validates that a string belongs to an Enum class.
        /// </summary>
        /// <typeparam name="TEnum">The type of Enum.</typeparam>
        /// <param name="strEnumCheck">The string to check.</param>
        /// <returns>The original string, if valid, or null.</returns>
        public static string ValidateStrEnum<TEnum>(string strEnumCheck) where TEnum : struct
        {
            TEnum enumValue;

            if (typeof(TEnum).IsEnum)
            {
                // We used to return "NotDefined" here.  However, that assumed that all types had "NotDefined" as a value.
                // It is better to return null.
                if (!Enum.TryParse(strEnumCheck, true, out enumValue))
                    return null;
            }
            return strEnumCheck;
        }

        public static TEnum ValidateEntityEnum<TEnum> (TEnum entityCheck)
        {
            return entityCheck;
        }
    }
}
