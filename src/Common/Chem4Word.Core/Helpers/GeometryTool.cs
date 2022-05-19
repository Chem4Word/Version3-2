// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Chem4Word.Core.Enums;

namespace Chem4Word.Core.Helpers
{
    public static class GeometryTool
    {
        public static Vector Perpendicular(this Vector v) => new Vector(-v.Y, v.X);

        /// See https://www.topcoder.com/community/data-science/data-science-tutorials/geometry-concepts-line-intersection-and-its-applications/
        /// for implementing some good basic operations in geometry

        // modified from http://csharphelper.com/blog/2014/07/find-the-convex-hull-of-a-set-of-points-in-c/

        // Return the points that make up a polygon's convex hull.
        // This method leaves the points list unchanged.
        public static List<Point> MakeConvexHull(List<Point> points)
        {
            // Cull.
            points = HullCull(points);

            // Find the remaining point with the smallest Y value.
            // if (there's a tie, take the one with the smaller X value.
            var bestPt = points[0];
            foreach (var pt in points)
            {
                if (pt.Y < bestPt.Y
                    || pt.Y == bestPt.Y && pt.X < bestPt.X)
                {
                    bestPt = pt;
                }
            }

            // Move this point to the convex hull.
            var hull = new List<Point>
                       {
                                      bestPt
                                  };
            points.Remove(bestPt);

            // Start wrapping up the other points.
            double sweepAngle = 0;
            while (true)
            {
                // Find the point with smallest AngleValue
                // from the last point.
                var x1 = hull[hull.Count - 1].X;
                var y1 = hull[hull.Count - 1].Y;
                bestPt = points[0];
                double bestAngle = 3600;

                // Search the rest of the points.
                foreach (var pt in points)
                {
                    var testAngle = AngleValue(x1, y1, pt.X, pt.Y);
                    if (testAngle >= sweepAngle
                        && bestAngle > testAngle)
                    {
                        bestAngle = testAngle;
                        bestPt = pt;
                    }
                }

                // See if the first point is better.
                // If so, we are done.
                var firstAngle = AngleValue(x1, y1, hull[0].X, hull[0].Y);
                if (firstAngle >= sweepAngle
                    && bestAngle >= firstAngle)
                {
                    // The first point is better. We're done.
                    break;
                }

                // Add the best point to the convex hull.
                hull.Add(bestPt);
                points.Remove(bestPt);

                sweepAngle = bestAngle;

                // If all of the points are on the hull, we're done.
                if (points.Count == 0)
                {
                    break;
                }
            }

            return hull;
        }

        // Cull points out of the convex hull that lie inside the
        // trapezoid defined by the vertices with smallest and
        // largest X and Y coordinates.
        // Return the points that are not culled.
        private static List<Point> HullCull(List<Point> points)
        {
            // Find a culling box.
            var cullingBox = GetMinMaxBox(points);

            // Cull the points.
            var results = new List<Point>();
            foreach (var pt in points)
            {
                // See if (this point lies outside of the culling box.
                if (pt.X <= cullingBox.Left ||
                    pt.X >= cullingBox.Right ||
                    pt.Y <= cullingBox.Top ||
                    pt.Y >= cullingBox.Bottom)
                {
                    // This point cannot be culled.
                    // Add it to the results.
                    results.Add(pt);
                }
            }

            return results;
        }

        // Return a number that gives the ordering of angles
        // WEST horizontal from the point (x1, y1) to (x2, y2).
        // In other words, AngleValue(x1, y1, x2, y2) is not
        // the angle, but if:
        //   Angle(x1, y1, x2, y2) > Angle(x1, y1, x2, y2)
        // then
        //   AngleValue(x1, y1, x2, y2) > AngleValue(x1, y1, x2, y2)
        // this angle is greater than the angle for another set
        // of points,) this number for
        //
        // This function is dy / (dy + dx).
        private static double AngleValue(double x1, double y1, double x2, double y2)
        {
            double dx;
            double dy;
            double ax;
            double ay;
            double t;

            dx = x2 - x1;
            ax = Math.Abs(dx);
            dy = y2 - y1;
            ay = Math.Abs(dy);
            if (ax + ay == 0)
            {
                // if (the two points are the same, return 360.
                t = 360f / 9f;
            }
            else
            {
                t = dy / (ax + ay);
            }
            if (dx < 0)
            {
                t = 2 - t;
            }
            else if (dy < 0)
            {
                t = 4 + t;
            }
            return t * 90;
        }

