// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Drawing;
using Chem4Word.Model2;
using Chem4Word.Model2.Annotations;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.ACME.Adorners
{
    public class PartialGhostAdorner : Adorner
    {
        [NotNull]
        private Geometry _outline;

        [NotNull]
        private SolidColorBrush _ghostBrush;

        [NotNull]
        private Pen _ghostPen;

        [NotNull]
        private Transform _shear;

        private IEnumerable<Atom> _atomList;
        public EditorCanvas CurrentEditor { get; }
        public EditViewModel CurrentViewModel { get; }

        public PartialGhostAdorner(EditViewModel currentModel, IEnumerable<Atom> atomList, Transform shear) : base(
            currentModel.CurrentEditor)
        {
            _shear = shear;
            var myAdornerLayer = AdornerLayer.GetAdornerLayer(currentModel.CurrentEditor);
            _atomList = atomList;
            myAdornerLayer.Add(this);
            PreviewMouseMove += PartialGhostAdorner_PreviewMouseMove;
            PreviewMouseUp += PartialGhostAdorner_PreviewMouseUp;
            MouseUp += PartialGhostAdorner_MouseUp;
            CurrentViewModel = currentModel;

            CurrentEditor = CurrentViewModel.CurrentEditor;
        }

        private void PartialGhostAdorner_MouseUp(object sender, MouseButtonEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        private void PartialGhostAdorner_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        private void PartialGhostAdorner_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        public Geometry Ghost
        {
            get { return _outline; }
            set
            {
                _outline = value;
                InvalidateVisual();
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            _ghostBrush = (SolidColorBrush)FindResource(Globals.GhostBrush);
            _ghostPen = new Pen(_ghostBrush, Globals.BondThickness);

            HashSet<Bond> bondSet = new HashSet<Bond>();
            Dictionary<Atom, Point> transformedPositions = new Dictionary<Atom, Point>();

            //compile a set of all the neighbours of the selected atoms

            foreach (Atom atom in _atomList)
            {
                foreach (Atom neighbour in atom.Neighbours)
                {
                    //add in all the existing position for neigbours not in selected atoms
                    if (!_atomList.Contains(neighbour))
                    {
                        //neighbourSet.Add(neighbour); //don't worry about adding them twice
                        transformedPositions[neighbour] = neighbour.Position;
                    }
                }

                //add in the bonds
                foreach (Bond bond in atom.Bonds)
                {
                    bondSet.Add(bond); //don't worry about adding them twice
                }

                //if we're just getting an overlay then don't bother transforming
                if (_shear != null)
                {
                    transformedPositions[atom] = _shear.Transform(atom.Position);
                }
                else
                {
                    transformedPositions[atom] = atom.Position;
                }
            }

            var modelXamlBondLength = CurrentViewModel.Model.XamlBondLength;
            double atomRadius = modelXamlBondLength / 7.50;

            foreach (Bond bond in bondSet)
            {
                List<Point> throwaway = new List<Point>();
                var startAtomPosition = transformedPositions[bond.StartAtom];
                var endAtomPosition = transformedPositions[bond.EndAtom];
                if (bond.OrderValue != 1.0 ||
                    !(bond.Stereo == Globals.BondStereo.Hatch | bond.Stereo == Globals.BondStereo.Wedge))
                {
                    var descriptor = BondVisual.GetBondDescriptor(CurrentEditor.GetAtomVisual(bond.StartAtom),
                                                                  CurrentEditor.GetAtomVisual(bond.EndAtom),
                                                                  modelXamlBondLength,
                                                                  bond.Stereo, startAtomPosition, endAtomPosition,
                                                                  bond.OrderValue,
                                                                  bond.Placement, bond.Centroid,
                                                                  bond.SubsidiaryRing?.Centroid,
                                                                  CurrentEditor.ViewModel.Standoff);
                    descriptor.Start = startAtomPosition;
                    descriptor.End = endAtomPosition;
                    var bondgeom = descriptor.DefiningGeometry;
                    drawingContext.DrawGeometry(_ghostBrush, _ghostPen, bondgeom);
                }
                else
                {
                    drawingContext.DrawLine(_ghostPen, startAtomPosition, endAtomPosition);
                }
            }

            foreach (Atom atom in transformedPositions.Keys)
            {
                var newPosition = transformedPositions[atom];

                if (atom.SymbolText != "")
                {
                    drawingContext.DrawEllipse(SystemColors.WindowBrush, _ghostPen, newPosition, atomRadius, atomRadius);
                }
            }
        }
    }
}