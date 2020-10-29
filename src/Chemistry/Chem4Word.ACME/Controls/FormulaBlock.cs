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
            FormulaBlock fb = d as FormulaBlock;

            string newFormula = (string)args.NewValue;

            var parts = FormulaHelper.ParseFormulaIntoParts(newFormula);

            foreach (MoleculeFormulaPart formulaPart in parts)
            {
                //add in the new element

                Run atom = new Run(formulaPart.Element);
                fb.Inlines.Add(atom);

                if (formulaPart.Count > 1)
                {
                    Run subs = new Run(formulaPart.Count.ToString());

                    subs.BaselineAlignment = BaselineAlignment.Subscript;
                    subs.FontSize = subs.FontSize - 2;
                    fb.Inlines.Add(subs);
                }
            }
        }
    }
}