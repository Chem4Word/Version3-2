// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Media;
using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.ACME.Drawing
{
    /// <summary>
    /// Draws a bracket around a molecule
    /// with charges and counts
    /// </summary>
    public class MoleculeVisual : ChemicalVisual
    {
        private readonly Molecule _molecule;
        private Rect _boundingBox;

        public MoleculeVisual(Molecule molecule, Rect? bb = null)
        {
            _molecule = molecule;
            _boundingBox = bb ?? molecule.BoundingBox;
        }

        public override void Render()
        {
            //draw the bracket first
            var bb = _boundingBox;
            var serifSize = _molecule.Model.XamlBondLength * Globals.BracketFactor;
            bb.Inflate(new Size(serifSize, serifSize));
            Vector horizontal = new Vector(serifSize, 0.0);
            Brush bracketBrush = new SolidColorBrush(Colors.Black);
            Pen bracketPen = new Pen(bracketBrush, Globals.BracketThickness);
            StreamGeometry sg = new StreamGeometry();

            using (DrawingContext dc = RenderOpen())
            {
                using (StreamGeometryContext sgc = sg.Open())
                {
                    //left bracket
                    sgc.BeginFigure(bb.BottomLeft + horizontal, false, false);
                    sgc.LineTo(bb.BottomLeft, true, true);
                    sgc.LineTo(bb.TopLeft, true, true);
                    sgc.LineTo(bb.TopLeft + horizontal, true, true);
                    //right bracket
                    sgc.BeginFigure(bb.BottomRight - horizontal, false, false);
                    sgc.LineTo(bb.BottomRight, true, true);
                    sgc.LineTo(bb.TopRight, true, true);
                    sgc.LineTo(bb.TopRight - horizontal, true, true);
                    sgc.Close();
                }

                dc.DrawGeometry(bracketBrush, bracketPen, sg);

                //now draw the charges and radicals
                string chargeString = AtomHelpers.GetChargeString(_molecule.FormalCharge);

                if (_molecule.SpinMultiplicity.HasValue && _molecule.SpinMultiplicity.Value > 1)
                {
                    // Append SpinMultiplicity
                    switch (_molecule.SpinMultiplicity.Value)
                    {
                        case 2:
                            chargeString += "•";
                            break;

                        case 3:
                            chargeString += "••";
                            break;
                    }
                }

                if (chargeString != "")
                {
                    Point pos = bb.TopRight +
                                 horizontal;
                    var mlv = new MoleculeLabelVisual(chargeString, pos, bracketBrush, _molecule.Model.XamlBondLength / 2.0d);
                    mlv.Render(dc);
                    AddVisualChild(mlv);
                }

                string countString = _molecule.Count.ToString();
                if (!string.IsNullOrEmpty(countString))
                {
                    Point pos = bb.BottomRight +
                                horizontal;
                    var mlv = new MoleculeLabelVisual(countString, pos, bracketBrush, _molecule.Model.XamlBondLength / 2.0d);
                    mlv.Render(dc, true);
                    AddVisualChild(mlv);
                }
            }
        }

        public float PixelsPerDip()
        {
            return (float)VisualTreeHelper.GetDpi(this).PixelsPerDip;
        }
    }
}