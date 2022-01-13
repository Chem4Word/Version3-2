// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Media.TextFormatting;

namespace Chem4Word.ACME.Drawing
{
    public class LabelTextRunTypographyProperties : TextRunTypographyProperties
    {
        public LabelTextRunTypographyProperties()
        {
        }

        public override int AnnotationAlternates
        {
            get { return 0; }
        }

        public override bool CapitalSpacing
        {
            get { return false; }
        }

        public override System.Windows.FontCapitals Capitals
        {
            get { return FontCapitals.Normal; }
        }

        public override bool CaseSensitiveForms
        {
            get { return false; }
        }

        public override bool ContextualAlternates
        {
            get { return false; }
        }

        public override bool ContextualLigatures
        {
            get { return false; }
        }

        public override int ContextualSwashes
        {
            get { return 0; }
        }

        public override bool DiscretionaryLigatures
        {
            get { return false; }
        }

        public override bool EastAsianExpertForms
        {
            get { return false; }
        }

        public override System.Windows.FontEastAsianLanguage EastAsianLanguage
        {
            get { return FontEastAsianLanguage.Normal; }
        }

        public override System.Windows.FontEastAsianWidths EastAsianWidths
        {
            get { return FontEastAsianWidths.Normal; }
        }

        public override System.Windows.FontFraction Fraction
        {
            get { return FontFraction.Normal; }
        }

        public override bool HistoricalForms
        {
            get { return false; }
        }

        public override bool HistoricalLigatures
        {
            get { return false; }
        }

        public override bool Kerning
        {
            get { return true; }
        }

        public override bool MathematicalGreek
        {
            get { return false; }
        }

        public override System.Windows.FontNumeralAlignment NumeralAlignment
        {
            get { return FontNumeralAlignment.Normal; }
        }

        public override System.Windows.FontNumeralStyle NumeralStyle
        {
            get { return FontNumeralStyle.Normal; }
        }

        public override bool SlashedZero
        {
            get { return false; }
        }

        public override bool StandardLigatures
        {
            get { return false; }
        }

        public override int StandardSwashes
        {
            get { return 0; }
        }

        public override int StylisticAlternates
        {
            get { return 0; }
        }

        public override bool StylisticSet1
        {
            get { return false; }
        }

        public override bool StylisticSet10
        {
            get { return false; }
        }

        public override bool StylisticSet11
        {
            get { return false; }
        }

        public override bool StylisticSet12
        {
            get { return false; }
        }

        public override bool StylisticSet13
        {
            get { return false; }
        }

        public override bool StylisticSet14
        {
            get { return false; }
        }

        public override bool StylisticSet15
        {
            get { return false; }
        }

        public override bool StylisticSet16
        {
            get { return false; }
        }

        public override bool StylisticSet17
        {
            get { return false; }
        }

        public override bool StylisticSet18
        {
            get { return false; }
        }

        public override bool StylisticSet19
        {
            get { return false; }
        }

        public override bool StylisticSet2
        {
            get { return false; }
        }

        public override bool StylisticSet20
        {
            get { return false; }
        }

        public override bool StylisticSet3
        {
            get { return false; }
        }

        public override bool StylisticSet4
        {
            get { return false; }
        }

        public override bool StylisticSet5
        {
            get { return false; }
        }

        public override bool StylisticSet6
        {
            get { return false; }
        }

        public override bool StylisticSet7
        {
            get { return false; }
        }

        public override bool StylisticSet8
        {
            get { return false; }
        }

        public override bool StylisticSet9
        {
            get { return false; }
        }

        public override FontVariants Variants
        {
            get { return FontVariants.Normal; }
        }
    }
}