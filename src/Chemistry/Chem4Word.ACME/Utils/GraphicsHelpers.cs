// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows.Media;

namespace Chem4Word.ACME.Utils
{
    public static class GraphicsHelpers
    {
        public static void IterateDrawingGroup(DrawingGroup textDrawing, DrawingContext drawingContext, Pen ghostPen, Brush ghostBrush, Transform lastOperation)
        {
            DrawingCollection dc = textDrawing.Children;
            foreach (var drawing in dc)
            {
                if (drawing is DrawingGroup dg)
                {
                    IterateDrawingGroup(dg, drawingContext, ghostPen, ghostBrush, lastOperation);
                }
                if (drawing is GeometryDrawing gd)
                {
                    drawingContext.DrawGeometry(ghostBrush, ghostPen, gd.Geometry);
                }
                if (drawing is GlyphRunDrawing grd)
                {
                    var outline = grd.GlyphRun.BuildGeometry();
                    outline.Transform = lastOperation;
                    drawingContext.DrawGeometry(ghostPen.Brush, null, outline);
                }
            }
        }
    }
}