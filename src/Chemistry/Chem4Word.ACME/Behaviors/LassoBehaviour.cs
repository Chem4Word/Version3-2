// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
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
using Chem4Word.ACME.Drawing;
using Chem4Word.ACME.Enums;
using Chem4Word.ACME.Utils;
using Chem4Word.Model2;

namespace Chem4Word.ACME.Behaviors
{
    public class LassoBehaviour : BaseEditBehavior
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
        public bool IsDragging { get; private set; }
        public bool ClickedOnAtomOrBond { get; set; }

        public Point StartPoint { get; set; }

        public bool RectMode
        {
            get { return (bool)GetValue(RectModeProperty); }
            set { SetValue(RectModeProperty, value); }
        }

        public static readonly DependencyProperty RectModeProperty =
            DependencyProperty.Register("RectMode", typeof(bool), typeof(LassoBehaviour), new PropertyMetadata(default(bool)));

        public LassoBehaviour() : this(false)
        { }

        public LassoBehaviour(bool isRectMode = false)
        { }

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
            _bondLength = CurrentEditor.ViewModel.Model.MeanBondLength;
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
                EditViewModel.ClearSelection();
            }

            _mouseTrack = new PointCollection();
            _startpoint = Mouse.GetPosition(CurrentEditor);

            Mouse.Capture(CurrentEditor);
            _mouseTrack.Add(_startpoint);

            if (e.ClickCount == 2 & EditViewModel.SelectionType == SelectionTypeCode.Molecule)
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

            if (EditViewModel.SelectedItems.Any())
            {
                EditViewModel.ClearSelection();
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
                    EditViewModel.DoTransform(_shift, _atomList);
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
                else if (_initialTarget != null && EditViewModel.SelectedItems.Contains(_initialTarget))
                {
                    DoSelectionClick(e);
                }

                if (_lassoAdorner != null)
                {
                    _lassoHits = new List<object>();
                    GatherSelection(_lassoAdorner.Outline);
                    _lassoHits = _lassoHits.Distinct().ToList();
                    EditViewModel.AddToSelection(_lassoHits);
                }
                if (EditViewModel.SelectedItems.Any())
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
            var al = AdornerLayer.GetAdornerLayer(CurrentEditor);
            al.Update();
            CurrentStatus = DefaultText;
        }

        private void GatherSelection(StreamGeometry lassoAdornerOutline)
        {
            VisualTreeHelper.HitTest(CurrentEditor, null, GatherCallback, new GeometryHitTestParameters(lassoAdornerOutline));
        }

        private HitTestResultBehavior GatherCallback(HitTestResult result)
        {
            var myShape = result.VisualHit;
            if (myShape is GroupVisual selGroup)
            {
                _lassoHits.Add(selGroup.ParentMolecule);
            }
            if (myShape is AtomVisual av)
            {
                var selAtom = av.ParentAtom;

                if (!EditViewModel.SelectedItems.Contains(selAtom))
                {
                    _lassoHits.Add(selAtom);
                }
            }
            return HitTestResultBehavior.Continue;
        }

        private object CurrentObject(MouseButtonEventArgs e)
        {
            var visual = CurrentEditor.GetTargetedVisual(e.GetPosition(CurrentEditor));

            object currentObject = null;
            if (visual is AtomVisual av)
            {
                currentObject = av.ParentAtom;
            }
            else if (visual is BondVisual bv)
            {
                currentObject = bv.ParentBond;
            }
            else if (visual is GroupVisual gv)
            {
                currentObject = gv.ParentMolecule;
            }

            return currentObject;
        }

        private void CurrentEditor_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var pos = Mouse.GetPosition(CurrentEditor);
            Vector shift; //how much we want to shift the objects by

            if (MouseIsDown(e) & !IsDragging)
            {
                CurrentStatus = "Draw around atoms and bonds to select.";
                if (_initialTarget == null)
                {
                    if (_mouseTrack == null)
                    {
                        _mouseTrack = new PointCollection();
                    }

                    StreamGeometry outline = null;
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

                    outline = GetPolyGeometry();

                    if (_lassoAdorner == null)
                    {
                        _lassoAdorner = new LassoAdorner(CurrentEditor, outline);
                    }

                    if (Mouse.Captured != CurrentEditor)
                    {
                        Mouse.Capture(CurrentEditor);
                    }

                    _lassoAdorner.Outline = outline;
                }
                else
                {
                    var target = CurrentObject(e);

                    if (_initialTarget != target)
                    {
                        IsDragging = true;
                    }
                }
            }
            //we're dragging an object around
            if (MouseIsDown(e) & IsDragging)
            {
                if (_initialTarget is Bond b &&
                    EditViewModel.SelectionType == SelectionTypeCode.Bond
                    && EditViewModel.SelectedItems.Count == 1) //i.e. we have one bond selected
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
                _ghostAdorner = new PartialGhostAdorner(EditViewModel, _atomList, _shift);
            }
        }

        private void DragAtom(Point pos)
        {
            Vector shift;

            //this code is horrendous, apologies
            //please don't modify it without good reason!
            //if you must then READ THE COMMENTS FIRST, PLEASE!

            _atomList = EditViewModel.SelectedItems.OfType<Atom>().ToList();
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
                for (var i = 0; (i < staticAtomBonds.Count()) & (connectingBond == null); i++)
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
                var movingAtom = connectingBond.OtherAtom(staticAtom);
                //get the location of the neighbour of the static atom that is going to move
                var movingPoint = movingAtom.Position;
                //now work out the separation between the current position and the moving atom
                var fragmentSpan = StartPoint - movingPoint; //this gives us the span of the deforming fragment
                var originalDistance = pos - staticPoint;
                //now we need to work out how far away from the static atom the moving atom should be
                var desiredDisplacement = originalDistance - fragmentSpan;
                //then we snap it
                var bondSnapper = new Snapper(staticPoint, EditViewModel, bondLength: _bondLength, lockAngle: 10);
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
            //do a quick test to work out the
            object currentObject = null;
            if (visual is AtomVisual av)
            {
                currentObject = av.ParentAtom;
            }
            else if (visual is BondVisual bv)
            {
                currentObject = bv.ParentBond;
            }

            return currentObject;
        }

        private Point GetCurrentMouseLocation(MouseEventArgs e) => e.GetPosition(CurrentEditor);

        private void CurrentEditor_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var currentObject = CurrentObject(e);

            if (!(currentObject != null | Utils.KeyboardUtils.HoldingDownShift()))
            {
                EditViewModel.ClearSelection();
            }

            StartPoint = e.GetPosition(CurrentEditor);

            _initialTarget = CurrentObject(e);
        }

        private void DisposeLasso()
        {
            RemoveAdorner(_lassoAdorner);
            _lassoAdorner = null;
        }

        private bool MouseIsDown(MouseEventArgs e) => e.LeftButton == MouseButtonState.Pressed;

        private void ModifySelection(StreamGeometry outline)
        {
            VisualTreeHelper.HitTest(CurrentEditor, null, HitTestCallback, new GeometryHitTestParameters(outline));
        }

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
                        EditViewModel.AddToSelection(atom);
                        CurrentStatus = ActiveSelText;
                        break;
                    }

                case BondVisual bv:
                    {
                        var bond = bv.ParentBond;
                        Debug.WriteLine($"Hit Bond {bond.Id} at ({e.GetPosition(CurrentEditor).X},{e.GetPosition(CurrentEditor).Y})");
                        EditViewModel.AddToSelection(bond);
                        CurrentStatus = ActiveSelText;
                        break;
                    }

                default:
                    EditViewModel.ClearSelection();
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
                    if (!EditViewModel.SelectedItems.Contains(mol))
                    {
                        EditViewModel.AddToSelection(mol);
                    }
                    else
                    {
                        EditViewModel.RemoveFromSelection(mol);
                    }

                    CurrentStatus = ActiveSelText;
                    break;

                case AtomVisual av:
                    {
                        var atom = av.ParentAtom;
                        var rootMolecule = atom.Parent.RootMolecule;
                        if (rootMolecule.IsGrouped)
                        {
                            EditViewModel.AddToSelection(rootMolecule);
                        }
                        else
                        {
                            if (!EditViewModel.SelectedItems.Contains(atom))
                            {
                                EditViewModel.AddToSelection(atom);
                            }
                            else
                            {
                                EditViewModel.RemoveFromSelection(atom);
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
                            EditViewModel.AddToSelection(rootMolecule);
                        }

                        if (!EditViewModel.SelectedItems.Contains(bond))
                        {
                            EditViewModel.AddToSelection(bond);
                        }
                        else
                        {
                            EditViewModel.RemoveFromSelection(bond);
                        }

                        CurrentStatus = ActiveSelText;
                        break;
                    }

                default:
                    EditViewModel.ClearSelection();
                    CurrentStatus = DefaultText;
                    break;
            }
        }

        private HitTestResult GetTarget(MouseButtonEventArgs e) =>
            VisualTreeHelper.HitTest(CurrentEditor, e.GetPosition(CurrentEditor));

        private HitTestResultBehavior HitTestCallback(HitTestResult result)
        {
            var id = ((GeometryHitTestResult)result).IntersectionDetail;

            var myShape = result.VisualHit;
            if (myShape is GroupVisual selGroup)
            {
                EditViewModel.AddToSelection(selGroup.ParentMolecule);
                return HitTestResultBehavior.Continue;
            }
            if (myShape != null && myShape is AtomVisual | myShape is BondVisual)
            {
                switch (id)
                {
                    case IntersectionDetail.FullyContains:
                    case IntersectionDetail.Intersects:
                    case IntersectionDetail.FullyInside:
                        var selAtom = (myShape as AtomVisual)?.ParentAtom;
                        var selBond = (myShape as BondVisual)?.ParentBond;

                        if (!(EditViewModel.SelectedItems.Contains(selAtom) ||
                              EditViewModel.SelectedItems.Contains(selBond)))
                        {
                            if (selAtom != null)
                            {
                                EditViewModel.AddToSelection(selAtom);
                            }

                            if (selBond != null)
                            {
                                EditViewModel.AddToSelection(selBond);
                            }
                        }

                        return HitTestResultBehavior.Continue;

                    case IntersectionDetail.Empty:
                        selAtom = (myShape as AtomVisual)?.ParentAtom;
                        selBond = (myShape as BondVisual)?.ParentBond;

                        if (EditViewModel.SelectedItems.Contains(selAtom) ||
                            EditViewModel.SelectedItems.Contains(selBond))
                        {
                            if (selAtom != null)
                            {
                                EditViewModel.RemoveFromSelection(selAtom);
                            }

                            if (selBond != null)
                            {
                                EditViewModel.RemoveFromSelection(selBond);
                            }
                        }

                        return HitTestResultBehavior.Continue;

                    default:
                        return HitTestResultBehavior.Stop;
                }
            }

            return HitTestResultBehavior.Continue;
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