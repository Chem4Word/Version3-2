// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
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
        /// <param name="point">Starting point</param>
        /// <param name="screen">Screen object</param>
        /// <param name="width">Desired width</param>
        /// <param name="height">Desired Height</param>
        /// <returns></returns>
        public static Point SensibleTopLeft(Point point, Screen screen, int width, int height)
        {
            var left = Clamp((int)point.X, width, screen.WorkingArea.Left, screen.WorkingArea.Left + screen.WorkingArea.Width);
            var top = Clamp((int)point.Y, height, screen.WorkingArea.Top, screen.WorkingArea.Top + screen.WorkingArea.Height);

            return new Point(left, top);
        }

        private static int Clamp(int leftOrTop, int widthOrHeight, int workingAreaLeftOrTop, int workingAreaWidthOrHeight)
        {
            var result = leftOrTop;

            var loops = 0;
            while (loops < 16 && result + widthOrHeight > workingAreaWidthOrHeight)
            {
                loops++;
                result -= 24;
                if (result < workingAreaLeftOrTop)
                {
                    result = workingAreaLeftOrTop;
                }
            }

            loops = 0;
            while (loops < 16 && result < workingAreaLeftOrTop)
            {
                loops++;
                result += 24;
            }

            return result;
        }

        public static string AsString(Point p)
            => $"{SafeDouble.AsString4(p.X)},{SafeDouble.AsString4(p.Y)}";

        public static object AsCMLString(Point p) =>
            $"{SafeDouble.AsCMLString(p.X)},{SafeDouble.AsCMLString(p.Y)}";
    }
}