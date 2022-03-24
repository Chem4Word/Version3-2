// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Chem4Word.ACME.Adorners;
using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Drawing.Visuals;
using Chem4Word.ACME.Enums;
using Chem4Word.ACME.Utils;
using Chem4Word.Model2;

namespace Chem4Word.ACME.Behaviors
{
    public class SelectBehaviour : BaseEditBehavior
    {
        private const string DefaultText =
            "Click to select; [Shift]-click to multselect; drag to select range; double-click to select molecule.";

        private const string ActiveSelText = "Set atoms/bonds using selectors; drag to reposition; [Delete] to remove.";
        private List<Atom> _atomList;

        private double _bondLength;

        private PartialGhostAdorner _ghostAdorner;
        private object _initialTarget;

        private LassoAdorner _lassoAdorner;

        private PointCollection _mouseTrack;

        private TransformGroup _shift;

        private Point _startpoint;
        private List<object> _lassoHits;
        private bool IsDragging { get; set; }

        private Point StartPoint { get; set; }

        public bool RectMode
        {
            get { return (bool)GetValue(RectModeProperty); }
            set { SetValue(RectModeProperty, value); }
        }

        public static readonly DependencyProperty RectModeProperty =
            DependencyProperty.Register("RectMode", typeof(bool), typeof(SelectBehaviour), new PropertyMetadata(default(bool)));

        protected override void OnAttached()
        {
            base.OnAttached();

            CurrentEditor = (EditorCanvas)AssociatedObject;

            CurrentEditor.PreviewMouseLeftButtonDown += CurrentEditor_PreviewMouseLeftButtonDown;
            CurrentEditor.PreviewMouseLeftButtonUp += CurrentEditor_PreviewMouseLeftButtonUp;
            CurrentEditor.PreviewMouseMove += CurrentEditor_PreviewMouseMove;
            CurrentEditor.PreviewMouseRightButtonUp += CurrentEditor_PreviewMouseRightButtonUp;

            CurrentEditor.Cursor = Cursors.Arrow;

            CurrentEditor.IsHitTestVisible = true;
            _bondLength = CurrentEditor.Controller.Model.MeanBondLength;
            CurrentStatus = DefaultText;
        }

        private void CurrentEditor_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            UIUtils.DoPropertyEdit(e, CurrentEditor);
        }

