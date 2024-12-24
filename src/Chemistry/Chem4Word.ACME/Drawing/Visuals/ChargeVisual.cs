// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Drawing.Text;
using Chem4Word.ACME.Utils;
using Chem4Word.Core.Enums;
using System;
using System.Windows;
using System.Windows.Media;
using static Chem4Word.Model2.Helpers.Globals;

namespace Chem4Word.ACME.Drawing.Visuals
{
    public class ChargeVisual : ChildTextVisual
    {
        public ChargeVisual(AtomVisual parentVisual,
                            DrawingContext drawingContext, AtomTextMetrics mainAtomMetrics, AtomTextMetrics hMetrics)
        {
            DrawingContext = drawingContext;
            ParentVisual = parentVisual;
            ParentMetrics = mainAtomMetrics;
            HydrogenMetrics = hMetrics;
        }

        private DrawingContext DrawingContext { get; }

        public override void Render()
        {
            var chargeString = TextUtils.GetChargeString(ParentVisual.Charge);
            var chargeText = DrawChargeOrRadical(DrawingContext,
                                                 chargeString,
                                                 ParentVisual.Fill);
            chargeText.TextMetrics.FlattenedPath = chargeText.TextRun.GetOutline();
            Metrics = chargeText.TextMetrics;
        }

        /// <summary>
        /// Draws a charge or radical label at the given point
        /// </summary>
        /// <returns></returns>
        private ChargeLabelText DrawChargeOrRadical(DrawingContext drawingContext, string chargeString, Brush fill)
        {
            ChargeLabelText chargeText = new ChargeLabelText(chargeString, PixelsPerDip(), ParentVisual.SuperscriptSize);

            //center the charge text on the atom to start with
            Point chargeCenter = ParentVisual.Position;
            var parentBoundingBox = ParentMetrics.TotalBoundingBox;

            chargeText.MeasureAtCenter(chargeCenter);
            var chargeBoundingBox = chargeText.TextMetrics.TotalBoundingBox;

            SetMultipleCharges();

            chargeText.MeasureAtCenter(chargeCenter);

            chargeText.Fill = fill;
            chargeText.DrawAtBottomLeft(chargeText.TextMetrics.BoundingBox.BottomLeft, drawingContext);
            return chargeText;
            //local function
            //places multiple charges on an atom
            void SetMultipleCharges()
            {
                if (!(HydrogenMetrics is null))
                {
                    var hbb = HydrogenMetrics.TotalBoundingBox;
                    switch (ParentVisual.HydrogenOrientation)
                    {
                        //need to take into account width of subscripted hydrogen when placing charge
                        case CompassPoints.North:
                            chargeCenter.Y -= parentBoundingBox.Height / 2;
                            chargeCenter.X += chargeBoundingBox.Width + Math.Max(parentBoundingBox.Width, hbb.Width) / 2;
                            break;

                        case CompassPoints.South:
                        case CompassPoints.West:
                            chargeCenter.Y -= parentBoundingBox.Height / 2;
                            chargeCenter.X += (chargeBoundingBox.Width + parentBoundingBox.Width) / 2;
                            break;

                        //hydrogen is out of the way
                        default:
                            {
                                if (chargeString == EnDashSymbol)
                                {
                                    chargeCenter.Y -= (parentBoundingBox.Height + chargeBoundingBox.Width * 1.1) / 2;
                                }
                                else
                                {
                                    chargeCenter.Y -= (parentBoundingBox.Height + chargeBoundingBox.Height * 1.1) / 2;
                                }

                                chargeCenter.X += parentBoundingBox.Width / 2;
                                break;
                            }
                    }
                }
                else //no hydrogens!
                {
                    chargeCenter.Y -= parentBoundingBox.Height / 2;
                    chargeCenter.X = ParentVisual.Position.X + (chargeBoundingBox.Width + parentBoundingBox.Width) / 2;
                }
            }
        }
    }
}