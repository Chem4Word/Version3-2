// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Media.TextFormatting;

namespace Chem4Word.ACME.Drawing
{
    public class SubscriptTextRunProperties : LabelTextRunProperties
    {
        private double _SubscriptSize = 0.0;

        public SubscriptTextRunProperties(string colour, double SubscriptSize) : base(colour, SubscriptSize)
        {
            _SubscriptSize = SubscriptSize;
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
            get { return BaselineAlignment.Subscript; }
        }

        public override TextRunTypographyProperties TypographyProperties
        {
            get
            {
                return new SubscriptTextRunTypographyProperties();
            }
        }
    }
}