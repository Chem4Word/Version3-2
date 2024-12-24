// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using IChem4Word.Contracts;
using System;
using System.ComponentModel;
using System.Windows;

namespace Chem4Word.ACME.Controls
{
    /// <summary>
    /// Interaction logic for SketcherHost.xaml
    /// </summary>
    public partial class SketcherHost : Window
    {
        private Point TopLeft { get; set; }

        public SketcherHost()
        {
            InitializeComponent();
        }

        public SketcherHost(AcmeOptions options, IChem4WordTelemetry telemetry, Point topLeft) : this()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                Sketcher.EditorOptions = options;
                Sketcher.Telemetry = telemetry;
                Sketcher.TopLeft = topLeft;

                TopLeft = topLeft;
            }
        }

        private void SketcherHost_OnClosing(object sender, CancelEventArgs e)
        {
            if (Sketcher.IsDirty)
            {
                // ToDo ???
            }
        }

        private void SketcherHost_OnContentRendered(object sender, EventArgs e)
        {
            if (Sketcher != null)
            {
                // ToDo ???
            }
        }

        private void SketcherHost_OnLoaded(object sender, RoutedEventArgs e)
        {
            Left = TopLeft.X;
            Top = TopLeft.Y;

            if (Sketcher != null)
            {
                Sketcher.TopLeft = new Point(TopLeft.X + Core.Helpers.Constants.TopLeftOffset, TopLeft.Y + Core.Helpers.Constants.TopLeftOffset);
            }
        }
    }
}