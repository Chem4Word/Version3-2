// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace Chem4Word.ACME.Interfaces
{
    public interface IPositioner
    {
        double Delta { get; set; }
        double StartX { get; set; }
        double StartY { get; set; }

        bool GetNextPoint(out double x, out double y);
    }
}