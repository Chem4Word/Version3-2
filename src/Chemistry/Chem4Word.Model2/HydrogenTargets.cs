// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Chem4Word.Model2
{
    public class HydrogenTargets
    {
        public List<Atom> Atoms { get; set; }
        public List<Bond> Bonds { get; set; }
        public Dictionary<Guid, Molecule> Molecules { get; set; }

        public HydrogenTargets()
        {
            Atoms = new List<Atom>();
            Bonds = new List<Bond>();
            Molecules = new Dictionary<Guid, Molecule>();
        }
    }
}