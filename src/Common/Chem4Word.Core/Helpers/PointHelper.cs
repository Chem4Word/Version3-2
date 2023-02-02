// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
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
            var left = Clamp((int)point.X, width, screen.WorkingArea.Left, screen.WorkingArea.Left + screen.WorkingArea.Width);
            var top = Clamp((int)point.Y, height, screen.WorkingArea.Top, screen.WorkingArea.Top + screen.WorkingArea.Height);

            //var maximumRight = screen.WorkingArea.Left +  screen.WorkingArea.Width;
            //var maximumBottom = screen.WorkingArea.Top + screen.WorkingArea.Height;

            //while (left + width > maximumRight)
            //{
            //    left -= 24;
            //    if (left < screen.WorkingArea.Left)
            //    {
            //        left = screen.WorkingArea.Left;
            //    }
            //}

            //while (left < screen.WorkingArea.Left)
            //{
            //    left += 24;
            //}

            //while (top + height > maximumBottom)
            //{
            //    top -= 24;
            //    if (top < screen.WorkingArea.Top)
            //    {
            //        top = screen.WorkingArea.Top;
            //    }
            //}

            //while (top < screen.WorkingArea.Top)
            //{
            //    top += 24;
            //}

            return new Point(left, top);
        }

        private static int Clamp(int leftOrTop, int widthOrHeight, int workingAreaLeftOrTop, int workingAreaWidthOrHeight)
        {
            var result = leftOrTop;

            while (result + widthOrHeight > workingAreaWidthOrHeight)
            {
                result -= 24;
                if (result < workingAreaLeftOrTop)
                {
                    result = workingAreaLeftOrTop;
                }
            }

            while (result < workingAreaLeftOrTop)
            {
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