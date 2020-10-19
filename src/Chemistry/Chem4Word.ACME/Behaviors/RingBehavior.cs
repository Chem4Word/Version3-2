// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Chem4Word.ACME.Adorners;
using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Drawing;
using Chem4Word.ACME.Utils;
using Chem4Word.Core;
using Chem4Word.Model2;
using Chem4Word.Model2.Geometry;

namespace Chem4Word.ACME.Behaviors
{
    /// <summary>
    ///     Puts the editor into fixed ring mode.
    /// </summary>
    public class RingBehavior : BaseEditBehavior
    {
        private FixedRingAdorner _currentAdorner;

        private Window _parent;
        private Cursor _lastCursor;
        public bool Clashing { get; private set; } = false;

        public RingBehavior()
        {
        }

        public RingBehavior(string ringspec) : this()
        {
            RingSize = int.Parse(ringspec[0].ToString());
            Unsaturated = ringspec[1] == 'U';
        }

        public int RingSize { get; set; }

        public FixedRingAdorner CurrentAdorner
        {
            get => _currentAdorner;
            set
            {
                RemoveRingAdorner();
                _currentAdorner = value;
                if (_currentAdorner != null)
                {
                    _currentAdorner.MouseLeftButtonDown += CurrentAdornerOnMouseLeftButtonDown;
                }

                //local function
                void RemoveRingAdorner()
                {
                    if (_currentAdorner != null)
                    {
                        var layer = AdornerLayer.GetAdornerLayer(CurrentEditor);
                        layer.Remove(_currentAdorner);
                        _currentAdorner.MouseLeftButtonDown -= CurrentAdornerOnMouseLeftButtonDown;
                        _currentAdorner = null;
                    }
                }
            }
        }

        public bool Unsaturated { get; set; }

        protected override void OnAttached()
        {
            base.OnAttached();
            EditViewModel.ClearSelection();

            CurrentEditor = (EditorCanvas)AssociatedObject;
            _parent = Application.Current.MainWindow;

            _lastCursor = CurrentEditor.Cursor;
            CurrentEditor.Cursor = CursorUtils.Pencil;

            CurrentEditor.MouseLeftButtonDown += CurrentEditor_MouseLeftButtonDown;
            CurrentEditor.MouseMove += CurrentEditor_MouseMove;
            CurrentEditor.MouseLeftButtonUp += CurrentEditor_MouseLeftButtonUp;
            AssociatedObject.IsHitTestVisible = true;

            if (_parent != null)
            {
                _parent.MouseLeftButtonDown += CurrentEditor_MouseLeftButtonDown;
            }

            CurrentStatus = "Draw a ring by clicking on a bond, atom or free space.";
        }

        public override void Abort()
        {
        }

        private void CurrentEditor_MouseMove(object sender, MouseEventArgs e)
        {
            List<Point> altPlacements;
            List<Point> preferredPlacements;

            Clashing = false;

            CurrentAdorner = null;
            var xamlBondSize = EditViewModel.Model.XamlBondLength;

            switch (CurrentEditor.ActiveVisual)
            {
                case AtomVisual av:
                    IdentifyPlacements(av.ParentAtom, xamlBondSize, out preferredPlacements, RingSize);
                    if (preferredPlacements != null)
                    {
                        CurrentAdorner = new FixedRingAdorner(CurrentEditor, EditViewModel.EditBondThickness,
                                                              preferredPlacements, Unsaturated);
                        if (av.ParentAtom.Degree >= 2)
                        {
                            CurrentStatus = "Click atom to spiro-fuse.";
                        }
                        else
                        {
                            CurrentStatus = "Click atom to draw a terminating ring.";
                        }
                    }
                    break;

                case BondVisual bv:
                    IdentifyPlacements(bv.ParentBond, out altPlacements, out preferredPlacements, RingSize, e.GetPosition(CurrentEditor));
                    if ((preferredPlacements != null) || (altPlacements != null))
                    {
                        CurrentAdorner = new FixedRingAdorner(CurrentEditor, EditViewModel.EditBondThickness,
                                                              preferredPlacements ?? altPlacements, Unsaturated);
                        CurrentStatus = "Click bond to fuse a ring";
                    }
                    break;

                default:
                    preferredPlacements = MarkOutAtoms(e.GetPosition(AssociatedObject), BasicGeometry.ScreenNorth,
                                                       xamlBondSize, RingSize);
                    //need to check whether the user is trying to fuse without
                    //having the pencil directly over the object!
                    foreach (Point p in preferredPlacements)
                    {
                        ChemicalVisual cv = CurrentEditor.GetTargetedVisual(p);
                        if (cv != null)
                        {
                            //user is trying to fuse wrongly
                            Clashing = true;
                            break;
                        }
                    }

                    if (!Clashing)
                    {
                        CurrentAdorner = new FixedRingAdorner(CurrentEditor, EditViewModel.EditBondThickness,
                                                            preferredPlacements, Unsaturated);
                        CurrentStatus = "Click to draw a standalone ring";
                    }
                    else
                    {
                        CurrentAdorner = new FixedRingAdorner(CurrentEditor, EditViewModel.EditBondThickness,
                                                              preferredPlacements, Unsaturated, greyedOut: true);
                        CurrentStatus = "Can't fuse ring here - hover pencil over atom or bond to place ring";
                    }
                    break;
            }
        }

