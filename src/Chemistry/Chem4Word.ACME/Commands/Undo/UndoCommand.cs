// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;

namespace Chem4Word.ACME.Commands.Undo
{
    public class UndoCommand : BaseCommand
    {
        public UndoCommand(EditController controller) : base(controller)
        {
        }

        public override bool CanExecute(object parameter) => EditController.UndoManager.CanUndo;

        public override void Execute(object parameter)
        {
            EditController.UndoManager.Undo();
        }

        public override event EventHandler CanExecuteChanged;

        public override void RaiseCanExecChanged()
        {
            var args = new EventArgs();

            CanExecuteChanged?.Invoke(this, args);
        }
    }
}