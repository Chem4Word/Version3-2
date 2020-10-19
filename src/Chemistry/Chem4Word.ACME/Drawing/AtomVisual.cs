// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
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
using Chem4Word.Model2;
using Chem4Word.Model2.Geometry;
using Chem4Word.Model2.Helpers;
using static Chem4Word.ACME.Drawing.GlyphUtils;

namespace Chem4Word.ACME.Drawing
{
    /// <summary>
    /// DrawingVisual based class for rendering an Atom label
    /// </summary>
    public class AtomVisual : ChemicalVisual
    {
        #region Fields

        public bool ShowInColour { get; set; }
        public bool ShowImplicitHydrogens { get; set; }
        public bool ShowAllCarbons { get; set; }

        #endregion Fields

        #region Nested Classes

        #region Text Classes

        /// <summary>
        /// handles a subscripted text annotation
        /// </summary>
        private class SubscriptedGroup
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
            /// <param name="pixelsperDip">Display dependent parameter for  rendering text</param>
            /// <returns>AtomTextMetrics object describing placement</returns>
            public AtomTextMetrics Measure(AtomTextMetrics parentMetrics, CompassPoints direction, float pixelsPerDip)
            {
                _subText = null;

                List<Point> mainOutline;
                //first, get some initial size measurements
                _mainText = new GlyphText(Text, SymbolTypeface, _fontSize, pixelsPerDip);
                _mainText.Premeasure();

                //measure up the subscript (if we have one)
                string subscriptText = AtomHelpers.GetSubText(Count);
                if (subscriptText != "")
                {
                    _subText = new SubLabelText(subscriptText, pixelsPerDip, _fontSize * ViewModel.ScriptScalingFactor);
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
            /// <param name="pixelsPerDip"></param>
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
                GlyphInfo adjunctGlyphInfo, GlyphInfo? subscriptInfo = null)
            {
                Point adjunctCenter;
                double charHeight = (GlyphUtils.GlyphTypeface.Baseline * _fontSize);
                double adjunctWidth = (parentMetrics.BoundingBox.Width + adjunctGlyphInfo.Width) / 2;
                switch (direction)
                {
                    //all addition in this routine is *vector* addition.
                    //We are not adding absolute X and Y values
                    case CompassPoints.East:
                    default:
                        adjunctCenter = parentMetrics.Geocenter + BasicGeometry.ScreenEast * adjunctWidth;
                        break;

                    case CompassPoints.North:
                        adjunctCenter = parentMetrics.Geocenter +
                                        BasicGeometry.ScreenNorth * charHeight;
                        break;

                    case CompassPoints.West:
                        if (subscriptInfo != null)
                        {
                            adjunctCenter = parentMetrics.Geocenter + (BasicGeometry.ScreenWest *
                                                                       (adjunctWidth + subscriptInfo.Value.Width));
                        }
                        else
                        {
                            adjunctCenter = parentMetrics.Geocenter + (BasicGeometry.ScreenWest * (adjunctWidth));
                        }
                        break;

                    case CompassPoints.South:
                        adjunctCenter = parentMetrics.Geocenter +
                                        BasicGeometry.ScreenSouth * charHeight;
                        break;
                }
                return adjunctCenter;
            }
        }

        #endregion Text Classes

        #endregion Nested Classes

        public AtomVisual(Atom atom, bool showInColour, bool showImplicitHydrogens, bool showAllCarbons) : this()
        {
            ParentAtom = atom;
            Position = atom.Position;
            AtomSymbol = ShowAllCarbons ? ParentAtom.Element.Symbol : ParentAtom.SymbolText;
            Charge = ParentAtom.FormalCharge;
            ImplicitHydrogenCount = ParentAtom.ImplicitHydrogenCount;
            Isotope = ParentAtom.IsotopeNumber;
            ShowInColour = showInColour;
            ShowImplicitHydrogens = showImplicitHydrogens;
            ShowAllCarbons = showAllCarbons;
        }

        public AtomVisual()
        {
        }

