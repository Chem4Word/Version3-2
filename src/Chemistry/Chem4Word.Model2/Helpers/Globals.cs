// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Windows.Media;

namespace Chem4Word.Model2.Helpers
{
    public static class Globals
    {
        public static PeriodicTable PeriodicTable = new PeriodicTable();
        public static List<FunctionalGroup> FunctionalGroupsList = FunctionalGroups.ShortcutList;

        #region Strings

        public static string CarbonSymbol = "C";
        public static string AllenicCarbonSymbol = "•";

        #endregion Strings

        #region Geometry Stuff

        public enum ClockDirections
        {
            Nothing = 0,
            I,
            II,
            III,
            IV,
            V,
            VI,
            VII,
            VIII,
            IX,
            X,
            XI,
            XII
        }

        #endregion Geometry Stuff

        #region Bond Stuff

        public enum BondDirection
        {
            Anticlockwise = -1,
            None = 0,
            Clockwise = 1
        }

        public enum BondStereo
        {
            None,
            Wedge,
            Hatch,
            Indeterminate,
            Cis,
            Trans
        }

        public const string OrderZero = "hbond";

        public const string OrderOther = "other";
        public const string OrderPartial01 = "partial01";
        public const string OrderSingle = "S";
        public const string OrderPartial12 = "partial12";
        public const string OrderAromatic = "A";
        public const string OrderDouble = "D";
        public const string OrderPartial23 = "partial23";
        public const string OrderTriple = "T";

        public static string GetStereoString(BondStereo stereo)
        {
            switch (stereo)
            {
                case BondStereo.None:
                    return null;

                case BondStereo.Hatch:
                    return "H";

                case BondStereo.Wedge:
                    return "W";

                case BondStereo.Cis:
                    return "C";

                case BondStereo.Trans:
                    return "T";

                case BondStereo.Indeterminate:
                    return "S";

                default:
                    return null;
            }
        }

        public static BondStereo StereoFromString(string stereo)
        {
            BondStereo result = BondStereo.None;

            switch (stereo)
            {
                case "N":
                    result = BondStereo.None;
                    break;

                case "W":
                    result = BondStereo.Wedge;
                    break;

                case "H":
                    result = BondStereo.Hatch;
                    break;

                case "S":
                    result = BondStereo.Indeterminate;
                    break;

                case "C":
                    result = BondStereo.Cis;
                    break;

                case "T":
                    result = BondStereo.Trans;
                    break;

                default:
                    result = BondStereo.None;
                    break;
            }

            return result;
        }

        public static string OrderValueToOrder(double val, bool isAromatic = false)
        {
            if (val == 0)
            {
                return OrderZero;
            }
            if (val == 0.5)
            {
                return OrderPartial01;
            }
            if (val == 1)
            {
                return OrderSingle;
            }
            if (val == 1.5)
            {
                if (isAromatic)
                {
                    return OrderAromatic;
                }
                else
                {
                    return OrderPartial12;
                }
            }
            if (val == 2)
            {
                return OrderDouble;
            }
            if (val == 2.5)
            {
                return OrderPartial23;
            }
            if (val == 3)
            {
                return OrderTriple;
            }
            if (val == 4)
            {
                return OrderAromatic;
            }
            return OrderZero;
        }

        public static double? OrderToOrderValue(string order)
        {
            switch (order)
            {
                case OrderZero:
                case OrderOther:
                    return 0;

                case OrderPartial01:
                    return 0.5;

                case OrderSingle:
                    return 1;

                case OrderPartial12:
                    return 1.5;

                case OrderAromatic:
                    return 1.5;

                case OrderDouble:
                    return 2;

                case OrderPartial23:
                    return 2.5;

                case OrderTriple:
                    return 3;

                default:
                    return null;
            }
        }

        #endregion Bond Stuff

        #region Layout Constants

        public const double VectorTolerance = 0.01d;

        // LineThickness of Bond if all else fails
        public const double DefaultBondLineFactor = 1.0;

        // Font Size to use if all else fails
        public const double DefaultFontSize = 20.0d;

        // Calculate Font size as bond length * FontSizePercentageBond
        public const double FontSizePercentageBond = 0.5d;

        // Double Bond Offset as %age of bond length
        public const double BondOffsetPercentage = 0.1d;

        // How much to magnify CML by for rendering in Display or Editor
        public const double ScaleFactorForXaml = 2.0d;

        // Percentage of Average bond length for any added Explicit Hydrogens
        public const double ExplicitHydrogenBondPercentage = 1.0;

        //thickness of interactively displayed bonds
        public const double BondThickness = ScaleFactorForXaml * 0.8;

        //thickness of hover adorner in pixels
        public const double HoverAdornerThickness = 3.0;

        public const string HoverAdornerColorDef = "#FFFF8C00"; //dark orange
        public static Color HoverAdornerColor => (Color)ColorConverter.ConvertFromString(HoverAdornerColorDef);

        public const string ThumbAdornerFillColorDef = "#FFFFA500"; //orange
        public static Color ThumbAdornerFillColour => (Color)ColorConverter.ConvertFromString(ThumbAdornerFillColorDef);

        public const string Chem4WordColorDef = "#2A579A";
        public static Color Chem4WordColor => (Color)ColorConverter.ConvertFromString(Chem4WordColorDef);

        public const string GroupBracketColorDef = "#FF00BFFF"; //deep sky blue
        public static Color GroupBracketColor => (Color)ColorConverter.ConvertFromString(GroupBracketColorDef);
        public const double AtomRadius = 5.0;

        //thickness of molecule brackets
        public const double BracketThickness = BondThickness;

        //spacing between molecule and brackets as multiple of bond length
        public const double BracketFactor = 0.2;

        //distance between group adorner and molecule as fraction of bond length
        public const double GroupInflateFactor = BracketFactor / 2;

        public const string AdornerBorderPen = "GrabHandlePen";

        //color of ghosted molecules
        public const string GhostBrush = "GrabHandleBorderBrush";

        //style keys - all of these are defined in ACMEResources.XAML
        public const string GroupHandleStyle = "GroupHandleStyle";

        public const string GrabHandleStyle = "GrabHandleStyle";

        public const string ThumbStyle = "BigThumbStyle";

        public const string DrawAdornerBrush = "DrawBondBrush";

        public const string RotateThumbStyle = "RotateThumb";

        public const string AdornerBorderBrush = "GrabHandleBorderBrush";

        public const string AdornerFillBrush = "ThumbFillBrush";

        public const string AtomBondSelectorBrush = "AtomBondSelectorBrush";

        public const string BlockedAdornerBrush = "BlockedAdornerBrush";

        #endregion Layout Constants

        #region Clipboard Formats

        public const string FormatCML = "CML";
        public const string FormatSDFile = "SDFile";

        #endregion Clipboard Formats
    }
}