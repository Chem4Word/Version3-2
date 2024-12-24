// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using System;
using System.Windows.Forms;

namespace Chem4Word.UI
{
    public partial class UpdateFailure : Form
    {
        public string WebPage { get; set; }

        public System.Windows.Point TopLeft { get; set; }

        public UpdateFailure()
        {
            InitializeComponent();
        }

        private void UpdateFailure_Load(object sender, EventArgs e)
        {
            if (!PointHelper.PointIsEmpty(TopLeft))
            {
                Left = (int)TopLeft.X;
                Top = (int)TopLeft.Y;
                var screen = Screen.FromControl(this);
                var sensible = PointHelper.SensibleTopLeft(TopLeft, screen, Width, Height);
                Left = (int)sensible.X;
                Top = (int)sensible.Y;
            }

            webBrowser1.DocumentText = WebPage;
        }
    }
}