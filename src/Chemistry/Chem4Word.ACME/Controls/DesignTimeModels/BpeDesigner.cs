// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace Chem4Word.ACME.Controls.DesignTimeModels
{
    public class BpeDesigner
    {
        // Two Way bindings must have get and set
        // One way bindings only need get

        public double BondOrderValue { get; set; }
        public bool IsSingle { get; }
        public bool IsDouble { get; }
        public bool Is1Point5 { get; }
        public bool Is2Point5 { get; }

        public double Angle { get; set; }
        public bool BondAngleChanged { get; }
        public bool BondAngleInvalid { get; }

        public string BondAngle { get; set; }
        public string LengthString { get; }

        public BpeDesigner()
        {
            BondOrderValue = 2;

            if (BondOrderValue <= 3)
            {
                IsSingle = BondOrderValue == 1;
                IsDouble = BondOrderValue == 2;
                Is1Point5 = BondOrderValue == 1.5;
                Is2Point5 = BondOrderValue == 2.5;
            }
            else
            {
                IsSingle = true;
                IsDouble = true;
                Is1Point5 = true;
                Is2Point5 = true;
            }

            Angle = 0;
            BondAngle = Angle.ToString("0.00");
            BondAngleChanged = Angle != 0;
            BondAngleInvalid = Angle < 0;
            LengthString = "20";
        }
    }
}