        // Find a box that fits inside the MinMax quadrilateral.
        private static Rect GetMinMaxBox(List<Point> points)
        {
            // Find the MinMax quadrilateral.
            Point ul = new Point(0, 0);
            Point ur = ul;
            Point ll = ul;
            Point lr = ul;

            GetMinMaxCorners(points, ref ul, ref ur, ref ll, ref lr);

            // Get the coordinates of a box that lies inside this quadrilateral.
            double xmin;
            double xmax;
            double ymin;
            double ymax;

            xmin = ul.X;
            ymin = ul.Y;

            xmax = ur.X;
            if (ymin < ur.Y)
            {
                ymin = ur.Y;
            }

            if (xmax > lr.X)
            {
                xmax = lr.X;
            }
            ymax = lr.Y;

            if (xmin < ll.X)
            {
                xmin = ll.X;
            }

            if (ymax > ll.Y)
            {
                ymax = ll.Y;
            }

            var result = new Rect();
            if (xmax - xmin > 0 && ymax - ymin > 0)
            {
                result = new Rect(xmin, ymin, xmax - xmin, ymax - ymin);
            }

            return result;
        }

        // Find the points nearest the upper left, upper right,
        // lower left, and lower right corners.
        private static void GetMinMaxCorners(List<Point> points, ref Point ul, ref Point ur, ref Point ll, ref Point lr)
        {
            // Start with the first point as the solution.
            ul = points[0];
            ur = ul;
            ll = ul;
            lr = ul;

            // Search the other points.
            foreach (var pt in points)
            {
                if (-pt.X - pt.Y > -ul.X - ul.Y)
                {
                    ul = pt;
                }

                if (pt.X - pt.Y > ur.X - ur.Y)
                {
                    ur = pt;
                }

                if (-pt.X + pt.Y > -ll.X + ll.Y)
                {
                    ll = pt;
                }

                if (pt.X + pt.Y > lr.X + lr.Y)
                {
                    lr = pt;
                }
            }
        }

        // http://csharphelper.com/blog/2016/01/clip-a-line-segment-to-a-polygon-in-c/

        // Return points where the segment enters and leaves the polygon.
        public static Point[] ClipLineWithPolygon(Point point1, Point point2, List<Point> points, out bool lineStartsOutsidePolygon)
        {
            // Make lists to hold points of
            // intersection and their t values.
            var intersections = new List<Point>();
            var tValues = new List<double>();

            // Add the segment's starting point.
            intersections.Add(point1);
            tValues.Add(0f);
            lineStartsOutsidePolygon = !PointIsInPolygon(point1.X, point1.Y, points.ToArray());

            // Examine the polygon's edges.
            for (var i1 = 0; i1 < points.Count; i1++)
            {
                // Get the end points for this edge.
                var i2 = (i1 + 1) % points.Count;

                // See where the edge intersects the segment.
                bool segmentsIntersect;
                Point intersection;

                FindIntersection(point1, point2,
                                 points[i1], points[i2],
                                 out _, out segmentsIntersect,
                                 out intersection);

                // See if the segment intersects the edge.
                if (segmentsIntersect)
                {
                    // See if we need to record this intersection.

                    // Record this intersection.
                    intersections.Add(intersection);
                }
            }

            // Add the segment's ending point.
            intersections.Add(point2);
            tValues.Add(1f);

            // Sort the points of intersection by t value.
            var intersectionsArray = intersections.ToArray();
            var tArray = tValues.ToArray();
            Array.Sort(tArray, intersectionsArray);

            // Return the intersections.
            return intersectionsArray;
        }

