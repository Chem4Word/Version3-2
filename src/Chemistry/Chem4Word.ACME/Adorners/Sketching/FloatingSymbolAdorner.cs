// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Drawing.Text;
using Chem4Word.ACME.Utils;

namespace Chem4Word.ACME.Adorners.Sketching
{
    public class FloatingSymbolAdorner : Adorner
    {
        private EditorCanvas _currentEditor;
        private string _defaultText;
        private Point _pos;
        private Brush _brush;
        public EditorCanvas CurrentEditor { get => _currentEditor; set => _currentEditor = value; }

        public FloatingSymbolAdorner(UIElement currentEditor, string defaultText, Point pos) : base(currentEditor)
        {
            CurrentEditor = (EditorCanvas)currentEditor;
            _defaultText = defaultText;
            _pos = pos;
            _brush = (Brush)FindResource(Common.AdornerBorderBrush);

            var myAdornerLayer = AdornerLayer.GetAdornerLayer(currentEditor);
            myAdornerLayer.Add(this);
        }

        public Point TopLeft { get; private set; }
        public Point Center { get; private set; }

        public float PixelsPerDip()
        {
            return (float)VisualTreeHelper.GetDpi(this).PixelsPerDip;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            FormattedText ghostText = new FormattedText(_defaultText,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                GlyphUtils.BlockTypeface,
                CurrentEditor.Controller.BlockTextSize,
                _brush, PixelsPerDip());
            var offset = new Vector(ghostText.Width, ghostText.Height);

            TopLeft = _pos - offset;
            Center = _pos - offset / 2;
            drawingContext.DrawText(ghostText, TopLeft);
        }
    }
}