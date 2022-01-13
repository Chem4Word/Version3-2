// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Chem4Word.Model2.Geometry;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.ACME.Drawing
{
    /// <summary>
    ///     Static class to handle bond geometries
    /// </summary>
    public static class BondGeometry
    {
        /// <summary>
        ///  Returns the geometry of a wedge bond.  Hatch bonds use the same geometry
        ///     but a different brush.
        /// </summary>
        /// <param name="desc">Descriptor defining the bond shape</param>
        /// <param name="perp">perpendicular vector to the bond</param>
        public static void GetWedgePoints(WedgeBondLayout desc, Vector perp)
        {
            desc.FirstCorner = desc.End + perp;

            desc.SecondCorner = desc.End - perp;

            desc.Boundary.AddRange(new[] { desc.Start, desc.FirstCorner, desc.SecondCorner });
        }

        /// <summary>
        /// Gets the geometry of a wedge bond.
        /// </summary>
        /// <param name="desc">WedgeBondDescriptor which is populated</param>
        /// <param name="standardBondLength">Standard bond length as defined by the model</param>
        /// <param name="standoff">Boundary width between atom label and bond terminus</param>
        public static void GetWedgeBondGeometry(WedgeBondLayout desc, double standardBondLength, double standoff)
        {
            //get the width of the wedge bond's thick end
            var bondVector = desc.PrincipleVector;
            var perpVector = bondVector.Perpendicular();
            perpVector.Normalize();
            perpVector *= standardBondLength * Globals.BondOffsetPercentage;

            // shrink the bond so it doesn't overlap any AtomVisuals
            AdjustTerminus(ref desc.Start, desc.End, desc.StartAtomHull, standoff);
            AdjustTerminus(ref desc.End, desc.Start, desc.EndAtomHull, standoff);

            //then draw it
            GetWedgePoints(desc, perpVector);
            //and pass it back as a Geometry
            StreamGeometry sg;
            sg = desc.GetOutline();
            sg.Freeze();
            desc.DefiningGeometry = sg;
        }

        /// <summary>
        ///     Defines the three parallel lines of a Triple bond.
        /// </summary>
        /// <param name="standardBondLength">Standard bond length as defined by the model</param>
        /// <param name="standoff">Boundary width between atom label and bond terminus</param>
        /// <param name="descriptor.StartAtomHull">AtomVisual defining the starting atom</param>
        /// <param name="descriptor.EndAtomHull">AtomVisual defining the end atom</param>
        /// <returns></returns>
        public static void GetTripleBondGeometry(TripleBondLayout descriptor, double standardBondLength, double standoff)
        {
            //start by getting the six points that define a standard triple bond
            GetTripleBondPoints(descriptor, standardBondLength, standoff);
            //and draw it
            var sg = new StreamGeometry();
            using (var sgc = sg.Open())
            {
                sgc.BeginFigure(descriptor.Start, false, false);
                sgc.LineTo(descriptor.Start, true, false);
                sgc.BeginFigure(descriptor.SecondaryStart, false, false);
                sgc.LineTo(descriptor.SecondaryEnd, true, false);
                sgc.BeginFigure(descriptor.TertiaryStart, false, false);
                sgc.LineTo(descriptor.TertiaryEnd, true, false);
                sgc.Close();
            }

            sg.Freeze();
            descriptor.DefiningGeometry = sg;
        }

        /// <summary>
        /// 'Draws' the triple bond
        /// </summary>StartAtomVisual
        /// <param name="descriptor">TripleBondDescriptor which is populated</param>
        /// <param name="standardBondLength">Standard bond length as defined by the model</param>
        /// <param name="standoff">Boundary width between atom label and bond terminus</param>
        public static void GetTripleBondPoints(TripleBondLayout descriptor, double standardBondLength, double standoff)
        {
            //get a standard perpendicular vector
            var v = descriptor.PrincipleVector;
            var normal = v.Perpendicular();
            normal.Normalize();

            //offset the secondaries
            var distance = standardBondLength * Globals.BondOffsetPercentage;
            descriptor.SecondaryStart = descriptor.Start + normal * distance;
            descriptor.SecondaryEnd = descriptor.SecondaryStart + v;

            descriptor.TertiaryStart = descriptor.Start - normal * distance;
            descriptor.TertiaryEnd = descriptor.TertiaryStart + v;
            //adjust the line ends
            if (descriptor.StartAtomHull != null)
            {
                AdjustTerminus(ref descriptor.Start, descriptor.End, descriptor.StartAtomHull, standoff);
                AdjustTerminus(ref descriptor.SecondaryStart, descriptor.SecondaryEnd, descriptor.StartAtomHull, standoff);
                AdjustTerminus(ref descriptor.TertiaryStart, descriptor.TertiaryEnd, descriptor.StartAtomHull, standoff);
            }

            if (descriptor.EndAtomHull != null)
            {
                AdjustTerminus(ref descriptor.End, descriptor.Start, descriptor.EndAtomHull, standoff);
                AdjustTerminus(ref descriptor.SecondaryEnd, descriptor.SecondaryStart, descriptor.EndAtomHull, standoff);
                AdjustTerminus(ref descriptor.TertiaryEnd, descriptor.TertiaryStart, descriptor.EndAtomHull, standoff);
            }

            //and define the boundary for hit testing
            descriptor.Boundary.Clear();
            descriptor.Boundary.AddRange(new[]
                                              {
                                                  descriptor.SecondaryStart, descriptor.SecondaryEnd,
                                                  descriptor.TertiaryEnd, descriptor.TertiaryStart
                                              });
        }

        /// <summary>
        ///     draws the two parallel lines of a double bond
        ///     These bonds can either straddle the atom-atom line or fall to one or other side of it
        /// </summary>
        /// <param name="descriptor">DoubleBondDescriptor which is populated</param>
        /// <param name="standardBondLength">Standard bond length as defined by the model</param>
        /// <param name="standoff"></param>
        /// <returns></returns>
        public static void GetDoubleBondGeometry(DoubleBondLayout descriptor, double standardBondLength, double standoff)

        {
            //get the standard points for a double bond
            GetDoubleBondPoints(descriptor, standardBondLength);
            //adjust the line ends
            if (descriptor.StartAtomHull != null)
            {
                AdjustTerminus(ref descriptor.Start, descriptor.End, descriptor.StartAtomHull, standoff);
                AdjustTerminus(ref descriptor.SecondaryStart, descriptor.SecondaryEnd, descriptor.StartAtomHull, standoff);
            }

            if (descriptor.EndAtomHull != null)
            {
                AdjustTerminus(ref descriptor.End, descriptor.Start, descriptor.EndAtomHull, standoff);
                AdjustTerminus(ref descriptor.SecondaryEnd, descriptor.SecondaryStart, descriptor.EndAtomHull, standoff);
            }
            //and draw it
            var sg = new StreamGeometry();
            using (var sgc = sg.Open())
            {
                sgc.BeginFigure(descriptor.Start, false, false);
                sgc.LineTo(descriptor.End, true, false);
                sgc.BeginFigure(descriptor.SecondaryStart, false, false);
                sgc.LineTo(descriptor.SecondaryEnd, true, false);
                sgc.Close();
            }

            sg.Freeze();
            descriptor.DefiningGeometry = sg;
        }

        /// <summary>
        ///     Defines a double bond
        /// </summary>
        /// <param name="descriptor">DoubleBondDescriptor which is populated</param>
        /// <param name="standardBondLength">Standard bond length as defined by the model</param>
        /// <returns></returns>
        public static void GetDoubleBondPoints(DoubleBondLayout descriptor, double standardBondLength)
        {
            Point? descriptorSecondaryStart;
            Point? descriptorSecondaryEnd;

            //use a struct here to return the values
            GetDefaultDoubleBondPoints(descriptor, standardBondLength);

            if (descriptor.PrimaryCentroid != null)
            {
                //now, if there is a centroid defined, the bond is part of a ring
                Point? workingCentroid = null;
                //work out whether the bond is place inside or outside the ring
                var bondvector = descriptor.PrincipleVector;
                var centreVector = descriptor.PrimaryCentroid - descriptor.Start;

                var computedPlacement = (Globals.BondDirection)Math.Sign(Vector.CrossProduct(centreVector.Value, bondvector));

                if (descriptor.Placement != Globals.BondDirection.None)
                {
                    if (computedPlacement == descriptor.Placement) //then we have nothing to worry about
                    {
                        workingCentroid = descriptor.PrimaryCentroid;
                    }
                    else //we need to adjust the points according to the other centroid
                    {
                        workingCentroid = descriptor.SecondaryCentroid;
                    }
                }

                if (workingCentroid != null)
                {
                    var bondVector = (descriptor.End - descriptor.Start);
                    var midPoint = bondVector / 2 + descriptor.Start;

                    var perpAngle = Math.Abs(Vector.AngleBetween(workingCentroid.Value - midPoint, bondVector));

                    if (perpAngle >= 80 && perpAngle <= 100) //probably convex ring
                    {
                        //shorten the second bond to fit neatly within the ring
                        descriptorSecondaryStart = BasicGeometry.LineSegmentsIntersect(descriptor.Start, workingCentroid.Value,
                                                                      descriptor.SecondaryStart,
                                                                      descriptor.SecondaryEnd);
                        descriptorSecondaryEnd = BasicGeometry.LineSegmentsIntersect(descriptor.End, workingCentroid.Value,
                                                                      descriptor.SecondaryStart,
                                                                      descriptor.SecondaryEnd);
                        var tempPoint3 = descriptorSecondaryStart ?? descriptor.SecondaryStart;
                        var tempPoint4 = descriptorSecondaryEnd ?? descriptor.SecondaryEnd;

                        descriptor.SecondaryStart = tempPoint3;
                        descriptor.SecondaryEnd = tempPoint4;
                    }
                    else //probably concave ring, so shorten by half the bond offset value
                    {
                        if (descriptor.StartNeigbourPositions != null && descriptor.EndNeighbourPositions != null)
                        {
                            SplitBondAngles(ref descriptor.SecondaryStart, descriptor.SecondaryEnd,
                                            descriptor.StartNeigbourPositions, descriptor.Start);
                            SplitBondAngles(ref descriptor.SecondaryEnd, descriptor.SecondaryStart,
                                            descriptor.EndNeighbourPositions, descriptor.End);
                        }
                    }
                }
                else //no centroid so split the angles anyway
                {
                    if (descriptor.StartNeigbourPositions != null && descriptor.EndNeighbourPositions != null)
                    {
                        SplitBondAngles(ref descriptor.SecondaryStart, descriptor.SecondaryEnd,
                                        descriptor.StartNeigbourPositions, descriptor.Start);
                        SplitBondAngles(ref descriptor.SecondaryEnd, descriptor.SecondaryStart,
                                        descriptor.EndNeighbourPositions, descriptor.End);
                    }
                }

                //get the boundary for hit testing purposes
                descriptor.Boundary.Clear();
                descriptor.Boundary.AddRange(new[]
                                                  {
                                                      descriptor.Start, descriptor.End, descriptor.SecondaryEnd,
                                                      descriptor.SecondaryStart
                                                  });
            }
        }

        /// <summary>
        ///Splits the angle between a secondary bond and any neighbouring bonds.
        ///This is used in special cases where we can't get a meaningful centroid
        /// </summary>
        /// <param name="secondaryStart">Start of the double bond's secondary: to be clipped</param>
        /// <param name="secondaryEnd">End of the double bond's secondary</param>
        /// <param name="atomPosList">Positions of atoms attached to the primary bond's atom</param>
        /// <param name="primaryAtomPos">Position of the primary bond's atom</param>
        private static void SplitBondAngles(ref Point secondaryStart, Point secondaryEnd, List<Point> atomPosList, Point primaryAtomPos)
        {
            foreach (Point neighbourPos in atomPosList)
            {
                Point? intersection =
                    BasicGeometry.LineSegmentsIntersect(secondaryStart, secondaryEnd, primaryAtomPos, neighbourPos);
                if (intersection != null)
                {
                    //need to shorten the line again
                    Vector splitVector = neighbourPos - primaryAtomPos;
                    Vector secondaryVector = secondaryEnd - secondaryStart;
                    double splitAngle = Vector.AngleBetween(secondaryVector, splitVector) / 2;
                    Matrix rotator = new Matrix();
                    rotator.Rotate(-splitAngle);
                    splitVector = splitVector * rotator;
                    Point? temp = BasicGeometry.LineSegmentsIntersect(secondaryStart, secondaryEnd, primaryAtomPos,
                                                                       primaryAtomPos + splitVector);
                    if (temp != null)
                    {
                        secondaryStart = temp.Value;
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Gets an unadjusted set of points for a double bond
        /// </summary>
        /// <param name="descriptor">DoubleBondDescriptor which is populated</param>
        /// <param name="standardBondLength">Standard bond length as defined by the model</param>
        private static void GetDefaultDoubleBondPoints(DoubleBondLayout descriptor, double standardBondLength)
        {
            var v = descriptor.PrincipleVector;
            var normal = v.Perpendicular();
            normal.Normalize();

            var distance = standardBondLength * Globals.BondOffsetPercentage;
            //first, calculate the default bond points as if there were no rings involved
            var tempStart = descriptor.Start;
            //offset according to placement
            switch (descriptor.Placement)
            {
                //case BondDirection.None is covered by default

                case Globals.BondDirection.Clockwise:
                    {
                        descriptor.SecondaryStart = tempStart - normal * 2 * distance;
                        descriptor.SecondaryEnd = descriptor.SecondaryStart + v;

                        break;
                    }

                case Globals.BondDirection.Anticlockwise:
                    descriptor.SecondaryStart = tempStart + normal * 2 * distance;
                    descriptor.SecondaryEnd = descriptor.SecondaryStart + v;
                    break;

                default:
                    descriptor.Start = tempStart + normal * distance;
                    descriptor.End = descriptor.Start + v;

                    descriptor.SecondaryStart = tempStart - normal * distance;
                    descriptor.SecondaryEnd = descriptor.SecondaryStart + v;
                    break;
            }
        }

        /// <summary>
        /// Draws the crossed double bond to indicate indeterminate geometry
        /// </summary>
        /// <param name="descriptor">BondLayout to hadle the physical bond shape</param>
        /// <param name="standardBondLength">Model's standard bond length</param>
        /// <param name="standoff">Boundary width between atom label and bond terminus</param>
        public static void GetCrossedDoubleGeometry(DoubleBondLayout descriptor, double standardBondLength, double standoff)
        {
            var v = descriptor.PrincipleVector;
            var normal = v.Perpendicular();
            normal.Normalize();

            Point point1, point2, point3, point4;

            var distance = standardBondLength * Globals.BondOffsetPercentage;

            point1 = descriptor.Start + normal * distance;
            point2 = point1 + v;

            point3 = descriptor.Start - normal * distance;
            point4 = point3 + v;

            if (descriptor.StartAtomHull != null)
            {
                AdjustTerminus(ref point1, point2, descriptor.StartAtomHull, standoff);
                AdjustTerminus(ref point3, point4, descriptor.StartAtomHull, standoff);
            }

            if (descriptor.StartAtomHull != null)
            {
                AdjustTerminus(ref point2, point1, descriptor.StartAtomHull, standoff);
                AdjustTerminus(ref point4, point3, descriptor.StartAtomHull, standoff);
            }

            var sg = new StreamGeometry();
            using (var sgc = sg.Open())
            {
                sgc.BeginFigure(point1, false, false);
                sgc.LineTo(point4, true, false);
                sgc.BeginFigure(point2, false, false);
                sgc.LineTo(point3, true, false);
                sgc.Close();
            }

            sg.Freeze();
            descriptor.DefiningGeometry = sg;
            descriptor.Boundary.Clear();
            descriptor.Boundary.AddRange(new[] { point1, point2, point4, point3 });
        }

        public static void GetSingleBondGeometry(BondLayout descriptor, double standoff)
        {
            var start = descriptor.Start;
            var end = descriptor.End;

            var sg = new StreamGeometry();

            if (descriptor.StartAtomHull != null)
            {
                AdjustTerminus(ref start, end, descriptor.StartAtomHull, standoff);
            }

            if (descriptor.EndAtomHull != null)
            {
                AdjustTerminus(ref end, start, descriptor.EndAtomHull, standoff);
            }

            using (var sgc = sg.Open())
            {
                sgc.BeginFigure(start, false, false);
                sgc.LineTo(end, true, false);
                sgc.Close();
            }

            sg.Freeze();
            descriptor.DefiningGeometry = sg;
        }

        private static Point? GetIntersection(Point start, Point end, List<Point> atomHull)
        {
            for (int i = 0; i < atomHull.Count; i++)
            {
                Point? p;
                if ((p = BasicGeometry.LineSegmentsIntersect(start, end, atomHull[i], atomHull[(i + 1) % atomHull.Count])) != null)
                {
                    return p;
                }
            }
            return null;
        }

        /// <summary>
        /// Adjusts the StartPoint of a bond to avoid the atom visual
        /// </summary>
        /// <param name="startPoint">Moveable start point</param>
        /// <param name="endPoint">Fixed end point</param>
        /// <param name="atomHull">List of Points delineating the hull of the atom label</param>
        /// <param name="standoff">Boundary width between atom label and bond terminus</param>
        private static void AdjustTerminus(ref Point startPoint, Point endPoint, List<Point> atomHull, double standoff)
        {
            if (atomHull != null
                && startPoint != endPoint)
            {
                var displacement = endPoint - startPoint;

                var intersection = GetIntersection(startPoint, endPoint, atomHull);
                if (intersection != null)
                {
                    displacement.Normalize();
                    displacement = displacement * standoff;
                    var tempPoint = new Point(intersection.Value.X, intersection.Value.Y) + displacement;
                    startPoint = new Point(tempPoint.X, tempPoint.Y);
                }
            }
        }

        private static List<PathFigure> GetSingleBondSegment(Point startPoint, Point endPoint)
        {
            var segments = new List<PathSegment> { new LineSegment(endPoint, false) };

            var figures = new List<PathFigure>();
            var pf = new PathFigure(startPoint, segments, true);
            figures.Add(pf);
            return figures;
        }

        /// <summary>
        /// Quite ghastly routine to draw a wiggly bond
        /// </summary>
        /// <param name="descriptor">BondDescriptor which is populated</param>
        /// <param name="standardBondLength">Standard bond length as defined by the model</param>
        /// <param name="standoff"></param>
        public static void GetWavyBondGeometry(BondLayout descriptor, double standardBondLength, double standoff)
        {
            var sg = new StreamGeometry();

            //first do the adjustment for any atom visuals
            if (descriptor.StartAtomHull != null)
            {
                AdjustTerminus(ref descriptor.Start, descriptor.End, descriptor.StartAtomHull, standoff);
            }

            if (descriptor.EndAtomHull != null)
            {
                AdjustTerminus(ref descriptor.End, descriptor.Start, descriptor.EndAtomHull, standoff);
            }

            //Work out the control points for a quadratic Bezier by sprouting alternately along the bond line
            Vector halfAWiggle;
            using (var sgc = sg.Open())
            {
                var bondVector = descriptor.PrincipleVector;
                //come up with a number of wiggles that looks aesthetically sensible
                var noOfWiggles = (int)Math.Ceiling(bondVector.Length / (standardBondLength * Globals.BondOffsetPercentage * 2));
                if (noOfWiggles < 3)
                {
                    noOfWiggles = 3;
                }
                //now calculate a wiggle vector that is 60 degrees from the bond angle
                var wiggleLength = bondVector.Length / noOfWiggles;

                halfAWiggle = bondVector;
                halfAWiggle.Normalize();
                halfAWiggle *= wiggleLength / 2;

                //work out left and right sprouting vectors
                var toLeft = new Matrix();
                toLeft.Rotate(-60);
                var toRight = new Matrix();
                toRight.Rotate(60);

                var leftVector = halfAWiggle * toLeft;
                var rightVector = halfAWiggle * toRight;

                var allpoints = new List<Point>();
                //allpoints holds the control points for the bezier
                allpoints.Add(descriptor.Start);

                var lastPoint = descriptor.Start;
                //move along the bond vector, sprouting control points alternately
                for (var i = 0; i < noOfWiggles; i++)
                {
                    var leftPoint = lastPoint + leftVector;
                    allpoints.Add(leftPoint);
                    allpoints.Add(lastPoint + halfAWiggle);
                    var rightPoint = lastPoint + halfAWiggle + rightVector;
                    allpoints.Add(rightPoint);
                    lastPoint += halfAWiggle * 2;
                    allpoints.Add(lastPoint);
                }

                allpoints.Add(descriptor.End);
                BezierFromPoints(sgc, allpoints);
                sgc.Close();
            }

            //define the boundary
            descriptor.Boundary.Clear();
            descriptor.Boundary.AddRange(new[]
                                              {
                                                  descriptor.Start - halfAWiggle.Perpendicular(),
                                                  descriptor.End - halfAWiggle.Perpendicular(),
                                                  descriptor.End + halfAWiggle.Perpendicular(),
                                                  descriptor.Start + halfAWiggle.Perpendicular()
                                              });

            sg.Freeze();
            descriptor.DefiningGeometry = sg;

            //local function
            void BezierFromPoints(StreamGeometryContext sgc, List<Point> allpoints)
            {
                sgc.BeginFigure(allpoints[0], false, false);
                sgc.PolyQuadraticBezierTo(allpoints.Skip(1).ToArray(), true, true);
            }
        }

        /// <summary>
        /// Chamfers or forks the end of a wedge bond under special circumstances
        /// (one or more incoming single bonds)
        /// </summary>
        /// <param name="descriptor"> WedgeBondDescriptor to be populated</param>
        /// <param name="standardBondLength">Standard bond length as defined by the model</param>
        /// <param name="otherAtomPoints">List of positions of atoms splaying from the end atom</param>
        /// <param name="standoff"></param>
        public static void GetChamferedWedgeGeometry(WedgeBondLayout descriptor,
                                                     double standardBondLength,
                                                     List<Point> otherAtomPoints, double standoff)
        {
            var bondVector = descriptor.PrincipleVector;

            //first get an unaltered bond
            GetWedgeBondGeometry(descriptor, standardBondLength, standoff);

            var firstEdgeVector = descriptor.FirstCorner - descriptor.Start;
            var secondEdgeVector = descriptor.SecondCorner - descriptor.Start;

            //get the two bonds with widest splay

            var widestPoints = from Point p in otherAtomPoints
                               orderby Math.Abs(Vector.AngleBetween(bondVector, p - descriptor.End)) descending
                               select p;

            //the scaling factors are what we multiply the bond edge vectors by
            double firstScalingFactor = 0d;
            double secondScalingFactor = 0d;

            //work out the biggest scaling factor for either long edge
            foreach (var point in widestPoints)
            {
                BasicGeometry.IntersectLines(descriptor.Start,
                                             descriptor.FirstCorner,
                                             descriptor.End,
                                             point, out var firstEdgeCut, out var otherBond1Cut);
                BasicGeometry.IntersectLines(descriptor.Start,
                                             descriptor.SecondCorner,
                                             descriptor.End,
                                             point, out var secondEdgeCut, out var otherBond2Cut);
                if (otherAtomPoints.Count == 1)
                {
                    if (firstEdgeCut > firstScalingFactor)
                    {
                        firstScalingFactor = firstEdgeCut;
                    }
                    if (secondEdgeCut > secondScalingFactor)
                    {
                        secondScalingFactor = secondEdgeCut;
                    }
                }
                else
                {
                    if (firstEdgeCut > firstScalingFactor && otherBond1Cut < 1d && otherBond1Cut > 0d)
                    {
                        firstScalingFactor = firstEdgeCut;
                    }
                    if (secondEdgeCut > secondScalingFactor && otherBond2Cut < 1d && otherBond2Cut > 0d)
                    {
                        secondScalingFactor = secondEdgeCut;
                    }
                }
            }

            //and multiply the edges by the scaling factors
            descriptor.FirstCorner = firstEdgeVector * firstScalingFactor + descriptor.Start;
            descriptor.SecondCorner = secondEdgeVector * secondScalingFactor + descriptor.Start;

            descriptor.CappedOff = true;

            var sg = descriptor.GetOutline();
            sg.Freeze();
            descriptor.DefiningGeometry = sg;
        }
    }
}