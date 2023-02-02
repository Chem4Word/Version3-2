// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Chem4Word.Model2.Enums;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.ACME.Controls
{
    public class FormulaBlock : TextBlock
    {
        public string Formula
        {
            get { return (string)GetValue(FormulaProperty); }
            set { SetValue(FormulaProperty, value); }
        }

        public static readonly DependencyProperty FormulaProperty =
            DependencyProperty.Register("Formula", typeof(string), typeof(FormulaBlock),
                                        new FrameworkPropertyMetadata("",
                                                                      FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure,
                                                                      FormulaChangedCallback));

        private static void FormulaChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            if (d is FormulaBlock formulaBlock)
            {
                // ToDo: Refactor; This is a near duplicate of $\src\Chemistry\Chem4Word.ACME\LabelsEditor.xaml.cs
                string newFormula = (string)args.NewValue;

                formulaBlock.Text = string.Empty;
                formulaBlock.Inlines.Clear();

                var parts = FormulaHelper.ParseFormulaIntoParts(newFormula);
                foreach (var formulaPart in parts)
                {
                    // Add in the new element
                    switch (formulaPart.PartType)
                    {
                        case FormulaPartType.Multiplier:
                        case FormulaPartType.Separator:
                            var run1 = new Run(formulaPart.Text);
                            formulaBlock.Inlines.Add(run1);
                            break;

                        case FormulaPartType.Element:
                            var run2 = new Run(formulaPart.Text);
                            formulaBlock.Inlines.Add(run2);
                            if (formulaPart.Count > 1)
                            {
                                var subscript = new Run($"{formulaPart.Count}")
                                {
                                    BaselineAlignment = BaselineAlignment.Subscript
                                };
                                subscript.FontSize -= 2;
                                formulaBlock.Inlines.Add(subscript);
                            }

                            break;

                        case FormulaPartType.Charge:
                            var absCharge = Math.Abs(formulaPart.Count);
                            if (absCharge > 1)
                            {
                                var superscript1 = new Run($"{absCharge}{formulaPart.Text}")
                                {
                                    BaselineAlignment = BaselineAlignment.Top
                                };
                                superscript1.FontSize -= 3;
                                formulaBlock.Inlines.Add(superscript1);
                            }
                            else
                            {
                                var superscript2 = new Run($"{formulaPart.Text}")
                                {
                                    BaselineAlignment = BaselineAlignment.Top
                                };
                                superscript2.FontSize -= 3;
                                formulaBlock.Inlines.Add(superscript2);
                            }
                            break;
                    }
                }
            }
        }
    }
}