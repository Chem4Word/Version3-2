﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Forms;
using IChem4Word.Contracts;
using System;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace Chem4Word.Searcher.PubChemPlugIn
{
    public partial class PubChemSettings : Form
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public System.Windows.Point TopLeft { get; set; }

        public IChem4WordTelemetry Telemetry { get; set; }

        public string SettingsPath { get; set; }

        public PubChemOptions SearcherOptions { get; set; }

        private bool _dirty;

        public PubChemSettings()
        {
            InitializeComponent();
        }

        private void Settings_Load(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Telemetry.Write(module, "Verbose", "Called");

                if (!PointHelper.PointIsEmpty(TopLeft))
                {
                    Left = (int)TopLeft.X;
                    Top = (int)TopLeft.Y;
                    var screen = Screen.FromControl(this);
                    var sensible = PointHelper.SensibleTopLeft(TopLeft, screen, Width, Height);
                    Left = (int)sensible.X;
                    Top = (int)sensible.Y;
                }
                RestoreControls();
                _dirty = false;
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void btnSetDefaults_Click(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Telemetry.Write(module, "Action", "Triggered");
            try
            {
                DialogResult dr = UserInteractions.AskUserOkCancel("Restore default settings");
                if (dr == DialogResult.OK)
                {
                    SearcherOptions.RestoreDefaults();
                    RestoreControls();
                    _dirty = true;
                }
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Telemetry.Write(module, "Action", "Triggered");
            try
            {
                SearcherOptions.Save();
                _dirty = false;
                DialogResult = DialogResult.OK;
                Hide();
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void RestoreControls()
        {
            txtPubChemWsUri.Text = SearcherOptions.PubChemWebServiceUri;
            txtPubChemRestUri.Text = SearcherOptions.PubChemRestApiUri;
            nudDisplayOrder.Value = SearcherOptions.DisplayOrder;
            nudResultsPerCall.Value = SearcherOptions.ResultsPerCall;
        }

        private void Settings_FormClosing(object sender, FormClosingEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (_dirty)
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
                            SearcherOptions.Save();
                            DialogResult = DialogResult.OK;
                            break;

                        case DialogResult.No:
                            DialogResult = DialogResult.Cancel;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void txtPubChemWsUri_TextChanged(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                SearcherOptions.PubChemWebServiceUri = txtPubChemWsUri.Text;
                _dirty = true;
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void txtPubChemRestUri_TextChanged(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                SearcherOptions.PubChemRestApiUri = txtPubChemRestUri.Text;
                _dirty = true;
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void nudResultsPerCall_ValueChanged(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Telemetry.Write(module, "Action", "Triggered");
            try
            {
                SearcherOptions.ResultsPerCall = (int)nudResultsPerCall.Value;
                _dirty = true;
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void nudDisplayOrder_ValueChanged(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                SearcherOptions.DisplayOrder = (int)nudDisplayOrder.Value;
                _dirty = true;
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }
    }
}