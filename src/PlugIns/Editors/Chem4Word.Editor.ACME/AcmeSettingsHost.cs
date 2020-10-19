// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using Chem4Word.ACME;
using Chem4Word.ACME.Controls;
using Chem4Word.Core;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Wpf;
using IChem4Word.Contracts;

namespace Chem4Word.Editor.ACME
{
    public partial class AcmeSettingsHost : Form
    {
        public Point TopLeft { get; set; }

        public IChem4WordTelemetry Telemetry { get; set; }

        public AcmeOptions EditorOptions { get; set; }

        private AcmeSettings _acmeSettings;

        public AcmeSettingsHost()
        {
            InitializeComponent();
        }

        private void Settings_Load(object sender, EventArgs e)
        {
            if (!PointHelper.PointIsEmpty(TopLeft))
            {
                Left = (int)TopLeft.X;
                Top = (int)TopLeft.Y;
            }

            _acmeSettings = new AcmeSettings();
            _acmeSettings.AcmeOptions = EditorOptions;
            _acmeSettings.Telemetry = Telemetry;
            _acmeSettings.TopLeft = TopLeft;
            _acmeSettings.OnButtonClick += UsOnOnButtonClick;
            elementHost1.Child = _acmeSettings;

            MinimumSize = Size;
        }

        private void UsOnOnButtonClick(object sender, WpfEventArgs e)
        {
            if (e.Button.Equals("CANCEL"))
            {
                Close();
            }

            if (e.Button.Equals("SAVE"))
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void Settings_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (elementHost1.Child is AcmeSettings us
                && us.AcmeOptions.Dirty)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Do you wish to save your changes?");
                sb.AppendLine("  Click 'Yes' to save your changes and exit.");
                sb.AppendLine("  Click 'No' to discard your changes and exit.");
                sb.AppendLine("  Click 'Cancel' to return to the form.");

                DialogResult dr = UserInteractions.AskUserYesNoCancel(sb.ToString());
                switch (dr)
                {
                    case DialogResult.Yes:
                        us.AcmeOptions.Save();
                        break;

                    case DialogResult.No:
                        break;

                    case DialogResult.Cancel:
                        e.Cancel = true;
                        break;
                }
            }
        }
    }
}