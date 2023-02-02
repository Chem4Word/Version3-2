// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.ComponentModel;
using System.Runtime.CompilerServices;
using Chem4Word.Model2.Annotations;

namespace Chem4Word.ACME.Models
{
    public class SettingsModel : INotifyPropertyChanged
    {
        private double _currentBondLength;

        public double CurrentBondLength
        {
            get
            {
                return _currentBondLength;
            }
            set
            {
                _currentBondLength = value;
                OnPropertyChanged();
            }
        }

        private bool _showImplicitHydrogens;

        public bool ShowImplicitHydrogens
        {
            get => _showImplicitHydrogens;
            set
            {
                _showImplicitHydrogens = value;
                OnPropertyChanged();
            }
        }

        private bool _showAtomsInColour;

        public bool ShowAtomsInColour
        {
            get => _showAtomsInColour;
            set
            {
                _showAtomsInColour = value;
                OnPropertyChanged();
            }
        }

        private bool _showAllCarbonAtoms;

        public bool ShowAllCarbonAtoms
        {
            get => _showAllCarbonAtoms;
            set
            {
                _showAllCarbonAtoms = value;
                OnPropertyChanged();
            }
        }

        private bool _showMoleculeGroups;

        public bool ShowMoleculeGroups
        {
            get => _showMoleculeGroups;
            set
            {
                _showMoleculeGroups = value;
                OnPropertyChanged();
            }
        }

        public string SettingsPath { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}