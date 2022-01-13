// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Windows;
using System.Windows.Input;
using Chem4Word.ACME.Utils;
using Chem4Word.Model2;

namespace Chem4Word.ACME
{
    /// <summary>
    /// Interaction logic for PTPopup.xaml
    /// </summary>
    public partial class PTPopup : Window
    {
        #region Properties

        public Point CentrePoint { get; set; }

        public ElementBase SelectedElement
        {
            get => (ElementBase)GetValue(SelectedElementProperty);
            set => SetValue(SelectedElementProperty, value);
        }

        public static readonly DependencyProperty SelectedElementProperty =
            DependencyProperty.Register("SelectedElement", typeof(ElementBase), typeof(PTPopup),
                                        new PropertyMetadata(default(ElementBase)));

        #endregion Properties

        public PTPopup()
        {
            InitializeComponent();
        }

        private void PTPicker_ElementSelected(object sender, Controls.VisualPeriodicTable.ElementEventArgs e)
        {
            SelectedElement = e.SelectedElement;
            Close();
        }

        private void PTPickerWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private void PTPickerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // This moves the window off screen while it renders
            var point = UIUtils.GetOffScreenPoint();
            Left = point.X;
            Top = point.Y;
        }

        private void PTPopup_OnContentRendered(object sender, EventArgs e)
        {
            // This moves the window to the correct position
            var point = UIUtils.GetOnScreenPoint(CentrePoint, ActualWidth, ActualHeight);
            Left = point.X;
            Top = point.Y;

            InvalidateArrange();
        }
    }
}