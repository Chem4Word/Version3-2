// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Media;

namespace Chem4Word.ACME.Graphics
{
    public class BlockedArrow : StraightArrow
    {
        public override void DrawArrowGeometry(DrawingContext drawingContext, Pen outlinePen, Brush headFillBrush)
        {
            base.DrawArrowGeometry(drawingContext, outlinePen, headFillBrush);
            Vector shaftVector = EndPoint - StartPoint;
            var midpoint = StartPoint + shaftVector * 0.5;
            double crossArmLength = HeadLength;
            Point[] points = new Point[4];
            //draw the X through the shaft;

            Matrix rotator = new Matrix();
            rotator.Rotate(-45);
            Vector shaftUnit = shaftVector;
            shaftUnit.Normalize();

            for (int i = 0; i < 4; i++)
            {
                rotator.Rotate(90);
                points[i] = midpoint + (shaftUnit * crossArmLength) * rotator;
            }

            drawingContext.DrawLine(outlinePen, points[0], points[2]);
            drawingContext.DrawLine(outlinePen, points[1], points[3]);
        }
    }
}