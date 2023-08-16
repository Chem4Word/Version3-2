// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2;
using System.Linq;

namespace Chem4Word.ACME.Commands.Layout.Alignment
{
    public class AlignOnlyMoleculesCommand : AlignCommand
    {
        public AlignOnlyMoleculesCommand(EditController controller) : base(controller)
        {
        }

        public override bool CanExecute(object parameter)
        {
            return ((EditController.SelectionType & Enums.SelectionTypeCode.Molecule) == Enums.SelectionTypeCode.Molecule
                || (EditController.SelectionType & Enums.SelectionTypeCode.Annotation) == Enums.SelectionTypeCode.Annotation) && EditController.SelectedItems.Count > 1;
        }

        public override void Execute(object parameter)
        {
            EditController.AlignCentres(EditController.SelectedItems.OfType<BaseObject>().ToList());
        }
    }
}