        private void DoSelectionClick(MouseButtonEventArgs e)
        {
            if (!(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
            {
                EditController.ClearSelection();
            }

            _mouseTrack = new PointCollection();
            _startpoint = Mouse.GetPosition(CurrentEditor);

            Mouse.Capture(CurrentEditor);
            _mouseTrack.Add(_startpoint);

            if (e.ClickCount == 2 && EditController.SelectionType == SelectionTypeCode.Molecule)
            {
                DoMolSelect(e);
                e.Handled = true;
            }

            if (e.ClickCount == 2)
            {
                DoMolSelect(e);
                e.Handled = true;
            }

            if (e.ClickCount == 1) //single click
            {
                ToggleSelect(e);
            }
        }

        public override void Abort()
        {
            if (IsDragging)
            {
                IsDragging = false;
                if (_ghostAdorner != null)
                {
                    RemoveAdorner(_ghostAdorner);
                    _ghostAdorner = null;
                }

                _atomList = null;
            }

            if (EditController.SelectedItems.Any())
            {
                EditController.ClearSelection();
                CurrentStatus = DefaultText;
            }

            if (_lassoAdorner != null)
            {
                DisposeLasso();
            }

            _initialTarget = null;
            _mouseTrack = null;

            CurrentEditor.ReleaseMouseCapture();
            CurrentStatus = DefaultText;
        }

        private void CurrentEditor_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            dynamic currentObject = CurrentObject(e);

            if (IsDragging)
            {
                if (_atomList != null && _atomList.Any())
                {
                    EditController.TransformAtoms(_shift, _atomList);
                    _atomList[0].Parent.UpdateVisual();
                }

                IsDragging = false;
                if (_ghostAdorner != null)
                {
                    RemoveAdorner(_ghostAdorner);
                    _ghostAdorner = null;
                }

                _atomList = null;
            }
            else
            {
                //did we go up on the target we went down on?
                if ((currentObject != null) & (currentObject == _initialTarget))
                {
                    //select it
                    DoSelectionClick(e);
                }
                else if (_initialTarget != null && EditController.SelectedItems.Contains(_initialTarget))
                {
                    DoSelectionClick(e);
                }

                if (_lassoAdorner != null)
                {
                    _lassoHits = new List<object>();
                    GatherSelection(_lassoAdorner.Outline);
                    _lassoHits = _lassoHits.Distinct().ToList();
                    EditController.AddObjectListToSelection(_lassoHits.Cast<BaseObject>().ToList());
                }
                if (EditController.SelectedItems.Any())
                {
                    CurrentStatus = ActiveSelText;
                }

                if (_lassoAdorner != null)
                {
                    DisposeLasso();
                }
            }

            _initialTarget = null;
            _mouseTrack = null;

            CurrentEditor.ReleaseMouseCapture();

            CurrentEditor.Focus();
            var layer = AdornerLayer.GetAdornerLayer(CurrentEditor);
            layer?.Update();
            CurrentStatus = DefaultText;
        }

        private void GatherSelection(StreamGeometry lassoAdornerOutline)
        {
            VisualTreeHelper.HitTest(CurrentEditor, null, GatherCallback, new GeometryHitTestParameters(lassoAdornerOutline));
        }

        private HitTestResultBehavior GatherCallback(HitTestResult result)
        {
            var myShape = result.VisualHit;
            switch (myShape)
            {
                case GroupVisual selGroup:
                    _lassoHits.Add(selGroup.ParentMolecule);
                    break;

                case AtomVisual av:
                    {
                        var selAtom = av.ParentAtom;

                        if (!EditController.SelectedItems.Contains(selAtom))
                        {
                            _lassoHits.Add(selAtom);
                        }

                        break;
                    }

                case ReactionVisual rv:
                    //only add the reaction of it's entirely enclosed by the selection region
                    if (_lassoAdorner.Outline.FillContains(rv.ParentReaction.HeadPoint) &&
                        _lassoAdorner.Outline.FillContains(rv.ParentReaction.TailPoint))
                    {
                        _lassoHits.Add(rv.ParentReaction);
                    }
                    break;

                case AnnotationVisual anv:
                    _lassoHits.Add(anv.ParentAnnotation);
                    break;
            }
            return HitTestResultBehavior.Continue;
        }

        private void CurrentEditor_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            //this is a *tunnelling* event which means it fires on the outmost
            //container before any of the visual children
            //TODO:  disable the hydrogen rotator selector
            var pos = Mouse.GetPosition(CurrentEditor);
            Vector shift; //how much we want to shift the objects by
            //first check to see whether we're dragging a thumb of some kind

            if (MouseIsDown(e) && !IsDragging)
            {
                CurrentStatus = "Draw around atoms and bonds to select.";
                if (_initialTarget == null)
                {
                    if (_mouseTrack == null)
                    {
                        _mouseTrack = new PointCollection();
                    }

                    if (!RectMode)
                    {
                        //just add the most recent point to the track
                        _mouseTrack.Add(pos);
                    }
                    else
                    {
                        //build a rectangle
                        _mouseTrack.Clear();
                        _mouseTrack.Add(StartPoint);
                        _mouseTrack.Add(new Point(pos.X, StartPoint.Y));
                        _mouseTrack.Add(new Point(pos.X, pos.Y));
                        _mouseTrack.Add(new Point(StartPoint.X, pos.Y));
                    }

                    StreamGeometry outline = GetPolyGeometry();

                    if (_lassoAdorner == null)
                    {
                        _lassoAdorner = new LassoAdorner(CurrentEditor, outline);
                    }

                    // ReSharper disable once PossibleUnintendedReferenceComparison
                    if (Mouse.Captured != CurrentEditor)
                    {
                        Mouse.Capture(CurrentEditor);
                    }

                    _lassoAdorner.Outline = outline;
                }
                else
                {
                    var target = CurrentObject(e);
                    if (target is Reaction r)
                    {
                        //don't drag the reaction if hitting a text block
                        ReactionVisual rv = (ReactionVisual)CurrentEditor.ChemicalVisuals[r];
                        if (rv.ReagentsBlockRect.Contains(pos) || rv.ConditionsBlockRect.Contains(pos))
                        {
                            IsDragging = false;
                            return;
                        }
                    }
                    if (_initialTarget != target)
                    {
                        IsDragging = true;
                    }
                }
            }

            //we're dragging an object around
            if (MouseIsDown(e) && IsDragging)
            {
                if (_initialTarget is Bond b
                    && EditController.SelectionType == SelectionTypeCode.Bond
                    && EditController.SelectedItems.Count == 1) //i.e. we have one bond selected
                {
                    CurrentStatus = "Drag bond to reposition.";
                    _atomList = new List<Atom> { b.StartAtom, b.EndAtom };
                    shift = pos - StartPoint;
                    var tt = new TranslateTransform(shift.X, shift.Y);
                    _shift = new TransformGroup();
                    _shift.Children.Add(tt);
                }
                else //we're dragging an atom
                {
                    RemoveGhost();
                    DragAtom(pos);
                }

                RemoveGhost();
                _ghostAdorner = new PartialGhostAdorner(EditController, _atomList, _shift);
            }
        }

