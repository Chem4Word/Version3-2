// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Windows;
using Chem4Word.Renderer.OoXmlV4.Entities.Diagnostic;

namespace Chem4Word.Renderer.OoXmlV4.Entities
{
    public class PositionerOutputs
    {
        public List<AtomLabelCharacter> AtomLabelCharacters { get; } = new List<AtomLabelCharacter>();
        public Dictionary<string, List<Point>> ConvexHulls { get; } = new Dictionary<string, List<Point>>();
        public List<BondLine> BondLines { get; } = new List<BondLine>();
        public List<Point> RingCenters { get; } = new List<Point>();
        public List<InnerCircle> InnerCircles { get; } = new List<InnerCircle>();
        public List<MoleculeExtents> AllMoleculeExtents { get; } = new List<MoleculeExtents>();
        public List<Rect> GroupBrackets { get; } = new List<Rect>();
        public List<Rect> MoleculeBrackets { get; } = new List<Rect>();
        public List<OoXmlString> MoleculeCaptions { get; } = new List<OoXmlString>();
        public Diagnostics Diagnostics { get; } = new Diagnostics();
    }
}