        #region Properties

        public virtual Atom ParentAtom { get; protected set; }

        #region Visual Properties

        public double SymbolSize { get; set; }
        public Point Position { get; set; }
        public string AtomSymbol { get; set; }
        public Brush BackgroundColor { get; set; }
        public Brush Fill { get; set; }
        public int? Charge { get; set; }
        public int? Isotope { get; set; }
        public int ImplicitHydrogenCount { get; set; }

        public virtual List<Point> Hull { get; protected set; }

        #endregion Visual Properties

        #endregion Properties

        #region Methods

        #region Rendering

        /// <summary>
        ///
        /// </summary>
        /// <param name="drawingContext"></param>
        /// <param name="mainAtomMetrics"></param>
        /// <param name="hMetrics"></param>
        /// <param name="isoMetrics"></param>
        /// <param name="defaultHOrientation"></param>
        /// <returns></returns>
        private LabelMetrics DrawCharges(DrawingContext drawingContext,
            AtomTextMetrics mainAtomMetrics,
            AtomTextMetrics hMetrics,
            LabelMetrics isoMetrics,
            CompassPoints defaultHOrientation)
        {
            var chargeString = AtomHelpers.GetChargeString(Charge);
            var chargeText = DrawChargeOrRadical(drawingContext, mainAtomMetrics, hMetrics, isoMetrics, chargeString, Fill, defaultHOrientation);
            chargeText.TextMetrics.FlattenedPath = chargeText.TextRun.GetOutline();
            return chargeText.TextMetrics;
        }

        /// <param name="drawingContext"></param>
        /// <param name="mainAtomMetrics">
        /// </param>
        /// <param name="hMetrics">
        /// </param>
        /// <param name="isoMetrics">
        /// </param>
        /// <param name="chargeString"></param>
        /// <param name="fill"></param>
        /// <param name="defaultHOrientation"></param>
        /// <returns></returns>
        /// <summary>
        /// Draws a charge or radical label at the given point
        /// </summary>
        /// <returns></returns>
        private ChargeLabelText DrawChargeOrRadical(DrawingContext drawingContext, AtomTextMetrics mainAtomMetrics, AtomTextMetrics hMetrics, LabelMetrics isoMetrics, string chargeString, Brush fill, CompassPoints defaultHOrientation)
        {
            ChargeLabelText chargeText = new ChargeLabelText(chargeString, PixelsPerDip(), SuperscriptSize);

            //try to place the charge at 2 o clock to the atom
            Vector chargeOffset = BasicGeometry.ScreenNorth * SymbolSize * 0.9;
            RotateUntilClear(mainAtomMetrics, hMetrics, isoMetrics, chargeOffset, chargeText, out var chargeCenter, defaultHOrientation);
            chargeText.MeasureAtCenter(chargeCenter);
            chargeText.Fill = fill;
            chargeText.DrawAtBottomLeft(chargeText.TextMetrics.BoundingBox.BottomLeft, drawingContext);
            return chargeText;
        }

        private static void RotateUntilClear(AtomTextMetrics mainAtomMetrics, AtomTextMetrics hMetrics, LabelMetrics isoMetrics,
            Vector labelOffset, GlyphText labelText, out Point labelCenter, CompassPoints defHOrientation)
        {
            Matrix rotator = new Matrix();
            double angle = Globals.ClockDirections.II.ToDegrees();
            rotator.Rotate(angle);

            labelOffset = labelOffset * rotator;
            Rect bb = new Rect();
            Rect bb2 = new Rect();
            if (hMetrics != null)
            {
                bb = hMetrics.TotalBoundingBox;
            }
            if (isoMetrics != null)
            {
                bb2 = isoMetrics.BoundingBox;
            }
            labelCenter = mainAtomMetrics.Geocenter + labelOffset;
            labelText.MeasureAtCenter(labelCenter);

            double increment;
            if (defHOrientation == CompassPoints.East)
            {
                increment = -10;
            }
            else
            {
                increment = 10;
            }
            while (labelText.CollidesWith(mainAtomMetrics.TotalBoundingBox, bb, bb2)
                   && Math.Abs(angle - 30) > 0.001)
            {
                rotator = new Matrix();

                angle += increment;
                rotator.Rotate(increment);
                labelOffset = labelOffset * rotator;
                labelCenter = mainAtomMetrics.Geocenter + labelOffset;
                labelText.MeasureAtCenter(labelCenter);
            }
        }

