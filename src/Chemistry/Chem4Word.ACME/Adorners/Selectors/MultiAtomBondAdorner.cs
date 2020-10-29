using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Drawing;
using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.ACME.Adorners.Selectors
{
    /// <summary>
    /// Creates a single Adorner to span all selected atoms and bonds
    /// </summary>
    public class MultiAtomBondAdorner : MultiChemistryAdorner
    {
        public CombinedGeometry OverallGeometry { get; }
        private double RenderRadius => (EditViewModel.Model.XamlBondLength * Globals.FontSizePercentageBond) / 4;

        public MultiAtomBondAdorner(EditorCanvas currentEditor, List<ChemistryBase> chemistries) : base(currentEditor, chemistries)
        {
            //first, create an empty PathGeometry to hold the summed-up atoms and bonds
            OverallGeometry = new CombinedGeometry();

            //iterate through each of the adorned atoms and bonds
            foreach (ChemistryBase adornedChemistry in AdornedChemistries)
            {
                switch (adornedChemistry)
                {
                    case Atom a:
                        OverallGeometry = new CombinedGeometry(GeometryCombineMode.Union, OverallGeometry, GetAtomAdornerGeometry(a));
                        break;

                    case Bond b:
                        Debug.Assert(b.Parent != null);
                        OverallGeometry = new CombinedGeometry(GeometryCombineMode.Union, OverallGeometry, GetBondAdornerGeometry(b));
                        break;
                }
            }

            //enable detection of mouse clicks:
            IsHitTestVisible = true;
        }

        private Geometry GetBondAdornerGeometry(Bond bond)
        {
            //work out the actual visible extent of the bond
            Point startPoint, endPoint;
            Vector unitVector = bond.BondVector;
            unitVector.Normalize();

            AtomVisual startAtomVisual = CurrentEditor.GetAtomVisual(bond.StartAtom);
            AtomVisual endAtomVisual = CurrentEditor.GetAtomVisual(bond.EndAtom);

            Point? sp;
            //work out where the bond vector intersects the start and end points of the bond
            if (!string.IsNullOrEmpty(bond.StartAtom.SymbolText)
                && (sp = startAtomVisual.GetIntersection(bond.StartAtom.Position, bond.EndAtom.Position)) != null)
            {
                startPoint = sp.Value;
            }
            else
            {
                startPoint = bond.StartAtom.Position + unitVector * RenderRadius;
            }

            Point? ep;
            if (!string.IsNullOrEmpty(bond.EndAtom.SymbolText)
                && (ep = endAtomVisual.GetIntersection(bond.StartAtom.Position, bond.EndAtom.Position)) != null)
            {
                endPoint = ep.Value;
            }
            else
            {
                endPoint = bond.EndAtom.Position - unitVector * RenderRadius;
            }

            //get the perpendiculars to the bond
            Matrix toLeft = new Matrix();
            toLeft.Rotate(-90);
            Matrix toRight = new Matrix();
            toRight.Rotate(90);

            Vector right = bond.BondVector * toRight;
            right.Normalize();
            Vector left = bond.BondVector * toLeft;
            left.Normalize();

            //draw the rectangle on top of the bond
            PathFigure pathFigure = new PathFigure();
            pathFigure.StartPoint = startPoint + right * RenderRadius;
            pathFigure.IsClosed = true;

            LineSegment lineSegment1 = new LineSegment();
            lineSegment1.Point = startPoint + left * RenderRadius;
            pathFigure.Segments.Add(lineSegment1);

            LineSegment lineSegment2 = new LineSegment();
            lineSegment2.Point = endPoint + left * RenderRadius;
            pathFigure.Segments.Add(lineSegment2);

            LineSegment lineSegment3 = new LineSegment();
            lineSegment3.Point = endPoint + right * RenderRadius;
            pathFigure.Segments.Add(lineSegment3);

            //add in the figure
            List<PathFigure> figures = new List<PathFigure>();
            figures.Add(pathFigure);

            //now create the geometry
            PathGeometry pathGeometry = new PathGeometry(figures);

            //work out the end caps
            Geometry start = new EllipseGeometry(startPoint, RenderRadius, RenderRadius);
            Geometry end = new EllipseGeometry(endPoint, RenderRadius, RenderRadius);

            //add them in
            CombinedGeometry result = new CombinedGeometry(GeometryCombineMode.Union, pathGeometry, start);
            result = new CombinedGeometry(GeometryCombineMode.Union, result, end);

            //and return
            return result;
        }

        private Geometry GetAtomAdornerGeometry(Atom atom)
        {
            if (atom.SymbolText == "")
            {
                return new EllipseGeometry(atom.Position, RenderRadius * 1.5, RenderRadius * 1.5);
            }
            else
            {
                return CurrentEditor.GetAtomVisual(atom).HullGeometry;
            }
        }

        #region Overrides

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            SolidColorBrush renderBrush = (SolidColorBrush)FindResource(Globals.AtomBondSelectorBrush);
            drawingContext.DrawGeometry(renderBrush, null, OverallGeometry);
        }

        #endregion Overrides
    }
}