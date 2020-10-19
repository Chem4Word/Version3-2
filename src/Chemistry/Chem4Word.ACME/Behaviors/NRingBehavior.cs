// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Chem4Word.ACME.Adorners;
using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Drawing;
using Chem4Word.ACME.Utils;
using Chem4Word.Model2;
using Chem4Word.Model2.Geometry;

namespace Chem4Word.ACME.Behaviors
{
    /// <summary>
    /// Puts the editor into variable ring mode.
    /// </summary>
    public class NRingBehavior : BaseEditBehavior
    {
        public int RingSize { get; set; }
        public bool Clashing { get; set; }

        private Window _parent;
        private NRingAdorner _currentAdorner;
        private List<Point> _preferredPlacements;
        private Cursor _lastCursor;
        private const string DefaultStatusText = "Drag on atom, bond or free space to draw ring.";

        public NRingBehavior()
        {
        }

        public bool MouseIsDown { get; set; }

        public Point FirstPoint { get; set; }
        public Point CurrentPoint { get; set; }
        public ChemicalVisual Target { get; set; }
        public bool IsDrawing { get; set; }

        public NRingAdorner CurrentAdorner
        {
            get { return _currentAdorner; }
            set
            {
                RemoveRingAdorner();
                _currentAdorner = value;
                if (_currentAdorner != null)
                {
                    _currentAdorner.MouseLeftButtonDown += _currentAdorner_MouseLeftButtonDown;
                    _currentAdorner.MouseLeftButtonUp += _currentAdorner_MouseLeftButtonUp;
                    _currentAdorner.PreviewKeyDown += CurrentAdorner_KeyDown;
                    _currentAdorner.MouseMove += _currentAdorner_MouseMove;
                }
            }
        }

        private void _currentAdorner_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            CurrentEditor_MouseLeftButtonUp(sender, e);
        }

        public override void Abort()
        {
            _preferredPlacements = null;
            CurrentEditor.ReleaseMouseCapture();
            CurrentEditor.Focus();
            CurrentAdorner = null;
            Target = null;
            MouseIsDown = false;
            IsDrawing = false;
            CurrentStatus = DefaultStatusText;
        }

        private void _currentAdorner_MouseMove(object sender, MouseEventArgs e)
        {
            CurrentEditor_MouseMove(sender, e);
            CurrentStatus = DefaultStatusText;
        }

        private void RemoveRingAdorner()
        {
            if (_currentAdorner != null)
            {
                var layer = AdornerLayer.GetAdornerLayer(CurrentEditor);
                layer.Remove(_currentAdorner);
                _currentAdorner.MouseLeftButtonDown -= _currentAdorner_MouseLeftButtonDown;
                _currentAdorner.MouseLeftButtonUp -= _currentAdorner_MouseLeftButtonUp;
                _currentAdorner.PreviewKeyDown -= CurrentAdorner_KeyDown;
                _currentAdorner.MouseMove -= _currentAdorner_MouseMove;
                _currentAdorner = null;
            }
        }

        private void CurrentAdorner_KeyDown(object sender, KeyEventArgs e)
        {
            CurrentEditor_KeyDown(sender, e);
        }

