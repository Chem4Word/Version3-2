// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Drawing;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Chem4Word.Core;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Core.UI.Wpf;

namespace Chem4Word.UI.WPF
{
    public partial class Chem4WordSettingsHost : Form
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private bool _closedInCode = false;

        public System.Windows.Point TopLeft { get; set; }

        public Chem4WordOptions SystemOptions
        {
            get
            {
                if (elementHost1.Child is SettingsControl sc)
                {
                    return sc.SystemOptions;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (elementHost1.Child is SettingsControl sc)
                {
                    sc.SystemOptions = value;
                }
            }
        }

        public Chem4WordSettingsHost()
        {
            InitializeComponent();
        }

        public Chem4WordSettingsHost(bool runtime)
        {
            using (new WaitCursor())
            {
                InitializeComponent();
                if (runtime)
                {
                    if (elementHost1.Child is SettingsControl sc)
                    {
                        sc.TopLeft = TopLeft;
                        sc.OnButtonClick += OnWpfButtonClick;
                    }
                }
            }
        }

        private void OnWpfButtonClick(object sender, EventArgs e)
        {
            WpfEventArgs args = (WpfEventArgs)e;
            switch (args.Button.ToLower())
            {
                case "ok":
                    DialogResult = DialogResult.OK;
                    if (elementHost1.Child is SettingsControl sc)
                    {
                        SystemOptions = sc.SystemOptions;
                        SystemOptions.Save();
                        sc.Dirty = false;
                        Hide();
                    }
                    break;

                case "cancel":
                    DialogResult = DialogResult.Cancel;
                    _closedInCode = true;
                    Hide();
                    break;
            }
        }

        private void SettingsHost_Load(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                using (new WaitCursor())
                {
                    if (!PointHelper.PointIsEmpty(TopLeft))
                    {
                        Left = (int)TopLeft.X;
                        Top = (int)TopLeft.Y;
                    }

                    MinimumSize = new Size(800, 600);
                }
            }
            catch (Exception ex)
            {
                new ReportError(Globals.Chem4WordV3.Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void SettingsHost_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_closedInCode)
            {
                if (elementHost1.Child is SettingsControl sc && sc.Dirty)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Do you wish to save your changes?");
                    sb.AppendLine("  Click 'Yes' to save your changes and exit.");
                    sb.AppendLine("  Click 'No' to discard your changes and exit.");
                    sb.AppendLine("  Click 'Cancel' to return to the form.");

                    DialogResult dr = UserInteractions.AskUserYesNoCancel(sb.ToString());
                    switch (dr)
                    {
                        case DialogResult.Cancel:
                            e.Cancel = true;
                            break;

                        case DialogResult.Yes:
                            SystemOptions = sc.SystemOptions;
                            SystemOptions.Save();
                            DialogResult = DialogResult.OK;
                            break;

                        case DialogResult.No:
                            DialogResult = DialogResult.Cancel;
                            break;
                    }
                }
            }
        }
    }
}