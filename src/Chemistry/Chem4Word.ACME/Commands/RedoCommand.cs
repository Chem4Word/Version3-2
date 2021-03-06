﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace Chem4Word.ACME.Commands
{
    public class RedoCommand : BaseCommand
    {
        public RedoCommand(EditViewModel vm) : base(vm)
        {
        }

        public override bool CanExecute(object parameter) => EditViewModel.UndoManager.CanRedo;

        public override void Execute(object parameter)
        {
            EditViewModel.UndoManager.Redo();
        }
    }
}