        // Return True if the point is in the polygon.
        private static bool PointIsInPolygon(double x, double y, Point[] points)
        {
            // Get the angle between the point and the
            // first and last vertices.
            var maxPoint = points.Length - 1;
            var totalAngle = GetAngle(
                points[maxPoint].X, points[maxPoint].Y,
                x, y,
                points[0].X, points[0].Y);

            // Add the angles from the point
            // to each other pair of vertices.
            for (var i = 0; i < maxPoint; i++)
            {
                totalAngle += GetAngle(
                    points[i].X, points[i].Y,
                    x, y,
                    points[i + 1].X, points[i + 1].Y);
            }

            // The total angle should be 2 * PI or -2 * PI if
            // the point is in the polygon and close to zero
            // if the point is outside the polygon.
            // The following statement was changed. See the comments.
            return (Math.Abs(totalAngle) > 1);
        }

        // https://stackoverflow.com/questions/1119451/how-to-tell-if-a-line-intersects-a-polygon-in-c
        public static bool IsOutside(Point lineP1, Point lineP2, List<Point> region)
        {
            if (region == null || !region.Any())
            {
                return true;
            }
            var side = GetSide(lineP1, lineP2, region.First());
            return side != 0 && region.All(x => GetSide(lineP1, lineP2, x) == side);
        }

        public static int GetSide(Point lineP1, Point lineP2, Point queryP)
        {
            return Math.Sign((lineP2.X - lineP1.X) * (queryP.Y - lineP1.Y) - (lineP2.Y - lineP1.Y) * (queryP.X - lineP1.X));
        }

        // Return the angle ABC.
        // Return a value between PI and -PI.
        // Note that the value is the opposite of what you might
        // expect because Y coordinates increase downward.
        private static double GetAngle(double ax, double ay, double bx, double by, double cx, double cy)
        {
            // Get the dot product.
            var dotProduct = DotProduct(ax, ay, bx, @by, cx, cy);

            // Get the cross product.
            var crossProductLength = CrossProductLength(ax, ay, bx, @by, cx, cy);

            // Calculate the angle.
            return Math.Atan2(crossProductLength, dotProduct);
        }

        // Return the dot product AB · BC.
        // Note that AB · BC = |AB| * |BC| * Cos(theta).
        private static double DotProduct(double ax, double ay, double bx, double by, double cx, double cy)
        {
            // Get the vectors' coordinates.
            var bax = ax - bx;
            var bay = ay - @by;
            var bcx = cx - bx;
            var bcy = cy - @by;

            // Calculate the dot product.
            return bax * bcx + bay * bcy;
        }

        // Return the cross product AB x BC.
        // The cross product is a vector perpendicular to AB
        // and BC having length |AB| * |BC| * Sin(theta) and
        // with direction given by the right-hand rule.
        // For two vectors in the X-Y plane, the result is a
        // vector with X and Y components 0 so the Z component
        // gives the vector's length and direction.
        private static double CrossProductLength(double ax, double ay, double bx, double by, double cx, double cy)
        {
            // Get the vectors' coordinates.
            var bax = ax - bx;
            var bay = ay - @by;
            var bcx = cx - bx;
            var bcy = cy - @by;

            // Calculate the Z coordinate of the cross product.
            return bax * bcy - bay * bcx;
        }

        /// <summary>
        /// Gets the mid point between two points
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static Point GetMidPoint(Point start, Point end)
        {
            double xx = (start.X + end.X) / 2;
            double yy = (start.Y + end.Y) / 2;

            return new Point(xx, yy);
        }

