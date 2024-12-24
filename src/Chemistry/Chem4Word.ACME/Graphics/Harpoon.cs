// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------
using System.Windows;
using System.Windows.Media;

namespace Chem4Word.ACME.Graphics
{
    public class Harpoon
    {
        public Point StartPoint { get; set; }
        public Point EndPoint { get; set; }
        public double HeadAngle { get; set; }
        public double HeadLength { get; set; }

        public Geometry HarpoonGeometry
        {
            get
            {
                PathGeometry pg = new PathGeometry();

                PathFigure main = new PathFigure { StartPoint = StartPoint, IsClosed = false, IsFilled = false };
                LineSegment ls = new LineSegment(EndPoint, true);
                ls.IsSmoothJoin = true;
                main.Segments.Add(ls);

                Vector barbVector = StartPoint - EndPoint;
                barbVector.Normalize();
                barbVector *= HeadLength;
                Matrix rotator = new Matrix();
                rotator.Rotate(HeadAngle);
                barbVector = barbVector * rotator;
                LineSegment barb = new LineSegment(EndPoint + barbVector, true);
                barb.IsSmoothJoin = true;
                main.Segments.Add(barb);

                pg.Figures.Add(main);

                return pg;
            }
        }
    }
}