        private void CurrentAdornerOnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CurrentEditor_MouseLeftButtonDown(sender, e);
        }

        private void CurrentEditor_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            CurrentStatus = "";
        }

        private void CurrentEditor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!Clashing)
            {
                var hitAtom = CurrentEditor.ActiveAtomVisual?.ParentAtom;
                var hitBond = CurrentEditor.ActiveBondVisual?.ParentBond;
                var position = e.GetPosition(CurrentEditor);

                List<Point> altPlacements = null;
                var startAt = 0; //used to change double bond positions in isolated odd numbered rings
                var newAtomPlacements = new List<NewAtomPlacement>();

                List<Point> preferredPlacements;
                var xamlBondSize = EditViewModel.Model.XamlBondLength;

                if (hitAtom != null)
                {
                    IdentifyPlacements(hitAtom, xamlBondSize, out preferredPlacements, RingSize);
                    if (preferredPlacements == null)
                    {
                        UserInteractions.AlertUser("No room left to draw any more rings!");
                    }
                    else if (preferredPlacements.Count % 2 == 1)
                    {
                        startAt = 1;
                    }
                }
                else if (hitBond != null)
                {
                    IdentifyPlacements(hitBond, out altPlacements, out preferredPlacements, RingSize, position);
                    if ((altPlacements == null) && (preferredPlacements == null))
                    {
                        UserInteractions.AlertUser("No room left to draw any more rings!");
                    }
                }
                else //clicked on empty space
                {
                    preferredPlacements = MarkOutAtoms(e.GetPosition(AssociatedObject), BasicGeometry.ScreenNorth,
                                                       xamlBondSize, RingSize);
                    if (preferredPlacements.Count % 2 == 1)
                    {
                        startAt = 1;
                    }
                }

                if ((preferredPlacements ?? altPlacements) != null)
                {
                    FillExistingAtoms(preferredPlacements, altPlacements, newAtomPlacements, CurrentEditor);

                    EditViewModel.DrawRing(newAtomPlacements, Unsaturated, startAt);
                }
            }
            CurrentAdorner = null;
        }

        public static void FillExistingAtoms(List<Point> preferredPlacements,
                                             List<Point> altPlacements,
                                             List<NewAtomPlacement> newAtomPlacements,
                                             EditorCanvas currentEditor)
        {
            foreach (var placement in preferredPlacements ?? altPlacements)
            {
                NewAtomPlacement nap = new NewAtomPlacement
                {
                    ExistingAtom = (currentEditor.GetTargetedVisual(placement) as AtomVisual)?.ParentAtom,
                    Position = placement
                };
                newAtomPlacements.Add(nap);
            }
        }

        private bool ClashesWithOtherFragments(List<Point> preferredPlacements, Molecule parentMolecule)
        {
            if (preferredPlacements == null)
            {
                return true;
            }
            foreach (Point placement in preferredPlacements)
            {
                var atomVisual = CurrentEditor.GetTargetedVisual(placement) as AtomVisual;
                if (atomVisual != null && atomVisual.ParentAtom.Parent != parentMolecule)
                {
                    return true;
                }
            }

            return false;
        }

        public void IdentifyPlacements(Atom hitAtom, double xamlBondSize, out List<Point> preferredPlacements, int ringSize)
        {
            Molecule parentMolecule;
            parentMolecule = hitAtom.Parent;
            Vector direction;
            if (hitAtom.Degree != 0)
            {
                direction = hitAtom.BalancingVector();
                if (ringSize == 3 || ringSize == 4 || ringSize == 6)
                {
                    direction = BasicGeometry.SnapVectorToClock(direction);
                }
            }
            else
            {
                direction = BasicGeometry.ScreenNorth;
            }

            //try to work out exactly where best to place the ring

            preferredPlacements = MarkOutAtoms(hitAtom, direction, xamlBondSize, ringSize);
            if (parentMolecule.Overlaps(preferredPlacements, new List<Atom> { hitAtom }))
            {
                preferredPlacements = null;
            }
            else if (ClashesWithOtherFragments(preferredPlacements, parentMolecule))
            {
                preferredPlacements = null;
            }
        }

        public void IdentifyPlacements(Bond hitBond, out List<Point> altPlacements, out List<Point> preferredPlacements, int ringSize, Point position)
        {
            Molecule parentMolecule;
            List<Point> placements;
            PathGeometry firstOverlap;
            PathGeometry secondOverlap;
            double firstOverlapArea;
            double secondOverlapArea;

            parentMolecule = hitBond.Parent;
            var bondDirection = hitBond.BondVector;
            var mouseDirection = position - hitBond.StartAtom.Position;
            if (ringSize == 3 || ringSize == 4 || ringSize == 6)
            {
                bondDirection = BasicGeometry.SnapVectorToClock(bondDirection);
                mouseDirection = BasicGeometry.SnapVectorToClock(mouseDirection);
            }
            bool followsBond = Vector.AngleBetween(bondDirection, mouseDirection) > 0;

            placements = MarkOutAtoms(hitBond, followsBond, ringSize);
            firstOverlap = parentMolecule.OverlapArea(placements);
            firstOverlapArea = firstOverlap.GetArea();

            altPlacements = MarkOutAtoms(hitBond, !followsBond, ringSize);
            secondOverlap = parentMolecule.OverlapArea(altPlacements);
            secondOverlapArea = secondOverlap.GetArea();

            // Get points on the less crowded side of the bond
            if (hitBond.GetUncrowdedSideVector() != null)
            {
                if (firstOverlapArea < 0.001)
                {
                    preferredPlacements = placements;
                }
                else if (secondOverlapArea < 0.001)
                {
                    preferredPlacements = altPlacements;
                }
                else
                {
                    preferredPlacements = null;
                    altPlacements = null;
                }
            }
            else
            {
                preferredPlacements = null;
                altPlacements = null;
            }
            if (ClashesWithOtherFragments(preferredPlacements, parentMolecule))
            {
                preferredPlacements = null;
                altPlacements = null;
            }
        }

        /// <summary>
        ///     Paces out the proposed placement points for a ring attached to one atom
        /// </summary>
        /// <param name="startAtom"></param>
        /// <param name="direction"></param>
        /// <param name="bondSize"></param>
        /// <param name="ringSize"></param>
        /// <returns></returns>
        public static List<Point> MarkOutAtoms(Atom startAtom, Vector direction, double bondSize, int ringSize)
        {
            var placements = new List<Point>();

            //the direction vector points towards the centre of the ring from the start atom
            //so, assuming we are going clockwise, we take the perpendicular of the vector
            //rotate through -90 degrees and then clockwise through half the angle.
            //subsequent rotations are through the full exterior angle

            var exteriorAngle = 360.0 / ringSize;
            var rotator = new Matrix();

            //do the initial rotation
            rotator.Rotate(-90);
            rotator.Rotate(exteriorAngle / 2);

            var bondVector = direction;
            bondVector.Normalize();
            bondVector *= bondSize;

            var lastPos = startAtom.Position;
            placements.Add(startAtom.Position);

            for (var i = 1; i < ringSize; i++)
            {
                var newBondVector = bondVector * rotator;
                lastPos = lastPos + newBondVector;
                placements.Add(lastPos);
                rotator.Rotate(exteriorAngle);
            }

            return placements;
        }

        public static List<Point> MarkOutAtoms(Bond startBond, bool followsBond, int ringSize)
        {
            var placements = new List<Point>();

            Point lastPos;

            Vector bondVector;
            if (followsBond)
            {
                bondVector = startBond.EndAtom.Position - startBond.StartAtom.Position;
                lastPos = startBond.StartAtom.Position;
            }
            else
            {
                bondVector = startBond.StartAtom.Position - startBond.EndAtom.Position;
                lastPos = startBond.EndAtom.Position;
            }

            var exteriorAngle = 360.0 / ringSize;
            var rotator = new Matrix();

            placements.Add(lastPos);

            for (var i = 1; i < ringSize; i++)
            {
                var newBondVector = bondVector * rotator;
                lastPos = lastPos + newBondVector;
                placements.Add(lastPos);
                rotator.Rotate(exteriorAngle);
            }

            return placements;
        }

        public static List<Point> MarkOutAtoms(Point start, Vector direction, double bondSize, int ringSize)
        {
            var placements = new List<Point>();

            //the direction vector points towards the centre of the ring from the start atom
            //so, assuming we are going clockwise, we take the perpendicular of the vector
            //rotate through -90 degrees and then clockwise through half the angle.
            //subsequent rotations are through the full exterior angle

            var exteriorAngle = 360.0 / ringSize;
            var rotator = new Matrix();

            //do the initial rotation
            rotator.Rotate(-90);
            rotator.Rotate(exteriorAngle / 2);

            var bondVector = direction;
            bondVector.Normalize();
            bondVector *= bondSize;

            var lastPos = start;
            placements.Add(start);

            for (var i = 1; i < ringSize; i++)
            {
                var newBondVector = bondVector * rotator;
                lastPos = lastPos + newBondVector;
                placements.Add(lastPos);
                rotator.Rotate(exteriorAngle);
            }

            return placements;
        }

        protected override void OnDetaching()
        {
            CurrentAdorner = null;
            CurrentEditor.MouseLeftButtonDown -= CurrentEditor_MouseLeftButtonDown;
            CurrentEditor.MouseMove -= CurrentEditor_MouseMove;
            CurrentEditor.MouseLeftButtonUp -= CurrentEditor_MouseLeftButtonUp;
            if (_parent != null)
            {
                _parent.MouseLeftButtonDown -= CurrentEditor_MouseLeftButtonDown;
            }
            _parent = null;
        }
    }
}