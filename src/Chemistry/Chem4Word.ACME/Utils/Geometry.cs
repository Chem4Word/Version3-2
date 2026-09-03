// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Chem4Word.ACME.Utils
{
    public static class Geometry
    {
        public static bool Overlaps(Molecule molecule, List<Point> placements, List<Atom> excludeAtoms = null)
        {
            PathGeometry area = OverlapArea(molecule, placements);

            if (area.GetArea() >= 0.01)
            {
                return true;
            }

            List<Atom> chainAtoms = molecule.Atoms.Values.Where(a => !a.Rings.Any()).ToList();
            if (excludeAtoms != null)
            {
                foreach (Atom excludeAtom in excludeAtoms)
                {
                    if (chainAtoms.Contains(excludeAtom))
                    {
                        chainAtoms.Remove(excludeAtom);
                    }
                }
            }

            var placementsArea = BuildPath(placements).Data;
            foreach (Atom chainAtom in chainAtoms)
            {
                if (placementsArea.FillContains(chainAtom.Position, 0.01, ToleranceType.Relative))
                {
                    return true;
                }
            }

            return false;
        }

        public static PathGeometry OverlapArea(Molecule molecule, List<Point> placements)
        {
            PathGeometry ringsGeo = null;
            foreach (Ring r in molecule.Rings)
            {
                Path ringHull = BuildPath(r.Traverse().Select(a => a.Position).ToList());
                if (ringsGeo == null)
                {
                    ringsGeo = ringHull.Data.GetOutlinedPathGeometry();
                }
                else
                {
                    var hull = ringHull.Data;
                    PathGeometry hullGeo = hull.GetOutlinedPathGeometry();
                    ringsGeo = new CombinedGeometry(GeometryCombineMode.Union, ringsGeo, hullGeo)
                        .GetOutlinedPathGeometry();
                }
            }

            Path otherGeo = BuildPath(placements);

            PathGeometry val1 = ringsGeo;
            if (val1 != null)
            {
                val1.FillRule = FillRule.EvenOdd;
            }

            PathGeometry val2 = otherGeo.Data.GetOutlinedPathGeometry();
            val2.FillRule = FillRule.EvenOdd;

            PathGeometry overlap = new CombinedGeometry(GeometryCombineMode.Intersect, val1, val2).GetOutlinedPathGeometry();
            return overlap;
        }

        /// <summary>
        /// Takes a list of points and builds a  Path object from it.
        /// Generally used for constructing masks
        /// </summary>
        /// <param name="hull">List of points making up the path </param>
        /// <param name="isClosed"></param>
        /// <returns></returns>
        public static Path BuildPath(List<Point> hull, bool isClosed = true)
        {
            var points = hull.ToArray();

            Path path = new Path
            {
                StrokeThickness = 0.0,
            };

            if (points.Length == 0)
            {
                return path;
            }

            PathSegmentCollection pathSegments = new PathSegmentCollection();
            for (int i = 1; i < points.Length; i++)
            {
                pathSegments.Add(new LineSegment(points[i], true));
            }
            path.Data = new PathGeometry
            {
                Figures = new PathFigureCollection
                                      {
                                          new PathFigure
                                          {
                                              StartPoint = points[0],
                                              Segments = pathSegments,
                                              IsClosed = isClosed,
                                              IsFilled = true
                                          }
                                      }
            };

            return path;
        }

        /// <summary>
        /// Takes a list of points and builds a  StreamGeometry object from it.
        /// Generally used for constructing masks
        /// </summary>
        /// <param name="hull"></param>
        /// <param name="isClosed"></param>
        /// <returns></returns>
        public static StreamGeometry BuildPolyPath(List<Point> hull, bool isClosed = true)
        {
            var points = hull.ToArray();
            StreamGeometry geo = new StreamGeometry();
            using (StreamGeometryContext c = geo.Open())
            {
                c.BeginFigure(points[0], true, isClosed);
                c.PolyLineTo(points.Skip(1).ToArray(), true, true);
                c.Close();
            }

            return geo;
        }

        public static System.Windows.Media.Geometry CreateGeometry(DrawingGroup drawingGroup)
        {
            var geometry = new GeometryGroup();

            foreach (var drawing in drawingGroup.Children)
            {
                if (drawing is GeometryDrawing geometryDrawing)
                {
                    geometry.Children.Add(geometryDrawing.Geometry);
                }
                else if (drawing is GlyphRunDrawing runDrawing)
                {
                    geometry.Children.Add(runDrawing.GlyphRun.BuildGeometry());
                }
                else if (drawing is DrawingGroup dg)
                {
                    geometry.Children.Add(CreateGeometry(dg));
                }
            }

            geometry.Transform = drawingGroup.Transform;
            return geometry;
        }

        public static void CombineGeometries(DrawingGroup drawingGroup, GeometryCombineMode combineMode, ref CombinedGeometry combinedGeometry)
        {
            if (combinedGeometry == null)
            {
                combinedGeometry = new CombinedGeometry();
            }

            DrawingCollection drawingCollection = drawingGroup.Children;

            foreach (System.Windows.Media.Drawing drawing in drawingCollection)
            {
                if (drawing is DrawingGroup dg)
                {
                    CombineGeometries(dg, combineMode, ref combinedGeometry);
                }
                else if (drawing is GeometryDrawing geoDrawing)
                {
                    combinedGeometry = new CombinedGeometry(combineMode, combinedGeometry, geoDrawing.Geometry);
                }
            }
        }

        public static void DrawGeometry(StreamGeometryContext ctx, System.Windows.Media.Geometry geo)
        {
            var pathGeometry = geo as PathGeometry ?? PathGeometry.CreateFromGeometry(geo);
            foreach (var figure in pathGeometry.Figures)
            {
                DrawFigure(ctx, figure);
            }
        }

        public static void DrawFigure(StreamGeometryContext ctx, PathFigure figure)
        {
            ctx.BeginFigure(figure.StartPoint, figure.IsFilled, figure.IsClosed);
            foreach (var segment in figure.Segments)
            {
                switch (segment)
                {
                    case LineSegment lineSegment:
                        ctx.LineTo(lineSegment.Point, lineSegment.IsStroked, lineSegment.IsSmoothJoin);
                        break;

                    case BezierSegment bezierSegment:
                        ctx.BezierTo(bezierSegment.Point1, bezierSegment.Point2, bezierSegment.Point3, bezierSegment.IsStroked, bezierSegment.IsSmoothJoin);
                        break;

                    case QuadraticBezierSegment quadraticSegment:
                        ctx.QuadraticBezierTo(quadraticSegment.Point1, quadraticSegment.Point2, quadraticSegment.IsStroked, quadraticSegment.IsSmoothJoin);
                        break;

                    case PolyLineSegment polyLineSegment:
                        ctx.PolyLineTo(polyLineSegment.Points, polyLineSegment.IsStroked, polyLineSegment.IsSmoothJoin);
                        break;

                    case PolyBezierSegment polyBezierSegment:
                        ctx.PolyBezierTo(polyBezierSegment.Points, polyBezierSegment.IsStroked, polyBezierSegment.IsSmoothJoin);
                        break;

                    case PolyQuadraticBezierSegment polyQuadraticSegment:
                        ctx.PolyQuadraticBezierTo(polyQuadraticSegment.Points, polyQuadraticSegment.IsStroked, polyQuadraticSegment.IsSmoothJoin);
                        break;

                    case ArcSegment arcSegment:
                        ctx.ArcTo(arcSegment.Point, arcSegment.Size, arcSegment.RotationAngle, arcSegment.IsLargeArc, arcSegment.SweepDirection, arcSegment.IsStroked, arcSegment.IsSmoothJoin);
                        break;
                }
            }
        }

        public static Point[] GetIntersectionPoints(System.Windows.Media.Geometry g1, System.Windows.Media.Geometry g2)
        {
            System.Windows.Media.Geometry og1 = g1.GetWidenedPathGeometry(new Pen(Brushes.Black, 1.0));
            System.Windows.Media.Geometry og2 = g2.GetWidenedPathGeometry(new Pen(Brushes.Black, 1.0));
            CombinedGeometry cg = new CombinedGeometry(GeometryCombineMode.Intersect, og1, og2);
            PathGeometry pg = cg.GetFlattenedPathGeometry();
            Point[] result = new Point[pg.Figures.Count];
            for (int i = 0; i < pg.Figures.Count; i++)
            {
                Rect fig = new PathGeometry(new PathFigure[] { pg.Figures[i] }).Bounds;
                result[i] = new Point(fig.Left + fig.Width / 2.0, fig.Top + fig.Height / 2.0);
            }
            return result;
        }
    }
}