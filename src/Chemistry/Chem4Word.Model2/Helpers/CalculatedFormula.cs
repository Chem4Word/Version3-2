// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;

namespace Chem4Word.Model2.Helpers
{
    public class CalculatedFormula
    {
        public List<MoleculeFormulaPart> Parts { get; }

        public CalculatedFormula()
        {
            Parts = new List<MoleculeFormulaPart>();
        }

        public CalculatedFormula(List<MoleculeFormulaPart> parts)
        {
            Parts = parts;
        }

        public string ToUnicodeString()
        {
            return FormulaHelper.FormulaPartsAsUnicode(Parts);
        }

        public override string ToString()
        {
            return FormulaHelper.FormulaPartsAsString(Parts);
        }
    }
}