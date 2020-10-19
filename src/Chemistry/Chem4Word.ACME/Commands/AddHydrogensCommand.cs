// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Linq;
using Chem4Word.Model2;

namespace Chem4Word.ACME.Commands
{
    public class AddHydrogensCommand : BaseCommand
    {
        public AddHydrogensCommand(EditViewModel vm) : base(vm)
        {
        }

        public override bool CanExecute(object parameter)
        {
            var mols = EditViewModel.SelectedItems.OfType<Molecule>().ToList();
            var atoms = EditViewModel.SelectedItems.OfType<Atom>().ToList();
            var bonds = EditViewModel.SelectedItems.OfType<Bond>().ToList();
            var nothingSelected = EditViewModel.SelectedItems.Count == 0;

            return nothingSelected || mols.Any() && !atoms.Any() && !bonds.Any();
        }

        public override void Execute(object parameter)
        {
            EditViewModel.AddHydrogens();
        }
    }
}