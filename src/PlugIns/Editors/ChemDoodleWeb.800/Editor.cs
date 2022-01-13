// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using Chem4Word.Core.UI.Forms;
using IChem4Word.Contracts;

namespace Chem4Word.Editor.ChemDoodleWeb800
{
    public class Editor : IChem4WordEditor
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public string Name => "ChemDoodle Web Structure Editor 8.0.0";

        public string Description => "This is a legacy editor using the ChemDoodle Web 8.0.0 (JavaScript) structure editor";

        public bool HasSettings => true;
        public bool CanEditNestedMolecules => false;
        public bool CanEditFunctionalGroups => false;
        public bool RequiresSeedAtom => true;

        public string SettingsPath { get; set; }

        public List<string> Used1DProperties { get; set; }

        public Point TopLeft { get; set; }

        public string Cml { get; set; }

        public Dictionary<string, string> Properties { get; set; }

        public IChem4WordTelemetry Telemetry { get; set; }

        private Cdw800Options _editorOptions;

        public Editor()
        {
            // Nothing to do here
        }

        public bool ChangeSettings(Point topLeft)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Telemetry.Write(module, "Verbose", "Called");
                _editorOptions = new Cdw800Options(SettingsPath);

                Cdw800Settings settings = new Cdw800Settings();
                settings.Telemetry = Telemetry;
                settings.TopLeft = topLeft;

                Cdw800Options tempOptions = _editorOptions.Clone();
                settings.SettingsPath = SettingsPath;
                settings.EditorOptions = tempOptions;

                DialogResult dr = settings.ShowDialog();
                if (dr == DialogResult.OK)
                {
                    _editorOptions = tempOptions.Clone();
                }
                settings.Close();
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
            return true;
        }

        public DialogResult Edit()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            DialogResult result = DialogResult.Cancel;

            try
            {
                Telemetry.Write(module, "Verbose", "Called");
                _editorOptions = new Cdw800Options(SettingsPath);

                EditorHost host = new EditorHost(Cml);
                host.TopLeft = TopLeft;
                host.Telemetry = Telemetry;
                host.SettingsPath = SettingsPath;
                host.UserOptions = _editorOptions;

                result = host.ShowDialog();
                if (result == DialogResult.OK)
                {
                    Properties = new Dictionary<string, string>();
                    Cml = host.OutputValue;
                    host.Close();
                }
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }

            return result;
        }
    }
}