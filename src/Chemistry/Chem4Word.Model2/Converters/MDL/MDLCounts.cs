// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace Chem4Word.Model2.Converters.MDL
{
    // ReSharper disable once InconsistentNaming
    public class MDLCounts
    {
        public string Version { get; set; }
        public int Atoms { get; set; }
        public int Bonds { get; set; }
        public string Message { get; set; }
    }
}