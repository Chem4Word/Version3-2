// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using Chem4Word.Renderer.OoXmlV4.Enums;

namespace Chem4Word.Renderer.OoXmlV4.Entities.Diagnostic
{
    public class DiagnosticLine
    {
        public Point Start { get; }

        public Point End { get; }

        public BondLineStyle Style { get; }

        public string Colour { get; }

        public DiagnosticLine(Point startPoint, Point endPoint, BondLineStyle style, string colour = "000000")
        {
            Start = startPoint;
            End = endPoint;
            Style = style;
            Colour = colour;
        }
    }
}