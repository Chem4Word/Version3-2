// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
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
                string newFormula = (string)args.NewValue;

                formulaBlock.Text = string.Empty;
                formulaBlock.Inlines.Clear();

                var parts = FormulaHelper.ParseFormulaIntoParts(newFormula);
                foreach (MoleculeFormulaPart formulaPart in parts)
                {
                    //add in the new element
                    Run atom = new Run(formulaPart.Element);
                    formulaBlock.Inlines.Add(atom);

                    if (formulaPart.Count > 1)
                    {
                        Run subs = new Run(formulaPart.Count.ToString());

                        subs.BaselineAlignment = BaselineAlignment.Subscript;
                        subs.FontSize = subs.FontSize - 2;
                        formulaBlock.Inlines.Add(subs);
                    }
                }
            }
        }
    }
}