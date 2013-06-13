//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
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
using System.IO;

namespace BIM.IFC.Utility
{
    class PropertyMap
    {
        public static Dictionary<Tuple<string, string>, string> LoadParameterMap()
        {
            Dictionary<Tuple<string, string>, string> parameterMap = new Dictionary<Tuple<string, string>, string>();
            try
            {
                string filename = GetFilename();
                if (File.Exists(filename))
                {
                    using (StreamReader sr = new StreamReader(filename))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                            ParseLine(parameterMap, line);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }

            return parameterMap;
        }

        private static void ParseLine(Dictionary<Tuple<string, string>, string> parameterMap, string line)
        {
            // if not a comment
            if (line[0] != '#')
            {
                // add the line
                string[] split = line.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (split.Count() == 3)
                    parameterMap.Add(Tuple.Create(split[0], split[1]), split[2]);
            }
        }

        private static string GetFilename()
        {
            string directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            return directory + @"\ParameterMappingTable.txt";
        }
    }
}
