// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2.Enums;

namespace Chem4Word.Model2.Helpers
{
    public class MoleculeFormulaPart
    {
        public FormulaPartType PartType { get; set; }
        public string Text { get; set; }
        public int Count { get; set; }
        public int Index { get; set; }

        public MoleculeFormulaPart(FormulaPartType partType, string text, int count)
        {
            PartType = partType;
            Text = text;
            Count = count;
        }

        public MoleculeFormulaPart(FormulaPartType partType, int index, string text, int count)
        {
            PartType = partType;
            Index = index;
            Text = text;
            Count = count;
        }
    }
}