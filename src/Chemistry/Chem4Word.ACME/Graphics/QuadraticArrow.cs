// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Chem4Word.ACME.Graphics
{
    /// <summary>
    /// Draws a Bezier arrow with one control point
    /// </summary>
    public class QuadraticArrow : StraightArrow
    {
        /// <summary>
        /// First control point for the Bezier of the shaft
        /// </summary>
        //StartPoint and EndPoint are inherited from StraightArrow
        public Point FirstControlPoint
        {
            get { return (Point)GetValue(FirstControlPointProperty); }
            set { SetValue(FirstControlPointProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ControlPoint1Point.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FirstControlPointProperty =
            DependencyProperty.Register("FirstControlPoint", typeof(Point),
                typeof(QuadraticArrow), new FrameworkPropertyMetadata(new Point(0, 0), FrameworkPropertyMetadataOptions.AffectsRender
        | FrameworkPropertyMetadataOptions.AffectsArrange
        | FrameworkPropertyMetadataOptions.AffectsMeasure));

        protected override PathFigure Shaft()
        {
            List<PathSegment> segments = new List<PathSegment>
                                         {
                                             new QuadraticBezierSegment(FirstControlPoint, EndPoint, true)
                                         };

            PathFigure pf = new PathFigure(StartPoint, segments, false) { IsClosed = false };
            return pf;
        }
    }
}