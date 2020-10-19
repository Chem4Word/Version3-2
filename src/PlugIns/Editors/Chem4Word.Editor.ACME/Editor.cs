// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using Chem4Word.ACME;
using Chem4Word.Core.UI.Forms;
using IChem4Word.Contracts;

namespace Chem4Word.Editor.ACME
{
    public class Editor : IChem4WordEditor
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public string Name => "ACME Structure Editor";

        public string Description => "This is the standard editor for Chem4Word 2020 editor. ACME stands for Advanced CML-based Molecule Editor.";

        public bool HasSettings => true;

        public bool CanEditNestedMolecules => true;
        public bool CanEditFunctionalGroups => true;
        public bool RequiresSeedAtom => false;

        public string SettingsPath { get; set; }

        public List<string> Used1DProperties { get; set; }

        public Point TopLeft { get; set; }

        public IChem4WordTelemetry Telemetry { get; set; }

        public Dictionary<string, string> Properties { get; set; }

        public string Cml { get; set; }

        private AcmeOptions _editorOptions;

        public bool ChangeSettings(Point topLeft)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Telemetry.Write(module, "Verbose", "Called");
                if (HasSettings)
                {
                    _editorOptions = new AcmeOptions(SettingsPath)
                    {
                        Dirty = false
                    };

                    using (var settings = new AcmeSettingsHost())
                    {
                        settings.Telemetry = Telemetry;
                        settings.TopLeft = topLeft;

                        var tempOptions = _editorOptions.Clone();
                        settings.EditorOptions = tempOptions;

                        DialogResult dr = settings.ShowDialog();
                        if (dr == DialogResult.OK)
                        {
                            _editorOptions = tempOptions.Clone();
                        }
                        settings.Close();
                    }
                }
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
            DialogResult dialogResult = DialogResult.Cancel;

            try
            {
                Telemetry.Write(module, "Verbose", "Called");
                if (HasSettings)
                {
                    _editorOptions = new AcmeOptions(SettingsPath)
                    {
                        Dirty = false
                    };
                }

                using (EditorHost host = new EditorHost(Cml, Used1DProperties, _editorOptions))
                {
                    host.TopLeft = TopLeft;
                    host.Telemetry = Telemetry;

                    DialogResult showDialog = host.ShowDialog();
                    if (showDialog == DialogResult.OK)
                    {
                        dialogResult = showDialog;
                        Cml = host.OutputValue;
                    }
                }
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }

            return dialogResult;
        }
    }
}