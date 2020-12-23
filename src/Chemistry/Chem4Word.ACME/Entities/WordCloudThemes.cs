﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Windows.Media;
using Chem4Word.ACME.Enums;

namespace Chem4Word.ACME.Entities
{
    public class WordCloudThemes
    {
        static WordCloudThemes()
        {
            SetupWordCloudThemes();
        }

        private static void SetupWordCloudThemes()
        {
            var segoeUi = new Typeface("Segoe UI");

            Default = new WordCloudTheme(segoeUi, WordCloudThemeWordRotation.Mixed, ColorPalette, Brushes.White);

            Horizontal = new WordCloudTheme(segoeUi, WordCloudThemeWordRotation.Horizontal, ColorPalette, Brushes.White);

            Mixed = new WordCloudTheme(segoeUi, WordCloudThemeWordRotation.Mixed, ColorPalette, Brushes.White);

            Random = new WordCloudTheme(segoeUi, WordCloudThemeWordRotation.Random, ColorPalette, Brushes.White);
        }

        public static WordCloudTheme Horizontal { get; private set; }

        public static WordCloudTheme Mixed { get; private set; }

        public static WordCloudTheme Random { get; private set; }

        public static WordCloudTheme Default { get; private set; }

        private static readonly List<SolidColorBrush> ColorPalette = new List<SolidColorBrush>
                                                                      {
                                                                          Brushes.BlueViolet,
                                                                          Brushes.LawnGreen,
                                                                          Brushes.CornflowerBlue,
                                                                          Brushes.DarkRed,
                                                                          Brushes.DarkBlue,
                                                                          Brushes.DarkGreen,
                                                                          Brushes.Navy,
                                                                          Brushes.DarkCyan,
                                                                          Brushes.DarkOrange,
                                                                          Brushes.DarkGoldenrod,
                                                                          Brushes.DarkKhaki,
                                                                          Brushes.Blue,
                                                                          Brushes.Red,
                                                                          Brushes.Green
                                                                      };

    }
}