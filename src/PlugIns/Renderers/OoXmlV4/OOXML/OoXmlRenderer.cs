// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Model2;
using Chem4Word.Renderer.OoXmlV4.Entities;
using Chem4Word.Renderer.OoXmlV4.Enums;
using Chem4Word.Renderer.OoXmlV4.TTF;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using IChem4Word.Contracts;
using Newtonsoft.Json;
using A = DocumentFormat.OpenXml.Drawing;
using Drawing = DocumentFormat.OpenXml.Wordprocessing.Drawing;
using Point = System.Windows.Point;
using Run = DocumentFormat.OpenXml.Wordprocessing.Run;
using Wp = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using Wpg = DocumentFormat.OpenXml.Office2010.Word.DrawingGroup;
using Wps = DocumentFormat.OpenXml.Office2010.Word.DrawingShape;

namespace Chem4Word.Renderer.OoXmlV4.OOXML
{
    // ReSharper disable PossiblyMistakenUseOfParamsMethod
    [SuppressMessage("Minor Code Smell", "S3220:Method calls should not resolve ambiguously to overloads with \"params\"", Justification = "<OoXml>")]
    public class OoXmlRenderer
    {
        // DrawingML Units
        // https://startbigthinksmall.wordpress.com/2010/01/04/points-inches-and-emus-measuring-units-in-office-open-xml/
        // EMU Calculator
        // http://lcorneliussen.de/raw/dashboards/ooxml/

        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private Wpg.WordprocessingGroup _wordprocessingGroup;
        private long _ooxmlId;
        private Rect _boundingBoxOfEverything;
        private Rect _boundingBoxOfAllAtoms;

        // Inputs to positioner
        private Dictionary<char, TtfCharacter> _TtfCharacterSet;

        private readonly OoXmlV4Options _options;
        private readonly IChem4WordTelemetry _telemetry;
        private Point _topLeft;
        private readonly Model _chemistryModel;
        private double _medianBondLength;

        // Outputs of positioner
        private PositionerOutputs _positionerOutputs;

        public OoXmlRenderer(Model model, OoXmlV4Options options, IChem4WordTelemetry telemetry, Point topLeft)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            _telemetry = telemetry;
            _telemetry.Write(module, "Verbose", "Called");

            _options = options;
            _topLeft = topLeft;
            _chemistryModel = model;
            _medianBondLength = model.MeanBondLength;

            LoadFont();

            _boundingBoxOfAllAtoms = _chemistryModel.BoundingBoxOfCmlPoints;
        }

        public Run GenerateRun()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            _telemetry.Write(module, "Verbose", "Called");

            Stopwatch swr = new Stopwatch();
            swr.Start();

            // Initialise OoXml Object counter
            _ooxmlId = 1;

            //set the median bond length
            _medianBondLength = _chemistryModel.MeanBondLength;
            if (_chemistryModel.GetAllBonds().Count == 0)
            {
                _medianBondLength = _options.BondLength;
            }

            // Initialise progress monitoring
            Progress progress = new Progress
            {
                TopLeft = _topLeft
            };

            var positioner = new OoXmlPositioner(new PositionerInputs
            {
                Progress = progress,
                Options = _options,
                TtfCharacterSet = _TtfCharacterSet,
                Telemetry = _telemetry,
                MeanBondLength = _medianBondLength,
                Model = _chemistryModel,
            });

            _positionerOutputs = positioner.Position();

            // 6.1  Calculate canvas size
            SetCanvasSize();

            // 6.2  Create Base OoXml Objects
            Run run = CreateRun();

            // 7.   Render Brackets
            // Render molecule grouping brackets
            if (_options.ShowMoleculeGrouping)
            {
                foreach (var group in _positionerOutputs.GroupBrackets)
                {
                    string bracketColour = _options.ColouredAtoms ? "00bbff" : "000000";
                    DrawGroupBrackets(group, _medianBondLength * 0.5, OoXmlHelper.ACS_LINE_WIDTH * 2, bracketColour);
                }
            }

            // Render molecule brackets
            foreach (var moleculeBracket in _positionerOutputs.MoleculeBrackets)
            {
                DrawMoleculeBrackets(moleculeBracket, OoXmlHelper.ACS_LINE_WIDTH, "000000");
            }

            // 8.   Render Diagnostic Markers
            if (_options.ShowMoleculeBoundingBoxes)
            {
                foreach (var item in _positionerOutputs.AllMoleculeExtents)
                {
                    DrawBox(item.AtomExtents, "ff0000", .25);
                    DrawBox(item.InternalCharacterExtents, "00ff00", .25);
                    DrawBox(item.ExternalCharacterExtents, "0000ff", .25);
                }

                DrawBox(_boundingBoxOfAllAtoms, "ff0000", .25);
                DrawBox(_boundingBoxOfEverything, "000000", .25);
            }

            if (_options.ShowHulls)
            {
                foreach (var hull in _positionerOutputs.ConvexHulls)
                {
                    var points = hull.Value.ToList();
                    DrawPolygon(points, "ff0000", 0.25);
                }
            }

            if (_options.ShowCharacterBoundingBoxes)
            {
                foreach (var atom in _chemistryModel.GetAllAtoms())
                {
                    List<AtomLabelCharacter> chars = _positionerOutputs.AtomLabelCharacters.FindAll(a => a.ParentAtom.Equals(atom.Path));
                    Rect atomCharsRect = Rect.Empty;
                    foreach (var alc in chars)
                    {
                        Rect thisBoundingBox = new Rect(alc.Position,
                                                      new Size(OoXmlHelper.ScaleCsTtfToCml(alc.Character.Width, _medianBondLength),
                                                               OoXmlHelper.ScaleCsTtfToCml(alc.Character.Height, _medianBondLength)));
                        if (alc.IsSmaller)
                        {
                            thisBoundingBox = new Rect(alc.Position,
                                                       new Size(OoXmlHelper.ScaleCsTtfToCml(alc.Character.Width, _medianBondLength) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR,
                                                                OoXmlHelper.ScaleCsTtfToCml(alc.Character.Height, _medianBondLength) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR));
                        }

                        DrawBox(thisBoundingBox, "00ff00", 0.25);

                        atomCharsRect.Union(thisBoundingBox);
                    }

                    if (!atomCharsRect.IsEmpty)
                    {
                        DrawBox(atomCharsRect, "ffa500", 0.5);
                    }
                }
            }

            foreach (var rectangle in _positionerOutputs.Diagnostics.Rectangles)
            {
                DrawBox(rectangle.BoundingBox, rectangle.Colour);
            }

            double spotSize = _medianBondLength * OoXmlHelper.MULTIPLE_BOND_OFFSET_PERCENTAGE / 3;

            if (_options.ShowRingCentres)
            {
                foreach (var point in _positionerOutputs.RingCenters)
                {
                    Rect extents = new Rect(new Point(point.X - spotSize, point.Y - spotSize),
                                       new Point(point.X + spotSize, point.Y + spotSize));
                    DrawShape(extents, A.ShapeTypeValues.Ellipse, true, "00ff00");
                }

                // ToDo: MAW - Experimental code to add inner circles for aromatic rings
                foreach (var innerCircle in _positionerOutputs.InnerCircles)
                {
                    var smallerCircle = new InnerCircle();
                    smallerCircle.Centre = innerCircle.Centre;
                    // Move all points towards centre
                    foreach (var point in innerCircle.Points)
                    {
                        var vector = smallerCircle.Centre - point;
                        var innerPoint = point + vector * OoXmlHelper.MULTIPLE_BOND_OFFSET_PERCENTAGE;
                        smallerCircle.Points.Add(innerPoint);

                        Rect extents = new Rect(new Point(innerPoint.X - spotSize, innerPoint.Y - spotSize),
                                                new Point(innerPoint.X + spotSize, innerPoint.Y + spotSize));
                        //DrawShape(extents, A.ShapeTypeValues.Ellipse, false, "ff0000", 0.5)
                    }

                    DrawInnerCircle(smallerCircle, "00ff00", 0.5);
                    //DrawPolygon(smallerCircle.Points, "00ff00", 0.5)
                }
            }

            if (_options.ShowAtomPositions)
            {
                foreach (var atom in _chemistryModel.GetAllAtoms())
                {
                    Rect extents = new Rect(new Point(atom.Position.X - spotSize, atom.Position.Y - spotSize),
                                            new Point(atom.Position.X + spotSize, atom.Position.Y + spotSize));
                    DrawShape(extents, A.ShapeTypeValues.Ellipse, true, "ff0000");
                }
            }

            if (_options.ShowHulls)
            {
                foreach (var hull in _positionerOutputs.ConvexHulls)
                {
                    var points = hull.Value.ToList();
                    DrawPolygon(points, "ff0000", 0.25);
                }
            }

