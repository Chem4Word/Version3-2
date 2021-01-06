// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
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
    public class GroupVisual : ChemicalVisual
    {
        private const double MainAreaOpacity = 0.05;

        public Rect BoundingBox { get; }
        public Molecule ParentMolecule { get; }
        public bool ShowInColour { get; set; }

        public GroupVisual(Molecule parent, bool showInColour, Rect? boundingBox = null)
        {
            ParentMolecule = parent;
            BoundingBox = boundingBox ?? ParentMolecule.BoundingBox;
            ShowInColour = showInColour;
            Render();
        }

        public override void Render()
        {
            //first work out the angle bracket size
            double bondLength = ParentMolecule.Model.XamlBondLength;
            double bracketLength = Globals.BracketFactor * bondLength;
            //now work out the main area
            Brush mainArea = new SolidColorBrush(Colors.Gray);
            mainArea.Opacity = MainAreaOpacity;
            Brush bracketBrush = ShowInColour ? new SolidColorBrush(Globals.GroupBracketColor) : new SolidColorBrush(Colors.Black);
            Pen bracketPen = new Pen(bracketBrush, Globals.BracketThickness);
            bracketPen.StartLineCap = PenLineCap.Round;
            bracketPen.EndLineCap = PenLineCap.Round;

            using (DrawingContext dc = RenderOpen())
            {
                var inflateFactor = ParentMolecule.Model.XamlBondLength * Globals.GroupInflateFactor;
                var bb = BoundingBox;

                bb.Inflate(new Size(inflateFactor, inflateFactor));

                dc.DrawRectangle(mainArea, null, bb);
                Vector right = new Vector(bracketLength, 0);
                Vector left = -right;
                Vector down = new Vector(0, bracketLength);
                Vector up = -down;

                dc.DrawLine(bracketPen, bb.BottomLeft, bb.BottomLeft + right);
                dc.DrawLine(bracketPen, bb.BottomLeft, bb.BottomLeft + up);

                dc.DrawLine(bracketPen, bb.BottomRight, bb.BottomRight + left);
                dc.DrawLine(bracketPen, bb.BottomRight, bb.BottomRight + up);

                dc.DrawLine(bracketPen, bb.TopLeft, bb.TopLeft + right);
                dc.DrawLine(bracketPen, bb.TopLeft, bb.TopLeft + down);

                dc.DrawLine(bracketPen, bb.TopRight, bb.TopRight + left);
                dc.DrawLine(bracketPen, bb.TopRight, bb.TopRight + down);
                dc.Close();
            }
        }
    }
}