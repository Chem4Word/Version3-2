// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;

namespace Chem4Word.Libraries
{
    public class LibrarySettings
    {
        public string ProgramDataPath { get; set; }
        public Point ParentTopLeft { get; set; }
        public string Version { get; set; }

        public double PreferredBondLength { get; set; }
        public bool SetBondLengthOnImport { get; set; }
        public bool RemoveExplicitHydrogensOnImport { get; set; }
    }
}