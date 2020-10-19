// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Chem4Word.ACME.Behaviors;
using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Utils;
using Chem4Word.Model2.Annotations;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.ACME.Adorners
{
    public class FixedRingAdorner : Adorner
    {
        private SolidColorBrush _solidColorBrush;
        public Pen BondPen { get; }
        public List<Point> Placements { get; }
        public bool Unsaturated { get; }
        public EditorCanvas CurrentEditor { get; }
        private readonly RingDrawer _ringDrawer;

        public FixedRingAdorner([NotNull] UIElement adornedElement, double bondThickness, List<Point> placements,
                                bool unsaturated = false, bool greyedOut = false) : base(adornedElement)
        {
            _solidColorBrush = (SolidColorBrush)FindResource(Globals.AdornerFillBrush);

            Cursor = CursorUtils.Pencil;
            if (!greyedOut)
            {
                BondPen = new Pen((SolidColorBrush)FindResource(Globals.DrawAdornerBrush), bondThickness);
            }
            else
            {
                BondPen = new Pen((SolidColorBrush)FindResource(Globals.BlockedAdornerBrush), bondThickness);
            }

            var myAdornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
            myAdornerLayer.Add(this);
            Placements = placements;
            Unsaturated = unsaturated;
            CurrentEditor = (EditorCanvas)adornedElement;
            _ringDrawer = new RingDrawer(this);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var newPlacements = new List<NewAtomPlacement>();

            RingBehavior.FillExistingAtoms(Placements, Placements, newPlacements, CurrentEditor);

            _ringDrawer.DrawNRing(drawingContext, newPlacements);
        }
    }
}