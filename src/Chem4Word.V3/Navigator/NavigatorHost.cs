// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows.Forms;
using Chem4Word.ACME;
using Microsoft.Office.Interop.Word;

namespace Chem4Word.Navigator
{
    public partial class NavigatorHost : UserControl
    {
        private readonly AcmeOptions _acmeOptions;

        public NavigatorHost()
        {
            InitializeComponent();
            _acmeOptions = new AcmeOptions();
        }

        public NavigatorHost(Document document) : this()
        {
            var controller = new NavigatorController(document);
            navigatorView1.ActiveDocument = document;

            _acmeOptions = new AcmeOptions(Globals.Chem4WordV3.AddInInfo.ProductAppDataPath);
            navigatorView1.SetOptions(_acmeOptions);

            navigatorView1.DataContext = controller;
        }
    }
}