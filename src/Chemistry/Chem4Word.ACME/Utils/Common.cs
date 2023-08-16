// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2.Helpers;
using IChem4Word.Contracts;
using System.Windows.Media;

namespace Chem4Word.ACME.Utils
{
    public static class Common
    {
        public static IChem4WordTelemetry Telemetry { get; set; }

        public static Color HoverAdornerColor => (Color)ColorConverter.ConvertFromString(HoverAdornerColorDef);
        public static Color ThumbAdornerFillColour => (Color)ColorConverter.ConvertFromString(ThumbAdornerFillColorDef);
        public static Color Chem4WordColor => (Color)ColorConverter.ConvertFromString(Chem4WordColorDef);
        public static Color GroupBracketColor => (Color)ColorConverter.ConvertFromString(GroupBracketColorDef);

        public const string HoverAdornerColorDef = "#FFFF8C00";           //dark orange

        public const string ThumbAdornerFillColorDef = "#FFFFA500";          //orange

        public const string Chem4WordColorDef = "#2A579A";

        public const string GroupBracketColorDef = "#FF00BFFF";           //deep sky blue

        public const string AdornerBorderPen = "GrabHandlePen";

        //colour of ghosted molecules
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

        //'gravity' limit during aligned dragging
        public const double DraggingTolerance = 20d;

        public const double VectorTolerance = 0.01d;

        // LineThickness of Bond if all else fails
        public const double DefaultBondLineFactor = 1.0;

        //thickness of hover adorner in pixels
        public const double HoverAdornerThickness = 3.0;

        public const double AtomRadius = 5.0;

        // Percentage of Average bond length for any added Explicit Hydrogens
        public const double ExplicitHydrogenBondPercentage = 1.0;

        //thickness of interactively displayed bonds
        public const double BondThickness = Globals.ScaleFactorForXaml * 0.8;

        //thickness of molecule brackets
        public const double BracketThickness = BondThickness;

        //spacing between molecule and brackets as multiple of bond length
        public const double BracketFactor = 0.2;

        //distance between group adorner and molecule as fraction of bond length
        public const double GroupInflateFactor = BracketFactor / 2;
    }
}