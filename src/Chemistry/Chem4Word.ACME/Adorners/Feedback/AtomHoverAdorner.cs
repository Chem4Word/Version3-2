// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Media;
using Chem4Word.ACME.Drawing.Visuals;

namespace Chem4Word.ACME.Adorners.Feedback
{
    public class AtomHoverAdorner : BaseHoverAdorner
    {
        public AtomHoverAdorner(UIElement adornedElement, AtomVisual targetedVisual) : base(adornedElement, targetedVisual)
        {
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            StreamGeometry sg = new StreamGeometry();

            Rect atomBounds = (TargetedVisual as AtomVisual).Bounds;
            atomBounds.Inflate(2.0, 2.0);
            Vector twiddle = new Vector(3, 0.0);
            using (StreamGeometryContext sgc = sg.Open())
            {
                sgc.BeginFigure(atomBounds.TopLeft + twiddle, false, false);
                sgc.LineTo(atomBounds.TopLeft, true, true);
                sgc.LineTo(atomBounds.BottomLeft, true, true);
                sgc.LineTo(atomBounds.BottomLeft + twiddle, true, true);

                sgc.BeginFigure(atomBounds.TopRight - twiddle, false, false);
                sgc.LineTo(atomBounds.TopRight, true, true);
                sgc.LineTo(atomBounds.BottomRight, true, true);
                sgc.LineTo(atomBounds.BottomRight - twiddle, true, true);
                sgc.Close();
            }

            drawingContext.DrawGeometry(BracketBrush, BracketPen, sg);
        }
    }
}