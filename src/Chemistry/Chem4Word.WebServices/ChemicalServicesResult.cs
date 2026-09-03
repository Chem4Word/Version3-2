// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;

namespace Chem4Word.WebServices
{
    public class ChemicalServicesResult
    {
        public ChemicalProperties[] Properties { get; set; }
        public List<string> Errors { get; set; }
        public List<string> Messages { get; set; }
    }
}