// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
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
using Chem4Word.Searcher.PubChemPlugIn.Properties;
using IChem4Word.Contracts;
using Point = System.Windows.Point;

namespace Chem4Word.Searcher.PubChemPlugIn
{
    public class Searcher : IChem4WordSearcher
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private PubChemOptions _searcherOptions;

        public bool HasSettings => true;

        public string ShortName => "Pubchem";
        public string Name => "PubChem Search PlugIn";
        public string Description => "Searches the PubChem public database";
        public Image Image => Resources.PubChem_Logo;

        public int DisplayOrder
        {
            get
            {
                _searcherOptions = new PubChemOptions(SettingsPath);
                return _searcherOptions.DisplayOrder;
            }
        }

        public Point TopLeft { get; set; }
        public IChem4WordTelemetry Telemetry { get; set; }
        public string SettingsPath { get; set; }
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
                _searcherOptions = new PubChemOptions(SettingsPath);

                PubChemSettings settings = new PubChemSettings();
                settings.Telemetry = Telemetry;
                settings.TopLeft = topLeft;

                PubChemOptions tempOptions = _searcherOptions.Clone();
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
            DialogResult result = DialogResult.Cancel;
            try
            {
                _searcherOptions = new PubChemOptions(SettingsPath);

                SearchPubChem searcher = new SearchPubChem();

                searcher.TopLeft = TopLeft;
                searcher.Telemetry = Telemetry;
                searcher.SettingsPath = SettingsPath;
                searcher.UserOptions = _searcherOptions;

                result = searcher.ShowDialog();
                if (result == DialogResult.OK)
                {
                    Properties = new Dictionary<string, string>();
                    Telemetry.Write(module, "Information", $"Importing Id {searcher.PubChemId}");
                    Cml = searcher.Cml;
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