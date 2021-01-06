// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Linq;
using Chem4Word.ACME.Enums;
using Chem4Word.Model2;

namespace Chem4Word.ACME.Commands
{
    /// <summary>
    /// handles the ungrouping of molecules
    /// </summary>
    public class UnGroupCommand : BaseCommand
    {
        public UnGroupCommand(EditViewModel vm) : base(vm)
        {
        }

        public override bool CanExecute(object parameter)
        {
            return EditViewModel.SelectionType == SelectionTypeCode.Molecule &&
                   EditViewModel.SelectedItems.OfType<Molecule>().All(m => m.IsGrouped);
        }

        public override void Execute(object parameter)
        {
            EditViewModel.UnGroup(EditViewModel.SelectedItems);
        }
    }
}