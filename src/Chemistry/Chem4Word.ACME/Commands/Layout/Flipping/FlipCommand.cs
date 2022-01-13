// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Diagnostics;
using Chem4Word.Model2;

namespace Chem4Word.ACME.Commands.Layout.Flipping
{
    public class FlipCommand : BaseCommand
    {
        public FlipCommand(EditController controller) : base(controller)
        {
        }

        public override bool CanExecute(object parameter)
        {
            return EditController.SingleMolSelected;
        }

        public override void Execute(object parameter)
        {
            Debug.Assert(EditController.SelectedItems[0] is Molecule);
            var selMolecule = EditController.SelectedItems[0] as Molecule;

            EditController.FlipMolecule(selMolecule, false, false);
        }
    }
}