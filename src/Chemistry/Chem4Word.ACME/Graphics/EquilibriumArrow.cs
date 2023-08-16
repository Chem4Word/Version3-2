// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------
using Chem4Word.Core.Helpers;
using System.Windows;
using System.Windows.Media;

namespace Chem4Word.ACME.Graphics
{
    public enum EquilibriumBias
    {
        None = 0,
        Forward = 1,
        Backward = 2
    }

    public class EquilibriumArrow : StraightArrow
    {
        public double Separation
        {
            get { return (double)GetValue(SeparationProperty); }
            set { SetValue(SeparationProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Separation.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SeparationProperty =
            DependencyProperty.Register("Separation", typeof(double), typeof(EquilibriumArrow), new PropertyMetadata(4d));

        public EquilibriumBias Bias
        {
            get { return (EquilibriumBias)GetValue(BiasProperty); }
            set { SetValue(BiasProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Bias.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BiasProperty =
            DependencyProperty.Register("Bias", typeof(EquilibriumBias), typeof(EquilibriumArrow), new PropertyMetadata(EquilibriumBias.None));

        protected override void OnRender(DrawingContext drawingContext)
        {
            //base.OnRender(drawingContext);
            DrawArrowGeometry(drawingContext, new Pen(Stroke, StrokeThickness), Stroke);
        }

        public override void DrawArrowGeometry(DrawingContext drawingContext, Pen arrowPen, Brush arrowBrush)
        {
            Vector vector = EndPoint - StartPoint;
            var perp = vector.Perpendicular();
            perp.Normalize();

            Vector bottomOffset = new Vector(0, 0), topOffset = new Vector(0, 0);
            double topScale = 1d, bottomScale = 1d;
            if (Bias == EquilibriumBias.Forward)
            {
                bottomOffset = vector * 0.2;
                bottomScale = 0.8;
            }
            else if (Bias == EquilibriumBias.Backward)
            {
                topOffset = vector * 0.2;
                topScale = 0.8;
            }

            var point1 = StartPoint - perp * Separation;
            var point2 = EndPoint - perp * Separation;
            var point3 = StartPoint + perp * Separation;
            var point4 = EndPoint + perp * Separation;

            var halfArrowForward = new Harpoon { StartPoint = point1 + topOffset, EndPoint = point2 - topOffset, HeadLength = HeadLength * topScale, HeadAngle = HeadAngle };
            var halfArrowBack = new Harpoon { StartPoint = point4 - bottomOffset, EndPoint = point3 + bottomOffset, HeadLength = HeadLength * bottomScale, HeadAngle = HeadAngle };

            drawingContext.DrawGeometry(arrowBrush, arrowPen, halfArrowForward.HarpoonGeometry);
            drawingContext.DrawGeometry(arrowBrush, arrowPen, halfArrowBack.HarpoonGeometry);

            GetOverlayPen(out Brush overlayBrush, out Pen overlayPen);
            drawingContext.DrawGeometry(overlayBrush, overlayPen, halfArrowForward.HarpoonGeometry);
            drawingContext.DrawGeometry(overlayBrush, overlayPen, halfArrowBack.HarpoonGeometry);
        }
    }
}