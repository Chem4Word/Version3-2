// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2;
using System.Windows;

namespace Chem4Word.ACME.Models
{
    public class NewAtomPlacement
    {
        public Point Position { get; set; }
        public Atom ExistingAtom { get; set; }
    }
}