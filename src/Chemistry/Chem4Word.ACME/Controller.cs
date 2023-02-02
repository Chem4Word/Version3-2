// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.ComponentModel;
using System.Runtime.CompilerServices;
using Chem4Word.ACME.Annotations;
using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.ACME
{
    public class Controller : INotifyPropertyChanged
    {
        public Controller(Model chemistryModel)
        {
            Model = chemistryModel;

            double xamlBondLength = chemistryModel.XamlBondLength == 0
                ? Globals.DefaultFontSize * 2
                : chemistryModel.XamlBondLength;

            SetTextParams(xamlBondLength);
        }

        public void SetTextParams(double bondLength)
        {
            SymbolSize = bondLength / 2.0d;
            BlockTextSize = SymbolSize;
        }

        #region Properties

        private double _symbolSize;

        public double SymbolSize
        {
            get => _symbolSize;
            set
            {
                _symbolSize = value;
                OnPropertyChanged();
            }
        }

        private double _blockTextSize;

        public double BlockTextSize
        {
            get => _blockTextSize;
            set
            {
                _blockTextSize = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ScriptSize));
            }
        }

        public double ScriptSize
        {
            get => BlockTextSize * ScriptScalingFactor;
        }

        public const double ScriptScalingFactor = 0.6;

        public event PropertyChangedEventHandler PropertyChanged;

        public double Standoff => SymbolSize / 6;
        public Model Model { get; }

        public double SymbolScalingFactor => 1.5;

        #endregion Properties

        #region Methods

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion Methods
    }
}