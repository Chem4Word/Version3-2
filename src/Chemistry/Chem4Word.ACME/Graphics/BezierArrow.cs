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
    /// Draws a Bezier arrow with two control points
    /// </summary>
    public class BezierArrow : QuadraticArrow
    {
        /// <summary>
        /// Second control point for the Bezier of the shaft
        /// </summary>
        // The first control point is inherited from QuadraticArrow
        public Point SecondControlPoint
        {
            get { return (Point)GetValue(SecondControlPointProperty); }
            set { SetValue(SecondControlPointProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SecondControlPoint.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SecondControlPointProperty =
            DependencyProperty.Register("SecondControlPoint", typeof(Point),
                                        typeof(QuadraticArrow), new FrameworkPropertyMetadata(new Point(0, 0), FrameworkPropertyMetadataOptions.AffectsRender
                                            | FrameworkPropertyMetadataOptions.AffectsArrange
                                            | FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        ///Draws a cubic Bezier curve as the shaft (two control points)
        /// </summary>
        /// <returns></returns>
        protected override PathFigure Shaft()
        {
            List<PathSegment> segments = new List<PathSegment>
                                         {
                                             new BezierSegment(FirstControlPoint, SecondControlPoint, EndPoint, true)
                                         };

            PathFigure pf = new PathFigure(StartPoint, segments, false) { IsClosed = false };
            return pf;
        }
    }
}