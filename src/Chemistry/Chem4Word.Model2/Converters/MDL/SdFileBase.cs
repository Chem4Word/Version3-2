// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.IO;

namespace Chem4Word.Model2.Converters.MDL
{
    public class SdFileBase
    {
        public virtual SdfState ImportFromStream(StreamReader reader, Molecule molecule, out string message)
        {
            message = null;
            return SdfState.Null;
        }
    }
}