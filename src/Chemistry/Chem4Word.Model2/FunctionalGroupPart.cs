// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace Chem4Word.Model2
{
    public class FunctionalGroupPart
    {
        public string Text { get; set; }

        public FunctionalGroupPartType Type { get; set; }

        public FunctionalGroupPart()
        {
            Type = FunctionalGroupPartType.Normal;
        }

        public override string ToString()
        {
            return $"{Text} {Type}";
        }
    }
}