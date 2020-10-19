// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.ACME
{
    public class ViewModel
    {
        public ViewModel(Model chemistryModel)
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
        }

        #region Properties

        public double SymbolSize { get; set; }

        public const double ScriptScalingFactor = 0.6;
        public double Standoff => SymbolSize / 6;
        public Model Model { get; }

        #endregion Properties
    }
}