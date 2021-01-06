// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Chem4Word.Core.Helpers
{
    public static class DebugHelper
    {
        public static void WriteLine(string message,
            [CallerLineNumber] int lineNumber = 0,
            [CallerMemberName] string caller = null,
            [CallerFilePath] string file = null)
        {
            Debug.WriteLine($"{message} [line {lineNumber} in {caller} of {file}]");
        }
    }
}