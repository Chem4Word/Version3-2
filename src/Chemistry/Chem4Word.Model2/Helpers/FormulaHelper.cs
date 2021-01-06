// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Chem4Word.Model2.Helpers
{
    public static class FormulaHelper
    {
        private static char[] subScriptNumbers = {
                                      '\u2080', '\u2081', '\u2082', '\u2083', '\u2084',
                                      '\u2085', '\u2086', '\u2087', '\u2088', '\u2089'
                                  };

        private static char[] superScriptNumbers = {
                                        '\u2070', '\u00B9', '\u00B2', '\u00B3', '\u2074',
                                        '\u2075', '\u2076', '\u2077', '\u2078', '\u2079'
                                    };

        public static string FormulaPartsAsString(List<MoleculeFormulaPart> parts)
        {
            var result = string.Empty;

            if (parts.Any())
            {
                var strings = new List<string>();

                foreach (var part in parts)
                {
                    strings.Add($"{part.Element} {part.Count}");
                }

                result = string.Join(" ", strings);
            }

            return result;
        }

        public static string FormulaPartsAsUnicode(List<MoleculeFormulaPart> parts)
        {
            string result = string.Empty;

            foreach (var part in parts)
            {
                switch (part.Count)
                {
                    case 0: // Separator or multiplier
                    case 1: // No Subscript
                        if (!string.IsNullOrEmpty(part.Element))
                        {
                            result += part.Element;
                        }
                        break;

                    default: // With Subscript
                        if (!string.IsNullOrEmpty(part.Element))
                        {
                            result += part.Element;
                        }

                        if (part.Count > 0)
                        {
                            result += string.Concat($"{part.Count}".Select(c => subScriptNumbers[c - 48]));
                        }
                        break;
                }
            }

            return result;
        }

        public static List<MoleculeFormulaPart> ParseFormulaIntoParts(string input)
        {
            // Input is any of
            //  "2 C 6 H 6 . C 7 H 7"
            //  "C 6 H 6"
            //  "C7H7"
            //  "C7H6N"

            List<MoleculeFormulaPart> parts = new List<MoleculeFormulaPart>();
            if (!string.IsNullOrEmpty(input))
            {
                // Remove all spaces
                string temp = input.Replace(" ", "");

                // Replace any Bullet characters <Alt>0183 with dot
                temp = temp.Replace("·", ".");

                string[] formulae = temp.Split('.');
                for (int i = 0; i < formulae.Length; i++)
                {
                    // http://stackoverflow.com/questions/11232801/regex-split-numbers-and-letter-groups-without-spaces
                    // Code below is based on answer "use 'look around' in split regex"
                    string[] xx = Regex.Split(formulae[i], @"(?<=\d)(?=\D)|(?=\d)(?<=\D)");
                    int j = 0;
                    while (j < xx.Length)
                    {
                        int x;
                        if (int.TryParse(xx[j], out x))
                        {
                            // Multiplier
                            parts.Add(new MoleculeFormulaPart($"{x} ", 0));
                            j++;
                        }
                        else
                        {
                            if (j <= xx.Length - 2)
                            {
                                // Atom and Count
                                parts.Add(new MoleculeFormulaPart(xx[j], int.Parse(xx[j + 1])));
                            }
                            else
                            {
                                // Atom and Implicit Count of 1
                                parts.Add(new MoleculeFormulaPart(xx[j], 1));
                            }
                            j += 2;
                        }
                    }

                    // Add Seperator
                    if (i < formulae.Length - 1)
                    {
                        // Using Bullet character <Alt>0183
                        parts.Add(new MoleculeFormulaPart(" · ", 0));
                    }
                }
            }
            return parts;
        }
    }
}