// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Windows;

namespace Chem4Word.Renderer.OoXmlV4.Entities
{
    public class InnerCircle
    {
        public List<Point> Points { get; set; } = new List<Point>();
        public Point Centre { get; set; }
    }
}