// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace Chem4Word.ACME.Drawing
{
    public static class GlyphUtils
    {
        public struct GlyphInfo
        {
            public ushort[] Indexes;
            public double Width;
            public double[] AdvanceWidths;
            public double[] UprightBaselineOffsets;
        }

        private static GlyphTypeface _glyphTypeface;

        public static GlyphTypeface GlyphTypeface
        {
            get
            {
                return _glyphTypeface;
            }
        }

        public static Typeface SymbolTypeface { get; } = new Typeface(new FontFamily("Arial"),
                                                                       FontStyles.Normal,
                                                                       FontWeights.Bold,
                                                                       FontStretches.Normal);

        public static Typeface MoleculelabelTypeface { get; } = new Typeface(new FontFamily("Arial"),
                                                                             FontStyles.Normal,
                                                                             FontWeights.Normal,
                                                                             FontStretches.Normal);

        static GlyphUtils()
        {
            if (!SymbolTypeface.TryGetGlyphTypeface(out _glyphTypeface))
            {
                Debugger.Break();
                throw new InvalidOperationException("No glyphtypeface found");
            }
        }

        /// <summary>
        /// Gets the vector that must be added to the atom position to center the glyph
        /// </summary>
        /// <param name="glyphRun">Run of text for atom symbol</param>
        /// <param name="symbolSize">Size of symbol in DIPs</param>
        /// <returns>Vector to be added to atom pos</returns>
        public static Vector GetOffsetVector(GlyphRun glyphRun, double symbolSize)
        {
            Rect rect = glyphRun.ComputeAlignmentBox();
            Vector offset = new Vector(-rect.Width / 2, glyphRun.GlyphTypeface.CapsHeight * symbolSize / 2);
            return offset;
        }

        /// <summary>
        /// Returns a glyph run for a given string of text
        /// </summary>
        /// <param name="symbolText">Text for the atom symbol</param>
        /// <param name="glyphTypeFace">Glyph type face used</param>
        /// <param name="size">Size in DIPS of the font</param>
        /// <returns>GlyphInfo of glyph indexes, overall width and array of advance widths</returns>
        public static GlyphInfo GetGlyphs(string symbolText, GlyphTypeface glyphTypeFace, double size)
        {
            ushort[] glyphIndexes = new ushort[symbolText.Length];
            double[] advanceWidths = new double[symbolText.Length];
            double[] uprightBaselineOffsets = new double[symbolText.Length];
            double totalWidth = 0;

            for (int n = 0; n < symbolText.Length; n++)
            {
                ushort glyphIndex = glyphTypeFace.CharacterToGlyphMap[symbolText[n]];
                glyphIndexes[n] = glyphIndex;

                double width = glyphTypeFace.AdvanceWidths[glyphIndex] * size;
                advanceWidths[n] = width;

                double ubo = glyphTypeFace.DistancesFromHorizontalBaselineToBlackBoxBottom[glyphIndex] * size;
                uprightBaselineOffsets[n] = ubo;
                totalWidth += width;
            }
            return new GlyphInfo { AdvanceWidths = advanceWidths, Indexes = glyphIndexes, Width = totalWidth, UprightBaselineOffsets = uprightBaselineOffsets };
        }

        public static GlyphUtils.GlyphInfo GetGlyphsAndInfo(string symbolText, float pixelsPerDip, out GlyphRun hydrogenGlyphRun, Point point, GlyphTypeface glyphTypeFace, double symbolSize)
        {
            //measure the H atom first
            var glyphInfo = GlyphUtils.GetGlyphs(symbolText, glyphTypeFace, symbolSize);
            hydrogenGlyphRun = GlyphUtils.GetGlyphRun(glyphInfo, glyphTypeFace,
                symbolSize, pixelsPerDip, point);
            //work out exactly how much we should offset from the center to get to the bottom left
            return glyphInfo;
        }

        /// <summary>
        /// Returns a rough outline of a glyph run.  useful for calculating a convex hull
        /// </summary>
        /// <param name="glyphRun">Glyph run to outline</param>
        /// <returns>List<Point> of geometry tracing the GlyphRun</Point></returns>
        public static List<Point> GetOutline(this GlyphRun glyphRun)
        {
            if (glyphRun != null)
            {
                var geo = glyphRun.BuildGeometry();
                return GetGeoPoints(geo);
            }

            return null;
        }

        public static List<Point> GetGeoPoints(Geometry geo)
        {
            List<Point> retval = new List<Point>();
            var pg = geo.GetFlattenedPathGeometry(0.01, ToleranceType.Relative);

            foreach (var f in pg.Figures)
            {
                foreach (var s in f.Segments)
                {
                    if (s is PolyLineSegment)
                    {
                        foreach (var pt in ((PolyLineSegment)s).Points)
                        {
                            retval.Add(pt);
                        }
                    }
                }
            }

            return retval;
        }

        /// <summary>
        /// gets a bounding box holding the overall glyph run
        /// </summary>
        /// <param name="glyphRun"></param>
        /// <param name="origin">where the run will be centered</param>
        /// <returns></returns>
        public static Rect GetBoundingBox(this GlyphRun glyphRun, Point origin)
        {
            Rect rect = glyphRun.ComputeInkBoundingBox();
            Matrix mat = new Matrix();
            mat.Translate(origin.X, origin.Y);

            rect.Transform(mat);
            return rect;
        }

        /// <summary>
        /// simple wrapper routine for generating a glyph run
        /// </summary>
        /// <param name="info"></param>
        /// <param name="glyphTypeface"></param>
        /// <param name="symbolSize"></param>
        /// <param name="pixelsPerDip"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static GlyphRun GetGlyphRun(GlyphInfo info, GlyphTypeface glyphTypeface, double symbolSize, float pixelsPerDip, Point point)
        {
            var run = new GlyphRun(glyphTypeface, 0, false, symbolSize, pixelsPerDip, info.Indexes, point, info.AdvanceWidths,
                null, null, null, null, null, null);

            return run;
        }
    }
}