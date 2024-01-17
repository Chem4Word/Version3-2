﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Enums;
using System;

namespace Chem4Word.ACME.Commands.Editing
{
    public class CutCommand : BaseCommand
    {
        public CutCommand(EditController controller) : base(controller)
        {
        }

        public override bool CanExecute(object parameter)
        {
            return EditController.SelectionType != SelectionTypeCode.None;
        }

        public override void Execute(object parameter)
        {
            EditController.CutSelection();
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