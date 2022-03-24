// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using Chem4Word.ACME.Adorners.Sketching;
using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Drawing.Visuals;
using Chem4Word.ACME.Utils;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using Chem4Word.Model2.Enums;
using Chem4Word.Model2.Geometry;
using static Chem4Word.Model2.Helpers.Globals;

namespace Chem4Word.ACME.Behaviors
{
    /// <summary>
    /// Does freehand drawing of atoms and bonds
    /// </summary>
    public class DrawBehavior : BaseEditBehavior
    {
        private AtomVisual _currentAtomVisual;
        private bool IsDrawing { get; set; }

        private Snapper _angleSnapper;

        private DrawBondAdorner _adorner;

        private AtomVisual _lastAtomVisual;
        private Cursor _lastCursor;

        private const string DefaultText = "Click existing atom to sprout a chain or modify element.";

        protected override void OnAttached()
        {
            base.OnAttached();

            CurrentEditor = (EditorCanvas)AssociatedObject;
            _lastCursor = CurrentEditor.Cursor;
            CurrentEditor.Cursor = CursorUtils.Pencil;
            EditController.ClearSelection();

            CurrentEditor.MouseLeftButtonDown += CurrentEditor_MouseLeftButtonDown;
            CurrentEditor.PreviewMouseLeftButtonUp += CurrentEditor_PreviewMouseLeftButtonUp;
            CurrentEditor.PreviewMouseMove += CurrentEditor_PreviewMouseMove;
            CurrentEditor.PreviewMouseRightButtonUp += CurrentEditor_PreviewMouseRightButtonUp;
            CurrentEditor.IsHitTestVisible = true;

            CurrentStatus = DefaultText;
        }

        private void CurrentEditor_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (CurrentEditor.ActiveVisual != null)
            {
                UIUtils.DoPropertyEdit(e, CurrentEditor);
            }
        }

        private void CurrentEditor_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            Bond existingBond = null;

            if (_adorner != null)
            {
                RemoveAdorner(ref _adorner);
            }

