// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Chem4Word.ACME.Controls;
using Chem4Word.Model2;
using Chem4Word.Model2.Annotations;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.ACME.Adorners
{
    /// <summary>
    /// Draws a chain image by dragging in free space or from at atom.
    /// Is refreshed repeatedly on drawing
    /// </summary>
    public class ChainAdorner : Adorner
    {
        public PathGeometry Geometry { get; private set; }

        public ChainAdorner(Point firstPoint, [NotNull] UIElement adornedElement, double bondThickness,
                            List<Point> placements, Point currentPoint, Atom target, bool greyedOut = false) : base(adornedElement)
        {
            SolidColorBrush bondBrush;
            if (greyedOut)
            {
                bondBrush = (SolidColorBrush)FindResource(Globals.BlockedAdornerBrush);
            }
            else
            {
                bondBrush = (SolidColorBrush)FindResource(Globals.DrawAdornerBrush);
            }

            BondPen = new Pen(bondBrush, bondThickness);
            DashedPen = BondPen.Clone();
            DashedPen.DashStyle = DashStyles.Dash;

            var myAdornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
            myAdornerLayer.Add(this);

            FirstPoint = firstPoint; //where the adorner is anchored
            Placements = placements; //list of placement points to draw the chain to
            CurrentPoint = currentPoint;
            CurrentEditor = (EditorCanvas)adornedElement;

            var psc = new PathSegmentCollection();
            foreach (var point in Placements)
            {
                psc.Add(new LineSegment(point, true));
            }

            var pf = new PathFigure(FirstPoint, psc, false);
            Geometry = new PathGeometry(new[] { pf });

            Unanchored = target == null;

            Focusable = true;
            Focus();
        }

        public Pen BondPen { get; }
        public List<Point> Placements { get; }
        public bool Unsaturated { get; }
        public EditorCanvas CurrentEditor { get; }
        public Point FirstPoint { get; set; }

        /// <summary>
        /// True if the adorner is being drawn in free space (i.e. unanchored)
        /// </summary>
        public bool Unanchored { get; set; }

        public Pen DashedPen { get; }

        public Point CurrentPoint { get; }

        public float PixelsPerDip() => (float)VisualTreeHelper.GetDpi(this).PixelsPerDip;

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawGeometry(null, BondPen, Geometry);
            drawingContext.DrawLine(DashedPen, Placements.Last(), CurrentPoint);
            if (Unanchored)
            {
                NRingAdorner.DrawRingSize(drawingContext, Placements.Count, Placements.Last(), PixelsPerDip(),
                                          BondPen.Brush.Clone(), CurrentEditor.ViewModel.SymbolSize);
            }
            else
            {
                if (Placements.Count > 1)
                {
                    NRingAdorner.DrawRingSize(drawingContext, Placements.Count - 1, Placements.Last(), PixelsPerDip(),
                                              BondPen.Brush.Clone(), CurrentEditor.ViewModel.SymbolSize);
                }
            }
        }
    }
}