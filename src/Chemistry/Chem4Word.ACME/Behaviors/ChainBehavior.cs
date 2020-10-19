// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
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
using Chem4Word.ACME.Adorners;
using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Drawing;
using Chem4Word.ACME.Utils;
using Chem4Word.Model2;
using Chem4Word.Model2.Geometry;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.ACME.Behaviors
{
    public class ChainBehavior : BaseEditBehavior
    {
        private ChainAdorner _currentAdorner;
        private Window _parent;
        public List<Point> Placements { get; private set; }
        public bool Clashing { get; private set; } = false;
        private Cursor _lastCursor;

        public ChainAdorner CurrentAdorner
        {
            get => _currentAdorner;
            set
            {
                RemoveChainAdorner();
                _currentAdorner = value;
                if (_currentAdorner != null)
                {
                    _currentAdorner.MouseLeftButtonDown += CurrentEditor_MouseLeftButtonDown;
                    _currentAdorner.MouseLeftButtonUp += CurrentEditor_MouseLeftButtonUp;
                    _currentAdorner.PreviewKeyDown += CurrentEditor_PreviewKeyDown;
                }

                //local function
                void RemoveChainAdorner()
                {
                    if (_currentAdorner != null)
                    {
                        var layer = AdornerLayer.GetAdornerLayer(CurrentEditor);
                        layer.Remove(_currentAdorner);
                        _currentAdorner.MouseLeftButtonDown -= CurrentEditor_MouseLeftButtonDown;
                        _currentAdorner.MouseLeftButtonUp -= CurrentEditor_MouseLeftButtonUp;
                        _currentAdorner.PreviewKeyDown -= CurrentEditor_PreviewKeyDown;
                        _currentAdorner = null;
                    }
                }
            }
        }

        private void CurrentEditor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Abort();
            }
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
            CurrentEditor.PreviewKeyDown += CurrentEditor_PreviewKeyDown;

            AssociatedObject.IsHitTestVisible = true;

            if (_parent != null)
            {
                _parent.MouseLeftButtonDown += CurrentEditor_MouseLeftButtonDown;
            }

            CurrentStatus = "Draw a ring by clicking on a bond, atom or free space.";
        }

        private void CurrentEditor_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (IsDrawing && !Clashing)
            {
                EditViewModel.DrawChain(Placements, Target);
            }

            MouseIsDown = false;
            IsDrawing = false;
            CurrentEditor.ReleaseMouseCapture();
            CurrentEditor.Focus();
            CurrentAdorner = null;
            Placements = null;
            Clashing = false;
            CurrentEditor.Cursor = CursorUtils.Pencil;
        }

        private void CurrentEditor_MouseMove(object sender, MouseEventArgs e)
        {
            Clashing = false;
            if (MouseIsDown)
            {
                IsDrawing = true;
            }

            if (IsDrawing)
            {
                if (CurrentEditor.ActiveVisual is GroupVisual gv)
                {
                    CurrentEditor.Cursor = Cursors.No;
                }
                else
                {
                    CurrentStatus = "Drag to start sizing chain: [Esc] to cancel.";
                    var endPoint = e.GetPosition(EditViewModel.CurrentEditor);

                    MarkOutAtoms(endPoint, e);
                    CurrentAdorner =
                        new ChainAdorner(FirstPoint, CurrentEditor, EditViewModel.EditBondThickness, Placements,
                                         endPoint, Target);

                    var targetedVisual = EditViewModel.CurrentEditor.GetTargetedVisual(endPoint);
                    //check to see we're not overwriting
                    bool overWritingSelf = false;
                    if (CurrentAdorner.Geometry != null)
                    {
                        //first test to see if the user is drawing over the adorner
                        overWritingSelf =
                            CurrentAdorner.Geometry.StrokeContains(new Pen(Brushes.Black, Globals.AtomRadius * 2),
                                                                   endPoint);
                    }

                    if (targetedVisual != null)
                    {
                        Clashing = targetedVisual is ChemicalVisual && (targetedVisual as AtomVisual)?.ParentAtom != Target
                                   || overWritingSelf;
                    }

                    //do an extra check to see whether we're overwriting existing chemistry
                    if (!Clashing)
                    {
                        for (int i = 1; i < Placements.Count; i++) //ignore the first atom as it will always clash
                        {
                            ChemicalVisual cv = CurrentEditor.GetTargetedVisual(Placements[i]);
                            if (cv != null)
                            {
                                //user is trying to overwrite
                                Clashing = true;
                                break;
                            }
                        }
                    }

                    //check to see if we're overwriting the existing adorner
                    for (int i = 0; i < Placements.Count; i++)
                    {
                        for (int j = 0; j < Placements.Count; j++)
                        {
                            if (i != j && (Placements[j] - Placements[i]).Length < 0.01)
                            {
                                Clashing = true;
                                break;
                            }
                        }
                    }

                    CurrentAdorner = new ChainAdorner(FirstPoint, CurrentEditor, EditViewModel.EditBondThickness, Placements,
                                                       endPoint, Target, Clashing);
                    if (!Clashing)
                    {
                        CurrentStatus = "Click to draw chain";
                    }
                    else
                    {
                        CurrentStatus = "Can't draw over existing atoms";
                    }
                }
            }
        }

        private void CurrentEditor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Atom hitAtom = CurrentEditor.ActiveAtomVisual?.ParentAtom;
            Placements = new List<Point>();
            //save the current targeted visual

            Target = hitAtom;
            FirstPoint = (hitAtom?.Position) ?? e.GetPosition(CurrentEditor);

            if (Target == null)
            {
                Placements.Add(FirstPoint);
            }
            else
            {
                Placements.Add(Target.Position);
            }

            Mouse.Capture(CurrentEditor);
            Keyboard.Focus(CurrentEditor);
            MouseIsDown = true;
        }

        public bool IsDrawing { get; set; }

        public bool MouseIsDown { get; set; }

        public Atom Target { get; set; }

        public Point FirstPoint { get; set; }

        public override void Abort()
        {
            MouseIsDown = false;
            IsDrawing = false;
            CurrentEditor.ReleaseMouseCapture();
            CurrentEditor.Focus();
            CurrentAdorner = null;
            Placements = null;
        }

        public void MarkOutAtoms(Point endPoint, MouseEventArgs e)
        {
            Vector GetNewSprout(Vector lastBondvector, Vector vector)
            {
                double angle = 0d;
                if (Vector.AngleBetween(lastBondvector, vector) > 0)
                {
                    angle = 60;
                }
                else
                {
                    angle = -60;
                }

                Matrix rotator = new Matrix();
                rotator.Rotate(angle);
                Vector newvector = lastBondvector;
                newvector = BasicGeometry.SnapVectorToClock(newvector);
                newvector.Normalize();
                newvector *= EditViewModel.Model.XamlBondLength;
                newvector *= rotator;
                return newvector;
            }

            Vector displacement = endPoint - Placements.Last();
            bool movedABond = displacement.Length > EditViewModel.Model.XamlBondLength;

            if (Target != null) //we hit an atom on mouse-down
            {
                if (movedABond)
                {
                    if (Placements.Count > 1) //we already have two atoms added
                    {
                        var lastBondvector = Placements.Last() - Placements[Placements.Count - 2];
                        var newvector = GetNewSprout(lastBondvector, displacement);
                        Placements.Add(Placements.Last() + newvector);
                    }
                    else //placements.count == 1
                    {
                        if (Target.Singleton)
                        {
                            Snapper snapper = new Snapper(Placements.Last(), EditViewModel);
                            var newBondVector = snapper.SnapVector(0, displacement);
                            Placements.Add(Placements.Last() + newBondVector);
                        }
                        else if (Target.Degree == 1) //it has one bond going into the atom
                        {
                            Vector balancing = Target.BalancingVector();
                            var newvector = GetNewSprout(balancing, displacement);
                            Placements.Add(Placements.Last() + newvector);
                        }
                        else
                        {
                            //Just sprout a balancing vector
                            Vector balancing = Target.BalancingVector();
                            balancing *= EditViewModel.Model.XamlBondLength;
                            Placements.Add(Placements.Last() + balancing);
                        }
                    }
                }
            }
            else //we've just drawn a free chain
            {
                if (Placements.Count == 1)
                {
                    //just got one entry in the list
                    Snapper snapper = new Snapper(FirstPoint, EditViewModel);
                    Point newEnd = snapper.SnapBond(endPoint, e);
                    Placements.Add(newEnd);
                }
                else if (movedABond) //placements must have more than one entry
                {
                    var lastBondvector = Placements.Last() - Placements[Placements.Count - 2];
                    var newvector = GetNewSprout(lastBondvector, displacement);
                    Placements.Add(Placements.Last() + newvector);
                }
            }
        }

        protected override void OnDetaching()
        {
            CurrentAdorner = null;
            CurrentEditor.MouseLeftButtonDown -= CurrentEditor_MouseLeftButtonDown;
            CurrentEditor.MouseMove -= CurrentEditor_MouseMove;
            CurrentEditor.MouseLeftButtonUp -= CurrentEditor_MouseLeftButtonUp;
            CurrentEditor.PreviewKeyDown -= CurrentEditor_PreviewKeyDown;

            if (_parent != null)
            {
                _parent.MouseLeftButtonDown -= CurrentEditor_MouseLeftButtonDown;
            }

            CurrentEditor.Cursor = _lastCursor;
            _parent = null;
        }
    }
}