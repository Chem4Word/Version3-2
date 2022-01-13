// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Media;

/* BondLayouts are simple classes that define the shape of a bond visual.
   They simplify the transfer of information into and out of drawing routines.
   You can either use the Point properties and draw primitives directly from those,
   or use the DefinedGeometry and draw that directly   */

namespace Chem4Word.ACME.Drawing
{
    public class WedgeBondLayout : BondLayout
    {
        public Point FirstCorner;
        public Point SecondCorner;
        public bool CappedOff; //is the bond end drawn as a line?

        public StreamGeometry GetOutline()
        {
            var streamGeometry = new StreamGeometry();
            ;
            using (var sgc = streamGeometry.Open())
            {
                sgc.BeginFigure(Start, true, false);
                sgc.LineTo(FirstCorner, false, true);
                sgc.LineTo(End, CappedOff, true);
                sgc.LineTo(SecondCorner, CappedOff, true);

                sgc.Close();
            }

            streamGeometry.Freeze();
            return streamGeometry;
        }
    }
}