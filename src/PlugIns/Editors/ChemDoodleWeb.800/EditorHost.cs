﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Reflection;
using System.Windows.Forms;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI;
using Chem4Word.Core.UI.Wpf;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Converters.JSON;
using IChem4Word.Contracts;

namespace Chem4Word.Editor.ChemDoodleWeb800
{
    public partial class EditorHost : Form
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public System.Windows.Point TopLeft { get; set; }

        public DialogResult Result = DialogResult.Cancel;

        public IChem4WordTelemetry Telemetry { get; set; }

        public string SettingsPath { get; set; }

        public Cdw800Options UserOptions { get; set; }

        private string _cml;

        public string OutputValue { get; set; }

        public EditorHost(string cml)
        {
            using (new WaitCursor())
            {
                string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
                _cml = cml;
                InitializeComponent();
            }
        }

        private void EditorHost_Load(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            using (new WaitCursor())
            {
                if (!PointHelper.PointIsEmpty(TopLeft))
                {
                    var screen = Screen.FromControl(this);
                    var sensible = PointHelper.SensibleTopLeft(TopLeft, screen, Width, Height);
                    Left = (int)sensible.X;
                    Top = (int)sensible.Y;
                }

                CMLConverter cc = new CMLConverter();
                JSONConverter jc = new JSONConverter();
                Model model = cc.Import(_cml);

                WpfChemDoodle editor = new WpfChemDoodle();
                editor.Telemetry = Telemetry;
                editor.SettingsPath = SettingsPath;
                editor.UserOptions = UserOptions;
                editor.TopLeft = TopLeft;

                editor.StructureJson = jc.Export(model);
                editor.IsSingleMolecule = model.Molecules.Count == 1;
                editor.AverageBondLength = model.MeanBondLength;

                editor.InitializeComponent();
                elementHost1.Child = editor;
                editor.OnButtonClick += OnWpfButtonClick;

                this.Show();
                Application.DoEvents();
            }
        }

        private void OnWpfButtonClick(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            WpfEventArgs args = (WpfEventArgs)e;

            if (args.Button.ToUpper().Equals("OK"))
            {
                DialogResult = DialogResult.OK;
                CMLConverter cc = new CMLConverter();
                JSONConverter jc = new JSONConverter();
                Model model = jc.Import(args.OutputValue);
                OutputValue = cc.Export(model);
            }

            if (args.Button.ToUpper().Equals("CANCEL"))
            {
                DialogResult = DialogResult.Cancel;
                OutputValue = "";
            }

            WpfChemDoodle editor = elementHost1.Child as WpfChemDoodle;
            if (editor != null)
            {
                editor.OnButtonClick -= OnWpfButtonClick;
            }
            Hide();
        }
    }
}