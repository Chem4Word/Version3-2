﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Linq;
using Chem4Word.Model2;

namespace Chem4Word.ACME.Commands.Layout.Alignment
{
    public class AlignRightsCommand : AlignOnlyMoleculesCommand
    {
        public AlignRightsCommand(EditController controller) : base(controller)
        {
        }

        public override void Execute(object parameter)
        {
            EditController.AlignRights(EditController.SelectedItems.OfType<Molecule>().ToList());
        }
    }
}