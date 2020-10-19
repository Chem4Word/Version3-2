// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Linq;
using System.Windows;
using Chem4Word.ACME.Utils;
using Chem4Word.Model2;

namespace Chem4Word.ACME.Commands
{
    public class PickElementCommand : BaseCommand
    {
        private readonly EditViewModel _evm;

        public PickElementCommand(EditViewModel vm) : base(vm)
        {
            _evm = vm;
        }

        public override bool CanExecute(object parameter) => true;

        public override void Execute(object parameter)
        {
            PTPopup popupPicker = new PTPopup();
            var mosusePosition = System.Windows.Forms.Control.MousePosition;
            popupPicker.CentrePoint = new Point(mosusePosition.X, mosusePosition.Y);
            UIUtils.ShowDialog(popupPicker, _evm.CurrentEditor);
            var popupPickerSelectedElement = popupPicker.SelectedElement;
            if (popupPickerSelectedElement != null)
            {
                if (!_evm.AtomOptions.Any(ao => ao.Element == popupPickerSelectedElement))
                {
                    var newOption = new AtomOption(popupPickerSelectedElement as Element);
                    _evm.AtomOptions.Add(newOption);
                    _evm.SelectedElement = popupPickerSelectedElement;
                }
            }
        }
    }
}