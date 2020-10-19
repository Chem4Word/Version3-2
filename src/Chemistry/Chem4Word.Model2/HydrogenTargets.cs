// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;

namespace Chem4Word.Model2
{
    public class HydrogenTargets
    {
        public List<Atom> Atoms { get; set; }
        public List<Bond> Bonds { get; set; }
        public Dictionary<string, Molecule> Molecules { get; set; }

        public HydrogenTargets()
        {
            Atoms = new List<Atom>();
            Bonds = new List<Bond>();
            Molecules = new Dictionary<string, Molecule>();
        }
    }
}