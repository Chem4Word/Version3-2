// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace Chem4Word.ACME.Drawing
{
    //adds charge and spin labels to bracket molecules
    public class MoleculeLabelVisual : DrawingVisual
    {
        private readonly Brush _drawingBrush;
        private readonly string _label;
        private readonly Point _pos;
        private double _textSize;

        public MoleculeLabelVisual(string label, Point position, Brush drawingBrush, double textSize)
        {
            _label = label;
            _pos = position;
            _drawingBrush = drawingBrush;
            _textSize = textSize;
        }

        public float PixelsPerDip() => (float)VisualTreeHelper.GetDpi(this).PixelsPerDip;

        public void Render(DrawingContext dc, bool isSubscript = false)
        {
            var chargeLabel = new FormattedText(_label, CultureInfo.CurrentCulture,
                                                FlowDirection.LeftToRight,
                                                GlyphUtils.MoleculelabelTypeface,
                                                _textSize, _drawingBrush, null,
                                                PixelsPerDip());
            var offset = new Vector(0.0, 0.0);

            if (isSubscript)
            {
                offset = new Vector(0.0, -chargeLabel.Baseline * 0.5);
            }

            dc.DrawText(chargeLabel, _pos + offset);
        }
    }
}