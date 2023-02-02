// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;

namespace Chem4Word.Renderer.OoXmlV4.Entities
{
    // Atoms <= InternalCharacters <= GroupBrackets <= MoleculesBrackets <= ExternalCharacters

    public class MoleculeExtents
    {
        private Rect _internalCharacterExtents;
        private Rect _moleculeBracketsExtents;
        private Rect _externalCharacterExtents;
        private Rect _groupBracketsExtents;

        public string Path { get; set; }

        /// <summary>
        /// BoundingBox of this molecule's Atom.Position(s)
        /// </summary>
        public Rect AtomExtents { get; }

        /// <summary>
        /// BoundingBox of Atom points and Atom label characters
        /// This will be a Union of AtomExtents and bounding box of each AtomLabelCharacter belonging to the atoms of this molecule
        /// </summary>
        public Rect InternalCharacterExtents
        {
            get => _internalCharacterExtents;
        }

        /// <summary>
        /// Where to draw Molecule Group Brackets
        /// </summary>
        public Rect GroupBracketsExtents
        {
            get => _groupBracketsExtents;
        }

        /// <summary>
        /// Where to draw Molecule Brackets
        /// </summary>
        public Rect MoleculeBracketsExtents
        {
            get => _moleculeBracketsExtents;
        }

        /// <summary>
        /// BoundingBox of molecule and all of it's characters
        /// </summary>
        public Rect ExternalCharacterExtents
        {
            get => _externalCharacterExtents;
        }

        public MoleculeExtents(string path, Rect extents)
        {
            Path = path;

            AtomExtents = extents;

            _internalCharacterExtents = extents;
            _moleculeBracketsExtents = extents;
            _externalCharacterExtents = extents;
            _groupBracketsExtents = extents;
        }

        public override string ToString()
        {
            return $"{Path}, {AtomExtents}";
        }

        public void SetInternalCharacterExtents(Rect extents)
        {
            _internalCharacterExtents = extents;
            _moleculeBracketsExtents = extents;
            _externalCharacterExtents = extents;
            _groupBracketsExtents = extents;
        }

        public void SetGroupBracketExtents(Rect extents)
        {
            _groupBracketsExtents = extents;
            _moleculeBracketsExtents = extents;
            _externalCharacterExtents = extents;
        }

        public void SetMoleculeBracketExtents(Rect extents)
        {
            _moleculeBracketsExtents = extents;
            _externalCharacterExtents = extents;
        }

        public void SetExternalCharacterExtents(Rect extents)
        {
            _externalCharacterExtents = extents;
        }
    }
}