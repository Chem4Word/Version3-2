// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Diagnostics;

namespace Chem4Word.ACME.Commands
{
    public class AddAtomCommand : BaseCommand
    {
        #region ICommand Implementation

        public override bool CanExecute(object parameter)
        {
            return false;
        }

        public override void Execute(object parameter)
        {
            Debugger.Break();
        }

        public override void RaiseCanExecChanged()
        {
            if (CanExecuteChanged != null)
            {
                CanExecuteChanged.Invoke(this, new EventArgs());
            }
        }

        public override event EventHandler CanExecuteChanged;

        #endregion ICommand Implementation

        #region Constructors

        public AddAtomCommand(EditViewModel vm) : base(vm)
        {
        }

        #endregion Constructors
    }
}