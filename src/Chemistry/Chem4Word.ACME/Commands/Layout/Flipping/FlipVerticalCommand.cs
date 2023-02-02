// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Utils;
using Chem4Word.Model2;

namespace Chem4Word.ACME.Commands.Layout.Flipping
{
    public class FlipVerticalCommand : FlipCommand
    {
        public FlipVerticalCommand(EditController controller) : base(controller)
        {
        }

        public override void Execute(object parameter)
        {
            var selMolecule = EditController.SelectedItems[0] as Molecule;
            bool flipStereo = KeyboardUtils.HoldingDownShift();
            EditController.FlipMolecule(selMolecule, true, flipStereo);
        }
    }
}