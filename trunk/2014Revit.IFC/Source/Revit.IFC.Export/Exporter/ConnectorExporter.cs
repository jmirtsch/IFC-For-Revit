﻿//
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
using System.Diagnostics;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit;

using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Exporter
{
    /// <summary>
    /// Provides methods to export MEP Connectors.
    /// </summary>
    class ConnectorExporter
    {
        /// <summary>
        /// Exports a connector instance. Almost verbatim exmaple from Revit 2012 API for Connector Class
        /// Works only for HVAC and Piping for now
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        public static void Export(ExporterIFC exporterIFC)
        {
            foreach (ConnectorSet connectorSet in ExporterCacheManager.MEPCache.MEPConnectors)
            {
                Export(exporterIFC, connectorSet);
            }
            // clear local cache 
            ConnectorExporter.ClearConnections();
        }

        // If originalConnector != null, use that connector for AddConnection routine, instead of connector.
        private static void ProcessConnections(ExporterIFC exporterIFC, Connector connector, Connector originalConnector)
        {
            bool isElectricalDomain = (connector.Domain == Domain.DomainElectrical);

            if (connector.ConnectorType == ConnectorType.End ||
                connector.ConnectorType == ConnectorType.Curve ||
                connector.ConnectorType == ConnectorType.Physical)
            {

                if (connector.IsConnected)
                {
                    ConnectorSet connectorSet = connector.AllRefs;
                    ConnectorSetIterator csi = connectorSet.ForwardIterator();

                    while (csi.MoveNext())
                    {
                        Connector connected = csi.Current as Connector;
                        if (connected != null && connected.Owner != null && connector.Owner != null)
                        {
                            if (connected.Owner.Id != connector.Owner.Id)
                            {
                                // look for physical connections
                                if (connected.ConnectorType == ConnectorType.End ||
                                    connected.ConnectorType == ConnectorType.Curve ||
                                    connected.ConnectorType == ConnectorType.Physical)
                                {
                                    Connector originalConnectorToUse = (originalConnector != null) ? originalConnector : connector;
                                    FlowDirectionType flowDirection = isElectricalDomain ? FlowDirectionType.Bidirectional : connector.Direction;
                                    if (flowDirection == FlowDirectionType.Out)
                                    {
                                        AddConnection(exporterIFC, connected, originalConnectorToUse, false, isElectricalDomain);
                                    }
                                    else
                                    {
                                        bool isBiDirectional = (flowDirection == FlowDirectionType.Bidirectional);
                                        AddConnection(exporterIFC, originalConnectorToUse, connected, isBiDirectional, isElectricalDomain);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Exports a connector instance. Almost verbatim exmaple from Revit 2012 API for Connector Class
        /// Works only for HVAC and Piping for now
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="connectors">The ConnectorSet object.</param>
        private static void Export(ExporterIFC exporterIFC, ConnectorSet connectors)
        {
            IFCFile file = exporterIFC.GetFile();
            using (IFCTransaction tr = new IFCTransaction(file))
            {
                foreach (Connector connector in connectors)
                {
                    try
                    {
                        if (connector != null)
                            ProcessConnections(exporterIFC, connector, null);
                    }
                    catch (System.Exception)
                    {
                        // Log an error here
                    }
                }
                tr.Commit();
            }
        }

        static IFCAnyHandle CreateLocalPlacementForConnector(ExporterIFC exporterIFC, Connector connector, IFCAnyHandle elementHandle,
            IFCFlowDirection flowDir)
        {
            try
            {
                IFCFile file = exporterIFC.GetFile();

                IFCAnyHandle elementPlacement = IFCAnyHandleUtil.GetObjectPlacement(elementHandle);
                Transform origTrf = ExporterIFCUtils.GetUnscaledTransform(exporterIFC, elementPlacement);

                Transform connectorCoordinateSystem = connector.CoordinateSystem;
                if (flowDir == IFCFlowDirection.Sink)
                {
                    // Reverse the direction of the connector.
                    connectorCoordinateSystem.BasisX = -connectorCoordinateSystem.BasisX;
                    connectorCoordinateSystem.BasisZ = -connectorCoordinateSystem.BasisZ;
                }

                Transform relTransform = origTrf.Inverse.Multiply(connectorCoordinateSystem);
                XYZ scaledOrigin = UnitUtil.ScaleLength(relTransform.Origin);
                 
                IFCAnyHandle relLocalPlacement = ExporterUtil.CreateAxis2Placement3D(file,
                    scaledOrigin, relTransform.BasisZ, relTransform.BasisX);

                return IFCInstanceExporter.CreateLocalPlacement(file, elementPlacement, relLocalPlacement);
            }
            catch
            {
            }
            return null;
        }

        static void AddConnection(ExporterIFC exporterIFC, Connector connector, Connector connected, bool isBiDirectional, bool isElectricalDomain)
        {
            Element inElement = connector.Owner;
            Element outElement = connected.Owner;

            if (isElectricalDomain)
            {
                // We may get a connection back to the original element.  Ignore it.
                if (inElement.Id == outElement.Id)
                    return;

                // Check the outElement to see if it is a Wire; if so, get its connections and "skip" the wire.
                if (outElement is Wire)
                {
                    if (m_ProcessedWires.Contains(outElement.Id))
                        return;
                    m_ProcessedWires.Add(outElement.Id);

                    try
                    {
                        ConnectorSet wireConnectorSet = MEPCache.GetConnectorsForWire(outElement as Wire);
                        if (wireConnectorSet != null)
                        {
                            foreach (Connector connectedToWire in wireConnectorSet)
                                ProcessConnections(exporterIFC, connectedToWire, connector);
                        }
                    }
                    catch
                    {
                    }
                    return;
                }
            }

            // Check if the connection already exist
            if (ConnectionExists(inElement.Id, outElement.Id))
                return;

            if (isBiDirectional)
            {
                if (ConnectionExists(outElement.Id, inElement.Id))
                    return;
            }

            IFCAnyHandle inElementIFCHandle = ExporterCacheManager.MEPCache.Find(inElement.Id);
            IFCAnyHandle outElementIFCHandle = ExporterCacheManager.MEPCache.Find(outElement.Id);

            if (inElementIFCHandle == null || outElementIFCHandle == null ||
                !IFCAnyHandleUtil.IsSubTypeOf(inElementIFCHandle, IFCEntityType.IfcElement)
                || !IFCAnyHandleUtil.IsSubTypeOf(outElementIFCHandle, IFCEntityType.IfcElement))
                return;

            IFCFile ifcFile = exporterIFC.GetFile();
            IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();
            IFCAnyHandle portOut = null;
            IFCAnyHandle portIn = null;
            // ----------------------- In Port ----------------------
            {
                string guid = GUIDUtil.CreateGUID();
                IFCFlowDirection flowDir = (isBiDirectional) ? IFCFlowDirection.SourceAndSink : IFCFlowDirection.Sink;

                IFCAnyHandle localPlacement = CreateLocalPlacementForConnector(exporterIFC, connector, inElementIFCHandle, flowDir);
                portIn = IFCInstanceExporter.CreateDistributionPort(ifcFile, guid, ownerHistory, null, null, null, localPlacement, null, flowDir);

                // Attach the port to the element
                guid = GUIDUtil.CreateGUID();
                IFCAnyHandle connectorIn = IFCInstanceExporter.CreateRelConnectsPortToElement(ifcFile, guid, ownerHistory, null, null, portIn, inElementIFCHandle);
            }

            // ----------------------- Out Port----------------------
            {
                string guid = GUIDUtil.CreateGUID();
                IFCFlowDirection flowDir = (isBiDirectional) ? IFCFlowDirection.SourceAndSink : IFCFlowDirection.Source;

                IFCAnyHandle localPlacement = CreateLocalPlacementForConnector(exporterIFC, connected, outElementIFCHandle, flowDir);
                portOut = IFCInstanceExporter.CreateDistributionPort(ifcFile, guid, ownerHistory, null, null, null, localPlacement, null, flowDir);

                // Attach the port to the element
                guid = GUIDUtil.CreateGUID();
                IFCAnyHandle connectorOut = IFCInstanceExporter.CreateRelConnectsPortToElement(ifcFile, guid, ownerHistory, null, null, portOut, outElementIFCHandle);
            }

            //  ----------------------- Out Port -> In Port ----------------------
            if (portOut != null && portIn != null)
            {
                Element elemToUse = (inElement.Id.IntegerValue < outElement.Id.IntegerValue) ? inElement : outElement;
                string guid = GUIDUtil.CreateGUID();
                IFCAnyHandle realizingElement = null;
                IFCInstanceExporter.CreateRelConnectsPorts(ifcFile, guid, ownerHistory, null, null, portIn, portOut, realizingElement);
                AddConnectionInternal(inElement.Id, outElement.Id);
            }

            // Add the handles to the connector system.
            HashSet<MEPSystem> systemList = new HashSet<MEPSystem>();
            try
            {
                MEPSystem system = connector.MEPSystem;
                if (system != null)
                    systemList.Add(system);
            }
            catch
            {
            }

            try
            {
                MEPSystem system = connected.MEPSystem;
                if (system != null)
                    systemList.Add(system);
            }
            catch
            {
            }

            if (isElectricalDomain)
            {
                foreach (MEPSystem system in systemList)
                {
                    ExporterCacheManager.SystemsCache.AddElectricalSystem(system.Id);
                    ExporterCacheManager.SystemsCache.AddHandleToElectricalSystem(system.Id, inElementIFCHandle);
                    ExporterCacheManager.SystemsCache.AddHandleToElectricalSystem(system.Id, outElementIFCHandle);
                    ExporterCacheManager.SystemsCache.AddHandleToElectricalSystem(system.Id, portIn);
                    ExporterCacheManager.SystemsCache.AddHandleToElectricalSystem(system.Id, portOut);
                }
            }
            else
            {
                foreach (MEPSystem system in systemList)
                {
                    ExporterCacheManager.SystemsCache.AddHandleToBuiltInSystem(system, inElementIFCHandle);
                    ExporterCacheManager.SystemsCache.AddHandleToBuiltInSystem(system, outElementIFCHandle);
                    ExporterCacheManager.SystemsCache.AddHandleToBuiltInSystem(system, portIn);
                    ExporterCacheManager.SystemsCache.AddHandleToBuiltInSystem(system, portOut);
                }
            }
        }

        /// <summary>
        /// Keeps track of created connection to prevent duplicate connections, 
        /// might not be necessary
        /// </summary>
        private static HashSet<string> m_ConnectionExists = new HashSet<string>();

        /// <summary>
        /// Keeps track of created connection to prevent duplicate connections, 
        /// might not be necessary
        /// </summary>
        private static HashSet<ElementId> m_ProcessedWires = new HashSet<ElementId>();

        /// <summary>
        /// Checks existance of the connects
        /// </summary>
        /// <param name="inID">ElementId of the incoming Element</param>
        /// <param name="outID">ElementId of the outgoing Element</param>
        /// <returns>True if the connection exists already</returns>
        private static bool ConnectionExists(ElementId inID, ElementId outID)
        {
            string elementIdKey = inID.ToString() + "_" + outID.ToString();
            return m_ConnectionExists.Contains(elementIdKey);
        }

        /// <summary>
        /// Add new Connection
        /// </summary>
        /// <param name="inID"></param>
        /// <param name="outID"></param>
        private static void AddConnectionInternal(ElementId inID, ElementId outID)
        {
            string elementIdKey = inID.ToString() + "_" + outID.ToString();
            m_ConnectionExists.Add(elementIdKey);
        }

        /// <summary>
        /// Clear the connection cache
        /// </summary>
        public static void ClearConnections()
        {
            m_ConnectionExists.Clear();
            m_ProcessedWires.Clear();
        }
    }
}
