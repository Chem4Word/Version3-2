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
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Model2;
using Chem4Word.Model2.Geometry;
using Chem4Word.Model2.Helpers;
using Chem4Word.Renderer.OoXmlV4.Entities;
using Chem4Word.Renderer.OoXmlV4.Entities.Diagnostic;
using Chem4Word.Renderer.OoXmlV4.Enums;
using Chem4Word.Renderer.OoXmlV4.TTF;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace Chem4Word.Renderer.OoXmlV4.OOXML
{
    public class OoXmlPositioner
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private PositionerInputs Inputs { get; }
        private PositionerOutputs Outputs { get; } = new PositionerOutputs();

        private const double EPSILON = 1e-4;

        public OoXmlPositioner(PositionerInputs inputs) => Inputs = inputs;

        /// <summary>
        /// Carries out the following
        /// 1. Position Atom Label Characters
        /// 2. Position Bond Lines
        /// 3. Position Brackets
        /// 4. Position Molecule Label Characters
        /// 5. Shrink Bond Lines
        /// </summary>
        /// <returns>PositionerOutputs a class to hold all of the required output types</returns>
        public PositionerOutputs Position()
        {
            int moleculeNo = 0;

            foreach (Molecule mol in Inputs.Model.Molecules.Values)
            {
                // Steps 1 .. 4
                ProcessMolecule(mol, Inputs.Progress, ref moleculeNo);
            }

            // 5.   Shrink Bond Lines
            if (Inputs.Options.ClipLines)
            {
                #region Step 4 - Shrink bond lines

                ShrinkBondLinesPass1(Inputs.Progress);
                ShrinkBondLinesPass2(Inputs.Progress);

                #endregion Step 4 - Shrink bond lines
            }

            return Outputs;
        }

        private void ShrinkBondLinesPass1(Progress pb)
        {
            // so that they do not overlap label characters

            if (Outputs.ConvexHulls.Count > 1)
            {
                pb.Show();
            }
            pb.Message = "Clipping Bond Lines - Pass 1";
            pb.Value = 0;
            pb.Maximum = Outputs.ConvexHulls.Count;

            foreach (var hull in Outputs.ConvexHulls)
            {
                pb.Increment(1);

                // select lines which start or end with this atom
                var targeted = from l in Outputs.BondLines
                               where (l.StartAtomPath == hull.Key || l.EndAtomPath == hull.Key)
                               select l;

                foreach (BondLine bl in targeted.ToList())
                {
                    Point start = new Point(bl.Start.X, bl.Start.Y);
                    Point end = new Point(bl.End.X, bl.End.Y);

                    bool outside;
                    var r = GeometryTool.ClipLineWithPolygon(start, end, hull.Value, out outside);

                    switch (r.Length)
                    {
                        case 3:
                            if (outside)
                            {
                                bl.Start = new Point(r[0].X, r[0].Y);
                                bl.End = new Point(r[1].X, r[1].Y);
                            }
                            else
                            {
                                bl.Start = new Point(r[1].X, r[1].Y);
                                bl.End = new Point(r[2].X, r[2].Y);
                            }
                            break;

                        case 2:
                            if (!outside)
                            {
                                // This line is totally inside so remove it!
                                Outputs.BondLines.Remove(bl);
                            }
                            break;
                    }
                }
            }
        }

        private void ShrinkBondLinesPass2(Progress pb)
        {
            // so that they do not overlap label characters

            if (Outputs.AtomLabelCharacters.Count > 1)
            {
                pb.Show();
            }
            pb.Message = "Clipping Bond Lines - Pass 2";
            pb.Value = 0;
            pb.Maximum = Outputs.AtomLabelCharacters.Count;

            foreach (AtomLabelCharacter alc in Outputs.AtomLabelCharacters)
            {
                pb.Increment(1);

                double width = OoXmlHelper.ScaleCsTtfToCml(alc.Character.Width, Inputs.MeanBondLength);
                double height = OoXmlHelper.ScaleCsTtfToCml(alc.Character.Height, Inputs.MeanBondLength);

                if (alc.IsSubScript)
                {
                    // Shrink bounding box
                    width = width * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR;
                    height = height * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR;
                }

                // Create rectangle of the bounding box with a suitable clipping margin
                Rect cbb = new Rect(alc.Position.X - OoXmlHelper.CML_CHARACTER_MARGIN,
                    alc.Position.Y - OoXmlHelper.CML_CHARACTER_MARGIN,
                    width + (OoXmlHelper.CML_CHARACTER_MARGIN * 2),
                    height + (OoXmlHelper.CML_CHARACTER_MARGIN * 2));

                // Just in case we end up splitting a line into two
                List<BondLine> extraBondLines = new List<BondLine>();

                // Select Lines which may require trimming
                // By using LINQ to implement the following SQL
                // Where (L.Right Between Cbb.Left And Cbb.Right)
                //    Or (L.Left Between Cbb.Left And Cbb.Right)
                //    Or (L.Top Between Cbb.Top And Cbb.Botton)
                //    Or (L.Bottom Between Cbb.Top And Cbb.Botton)

                var targeted = from l in Outputs.BondLines
                               where (cbb.Left <= l.BoundingBox.Right && l.BoundingBox.Right <= cbb.Right)
                                     || (cbb.Left <= l.BoundingBox.Left && l.BoundingBox.Left <= cbb.Right)
                                     || (cbb.Top <= l.BoundingBox.Top && l.BoundingBox.Top <= cbb.Bottom)
                                     || (cbb.Top <= l.BoundingBox.Bottom && l.BoundingBox.Bottom <= cbb.Bottom)
                               select l;
                targeted = targeted.ToList();

                foreach (BondLine bl in targeted)
                {
                    Point start = new Point(bl.Start.X, bl.Start.Y);
                    Point end = new Point(bl.End.X, bl.End.Y);

                    int attempts = 0;
                    if (CohenSutherland.ClipLine(cbb, ref start, ref end, out attempts))
                    {
                        bool bClipped = false;

                        if (Math.Abs(bl.Start.X - start.X) < EPSILON && Math.Abs(bl.Start.Y - start.Y) < EPSILON)
                        {
                            bl.Start = new Point(end.X, end.Y);
                            bClipped = true;
                        }
                        if (Math.Abs(bl.End.X - end.X) < EPSILON && Math.Abs(bl.End.Y - end.Y) < EPSILON)
                        {
                            bl.End = new Point(start.X, start.Y);
                            bClipped = true;
                        }

                        if (!bClipped && bl.Bond != null)
                        {
                            // Only convert to two bond lines if not wedge or hatch
                            bool ignoreWedgeOrHatch = bl.Bond.Order == Globals.OrderSingle
                                                      && bl.Bond.Stereo == Globals.BondStereo.Wedge
                                                        || bl.Bond.Stereo == Globals.BondStereo.Hatch;
                            if (!ignoreWedgeOrHatch)
                            {
                                // Line was clipped at both ends
                                // 1. Generate new line
                                BondLine extraLine = new BondLine(bl.Style, new Point(end.X, end.Y), new Point(bl.End.X, bl.End.Y), bl.Bond);
                                extraBondLines.Add(extraLine);
                                // 2. Trim existing line
                                bl.End = new Point(start.X, start.Y);
                            }
                        }
                    }
                    if (attempts >= 15)
                    {
                        Debug.WriteLine("Clipping failed !");
                    }
                }

                // Add any extra lines generated by this character into the List of Bond Lines
                foreach (BondLine bl in extraBondLines)
                {
                    Outputs.BondLines.Add(bl);
                }
            }
        }

        private void ProcessMolecule(Molecule mol, Progress pb, ref int molNumber)
        {
            molNumber++;

            // 1. Position Atom Label Characters
            ProcessAtoms(mol, pb, molNumber);

            // 2. Position Bond Lines
            ProcessBonds(mol, pb, molNumber);

            // Populate diagnostic data
            foreach (Ring ring in mol.Rings)
            {
                if (ring.Centroid.HasValue)
                {
                    var centre = ring.Centroid.Value;
                    Outputs.RingCenters.Add(centre);

                    var innerCircle = new InnerCircle();
                    // Traverse() obtains list of atoms in anti-clockwise direction around ring
                    innerCircle.Points.AddRange(ring.Traverse().Select(a => a.Position).ToList());
                    innerCircle.Centre = centre;
                    //Outputs.InnerCircles.Add(innerCircle)
                }
            }

            // Recurse into any child molecules
            foreach (var child in mol.Molecules.Values)
            {
                ProcessMolecule(child, pb, ref molNumber);
            }

            // 3 Determine Extents

            // Atoms <= InternalCharacters <= GroupBrackets <= MoleculesBrackets <= ExternalCharacters

            // 3.1. Atoms & InternalCharacters
            var thisMoleculeExtents = new MoleculeExtents(mol.Path, mol.BoundingBox);
            thisMoleculeExtents.SetInternalCharacterExtents(CharacterExtents(mol, thisMoleculeExtents.AtomExtents));
            Outputs.AllMoleculeExtents.Add(thisMoleculeExtents);

            // 3.2. Grouped Molecules
            if (mol.IsGrouped)
            {
                Rect boundingBox = Rect.Empty;

                var childGroups = Outputs.AllMoleculeExtents.Where(g => g.Path.StartsWith($"{mol.Path}/")).ToList();
                foreach (var child in childGroups)
                {
                    boundingBox.Union(child.ExternalCharacterExtents);
                }

                if (boundingBox != Rect.Empty)
                {
                    boundingBox.Union(thisMoleculeExtents.ExternalCharacterExtents);
                    if (Inputs.Options.ShowMoleculeGrouping)
                    {
                        boundingBox = Inflate(boundingBox, OoXmlHelper.BracketOffset(Inputs.MeanBondLength));
                        Outputs.GroupBrackets.Add(boundingBox);
                    }
                    thisMoleculeExtents.SetGroupBracketExtents(boundingBox);
                }
            }

            // 3.3 Add required Brackets
            bool showBrackets = mol.ShowMoleculeBrackets.HasValue && mol.ShowMoleculeBrackets.Value
                                || mol.Count.HasValue && mol.Count.Value > 0
                                || mol.FormalCharge.HasValue && mol.FormalCharge.Value != 0
                                || mol.SpinMultiplicity.HasValue && mol.SpinMultiplicity.Value > 1;

            var rect = thisMoleculeExtents.GroupBracketsExtents;
            var children = Outputs.AllMoleculeExtents.Where(g => g.Path.StartsWith($"{mol.Path}/")).ToList();
            foreach (var child in children)
            {
                rect.Union(child.GroupBracketsExtents);
            }

            if (showBrackets)
            {
                rect = Inflate(rect, OoXmlHelper.BracketOffset(Inputs.MeanBondLength));
                Outputs.MoleculeBrackets.Add(rect);
            }
            thisMoleculeExtents.SetMoleculeBracketExtents(rect);

            TtfCharacter hydrogenCharacter = Inputs.TtfCharacterSet['H'];

            string characters = string.Empty;

            if (mol.FormalCharge.HasValue && mol.FormalCharge.Value != 0)
            {
                // Add FormalCharge at top right
                int charge = mol.FormalCharge.Value;
                int absCharge = Math.Abs(charge);

                string chargeSign = Math.Sign(charge) > 0 ? "+" : "-";
                characters = absCharge == 1 ? chargeSign : $"{absCharge}{chargeSign}";
            }

            if (mol.SpinMultiplicity.HasValue && mol.SpinMultiplicity.Value > 1)
            {
                // Append SpinMultiplicity
                switch (mol.SpinMultiplicity.Value)
                {
                    case 2:
                        characters += "•";
                        break;

                    case 3:
                        characters += "••";
                        break;
                }
            }

            if (!string.IsNullOrEmpty(characters))
            {
                // Draw characters at top right (outside of any brackets)
                var point = new Point(thisMoleculeExtents.MoleculeBracketsExtents.Right
                                      + OoXmlHelper.MULTIPLE_BOND_OFFSET_PERCENTAGE * Inputs.MeanBondLength,
                                      thisMoleculeExtents.MoleculeBracketsExtents.Top
                                      + OoXmlHelper.ScaleCsTtfToCml(hydrogenCharacter.Height, Inputs.MeanBondLength) / 2);
                PlaceString(characters, point, mol.Path);
            }

            if (mol.Count.HasValue && mol.Count.Value > 0)
            {
                // Draw Count at bottom right
                var point = new Point(thisMoleculeExtents.MoleculeBracketsExtents.Right
                                      + OoXmlHelper.MULTIPLE_BOND_OFFSET_PERCENTAGE * Inputs.MeanBondLength,
                                      thisMoleculeExtents.MoleculeBracketsExtents.Bottom
                                      + OoXmlHelper.ScaleCsTtfToCml(hydrogenCharacter.Height, Inputs.MeanBondLength) / 2);
                PlaceString($"{mol.Count}", point, mol.Path);
            }

            if (mol.Count.HasValue
                || mol.FormalCharge.HasValue
                || mol.SpinMultiplicity.HasValue)
            {
                // Recalculate as we have just added extra characters
                thisMoleculeExtents.SetExternalCharacterExtents(CharacterExtents(mol, thisMoleculeExtents.MoleculeBracketsExtents));
            }

            // 4. Position Molecule Label Characters
            // Handle optional rendering of molecule labels centered on brackets (if any) and below any molecule property characters
            if (Inputs.Options.ShowMoleculeCaptions && mol.Captions.Any())
            {
                var point = new Point(thisMoleculeExtents.MoleculeBracketsExtents.Left
                                        + thisMoleculeExtents.MoleculeBracketsExtents.Width / 2,
                                      thisMoleculeExtents.ExternalCharacterExtents.Bottom
                                        + Inputs.MeanBondLength * OoXmlHelper.MULTIPLE_BOND_OFFSET_PERCENTAGE / 2);

                if (Inputs.Options.RenderCaptionsAsTextBox)
                {
                    AddMoleculeCaptionsAsTextBox(mol.Captions.ToList(), point, mol.Path);
                    var revisedExtents = thisMoleculeExtents.ExternalCharacterExtents;
                    foreach (var ooXmlString in Outputs.MoleculeCaptions.Where(p => p.ParentMolecule.Equals(mol.Path)))
                    {
                        revisedExtents.Union(ooXmlString.Extents);
                    }
                    thisMoleculeExtents.SetExternalCharacterExtents(revisedExtents);
                }
                else
                {
                    AddMoleculeCaptionsAsCharacters(mol.Captions.ToList(), point, mol.Path);
                    // Recalculate as we have just added extra characters
                    thisMoleculeExtents.SetExternalCharacterExtents(CharacterExtents(mol, thisMoleculeExtents.MoleculeBracketsExtents));
                }
            }
        }

        private Rect Inflate(Rect r, double x)
        {
            Rect r1 = r;
            r1.Inflate(x, x);
            return r1;
        }

        private Rect CharacterExtents(Molecule mol, Rect existing)
        {
            var chars = Outputs.AtomLabelCharacters.Where(m => m.ParentMolecule.StartsWith(mol.Path)).ToList();
            foreach (var c in chars)
            {
                if (c.IsSmaller)
                {
                    Rect r = new Rect(c.Position,
                                      new Size(OoXmlHelper.ScaleCsTtfToCml(c.Character.Width, Inputs.MeanBondLength) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR,
                                               OoXmlHelper.ScaleCsTtfToCml(c.Character.Height, Inputs.MeanBondLength) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR));
                    existing.Union(r);
                }
                else
                {
                    Rect r = new Rect(c.Position,
                                      new Size(OoXmlHelper.ScaleCsTtfToCml(c.Character.Width, Inputs.MeanBondLength),
                                               OoXmlHelper.ScaleCsTtfToCml(c.Character.Height, Inputs.MeanBondLength)));
                    existing.Union(r);
                }
            }

            return existing;
        }

        private void ProcessAtoms(Molecule mol, Progress pb, int moleculeNo)
        {
            // Create Characters
            if (mol.Atoms.Count > 1)
            {
                pb.Show();
            }
            pb.Message = $"Processing Atoms in Molecule {moleculeNo}";
            pb.Value = 0;
            pb.Maximum = mol.Atoms.Count;

            foreach (Atom atom in mol.Atoms.Values)
            {
                pb.Increment(1);
                if (atom.Element is Element)
                {
                    CreateElementCharacters(atom);
                }

                if (atom.Element is FunctionalGroup)
                {
                    CreateFunctionalGroupCharacters(atom);
                }
            }
        }

        private void ProcessBonds(Molecule mol, Progress pb, int moleculeNo)
        {
            if (mol.Bonds.Count > 0)
            {
                pb.Show();
            }
            pb.Message = $"Processing Bonds in Molecule {moleculeNo}";
            pb.Value = 0;
            pb.Maximum = mol.Bonds.Count;

            foreach (Bond bond in mol.Bonds)
            {
                pb.Increment(1);
                CreateLines(bond);
            }

            // Rendering molecular sketches for publication quality output
            // Alex M Clark
            // Implement beautification of semi open double bonds and double bonds touching rings

            // Obtain list of Double Bonds with Placement of BondDirection.None
            List<Bond> doubleBonds = mol.Bonds.Where(b => b.OrderValue.HasValue && b.OrderValue.Value == 2 && b.Placement == Globals.BondDirection.None).ToList();
            if (doubleBonds.Count > 0)
            {
                pb.Message = $"Processing Double Bonds in Molecule {moleculeNo}";
                pb.Value = 0;
                pb.Maximum = doubleBonds.Count;

                foreach (Bond bond in doubleBonds)
                {
                    BeautifyLines(bond.StartAtom, bond.Path);
                    BeautifyLines(bond.EndAtom, bond.Path);
                }
            }
        }

        private void BeautifyLines(Atom atom, string bondPath)
        {
            if (atom.Element is Element element
                && element == Globals.PeriodicTable.C
                && atom.Bonds.ToList().Count == 3)
            {
                bool isInRing = atom.IsInRing;
                List<BondLine> lines = Outputs.BondLines.Where(bl => bl.BondPath.Equals(bondPath)).ToList();
                if (lines.Any())
                {
                    List<Bond> otherLines;
                    if (isInRing)
                    {
                        otherLines = atom.Bonds.Where(b => !b.Path.Equals(bondPath)).ToList();
                    }
                    else
                    {
                        otherLines = atom.Bonds.Where(b => !b.Path.Equals(bondPath) && b.Order.Equals(Globals.OrderSingle)).ToList();
                    }

                    if (lines.Count == 2 && otherLines.Count == 2)
                    {
                        BondLine line1 = Outputs.BondLines.First(bl => bl.BondPath.Equals(otherLines[0].Path));
                        BondLine line2 = Outputs.BondLines.First(bl => bl.BondPath.Equals(otherLines[1].Path));
                        TrimLines(lines, line1, line2, isInRing);
                    }
                }
            }
        }

        private void TrimLines(List<BondLine> mainPair, BondLine line1, BondLine line2, bool isInRing)
        {
            // Only two of these calls are expected to do anything
            if (!TrimLine(mainPair[0], line1, isInRing))
            {
                TrimLine(mainPair[0], line2, isInRing);
            }
            // Only two of these calls are expected to do anything
            if (!TrimLine(mainPair[1], line1, isInRing))
            {
                TrimLine(mainPair[1], line2, isInRing);
            }
        }

        private bool TrimLine(BondLine leftOrRight, BondLine line, bool isInRing)
        {
            bool dummy;
            bool intersect;
            Point intersection;

            // Make a longer version of the line
            Point startLonger = new Point(leftOrRight.Start.X, leftOrRight.Start.Y);
            Point endLonger = new Point(leftOrRight.End.X, leftOrRight.End.Y);
            CoordinateTool.AdjustLineAboutMidpoint(ref startLonger, ref endLonger, Inputs.MeanBondLength / 5);

            // See if they intersect at one end
            CoordinateTool.FindIntersection(startLonger, endLonger, line.Start, line.End,
                out dummy, out intersect, out intersection);

            // If they intersect update the main line
            if (intersect)
            {
                double l1 = CoordinateTool.DistanceBetween(intersection, leftOrRight.Start);
                double l2 = CoordinateTool.DistanceBetween(intersection, leftOrRight.End);
                if (l1 > l2)
                {
                    leftOrRight.End = new Point(intersection.X, intersection.Y);
                }
                else
                {
                    leftOrRight.Start = new Point(intersection.X, intersection.Y);
                }
                if (!isInRing)
                {
                    l1 = CoordinateTool.DistanceBetween(intersection, line.Start);
                    l2 = CoordinateTool.DistanceBetween(intersection, line.End);
                    if (l1 > l2)
                    {
                        line.End = new Point(intersection.X, intersection.Y);
                    }
                    else
                    {
                        line.Start = new Point(intersection.X, intersection.Y);
                    }
                }
            }

            return intersect;
        }

        private void CreateElementCharacters(Atom atom)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            string atomLabel = atom.SymbolText;
            if (Inputs.Options.ShowCarbons)
            {
                if (atom.Element is Element e)
                {
                    if (e == Globals.PeriodicTable.C)
                    {
                        atomLabel = e.Symbol;
                    }
                }
            }

            if (!string.IsNullOrEmpty(atomLabel))
            {
                #region Set Up Atom Colour

                string atomColour = "000000";
                if (Inputs.Options.ColouredAtoms
                    && atom.Element.Colour != null)
                {
                    atomColour = atom.Element.Colour;
                    // Strip out # as OoXml does not use it
                    atomColour = atomColour.Replace("#", "");
                }

                #endregion Set Up Atom Colour

                // 1. Create main character group
                GroupOfCharacters main = new GroupOfCharacters(atom.Position, atom.Path, atom.Parent.Path,
                                                               Inputs.TtfCharacterSet, Inputs.MeanBondLength);
                main.AddString(atomLabel, atomColour);
                main.AdjustPosition(atom.Position - main.Centre);

                // 2. Create other character groups

                // 2.1 Determine position of implicit hydrogens if any
                var orientation = CompassPoints.East;
                if (!atom.Singleton)
                {
                    double angleFromNorth = Vector.AngleBetween(BasicGeometry.ScreenNorth, atom.BalancingVector(true));
                    orientation = atom.Bonds.ToList().Count == 1 ? BasicGeometry.SnapTo2EW(angleFromNorth) : BasicGeometry.SnapTo4NESW(angleFromNorth);
                }

                // 2.2 Implicit Hydrogens
                GroupOfCharacters hydrogens = null;

                int implicitHCount = atom.ImplicitHydrogenCount;
                if (implicitHCount > 0)
                {
                    hydrogens = new GroupOfCharacters(atom.Position, atom.Path, atom.Parent.Path,
                                                      Inputs.TtfCharacterSet, Inputs.MeanBondLength);

                    // Create characters
                    hydrogens.AddCharacter('H', atomColour);
                    if (implicitHCount > 1)
                    {
                        foreach (var character in implicitHCount.ToString())
                        {
                            hydrogens.AddCharacter(character, atomColour, true);
                        }
                    }

                    // Adjust position of block
                    switch (orientation)
                    {
                        case CompassPoints.North:
                            hydrogens.AdjustPosition(main.NorthCentre - hydrogens.SouthCentre);
                            break;

                        case CompassPoints.East:
                            hydrogens.AdjustPosition(main.BoundingBox.TopRight - hydrogens.BoundingBox.TopLeft);
                            break;

                        case CompassPoints.South:
                            hydrogens.AdjustPosition(main.SouthCentre - hydrogens.NorthCentre);
                            break;

                        case CompassPoints.West:
                            hydrogens.AdjustPosition(main.BoundingBox.TopLeft - hydrogens.BoundingBox.TopRight);
                            break;
                    }
                    hydrogens.Nudge(orientation);
                }

                // 2.3 Charge
                GroupOfCharacters charge = null;

                int chargeValue = atom.FormalCharge ?? 0;
                int absCharge = Math.Abs(chargeValue);

                if (absCharge > 0)
                {
                    charge = new GroupOfCharacters(atom.Position, atom.Path, atom.Parent.Path,
                                                      Inputs.TtfCharacterSet, Inputs.MeanBondLength);

                    // Create characters
                    string chargeSign = Math.Sign(chargeValue) > 0 ? "+" : "-";
                    string digits = absCharge == 1 ? chargeSign : $"{absCharge}{chargeSign}";

                    foreach (var character in digits)
                    {
                        charge.AddCharacter(character, atomColour, true);
                    }

                    // Adjust position of charge
                    if (hydrogens == null)
                    {
                        charge.AdjustPosition(main.BoundingBox.TopRight - charge.WestCentre);
                        charge.Nudge(CompassPoints.East);
                    }
                    else
                    {
                        var destination = main.BoundingBox.TopRight;

                        switch (orientation)
                        {
                            case CompassPoints.North:
                                if (hydrogens.BoundingBox.Right >= main.BoundingBox.Right)
                                {
                                    destination.X = hydrogens.BoundingBox.Right;
                                }
                                charge.AdjustPosition(destination - charge.WestCentre);
                                charge.Nudge(CompassPoints.East);
                                break;

                            case CompassPoints.East:
                                charge.AdjustPosition(destination - charge.SouthCentre);
                                charge.Nudge(CompassPoints.North);
                                break;

                            case CompassPoints.South:
                            case CompassPoints.West:
                                charge.AdjustPosition(destination - charge.WestCentre);
                                charge.Nudge(CompassPoints.East);
                                break;
                        }
                    }
                }

                // 2.4 Isotope
                GroupOfCharacters isotope = null;

                int isoValue = atom.IsotopeNumber ?? 0;

                if (isoValue > 0)
                {
                    isotope = new GroupOfCharacters(atom.Position, atom.Path, atom.Parent.Path,
                                                    Inputs.TtfCharacterSet, Inputs.MeanBondLength);

                    foreach (var character in isoValue.ToString())
                    {
                        isotope.AddCharacter(character, atomColour, true);
                    }

                    // Adjust position of isotope
                    if (hydrogens == null)
                    {
                        isotope.AdjustPosition(main.BoundingBox.TopLeft - isotope.EastCentre);
                        isotope.Nudge(CompassPoints.East);
                    }
                    else
                    {
                        var destination = main.BoundingBox.TopLeft;

                        switch (orientation)
                        {
                            case CompassPoints.North:
                                if (hydrogens.BoundingBox.Left <= main.BoundingBox.Left)
                                {
                                    destination.X = hydrogens.BoundingBox.Left;
                                }
                                isotope.AdjustPosition(destination - isotope.EastCentre);
                                isotope.Nudge(CompassPoints.West);
                                break;

                            case CompassPoints.East:
                            case CompassPoints.South:
                                isotope.AdjustPosition(destination - isotope.EastCentre);
                                isotope.Nudge(CompassPoints.West);
                                break;

                            case CompassPoints.West:
                                isotope.AdjustPosition(destination - isotope.SouthCentre);
                                isotope.Nudge(CompassPoints.North);
                                break;
                        }
                    }
                }

                // 3 transfer to output
                foreach (var character in main.Characters)
                {
                    Outputs.AtomLabelCharacters.Add(character);
                }
                //Outputs.Diagnostics.Rectangles.Add(new DiagnosticRectangle(main.BoundingBox, "00e050"))

                if (hydrogens != null)
                {
                    foreach (var character in hydrogens.Characters)
                    {
                        Outputs.AtomLabelCharacters.Add(character);
                    }
                    //Outputs.Diagnostics.Rectangles.Add(new DiagnosticRectangle(hydrogens.BoundingBox, "ff80ff"))
                }

                if (charge != null)
                {
                    foreach (var character in charge.Characters)
                    {
                        Outputs.AtomLabelCharacters.Add(character);
                    }
                    //Outputs.Diagnostics.Rectangles.Add(new DiagnosticRectangle(charge.BoundingBox, "ff0000"))
                }

                if (isotope != null)
                {
                    foreach (var character in isotope.Characters)
                    {
                        Outputs.AtomLabelCharacters.Add(character);
                    }
                    //Outputs.Diagnostics.Rectangles.Add(new DiagnosticRectangle(isotope.BoundingBox, "0070c0"))
                }

                // 4 Generate Convex Hull
                Outputs.ConvexHulls.Add(atom.Path, ConvexHull(atom.Path));
            }
        }

        private void CreateFunctionalGroupCharacters(Atom atom)
        {
            FunctionalGroup fg = atom.Element as FunctionalGroup;
            bool reverse = atom.FunctionalGroupPlacement == CompassPoints.West;

            #region Set Up Atom Colour

            string atomColour = "000000";
            if (Inputs.Options.ColouredAtoms
                && fg?.Colour != null)
            {
                atomColour = fg.Colour;
                // Strip out # as OoXml does not use it
                atomColour = atomColour.Replace("#", "");
            }

            #endregion Set Up Atom Colour

            List<FunctionalGroupTerm> terms = fg.ExpandIntoTerms(reverse);

            GroupOfCharacters main = new GroupOfCharacters(atom.Position, atom.Path, atom.Parent.Path,
                                                           Inputs.TtfCharacterSet, Inputs.MeanBondLength);

            GroupOfCharacters auxiliary = new GroupOfCharacters(atom.Position, atom.Path, atom.Parent.Path,
                                                           Inputs.TtfCharacterSet, Inputs.MeanBondLength);

            // 1. Generate characters
            foreach (var term in terms)
            {
                foreach (var part in term.Parts)
                {
                    foreach (char c in part.Text)
                    {
                        if (term.IsAnchor)
                        {
                            switch (part.Type)
                            {
                                case FunctionalGroupPartType.Normal:
                                    main.AddCharacter(c, atomColour);
                                    break;

                                case FunctionalGroupPartType.Subscript:
                                    main.AddCharacter(c, atomColour, true);
                                    break;

                                case FunctionalGroupPartType.Superscript:
                                    main.AddCharacter(c, atomColour, isSuperScript: true);
                                    break;
                            }
                        }
                        else
                        {
                            switch (part.Type)
                            {
                                case FunctionalGroupPartType.Normal:
                                    auxiliary.AddCharacter(c, atomColour);
                                    break;

                                case FunctionalGroupPartType.Subscript:
                                    auxiliary.AddCharacter(c, atomColour, true);
                                    break;

                                case FunctionalGroupPartType.Superscript:
                                    auxiliary.AddCharacter(c, atomColour, isSuperScript: true);
                                    break;
                            }
                        }
                    }
                }
            }

            // 2. Position characters
            main.AdjustPosition(atom.Position - main.Centre);
            //Outputs.Diagnostics.Rectangles.Add(new DiagnosticRectangle(main.BoundingBox, "00ff00"))

            if (auxiliary.Characters.Any())
            {
                switch (atom.FunctionalGroupPlacement)
                {
                    case CompassPoints.East:
                        auxiliary.AdjustPosition(main.BoundingBox.TopRight - auxiliary.BoundingBox.TopLeft);
                        auxiliary.Nudge(CompassPoints.East);
                        break;

                    case CompassPoints.West:
                        auxiliary.AdjustPosition(main.BoundingBox.TopLeft - auxiliary.BoundingBox.TopRight);
                        auxiliary.Nudge(CompassPoints.West);
                        break;
                }

                //Outputs.Diagnostics.Rectangles.Add(new DiagnosticRectangle(auxiliary.BoundingBox, "ffff00"))
            }

            // 3. Transfer to output
            foreach (var character in main.Characters)
            {
                Outputs.AtomLabelCharacters.Add(character);
            }

            if (auxiliary.Characters.Any())
            {
                foreach (var character in auxiliary.Characters)
                {
                    Outputs.AtomLabelCharacters.Add(character);
                }
            }

            // 4. Generate Convex Hull
            Outputs.ConvexHulls.Add(atom.Path, ConvexHull(atom.Path));
        }

        private void AddMoleculeCaptionsAsCharacters(List<TextualProperty> labels, Point centrePoint, string moleculePath)
        {
            Point measure = new Point(centrePoint.X, centrePoint.Y);

            foreach (var label in labels)
            {
                // 1. Measure string
                var bb = MeasureString(label.Value, measure);

                // 2. Place string characters such that they are hanging below the "line"
                if (bb != Rect.Empty)
                {
                    Point place = new Point(measure.X - bb.Width / 2, measure.Y + (measure.Y - bb.Top));
                    PlaceString(label.Value, place, moleculePath);
                }

                // 3. Move to next line
                measure.Offset(0, bb.Height + Inputs.MeanBondLength * OoXmlHelper.MULTIPLE_BOND_OFFSET_PERCENTAGE / 2);
            }
        }

        private void AddMoleculeCaptionsAsTextBox(List<TextualProperty> labels, Point centrePoint, string moleculePath)
        {
            Point measure = new Point(centrePoint.X, centrePoint.Y);

            // Adjust size to allow for text box to be bigger than the text
            TtfCharacter hydrogenCharacter = Inputs.TtfCharacterSet['H'];

            foreach (var label in labels)
            {
                // 1. Measure string
                var bb = MeasureString(label.Value, measure);

                // Adjustments to take into account text box margins
                bb.Width = bb.Width + OoXmlHelper.ScaleCsTtfToCml(hydrogenCharacter.Width, Inputs.MeanBondLength) * 2.5;
                bb.Height = bb.Height * 1.5;

                // 2. Place string characters such that they are hanging below the "line"
                if (bb != Rect.Empty)
                {
                    Point place = new Point(measure.X - bb.Width / 2, measure.Y);
                    Outputs.MoleculeCaptions.Add(new OoXmlString(new Rect(place, bb.Size), label.Value, moleculePath));
                }

                // 3. Move to next line
                measure.Offset(0, bb.Height + Inputs.MeanBondLength * OoXmlHelper.MULTIPLE_BOND_OFFSET_PERCENTAGE / 2);
            }
        }

        /// <summary>
        /// Creates the lines for a bond
        /// </summary>
        /// <param name="bond"></param>
        private void CreateLines(Bond bond)
        {
            IEnumerable<Ring> rings = bond.Rings;
            int ringCount = 0;
            foreach (Ring r in rings)
            {
                ringCount++;
            }

            Point bondStart = bond.StartAtom.Position;

            Point bondEnd = bond.EndAtom.Position;

            #region Create Bond Line objects

            switch (bond.Order)
            {
                case Globals.OrderZero:
                case "unknown":
                    Outputs.BondLines.Add(new BondLine(BondLineStyle.Zero, bond));
                    break;

                case Globals.OrderPartial01:
                    Outputs.BondLines.Add(new BondLine(BondLineStyle.Half, bond));
                    break;

                case "1":
                case Globals.OrderSingle:
                    switch (bond.Stereo)
                    {
                        case Globals.BondStereo.None:
                            Outputs.BondLines.Add(new BondLine(BondLineStyle.Solid, bond));
                            break;

                        case Globals.BondStereo.Hatch:
                            Outputs.BondLines.Add(new BondLine(BondLineStyle.Hatch, bond));
                            break;

                        case Globals.BondStereo.Wedge:
                            Outputs.BondLines.Add(new BondLine(BondLineStyle.Wedge, bond));
                            break;

                        case Globals.BondStereo.Indeterminate:
                            Outputs.BondLines.Add(new BondLine(BondLineStyle.Wavy, bond));
                            break;

                        default:

                            Outputs.BondLines.Add(new BondLine(BondLineStyle.Solid, bond));
                            break;
                    }
                    break;

                case Globals.OrderPartial12:
                case Globals.OrderAromatic:

                    BondLine onePointFive;
                    BondLine onePointFiveDashed;
                    Point onePointFiveStart;
                    Point onePointFiveEnd;

                    switch (bond.Placement)
                    {
                        case Globals.BondDirection.Clockwise:
                            onePointFive = new BondLine(BondLineStyle.Solid, bond);
                            Outputs.BondLines.Add(onePointFive);
                            onePointFiveDashed = onePointFive.GetParallel(BondOffset());
                            onePointFiveStart = new Point(onePointFiveDashed.Start.X, onePointFiveDashed.Start.Y);
                            onePointFiveEnd = new Point(onePointFiveDashed.End.X, onePointFiveDashed.End.Y);
                            CoordinateTool.AdjustLineAboutMidpoint(ref onePointFiveStart, ref onePointFiveEnd, -(BondOffset() / OoXmlHelper.LINE_SHRINK_PIXELS));
                            onePointFiveDashed = new BondLine(BondLineStyle.Half, onePointFiveStart, onePointFiveEnd, bond);
                            Outputs.BondLines.Add(onePointFiveDashed);
                            break;

                        case Globals.BondDirection.Anticlockwise:
                            onePointFive = new BondLine(BondLineStyle.Solid, bond);
                            Outputs.BondLines.Add(onePointFive);
                            onePointFiveDashed = onePointFive.GetParallel(-BondOffset());
                            onePointFiveStart = new Point(onePointFiveDashed.Start.X, onePointFiveDashed.Start.Y);
                            onePointFiveEnd = new Point(onePointFiveDashed.End.X, onePointFiveDashed.End.Y);
                            CoordinateTool.AdjustLineAboutMidpoint(ref onePointFiveStart, ref onePointFiveEnd, -(BondOffset() / OoXmlHelper.LINE_SHRINK_PIXELS));
                            onePointFiveDashed = new BondLine(BondLineStyle.Half, onePointFiveStart, onePointFiveEnd, bond);
                            Outputs.BondLines.Add(onePointFiveDashed);
                            break;

                        case Globals.BondDirection.None:
                            onePointFive = new BondLine(BondLineStyle.Solid, bond);
                            Outputs.BondLines.Add(onePointFive.GetParallel(-(BondOffset() / 2)));
                            onePointFiveDashed = onePointFive.GetParallel(BondOffset() / 2);
                            onePointFiveDashed.SetLineStyle(BondLineStyle.Half);
                            Outputs.BondLines.Add(onePointFiveDashed);
                            break;
                    }
                    break;

                case "2":
                case Globals.OrderDouble:
                    if (bond.Stereo == Globals.BondStereo.Indeterminate) //crossing bonds
                    {
                        // Crossed lines
                        BondLine d = new BondLine(BondLineStyle.Solid, bondStart, bondEnd, bond);
                        BondLine d1 = d.GetParallel(-(BondOffset() / 2));
                        BondLine d2 = d.GetParallel(BondOffset() / 2);
                        Outputs.BondLines.Add(new BondLine(BondLineStyle.Solid, new Point(d1.Start.X, d1.Start.Y), new Point(d2.End.X, d2.End.Y), bond));
                        Outputs.BondLines.Add(new BondLine(BondLineStyle.Solid, new Point(d2.Start.X, d2.Start.Y), new Point(d1.End.X, d1.End.Y), bond));
                    }
                    else
                    {
                        switch (bond.Placement)
                        {
                            case Globals.BondDirection.Anticlockwise:
                                BondLine da = new BondLine(BondLineStyle.Solid, bond);
                                Outputs.BondLines.Add(da);
                                Outputs.BondLines.Add(PlaceSecondaryLine(da, da.GetParallel(-BondOffset())));
                                break;

                            case Globals.BondDirection.Clockwise:
                                BondLine dc = new BondLine(BondLineStyle.Solid, bond);
                                Outputs.BondLines.Add(dc);
                                Outputs.BondLines.Add(PlaceSecondaryLine(dc, dc.GetParallel(BondOffset())));
                                break;

                                // Local Function
                                BondLine PlaceSecondaryLine(BondLine primaryLine, BondLine secondaryLine)
                                {
                                    var primaryMidpoint = CoordinateTool.GetMidPoint(primaryLine.Start, primaryLine.End);
                                    var secondaryMidpoint = CoordinateTool.GetMidPoint(secondaryLine.Start, secondaryLine.End);

                                    Point startPointa = secondaryLine.Start;
                                    Point endPointa = secondaryLine.End;

                                    Point? centre = null;

                                    bool clip = false;

                                    // Does bond have a primary ring?
                                    if (bond.PrimaryRing != null && bond.PrimaryRing.Centroid != null)
                                    {
                                        // Get angle between bond and vector to primary ring centre
                                        centre = bond.PrimaryRing.Centroid.Value;
                                        var primaryRingVector = primaryMidpoint - centre.Value;
                                        var angle = CoordinateTool.AngleBetween(bond.BondVector, primaryRingVector);

                                        // Does bond have a secondary ring?
                                        if (bond.SubsidiaryRing != null && bond.SubsidiaryRing.Centroid != null)
                                        {
                                            // Get angle between bond and vector to secondary ring centre
                                            var centre2 = bond.SubsidiaryRing.Centroid.Value;
                                            var secondaryRingVector = primaryMidpoint - centre2;
                                            var angle2 = CoordinateTool.AngleBetween(bond.BondVector, secondaryRingVector);

                                            // Get angle in which the offset line has moved with respect to the bond line
                                            var offsetVector = primaryMidpoint - secondaryMidpoint;
                                            var offsetAngle = CoordinateTool.AngleBetween(bond.BondVector, offsetVector);

                                            // If in the same direction as secondary ring centre, use it
                                            if (Math.Sign(angle2) == Math.Sign(offsetAngle))
                                            {
                                                centre = centre2;
                                            }
                                        }

                                        // Is projection to centre at right angles +/- 10 degrees
                                        if (Math.Abs(angle) > 80 && Math.Abs(angle) < 100)
                                        {
                                            clip = true;
                                        }

                                        // Is secondary line outside of the "selected" ring
                                        var distance1 = primaryRingVector.Length;
                                        var distance2 = (secondaryMidpoint - centre.Value).Length;
                                        if (distance2 > distance1)
                                        {
                                            clip = false;
                                        }
                                    }

                                    if (clip)
                                    {
                                        Point outIntersectP1;
                                        Point outIntersectP2;

                                        CoordinateTool.FindIntersection(startPointa, endPointa, bondStart, centre.Value,
                                                                        out _, out _, out outIntersectP1);
                                        CoordinateTool.FindIntersection(startPointa, endPointa, bondEnd, centre.Value,
                                                                        out _, out _, out outIntersectP2);

                                        if (Inputs.Options.ShowBondClippingLines)
                                        {
                                            // Diagnostics
                                            Outputs.Diagnostics.Lines.Add(new DiagnosticLine(bond.StartAtom.Position, centre.Value, BondLineStyle.Dotted, "ff0000"));
                                            Outputs.Diagnostics.Lines.Add(new DiagnosticLine(bond.EndAtom.Position, centre.Value, BondLineStyle.Dotted, "ff0000"));
                                        }

                                        return new BondLine(BondLineStyle.Solid, outIntersectP1, outIntersectP2, bond);
                                    }
                                    else
                                    {
                                        CoordinateTool.AdjustLineAboutMidpoint(ref startPointa, ref endPointa, -(BondOffset() / OoXmlHelper.LINE_SHRINK_PIXELS));
                                        return TrimSecondaryLine(new BondLine(BondLineStyle.Solid, startPointa, endPointa, bond));
                                    }
                                }

                                // Local Function
                                BondLine TrimSecondaryLine(BondLine bondLine)
                                {
                                    var otherStartBonds = bond.StartAtom.Bonds.Except(new[] { bond }).ToList();
                                    var otherEndBonds = bond.EndAtom.Bonds.Except(new[] { bond }).ToList();

                                    foreach (var otherBond in otherStartBonds)
                                    {
                                        TrimSecondaryBondLine(bond.StartAtom.Position, bond.EndAtom.Position,
                                                              otherBond.OtherAtom(bond.StartAtom).Position);
                                    }

                                    foreach (var otherBond in otherEndBonds)
                                    {
                                        TrimSecondaryBondLine(bond.EndAtom.Position, bond.StartAtom.Position,
                                                              otherBond.OtherAtom(bond.EndAtom).Position);
                                    }

                                    void TrimSecondaryBondLine(Point common, Point left, Point right)
                                    {
                                        Vector v1 = left - common;
                                        Vector v2 = right - common;
                                        var angle = CoordinateTool.AngleBetween(v1, v2);
                                        Matrix matrix = new Matrix();
                                        matrix.Rotate(angle / 2);
                                        v1 = v1 * 2 * matrix;
                                        if (Inputs.Options.ShowBondClippingLines)
                                        {
                                            Outputs.Diagnostics.Lines.Add(new DiagnosticLine(common, common + v1, BondLineStyle.Dotted, "0000ff"));
                                        }

                                        bool intersect;
                                        Point meetingPoint;
                                        CoordinateTool.FindIntersection(bondLine.Start, bondLine.End,
                                                                        common, common + v1,
                                                                        out _, out intersect, out meetingPoint);
                                        if (intersect)
                                        {
                                            if (common == bondLine.Bond.StartAtom.Position)
                                            {
                                                bondLine.Start = meetingPoint;
                                            }
                                            if (common == bondLine.Bond.EndAtom.Position)
                                            {
                                                bondLine.End = meetingPoint;
                                            }
                                        }
                                    }

                                    return bondLine;
                                }

                            default:
                                switch (bond.Stereo)
                                {
                                    case Globals.BondStereo.Cis:
                                        BondLine dcc = new BondLine(BondLineStyle.Solid, bond);
                                        Outputs.BondLines.Add(dcc);
                                        BondLine blnewc = dcc.GetParallel(BondOffset());
                                        Point startPointn = blnewc.Start;
                                        Point endPointn = blnewc.End;
                                        CoordinateTool.AdjustLineAboutMidpoint(ref startPointn, ref endPointn, -(BondOffset() / OoXmlHelper.LINE_SHRINK_PIXELS));
                                        Outputs.BondLines.Add(new BondLine(BondLineStyle.Solid, startPointn, endPointn, bond));
                                        break;

                                    case Globals.BondStereo.Trans:
                                        BondLine dtt = new BondLine(BondLineStyle.Solid, bond);
                                        Outputs.BondLines.Add(dtt);
                                        BondLine blnewt = dtt.GetParallel(BondOffset());
                                        Point startPointt = blnewt.Start;
                                        Point endPointt = blnewt.End;
                                        CoordinateTool.AdjustLineAboutMidpoint(ref startPointt, ref endPointt, -(BondOffset() / OoXmlHelper.LINE_SHRINK_PIXELS));
                                        Outputs.BondLines.Add(new BondLine(BondLineStyle.Solid, startPointt, endPointt, bond));
                                        break;

                                    default:
                                        BondLine dp = new BondLine(BondLineStyle.Solid, bond);
                                        Outputs.BondLines.Add(dp.GetParallel(-(BondOffset() / 2)));
                                        Outputs.BondLines.Add(dp.GetParallel(BondOffset() / 2));
                                        break;
                                }
                                break;
                        }
                    }
                    break;

                case Globals.OrderPartial23:
                    BondLine twoPointFive;
                    BondLine twoPointFiveDashed;
                    BondLine twoPointFiveParallel;
                    Point twoPointFiveStart;
                    Point twoPointFiveEnd;
                    switch (bond.Placement)
                    {
                        case Globals.BondDirection.Clockwise:
                            // Central bond line
                            twoPointFive = new BondLine(BondLineStyle.Solid, bond);
                            Outputs.BondLines.Add(twoPointFive);
                            // Solid bond line
                            twoPointFiveParallel = twoPointFive.GetParallel(-BondOffset());
                            twoPointFiveStart = new Point(twoPointFiveParallel.Start.X, twoPointFiveParallel.Start.Y);
                            twoPointFiveEnd = new Point(twoPointFiveParallel.End.X, twoPointFiveParallel.End.Y);
                            CoordinateTool.AdjustLineAboutMidpoint(ref twoPointFiveStart, ref twoPointFiveEnd, -(BondOffset() / OoXmlHelper.LINE_SHRINK_PIXELS));
                            twoPointFiveParallel = new BondLine(BondLineStyle.Solid, twoPointFiveStart, twoPointFiveEnd, bond);
                            Outputs.BondLines.Add(twoPointFiveParallel);
                            // Dashed bond line
                            twoPointFiveDashed = twoPointFive.GetParallel(BondOffset());
                            twoPointFiveStart = new Point(twoPointFiveDashed.Start.X, twoPointFiveDashed.Start.Y);
                            twoPointFiveEnd = new Point(twoPointFiveDashed.End.X, twoPointFiveDashed.End.Y);
                            CoordinateTool.AdjustLineAboutMidpoint(ref twoPointFiveStart, ref twoPointFiveEnd, -(BondOffset() / OoXmlHelper.LINE_SHRINK_PIXELS));
                            twoPointFiveDashed = new BondLine(BondLineStyle.Half, twoPointFiveStart, twoPointFiveEnd, bond);
                            Outputs.BondLines.Add(twoPointFiveDashed);
                            break;

                        case Globals.BondDirection.Anticlockwise:
                            // Central bond line
                            twoPointFive = new BondLine(BondLineStyle.Solid, bond);
                            Outputs.BondLines.Add(twoPointFive);
                            // Dashed bond line
                            twoPointFiveDashed = twoPointFive.GetParallel(-BondOffset());
                            twoPointFiveStart = new Point(twoPointFiveDashed.Start.X, twoPointFiveDashed.Start.Y);
                            twoPointFiveEnd = new Point(twoPointFiveDashed.End.X, twoPointFiveDashed.End.Y);
                            CoordinateTool.AdjustLineAboutMidpoint(ref twoPointFiveStart, ref twoPointFiveEnd, -(BondOffset() / OoXmlHelper.LINE_SHRINK_PIXELS));
                            twoPointFiveDashed = new BondLine(BondLineStyle.Half, twoPointFiveStart, twoPointFiveEnd, bond);
                            Outputs.BondLines.Add(twoPointFiveDashed);
                            // Solid bond line
                            twoPointFiveParallel = twoPointFive.GetParallel(BondOffset());
                            twoPointFiveStart = new Point(twoPointFiveParallel.Start.X, twoPointFiveParallel.Start.Y);
                            twoPointFiveEnd = new Point(twoPointFiveParallel.End.X, twoPointFiveParallel.End.Y);
                            CoordinateTool.AdjustLineAboutMidpoint(ref twoPointFiveStart, ref twoPointFiveEnd, -(BondOffset() / OoXmlHelper.LINE_SHRINK_PIXELS));
                            twoPointFiveParallel = new BondLine(BondLineStyle.Solid, twoPointFiveStart, twoPointFiveEnd, bond);
                            Outputs.BondLines.Add(twoPointFiveParallel);
                            break;

                        case Globals.BondDirection.None:
                            twoPointFive = new BondLine(BondLineStyle.Solid, bond);
                            Outputs.BondLines.Add(twoPointFive);
                            Outputs.BondLines.Add(twoPointFive.GetParallel(-BondOffset()));
                            twoPointFiveDashed = twoPointFive.GetParallel(BondOffset());
                            twoPointFiveDashed.SetLineStyle(BondLineStyle.Half);
                            Outputs.BondLines.Add(twoPointFiveDashed);
                            break;
                    }
                    break;

                case "3":
                case Globals.OrderTriple:
                    BondLine triple = new BondLine(BondLineStyle.Solid, bond);
                    Outputs.BondLines.Add(triple);
                    Outputs.BondLines.Add(triple.GetParallel(BondOffset()));
                    Outputs.BondLines.Add(triple.GetParallel(-BondOffset()));
                    break;

                default:
                    // Draw a single line, so that there is something to see
                    Outputs.BondLines.Add(new BondLine(BondLineStyle.Solid, bond));
                    break;
            }

            #endregion Create Bond Line objects
        }

        private double BondOffset() => Inputs.MeanBondLength * OoXmlHelper.MULTIPLE_BOND_OFFSET_PERCENTAGE;

        private Rect MeasureString(string text, Point startPoint)
        {
            Rect boundingBox = Rect.Empty;
            Point cursor = new Point(startPoint.X, startPoint.Y);

            TtfCharacter i = Inputs.TtfCharacterSet['i'];

            for (int idx = 0; idx < text.Length; idx++)
            {
                TtfCharacter c = Inputs.TtfCharacterSet['?'];
                char chr = text[idx];
                if (Inputs.TtfCharacterSet.ContainsKey(chr))
                {
                    c = Inputs.TtfCharacterSet[chr];
                }

                if (c != null)
                {
                    Point position = GetCharacterPosition(cursor, c);

                    Rect thisRect = new Rect(new Point(position.X, position.Y),
                                    new Size(OoXmlHelper.ScaleCsTtfToCml(c.Width, Inputs.MeanBondLength),
                                             OoXmlHelper.ScaleCsTtfToCml(c.Height, Inputs.MeanBondLength)));

                    boundingBox.Union(thisRect);

                    if (idx < text.Length - 1)
                    {
                        // Move to next Character position
                        // We ought to be able to use c.IncrementX, but this does not work with string such as "Bowl"
                        cursor.Offset(OoXmlHelper.ScaleCsTtfToCml(c.Width + i.Width, Inputs.MeanBondLength), 0);
                    }
                }
            }

            return boundingBox;
        }

        private void PlaceString(string text, Point startPoint, string path)
        {
            Point cursor = new Point(startPoint.X, startPoint.Y);

            TtfCharacter i = Inputs.TtfCharacterSet['i'];

            for (int idx = 0; idx < text.Length; idx++)
            {
                TtfCharacter c = Inputs.TtfCharacterSet['?'];
                char chr = text[idx];
                if (Inputs.TtfCharacterSet.ContainsKey(chr))
                {
                    c = Inputs.TtfCharacterSet[chr];
                }

                if (c != null)
                {
                    Point position = GetCharacterPosition(cursor, c);

                    AtomLabelCharacter alc = new AtomLabelCharacter(position, c, "000000", path, path);
                    Outputs.AtomLabelCharacters.Add(alc);

                    if (idx < text.Length - 1)
                    {
                        // Move to next Character position
                        // We ought to be able to use c.IncrementX, but this does not work with string such as "Bowl"
                        cursor.Offset(OoXmlHelper.ScaleCsTtfToCml(c.Width + i.Width, Inputs.MeanBondLength), 0);
                    }
                }
            }
        }

        private List<Point> ConvexHull(string atomPath)
        {
            List<Point> points = new List<Point>();

            var chars = Outputs.AtomLabelCharacters.Where(m => m.ParentAtom == atomPath);
            double margin = OoXmlHelper.CML_CHARACTER_MARGIN;
            foreach (var c in chars)
            {
                // Top Left --
                points.Add(new Point(c.Position.X - margin, c.Position.Y - margin));
                if (c.IsSmaller)
                {
                    points.Add(new Point(c.Position.X + OoXmlHelper.ScaleCsTtfToCml(c.Character.Width, Inputs.MeanBondLength) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR + margin,
                                        c.Position.Y - margin));
                    points.Add(new Point(c.Position.X + OoXmlHelper.ScaleCsTtfToCml(c.Character.Width, Inputs.MeanBondLength) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR + margin,
                                        c.Position.Y + OoXmlHelper.ScaleCsTtfToCml(c.Character.Height, Inputs.MeanBondLength) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR + margin));
                    points.Add(new Point(c.Position.X - margin,
                                        c.Position.Y + OoXmlHelper.ScaleCsTtfToCml(c.Character.Height, Inputs.MeanBondLength) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR + margin));
                }
                else
                {
                    points.Add(new Point(c.Position.X + OoXmlHelper.ScaleCsTtfToCml(c.Character.Width, Inputs.MeanBondLength) + margin,
                                        c.Position.Y - margin));
                    points.Add(new Point(c.Position.X + OoXmlHelper.ScaleCsTtfToCml(c.Character.Width, Inputs.MeanBondLength) + margin,
                                        c.Position.Y + OoXmlHelper.ScaleCsTtfToCml(c.Character.Height, Inputs.MeanBondLength) + margin));
                    points.Add(new Point(c.Position.X - margin,
                                          c.Position.Y + OoXmlHelper.ScaleCsTtfToCml(c.Character.Height, Inputs.MeanBondLength) + margin));
                }
            }

            return GeometryTool.MakeConvexHull(points);
        }

        private Point GetCharacterPosition(Point cursorPosition, TtfCharacter character)
        {
            // Add the (negative) OriginY to raise the character by it
            return new Point(cursorPosition.X, cursorPosition.Y + OoXmlHelper.ScaleCsTtfToCml(character.OriginY, Inputs.MeanBondLength));
        }
    }
}