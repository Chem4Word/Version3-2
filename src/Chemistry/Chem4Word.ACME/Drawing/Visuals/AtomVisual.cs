// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Drawing.Text;
using Chem4Word.ACME.Utils;
using Chem4Word.Core.Enums;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using Chem4Word.Model2.Geometry;
using Chem4Word.Model2.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using static Chem4Word.ACME.Drawing.Text.GlyphUtils;
using Geometry = System.Windows.Media.Geometry;

namespace Chem4Word.ACME.Drawing.Visuals
{
    /// <summary>
    /// DrawingVisual based class for rendering an Atom label
    /// </summary>
    public class AtomVisual : ChemicalVisual
    {
        public HydrogenVisual HydrogenChildVisual { get; set; }
        private ChargeVisual ChargeChildVisual { get; set; }
        public IsotopeVisual IsotopeChildVisual { get; set; }
        private List<Visual> children = new List<Visual>();

        #region Fields

        public bool ShowInColour { get; set; }
        public bool ShowImplicitHydrogens { get; set; }
        public bool ShowAllCarbons { get; set; }

        #endregion Fields

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
        private string AtomSymbol { get; set; }
        public Brush BackgroundColor { get; set; }
        public Brush Fill { get; set; }
        public int? Charge { get; set; }
        public int? Isotope { get; set; }
        public int ImplicitHydrogenCount { get; set; }
        public CompassPoints HydrogenOrientation { get; set; }

        protected List<Point> CoreHull { get; set; }

        /// <summary>
        /// Returns a list of points corresponding to the combined hulls of all label parts
        /// </summary>
        public virtual List<Point> Hull
        {
            get
            {
                List<Point> tempHull = new List<Point>(CoreHull);
                if (HydrogenChildVisual != null)
                {
                    tempHull.AddRange(HydrogenChildVisual.FlattenedPath);
                }

                if (IsotopeChildVisual != null)
                {
                    tempHull.AddRange(IsotopeChildVisual.Metrics.Corners);
                }

                if (ChargeChildVisual != null)
                {
                    tempHull.AddRange(ChargeChildVisual.Metrics.Corners);
                }
                var sortedHull = (from Point p in tempHull
                                  orderby p.X, p.Y descending
                                  select p).ToList();

                return Geometry<Point>.GetHull(sortedHull, p => p);
            }
        }

        #endregion Visual Properties

        #endregion Properties

        #region Methods

        #region Rendering

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

        private void RenderAsSymbol(DrawingContext drawingContext)
        {
            //renders the atom complete with charges, hydrogens and labels.
            //this code is *complex* - alter it at your own risk!

            //private variables used to keep track of onscreen visuals
            CoreHull = new List<Point>();

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
                    CoreHull.AddRange(symbolText.FlattenedPath);
                }
            }

            //stage 2.  grab the main atom metrics br drawing it

            var mainAtomMetrics = DrawSelf(drawingContext);
            //if it's a vertex atom we need the hull
            if (AtomSymbol == "")
            {
                CoreHull.AddRange(mainAtomMetrics.FlattenedPath);
            }

            //stage 3:  measure up the hydrogens
            //if we have implicit hydrogens and we have an explicit label, draw them
            if (ShowImplicitHydrogens && ImplicitHydrogenCount > 0 && AtomSymbol != "")
            {
                HydrogenOrientation = ParentAtom.ImplicitHPlacement;

                HydrogenChildVisual = new HydrogenVisual(this, mainAtomMetrics, ImplicitHydrogenCount, SymbolSize, drawingContext);
                HydrogenChildVisual.Render();
                AddVisualChild(HydrogenChildVisual);
                children.Add(HydrogenChildVisual);
            }

            //stage 6:  draw an isotope label if needed
            if (Isotope != null)
            {
                IsotopeChildVisual = new IsotopeVisual(this,
                                                       drawingContext,
                                                       mainAtomMetrics,
                                                       HydrogenChildVisual?.Metrics);
                IsotopeChildVisual.Render();
                AddVisualChild(IsotopeChildVisual);
                children.Add(IsotopeChildVisual);
            }

            //stage7:  draw any charges
            if ((Charge ?? 0) != 0)
            {
                ChargeChildVisual = new ChargeVisual(this, drawingContext, mainAtomMetrics, HydrogenChildVisual?.Metrics);
                ChargeChildVisual.Render();
                AddVisualChild(ChargeChildVisual);
                children.Add(ChargeChildVisual);
            }

            // Diag: Show the Hull
#if DEBUG
#if SHOWHULLS
                ShowHull(Hull, drawingContext);
#endif
            // End Diag

            // Diag: Show the Atom Point
#if SHOWATOMCENTRES
            drawingContext.DrawEllipse(Brushes.Red, null, ParentAtom.Position, 5, 5);
#endif
#endif
            // End Diag
        }

#if DEBUG

        private void ShowHull(List<Point> points, DrawingContext drawingContext)
        {
            var path = Utils.Geometry.BuildPath(points);
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

                        dc.DrawGeometry(warningFill, new Pen(new SolidColorBrush(Colors.OrangeRed), Common.BondThickness), eg);
                    }

                    if (atomSymbol == "")
                    {
                        //draw an empty circle for hit testing purposes
                        RenderAsVertex(dc);
                    }
                    else
                    {
                        RenderAsSymbol(dc);
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

        /// <summary>
        /// Renders a 'vertex' carbon atom
        /// (draws a transparent circle around it for hit testing)
        /// </summary>
        /// <param name="dc">DrawingContext to render the atom to</param>
        private void RenderAsVertex(DrawingContext dc)
        {
            EllipseGeometry eg = new EllipseGeometry(ParentAtom.Position, Common.AtomRadius, Common.AtomRadius);

            dc.DrawGeometry(Brushes.Transparent, new Pen(Brushes.Transparent, 1.0), eg);
            //very simple hull definition
            CoreHull = new List<Point>();

            CoreHull.AddRange(new[] { eg.Bounds.BottomLeft, eg.Bounds.TopLeft, eg.Bounds.TopRight, eg.Bounds.BottomRight });
            dc.Close();
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
                    Geometry geo1 = Utils.Geometry.BuildPolyPath(Hull);
                    CombinedGeometry cg = new CombinedGeometry(geo1,
                                                               geo1.GetWidenedPathGeometry(new Pen(Brushes.Black, Standoff)));
                    return cg;
                }
                return Geometry.Empty;
            }
        }

        public virtual Rect Bounds
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
                if (HydrogenChildVisual != null && HydrogenChildVisual.HullGeometry.FillContains(hitTestParameters.HitPoint))
                {
                    return new PointHitTestResult(HydrogenChildVisual, hitTestParameters.HitPoint);
                }

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
                if ((p = GeometryTool.GetIntersection(start, end, Hull[i], Hull[(i + 1) % Hull.Count])) != null)
                {
                    return p;
                }
            }
            return null;
        }
    }
}