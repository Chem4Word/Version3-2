// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Chem4Word.ACME.Graphics
{
    /// <summary>
    /// ArcArrow draws an arrow of a specified radius,
    /// centered on a point, that has a start and end angle
    /// Designed to be used directly from XAML
    /// </summary>
    public class ArcArrow : Arrow
    {
        /// <summary>
        /// Point describing the center of the arc
        /// </summary>
        public Point Center
        {
            get { return (Point)GetValue(CenterProperty); }
            set { SetValue(CenterProperty, value); }
        }

        public static readonly DependencyProperty CenterProperty =
            DependencyProperty.Register("Center", typeof(Point), typeof(ArcArrow),
                                        new FrameworkPropertyMetadata(new Point(0, 0), FrameworkPropertyMetadataOptions.AffectsRender
                                                                                                        | FrameworkPropertyMetadataOptions.AffectsArrange
                                                                                                        | FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// Measured relative to Compass North
        /// </summary>
        public double StartAngle
        {
            get { return (double)GetValue(StartAngleProperty); }
            set { SetValue(StartAngleProperty, value); }
        }

        public static readonly DependencyProperty StartAngleProperty =
            DependencyProperty.Register("StartAngle", typeof(double), typeof(ArcArrow),
                                        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender
                                                                           | FrameworkPropertyMetadataOptions.AffectsArrange
                                                                           | FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// Measured relative to Compass North
        /// </summary>
        public double EndAngle
        {
            get { return (double)GetValue(EndAngleProperty); }
            set { SetValue(EndAngleProperty, value); }
        }

        public static readonly DependencyProperty EndAngleProperty =
            DependencyProperty.Register("EndAngle", typeof(double), typeof(ArcArrow),
                                        new FrameworkPropertyMetadata(Math.PI / 2.0, FrameworkPropertyMetadataOptions.AffectsRender
                                                                      | FrameworkPropertyMetadataOptions.AffectsArrange
                                                                      | FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// Radius of the arc
        /// </summary>
        public double Radius
        {
            get { return (double)GetValue(RadiusProperty); }
            set { SetValue(RadiusProperty, value); }
        }

        public static readonly DependencyProperty RadiusProperty =
            DependencyProperty.Register("Radius", typeof(double), typeof(ArcArrow),
                                        new FrameworkPropertyMetadata(10.0, FrameworkPropertyMetadataOptions.AffectsRender
                                                                            | FrameworkPropertyMetadataOptions.AffectsArrange
                                                                            | FrameworkPropertyMetadataOptions.AffectsMeasure));

        protected override PathFigure Shaft()
        {
            var startVector = GeometryTool.ScreenNorth * Radius;
            var endVector = startVector;

            Matrix startRotator = new Matrix();
            Matrix endRotator = new Matrix();
            var startAngle = CorrectedAngle(StartAngle);
            var endAngle = CorrectedAngle(EndAngle);

            startRotator.Rotate(startAngle);
            endRotator.Rotate(endAngle);

            startVector = startVector * startRotator;
            endVector = endVector * endRotator;

            bool large = Math.Abs(endAngle - startAngle) > 180;

            Point startPoint = Center + startVector;
            Point endPoint = Center + endVector;

            var sweep = startAngle < endAngle ? SweepDirection.Clockwise : SweepDirection.Counterclockwise;

            List<PathSegment> segments = new List<PathSegment>
            {
                new ArcSegment(endPoint, new Size(Radius, Radius), 0.0, large, sweep, true)
            };

            PathFigure pf = new PathFigure(startPoint, segments, false) { IsClosed = false };
            return pf;
        }

        private double CorrectedAngle(double value)
        {
            double result = value;
            while (result > 360)
            {
                result -= 360;
            }

            while (result < -360)
            {
                result += 360;
            }
            return result;
        }
    }
}