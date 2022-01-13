// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Windows.Media;
using Chem4Word.ACME.Enums;

namespace Chem4Word.ACME.Entities
{
    public class WordCloudTheme
    {
        public WordCloudTheme(Typeface typeFace, WordCloudThemeWordRotation wordRotation, IList<SolidColorBrush> brushList, SolidColorBrush backgroundBrush)
        {
            Typeface = typeFace;
            WordRotation = wordRotation;
            BrushList = brushList;
            BackgroundBrush = backgroundBrush;

            foreach (var color in BrushList)
            {
                color.Freeze();
            }
        }

        public Typeface Typeface
        {
            get;
        }

        public WordCloudThemeWordRotation WordRotation
        {
            get;
        }

        public IList<SolidColorBrush> BrushList
        {
            get;
        }

        public SolidColorBrush BackgroundBrush
        {
            get;
        }
    }
}