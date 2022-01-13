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
    public class InsertTextCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public string Text { get; }

        public AnnotationEditor Editor { get; }

        public InsertTextCommand(AnnotationEditor editor, string textToInsert)
        {
            Editor = editor;
            Text = textToInsert;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            Editor.Selection.Text = Text;
        }

        public void RaiseCanExecChanged()
        {
            if (CanExecuteChanged != null)
            {
                CanExecuteChanged.Invoke(this, new EventArgs());
            }
        }
    }
}