// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Chem4Word.ACME;
using Chem4Word.Core;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2.Converters.CML;

namespace Chem4Word.Editor.SimpleWpfEditor
{
    public partial class EditorHost : Form
    {
        public System.Windows.Point TopLeft { get; set; }

        public Size FormSize { get; set; }

        public string OutputValue { get; set; }
        private string _cml;

        public EditorHost(string cml)
        {
            InitializeComponent();
            _cml = cml;
        }

        private void EditorHost_Load(object sender, EventArgs e)
        {
            if (!PointHelper.PointIsEmpty(TopLeft))
            {
                Left = (int)TopLeft.X;
                Top = (int)TopLeft.Y;
            }

            MinimumSize = new Size(300, 200);

            if (FormSize.Width != 0 && FormSize.Height != 0)
            {
                Width = FormSize.Width;
                Height = FormSize.Height;
            }

            // Fix bottom panel
            int margin = Buttons.Height - Save.Bottom;
            splitContainer1.SplitterDistance = splitContainer1.Height - Save.Height - margin * 2;
            splitContainer1.FixedPanel = FixedPanel.Panel2;
            splitContainer1.IsSplitterFixed = true;

            // Set Up WPF UC
            if (elementHost1.Child is CmlEditor editor)
            {
                editor.Cml = _cml;
            }
        }

        private void Save_Click(object sender, EventArgs e)
        {
            CMLConverter cc = new CMLConverter();
            DialogResult = DialogResult.Cancel;

            if (elementHost1.Child is CmlEditor editor
                && editor.IsDirty)
            {
                DialogResult = DialogResult.OK;
                OutputValue = cc.Export(editor.EditedModel);
            }
            Hide();
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Hide();
        }

        private void EditorHost_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (DialogResult != DialogResult.OK && e.CloseReason == CloseReason.UserClosing)
            {
                if (elementHost1.Child is CmlEditor editor
                    && editor.IsDirty)
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
                            DialogResult = DialogResult.OK;
                            CMLConverter cc = new CMLConverter();
                            OutputValue = cc.Export(editor.EditedModel);
                            Hide();
                            break;

                        case DialogResult.No:
                            break;
                    }
                }
            }
        }
    }
}