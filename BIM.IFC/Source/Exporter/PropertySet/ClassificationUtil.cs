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
using BIM.IFC.Utility;
using BIM.IFC.Toolkit;
using System.Text.RegularExpressions;
using Revit.IFC.Common.Extensions;

namespace BIM.IFC.Exporter.PropertySet
{
    /// <summary>
    /// Provides static methods to create varies IFC classifications.
    /// </summary>
    class ClassificationUtil
    {
        /// <summary>
        /// Creates uniformat classification.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC.</param>
        /// <param name="file">The file.</param>
        /// <param name="element">The element.</param>
        /// <param name="elemHnd">The element handle.</param>
        public static void CreateUniformatClassification(ExporterIFC exporterIFC, IFCFile file, Element element, IFCAnyHandle elemHnd)
        {
            // Create Uniformat classification, if it is not set.
            string uniformatKeyString = "Uniformat";
            string uniformatCode = "";
            if (ParameterUtil.GetStringValueFromElementOrSymbol(element, BuiltInParameter.UNIFORMAT_CODE, false, out uniformatCode) == null)
                ParameterUtil.GetStringValueFromElementOrSymbol(element, "Assembly Code", out uniformatCode);
            string uniformatDescription = "";

            if (!String.IsNullOrWhiteSpace(uniformatCode))
            {
                if (ParameterUtil.GetStringValueFromElementOrSymbol(element, BuiltInParameter.UNIFORMAT_DESCRIPTION, false, out uniformatDescription) == null)
                    ParameterUtil.GetStringValueFromElementOrSymbol(element, "Assembly Description", out uniformatDescription);
            }

            IFCAnyHandle classification;
            if (!ExporterCacheManager.ClassificationCache.ClassificationHandles.TryGetValue(uniformatKeyString, out classification))
            {
                classification = IFCInstanceExporter.CreateClassification(file, "http://www.csiorg.net/uniformat", "1998", null, uniformatKeyString);
                ExporterCacheManager.ClassificationCache.ClassificationHandles.Add(uniformatKeyString, classification);
            }

                InsertClassificationReference(exporterIFC, file, element, elemHnd, uniformatKeyString, uniformatCode, uniformatDescription, "http://www.csiorg.net/uniformat" );

        }

