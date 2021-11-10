// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Linq;
using Chem4Word.Model2;

namespace Chem4Word.ACME.Commands.Layout.Alignment
{
    public class AlignCommand : BaseCommand
    {
        public AlignCommand(EditController controller) : base(controller)
        {
        }

        public override bool CanExecute(object parameter)
        {
            return EditController.SelectedItems.Where(m => m is Molecule).Count() > 1;
        }

        public override void Execute(object parameter)
        {
        }
    }
}