// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;

namespace Chem4Word.ACME.Commands
{
    public class UndoCommand : BaseCommand
    {
        //private EditViewModel _currentVM;

        public UndoCommand(EditViewModel vm) : base(vm)
        {
        }

        public override bool CanExecute(object parameter)
        {
            return EditViewModel.UndoManager.CanUndo;
        }

        public override void Execute(object parameter)
        {
            EditViewModel.UndoManager.Undo();
        }

        public override event EventHandler CanExecuteChanged;

        public override void RaiseCanExecChanged()
        {
            var args = new EventArgs();

            CanExecuteChanged?.Invoke(this, args);
        }
    }
}