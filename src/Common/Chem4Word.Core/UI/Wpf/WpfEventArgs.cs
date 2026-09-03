// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;

namespace Chem4Word.Core.UI.Wpf
{
    public class WpfEventArgs : EventArgs
    {
        public string Button { get; set; }
        public string OutputValue { get; set; }
    }
}