            // 9.1  Calculate and tweak Wedge and Hatch bonds
            var wedges =
                _positionerOutputs.BondLines.Where(t => t.Style == BondLineStyle.Wedge
                                                            || t.Style == BondLineStyle.Hatch).ToList();
            if (wedges.Any())
            {
                // Pass 1 Calculate basic outline
                foreach (var line in wedges)
                {
                    line.CalculateWedgeOutline(_medianBondLength);
                }

                // Pass 2 Adjust if touching another wedge or hatch
                foreach (var wedge in wedges)
                {
                    var tailSharedWith = wedges.Where(t => !t.BondPath.Equals(wedge.BondPath)
                                                           && t.Tail == wedge.Tail).ToList();
                    if (tailSharedWith.Count == 1)
                    {
                        var shared = tailSharedWith[0];

                        var angle = Math.Abs(Vector.AngleBetween(wedge.Nose - wedge.Tail, shared.Nose - shared.Tail));
                        if (Math.Abs(angle) > 100)
                        {
                            Vector v1 = (wedge.Nose - wedge.LeftTail) * 2;
                            Point p1 = wedge.Nose - v1;
                            //_positionerOutputs.Diagnostics.Lines.Add(new DiagnosticLine(wedge.Nose, p1, BondLineStyle.Dotted, "ff0000"))

                            Vector v2 = (shared.Nose - shared.RightTail) * 2;
                            Point p2 = shared.Nose - v2;
                            //_positionerOutputs.Diagnostics.Lines.Add(new DiagnosticLine(shared.Nose, p2, BondLineStyle.Dotted, "ff0000"))

                            bool intersect;
                            Point intersection;

                            CoordinateTool.FindIntersection(wedge.Nose, p1, shared.Nose, p2,
                                                            out _, out intersect, out intersection);
                            if (intersect)
                            {
                                wedge.LeftTail = intersection;
                                shared.RightTail = intersection;
                            }
                        }
                    }
                }
            }

            // 9.2  Render Bond Lines
            foreach (var bondLine in _positionerOutputs.BondLines)
            {
                switch (bondLine.Style)
                {
                    case BondLineStyle.Wedge:
                        //_positionerOutputs.Diagnostics.Polygons.Add(bondLine.WedgeOutline())
                        DrawWedgeBond(bondLine.WedgeOutline(), bondLine.BondPath, bondLine.Colour);
                        break;

                    case BondLineStyle.Hatch:
                        //_positionerOutputs.Diagnostics.Polygons.Add(bondLine.WedgeOutline())
                        DrawHatchBond(bondLine.WedgeOutline(), bondLine.BondPath, bondLine.Colour);
                        break;

                    default:
                        DrawBondLine(bondLine.Start, bondLine.End, bondLine.BondPath, bondLine.Style, bondLine.Colour);
                        break;
                }
            }

            // 10.  Render Atom and Molecule Characters
            foreach (var character in _positionerOutputs.AtomLabelCharacters)
            {
                DrawCharacter(character);
            }

            // 11.  Render Molecule Captions as TextBoxes
            if (_options.ShowMoleculeCaptions && _options.RenderCaptionsAsTextBox)
            {
                foreach (var caption in _positionerOutputs.MoleculeCaptions)
                {
                    //DrawBox(moleculeCpation.Extents, "ff0000", 0.25)
                    //caption.Colour = "ff0000"
                    DrawTextBox(caption.Extents, caption.Value, caption.Colour);
                }
            }

            // Finally draw any debugging diagnostics
            foreach (var line in _positionerOutputs.Diagnostics.Lines)
            {
                DrawBondLine(line.Start, line.End, "", line.Style, line.Colour, 0.5);
            }

            foreach (var polygon in _positionerOutputs.Diagnostics.Polygons)
            {
                DrawPolygon(polygon, "00ff00", 0.25);
            }

            foreach (var spot in _positionerOutputs.Diagnostics.Points)
            {
                double half = spot.Diameter / 2;
                Rect extents = new Rect(new Point(spot.Point.X - half, spot.Point.Y - half),
                                        new Point(spot.Point.X + half, spot.Point.Y + half));
                DrawShape(extents, A.ShapeTypeValues.Ellipse, true, spot.Colour);
            }

            _telemetry.Write(module, "Timing", $"Rendering {_chemistryModel.Molecules.Count} molecules with {_chemistryModel.TotalAtomsCount} atoms and {_chemistryModel.TotalBondsCount} bonds took {SafeDouble.AsString0(swr.ElapsedMilliseconds)} ms; Average Bond Length: {SafeDouble.AsString(_chemistryModel.MeanBondLength)}");

            ShutDownProgress(progress);

