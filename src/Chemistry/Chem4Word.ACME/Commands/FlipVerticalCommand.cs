// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Utils;
using Chem4Word.Model2;

namespace Chem4Word.ACME.Commands
{
    public class FlipVerticalCommand : FlipCommand
    {
        public FlipVerticalCommand(EditViewModel vm) : base(vm)
        {
        }

        public override void Execute(object parameter)
        {
            var selMolecule = EditViewModel.SelectedItems[0] as Molecule;
            bool flipStereo = KeyboardUtils.HoldingDownShift();
            EditViewModel.FlipMolecule(selMolecule, true, flipStereo);
        }
    }
}