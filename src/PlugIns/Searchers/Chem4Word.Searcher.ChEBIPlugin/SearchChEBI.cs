// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Media;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Converters.MDL;
using Chem4Word.Searcher.ChEBIPlugin.ChEBI;
using IChem4Word.Contracts;

namespace Chem4Word.Searcher.ChEBIPlugin
{
    public partial class SearchChEBI : Form
    {
        #region Fields

        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];

        private const string EmptyCml = "<cml></cml>";

        private Entity _allResults;
        private Model _lastModel;
        private string _lastMolfile = string.Empty;

        #endregion Fields

        #region Constructors

        public SearchChEBI()
        {
            InitializeComponent();
        }

        #endregion Constructors

        #region Properties

        public string Cml { get; set; }
        public string ChebiId { get; set; }
        public string SettingsPath { get; set; }
        public IChem4WordTelemetry Telemetry { get; set; }
        public System.Windows.Point TopLeft { get; set; }
        public ChEBIOptions UserOptions { get; set; }

        #endregion Properties

        #region Methods

        private void EnableImport()
        {
            bool state = ResultsListView.SelectedItems.Count > 0
                         && display1.Chemistry != null
                         && string.IsNullOrEmpty(ErrorsAndWarnings.Text);
            ImportButton.Enabled = state;
            if (ShowMolfile.Visible)
            {
                state = !string.IsNullOrEmpty(_lastMolfile);
                ShowMolfile.Enabled = state;
            }
        }

