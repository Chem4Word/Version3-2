﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Linq;
using Chem4Word.Model2;

namespace Chem4Word.ACME.Commands.Layout.Alignment
{
    public class AlignOnlyMoleculesCommand : AlignCommand
    {
        public AlignOnlyMoleculesCommand(EditController controller) : base(controller)
        {

        }
        public override bool CanExecute(object parameter)
        {
            return EditController.SelectionType == Enums.SelectionTypeCode.Molecule && EditController.SelectedItems.Count > 1;
        }
        public override void Execute(object parameter)
        {
            EditController.AlignCentres(EditController.SelectedItems.OfType<ChemistryBase>().ToList());
        }
    }
}