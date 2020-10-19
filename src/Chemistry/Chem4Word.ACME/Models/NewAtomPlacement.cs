// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using Chem4Word.Model2;

namespace Chem4Word.ACME
{
    public class NewAtomPlacement
    {
        public Point Position { get; set; }
        public Atom ExistingAtom { get; set; }
    }
}