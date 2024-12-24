// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;

namespace Chem4Word.Model2.Helpers
{
    public static class Globals
    {
        public static PeriodicTable PeriodicTable = new PeriodicTable();
        public static List<FunctionalGroup> FunctionalGroupsList = FunctionalGroups.ShortcutList;

        #region Strings

        public static string CarbonSymbol = "C";
        public static string AllenicCarbonSymbol = "•";
        public const string EnDashSymbol = "\u2013";

        #endregion Strings

        #region Bond Stuff

        public const string OrderZero = "hbond";

        public const string OrderOther = "other";
        public const string OrderPartial01 = "partial01";
        public const string OrderSingle = "S";
        public const string OrderPartial12 = "partial12";
        public const string OrderAromatic = "A";
        public const string OrderDouble = "D";
        public const string OrderPartial23 = "partial23";
        public const string OrderTriple = "T";

        #endregion Bond Stuff

        #region Layout Constants

        // ****************************************************************
        // These should not really be here at all, but they are fairly safe
        // ****************************************************************

        // Font Size to use if all else fails
        public const double DefaultFontSize = 20.0d;

        // Calculate Font size as bond length * FontSizePercentageBond
        public const double FontSizePercentageBond = 0.5d;

        // Double Bond Offset as %age of bond length
        public const double BondOffsetPercentage = 0.1d;

        // How much to magnify CML by for rendering in Display or Editor
        public const double ScaleFactorForXaml = 2.0d;

        #endregion Layout Constants

        #region Clipboard Formats

        public const string FormatCML = "CML";
        public const string FormatSDFile = "SDFile";

        #endregion Clipboard Formats
    }
}