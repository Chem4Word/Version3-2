// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Chem4Word.Model2.Helpers
{
    public static class Utils
    {
        public static bool AreAllH(this IEnumerable<Atom> atomlist)
        {
            return atomlist.All(a => (a.Element as Element) == Globals.PeriodicTable.H);
        }

        public static bool ContainNoH(this IEnumerable<Atom> atomList)
        {
            return atomList.All(a => ((a.Element as Element) != Globals.PeriodicTable.H & a.ImplicitHydrogenCount == 0));
        }

        public static Atom GetFirstNonH(this IEnumerable<Atom> atomList)
        {
            return atomList.FirstOrDefault(a => a.Element as Element != Globals.PeriodicTable.H);
        }

        public static int GetHCount(this IEnumerable<Atom> atomList)
        {
            return atomList.Count(a => a.Element as Element == Globals.PeriodicTable.H);
        }

        public static int GetNonHCount(this IEnumerable<Atom> atomList)
        {
            return atomList.Count() - atomList.GetHCount();
        }

        public static string GetRelativePath(string ancestor, string descendant)
        {
            Uri urifrom = new Uri(ancestor);

            Uri urito = new Uri(descendant);

            Uri relativeURI = urifrom.MakeRelativeUri(urito);

            return relativeURI.ToString();
        }

        public static string UpTo(this string input, string terminator)
        {
            if (!string.IsNullOrEmpty(terminator))
            {
                if (!string.IsNullOrEmpty(input))
                {
                    var index = input.IndexOf(terminator, StringComparison.InvariantCulture);
                    if (index >= 0)
                    {
                        return input.Substring(0, index);
                    }
                }
            }

            return input;
        }
    }
}