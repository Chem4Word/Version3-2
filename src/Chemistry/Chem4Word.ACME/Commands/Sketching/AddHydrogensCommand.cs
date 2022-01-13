// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Linq;
using Chem4Word.Model2;

namespace Chem4Word.ACME.Commands.Sketching
{
    public class AddHydrogensCommand : BaseCommand
    {
        public AddHydrogensCommand(EditController controller) : base(controller)
        {
        }

        public override bool CanExecute(object parameter)
        {
            var mols = EditController.SelectedItems.OfType<Molecule>().ToList();
            var atoms = EditController.SelectedItems.OfType<Atom>().ToList();
            var bonds = EditController.SelectedItems.OfType<Bond>().ToList();
            var nothingSelected = EditController.SelectedItems.Count == 0;

            return nothingSelected || mols.Any() && !atoms.Any() && !bonds.Any();
        }

        public override void Execute(object parameter)
        {
            EditController.AddHydrogens();
        }
    }
}