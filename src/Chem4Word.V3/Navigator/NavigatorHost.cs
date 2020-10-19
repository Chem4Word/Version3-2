// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
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
        private Document _activeDoc;
        private AcmeOptions _acmeOptions;

        public NavigatorHost()
        {
            InitializeComponent();
            _acmeOptions = new AcmeOptions();
        }

        public NavigatorHost(Microsoft.Office.Interop.Word.Application app, Document doc) : this()
        {
            _acmeOptions = new AcmeOptions(Globals.Chem4WordV3.AddInInfo.ProductAppDataPath);
            navigatorView1.SetOptions(_acmeOptions);

            ActiveApp = app;
            ActiveDoc = doc;
        }

        public Document ActiveDoc
        {
            get { return _activeDoc; }

            set
            {
                _activeDoc = value;
                navigatorView1.ActiveDocument = value;
            }
        }

        private Microsoft.Office.Interop.Word.Application _activeApp;

        public Microsoft.Office.Interop.Word.Application ActiveApp
        {
            get { return _activeApp; }
            set
            {
                if (_activeApp != null)
                {
                    //_activeApp.
                }

                _activeApp = value;
                navigatorView1.ActiveApplication = value;
            }
        }
    }
}