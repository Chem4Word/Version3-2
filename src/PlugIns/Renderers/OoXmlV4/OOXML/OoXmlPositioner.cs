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
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using Chem4Word.Core.Enums;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Model2;
using Chem4Word.Model2.Enums;
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
        private TtfCharacter _hydrogenCharacter;

        public OoXmlPositioner(PositionerInputs inputs) => Inputs = inputs;

        /// <summary>
        /// Carries out the following
        /// 1. Position atom Label characters
        /// 2. Position bond lines
        /// 3. Position brackets (molecules and groups)
        /// 4. Position molecule label characters
        /// 5. Shrink bond lines to not clash with atom labels
        /// 6. Add mask underneath long bond lines of bonds detected as having crossing points
        /// </summary>
        /// <returns>PositionerOutputs a class to hold all of the required output types</returns>
        public PositionerOutputs Position()
        {
            _hydrogenCharacter = Inputs.TtfCharacterSet['H'];

            var moleculeNo = 0;

            if (Inputs.Options.ClipCrossingBonds)
            {
                DetectCrossingLines();
            }

            foreach (var mol in Inputs.Model.Molecules.Values)
            {
                ProcessMolecule(mol, Inputs.Progress, ref moleculeNo);
            }

            // Shrink Bond Lines
            if (Inputs.Options.ClipBondLines)
            {
                ShrinkBondLinesPass1(Inputs.Progress);
                ShrinkBondLinesPass2(Inputs.Progress);
            }

            // Make it look like we are clipping the overlapping bonds
            AddMaskBehindCrossedBonds();

            // Render reaction and annotation texts
            ProcessReactionTexts();
            ProcessAnnotationTexts();

            // We are done now so we can return the final values
            return Outputs;
        }

        private void AddMaskBehindCrossedBonds()
        {
            // Add mask underneath long bond lines of bonds detected as having crossing points
            foreach (var crossedBonds in Inputs.Model.CrossedBonds.Values)
            {
                // Find all lines for this bond
                var lines = Outputs.BondLines.Where(b => b.BondPath.Equals(crossedBonds.LongBond.Path)).ToList();
                foreach (var line in lines)
                {
                    // Create two copies for use later on
                    var replacement = line.Copy();
                    var mask = line.Copy();

                    // Remove the line so we can add two more so that layering is correct
                    Outputs.BondLines.Remove(line);

                    // Set up mask which goes behind the replacement
                    mask.SetLineStyle(BondLineStyle.Solid);
                    // Change this to yellow [ffff00] to see mask
                    mask.Colour = "ffffff";
                    mask.Width = OoXmlHelper.AcsLineWidth * 4;
                    var shrinkBy = (mask.Start - mask.End).Length * OoXmlHelper.MultipleBondOffsetPercentage / 1.5;
                    mask.Shrink(-shrinkBy);

                    // Add mask
                    Outputs.BondLines.Add(mask);
                    // Add replacement so that it's on top of mask
                    Outputs.BondLines.Add(replacement);
                }
            }
        }

        private void ProcessReactionTexts()
        {
            foreach (var scheme in Inputs.Model.ReactionSchemes.Values)
            {
                foreach (var reaction in scheme.Reactions.Values)
                {
                    if (!string.IsNullOrEmpty(reaction.ReagentText))
                    {
                        var terms = TermsFromFlowDocument(reaction.ReagentText);
                        AddReactionCharacters(reaction, terms, true);
                    }

                    if (!string.IsNullOrEmpty(reaction.ConditionsText))
                    {
                        var terms = TermsFromFlowDocument(reaction.ConditionsText);
                        AddReactionCharacters(reaction, terms, false);
                    }
                }
            }
        }

        private void AddReactionCharacters(Reaction reaction, List<FunctionalGroupTerm> terms,
                                           bool isReagent = true, TextBlockJustification justification = TextBlockJustification.Centre)
        {
            var path = reaction.Path + (isReagent ? "/reagent" : "/conditions");

            var groupOfCharacters = GroupOfCharactersFromTerms(reaction.MidPoint, path, terms, justification);
            if (groupOfCharacters.Characters.Any())
            {
                // Position characters
                // Centre group on reaction midpoint
                groupOfCharacters.AdjustPosition(reaction.MidPoint - groupOfCharacters.Centre);

                // March away from reaction midpoint
                var vector = OffsetVector(reaction, isReagent);

                bool isOutside;
                var maxLoops = 0;
                do
                {
                    groupOfCharacters.AdjustPosition(vector);
                    var hull = ConvexHull(groupOfCharacters.Characters);
                    isOutside = GeometryTool.IsOutside(reaction.HeadPoint, reaction.TailPoint, hull);

                    if (maxLoops++ >= 10)
                    {
                        break;
                    }
                } while (!isOutside);
                groupOfCharacters.AdjustPosition(vector);

                // Transfer to output
                foreach (var character in groupOfCharacters.Characters)
                {
                    Outputs.AtomLabelCharacters.Add(character);
                }

                // Finally create diagnostics
                Outputs.Diagnostics.Rectangles.Add(new DiagnosticRectangle(Inflate(groupOfCharacters.BoundingBox, OoXmlHelper.AcsLineWidth / 2), "00b050"));
                Outputs.ConvexHulls.Add(path, ConvexHull(path));
            }
        }

        private void ProcessAnnotationTexts()
        {
            foreach (var annotation in Inputs.Model.Annotations.Values)
            {
                if (!string.IsNullOrEmpty(annotation.Xaml))
                {
                    var terms = TermsFromFlowDocument(annotation.Xaml);
                    AddAnnotationCharacters(annotation, annotation.Path, terms);
                }
            }
        }

        private void AddAnnotationCharacters(Annotation annotation, string path, List<FunctionalGroupTerm> terms,
                                             TextBlockJustification justification = TextBlockJustification.Left)
        {
            // Position characters
            var groupOfCharacters = GroupOfCharactersFromTerms(annotation.Position, annotation.Path, terms, justification);

            if (groupOfCharacters.Characters.Any())
            {
                // Centre group on annotation position
                groupOfCharacters.AdjustPosition(annotation.Position - groupOfCharacters.BoundingBox.TopLeft);

                // Transfer to output
                foreach (var character in groupOfCharacters.Characters)
                {
                    Outputs.AtomLabelCharacters.Add(character);
                }

                // Finally create diagnostics
                Outputs.Diagnostics.Rectangles.Add(new DiagnosticRectangle(Inflate(groupOfCharacters.BoundingBox, OoXmlHelper.AcsLineWidth / 2), "00b050"));
                Outputs.ConvexHulls.Add(path, ConvexHull(path));
            }
        }

        private GroupOfCharacters GroupOfCharactersFromTerms(Point position, string path, List<FunctionalGroupTerm> terms, TextBlockJustification justification)
        {
            var groupOfCharacters = new GroupOfCharacters(position, path, path,
                                              Inputs.TtfCharacterSet, Inputs.MeanBondLength);

            var lineNumber = 0;

            if (terms != null)
            {
                // Generate characters
                foreach (var term in terms)
                {
                    if (term.Parts.Any())
                    {
                        // Measure
                        var measure = new GroupOfCharacters(new Point(0, 0), null, null,
                                                            Inputs.TtfCharacterSet, Inputs.MeanBondLength);
                        measure.AddParts(term.Parts, OoXmlHelper.Black);

                        if (lineNumber++ > 0)
                        {
                            // Apply NewLine with measured offset
                            switch (justification)
                            {
                                case TextBlockJustification.Left:
                                    groupOfCharacters.NewLine();
                                    break;

                                case TextBlockJustification.Centre:
                                    groupOfCharacters.NewLine(
                                        groupOfCharacters.BoundingBox.Width / 2 - measure.BoundingBox.Width / 2);
                                    break;

                                case TextBlockJustification.Right:
                                    groupOfCharacters.NewLine(
                                        groupOfCharacters.BoundingBox.Width - measure.BoundingBox.Width);
                                    break;
                            }
                        }

                        // Add Characters for real
                        groupOfCharacters.AddParts(term.Parts, OoXmlHelper.Black);
                    }
                }
            }

            return groupOfCharacters;
        }

        private Vector OffsetVector(Reaction reaction, bool isReagent)
        {
            var arrowIsBackwards = reaction.TailPoint.X > reaction.HeadPoint.X;
            double perpendicularAngle = 90;
            if (arrowIsBackwards)
            {
                perpendicularAngle = -perpendicularAngle;
            }

            var rotator = new Matrix();
            var userOffset = isReagent ? reaction.ReagentsBlockOffset : reaction.ConditionsBlockOffset;
            if (userOffset is null)
            {
                // above or below the arrow
                if (isReagent)
                {
                    rotator.Rotate(-perpendicularAngle);
                }
                else
                {
                    rotator.Rotate(perpendicularAngle);
                }
            }
            else
            {
                // ToDo: Implement if required
                Debugger.Break();
            }

            // Create the perpendicular vector
            var perpendicularVector = reaction.ReactionVector;
            perpendicularVector.Normalize();
            perpendicularVector *= rotator;

            perpendicularVector *= OoXmlHelper.MultipleBondOffsetPercentage * Inputs.MeanBondLength;

            return perpendicularVector;
        }

        private List<FunctionalGroupTerm> TermsFromFlowDocument(string flowDocument)
        {
            var result = new List<FunctionalGroupTerm>();

            var xml = new XmlDocument();
            xml.LoadXml(flowDocument);

            var root = xml.FirstChild.FirstChild;
            var term = new FunctionalGroupTerm();

            foreach (XmlNode node in root.ChildNodes)
            {
                switch (node.LocalName)
                {
                    case "Run":
                        var part = new FunctionalGroupPart
                        {
                            Text = node.InnerText
                        };
                        if (!string.IsNullOrEmpty(part.Text))
                        {
                            if (node.Attributes?["BaselineAlignment"] != null)
                            {
                                var alignment = node.Attributes["BaselineAlignment"].Value;
                                switch (alignment)
                                {
                                    case "Subscript":
                                        part.Type = FunctionalGroupPartType.Subscript;
                                        break;

                                    case "Superscript":
                                        part.Type = FunctionalGroupPartType.Superscript;
                                        break;
                                }
                            }
                            term.Parts.Add(part);
                        }
                        break;

                    case "LineBreak":
                        result.Add(term);
                        term = new FunctionalGroupTerm();
                        break;
                }
            }

            // Handle "missing" LineBreak
            if (term.Parts.Any())
            {
                result.Add(term);
            }

            // Add 'fake' space character to any blank lines
            foreach (var line in result)
            {
                if (line.Parts.Count == 0)
                {
                    line.Parts.Add(new FunctionalGroupPart
                    {
                        Text = " "
                    });
                }
            }

            return result;
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

                foreach (var bl in targeted.ToList())
                {
                    var start = new Point(bl.Start.X, bl.Start.Y);
                    var end = new Point(bl.End.X, bl.End.Y);

                    bool lineStartsOutsidePolygon;
                    var result = GeometryTool.ClipLineWithPolygon(start, end, hull.Value, out lineStartsOutsidePolygon);

                    switch (result.Length)
                    {
                        case 3:
                            if (lineStartsOutsidePolygon)
                            {
                                bl.Start = new Point(result[0].X, result[0].Y);
                                bl.End = new Point(result[1].X, result[1].Y);
                            }
                            else
                            {
                                bl.Start = new Point(result[1].X, result[1].Y);
                                bl.End = new Point(result[2].X, result[2].Y);
                            }
                            break;

                        case 2:
                            if (!lineStartsOutsidePolygon)
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

            foreach (var alc in Outputs.AtomLabelCharacters)
            {
                pb.Increment(1);

                var width = OoXmlHelper.ScaleCsTtfToCml(alc.Character.Width, Inputs.MeanBondLength);
                var height = OoXmlHelper.ScaleCsTtfToCml(alc.Character.Height, Inputs.MeanBondLength);

                if (alc.IsSmaller)
                {
                    // Shrink bounding box
                    width *= OoXmlHelper.SubscriptScaleFactor;
                    height *= OoXmlHelper.SubscriptScaleFactor;
                }

                // Create rectangle of the bounding box with a suitable clipping margin
                var cbb = new Rect(alc.Position.X - OoXmlHelper.CmlCharacterMargin,
                                   alc.Position.Y - OoXmlHelper.CmlCharacterMargin,
                                   width + (OoXmlHelper.CmlCharacterMargin * 2),
                                   height + (OoXmlHelper.CmlCharacterMargin * 2));

                // Just in case we end up splitting a line into two
                var extraBondLines = new List<BondLine>();

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

                foreach (var bl in targeted)
                {
                    var start = new Point(bl.Start.X, bl.Start.Y);
                    var end = new Point(bl.End.X, bl.End.Y);

                    var attempts = 0;
                    if (CohenSutherland.ClipLine(cbb, ref start, ref end, out attempts))
                    {
                        var bClipped = false;

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
                            var ignoreWedgeOrHatch = bl.Bond.Order == Globals.OrderSingle
                                                     && bl.Bond.Stereo == BondStereo.Wedge
                                                     || bl.Bond.Stereo == BondStereo.Hatch;
                            if (!ignoreWedgeOrHatch)
                            {
                                // Line was clipped at both ends
                                // 1. Generate new line
                                var extraLine = new BondLine(bl.Style, new Point(end.X, end.Y), new Point(bl.End.X, bl.End.Y), bl.Bond);
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
                foreach (var bl in extraBondLines)
                {
                    Outputs.BondLines.Add(bl);
                }
            }
        }

        private void DetectCrossingLines()
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            var model = Inputs.Model;

            var sw = new Stopwatch();
            sw.Start();

            model.DetectCrossingLines();
            foreach (var crossedBond in model.CrossedBonds)
            {
                Outputs.CrossingPoints.Add(crossedBond.Value.CrossingPoint);
            }
            sw.Stop();
            Inputs.Telemetry.Write(module, "Timing", $"Detection of {model.CrossedBonds.Count} line crossing points took {SafeDouble.AsString0(sw.ElapsedMilliseconds)} ms");
        }

        private void ProcessMolecule(Molecule mol, Progress pb, ref int molNumber)
        {
            molNumber++;

            // Position Atom Label Characters
            ProcessAtoms(mol, pb, molNumber);

            // Position Bond Lines
            ProcessBonds(mol, pb, molNumber);

            // Populate diagnostic data
            foreach (var ring in mol.Rings)
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

            // Determine Extents

            // Atoms <= InternalCharacters <= GroupBrackets <= MoleculesBrackets <= ExternalCharacters

            // Atoms & InternalCharacters
            var thisMoleculeExtents = new MoleculeExtents(mol.Path, mol.BoundingBox);
            thisMoleculeExtents.SetInternalCharacterExtents(CharacterExtents(mol, thisMoleculeExtents.AtomExtents));
            Outputs.AllMoleculeExtents.Add(thisMoleculeExtents);

            // Grouped Molecules
            if (mol.IsGrouped)
            {
                var boundingBox = Rect.Empty;

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

            // Add required Brackets
            var showBrackets = mol.ShowMoleculeBrackets.HasValue && mol.ShowMoleculeBrackets.Value
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

            var characters = string.Empty;

            if (mol.FormalCharge.HasValue && mol.FormalCharge.Value != 0)
            {
                // Add FormalCharge at top right
                var charge = mol.FormalCharge.Value;
                var absCharge = Math.Abs(charge);

                var chargeSign = Math.Sign(charge) > 0 ? "+" : "-";
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
                                      + OoXmlHelper.MultipleBondOffsetPercentage * Inputs.MeanBondLength,
                                      thisMoleculeExtents.MoleculeBracketsExtents.Top
                                      + OoXmlHelper.ScaleCsTtfToCml(_hydrogenCharacter.Height, Inputs.MeanBondLength) / 2);
                PlaceString(characters, point, mol.Path);
            }

            if (mol.Count.HasValue && mol.Count.Value > 0)
            {
                // Draw Count at bottom right
                var point = new Point(thisMoleculeExtents.MoleculeBracketsExtents.Right
                                      + OoXmlHelper.MultipleBondOffsetPercentage * Inputs.MeanBondLength,
                                      thisMoleculeExtents.MoleculeBracketsExtents.Bottom
                                      + OoXmlHelper.ScaleCsTtfToCml(_hydrogenCharacter.Height, Inputs.MeanBondLength) / 2);
                PlaceString($"{mol.Count}", point, mol.Path);
            }

            if (mol.Count.HasValue
                || mol.FormalCharge.HasValue
                || mol.SpinMultiplicity.HasValue)
            {
                // Recalculate as we have just added extra characters
                thisMoleculeExtents.SetExternalCharacterExtents(CharacterExtents(mol, thisMoleculeExtents.MoleculeBracketsExtents));
            }

            // Position Molecule Label Characters
            // Handle optional rendering of molecule labels centered on brackets (if any) and below any molecule property characters
            if (Inputs.Options.ShowMoleculeCaptions && mol.Captions.Any())
            {
                var point = new Point(thisMoleculeExtents.MoleculeBracketsExtents.Left
                                        + thisMoleculeExtents.MoleculeBracketsExtents.Width / 2,
                                      thisMoleculeExtents.ExternalCharacterExtents.Bottom
                                        + Inputs.MeanBondLength * OoXmlHelper.MultipleBondOffsetPercentage / 2);

                AddMoleculeCaptionsAsCharacters(mol.Captions.ToList(), point, mol.Path);
                // Recalculate as we have just added extra characters
                thisMoleculeExtents.SetExternalCharacterExtents(CharacterExtents(mol, thisMoleculeExtents.MoleculeBracketsExtents));
            }
        }

        private Rect Inflate(Rect r, double x)
        {
            var r1 = r;
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
                    var r = new Rect(c.Position,
                                     new Size(OoXmlHelper.ScaleCsTtfToCml(c.Character.Width, Inputs.MeanBondLength) * OoXmlHelper.SubscriptScaleFactor,
                                              OoXmlHelper.ScaleCsTtfToCml(c.Character.Height, Inputs.MeanBondLength) * OoXmlHelper.SubscriptScaleFactor));
                    existing.Union(r);
                }
                else
                {
                    var r = new Rect(c.Position,
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

            foreach (var atom in mol.Atoms.Values)
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

            foreach (var bond in mol.Bonds)
            {
                pb.Increment(1);
                CreateLines(bond);
            }

            // Rendering molecular sketches for publication quality output
            // Alex M Clark
            // Implement beautification of semi open double bonds and double bonds touching rings

            // Obtain list of Double Bonds with Placement of BondDirection.None
            var doubleBonds = mol.Bonds.Where(b => b.OrderValue.HasValue && b.OrderValue.Value == 2 && b.Placement == BondDirection.None).ToList();
            if (doubleBonds.Count > 0)
            {
                pb.Message = $"Processing Double Bonds in Molecule {moleculeNo}";
                pb.Value = 0;
                pb.Maximum = doubleBonds.Count;

                foreach (var bond in doubleBonds)
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
                var isInRing = atom.IsInRing;
                var lines = Outputs.BondLines.Where(bl => bl.BondPath.Equals(bondPath)).ToList();
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
                        var line1 = Outputs.BondLines.FirstOrDefault(bl => bl.BondPath.Equals(otherLines[0].Path));
                        var line2 = Outputs.BondLines.FirstOrDefault(bl => bl.BondPath.Equals(otherLines[1].Path));
                        if (line1 != null && line2 != null)
                        {
                            TrimLines(lines, line1, line2, isInRing);
                        }
                        else
                        {
                            // Hopefully never hit this
                            Debugger.Break();
                        }
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
            var startLonger = new Point(leftOrRight.Start.X, leftOrRight.Start.Y);
            var endLonger = new Point(leftOrRight.End.X, leftOrRight.End.Y);
            GeometryTool.AdjustLineAboutMidpoint(ref startLonger, ref endLonger, Inputs.MeanBondLength / 5);

            // See if they intersect at one end
            GeometryTool.FindIntersection(startLonger, endLonger, line.Start, line.End,
                                          out dummy, out intersect, out intersection);

            // If they intersect update the main line
            if (intersect)
            {
                var l1 = GeometryTool.DistanceBetween(intersection, leftOrRight.Start);
                var l2 = GeometryTool.DistanceBetween(intersection, leftOrRight.End);
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
                    l1 = GeometryTool.DistanceBetween(intersection, line.Start);
                    l2 = GeometryTool.DistanceBetween(intersection, line.End);
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
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            var atomLabel = atom.SymbolText;
            if (Inputs.Options.ShowCarbons
                && atom.Element is Element e
                && e == Globals.PeriodicTable.C)
            {
                atomLabel = e.Symbol;
            }

            if (!string.IsNullOrEmpty(atomLabel))
            {
                #region Set Up Atom Colour

                string atomColour = OoXmlHelper.Black;
                if (Inputs.Options.ColouredAtoms
                    && atom.Element.Colour != null)
                {
                    atomColour = atom.Element.Colour;
                    // Strip out # as OoXml does not use it
                    atomColour = atomColour.Replace("#", "");
                }

                #endregion Set Up Atom Colour

                // Create main character group
                var main = new GroupOfCharacters(atom.Position, atom.Path, atom.Parent.Path,
                                                 Inputs.TtfCharacterSet, Inputs.MeanBondLength);
                main.AddString(atomLabel, atomColour);

                // Create a special group for the first character
                var firstCharacter = new GroupOfCharacters(atom.Position, atom.Path, atom.Parent.Path,
                                                           Inputs.TtfCharacterSet, Inputs.MeanBondLength);
                firstCharacter.AddCharacter(atomLabel[0], atomColour);

                // Distance to move horizontally to midpoint of whole label
                var x = atom.Position.X - main.Centre.X;
                // Distance to move vertically to midpoint of first character
                var y = atom.Position.Y - firstCharacter.Centre.Y;

                // Move to new position
                main.AdjustPosition(new Vector(x, y));

                // Create other character groups

                // Determine position of implicit hydrogens if any
                var orientation = atom.ImplicitHPlacement;

                // Implicit Hydrogens
                GroupOfCharacters hydrogens = null;

                var implicitHCount = atom.ImplicitHydrogenCount;
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
                            hydrogens.AdjustPosition(main.BoundingBox.TopLeft - hydrogens.BoundingBox.BottomLeft);
                            hydrogens.Nudge(CompassPoints.East, main.BoundingBox.Width / 2 - OoXmlHelper.ScaleCsTtfToCml(_hydrogenCharacter.Width, Inputs.MeanBondLength) / 2);
                            break;

                        case CompassPoints.East:
                            hydrogens.AdjustPosition(main.BoundingBox.TopRight - hydrogens.BoundingBox.TopLeft);
                            break;

                        case CompassPoints.South:
                            hydrogens.AdjustPosition(main.BoundingBox.BottomLeft - hydrogens.BoundingBox.TopLeft);
                            hydrogens.Nudge(CompassPoints.East, main.BoundingBox.Width / 2 - OoXmlHelper.ScaleCsTtfToCml(_hydrogenCharacter.Width, Inputs.MeanBondLength) / 2);
                            break;

                        case CompassPoints.West:
                            hydrogens.AdjustPosition(main.BoundingBox.TopLeft - hydrogens.BoundingBox.TopRight);
                            break;
                    }
                    hydrogens.Nudge(orientation);
                }

                // Charge
                GroupOfCharacters charge = null;

                var chargeValue = atom.FormalCharge ?? 0;
                var absCharge = Math.Abs(chargeValue);

                if (absCharge > 0)
                {
                    charge = new GroupOfCharacters(atom.Position, atom.Path, atom.Parent.Path,
                                                      Inputs.TtfCharacterSet, Inputs.MeanBondLength);

                    // Create characters
                    var chargeSign = Math.Sign(chargeValue) > 0 ? "+" : "-";
                    var digits = absCharge == 1 ? chargeSign : $"{absCharge}{chargeSign}";

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

                // Isotope
                GroupOfCharacters isotope = null;

                var isoValue = atom.IsotopeNumber ?? 0;

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
                        isotope.Nudge(CompassPoints.West);
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

                // Transfer to output
                foreach (var character in main.Characters)
                {
                    Outputs.AtomLabelCharacters.Add(character);
                }

                Outputs.Diagnostics.Rectangles.Add(new DiagnosticRectangle(Inflate(main.BoundingBox, OoXmlHelper.AcsLineWidth / 2), "00b050"));

                if (hydrogens != null)
                {
                    foreach (var character in hydrogens.Characters)
                    {
                        Outputs.AtomLabelCharacters.Add(character);
                    }

                    Outputs.Diagnostics.Rectangles.Add(new DiagnosticRectangle(Inflate(hydrogens.BoundingBox, OoXmlHelper.AcsLineWidth / 2), "7030a0"));
                }

                if (charge != null)
                {
                    foreach (var character in charge.Characters)
                    {
                        Outputs.AtomLabelCharacters.Add(character);
                    }

                    Outputs.Diagnostics.Rectangles.Add(new DiagnosticRectangle(Inflate(charge.BoundingBox, OoXmlHelper.AcsLineWidth / 2), "ff0000"));
                }

                if (isotope != null)
                {
                    foreach (var character in isotope.Characters)
                    {
                        Outputs.AtomLabelCharacters.Add(character);
                    }

                    Outputs.Diagnostics.Rectangles.Add(new DiagnosticRectangle(Inflate(isotope.BoundingBox, OoXmlHelper.AcsLineWidth / 2), "0070c0"));
                }

                // Generate Convex Hull
                Outputs.ConvexHulls.Add(atom.Path, ConvexHull(atom.Path));
            }
        }

        private void CreateFunctionalGroupCharacters(Atom atom)
        {
            var fg = atom.Element as FunctionalGroup;
            var reverse = atom.FunctionalGroupPlacement == CompassPoints.West;

            #region Set Up Atom Colour

            var atomColour = OoXmlHelper.Black;
            if (Inputs.Options.ColouredAtoms
                && fg?.Colour != null)
            {
                atomColour = fg.Colour;
                // Strip out # as OoXml does not use it
                atomColour = atomColour.Replace("#", "");
            }

            #endregion Set Up Atom Colour

            if (fg != null)
            {
                var terms = fg.ExpandIntoTerms(reverse);

                // Create a special group for the first character
                var firstCapital = new GroupOfCharacters(atom.Position, atom.Path, atom.Parent.Path,
                                                           Inputs.TtfCharacterSet, Inputs.MeanBondLength);

                var main = new GroupOfCharacters(atom.Position, atom.Path, atom.Parent.Path,
                                                 Inputs.TtfCharacterSet, Inputs.MeanBondLength);

                var auxiliary = new GroupOfCharacters(atom.Position, atom.Path, atom.Parent.Path,
                                                      Inputs.TtfCharacterSet, Inputs.MeanBondLength);

                bool firstCapitalFound = false;

                // Generate characters
                foreach (var term in terms)
                {
                    if (term.IsAnchor)
                    {
                        main.AddParts(term.Parts, atomColour);
                        if (!firstCapitalFound)
                        {
                            firstCapital.AddCharacter(term.FirstCaptial, atomColour);
                            firstCapitalFound = true;
                        }
                    }
                    else
                    {
                        auxiliary.AddParts(term.Parts, atomColour);
                    }
                }

                // Position characters
                if (firstCapitalFound)
                {
                    // Distance to move horizontally to midpoint of whole label
                    var x = atom.Position.X - main.Centre.X;
                    // Distance to move vertically to midpoint of first character
                    var y = atom.Position.Y - firstCapital.Centre.Y;

                    // Move to new position
                    main.AdjustPosition(new Vector(x, y));
                }
                else
                {
                    // Fallback to old method
                    main.AdjustPosition(atom.Position - main.Centre);
                }

                Outputs.Diagnostics.Rectangles.Add(new DiagnosticRectangle(Inflate(main.BoundingBox, OoXmlHelper.AcsLineWidth / 2), "00b050"));

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

                    Outputs.Diagnostics.Rectangles.Add(new DiagnosticRectangle(Inflate(auxiliary.BoundingBox, OoXmlHelper.AcsLineWidth / 2), "ffc000"));
                }

                // Transfer to output
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
            }

            // Generate Convex Hull
            Outputs.ConvexHulls.Add(atom.Path, ConvexHull(atom.Path));
        }

        private void AddMoleculeCaptionsAsCharacters(List<TextualProperty> labels, Point centrePoint, string moleculePath)
        {
            var measure = new Point(centrePoint.X, centrePoint.Y);

            foreach (var label in labels)
            {
                // Measure string
                var boundingBox = MeasureString(label.Value, measure);

                // Place string characters such that they are hanging below the "line"
                if (boundingBox != Rect.Empty)
                {
                    var place = new Point(measure.X - boundingBox.Width / 2, measure.Y + (measure.Y - boundingBox.Top));
                    PlaceString(label.Value, place, moleculePath);
                }

                // Move to next line
                measure.Offset(0, boundingBox.Height + Inputs.MeanBondLength * OoXmlHelper.MultipleBondOffsetPercentage / 2);
            }
        }

        // Do Not delete as we may yet end up using this ...
        private void AddMoleculeCaptionsAsTextBox(List<TextualProperty> labels, Point centrePoint, string moleculePath)
        {
            var measure = new Point(centrePoint.X, centrePoint.Y);

            // Adjust size to allow for text box to be bigger than the text

            foreach (var label in labels)
            {
                // Measure string
                var boundingBox = MeasureString(label.Value, measure);

                // Adjustments to take into account text box margins
                boundingBox.Width += OoXmlHelper.ScaleCsTtfToCml(_hydrogenCharacter.Width, Inputs.MeanBondLength) * 2.5;
                boundingBox.Height *= 1.5;

                // Place string characters such that they are hanging below the "line"
                if (boundingBox != Rect.Empty)
                {
                    var place = new Point(measure.X - boundingBox.Width / 2, measure.Y);
                    Outputs.MoleculeCaptions.Add(new OoXmlString(new Rect(place, boundingBox.Size), label.Value, moleculePath));
                }

                // Move to next line
                measure.Offset(0, boundingBox.Height + Inputs.MeanBondLength * OoXmlHelper.MultipleBondOffsetPercentage / 2);
            }
        }

        /// <summary>
        /// Creates the lines for a bond
        /// </summary>
        /// <param name="bond"></param>
        private void CreateLines(Bond bond)
        {
            var bondStart = bond.StartAtom.Position;

            var bondEnd = bond.EndAtom.Position;

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
                        case BondStereo.None:
                            Outputs.BondLines.Add(new BondLine(BondLineStyle.Solid, bond));
                            break;

                        case BondStereo.Hatch:
                            Outputs.BondLines.Add(new BondLine(BondLineStyle.Hatch, bond));
                            break;

                        case BondStereo.Wedge:
                            Outputs.BondLines.Add(new BondLine(BondLineStyle.Wedge, bond));
                            break;

                        case BondStereo.Indeterminate:
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
                        case BondDirection.Clockwise:
                            onePointFive = new BondLine(BondLineStyle.Solid, bond);
                            Outputs.BondLines.Add(onePointFive);
                            onePointFiveDashed = onePointFive.GetParallel(BondOffset());
                            onePointFiveStart = new Point(onePointFiveDashed.Start.X, onePointFiveDashed.Start.Y);
                            onePointFiveEnd = new Point(onePointFiveDashed.End.X, onePointFiveDashed.End.Y);
                            GeometryTool.AdjustLineAboutMidpoint(ref onePointFiveStart, ref onePointFiveEnd, -(BondOffset() / OoXmlHelper.LineShrinkPixels));
                            onePointFiveDashed = new BondLine(BondLineStyle.Half, onePointFiveStart, onePointFiveEnd, bond);
                            Outputs.BondLines.Add(onePointFiveDashed);
                            break;

                        case BondDirection.Anticlockwise:
                            onePointFive = new BondLine(BondLineStyle.Solid, bond);
                            Outputs.BondLines.Add(onePointFive);
                            onePointFiveDashed = onePointFive.GetParallel(-BondOffset());
                            onePointFiveStart = new Point(onePointFiveDashed.Start.X, onePointFiveDashed.Start.Y);
                            onePointFiveEnd = new Point(onePointFiveDashed.End.X, onePointFiveDashed.End.Y);
                            GeometryTool.AdjustLineAboutMidpoint(ref onePointFiveStart, ref onePointFiveEnd, -(BondOffset() / OoXmlHelper.LineShrinkPixels));
                            onePointFiveDashed = new BondLine(BondLineStyle.Half, onePointFiveStart, onePointFiveEnd, bond);
                            Outputs.BondLines.Add(onePointFiveDashed);
                            break;

                        case BondDirection.None:
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
                    if (bond.Stereo == BondStereo.Indeterminate) //crossing bonds
                    {
                        // Crossed lines
                        var d = new BondLine(BondLineStyle.Solid, bondStart, bondEnd, bond);
                        var d1 = d.GetParallel(-(BondOffset() / 2));
                        var d2 = d.GetParallel(BondOffset() / 2);
                        Outputs.BondLines.Add(new BondLine(BondLineStyle.Solid, new Point(d1.Start.X, d1.Start.Y), new Point(d2.End.X, d2.End.Y), bond));
                        Outputs.BondLines.Add(new BondLine(BondLineStyle.Solid, new Point(d2.Start.X, d2.Start.Y), new Point(d1.End.X, d1.End.Y), bond));
                    }
                    else
                    {
                        switch (bond.Placement)
                        {
                            case BondDirection.Anticlockwise:
                                var da = new BondLine(BondLineStyle.Solid, bond);
                                Outputs.BondLines.Add(da);
                                Outputs.BondLines.Add(PlaceSecondaryLine(da, da.GetParallel(-BondOffset())));
                                break;

                            case BondDirection.Clockwise:
                                var dc = new BondLine(BondLineStyle.Solid, bond);
                                Outputs.BondLines.Add(dc);
                                Outputs.BondLines.Add(PlaceSecondaryLine(dc, dc.GetParallel(BondOffset())));
                                break;

                                // Local Function
                                BondLine PlaceSecondaryLine(BondLine primaryLine, BondLine secondaryLine)
                                {
                                    var primaryMidpoint = GeometryTool.GetMidPoint(primaryLine.Start, primaryLine.End);
                                    var secondaryMidpoint = GeometryTool.GetMidPoint(secondaryLine.Start, secondaryLine.End);

                                    var startPointa = secondaryLine.Start;
                                    var endPointa = secondaryLine.End;

                                    Point? centre = null;

                                    var clip = false;

                                    // Does bond have a primary ring?
                                    if (bond.PrimaryRing != null && bond.PrimaryRing.Centroid != null)
                                    {
                                        // Get angle between bond and vector to primary ring centre
                                        centre = bond.PrimaryRing.Centroid.Value;
                                        var primaryRingVector = primaryMidpoint - centre.Value;
                                        var angle = GeometryTool.AngleBetween(bond.BondVector, primaryRingVector);

                                        // Does bond have a secondary ring?
                                        if (bond.SubsidiaryRing != null && bond.SubsidiaryRing.Centroid != null)
                                        {
                                            // Get angle between bond and vector to secondary ring centre
                                            var centre2 = bond.SubsidiaryRing.Centroid.Value;
                                            var secondaryRingVector = primaryMidpoint - centre2;
                                            var angle2 = GeometryTool.AngleBetween(bond.BondVector, secondaryRingVector);

                                            // Get angle in which the offset line has moved with respect to the bond line
                                            var offsetVector = primaryMidpoint - secondaryMidpoint;
                                            var offsetAngle = GeometryTool.AngleBetween(bond.BondVector, offsetVector);

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

                                        GeometryTool.FindIntersection(startPointa, endPointa, bondStart, centre.Value,
                                                                      out _, out _, out outIntersectP1);
                                        GeometryTool.FindIntersection(startPointa, endPointa, bondEnd, centre.Value,
                                                                      out _, out _, out outIntersectP2);

                                        if (Inputs.Options.ShowDoubleBondTrimmingLines)
                                        {
                                            // Diagnostics
                                            Outputs.Diagnostics.Lines.Add(new DiagnosticLine(bond.StartAtom.Position, centre.Value, BondLineStyle.Dotted, "ff0000"));
                                            Outputs.Diagnostics.Lines.Add(new DiagnosticLine(bond.EndAtom.Position, centre.Value, BondLineStyle.Dotted, "ff0000"));
                                        }

                                        return new BondLine(BondLineStyle.Solid, outIntersectP1, outIntersectP2, bond);
                                    }
                                    else
                                    {
                                        GeometryTool.AdjustLineAboutMidpoint(ref startPointa, ref endPointa, -(BondOffset() / OoXmlHelper.LineShrinkPixels));
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
                                        var v1 = left - common;
                                        var v2 = right - common;
                                        var angle = GeometryTool.AngleBetween(v1, v2);
                                        var matrix = new Matrix();
                                        matrix.Rotate(angle / 2);
                                        v1 = v1 * 2 * matrix;
                                        if (Inputs.Options.ShowDoubleBondTrimmingLines)
                                        {
                                            Outputs.Diagnostics.Lines.Add(new DiagnosticLine(common, common + v1, BondLineStyle.Dotted, "0000ff"));
                                        }

                                        bool intersect;
                                        Point meetingPoint;
                                        GeometryTool.FindIntersection(bondLine.Start, bondLine.End,
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
                                    case BondStereo.Cis:
                                        var dcc = new BondLine(BondLineStyle.Solid, bond);
                                        Outputs.BondLines.Add(dcc);
                                        var blnewc = dcc.GetParallel(BondOffset());
                                        var startPointn = blnewc.Start;
                                        var endPointn = blnewc.End;
                                        GeometryTool.AdjustLineAboutMidpoint(ref startPointn, ref endPointn, -(BondOffset() / OoXmlHelper.LineShrinkPixels));
                                        Outputs.BondLines.Add(new BondLine(BondLineStyle.Solid, startPointn, endPointn, bond));
                                        break;

                                    case BondStereo.Trans:
                                        var dtt = new BondLine(BondLineStyle.Solid, bond);
                                        Outputs.BondLines.Add(dtt);
                                        var blnewt = dtt.GetParallel(BondOffset());
                                        var startPointt = blnewt.Start;
                                        var endPointt = blnewt.End;
                                        GeometryTool.AdjustLineAboutMidpoint(ref startPointt, ref endPointt, -(BondOffset() / OoXmlHelper.LineShrinkPixels));
                                        Outputs.BondLines.Add(new BondLine(BondLineStyle.Solid, startPointt, endPointt, bond));
                                        break;

                                    default:
                                        var dp = new BondLine(BondLineStyle.Solid, bond);
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
                        case BondDirection.Clockwise:
                            // Central bond line
                            twoPointFive = new BondLine(BondLineStyle.Solid, bond);
                            Outputs.BondLines.Add(twoPointFive);
                            // Solid bond line
                            twoPointFiveParallel = twoPointFive.GetParallel(-BondOffset());
                            twoPointFiveStart = new Point(twoPointFiveParallel.Start.X, twoPointFiveParallel.Start.Y);
                            twoPointFiveEnd = new Point(twoPointFiveParallel.End.X, twoPointFiveParallel.End.Y);
                            GeometryTool.AdjustLineAboutMidpoint(ref twoPointFiveStart, ref twoPointFiveEnd, -(BondOffset() / OoXmlHelper.LineShrinkPixels));
                            twoPointFiveParallel = new BondLine(BondLineStyle.Solid, twoPointFiveStart, twoPointFiveEnd, bond);
                            Outputs.BondLines.Add(twoPointFiveParallel);
                            // Dashed bond line
                            twoPointFiveDashed = twoPointFive.GetParallel(BondOffset());
                            twoPointFiveStart = new Point(twoPointFiveDashed.Start.X, twoPointFiveDashed.Start.Y);
                            twoPointFiveEnd = new Point(twoPointFiveDashed.End.X, twoPointFiveDashed.End.Y);
                            GeometryTool.AdjustLineAboutMidpoint(ref twoPointFiveStart, ref twoPointFiveEnd, -(BondOffset() / OoXmlHelper.LineShrinkPixels));
                            twoPointFiveDashed = new BondLine(BondLineStyle.Half, twoPointFiveStart, twoPointFiveEnd, bond);
                            Outputs.BondLines.Add(twoPointFiveDashed);
                            break;

                        case BondDirection.Anticlockwise:
                            // Central bond line
                            twoPointFive = new BondLine(BondLineStyle.Solid, bond);
                            Outputs.BondLines.Add(twoPointFive);
                            // Dashed bond line
                            twoPointFiveDashed = twoPointFive.GetParallel(-BondOffset());
                            twoPointFiveStart = new Point(twoPointFiveDashed.Start.X, twoPointFiveDashed.Start.Y);
                            twoPointFiveEnd = new Point(twoPointFiveDashed.End.X, twoPointFiveDashed.End.Y);
                            GeometryTool.AdjustLineAboutMidpoint(ref twoPointFiveStart, ref twoPointFiveEnd, -(BondOffset() / OoXmlHelper.LineShrinkPixels));
                            twoPointFiveDashed = new BondLine(BondLineStyle.Half, twoPointFiveStart, twoPointFiveEnd, bond);
                            Outputs.BondLines.Add(twoPointFiveDashed);
                            // Solid bond line
                            twoPointFiveParallel = twoPointFive.GetParallel(BondOffset());
                            twoPointFiveStart = new Point(twoPointFiveParallel.Start.X, twoPointFiveParallel.Start.Y);
                            twoPointFiveEnd = new Point(twoPointFiveParallel.End.X, twoPointFiveParallel.End.Y);
                            GeometryTool.AdjustLineAboutMidpoint(ref twoPointFiveStart, ref twoPointFiveEnd, -(BondOffset() / OoXmlHelper.LineShrinkPixels));
                            twoPointFiveParallel = new BondLine(BondLineStyle.Solid, twoPointFiveStart, twoPointFiveEnd, bond);
                            Outputs.BondLines.Add(twoPointFiveParallel);
                            break;

                        case BondDirection.None:
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
                    var triple = new BondLine(BondLineStyle.Solid, bond);
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

        private double BondOffset() => Inputs.MeanBondLength * OoXmlHelper.MultipleBondOffsetPercentage;

        private Rect MeasureString(string text, Point startPoint)
        {
            var boundingBox = Rect.Empty;
            var cursor = new Point(startPoint.X, startPoint.Y);

            for (var idx = 0; idx < text.Length; idx++)
            {
                var c = Inputs.TtfCharacterSet[OoXmlHelper.DefaultCharacter];
                var chr = text[idx];
                if (Inputs.TtfCharacterSet.ContainsKey(chr))
                {
                    c = Inputs.TtfCharacterSet[chr];
                }

                if (c != null)
                {
                    var position = GetCharacterPosition(cursor, c);

                    var thisRect = new Rect(new Point(position.X, position.Y),
                                            new Size(OoXmlHelper.ScaleCsTtfToCml(c.Width, Inputs.MeanBondLength),
                                                     OoXmlHelper.ScaleCsTtfToCml(c.Height, Inputs.MeanBondLength)));

                    boundingBox.Union(thisRect);

                    if (idx < text.Length - 1)
                    {
                        // Move to next Character position
                        cursor.Offset(OoXmlHelper.ScaleCsTtfToCml(c.IncrementX, Inputs.MeanBondLength), 0);
                    }
                }
            }

            return boundingBox;
        }

        private void PlaceString(string text, Point startPoint, string path)
        {
            var cursor = new Point(startPoint.X, startPoint.Y);

            for (var idx = 0; idx < text.Length; idx++)
            {
                var c = Inputs.TtfCharacterSet[OoXmlHelper.DefaultCharacter];
                var chr = text[idx];
                if (Inputs.TtfCharacterSet.ContainsKey(chr))
                {
                    c = Inputs.TtfCharacterSet[chr];
                }

                if (c != null)
                {
                    var position = GetCharacterPosition(cursor, c);

                    var alc = new AtomLabelCharacter(position, c, OoXmlHelper.Black, path, path);
                    Outputs.AtomLabelCharacters.Add(alc);

                    if (idx < text.Length - 1)
                    {
                        // Move to next Character position
                        cursor.Offset(OoXmlHelper.ScaleCsTtfToCml(c.IncrementX, Inputs.MeanBondLength), 0);
                    }
                }
            }
        }

        private List<Point> ConvexHull(string atomPath)
        {
            var chars = Outputs.AtomLabelCharacters.Where(m => m.ParentAtom == atomPath).ToList();
            return ConvexHull(chars);
        }

        private List<Point> ConvexHull(List<AtomLabelCharacter> chars)
        {
            var points = new List<Point>();

            var margin = OoXmlHelper.CmlCharacterMargin;
            foreach (var c in chars)
            {
                // Top Left --
                points.Add(new Point(c.Position.X - margin, c.Position.Y - margin));
                if (c.IsSmaller)
                {
                    points.Add(new Point(c.Position.X + OoXmlHelper.ScaleCsTtfToCml(c.Character.Width, Inputs.MeanBondLength) * OoXmlHelper.SubscriptScaleFactor + margin,
                                         c.Position.Y - margin));
                    points.Add(new Point(c.Position.X + OoXmlHelper.ScaleCsTtfToCml(c.Character.Width, Inputs.MeanBondLength) * OoXmlHelper.SubscriptScaleFactor + margin,
                                         c.Position.Y + OoXmlHelper.ScaleCsTtfToCml(c.Character.Height, Inputs.MeanBondLength) * OoXmlHelper.SubscriptScaleFactor + margin));
                    points.Add(new Point(c.Position.X - margin,
                                         c.Position.Y + OoXmlHelper.ScaleCsTtfToCml(c.Character.Height, Inputs.MeanBondLength) * OoXmlHelper.SubscriptScaleFactor + margin));
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
            var position = new Point(cursorPosition.X + OoXmlHelper.ScaleCsTtfToCml(character.OriginX, Inputs.MeanBondLength),
                                     cursorPosition.Y + OoXmlHelper.ScaleCsTtfToCml(character.OriginY, Inputs.MeanBondLength));

            return position;
        }
    }
}