        private void ExecuteSearch()
        {
            ErrorsAndWarnings.Text = "";
            using (new WaitCursor())
            {
                display1.Chemistry = null;
                if (!string.IsNullOrEmpty(SearchFor.Text))
                {
                    ChebiWebServiceService ws = new ChebiWebServiceService();
                    getLiteEntityResponse results;

                    var securityProtocol = ServicePointManager.SecurityProtocol;
                    ServicePointManager.SecurityProtocol = securityProtocol | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

                    ws.Url = UserOptions.ChEBIWebServiceUri;
                    ws.UserAgent = "Chem4Word";

                    results = ws.getLiteEntity(new getLiteEntity
                    {
                        search = SearchFor.Text,
                        maximumResults = UserOptions.MaximumResults,
                        searchCategory = SearchCategory.ALL,
                        stars = StarsCategory.ALL
                    });

                    try
                    {
                        var allResults = results.@return;
                        ResultsListView.Items.Clear();
                        ResultsListView.Enabled = true;
                        if (allResults.Length > 0)
                        {
                            foreach (LiteEntity res in allResults)
                            {
                                var li = new ListViewItem();
                                li.Text = res.chebiId;
                                li.Tag = res;
                                ListViewItem.ListViewSubItem name =
                                    new ListViewItem.ListViewSubItem(li, res.chebiAsciiName);
                                li.SubItems.Add(name);

                                ListViewItem.ListViewSubItem score =
                                    new ListViewItem.ListViewSubItem(li, res.searchScore.ToString());
                                li.SubItems.Add(score);
                                ResultsListView.Items.Add(li);
                            }

                            ResultsListView.Columns[0].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                        }
                        else
                        {
                            ErrorsAndWarnings.Text = "Sorry: No results found.";
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorsAndWarnings.Text = "The operation has timed out".Equals(ex.Message)
                            ? "Please try again later - the service has timed out"
                            : ex.Message;
                    }
                    finally
                    {
                        ServicePointManager.SecurityProtocol = securityProtocol;
                    }
                }
            }

            EnableImport();
        }

        private string GetChemStructure(LiteEntity le)
        {
            using (new WaitCursor())
            {
                var securityProtocol = ServicePointManager.SecurityProtocol;
                ServicePointManager.SecurityProtocol = securityProtocol | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

                ChebiWebServiceService ws = new ChebiWebServiceService();
                getCompleteEntityResponse results;

                ws.Url = UserOptions.ChEBIWebServiceUri;
                ws.UserAgent = "Chem4Word";

                getCompleteEntity gce = new getCompleteEntity();
                gce.chebiId = le.chebiId;

                results = ws.getCompleteEntity(gce);

                _allResults = results.@return;

                var chemStructure = _allResults?.ChemicalStructures?[0]?.structure;

                ServicePointManager.SecurityProtocol = securityProtocol;
                return chemStructure;
            }
        }

        private void ImportButton_Click(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                ImportStructure();
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void ImportStructure()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            using (new WaitCursor())
            {
                Cml = string.Empty;

                CMLConverter conv = new CMLConverter();

                var expModel = _lastModel;

                if (expModel != null && expModel.TotalAtomsCount > 0)
                {
                    expModel.Relabel(true);

                    using (new WaitCursor())
                    {
                        if (expModel.Molecules.Values.Any())
                        {
                            var mol = expModel.Molecules.Values.First();

                            mol.Names.Clear();

                            if (_allResults.IupacNames != null)
                            {
                                foreach (var di in _allResults.IupacNames)
                                {
                                    var cn = new TextualProperty();
                                    cn.Value = di.data;
                                    cn.FullType = "chebi:Iupac";
                                    mol.Names.Add(cn);
                                }
                            }

                            if (_allResults.Synonyms != null)
                            {
                                foreach (var di in _allResults.Synonyms)
                                {
                                    var cn = new TextualProperty();
                                    cn.Value = di.data;
                                    cn.FullType = "chebi:Synonym";
                                    mol.Names.Add(cn);
                                }
                            }

                            Cml = conv.Export(expModel);
                        }
                    }
                }
            }
        }

        private string ConvertToWindows(string message)
        {
            char etx = (char)3;
            string temp = message.Replace("\r\n", $"{etx}");
            temp = temp.Replace("\n", $"{etx}");
            temp = temp.Replace("\r", $"{etx}");
            string[] lines = temp.Split(etx);
            return string.Join(Environment.NewLine, lines);
        }

        private void ShowMolfile_Click(object sender, EventArgs e)
        {
            MolFileViewer tv = new MolFileViewer(new System.Windows.Point(TopLeft.X + Core.Helpers.Constants.TopLeftOffset, TopLeft.Y + Core.Helpers.Constants.TopLeftOffset), _lastMolfile);
            tv.ShowDialog();
            ResultsListView.Focus();
        }

        private void ResultsListView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                ErrorsAndWarnings.Text = "";
                using (new WaitCursor())
                {
                    var itemUnderCursor = ResultsListView.HitTest(e.Location).Item;
                    if (itemUnderCursor != null)
                    {
                        UpdateDisplay();
                        if (_lastModel != null && _lastModel.AllErrors.Count + _lastModel.AllWarnings.Count == 0)
                        {
                            ResultsListView.SelectedItems.Clear();
                            itemUnderCursor.Selected = true;
                            EnableImport();
                            ImportStructure();
                            if (!string.IsNullOrEmpty(Cml))
                            {
                                DialogResult = DialogResult.OK;
                                Close();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void ResultsListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            ErrorsAndWarnings.Text = "";

            using (var cursor = new WaitCursor())
            {
                try
                {
                    if (ResultsListView.SelectedItems.Count > 0)
                    {
                        UpdateDisplay();

                        EnableImport();
                    }
                }
                catch (Exception ex)
                {
                    cursor.Reset();
                    new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
                }
            }
        }

        private void SearchButton_Click(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (!string.IsNullOrEmpty(SearchFor.Text))
                {
                    Telemetry.Write(module, "Information", $"User searched for '{SearchFor.Text}'");
                    ExecuteSearch();
                }
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void SearchChEBI_Load(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (!PointHelper.PointIsEmpty(TopLeft))
                {
                    Left = (int)TopLeft.X;
                    Top = (int)TopLeft.Y;
                }

                ResultsListView.Enabled = false;
                ResultsListView.Items.Clear();

                AcceptButton = SearchButton;

                display1.Background = Brushes.White;
                display1.HighlightActive = false;

                EnableImport();

#if DEBUG
#else
                ShowMolfile.Visible = false;
#endif
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
            ErrorsAndWarnings.Text = "";
        }

        private void UpdateDisplay()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            ErrorsAndWarnings.Text = "";

            using (new WaitCursor())
            {
                var tag = ResultsListView.SelectedItems[0]?.Tag;

                if (tag is LiteEntity le && !string.IsNullOrEmpty(le.chebiId))
                {
                    ChebiId = le.chebiId;

                    var chemStructure = GetChemStructure(le);

                    if (!string.IsNullOrEmpty(chemStructure))
                    {
                        _lastMolfile = ConvertToWindows(chemStructure);
                        var sdConverter = new SdFileConverter();
                        _lastModel = sdConverter.Import(chemStructure);

                        if (_lastModel.AllWarnings.Count > 0 || _lastModel.AllErrors.Count > 0)
                        {
                            Telemetry.Write(module, "Exception(Data)", chemStructure);
                            var lines = new List<string>();
                            if (_lastModel.AllErrors.Count > 0)
                            {
                                Telemetry.Write(module, "Exception(Data)", string.Join(Environment.NewLine, _lastModel.AllErrors));
                                lines.Add("Errors(s)");
                                lines.AddRange(_lastModel.AllErrors);
                            }
                            if (_lastModel.AllWarnings.Count > 0)
                            {
                                Telemetry.Write(module, "Exception(Data)", string.Join(Environment.NewLine, _lastModel.AllWarnings));
                                lines.Add("Warnings(s)");
                                lines.AddRange(_lastModel.AllWarnings);
                            }
                            ErrorsAndWarnings.Text = string.Join(Environment.NewLine, lines);
                        }

                        var copy = _lastModel.Copy();
                        copy.ScaleToAverageBondLength(Core.Helpers.Constants.StandardBondLength);
                        display1.Chemistry = copy;
                    }
                    else
                    {
                        _lastMolfile = string.Empty;
                        var cmlConverter = new CMLConverter();
                        _lastModel = cmlConverter.Import(EmptyCml);
                        display1.Clear();
                        ErrorsAndWarnings.Text = "No structure available.";
                    }

                    EnableImport();
                }
            }
        }

        #endregion Methods
    }
}