// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Adorners.Sketching;
using Chem4Word.ACME.Controls;
using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

namespace Chem4Word.ACME.Behaviors
{
    public class AnnotationBehavior : BaseEditBehavior
    {
        private Window _window;
        private bool _clashing = false;
        private FloatingSymbolAdorner _currentAdorner;

        public FloatingSymbolAdorner CurrentAdorner
        {
            get => _currentAdorner;
            set
            {
                if (_currentAdorner != null)
                {
                    var layer = AdornerLayer.GetAdornerLayer(CurrentEditor);
                    layer.Remove(_currentAdorner);
                    _currentAdorner.MouseLeftButtonDown -= CurrentAdornerOnMouseLeftButtonDown;
                    _currentAdorner = null;
                }
                _currentAdorner = value;
                if (_currentAdorner != null)
                {
                    _currentAdorner.MouseLeftButtonDown += CurrentAdornerOnMouseLeftButtonDown;
                }
            }
        }

        private void CurrentAdornerOnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!Clashing)
            {
                var pos = CurrentAdorner.TopLeft;
                EditController.AddFloatingSymbol(pos, DefaultText);
            }
        }

        public bool Clashing
        {
            get
            { return _clashing; }
            private set
            {
                _clashing = value;
                if (value)
                {
                    CurrentEditor.Cursor = Cursors.No;
                }
                else
                {
                    CurrentEditor.Cursor = Cursors.Hand;
                }
            }
        }

        public string DefaultText
        {
            get { return (string)GetValue(DefaultTextProperty); }
            set { SetValue(DefaultTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DefaultText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DefaultTextProperty =
            DependencyProperty.Register("DefaultText", typeof(string), typeof(AnnotationBehavior), new PropertyMetadata("+"));

        public string StatusText
        {
            get { return (string)GetValue(StatusTextProperty); }
            set { SetValue(StatusTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for StatusText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StatusTextProperty =
            DependencyProperty.Register("StatusText", typeof(string), typeof(AnnotationBehavior), new PropertyMetadata("Click to add text"));

        public bool IsEditable
        {
            get { return (bool)GetValue(IsEditableProperty); }
            set { SetValue(IsEditableProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsEditable.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsEditableProperty =
            DependencyProperty.Register("IsEditable", typeof(bool), typeof(AnnotationBehavior), new PropertyMetadata(true));

        public AnnotationBehavior() : base()
        {
        }

        public override void Abort()
        {
            throw new NotImplementedException();
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            EditController.ClearSelection();

            CurrentEditor = (EditorCanvas)AssociatedObject;
            _window = Application.Current.MainWindow;

            CurrentEditor.Cursor = Cursors.Hand;

            CurrentEditor.MouseLeftButtonDown += CurrentEditor_MouseLeftButtonDown;
            CurrentEditor.MouseMove += CurrentEditor_MouseMove;
            AssociatedObject.IsHitTestVisible = true;

            if (_window != null)
            {
                _window.MouseLeftButtonDown += CurrentEditor_MouseLeftButtonDown;
            }

            CurrentStatus = StatusText;
        }

        private void CurrentEditor_MouseMove(object sender, MouseEventArgs e)
        {
            var mousePos = e.GetPosition(CurrentEditor);
            CurrentAdorner = new FloatingSymbolAdorner(CurrentEditor, DefaultText, mousePos);
            Point pos = CurrentAdorner.Center;
            Clashing = CurrentEditor.GetTargetedVisual(pos) != null;
        }

        private void CurrentEditor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CurrentAdornerOnMouseLeftButtonDown(sender, e);
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            CurrentAdorner = null;
            CurrentEditor.MouseLeftButtonDown -= CurrentEditor_MouseLeftButtonDown;
            CurrentEditor.MouseMove -= CurrentEditor_MouseMove;
            if (_window != null)
            {
                _window.MouseLeftButtonDown -= CurrentEditor_MouseLeftButtonDown;
            }
            _window = null;
        }
    }
}