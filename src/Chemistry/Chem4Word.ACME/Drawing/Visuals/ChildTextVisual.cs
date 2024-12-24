// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Drawing.Text;
using System.Windows.Media;

namespace Chem4Word.ACME.Drawing.Visuals
{
    public class ChildTextVisual : AtomVisual
    {
        public AtomVisual ParentVisual { get; protected set; }
        public DrawingContext Context { get; set; }
        public AtomTextMetrics ParentMetrics { get; set; }
        public AtomTextMetrics Metrics { get; protected set; }
        public AtomTextMetrics HydrogenMetrics { get; set; }
    }
}