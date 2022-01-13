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
using Chem4Word.Core.UI;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Searcher.ChEBIPlugin.Properties;
using IChem4Word.Contracts;
using Point = System.Windows.Point;

namespace Chem4Word.Searcher.ChEBIPlugin
{
    public class Searcher : IChem4WordSearcher
    {
        #region Fields

        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private ChEBIOptions _searcherOptions = new ChEBIOptions();

        #endregion Fields

        #region Constructors

        public Searcher()
        {
            //nothing to do here
        }

        #endregion Constructors

        #region Properties

        public string Cml { get; set; }
        public string Description => "Searches the Chemical Entities of Biological Interest database.";

        public int DisplayOrder
        {
            get
            {
                _searcherOptions = new ChEBIOptions(SettingsPath);

                return _searcherOptions.DisplayOrder;
            }
        }

        public bool HasSettings => true;
        public Image Image => Resources.chebi;
        public string Name => "ChEBI Search PlugIn";
        public string SettingsPath { get; set; }
        public Dictionary<string, string> Properties { get; set; }
        public string ShortName => "ChEBI";
        public IChem4WordTelemetry Telemetry { get; set; }
        public Point TopLeft { get; set; }

        #endregion Properties

        #region Methods

        public bool ChangeSettings(Point topLeft)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Telemetry.Write(module, "Verbose", "Called");
                _searcherOptions = new ChEBIOptions(SettingsPath);

                ChEBISettings settings = new ChEBISettings();
                settings.Telemetry = Telemetry;
                settings.TopLeft = topLeft;

                ChEBIOptions tempOptions = _searcherOptions.Clone();
                settings.SettingsPath = SettingsPath;
                settings.SearcherOptions = tempOptions;

                DialogResult dr = settings.ShowDialog();
                if (dr == DialogResult.OK)
                {
                    _searcherOptions = tempOptions.Clone();
                }
                settings.Close();
                settings = null;
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
            try
            {
                _searcherOptions = new ChEBIOptions(SettingsPath);

                using (new WaitCursor())
                {
                    var searcher = new SearchChEBI
                                   {
                                       TopLeft = TopLeft,
                                       Telemetry = Telemetry,
                                       SettingsPath = SettingsPath,
                                       UserOptions = _searcherOptions
                                   };

                    using (new WaitCursor(Cursors.Default))
                    {
                        DialogResult dr = searcher.ShowDialog();
                        if (dr == DialogResult.OK)
                        {
                            Properties = new Dictionary<string, string>();
                            Telemetry.Write(module, "Information", $"Importing Id {searcher.ChebiId}");
                            Cml = searcher.Cml;
                        }

                        return dr;
                    }
                }
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
                return DialogResult.Cancel;
            }
        }

        #endregion Methods
    }
}