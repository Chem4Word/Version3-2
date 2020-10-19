// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;
using Chem4Word.Renderer.OoXmlV4.Enums;
using Chem4Word.Renderer.OoXmlV4.OOXML;

namespace Chem4Word.Renderer.OoXmlV4.Entities
{
    public class BondLine
    {
        public Bond Bond { get; }

        public string BondPath => Bond != null ? Bond.Path : string.Empty;

        public string StartAtomPath => Bond != null ? Bond.StartAtom.Path : string.Empty;

        public string EndAtomPath => Bond != null ? Bond.EndAtom.Path : string.Empty;

        public BondLineStyle Style { get; private set; }

        public string Colour { get; set; } = "000000";

        /// <summary>
        /// For a Wedge or Hatch bond this is the nose of the wedge
        /// </summary>
        public Point Start { get; set; }

        /// <summary>
        /// For a Wedge or Hatch bond this is the centre of the "tail"
        /// </summary>
        public Point End { get; set; }

        public Point Nose => Start;
        public Point Tail => End;

        /// <summary>
        /// Only relevant to Wedge or Hatch bond
        /// </summary>
        public Point LeftTail { get; set; }

        /// <summary>
        /// Only relevant to Wedge or Hatch bond
        /// </summary>
        public Point RightTail { get; set; }

        private Rect _boundingBox = Rect.Empty;

        public Rect BoundingBox
        {
            get
            {
                if (_boundingBox.IsEmpty)
                {
                    _boundingBox = new Rect(Start, End);
                }

                return _boundingBox;
            }
        }

        public BondLine(BondLineStyle style, Bond bond)
        {
            Style = style;
            Bond = bond;

            if (bond != null)
            {
                Start = bond.StartAtom.Position;
                End = bond.EndAtom.Position;
            }
        }

        public BondLine(BondLineStyle style, Point startPoint, Point endPoint, Bond bond)
            : this(style, startPoint, endPoint)
        {
            Bond = bond;
        }

        private BondLine(BondLineStyle style, Point startPoint, Point endPoint)
        {
            Style = style;
            Start = startPoint;
            End = endPoint;
        }

        private double BondOffset(double medianBondLength)
        {
            return medianBondLength * OoXmlHelper.MULTIPLE_BOND_OFFSET_PERCENTAGE;
        }

        public List<Point> WedgeOutline()
        {
            var outline = new List<Point>();

            outline.Add(Nose);
            outline.Add(LeftTail);
            outline.Add(Tail);
            outline.Add(RightTail);

            return outline;
        }

        public void CalculateWedgeOutline(double medianBondLength)
        {
            var leftLine = GetParallel(BondOffset(medianBondLength) / 2);
            var rightLine = GetParallel(-BondOffset(medianBondLength) / 2);

            LeftTail = new Point(leftLine.End.X, leftLine.End.Y);
            RightTail = new Point(rightLine.End.X, rightLine.End.Y);

            Atom endAtom = Bond.EndAtom;
            // EndAtom == C and Label is "" and has at least one other bond
            if (endAtom.Element as Element == Globals.PeriodicTable.C
                && string.IsNullOrEmpty(endAtom.SymbolText)
                && endAtom.Bonds.Count() > 1)
            {
                var otherBonds = endAtom.Bonds.Except(new[] { Bond }).ToList();
                bool allSingle = true;
                List<Bond> nonHydrogenBonds = new List<Bond>();
                foreach (var otherBond in otherBonds)
                {
                    if (!otherBond.Order.Equals(Globals.OrderSingle))
                    {
                        allSingle = false;
                    }

                    var otherAtom = otherBond.OtherAtom(endAtom);
                    if (otherAtom.Element as Element != Globals.PeriodicTable.H)
                    {
                        nonHydrogenBonds.Add(otherBond);
                    }
                }

                // All other bonds are single
                if (allSingle)
                {
                    bool oblique = true;

                    var wedgeVector = endAtom.Position - Bond.StartAtom.Position;
                    foreach (var bond in otherBonds)
                    {
                        var otherAtom = bond.OtherAtom(Bond.EndAtom);
                        var angle = Math.Abs(Vector.AngleBetween(wedgeVector, endAtom.Position - otherAtom.Position));

                        if (angle < 109.5 || angle > 130.5)
                        {
                            oblique = false;
                            break;
                        }
                    }

                    if (oblique)
                    {
                        // Determine chamfer shape
                        Vector left = (LeftTail - Nose) * 2;
                        Point leftEnd = Nose + left;

                        Vector right = (RightTail - Nose) * 2;
                        Point rightEnd = Nose + right;

                        Vector shortestLeft = left;
                        Vector shortestRight = right;
                        Point otherEnd;
                        Point atomPosition;

                        if (otherBonds.Count - nonHydrogenBonds.Count == 1)
                        {
                            otherBonds = nonHydrogenBonds;
                        }

                        if (otherBonds.Count == 1)
                        {
                            Bond bond = otherBonds[0];
                            Atom atom = bond.OtherAtom(endAtom);
                            Vector vv = (endAtom.Position - atom.Position) * 2;
                            otherEnd = atom.Position + vv;
                            atomPosition = atom.Position;

                            TrimVector(Nose, leftEnd, atomPosition, otherEnd,
                                       ref shortestLeft);
                            TrimVector(Nose, rightEnd, atomPosition, otherEnd,
                                       ref shortestRight);

                            LeftTail = Nose + shortestLeft;
                            RightTail = Nose + shortestRight;
                        }
                        else
                        {
                            foreach (var bond in otherBonds)
                            {
                                Vector bv = (bond.EndAtom.Position - bond.StartAtom.Position) * 2;
                                otherEnd = bond.StartAtom.Position + bv;

                                atomPosition = bond.StartAtom.Position;

                                TrimVector(Nose, leftEnd, atomPosition, otherEnd,
                                           ref shortestLeft);
                                TrimVector(Nose, rightEnd, atomPosition, otherEnd,
                                           ref shortestRight);
                            }

                            LeftTail = Nose + shortestLeft;
                            RightTail = Nose + shortestRight;
                        }
                    }
                }
            }
        }

        private void TrimVector(Point line1Start, Point line1End, Point line2Start, Point line2End, ref Vector vector)
        {
            bool intersect;
            Point intersection;
            CoordinateTool.FindIntersection(line1Start, line1End, line2Start, line2End,
                                            out _, out intersect, out intersection);
            if (intersect)
            {
                Vector v = intersection - line1Start;
                if (v.Length < vector.Length)
                {
                    vector = v;
                }
            }
        }

        public BondLine GetParallel(double offset)
        {
            double xDifference = Start.X - End.X;
            double yDifference = Start.Y - End.Y;
            double length = Math.Sqrt(Math.Pow(xDifference, 2) + Math.Pow(yDifference, 2));

            Point newStartPoint = new Point((float)(Start.X - offset * yDifference / length),
                                            (float)(Start.Y + offset * xDifference / length));
            Point newEndPoint = new Point((float)(End.X - offset * yDifference / length),
                                          (float)(End.Y + offset * xDifference / length));

            return new BondLine(Style, newStartPoint, newEndPoint, Bond)
            {
                Colour = Colour
            };
        }

        public void SetLineStyle(BondLineStyle style)
        {
            Style = style;
        }

        public override string ToString()
        {
            string result = $"{Style} from {PointHelper.AsString(Start)} to {PointHelper.AsString(End)}";
            if (Bond != null)
            {
                result += $" [{Bond}]";
            }

            return result;
        }
    }
}