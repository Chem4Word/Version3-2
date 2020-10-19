// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Runtime.InteropServices;
using System.Windows.Controls;
using Chem4Word.ACME;
using Microsoft.Office.Interop.Word;
using Application = Microsoft.Office.Interop.Word.Application;

namespace Chem4Word.Navigator
{
    /// <summary>
    /// Interaction logic for NavigatorView.xaml
    /// </summary>
    public partial class NavigatorView : UserControl
    {
        private Application _activeApplication;
        private Document _activeDoc;
        private AcmeOptions _options;

        public NavigatorView()
        {
            InitializeComponent();
            _options = new AcmeOptions();
        }

        public void SetOptions(AcmeOptions options)
        {
            _options = options;
        }

        public bool ShowAllCarbonAtoms => _options.ShowCarbons;
        public bool ShowImplicitHydrogens => _options.ShowHydrogens;
        public bool ShowAtomsInColour => _options.ColouredAtoms;
        public bool ShowMoleculeGrouping => _options.ShowMoleculeGrouping;

        public Application ActiveApplication
        {
            get { return _activeApplication; }
            set
            {
                _activeApplication = value;
            }
        }

        public Document ActiveDocument
        {
            get { return _activeDoc; }
            set
            {
                _activeDoc = value;
                try
                {
                    if (_activeDoc != null)
                    {
                        NavigatorViewModel nvm = new NavigatorViewModel(_activeDoc);
                        this.DataContext = nvm;
                    }
                    else
                    {
                        this.DataContext = null;
                    }
                }
                catch (COMException) //document not open
                {
                    this.DataContext = null;
                }
            }
        }
    }
}