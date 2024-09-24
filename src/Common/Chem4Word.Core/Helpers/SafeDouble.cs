// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
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

        /// <summary>
        /// Formats a double as "#,##0.00" without any culture.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string AsString(double value)
            => value.ToString("#,##0.00", CultureInfo.InvariantCulture);

        /// <summary>
        /// Formats a double as "#,##0.0000" without any culture.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string AsString4(double value)
            => value.ToString("#,##0.0000", CultureInfo.InvariantCulture);

        /// <summary>
        /// Formats a double as "0.0###" without any culture.
        /// We are writing co-ordinates in CML with 4 decimal places to be consistent with industry standard MDL format
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string AsCMLString(double value)
            => value.ToString("0.0###", CultureInfo.InvariantCulture);

        /// <summary>
        /// Formats a double as "#,##0" without any culture.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string AsString0(double value)
            => value.ToString("#,##0", CultureInfo.InvariantCulture);

        /// <summary>
        /// Parses a value into a double in a culture independent manner
        /// In chemistry files doubles are always represented using comma for thousands and dot for decimal point
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double Parse(string value)
            => double.Parse(value, CultureInfo.InvariantCulture);
    }
}