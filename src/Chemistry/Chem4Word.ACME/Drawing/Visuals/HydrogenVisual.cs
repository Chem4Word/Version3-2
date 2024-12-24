// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Drawing.Text;
using Chem4Word.Model2;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Chem4Word.ACME.Drawing.Visuals
{
    public class HydrogenVisual : ChildTextVisual
    {
        /// <summary>
        /// Draws a subscripted hydrogen visual separately from the main atom visual
        /// </summary>
        public HydrogenVisual(AtomVisual parent, AtomTextMetrics mainAtomMetrics, int implicitHydrogenCount, double symbolSize, DrawingContext dc)
        {
            ParentVisual = parent;
            ImplicitHydrogenCount = implicitHydrogenCount;
            SymbolSize = symbolSize;
            Context = dc;
            ParentMetrics = mainAtomMetrics;
            Fill = ParentVisual.Fill;
        }

        public List<Point> FlattenedPath => Metrics.FlattenedPath;

        public override Geometry HullGeometry
        {
            get
            {
                if (Hull != null && Hull.Count != 0)
                {
                    Geometry geo1 = Utils.Geometry.BuildPolyPath(Hull);
                    CombinedGeometry cg = new CombinedGeometry(geo1,
                                                               geo1.GetWidenedPathGeometry(new Pen(Brushes.Black, Standoff)));
                    return cg;
                }
                return Geometry.Empty;
            }
        }

        public override Rect Bounds
        {
            get
            {
                return Metrics.BoundingBox;
            }
        }

        public override void Render()
        {
            var subscriptedGroup = new SubscriptedGroup(ImplicitHydrogenCount, "H", SymbolSize);
            Metrics = subscriptedGroup.Measure(ParentMetrics, ParentVisual.HydrogenOrientation, ParentVisual.PixelsPerDip());
            subscriptedGroup.DrawSelf(Context, Metrics, ParentVisual.PixelsPerDip(), Fill);

            CoreHull = FlattenedPath;
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            if (ParentAtom.Element is Element)
            {
                if (HullGeometry.FillContains(hitTestParameters.HitPoint))
                {
                    return new PointHitTestResult(this, hitTestParameters.HitPoint);
                }
            }

            return null;
        }
    }
}