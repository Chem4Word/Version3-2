// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Chem4Word.ACME.Annotations;
using Chem4Word.ACME.Models;
using Chem4Word.Model2;

namespace Chem4Word.ACME.Controls
{
    /// <summary>
    /// Interaction logic for NamesEditor.xaml
    /// </summary>
    public partial class NamesEditor : UserControl, INotifyPropertyChanged
    {
        private NamesModel _namesModel;

        public NamesModel NamesModel
        {
            get { return _namesModel; }
            set
            {
                _namesModel = value;
                DataContext = _namesModel;
                OnPropertyChanged();
            }
        }

        public NamesEditor()
        {
            InitializeComponent();
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                NamesModel = new NamesModel();
            }
        }

        private void OnDeleteRowClicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button deleteButton)
            {
                if (deleteButton.DataContext is TextualProperty property)
                {
                    Debug.WriteLine($"Deleting {property}");
                    NamesModel.ListOfNames.Remove(property);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}