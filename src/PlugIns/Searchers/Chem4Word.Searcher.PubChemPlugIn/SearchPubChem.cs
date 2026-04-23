// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Converters.MDL;
using IChem4Word.Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Chem4Word.Searcher.PubChemPlugIn
{
    public partial class SearchPubChem : Form
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private const string SearchForTemplate = "{0}entrez/eutils/esearch.fcgi?db=pccompound&term={1}&retmode=xml&relevanceorder=on&usehistory=y&retmax={2}";
        private const string FindMoreTemplate = "{0}entrez/eutils/esearch.fcgi?db=pccompound&term={1}&retmode=xml&relevanceorder=on&usehistory=y&retmax={2}&WebEnv={3}&RetStart={4}";
        private const string GetDetailsTemplate = "{0}entrez/eutils/esummary.fcgi?db=pccompound&id={1}&retmode=xml";
        private const string FetchStructureTemplate = "{0}rest/pug/compound/cid/{1}/record/SDF";

        public Point TopLeft { get; set; }
        public IChem4WordTelemetry Telemetry { get; set; }
        public string SettingsPath { get; set; }
        public string PubChemId { get; set; }

        public string Cml { get; set; }

        public PubChemOptions UserOptions { get; set; }

        private List<string> _ids = new List<string>();

        private int _resultsCount;
        private string _webEnv;
        private int _lastResult;
        private int _firstResult;
        //private const int _numResults = 20;

        private Dictionary<string, string> _structureCache = new Dictionary<string, string>();

        private string _lastMolfile = string.Empty;

        public SearchPubChem()
        {
            InitializeComponent();
        }

        private void OnLoad_SearchPubChem(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (!PointHelper.PointIsEmpty(TopLeft))
                {
                    Left = (int)TopLeft.X;
                    Top = (int)TopLeft.Y;
                    Screen screen = Screen.FromControl(this);
                    Point sensible = PointHelper.SensibleTopLeft(new Point(Left, Top), screen, Width, Height);
                    Left = (int)sensible.X;
                    Top = (int)sensible.Y;
                }

                display1.Background = Brushes.White;
                display1.HighlightActive = false;

                NextButton.Enabled = false;
                PreviousButton.Enabled = false;
                ImportButton.Enabled = false;
                SearchButton.Enabled = false;

                Results.Enabled = false;
                AcceptButton = SearchButton;

                Results.Items.Clear();
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void OnTextChanged_SearchFor(object sender, EventArgs e)
        {
            SearchButton.Enabled = TextHelper.IsValidSearchString(SearchFor.Text);
        }

        private void OnClick_SearchButton(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            string searchFor = TextHelper.StripAsciiControlCharacters(SearchFor.Text).Trim();
            string webSafe = TextHelper.NormalizeCharacters(WebUtility.HtmlEncode(searchFor));

            Telemetry.Write(module, "Information", $"User searched for '{searchFor}'");

            ErrorsAndWarnings.Text = "";
            display1.Clear();
            Results.Items.Clear();

            _structureCache.Clear();
            ExecuteSearch(webSafe, 0);
        }

        private void OnClick_PreviousButton(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            string searchFor = TextHelper.StripAsciiControlCharacters(SearchFor.Text).Trim();
            string webSafe = TextHelper.NormalizeCharacters(WebUtility.HtmlEncode(searchFor));

            ErrorsAndWarnings.Text = "";
            display1.Clear();
            Results.Items.Clear();

            ExecuteSearch(webSafe, -1);
        }

        private void OnClick_NextButton(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            string searchFor = TextHelper.StripAsciiControlCharacters(SearchFor.Text).Trim();
            string webSafe = TextHelper.NormalizeCharacters(WebUtility.HtmlEncode(searchFor));

            ErrorsAndWarnings.Text = "";
            display1.Clear();
            Results.Items.Clear();

            ExecuteSearch(webSafe, 1);
        }

        private void OnClick_ImportButton(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                DialogResult = DialogResult.OK;
                Hide();
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void OnSelectedIndexChanged_Results(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                ListView.SelectedListViewItemCollection selected = Results.SelectedItems;
                if (selected.Count > 0)
                {
                    ListViewItem item = selected[0];
                    string pubChemId = item.Text;
                    PubChemId = pubChemId;

                    if (!_structureCache.TryGetValue(pubChemId, out string sdFile))
                    {
                        sdFile = FetchStructure(pubChemId);
                        if (!string.IsNullOrEmpty(sdFile))
                        {
                            _structureCache.Add(pubChemId, sdFile);
                        }
                    }
                    else
                    {
                        if (Debugger.IsAttached)
                        {
                            Telemetry.Write(module, "Information", $"Structure '{pubChemId}' found in cache");
                        }
                    }

                    if (!string.IsNullOrEmpty(sdFile))
                    {
                        DisplayStructure(sdFile);
                    }
                }
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void OnDoubleClick_Results(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Debug.WriteLine("OnDoubleClick_Results");
                DialogResult = DialogResult.OK;
                Hide();
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void ExecuteSearch(string webSafe, int direction)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            using (new WaitCursor())
            {
                try
                {
                    string webCall = string.Empty;
                    int startFrom;

                    switch (direction)
                    {
                        case 0:
                            webCall = string.Format(CultureInfo.InvariantCulture, SearchForTemplate,
                                                    UserOptions.PubChemWebServiceUri, webSafe, UserOptions.ResultsPerCall);
                            break;

                        case 1:
                            startFrom = _firstResult + UserOptions.ResultsPerCall;
                            webCall = string.Format(CultureInfo.InvariantCulture, FindMoreTemplate,
                                                    UserOptions.PubChemWebServiceUri, webSafe, UserOptions.ResultsPerCall,
                                                    _webEnv, startFrom);
                            break;

                        case -1:
                            startFrom = _firstResult - UserOptions.ResultsPerCall;
                            webCall = string.Format(CultureInfo.InvariantCulture, FindMoreTemplate,
                                                    UserOptions.PubChemWebServiceUri, webSafe, UserOptions.ResultsPerCall,
                                                    _webEnv, startFrom);
                            break;
                    }

                    ApiResult apiResult = HttpHelper.InvokeGet(webCall);
                    if (apiResult.StatusCode == HttpStatusCode.OK)
                    {
                        if (ParseResponseBody(apiResult.Content))
                        {
                            EnableButtons();
                            FillListView();
                        }
                    }
                    else
                    {
                        Telemetry.Write(module, "Exception", $"{apiResult.StatusCode} - {apiResult.Message}");
                    }
                }
                catch (Exception exception)
                {
                    Telemetry.Write(module, "Exception", exception.Message);
                    Telemetry.Write(module, "Exception", exception.StackTrace);
                }
            }
        }

        private void EnableButtons()
        {
            if (_lastResult > UserOptions.ResultsPerCall)
            {
                PreviousButton.Enabled = true;
            }
            else
            {
                PreviousButton.Enabled = false;
            }

            if (_lastResult < _resultsCount)
            {
                NextButton.Enabled = true;
            }
            else
            {
                NextButton.Enabled = false;
            }
        }

        private void FillListView()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            Results.Items.Clear();
            Refresh();

            if (_ids.Any())
            {
                // Set form title
                Text = $"Search PubChem - Showing {_firstResult + 1} to {_lastResult} [of {_resultsCount}]";

                GetData(string.Join(",", _ids));
            }
            else
            {
                ErrorsAndWarnings.Text = "Sorry, no results were found.";
            }
        }

        private bool ParseResponseBody(string responseBody)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            bool result = true;
            if (!string.IsNullOrEmpty(responseBody))
            {
                XDocument xDocument = XDocument.Parse(responseBody);

                // Get the count of results
                _resultsCount = GetInt(GetElement(xDocument, "Count"));

                // Current position
                _firstResult = GetInt(GetElement(xDocument, "RetStart"));
                int fetched = GetInt(GetElement(xDocument, "RetMax"));
                _lastResult = _firstResult + fetched;

                // WebEnv for history
                _webEnv = GetElement(xDocument, "WebEnv");

                List<string> errors = GetErrors(xDocument);
                List<string> warnings = GetWarnings(xDocument);

                if (errors.Any())
                {
                    result = false;
                    foreach (string error in errors)
                    {
                        Telemetry.Write(module, "Exception", error);
                    }
                }

                if (warnings.Any())
                {
                    foreach (string warning in warnings)
                    {
                        Telemetry.Write(module, "Warning", warning);
                    }
                }

                List<string> showToUser = errors;
                showToUser.AddRange(warnings);
                ErrorsAndWarnings.Text = string.Join(Environment.NewLine, showToUser);

                _ids = GetIds(xDocument);
            }

            return result;
        }

        private string GetElement(XDocument xDocument, string elementName)
        {
            XElement element = xDocument.XPathSelectElement($"//{elementName}");
            return element != null
                ? element.Value
                : string.Empty;
        }

        private List<string> GetElements(XDocument xDocument, string xpath)
        {
            return xDocument
                   .Descendants(xpath)
                   .Elements()
                   .Select(e => $"{e.Name.LocalName}: {e.Value}")
                   .ToList();
        }

        private List<string> GetErrors(XDocument xDocument)
        {
            return GetElements(xDocument, "ErrorList");
        }

        private List<string> GetWarnings(XDocument xDocument)
        {
            return GetElements(xDocument, "WarningList");
        }

        private int GetInt(string value)
        {
            int.TryParse(value, out int result);

            return result;
        }

        private void GetData(string idlist)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            string webCall = string.Format(CultureInfo.InvariantCulture, GetDetailsTemplate,
                                           UserOptions.PubChemWebServiceUri, idlist);

            ApiResult apiResult = HttpHelper.InvokeGet(webCall);
            if (apiResult.StatusCode == HttpStatusCode.OK)
            {
                XDocument xDocument = XDocument.Parse(apiResult.Content);
                if (xDocument != null)
                {
                    IEnumerable<XElement> compounds = xDocument.XPathSelectElements("//DocSum");
                    if (compounds.Any())
                    {
                        foreach (XElement compound in compounds)
                        {
                            XElement id = compound.XPathSelectElement("./Id");
                            XElement name = compound.XPathSelectElement("./Item[@Name='IUPACName']");
                            XElement formula = compound.XPathSelectElement("./Item[@Name='MolecularFormula']");
                            ListViewItem lvi = new ListViewItem(id.Value);

                            lvi.SubItems.Add(new ListViewItem.ListViewSubItem(lvi, name.Value));
                            lvi.SubItems.Add(new ListViewItem.ListViewSubItem(lvi, formula.Value));

                            Results.Items.Add(lvi);
                            // Add to a list view ...
                        }

                        Results.Enabled = true;
                    }
                    else
                    {
                        Debug.WriteLine("Something went wrong");
                        Debugger.Break();
                    }
                }
                else
                {
                    Telemetry.Write(module, "Exception", $"Error parsing {apiResult.Content}");
                }
            }
            else
            {
                Telemetry.Write(module, "Exception", $"{apiResult.StatusCode} - {apiResult.Message}");
            }
        }

        private List<string> GetIds(XDocument xDocument)
        {
            List<string> lisOfIds = new List<string>();
            IEnumerable<XElement> ids = xDocument.XPathSelectElements("//Id");
            foreach (XElement id in ids)
            {
                lisOfIds.Add(id.Value);
            }
            return lisOfIds;
        }

        private void DisplayStructure(string sdFile)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            SdFileConverter sdFileConverter = new SdFileConverter();
            Model model = sdFileConverter.Import(sdFile);

            CMLConverter cmlConverter = new CMLConverter();
            Cml = cmlConverter.Export(model);

            model.ScaleToAverageBondLength(Core.Helpers.Constants.StandardBondLength);
            display1.Chemistry = model;

            if (model.AllWarnings.Count > 0 || model.AllErrors.Count > 0)
            {
                Telemetry.Write(module, "Exception(Data)", _lastMolfile);
                List<string> lines = new List<string>();
                if (model.AllErrors.Count > 0)
                {
                    Telemetry.Write(module, "Exception(Data)",
                                    string.Join(Environment.NewLine, model.AllErrors));
                    lines.Add("Errors(s)");
                    lines.AddRange(model.AllErrors);
                }

                if (model.AllWarnings.Count > 0)
                {
                    Telemetry.Write(module, "Warning",
                                    string.Join(Environment.NewLine, model.AllWarnings));
                    lines.Add("Warnings(s)");
                    lines.AddRange(model.AllWarnings);
                }

                ErrorsAndWarnings.Text = string.Join(Environment.NewLine, lines);
            }
            else
            {
                ImportButton.Enabled = true;
            }
        }

        private string FetchStructure(string pubChemId)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            string result = string.Empty;

            using (new WaitCursor())
            {
                try
                {
                    Telemetry.Write(module, "Information", $"Fetching structure for '{pubChemId}'");

                    string webCall = string.Format(CultureInfo.InvariantCulture, FetchStructureTemplate,
                                                   UserOptions.PubChemRestApiUri, pubChemId);

                    ApiResult apiResult = HttpHelper.InvokeGet(webCall);
                    if (apiResult.StatusCode == HttpStatusCode.OK)
                    {
                        if (MolFileIsValid(apiResult.Content))
                        {
                            result = apiResult.Content;
                        }
                    }
                    else
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine($"Bad request. Status code: {apiResult.StatusCode}");
                        UserInteractions.AlertUser(sb.ToString());
                    }
                }
                catch (Exception exception)
                {
                    Telemetry.Write(module, "Exception", exception.Message);
                    Telemetry.Write(module, "Exception", exception.StackTrace);
                }
            }

            return result;
        }

        private void OnFormClosing_SearchPubChem(object sender, FormClosingEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (DialogResult != DialogResult.OK)
                {
                    DialogResult = DialogResult.Cancel;
                }
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private static bool MolFileIsValid(string molFile)
        {
            string file = molFile.ToUpper();

            int idx1 = file.IndexOf("V2000");
            int idx2 = file.IndexOf("M  END");

            return idx2 > idx1;
        }
    }
}
