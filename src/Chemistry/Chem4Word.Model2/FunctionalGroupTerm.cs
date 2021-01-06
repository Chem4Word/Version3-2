// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;

namespace Chem4Word.Model2
{
    public class FunctionalGroupTerm
    {
        public List<FunctionalGroupPart> Parts { get; set; }
        public bool IsAnchor { get; set; }

        public FunctionalGroupTerm()
        {
            Parts = new List<FunctionalGroupPart>();
        }

        public override string ToString()
        {
            return $"{Parts.Count} parts; IsAnchor {IsAnchor}";
        }
    }
}