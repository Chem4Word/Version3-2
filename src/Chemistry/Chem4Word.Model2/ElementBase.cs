// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.ComponentModel;

namespace Chem4Word.Model2
{
    [TypeConverter(typeof(ElementConverter))]
    public abstract class ElementBase
    {
        public virtual double AtomicWeight { get; set; }

        public virtual string Symbol { get; set; }

        public virtual string Name { get; set; }

        public virtual string Colour { get; set; }
    }
}