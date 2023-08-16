// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Controls;
using System;
using System.Windows.Input;

namespace Chem4Word.ACME.Commands.BlockEditing
{
    public class SuperscriptCommand : ICommand
    {
        public SuperscriptCommand(AnnotationEditor editor)
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
            Editor.ToggleSuperscript(Editor.Selection);
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