        /// <summary>
        /// Shrinks line by n pixels about midpoint
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <param name="pixelCount"></param>
        public static void AdjustLineAboutMidpoint(ref Point startPoint, ref Point endPoint, double pixelCount)
        {
            Point midPoint = GetMidPoint(startPoint, endPoint);
            AdjustLineEndPoint(midPoint, ref startPoint, pixelCount);
            AdjustLineEndPoint(midPoint, ref endPoint, pixelCount);
        }

        private static void AdjustLineEndPoint(Point startPoint, ref Point endPoint, double pixelCount)
        {
            double dx = endPoint.X - startPoint.X;
            double dy = endPoint.Y - startPoint.Y;

            if (dx == 0)
            {
                // vertical line:
                if (endPoint.Y < startPoint.Y)
                {
                    endPoint.Y -= pixelCount;
                }
                else
                {
                    endPoint.Y += pixelCount;
                }
            }
            else if (dy == 0)
            {
                // horizontal line:
                if (endPoint.X < startPoint.X)
                {
                    endPoint.X -= pixelCount;
                }
                else
                {
                    endPoint.X += pixelCount;
                }
            }
            else
            {
                // non-horizontal, non-vertical line:
                double length = Math.Sqrt(dx * dx + dy * dy);
                double scale = (length + pixelCount) / length;

                dx *= scale;
                dy *= scale;
                endPoint.X = startPoint.X + dx;
                endPoint.Y = startPoint.Y + dy;
            }
        }

        /// <summary>
        /// Find the point of intersection between the lines line1Start --> line1End and line2Start --> line2End.
        /// </summary>
        /// <param name="line1Start"></param>
        /// <param name="line1End"></param>
        /// <param name="line2Start"></param>
        /// <param name="line2End"></param>
        /// <param name="canIntersect">True if the lines containing the segments can intersect</param>
        /// <param name="doIntersect">True if the segments intersect</param>
        /// <param name="intersection">The point where the lines do or would intersect</param>
        public static void FindIntersection(Point line1Start, Point line1End, Point line2Start, Point line2End,
            out bool canIntersect, out bool doIntersect, out Point intersection)
        {
            // Source: http://csharphelper.com/blog/2014/08/determine-where-two-lines-intersect-in-c/

            // Get the segments' parameters.
            double dx12 = line1End.X - line1Start.X;
            double dy12 = line1End.Y - line1Start.Y;
            double dx34 = line2End.X - line2Start.X;
            double dy34 = line2End.Y - line2Start.Y;

            // Solve for t1 and t2
            double denominator = dy12 * dx34 - dx12 * dy34;

            double t1 = ((line1Start.X - line2Start.X) * dy34 + (line2Start.Y - line1Start.Y) * dx34) / denominator;
            if (double.IsInfinity(t1))
            {
                // The lines are parallel (or close enough to it).
                canIntersect = false;
                doIntersect = false;
                intersection = new Point(double.NaN, double.NaN);
                return;
            }

            canIntersect = true;

            double t2 = ((line2Start.X - line1Start.X) * dy12 + (line1Start.Y - line2Start.Y) * dx12) / -denominator;

            // Find the point of intersection.
            intersection = new Point(line1Start.X + dx12 * t1, line1Start.Y + dy12 * t1);

            // The segments intersect if t1 and t2 are between 0 and 1.
            doIntersect = t1 >= 0 && t1 <= 1 && t2 >= 0 && t2 <= 1;
        }

        /// <summary>
        /// AngleBetween - the angle between 2 vectors
        /// </summary>
        /// <returns>
        /// Returns the the angle in degrees between vector1 and vector2
        /// </returns>
        /// <param name="vector1"> The first Vector </param>
        /// <param name="vector2"> The second Vector </param>
        public static double AngleBetween(Vector vector1, Vector vector2)
        {
            double sin = vector1.X * vector2.Y - vector2.X * vector1.Y;
            double cos = vector1.X * vector2.X + vector1.Y * vector2.Y;

            return Math.Atan2(sin, cos) * (180 / Math.PI);
        }

