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

namespace Chem4Word.Renderer.OoXmlV4
{
    public partial class OoXmlV4Settings : Form
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public System.Windows.Point TopLeft { get; set; }

        public IChem4WordTelemetry Telemetry { get; set; }

        public string SettingsPath { get; set; }

        public OoXmlV4Options RendererOptions { get; set; }

        private bool _dirty;

        public OoXmlV4Settings()
        {
            InitializeComponent();
        }

        private void Settings_Load(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
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

#if DEBUG
#else
                tabControlEx.TabPages.Remove(tabDebug);
#endif
                _dirty = false;
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void Ok_Click(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                _dirty = false;
                RendererOptions.Save();
                DialogResult = DialogResult.OK;
                Hide();
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void SetDefaults_Click(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                var dr = UserInteractions.AskUserOkCancel("Restore default settings");
                if (dr == DialogResult.OK)
                {
                    RendererOptions.RestoreDefaults();
                    RestoreControls();
                    _dirty = true;
                }
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void RestoreControls()
        {
            ColouredAtoms.Checked = RendererOptions.ColouredAtoms;
            ShowHydrogens.Checked = RendererOptions.ShowHydrogens;
            ShowAllCarbonAtoms.Checked = RendererOptions.ShowCarbons;
            ClipCrossingBonds.Checked = RendererOptions.ClipCrossingBonds;
            ShowMoleculeCaptions.Checked = RendererOptions.ShowMoleculeCaptions;
            ShowMoleculeGrouping.Checked = RendererOptions.ShowMoleculeGrouping;

            // Debugging Options
            ClipBondLines.Checked = RendererOptions.ClipBondLines;
            ShowCharacterBox.Checked = RendererOptions.ShowCharacterBoundingBoxes;
            ShowMoleculeBox.Checked = RendererOptions.ShowMoleculeBoundingBoxes;
            ShowRingCentres.Checked = RendererOptions.ShowRingCentres;
            ShowAtomPositions.Checked = RendererOptions.ShowAtomPositions;
            ShowConvexHulls.Checked = RendererOptions.ShowHulls;
            ShowDoubleBondTrimmingLines.Checked = RendererOptions.ShowDoubleBondTrimmingLines;
            ShowBondDirection.Checked = RendererOptions.ShowBondDirection;
            ShowCharacterGroupsBox.Checked = RendererOptions.ShowCharacterGroupBoundingBoxes;
            ShowBondCrossingPoints.Checked = RendererOptions.ShowBondCrossingPoints;
        }

        private void Settings_FormClosing(object sender, FormClosingEventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                // Nothing to do here ...
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
            if (_dirty)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Do you wish to save your changes?");
                sb.AppendLine("  Click 'Yes' to save your changes and exit.");
                sb.AppendLine("  Click 'No' to discard your changes and exit.");
                sb.AppendLine("  Click 'Cancel' to return to the form.");
                var dr = UserInteractions.AskUserYesNoCancel(sb.ToString());
                switch (dr)
                {
                    case DialogResult.Cancel:
                        e.Cancel = true;
                        break;

                    case DialogResult.Yes:
                        RendererOptions.Save();
                        DialogResult = DialogResult.OK;
                        break;

                    case DialogResult.No:
                        DialogResult = DialogResult.Cancel;
                        break;
                }
            }
        }

        private void ClipLines_CheckedChanged(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                RendererOptions.ClipBondLines = ClipBondLines.Checked;
                _dirty = true;
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void ShowHydrogens_CheckedChanged(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                RendererOptions.ShowHydrogens = ShowHydrogens.Checked;
                _dirty = true;
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void ColouredAtoms_CheckedChanged(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                RendererOptions.ColouredAtoms = ColouredAtoms.Checked;
                _dirty = true;
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void ShowRingCentres_CheckedChanged(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                RendererOptions.ShowRingCentres = ShowRingCentres.Checked;
                _dirty = true;
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void ShowMoleculeBox_CheckedChanged(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                RendererOptions.ShowMoleculeBoundingBoxes = ShowMoleculeBox.Checked;
                _dirty = true;
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void ShowCharacterBox_CheckedChanged(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                RendererOptions.ShowCharacterBoundingBoxes = ShowCharacterBox.Checked;
                _dirty = true;
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void ShowAtomCentres_CheckedChanged(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                RendererOptions.ShowAtomPositions = ShowAtomPositions.Checked;
                _dirty = true;
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void ShowConvexHulls_CheckedChanged(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                RendererOptions.ShowHulls = ShowConvexHulls.Checked;
                _dirty = true;
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void ShowMoleculeGrouping_CheckedChanged(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                RendererOptions.ShowMoleculeGrouping = ShowMoleculeGrouping.Checked;
                _dirty = true;
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void ShowMoleculeCaptions_CheckedChanged(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                RendererOptions.ShowMoleculeCaptions = ShowMoleculeCaptions.Checked;
                _dirty = true;
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void ShowAllCarbonAtoms_CheckedChanged(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                RendererOptions.ShowCarbons = ShowAllCarbonAtoms.Checked;
                _dirty = true;
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void ShowDoubleBondTrimmingLines_CheckedChanged(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                RendererOptions.ShowDoubleBondTrimmingLines = ShowDoubleBondTrimmingLines.Checked;
                _dirty = true;
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void ShowBondDirection_CheckedChanged(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                RendererOptions.ShowBondDirection = ShowBondDirection.Checked;
                _dirty = true;
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void ShowCharacterGroupsBox_CheckedChanged(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                RendererOptions.ShowCharacterGroupBoundingBoxes = ShowCharacterGroupsBox.Checked;
                _dirty = true;
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void ShowBondCrossingPoints_CheckedChanged(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                RendererOptions.ShowBondCrossingPoints = ShowBondCrossingPoints.Checked;
                _dirty = true;
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void ClipCrossingBonds_CheckedChanged(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                RendererOptions.ClipCrossingBonds = ClipCrossingBonds.Checked;
                _dirty = true;
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }
    }
}