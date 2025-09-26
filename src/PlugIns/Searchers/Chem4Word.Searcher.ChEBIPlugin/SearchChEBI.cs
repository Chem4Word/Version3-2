// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Converters.MDL;
using Chem4Word.Searcher.ChEBIPlugin.Models;
using IChem4Word.Contracts;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;

namespace Chem4Word.Searcher.ChEBIPlugin
{
    public partial class SearchChEBI : Form
    {
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];

        #region Fields

        private Dictionary<string, string> _structureCache = new Dictionary<string, string>();
        private string _guid = Guid.NewGuid().ToString("N");

        private Model _lastModel;
        private string _lastMolfile = string.Empty;

        private HttpClient _client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

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

        private void OnTextChanged_SearchFor(object sender, EventArgs e)
        {
            SearchButton.Enabled = TextHelper.IsValidSearchString(SearchFor.Text);
        }

        private void EnableButtons()
        {
            if (_lastModel != null)
            {
                bool state = ResultsListView.SelectedItems.Count > 0
                             && display1.Chemistry != null
                             && _lastModel.AllErrors.Count == 0
                             && _lastModel.TotalAtomsCount > 0;
                ImportButton.Enabled = state;

                if (ShowMolfile.Visible)
                {
                    ShowMolfile.Enabled = true;
                }
            }
            else
            {
                ImportButton.Enabled = false;
                ShowMolfile.Enabled = false;
            }
        }

        private void ExecuteSearch(string searchFor)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            _structureCache.Clear();

