// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Drawing.Visuals;
using System.Windows;
using System.Windows.Media;

namespace Chem4Word.ACME.Adorners.Feedback
{
    public class GroupHoverAdorner : BaseHoverAdorner
    {
        private const double Spacing = 2.0;

        public GroupHoverAdorner(UIElement adornedElement, GroupVisual targetedVisual) : base(adornedElement, targetedVisual)
        {
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            StreamGeometry sg = new StreamGeometry();

            Rect atomBounds = TargetedVisual.ContentBounds;
            atomBounds.Inflate(Spacing, Spacing);
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