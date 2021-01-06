// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using Chem4Word.ACME.Entities;
using Chem4Word.Model2;

namespace Chem4Word.ACME.Models
{
    public class MoleculePropertiesModel : BaseDialogModel
    {
        public Model Data { get; set; }

        public AcmeOptions Options { get; set; }

        public bool ShowAllCarbonAtoms => Options.ShowCarbons;
        public bool ShowImplicitHydrogens => Options.ShowHydrogens;
        public bool ShowAtomsInColour => Options.ColouredAtoms;
        public bool ShowMoleculeGrouping => Options.ShowMoleculeGrouping;

        public List<string> Used1DProperties { get; set; }

        private bool? _showMoleculeBrackets;

        public bool? ShowMoleculeBrackets
        {
            get => _showMoleculeBrackets;
            set
            {
                _showMoleculeBrackets = value;
                OnPropertyChanged();
            }
        }

        private int? _spinMultiplicity;

        public int? SpinMultiplicity
        {
            get => _spinMultiplicity;
            set
            {
                _spinMultiplicity = value;
                OnPropertyChanged();
            }
        }

        private int? _count;

        public int? Count
        {
            get => _count;
            set
            {
                _count = value;
                OnPropertyChanged();
            }
        }

        private int? _charge;

        public int? Charge
        {
            get => _charge;
            set
            {
                _charge = value;
                OnPropertyChanged();
            }
        }

        public List<ChargeValue> MultiplicityValues
        {
            get
            {
                List<ChargeValue> values = new List<ChargeValue>();

                values.Add(new ChargeValue { Value = 0, Label = "(none)" });
                values.Add(new ChargeValue { Value = 2, Label = "•" });
                values.Add(new ChargeValue { Value = 3, Label = "• •" });

                return values;
            }
        }

        public List<ChargeValue> Charges
        {
            get
            {
                List<ChargeValue> charges = new List<ChargeValue>();
                for (int charge = -8; charge < 9; charge++)
                {
                    charges.Add(new ChargeValue { Value = charge, Label = charge.ToString() });
                }

                return charges;
            }
        }
    }
}