        /// <summary>
        /// Finds the angle between two points
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static double AngleBetween(Point p1, Point p2)
        {
            double xDiff = p2.X - p1.X;
            double yDiff = p2.Y - p1.Y;
            return Math.Atan2(yDiff, xDiff) * 180.0 / Math.PI;
        }

        public static double DistanceBetween(Point p1, Point p2)
        {
            double a = p2.X - p1.X;
            double b = p2.Y - p1.Y;

            return Math.Sqrt(a * a + b * b);
        }

        /// <summary>
        /// gets signed angle between three points
        /// direction is anticlockwise
        /// example:
        /// GetAngle(new Point2(1,0), new Point2(0,0), new Point2(0,1)) => Math.PI/2
        /// GetAngle(new Point2(-1,0), new Point2(0,0), new Point2(0,1)) => -Math.PI/2
        /// GetAngle(new Point2(0,1), new Point2(0,0), new Point2(1,0)) => -Math.PI/2
        ///
        /// <param name="point0">first point</param>
        /// <param name="point1">centre point</param>
        /// <param name="point2">final point</param>
        /// <param name="epsilon">tolerance</param>
        /// <exception cref="ArgumentException">if any atoms are coincident</exception>
        /// <returns>null if any points are null</returns>
        /// </summary>
        public static double? GetAngle(Point? point0, Point? point1, Point? point2, double epsilon)
        {
            double? angle = null;

            if (point0 != null && point1 != null && point2 != null)
            {
                if ((point1 - point0).Value.Length < epsilon || (point1 - point2).Value.Length < epsilon)
                {
                    Debugger.Break();
                    throw new ArgumentException("coincident points in GetAngle");
                }

                Vector fromVector = point0.Value - point1.Value;
                Vector toVector = point2.Value - point1.Value;
                angle = Vector.AngleBetween(fromVector, toVector);
            }

            return angle;
        }

        public static double Determinant(Vector vector1, Vector vector2)
        {
            return vector1.X * vector2.Y - vector1.Y * vector2.X;
        }

        /// <summary>
        /// If two finite line segments intersect, returns a point at which they cross. Null otherwise.
        /// </summary>
        /// <param name="segment1Start">Point at which first segment starts</param>
        /// <param name="segment1End">Point at which first segment ends</param>
        /// <param name="segment2Start">Point at which second segment starts</param>
        /// <param name="segment2End">Point at which second segment ends</param>
        /// <returns>Point at which both lines intersect, null if otherwise</returns>
        public static Point? GetIntersection(Point segment1Start, Point segment1End, Point segment2Start, Point segment2End)
        {
            IntersectLines(segment1Start, segment1End, segment2Start, segment2End, out var t, out var u);
            if (t >= 0 && u >= 0 && t <= 1 && u <= 1) //voila, we have an intersection
            {
                Vector vIntersect = (segment1End - segment1Start) * t;
                return segment1Start + vIntersect;
            }

            return null;
        }

        /// <summary>
        /// intersects two straight line segments.  Returns two values that indicate
        /// how far along the segments the intersection takes place.
        /// Values between 0 and 1 for both segments indicate the lines cross
        /// Values between 0 and 1 for ONE segment indicates that the projection
        /// of the other segment intersects it
        /// </summary>
        /// <param name="segment1Start">what it says</param>
        /// <param name="segment1End">what it says</param>
        /// <param name="segment2Start">what it says</param>
        /// <param name="segment2End">what it says</param>
        /// <param name="t">proportion along the line of the first segment</param>
        /// <param name="u">proportion along the line of the second segment</param>
        public static void IntersectLines(Point segment1Start,
                                          Point segment1End,
                                          Point segment2Start,
                                          Point segment2End,
                                          out double t,
                                          out double u)
        {
            double det = Determinant(segment1End - segment1Start, segment2Start - segment2End);
            t = Determinant(segment2Start - segment1Start, segment2Start - segment2End) / det;
            u = Determinant(segment1End - segment1Start, segment2Start - segment1Start) / det;
        }

