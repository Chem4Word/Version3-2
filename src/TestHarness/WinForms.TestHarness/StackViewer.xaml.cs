// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Controls;
using Chem4Word.ACME;

namespace WinForms.TestHarness
{
    /// <summary>
    /// Interaction logic for StackViewer.xaml
    /// </summary>
    public partial class StackViewer : UserControl
    {
        public StackViewer()
        {
            InitializeComponent();
            SetOptions(new AcmeOptions());
        }

        public StackViewer(AcmeOptions options)
        {
            InitializeComponent();
            SetOptions(options);
        }

        public void SetOptions(AcmeOptions options)
        {
            ShowAtomsInColour = options.ColouredAtoms;
            ShowAllCarbonAtoms = options.ShowCarbons;
            ShowImplicitHydrogens = options.ShowHydrogens;
            ShowMoleculeGrouping = options.ShowMoleculeGrouping;
        }

        public bool ShowMoleculeGrouping
        {
            get { return (bool)GetValue(ShowMoleculeGroupingProperty); }
            set { SetValue(ShowMoleculeGroupingProperty, value); }
        }

        public static readonly DependencyProperty ShowMoleculeGroupingProperty =
            DependencyProperty.Register("ShowMoleculeGrouping", typeof(bool), typeof(StackViewer), new FrameworkPropertyMetadata(true,
                                                                                                                                 FrameworkPropertyMetadataOptions.AffectsRender
                                                                                                                                 | FrameworkPropertyMetadataOptions.AffectsArrange
                                                                                                                                 | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public bool ShowAtomsInColour
        {
            get { return (bool)GetValue(ShowAtomsInColourProperty); }
            set { SetValue(ShowAtomsInColourProperty, value); }
        }

        public static readonly DependencyProperty ShowAllCarbonAtomsProperty =
            DependencyProperty.Register("ShowAllCarbonAtoms", typeof(bool), typeof(StackViewer), new FrameworkPropertyMetadata(true,
                                                                                                                               FrameworkPropertyMetadataOptions.AffectsRender
                                                                                                                               | FrameworkPropertyMetadataOptions.AffectsArrange
                                                                                                                               | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public bool ShowAllCarbonAtoms
        {
            get { return (bool)GetValue(ShowAllCarbonAtomsProperty); }
            set { SetValue(ShowAllCarbonAtomsProperty, value); }
        }

        public static readonly DependencyProperty ShowAtomsInColourProperty =
            DependencyProperty.Register("ShowAtomsInColour", typeof(bool), typeof(StackViewer), new FrameworkPropertyMetadata(true,
                                                                                                                              FrameworkPropertyMetadataOptions.AffectsRender
                                                                                                                              | FrameworkPropertyMetadataOptions.AffectsArrange
                                                                                                                              | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public bool ShowImplicitHydrogens
        {
            get { return (bool)GetValue(ShowImplicitHydrogensProperty); }
            set { SetValue(ShowImplicitHydrogensProperty, value); }
        }

        public static readonly DependencyProperty ShowImplicitHydrogensProperty =
            DependencyProperty.Register("ShowImplicitHydrogens", typeof(bool), typeof(StackViewer), new FrameworkPropertyMetadata(true,
                                                                                                                                  FrameworkPropertyMetadataOptions.AffectsRender
                                                                                                                                  | FrameworkPropertyMetadataOptions.AffectsArrange
                                                                                                                                  | FrameworkPropertyMetadataOptions.AffectsMeasure));
    }
}