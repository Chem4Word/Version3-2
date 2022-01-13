// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Chem4Word.ACME.Entities;

namespace Chem4Word.ACME.Models
{
    public class WordDrawing
    {
        private Rect _bounds;

        private readonly TransformGroup _transformGroup = new TransformGroup();
        private readonly TranslateTransform _translateTransform = new TranslateTransform();
        private readonly TransformGroup _scaleTransformGroup = new TransformGroup();
        private readonly Geometry _geo;

        public WordDrawing(WordCloudEntry wordEntry, WordCloudTheme theme, DpiScale scale)
        {
            var text = new FormattedText(wordEntry.Word,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                theme.Typeface,
                100,
                wordEntry.Brush,
                scale.PixelsPerDip);

            var textGeometry = text.BuildGeometry(new Point(0, 0));
            _geo = textGeometry;
            WordCloudEntry = wordEntry;
            _bounds = textGeometry.Bounds;
            _geo.Transform = _transformGroup;

            var rotateTransform = new RotateTransform(WordCloudEntry.Angle, _bounds.Width / 2, _bounds.Height / 2);

            _transformGroup.Children.Add(rotateTransform);
            _bounds = rotateTransform.TransformBounds(_bounds);

            _transformGroup.Children.Add(_scaleTransformGroup);

            _initialPlacementTransform = new TranslateTransform(-_bounds.X, -_bounds.Y);

            _transformGroup.Children.Add(_initialPlacementTransform);

            _bounds.X = 0;
            _bounds.Y = 0;

            IntWidth = (int)Math.Ceiling(_bounds.Width);
            IntHeight = (int)Math.Ceiling(_bounds.Height);
            _transformGroup.Children.Add(_translateTransform);
        }

        public Geometry Geo
        {
            get
            {
                _translateTransform.X = _bounds.X;
                _translateTransform.Y = _bounds.Y;
                return _geo;
            }
        }

        public WordCloudEntry WordCloudEntry { get; }

        public double X
        {
            get => _bounds.X;
            set => _bounds.X = value;
        }

        public double Y
        {
            get => _bounds.Y;
            set => _bounds.Y = value;
        }

        public double Bottom => _bounds.Y + Height;
        public double Right => _bounds.X + Width;

        public double Width => _bounds.Width;
        public double Height => _bounds.Height;

        public long Weight => WordCloudEntry.Weight;

        public void ApplyScale(double scale)
        {
            var scaleTransform = new ScaleTransform(scale, scale);
            _scaleTransformGroup.Children.Add(scaleTransform);

            if (_initialPlacementTransform.Inverse != null)
            {
                _bounds = _initialPlacementTransform.Inverse.TransformBounds(_bounds);
            }
            _bounds = scaleTransform.TransformBounds(_bounds);

            IntWidth = (int)Math.Ceiling(_bounds.Width);
            IntHeight = (int)Math.Ceiling(_bounds.Height);

            _initialPlacementTransform.X = -_bounds.X;
            _initialPlacementTransform.Y = -_bounds.Y;

            _bounds.X = 0;
            _bounds.Y = 0;
        }

        public int IntWidth;
        public int IntHeight;
        private readonly TranslateTransform _initialPlacementTransform;
        private Rect _frozenBounds;

        public int IntX => (int)Math.Ceiling(_bounds.X);
        public int IntY => (int)Math.Ceiling(_bounds.Y);

        public int IntBottom => IntY + IntHeight;
        public int IntRight => IntX + IntWidth;

        public GeometryDrawing GetDrawing()
        {
            var geoDrawing = new GeometryDrawing
            {
                Geometry = _geo,
                Brush = WordCloudEntry.Brush,
            };

            _translateTransform.X = _bounds.X;
            _translateTransform.Y = _bounds.Y;

            if (!geoDrawing.IsFrozen)
            {
                geoDrawing.Freeze();
                _frozenBounds = new Rect(X, Y, Width, Height);
            }

            return geoDrawing;
        }

        public override string ToString()
        {
            return WordCloudEntry.Word;
        }

        public bool Contains(double x, double y)
        {
            if (x >= _bounds.X && x - _bounds.Width <= _bounds.X && y >= _bounds.Y)
            {
                return y - _bounds.Height <= _bounds.Y;
            }

            return false;
        }

        public Rect GetBounds()
        {
            return _geo.IsFrozen ? _frozenBounds : new Rect(X, Y, Width, Height);
        }
    }
}