// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Media;
using Chem4Word.Core.Helpers;

namespace Chem4Word.ACME.Graphics
{
    public class RetrosyntheticArrow : EquilibriumArrow
    {
        public override void DrawArrowGeometry(DrawingContext drawingContext, Pen arrowPen, Brush arrowBrush)
        {
            Vector vector = EndPoint - StartPoint;
            var perp = vector.Perpendicular();
            perp.Normalize();

            Vector offset = perp * Separation;
            var topLeft = StartPoint - offset;
            var topRight = EndPoint - offset;
            var bottomLeft = StartPoint + offset;
            var bottomRight = EndPoint + offset;

            //assume a 45 degree head angle
            Vector barb = offset * 4;
            Matrix rotator = new Matrix();
            rotator.Rotate(-45);
            Point headStart = EndPoint - barb * rotator;
            rotator.Rotate(+90);
            Point headEnd = EndPoint + barb * rotator;
            if ((headEnd - headStart).Length > 1E-6) //check it's not zero length otherwise recalc
            {
                //recalculate the end points of the long bits
                var newTopRight = GeometryTool.GetIntersection(topLeft, topRight, EndPoint, headStart);
                var newBottomRight = GeometryTool.GetIntersection(bottomLeft, bottomRight, EndPoint, headEnd);
                //need to check we a
                if (newTopRight != null && newBottomRight != null)
                {
                    //draw the long lines
                    drawingContext.DrawLine(arrowPen, topLeft, newTopRight.Value);
                    drawingContext.DrawLine(arrowPen, bottomLeft, newBottomRight.Value);
                    //draw the head
                    drawingContext.DrawLine(arrowPen, headStart, EndPoint);
                    drawingContext.DrawLine(arrowPen, EndPoint, headEnd);

                    GetOverlayPen(out _, out Pen overlayPen);
                    //draw the overlay lines
                    drawingContext.DrawLine(overlayPen, topLeft, newTopRight.Value);
                    drawingContext.DrawLine(overlayPen, bottomLeft, newBottomRight.Value);
                    //draw the head
                    PathSegmentCollection psc = new PathSegmentCollection();
                    PathSegment ps1 = new LineSegment(EndPoint, true);
                    PathSegment ps2 = new LineSegment(headEnd, true);
                    psc.Add(ps1);
                    psc.Add(ps2);
                    PathFigure pf = new PathFigure(headStart, psc, false);
                    PathGeometry pg = new PathGeometry();
                    pg.Figures.Add(pf);

                    drawingContext.DrawGeometry(null, overlayPen, pg);
                }
            }
        }
    }
}