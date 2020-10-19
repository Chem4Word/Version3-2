// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Diagnostics;
using Chem4Word.Model2;

namespace Chem4Word.ACME.Commands
{
    public class FlipCommand : BaseCommand
    {
        public FlipCommand(EditViewModel vm) : base(vm)
        {
        }

        public override bool CanExecute(object parameter)
        {
            return EditViewModel.SingleMolSelected;
        }

        public override void Execute(object parameter)
        {
            Debug.Assert(EditViewModel.SelectedItems[0] is Molecule);
            var selMolecule = EditViewModel.SelectedItems[0] as Molecule;

            EditViewModel.FlipMolecule(selMolecule, false, false);
        }
    }
}