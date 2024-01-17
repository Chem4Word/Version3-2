// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace Chem4Word.ACME.Interfaces
{
    public interface IRandomizer
    {
        int RandomInt(int max);

        int RandomInt(int min, int max);
    }
}