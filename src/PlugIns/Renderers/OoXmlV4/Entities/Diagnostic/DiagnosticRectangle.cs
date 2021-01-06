// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;

namespace Chem4Word.Renderer.OoXmlV4.Entities.Diagnostic
{
    public class DiagnosticRectangle
    {
        public Rect BoundingBox { get; }

        public string Colour { get; }

        public DiagnosticRectangle(Rect boundingBox, string colour)
        {
            BoundingBox = boundingBox;
            Colour = colour;
        }
    }
}