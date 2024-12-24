// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Drawing.Visuals;
using Chem4Word.ACME.Graphics;
using Chem4Word.ACME.Utils;
using Chem4Word.Model2;
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
        private readonly DrawingVisual HeadHandle = new DrawingVisual(); //these do the resizing
        private readonly DrawingVisual TailHandle = new DrawingVisual();    //these do the resizing
        private DrawingVisual _draggedVisual;
        private bool Resizing;
        private bool Dragging;
        private SolidColorBrush _solidColorBrush;
        private Pen _dashPen;

        private const string UnlockStatus = "[Shift] = unlock length; [Ctrl] = unlock angle; [Esc] = cancel.";
        private const string DefaultStatus = "Drag a handle to resize; drag shaft to reposition.";
        private const string EditReagentsStatus = "Double-click box to edit reagents.";
        private const string EditConditionsStatus = "Double-click box to edit conditions";

        private Point OriginalLocation { get; set; }
        private Point CurrentLocation { get; set; }

        public ReactionSelectionAdorner(EditorCanvas currentEditor, ReactionVisual reactionVisual) : base(currentEditor)
        {
            _solidColorBrush = (SolidColorBrush)FindResource(Common.DrawAdornerBrush);
            _dashPen = new Pen(_solidColorBrush, 1);

            _halfThumbWidth = ThumbWidth / 2;
            AdornedReactionVisual = reactionVisual;
            AdornedReaction = reactionVisual.ParentReaction;

            AttachHandlers();
            EditController.SendStatus(DefaultStatus);
        }

        private ReactionVisual AdornedReactionVisual { get; }

        public Reaction AdornedReaction { get; }

        private Vector HeadDisplacement { get; set; }
        private Vector TailDisplacement { get; set; }

        private void DisableHandlers()
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

            if (e.ClickCount == 2)
            {
                if (AdornedReactionVisual.ConditionsBlockRect.Contains(CurrentLocation))
                {
                    e.Handled = true;
                }
                else if (AdornedReactionVisual.ReagentsBlockRect.Contains(CurrentLocation))
                {
                    e.Handled = true;
                }
            }
        }

        private void ReactionSelectionAdorner_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

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
                //don't drag if we're on the text blocks
                Dragging = !(AdornedReactionVisual.ReagentsBlockRect.Contains(OriginalLocation) | AdornedReactionVisual.ConditionsBlockRect.Contains(OriginalLocation));
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
            if (Resizing || Dragging)
            {
                EditController.MoveReaction(AdornedReaction, AdornedReaction.TailPoint + TailDisplacement, AdornedReaction.HeadPoint + HeadDisplacement);
            }

            Resizing = false;
            Dragging = false;

            ReleaseMouseCapture();
            InvalidateVisual();
            e.Handled = true;
            EditController.SendStatus(DefaultStatus);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            Brush handleFillBrush = (Brush)FindResource(Common.AdornerFillBrush);
            Pen handleBorderPen = (Pen)FindResource(Common.AdornerBorderPen);

            RemoveHandle(HeadHandle);
            RemoveHandle(TailHandle);

            Point newTailPoint = AdornedReaction.TailPoint;
            Point newHeadPoint = AdornedReaction.HeadPoint;

            base.OnRender(drawingContext);
            if (Resizing || Dragging)
            {
                newTailPoint = AdornedReaction.TailPoint + TailDisplacement;
                newHeadPoint = AdornedReaction.HeadPoint + HeadDisplacement;
                Debug.WriteLine($"New Tail Point = {newTailPoint}, New Head Point = {newHeadPoint}");
                var tempReaction = new Reaction()
                {
                    ConditionsText = AdornedReaction.ConditionsText,
                    HeadPoint = newHeadPoint,
                    ReactionType = AdornedReaction.ReactionType,
                    ReagentText = AdornedReaction.ReagentText,
                    TailPoint = newTailPoint
                };
                var rv = new ReactionVisual(tempReaction);
                rv.TextSize = EditController.BlockTextSize;
                rv.ScriptSize = EditController.BlockTextSize * Controller.ScriptScalingFactor;
                rv.RenderFullGeometry(AdornedReaction.ReactionType, AdornedReaction.TailPoint,
                                      AdornedReaction.HeadPoint,
                                      drawingContext, AdornedReaction.ReagentText, AdornedReaction.ConditionsText,
                                      _dashPen,
                                      _solidColorBrush);
            }
            else
            {
                BuildHandle(drawingContext, HeadHandle, newHeadPoint, handleFillBrush, handleBorderPen);
                BuildHandle(drawingContext, TailHandle, newTailPoint, handleFillBrush, handleBorderPen);

                foreach (var reactant in AdornedReaction.Reactants.Values)
                {
                    BuildRoleIndicator(drawingContext, reactant, false);
                }
                foreach (var product in AdornedReaction.Products.Values)
                {
                    BuildRoleIndicator(drawingContext, product, true);
                }
            }

            Arrow arrowVisual = GetArrowShape(newTailPoint, newHeadPoint, AdornedReaction);
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

        private void BuildRoleIndicator(DrawingContext drawingContext, Molecule mol, bool isProduct)
        {
            Brush productBrush = (Brush)FindResource("ProductIndicatorBrush");
            Brush reactantBrush = (Brush)FindResource("ReactantIndicatorBrush");
            Brush arrowBrush = (Brush)FindResource("ArrowIndicatorBrush");
            Brush background = (Brush)FindResource("IndicatorBackgroundBrush");
            //set up metrics
            var bondLength = CurrentEditor.Controller.Model.XamlBondLength;
            var linelength = bondLength;
            var lineThickness = linelength / 10;
            Vector goLeft = new Vector(-linelength, 0);
            Vector goRight = -goLeft;

            //now draw the role circles
            Brush reactantFill = null;
            Pen reactantPen = null;
            Brush productFill = null;
            Pen productPen = null;
            if (isProduct)
            {
                productFill = productBrush;
                reactantPen = new Pen(reactantBrush, lineThickness);
            }
            else
            {
                reactantFill = reactantBrush;
                productPen = new Pen(productBrush, lineThickness);
            }

            double radius = linelength / 4;

            Point reactantCenter = mol.Centre + goLeft;
            Point productCenter = mol.Centre + goRight;

            //draw the background
            Pen backgroundPen = new Pen(background, radius * 2.5) { EndLineCap = PenLineCap.Round, StartLineCap = PenLineCap.Round };
            drawingContext.DrawLine(backgroundPen, reactantCenter, productCenter);

            //draw the indicators
            drawingContext.DrawEllipse(reactantFill, reactantPen, reactantCenter, radius, radius);
            drawingContext.DrawEllipse(productFill, productPen, productCenter, radius, radius);

            //now draw the arrow
            Vector arrowVector = productCenter - reactantCenter;
            arrowVector.Normalize();
            arrowVector *= radius * 1.5;
            Point arrowStart = reactantCenter + arrowVector;
            Point arrowEnd = productCenter - arrowVector;
            Arrow arrowVisual = new StraightArrow { StartPoint = arrowStart, EndPoint = arrowEnd, HeadLength = radius * 1.6, ArrowHeadClosed = false };
            Pen outlinePen = new Pen(arrowBrush, lineThickness);
            arrowVisual.DrawArrowGeometry(drawingContext, outlinePen, null);
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
            Pen handleBorderPen = new Pen { Brush = (Brush)FindResource(Common.AdornerBorderBrush), DashStyle = DashStyles.Dash };
            drawingContext.DrawRectangle((Brush)FindResource(Common.AdornerFillBrush), handleBorderPen, blockBounds);
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
                case ReactionType.Reversible:
                    arrowVisual = new EquilibriumArrow { StartPoint = newStartPoint, EndPoint = newEndPoint };
                    break;

                case ReactionType.ReversibleBiasedForward:
                    arrowVisual = new EquilibriumArrow { StartPoint = newStartPoint, EndPoint = newEndPoint, Bias = Graphics.EquilibriumBias.Forward };
                    break;

                case ReactionType.ReversibleBiasedReverse:
                    arrowVisual = new EquilibriumArrow { StartPoint = newStartPoint, EndPoint = newEndPoint, Bias = Graphics.EquilibriumBias.Backward };
                    break;

                case ReactionType.Blocked:
                    arrowVisual = new BlockedArrow { StartPoint = newStartPoint, EndPoint = newEndPoint };
                    break;

                case ReactionType.Resonance:
                    arrowVisual = new StraightArrow { StartPoint = newStartPoint, EndPoint = newEndPoint, ArrowEnds = Enums.ArrowEnds.Both };
                    break;

                case ReactionType.Retrosynthetic:
                    arrowVisual = new RetrosyntheticArrow { StartPoint = newStartPoint, EndPoint = newEndPoint };
                    break;

                default:
                    arrowVisual = new StraightArrow { StartPoint = newStartPoint, EndPoint = newEndPoint };
                    break;
            }

            return arrowVisual;
        }
    }
}