// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace Chem4Word.Searcher.OpsinPlugIn.Model
{
    public class OpsinResult
    {
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Cml { get; set; } = string.Empty;
        public string Inchi { get; set; } = string.Empty;
        public string StdInchi { get; set; } = string.Empty;
        public string StdInchiKey { get; set; } = string.Empty;
        public string Smiles { get; set; } = string.Empty;
    }
}
