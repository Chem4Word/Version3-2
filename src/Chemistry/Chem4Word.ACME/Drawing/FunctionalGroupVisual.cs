// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using Chem4Word.Model2;
using Chem4Word.Model2.Geometry;

namespace Chem4Word.ACME.Drawing
{
    public class FunctionalGroupVisual : AtomVisual
    {
        private List<Point> _sortedOutline;
        public FunctionalGroup ParentGroup { get; }
        public bool Flipped => ParentAtom.FunctionalGroupPlacement == CompassPoints.West;

        public override List<Point> Hull { get; protected set; }

        public FunctionalGroupVisual(Atom parent, bool showInColour)
            : base(parent, showInColour, true, true)
        {
            ParentGroup = (FunctionalGroup)parent.Element;
        }

        public override void Render()
        {
            Render(ParentAtom.Position, ShowInColour ? ParentGroup.Colour : "#000000");
        }

        private void Render(Point location, string colour)
        {
            int textStorePosition = 0;
            bool flipped;

            Position = location;
            if (ParentAtom == null)
            {
                flipped = false;
            }
            else
            {
                flipped = Flipped;
            }

            var textStore = new FunctionalGroupTextSource(ParentGroup, colour, flipped)
            {
                SymbolSize = SymbolSize,
                SubscriptSize = SubscriptSize,
                SuperscriptSize = SuperscriptSize
            };

            //main textformatter - this does the writing of the visual
            using (TextFormatter textFormatter = TextFormatter.Create())
            {
                //set up the default paragraph properties
                var paraprops = new FunctionalGroupTextSource.GenericTextParagraphProperties(
                    FlowDirection.LeftToRight,
                    TextAlignment.Left,
                    true,
                    false,
                    new LabelTextRunProperties(colour, SymbolSize),
                    TextWrapping.NoWrap,
                    SymbolSize,
                    0d);

                var anchorRuns = textStore.Runs.Where(f => f.IsAnchor);
                string anchorString = string.Empty;
                foreach (var run in anchorRuns)
                {
                    anchorString += run.Text;
                }

                using (TextLine myTextLine =
                    textFormatter.FormatLine(textStore, textStorePosition, 999, paraprops, null))
                {
                    IList<TextBounds> textBounds;
                    Rect firstRect = Rect.Empty;
                    if (!Flipped) //isolate them at the beginning
                    {
                        textBounds = myTextLine.GetTextBounds(0, anchorString.Length);
                    }
                    else
                    {
                        //isolate them at the end
                        var start = myTextLine.Length - 1 - anchorString.Length;
                        textBounds = myTextLine.GetTextBounds(start, anchorString.Length);
                    }

                    //add all the bounds together
                    foreach (TextBounds anchorBound in textBounds)
                    {
                        firstRect.Union(anchorBound.Rectangle);
                    }

                    //center will be position close to the origin 0,0
                    Point center = new Point((firstRect.Left + firstRect.Right) / 2,
                                             (firstRect.Top + firstRect.Bottom) / 2);

                    //the displacement vector will be added to each relative coordinate for the glyph run
                    var displacementVector = location - center;

                    //locus is where the text line is drawn
                    var locus = new Point(0, 0) + displacementVector;

                    textBounds = myTextLine.GetTextBounds(0, 999);
                    var obb = textBounds[0].Rectangle;

                    //draw the line of text
                    using (DrawingContext dc = RenderOpen())
                    {
                        myTextLine.Draw(dc, locus, InvertAxes.None);
#if DEBUG
#if SHOWBOUNDS
                        obb.Offset(new Vector(locus.X, locus.Y));
                        dc.DrawRectangle(null, new Pen(new SolidColorBrush(Colors.BlueViolet), 1.0), obb);
#endif
#endif
                        var glyphRuns = myTextLine.GetIndexedGlyphRuns();
                        List<Point> outline = new List<Point>();
                        double advanceWidths = 0d;

                        //build up the convex hull from each glyph
                        //you need to add in the advance widths for each
                        //glyph run as they are traversed,
                        //to the outline
                        foreach (IndexedGlyphRun igr in glyphRuns)
                        {
                            var originalRun = textStore.GetTextRun(igr.TextSourceCharacterIndex);
                            var currentRun = igr.GlyphRun;
                            //need to work out how much the current run has been offset from the baseline
                            var runBounds =
                                myTextLine.GetTextBounds(igr.TextSourceCharacterIndex, igr.TextSourceLength);
                            //get the bounding rect
                            var rect = runBounds[0].TextRunBounds[0].Rectangle;

                            //it's relative to the baseline
                            //adjust it
                            rect.Offset(new Vector(locus.X, locus.Y));
                            var rectCopy = rect;
#if DEBUG
#if SHOWBOUNDS
                            dc.DrawRectangle(null, new Pen(new SolidColorBrush(Colors.DarkOrange), 1.0), rect);
#endif
#endif
                            var runOutline = GlyphUtils.GetOutline(currentRun);
                            //need to see if the run has been super or sub-scripted
                            var variants = originalRun.Properties.TypographyProperties.Variants;

                            if (variants == FontVariants.Subscript || variants == FontVariants.Superscript)
                            {
                                //simply union in the rect -it's easier!
                                outline.AddRange(new[]
                                                 {
                                                     rectCopy.BottomLeft, rectCopy.BottomRight, rectCopy.TopLeft,
                                                     rectCopy.TopRight
                                                 });
                            }
                            else
                            {
                                //add in the points from the convex hull
                                for (int i = 0; i < runOutline.Count; i++)
                                {
                                    var point = runOutline[i] + displacementVector +
                                                new Vector(0.0, myTextLine.Baseline);
                                    point.X += advanceWidths;
                                    runOutline[i] = point;
                                }

                                outline.AddRange(runOutline);
                            }

                            advanceWidths += currentRun.AdvanceWidths.Sum();
                        }

                        _sortedOutline = (from Point p in outline
                                          orderby p.X ascending, p.Y descending
                                          select p).ToList();

                        Hull = Geometry<Point>.GetHull(_sortedOutline, p => p);
                        // Diag: Show Hulls or Atom centres
#if DEBUG
#if SHOWHULLS
                        dc.DrawGeometry(null, new Pen(Brushes.GreenYellow, thickness: 1), HullGeometry);
#endif
#if SHOWATOMCENTRES
                        dc.DrawEllipse(Brushes.Red, null, ParentAtom.Position, 5, 5);
#endif
#endif
                        // End Diag
                        dc.Close();
                    }
                }
            }
        }

        public override Geometry HullGeometry
        {
            get
            {
                //need to combine the actually filled atom area
                //with a stroked outline of it, to give a sufficient margin
                Geometry geo1 = BasicGeometry.BuildPolyPath(Hull);
                CombinedGeometry cg = new CombinedGeometry(geo1, geo1.GetWidenedPathGeometry(new Pen(Brushes.Black, Standoff)));
                return cg;
            }
        }

        public override Geometry WidenedHullGeometry
        {
            get
            {
                return HullGeometry;
            }
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            if (this.HullGeometry.FillContains(hitTestParameters.HitPoint))
            {
                return new PointHitTestResult(this, hitTestParameters.HitPoint);
            }
            return null;
        }
    }
}