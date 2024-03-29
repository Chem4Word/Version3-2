﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Utils;
using Chem4Word.Core;
using Chem4Word.Core.UI.Wpf;
using IChem4Word.Contracts;
using System;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Forms;

namespace Chem4Word.ACME.Controls
{
    /// <summary>
    /// Interaction logic for SettingsHost.xaml
    /// </summary>
    public partial class SettingsHost : Window
    {
        private Point _topLeft { get; set; }

        public SettingsHost()
        {
            InitializeComponent();
        }

        public SettingsHost(AcmeOptions options, IChem4WordTelemetry telemetry, Point topLeft) : this()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                UcSettings.AcmeOptions = options;
                UcSettings.Telemetry = telemetry;
                UcSettings.OnButtonClick += UcSettingsOnOnButtonClick;
                _topLeft = topLeft;
            }
        }

        private void UcSettingsOnOnButtonClick(object sender, WpfEventArgs e)
        {
            if (e.Button.Equals("CANCEL"))
            {
                Close();
            }

            if (e.Button.Equals("SAVE"))
            {
                Close();
            }
        }

        private void SettingsHost_OnLoaded(object sender, RoutedEventArgs e)
        {
            var p1 = new Point(_topLeft.X + Width / 2, _topLeft.Y + Height / 2);
            var p2 = UIUtils.GetOnScreenCentrePoint(p1, Width, Height);
            Left = p2.X;
            Top = p2.Y;
            if (UcSettings != null)
            {
                UcSettings.TopLeft = new Point(p2.X + Core.Helpers.Constants.TopLeftOffset, p2.Y + Core.Helpers.Constants.TopLeftOffset);
            }
        }

        private void SettingsHost_OnClosing(object sender, CancelEventArgs e)
        {
            if (UcSettings.AcmeOptions.Dirty)
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
                        UcSettings.AcmeOptions.Save();
                        break;

                    case System.Windows.Forms.DialogResult.No:
                        break;

                    case System.Windows.Forms.DialogResult.Cancel:
                        e.Cancel = true;
                        break;
                }
            }
        }

        private void SettingsHost_OnContentRendered(object sender, EventArgs e)
        {
            if (UcSettings?.AcmeOptions != null)
            {
                UcSettings.AcmeOptions.Dirty = false;
            }
        }
    }
}