        //draws the isotope label at ten-o-clock
        private LabelMetrics DrawIsotopeLabel(DrawingContext drawingContext, AtomTextMetrics mainAtomMetrics, AtomTextMetrics hMetrics)
        {
            Debug.Assert(Isotope != null);

            string isoLabel = Isotope.ToString();
            var isotopeText = new IsotopeLabelText(isoLabel, PixelsPerDip(), SuperscriptSize);

            Vector isotopeOffsetVector = BasicGeometry.ScreenNorth * SymbolSize;
            Matrix rotator = new Matrix();
            double angle = -60;
            //avoid overlap of label and hydrogens
            if (hMetrics != null && ParentAtom.GetDefaultHOrientation() == CompassPoints.West)
            {
                angle = -35;
            }

            rotator.Rotate(angle);
            isotopeOffsetVector = isotopeOffsetVector * rotator;
            Point isoCenter = mainAtomMetrics.Geocenter + isotopeOffsetVector;
            isotopeText.MeasureAtCenter(isoCenter);
            isotopeText.Fill = Fill;
            isotopeText.DrawAtBottomLeft(isotopeText.TextMetrics.BoundingBox.BottomLeft, drawingContext);
            return isotopeText.TextMetrics;
        }

        //draws the main atom symbol, or an ellipse if necessary
        private AtomTextMetrics DrawSelf(DrawingContext drawingContext, bool measureOnly = false)
        {
            if (AtomSymbol != "")
            {
                var symbolText = new GlyphText(AtomSymbol, SymbolTypeface, SymbolSize, PixelsPerDip());
                symbolText.Fill = Fill;
                symbolText.MeasureAtCenter(Position);
                if (!measureOnly)
                {
                    symbolText.DrawAtBottomLeft(symbolText.TextMetrics.BoundingBox.BottomLeft, drawingContext);
                }

                return symbolText.TextMetrics;
            }
            else
            {
                //so draw a circle
                double radiusX = SymbolSize / 3;

                Rect boundingBox = new Rect(new Point(Position.X - radiusX, Position.Y - radiusX),
                    new Point(Position.X + radiusX, Position.Y + radiusX));
                return new AtomTextMetrics
                {
                    BoundingBox = boundingBox,
                    Geocenter = Position,
                    TotalBoundingBox = boundingBox,
                    FlattenedPath = new List<Point>
                        {boundingBox.BottomLeft, boundingBox.TopLeft, boundingBox.TopRight, boundingBox.BottomRight}
                };
            }
        }

