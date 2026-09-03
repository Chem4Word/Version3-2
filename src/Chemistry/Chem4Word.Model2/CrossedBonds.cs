// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;

namespace Chem4Word.Model2
{
    public class CrossedBonds
    {
        public Bond ShortBond { get; }
        public Bond LongBond { get; }
        public Point CrossingPoint { get; }

        public CrossedBonds(Bond bond1, Bond bond2, Point crossing)
        {
            CrossingPoint = crossing;

            if (bond1.BondLength > bond2.BondLength)
            {
                LongBond = bond1;
                ShortBond = bond2;
            }
            else
            {
                ShortBond = bond1;
                LongBond = bond2;
            }
        }
    }
}