// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Windows;

namespace Chem4Word.Renderer.OoXmlV4.Entities.Diagnostic
{
    public class Diagnostics
    {
        public List<DiagnosticLine> Lines { get; } = new List<DiagnosticLine>();
        public List<List<Point>> Polygons { get; } = new List<List<Point>>();
        public List<DiagnosticSpot> Points { get; } = new List<DiagnosticSpot>();
        public List<DiagnosticRectangle> Rectangles { get; } = new List<DiagnosticRectangle>();
    }
}