        private void DragAtom(Point pos)
        {
            Vector shift;

            //this code is horrendous, apologies
            //please don't modify it without good reason!
            //if you must then READ THE COMMENTS FIRST, PLEASE!

            _atomList = EditController.SelectedItems.OfType<Atom>().ToList();
            var immediateNeighbours = GetImmediateNeighbours(_atomList);
            //we need to check to see whether we are moving an atom connected to the rest of the molecule by a single bond
            //if we are then we can invoke the bond snapper to limit the movement
            if (immediateNeighbours.Count == 1) //we are moving an atom attached by a single bond
            {
                CurrentStatus = "[Shift] = unlock length; [Ctrl] = unlock angle; [Alt] = pivot.";
                //so invoke the snapper!
                //grab the atom in the static fragment
                var staticAtom = immediateNeighbours[0];

                //now identify the connecting bond with the moving fragment
                Bond connectingBond = null;
                var staticAtomBonds = staticAtom.Bonds.ToArray();
                for (var i = 0; i < staticAtomBonds.Length && connectingBond == null; i++)
                {
                    var bond = staticAtomBonds[i];
                    var otherAtom = bond.OtherAtom(staticAtom);
                    if (_atomList.Contains(otherAtom))
                    {
                        connectingBond = staticAtom.BondBetween(otherAtom);
                    }
                }

                //locate the static atom
                var staticPoint = staticAtom.Position;
                //identify the moving atom
                if (connectingBond != null)
                {
                    var movingAtom = connectingBond.OtherAtom(staticAtom);
                    //get the location of the neighbour of the static atom that is going to move
                    var movingPoint = movingAtom.Position;
                    //now work out the separation between the current position and the moving atom
                    var fragmentSpan = StartPoint - movingPoint; //this gives us the span of the deforming fragment
                    var originalDistance = pos - staticPoint;
                    //now we need to work out how far away from the static atom the moving atom should be
                    var desiredDisplacement = originalDistance - fragmentSpan;
                    //then we snap it
                    var bondSnapper = new Snapper(staticPoint, EditController, bondLength: _bondLength, lockAngle: 10);
                    var snappedBondVector = bondSnapper.SnapVector(connectingBond.Angle, desiredDisplacement);

                    //subtract the original bond vector to get the actual desired, snapped shift
                    var bondVector = movingPoint - staticPoint;
                    //now calculate the angle between the starting bond and the snapped vector
                    var rotation = Vector.AngleBetween(bondVector, snappedBondVector);

                    shift = snappedBondVector - bondVector;
                    //shift the atom and rotate the group around the new terminus
                    var pivot = staticPoint + snappedBondVector;
                    RotateTransform rt;
                    if (KeyboardUtils.HoldingDownAlt())
                    {
                        rt = new RotateTransform(rotation, pivot.X, pivot.Y);
                    }
                    else
                    {
                        rt = new RotateTransform();
                    }

                    var tg = new TransformGroup();
                    tg.Children.Add(new TranslateTransform(shift.X, shift.Y));
                    tg.Children.Add(rt);

                    _shift = tg;
                }
            }
            else //moving an atom linked to two other neighbours
            {
                shift = pos - StartPoint;
                CurrentStatus = "Drag atom to reposition";
                var tt = new TranslateTransform(shift.X, shift.Y);
                _shift = new TransformGroup();
                _shift.Children.Add(tt);
            }
        }

