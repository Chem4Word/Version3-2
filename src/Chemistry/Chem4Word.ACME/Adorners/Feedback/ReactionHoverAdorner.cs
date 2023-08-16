// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Drawing.Visuals;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using System.Windows;
using System.Windows.Media;

namespace Chem4Word.ACME.Adorners.Feedback
{
    public class ReactionHoverAdorner : BaseHoverAdorner
    {
        private Reaction TargetedReaction { get; }

        public ReactionHoverAdorner(UIElement adornedElement, ReactionVisual targetedVisual) : base(adornedElement, targetedVisual)
        {
            TargetedReaction = targetedVisual.ParentReaction;
        }

        /// <summary>
        /// Draws the adorner on top of the editor
        /// </summary>
        /// <param name="drawingContext"></param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            StreamGeometry sg = new StreamGeometry();

            double offset = 6d;

            //this tells us how much to rotate the brackets at the end of the bond
            double bondAngle = TargetedReaction.Angle;

            Matrix rotator = new Matrix();
            rotator.Rotate(bondAngle);
            Vector offsetVector1 = new Vector(offset, 0d) * rotator;

            Vector twiddle = -offsetVector1.Perpendicular();
            twiddle.Normalize();
            twiddle *= 3.0;

            using (StreamGeometryContext sgc = sg.Open())
            {
                sgc.BeginFigure(TargetedReaction.TailPoint + offsetVector1 + twiddle, false, false);
                sgc.LineTo(TargetedReaction.TailPoint + offsetVector1, true, true);
                sgc.LineTo(TargetedReaction.TailPoint - offsetVector1, true, true);
                sgc.LineTo(TargetedReaction.TailPoint - offsetVector1 + twiddle, true, true);

                sgc.BeginFigure(TargetedReaction.HeadPoint + offsetVector1 - twiddle, false, false);
                sgc.LineTo(TargetedReaction.HeadPoint + offsetVector1, true, true);
                sgc.LineTo(TargetedReaction.HeadPoint - offsetVector1, true, true);
                sgc.LineTo(TargetedReaction.HeadPoint - offsetVector1 - twiddle, true, true);

                sgc.Close();
            }

            drawingContext.DrawGeometry(BracketBrush, BracketPen, sg);
        }
    }
}