﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;

namespace Chem4Word.Core.Helpers
{
    public static class PointHelper
    {
        public static bool PointIsEmpty(Point point)
            => point.Equals(new Point(0, 0));

        public static string AsString(Point p)
            => $"{SafeDouble.AsString4(p.X)},{SafeDouble.AsString4(p.Y)}";

        public static object AsCMLString(Point p) =>
            $"{SafeDouble.AsCMLString(p.X)},{SafeDouble.AsCMLString(p.Y)}";
    }
}