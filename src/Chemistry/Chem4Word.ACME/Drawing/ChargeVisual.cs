// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2.Geometry;
using Chem4Word.Model2.Helpers;
using System;
using System.Windows;
using System.Windows.Media;

namespace Chem4Word.ACME.Drawing
{
    public class ChargeVisual : ChildTextVisual
    {
        public ChargeVisual(DrawingContext drawingContext, AtomTextMetrics mainAtomMetrics, AtomVisual parentVisual,
                            CompassPoints compassPoints)
        {
            DrawingContext = drawingContext;
            ParentVisual = parentVisual;
            ParentMetrics = mainAtomMetrics;
        }

        private DrawingContext DrawingContext { get; }

        public override void Render()
        {
            var chargeString = AtomHelpers.GetChargeString(ParentVisual.Charge);
            var chargeText = DrawChargeOrRadical(DrawingContext,
                                                 ParentMetrics,
                                                 ParentVisual.HydrogenChildVisual?.Metrics,
                                                 ParentVisual.IsotopeChildVisual?.Metrics,
                                                 chargeString,
                                                 ParentVisual.Fill,
                                                 ParentVisual.ParentAtom.ImplicitHPlacement);
            chargeText.TextMetrics.FlattenedPath = chargeText.TextRun.GetOutline();
            Metrics = chargeText.TextMetrics;
        }

        /// <summary>
        /// Draws a charge or radical label at the given point
        /// </summary>
        /// <returns></returns>
        private ChargeLabelText DrawChargeOrRadical(DrawingContext drawingContext, AtomTextMetrics mainAtomMetrics, AtomTextMetrics hMetrics, LabelMetrics isoMetrics, string chargeString, Brush fill, CompassPoints defaultHOrientation)
        {
            ChargeLabelText chargeText = new ChargeLabelText(chargeString, PixelsPerDip(), ParentVisual.SuperscriptSize);

            //try to place the charge at 2 o clock to the atom
            Vector chargeOffset = BasicGeometry.ScreenNorth * ParentVisual.SymbolSize * 0.9;
            RotateUntilClear(mainAtomMetrics, hMetrics, isoMetrics, chargeOffset, chargeText, out var chargeCenter, defaultHOrientation);
            chargeText.MeasureAtCenter(chargeCenter);
            chargeText.Fill = fill;
            chargeText.DrawAtBottomLeft(chargeText.TextMetrics.BoundingBox.BottomLeft, drawingContext);
            return chargeText;
        }

        private static void RotateUntilClear(AtomTextMetrics mainAtomMetrics, AtomTextMetrics hMetrics, LabelMetrics isoMetrics,
                                             Vector labelOffset, GlyphText labelText, out Point labelCenter, CompassPoints defHOrientation)
        {
            Matrix rotator = new Matrix();
            double angle = Globals.ClockDirections.II.ToDegrees();
            rotator.Rotate(angle);

            labelOffset = labelOffset * rotator;
            Rect bb = new Rect();
            Rect bb2 = new Rect();
            if (hMetrics != null)
            {
                bb = hMetrics.TotalBoundingBox;
            }
            if (isoMetrics != null)
            {
                bb2 = isoMetrics.BoundingBox;
            }
            labelCenter = mainAtomMetrics.Geocenter + labelOffset;
            labelText.MeasureAtCenter(labelCenter);

            double increment;
            if (defHOrientation == CompassPoints.East)
            {
                increment = -10;
            }
            else
            {
                increment = 10;
            }
            while (labelText.CollidesWith(mainAtomMetrics.TotalBoundingBox, bb, bb2)
                   && Math.Abs(angle - 30) > 0.001)
            {
                rotator = new Matrix();

                angle += increment;
                rotator.Rotate(increment);
                labelOffset = labelOffset * rotator;
                labelCenter = mainAtomMetrics.Geocenter + labelOffset;
                labelText.MeasureAtCenter(labelCenter);
            }
        }
    }
}