        private void CurrentEditor_MouseMove(object sender, MouseEventArgs e)
        {
            Clashing = false;
            CurrentPoint = e.GetPosition(CurrentEditor);
            //check to see whether we've dragged off the target first
            if (MouseIsDown && IsDrawing)
            {
                CurrentStatus = "Drag along arrow to size ring: [Esc] to cancel";
                double xamlBondSize = EditViewModel.Model.XamlBondLength;
                if (Target != null && GetTargetedVisual(e) != Target) //dragging off a bond or atom
                {
                    //first, work out how far we've dragged the mouse

                    switch (Target)
                    {
                        case AtomVisual av:

                            RingSize = GetRingSize(av, CurrentPoint, xamlBondSize);
                            IdentifyPlacements(av.ParentAtom, xamlBondSize, out _preferredPlacements, RingSize);
                            if (_preferredPlacements != null)
                            {
                                Vector parallelToBV =
                                    GetProjection(av.ParentAtom.BalancingVector(), av.Position, CurrentPoint);
                                Point endPoint = av.Position + parallelToBV;
                                CurrentAdorner = new NRingAdorner(CurrentEditor, EditViewModel.EditBondThickness,
                                                                  _preferredPlacements, av.Position, endPoint);
                            }

                            break;

                        case BondVisual bv:
                            RingSize = GetRingSizeFromEdge(bv, CurrentPoint);
                            Vector perpToBV =
                                GetProjection(bv.ParentBond.BondVector.Perpendicular(), bv.ParentBond.MidPoint, CurrentPoint);

                            IdentifyPlacements(bv.ParentBond, out _preferredPlacements, RingSize, perpToBV);
                            if (_preferredPlacements != null)
                            {
                                Vector? perpToAV = bv.ParentBond.GetUncrowdedSideVector();
                                if (perpToAV != null)
                                {
                                    var parentBondMidPoint = bv.ParentBond.MidPoint;
                                    Vector parallelToPerp =
                                        GetProjection(perpToAV.Value, parentBondMidPoint, CurrentPoint);
                                    Point endPoint = parentBondMidPoint + parallelToPerp;
                                    CurrentAdorner = new NRingAdorner(CurrentEditor, EditViewModel.EditBondThickness,
                                                                  _preferredPlacements,
                                                                  parentBondMidPoint, endPoint);
                                }
                                else
                                {
                                    CurrentAdorner = null;
                                }
                            }
                            else
                            {
                                CurrentAdorner = null;
                            }
                            break;
                    }
                }
                else if (Target == null) //dragging in empty space
                {
                    RingSize = GetRingSize(FirstPoint, CurrentPoint, xamlBondSize);
                    _preferredPlacements = MarkOutAtoms(FirstPoint, CurrentPoint - FirstPoint,
                                                        xamlBondSize, RingSize);
                    //need to check whether the user is trying to fuse without
                    //having the pencil directly over the object!
                    foreach (Point p in _preferredPlacements)
                    {
                        ChemicalVisual cv = CurrentEditor.GetTargetedVisual(p);
                        if (cv != null)
                        {
                            //user is trying to fuse wrongly
                            Clashing = true;
                            break;
                        }
                    }

                    if (Clashing)
                    {
                        CurrentStatus = "Can't draw ring here - drag over atom or bond to draw fused ring";
                    }
                    CurrentAdorner = new NRingAdorner(CurrentEditor, EditViewModel.EditBondThickness,
                                                      _preferredPlacements, FirstPoint, CurrentPoint, Clashing);
                }
                else
                {
                    switch (CurrentEditor.ActiveVisual)
                    {
                        case AtomVisual av:
                            CurrentStatus = "Drag from atom to size ring.";
                            break;

                        case BondVisual bv:
                            CurrentStatus = "Drag from bond to size ring.";
                            break;

                        default:
                            CurrentStatus = DefaultStatusText;
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Gets a vector projected onto the line connecting the start and end points
        /// </summary>
        /// <param name="input">Input vector to project onto</param>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <param name="minval">Minmum value vector to return</param>
        /// <returns></returns>
        private Vector GetProjection(Vector input, Point startPoint, Point endPoint)
        {
            Vector offset = endPoint - startPoint;
            return GetProjection(input, offset);
        }

        /// <summary>
        /// Gets a vector projected onto the line connecting the start and end points
        /// </summary>
        /// <param name="input">Input vector to project onto</param>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        private static Vector GetProjection(Vector input, Vector offset)
        {
            input.Normalize();
            var projection = (input * offset) * input;
            return projection;
        }

        private int GetRingSizeFromEdge(BondVisual start, Point currentPoint)
        {
            var bond = start.ParentBond;
            var midPoint = bond.MidPoint;
            var uncrowdedSideVector = bond.GetUncrowdedSideVector();
            if (uncrowdedSideVector != null)
            {
                GetProjection(uncrowdedSideVector.Value, midPoint, currentPoint);
                var distance = Math.Abs((currentPoint - midPoint).Length);
                return GetRingSize(Math.Abs(bond.BondVector.Length), distance);
            }

            return 3;
        }

        private ChemicalVisual GetTargetedVisual(MouseEventArgs e) => CurrentEditor.GetTargetedVisual(e.GetPosition(CurrentEditor));

        private int GetRingSize(Point start, Point end, double bondSize)
        {
            double displacement = Math.Sqrt((end - start).LengthSquared);
            return GetRingSize(bondSize, displacement);
        }

        private int GetRingSize(AtomVisual start, Point end, double bondSize)
        {
            //take the dot product of the distance we moved the mouse with the balancing vector

            double displacement = Vector.Multiply((end - start.Position), start.ParentAtom.BalancingVector());
            return GetRingSize(bondSize, displacement);
        }

        private static int GetRingSize(double bondSize, double displacement)
        {
            if (displacement < 0)
            {
                displacement = 0d;
            }

            double circ = Math.PI * displacement;

            for (int i = 4; i <= 100; i++)
            {
                if ((i - 1) * bondSize <= circ && i * bondSize > circ)
                {
                    return i;
                }
            }

            return 3;
        }

        private void _currentAdorner_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CurrentEditor_MouseLeftButtonDown(sender, e);
        }

        private void CurrentEditor_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!Clashing && MouseIsDown)
            {
                if (_preferredPlacements != null)
                {
                    List<NewAtomPlacement> newAtomPlacements = new List<NewAtomPlacement>();
                    FillExistingAtoms(_preferredPlacements, newAtomPlacements, CurrentEditor);
                    EditViewModel.DrawRing(newAtomPlacements, false);
                }
            }
            CurrentEditor.ReleaseMouseCapture();
            CurrentEditor.Focus();
            CurrentAdorner = null;
            Target = null;
            MouseIsDown = false;
            IsDrawing = false;
            CurrentStatus = DefaultStatusText;
        }

        private void CurrentEditor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //save the current targeted visual
            Target = CurrentEditor.ActiveVisual;
            FirstPoint = e.GetPosition(CurrentEditor);
            Mouse.Capture(CurrentEditor);
            Keyboard.Focus(CurrentEditor);
            MouseIsDown = true;
            IsDrawing = true;
            CurrentStatus = "Drag to start sizing ring.";
        }

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
            CurrentEditor.KeyDown += CurrentEditor_KeyDown;
            CurrentEditor.IsHitTestVisible = true;
            CurrentEditor.Focusable = true;
            if (_parent != null)
            {
                _parent.MouseLeftButtonDown += CurrentEditor_MouseLeftButtonDown;
            }
        }

        private void CurrentEditor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                RemoveRingAdorner();
                MouseIsDown = false;
                Target = null;
                IsDrawing = false;
            }
        }

        protected override void OnDetaching()
        {
            CurrentAdorner = null;
            CurrentEditor.MouseLeftButtonDown -= CurrentEditor_MouseLeftButtonDown;
            CurrentEditor.MouseMove -= CurrentEditor_MouseMove;
            CurrentEditor.MouseLeftButtonUp -= CurrentEditor_MouseLeftButtonUp;
            CurrentEditor.PreviewKeyDown -= CurrentEditor_KeyDown;
            CurrentEditor = null;
            if (_parent != null)
            {
                _parent.MouseLeftButtonDown -= CurrentEditor_MouseLeftButtonDown;
            }
            _parent = null;
        }

        public static void FillExistingAtoms(List<Point> preferredPlacements,
                                             List<NewAtomPlacement> newAtomPlacements,
                                             EditorCanvas currentEditor)
        {
            foreach (Point placement in preferredPlacements)
            {
                NewAtomPlacement nap = new NewAtomPlacement
                {
                    ExistingAtom = (currentEditor.GetTargetedVisual(placement) as AtomVisual)?.ParentAtom,
                    Position = placement
                };
                newAtomPlacements.Add(nap);
            }
        }

        public static void IdentifyPlacements(Atom hitAtom, double xamlBondSize, out List<Point> preferredPlacements, int ringSize)
        {
            Molecule parentMolecule;
            parentMolecule = hitAtom.Parent;
            Vector direction;
            if (hitAtom.Degree != 0)
            {
                direction = hitAtom.BalancingVector();
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
        }

        public static void IdentifyPlacements(Bond hitBond, out List<Point> preferredPlacements, int ringSize, Vector? perpvector = null)
        {
            if (perpvector == null)
            {
                //get a point on the less crowded side of the bond
                perpvector = hitBond.GetUncrowdedSideVector();
            }

            bool followsBond = Vector.AngleBetween(hitBond.BondVector, perpvector.Value) > 0;

            preferredPlacements = MarkOutAtoms(hitBond, followsBond, ringSize: ringSize);
        }

        /// <summary>
        /// Paces out the proposed placement points for a ring attached to one atom
        /// </summary>
        /// <param name="startAtom"></param>
        /// <param name="direction"></param>
        /// <param name="bondSize"></param>
        /// <param name="ringSize"></param>
        /// <returns></returns>
        public static List<Point> MarkOutAtoms(Atom startAtom, Vector direction, double bondSize, int ringSize)
        {
            List<Point> placements = new List<Point>();

            //the direction vector points towards the centre of the ring from the start atom
            //so, assuming we are going clockwise, we take the perpendicular of the vector
            //rotate through -90 degrees and then clockwise through half the angle.
            //subsequent rotations are through the full exterior angle

            double exteriorAngle = 360.0 / ringSize;
            Matrix rotator = new Matrix();

            //do the initial rotation
            rotator.Rotate(-90);
            rotator.Rotate(exteriorAngle / 2);

            Vector bondVector = direction;
            bondVector.Normalize();
            bondVector *= bondSize;

            Point lastPos = startAtom.Position;
            placements.Add(startAtom.Position);

            for (int i = 1; i < ringSize; i++)
            {
                Vector newBondVector = bondVector * rotator;
                lastPos = lastPos + newBondVector;
                placements.Add(lastPos);
                rotator.Rotate(exteriorAngle);
            }
            return placements;
        }

        public static List<Point> MarkOutAtoms(Bond startBond, bool followsBond, int ringSize)
        {
            List<Point> placements = new List<Point>();

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

            double exteriorAngle = 360.0 / ringSize;
            Matrix rotator = new Matrix();

            placements.Add(lastPos);

            for (int i = 1; i < ringSize; i++)
            {
                Vector newBondVector = bondVector * rotator;
                lastPos = lastPos + newBondVector;
                placements.Add(lastPos);
                rotator.Rotate(exteriorAngle);
            }
            return placements;
        }

        public static List<Point> MarkOutAtoms(Point start, Vector direction, double bondSize, int ringSize)
        {
            List<Point> placements = new List<Point>();

            //the direction vector points towards the centre of the ring from the start atom
            //so, assuming we are going clockwise, we take the perpendicular of the vector
            //rotate through -90 degrees and then clockwise through half the angle.
            //subsequent rotations are through the full exterior angle

            double exteriorAngle = 360.0 / ringSize;
            Matrix rotator = new Matrix();

            //do the initial rotation
            rotator.Rotate(-90);
            rotator.Rotate(exteriorAngle / 2);

            Vector bondVector = direction;
            bondVector.Normalize();
            bondVector *= bondSize;

            Point lastPos = start;
            placements.Add(start);

            for (int i = 1; i < ringSize; i++)
            {
                Vector newBondVector = bondVector * rotator;
                lastPos = lastPos + newBondVector;
                placements.Add(lastPos);
                rotator.Rotate(exteriorAngle);
            }
            return placements;
        }
    }
}