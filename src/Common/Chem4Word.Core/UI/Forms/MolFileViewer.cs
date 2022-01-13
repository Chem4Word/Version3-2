// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Windows.Forms;
using Chem4Word.Core.Helpers;

namespace Chem4Word.Core.UI.Forms
{
    public partial class MolFileViewer : Form
    {
        public string Message { get; set; }
        public System.Windows.Point TopLeft { get; set; }

        public MolFileViewer(System.Windows.Point topLeft, string text)
        {
            InitializeComponent();

            TopLeft = topLeft;
            Message = text;
        }

        private void TextViewer_Load(object sender, EventArgs e)
        {
            if (!PointHelper.PointIsEmpty(TopLeft))
            {
                Left = (int)TopLeft.X;
                Top = (int)TopLeft.Y;
            }

            try
            {
                textBox1.Text = Message;
                textBox1.SelectionStart = 0;
                textBox1.SelectionLength = 1;
            }
            catch (Exception)
            {
                // Do Nothing
            }
        }
    }
}