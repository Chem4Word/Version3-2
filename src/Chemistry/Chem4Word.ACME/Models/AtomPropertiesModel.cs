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

namespace Chem4Word.ACME.Models
{
    public class AtomPropertiesModel : BaseDialogModel
    {
        private ElementBase _element;
        private int? _charge;
        private string _isotope;
        private bool? _explicitC;
        private bool _isFunctionalGroup;
        private bool _isElement;
        private bool _showCompass;
        private CompassPoints? _explicitHydrogenPlacement;
        private CompassPoints? _fgPlacement;

        public ElementBase AddedElement { get; set; }

        public bool IsFunctionalGroup
        {
            get => _isFunctionalGroup;
            set
            {
                _isFunctionalGroup = value;
                OnPropertyChanged();
            }
        }

        public bool IsElement
        {
            get => _isElement;
            set
            {
                _isElement = value;
                OnPropertyChanged();
            }
        }

        public Model MicroModel { get; set; }

        public bool HasIsotopes
        {
            get { return IsotopeMasses.Count > 1; }
        }

        public Element SelectedElement { get; private set; }

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
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsotopeMasses));
                OnPropertyChanged(nameof(HasIsotopes));
            }
        }

        public int? Charge
        {
            get => _charge;
            set
            {
                _charge = value;
                OnPropertyChanged();
            }
        }

        public CompassPoints? ExplicitHydrogenPlacement
        {
            get => _explicitHydrogenPlacement;
            set
            {
                _explicitHydrogenPlacement = value;
                OnPropertyChanged();
            }
        }

        public CompassPoints? ExplicitFunctionalGroupPlacement
        {
            get => _fgPlacement;
            set
            {
                _fgPlacement = value;
                OnPropertyChanged();
            }
        }

        public string Isotope
        {
            get => _isotope;
            set
            {
                _isotope = value;
                OnPropertyChanged();
            }
        }

        public bool? ExplicitC
        {
            get => _explicitC;
            set
            {
                _explicitC = value;
                OnPropertyChanged();
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

        public List<IsotopeValue> IsotopeMasses
        {
            get
            {
                List<IsotopeValue> temp = new List<IsotopeValue>();

                var e = Element as Element;
                temp.Add(new IsotopeValue { Label = "", Mass = null });
                if (e != null && e.IsotopeMasses != null)
                {
                    foreach (int mass in e.IsotopeMasses)
                    {
                        temp.Add(new IsotopeValue { Label = mass.ToString(), Mass = mass });
                    }
                }

                return temp;
            }
        }

        public object IsNotSingleton { get; set; }

        public bool ShowCompass
        {
            get

            {
                return _showCompass;
            }
            set
            {
                _showCompass = value;
                OnPropertyChanged();
            }
        }
    }
}