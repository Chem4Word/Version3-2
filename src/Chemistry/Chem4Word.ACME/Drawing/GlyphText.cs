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
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using Chem4Word.Model2.Geometry;

namespace Chem4Word.ACME.Drawing
{
    /// <summary>
    /// Wraps up some of the glyph handling into a handy class
    /// Mostly stateful and uses properties to simplify the client code
    /// </summary>
    public class GlyphText
    {
        public string Text { get; }
        public Typeface CurrentTypeface { get; }

        //standard label font
        public double TypeSize { get; }

        public float PixelsPerDip { get; }

        protected GlyphTypeface _glyphTypeface;

        public GlyphUtils.GlyphInfo GlyphInfo { get; protected set; }
        public AtomTextMetrics TextMetrics { get; protected set; }

        public GlyphRun TextRun { get; protected set; }
        public Brush Fill { get; set; }

        public Path Outline
        {
            get
            {
                var hull = Hull;
                return BasicGeometry.BuildPath(hull);
            }
        }

        public List<Point> Hull
        {
            get
            {
                var outline = GlyphUtils.GetOutline(TextRun);

                var sortedHull = (from Point p in outline
                                  orderby p.X ascending, p.Y descending
                                  select p).ToList();

                List<Point> hull = Geometry<Point>.GetHull(sortedHull, p => p);
                return hull;
            }
        }

        public GlyphText(string text, Typeface typeface, double typesize, float pixelsPerDip)
        {
            if (!GlyphUtils.SymbolTypeface.TryGetGlyphTypeface(out _glyphTypeface))
            {
                Debugger.Break();
                throw new InvalidOperationException($"No glyph typeface found for the Windows Typeface '{typeface.FaceNames[XmlLanguage.GetLanguage("en-GB")]}'");
            }
            Text = text;
            CurrentTypeface = typeface;
            TypeSize = typesize;
            PixelsPerDip = pixelsPerDip;

            TextMetrics = null;
        }

        public double FirstBearing(GlyphRun gr)
        {
            return _glyphTypeface.LeftSideBearings[gr.GlyphIndices.First()] * TypeSize;
        }

        public double LeadingBearing
        {
            get
            {
                return _glyphTypeface.LeftSideBearings[TextRun.GlyphIndices.First()] * TypeSize;
            }
        }

        public double TrailingBearing
        {
            get
            {
                return _glyphTypeface.RightSideBearings[TextRun.GlyphIndices.Last()] * TypeSize;
            }
        }

        public double MaxBaselineOffset
        {
            get
            {
                return GlyphInfo.UprightBaselineOffsets.Max();
            }
        }

        public List<Point> FlattenedPath
        {
            get
            {
                Vector offset = new Vector(0.0, MaxBaselineOffset) + this.TextMetrics.OffsetVector;
                return TextRun.GetOutline().Select(p => p + offset).ToList();
            }
        }

        public void MeasureAtCenter(Point center)
        {
            GlyphInfo = GlyphUtils.GetGlyphsAndInfo(Text, PixelsPerDip, out GlyphRun groupGlyphRun, center, _glyphTypeface, TypeSize);
            //compensate the main offset vector for any descenders

            Vector mainOffset = GlyphUtils.GetOffsetVector(groupGlyphRun, TypeSize) +
                                new Vector(0.0, -MaxBaselineOffset);
            var bb = groupGlyphRun.GetBoundingBox(center + mainOffset);
            Vector textFormatterOffset = new Vector(mainOffset.X, -FirstBearing(groupGlyphRun) - bb.Height / 2);

            TextRun = groupGlyphRun;
            TextMetrics = new AtomTextMetrics
            {
                BoundingBox = bb,
                Geocenter = center,
                TotalBoundingBox = groupGlyphRun.GetBoundingBox(center + mainOffset),
                OffsetVector = mainOffset,
                TextFormatterOffset = textFormatterOffset
            };
        }

        public void Premeasure()
        {
            MeasureAtCenter(new Point(0d, 0d));
        }

