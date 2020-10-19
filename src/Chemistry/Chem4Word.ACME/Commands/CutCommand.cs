// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using Chem4Word.ACME.Enums;

namespace Chem4Word.ACME.Commands
{
    public class CutCommand : BaseCommand
    {
        public CutCommand(EditViewModel vm) : base(vm)
        {
        }

        public override bool CanExecute(object parameter)
        {
            return EditViewModel.SelectionType != SelectionTypeCode.None;
        }

        public override void Execute(object parameter)
        {
            EditViewModel.CutSelection();
        }

        public override void RaiseCanExecChanged()
        {
            if (CanExecuteChanged != null)
            {
                CanExecuteChanged.Invoke(this, new EventArgs());
            }
        }

        public override event EventHandler CanExecuteChanged;
    }
}