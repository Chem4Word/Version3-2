// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Linq;

namespace Chem4Word.Model2.Helpers
{
    public static class AtomHelpers
    {
        public static bool TryParse(string text, out ElementBase result)
        {
            bool success = false;
            result = null;

            if (Globals.PeriodicTable.HasElement(text))
            {
                result = Globals.PeriodicTable.Elements[text];
                success = true;
            }
            else
            {
                result = Globals.FunctionalGroupsList.FirstOrDefault(n => n.Name.Equals(text));
                success = result != null;
            }

            return success;
        }
    }
}