        private void RemoveGhost()
        {
            if (_ghostAdorner != null)
            {
                RemoveAdorner(_ghostAdorner);
                _ghostAdorner = null;
            }
        }

        private List<Atom> GetImmediateNeighbours(List<Atom> atomList)
        {
            var neighbours = from a in atomList
                             from n in a.Neighbours
                             where !atomList.Contains(n)
                             select n;
            return neighbours.ToList();
        }

        private object CurrentObject(MouseEventArgs e)
        {
            var visual = CurrentEditor.GetTargetedVisual(GetCurrentMouseLocation(e));

            object currentObject = null;
            if (visual is AtomVisual av)
            {
                currentObject = av.ParentAtom;
            }
            else if (visual is BondVisual bv)
            {
                currentObject = bv.ParentBond;
            }
            else if (visual is ReactionVisual rv)
            {
                currentObject = rv.ParentReaction;
            }
            return currentObject;
        }

        private object CurrentObject(MouseButtonEventArgs e)
        {
            var visual = CurrentEditor.GetTargetedVisual(e.GetPosition(CurrentEditor));

            object currentObject = null;

            switch (visual)
            {
                case AtomVisual av:
                    currentObject = av.ParentAtom;
                    break;

                case BondVisual bv:
                    currentObject = bv.ParentBond;
                    break;

                case GroupVisual gv:
                    currentObject = gv.ParentMolecule;
                    break;

                case ReactionVisual rv:
                    currentObject = rv.ParentReaction;
                    break;

                case AnnotationVisual anv:
                    currentObject = anv.ParentAnnotation;
                    break;

                default:
                    currentObject = null;
                    break;
            }
            return currentObject;
        }

        private Point GetCurrentMouseLocation(MouseEventArgs e) => e.GetPosition(CurrentEditor);

