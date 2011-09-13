//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2011  Autodesk, Inc.
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
using System.Text;
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;


namespace BIM.IFC.Utility
{
    /// <summary>
    /// Provides static methods for geometry related manipulations.
    /// </summary>
    class GeometryUtil
    {
        /// <summary>
        /// Creates a default plane.
        /// </summary>
        /// <remarks>
        /// The origin of the plane is (0, 0, 0) and the normal is (0, 0, 1).
        /// </remarks>
        /// <returns>
        /// The Plane.
        /// </returns>
        public static Plane CreateDefaultPlane()
        {
            XYZ normal = new XYZ(0, 0, 1);
            XYZ origin = XYZ.Zero;
            return new Plane(normal, origin);
        }

        /// <summary>
        /// Determines if curve loop is counterclockwise.
        /// </summary>
        /// <param name="curveLoop">
        /// The curveLoop.
        /// </param>
        /// <param name="normal">
        /// The normal.
        /// </param>
        /// <returns>
        /// Returns true only if the loop is counterclockwise, false otherwise.
        /// </returns>
        public static bool IsIFCLoopCCW(CurveLoop curveLoop, XYZ normal)
        {
            if (curveLoop == null)
                throw new Exception("CurveLoop is null.");

            // If loop is not suitable for ccw evaluation an exception is thrown
            return curveLoop.IsCounterclockwise(normal);
        }

        /// <summary>
        /// Moves curve along the direction.
        /// </summary>
        /// <param name="originalCurve">
        /// The curve.
        /// </param>
        /// <param name="direction">
        /// The direction.
        /// </param>
        /// <returns>
        /// The moved curve.
        /// </returns>
        public static Curve MoveCurve(Curve originalCurve, XYZ direction)
        {
            Transform moveTrf = Transform.get_Translation(direction);
            return originalCurve.get_Transformed(moveTrf);
        }

        /// <summary>
        /// Checks if curve is line or arc.
        /// </summary>
        /// <param name="curve">
        /// The curve.
        /// </param>
        /// <returns>
        /// True if the curve is line or arc, false otherwise.
        /// </returns>
        public static bool CurveIsLineOrArc(Curve curve)
        {
            return curve is Line || curve is Arc;
        }

        /// <summary>
        /// Reverses curve loop.
        /// </summary>
        /// <param name="curveloop">
        /// The curveloop.
        /// </param>
        /// <returns>
        /// The reversed curve loop.
        /// </returns>
        public static CurveLoop ReverseOrientation(CurveLoop curveloop)
        {
            CurveLoop copyOfCurveLoop = CurveLoop.CreateViaCopy(curveloop);
            copyOfCurveLoop.Flip();
            return copyOfCurveLoop;
        }

        /// <summary>
        /// Gets origin, X direction and curve bound from a curve.
        /// </summary>
        /// <param name="curve">
        /// The curve.
        /// </param>
        /// <param name="curveBounds">
        /// The output curve bounds.
        /// </param>
        /// <param name="xDirection">
        /// The output X direction.
        /// </param>
        /// <param name="origin">
        /// The output origin.
        /// </param>
        public static void GetAxisAndRangeFromCurve(Curve curve,
           out UV curveBounds, out XYZ xDir, out XYZ orig)
        {
            curveBounds = new UV(curve.get_EndParameter(0), curve.get_EndParameter(1));
            orig = curve.Evaluate(curveBounds.U, false);
            Transform trf = curve.ComputeDerivatives(curveBounds.U, false);
            xDir = trf.get_Basis(0);
        }
    }
}
