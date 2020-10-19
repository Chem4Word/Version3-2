// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Chem4Word.ACME.Graphics
{
    /// <summary>
    /// ArcArrow draws an arrow of a specifed radius,
    /// centered on a point, that has a start and end angle
    /// Designed to be used directly from XAML
    /// </summary>
    internal class ArcArrow : ArrowBase
    {
        public Point Center
        {
            get { return (Point)GetValue(CenterProperty); }
            set { SetValue(CenterProperty, value); }
        }

        public static readonly DependencyProperty CenterProperty =
            DependencyProperty.Register("Center", typeof(Point), typeof(Arc),
                                        new FrameworkPropertyMetadata(new Point(0, 0), FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Measured relative to Compass East
        /// </summary>
        public double StartAngle
        {
            get { return (double)GetValue(StartAngleProperty); }
            set { SetValue(StartAngleProperty, value); }
        }

        public static readonly DependencyProperty StartAngleProperty =
            DependencyProperty.Register("StartAngle", typeof(double), typeof(Arc),
                                        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Measured relative to Compass East
        /// </summary>
        public double EndAngle
        {
            get { return (double)GetValue(EndAngleProperty); }
            set { SetValue(EndAngleProperty, value); }
        }

        public static readonly DependencyProperty EndAngleProperty =
            DependencyProperty.Register("EndAngle", typeof(double), typeof(Arc),
                                        new FrameworkPropertyMetadata(Math.PI / 2.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public double Radius
        {
            get { return (double)GetValue(RadiusProperty); }
            set { SetValue(RadiusProperty, value); }
        }

        public static readonly DependencyProperty RadiusProperty =
            DependencyProperty.Register("Radius", typeof(double), typeof(Arc),
                                        new FrameworkPropertyMetadata(10.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public bool SmallAngle
        {
            get { return (bool)GetValue(SmallAngleProperty); }
            set { SetValue(SmallAngleProperty, value); }
        }

        public static readonly DependencyProperty SmallAngleProperty =
            DependencyProperty.Register("SmallAngle", typeof(bool), typeof(Arc),
                                        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        protected override PathFigure ArrowLineFigure()
        {
            var startAngle = (StartAngle / 360) * 2 * Math.PI;
            var endAngle = (EndAngle / 360) * 2 * Math.PI;

            var a0 = startAngle < 0 ? startAngle + 2 * Math.PI : startAngle;
            var a1 = endAngle < 0 ? endAngle + 2 * Math.PI : endAngle;

            if (a1 < a0)
            {
                a1 += Math.PI * 2;
            }

            SweepDirection d = SweepDirection.Counterclockwise;
            bool large;

            if (SmallAngle)
            {
                large = false;
                double t = a1;
                if ((a1 - a0) > Math.PI)
                {
                    d = SweepDirection.Counterclockwise;
                }
                else
                {
                    d = SweepDirection.Clockwise;
                }
            }
            else
            {
                large = (Math.Abs(a1 - a0) < Math.PI);
            }
            Point p0 = Center + new Vector(Math.Cos(a0), Math.Sin(a0)) * Radius;
            Point p1 = Center + new Vector(Math.Cos(a1), Math.Sin(a1)) * Radius;

            List<PathSegment> segments = new List<PathSegment>(1);
            segments.Add(new ArcSegment(p1, new Size(Radius, Radius), 0.0, large, d, true));

            List<PathFigure> figures = new List<PathFigure>(1);
            PathFigure pf = new PathFigure(p0, segments, false);
            pf.IsClosed = false;
            return pf;
        }
    }
}