// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows.Forms;

namespace WinForms.TestHarness
{
    public partial class Dumper : Form
    {
        public Dumper(string data)
        {
            InitializeComponent();
            Dump.Text = data;
            Dump.SelectionStart = 0;
            Dump.SelectionLength = 0;
        }
    }
}