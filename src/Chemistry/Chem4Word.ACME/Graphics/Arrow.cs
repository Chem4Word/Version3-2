// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Enums;
using Chem4Word.Core.Helpers;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Chem4Word.ACME.Graphics
{
    /// <summary>
    /// Abstract class from which all arrows derive.
    /// Override Shaft to draw a straight, arced or splined arrow
    /// </summary>

    public abstract class Arrow : Shape
    {
        #region "Constructors"

        protected Arrow()
        {
            ClipToBounds = false;
        }

        #endregion "Constructors"

        #region "Properties"

        #region "Dependency Properties"

        /// <summary>
        /// Determines whether the arrow head is closed
        /// </summary>
        public bool ArrowHeadClosed
        {
            get { return (bool)GetValue(ArrowHeadClosedProperty); }
            set { SetValue(ArrowHeadClosedProperty, value); }
        }

        public static readonly DependencyProperty ArrowHeadClosedProperty =
            DependencyProperty.Register("ArrowHeadClosed", typeof(bool), typeof(Arrow),
                                        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender
                                                                      | FrameworkPropertyMetadataOptions.AffectsArrange
                                                                      | FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        ///     Identifies the ArrowEnds dependency property.
        /// </summary>
        public static readonly DependencyProperty ArrowEndsProperty =
            DependencyProperty.Register("ArrowEnds",
                typeof(ArrowEnds), typeof(Arrow),
                new FrameworkPropertyMetadata(ArrowEnds.End,
                                              FrameworkPropertyMetadataOptions.AffectsRender
                                              | FrameworkPropertyMetadataOptions.AffectsArrange
                                              | FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        ///     Gets or sets the property that determines which ends of the
        ///     line have arrows.
        /// </summary>
        public ArrowEnds ArrowEnds
        {
            set { SetValue(ArrowEndsProperty, value); }
            get { return (ArrowEnds)GetValue(ArrowEndsProperty); }
        }

        /// <summary>
        /// length of the 'barbs' of the arrow
        /// </summary>
        public double HeadLength
        {
            get { return (double)GetValue(HeadLengthProperty); }
            set { SetValue(HeadLengthProperty, value); }
        }

        public static readonly DependencyProperty HeadLengthProperty =
            DependencyProperty.Register("HeadLength", typeof(double), typeof(Arrow),
                                        new FrameworkPropertyMetadata(12.0,
                                                                      FrameworkPropertyMetadataOptions.AffectsRender
                                                                      | FrameworkPropertyMetadataOptions.AffectsArrange
                                                                      | FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// Angle between the barbs and the shaft
        /// </summary>
        public double HeadAngle
        {
            get { return (double)GetValue(HeadAngleProperty); }
            set { SetValue(HeadAngleProperty, value); }
        }

        public static readonly DependencyProperty HeadAngleProperty =
            DependencyProperty.Register("HeadAngle", typeof(double), typeof(Arrow),
                                        new FrameworkPropertyMetadata(30d,
                                                                      FrameworkPropertyMetadataOptions.AffectsRender
                                                                      | FrameworkPropertyMetadataOptions.AffectsArrange
                                                                      | FrameworkPropertyMetadataOptions.AffectsMeasure));

        #endregion "Dependency Properties"

        /// <summary>
        /// Standard override for Shapes
        /// </summary>
        protected override System.Windows.Media.Geometry DefiningGeometry
        {
            //all rendering handled in OnRender
            get { return Geometry.Empty; }
        }

        /// <summary>
        /// Returns the geometry of the main line.
        /// Override this if you want to draw a curvy arrow say
        /// </summary>
        /// <returns>PathFigure describing the line of the arrow</returns>
        protected abstract PathFigure Shaft();

        #endregion "Properties"

        /// <summary>
        /// Returns the head of an arrow
        /// </summary>
        /// <param name="line">PathFigure describing the body of the arrow</param>
        /// <param name="reverse">Draw arrow head at start instead of end of arrow</param>
        /// <returns>Simple path figure of arrow head, oriented appropriately </returns>
        public virtual PathFigure ArrowHeadGeometry(PathFigure line, bool reverse = false)
        {
            var headAngleInRadians = HeadAngle / 360 * 2 * Math.PI;

            //work out how far back the arrowhead extends
            double offset = HeadLength * Math.Cos(headAngleInRadians);

            var length = GetPathFigureLength(line);

            double fraction;  //if we're going for the start or end of line
            if (reverse)
            {
                fraction = offset / length;
            }
            else
            {
                fraction = (length - offset) / length;
            }

            //create a simple geometry so we can use a wpf trick to determine the length
            var tempPG = new PathGeometry();
            tempPG.Figures.Add(line);

            Point endPoint;

            //this is a really cool method to get the angle at the end of a line of any shape.
            //we need to get the actual angle at the very point the arrow line enters the head
            tempPG.GetPointAtFractionLength(fraction, out Point intersection, out Point tangent);
            //get the ends
            if (reverse)
            {
                tempPG.GetPointAtFractionLength(0.0, out endPoint, out _);
            }
            else
            {
                tempPG.GetPointAtFractionLength(1.0, out endPoint, out _);
            }

            //get the perpendicular to the tangent and offset appropriately
            Vector perp = new Vector(tangent.X, tangent.Y).Perpendicular();
            perp.Normalize();
            perp *= Math.Sin(headAngleInRadians);
            perp *= HeadLength;

            PolyLineSegment polyseg = new PolyLineSegment();

            var pointa = intersection + perp;

            polyseg.Points.Add(endPoint);

            var pointb = intersection - perp;
            polyseg.Points.Add(pointb);

            PathSegmentCollection psc = new PathSegmentCollection { polyseg };

            PathFigure pathfig = new PathFigure(pointa, psc, ArrowHeadClosed);

            return pathfig;
        }

        /// <summary>
        /// used to return the length of the Shaft
        /// </summary>
        /// <param name="line">PathFigure describing the arrow shaft</param>
        /// <returns></returns>
        protected static double GetPathFigureLength(PathFigure line)
        {
            var pathbits = line.GetFlattenedPathFigure();

            double length = 0.0;
            var lastPoint = line.StartPoint;

            foreach (PathSegment pathSegment in pathbits.Segments)
            {
                if (pathSegment is LineSegment ls)
                {
                    length += (ls.Point - lastPoint).Length;
                    lastPoint = ls.Point;
                }
                else if (pathSegment is PolyLineSegment pls)
                {
                    foreach (Point plsPoint in pls.Points)
                    {
                        length += (plsPoint - lastPoint).Length;
                        lastPoint = plsPoint;
                    }
                }
            }
            return length;
        }

        /// <summary>
        /// Draw the arrow
        /// </summary>
        /// <param name="drawingContext">DrawingContext provided by WPF</param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            DrawArrowGeometry(drawingContext, new Pen(Stroke, StrokeThickness), Fill);
        }

        /// <summary>
        /// Draws the arrow to a drawing context (possibly external)
        /// </summary>
        /// <param name="drawingContext">DrawingContext provided by Windows or the calling code</param>
        /// <param name="outlinePen">Traces the arrow outline</param>
        /// <param name="headFillBrush">what the head is filled in with</param>
        public virtual void DrawArrowGeometry(DrawingContext drawingContext, Pen outlinePen, Brush headFillBrush)
        {
            Brush overlayBrush;
            GetOverlayPen(out overlayBrush, out Pen overlayPen);

            base.OnRender(drawingContext);
            var mainLine = Shaft();
            PathFigureCollection pfc1 = new PathFigureCollection() { mainLine };
            outlinePen.StartLineCap = PenLineCap.Round;
            outlinePen.EndLineCap = PenLineCap.Round;
            PathGeometry lineGeometry = new PathGeometry(pfc1);
            drawingContext.DrawGeometry(null, outlinePen, lineGeometry);

            var overlay = lineGeometry.GetWidenedPathGeometry(overlayPen);

            drawingContext.DrawGeometry(overlayBrush, null, overlay);
            PathFigureCollection pfc2 = new PathFigureCollection();

            // Draw the arrow at the start of the line.
            if ((ArrowEnds & ArrowEnds.Start) == ArrowEnds.Start)
            {
                PathFigure value = ArrowHeadGeometry(mainLine, true);
                pfc2.Add(value);
            }

            // Draw the arrow at the end of the line.
            if ((ArrowEnds & ArrowEnds.End) == ArrowEnds.End)
            {
                pfc2.Add(ArrowHeadGeometry(mainLine));
            }

            PathGeometry geometry = new PathGeometry(pfc2);
            drawingContext.DrawGeometry(headFillBrush, outlinePen, geometry);
            overlay = geometry.GetWidenedPathGeometry(overlayPen);

            drawingContext.DrawGeometry(overlayBrush, overlayPen, overlay);
        }

        public void GetOverlayPen(out Brush overlayBrush, out Pen pen)
        {
#if SHOWBOUNDS
            overlayBrush = new SolidColorBrush(Colors.LightGreen) { Opacity = 0.4 };
#else
            overlayBrush = Brushes.Transparent;
#endif
            double overlayWidth = 2 * HeadLength * Math.Sin(HeadAngle / 360 * 2 * Math.PI);
            pen = new Pen(overlayBrush, overlayWidth);
            pen.StartLineCap = PenLineCap.Round;
            pen.EndLineCap = PenLineCap.Round;
        }
    }
}