// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Globalization;

namespace Chem4Word.Core.Helpers
{
    public static class SafeDouble
    {
        public static string Duration(double duration)
            => AsString(duration);

        public static string AsString(double value)
            => value.ToString("#,##0.00", CultureInfo.InvariantCulture);

        public static string AsString4(double value)
            => value.ToString("#,##0.0000", CultureInfo.InvariantCulture);

        public static string AsString0(double value)
            => value.ToString("#,##0", CultureInfo.InvariantCulture);
    }
}