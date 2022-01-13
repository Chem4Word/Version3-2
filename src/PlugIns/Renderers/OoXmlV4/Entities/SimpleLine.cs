// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
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
        public Point Start { get; set; }

        public Point End { get; set; }

        public SimpleLine(Point startPoint, Point endPoint)
        {
            Start = startPoint;
            End = endPoint;
        }

        public SimpleLine GetParallel(double offset)
        {
            double xDifference = Start.X - End.X;
            double yDifference = Start.Y - End.Y;
            double length = Math.Sqrt(Math.Pow(xDifference, 2) + Math.Pow(yDifference, 2));

            Point newStartPoint = new Point((float)(Start.X - offset * yDifference / length),
                                            (float)(Start.Y + offset * xDifference / length));
            Point newEndPoint = new Point((float)(End.X - offset * yDifference / length),
                                          (float)(End.Y + offset * xDifference / length));

            return new SimpleLine(newStartPoint, newEndPoint);
        }
    }
}