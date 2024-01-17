// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Windows;

namespace Chem4Word.Renderer.OoXmlV4.Entities
{
    public class SimpleLine
    {
        public Point Start { get; }

        public Point End { get; }

        public SimpleLine(Point startPoint, Point endPoint)
        {
            Start = startPoint;
            End = endPoint;
        }

        public SimpleLine GetParallel(double offset)
        {
            var xDifference = Start.X - End.X;
            var yDifference = Start.Y - End.Y;
            var length = Math.Sqrt(xDifference * xDifference + yDifference * yDifference);

            var newStartPoint = new Point((float)(Start.X - offset * yDifference / length),
                                          (float)(Start.Y + offset * xDifference / length));
            var newEndPoint = new Point((float)(End.X - offset * yDifference / length),
                                        (float)(End.Y + offset * xDifference / length));

            return new SimpleLine(newStartPoint, newEndPoint);
        }
    }
}