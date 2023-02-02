﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace Chem4Word.Model2
{
    public enum ReactionType
    {
        Normal,
        Reversible,
        ReversibleBiasedForward,
        ReversibleBiasedReverse,
        Blocked,
        Resonance,
        Retrosynthetic
    }
}