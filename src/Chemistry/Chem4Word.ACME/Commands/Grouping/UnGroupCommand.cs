﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Linq;
using Chem4Word.ACME.Enums;
using Chem4Word.Model2;

namespace Chem4Word.ACME.Commands.Grouping
{
    /// <summary>
    /// handles the ungrouping of molecules
    /// </summary>
    public class UnGroupCommand : BaseCommand
    {
        public UnGroupCommand(EditController controller) : base(controller)
        {
        }

        public override bool CanExecute(object parameter)
        {
            return EditController.SelectionType == SelectionTypeCode.Molecule &&
                   EditController.SelectedItems.OfType<Molecule>().All(m => m.IsGrouped);
        }

        public override void Execute(object parameter)
        {
            EditController.UnGroup(EditController.SelectedItems);
        }
    }
}