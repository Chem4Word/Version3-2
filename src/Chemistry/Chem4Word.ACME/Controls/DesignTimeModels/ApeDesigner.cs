// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using Chem4Word.ACME.Entities;
using Chem4Word.Core.Enums;
using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.ACME.Controls.DesignTimeModels
{
    public class ApeDesigner
    {
        // Two Way bindings must have get and set
        // One way bindings only need get

        public bool IsElement { get; set; }

        public bool IsFunctionalGroup { get; set; }

        public Element SelectedElement { get; set; }

        private ElementBase _element;

        public ElementBase Element
        {
            get => _element;
            set
            {
                if (value is Element element)
                {
                    SelectedElement = element;
                }
                else
                {
                    SelectedElement = null;
                }
                _element = value;
            }
        }

        public int? Charge { get; set; }
        public string Isotope { get; set; }

        public bool HasIsotopes { get; }

        public bool ShowCompass { get; }

        public CompassPoints? ExplicitHydrogenPlacement { get; set; }
        public CompassPoints? ExplicitFunctionalGroupPlacement { get; set; }

        // Drop Down Lists
        public List<ChargeValue> Charges { get; private set; }

        public List<IsotopeValue> IsotopeMasses { get; private set; }

        public ApeDesigner()
        {
            IsElement = false;
            IsFunctionalGroup = !IsElement;

            if (IsElement)
            {
                Element = Globals.PeriodicTable.C;
                Charge = -1;

                HasIsotopes = true;
                Isotope = "13";

                ExplicitHydrogenPlacement = CompassPoints.East;

                // For some reason these have to be done after setting the properties
                SetUpCharges();
                SetUpIsotopeMasses();
            }
            else
            {
                Element = Globals.FunctionalGroupsList.Find(fg => fg.Name == "CH2CH2OH");
                ExplicitFunctionalGroupPlacement = CompassPoints.East;
            }

            ShowCompass = true;
        }

        private void SetUpCharges()
        {
            Charges = new List<ChargeValue>();
            for (int charge = -8; charge < 9; charge++)
            {
                Charges.Add(new ChargeValue { Value = charge, Label = charge.ToString() });
            }
        }

        private void SetUpIsotopeMasses()
        {
            IsotopeMasses = new List<IsotopeValue>();

            var e = Element as Element;
            IsotopeMasses.Add(new IsotopeValue { Label = "", Mass = null });
            if (e != null && e.IsotopeMasses != null)
            {
                foreach (int mass in e.IsotopeMasses)
                {
                    IsotopeMasses.Add(new IsotopeValue { Label = mass.ToString(), Mass = mass });
                }
            }
        }
    }
}