// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.Model2.Geometry
{
    public static class AngleMethods
    {
        public static Vector ToVector(this Globals.ClockDirections dir)
        {
            Matrix rotator = new Matrix();
            rotator.Rotate((int)dir.ToDegrees());
            return BasicGeometry.ScreenNorth * rotator;
        }

        public static double ToDegrees(this Globals.ClockDirections cd)
        {
            return 30 * ((int)cd % 12);
        }

        /// <summary>
        /// Splits the angle between two clock directions
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns>A clock direction pointing to the new direction </returns>
        public static Globals.ClockDirections Split(this Globals.ClockDirections first, Globals.ClockDirections second)
        {
            return (Globals.ClockDirections)((((int)first + (int)second) % 12) / 2);
        }

        public static double ToDegrees(this CompassPoints cp)
        {
            return 45 * (int)cp;
        }
    }

    public enum CompassPoints
    {
        North,
        NorthEast,
        East,
        SouthEast,
        South,
        SouthWest,
        West,
        NorthWest
    }

    public class Geometry<T>
    {
        /// <summary>
        /// Returns the objects that make up the convex hull, in the right order
        /// </summary>
        /// <param name="sortedObjectList">List of objects to process sorted by X, Y coordinates</param>
        /// <param name="getPosition">Lambda or delegate to obtain the position property of the object</param>
        /// <returns></returns>
        public static List<T> GetHull(IEnumerable<T> sortedObjectList, Func<T, Point> getPosition)
        {
            List<T> upper = new List<T>();
            List<T> lower = new List<T>();
            var sortedObjects = sortedObjectList.ToArray();

            for (int i = 0; i < sortedObjects.Count(); i++)
            {
                while (lower.Count >= 2 &&
                       Vector.AngleBetween((getPosition(lower[lower.Count - 2]) - getPosition(lower[lower.Count - 1])),
                           (getPosition(sortedObjects[i]) - getPosition(lower[lower.Count - 1]))) > 0)
                {
                    lower.RemoveAt(lower.Count() - 1);
                }
                lower.Add(sortedObjects[i]);
            }

            for (int i = sortedObjects.Count() - 1; i >= 0; i--)
            {
                while (upper.Count >= 2 &&
                       Vector.AngleBetween((getPosition(upper[upper.Count - 2]) - getPosition(upper[upper.Count - 1])),
                           (getPosition(sortedObjects[i]) - getPosition(upper[upper.Count - 1]))) > 0)
                {
                    upper.RemoveAt(upper.Count() - 1);
                }
                upper.Add(sortedObjects[i]);
            }
            upper.RemoveAt(upper.Count() - 1);
            lower.RemoveAt(lower.Count() - 1);
            lower.AddRange(upper);
            return lower;
        }

        /// <summary>
        /// gets the centroid of an array of points
        /// </summary>
        /// <param name="poly">Polygon represented as array of objects, sorted in anticlockwise order</param>
        /// <param name="getPosition">Lambda to return position of T</param>
        /// <returns>Point as geocenter</returns>
        public static Point? GetCentroid(T[] poly, Func<T, Point> getPosition)
        {
            double accumulatedArea = 0.0f;
            double centerX = 0.0f;
            double centerY = 0.0f;
            if (poly.Any())
            {
                var minX = poly.Min(p => getPosition(p).X);
                var maxX = poly.Max(p => getPosition(p).X);
                var minY = poly.Min(p => getPosition(p).Y);
                var maxY = poly.Max(p => getPosition(p).Y);

                for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                {
                    double temp = getPosition(poly[i]).X * getPosition(poly[j]).Y
                                  - getPosition(poly[j]).X * getPosition(poly[i]).Y;
                    accumulatedArea += temp;
                    centerX += (getPosition(poly[i]).X + getPosition(poly[j]).X) * temp;
                    centerY += (getPosition(poly[i]).Y + getPosition(poly[j]).Y) * temp;
                }

                if (Math.Abs(accumulatedArea) < 1E-7f)
                {
                    return null; // Avoid division by zero
                }

                accumulatedArea *= 3f;
                var centroid = new Point(centerX / accumulatedArea, centerY / accumulatedArea);
                //Debug.Assert(centroid.X >= minX & centroid.X <= maxX & centroid.Y >= minY & centroid.Y <= maxY);
                return centroid;
            }

            return null;
        }
    }
}