            return run;
        }

        public void DrawCharacter(AtomLabelCharacter alc)
        {
            Point characterPosition = new Point(alc.Position.X, alc.Position.Y);
            characterPosition.Offset(-_boundingBoxOfEverything.Left, -_boundingBoxOfEverything.Top);

            Int64Value emuWidth = OoXmlHelper.ScaleCsTtfToEmu(alc.Character.Width, _medianBondLength);
            Int64Value emuHeight = OoXmlHelper.ScaleCsTtfToEmu(alc.Character.Height, _medianBondLength);
            if (alc.IsSmaller)
            {
                emuWidth = OoXmlHelper.ScaleCsTtfSubScriptToEmu(alc.Character.Width, _medianBondLength);
                emuHeight = OoXmlHelper.ScaleCsTtfSubScriptToEmu(alc.Character.Height, _medianBondLength);
            }
            Int64Value emuTop = OoXmlHelper.ScaleCmlToEmu(characterPosition.Y);
            Int64Value emuLeft = OoXmlHelper.ScaleCmlToEmu(characterPosition.X);

            string parent = alc.ParentAtom.Equals(alc.ParentMolecule) ? alc.ParentMolecule : alc.ParentAtom;
            string shapeName = $"Character {alc.Character.Character} of {parent}";
            Wps.WordprocessingShape wordprocessingShape = CreateShape(_ooxmlId++, shapeName);
            Wps.ShapeProperties shapeProperties = CreateShapeProperties(wordprocessingShape, emuTop, emuLeft, emuWidth, emuHeight);

            // Start of the lines

            A.PathList pathList = new A.PathList();

            A.Path path = new A.Path { Width = emuWidth, Height = emuHeight };

            foreach (TtfContour contour in alc.Character.Contours)
            {
                int i = 0;

                while (i < contour.Points.Count)
                {
                    TtfPoint thisPoint = contour.Points[i];
                    TtfPoint nextPoint = null;
                    if (i < contour.Points.Count - 1)
                    {
                        nextPoint = contour.Points[i + 1];
                    }

                    switch (thisPoint.Type)
                    {
                        case TtfPoint.PointType.Start:
                            A.MoveTo moveTo = new A.MoveTo();
                            if (alc.IsSmaller)
                            {
                                A.Point point = MakeSubscriptPoint(thisPoint);
                                moveTo.Append(point);
                                path.Append(moveTo);
                            }
                            else
                            {
                                A.Point point = MakeNormalPoint(thisPoint);
                                moveTo.Append(point);
                                path.Append(moveTo);
                            }
                            i++;
                            break;

                        case TtfPoint.PointType.Line:
                            A.LineTo lineTo = new A.LineTo();
                            if (alc.IsSmaller)
                            {
                                A.Point point = MakeSubscriptPoint(thisPoint);
                                lineTo.Append(point);
                                path.Append(lineTo);
                            }
                            else
                            {
                                A.Point point = MakeNormalPoint(thisPoint);
                                lineTo.Append(point);
                                path.Append(lineTo);
                            }
                            i++;
                            break;

                        case TtfPoint.PointType.CurveOff:
                            A.QuadraticBezierCurveTo quadraticBezierCurveTo = new A.QuadraticBezierCurveTo();
                            if (alc.IsSmaller)
                            {
                                A.Point pointA = MakeSubscriptPoint(thisPoint);
                                A.Point pointB = MakeSubscriptPoint(nextPoint);
                                quadraticBezierCurveTo.Append(pointA);
                                quadraticBezierCurveTo.Append(pointB);
                                path.Append(quadraticBezierCurveTo);
                            }
                            else
                            {
                                A.Point pointA = MakeNormalPoint(thisPoint);
                                A.Point pointB = MakeNormalPoint(nextPoint);
                                quadraticBezierCurveTo.Append(pointA);
                                quadraticBezierCurveTo.Append(pointB);
                                path.Append(quadraticBezierCurveTo);
                            }
                            i++;
                            i++;
                            break;

                        case TtfPoint.PointType.CurveOn:
                            // Should never get here !
                            i++;
                            break;
                    }
                }

                A.CloseShapePath closeShapePath = new A.CloseShapePath();
                path.Append(closeShapePath);
            }

            pathList.Append(path);

            // End of the lines

            A.SolidFill solidFill = new A.SolidFill();

            // Set Colour
            A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = alc.Colour };
            solidFill.Append(rgbColorModelHex);

            shapeProperties.Append(CreateCustomGeometry(pathList));
            shapeProperties.Append(solidFill);

            wordprocessingShape.Append(CreateShapeStyle());

            Wps.TextBodyProperties textBodyProperties = new Wps.TextBodyProperties();
            wordprocessingShape.Append(textBodyProperties);

            _wordprocessingGroup.Append(wordprocessingShape);

            // Local Functions
            A.Point MakeSubscriptPoint(TtfPoint ttfPoint)
            {
                A.Point pp = new A.Point
                {
                    X = $"{OoXmlHelper.ScaleCsTtfSubScriptToEmu(ttfPoint.X - alc.Character.OriginX, _medianBondLength)}",
                    Y = $"{OoXmlHelper.ScaleCsTtfSubScriptToEmu(alc.Character.Height + ttfPoint.Y - (alc.Character.Height + alc.Character.OriginY), _medianBondLength)}"
                };
                return pp;
            }

            A.Point MakeNormalPoint(TtfPoint ttfPoint)
            {
                A.Point pp = new A.Point
                {
                    X = $"{OoXmlHelper.ScaleCsTtfToEmu(ttfPoint.X - alc.Character.OriginX, _medianBondLength)}",
                    Y = $"{OoXmlHelper.ScaleCsTtfToEmu(alc.Character.Height + ttfPoint.Y - (alc.Character.Height + alc.Character.OriginY), _medianBondLength)}"
                };
                return pp;
            }
        }

        private void DrawBondLine(Point bondStart, Point bondEnd, string bondPath,
                                  BondLineStyle lineStyle = BondLineStyle.Solid,
                                  string colour = "000000",
                                  double lineWidth = OoXmlHelper.ACS_LINE_WIDTH)
        {
            switch (lineStyle)
            {
                case BondLineStyle.Solid:
                case BondLineStyle.Zero:
                case BondLineStyle.Half:
                case BondLineStyle.Dotted: // Diagnostics
                case BondLineStyle.Dashed: // Diagnostics
                    DrawStraightLine(bondStart, bondEnd, bondPath, lineStyle, colour, lineWidth);
                    break;

                case BondLineStyle.Wavy:
                    DrawWavyLine(bondStart, bondEnd, bondPath, colour);
                    break;

                default:
                    DrawStraightLine(bondStart, bondEnd, bondPath, BondLineStyle.Zero, "00ff00", lineWidth);
                    break;
            }
        }

        private List<SimpleLine> CreateHatchLines(List<Point> points)
        {
            List<SimpleLine> lines = new List<SimpleLine>();

            Point wedgeStart = points[0];
            Point wedgeEndMiddle = points[2];

            // Vector pointing from wedgeStart to wedgeEndMiddle
            Vector direction = wedgeEndMiddle - wedgeStart;
            Matrix rightAngles = new Matrix();
            rightAngles.Rotate(90);
            Vector perpendicular = direction * rightAngles;

            Vector step = direction;
            step.Normalize();
            step *= OoXmlHelper.ScaleCmlToEmu(15 * OoXmlHelper.MULTIPLE_BOND_OFFSET_PERCENTAGE);

            int steps = (int)Math.Ceiling(direction.Length / step.Length);
            double stepLength = direction.Length / steps;

            step.Normalize();
            step *= stepLength;

            Point p0 = wedgeStart + step;
            Point p1 = p0 + perpendicular;
            Point p2 = p0 - perpendicular;

            var r = GeometryTool.ClipLineWithPolygon(p1, p2, points, out _);
            while (r.Length > 2)
            {
                if (r.Length == 4)
                {
                    lines.Add(new SimpleLine(r[1], r[2]));
                }

                if (r.Length == 6)
                {
                    lines.Add(new SimpleLine(r[1], r[2]));
                    lines.Add(new SimpleLine(r[3], r[4]));
                }

                p0 = p0 + step;
                p1 = p0 + perpendicular;
                p2 = p0 - perpendicular;

                r = GeometryTool.ClipLineWithPolygon(p1, p2, points, out _);
            }

            // Define Tail Lines
            lines.Add(new SimpleLine(wedgeEndMiddle, points[1]));
            lines.Add(new SimpleLine(wedgeEndMiddle, points[3]));

            return lines;
        }

        private void DrawTextBox(Rect cmlExtents, string value, string colour)
        {
            Int64Value emuWidth = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Width);
            Int64Value emuHeight = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Height);
            Int64Value emuTop = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Top);
            Int64Value emuLeft = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Left);

            Point location = new Point(emuLeft, emuTop);
            Size size = new Size(emuWidth, emuHeight);
            location.Offset(OoXmlHelper.ScaleCmlToEmu(-_boundingBoxOfEverything.Left), OoXmlHelper.ScaleCmlToEmu(-_boundingBoxOfEverything.Top));
            Rect boundingBox = new Rect(location, size);

            emuWidth = (Int64Value)boundingBox.Width;
            emuHeight = (Int64Value)boundingBox.Height;
            emuTop = (Int64Value)boundingBox.Top;
            emuLeft = (Int64Value)boundingBox.Left;

            UInt32Value id = UInt32Value.FromUInt32((uint)_ooxmlId++);
            string shapeName = "String " + id;
            Wps.WordprocessingShape wordprocessingShape = CreateShape(id, shapeName);

            Wps.ShapeProperties shapeProperties = new Wps.ShapeProperties();

            A.Transform2D transform2D = new A.Transform2D();
            A.Offset offset = new A.Offset { X = emuLeft, Y = emuTop };
            A.Extents extents = new A.Extents { Cx = emuWidth, Cy = emuHeight };
            transform2D.Append(offset);
            transform2D.Append(extents);
            shapeProperties.Append(transform2D);

            A.AdjustValueList adjustValueList = new A.AdjustValueList();
            A.PresetGeometry presetGeometry = new A.PresetGeometry { Preset = A.ShapeTypeValues.Rectangle };
            presetGeometry.Append(adjustValueList);
            shapeProperties.Append(presetGeometry);

            // The TextBox

            Wps.TextBoxInfo2 textBoxInfo2 = new Wps.TextBoxInfo2();
            TextBoxContent textBoxContent = new TextBoxContent();
            textBoxInfo2.Append(textBoxContent);

            // The Paragrah
            Paragraph paragraph = new Paragraph();
            textBoxContent.Append(paragraph);

            ParagraphProperties paragraphProperties = new ParagraphProperties();
            Justification justification = new Justification { Val = JustificationValues.Center };
            paragraphProperties.Append(justification);

            paragraph.Append(paragraphProperties);

            // Now for the text Run
            Run run = new Run();
            paragraph.Append(run);
            RunProperties runProperties = new RunProperties();
            runProperties.Append(CommonRunProperties());

            run.Append(runProperties);

            Text text = new Text(value);
            run.Append(text);

            wordprocessingShape.Append(shapeProperties);
            wordprocessingShape.Append(textBoxInfo2);

            Wps.TextBodyProperties textBodyProperties = new Wps.TextBodyProperties { LeftInset = 0, TopInset = 0, RightInset = 0, BottomInset = 0 };
            wordprocessingShape.Append(textBodyProperties);

            _wordprocessingGroup.Append(wordprocessingShape);

            OpenXmlElement[] CommonRunProperties()
            {
                var result = new List<OpenXmlElement>();

                var pointSize = OoXmlHelper.EmusPerCsTtfPoint(_medianBondLength) * 2;

                RunFonts runFonts = new RunFonts { Ascii = "Arial", HighAnsi = "Arial" };
                result.Add(runFonts);

                Color color = new Color { Val = colour };
                result.Add(color);

                FontSize fontSize1 = new FontSize { Val = pointSize.ToString("0") };
                result.Add(fontSize1);

                return result.ToArray();
            }
        }

        private void DrawShape(Rect cmlExtents, A.ShapeTypeValues shape, bool filled, string colour,
                               double outlineWidth = OoXmlHelper.ACS_LINE_WIDTH)
        {
            Int64Value emuWidth = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Width);
            Int64Value emuHeight = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Height);
            Int64Value emuTop = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Top);
            Int64Value emuLeft = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Left);

            Point location = new Point(emuLeft, emuTop);
            Size size = new Size(emuWidth, emuHeight);
            location.Offset(OoXmlHelper.ScaleCmlToEmu(-_boundingBoxOfEverything.Left), OoXmlHelper.ScaleCmlToEmu(-_boundingBoxOfEverything.Top));
            Rect boundingBox = new Rect(location, size);

            emuWidth = (Int64Value)boundingBox.Width;
            emuHeight = (Int64Value)boundingBox.Height;
            emuTop = (Int64Value)boundingBox.Top;
            emuLeft = (Int64Value)boundingBox.Left;

            UInt32Value id = UInt32Value.FromUInt32((uint)_ooxmlId++);
            string shapeName = "Shape" + id;
            Wps.WordprocessingShape wordprocessingShape = CreateShape(id, shapeName);

            Wps.ShapeProperties shapeProperties = new Wps.ShapeProperties();

            A.Transform2D transform2D = new A.Transform2D();
            A.Offset offset = new A.Offset { X = emuLeft, Y = emuTop };
            A.Extents extents = new A.Extents { Cx = emuWidth, Cy = emuHeight };
            transform2D.Append(offset);
            transform2D.Append(extents);
            shapeProperties.Append(transform2D);

            A.AdjustValueList adjustValueList = new A.AdjustValueList();
            A.PresetGeometry presetGeometry = new A.PresetGeometry { Preset = shape };
            presetGeometry.Append(adjustValueList);
            shapeProperties.Append(presetGeometry);

            if (filled)
            {
                // Set shape fill colour
                A.SolidFill solidFill = new A.SolidFill();
                A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = colour };
                solidFill.Append(rgbColorModelHex);
                shapeProperties.Append(solidFill);
            }
            else
            {
                // Set shape outline and colour
                Int32Value emuLineWidth = (Int32Value)(outlineWidth * OoXmlHelper.EMUS_PER_WORD_POINT);
                A.Outline outline = new A.Outline { Width = emuLineWidth, CapType = A.LineCapValues.Round };
                A.RgbColorModelHex rgbColorModelHex2 = new A.RgbColorModelHex { Val = colour };
                A.SolidFill outlineFill = new A.SolidFill();
                outlineFill.Append(rgbColorModelHex2);
                outline.Append(outlineFill);
                shapeProperties.Append(outline);
            }

            wordprocessingShape.Append(shapeProperties);
            wordprocessingShape.Append(CreateShapeStyle());

            Wps.TextBodyProperties textBodyProperties = new Wps.TextBodyProperties();
            wordprocessingShape.Append(textBodyProperties);

            _wordprocessingGroup.Append(wordprocessingShape);
        }

        private void DrawHatchBond(List<Point> points, string bondPath,
                                   string colour = "000000")
        {
            Rect cmlExtents = new Rect(points[0], points[points.Count - 1]);

            for (int i = 0; i < points.Count - 1; i++)
            {
                cmlExtents.Union(new Rect(points[i], points[i + 1]));
            }

            // Move Extents to have 0,0 Top Left Reference
            cmlExtents.Offset(-_boundingBoxOfEverything.Left, -_boundingBoxOfEverything.Top);

            Int64Value emuTop = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Top);
            Int64Value emuLeft = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Left);
            Int64Value emuWidth = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Width);
            Int64Value emuHeight = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Height);

            string shapeName = "Hatch " + bondPath;

            Wps.WordprocessingShape wordprocessingShape = CreateShape(_ooxmlId++, shapeName);
            Wps.ShapeProperties shapeProperties = CreateShapeProperties(wordprocessingShape, emuTop, emuLeft, emuWidth, emuHeight);

            // Start of the lines

            A.PathList pathList = new A.PathList();

            // Draw a small circle for the starting point
            var xx = 0.5;
            Rect extents = new Rect(new Point(points[0].X - xx, points[0].Y - xx), new Point(points[0].X + xx, points[0].Y + xx));
            DrawShape(extents, A.ShapeTypeValues.Ellipse, true, colour);

            // Pre offset and scale the extents
            var scaledPoints = new List<Point>();
            foreach (var point in points)
            {
                point.Offset(-_boundingBoxOfEverything.Left, -_boundingBoxOfEverything.Top);
                point.Offset(-cmlExtents.Left, -cmlExtents.Top);
                scaledPoints.Add(new Point(OoXmlHelper.ScaleCmlToEmu(point.X), OoXmlHelper.ScaleCmlToEmu(point.Y)));
            }

            var lines = CreateHatchLines(scaledPoints);

            foreach (var line in lines)
            {
                A.Path path = new A.Path { Width = emuWidth, Height = emuHeight };

                A.MoveTo moveTo = new A.MoveTo();
                A.Point startPoint = new A.Point
                {
                    X = line.Start.X.ToString("0"),
                    Y = line.Start.Y.ToString("0")
                };

                moveTo.Append(startPoint);
                path.Append(moveTo);

                A.LineTo lineTo = new A.LineTo();
                A.Point endPoint = new A.Point
                {
                    X = line.End.X.ToString("0"),
                    Y = line.End.Y.ToString("0")
                };
                lineTo.Append(endPoint);
                path.Append(lineTo);

                pathList.Append(path);
            }

            // End of the lines

            shapeProperties.Append(CreateCustomGeometry(pathList));

            // Set shape fill colour
            A.SolidFill insideFill = new A.SolidFill();
            A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = colour };
            insideFill.Append(rgbColorModelHex);

            shapeProperties.Append(insideFill);

            // Set shape outline colour
            A.Outline outline = new A.Outline { Width = Int32Value.FromInt32((int)OoXmlHelper.ACS_LINE_WIDTH_EMUS), CapType = A.LineCapValues.Round };
            A.RgbColorModelHex rgbColorModelHex2 = new A.RgbColorModelHex { Val = colour };
            A.SolidFill outlineFill = new A.SolidFill();
            outlineFill.Append(rgbColorModelHex2);
            outline.Append(outlineFill);

            shapeProperties.Append(outline);

            wordprocessingShape.Append(CreateShapeStyle());

            Wps.TextBodyProperties textBodyProperties = new Wps.TextBodyProperties();
            wordprocessingShape.Append(textBodyProperties);

            _wordprocessingGroup.Append(wordprocessingShape);
        }

        private A.Point MakePoint(Point pp, Rect cmlExtents)
        {
            pp.Offset(-_boundingBoxOfEverything.Left, -_boundingBoxOfEverything.Top);
            pp.Offset(-cmlExtents.Left, -cmlExtents.Top);
            return new A.Point
            {
                X = $"{OoXmlHelper.ScaleCmlToEmu(pp.X)}",
                Y = $"{OoXmlHelper.ScaleCmlToEmu(pp.Y)}"
            };
        }

        private void DrawWedgeBond(List<Point> points, string bondPath,
                                   string colour = "000000")
        {
            Rect cmlExtents = new Rect(points[0], points[points.Count - 1]);

            for (int i = 0; i < points.Count - 1; i++)
            {
                cmlExtents.Union(new Rect(points[i], points[i + 1]));
            }

            // Move Extents to have 0,0 Top Left Reference
            cmlExtents.Offset(-_boundingBoxOfEverything.Left, -_boundingBoxOfEverything.Top);

            Int64Value emuTop = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Top);
            Int64Value emuLeft = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Left);
            Int64Value emuWidth = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Width);
            Int64Value emuHeight = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Height);

            string shapeName = "Wedge " + bondPath;

            Wps.WordprocessingShape wordprocessingShape = CreateShape(_ooxmlId++, shapeName);
            Wps.ShapeProperties shapeProperties = CreateShapeProperties(wordprocessingShape, emuTop, emuLeft, emuWidth, emuHeight);

            // Start of the lines

            A.PathList pathList = new A.PathList();

            A.Path path = new A.Path { Width = emuWidth, Height = emuHeight };

            A.MoveTo moveTo = new A.MoveTo();
            moveTo.Append(MakePoint(points[0], cmlExtents));
            path.Append(moveTo);

            for (int i = 1; i < points.Count; i++)
            {
                A.LineTo lineTo = new A.LineTo();
                lineTo.Append(MakePoint(points[i], cmlExtents));
                path.Append(lineTo);
            }

            A.CloseShapePath closeShapePath = new A.CloseShapePath();
            path.Append(closeShapePath);

            pathList.Append(path);

            // End of the lines

            shapeProperties.Append(CreateCustomGeometry(pathList));

            // Set shape fill colour
            A.SolidFill insideFill = new A.SolidFill();
            A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = colour };
            insideFill.Append(rgbColorModelHex);

            shapeProperties.Append(insideFill);

            // Set shape outline colour
            A.Outline outline = new A.Outline { Width = Int32Value.FromInt32((int)OoXmlHelper.ACS_LINE_WIDTH_EMUS), CapType = A.LineCapValues.Round };
            A.RgbColorModelHex rgbColorModelHex2 = new A.RgbColorModelHex { Val = colour };
            A.SolidFill outlineFill = new A.SolidFill();
            outlineFill.Append(rgbColorModelHex2);
            outline.Append(outlineFill);

            shapeProperties.Append(outline);

            wordprocessingShape.Append(CreateShapeStyle());

            Wps.TextBodyProperties textBodyProperties = new Wps.TextBodyProperties();
            wordprocessingShape.Append(textBodyProperties);

            _wordprocessingGroup.Append(wordprocessingShape);
        }

        private void DrawMoleculeBrackets(Rect cmlExtents, double lineWidth, string lineColour)
        {
            Int64Value emuWidth = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Width);
            Int64Value emuHeight = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Height);
            Int64Value emuTop = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Top);
            Int64Value emuLeft = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Left);

            Point location = new Point(emuLeft, emuTop);
            Size size = new Size(emuWidth, emuHeight);
            location.Offset(OoXmlHelper.ScaleCmlToEmu(-_boundingBoxOfEverything.Left), OoXmlHelper.ScaleCmlToEmu(-_boundingBoxOfEverything.Top));
            Rect boundingBox = new Rect(location, size);

            emuWidth = (Int64Value)boundingBox.Width;
            emuHeight = (Int64Value)boundingBox.Height;
            emuTop = (Int64Value)boundingBox.Top;
            emuLeft = (Int64Value)boundingBox.Left;

            string shapeName = "Box " + _ooxmlId++;

            Wps.WordprocessingShape wordprocessingShape = CreateShape(_ooxmlId, shapeName);
            Wps.ShapeProperties shapeProperties = CreateShapeProperties(wordprocessingShape, emuTop, emuLeft, emuWidth, emuHeight);

            // Start of the lines

            A.PathList pathList = new A.PathList();

            double gap = boundingBox.Width * 0.8;
            double leftSide = (emuWidth - gap) / 2;
            double rightSide = emuWidth - leftSide;

            // Left Path
            A.Path path1 = new A.Path { Width = emuWidth, Height = emuHeight };

            A.MoveTo moveTo = new A.MoveTo();
            A.Point point1 = new A.Point { X = leftSide.ToString("0"), Y = "0" };
            moveTo.Append(point1);

            // Mid Point
            A.LineTo lineTo1 = new A.LineTo();
            A.Point point2 = new A.Point { X = "0", Y = "0" };
            lineTo1.Append(point2);

            // Last Point
            A.LineTo lineTo2 = new A.LineTo();
            A.Point point3 = new A.Point { X = "0", Y = boundingBox.Height.ToString("0") };
            lineTo2.Append(point3);

            // Mid Point
            A.LineTo lineTo3 = new A.LineTo();
            A.Point point4 = new A.Point { X = leftSide.ToString("0"), Y = boundingBox.Height.ToString("0") };
            lineTo3.Append(point4);

            path1.Append(moveTo);
            path1.Append(lineTo1);
            path1.Append(lineTo2);
            path1.Append(lineTo3);

            pathList.Append(path1);

            // Right Path
            A.Path path2 = new A.Path { Width = emuWidth, Height = emuHeight };

            A.MoveTo moveTo2 = new A.MoveTo();
            A.Point point5 = new A.Point { X = rightSide.ToString("0"), Y = "0" };
            moveTo2.Append(point5);

            // Mid Point
            A.LineTo lineTo4 = new A.LineTo();
            A.Point point6 = new A.Point { X = boundingBox.Width.ToString("0"), Y = "0" };
            lineTo4.Append(point6);

            // Last Point
            A.LineTo lineTo5 = new A.LineTo();
            A.Point point7 = new A.Point { X = boundingBox.Width.ToString("0"), Y = boundingBox.Height.ToString("0") };
            lineTo5.Append(point7);

            // Mid Point
            A.LineTo lineTo6 = new A.LineTo();
            A.Point point8 = new A.Point { X = rightSide.ToString("0"), Y = boundingBox.Height.ToString("0") };
            lineTo6.Append(point8);

            path2.Append(moveTo2);
            path2.Append(lineTo4);
            path2.Append(lineTo5);
            path2.Append(lineTo6);

            pathList.Append(path2);

            // End of the lines

            shapeProperties.Append(CreateCustomGeometry(pathList));

            Int32Value emuLineWidth = (Int32Value)(lineWidth * OoXmlHelper.EMUS_PER_WORD_POINT);
            A.Outline outline = new A.Outline { Width = emuLineWidth, CapType = A.LineCapValues.Round };

            A.SolidFill solidFill = new A.SolidFill();
            A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = lineColour };
            solidFill.Append(rgbColorModelHex);
            outline.Append(solidFill);

            shapeProperties.Append(outline);

            wordprocessingShape.Append(CreateShapeStyle());

            Wps.TextBodyProperties textBodyProperties = new Wps.TextBodyProperties();
            wordprocessingShape.Append(textBodyProperties);

            _wordprocessingGroup.Append(wordprocessingShape);
        }

        private void DrawGroupBrackets(Rect cmlExtents, double armLength, double lineWidth, string lineColour)
        {
            if (cmlExtents != Rect.Empty)
            {
                Int64Value emuWidth = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Width);
                Int64Value emuHeight = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Height);
                Int64Value emuTop = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Top);
                Int64Value emuLeft = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Left);

                Point location = new Point(emuLeft, emuTop);
                Size size = new Size(emuWidth, emuHeight);
                location.Offset(OoXmlHelper.ScaleCmlToEmu(-_boundingBoxOfEverything.Left), OoXmlHelper.ScaleCmlToEmu(-_boundingBoxOfEverything.Top));
                Rect boundingBox = new Rect(location, size);
                Int64Value armLengthEmu = OoXmlHelper.ScaleCmlToEmu(armLength);

                emuWidth = (Int64Value)boundingBox.Width;
                emuHeight = (Int64Value)boundingBox.Height;
                emuTop = (Int64Value)boundingBox.Top;
                emuLeft = (Int64Value)boundingBox.Left;

                string shapeName = "Box " + _ooxmlId++;

                Wps.WordprocessingShape wordprocessingShape = CreateShape(_ooxmlId, shapeName);
                Wps.ShapeProperties shapeProperties = CreateShapeProperties(wordprocessingShape, emuTop, emuLeft, emuWidth, emuHeight);

                // Start of the lines

                A.PathList pathList = new A.PathList();

                pathList.Append(MakeCorner(boundingBox, "TopLeft", armLengthEmu));
                pathList.Append(MakeCorner(boundingBox, "TopRight", armLengthEmu));
                pathList.Append(MakeCorner(boundingBox, "BottomLeft", armLengthEmu));
                pathList.Append(MakeCorner(boundingBox, "BottomRight", armLengthEmu));

                // End of the lines

                shapeProperties.Append(CreateCustomGeometry(pathList));

                Int32Value emuLineWidth = (Int32Value)(lineWidth * OoXmlHelper.EMUS_PER_WORD_POINT);
                A.Outline outline = new A.Outline { Width = emuLineWidth, CapType = A.LineCapValues.Round };

                A.SolidFill solidFill = new A.SolidFill();
                A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = lineColour };
                solidFill.Append(rgbColorModelHex);
                outline.Append(solidFill);

                shapeProperties.Append(outline);

                wordprocessingShape.Append(CreateShapeStyle());

                Wps.TextBodyProperties textBodyProperties = new Wps.TextBodyProperties();
                wordprocessingShape.Append(textBodyProperties);

                _wordprocessingGroup.Append(wordprocessingShape);

                // Local function
                A.Path MakeCorner(Rect bbRect, string corner, double armsSize)
                {
                    var path = new A.Path { Width = (Int64Value)bbRect.Width, Height = (Int64Value)bbRect.Height };

                    A.Point p0 = new A.Point();
                    A.Point p1 = new A.Point();
                    A.Point p2 = new A.Point();

                    switch (corner)
                    {
                        case "TopLeft":
                            p0 = new A.Point
                            {
                                X = armsSize.ToString("0"),
                                Y = "0"
                            };
                            p1 = new A.Point
                            {
                                X = "0",
                                Y = "0"
                            };
                            p2 = new A.Point
                            {
                                X = "0",
                                Y = armsSize.ToString("0")
                            };
                            break;

                        case "TopRight":
                            p0 = new A.Point
                            {
                                X = (bbRect.Width - armsSize).ToString("0"),
                                Y = "0"
                            };
                            p1 = new A.Point
                            {
                                X = bbRect.Width.ToString("0"),
                                Y = "0"
                            };
                            p2 = new A.Point
                            {
                                X = bbRect.Width.ToString("0"),
                                Y = armsSize.ToString("0")
                            };
                            break;

                        case "BottomLeft":
                            p0 = new A.Point
                            {
                                X = "0",
                                Y = (bbRect.Height - armsSize).ToString("0")
                            };
                            p1 = new A.Point
                            {
                                X = "0",
                                Y = bbRect.Height.ToString("0")
                            };
                            p2 = new A.Point
                            {
                                X = armsSize.ToString("0"),
                                Y = bbRect.Height.ToString("0")
                            };
                            break;

                        case "BottomRight":
                            p0 = new A.Point
                            {
                                X = bbRect.Width.ToString("0"),
                                Y = (bbRect.Height - armsSize).ToString("0")
                            };
                            p1 = new A.Point
                            {
                                X = bbRect.Width.ToString("0"),
                                Y = bbRect.Height.ToString("0")
                            };
                            p2 = new A.Point
                            {
                                X = (bbRect.Width - armsSize).ToString("0"),
                                Y = bbRect.Height.ToString("0")
                            };
                            break;
                    }

                    var moveTo = new A.MoveTo();
                    moveTo.Append(p0);
                    path.Append(moveTo);

                    var lineTo1 = new A.LineTo();
                    lineTo1.Append(p1);
                    path.Append(lineTo1);

                    var lineTo2 = new A.LineTo();
                    lineTo2.Append(p2);
                    path.Append(lineTo2);

                    return path;
                }
            }
        }

        private void DrawBox(Rect cmlExtents,
                             string lineColour = "000000",
                             double lineWidth = OoXmlHelper.ACS_LINE_WIDTH)
        {
            if (cmlExtents != Rect.Empty)
            {
                Int64Value emuWidth = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Width);
                Int64Value emuHeight = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Height);
                Int64Value emuTop = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Top);
                Int64Value emuLeft = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Left);

                Point location = new Point(emuLeft, emuTop);
                Size size = new Size(emuWidth, emuHeight);
                location.Offset(OoXmlHelper.ScaleCmlToEmu(-_boundingBoxOfEverything.Left), OoXmlHelper.ScaleCmlToEmu(-_boundingBoxOfEverything.Top));
                Rect boundingBox = new Rect(location, size);

                emuWidth = (Int64Value)boundingBox.Width;
                emuHeight = (Int64Value)boundingBox.Height;
                emuTop = (Int64Value)boundingBox.Top;
                emuLeft = (Int64Value)boundingBox.Left;

                string shapeName = "Box " + _ooxmlId++;

                Wps.WordprocessingShape wordprocessingShape = CreateShape(_ooxmlId, shapeName);
                Wps.ShapeProperties shapeProperties = CreateShapeProperties(wordprocessingShape, emuTop, emuLeft, emuWidth, emuHeight);

                // Start of the lines

                A.PathList pathList = new A.PathList();

                A.Path path = new A.Path { Width = emuWidth, Height = emuHeight };

                // Starting Point
                A.MoveTo moveTo = new A.MoveTo();
                A.Point point1 = new A.Point { X = "0", Y = "0" };
                moveTo.Append(point1);

                // Mid Point
                A.LineTo lineTo1 = new A.LineTo();
                A.Point point2 = new A.Point { X = boundingBox.Width.ToString("0"), Y = "0" };
                lineTo1.Append(point2);

                // Mid Point
                A.LineTo lineTo2 = new A.LineTo();
                A.Point point3 = new A.Point { X = boundingBox.Width.ToString("0"), Y = boundingBox.Height.ToString("0") };
                lineTo2.Append(point3);

                // Last Point
                A.LineTo lineTo3 = new A.LineTo();
                A.Point point4 = new A.Point { X = "0", Y = boundingBox.Height.ToString("0") };
                lineTo3.Append(point4);

                // Back to Start Point
                A.LineTo lineTo4 = new A.LineTo();
                A.Point point5 = new A.Point { X = "0", Y = "0" };
                lineTo4.Append(point5);

                path.Append(moveTo);
                path.Append(lineTo1);
                path.Append(lineTo2);
                path.Append(lineTo3);
                path.Append(lineTo4);

                pathList.Append(path);

                // End of the lines

                shapeProperties.Append(CreateCustomGeometry(pathList));

                Int32Value emuLineWidth = (Int32Value)(lineWidth * OoXmlHelper.EMUS_PER_WORD_POINT);
                A.Outline outline = new A.Outline { Width = emuLineWidth, CapType = A.LineCapValues.Round };

                A.SolidFill solidFill = new A.SolidFill();
                A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = lineColour };
                solidFill.Append(rgbColorModelHex);
                outline.Append(solidFill);

                shapeProperties.Append(outline);

                wordprocessingShape.Append(CreateShapeStyle());

                Wps.TextBodyProperties textBodyProperties = new Wps.TextBodyProperties();
                wordprocessingShape.Append(textBodyProperties);

                _wordprocessingGroup.Append(wordprocessingShape);
            }
        }

        private void DrawInnerCircle(InnerCircle innerCircle, string lineColour, double lineWidth)
        {
            Rect cmlExtents = new Rect(innerCircle.Points[0], innerCircle.Points[innerCircle.Points.Count - 1]);

            for (int i = 0; i < innerCircle.Points.Count - 1; i++)
            {
                cmlExtents.Union(new Rect(innerCircle.Points[i], innerCircle.Points[i + 1]));
            }

            // Move Extents to have 0,0 Top Left Reference
            cmlExtents.Offset(-_boundingBoxOfEverything.Left, -_boundingBoxOfEverything.Top);

            Int64Value emuTop = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Top);
            Int64Value emuLeft = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Left);
            Int64Value emuWidth = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Width);
            Int64Value emuHeight = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Height);

            long id = _ooxmlId++;
            string shapeName = "InnerCircle " + id;

            List<Point> allpoints = new List<Point>();

            var startingPoint = CoordinateTool.GetMidPoint(innerCircle.Points[innerCircle.Points.Count - 1], innerCircle.Points[0]);
            var leftPoint = startingPoint;
            var middlePoint = innerCircle.Points[0];
            var rightPoint = CoordinateTool.GetMidPoint(innerCircle.Points[0], innerCircle.Points[1]);
            allpoints.Add(leftPoint);
            allpoints.Add(middlePoint);
            allpoints.Add(rightPoint);

            for (int i = 1; i < innerCircle.Points.Count - 1; i++)
            {
                leftPoint = CoordinateTool.GetMidPoint(innerCircle.Points[i - 1], innerCircle.Points[i]);
                middlePoint = innerCircle.Points[i];
                rightPoint = CoordinateTool.GetMidPoint(innerCircle.Points[i], innerCircle.Points[i + 1]);

                allpoints.Add(leftPoint);
                allpoints.Add(middlePoint);
                allpoints.Add(rightPoint);
            }

            leftPoint = CoordinateTool.GetMidPoint(innerCircle.Points[innerCircle.Points.Count - 2], innerCircle.Points[innerCircle.Points.Count - 1]);
            middlePoint = innerCircle.Points[innerCircle.Points.Count - 1];
            rightPoint = CoordinateTool.GetMidPoint(innerCircle.Points[innerCircle.Points.Count - 1], innerCircle.Points[0]);
            allpoints.Add(leftPoint);
            allpoints.Add(middlePoint);
            allpoints.Add(rightPoint);

            Wps.WordprocessingShape wordprocessingShape = CreateShape(id, shapeName);
            Wps.ShapeProperties shapeProperties = CreateShapeProperties(wordprocessingShape, emuTop, emuLeft, emuWidth, emuHeight);

            // Start of the lines

            A.PathList pathList = new A.PathList();

            A.Path path = new A.Path { Width = emuWidth, Height = emuHeight };

            // First point
            A.MoveTo moveTo = new A.MoveTo();
            moveTo.Append(MakePoint(startingPoint, cmlExtents));
            path.Append(moveTo);

            // Straight Lines
            foreach (var p in allpoints)
            {
                //A.LineTo lineTo = new A.LineTo();
                //A.Point point = MakePoint(p, cmlExtents);
                //lineTo.Append(point);
                //path.Append(lineTo);
            }

            // Curved Lines
            for (int i = 0; i < allpoints.Count; i += 3)
            {
                var cubicBezierCurveTo = new A.CubicBezierCurveTo();

                for (int j = 0; j < 3; j++)
                {
                    var curvePoint = MakePoint(allpoints[i + j], cmlExtents);
                    cubicBezierCurveTo.Append(curvePoint);
                }
                path.Append(cubicBezierCurveTo);
            }

            pathList.Append(path);

            // End of the lines

            Int32Value emuLineWidth = (Int32Value)(lineWidth * OoXmlHelper.EMUS_PER_WORD_POINT);
            A.Outline outline = new A.Outline { Width = emuLineWidth, CapType = A.LineCapValues.Round };

            A.SolidFill solidFill = new A.SolidFill();
            A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = lineColour };
            solidFill.Append(rgbColorModelHex);
            outline.Append(solidFill);

            shapeProperties.Append(CreateCustomGeometry(pathList));
            shapeProperties.Append(outline);

            wordprocessingShape.Append(CreateShapeStyle());

            Wps.TextBodyProperties textBodyProperties = new Wps.TextBodyProperties();
            wordprocessingShape.Append(textBodyProperties);

            _wordprocessingGroup.Append(wordprocessingShape);
        }

        private void DrawPolygon(List<Point> points, string lineColour, double lineWidth)
        {
            Rect cmlExtents = new Rect(points[0], points[points.Count - 1]);

            for (int i = 0; i < points.Count - 1; i++)
            {
                cmlExtents.Union(new Rect(points[i], points[i + 1]));
            }

            // Move Extents to have 0,0 Top Left Reference
            cmlExtents.Offset(-_boundingBoxOfEverything.Left, -_boundingBoxOfEverything.Top);

            Int64Value emuTop = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Top);
            Int64Value emuLeft = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Left);
            Int64Value emuWidth = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Width);
            Int64Value emuHeight = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Height);

            long id = _ooxmlId++;
            string shapeName = "Polygon " + id;

            Wps.WordprocessingShape wordprocessingShape = CreateShape(id, shapeName);
            Wps.ShapeProperties shapeProperties = CreateShapeProperties(wordprocessingShape, emuTop, emuLeft, emuWidth, emuHeight);

            // Start of the lines

            A.PathList pathList = new A.PathList();

            A.Path path = new A.Path { Width = emuWidth, Height = emuHeight };

            // First point
            A.MoveTo moveTo = new A.MoveTo();
            moveTo.Append(MakePoint(points[0], cmlExtents));
            path.Append(moveTo);

            // Remaining points
            for (int i = 1; i < points.Count; i++)
            {
                A.LineTo lineTo = new A.LineTo();
                lineTo.Append(MakePoint(points[i], cmlExtents));
                path.Append(lineTo);
            }

            // Close the path
            A.CloseShapePath closeShapePath = new A.CloseShapePath();
            path.Append(closeShapePath);

            pathList.Append(path);

            // End of the lines

            Int32Value emuLineWidth = (Int32Value)(lineWidth * OoXmlHelper.EMUS_PER_WORD_POINT);
            A.Outline outline = new A.Outline { Width = emuLineWidth, CapType = A.LineCapValues.Round };

            A.SolidFill solidFill = new A.SolidFill();
            A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = lineColour };
            solidFill.Append(rgbColorModelHex);
            outline.Append(solidFill);

            shapeProperties.Append(CreateCustomGeometry(pathList));
            shapeProperties.Append(outline);

            wordprocessingShape.Append(CreateShapeStyle());

            Wps.TextBodyProperties textBodyProperties = new Wps.TextBodyProperties();
            wordprocessingShape.Append(textBodyProperties);

            _wordprocessingGroup.Append(wordprocessingShape);
        }

        private void DrawStraightLine(Point bondStart, Point bondEnd, string bondPath,
                                      BondLineStyle lineStyle, string lineColour, double lineWidth)
        {
            var tuple = OffsetPoints(bondStart, bondEnd);
            Point cmlStartPoint = tuple.Start;
            Point cmlEndPoint = tuple.End;
            Rect cmlLineExtents = tuple.Extents;

            Int64Value emuTop = OoXmlHelper.ScaleCmlToEmu(cmlLineExtents.Top);
            Int64Value emuLeft = OoXmlHelper.ScaleCmlToEmu(cmlLineExtents.Left);
            Int64Value emuWidth = OoXmlHelper.ScaleCmlToEmu(cmlLineExtents.Width);
            Int64Value emuHeight = OoXmlHelper.ScaleCmlToEmu(cmlLineExtents.Height);

            long id = _ooxmlId++;
            string suffix = string.IsNullOrEmpty(bondPath) ? id.ToString() : bondPath;
            string shapeName = "Straight Line " + suffix;

            Wps.WordprocessingShape wordprocessingShape = CreateShape(id, shapeName);
            Wps.ShapeProperties shapeProperties = CreateShapeProperties(wordprocessingShape, emuTop, emuLeft, emuWidth, emuHeight);

            // Start of the lines

            A.PathList pathList = new A.PathList();

            A.Path path = new A.Path { Width = emuWidth, Height = emuHeight };

            A.MoveTo moveTo = new A.MoveTo();
            A.Point point1 = new A.Point { X = OoXmlHelper.ScaleCmlToEmu(cmlStartPoint.X).ToString(), Y = OoXmlHelper.ScaleCmlToEmu(cmlStartPoint.Y).ToString() };
            moveTo.Append(point1);
            path.Append(moveTo);

            A.LineTo lineTo = new A.LineTo();
            A.Point point2 = new A.Point { X = OoXmlHelper.ScaleCmlToEmu(cmlEndPoint.X).ToString(), Y = OoXmlHelper.ScaleCmlToEmu(cmlEndPoint.Y).ToString() };
            lineTo.Append(point2);
            path.Append(lineTo);

            pathList.Append(path);

            // End of the lines

            Int32Value emuLineWidth = (Int32Value)(lineWidth * OoXmlHelper.EMUS_PER_WORD_POINT);
            A.Outline outline = new A.Outline { Width = emuLineWidth, CapType = A.LineCapValues.Round };

            A.SolidFill solidFill = new A.SolidFill();
            A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = lineColour };
            solidFill.Append(rgbColorModelHex);
            outline.Append(solidFill);

            switch (lineStyle)
            {
                case BondLineStyle.Zero:
                    outline.Append(new A.PresetDash { Val = A.PresetLineDashValues.SystemDot });
                    break;

                case BondLineStyle.Half:
                    outline.Append(new A.PresetDash { Val = A.PresetLineDashValues.SystemDash });
                    break;

                case BondLineStyle.Dotted: // Diagnostics
                    outline.Append(new A.PresetDash { Val = A.PresetLineDashValues.Dot });
                    break;

                case BondLineStyle.Dashed: // Diagnostics
                    outline.Append(new A.PresetDash { Val = A.PresetLineDashValues.Dash });
                    break;
            }

            if (!string.IsNullOrEmpty(bondPath) && _options.ShowBondDirection)
            {
                A.TailEnd tailEnd = new A.TailEnd
                {
                    Type = A.LineEndValues.Arrow,
                    Width = A.LineEndWidthValues.Small,
                    Length = A.LineEndLengthValues.Small
                };
                outline.Append(tailEnd);
            }

            shapeProperties.Append(CreateCustomGeometry(pathList));
            shapeProperties.Append(outline);

            wordprocessingShape.Append(CreateShapeStyle());

            Wps.TextBodyProperties textBodyProperties = new Wps.TextBodyProperties();
            wordprocessingShape.Append(textBodyProperties);

            _wordprocessingGroup.Append(wordprocessingShape);
        }

        private void DrawWavyLine(Point bondStart, Point bondEnd, string bondPath, string lineColour)
        {
            var tuple = OffsetPoints(bondStart, bondEnd);
            Point cmlStartPoint = tuple.Start;
            Point cmlEndPoint = tuple.End;
            Rect cmlLineExtents = tuple.Extents;

            // Calculate wiggles

            Vector bondVector = cmlEndPoint - cmlStartPoint;
            int noOfWiggles = (int)Math.Ceiling(bondVector.Length / BondOffset());
            if (noOfWiggles < 1)
            {
                noOfWiggles = 1;
            }

            double wiggleLength = bondVector.Length / noOfWiggles;
            Debug.WriteLine($"v.Length: {bondVector.Length} noOfWiggles: {noOfWiggles}");

            Vector originalWigglePortion = bondVector;
            originalWigglePortion.Normalize();
            originalWigglePortion *= wiggleLength / 2;

            Matrix toLeft = new Matrix();
            toLeft.Rotate(-60);
            Matrix toRight = new Matrix();
            toRight.Rotate(60);
            Vector leftVector = originalWigglePortion * toLeft;
            Vector rightVector = originalWigglePortion * toRight;

            List<Point> allpoints = new List<Point>();

            Point lastPoint = cmlStartPoint;

            for (int i = 0; i < noOfWiggles; i++)
            {
                // Left
                allpoints.Add(lastPoint);
                Point leftPoint = lastPoint + leftVector;
                allpoints.Add(leftPoint);
                Point midPoint = lastPoint + originalWigglePortion;
                allpoints.Add(midPoint);

                // Right
                allpoints.Add(midPoint);
                Point rightPoint = lastPoint + originalWigglePortion + rightVector;
                allpoints.Add(rightPoint);
                lastPoint += originalWigglePortion * 2;
                allpoints.Add(lastPoint);
            }

            double minX = double.MaxValue;
            double maxX = double.MinValue;
            double minY = double.MaxValue;
            double maxY = double.MinValue;

            foreach (Point p in allpoints)
            {
                maxX = Math.Max(p.X + cmlLineExtents.Left, maxX);
                minX = Math.Min(p.X + cmlLineExtents.Left, minX);
                maxY = Math.Max(p.Y + cmlLineExtents.Top, maxY);
                minY = Math.Min(p.Y + cmlLineExtents.Top, minY);
            }

            Rect newExtents = new Rect(minX, minY, maxX - minX, maxY - minY);
            double xOffset = cmlLineExtents.Left - newExtents.Left;
            double yOffset = cmlLineExtents.Top - newExtents.Top;

            Int64Value emuTop = OoXmlHelper.ScaleCmlToEmu(newExtents.Top);
            Int64Value emuLeft = OoXmlHelper.ScaleCmlToEmu(newExtents.Left);
            Int64Value emuWidth = OoXmlHelper.ScaleCmlToEmu(newExtents.Width);
            Int64Value emuHeight = OoXmlHelper.ScaleCmlToEmu(newExtents.Height);

            string shapeName = "Wavy Line " + bondPath;

            Wps.WordprocessingShape wordprocessingShape = CreateShape(_ooxmlId++, shapeName);
            Wps.ShapeProperties shapeProperties = CreateShapeProperties(wordprocessingShape, emuTop, emuLeft, emuWidth, emuHeight);

            // Start of the lines

            A.PathList pathList = new A.PathList();

            A.Path path = new A.Path { Width = emuWidth, Height = emuHeight };

            A.MoveTo moveTo = new A.MoveTo();
            A.Point firstPoint = new A.Point { X = OoXmlHelper.ScaleCmlToEmu(cmlStartPoint.X + xOffset).ToString(), Y = OoXmlHelper.ScaleCmlToEmu(cmlStartPoint.Y + yOffset).ToString() };
            moveTo.Append(firstPoint);
            path.Append(moveTo);

            // Straight Lines
            //foreach (var p in allpoints)
            //{
            //    A.LineTo lineTo = new A.LineTo();
            //    A.Point point = new A.Point { X = OoXmlHelper.ScaleCmlToEmu(p.X + xOffset).ToString(), Y = OoXmlHelper.ScaleCmlToEmu(p.Y + yOffset).ToString() };
            //    lineTo.Append(point);
            //    path.Append(lineTo);
            //}

            // Curved Lines
            for (int i = 0; i < allpoints.Count; i += 3)
            {
                var cubicBezierCurveTo = new A.CubicBezierCurveTo();

                for (int j = 0; j < 3; j++)
                {
                    A.Point nextPoint = new A.Point { X = OoXmlHelper.ScaleCmlToEmu(allpoints[i + j].X + xOffset).ToString(), Y = OoXmlHelper.ScaleCmlToEmu(allpoints[i + j].Y + yOffset).ToString() };
                    cubicBezierCurveTo.Append(nextPoint);
                }
                path.Append(cubicBezierCurveTo);
            }

            pathList.Append(path);

            // End of the lines

            double lineWidth = OoXmlHelper.ACS_LINE_WIDTH;
            Int32Value emuLineWidth = (Int32Value)(lineWidth * OoXmlHelper.EMUS_PER_WORD_POINT);
            A.Outline outline = new A.Outline { Width = emuLineWidth, CapType = A.LineCapValues.Round };

            A.SolidFill solidFill = new A.SolidFill();
            A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = lineColour };
            solidFill.Append(rgbColorModelHex);
            outline.Append(solidFill);

            if (_options.ShowBondDirection)
            {
                A.TailEnd tailEnd = new A.TailEnd { Type = A.LineEndValues.Stealth };
                outline.Append(tailEnd);
            }

            shapeProperties.Append(CreateCustomGeometry(pathList));
            shapeProperties.Append(outline);

            wordprocessingShape.Append(CreateShapeStyle());

            Wps.TextBodyProperties textBodyProperties = new Wps.TextBodyProperties();
            wordprocessingShape.Append(textBodyProperties);

            _wordprocessingGroup.Append(wordprocessingShape);
        }

        private Wps.WordprocessingShape CreateShape(long id, string shapeName)
        {
            UInt32Value id32 = UInt32Value.FromUInt32((uint)id);
            Wps.WordprocessingShape wordprocessingShape = new Wps.WordprocessingShape();
            Wps.NonVisualDrawingProperties nonVisualDrawingProperties = new Wps.NonVisualDrawingProperties { Id = id32, Name = shapeName };
            Wps.NonVisualDrawingShapeProperties nonVisualDrawingShapeProperties = new Wps.NonVisualDrawingShapeProperties();

            wordprocessingShape.Append(nonVisualDrawingProperties);
            wordprocessingShape.Append(nonVisualDrawingShapeProperties);

            return wordprocessingShape;
        }

        private Wps.ShapeProperties CreateShapeProperties(Wps.WordprocessingShape wordprocessingShape,
                                                          Int64Value emuTop, Int64Value emuLeft, Int64Value emuWidth, Int64Value emuHeight)
        {
            Wps.ShapeProperties shapeProperties = new Wps.ShapeProperties();

            wordprocessingShape.Append(shapeProperties);

            A.Transform2D transform2D = new A.Transform2D();
            A.Offset offset = new A.Offset { X = emuLeft, Y = emuTop };
            A.Extents extents = new A.Extents { Cx = emuWidth, Cy = emuHeight };
            transform2D.Append(offset);
            transform2D.Append(extents);
            shapeProperties.Append(transform2D);

            return shapeProperties;
        }

        private Wps.ShapeStyle CreateShapeStyle()
        {
            Wps.ShapeStyle shapeStyle = new Wps.ShapeStyle();
            A.LineReference lineReference = new A.LineReference { Index = (UInt32Value)0U };
            A.FillReference fillReference = new A.FillReference { Index = (UInt32Value)0U };
            A.EffectReference effectReference = new A.EffectReference { Index = (UInt32Value)0U };
            A.FontReference fontReference = new A.FontReference { Index = A.FontCollectionIndexValues.Minor };

            shapeStyle.Append(lineReference);
            shapeStyle.Append(fillReference);
            shapeStyle.Append(effectReference);
            shapeStyle.Append(fontReference);

            return shapeStyle;
        }

        private A.CustomGeometry CreateCustomGeometry(A.PathList pathList)
        {
            A.CustomGeometry customGeometry = new A.CustomGeometry();
            A.AdjustValueList adjustValueList = new A.AdjustValueList();
            A.Rectangle rectangle = new A.Rectangle { Left = "l", Top = "t", Right = "r", Bottom = "b" };
            customGeometry.Append(adjustValueList);
            customGeometry.Append(rectangle);
            customGeometry.Append(pathList);
            return customGeometry;
        }

        private Run CreateRun()
        {
            Run run = new Run();

            Drawing drawing = new Drawing();
            run.Append(drawing);

            Wp.Inline inline = new Wp.Inline
            {
                DistanceFromTop = (UInt32Value)0U,
                DistanceFromLeft = (UInt32Value)0U,
                DistanceFromBottom = (UInt32Value)0U,
                DistanceFromRight = (UInt32Value)0U
            };
            drawing.Append(inline);

            Int64Value width = OoXmlHelper.ScaleCmlToEmu(_boundingBoxOfEverything.Width);
            Int64Value height = OoXmlHelper.ScaleCmlToEmu(_boundingBoxOfEverything.Height);
            Wp.Extent extent = new Wp.Extent { Cx = width, Cy = height };

            Wp.EffectExtent effectExtent = new Wp.EffectExtent
            {
                TopEdge = 0L,
                LeftEdge = 0L,
                BottomEdge = 0L,
                RightEdge = 0L
            };

            inline.Append(extent);
            inline.Append(effectExtent);

            UInt32Value inlineId = UInt32Value.FromUInt32((uint)_ooxmlId);
            Wp.DocProperties docProperties = new Wp.DocProperties
            {
                Id = inlineId,
                Name = "Chem4Word Structure"
            };

            inline.Append(docProperties);

            A.Graphic graphic = new A.Graphic();
            graphic.AddNamespaceDeclaration("a", "http://schemas.openxmlformats.org/drawingml/2006/main");

            inline.Append(graphic);

            A.GraphicData graphicData = new A.GraphicData
            {
                Uri = "http://schemas.microsoft.com/office/word/2010/wordprocessingGroup"
            };

            graphic.Append(graphicData);

            _wordprocessingGroup = new Wpg.WordprocessingGroup();
            graphicData.Append(_wordprocessingGroup);

            Wpg.NonVisualGroupDrawingShapeProperties nonVisualGroupDrawingShapeProperties = new Wpg.NonVisualGroupDrawingShapeProperties();

            Wpg.GroupShapeProperties groupShapeProperties = new Wpg.GroupShapeProperties();

            A.TransformGroup transformGroup = new A.TransformGroup();
            A.Offset offset = new A.Offset { X = 0L, Y = 0L };
            A.Extents extents = new A.Extents { Cx = width, Cy = height };
            A.ChildOffset childOffset = new A.ChildOffset { X = 0L, Y = 0L };
            A.ChildExtents childExtents = new A.ChildExtents { Cx = width, Cy = height };

            transformGroup.Append(offset);
            transformGroup.Append(extents);
            transformGroup.Append(childOffset);
            transformGroup.Append(childExtents);

            groupShapeProperties.Append(transformGroup);

            _wordprocessingGroup.Append(nonVisualGroupDrawingShapeProperties);
            _wordprocessingGroup.Append(groupShapeProperties);

            return run;
        }

        private (Point Start, Point End, Rect Extents) OffsetPoints(Point start, Point end)
        {
            Point startPoint = new Point(start.X, start.Y);
            Point endPoint = new Point(end.X, end.Y);
            Rect extents = new Rect(startPoint, endPoint);

            // Move Extents and Points to have 0,0 Top Left Reference
            startPoint.Offset(-_boundingBoxOfEverything.Left, -_boundingBoxOfEverything.Top);
            endPoint.Offset(-_boundingBoxOfEverything.Left, -_boundingBoxOfEverything.Top);
            extents.Offset(-_boundingBoxOfEverything.Left, -_boundingBoxOfEverything.Top);

            // Move points into New Extents
            startPoint.Offset(-extents.Left, -extents.Top);
            endPoint.Offset(-extents.Left, -extents.Top);

            // Return a Tuple with the results
            return (Start: startPoint, End: endPoint, Extents: extents);
        }

        private void LoadFont()
        {
            string json = ResourceHelper.GetStringResource(Assembly.GetExecutingAssembly(), "Arial.json");
            _TtfCharacterSet = JsonConvert.DeserializeObject<Dictionary<char, TtfCharacter>>(json);
        }

        /// <summary>
        /// Sets the canvas size to accommodate any extra space required by label characters
        /// </summary>
        private void SetCanvasSize()
        {
            _boundingBoxOfEverything = _boundingBoxOfAllAtoms;

            foreach (AtomLabelCharacter alc in _positionerOutputs.AtomLabelCharacters)
            {
                if (alc.IsSubScript)
                {
                    Rect r = new Rect(alc.Position,
                                      new Size(OoXmlHelper.ScaleCsTtfToCml(alc.Character.Width, _medianBondLength) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR,
                                               OoXmlHelper.ScaleCsTtfToCml(alc.Character.Height, _medianBondLength) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR));
                    _boundingBoxOfEverything.Union(r);
                }
                else
                {
                    Rect r = new Rect(alc.Position,
                                      new Size(OoXmlHelper.ScaleCsTtfToCml(alc.Character.Width, _medianBondLength),
                                               OoXmlHelper.ScaleCsTtfToCml(alc.Character.Height, _medianBondLength)));
                    _boundingBoxOfEverything.Union(r);
                }
            }

            foreach (var group in _positionerOutputs.AllMoleculeExtents)
            {
                _boundingBoxOfEverything.Union(group.ExternalCharacterExtents);
            }

            _boundingBoxOfEverything.Inflate(OoXmlHelper.DRAWING_MARGIN, OoXmlHelper.DRAWING_MARGIN);
        }

        private double BondOffset()
        {
            return _medianBondLength * OoXmlHelper.MULTIPLE_BOND_OFFSET_PERCENTAGE;
        }

        private static void ShutDownProgress(Progress pb)
        {
            pb.Value = 0;
            pb.Hide();
            pb.Close();
        }
    }
}