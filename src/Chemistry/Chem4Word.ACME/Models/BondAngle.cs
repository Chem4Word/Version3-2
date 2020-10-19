// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace Chem4Word.ACME.Models
{
    public class BondAngle
    {
        public double Value { get; private set; }

        public string Display { get; private set; }

        public BondAngle(double value)
        {
            Value = value;
            Display = $"{value:N2}";
        }

        public BondAngle(double value, string display)
        {
            Value = value;
            Display = display;
        }
    }
}