// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using DocumentFormat.OpenXml;

namespace Chem4Word.Renderer.OoXmlV4.OOXML
{
    public static class OoXmlHelper
    {
        // https://startbigthinksmall.wordpress.com/2010/02/05/unit-converter-and-specification-search-for-ooxmlwordml-development/
        // http://lcorneliussen.de/raw/dashboards/ooxml/

        // Fixed values
        public const int EmusPerWordPoint = 12700;

        public const string Black = "000000";

        // This character is used to replace any which have not been extracted to Arial.json
        public const char DefaultCharacter = '⊠'; // https://www.compart.com/en/unicode/U+22A0

        // Margins are in CML Points
        public const double DrawingMargin = 5; // 5 is a good value to use (Use 0 to compare with AMC diagrams)

        public const double CmlCharacterMargin = 1.25; // margin in cml pixels

        public const double SubscriptScaleFactor = 0.6;
        public const double SubscriptDropFactor = 0.75;
        public const double CsSuperscriptRaiseFactor = 0.3;

        private const double BracketOffsetPercentage = 0.2;

        // Percentage of average (median) bond length
        // V3 == 0.2 -> ACS == 0.18
        public const double MultipleBondOffsetPercentage = 0.18;

        public const double LineShrinkPixels = 1.75; // cml pixels

        // V3 == 0.75 -> ACS == 0.6
        // This makes bond line width equal to ACS Guide of 0.6pt
        public const double AcsLineWidth = 0.6;

        public const double AcsLineWidthEmus = AcsLineWidth * EmusPerWordPoint;

        // V3 == 9500 -> V3.1 [ACS] == 9144
        // This makes cml bond length of 20 equal ACS guide 0.2" (0.508cm)
        private const double EmusPerCmlPoint = 9144;

        /// <summary>
        /// Scales a CML X or Y co-ordinate to DrawingML Units (EMU)
        /// </summary>
        /// <param name="XorY"></param>
        /// <returns></returns>
        public static Int64Value ScaleCmlToEmu(double XorY)
        {
            var scaled = XorY * EmusPerCmlPoint;
            return Int64Value.FromInt64((long)scaled);
        }

        public static double BracketOffset(double bondLength)
        {
            return bondLength * BracketOffsetPercentage;
        }

        #region C# TTF

        /// <summary>
        /// Scales a C# TTF X or Y co-ordinate to DrawingML Units (EMU)
        /// </summary>
        /// <param name="XorY"></param>
        /// <param name="bondLength"></param>
        /// <returns></returns>
        public static Int64Value ScaleCsTtfToEmu(double XorY, double bondLength)
        {
            if (bondLength > 0.1)
            {
                var scaled = XorY * EmusPerCsTtfPoint(bondLength);
                return Int64Value.FromInt64((long)scaled);
            }
            else
            {
                var scaled = XorY * EmusPerCsTtfPoint(20);
                return Int64Value.FromInt64((long)scaled);
            }
        }

        /// <summary>
        /// Scales a CS TTF SubScript X or Y co-ordinate to DrawingML Units (EMU)
        /// <param name="XorY"></param>
        /// <param name="bondLength"></param>
        /// </summary>
        public static Int64Value ScaleCsTtfSubScriptToEmu(double XorY, double bondLength)
        {
            if (bondLength > 0.1)
            {
                var scaled = XorY * EmusPerCsTtfPointSubscript(bondLength);
                return Int64Value.FromInt64((long)scaled);
            }
            else
            {
                var scaled = XorY * EmusPerCsTtfPointSubscript(20);
                return Int64Value.FromInt64((long)scaled);
            }
        }

        /// <summary>
        /// Scales a C# TTF X or Y co-ordinate to CML Units
        /// </summary>
        /// <param name="XorY"></param>
        /// <param name="bondLength"></param>
        /// <returns></returns>
        public static double ScaleCsTtfToCml(double XorY, double bondLength)
        {
            if (bondLength > 0.1)
            {
                return XorY / CsTtfToCml(bondLength);
            }
            else
            {
                return XorY / CsTtfToCml(20);
            }
        }

        // These calculations yield a font which has a point size of 8 at a bond length of 20
        public static double EmusPerCsTtfPoint(double bondLength)
        {
            return bondLength / 2.5;
        }

        private static double EmusPerCsTtfPointSubscript(double bondLength)
        {
            if (bondLength > 0.1)
            {
                return EmusPerCsTtfPoint(bondLength) * SubscriptScaleFactor;
            }
            else
            {
                return EmusPerCsTtfPoint(20) * SubscriptScaleFactor;
            }
        }

        private static double CsTtfToCml(double bondLength)
        {
            if (bondLength > 0.1)
            {
                return EmusPerCmlPoint / EmusPerCsTtfPoint(bondLength);
            }
            else
            {
                return EmusPerCmlPoint / EmusPerCsTtfPoint(20);
            }
        }

        #endregion C# TTF
    }
}