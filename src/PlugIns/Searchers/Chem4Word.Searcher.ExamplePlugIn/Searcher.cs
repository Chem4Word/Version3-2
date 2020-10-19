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
using System.Windows.Forms;
using Chem4Word.Core.UI.Forms;
using IChem4Word.Contracts;
using Point = System.Windows.Point;

namespace Chem4Word.Searcher.ExamplePlugIn
{
    public class Searcher : IChem4WordSearcher
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public int DisplayOrder => -1; // Don't Show
        public string ShortName => "Example";
        public string Name => "Example Search PlugIn";
        public string Description => "Does nothing ...";

        public bool HasSettings => true;
        public Point TopLeft { get; set; }
        public IChem4WordTelemetry Telemetry { get; set; }

        public string SettingsPath { get; set; }
        private ExampleOptions _searcherOptions;

        public Dictionary<string, string> Properties { get; set; }
        public string Cml { get; set; }

        public Searcher()
        {
            // Nothing to do here
        }

        public bool ChangeSettings(Point topLeft)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Telemetry.Write(module, "Verbose", "Called");
                _searcherOptions = new ExampleOptions(SettingsPath);

                ExampleSettings settings = new ExampleSettings();
                settings.Telemetry = Telemetry;
                settings.TopLeft = topLeft;

                var tempOptions = _searcherOptions.Clone();
                settings.SettingsPath = SettingsPath;
                settings.SearcherOptions = tempOptions;

                DialogResult dr = settings.ShowDialog();
                if (dr == DialogResult.OK)
                {
                    _searcherOptions = tempOptions.Clone();
                }
                settings.Close();
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
            return true;
        }

        public DialogResult Search()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            DialogResult dr = DialogResult.Cancel;

            try
            {
                // ToDo: Set any (extra) Properties required before calling this function
                // ToDo: Set Cml property with search result
                // ToDo: Return DialogResult.OK if operation not cancelled

                _searcherOptions = new ExampleOptions(SettingsPath);
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }

            return dr;
        }

        public Image Image
        {
            get { return null; }
        }
    }
}