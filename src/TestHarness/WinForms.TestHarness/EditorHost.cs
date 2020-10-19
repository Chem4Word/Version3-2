// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using Chem4Word.ACME;
using Chem4Word.Core;
using Chem4Word.Core.UI.Wpf;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Telemetry;
using Size = System.Drawing.Size;

namespace WinForms.TestHarness
{
    public partial class EditorHost : Form
    {
        public string OutputValue { get; set; }

        private readonly string _editorType;

        public EditorHost(string cml, string type)
        {
            InitializeComponent();
            _editorType = type;

            AcmeOptions acmeOptions = new AcmeOptions(null);

            var used1D = SimulateGetUsed1DLabels(cml);

            MessageFromWpf.Text = "";

            SystemHelper helper = new SystemHelper();
            var telemetry = new TelemetryWriter(true, helper);

            switch (_editorType)
            {
                case "ACME":
                    Editor acmeEditor = new Editor();
                    acmeEditor.EditorOptions = acmeOptions;
                    acmeEditor.InitializeComponent();
                    elementHost1.Child = acmeEditor;

                    // Configure Control
                    acmeEditor.ShowFeedback = false;
                    acmeEditor.TopLeft = new Point(Left, Top);
                    acmeEditor.Telemetry = telemetry;
                    acmeEditor.SetProperties(cml, used1D, acmeOptions);

                    acmeEditor.OnFeedbackChange += AcmeEditorOnFeedbackChange;

                    break;

                case "LABELS":
                    LabelsEditor labelsEditor = new LabelsEditor(acmeOptions);
                    labelsEditor.InitializeComponent();
                    elementHost1.Child = labelsEditor;

                    // Configure Control
                    labelsEditor.TopLeft = new Point(Left, Top);
                    labelsEditor.Used1D = used1D;
                    labelsEditor.PopulateTreeView(cml);

                    break;

                default:
                    CmlEditor cmlEditor = new CmlEditor();
                    cmlEditor.InitializeComponent();
                    elementHost1.Child = cmlEditor;

                    // Configure Control
                    cmlEditor.Cml = cml;

                    break;
            }
        }

        private void AcmeEditorOnFeedbackChange(object sender, WpfEventArgs e)
        {
            MessageFromWpf.Text = e.OutputValue;
        }

        private List<string> SimulateGetUsed1DLabels(string cml)
        {
            CMLConverter cc = new CMLConverter();
            Model model = cc.Import(cml);

            List<string> used1D = new List<string>();

            foreach (var property in model.AllTextualProperties)
            {
                if (property.FullType != null
                    && (property.FullType.Equals(CMLConstants.ValueChem4WordCaption)
                      || property.FullType.Equals(CMLConstants.ValueChem4WordFormula)
                      || property.FullType.Equals(CMLConstants.ValueChem4WordSynonym)))
                {
                    used1D.Add($"{property.Id}:{model.CustomXmlPartGuid}");
                }
            }

            return used1D;
        }

        private void EditorHost_Load(object sender, EventArgs e)
        {
            MinimumSize = new Size(900, 600);

            // Fix bottom panel
            int margin = Buttons.Height - Save.Bottom;
            splitContainer1.SplitterDistance = splitContainer1.Height - Save.Height - margin * 2;
            splitContainer1.FixedPanel = FixedPanel.Panel2;
            splitContainer1.IsSplitterFixed = true;

            switch (_editorType)
            {
                case "ACME":
                    if (elementHost1.Child is Editor acmeEditor)
                    {
                        acmeEditor.TopLeft = new Point(Location.X + Chem4Word.Core.Helpers.Constants.TopLeftOffset, Location.Y + Chem4Word.Core.Helpers.Constants.TopLeftOffset);
                    }
                    break;

                case "LABELS":
                    if (elementHost1.Child is LabelsEditor labelsEditor)
                    {
                        labelsEditor.TopLeft = new Point(Location.X + Chem4Word.Core.Helpers.Constants.TopLeftOffset, Location.Y + Chem4Word.Core.Helpers.Constants.TopLeftOffset);
                    }
                    break;

                default:
                    // Do Nothing
                    break;
            }
        }

