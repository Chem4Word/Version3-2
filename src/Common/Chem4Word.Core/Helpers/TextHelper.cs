// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            bool result = false;

            string temp = StripAsciiControlCharacters(value).Trim();

            if (!string.IsNullOrEmpty(temp)
                && temp.Length >= 3)
            {
                result = !(temp.Contains("<") || temp.Contains(">"));
            }

            return result;
        }

        public static string StripAsciiControlCharacters(string value)
        {
            return new string(value.Where(c => c >= 32).ToArray());
        }

        /// <summary>
        /// Converts subscript and superscript and other special characters to "normal" characters
        /// i.e. C₂H₅OH → C2H5OH
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string NormalizeCharacters(string input)
        {
            // User searched for 'Glycerol + 3 Fatty acids (RCOOH) →Triglyceride (RCOO)3C3H5 + 3H2O'
            // User searched for 'C₂H₅OH'

            if (string.IsNullOrEmpty(input)) return input;

            StringBuilder sb = new StringBuilder(input.Length);
            foreach (char c in input)
            {
                if (SubscriptMap.TryGetValue(c, out char normal)
                    || SuperscriptMap.TryGetValue(c, out normal)
                    || OtherCharactersMap.TryGetValue(c, out normal))
                {
                    sb.Append(normal);
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static readonly Dictionary<char, char> SuperscriptMap = new Dictionary<char, char>
        {
            ['⁰'] = '0',
            ['¹'] = '1',
            ['²'] = '2',
            ['³'] = '3',
            ['⁴'] = '4',
            ['⁵'] = '5',
            ['⁶'] = '6',
            ['⁷'] = '7',
            ['⁸'] = '8',
            ['⁹'] = '9'
        };

        private static readonly Dictionary<char, char> SubscriptMap = new Dictionary<char, char>
        {
            ['₀'] = '0',
            ['₁'] = '1',
            ['₂'] = '2',
            ['₃'] = '3',
            ['₄'] = '4',
            ['₅'] = '5',
            ['₆'] = '6',
            ['₇'] = '7',
            ['₈'] = '8',
            ['₉'] = '9'
        };

        private static readonly Dictionary<char, char> OtherCharactersMap = new Dictionary<char, char>
        {
            ['→'] = '>'
        };
    }
}
