// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Chem4Word.ACME.Enums;
using Chem4Word.ACME.Models;
using Chem4Word.Core.UI.Wpf;

namespace Chem4Word.ACME.Controls
{
    /// <summary>
    /// Interaction logic for ChemistryItem.xaml
    /// </summary>
    public partial class ChemistryItem : UserControl
    {
        public ChemistryItem()
        {
            InitializeComponent();
        }

        // Source of Event Bubbling Example https://www.stevefenton.co.uk/2012/09/wpf-bubbling-a-command-from-a-child-view/

        // This defines the custom event
        public static readonly RoutedEvent ChemistryItemButtonClickEvent = EventManager.RegisterRoutedEvent(
            "ChemistryItemButtonClick", // Event name
            RoutingStrategy.Bubble,         // Bubble means the event will bubble up through the tree
            typeof(RoutedEventHandler),     // The event type
            typeof(ChemistryItem)); // Belongs to ChemistryItem

        // Allows add and remove of event handlers to handle the custom event
        public event RoutedEventHandler ChemistryItemButtonClick
        {
            add => AddHandler(ChemistryItemButtonClickEvent, value);
            remove => RemoveHandler(ChemistryItemButtonClickEvent, value);
        }

        public double DisplayWidth
        {
            get => (double)GetValue(DisplayWidthProperty);
            set => SetValue(DisplayWidthProperty, value);
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DisplayWidthProperty =
            DependencyProperty.Register("DisplayWidth", typeof(double), typeof(ChemistryItem),
                                        new FrameworkPropertyMetadata(double.NaN,
                                                                      FrameworkPropertyMetadataOptions.AffectsRender
                                                                      | FrameworkPropertyMetadataOptions.AffectsArrange
                                                                      | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public double DisplayHeight
        {
            get => (double)GetValue(DisplayHeightProperty);
            set => SetValue(DisplayHeightProperty, value);
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DisplayHeightProperty =
            DependencyProperty.Register("DisplayHeight", typeof(double), typeof(ChemistryItem),
                                        new FrameworkPropertyMetadata(double.NaN,
                                                                      FrameworkPropertyMetadataOptions.AffectsRender
                                                                      | FrameworkPropertyMetadataOptions.AffectsArrange
                                                                      | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public ChemistryItemMode ItemMode
        {
            get => (ChemistryItemMode)GetValue(ItemModeProperty);
            set => SetValue(ItemModeProperty, value);
        }

        public static readonly DependencyProperty ItemModeProperty =
            DependencyProperty.Register("ItemMode", typeof(ChemistryItemMode), typeof(ChemistryItem),
                                        new FrameworkPropertyMetadata(ChemistryItemMode.NotSet,
                                                                      FrameworkPropertyMetadataOptions.AffectsRender
                                                                      | FrameworkPropertyMetadataOptions.AffectsArrange
                                                                      | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public bool ShowMoleculeGrouping
        {
            get => (bool)GetValue(ShowMoleculeGroupingProperty);
            set => SetValue(ShowMoleculeGroupingProperty, value);
        }

        public static readonly DependencyProperty ShowMoleculeGroupingProperty =
            DependencyProperty.Register("ShowMoleculeGrouping", typeof(bool), typeof(ChemistryItem),
                                        new FrameworkPropertyMetadata(true,
                                             FrameworkPropertyMetadataOptions.AffectsRender
                                             | FrameworkPropertyMetadataOptions.AffectsArrange
                                             | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public bool ShowAtomsInColour
        {
            get => (bool)GetValue(ShowAtomsInColourProperty);
            set => SetValue(ShowAtomsInColourProperty, value);
        }

        public static readonly DependencyProperty ShowAtomsInColourProperty =
            DependencyProperty.Register("ShowAtomsInColour", typeof(bool), typeof(ChemistryItem),
                                        new FrameworkPropertyMetadata(true,
                                              FrameworkPropertyMetadataOptions.AffectsRender));

        public bool ShowAllCarbonAtoms
        {
            get => (bool)GetValue(ShowAllCarbonAtomsProperty);
            set => SetValue(ShowAllCarbonAtomsProperty, value);
        }

        public static readonly DependencyProperty ShowAllCarbonAtomsProperty =
            DependencyProperty.Register("ShowAllCarbonAtoms", typeof(bool), typeof(ChemistryItem),
                                        new FrameworkPropertyMetadata(false,
                                           FrameworkPropertyMetadataOptions.AffectsRender
                                           | FrameworkPropertyMetadataOptions.AffectsArrange
                                           | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public bool ShowImplicitHydrogens
        {
            get => (bool)GetValue(ShowImplicitHydrogensProperty);
            set => SetValue(ShowImplicitHydrogensProperty, value);
        }

        public static readonly DependencyProperty ShowImplicitHydrogensProperty =
            DependencyProperty.Register("ShowImplicitHydrogens", typeof(bool), typeof(ChemistryItem),
                                        new FrameworkPropertyMetadata(true,
                                          FrameworkPropertyMetadataOptions.AffectsRender
                                          | FrameworkPropertyMetadataOptions.AffectsArrange
                                          | FrameworkPropertyMetadataOptions.AffectsMeasure));

        private void OnItemButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button
                && DataContext is ChemistryObject chemistryObject)
            {
                var eventArgs = new RoutedEventArgs(ChemistryItemButtonClickEvent);

                var wpfEventArgs = new WpfEventArgs
                {
                    Button = button.Tag.ToString()
                };

                if (ItemMode == ChemistryItemMode.Navigator)
                {
                    wpfEventArgs.OutputValue = $"Tag={chemistryObject.CustomControlTag}";
                }
                else
                {
                    wpfEventArgs.OutputValue = $"Id={chemistryObject.Id}";
                }

                eventArgs.Source = wpfEventArgs;
                RaiseEvent(eventArgs);
            }
        }

        private void CheckBox_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox
                && DataContext is ChemistryObject chemistryObject)
            {
                if (checkBox.IsChecked != null)
                {
                    chemistryObject.IsChecked = checkBox.IsChecked.Value;
                }

                var eventArgs = new RoutedEventArgs(ChemistryItemButtonClickEvent);

                var wpfEventArgs = new WpfEventArgs
                {
                    Button = "CheckBox",
                    OutputValue = $"Id={chemistryObject.Id}"
                };

                eventArgs.Source = wpfEventArgs;
                RaiseEvent(eventArgs);
            }
        }

        private void AcmeDisplay_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ItemMode == ChemistryItemMode.Catalogue
                && sender is Display
                && DataContext is ChemistryObject chemistryObject)
            {
                var eventArgs = new RoutedEventArgs(ChemistryItemButtonClickEvent);

                var wpfEventArgs = new WpfEventArgs
                {
                    Button = "DisplayDoubleClick",
                    OutputValue = $"Id={chemistryObject.Id}"
                };

                eventArgs.Source = wpfEventArgs;
                RaiseEvent(eventArgs);
            }
        }
    }
}
