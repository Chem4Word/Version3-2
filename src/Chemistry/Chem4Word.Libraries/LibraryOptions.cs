// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;

namespace Chem4Word.Libraries
{
    public class LibraryOptions
    {
        public string ProgramDataPath { get; set; }
        public Point ParentTopLeft { get; set; }
        public string Chem4WordVersion { get; set; }

        public double PreferredBondLength { get; set; }
        public bool SetBondLengthOnImport { get; set; }
        public bool RemoveExplicitHydrogensOnImport { get; set; }
    }
}