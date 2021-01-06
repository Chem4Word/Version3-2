// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;

namespace Chem4Word.Renderer.OoXmlV4.Entities.Diagnostic
{
    public class DiagnosticSpot
    {
        public Point Point { get; }

        public string Colour { get; }

        public double Diameter { get; }

        public DiagnosticSpot(Point point, string colour, double diameter)
        {
            Point = point;
            Colour = colour;
            Diameter = diameter;
        }
    }
}