// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.ACME.Utils
{
    public static class TextUtils
    {
        public static string GetSubText(int hCount = 0)
        {
            string mult = "";
            int i;
            if ((i = Math.Abs(hCount)) > 1)
            {
                mult = i.ToString();
            }

            return mult;
        }

        /// <summary>
        /// gets the charge annotation string for an atom symbol
        /// </summary>
        /// <param name="charge">Int containing the charge value</param>
        /// <returns></returns>
        public static string GetChargeString(int? charge)
        {
            string chargeString = "";

            int chargeVal = charge ?? 0;
            if (chargeVal > 0)
            {
                chargeString = "+";
            }
            //use the en-rule for single negative charges - it makes them more distinct
            else if (chargeVal < 0)
            {
                chargeString = Globals.EnDashSymbol;
            }

            int abscharge;
            if ((abscharge = Math.Abs(chargeVal)) > 1)
            {
                chargeString = abscharge + chargeString;
            }
            return chargeString;
        }
    }
}