// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Media;

namespace Chem4Word.ACME.Drawing
{
    public static class FatArrowGeometry
    {
        public static PathGeometry GetArrowGeometry(Point startPoint, Point endPoint)
        {
            Vector extent = endPoint - startPoint;
            Vector headlength = extent * 0.1;
            Vector headwidth = headlength;

            Vector halfWidth = headwidth * 0.5;
            Point headStart = (extent - headlength) + startPoint;

            Matrix rotator = new Matrix();
            rotator.Rotate(-90);
            Point firstNotch = headStart + (halfWidth * rotator);
            Point firstbarb = headStart + (headwidth * rotator);

            rotator.Rotate(180);
            Point secondNotch = headStart + (halfWidth * rotator);
            Point secondBarb = headStart + (headwidth * rotator);

            var ps1 = new LineSegment(firstNotch, true);
            var ps2 = new LineSegment(firstbarb, true);
            var ps3 = new LineSegment(endPoint, true);
            var ps4 = new LineSegment(secondBarb, true);
            var ps5 = new LineSegment(secondNotch, true);

            PathFigure pf = new PathFigure(startPoint, new[] { ps1, ps2, ps3, ps4, ps5 }, true);
            return new PathGeometry(new[] { pf });
        }
    }
}