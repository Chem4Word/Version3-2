// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Windows;
using System.Windows.Media;
using Chem4Word.ACME.Drawing;
using Chem4Word.ACME.Enums;
using Chem4Word.ACME.Graphics;
using Chem4Word.Model2.Annotations;
using Chem4Word.Model2.Geometry;

namespace Chem4Word.ACME.Adorners.Feedback
{
    public class HRotatorAdorner : AtomHoverAdorner
    {
        private Rect Bounds;
        private HydrogenVisual HydrogenVisual { get; }

        public HRotatorAdorner([NotNull] UIElement adornedElement, HydrogenVisual targetedVisual) :
            base(adornedElement, targetedVisual)
        {
            HydrogenVisual = targetedVisual;
            Bounds = HydrogenVisual.Bounds;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            Vector baseVector = new Vector(0, -Math.Max(Bounds.Height, Bounds.Width) * 1.5);

            var orientation = HydrogenVisual.ParentVisual.ParentAtom.ImplicitHPlacement;
            Matrix rotator = new Matrix();
            Point centroid = new Point((Bounds.Left + Bounds.Right) / 2,
                                       (Bounds.Top + Bounds.Bottom) / 2);

            var newPlacementAngle = AngleMethods.ToDegrees(orientation) + 90;
            rotator.Rotate(newPlacementAngle - 45);
            Point startPoint = centroid + baseVector * rotator;
            rotator.Rotate(-90);
            Point firstArrowEnd = startPoint + 0.5 * baseVector * rotator;
            rotator.Rotate(180);
            Point secondArrowEnd = startPoint + 0.5 * baseVector * rotator;

            var rotator2 = new Matrix();
            rotator2.Rotate(-45);
            ArrowBase arrow1 = new ArrowBase()
            {
                ArrowEnds = ArrowEnds.End,
                ArrowHeadLength = baseVector.Length * 0.3,
                StartPoint = firstArrowEnd,
                EndPoint = secondArrowEnd,
                IsArrowClosed = true
            };

            drawingContext.DrawGeometry(BracketBrush, BracketPen, arrow1.GetArrowGeometry());
        }
    }
}