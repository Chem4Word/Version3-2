// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Chem4Word.ACME.Models;
using Chem4Word.ACME.Utils;
using Chem4Word.Core;
using TextBox = System.Windows.Controls.TextBox;

namespace Chem4Word.ACME.Controls
{
    /// <summary>
    ///     Interaction logic for BondPropertyEditor.xaml
    /// </summary>
    public partial class BondPropertyEditor : Window
    {
        private bool _closedByUser;

        private readonly BondPropertiesModel _bondPropertiesModel;

        public BondPropertyEditor()
        {
            InitializeComponent();
        }

        public BondPropertyEditor(BondPropertiesModel model) : this()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                _bondPropertiesModel = model;
                DataContext = _bondPropertiesModel;
                BondPath.Text = _bondPropertiesModel.Path;
            }
        }

        private void BondPropertyEditor_OnLoaded(object sender, RoutedEventArgs e)
        {
            var point = UIUtils.GetOffScreenPoint();
            Left = point.X;
            Top = point.Y;
        }

        private void BondPropertyEditor_OnContentRendered(object sender, EventArgs e)
        {
            // This moves the window to the correct position
            var point = UIUtils.GetOnScreenPoint(_bondPropertiesModel.Centre, ActualWidth, ActualHeight);
            Left = point.X;
            Top = point.Y;

            InvalidateArrange();
            _bondPropertiesModel.ClearFlags();
        }

        private void Cancel_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Save_OnClick(object sender, RoutedEventArgs e)
        {
            _bondPropertiesModel.Save = true;
            _closedByUser = true;
            Close();
        }

        private void BondPropertyEditor_OnClosing(object sender, CancelEventArgs e)
        {
            if (!_closedByUser && _bondPropertiesModel.IsDirty)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Do you wish to save your changes?");
                sb.AppendLine("  Click 'Yes' to save your changes and exit.");
                sb.AppendLine("  Click 'No' to discard your changes and exit.");
                sb.AppendLine("  Click 'Cancel' to return to the form.");

                DialogResult dr = UserInteractions.AskUserYesNoCancel(sb.ToString());
                switch (dr)
                {
                    case System.Windows.Forms.DialogResult.Yes:
                        _bondPropertiesModel.Save = true;
                        break;

                    case System.Windows.Forms.DialogResult.No:
                        _bondPropertiesModel.Save = false;
                        break;

                    case System.Windows.Forms.DialogResult.Cancel:
                        e.Cancel = true;
                        break;
                }
            }
        }

        private void BondAngle_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                _bondPropertiesModel.ValidateBondAngle(textBox.Text);
            }
        }
    }
}