// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

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

            if (!string.IsNullOrEmpty(value))
            {
                if (value.Length >= 3)
                {
                    result = !(value.Contains("<") || value.Contains(">"));
                }
            }

            return result;
        }
    }
}