// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Drawing.Visuals;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;
using System.Windows;
using System.Windows.Media;

namespace Chem4Word.ACME.Adorners.Feedback
{
    public class BondHoverAdorner : BaseHoverAdorner
    {
        private Bond TargetedBond { get; }

        public BondHoverAdorner(UIElement adornedElement, BondVisual targetedVisual) : base(adornedElement, targetedVisual)
        {
            TargetedBond = targetedVisual.ParentBond;
        }

        /// <summary>
        /// Draws the adorner on top of the editor
        /// </summary>
        /// <param name="drawingContext"></param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            StreamGeometry sg = new StreamGeometry();
            double orderValue;
            if (TargetedBond.OrderValue == null || TargetedBond.OrderValue < 1d)
            {
                orderValue = 1d;
            }
            else
            {
                orderValue = TargetedBond.OrderValue.Value;
            }
            double offset = Globals.BondOffsetPercentage * TargetedBond.BondLength * orderValue;

            //this tells us how much to rotate the brackets at the end of the bond
            double bondAngle = TargetedBond.Angle;

            Matrix rotator = new Matrix();
            rotator.Rotate(bondAngle);
            Vector offsetVector1 = new Vector(offset, 0d) * rotator;

            Vector twiddle = -offsetVector1.Perpendicular();
            twiddle.Normalize();
            twiddle *= 3.0;

            using (StreamGeometryContext sgc = sg.Open())
            {
                var startAtom = TargetedBond.StartAtom;
                sgc.BeginFigure(startAtom.Position + offsetVector1 + twiddle, false, false);
                sgc.LineTo(startAtom.Position + offsetVector1, true, true);
                sgc.LineTo(startAtom.Position - offsetVector1, true, true);
                sgc.LineTo(startAtom.Position - offsetVector1 + twiddle, true, true);

                var endAtom = TargetedBond.EndAtom;
                sgc.BeginFigure(endAtom.Position + offsetVector1 - twiddle, false, false);
                sgc.LineTo(endAtom.Position + offsetVector1, true, true);
                sgc.LineTo(endAtom.Position - offsetVector1, true, true);
                sgc.LineTo(endAtom.Position - offsetVector1 - twiddle, true, true);

                sgc.Close();
            }

            drawingContext.DrawGeometry(BracketBrush, BracketPen, sg);
        }
    }
}