        public void MeasureAtBottomLeft(Point bottomLeft, float PixelsPerDip)
        {
            GlyphInfo = GlyphUtils.GetGlyphsAndInfo(Text, PixelsPerDip, out GlyphRun groupGlyphRun, bottomLeft, _glyphTypeface, TypeSize);
            TextRun = groupGlyphRun;
            TextMetrics = new AtomTextMetrics
            {
                BoundingBox = groupGlyphRun.GetBoundingBox(bottomLeft),
                Geocenter = bottomLeft,
                TotalBoundingBox = groupGlyphRun.GetBoundingBox(bottomLeft),
                FlattenedPath = GlyphUtils.GetOutline(TextRun),
                OffsetVector = new Vector(0.0d, 0.0d)
            };
        }

        public void DrawAtBottomLeft(Point bottomLeft, DrawingContext dc)
        {
            GlyphInfo = GlyphUtils.GetGlyphsAndInfo(Text, PixelsPerDip, out GlyphRun groupGlyphRun, bottomLeft, _glyphTypeface, TypeSize);
            dc.DrawGlyphRun(Fill, groupGlyphRun);
            TextRun = groupGlyphRun;
        }

        public void Union(GlyphText gt)
        {
            Rect res = TextMetrics.BoundingBox;
            res.Union(gt.TextMetrics.TotalBoundingBox);
            TextMetrics.TotalBoundingBox = res;
        }

        public bool CollidesWith(params Rect[] occupiedAreas)
        {
            foreach (Rect area in occupiedAreas)
            {
                if (area.IntersectsWith(TextMetrics.TotalBoundingBox))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public class MainLabelText : GlyphText
    {
        public MainLabelText(string text, float pixelsPerDip, double typeSize)
            : base(text, GlyphUtils.SymbolTypeface, typeSize, pixelsPerDip)
        { }
    }

    public class SubLabelText : GlyphText
    {
        public SubLabelText(string text, float pixelsPerDip, double SubscriptSize)
            : base(text, GlyphUtils.SymbolTypeface, SubscriptSize, pixelsPerDip)
        { }
    }

    public class IsotopeLabelText : GlyphText
    {
        public IsotopeLabelText(string text, float pixelsPerDip, double superScriptSize)
            : base(text, GlyphUtils.SymbolTypeface, superScriptSize, pixelsPerDip)
        { }
    }

    public class ChargeLabelText : GlyphText
    {
        public ChargeLabelText(string text, float pixelsPerDip, double superScriptSize)
            : base(text, GlyphUtils.SymbolTypeface, superScriptSize, pixelsPerDip)
        {
        }
    }

    /// <summary>
    /// Facilitates layout and positioning of text
    /// </summary>
    public class AtomTextMetrics : LabelMetrics
    {
        public Rect TotalBoundingBox; //surrounds ALL the text

        public AtomTextMetrics()
        {
            TotalBoundingBox = new Rect(0d, 0d, 0d, 0d);
        }

        public override List<Point> Corners
        {
            get
            {
                List<Point> corners = new List<Point>();
                corners.Add(TotalBoundingBox.BottomLeft);
                corners.Add(TotalBoundingBox.BottomRight);
                corners.Add(TotalBoundingBox.TopLeft);
                corners.Add(TotalBoundingBox.TopRight);
                return corners;
            }
        }

        public Vector OffsetVector { get; set; }
        public Vector TextFormatterOffset { get; set; }
    }

    public class LabelMetrics
    {
        public Point Geocenter;  //the center of the charge text
        public Rect BoundingBox; //the bounding box surrounds the text

        public LabelMetrics()
        {
            Geocenter = new Point(0d, 0d);
            BoundingBox = new Rect(0d, 0d, 0d, 0d);
        }

        public virtual List<Point> Corners
        {
            get
            {
                List<Point> corners = new List<Point>();
                corners.Add(BoundingBox.BottomLeft);
                corners.Add(BoundingBox.BottomRight);
                corners.Add(BoundingBox.TopLeft);
                corners.Add(BoundingBox.TopRight);
                return corners;
            }
        }

        public List<Point> FlattenedPath { get; set; }
    }
}