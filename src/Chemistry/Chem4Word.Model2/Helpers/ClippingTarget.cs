// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Windows;

namespace Chem4Word.Model2.Helpers
{
    public class ClippingTarget : IComparable
    {
        private static double EPSILON = 0.000001;

        // Required by constructor;
        public string Name { get; }

        public Point Start { get; }
        public Point End { get; }

        public Bond Bond { get; }

        // Calculated properties
        public Rect BoundingBox { get; }

        public double Length { get; }

        #region Constructor

        public ClippingTarget(Bond bond)
        {
            Bond = bond;
            Name = bond.Path;
            Start = bond.StartAtom.Position;
            End = bond.EndAtom.Position;
            Length = (Start - End).Length;

            // Create a slightly smaller bounding box
            // This should eliminate any false positives from BoundingBox checks (due to lines joining)
            var start = new Point(Start.X, Start.Y);
            var end = new Point(End.X, End.Y);
            Core.Helpers.GeometryTool.AdjustLineAboutMidpoint(ref start, ref end, -0.1);
            BoundingBox = new Rect(start, end);
        }

        #endregion Constructor

        #region Implementation of IComparable

        public int CompareTo(object obj)
        {
            var result = 0;

            if (obj is ClippingTarget line)
            {
                var thisFirstX = Start.X < End.X ? Start.X : End.X;
                var otherFirstX = line.Start.X < line.End.X ? line.Start.X : line.End.X;

                if (Math.Abs(thisFirstX - otherFirstX) < EPSILON)
                {
                    result = 0;
                }
                else if (thisFirstX > otherFirstX)
                {
                    result = 1;
                }
                else
                {
                    result = -1;
                }
            }

            return result;
        }

        #endregion Implementation of IComparable
    }
}