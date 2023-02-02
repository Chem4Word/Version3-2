// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Chem4Word.ACME.Adorners.Selectors;
using Chem4Word.ACME.Drawing.Visuals;
using Chem4Word.Model2;

namespace Chem4Word.ACME.Controls
{
    public class EditorCanvas : ChemistryCanvas
    {
        #region Constructors

        public EditorCanvas() : base()
        {
        }

        #endregion Constructors

        #region Methods

        public Rect GetMoleculeBoundingBox(Molecule mol)
        {
            Rect union = Rect.Empty;
            var atomList = new List<Atom>();

            mol.BuildAtomList(atomList);
            foreach (Atom atom in atomList)
            {
                union.Union(ChemicalVisuals[atom].ContentBounds);
            }

            return union;
        }

        public Rect GetCombinedBoundingBox(List<BaseObject> chemistries)
        {
            Rect union = Rect.Empty;
            foreach (BaseObject cb in chemistries)
            {
                Rect boundingBox = Rect.Empty;
                if (cb is Molecule molecule)
                {
                    if (molecule.IsGrouped)
                    {
                        boundingBox = GetDrawnBoundingBox(molecule);
                    }
                    else
                    {
                        boundingBox = GetMoleculeBoundingBox(molecule);
                    }
                }
                else if (cb is Annotation annotation)
                {
                    boundingBox = GetDrawnBoundingBox(annotation);
                }
                else if (cb is Reaction reaction)
                {
                    boundingBox = GetReactionBoundingBox(new List<Reaction> { reaction });
                }
                union.Union(boundingBox);
            }

            return union;
        }

        /// <summary>
        /// Draws a 'ghost image' of the selected molecules.
        /// Useful in visual operations
        /// </summary>
        /// <param name="adornedMolecules">List of molecules to ghost</param>
        /// <param name="operation"></param>
        /// <returns></returns>
        public Geometry GhostMolecules(List<Molecule> adornedMolecules, Transform operation = null)
        {
            var atomList = new List<Atom>();
            foreach (Molecule mol in adornedMolecules)
            {
                mol.BuildAtomList(atomList);
            }

            return PartialGhost(atomList, operation);
        }

        /// <summary>
        /// Use for 'rubber-banding' atoms during drag operations
        /// </summary>
        /// <param name="selectedAtoms">List of selected atoms to rubber-band</param>
        /// <param name="shear">TranslateTransform describing how atoms are shifted. Omit to obtain an overlay to transform later</param>
        /// <returns>Geometry of deformed atoms</returns>
        private Geometry PartialGhost(List<Atom> selectedAtoms, Transform shear = null)
        {
            HashSet<Bond> bondSet = new HashSet<Bond>();
            Dictionary<Atom, Point> transformedPositions = new Dictionary<Atom, Point>();
            //compile a set of all the neighbours of the selected atoms
            foreach (Atom atom in selectedAtoms)
            {
                foreach (Atom neighbour in atom.Neighbours)
                {
                    //add in all the existing position for neigbours not in selected atoms
                    if (!selectedAtoms.Contains(neighbour))
                    {
                        transformedPositions[neighbour] = neighbour.Position;
                    }
                }
                //add in the bonds
                foreach (Bond bond in atom.Bonds)
                {
                    bondSet.Add(bond);//don't worry about adding them twice
                }
                //and while we're at it, work out the new locations

                //if we're just getting an overlay then don't bother transforming
                if (shear != null)
                {
                    transformedPositions[atom] = shear.Transform(atom.Position);
                }
                else
                {
                    transformedPositions[atom] = atom.Position;
                }
            }

            StreamGeometry ghostGeometry = new StreamGeometry();

            double atomRadius = Controller.Model.XamlBondLength / 7.50;
            using (StreamGeometryContext ghostContext = ghostGeometry.Open())
            {
                foreach (Atom atom in transformedPositions.Keys)
                {
                    var newPosition = transformedPositions[atom];

                    if (atom.SymbolText != "")
                    {
                        EllipseGeometry atomCircle = new EllipseGeometry(newPosition, atomRadius, atomRadius);
                        Utils.Geometry.DrawGeometry(ghostContext, atomCircle);
                    }
                }
                foreach (Bond bond in bondSet)
                {
                    var startAtomPosition = transformedPositions[bond.StartAtom];
                    var endAtomPosition = transformedPositions[bond.EndAtom];
                    var startAtomVisual = (AtomVisual)(ChemicalVisuals[bond.StartAtom]);
                    var endAtomVisual = (AtomVisual)(ChemicalVisuals[bond.EndAtom]);
                    var descriptor = BondVisual.GetBondDescriptor(startAtomVisual, endAtomVisual, Controller.Model.XamlBondLength,
                                                       bond.Stereo, startAtomPosition, endAtomPosition, bond.OrderValue,
                                                       bond.Placement, bond.Centroid, bond.SubsidiaryRing?.Centroid, Controller.Standoff);
                    descriptor.Start = startAtomPosition;
                    descriptor.End = endAtomPosition;
                    var bondgeom = descriptor.DefiningGeometry;
                    Utils.Geometry.DrawGeometry(ghostContext, bondgeom);
                }
                ghostContext.Close();
            }

            return ghostGeometry;
        }

        public AtomVisual GetAtomVisual(Atom adornedAtom)
        {
            return ChemicalVisuals[adornedAtom] as AtomVisual;
        }

        public MoleculeSelectionAdorner GetMoleculeAdorner(Point p)
        {
            MoleculeSelectionAdorner result = null;

            var layer = AdornerLayer.GetAdornerLayer(this);
            var children = layer.GetAdorners(this);
            if (children != null)
            {
                foreach (var adorner in children)
                {
                    if (adorner is MoleculeSelectionAdorner moleculeAdorner)
                    {
                        var bb = moleculeAdorner.BoundingBox;
                        if (bb.Contains(p))
                        {
                            result = moleculeAdorner;
                        }

                        break;
                    }
                }
            }

            return result;
        }

        #endregion Methods

        #region Overrides

        /// <summary>
        /// Autosizes display to chemistry
        /// </summary>
        /// <param name="constraint"></param>
        /// <returns></returns>
        protected override Size MeasureOverride(Size constraint)
        {
            var tempSize = DesiredSize;
            if (Controller != null)
            {
                var modelWidth = Controller.Model.MaxX - Controller.Model.MinX;
                var modelHeight = Controller.Model.MaxY - Controller.Model.MinY;
                if (tempSize.Width < modelWidth)
                {
                    tempSize.Width = modelWidth;
                }

                if (tempSize.Height < modelHeight)
                {
                    tempSize.Height = modelHeight;
                }
            }
            return tempSize;
        }

        internal Rect GetReactionBoundingBox(List<Reaction> adornedReactions)
        {
            Rect union = Rect.Empty;
            foreach (Reaction r in adornedReactions)
            {
                union.Union(GetDrawnBoundingBox(r));
            }

            return union;
        }

        #endregion Overrides
    }
}