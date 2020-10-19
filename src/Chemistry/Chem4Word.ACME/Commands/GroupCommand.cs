// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Linq;
using Chem4Word.ACME.Enums;
using Chem4Word.Model2;

namespace Chem4Word.ACME.Commands
{
    public class GroupCommand : BaseCommand
    {
        public GroupCommand(EditViewModel vm) : base(vm)
        {
        }

        public override bool CanExecute(object parameter)
        {
            return EditViewModel.SelectionType == SelectionTypeCode.Molecule
                   && EditViewModel.SelectedItems.OfType<Molecule>().Count() > 1;
        }

        public override void Execute(object parameter)
        {
            EditViewModel.Group(EditViewModel.SelectedItems.ToList());
        }
    }
}