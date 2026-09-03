// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.ComponentModel.Design;
using System.Windows.Forms;

namespace WinForms.TestHarness
{
    public partial class HexViewer : Form
    {
        public HexViewer(string filename)
        {
            InitializeComponent();
            ByteViewer bv = new ByteViewer();
            bv.SetFile(filename);
            bv.Dock = DockStyle.Fill;
            bv.SetDisplayMode(DisplayMode.Hexdump);
            Controls.Add(bv);
        }
    }
}