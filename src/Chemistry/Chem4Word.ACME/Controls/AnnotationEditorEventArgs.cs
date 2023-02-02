﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;

namespace Chem4Word.ACME.Controls
{
    public class AnnotationEditorEventArgs : RoutedEventArgs
    {
        public AnnotationEditorExitArgsType Reason { get; set; }
    }
}