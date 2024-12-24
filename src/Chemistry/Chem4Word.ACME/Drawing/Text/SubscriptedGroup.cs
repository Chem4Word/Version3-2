// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Utils;
using Chem4Word.Core.Enums;
using Chem4Word.Core.Helpers;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Chem4Word.ACME.Drawing.Text
{
    /// <summary>
    /// handles a subscripted text annotation
    /// </summary>
    public class SubscriptedGroup
    {
        //how many atoms in the group
        public int Count { get; }

        //the group text
        public string Text { get; }

        //holds the text of the atoms
        private GlyphText _mainText;

        //holds the text of the subscript
        private SubLabelText _subText;

        private static double _fontSize;

        public SubscriptedGroup(int count, string text, double fontSize)
        {
            Count = count;
            Text = text;
            _fontSize = fontSize;
        }

        /// <summary>
        /// Measures the dimensions of the atom prior to rendering
        /// </summary>
        /// <param name="parentMetrics">Metrics of the parent atom</param>
        /// <param name="direction">Orientation of the group relative to the parent atom, i.e. NESW</param>
        /// <param name="pixelsPerDip">Display dependent parameter for rendering text</param>
        /// <returns>AtomTextMetrics object describing placement</returns>
        public AtomTextMetrics Measure(AtomTextMetrics parentMetrics, CompassPoints direction, float pixelsPerDip)
        {
            _subText = null;

            List<Point> mainOutline;
            //first, get some initial size measurements
            _mainText = new GlyphText(Text, GlyphUtils.SymbolTypeface, _fontSize, pixelsPerDip);
            _mainText.Premeasure();

            //measure up the subscript (if we have one)
            string subscriptText = TextUtils.GetSubText(Count);
            if (subscriptText != "")
            {
                _subText = new SubLabelText(subscriptText, pixelsPerDip, _fontSize * Controller.ScriptScalingFactor);
                _subText.Premeasure();
            }

            //calculate the center of the H Atom depending on the direction
            var groupCenter = GetAdjunctCenter(parentMetrics, direction, _mainText.GlyphInfo, _subText?.GlyphInfo);
            //remeasure the main text
            _mainText.MeasureAtCenter(groupCenter);

            mainOutline = _mainText.FlattenedPath;

            if (_subText != null)
            //get the offset for the subscript
            {
                Vector subscriptOffset = new Vector(_mainText.TextMetrics.TotalBoundingBox.Width + _mainText.TrailingBearing + _subText.LeadingBearing,
                                                    _subText.TextMetrics.BoundingBox.Height / 2);
                Point subBottomLeft = _mainText.TextMetrics.TotalBoundingBox.BottomLeft + subscriptOffset;
                _subText.MeasureAtBottomLeft(subBottomLeft, pixelsPerDip);
                //merge the total bounding boxes
                _mainText.Union(_subText);
                mainOutline.AddRange(_subText.FlattenedPath);
            }
            //return the placement metrics for the subscripted atom.
            AtomTextMetrics result = new AtomTextMetrics
            {
                Geocenter = groupCenter,
                BoundingBox = _mainText.TextMetrics.BoundingBox,
                TotalBoundingBox = _mainText.TextMetrics.TotalBoundingBox,
                FlattenedPath = mainOutline
            };

            return result;
        }

        /// <summary>
        /// Draws the subscripted group text
        /// </summary>
        /// <param name="drawingContext">DC supplied by OnRender</param>
        /// <param name="measure">Provided by calling the Measure method previously</param>
        /// <param name="pixelsPerDip">System-dependent rendering factor</param>
        /// <param name="fill"></param>
        public void DrawSelf(DrawingContext drawingContext, AtomTextMetrics measure, float pixelsPerDip, Brush fill)
        {
            _mainText.Fill = fill;
            _mainText.DrawAtBottomLeft(measure.BoundingBox.BottomLeft, drawingContext);
            if (_subText != null)
            {
                _subText.Fill = fill;
                _subText.DrawAtBottomLeft(_subText.TextMetrics.BoundingBox.BottomLeft, drawingContext);
            }
        }

        /// <summary>
        /// Gets the center point of an atom adjunct (like an implicit hydrogen plus subscripts)
        /// The Adjunct in NH2 is H2
        /// </summary>
        /// <param name="parentMetrics">Metrics of the parent atom</param>
        /// <param name="direction">NESW direction of the adjunct respective to the atom</param>
        /// <param name="adjunctGlyphInfo">Initial measurements of the adjunct</param>
        /// <param name="subscriptInfo">Initial measurements of the subscript (can be null for no subscripts)</param>
        /// <returns></returns>
        private static Point GetAdjunctCenter(AtomTextMetrics parentMetrics, CompassPoints direction,
                                              GlyphUtils.GlyphInfo adjunctGlyphInfo, GlyphUtils.GlyphInfo? subscriptInfo = null)
        {
            Point adjunctCenter;
            double charHeight = GlyphUtils.GlyphTypeface.Baseline * _fontSize;
            double adjunctWidth = (parentMetrics.BoundingBox.Width + adjunctGlyphInfo.Width) / 2;
            switch (direction)
            {
                //all addition in this routine is *vector* addition.
                //We are not adding absolute X and Y values
                case CompassPoints.North:
                    adjunctCenter = parentMetrics.Geocenter +
                                    GeometryTool.ScreenNorth * charHeight;
                    break;

                case CompassPoints.West:
                    if (subscriptInfo != null)
                    {
                        adjunctCenter = parentMetrics.Geocenter + GeometryTool.ScreenWest *
                            (adjunctWidth + subscriptInfo.Value.Width);
                    }
                    else
                    {
                        adjunctCenter = parentMetrics.Geocenter + GeometryTool.ScreenWest * adjunctWidth;
                    }
                    break;

                case CompassPoints.South:
                    adjunctCenter = parentMetrics.Geocenter +
                                    GeometryTool.ScreenSouth * charHeight;
                    break;

                default:
                    adjunctCenter = parentMetrics.Geocenter + GeometryTool.ScreenEast * adjunctWidth;
                    break;
            }

            return adjunctCenter;
        }
    }
}