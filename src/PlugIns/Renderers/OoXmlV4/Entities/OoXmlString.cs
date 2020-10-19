// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;

namespace Chem4Word.Renderer.OoXmlV4.Entities
{
    public class OoXmlString
    {
        public string ParentMolecule { get; set; }
        public Rect Extents { get; set; }
        public string Value { get; set; }
        public string Colour { get; set; } = "000000";

        public OoXmlString(Rect extents, string value, string parentMolecule)
        {
            Extents = extents;
            Value = value;
            ParentMolecule = parentMolecule;
        }

        public OoXmlString(Rect extents, string value, string parentMolecule, string colour)
            : this(extents, value, parentMolecule)
        {
            Colour = colour;
        }
    }
}