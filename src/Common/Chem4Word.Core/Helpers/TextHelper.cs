// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Linq;

namespace Chem4Word.Core.Helpers
{
    public static class TextHelper
    {
        /// <summary>
        /// Checks to see if a string is valid to use for searching
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsValidSearchString(string value)
        {
            var result = false;

            var temp = StripControlCharacters(value).Trim();

            if (!string.IsNullOrEmpty(temp)
                && temp.Length >= 3)
            {
                result = !(temp.Contains("<") || temp.Contains(">"));
            }

            return result;
        }

        public static string StripControlCharacters(string value)
        {
            return new string(value.Where(c => c >= 32).ToArray());
        }
    }
}