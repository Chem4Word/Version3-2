// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Media;

namespace Chem4Word.ACME.Graphics
{
    /// <summary>
    /// Draws a simple arrow shape
    /// </summary>
    public class StraightArrow : Arrow
    {
        /// <summary>
        /// Where the arrow starts
        /// </summary>
        public Point StartPoint
        {
            get { return (Point)GetValue(StartPointProperty); }
            set { SetValue(StartPointProperty, value); }
        }

        public static readonly DependencyProperty StartPointProperty =
            DependencyProperty.Register("StartPoint", typeof(Point), typeof(Arrow),
                                        new FrameworkPropertyMetadata(new Point(0, 0), FrameworkPropertyMetadataOptions.AffectsRender
                                                                      | FrameworkPropertyMetadataOptions.AffectsArrange
                                                                      | FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// Where the arrow ends
        /// </summary>
        public Point EndPoint
        {
            get { return (Point)GetValue(EndPointProperty); }
            set { SetValue(EndPointProperty, value); }
        }

        public static readonly DependencyProperty EndPointProperty =
            DependencyProperty.Register("EndPoint", typeof(Point), typeof(Arrow),
                                        new FrameworkPropertyMetadata(new Point(0, 0), FrameworkPropertyMetadataOptions.AffectsRender
                                                                      | FrameworkPropertyMetadataOptions.AffectsArrange
                                                                      | FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// Draws the shaft.  Override to draw a non-straight line
        /// </summary>
        /// <returns></returns>
        protected override PathFigure Shaft()
        {
            PathFigure mainline = new PathFigure { StartPoint = StartPoint };
            LineSegment ls = new LineSegment(EndPoint, true);
            mainline.Segments.Add(ls);
            return mainline;
        }
    }
}