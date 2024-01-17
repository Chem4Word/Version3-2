// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Drawing.Text;
using Chem4Word.Core.Enums;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace Chem4Word.ACME.Drawing.Visuals
{
    public class IsotopeVisual : ChildTextVisual
    {
        public IsotopeVisual(AtomVisual parent,
                             DrawingContext drawingContext,
                             AtomTextMetrics mainAtomMetrics,
                             AtomTextMetrics hMetrics)
        {
            ParentVisual = parent;
            Context = drawingContext;
            ParentMetrics = mainAtomMetrics;
            HydrogenMetrics = hMetrics;
        }

        public override sealed void Render()
        {
            Debug.Assert(ParentVisual.Isotope != null);

            string isoLabel = ParentVisual.Isotope.ToString();
            var isotopeText = new IsotopeLabelText(isoLabel, PixelsPerDip(), ParentVisual.SuperscriptSize);

            //first position the isotope label at  the center
            Point isoCenter = ParentVisual.Position;
            var parentBoundingBox = ParentMetrics.TotalBoundingBox;
            isotopeText.MeasureAtCenter(isoCenter);
            //now do some adjustments
            //if we have no hydrogens, then adjust
            var ibb = isotopeText.TextMetrics.TotalBoundingBox;

            if (HydrogenMetrics is null)
            {
                isoCenter.Y -= parentBoundingBox.Height / 2;
                isoCenter.X -= (ibb.Width + parentBoundingBox.Width) / 2;
            }
            else //we do have hydrogens
            {
                var hbb = HydrogenMetrics.TotalBoundingBox;

                switch (ParentVisual.HydrogenOrientation)
                {
                    case CompassPoints.North:
                        isoCenter.Y -= parentBoundingBox.Height / 2;
                        isoCenter.X -= (ibb.Width + Math.Max(parentBoundingBox.Width, hbb.Width)) / 2;
                        break;

                    case CompassPoints.West:
                        isoCenter.Y -= (parentBoundingBox.Height + ibb.Height) / 2;
                        isoCenter.X -= parentBoundingBox.Width / 2;
                        break;

                    default:
                        isoCenter.Y -= parentBoundingBox.Height / 2;
                        isoCenter.X -= (ibb.Width + parentBoundingBox.Width) / 2;
                        break;
                }
            }
            //measure again with adjustments
            isotopeText.MeasureAtCenter(isoCenter);

            isotopeText.Fill = ParentVisual.Fill;
            isotopeText.DrawAtBottomLeft(isotopeText.TextMetrics.BoundingBox.BottomLeft, Context);
            Metrics = isotopeText.TextMetrics;
        }
    }
}