// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Windows;
using Chem4Word.Model2.Helpers;

/* Descriptors are simple classes that define the shape of a bond visual.
   They simplify the transfer of information into and out of drawing routines.
   You can either use the Point properties and draw primitives directly from those,
   or use the DefinedGeometry and draw that directly
   */

namespace Chem4Word.ACME.Drawing
{
    public class DoubleBondLayout : BondLayout
    {
        public Point SecondaryStart; //refers to the subsidiary bond (dotted in line in 1.5 bonds)
        public Point SecondaryEnd;
        public Globals.BondDirection Placement; //which side of the line the subsidiary goes
        public Point? PrimaryCentroid;
        public Point? SecondaryCentroid;

        public List<Point> StartNeigbourPositions;
        public List<Point> EndNeighbourPositions;
    }
}