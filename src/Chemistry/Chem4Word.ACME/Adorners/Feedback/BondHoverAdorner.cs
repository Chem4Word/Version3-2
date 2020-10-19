// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Drawing;
using Chem4Word.Model2;
using Chem4Word.Model2.Annotations;
using Chem4Word.Model2.Geometry;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.ACME.Adorners.Feedback
{
    public class BondHoverAdorner : Adorner
    {
        private SolidColorBrush _solidColorBrush;
        private Pen _bracketPen;
        private BondVisual _targetedVisual;
        private Bond _targetedBond;
        public EditorCanvas CurrentEditor { get; }

        public BondHoverAdorner([NotNull] UIElement adornedElement) : base(adornedElement)
        {
            _solidColorBrush = new SolidColorBrush(Globals.HoverAdornerColor);

            _bracketPen = new Pen(_solidColorBrush, Globals.HoverAdornerThickness);
            _bracketPen.StartLineCap = PenLineCap.Round;
            _bracketPen.EndLineCap = PenLineCap.Round;

            var myAdornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
            myAdornerLayer.Add(this);
        }

        public BondHoverAdorner(UIElement adornedElement, BondVisual targetedVisual) : this(adornedElement)
        {
            _targetedVisual = targetedVisual;
            _targetedBond = _targetedVisual.ParentBond;
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
            if (_targetedBond.OrderValue == null || _targetedBond.OrderValue < 1d)
            {
                orderValue = 1d;
            }
            else
            {
                orderValue = _targetedBond.OrderValue.Value;
            }
            double offset = Globals.BondOffsetPercentage * _targetedBond.BondLength * orderValue;

            //this tells us how much to rotate the brackets at the end of the bond
            double bondAngle = _targetedBond.Angle;

            Vector offsetVector1 = new Vector(offset, 0d);

            Matrix rotator = new Matrix();
            rotator.Rotate(bondAngle);

            offsetVector1 = offsetVector1 * rotator;

            Vector twiddle = -offsetVector1.Perpendicular();
            twiddle.Normalize();
            twiddle *= 3.0;

            using (StreamGeometryContext sgc = sg.Open())
            {
                sgc.BeginFigure(_targetedBond.StartAtom.Position + offsetVector1 + twiddle, false, false);
                sgc.LineTo(_targetedBond.StartAtom.Position + offsetVector1, true, true);
                sgc.LineTo(_targetedBond.StartAtom.Position - offsetVector1, true, true);
                sgc.LineTo(_targetedBond.StartAtom.Position - offsetVector1 + twiddle, true, true);

                sgc.BeginFigure(_targetedBond.EndAtom.Position + offsetVector1 - twiddle, false, false);
                sgc.LineTo(_targetedBond.EndAtom.Position + offsetVector1, true, true);
                sgc.LineTo(_targetedBond.EndAtom.Position - offsetVector1, true, true);
                sgc.LineTo(_targetedBond.EndAtom.Position - offsetVector1 - twiddle, true, true);

                sgc.Close();
            }

            drawingContext.DrawGeometry(_solidColorBrush, _bracketPen, sg);
        }
    }
}