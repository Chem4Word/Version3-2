// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Drawing.Visuals;
using Chem4Word.ACME.Utils;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Chem4Word.ACME.Adorners.Selectors
{
    public class MoleculeSelectionAdorner : SingleObjectSelectionAdorner
    {
        //static as they need to be set only when the adorner is first created
        private static double? _thumbWidth;

        private static double _halfThumbWidth;
        private static double _rotateThumbWidth;

        //some things to grab hold of
        protected readonly Thumb TopLeftHandle; //these do the resizing

        protected readonly Thumb TopRightHandle;    //these do the resizing
        protected readonly Thumb BottomLeftHandle;  //these do the resizing
        protected readonly Thumb BottomRightHandle; //these do the resizing

        //the rotator
        protected readonly Thumb RotateHandle; //Grab hold of this to rotate the molecule

        //flags
        protected bool Resizing;

        protected bool Rotating;

        private double _rotateAngle;
        private Point _centroid;
        private Point _rotateThumbPos;
        private Snapper _rotateSnapper;
        private double _yPlacement;
        private double _xPlacement;

        public MoleculeSelectionAdorner(EditorCanvas currentEditor, List<BaseObject> objects)
            : base(currentEditor, objects)
        {
            if (objects is null)
            {
                throw new ArgumentNullException(nameof(objects));
            }

            if (_thumbWidth == null)
            {
                _thumbWidth = 15;
                _halfThumbWidth = _thumbWidth.Value / 2;
                _rotateThumbWidth = _thumbWidth.Value;
            }

            BuildAdornerCorner(ref TopLeftHandle, Cursors.SizeNWSE);
            BuildAdornerCorner(ref TopRightHandle, Cursors.SizeNESW);
            BuildAdornerCorner(ref BottomLeftHandle, Cursors.SizeNESW);
            BuildAdornerCorner(ref BottomRightHandle, Cursors.SizeNWSE);

            BuildRotateThumb(ref RotateHandle);

            SetCentroid();
            SetBoundingBox();
            AttachHandlers();

            IsHitTestVisible = true;
            Focusable = true;
            Focus();
        }

        #region Properties

        public double AspectRatio { get; set; }

        public Rect BoundingBox { get; set; }

        private new bool IsWorking => Dragging || Resizing || Rotating;

        #endregion Properties

        #region Methods

        protected new void AttachHandlers()
        {
            //detach the base class handlers to stop them interfering
            DisableHandlers();

            //wire up the event handling
            TopLeftHandle.DragStarted += ResizeStarted;
            TopRightHandle.DragStarted += ResizeStarted;
            BottomLeftHandle.DragStarted += ResizeStarted;
            BottomRightHandle.DragStarted += ResizeStarted;

            TopLeftHandle.DragDelta += TopLeftHandleDragDelta;
            TopRightHandle.DragDelta += TopRightHandleDragDelta;
            BottomLeftHandle.DragDelta += BottomLeftHandleDragDelta;
            BottomRightHandle.DragDelta += BottomRightHandleDragDelta;

            TopLeftHandle.DragCompleted += HandleResizeCompleted;
            TopRightHandle.DragCompleted += HandleResizeCompleted;
            BottomLeftHandle.DragCompleted += HandleResizeCompleted;
            BottomRightHandle.DragCompleted += HandleResizeCompleted;
        }

        private void ResizeStarted(object sender, DragStartedEventArgs e)
        {
            Resizing = true;
            Dragging = false;
            Keyboard.Focus(this);
            Mouse.Capture((Thumb)sender);
            SetBoundingBox();
            DragXTravel = 0.0d;
            DragYTravel = 0.0d;
        }

        private void BuildRotateThumb(ref Thumb rotateThumb)
        {
            rotateThumb = new Thumb();

            rotateThumb.IsHitTestVisible = true;

            RotateHandle.Width = _rotateThumbWidth;
            RotateHandle.Height = _rotateThumbWidth;
            RotateHandle.Cursor = Cursors.Hand;
            rotateThumb.Style = (Style)FindResource(Common.RotateThumbStyle);
            rotateThumb.DragStarted += RotateStarted;
            rotateThumb.DragDelta += RotateThumb_DragDelta;
            rotateThumb.DragCompleted += HandleResizeCompleted;
            rotateThumb.ToolTip = "Drag this to rotate molecule";

            VisualChildren.Add(rotateThumb);
        }

        private void RotateStarted(object sender, DragStartedEventArgs e)
        {
            Rotating = true;
            if (_rotateAngle == 0.0d)
            {
                //we have not yet rotated anything
                //so take a snapshot of the centroid of the molecule
                SetCentroid();
            }
        }

        private void RotateThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (Rotating)
            {
                Point mouse = Mouse.GetPosition(CurrentEditor);

                var originalDisplacement = mouse - _centroid;

                double snapAngle = Vector.AngleBetween(GeometryTool.ScreenNorth,
                                                       _rotateSnapper.SnapVector(0, originalDisplacement));
                _rotateAngle = snapAngle;
                LastOperation = new RotateTransform(snapAngle, _centroid.X, _centroid.Y);

                InvalidateVisual();
            }
        }

        private void SetCentroid()
        {
            _centroid = GeometryTool.GetCentroid(CurrentEditor.GetCombinedBoundingBox(AdornedObjects));
            _rotateSnapper = new Snapper(_centroid, CurrentEditor.Controller as EditController);
        }

        public event DragCompletedEventHandler ResizeCompleted;

        private void HandleResizeCompleted(object sender, DragCompletedEventArgs dragCompletedEventArgs)
        {
            Resizing = false;

            if (LastOperation != null)
            {
                EditController.TransformObjects(LastOperation, AdornedObjects);

                SetBoundingBox();
                ResizeCompleted?.Invoke(this, dragCompletedEventArgs);
                SetCentroid();
                InvalidateVisual();
            }
            (sender as Thumb)?.ReleaseMouseCapture();
        }

        private void SetBoundingBox()
        {
            BoundingBox = CurrentEditor.GetCombinedBoundingBox(AdornedObjects);
            //and work out the aspect ratio for later resizing
            AspectRatio = BoundingBox.Width / BoundingBox.Height;
        }

        private void BuildAdornerCorner(ref Thumb cornerThumb, Cursor customizedCursor)
        {
            if (cornerThumb != null)
            {
                return;
            }

            cornerThumb = new DragHandle(cursor: customizedCursor);
            cornerThumb.ToolTip = "Drag this to resize";
            SetThumbStyle(cornerThumb);
            VisualChildren.Add(cornerThumb);
        }

        protected virtual void SetThumbStyle(Thumb cornerThumb)
        {
            cornerThumb.Style = (Style)FindResource(Common.GrabHandleStyle);
        }

        #endregion Methods

        #region Overrides

        // Arrange the Adorners.
        protected override Size ArrangeOverride(Size finalSize)
        {
            // desiredWidth and desiredHeight are the width and height of the element that's being adorned.
            // These will be used to place the ResizingAdorner at the corners of the adorned element.
            var boundingBox = CurrentEditor.GetCombinedBoundingBox(AdornedObjects);

            if (LastOperation != null)
            {
                boundingBox = LastOperation.TransformBounds(boundingBox);
            }

            TopLeftHandle.Arrange(new Rect(boundingBox.Left - _halfThumbWidth, boundingBox.Top - _halfThumbWidth, _thumbWidth.Value,
                                           _thumbWidth.Value));
            TopRightHandle.Arrange(new Rect(boundingBox.Left + boundingBox.Width - _halfThumbWidth, boundingBox.Top - _halfThumbWidth,
                                            _thumbWidth.Value,
                                            _thumbWidth.Value));
            BottomLeftHandle.Arrange(new Rect(boundingBox.Left - _halfThumbWidth, boundingBox.Top + boundingBox.Height - _halfThumbWidth,
                                              _thumbWidth.Value, _thumbWidth.Value));
            BottomRightHandle.Arrange(new Rect(boundingBox.Left + boundingBox.Width - _halfThumbWidth,
                                               boundingBox.Height + boundingBox.Top - _halfThumbWidth, _thumbWidth.Value,
                                               _thumbWidth.Value));

            //add the rotator
            _xPlacement = (boundingBox.Left + boundingBox.Right) / 2;
            _yPlacement = boundingBox.Top - RotateHandle.Height * 3;
            _rotateThumbPos = new Point(_xPlacement, _yPlacement);

            if (BigThumb.IsDragging
                || TopLeftHandle.IsDragging
                || TopRightHandle.IsDragging
                || BottomLeftHandle.IsDragging
                || BottomRightHandle.IsDragging)
            {
                RotateHandle.Visibility = Visibility.Hidden;
            }
            else
            {
                RotateHandle.Visibility = Visibility.Visible;
                SetCentroid();
            }

            if (Rotating && LastOperation != null)
            {
                _rotateThumbPos = LastOperation.Transform(_rotateThumbPos);
            }

            Vector rotateThumbTweak = new Vector(-RotateHandle.Width / 2, -RotateHandle.Height / 2);
            Point newLoc = _rotateThumbPos + rotateThumbTweak;

            if (Rotating && LastOperation != null)
            {
                TopLeftHandle.Visibility = Visibility.Collapsed;
                TopRightHandle.Visibility = Visibility.Collapsed;
                BottomLeftHandle.Visibility = Visibility.Collapsed;
                BottomRightHandle.Visibility = Visibility.Collapsed;
                BigThumb.Visibility = Visibility.Collapsed;
            }
            else
            {
                TopLeftHandle.Visibility = Visibility.Visible;
                TopRightHandle.Visibility = Visibility.Visible;
                BottomLeftHandle.Visibility = Visibility.Visible;
                BottomRightHandle.Visibility = Visibility.Visible;
                BigThumb.Visibility = Visibility.Visible;
            }

            RotateHandle.Arrange(new Rect(newLoc.X, newLoc.Y, RotateHandle.Width, RotateHandle.Height));

            base.ArrangeOverride(finalSize);
            return finalSize;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            var ghostBrush = (Brush)FindResource(Common.AdornerBorderBrush);
            var ghostPen = new Pen(ghostBrush, 1.0);
            if (!(BigThumb.IsDragging || TopLeftHandle.IsDragging || TopRightHandle.IsDragging
                  || BottomLeftHandle.IsDragging || BottomRightHandle.IsDragging))
            {
                drawingContext.DrawLine(ghostPen, _centroid, _rotateThumbPos);
                drawingContext.DrawEllipse(ghostBrush, ghostPen, _centroid, 2, 2);
            }

            if (IsWorking)
            {
                //identify which Molecule the atom belongs to
                //take a snapshot of the molecule
                var ghost = CurrentEditor.GhostMolecules(AdornedMolecules);
                ghost.Transform = LastOperation;
                drawingContext.DrawGeometry(ghostBrush, ghostPen, ghost);
                foreach (Reaction r in AdornedReactions)
                {
                    var newEndPoint = LastOperation.Transform(r.HeadPoint);
                    var newStartPoint = LastOperation.Transform(r.TailPoint);

                    //create temporary Reactions and visuals and throw them away afterwards
                    var tempReaction = new Reaction()
                    {
                        ConditionsText = r.ConditionsText,
                        HeadPoint = newEndPoint,
                        ReactionType = r.ReactionType,
                        ReagentText = r.ReagentText,
                        TailPoint = newStartPoint
                    };

                    var rv = new ReactionVisual(tempReaction)
                    {
                        TextSize = EditController.BlockTextSize,
                        ScriptSize = EditController.BlockTextSize * Controller.ScriptScalingFactor
                    };
                    rv.RenderFullGeometry(r.ReactionType, newStartPoint,
                                          newEndPoint,
                                          drawingContext, r.ReagentText, r.ConditionsText, ghostPen,
                                          ghostBrush);

                    var arrow = ReactionSelectionAdorner.GetArrowShape(newStartPoint,
                                                                       newEndPoint, r);
                    arrow.DrawArrowGeometry(drawingContext, ghostPen, ghostBrush);
                }
            }
        }

        #endregion Overrides

        #region Resizing

        // Handler for resizing from the bottom-right.
        private void IncrementDragging(DragDeltaEventArgs args)
        {
            var argsHorizontalChange = args.HorizontalChange;
            var argsVerticalChange = args.VerticalChange;

            if (double.IsNaN(argsHorizontalChange))
            {
                argsHorizontalChange = 0d;
            }

            if (double.IsNaN(argsVerticalChange))
            {
                argsVerticalChange = 0d;
            }

            DragXTravel += argsHorizontalChange;
            DragYTravel += argsVerticalChange;
        }

        private double GetScaleFactor(double left, double top, double right, double bottom)
        {
            double scaleFactor;
            var newAspectRatio = Math.Abs(right - left) / Math.Abs(bottom - top);
            if (newAspectRatio > AspectRatio) //it's wider now than it is deep
            {
                scaleFactor = Math.Abs(top - bottom) / BoundingBox.Height;
            }
            else //it's deeper than it's wide
            {
                scaleFactor = Math.Abs(right - left) / BoundingBox.Width;
            }

            return scaleFactor;
        }

        // Handler for resizing from the top-right.
        private void TopRightHandleDragDelta(object sender, DragDeltaEventArgs args)
        {
            IncrementDragging(args);

            if (NotDraggingBackwards())
            {
                var scaleFactor = GetScaleFactor(BoundingBox.Left,
                                                 BoundingBox.Top + DragYTravel,
                                                 BoundingBox.Right + DragXTravel,
                                                 BoundingBox.Bottom);

                LastOperation = new ScaleTransform(scaleFactor, scaleFactor, BoundingBox.Left, BoundingBox.Bottom);

                InvalidateVisual();
            }
        }

        // Handler for resizing from the top-left.
        private void TopLeftHandleDragDelta(object sender, DragDeltaEventArgs args)
        {
            IncrementDragging(args);
            if (NotDraggingBackwards())
            {
                var scaleFactor = GetScaleFactor(
                    BoundingBox.Left + DragXTravel,
                    BoundingBox.Top + DragYTravel,
                    BoundingBox.Right,
                    BoundingBox.Bottom);

                LastOperation = new ScaleTransform(scaleFactor, scaleFactor, BoundingBox.Right, BoundingBox.Bottom);

                InvalidateVisual();
            }
        }

        // Handler for resizing from the bottom-left.
        private void BottomLeftHandleDragDelta(object sender, DragDeltaEventArgs args)
        {
            IncrementDragging(args);
            if (NotDraggingBackwards())
            {
                var scaleFactor = GetScaleFactor(BoundingBox.Left + DragXTravel,
                                                 BoundingBox.Top,
                                                 BoundingBox.Right,
                                                 BoundingBox.Bottom + DragYTravel);

                LastOperation = new ScaleTransform(scaleFactor, scaleFactor, BoundingBox.Right, BoundingBox.Top);

                InvalidateVisual();
            }
        }

        // Handler for resizing from the bottom-right.
        private void BottomRightHandleDragDelta(object sender, DragDeltaEventArgs args)
        {
            IncrementDragging(args);
            if (NotDraggingBackwards())
            {
                var scaleFactor = GetScaleFactor(BoundingBox.Left,
                                                 BoundingBox.Top,
                                                 BoundingBox.Right + DragXTravel,
                                                 BoundingBox.Bottom + DragYTravel);

                LastOperation = new ScaleTransform(scaleFactor, scaleFactor, BoundingBox.Left, BoundingBox.Top);

                InvalidateVisual();
            }
        }

        private bool NotDraggingBackwards()
        {
            return BigThumb.Height >= 10 && BigThumb.Width >= 10;
        }

        #endregion Resizing
    }
}