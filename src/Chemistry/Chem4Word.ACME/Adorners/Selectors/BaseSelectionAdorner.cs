// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Chem4Word.ACME.Controls;
using Chem4Word.Model2.Annotations;

namespace Chem4Word.ACME.Adorners.Selectors
{
    /// <summary>
    /// All selection adorners are 'immutable':  they are drawn once and subsequently destroyed
    /// rather than being visually modified.  This makes coding much easier.
    /// When changing the visual appearance of a selection, create a new instance of the adorner
    /// supplying the element to the constructor along with the editor canvas
    /// </summary>
    public abstract class BaseSelectionAdorner : Adorner
    {
        protected double AdornerOpacity;

        #region Shared Properties

        //the editor that the Adorner attaches to
        public EditorCanvas CurrentEditor => (EditorCanvas)AdornedElement;

        public EditViewModel EditViewModel => (CurrentEditor.ViewModel as EditViewModel);
        public SolidColorBrush RenderBrush { get; protected set; }

        [NotNull]
        public AdornerLayer AdornerLayer { get; private set; }

        public VisualCollection VisualChildren { get; set; }

        protected override int VisualChildrenCount => VisualChildren.Count;

        #endregion Shared Properties

        #region Constructors

        protected BaseSelectionAdorner(EditorCanvas currentEditor) : base(currentEditor)
        {
            VisualChildren = new VisualCollection(this);
            DefaultSettings();
            AttachHandlers();
            BondToLayer();
            IsHitTestVisible = false;
            AdornerOpacity = 0.25;
        }

        protected override Visual GetVisualChild(int index)
        {
            return VisualChildren[index];
        }

        /// <summary>
        /// Bonds the adorner to the current editor
        /// </summary>
        private void BondToLayer()
        {
            AdornerLayer = AdornerLayer.GetAdornerLayer(CurrentEditor);
            AdornerLayer.Add(this);
        }

        ~BaseSelectionAdorner()
        {
            DetachHandlers();
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Attaches the default handlers to the adorner.
        /// These simply relay events to the CurrentEditor
        /// The CurrentEditor then forwards them to the attached Behavior
        /// </summary>
        protected virtual void AttachHandlers()
        {
            MouseLeftButtonDown += BaseSelectionAdorner_MouseLeftButtonDown;
            PreviewMouseLeftButtonDown += BaseSelectionAdorner_PreviewMouseLeftButtonDown;
            MouseLeftButtonDown += BaseSelectionAdorner_MouseLeftButtonDown;
            PreviewMouseLeftButtonDown += BaseSelectionAdorner_PreviewMouseLeftButtonDown;
            MouseMove += BaseSelectionAdorner_MouseMove;
            PreviewMouseMove += BaseSelectionAdorner_PreviewMouseMove;
            PreviewMouseLeftButtonUp += BaseSelectionAdorner_PreviewMouseLeftButtonUp;
            PreviewMouseRightButtonUp += BaseSelectionAdorner_PreviewMouseRightButtonUp;
            MouseLeftButtonUp += BaseSelectionAdorner_MouseLeftButtonUp;
            PreviewKeyDown += BaseSelectionAdorner_PreviewKeyDown;
            KeyDown += BaseSelectionAdorner_KeyDown;
        }

        protected virtual void BaseSelectionAdorner_KeyDown(object sender, KeyEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        protected void DetachHandlers()
        {
            MouseLeftButtonDown -= BaseSelectionAdorner_MouseLeftButtonDown;
            PreviewMouseLeftButtonDown -= BaseSelectionAdorner_PreviewMouseLeftButtonDown;
            MouseLeftButtonDown -= BaseSelectionAdorner_MouseLeftButtonDown;
            PreviewMouseLeftButtonDown -= BaseSelectionAdorner_PreviewMouseLeftButtonDown;
            MouseMove -= BaseSelectionAdorner_MouseMove;
            PreviewMouseMove -= BaseSelectionAdorner_PreviewMouseMove;
            PreviewMouseLeftButtonUp -= BaseSelectionAdorner_PreviewMouseLeftButtonUp;
            PreviewMouseRightButtonUp -= BaseSelectionAdorner_PreviewMouseRightButtonUp;
            MouseLeftButtonUp -= BaseSelectionAdorner_MouseLeftButtonUp;
            PreviewKeyDown -= BaseSelectionAdorner_PreviewKeyDown;
            KeyDown -= BaseSelectionAdorner_KeyDown;
        }

        protected void DefaultSettings()
        {
            IsHitTestVisible = true;
            RenderBrush = new SolidColorBrush(SystemColors.HighlightColor);
            RenderBrush.Opacity = AdornerOpacity;
        }

        #endregion Methods

        #region Event Handlers

        //override these methods in derived classes to handle specific events
        //The forwarding chain for events is adorner -> CurrentEditor -> attached behaviour
        protected virtual void BaseSelectionAdorner_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        protected virtual void BaseSelectionAdorner_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        protected virtual void BaseSelectionAdorner_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        protected virtual void BaseSelectionAdorner_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        protected virtual void BaseSelectionAdorner_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        protected virtual void BaseSelectionAdorner_MouseMove(object sender, MouseEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        protected virtual void BaseSelectionAdorner_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        protected virtual void BaseSelectionAdorner_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        #endregion Event Handlers
    }
}