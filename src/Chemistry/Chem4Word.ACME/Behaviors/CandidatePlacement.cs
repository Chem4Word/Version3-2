// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;

namespace Chem4Word.ACME.Behaviors
{
    public class CandidatePlacement
    {
        public int Separation { get; set; }
        public Vector Orientation { get; set; }
        public int NeighbourWeights { get; set; }
        public Point PossiblePlacement { get; set; }
        public bool Crowding { get; set; }
    }
}