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
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
    public abstract class IFCSweptAreaSolid : IFCSolidModel
    {
        IFCProfile m_SweptArea = null;

        Transform m_Position = null;

        /// <summary>
        /// The swept area profile of the IfcSweptAreaSolid.
        /// </summary>
        public IFCProfile SweptArea
        {
            get { return m_SweptArea; }
            protected set { m_SweptArea = value; }
        }

        /// <summary>
        /// The local coordinate system of the IfcSweptAreaSolid.
        /// </summary>
        public Transform Position
        {
            get { return m_Position; }
            protected set { m_Position = value; }
        }

        protected IFCSweptAreaSolid()
        {
        }

        override protected void Process(IFCAnyHandle solid)
        {
            base.Process(solid);

            IFCAnyHandle sweptArea = IFCImportHandleUtil.GetRequiredInstanceAttribute(solid, "SweptArea", true);
            SweptArea = IFCProfile.ProcessIFCProfile(sweptArea);

            IFCAnyHandle location = IFCImportHandleUtil.GetRequiredInstanceAttribute(solid, "Position", false);
            if (location != null)
                Position = IFCLocation.ProcessIFCAxis2Placement(location);
            else
                Position = Transform.Identity;
        }

        protected IFCSweptAreaSolid(IFCAnyHandle solid)
        {
        }

        private void GetTransformedCurveLoopsFromProfile(IFCProfile profile, Transform lcs, IList<CurveLoop> loops)
        {
            if (profile is IFCSimpleProfile)
            {
                IFCSimpleProfile simpleSweptArea = profile as IFCSimpleProfile;

                // It is legal for simpleSweptArea.Position to be null, for example for IfcArbitraryClosedProfileDef.
                Transform sweptAreaPosition =
                    (simpleSweptArea.Position == null) ? lcs : lcs.Multiply(simpleSweptArea.Position);

                CurveLoop currLoop = simpleSweptArea.OuterCurve;
                if (currLoop == null || currLoop.Count() == 0)
                {
                    IFCImportFile.TheLog.LogError(profile.Id, "No outer curve loop for profile, ignoring.", false);
                    return;
                }
                loops.Add(IFCGeometryUtil.CreateTransformed(currLoop, sweptAreaPosition));

                if (simpleSweptArea.InnerCurves != null)
                {
                    foreach (CurveLoop innerCurveLoop in simpleSweptArea.InnerCurves)
                        loops.Add(IFCGeometryUtil.CreateTransformed(innerCurveLoop, sweptAreaPosition));
                }
            }
            else if (profile is IFCCompositeProfile)
            {
                IFCCompositeProfile compositeSweptArea = profile as IFCCompositeProfile;

                foreach (IFCProfile subProfile in compositeSweptArea.Profiles)
                    GetTransformedCurveLoopsFromProfile(subProfile, lcs, loops);
            }
            else
            {
                // TODO: Support.
                IFCImportFile.TheLog.LogError(Id, "SweptArea Profile #" + profile.Id + " not yet supported.", false);
            }
        }

        protected IList<CurveLoop> GetTransformedCurveLoops(Transform lcs)
        {
            IList<CurveLoop> loops = new List<CurveLoop>();
            GetTransformedCurveLoopsFromProfile(SweptArea, lcs, loops);
            return loops;
        }

        /// <summary>
        /// Create an IFCSolidModel object from a handle of type IfcSweptAreaSolid.
        /// </summary>
        /// <param name="ifcSweptAreaSolid">The IFC handle.</param>
        /// <returns>The IFCSweptAreaSolid object.</returns>
        public static IFCSweptAreaSolid ProcessIFCSweptAreaSolid(IFCAnyHandle ifcSweptAreaSolid)
        {
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcSweptAreaSolid))
            {
                IFCImportFile.TheLog.LogNullError(IFCEntityType.IfcSweptAreaSolid);
                return null;
            }

            if (IFCAnyHandleUtil.IsSubTypeOf(ifcSweptAreaSolid, IFCEntityType.IfcExtrudedAreaSolid))
                return IFCExtrudedAreaSolid.ProcessIFCExtrudedAreaSolid(ifcSweptAreaSolid);
            if (IFCAnyHandleUtil.IsSubTypeOf(ifcSweptAreaSolid, IFCEntityType.IfcRevolvedAreaSolid))
                return IFCRevolvedAreaSolid.ProcessIFCRevolvedAreaSolid(ifcSweptAreaSolid);
            if (IFCAnyHandleUtil.IsSubTypeOf(ifcSweptAreaSolid, IFCEntityType.IfcSurfaceCurveSweptAreaSolid))
                return IFCSurfaceCurveSweptAreaSolid.ProcessIFCSurfaceCurveSweptAreaSolid(ifcSweptAreaSolid);

            IFCImportFile.TheLog.LogUnhandledSubTypeError(ifcSweptAreaSolid, IFCEntityType.IfcSweptAreaSolid, true);
            return null;
        }
    }
}
