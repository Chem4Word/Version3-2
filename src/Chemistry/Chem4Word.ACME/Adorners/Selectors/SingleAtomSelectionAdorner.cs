// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Chem4Word.ACME.Controls;
using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.ACME.Adorners.Selectors
{
    public class SingleAtomSelectionAdorner : MultiChemistryAdorner
    {
        //this is the main grab area for the molecule
        protected Thumb BigThumb;

        public List<Molecule> AdornedMolecules => AdornedChemistries.Select(c => (Molecule)c).ToList();

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

        public SingleAtomSelectionAdorner(EditorCanvas currentEditor, Molecule molecule)
            : this(currentEditor, new List<ChemistryBase> { molecule })
        {
        }

        public SingleAtomSelectionAdorner(EditorCanvas currentEditor, List<Molecule> mols)
            : this(currentEditor, mols.ConvertAll(m => (ChemistryBase)m))
        {
        }

        public SingleAtomSelectionAdorner(EditorCanvas currentEditor, List<ChemistryBase> molecules) : base(currentEditor, molecules)
        {
            BuildBigDragArea();

            AttachHandler();
            Focusable = false;
            IsHitTestVisible = true;

            Focusable = true;
            Keyboard.Focus(this);
        }

        protected void AttachHandler()
        {
            //detach the handlers to stop them interfering with dragging
            PreviewMouseLeftButtonDown -= BaseSelectionAdorner_PreviewMouseLeftButtonDown;
            MouseLeftButtonDown -= BaseSelectionAdorner_MouseLeftButtonDown;
            PreviewMouseMove -= BaseSelectionAdorner_PreviewMouseMove;
        }

        private void BigThumb_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                RaiseEvent(e);
            }
        }

        protected virtual void AbortDragging()
        {
            Dragging = false;

            LastOperation = null;
            InvalidateVisual();
        }

        /// <summary>
        /// Creates the big thumb that allows dragging a molecule around the canvas
        /// </summary>
        private void BuildBigDragArea()
        {
            BigThumb = new Thumb();
            VisualChildren.Add(BigThumb);
            BigThumb.IsHitTestVisible = true;

            BigThumb.Style = (Style)FindResource(Globals.ThumbStyle);
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
            var borderPen = (Pen)FindResource(Globals.AdornerBorderPen);
            var renderBrush = (Brush)FindResource(Globals.AdornerFillBrush);
            if (IsWorking)
            {
                object elem = AdornedElement;
                //identify which Molecule the atom belongs to

                //take a snapshot of the molecule
                if (_ghostMolecule == null)
                {
                    _ghostMolecule = CurrentEditor.GhostMolecule(AdornedMolecules);
                }

                _ghostMolecule.Transform = LastOperation;

                drawingContext.DrawGeometry(renderBrush, borderPen, _ghostMolecule);

                base.OnRender(drawingContext);
            }
        }

        // Arrange the Adorners.
        protected override Size ArrangeOverride(Size finalSize)
        {
            // desiredWidth and desiredHeight are the width and height of the element that's being adorned.
            // These will be used to place the ResizingAdorner at the corners of the adorned element.
            var bbb = CurrentEditor.GetMoleculeBoundingBox(AdornedMolecules);

            if (LastOperation != null)
            {
                bbb = LastOperation.TransformBounds(bbb);
            }

            //put a box right around the entire shebang
            BigThumb.Arrange(bbb);
            Canvas.SetLeft(BigThumb, bbb.Left);
            Canvas.SetTop(BigThumb, bbb.Top);
            BigThumb.Height = bbb.Height;
            BigThumb.Width = bbb.Width;

            // Return the final size.
            return finalSize;
        }

        #region Events

        public event DragCompletedEventHandler DragCompleted;

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
            DragXTravel += e.HorizontalChange;
            DragYTravel += e.VerticalChange;

            var lastTranslation = (TranslateTransform)LastOperation;

            lastTranslation.X = DragXTravel;
            lastTranslation.Y = DragYTravel;

            Canvas.SetLeft(BigThumb, StartPos.X + DragXTravel);
            Canvas.SetTop(BigThumb, StartPos.Y + DragYTravel);

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
                EditViewModel.DoTransform(LastOperation, AdornedMolecules);

                RaiseDRCompleted(sender, e);

                CurrentEditor.SuppressRedraw = false;

                foreach (Molecule adornedMolecule in AdornedMolecules)
                {
                    adornedMolecule.UpdateVisual();
                }
            }
            else
            {
                EditViewModel.RemoveFromSelection(AdornedMolecules.ConvertAll(am => (ChemistryBase)am));
            }
            Dragging = false;
        }

        #endregion MouseIsDown

        protected void RaiseDRCompleted(object sender, DragCompletedEventArgs dragCompletedEventArgs)
        {
            DragCompleted?.Invoke(this, dragCompletedEventArgs);
        }
    }
}