        private void RenderAtomSymbol(DrawingContext drawingContext)
        {
            //renders the atom complete with charges, hydrogens and labels.
            //this code is *complex* - alter it at your own risk!

            List<Point> hydrogenPoints;

            //private variables used to keep track of onscreen visuals
            AtomTextMetrics hydrogenMetrics = null;
            LabelMetrics isoMetrics = null;
            SubscriptedGroup subscriptedGroup;
            Hull = new List<Point>();

            //stage 1:  measure up the main atom symbol in position
            //we need the metrics first
            if (AtomSymbol != "")
            {
                var symbolText = new GlyphText(AtomSymbol,
                    SymbolTypeface, SymbolSize, PixelsPerDip());
                symbolText.MeasureAtCenter(Position);
                //grab the hull for later
                if (symbolText.FlattenedPath != null)
                {
                    Hull.AddRange(symbolText.FlattenedPath);
                }
            }

            //stage 2.  grab the main atom metrics br drawing it

            var mainAtomMetrics = DrawSelf(drawingContext);
            //if it's a vertex atom we need the hull
            if (AtomSymbol == "")
            {
                Hull.AddRange(mainAtomMetrics.FlattenedPath);
            }

            //stage 3:  measure up the hydrogens
            //if we have implicit hydrogens and we have an explicit label, draw them
            if (ShowImplicitHydrogens && ImplicitHydrogenCount > 0 && AtomSymbol != "")
            {
                var defaultHOrientation = ParentAtom.GetDefaultHOrientation();

                subscriptedGroup = new SubscriptedGroup(ImplicitHydrogenCount, "H", SymbolSize);
                hydrogenMetrics = subscriptedGroup.Measure(mainAtomMetrics, defaultHOrientation, PixelsPerDip());

                subscriptedGroup.DrawSelf(drawingContext, hydrogenMetrics, PixelsPerDip(), Fill);
                hydrogenPoints = hydrogenMetrics.FlattenedPath;
                Hull.AddRange(hydrogenPoints);
            }

            //stage 6:  draw an isotope label if needed
            if (Isotope != null)
            {
                isoMetrics = DrawIsotopeLabel(drawingContext, mainAtomMetrics, hydrogenMetrics);
                Hull.AddRange(isoMetrics.Corners);
            }

            //stage7:  draw any charges
            if ((Charge ?? 0) != 0)
            {
                LabelMetrics cMetrics = DrawCharges(drawingContext, mainAtomMetrics, hydrogenMetrics, isoMetrics, ParentAtom.GetDefaultHOrientation());
                Hull.AddRange(cMetrics.FlattenedPath);
            }

            //stage 8:  recalculate the hull
            if (Hull.Any())
            {
                //sort the points properly before doing a hull calculation
                var sortedHull = (from Point p in Hull
                                  orderby p.X, p.Y descending
                                  select p).ToList();

                Hull = Geometry<Point>.GetHull(sortedHull, p => p);

                // Diag: Show the Hull
#if DEBUG
#if SHOWHULLS
                ShowHull(Hull, drawingContext);
#endif
#endif
                // End Diag
            }
            // Diag: Show the Atom Point
#if DEBUG
#if SHOWATOMCENTRES
            drawingContext.DrawEllipse(Brushes.Red, null, ParentAtom.Position, 5, 5);
#endif
#endif
            // End Diag
        }

#if DEBUG

        private void ShowHull(List<Point> points, DrawingContext drawingContext)
        {
            var path = BasicGeometry.BuildPath(points);
            // Diag: Show the Hull or it's Points
#if SHOWHULLS
            drawingContext.DrawGeometry(null, new Pen(new SolidColorBrush(Colors.GreenYellow), 0.01), path.Data);
            //ShowPoints(Hull, drawingContext);
#endif
            // End Diag
        }

        public void ShowPoints(List<Point> points, DrawingContext drawingContext)
        {
            // Show points for debugging
            SolidColorBrush firstPoint = new SolidColorBrush(Colors.Red);
            SolidColorBrush otherPoints = new SolidColorBrush(Colors.Blue);
            SolidColorBrush lastPoint = new SolidColorBrush(Colors.Green);
            int i = 0;
            int max = points.Count - 1;
            foreach (var point in points)
            {
                if (i > 0 && i < max)
                {
                    drawingContext.DrawEllipse(otherPoints, null, point, 1, 1);
                }
                if (i == 0)
                {
                    drawingContext.DrawEllipse(firstPoint, null, point, 1, 1);
                }
                if (i == max)
                {
                    drawingContext.DrawEllipse(lastPoint, null, point, 1, 1);
                }
                i++;
            }
        }

#endif

