// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.ACME.Drawing.Text
{
    public class BlockTextRunProperties : TextRunProperties
    {
        private string _colour;

        private double _symbolSize;

        public BlockTextRunProperties(string colour, double symbolSize, Typeface defaultTypeface = null)
        {
            Colour = colour;
            SymbolSize = symbolSize;
            DefaultTypeface = defaultTypeface;
        }

        public override Brush BackgroundBrush
        {
            get { return null; }
        }

        public override CultureInfo CultureInfo
        {
            get { return CultureInfo.CurrentCulture; }
        }

        public override double FontHintingEmSize
        {
            get { return SymbolSize; }
        }

        public override double FontRenderingEmSize
        {
            get { return SymbolSize; }
        }

        public override Brush ForegroundBrush
        {
            get
            {
                var brush = Brushes.Black;
                try
                {
                    if (string.IsNullOrEmpty(Colour))
                    {
                        brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Globals.PeriodicTable.C.Colour));
                    }
                    else
                    {
                        brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Colour));
                    }
                }
                catch (Exception)
                {
                    // Do nothing
                }
                return brush;
            }
        }

        public override System.Windows.TextDecorationCollection TextDecorations
        {
            get { return new System.Windows.TextDecorationCollection(); }
        }

        public override TextEffectCollection TextEffects
        {
            get { return new TextEffectCollection(); }
        }

        public override Typeface Typeface
        {
            get
            {
                return DefaultTypeface ?? GlyphUtils.BlockTypeface;
            }
        }

        public override TextRunTypographyProperties TypographyProperties
        {
            get
            {
                return new LabelTextRunTypographyProperties();
            }
        }

        public double SymbolSize { get => _symbolSize; set => _symbolSize = value; }
        public Typeface DefaultTypeface { get; set; }
        public string Colour { get => _colour; set => _colour = value; }
    }
}