        public static Point GetCentroid(Rect rectangle) => rectangle.TopLeft + (rectangle.BottomRight - rectangle.TopLeft) * 0.5;

        // ReSharper disable once InconsistentNaming
        public static CompassPoints SnapTo2EW(double angleFromNorth)
        {
            if (angleFromNorth >= 0 || angleFromNorth <= -180)
            {
                return CompassPoints.East;
            }

            return CompassPoints.West;
        }

        // ReSharper disable once InconsistentNaming
        public static CompassPoints SnapTo4NESW(double angleFromNorth)
        {
            if (angleFromNorth >= -45 && angleFromNorth <= 45)
            {
                return CompassPoints.North;
            }

            if (angleFromNorth > 45 && angleFromNorth < 135)
            {
                return CompassPoints.East;
            }

            if (angleFromNorth > -135 && angleFromNorth < -45)
            {
                return CompassPoints.West;
            }

            return CompassPoints.South;
        }

        public static int SnapToClock(double angleFromNorth)
        {
            int tolerance = 15;
            var sector = SnapAngleToTolerance(angleFromNorth, tolerance);
            return sector;
        }

        public static Vector SnapVectorToClock(Vector vector)
        {
            double angle = Vector.AngleBetween(vector, GeometryTool.ScreenNorth);
            angle = Math.Round(angle / 30.0) * 30;

            int clock = Convert.ToInt32(angle);

            Matrix rotator = new Matrix();
            rotator.Rotate(-clock);

            Vector result = GeometryTool.ScreenNorth;
            result *= rotator;

            return result;
        }

        private static int SnapAngleToTolerance(double angleFromNorth, int tolerance)
        {
            if (angleFromNorth < 0)
            {
                angleFromNorth += 360.0;
            }
            double adjustedAngle = angleFromNorth + tolerance;
            int sector = (int)adjustedAngle / (tolerance * 2);
            return sector;
        }

        //determines whether a rectangle clips a line

        public static bool RectClips(Rect rect, Point startPoint, Point endPoint)
        {
            Point? intersection;
            intersection = GetIntersection(rect.TopLeft, rect.TopRight, startPoint, endPoint);
            if (intersection != null)
            {
                return true;
            }
            intersection = GetIntersection(rect.TopRight, rect.BottomRight, startPoint, endPoint);
            if (intersection != null)
            {
                return true;
            }

            intersection = GetIntersection(rect.BottomRight, rect.BottomLeft, startPoint, endPoint);
            if (intersection != null)
            {
                return true;
            }

            intersection = GetIntersection(rect.BottomLeft, rect.TopLeft, startPoint, endPoint);
            if (intersection != null)
            {
                return true;
            }

            return false;
        }

        //determines the point at which a line drawn into a block clips the boundary

        public static Point? GetClippingPoint(Rect blockBounds, Point blockCentre, Point rectMidPoint)
        {
            Point? intersection;
            intersection = GetIntersection(rectMidPoint, blockCentre, blockBounds.TopLeft, blockBounds.TopRight);
            if (intersection is null)
            {
                intersection = GetIntersection(rectMidPoint, blockCentre, blockBounds.TopRight, blockBounds.BottomRight);
            }

            if (intersection is null)
            {
                intersection =
                    GetIntersection(rectMidPoint, blockCentre, blockBounds.BottomRight, blockBounds.BottomLeft);
            }

            if (intersection is null)
            {
                intersection = GetIntersection(rectMidPoint, blockCentre, blockBounds.BottomLeft, blockBounds.TopLeft);
            }

            return intersection;
        }

        #region extension methods

        public static Vector ScreenSouth => new Vector(0, 1);

        public static Vector ScreenEast => new Vector(1, 0);

        public static Vector ScreenNorth => -ScreenSouth;

        public static Vector ScreenWest => -ScreenEast;

        #endregion extension methods
    }
}