        /// <summary>
        /// Draws the atom and all associated decorations
        /// </summary>
        public override void Render()
        {
            if (ParentAtom.Element is Element e)
            {
                using (DrawingContext dc = RenderOpen())
                {
                    Fill = ShowInColour
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(e.Colour))
                        : new SolidColorBrush(Colors.Black);

                    var atomSymbol = ParentAtom.SymbolText;
                    if (ShowAllCarbons)
                    {
                        atomSymbol = ParentAtom.Element.Symbol;
                        AtomSymbol = ParentAtom.Element.Symbol;
                    }

                    //if it's over bonded draw the warning circle
                    if (DisplayOverbonding && ParentAtom.Overbonded)
                    {
                        double radius = ParentAtom.Parent.Model.XamlBondLength * Globals.FontSizePercentageBond / 2;
                        EllipseGeometry eg = new EllipseGeometry(ParentAtom.Position, radius, radius);

                        Brush warningFill = new SolidColorBrush(Colors.Salmon);
                        warningFill.Opacity = 0.75;

                        dc.DrawGeometry(warningFill, new Pen(new SolidColorBrush(Colors.OrangeRed), Globals.BondThickness), eg);
                    }

                    if (atomSymbol == "")
                    {
                        //draw an empty circle for hit testing purposes
                        EllipseGeometry eg = new EllipseGeometry(ParentAtom.Position, Globals.AtomRadius, Globals.AtomRadius);

                        dc.DrawGeometry(Brushes.Transparent, new Pen(Brushes.Transparent, 1.0), eg);
                        //very simple hull definition
                        Hull = new List<Point>();

                        Hull.AddRange(new[] { eg.Bounds.BottomLeft, eg.Bounds.TopLeft, eg.Bounds.TopRight, eg.Bounds.BottomRight });
                        dc.Close();
                    }
                    else
                    {
                        RenderAtomSymbol(dc);
#if DEBUG
#if SHOWHULLS
                        // Diag: Show the convex hull
                        if (AtomSymbol != "")
                        {
                            dc.DrawGeometry(null, new Pen(new SolidColorBrush(Colors.GreenYellow), 1.0), WidenedHullGeometry);
                        }
                        // End Diag
#endif
#endif

                        dc.Close();
                    }
                }
            }
        }

        #endregion Rendering

        #region Helpers

        public float PixelsPerDip()
        {
            return (float)VisualTreeHelper.GetDpi(this).PixelsPerDip;
        }

        #endregion Helpers

        #endregion Methods

        public virtual Geometry HullGeometry
        {
            get
            {
                if (Hull != null && Hull.Count != 0)
                {
                    Geometry geo1 = BasicGeometry.BuildPolyPath(Hull);
                    CombinedGeometry cg = new CombinedGeometry(geo1,
                                                               geo1.GetWidenedPathGeometry(new Pen(Brushes.Black, Standoff)));
                    return cg;
                }
                return Geometry.Empty;
            }
        }

        public Rect Bounds
        {
            get
            {
                var myBounds = ContentBounds;
                if (Children.Count > 0)
                {
                    myBounds.Union(((FunctionalGroupVisual)Children[0]).ContentBounds);
                }

                return myBounds;
            }
        }

        public virtual Geometry WidenedHullGeometry
        {
            get
            {
                if (!string.IsNullOrEmpty(AtomSymbol))
                {
                    return HullGeometry;
                }

                return null;
            }
        }

        public bool DisplayOverbonding { get; set; }
        public double Standoff { get; set; }
        public double SubscriptSize { get; set; }
        public double SuperscriptSize { get; set; }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            if (ParentAtom.Element is Element)
            {
                if (HullGeometry.FillContains(hitTestParameters.HitPoint))
                {
                    return new PointHitTestResult(this, hitTestParameters.HitPoint);
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the intersection point of a line with the Convex Hull
        /// </summary>
        /// <param name="start">Start point of line</param>
        /// <param name="end">End point of line</param>
        /// <returns>Point? defining the crossing point</returns>
        public Point? GetIntersection(Point start, Point end)
        {
            for (int i = 0; i < Hull.Count; i++)
            {
                Point? p;
                if ((p = BasicGeometry.LineSegmentsIntersect(start, end, Hull[i], Hull[(i + 1) % Hull.Count])) != null)
                {
                    return p;
                }
            }
            return null;
        }
    }
}