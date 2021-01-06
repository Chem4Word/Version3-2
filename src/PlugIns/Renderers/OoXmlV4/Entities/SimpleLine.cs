// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;

namespace Chem4Word.Renderer.OoXmlV4.Entities
{
    public class SimpleLine
    {
        public Point Start { get; set; }

        public Point End { get; set; }

        public SimpleLine(Point startPoint, Point endPoint)
        {
            Start = startPoint;
            End = endPoint;
        }
    }
}