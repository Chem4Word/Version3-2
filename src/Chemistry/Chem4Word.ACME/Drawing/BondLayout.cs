// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

/*
    These are simple classes that define the shape of a bond visual.
    They simplify the transfer of information into and out of drawing routines.
    You can either use the Point properties and draw primitives directly from those,
    or use the DefinedGeometry and draw that directly
*/

namespace Chem4Word.ACME.Drawing
{
    /// <summary>
    ///     Static class to define bond geometries
    ///     now uses StreamGeometry in preference to PathGeometry
    /// </summary>
    public class BondLayout
    {
        //start and end point of the bond
        public Point Start;

        public Point End;

        //convex hulls of the start and end atom labels
        public List<Point> StartAtomHull;

        public List<Point> EndAtomHull;

        //boundary of the bond
        public virtual List<System.Windows.Point> Boundary { get; }

        public Geometry DefiningGeometry { get; set; }

        public Vector PrincipleVector
        {
            get { return End - Start; }
        }

        public BondLayout()
        {
            Boundary = new List<Point>();
        }
    }
}