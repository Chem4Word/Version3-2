// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Drawing.Visuals;
using Chem4Word.ACME.Utils;
using Chem4Word.Model2;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using static Chem4Word.ACME.Utils.GraphicsHelpers;
using Geometry = System.Windows.Media.Geometry;

namespace Chem4Word.ACME.Adorners.Selectors
{
    public class SingleObjectSelectionAdorner : MultiObjectAdorner
    {
        //this is the main grab area for the molecule
        protected Thumb BigThumb;

        public List<Molecule> AdornedMolecules => AdornedObjects.OfType<Molecule>().ToList();
        public List<Reaction> AdornedReactions => AdornedObjects.OfType<Reaction>().ToList();

        public List<Annotation> AdornedAnnotations => AdornedObjects.OfType<Annotation>().ToList();

        //tracks the last operation performed
        protected Transform LastOperation;

        //status flag
        protected bool Dragging;

        //tracks the amount of travel during drag operations
        protected double DragXTravel;

        protected double DragYTravel;

        //where the dragging starts
        protected Point StartPos;

        protected bool IsWorking => Dragging;

        private Geometry _ghostMolecule;

        public SingleObjectSelectionAdorner(EditorCanvas currentEditor, Molecule molecule)
            : this(currentEditor, new List<BaseObject> { molecule })
        {
        }

        public SingleObjectSelectionAdorner(EditorCanvas currentEditor, List<Molecule> mols)
            : this(currentEditor, mols.ConvertAll(m => (BaseObject)m))
        {
        }

        public SingleObjectSelectionAdorner(EditorCanvas currentEditor, List<BaseObject> molecules) : base(currentEditor, molecules)
        {
            BuildBigDragArea();

            DisableHandlers();
            Focusable = false;
            IsHitTestVisible = true;

            Focusable = true;
            Keyboard.Focus(this);
        }

        protected void DisableHandlers()
        {
            //detach the handlers to stop them interfering with dragging
            PreviewMouseLeftButtonDown -= BaseSelectionAdorner_PreviewMouseLeftButtonDown;
            MouseLeftButtonDown -= BaseSelectionAdorner_MouseLeftButtonDown;
            PreviewMouseMove -= BaseSelectionAdorner_PreviewMouseMove;
            PreviewMouseLeftButtonUp -= BaseSelectionAdorner_PreviewMouseLeftButtonUp;
            MouseLeftButtonUp -= BaseSelectionAdorner_MouseLeftButtonUp;
        }

        private void BigThumb_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                RaiseEvent(e);
            }
        }

        /// <summary>
        /// Creates the big thumb that allows dragging a molecule around the canvas
        /// </summary>
        private void BuildBigDragArea()
        {
            BigThumb = new Thumb();
            VisualChildren.Add(BigThumb);
            BigThumb.IsHitTestVisible = true;

            BigThumb.Style = (Style)FindResource(Common.ThumbStyle);
            BigThumb.Cursor = Cursors.Hand;
            BigThumb.DragStarted += BigThumb_DragStarted;
            BigThumb.DragCompleted += BigThumb_DragCompleted;
            BigThumb.DragDelta += BigThumb_DragDelta;
            BigThumb.MouseLeftButtonDown += BigThumb_MouseLeftButtonDown;
            BigThumb.Focusable = true;
            Keyboard.Focus(BigThumb);
        }

        /// <summary>
        /// Override this to change the appearance of the main area
        /// </summary>
        /// <param name="drawingContext"></param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            var ghostPen = (Pen)FindResource(Common.AdornerBorderPen);
            var ghostBrush = (Brush)FindResource(Common.AdornerFillBrush);
            if (IsWorking)
            {
                //take a snapshot of the molecule
                if (_ghostMolecule == null)
                {
                    _ghostMolecule = CurrentEditor.GhostMolecules(AdornedMolecules);
                }

                _ghostMolecule.Transform = LastOperation;

                drawingContext.DrawGeometry(ghostBrush, ghostPen, _ghostMolecule);

                foreach (Reaction r in AdornedReactions)
                {
                    var arrow = ReactionSelectionAdorner.GetArrowShape(LastOperation.Transform(r.TailPoint), LastOperation.Transform(r.HeadPoint), r);
                    arrow.DrawArrowGeometry(drawingContext, ghostPen, ghostBrush);
                }
                foreach (Annotation an in AdornedAnnotations)
                {
                    var annotationVisual = CurrentEditor.ChemicalVisuals[an] as AnnotationVisual;
                    if (annotationVisual != null)
                    {
                        var textDrawing = annotationVisual.Drawing;
                        textDrawing.Transform = LastOperation;
                        IterateDrawingGroup(textDrawing, drawingContext, ghostPen, ghostBrush, LastOperation);
                    }
                }
                base.OnRender(drawingContext);
            }
        }

        // Arrange the Adorners.
        protected override Size ArrangeOverride(Size finalSize)
        {
            // desiredWidth and desiredHeight are the width and height of the element that's being adorned.
            // These will be used to place the ResizingAdorner at the corners of the adorned element.
            var bigBoundingBox = CurrentEditor.GetCombinedBoundingBox(AdornedObjects);

            if (LastOperation != null)
            {
                bigBoundingBox = LastOperation.TransformBounds(bigBoundingBox);
            }

            //put a box right around the entire shebang
            BigThumb.Arrange(bigBoundingBox);
            Canvas.SetLeft(BigThumb, bigBoundingBox.Left);
            Canvas.SetTop(BigThumb, bigBoundingBox.Top);
            BigThumb.Height = bigBoundingBox.Height;
            BigThumb.Width = bigBoundingBox.Width;

            // Return the final size.
            return finalSize;
        }

        #region Events

        public event DragCompletedEventHandler DragIsCompleted;

        #endregion Events

        #region MouseIsDown

        private void BigThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            Dragging = true;

            DragXTravel = 0.0d;
            DragYTravel = 0.0d;
            Keyboard.Focus(this);
            StartPos = new Point(Canvas.GetLeft(BigThumb), Canvas.GetTop(BigThumb));
            LastOperation = new TranslateTransform();
        }

        private void BigThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            //update how far it's travelled so far
            double horizontalChange = e.HorizontalChange;
            double verticalChange = e.VerticalChange;

            DragXTravel += horizontalChange;
            DragYTravel += verticalChange;

            double vOffset = DragYTravel;
            double hOffset = DragXTravel;

            var lastTranslation = (TranslateTransform)LastOperation;

            lastTranslation.X = hOffset;
            lastTranslation.Y = vOffset;

            InvalidateVisual();
        }

        /// <summary>
        /// Handles all drag events from all thumbs.
        /// The actual transformation is set in other code
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BigThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (!e.Canceled)
            {
                var lastTranslation = (TranslateTransform)LastOperation;
                lastTranslation.X = DragXTravel;
                lastTranslation.Y = DragYTravel;

                InvalidateVisual();

                //move the molecule
                EditController.TransformObjects(LastOperation, AdornedObjects);

                RaiseDRCompleted(sender, e);

                CurrentEditor.SuppressRedraw = false;

                foreach (Molecule adornedMolecule in AdornedMolecules)
                {
                    adornedMolecule.UpdateVisual();
                }
            }
            else
            {
                EditController.RemoveFromSelection(AdornedObjects);
            }
            Dragging = false;
        }

        #endregion MouseIsDown

        protected void RaiseDRCompleted(object sender, DragCompletedEventArgs dragCompletedEventArgs)
        {
            DragIsCompleted?.Invoke(this, dragCompletedEventArgs);
        }
    }
}