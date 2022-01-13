// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows.Media;

namespace Chem4Word.ACME.Entities
{
    public class WordCloudEntry
    {
        public int Angle { get; set; }

        public SolidColorBrush Brush { get; set; }

        public long Weight { get; set; }

        public string Word { get; set; }
    }
}