        private void Save_Click(object sender, EventArgs e)
        {
            CMLConverter cc = new CMLConverter();
            DialogResult = DialogResult.Cancel;

            switch (_editorType)
            {
                case "ACME":
                    if (elementHost1.Child is Editor acmeEditor
                        && acmeEditor.IsDirty)
                    {
                        DialogResult = DialogResult.OK;
                        var model = acmeEditor.EditedModel;
                        model.RescaleForCml();
                        // Replace any temporary Ids which are Guids
                        model.ReLabelGuids();
                        OutputValue = cc.Export(model);
                    }
                    break;

                case "LABELS":
                    if (elementHost1.Child is LabelsEditor labelsEditor
                        && labelsEditor.IsDirty)
                    {
                        DialogResult = DialogResult.OK;
                        OutputValue = cc.Export(labelsEditor.EditedModel);
                    }
                    break;

                default:
                    if (elementHost1.Child is CmlEditor cmlEditor
                        && cmlEditor.IsDirty)
                    {
                        DialogResult = DialogResult.OK;
                        OutputValue = cc.Export(cmlEditor.EditedModel);
                    }
                    break;
            }
            Hide();
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            Hide();
        }

        private void EditorHost_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (DialogResult != DialogResult.OK && e.CloseReason == CloseReason.UserClosing)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Do you wish to save your changes?");
                sb.AppendLine("  Click 'Yes' to save your changes and exit.");
                sb.AppendLine("  Click 'No' to discard your changes and exit.");
                sb.AppendLine("  Click 'Cancel' to return to the form.");

                CMLConverter cc = new CMLConverter();

                switch (_editorType)
                {
                    case "ACME":
                        if (elementHost1.Child is Editor acmeEditor
                            && acmeEditor.IsDirty)
                        {
                            DialogResult dr = UserInteractions.AskUserYesNoCancel(sb.ToString());
                            switch (dr)
                            {
                                case DialogResult.Cancel:
                                    e.Cancel = true;
                                    break;

                                case DialogResult.Yes:
                                    DialogResult = DialogResult.OK;
                                    var model = acmeEditor.EditedModel;
                                    model.RescaleForCml();
                                    // Replace any temporary Ids which are Guids
                                    model.ReLabelGuids();
                                    OutputValue = cc.Export(model);
                                    Hide();
                                    break;

                                case DialogResult.No:
                                    break;
                            }
                        }
                        break;

                    case "LABELS":
                        if (elementHost1.Child is LabelsEditor labelsEditor
                            && labelsEditor.IsDirty)
                        {
                            DialogResult dr = UserInteractions.AskUserYesNoCancel(sb.ToString());
                            switch (dr)
                            {
                                case DialogResult.Cancel:
                                    e.Cancel = true;
                                    break;

                                case DialogResult.Yes:
                                    DialogResult = DialogResult.OK;
                                    OutputValue = cc.Export(labelsEditor.EditedModel);
                                    Hide();
                                    break;

                                case DialogResult.No:
                                    break;
                            }
                        }
                        break;

                    default:
                        if (elementHost1.Child is CmlEditor editor
                            && editor.IsDirty)
                        {
                            DialogResult dr = UserInteractions.AskUserYesNoCancel(sb.ToString());
                            switch (dr)
                            {
                                case DialogResult.Cancel:
                                    e.Cancel = true;
                                    break;

                                case DialogResult.Yes:
                                    DialogResult = DialogResult.OK;
                                    OutputValue = cc.Export(editor.EditedModel);
                                    Hide();
                                    break;

                                case DialogResult.No:
                                    break;
                            }
                        }
                        break;
                }
            }
        }
    }
}