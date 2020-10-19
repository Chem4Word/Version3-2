// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Chem4Word.ACME;
using Chem4Word.Core;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Model2.Converters.CML;

namespace Chem4Word.UI.WPF
{
    public partial class EditLabelsHost : Form
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public System.Windows.Point TopLeft { get; set; }
        public string Cml { get; set; }
        public List<string> Used1D { get; set; }
        public string Message { get; set; }

        private AcmeOptions _options;

        private bool _closedInCode = false;

        public EditLabelsHost()
        {
            _options = new AcmeOptions();
            InitializeComponent();
        }

        public EditLabelsHost(AcmeOptions options)
        {
            using (new WaitCursor())
            {
                _options = options;
                InitializeComponent();
            }
        }

        private void EditLabelsHost_Load(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                using (new WaitCursor())
                {
                    MinimumSize = new Size(900, 600);

                    if (!PointHelper.PointIsEmpty(TopLeft))
                    {
                        Left = (int)TopLeft.X;
                        Top = (int)TopLeft.Y;
                    }

                    // Fix bottom panel
                    int margin = Buttons.Height - Save.Bottom;
                    splitContainer1.SplitterDistance = splitContainer1.Height - Save.Height - margin * 2;
                    splitContainer1.FixedPanel = FixedPanel.Panel2;
                    splitContainer1.IsSplitterFixed = true;

                    var editor = new LabelsEditor(_options);
                    editor.InitializeComponent();
                    elementHost1.Child = editor;

                    editor.TopLeft = TopLeft;
                    editor.Used1D = Used1D;
                    editor.PopulateTreeView(Cml);

                    Warning.Text = Message;
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, TopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        private void Save_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            _closedInCode = true;
            if (elementHost1.Child is LabelsEditor editor)
            {
                CMLConverter cc = new CMLConverter();
                DialogResult = DialogResult.OK;
                Cml = cc.Export(editor.EditedModel);
                Hide();
            }
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            Hide();
        }

        private void EditLabelsHost_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_closedInCode)
            {
                if (elementHost1.Child is LabelsEditor editor)
                {
                    if (editor.IsDirty)
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
                                var cmlConvertor = new CMLConverter();
                                Cml = cmlConvertor.Export(editor.EditedModel);
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
}