            var targetedVisual = CurrentEditor.ActiveVisual;
            string bondOrder = EditController.CurrentBondOrder;
            //check to see if we have already got an atom remembered
            if (_currentAtomVisual != null && !(_currentAtomVisual is HydrogenVisual))
            {
                Point? lastPos;

                if (Dragging(e))
                {
                    CurrentStatus = "[Shift] = unlock length; [Ctrl] = unlock angle; [Esc] = cancel.";
                    //are we already on top of an atom?
                    if (targetedVisual is GroupVisual)
                    {
                        CurrentEditor.Cursor = Cursors.No;
                        lastPos = null;
                    }
                    else if (!(targetedVisual is HydrogenVisual) && targetedVisual is AtomVisual atomUnderCursor)
                    {
                        CurrentEditor.Cursor = CursorUtils.Pencil;
                        //if so. snap to the atom's position
                        lastPos = atomUnderCursor.Position;
                        //if we are stroking over an existing bond
                        //then draw a double bond adorner

                        existingBond = _lastAtomVisual.ParentAtom.BondBetween(atomUnderCursor.ParentAtom);
                        if (_lastAtomVisual != null &&
                            existingBond != null)
                        {
                            if (existingBond.Order == OrderSingle)
                            {
                                bondOrder = OrderDouble;
                            }
                            else if (existingBond.Order == OrderDouble)
                            {
                                bondOrder = OrderTriple;
                            }
                            else if (existingBond.Order == OrderTriple)
                            {
                                bondOrder = OrderSingle;
                            }
                        }
                    }
                    else //or dangling over free space?
                    {
                        CurrentEditor.Cursor = CursorUtils.Pencil;
                        lastPos = e.GetPosition(CurrentEditor);

                        var angleBetween =
                            Vector.AngleBetween(_lastAtomVisual?.ParentAtom?.BalancingVector() ?? GeometryTool.ScreenNorth,
                                                GeometryTool.ScreenNorth);
                        //snap a bond into position
                        lastPos = _angleSnapper.SnapBond(lastPos.Value, angleBetween);
                    }

                    if (lastPos != null)
                    {
                        _adorner = new DrawBondAdorner(CurrentEditor, Common.BondThickness)
                        {
                            Stereo = EditController.CurrentStereo,
                            BondOrder = bondOrder,
                            ExistingBond = existingBond
                        };
                        _adorner.StartPoint = _currentAtomVisual.Position;
                        _adorner.EndPoint = lastPos.Value;
                    }
                }
            }
            else
            {
                if (targetedVisual != null)
                {
                    switch (targetedVisual)
                    {
                        case ReactionVisual _:
                            CurrentStatus = "Click to set reaction type";
                            CurrentEditor.Cursor = CursorUtils.Pencil;
                            break;

                        case GroupVisual _:
                            CurrentStatus = "Ungroup before attempting to draw.";
                            CurrentEditor.Cursor = Cursors.No;
                            break;

                        case HydrogenVisual _:
                            CurrentStatus = "Click to rotate hydrogen";
                            CurrentEditor.Cursor = Cursors.Hand;
                            break;

                        case AtomVisual av:
                            CurrentEditor.Cursor = CursorUtils.Pencil;
                            if (EditController.SelectedElement != av.ParentAtom.Element)
                            {
                                CurrentStatus = "Click to set element.";
                            }
                            else
                            {
                                CurrentStatus = "Click to sprout chain";
                            }
                            break;

                        case BondVisual _:
                            CurrentEditor.Cursor = CursorUtils.Pencil;
                            CurrentStatus = "Click to modify bond";
                            break;
                    }
                }
                else
                {
                    CurrentEditor.Cursor = CursorUtils.Pencil;
                    CurrentStatus = "Click to draw atom";
                }
            }
        }

        private void CurrentEditor_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            CurrentEditor.ReleaseMouseCapture();
            CurrentStatus = "";
            if (_currentAtomVisual is HydrogenVisual) //just exit
            {
                return;
            }
            if (IsDrawing)
            {
                var newPos = e.GetPosition(CurrentEditor);

                //first get the current active visuals
                var landedGroupVisual = CurrentEditor.GetTargetedVisual(newPos) as GroupVisual;
                var landedAtomVisual = CurrentEditor.GetTargetedVisual(newPos) as AtomVisual;
                var landedBondVisual = CurrentEditor.GetTargetedVisual(newPos) as BondVisual;
                if (landedAtomVisual is HydrogenVisual) //just exit
                {
                    return;
                }

                //check to see whether or not we've clicked and released on the same atom
                bool sameAtom = landedAtomVisual == _currentAtomVisual;

                //check to see whether the target is in the same molecule
                bool sameMolecule = landedAtomVisual?.ParentAtom.Parent == _currentAtomVisual?.ParentAtom.Parent;

                if (landedGroupVisual != null)
                {
                    ClearTemporaries();
                    return;
                }

                //check bonds first - we can't connect to a bond so we need to simply do some stuff with it
                if (landedBondVisual != null)
                {
                    //clicking on a stereo bond should just invert it
                    var parentBond = landedBondVisual.ParentBond;
                    if (parentBond.Stereo == BondStereo.Hatch && EditController.CurrentStereo == BondStereo.Hatch
                        || parentBond.Stereo == BondStereo.Wedge && EditController.CurrentStereo == BondStereo.Wedge)
                    {
                        EditController.SwapBondDirection(parentBond);
                    }
                    else
                    {
                        //modify the bond attribute (order, stereo, whatever's selected really)
                        EditController.SetBondAttributes(parentBond);
                    }
                }
                else //we clicked on empty space or an atom
                {
                    Atom parentAtom = _currentAtomVisual?.ParentAtom;
                    if (landedAtomVisual == null) //no atom hit
                    {
                        if (parentAtom != null)
                        {
                            //so just sprout a chain off it at two-o-clock
                            EditController.AddAtomChain(
                                parentAtom, _angleSnapper.SnapBond(newPos),
                                ClockDirections.II);
                        }
                        else
                        {
                            //otherwise create a singleton
                            EditController.AddAtomChain(null, newPos, ClockDirections.II);
                        }
                    }
                    else //we went mouse-up on an atom
                    {
                        Atom lastAtom = landedAtomVisual.ParentAtom;
                        if (sameAtom) //both are the same atom
                        {
                            if (lastAtom.Element.Symbol != EditController.SelectedElement.Symbol)
                            {
                                EditController.SetElement(EditController.SelectedElement, new List<Atom> { lastAtom });
                            }
                            else
                            {
                                var atomMetrics = GetNewChainEndPos(landedAtomVisual);
                                EditController.AddAtomChain(lastAtom, atomMetrics.NewPos, atomMetrics.sproutDir);
                                parentAtom.UpdateVisual();
                            }
                        }
                        else //we must have hit a different atom altogether
                        {
                            if (parentAtom != null)
                            {
                                //already has a bond to the target atom
                                var existingBond = parentAtom.BondBetween(lastAtom);
                                if (existingBond != null) //it must be in the same molecule
                                {
                                    EditController.IncreaseBondOrder(existingBond);
                                }
                                else //doesn't have a bond to the target atom
                                {
                                    if (sameMolecule)
                                    {
                                        EditController.AddNewBond(parentAtom, lastAtom, parentAtom.Parent);
                                    }
                                    else
                                    {
                                        EditController.JoinMolecules(parentAtom, lastAtom,
                                                                    EditController.CurrentBondOrder,
                                                                    EditController.CurrentStereo);
                                    }
                                    parentAtom.UpdateVisual();
                                    lastAtom.UpdateVisual();
                                }
                            }
                        }
                    }
                }
            }

            ClearTemporaries();
        }

        public override void Abort()
        {
            CurrentEditor.ReleaseMouseCapture();
            CurrentStatus = "";
            ClearTemporaries();
        }

        private void ClearTemporaries()
        {
            if (_adorner != null)
            {
                RemoveAdorner(ref _adorner);
            }

            _currentAtomVisual = null;
            IsDrawing = false;
            //clear this to prevent a weird bug in drawing
            CurrentEditor.ActiveChemistry = null;
            CurrentEditor.Focus();
        }

        private bool CrowdingOut(Point p)
        {
            return CurrentEditor.GetTargetedVisual(p) is AtomVisual;
        }

        private void RemoveAdorner(ref DrawBondAdorner adorner)
        {
            var layer = AdornerLayer.GetAdornerLayer(CurrentEditor);

            layer.Remove(adorner);
            adorner = null;
        }

        private static ClockDirections GetGeneralDir(Vector bondVector)
        {
            double bondAngle = Vector.AngleBetween(GeometryTool.ScreenNorth, bondVector);

            ClockDirections hour = (ClockDirections)GeometryTool.SnapToClock(bondAngle);
            return hour;
        }

        /// <summary>
        /// tells you where to put a new atom
        /// </summary>
        /// <param name="lastAtomVisual"></param>
        /// <returns></returns>
        private (Point NewPos, ClockDirections sproutDir) GetNewChainEndPos(AtomVisual lastAtomVisual)
        {
            var lastAtom = lastAtomVisual.ParentAtom;
            Vector newDirection;

            ClockDirections newTag;

            if (lastAtom.Degree == 0) //isolated atom
            {
                newDirection = ClockDirections.II.ToVector() * EditController.Model.XamlBondLength;
                newTag = ClockDirections.II;
            }
            else if (lastAtom.Degree == 1)
            {
                Vector bondVector = lastAtom.Position - lastAtom.Neighbours.First().Position;

                var hour = SnapToHour(bondVector);

                if (VirginAtom(lastAtom)) //it hasn't yet sprouted
                {
                    //Tag is used to store the direction the atom sprouted from its previous atom
                    newTag = GetNewSproutDirection(hour);
                    newDirection = newTag.ToVector() * EditController.Model.XamlBondLength;
                }
                else //it has sprouted, so where to put the new branch?
                {
                    var vecA = ((ClockDirections)lastAtom.Tag).ToVector();
                    vecA.Normalize();
                    var vecB = -bondVector;
                    vecB.Normalize();

                    var balancingVector = -(vecA + vecB);
                    balancingVector.Normalize();
                    newTag = SnapToHour(balancingVector);
                    newDirection = balancingVector * EditController.Model.XamlBondLength;
                }
            }
            else if (lastAtom.Degree == 2)
            {
                var balancingVector = lastAtom.BalancingVector();
                balancingVector.Normalize();
                newDirection = balancingVector * EditController.Model.XamlBondLength;
                newTag = SnapToHour(balancingVector);
            }
            else //lastAtom.Degree >= 2:  could get congested
            {
                FindOpenSpace(lastAtom, EditController.Model.XamlBondLength, out newDirection);
                newTag = SnapToHour(newDirection);
            }

            return (newDirection + lastAtom.Position, newTag);

            //local function
            ClockDirections SnapToHour(Vector bondVector)
            {
                double bondAngle = Vector.AngleBetween(GeometryTool.ScreenNorth, bondVector);

                ClockDirections hour = (ClockDirections)GeometryTool.SnapToClock(bondAngle);
                return hour;
            }
        }

        /// <summary>
        /// Tries to find the best place to put a bond
        /// by placing it in uncongested space
        /// </summary>
        /// <param name="rootAtom"></param>
        /// <param name="modelXamlBondLength"></param>
        /// <param name="vector"></param>
        /// <returns></returns>
        private void FindOpenSpace(Atom rootAtom, double modelXamlBondLength, out Vector vector)
        {
            //we need to work out which adjacent pairs of bonds around the atom have most space
            //first, sort each bond around the atom by its position from twelve-o-clock
            List<CandidatePlacement> possiblePlacements = new List<CandidatePlacement>();
            var atomBonds = (from b in rootAtom.Bonds
                             orderby b.AngleStartingAt(rootAtom)
                             select b).ToList();
            //add the first item in at the end so this makes comparison of pairs easier
            atomBonds.Add(atomBonds[0]);
            //now compare each bond with the previous bond and sort them by angle descending

            for (int i = 1; i < atomBonds.Count; i++)
            {
                var otherAtom = atomBonds[i - 1].OtherAtom(rootAtom);
                Vector vec0 = otherAtom.Position - rootAtom.Position;
                var atom = atomBonds[i].OtherAtom(rootAtom);
                Vector vec1 = atom.Position - rootAtom.Position;

                vec0.Normalize();
                vec1.Normalize();

                var splitDirection = vec0 + vec1;
                splitDirection.Normalize();

                var angleBetween = Vector.AngleBetween(vec0, vec1);
                if (angleBetween < 180d)
                {
                    var combinedWeights = atom.Degree + otherAtom.Degree;
                    var possiblePlacement = rootAtom.Position + (splitDirection * modelXamlBondLength);
                    CandidatePlacement cp = new CandidatePlacement
                    {
                        NeighbourWeights = combinedWeights,
                        Orientation = splitDirection,
                        Separation = (int)angleBetween,
                        PossiblePlacement = possiblePlacement,
                        Crowding = CrowdingOut(possiblePlacement)
                    };
                    possiblePlacements.Add(cp);
                }
            }

            var sortedPlacements = (from p in possiblePlacements
                                    orderby p.Crowding ascending, p.NeighbourWeights, p.Separation descending

                                    select p);

            Vector newPlacement = sortedPlacements.First().Orientation;

            newPlacement.Normalize();
            newPlacement *= modelXamlBondLength;
            vector = newPlacement;
        }

        private bool VirginAtom(Atom lastAtom)
        {
            return lastAtom.Tag == null;
        }

        private static ClockDirections GetNewSproutDirection(ClockDirections hour)
        {
            ClockDirections newTag;
            switch (hour)
            {
                case ClockDirections.I:
                    newTag = ClockDirections.III;
                    break;

                case ClockDirections.II:
                    newTag = ClockDirections.IV;
                    break;

                case ClockDirections.III:
                    newTag = ClockDirections.II;
                    break;

                case ClockDirections.IV:
                    newTag = ClockDirections.II;
                    break;

                case ClockDirections.V:
                    newTag = ClockDirections.III;
                    break;

                case ClockDirections.VI:
                    newTag = ClockDirections.VIII;
                    break;

                case ClockDirections.VII:
                    newTag = ClockDirections.IX;
                    break;

                case ClockDirections.VIII:
                    newTag = ClockDirections.X;
                    break;

                case ClockDirections.IX:
                    newTag = ClockDirections.XI;
                    break;

                case ClockDirections.X:
                    newTag = ClockDirections.VIII;
                    break;

                case ClockDirections.XII:
                    newTag = ClockDirections.I;
                    break;

                default:
                    newTag = ClockDirections.II;
                    break;
            }
            return newTag;
        }

        private void CurrentEditor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(CurrentEditor);
            ChemicalVisual chemicalVisual = CurrentEditor.GetTargetedVisual(position);
            if (chemicalVisual is ReactionVisual rv) //we hit a reaction
            {
                //only bother modifying if the selected reaction type is different
                if (EditController.SelectedReactionType.Value != rv.ParentReaction.ReactionType)
                {
                    EditController.SetReactionType(EditController.SelectedReactionType.Value, rv.ParentReaction);
                }
            }
            else
            {
                _currentAtomVisual = chemicalVisual as AtomVisual;
                IsDrawing = true;

                if (_currentAtomVisual is null)
                {
                    _angleSnapper = new Snapper(position, EditController);
                }
                else if (!(_currentAtomVisual is HydrogenVisual))
                {
                    _angleSnapper = new Snapper(_currentAtomVisual.ParentAtom.Position, EditController);
                    Mouse.Capture(CurrentEditor);
                    _lastAtomVisual = _currentAtomVisual;
                }
                else //its a hydrogen visual
                {
                    HydrogenVisual hv = (HydrogenVisual)_currentAtomVisual;
                    EditController.RotateHydrogen(hv.ParentVisual.ParentAtom);
                    IsDrawing = false;  //stops drop of an isolated atom
                    e.Handled = true;
                }
            }
        }

        private bool Dragging(MouseEventArgs e)
        {
            return e.LeftButton == MouseButtonState.Pressed & IsDrawing;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            CurrentEditor.MouseLeftButtonDown -= CurrentEditor_MouseLeftButtonDown;
            CurrentEditor.PreviewMouseLeftButtonUp -= CurrentEditor_PreviewMouseLeftButtonUp;
            CurrentEditor.PreviewMouseMove -= CurrentEditor_PreviewMouseMove;
            CurrentEditor.PreviewMouseRightButtonUp -= CurrentEditor_PreviewMouseRightButtonUp;
            CurrentStatus = "";
            CurrentEditor.Cursor = _lastCursor;
        }
    }
}