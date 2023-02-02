﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace Chem4Word.ACME.Commands.Reactions
{
    public class ClearReactionRolesCommand : BaseCommand
    {
        public ClearReactionRolesCommand(EditController controller) : base(controller)
        {
        }

        public override bool CanExecute(object parameter)
        {
            return EditController.SingleReactionSelected();
        }

        public override void Execute(object parameter)
        {
            EditController.ClearReactionRoles();
        }
    }
}