// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;

namespace Chem4Word.ACME.Drawing.Text
{
    public class SubscriptTextRunTypographyProperties : LabelTextRunTypographyProperties
    {
        public override FontVariants Variants
        {
            get { return FontVariants.Subscript; }
        }
    }
}