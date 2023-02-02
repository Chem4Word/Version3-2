// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using Chem4Word.ACME.Adorners.Sketching;
using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Utils;
using Chem4Word.Model2;

namespace Chem4Word.ACME.Behaviors
{
    public class ReactionBehavior : BaseEditBehavior
    {
        private Window _parent;
        private Cursor _lastCursor;

        public static bool MouseIsDown { get; private set; }
        public static bool IsDrawing { get; private set; }

        private Snapper _angleSnapper;

        public Point LastPos { get; private set; }

        private Adorner _adorner;
        private const double DRAW_TOLERANCE = 20d;

        public Point FirstPoint { get; set; }
        public Point CurrentPoint { get; set; }

        public ReactionBehavior()
        {
        }

        protected override void OnAttached()
        {
            base.OnAttached();

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
                Abort();
            }
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
                RemoveReactionAdorner();
            }

            IsDrawing = false;
            //clear this to prevent a weird bug in drawing
            CurrentEditor.ActiveChemistry = null;
            CurrentEditor.Focus();
        }

        private void RemoveReactionAdorner()
        {
            var layer = AdornerLayer.GetAdornerLayer(CurrentEditor);

            layer.Remove(_adorner);
            _adorner = null;
        }

        private bool Dragging(MouseEventArgs e)
        {
            return e.LeftButton == MouseButtonState.Pressed & IsDrawing;
        }

        private void CurrentEditor_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            CurrentEditor.ReleaseMouseCapture();
            CurrentStatus = "";
            if (IsDrawing)
            {
                var currentPos = e.GetPosition(CurrentEditor);
                if ((currentPos - LastPos).LengthSquared >= DRAW_TOLERANCE)
                {
                    currentPos = _angleSnapper.SnapBond(currentPos);
                    Reaction reaction = new Reaction() { TailPoint = LastPos, HeadPoint = currentPos, ReactionType = EditController.SelectedReactionType.Value };
                    EditController.AddReaction(reaction);
                }
            }
            ClearTemporaries();
        }

        private void CurrentEditor_MouseMove(object sender, MouseEventArgs e)
        {
            var currentPos = e.GetPosition(CurrentEditor);
            var displacement = currentPos - LastPos;
            if (_adorner != null)
            {
                RemoveReactionAdorner();
            }
            if (Dragging(e))
            {
                CurrentStatus = "[Shift] = unlock length; [Ctrl] = unlock angle; [Esc] = cancel.";
                CurrentEditor.Cursor = CursorUtils.Pencil;
                var pt = _angleSnapper.SnapBond(currentPos);
                _adorner = new DrawReactionAdorner(CurrentEditor) { StartPoint = LastPos, EndPoint = pt, ReactionType = EditController.SelectedReactionType.Value };
            }
        }

        private void CurrentEditor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(CurrentEditor);

            IsDrawing = true;

            _angleSnapper = new Snapper(position, EditController, snapLength: false);
            LastPos = position;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            CurrentEditor.MouseLeftButtonDown -= CurrentEditor_MouseLeftButtonDown;
            CurrentEditor.MouseLeftButtonUp -= CurrentEditor_MouseLeftButtonUp;
            CurrentEditor.MouseMove -= CurrentEditor_MouseMove;

            CurrentStatus = "";
            CurrentEditor.Cursor = _lastCursor;
        }
    }
}