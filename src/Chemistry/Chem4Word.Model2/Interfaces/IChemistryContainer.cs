// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace Chem4Word.Model2.Interfaces
{
    public interface IChemistryContainer
    {
        IChemistryContainer Root { get; }

        bool RemoveMolecule(Molecule mol);

        Molecule AddMolecule(Molecule newMol);
    }
}