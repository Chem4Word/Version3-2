// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Graphics;
using Chem4Word.ACME.Utils;
using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Chem4Word.ACME.Adorners.Selectors
{
    public class ReactionSelectionAdorner : BaseSelectionAdorner
    {
        private const double THUMB_WIDTH = 22;
        private static double _halfThumbWidth;

        private static Snapper _resizeSnapper;
        protected readonly DrawingVisual HeadHandle = new DrawingVisual(); //these do the resizing
        protected readonly DrawingVisual TailHandle = new DrawingVisual();    //these do the resizing
        private DrawingVisual _draggedVisual;
        protected bool Resizing;
        protected bool Dragging;
        private SolidColorBrush _solidColorBrush;
        private Pen _dashPen;
        private bool MouseDown { get; set; }
        private Point MouseLoc { get; set; }
        private const string UNLOCK_STATUS = "[Shift] = unlock length; [Ctrl] = unlock angle; [Esc] = cancel.";
        private const string DEFAULT_STATUS = "Drag a handle to resize; drag shaft to reposition.";
        public Point OriginalLocation { get; private set; }
        public Point CurrentLocation { get; set; }

        public event DragCompletedEventHandler DragCompleted;

        public ReactionSelectionAdorner(EditorCanvas currentEditor, Reaction reaction) : base(currentEditor)
        {
            _solidColorBrush = (SolidColorBrush)FindResource(Globals.DrawAdornerBrush);
            _dashPen = new Pen(_solidColorBrush, 1);

            _halfThumbWidth = THUMB_WIDTH / 2;

            AdornedReaction = reaction;

            AttachHandlers();
            EditController.SendStatus(DEFAULT_STATUS);
        }

        public Reaction AdornedReaction { get; }

        public Vector HeadDisplacement { get; private set; }
        public Vector TailDisplacement { get; private set; }

        protected void SetThumbStyle(Thumb cornerThumb)
        {
            cornerThumb.Style = (Style)FindResource(Globals.GrabHandleStyle);
        }

        private void PositionHandle(Thumb handle, Point endPoint)
        {
            handle.Arrange(new Rect(endPoint.X - _halfThumbWidth, endPoint.Y - _halfThumbWidth, THUMB_WIDTH,
                                           THUMB_WIDTH));
        }

        protected void DisableHandlers()
        {
            //detach the handlers to stop them interfering with dragging
            PreviewMouseLeftButtonDown -= BaseSelectionAdorner_PreviewMouseLeftButtonDown;
            MouseLeftButtonDown -= BaseSelectionAdorner_MouseLeftButtonDown;
            //MouseMove -= BaseSelectionAdorner_PreviewMouseMove;
            PreviewMouseLeftButtonUp -= BaseSelectionAdorner_PreviewMouseLeftButtonUp;
            MouseLeftButtonUp -= BaseSelectionAdorner_MouseLeftButtonUp;
        }

        protected new void AttachHandlers()
        {
            //detach the handlers to stop them interfering with dragging

            DisableHandlers();

            MouseLeftButtonDown += ReactionSelectionAdorner_MouseLeftButtonDown;
            PreviewMouseMove += ReactionSelectionAdorner_MouseMove;

            MouseLeftButtonUp += ReactionSelectionAdorner_MouseLeftButtonUp;
        }

        private void ReactionSelectionAdorner_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            _visualsHit.Clear();

            OriginalLocation = e.GetPosition(CurrentEditor);
            CurrentLocation = OriginalLocation;
            Mouse.Capture((ReactionSelectionAdorner)sender);

            DrawingVisual dv = null;

            //get rid of the original handles
            RemoveHandle(HeadHandle);
            RemoveHandle(TailHandle);

            //VisualTreeHelper.HitTest(CurrentEditor, null, HitTestCallBack, new PointHitTestParameters(OriginalLocation));

            if ((CurrentLocation - AdornedReaction.HeadPoint).LengthSquared <= _halfThumbWidth * _halfThumbWidth)
            {
                dv = HeadHandle;
            }
            else if ((OriginalLocation - AdornedReaction.TailPoint).LengthSquared <= _halfThumbWidth * _halfThumbWidth)
            {
                dv = TailHandle;
            }

            if (dv == HeadHandle)
            {
                TailDisplacement = new Vector(0d, 0d);
                _draggedVisual = HeadHandle;
                Resizing = true;
                Dragging = false;
                _resizeSnapper = new Snapper(AdornedReaction.HeadPoint, EditController, 15);
            }
            else if (dv == TailHandle)
            {
                HeadDisplacement = new Vector(0d, 0d);
                _draggedVisual = TailHandle;
                Resizing = true;
                Dragging = false;
                _resizeSnapper = new Snapper(AdornedReaction.TailPoint, EditController, 15);
            }
            else
            {
                Dragging = true;
                Resizing = false;
            }
            e.Handled = true;
        }

        private void ReactionSelectionAdorner_MouseMove(object sender, MouseEventArgs e)
        {
            Keyboard.Focus(this);
            CurrentLocation = e.GetPosition(CurrentEditor);
            if (Resizing)
            {
                Mouse.Capture((ReactionSelectionAdorner)sender);
                //MainDisplacement = e.GetPosition(CurrentEditor) - OriginalLocation;
                if (_draggedVisual == HeadHandle)
                {
                    CurrentLocation = _resizeSnapper.SnapBond(CurrentLocation, 90);
                    HeadDisplacement = CurrentLocation - AdornedReaction.HeadPoint;
                }
                else if (_draggedVisual == TailHandle)
                {
                    CurrentLocation = _resizeSnapper.SnapBond(CurrentLocation, 90);
                    TailDisplacement = CurrentLocation - AdornedReaction.TailPoint;
                }
                EditController.SendStatus(UNLOCK_STATUS);
            }
            else if (Dragging)
            {
                HeadDisplacement = CurrentLocation - OriginalLocation;
                TailDisplacement = HeadDisplacement;
            }
            e.Handled = true;
            InvalidateVisual();
        }

        private void ReactionSelectionAdorner_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            MouseDown = false;

            EditController.MoveReaction(AdornedReaction, AdornedReaction.TailPoint + TailDisplacement, AdornedReaction.HeadPoint + HeadDisplacement);

            Resizing = false;
            Dragging = false;

            //
            this.ReleaseMouseCapture();
            InvalidateVisual();
            e.Handled = true;
            EditController.SendStatus(DEFAULT_STATUS);
        }

        private List<DrawingVisual> _visualsHit = new List<DrawingVisual>();

        protected override void OnRender(DrawingContext drawingContext)
        {
            Brush handleFillBrush = (Brush)FindResource(Globals.AdornerFillBrush);
            Pen handleBorderPen = (Pen)FindResource(Globals.AdornerBorderPen);

            RemoveHandle(HeadHandle);
            RemoveHandle(TailHandle);

            Point newStartPoint = AdornedReaction.TailPoint;
            Point newEndPoint = AdornedReaction.HeadPoint;

            Graphics.Arrow arrowVisual;
            base.OnRender(drawingContext);
            if (Resizing)
            {
                newStartPoint = AdornedReaction.TailPoint + TailDisplacement;
                newEndPoint = AdornedReaction.HeadPoint + HeadDisplacement;
            }
            else if (Dragging)
            {
                newStartPoint = AdornedReaction.TailPoint + TailDisplacement;
                newEndPoint = AdornedReaction.HeadPoint + HeadDisplacement;
            }

            Debug.WriteLine($"New Start Point = {newStartPoint}, New End Point = {newEndPoint}");
            if (!(Resizing || Dragging))
            {
                BuildHandle(drawingContext, HeadHandle, newEndPoint, handleFillBrush, handleBorderPen);
                BuildHandle(drawingContext, TailHandle, newStartPoint, handleFillBrush, handleBorderPen);
            }

            arrowVisual = GetArrowShape(newStartPoint, newEndPoint);
            arrowVisual.DrawArrowGeometry(drawingContext, _dashPen, _solidColorBrush);
            arrowVisual.GetOverlayPen(out Brush overlayBrush, out Pen overlayPen);
            arrowVisual.DrawArrowGeometry(drawingContext, overlayPen, overlayBrush);
        }

        private void BuildHandle(DrawingContext drawingContext, DrawingVisual handle, Point centre, Brush handleFillBrush, Pen handleBorderPen)
        {
            drawingContext.DrawEllipse(handleFillBrush, handleBorderPen, centre, _halfThumbWidth, _halfThumbWidth);
            AddVisualChild(handle);
            AddLogicalChild(handle);
        }

        private void RemoveHandle(DrawingVisual handle)
        {
            if (VisualChildren.Contains(handle))
            {
                RemoveVisualChild(handle);
                RemoveLogicalChild(handle);
            }
        }

        private Arrow GetArrowShape(Point newStartPoint, Point newEndPoint)
        {
            Arrow arrowVisual;
            switch (AdornedReaction.ReactionType)
            {
                case Globals.ReactionType.Equilibrium:
                    arrowVisual = new Graphics.EquilibriumArrow { StartPoint = newStartPoint, EndPoint = newEndPoint };
                    break;

                case Globals.ReactionType.EquilibriumBiasedForward:
                    arrowVisual = new Graphics.EquilibriumArrow { StartPoint = newStartPoint, EndPoint = newEndPoint, Bias = Graphics.EquilibriumBias.Forward };
                    break;

                case Globals.ReactionType.EquilibriumBiasedReverse:
                    arrowVisual = new Graphics.EquilibriumArrow { StartPoint = newStartPoint, EndPoint = newEndPoint, Bias = Graphics.EquilibriumBias.Backward };
                    break;

                case Globals.ReactionType.Blocked:
                    arrowVisual = new Graphics.BlockedArrow { StartPoint = newStartPoint, EndPoint = newEndPoint };
                    break;

                default:
                    arrowVisual = new Graphics.StraightArrow { StartPoint = newStartPoint, EndPoint = newEndPoint };
                    break;
            }

            return arrowVisual;
        }
    }
}