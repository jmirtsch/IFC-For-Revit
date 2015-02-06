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
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
    /// <summary>
    /// Represents an IfcPort.
    /// </summary>
    public abstract class IFCPort : IFCProduct
    {
        private IFCElement m_ContainedIn = null;

        private IFCPort m_ConnectedFrom = null; 
        
        private IFCPort m_ConnectedTo = null;

        /// <summary>
        /// The IFCElement that contains this port.
        /// </summary>
        public IFCElement ContainedIn 
        {
            get { return m_ContainedIn; }
            protected set { m_ContainedIn = value; }
        }

        /// <summary>
        /// The IFCPort connected to this port.  Ports are required to be connected in pairs.
        /// Either ConnectedFrom or ConnectedTo will be null.
        /// </summary>
        public IFCPort ConnectedFrom
        {
            get { return m_ConnectedFrom; }
            protected set { m_ConnectedFrom = value; }
        }
        
        /// <summary>
        /// The IFCPort connected to this port.  Ports are required to be connected in pairs.
        /// Either ConnectedFrom or ConnectedTo will be null.
        /// </summary>
        public IFCPort ConnectedTo
        {
            get { return m_ConnectedTo; }
            protected set { m_ConnectedTo = value; }
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        protected IFCPort()
        {

        }

        /// <summary>
        /// Processes IfcPort attributes.
        /// </summary>
        /// <param name="ifcPort">The IfcPort handle.</param>
        protected override void Process(IFCAnyHandle ifcPort)
        {
            base.Process(ifcPort);

            //IFCAnyHandle ifcContainedIn = IFCImportHandleUtil.GetOptionalInstanceAttribute(ifcPort, "ContainedIn");
            // ProcessIFCRelation.ProcessRelatedElement does a null check on the handle.
            //ContainedIn = ProcessIFCRelation.ProcessRelatedElement(ifcContainedIn);

            HashSet<IFCAnyHandle> containedIn = IFCAnyHandleUtil.GetAggregateInstanceAttribute<HashSet<IFCAnyHandle>>(ifcPort, "ContainedIn");
            if (containedIn != null && containedIn.Count != 0)
            {
                // ProcessIFCRelation.ProcessRelatedElement does a null check on the handle.
                ContainedIn = ProcessIFCRelation.ProcessRelatedElement(containedIn.First());
            }

            HashSet<IFCAnyHandle> connectedFrom = IFCAnyHandleUtil.GetAggregateInstanceAttribute<HashSet<IFCAnyHandle>>(ifcPort, "ConnectedFrom");
            if (connectedFrom != null && connectedFrom.Count != 0)
            {
                // ProcessIFCRelation.ProcessRelatedElement does a null check on the handle.
                ConnectedFrom = ProcessIFCRelation.ProcessRelatingPort(connectedFrom.First());
            }

            HashSet<IFCAnyHandle> connectedTo = IFCAnyHandleUtil.GetAggregateInstanceAttribute<HashSet<IFCAnyHandle>>(ifcPort, "ConnectedTo");
            if (connectedTo != null && connectedTo.Count != 0)
            {
                // ProcessIFCRelation.ProcessRelatedElement does a null check on the handle.
                ConnectedTo = ProcessIFCRelation.ProcessRelatedPort(connectedTo.First());
            }
        }

        /// <summary>
        /// Creates or populates Revit element params based on the information contained in this class.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="element">The element.</param>
        protected override void CreateParametersInternal(Document doc, Element element)
        {
            base.CreateParametersInternal(doc, element);

            if (element != null)
            {
                if (ContainedIn != null)
                {
                    string guid = ContainedIn.GlobalId;
                    if (!string.IsNullOrWhiteSpace(guid))
                        IFCPropertySet.AddParameterString(doc, element, "IfcElement ContainedIn IfcGUID", guid, Id);

                    string name = ContainedIn.Name;
                    if (!string.IsNullOrWhiteSpace(name))
                        IFCPropertySet.AddParameterString(doc, element, "IfcElement ContainedIn Name", name, Id);
                }

                if (ConnectedFrom != null)
                {
                    string guid = ConnectedFrom.GlobalId;
                    if (!string.IsNullOrWhiteSpace(guid))
                        IFCPropertySet.AddParameterString(doc, element, "IfcPort ConnectedFrom IfcGUID", guid, Id);

                    string name = ConnectedFrom.Name;
                    if (!string.IsNullOrWhiteSpace(name))
                        IFCPropertySet.AddParameterString(doc, element, "IfcPort ConnectedFrom Name", name, Id);
                }

                if (ConnectedTo != null)
                {
                    string guid = ConnectedTo.GlobalId;
                    if (!string.IsNullOrWhiteSpace(guid))
                        IFCPropertySet.AddParameterString(doc, element, "IfcPort ConnectedTo IfcGUID", guid, Id);

                    string name = ConnectedTo.Name;
                    if (!string.IsNullOrWhiteSpace(name))
                        IFCPropertySet.AddParameterString(doc, element, "IfcPort ConnectedTo Name", name, Id);
                }
            }
        }

        /// <summary>
        /// Processes an IfcPort object.
        /// </summary>
        /// <param name="ifcPort">The IfcPort handle.</param>
        /// <returns>The IFCPort object.</returns>
        public static IFCPort ProcessIFCPort(IFCAnyHandle ifcPort)
        {
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcPort))
            {
                IFCImportFile.TheLog.LogNullError(IFCEntityType.IfcPort);
                return null;
            }

            try
            {
                IFCEntity cachedPort;
                if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcPort.StepId, out cachedPort))
                    return (cachedPort as IFCPort);

                if (IFCAnyHandleUtil.IsSubTypeOf(ifcPort, IFCEntityType.IfcDistributionPort))
                    return IFCDistributionPort.ProcessIFCDistributionPort(ifcPort);
            }
            catch (Exception ex)
            {
                if (ex.Message != "Don't Import")
                    IFCImportFile.TheLog.LogError(ifcPort.StepId, ex.Message, false);
                return null;
            }

            IFCImportFile.TheLog.LogUnhandledSubTypeError(ifcPort, IFCEntityType.IfcPort, false);
            return null;
        }

        /// <summary>
        /// Cleans out the IFCEntity to save memory.
        /// </summary>
        public override void CleanEntity()
        {
            // Don't do anything; IfcPorts will be accessed multiple times.
        }
    }
}
