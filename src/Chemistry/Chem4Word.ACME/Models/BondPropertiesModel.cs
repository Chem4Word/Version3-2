// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using Chem4Word.ACME.Controls;

namespace Chem4Word.ACME.Models
{
    public class BondPropertiesModel : BaseDialogModel
    {
        public bool IsDirty { get; private set; }

        public bool PlacementChanged { get; private set; }

        private DoubleBondType _doubleBondChoice;

        public DoubleBondType DoubleBondChoice
        {
            get => _doubleBondChoice;
            set
            {
                _doubleBondChoice = value;
                IsDirty = true;
                PlacementChanged = true;
                OnPropertyChanged(nameof(PlacementChanged));
            }
        }

        public bool StereoChanged { get; private set; }

        private SingleBondType _singleBondChoice;

        public SingleBondType SingleBondChoice
        {
            get => _singleBondChoice;
            set
            {
                _singleBondChoice = value;
                IsDirty = true;
                StereoChanged = true;
                OnPropertyChanged(nameof(StereoChanged));
            }
        }

        public void ClearFlags()
        {
            IsDirty = false;
            StereoChanged = false;
            PlacementChanged = false;
            BondOrderChanged = false;
            BondAngleChanged = false;
            BondAngleInvalid = false;
        }

        public bool BondAngleChanged { get; private set; }
        public bool BondAngleInvalid { get; private set; }

        private string _bondAngle;

        public string BondAngle
        {
            get => _bondAngle;
            set
            {
                _bondAngle = value;
                ValidateBondAngle(value);
                IsDirty = true;
            }
        }

        public void ValidateBondAngle(string value)
        {
            bool invalid = true;

            double angle;
            if (double.TryParse(value, out angle))
            {
                if (angle >= -180 && angle <= 180)
                {
                    invalid = false;
                    if (Math.Abs(Angle - angle) >= 0.005)
                    {
                        BondAngleChanged = true;
                        OnPropertyChanged(nameof(BondAngleChanged));
                    }
                    else
                    {
                        BondAngleChanged = false;
                        OnPropertyChanged(nameof(BondAngleChanged));
                    }
                }
            }

            BondAngleInvalid = invalid;
            OnPropertyChanged(nameof(BondAngleInvalid));

            IsDirty = true;
        }

        public double Angle { get; set; }
        public string AngleString => $"{Angle:N2}";
        public double Length { get; set; }
        public string LengthString => $"{Length:N2}";

        public bool IsSingle { get; set; }
        public bool IsDouble { get; set; }

        public bool Is1Point5 { get; set; }
        public bool Is2Point5 { get; set; }

        public bool BondOrderChanged { get; private set; }
        private double _bondOrderValue;

        public double BondOrderValue
        {
            get { return _bondOrderValue; }
            set
            {
                if (value != _bondOrderValue)
                {
                    _bondOrderValue = value;

                    IsSingle = value == 1;
                    IsDouble = value == 2;
                    Is1Point5 = value == 1.5;
                    Is2Point5 = value == 2.5;

                    OnPropertyChanged(nameof(IsSingle));
                    OnPropertyChanged(nameof(IsDouble));
                    OnPropertyChanged(nameof(Is1Point5));
                    OnPropertyChanged(nameof(Is2Point5));

                    if (IsSingle)
                    {
                        SingleBondChoice = SingleBondType.None;
                        OnPropertyChanged(nameof(SingleBondChoice));
                    }

                    if (IsDouble | Is1Point5 | Is2Point5)
                    {
                        DoubleBondChoice = DoubleBondType.Auto;
                        OnPropertyChanged(nameof(DoubleBondChoice));
                    }

                    OnPropertyChanged();
                    IsDirty = true;
                    BondOrderChanged = true;
                    OnPropertyChanged(nameof(BondOrderChanged));
                }
            }
        }
    }
}