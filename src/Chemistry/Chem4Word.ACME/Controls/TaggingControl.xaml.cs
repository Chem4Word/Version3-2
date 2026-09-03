// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Models.Chem4Word.Controls.TagControl;
using Chem4Word.Model2.Annotations;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace Chem4Word.ACME.Controls
{
    /// <summary>
    ///     Interaction logic for TaggingControl.xaml
    /// </summary>
    public partial class TaggingControl : UserControl, INotifyPropertyChanged
    {
        public TaggingControl()
        {
            InitializeComponent();

            TagControlModel = new TagControlModel(new ObservableCollection<string>(),
                                                  new ObservableCollection<string>(),
                                                  new SortedDictionary<string, long>());
        }

        public TagControlModel TagControlModel
        {
            get
            {
                return (TagControlModel)GetValue(TagControlModelProperty);
            }
            set
            {
                SetValue(TagControlModelProperty, value);
                GridOfTags.DataContext = TagControlModel;
            }
        }

        public static readonly DependencyProperty TagControlModelProperty = DependencyProperty.Register("TagControlModel",
                                                                                                        typeof(TagControlModel), typeof(TaggingControl));

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            if (PropertyChanged != null)
            {
                Debug.WriteLine($"OnPropertyChanged invoked for {propertyName} from {this}");
            }
        }
    }
}