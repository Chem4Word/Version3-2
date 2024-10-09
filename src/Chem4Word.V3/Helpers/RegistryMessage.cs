// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
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