        /// <summary>
        /// Create IfcClassification references from hardwired or custom classification code fields.
        /// </summary>
        /// <param name="exporterIFC">The exporterIFC class.</param>
        /// <param name="file">The IFC file class.</param>
        /// <param name="element">The element to export.</param>
        /// <param name="elemHnd">The corresponding IFC entity handle.</param>
        /// <returns>True if a classification or classification reference is created.</returns>
        public static bool CreateClassification(ExporterIFC exporterIFC, IFCFile file, Element element, IFCAnyHandle elemHnd)
        {
            bool createdClassification = false;

            string paramClassificationCode = "";
            string baseClassificationCodeFieldName = "ClassificationCode";
            IList<string> customClassificationCodeNames = new List<string>();

            string classificationName = null;
            string classificationCode = null;
            string classificationDescription = null;

            int customPass = 0;
            int standardPass = 1;
            int numCustomCodes = ExporterCacheManager.ClassificationCache.CustomClassificationCodeNames.Count;
            
            while (true)
            {
                // Create a classification, if it is not set.
                string classificationCodeFieldName = null;
                if (customPass < numCustomCodes)
                {
                    classificationCodeFieldName = ExporterCacheManager.ClassificationCache.CustomClassificationCodeNames[customPass];
                    customPass++;
                }
                else
                {
                    classificationCodeFieldName = baseClassificationCodeFieldName;
                    if (standardPass > 1)
                        classificationCodeFieldName += "(" + standardPass + ")";
                    standardPass++;
                }

                if (ParameterUtil.GetStringValueFromElementOrSymbol(element, classificationCodeFieldName, out paramClassificationCode) == null)
                    break;

                parseClassificationCode(paramClassificationCode, out classificationName, out classificationCode, out classificationDescription);

                    if (string.IsNullOrEmpty(classificationName))
                {
                    if (!ExporterCacheManager.ClassificationCache.FieldNameToClassificationNames.TryGetValue(classificationCodeFieldName, out classificationName))
                        classificationName = "Default Classification";
                }

                    IFCAnyHandle classification;
                if (!ExporterCacheManager.ClassificationCache.ClassificationHandles.TryGetValue(classificationName, out classification))
                    {
                        IFCClassification savedClassification = new IFCClassification();
                    if (ExporterCacheManager.ClassificationCache.ClassificationsByName.TryGetValue(classificationName, out savedClassification))
                        {
                            if (savedClassification.ClassificationEditionDate == null)
                            {
                                IFCAnyHandle editionDate = IFCInstanceExporter.CreateCalendarDate(file, savedClassification.ClassificationEditionDate.Day, savedClassification.ClassificationEditionDate.Month, savedClassification.ClassificationEditionDate.Year);

                                classification = IFCInstanceExporter.CreateClassification(file, savedClassification.ClassificationSource, savedClassification.ClassificationEdition,
                                    editionDate, savedClassification.ClassificationName);
                            }
                            else
                        {
                                classification = IFCInstanceExporter.CreateClassification(file, savedClassification.ClassificationSource, savedClassification.ClassificationEdition,
                                    null, savedClassification.ClassificationName);
                        }

                        if (!String.IsNullOrEmpty(savedClassification.ClassificationLocation))
                            ExporterCacheManager.ClassificationLocationCache.Add(classificationName, savedClassification.ClassificationLocation);
                    }
                        else
                    {
                            classification = IFCInstanceExporter.CreateClassification(file, "", "", null, classificationName);
                    }

                    ExporterCacheManager.ClassificationCache.ClassificationHandles.Add(classificationName, classification);
                    createdClassification = true;
                    }

                string location = null;
                        ExporterCacheManager.ClassificationLocationCache.TryGetValue(classificationName, out location);
                {
                        InsertClassificationReference(exporterIFC, file, element, elemHnd, classificationName, classificationCode, classificationDescription, location);
                    createdClassification = true;
                }
            }

            return createdClassification;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="paramClassificationCode"></param>
        /// <param name="classificationName"></param>
        /// <param name="classificationCode"></param>
        /// <param name="classificationDescription"></param>
        /// <returns></returns>
        private static int parseClassificationCode(string paramClassificationCode, out string classificationName, out string classificationCode, out string classificationDescription)
        {
            // Processing the following format: [<classification name>] <classification code> | <classification description>
            // Partial format will also be supported as long as it follows: (following existing OmniClass style for COBIe, using :)
            //          <classification code>
            //          <classification code> : <classification description>
            //          [<Classification name>] <classification code>
            //          [<Classification name>] <classification code> : <classification description>

            // Will be nice to use a single Regular expression if I have mastered the use of the regular expression. For now use simple Split method

            classificationName = null;
            classificationCode = null;
            classificationDescription = null;
            int noCodepart = 0;

            if (string.IsNullOrWhiteSpace(paramClassificationCode))
                return noCodepart;     // do nothing if it is empty
            string[] splitResult1 = paramClassificationCode.Split(new Char[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries);
            if (splitResult1.Length > 1)
            {
                // found [<classification Name>]
                classificationName = splitResult1[0].Trim();
                noCodepart++;
            }
            splitResult1 = splitResult1[splitResult1.Length - 1].Split(new Char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            classificationCode = splitResult1[0].Trim();
            noCodepart++;

            if (splitResult1.Length > 1)
            {
                classificationDescription = splitResult1[1].Trim();
                noCodepart++;
            }

            return noCodepart;
        }

/// <summary>
        /// Inserts a new classification reference in the ClassificationCache.
        /// </summary>
        /// <param name="exporterIFC">The exporterIFC class.</param>
        /// <param name="file">The IFC file class.</param>
        /// <param name="element">The element.</param>
        /// <param name="elemHnd">The corresponding IFC entity handle.</param>
        /// <param name="classificationKeyString">The classification name.</param>
        /// <param name="classificationCode">The classification code.</param>
        /// <param name="classificationDescription">The classification description.</param>
        /// <param name="location">The location of the classification.</param>
        public static void InsertClassificationReference(ExporterIFC exporterIFC, IFCFile file, Element element, IFCAnyHandle elemHnd, string classificationKeyString, string classificationCode, string classificationDescription, string location)
        {
            IFCAnyHandle classification;

            // Check whether Classification is already defined before
            if (!ExporterCacheManager.ClassificationCache.ClassificationHandles.TryGetValue(classificationKeyString, out classification))
            {
                classification = IFCInstanceExporter.CreateClassification(file, "", "", null, classificationKeyString);
                ExporterCacheManager.ClassificationCache.ClassificationHandles.Add(classificationKeyString, classification);
            }

            IFCAnyHandle classificationReference = IFCInstanceExporter.CreateClassificationReference(file,
               location, classificationCode, classificationDescription, classification);

            HashSet<IFCAnyHandle> relatedObjects = new HashSet<IFCAnyHandle>();
            relatedObjects.Add(elemHnd);

            IFCAnyHandle relAssociates = IFCInstanceExporter.CreateRelAssociatesClassification(file, GUIDUtil.CreateGUID(),
               exporterIFC.GetOwnerHistoryHandle(), classificationKeyString+" Classification", "", relatedObjects, classificationReference);

        }
    }
}
