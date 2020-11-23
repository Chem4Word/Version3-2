﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Windows;
using Chem4Word.ACME.Models;

namespace WinForms.TestLibrary.Wpf
{
    public class NewNavigatorViewModel : DependencyObject
    {
        public ObservableCollection<ChemistryObject> NavigatorItems { get; }

        public NewNavigatorViewModel()
        {
            NavigatorItems = new ObservableCollection<ChemistryObject>();
        }
    }
}