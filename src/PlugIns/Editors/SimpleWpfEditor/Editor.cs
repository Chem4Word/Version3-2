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
using Chem4Word.Core.UI.Forms;
using IChem4Word.Contracts;

namespace Chem4Word.Editor.SimpleWpfEditor
{
    public class Editor : IChem4WordEditor
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public string Name => "Example Wpf Structure Editor";

        public string Description => "This is a PoC to show that a WPF editor can be made";

        public Point TopLeft { get; set; }

        public bool HasSettings => false;
        public bool CanEditNestedMolecules => true;
        public bool CanEditFunctionalGroups => true;
        public bool RequiresSeedAtom => false;

        public string SettingsPath { get; set; }

        public List<string> Used1DProperties { get; set; }

        public string Cml { get; set; }

        public Dictionary<string, string> Properties { get; set; }

        public IChem4WordTelemetry Telemetry { get; set; }

        public Editor()
        {
            // Nothing to do here
        }

        public bool ChangeSettings(Point topLeft) => false;

        public DialogResult Edit()
        {
            DialogResult dialogResult = DialogResult.Cancel;

            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Telemetry.Write(module, "Verbose", "Called");

                using (EditorHost host = new EditorHost(Cml))
                {
                    host.TopLeft = TopLeft;

                    DialogResult showDialog = host.ShowDialog();
                    if (showDialog == DialogResult.OK)
                    {
                        dialogResult = showDialog;
                        Cml = host.OutputValue;
                    }

                    host.Close();
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