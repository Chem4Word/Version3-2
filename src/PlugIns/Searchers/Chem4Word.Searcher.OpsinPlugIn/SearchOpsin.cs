// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Model2.Converters.CML;
using IChem4Word.Contracts;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;

namespace Chem4Word.Searcher.OpsinPlugIn
{
    public partial class SearchOpsin : Form
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod()?.DeclaringType?.Name;

        private const string BaseTemplate = "{0}{1}";

        public Point TopLeft { get; set; }
        public IChem4WordTelemetry Telemetry { get; set; }
        public string SettingsPath { get; set; }

        public string Cml { get; set; }

        public SearcherOptions UserOptions { get; set; }

        public SearchOpsin()
        {
            InitializeComponent();
        }

        private void OnTextChanged_SearchFor(object sender, EventArgs e)
        {
            SearchButton.Enabled = TextHelper.IsValidSearchString(SearchFor.Text);
        }

        private void OnClick_SearchButton(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            string searchFor = TextHelper.StripAsciiControlCharacters(SearchFor.Text).Trim();
            string webSafe = TextHelper.NormalizeCharacters(WebUtility.HtmlEncode(searchFor));

            Telemetry.Write(module, LoggingLevel.Search, $"User searched for '{searchFor}'");

            display1.Clear();
            LabelInfo.Text = "";

            using (new WaitCursor())
            {
                string webCall = string.Format(CultureInfo.InvariantCulture, BaseTemplate, UserOptions.WebServiceUri, webSafe);

                Dictionary<string, string> headers = new Dictionary<string, string>
                                                     {
                                                         { "Accept", "chemical/x-cml" }
                                                     };

                ApiResult apiResult = HttpHelper.InvokeGet(webCall, headers);
                if (apiResult.StatusCode == HttpStatusCode.OK)
                {
                    if (apiResult.Content.ToLower().Contains("<!doctype html"))
                    {
                        Telemetry.Write(module, LoggingLevel.Warning, "Showing user html result of web call");
                        Telemetry.Write(module, LoggingLevel.WarningData, apiResult.Content.Trim());

                        Screen screen = Screen.FromControl(SearchButton);
                        HtmlViewer viewer = new HtmlViewer(new Point(TopLeft.X + Constants.TopLeftOffset, TopLeft.Y + Constants.TopLeftOffset), screen, "EXTERNAL OPSIN Search Error", apiResult.Content);
                        viewer.ShowDialog(this);
                    }
                    else
                    {
                        CMLConverter cmlConverter = new CMLConverter();
                        Model2.Model model = cmlConverter.Import(apiResult.Content);
                        if (!model.HasValidBondLengths)
                        {
                            model.ScaleToAverageBondLength(Constants.StandardBondLength);
                        }

                        Cml = cmlConverter.Export(model);

                        display1.Chemistry = Cml;
                        ImportButton.Enabled = true;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(apiResult.Content))
                    {
                        Telemetry.Write(module, LoggingLevel.SearchFailureData, apiResult.Content.Trim());
                    }
                    Telemetry.Write(module, LoggingLevel.SearchFailure, $"[{(int)apiResult.StatusCode}] {apiResult.StatusCode}");
                    LabelInfo.Text = $"No match for '{searchFor}' found";
                    ImportButton.Enabled = false;
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

        private void OnClick_ImportButton(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Hide();
        }

        private void OnLoad_SearchOpsin(object sender, EventArgs e)
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

            ImportButton.Enabled = false;
            SearchButton.Enabled = false;

            LabelInfo.Text = "";
            AcceptButton = SearchButton;
        }
    }
}
