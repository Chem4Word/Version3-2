// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Chem4Word.ACME.Models;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Wpf;
using Chem4Word.Model2.Annotations;
using IChem4Word.Contracts;
using UserControl = System.Windows.Controls.UserControl;

namespace Chem4Word.ACME.Controls
{
    /// <summary>
    ///     Interaction logic for UserSettings.xaml
    /// </summary>
    public partial class AcmeSettings : UserControl, INotifyPropertyChanged
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public IChem4WordTelemetry Telemetry { get; set; }

        public Point TopLeft { get; set; }

        public event EventHandler<WpfEventArgs> OnButtonClick;

        private AcmeOptions _acmeOptions;

        public AcmeOptions AcmeOptions
        {
            get { return _acmeOptions; }
            set
            {
                if (value != null)
                {
                    _acmeOptions = value;

                    var model = new SettingsModel();

                    model.CurrentBondLength = (double)_acmeOptions.BondLength;

                    model.ShowMoleculeGroups = _acmeOptions.ShowMoleculeGrouping;
                    model.ShowImplicitHydrogens = _acmeOptions.ShowHydrogens;
                    model.ShowAtomsInColour = _acmeOptions.ColouredAtoms;
                    model.ShowAllCarbonAtoms = _acmeOptions.ShowCarbons;
                    model.SettingsPath = FileSystemHelper.GetWritablePath(_acmeOptions.SettingsPath);

                    SettingsModel = model;

#if !DEBUG
                    DebugTab.Visibility = Visibility.Collapsed;
#endif

                    DataContext = SettingsModel;
                }
            }
        }

        private SettingsModel _model;

        public SettingsModel SettingsModel
        {
            get { return _model; }
            set
            {
                _model = value;
                OnPropertyChanged();
            }
        }

        private bool _loading;

        public AcmeSettings()
        {
            _loading = true;
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Defaults_OnClick(object sender, RoutedEventArgs e)
        {
            _acmeOptions.RestoreDefaults();

            SettingsModel.CurrentBondLength = AcmeOptions.BondLength;
            SettingsModel.ShowMoleculeGroups = AcmeOptions.ShowMoleculeGrouping;
            SettingsModel.ShowAtomsInColour = AcmeOptions.ColouredAtoms;
            SettingsModel.ShowImplicitHydrogens = AcmeOptions.ShowHydrogens;
            SettingsModel.ShowAllCarbonAtoms = AcmeOptions.ShowCarbons;

            _acmeOptions.Dirty = true;

            OnPropertyChanged(nameof(SettingsModel));
        }

        private void Cancel_OnClick(object sender, RoutedEventArgs e)
        {
            AcmeOptions.Dirty = false;
            WpfEventArgs args = new WpfEventArgs();
            args.Button = "CANCEL";
            args.OutputValue = "";
            OnButtonClick?.Invoke(this, args);
        }

        private void Save_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            // Copy current model values to options before saving
            AcmeOptions.BondLength = (int)SettingsModel.CurrentBondLength;
            AcmeOptions.ShowMoleculeGrouping = SettingsModel.ShowMoleculeGroups;
            AcmeOptions.ShowHydrogens = SettingsModel.ShowImplicitHydrogens;
            AcmeOptions.ColouredAtoms = SettingsModel.ShowAtomsInColour;
            AcmeOptions.ShowCarbons = SettingsModel.ShowAllCarbonAtoms;

            AcmeOptions.Save();
            if (AcmeOptions.Errors.Any())
            {
                Telemetry.Write(module, "Exception", string.Join(Environment.NewLine, AcmeOptions.Errors));
                AcmeOptions.Errors = new List<string>();
            }
            AcmeOptions.Dirty = false;

            WpfEventArgs args = new WpfEventArgs();
            args.Button = "SAVE";
            args.OutputValue = "";
            OnButtonClick?.Invoke(this, args);
        }

        private void DefaultBondLength_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AcmeOptions.BondLength = (int)SettingsModel.CurrentBondLength;
            if (!_loading)
            {
                AcmeOptions.Dirty = true;
            }
        }

        private void UserSettings_OnLoaded(object sender, RoutedEventArgs e)
        {
            _loading = false;
            AcmeOptions.Dirty = false;
        }

        private void ShowAtomsInColour_OnClick(object sender, RoutedEventArgs e)
        {
            if (!_loading)
            {
                AcmeOptions.ColouredAtoms = SettingsModel.ShowAtomsInColour;
                AcmeOptions.Dirty = true;
            }
        }

        private void ShowImplicitHydrogens_OnClick(object sender, RoutedEventArgs e)
        {
            if (!_loading)
            {
                AcmeOptions.ShowHydrogens = SettingsModel.ShowImplicitHydrogens;
                AcmeOptions.Dirty = true;
            }
        }

        private void ShowAllCarbonAtoms_OnClick(object sender, RoutedEventArgs e)
        {
            if (!_loading)
            {
                AcmeOptions.ShowCarbons = SettingsModel.ShowAllCarbonAtoms;
                AcmeOptions.Dirty = true;
            }
        }

        private void ShowMoleculeGroups_OnClick(object sender, RoutedEventArgs e)
        {
            if (!_loading)
            {
                AcmeOptions.ShowMoleculeGrouping = SettingsModel.ShowMoleculeGroups;
                AcmeOptions.Dirty = true;
            }
        }
    }
}