// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;

namespace Chem4Word.ACME.Enums
{
    [Flags]
    public enum SelectionTypeCode
    {
        None = 0,
        Atom = 1,
        Bond = 2,
        Molecule = 4,
        Reaction = 8
    }
}