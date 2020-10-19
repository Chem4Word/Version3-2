// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Windows.Input;

namespace Chem4Word.ACME.Commands
{
    public abstract class BaseCommand : ICommand
    {
        public EditViewModel EditViewModel { get; set; }

        #region ICommand Implementation

        public abstract bool CanExecute(object parameter);

        public abstract void Execute(object parameter);

        public virtual event EventHandler CanExecuteChanged;

        public virtual void RaiseCanExecChanged()
        {
            if (CanExecuteChanged != null)
            {
                CanExecuteChanged.Invoke(this, new EventArgs());
            }
        }

        #endregion ICommand Implementation

        #region Constructors

        protected BaseCommand(EditViewModel vm)
        {
            EditViewModel = vm;
        }

        #endregion Constructors
    }
}