// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace Chem4Word.Renderer.OoXmlV4.TTF
{
    public class TtfPoint
    {
        public enum PointType
        {
            Start,
            Line,
            CurveOff,
            CurveOn
        }

        public int X { get; set; }
        public int Y { get; set; }
        public PointType Type { get; set; }
    }
}