// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Windows.Input;
using Chem4Word.ACME.Controls;

namespace Chem4Word.ACME.Commands.Block_Editing
{
    public class SubscriptCommand : ICommand
    {
        public SubscriptCommand(AnnotationEditor editor)
        {
            Editor = editor;
        }

        public AnnotationEditor Editor { get; }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            Editor.ToggleSubscript(Editor.Selection);
        }

        public void RaiseCanExecChanged()
        {
            if (CanExecuteChanged != null)
            {
                CanExecuteChanged.Invoke(this, new EventArgs());
            }
        }

        public event EventHandler CanExecuteChanged;
    }
}