        private void CurrentEditor_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ChemicalVisual visual = CurrentEditor.GetTargetedVisual(e.GetPosition(CurrentEditor));
            if (e.ClickCount == 1)
            {
                if (visual is HydrogenVisual hv)
                {
                    EditController.RotateHydrogen(hv.ParentVisual.ParentAtom);
                }
                else
                {
                    var currentObject = CurrentObject(e);

                    if (!(currentObject != null || KeyboardUtils.HoldingDownShift()))
                    {
                        EditController.ClearSelection();
                    }

                    StartPoint = e.GetPosition(CurrentEditor);

                    _initialTarget = CurrentObject(e);
                }
            }
            else if (e.ClickCount == 2)
            {
                if (visual is ReactionVisual rv)
                //edit the reagents/conditions
                {
                    //make sure that the reaction visual is selected
                    EditController.AddToSelection(rv.ParentReaction);

                    var originalLocation = e.GetPosition(CurrentEditor);
                    if (e.ClickCount == 2)
                    {
                        if (rv.ConditionsBlockRect.Contains(originalLocation))
                        {
                            EditController.EditConditions();
                            e.Handled = true;
                        }
                        else if (rv.ReagentsBlockRect.Contains(originalLocation))
                        {
                            EditController.EditReagents();
                            e.Handled = true;
                        }
                    }
                }
            }
        }

        private void DisposeLasso()
        {
            RemoveAdorner(_lassoAdorner);
            _lassoAdorner = null;
        }

        private bool MouseIsDown(MouseEventArgs e) => e.LeftButton == MouseButtonState.Pressed;

        private void RemoveAdorner(Adorner adorner)
        {
            var layer = AdornerLayer.GetAdornerLayer(CurrentEditor);

            layer.Remove(adorner);
        }

        private StreamGeometry GetPolyGeometry()
        {
            if (_mouseTrack != null)
            {
                var geo = new StreamGeometry();
                using (var context = geo.Open())
                {
                    context.BeginFigure(_mouseTrack[0], true, true);

                    // Add the points after the first one.
                    context.PolyLineTo(_mouseTrack.Skip(1).ToArray(), true, false);
                }

                return geo;
            }
            else
            {
                return null;
            }
        }

        private void DoMolSelect(MouseButtonEventArgs e)
        {
            var activeVisual = CurrentEditor.ActiveVisual;
            CurrentEditor.ActiveVisual = null;

            switch (activeVisual)
            {
                case AtomVisual av:
                    {
                        var atom = av.ParentAtom;
                        Debug.WriteLine($"Hit Atom {atom.Id} at ({atom.Position.X},{atom.Position.Y})");
                        EditController.AddToSelection(atom);
                        CurrentStatus = ActiveSelText;
                        break;
                    }

                case BondVisual bv:
                    {
                        var bond = bv.ParentBond;
                        Debug.WriteLine($"Hit Bond {bond.Id} at ({e.GetPosition(CurrentEditor).X},{e.GetPosition(CurrentEditor).Y})");
                        EditController.AddToSelection(bond);
                        CurrentStatus = ActiveSelText;
                        break;
                    }

                default:
                    EditController.ClearSelection();
                    CurrentStatus = DefaultText;
                    break;
            }
        }

        private void ToggleSelect(MouseButtonEventArgs e)
        {
            var activeVisual = CurrentEditor.ActiveVisual;
            CurrentEditor.ActiveVisual = null;

            switch (activeVisual)
            {
                case GroupVisual gv:
                    var mol = gv.ParentMolecule;
                    if (!EditController.SelectedItems.Contains(mol))
                    {
                        EditController.AddToSelection(mol);
                    }
                    else
                    {
                        EditController.RemoveFromSelection(mol);
                    }

                    CurrentStatus = ActiveSelText;
                    break;

                case AtomVisual av:
                    {
                        var atom = av.ParentAtom;
                        //check just in case the parent atom is null -- can happen occasionally
                        if (atom != null)
                        {
                            var rootMolecule = atom.Parent.RootMolecule;
                            if (rootMolecule.IsGrouped)
                            {
                                EditController.AddToSelection(rootMolecule);
                            }
                            else
                            {
                                if (!EditController.SelectedItems.Contains(atom))
                                {
                                    EditController.AddToSelection(atom);
                                }
                                else
                                {
                                    EditController.RemoveFromSelection(atom);
                                }
                            }
                        }

                        CurrentStatus = ActiveSelText;
                        break;
                    }

                case BondVisual bv:
                    {
                        var bond = bv.ParentBond;
                        var rootMolecule = bond.Parent.RootMolecule;
                        if (rootMolecule.IsGrouped)
                        {
                            EditController.AddToSelection(rootMolecule);
                        }

                        if (!EditController.SelectedItems.Contains(bond))
                        {
                            EditController.AddToSelection(bond);
                        }
                        else
                        {
                            EditController.RemoveFromSelection(bond);
                        }

                        CurrentStatus = ActiveSelText;
                        break;
                    }
                case ReactionVisual rv:
                    {
                        var reaction = rv.ParentReaction;
                        if (!EditController.SelectedItems.Contains(reaction))
                        {
                            EditController.AddToSelection(reaction);
                        }
                        else
                        {
                            EditController.RemoveFromSelection(reaction);
                        }
                        break;
                    }
                case AnnotationVisual anv:
                    {
                        var annotation = anv.ParentAnnotation;
                        if (!EditController.SelectedItems.Contains(anv))
                        {
                            EditController.AddToSelection(annotation);
                        }
                        else
                        {
                            EditController.RemoveFromSelection(annotation);
                        }
                        break;
                    }
                default:
                    EditController.ClearSelection();
                    CurrentStatus = DefaultText;
                    break;
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            CurrentEditor.PreviewMouseLeftButtonDown -= CurrentEditor_PreviewMouseLeftButtonDown;
            CurrentEditor.MouseLeftButtonUp -= CurrentEditor_PreviewMouseLeftButtonUp;
            CurrentEditor.PreviewMouseMove -= CurrentEditor_PreviewMouseMove;
            CurrentEditor.PreviewMouseRightButtonUp -= CurrentEditor_PreviewMouseRightButtonUp;

            _lassoAdorner = null;
        }
    }
}