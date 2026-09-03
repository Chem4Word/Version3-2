// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace Chem4Word.Helpers
{
    public class RegistryMessage
    {
        public string Date { get; set; }
        public string ProcessId { get; set; }
        public string Message { get; set; }

        public override string ToString() => $"{Date} [{ProcessId}] {Message}";
    }
}