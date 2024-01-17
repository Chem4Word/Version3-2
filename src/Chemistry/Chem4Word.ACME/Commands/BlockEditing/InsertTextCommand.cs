// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Controls;
using System;
using System.Windows.Input;

namespace Chem4Word.ACME.Commands.BlockEditing
{
    public class InsertTextCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public string Text { get; set; }

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
            if (parameter != null)
            {
                Editor.Selection.Text = (string)parameter;
            }
            else
            {
                Editor.Selection.Text = Text;
            }
            Editor.Selection.Select(Editor.Selection.End, Editor.Selection.End);
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