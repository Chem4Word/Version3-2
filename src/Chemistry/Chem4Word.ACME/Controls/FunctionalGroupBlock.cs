// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Chem4Word.Model2;

namespace Chem4Word.ACME.Controls
{
    public class FunctionalGroupBlock : TextBlock
    {
        private const double ScriptSize = 0.6;

        public static readonly DependencyProperty ParentGroupProperty =
            DependencyProperty.Register("ParentGroup", typeof(FunctionalGroup), typeof(FunctionalGroupBlock),
                                        new FrameworkPropertyMetadata(FunctionalGroupChanged));

        public FunctionalGroupBlock()
        {
            Width = double.NaN;
            FontSize = 18;
            FontWeight = FontWeights.DemiBold;
            FontFamily = new FontFamily("Arial");
        }

        public FunctionalGroup ParentGroup
        {
            get => (FunctionalGroup)GetValue(ParentGroupProperty);
            set => SetValue(ParentGroupProperty, value);
        }

        private static void FunctionalGroupChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FunctionalGroupBlock functionalGroupBlock)
            {
                functionalGroupBlock.Text = string.Empty;
                functionalGroupBlock.Inlines.Clear();
                functionalGroupBlock.BuildTextBlock((FunctionalGroup)e.NewValue);
            }
        }

        public void BuildTextBlock(FunctionalGroup functionalGroup)
        {
            foreach (var term in functionalGroup.ExpandIntoTerms())
            {
                foreach (var part in term.Parts)
                {
                    switch (part.Type)
                    {
                        case FunctionalGroupPartType.Superscript:
                            Inlines.Add(new Run(part.Text)
                            {
                                Typography = { Variants = FontVariants.Superscript },
                                BaselineAlignment = BaselineAlignment.Superscript,
                                FontSize = FontSize * ScriptSize
                            });
                            break;

                        case FunctionalGroupPartType.Subscript:
                            Inlines.Add(new Run(part.Text)
                            {
                                Typography = { Variants = FontVariants.Subscript },
                                BaselineAlignment = BaselineAlignment.Subscript,
                                FontSize = FontSize * ScriptSize
                            });
                            break;

                        default:
                            Inlines.Add(new Run(part.Text));
                            break;
                    }
                }
            }
        }
    }
}