// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Drawing;
using Chem4Word.ACME.Graphics;
using Chem4Word.ACME.Utils;
using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Chem4Word.ACME.Adorners.Selectors
{
    public class ReactionSelectionAdorner : BaseSelectionAdorner
    {
        private const double ThumbWidth = 22;
        private static double _halfThumbWidth;

        private static Snapper _resizeSnapper;
        protected readonly DrawingVisual HeadHandle = new DrawingVisual(); //these do the resizing
        protected readonly DrawingVisual TailHandle = new DrawingVisual();    //these do the resizing
        private DrawingVisual _draggedVisual;
        protected bool Resizing;
        protected bool Dragging;
        private SolidColorBrush _solidColorBrush;
        private Pen _dashPen;
        private bool MouseIsDown { get; set; }
        private const string UnlockStatus = "[Shift] = unlock length; [Ctrl] = unlock angle; [Esc] = cancel.";
        private const string DefaultStatus = "Drag a handle to resize; drag shaft to reposition.";
        private const string EditReagentsStatus = "Click box to edit reagents.";
        private const string EditConditionsStatus = "Click box to edit conditions";

        public Point OriginalLocation { get; private set; }
        public Point CurrentLocation { get; set; }

        public ReactionSelectionAdorner(EditorCanvas currentEditor, ReactionVisual reactionVisual) : base(currentEditor)
        {
            _solidColorBrush = (SolidColorBrush)FindResource(Globals.DrawAdornerBrush);
            _dashPen = new Pen(_solidColorBrush, 1);

            _halfThumbWidth = ThumbWidth / 2;
            AdornedReactionVisual = reactionVisual;
            AdornedReaction = reactionVisual.ParentReaction;

            AttachHandlers();
            EditController.SendStatus(DefaultStatus);
        }

        public ReactionVisual AdornedReactionVisual { get; }

        public Reaction AdornedReaction { get; }

        public Vector HeadDisplacement { get; private set; }
        public Vector TailDisplacement { get; private set; }

        protected void DisableHandlers()
        {
            //detach the handlers to stop them interfering with dragging
            MouseLeftButtonDown -= BaseSelectionAdorner_MouseLeftButtonDown;
            MouseLeftButtonUp -= BaseSelectionAdorner_MouseLeftButtonUp;
            PreviewMouseLeftButtonDown -= BaseSelectionAdorner_PreviewMouseLeftButtonDown;
            PreviewMouseLeftButtonUp -= BaseSelectionAdorner_PreviewMouseLeftButtonUp;
        }

        protected new void AttachHandlers()
        {
            //detach the handlers to stop them interfering with dragging
            DisableHandlers();

            MouseLeftButtonDown += ReactionSelectionAdorner_MouseLeftButtonDown;
            MouseLeftButtonUp += ReactionSelectionAdorner_MouseLeftButtonUp;
            PreviewMouseMove += ReactionSelectionAdorner_MouseMove;
            PreviewMouseLeftButtonDown += ReactionSelectionAdorner_PreviewMouseLeftButtonDown;
        }

        private void ReactionSelectionAdorner_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            
            OriginalLocation = e.GetPosition(CurrentEditor);
            CurrentLocation = OriginalLocation;
            //Mouse.Capture((ReactionSelectionAdorner)sender);
            if(AdornedReactionVisual.ConditionsBlockRect.Contains(CurrentLocation))
            {
                MessageBox.Show("Clicked Conditions box");
                e.Handled=true;
            }
            else if (AdornedReactionVisual.ReagentsBlockRect.Contains(CurrentLocation))
            {
                MessageBox.Show("Clicked Reagents box");
                e.Handled = true;   
            }
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
            CurrentLocation = e.GetPosition(CurrentEditor);
            if (Resizing || Dragging)
            {
                Keyboard.Focus(this);

                if (Resizing)
                {
                    Mouse.Capture((ReactionSelectionAdorner)sender);
                    if (_draggedVisual == HeadHandle)
                    {
                        CurrentLocation = AdornedReaction.TailPoint + _resizeSnapper.SnapVector(AdornedReaction.Angle, CurrentLocation - AdornedReaction.TailPoint);
                        HeadDisplacement = CurrentLocation - AdornedReaction.HeadPoint;
                    }
                    else if (_draggedVisual == TailHandle)
                    {
                        CurrentLocation = AdornedReaction.HeadPoint + _resizeSnapper.SnapVector(AdornedReaction.Angle, CurrentLocation - AdornedReaction.HeadPoint);
                        TailDisplacement = CurrentLocation - AdornedReaction.TailPoint;
                    }
                    EditController.SendStatus(UnlockStatus);
                }
                else if (Dragging)
                {
                    HeadDisplacement = CurrentLocation - OriginalLocation;
                    TailDisplacement = HeadDisplacement;
                }

                InvalidateVisual();
            }
            else
            {
                if (AdornedReactionVisual.ReagentsBlockRect.Contains(CurrentLocation))
                {
                    EditController.SendStatus(EditReagentsStatus);
                }
                else if (AdornedReactionVisual.ConditionsBlockRect.Contains(CurrentLocation))
                {
                    EditController.SendStatus(EditConditionsStatus);
                }
            }
            e.Handled = true;
        }


        private void ReactionSelectionAdorner_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            MouseIsDown = false;

            EditController.MoveReaction(AdornedReaction, AdornedReaction.TailPoint + TailDisplacement, AdornedReaction.HeadPoint + HeadDisplacement);

            Resizing = false;
            Dragging = false;

            ReleaseMouseCapture();
            InvalidateVisual();
            e.Handled = true;
            EditController.SendStatus(DefaultStatus);
        }

        private List<DrawingVisual> _visualsHit = new List<DrawingVisual>();

        protected override void OnRender(DrawingContext drawingContext)
        {
            Brush handleFillBrush = (Brush)FindResource(Globals.AdornerFillBrush);
            Pen handleBorderPen = (Pen)FindResource(Globals.AdornerBorderPen);

            RemoveHandle(HeadHandle);
            RemoveHandle(TailHandle);

            Point newTailPoint = AdornedReaction.TailPoint;
            Point newHeadPoint = AdornedReaction.HeadPoint;

            Arrow arrowVisual;
            base.OnRender(drawingContext);
            if (Resizing || Dragging)
            {
                newTailPoint = AdornedReaction.TailPoint + TailDisplacement;
                newHeadPoint = AdornedReaction.HeadPoint + HeadDisplacement;
            }

            Debug.WriteLine($"New Tail Point = {newTailPoint}, New Head Point = {newHeadPoint}");
            if (!(Resizing || Dragging))
            {
                BuildHandle(drawingContext, HeadHandle, newHeadPoint, handleFillBrush, handleBorderPen);
                BuildHandle(drawingContext, TailHandle, newTailPoint, handleFillBrush, handleBorderPen);
            }

            arrowVisual = GetArrowShape(newTailPoint, newHeadPoint, AdornedReaction);
            arrowVisual.DrawArrowGeometry(drawingContext, _dashPen, _solidColorBrush);
            arrowVisual.GetOverlayPen(out Brush overlayBrush, out Pen overlayPen);
            arrowVisual.DrawArrowGeometry(drawingContext, overlayPen, overlayBrush);

            if (AdornedReactionVisual.ReagentsBlockRect != Rect.Empty)
            {
                DrawReagentsBlockOutline(drawingContext);
            }
            if (AdornedReactionVisual.ConditionsBlockRect != Rect.Empty)
            {
                DrawConditionsBlockOutline(drawingContext);
            }
        }
        /// <summary>
        /// Draws a dashed highlight around the conditions block
        /// </summary>
        /// <param name="drawingContext"></param>
        private void DrawConditionsBlockOutline(DrawingContext drawingContext)
        {
            DrawBlockRect(drawingContext, AdornedReactionVisual.ConditionsBlockRect);
        }

        /// <summary>
        /// Draws a dashed highlight around a rectangle
        /// </summary>
        /// <param name="drawingContext">Passed in from the calling OnRender method</param>
        /// <param name="blockBounds">Rectangle describing the layout of the block</param>
        private void DrawBlockRect(DrawingContext drawingContext, Rect blockBounds)
        {
            Pen handleBorderPen = new Pen { Brush = (Brush)FindResource(Globals.AdornerBorderBrush), DashStyle = DashStyles.Dash };
            drawingContext.DrawRectangle((Brush)FindResource(Globals.AdornerFillBrush), handleBorderPen, blockBounds);
        }
        /// <summary>
        /// Draws a dashed highlight around the reagents block
        /// </summary>
        /// <param name="drawingContext"></param>
        private void DrawReagentsBlockOutline(DrawingContext drawingContext)
        {
            DrawBlockRect(drawingContext, AdornedReactionVisual.ReagentsBlockRect);
        }
        /// <summary>
        /// builds a grab handle for the tail or head of the arrow
        /// </summary>
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

        public static Arrow GetArrowShape(Point newStartPoint, Point newEndPoint, Reaction adornedReaction)
        {
            Arrow arrowVisual;
            switch (adornedReaction.ReactionType)
            {
                case Globals.ReactionType.Reversible:
                    arrowVisual = new Graphics.EquilibriumArrow { StartPoint = newStartPoint, EndPoint = newEndPoint };
                    break;

                case Globals.ReactionType.ReversibleBiasedForward:
                    arrowVisual = new Graphics.EquilibriumArrow { StartPoint = newStartPoint, EndPoint = newEndPoint, Bias = Graphics.EquilibriumBias.Forward };
                    break;

                case Globals.ReactionType.ReversibleBiasedReverse:
                    arrowVisual = new Graphics.EquilibriumArrow { StartPoint = newStartPoint, EndPoint = newEndPoint, Bias = Graphics.EquilibriumBias.Backward };
                    break;

                case Globals.ReactionType.Blocked:
                    arrowVisual = new Graphics.BlockedArrow { StartPoint = newStartPoint, EndPoint = newEndPoint };
                    break;

                case Globals.ReactionType.Resonance:
                    arrowVisual = new Graphics.StraightArrow { StartPoint = newStartPoint, EndPoint = newEndPoint, ArrowEnds = Enums.ArrowEnds.Both };
                    break;

                default:
                    arrowVisual = new Graphics.StraightArrow { StartPoint = newStartPoint, EndPoint = newEndPoint };
                    break;
            }

            return arrowVisual;
        }
    }
}