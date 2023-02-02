// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;

namespace Chem4Word.Model2.Helpers
{
    public class ModelFormulaPart
    {
        public List<MoleculeFormulaPart> Parts { get; }
        public int Count { get; set; }

        public ModelFormulaPart(List<MoleculeFormulaPart> parts, int count)
        {
            Parts = parts;
            Count = count;
        }
    }
}