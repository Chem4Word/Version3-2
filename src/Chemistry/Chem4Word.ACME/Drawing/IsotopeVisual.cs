// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using Chem4Word.Model2.Geometry;

namespace Chem4Word.ACME.Drawing
{
    public class IsotopeVisual : ChildTextVisual
    {
        public IsotopeVisual(AtomVisual parent, DrawingContext drawingContext, AtomTextMetrics mainAtomMetrics,
                             AtomTextMetrics hMetrics)
        {
            ParentVisual = parent;
            Context = drawingContext;
            ParentMetrics = mainAtomMetrics;
            HydrogenMetrics = hMetrics;
        }

        public sealed override void Render()
        {
            Debug.Assert(ParentVisual.Isotope != null);

            string isoLabel = ParentVisual.Isotope.ToString();
            var isotopeText = new IsotopeLabelText(isoLabel, PixelsPerDip(), ParentVisual.SuperscriptSize);

            Vector isotopeOffsetVector = BasicGeometry.ScreenNorth * ParentVisual.SymbolSize;
            Matrix rotator = new Matrix();
            double angle = -60;
            //avoid overlap of label and hydrogens
            if (HydrogenMetrics != null && ParentVisual.ParentAtom.ImplicitHPlacement == CompassPoints.West)
            {
                angle = -35;
            }

            rotator.Rotate(angle);
            isotopeOffsetVector = isotopeOffsetVector * rotator;
            Point isoCenter = ParentVisual.Position + isotopeOffsetVector;
            isotopeText.MeasureAtCenter(isoCenter);
            isotopeText.Fill = ParentVisual.Fill;
            isotopeText.DrawAtBottomLeft(isotopeText.TextMetrics.BoundingBox.BottomLeft, Context);
            Metrics = isotopeText.TextMetrics;
        }
    }
}