            using (new WaitCursor())
            {
                SecurityProtocolType securityProtocol = ServicePointManager.SecurityProtocol;
                ServicePointManager.SecurityProtocol = securityProtocol | SecurityProtocolType.Tls12;

                try
                {
                    // https://www.ebi.ac.uk/chebi/backend/api/public/es_search/?term=benzene&page=1&size=10
                    // DefaultChEBIWebServiceUri = "https://www.ebi.ac.uk/chebi/"
                    string query = $"{UserOptions.ChEBIWebService2Uri}/backend/api/public/es_search/?term={searchFor}&page=1&size={UserOptions.MaximumResults}";

                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, query);
                    request.Headers.Add("User-Agent", "Chem4Word");
                    request.Headers.Add("Cookie", $"JSESSIONID={_guid}");

                    HttpResponseMessage response = _client.SendAsync(request).Result;
                    response.EnsureSuccessStatusCode();

                    string body = response.Content.ReadAsStringAsync().Result;
                    ProcessSearchResponse(body);
                }
                catch (Exception exception)
                {
                    if (exception.Message.Equals("The operation has timed out"))
                    {
                        ErrorsAndWarnings.Text = "Please try again later - the service has timed out";
                    }
                    else
                    {
                        ErrorsAndWarnings.Text = exception.Message;
                        Telemetry.Write(module, "Exception", exception.Message);
                        Telemetry.Write(module, "Exception", exception.StackTrace);
                    }
                }
                finally
                {
                    ServicePointManager.SecurityProtocol = securityProtocol;
                }
            }

            stopwatch.Stop();
            Telemetry.Write(module, "Information", $"Search for {searchFor} took {stopwatch.Elapsed}");
        }

        private void ProcessSearchResponse(string body)
        {
            ErrorsAndWarnings.Text = string.Empty;

            SearchResult data = JsonConvert.DeserializeObject<SearchResult>(body);

            if (data != null && data.Results.Any())
            {
                ResultsListView.Items.Clear();
                ResultsListView.Enabled = true;
                foreach (Result result in data.Results
                                              .OrderByDescending(r => r.Score)
                                              .ToList())
                {
                    ListViewItem li = new ListViewItem
                    {
                        Text = result.Source.ChebiId,
                        Tag = result
                    };

                    ListViewItem.ListViewSubItem name =
                        new ListViewItem.ListViewSubItem(li, result.Source.Name);
                    li.SubItems.Add(name);

                    ListViewItem.ListViewSubItem score =
                        new ListViewItem.ListViewSubItem(li, SafeDouble.AsString0(result.Score));
                    li.SubItems.Add(score);
                    ResultsListView.Items.Add(li);
                }

                ResultsListView.Columns[0].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
            }
        }

        private string GetStructureFromWeb(string chebiId)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            string result = string.Empty;

            using (new WaitCursor())
            {
                SecurityProtocolType securityProtocol = ServicePointManager.SecurityProtocol;
                ServicePointManager.SecurityProtocol = securityProtocol | SecurityProtocolType.Tls12;

                try
                {
                    // DefaultChEBIWebServiceUri = "https://www.ebi.ac.uk/chebi/"
                    // https://www.ebi.ac.uk/chebi/saveStructure.do?sdf=true&chebiId=CHEBI:82274&imageId=0

                    string query = $"{UserOptions.ChEBIWebService2Uri}/saveStructure.do?sdf=true&chebiId={chebiId}&imageId=0";

                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, query);
                    request.Headers.Add("User-Agent", "Chem4Word");
                    request.Headers.Add("Cookie", $"JSESSIONID={_guid}");

                    HttpResponseMessage response = _client.SendAsync(request).Result;
                    response.EnsureSuccessStatusCode();

                    result = response.Content.ReadAsStringAsync().Result;
                }
                catch (Exception exception)
                {
                    if (exception.Message.Equals("The operation has timed out"))
                    {
                        ErrorsAndWarnings.Text = "Please try again later - the service has timed out";
                    }
                    else
                    {
                        ErrorsAndWarnings.Text = exception.Message;
                        Telemetry.Write(module, "Exception", exception.Message);
                        Telemetry.Write(module, "Exception", exception.StackTrace);
                    }
                }
                finally
                {
                    ServicePointManager.SecurityProtocol = securityProtocol;
                }
            }

            stopwatch.Stop();
            Telemetry.Write(module, "Information", $"Get SDF for '{ChebiId}' took {stopwatch.Elapsed}");

            return result;
        }

        private void OnClick_ImportButton(object sender, EventArgs e)
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

            if (_lastModel != null)
            {
                using (new WaitCursor())
                {
                    CMLConverter conv = new CMLConverter();

                    double before = _lastModel.MeanBondLength;
                    _lastModel.ScaleToAverageBondLength(Core.Helpers.Constants.StandardBondLength);
                    double after = _lastModel.MeanBondLength;
                    Telemetry.Write(module, "Information", $"Structure rescaled from {before.ToString("#0.00")} to {after.ToString("#0.00")}");
                    _lastModel.Relabel(true);
                    Model expModel = _lastModel;

                    using (new WaitCursor())
                    {
                        if (expModel.Molecules.Values.Any())
                        {
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

        private void OnClick_ShowMolfile(object sender, EventArgs e)
        {
            if (_lastModel != null)
            {
                MolFileViewer tv = new MolFileViewer(new System.Windows.Point(TopLeft.X + Core.Helpers.Constants.TopLeftOffset, TopLeft.Y + Core.Helpers.Constants.TopLeftOffset), _lastMolfile);
                tv.ShowDialog();
                ResultsListView.Focus();
            }
        }

        private void OnMouseDoubleClick_ResultsListView(object sender, MouseEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                ErrorsAndWarnings.Text = "";

                using (new WaitCursor())
                {
                    ListViewItem itemUnderCursor = ResultsListView.HitTest(e.Location).Item;
                    if (itemUnderCursor != null)
                    {
                        UpdateDisplay();
                        if (_lastModel != null && _lastModel.AllErrors.Count + _lastModel.AllWarnings.Count == 0)
                        {
                            ResultsListView.SelectedItems.Clear();
                            itemUnderCursor.Selected = true;
                            EnableButtons();
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

        private void OnSelectedIndexChanged_ResultsListView(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            ErrorsAndWarnings.Text = "";

            using (WaitCursor cursor = new WaitCursor())
            {
                try
                {
                    if (ResultsListView.SelectedItems.Count > 0)
                    {
                        UpdateDisplay();

                        EnableButtons();
                    }
                }
                catch (Exception ex)
                {
                    cursor.Reset();
                    new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
                }
            }
        }

        private void OnClick_SearchButton(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                string searchFor = TextHelper.StripControlCharacters(SearchFor.Text).Trim();

                if (!string.IsNullOrEmpty(searchFor))
                {
                    Telemetry.Write(module, "Information", $"User searched for '{searchFor}'");

                    _lastModel = null;
                    _lastMolfile = string.Empty;
                    display1.Chemistry = null;
                    display1.Clear();

                    ExecuteSearch(searchFor);
                }
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void OnLoad_SearchChEBI(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (!PointHelper.PointIsEmpty(TopLeft))
                {
                    Left = (int)TopLeft.X;
                    Top = (int)TopLeft.Y;
                    Screen screen = Screen.FromControl(this);
                    Point sensible = PointHelper.SensibleTopLeft(TopLeft, screen, Width, Height);
                    Left = (int)sensible.X;
                    Top = (int)sensible.Y;
                }

                display1.Background = Brushes.White;
                display1.HighlightActive = false;

                ResultsListView.Enabled = false;
                ResultsListView.Items.Clear();
                AcceptButton = SearchButton;
                SearchButton.Enabled = false;

                EnableButtons();

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
                if (ResultsListView.SelectedItems[0]?.Tag is Result item)
                {
                    ChebiId = item.Source.ChebiId;

                    if (!_structureCache.TryGetValue(ChebiId, out string chemStructure))
                    {
                        chemStructure = GetStructureFromWeb(item.Source.ChebiId);
                        if (!string.IsNullOrEmpty(chemStructure))
                        {
                            _structureCache.Add(ChebiId, chemStructure);
                        }
                    }
                    else
                    {
                        Telemetry.Write(module, "Information", $"Structure '{ChebiId}' found in cache");
                    }

                    if (!string.IsNullOrEmpty(chemStructure))
                    {
                        _lastMolfile = ConvertToWindows(chemStructure);
                        SdFileConverter sdConverter = new SdFileConverter();
                        _lastModel = sdConverter.Import(chemStructure);
                    }

                    if (_lastModel != null)
                    {
                        if (_lastModel.TotalAtomsCount == 0)
                        {
                            display1.Chemistry = null;
                            display1.Clear();
                            ErrorsAndWarnings.Text = "No structure available.";
                        }
                        else if (_lastModel.AllWarnings.Count > 0 || _lastModel.AllErrors.Count > 0)
                        {
                            List<string> lines = new List<string>();
                            if (_lastModel.AllErrors.Count > 0)
                            {
                                Telemetry.Write(module, "Exception(Data)", string.Join(Environment.NewLine, _lastModel.AllErrors));
                                Telemetry.Write(module, "Exception(Data)", chemStructure);
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

                        Model copy = _lastModel.Copy();
                        copy.ScaleToAverageBondLength(Core.Helpers.Constants.StandardBondLength);
                        display1.Chemistry = copy;
                    }
                    else
                    {
                        _lastModel = null;
                        _lastMolfile = string.Empty;
                        display1.Chemistry = null;
                        display1.Clear();
                        ErrorsAndWarnings.Text = "No structure available.";
                    }
                }
            }
        }

        #endregion Methods
    }
}
