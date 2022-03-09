// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Windows;
using Chem4Word.Core.Helpers;

namespace Chem4Word.Renderer.OoXmlV4.Entities
{
    public class SimpleLine : IComparable
    {
        public Point Start { get; }

        public Point End { get; }

        public string Name { get; }

        public Rect SmallerBoundingBox { get; }
        public double Length { get; }

        public SimpleLine(string name, Point startPoint, Point endPoint)
        {
            Name = name;
            Start = startPoint;
            End = endPoint;
            Length = (startPoint - endPoint).Length;

            // Create a slightly smaller Bounding Box by shrinking it by length / 10 at each end
            // This eliminates false positives from BoundingBox checks (due to lines joining)
            GeometryTool.AdjustLineAboutMidpoint(ref startPoint, ref endPoint, -0.01);
            SmallerBoundingBox = new Rect(startPoint, endPoint);
        }

        public SimpleLine(Point startPoint, Point endPoint)
        {
            Start = startPoint;
            End = endPoint;
        }

        public int CompareTo(object obj)
        {
            var result = 0;

            if (obj is SimpleLine line)
            {
                var thisFirstX = Start.X < End.X ? Start.X : End.X;
                var otherFirstX = line.Start.X < line.End.X ? line.Start.X : line.End.X;

                if (Math.Abs(thisFirstX - otherFirstX) < 0.0001)
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

        public SimpleLine GetParallel(double offset)
        {
            var xDifference = Start.X - End.X;
            var yDifference = Start.Y - End.Y;
            var length = Math.Sqrt(Math.Pow(xDifference, 2) + Math.Pow(yDifference, 2));

            var newStartPoint = new Point((float)(Start.X - offset * yDifference / length),
                                          (float)(Start.Y + offset * xDifference / length));
            var newEndPoint = new Point((float)(End.X - offset * yDifference / length),
                                        (float)(End.Y + offset * xDifference / length));

            return new SimpleLine(newStartPoint, newEndPoint);
        }
    }
}