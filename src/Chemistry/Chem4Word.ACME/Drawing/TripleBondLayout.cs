// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;

/* Descriptors are simple classes that define the shape of a bond visual.
   They simplify the transfer of information into and out of drawing routines.
   You can either use the Point properties and draw primitives directly from those,
   or use the DefinedGeometry and draw that directly
   */

namespace Chem4Word.ACME.Drawing
{
    public class TripleBondLayout : DoubleBondLayout
    {
        public Point TertiaryStart;
        public Point TertiaryEnd;
    }
}