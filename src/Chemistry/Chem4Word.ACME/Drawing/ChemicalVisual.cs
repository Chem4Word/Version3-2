// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Windows.Media;

namespace Chem4Word.ACME.Drawing
{
    public abstract class ChemicalVisual : DrawingVisual
    {
        public int RefCount { get; set; } //how many separate references are there to this visual within the model
        public Dictionary<object, DrawingVisual> ChemicalVisuals { get; set; }

        public ChemicalVisual()
        {
            RefCount = 0;
        }

        public abstract void Render();
    }
}