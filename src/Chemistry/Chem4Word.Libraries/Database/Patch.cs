// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Chem4Word.Libraries.Database
{
    public class Patch
    {
        public Version Version { get; set; }
        public List<string> Scripts { get; set; }
    }
}