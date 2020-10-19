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
using Chem4Word.Model2.Annotations;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.ACME.Adorners
{
    public class LassoAdorner : Adorner
    {
        private StreamGeometry _outline;
        private SolidColorBrush _solidColorBrush;
        private Pen _dashPen;
        private EditorCanvas CurrentEditor { get; }

        public LassoAdorner([NotNull] UIElement adornedElement) : base(adornedElement)
        {
            _solidColorBrush = (SolidColorBrush)FindResource(Globals.AdornerFillBrush);

            _dashPen = new Pen((SolidColorBrush)FindResource(Globals.AdornerBorderBrush), 1);
            _dashPen.DashStyle = DashStyles.Dash;
            CurrentEditor = (EditorCanvas)adornedElement;
            var myAdornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
            myAdornerLayer.Add(this);

            Focusable = true;
            Focus();
        }

        public LassoAdorner([NotNull] UIElement adornedElement, StreamGeometry outline) : this(adornedElement)
        {
            _outline = outline;
        }

        public StreamGeometry Outline
        {
            get { return _outline; }
            set
            {
                _outline = value;
                InvalidateVisual();
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawGeometry(_solidColorBrush, _dashPen, _outline);
        }
    }
}