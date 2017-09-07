using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Revit.IFC.Common.Utility
{
   /// <summary>
   /// It is a class that captures IFC entities in their respective hierarchical inheritance structure, to be captured from the IFCXML schema
   /// It uses static dictionary and set!!
   /// </summary>
   public class IfcSchemaEntityTree
   {
      static SortedDictionary<string, IfcSchemaEntityNode> IfcEntityDict = new SortedDictionary<string, IfcSchemaEntityNode>();
      static HashSet<IfcSchemaEntityNode> rootNodes = new HashSet<IfcSchemaEntityNode>();

      /// <summary>
      /// Reset the static Dictionary and Set. To be done before parsing another IFC schema
      /// </summary>
      public static void ResetAll()
      {
         IfcEntityDict.Clear();
         rootNodes.Clear();
      }

      /// <summary>
      /// Property: the Entity Dictionary, which lists all the entities 
      /// </summary>
      public static IDictionary<string, IfcSchemaEntityNode> EntityDict
      {
         get { return IfcEntityDict; }
      }

      /// <summary>
      /// Property: the set containing the root nodes of the IFC entity tree
      /// </summary>
      public static HashSet<IfcSchemaEntityNode> TheTree
      {
         get { return rootNodes; }
      }

      /// <summary>
      /// Add a new node into the tree
      /// </summary>
      /// <param name="entityName">the entity name</param>
      /// <param name="parentNodeName">the name of the supertype entity</param>
      static public void Add(string entityName, string parentNodeName, bool isAbstract=false)
      {
         if (string.IsNullOrEmpty(entityName))
            return;

         // We will skip the entityname or its parent name that does not start with Ifc (except Entity)
         if (string.Compare(entityName, 0, "Ifc", 0, 3, ignoreCase: true) != 0
            || (string.Compare(parentNodeName, 0, "Ifc", 0, 3, ignoreCase: true) != 0 && string.Compare(parentNodeName,"Entity",ignoreCase:true)!=0) )
            return;

         IfcSchemaEntityNode parentNode = null;
         if (!string.IsNullOrEmpty(parentNodeName))
         {
            // skip if the parent name does not start with Ifc
            if (string.Compare(parentNodeName, 0, "Ifc", 0, 3, ignoreCase: true) == 0)
            {
               if (!IfcEntityDict.TryGetValue(parentNodeName, out parentNode))
               {
                  // Parent node does not exist yet, create
                  parentNode = new IfcSchemaEntityNode(parentNodeName);

                  IfcEntityDict.Add(parentNodeName, parentNode);
                  rootNodes.Add(parentNode);    // Add first into the rootNodes because the parent is null at this stage, we will remove it later is not the case
               }
            }
         }

         IfcSchemaEntityNode entityNode;
         if (!IfcEntityDict.TryGetValue(entityName, out entityNode))
         {
            if (parentNode != null)
            {
               entityNode = new IfcSchemaEntityNode(entityName, parentNode, abstractEntity:isAbstract);
               parentNode.AddChildNode(entityNode);
            }
            else
            {
               entityNode = new IfcSchemaEntityNode(entityName, abstractEntity:isAbstract);
               // Add into the set of root nodes when parent is null/no parent
               rootNodes.Add(entityNode);
            }

            IfcEntityDict.Add(entityName, entityNode);
         }
         else
         {
            // Update the node's isAbstract property and the parent node (if any)
            entityNode.isAbstract = isAbstract;
            if (parentNode != null)
            {
               entityNode.SetParentNode(parentNode);
               if (rootNodes.Contains(entityNode))
                  rootNodes.Remove(entityNode);
               parentNode.AddChildNode(entityNode);
            }
         }
      }

      /// <summary>
      /// Find whether an entity is already created before
      /// </summary>
      /// <param name="entityName">the entity in concern</param>
      /// <returns>the entity node in the tree</returns>
      static public IfcSchemaEntityNode Find(string entityName)
      {
         IfcSchemaEntityNode res = null;
         IfcEntityDict.TryGetValue(entityName, out res);
         return res;
      }

      /// <summary>
      /// Check whether an entity is a subtype of another entity
      /// </summary>
      /// <param name="subTypeName">candidate of the subtype entity name</param>
      /// <param name="superTypeName">candidate of the supertype entity name</param>
      /// <returns>true: if the the subTypeName is the subtype of supertTypeName</returns>
      static public bool IsSubTypeOf(string subTypeName, string superTypeName)
      {
         IfcSchemaEntityNode theNode = Find(subTypeName);
         if (theNode != null)
            return theNode.IsSubTypeOf(superTypeName);

         return false;
      }

      /// <summary>
      /// Check whether an entity is a supertype of another entity
      /// </summary>
      /// <param name="superTypeName">candidate of the supertype entity name</param>
      /// <param name="subTypeName">candidate of the subtype entity name</param>
      /// <returns>true: if the the superTypeName is the supertype of subtTypeName</returns>
      static public bool IsSuperTypeOf(string superTypeName, string subTypeName)
      {
         IfcSchemaEntityNode theNode = Find(superTypeName);
         if (theNode != null)
            return theNode.IsSuperTypeOf(subTypeName);

         return false;

      }

      /// <summary>
      /// Dump the IFC entity names in a list
      /// </summary>
      /// <param name="listName">a name of the list</param>
      /// <returns>the list dump in a string</returns>
      static public string DumpEntityDict(string listName)
      {
         string entityList;
         entityList = "namespace Revit.IFC.Common.Enums." + listName
                     + "\n{\n\tpublic enum IFCEntityType"
                     + "\n\t{";
          
         foreach (KeyValuePair<string,IfcSchemaEntityNode> ent in IfcEntityDict)
         {
            entityList += "\n\t\t" + ent.Key + ",";
         }
         entityList += "\n\t\tUnknown"
                     + "\n\t}"
                     + "\n}";

         return entityList;
      }

      /// <summary>
      /// Dump the IFC entity hierarchical tree
      /// </summary>
      /// <returns>the IFC entity tree in a string</returns>
      static public string DumpTree()
      {
         string tree = string.Empty;
         foreach(IfcSchemaEntityNode rootNode in rootNodes)
         {
            tree += rootNode.PrintBranch();
         }

         return tree;
      }
   }
}
