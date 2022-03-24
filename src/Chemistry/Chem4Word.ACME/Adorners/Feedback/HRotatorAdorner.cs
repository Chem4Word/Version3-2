// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Windows;
using System.Windows.Media;
using Chem4Word.ACME.Drawing.Visuals;
using Chem4Word.ACME.Enums;
using Chem4Word.ACME.Graphics;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2.Annotations;

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

            var parentAtom = HydrogenVisual.ParentVisual.ParentAtom;

            var hloc = new Point((Bounds.Right + Bounds.Left) / 2, (Bounds.Top + Bounds.Bottom) / 2);
            var radiusVector = hloc - parentAtom.Position;
            var newPlacementAngle = Vector.AngleBetween(GeometryTool.ScreenNorth, radiusVector);

            Arrow arrow1 = new ArcArrow
            {
                ArrowEnds = ArrowEnds.End,
                HeadLength = baseVector.Length * 0.2,
                Center = parentAtom.Position,
                StartAngle = newPlacementAngle + 30,
                EndAngle = newPlacementAngle + 100,
                Radius = radiusVector.Length,
                ArrowHeadClosed = true
            };
            arrow1.DrawArrowGeometry(drawingContext, BracketPen, BracketBrush);
        }
    }
}