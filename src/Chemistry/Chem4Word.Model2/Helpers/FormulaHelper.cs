// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Chem4Word.Model2.Helpers
{
    public static class FormulaHelper
    {
        private static readonly char[] SubScriptNumbers = {
                                                              '\u2080', '\u2081', '\u2082', '\u2083', '\u2084',
                                                              '\u2085', '\u2086', '\u2087', '\u2088', '\u2089'
                                                          };

        private static readonly char[] SuperScriptNumbers = {
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
                    switch (part.PartType)
                    {
                        case FormulaPartType.Separator:
                        case FormulaPartType.Multiplier:
                            strings.Add(part.Text);
                            break;

                        case FormulaPartType.Element:
                            strings.Add($"{part.Text} {part.Count}");
                            break;

                        case FormulaPartType.Charge:
                            if (part.Count != 0)
                            {
                                var absCharge = Math.Abs(part.Count);
                                if (absCharge == 1)
                                {
                                    strings.Add($"{part.Text}");
                                }
                                else
                                {
                                    strings.Add($"{part.Text} {absCharge}");
                                }
                            }
                            break;
                    }
                }

                result = string.Join(" ", strings);
            }

            return result;
        }

        public static string FormulaPartsAsUnicode(List<MoleculeFormulaPart> parts)
        {
            var result = string.Empty;

            foreach (var part in parts)
            {
                switch (part.PartType)
                {
                    case FormulaPartType.Separator:
                    case FormulaPartType.Multiplier:
                        if (!string.IsNullOrEmpty(part.Text))
                        {
                            result += part.Text;
                        }
                        break;

                    case FormulaPartType.Element:
                        switch (part.Count)
                        {
                            case 1: // No Subscript
                                if (!string.IsNullOrEmpty(part.Text))
                                {
                                    result += part.Text;
                                }
                                break;

                            default: // With Subscript
                                if (!string.IsNullOrEmpty(part.Text))
                                {
                                    result += part.Text;
                                }

                                if (part.Count > 0)
                                {
                                    result += string.Concat($"{part.Count}".Select(c => SubScriptNumbers[c - 48]));
                                }
                                break;
                        }
                        break;

                    case FormulaPartType.Charge:
                        var absCharge = Math.Abs(part.Count);
                        if (absCharge > 1)
                        {
                            result += string.Concat($"{absCharge}".Select(c => SuperScriptNumbers[c - 48]));
                        }

                        // Unicode Superscript for + or -
                        switch (part.Text)
                        {
                            case "+":
                                result += '\u207a';
                                break;

                            case "-":
                                result += '\u207b';
                                break;
                        }
                        break;
                }
            }

            return result;
        }

        public static List<MoleculeFormulaPart> ParseFormulaIntoParts(string input)
        {
            var allParts = new List<MoleculeFormulaPart>();

            var elements = Globals.PeriodicTable.ValidElements.Split('|').ToList();
            // Add charge and special characters so we can detect them
            elements.AddRange(new[] { "+", "-", "[", "]", "." });

            // Sort elements by length descending this enables accurate detection of two character, then one character elements
            elements.Sort((b, a) => a.Length.CompareTo(b.Length));

            if (!string.IsNullOrEmpty(input))
            {
                var chunks = SplitString(input);

                for (var i = 0; i < chunks.Length; i++)
                {
                    var parsed = ParseString(elements, chunks[i]);
                    allParts.AddRange(parsed);
                }
            }

            // Detect if we found any elements
            var c1 = allParts.Count(w => w.PartType == FormulaPartType.Element);

            // Return a List if at least one element was found
            return c1 > 0
                ? allParts
                : new List<MoleculeFormulaPart>();
        }

        private static string[] SplitString(string value)
        {
            var result = new List<string>();

            // Remove all spaces
            var temp = value.Replace(" ", "");

            // Replace any Bullet characters <Alt>0183 with dot
            temp = temp.Replace("·", ".");

            var chunk = "";

            for (var i = 0; i < temp.Length; i++)
            {
                if (temp[i] == '[' || temp[i] == '.' || temp[i] == ']')
                {
                    // Got a match
                    if (!string.IsNullOrEmpty(chunk))
                    {
                        result.Add(chunk);
                    }
                    result.Add(temp[i].ToString());
                    chunk = "";
                }
                else
                {
                    chunk += temp[i];
                }
            }

            // Add in what's left over
            if (!string.IsNullOrEmpty(chunk))
            {
                result.Add(chunk);
            }

            return result.ToArray();
        }

        private static List<MoleculeFormulaPart> ParseString(List<string> elements, string formula)
        {
            var parts = new SortedDictionary<int, MoleculeFormulaPart>();

            #region Detect each type of element, charge or separator

            foreach (var element in elements)
            {
                if (formula.Contains(element))
                {
                    var idx = formula.IndexOf(element, StringComparison.InvariantCulture);

                    var type = FormulaPartType.Element;
                    switch (element)
                    {
                        case "+":
                        case "-":
                            type = FormulaPartType.Charge;
                            break;

                        case "[":
                        case ".":
                        case "]":
                            type = FormulaPartType.Separator;
                            break;
                    }

                    var info = new MoleculeFormulaPart(type, idx, element, 0);

                    // Convert dot to a Bullett
                    if (info.PartType == FormulaPartType.Separator && element.Equals("."))
                    {
                        // Bullet character <Alt>0183
                        info.Text = " · ";
                    }

                    // Prevent insertion of element with less characters at same index
                    if (!parts.ContainsKey(idx))
                    {
                        parts.Add(idx, info);
                    }
                }
            }

            #endregion Detect each type of element, charge or separator

            // Convert SortedDictionary to a list to make it easier to process
            var list = parts.Values.ToList();

            // Handle Multiplier
            if (list.Count > 0 && list[0].Index > 0)
            {
                var multiplier = formula.Substring(0, list[0].Index);
                parts.Add(0, new MoleculeFormulaPart(FormulaPartType.Multiplier, multiplier, 0));
            }

            #region Detect counts

            for (var i = 0; i < list.Count; i++)
            {
                var start = list[i].Index;
                // Extract chunk from first character of element to first character of next chunk or end of target
                string chunk;
                if (i < list.Count - 1)
                {
                    var length = list[i + 1].Index - start;
                    chunk = formula.Substring(start, length);
                }
                else
                {
                    chunk = formula.Substring(start);
                }

                var symbol = list[i].Text;

                if (list[i].PartType == FormulaPartType.Element
                    || list[i].PartType == FormulaPartType.Charge)
                {
                    // Remove it's symbol from the chunk to leave behind the numeric portion
                    var digits = chunk.Replace(symbol, "");

                    int number;
                    if (string.IsNullOrEmpty(digits))
                    {
                        // Assume 1 if it's an empty string
                        number = 1;
                    }
                    else
                    {
                        if (int.TryParse(digits, out number))
                        {
                            list[i].Count = number;
                        }
                        else
                        {
                            list[i].Count = 999999;
                        }
                    }

                    // If this is a negative charge invert the value
                    if (list[i].PartType == FormulaPartType.Charge
                        && symbol.Equals("-"))
                    {
                        list[i].Count = 0 - list[i].Count;
                    }
                }
            }

            #endregion Detect counts

            // Detect counts which mark invalid parsing
            var c1 = parts.Values.Count(c => c.Count == 999999);
            var c2 = parts.Values.Count(c => c.Count == -999999);

            // Return a List if everything is valid
            return c1 + c2 == 0
                ? parts.Values.ToList()
                : new List<MoleculeFormulaPart>();
        }
    }
}