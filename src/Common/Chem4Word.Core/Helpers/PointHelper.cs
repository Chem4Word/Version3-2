// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Forms;

namespace Chem4Word.Core.Helpers
{
    public static class PointHelper
    {
        public static bool PointIsEmpty(Point point)
            => point.Equals(new Point(0, 0));

        /// <summary>
        /// Returns a point which can be used as the top left hand corner of a form without hiding any part of it off bottom right of current screen
        /// </summary>
        /// <param name="point"></param>
        /// <param name="screen"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Point SensibleTopLeft(Point point, Screen screen, int width, int height)
        {
            var left = point.X;
            var top = point.Y;

            var screenWidth = screen.WorkingArea.Width;
            var screenHeight = screen.WorkingArea.Height;

            while (left + width > screenWidth)
            {
                left -= 24;
                if (left < 0)
                {
                    left = 0;
                }
            }

            while (top + height > screenHeight)
            {
                top -= 24;
                if (top < 0)
                {
                    top = 0;
                }
            }

            return new Point(left, top);
        }

        public static string AsString(Point p)
            => $"{SafeDouble.AsString4(p.X)},{SafeDouble.AsString4(p.Y)}";

        public static object AsCMLString(Point p) =>
            $"{SafeDouble.AsCMLString(p.X)},{SafeDouble.AsCMLString(p.Y)}";
    }
}