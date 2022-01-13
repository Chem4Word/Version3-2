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
    public class SuperscriptTextRunProperties : LabelTextRunProperties
    {
        private double _SubscriptSize = 0.0;

        public SuperscriptTextRunProperties(string colour, double subScriptSize) : base(colour, subScriptSize)
        {
            _SubscriptSize = subScriptSize;
        }

        public override double FontHintingEmSize
        {
            get { return _SubscriptSize; }
        }

        public override double FontRenderingEmSize
        {
            get { return _SubscriptSize; }
        }

        public override BaselineAlignment BaselineAlignment
        {
            get { return BaselineAlignment.Superscript; }
        }

        public override TextRunTypographyProperties TypographyProperties
        {
            get
            {
                return new SuperscriptTextRunTypographyProperties();
            }
        }
    }
}