// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms;

namespace Chem4Word.Core.UI.Forms
{
    public partial class HtmlViewer : Form
    {
        public Point TopLeft { get; set; }

        private readonly Screen _screen;

        public string Html { get; set; }
        public string FormTitle { get; set; }

        private readonly WebView2 _webView;

        public HtmlViewer(Point topLeft, Screen screen, string title, string html)
        {
            InitializeComponent();

            _webView = new WebView2 { Dock = DockStyle.Fill };
            Controls.Add(_webView);

            _screen = screen;

            TopLeft = topLeft;
            FormTitle = title;
            Html = html;
        }

        private async void OnLoad_HtmlViewer(object sender, EventArgs e)
        {
            if (!PointHelper.PointIsEmpty(TopLeft))
            {
                Left = (int)TopLeft.X;
                Top = (int)TopLeft.Y;

                Point sensible = PointHelper.SensibleTopLeft(new Point(Left, Top), _screen, Width, Height);

                Left = (int)sensible.X;
                Top = (int)sensible.Y;
            }

            Text = FormTitle;

            await _webView.EnsureCoreWebView2Async();

            _webView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
            _webView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;

            _webView.CoreWebView2.NavigateToString(Html);
        }

        private void CoreWebView2_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            HandleSpecialLinks(e.Uri, e);
        }

        private void CoreWebView2_NewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            e.Handled = true; // Prevent WebView2 from opening a new window
            HandleSpecialLinks(e.Uri, null);
        }

        /// <summary>
        /// Handles mailto, tel, and external links.
        /// </summary>
        private void HandleSpecialLinks(string url, CoreWebView2NavigationStartingEventArgs navArgs)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                {
                    return;
                }

                if (url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
                    || url.StartsWith("tel:", StringComparison.OrdinalIgnoreCase)
                    || url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    navArgs?.CancelNavigation();
                    OpenWithShell(url);
                }
            }
            catch
            {
                // Do Nothing
            }
        }

        /// <summary>
        /// Opens a URL with the system's default handler.
        /// </summary>
        private void OpenWithShell(string uri)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = uri,
                UseShellExecute = true
            });
        }
    }

    // Extension method for cleaner cancel handling
    public static class WebView2Extensions
    {
        public static void CancelNavigation(this CoreWebView2NavigationStartingEventArgs e)
        {
            if (e != null)
            {
                e.Cancel = true;
            }
        }
    }
}
