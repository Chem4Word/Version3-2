// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Chem4Word.Core;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Converters.MDL;

namespace Chem4Word.ACME
{
    /// <summary>
    /// Interaction logic for Display.xaml
    /// </summary>
    public partial class Display : UserControl
    {
        public Display()
        {
            InitializeComponent();
        }

        #region Public Properties

        public ViewModel CurrentViewModel { get; set; }

        #region Chemistry (DependencyProperty)

        public object Chemistry
        {
            get { return (object)GetValue(ChemistryProperty); }
            set { SetValue(ChemistryProperty, value); }
        }

        public static readonly DependencyProperty ChemistryProperty =
            DependencyProperty.Register("Chemistry", typeof(object), typeof(Display),
                                        new FrameworkPropertyMetadata(null,
                                                                      FrameworkPropertyMetadataOptions.AffectsRender
                                                                      | FrameworkPropertyMetadataOptions.AffectsArrange
                                                                      | FrameworkPropertyMetadataOptions.AffectsMeasure,
                                                                      ChemistryChanged));

        #endregion Chemistry (DependencyProperty)

        public Brush BackgroundColor
        {
            get { return (Brush)GetValue(BackgroundColorProperty); }
            set { SetValue(BackgroundColorProperty, value); }
        }

        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.Register("BackgroundColor", typeof(Brush), typeof(Display),
                                        new FrameworkPropertyMetadata(SystemColors.WindowBrush,
                                            FrameworkPropertyMetadataOptions.AffectsRender));

        public bool HighlightActive
        {
            get { return (bool)GetValue(HighlightActiveProperty); }
            set { SetValue(HighlightActiveProperty, value); }
        }

        public static readonly DependencyProperty HighlightActiveProperty =
            DependencyProperty.Register("HighlightActive", typeof(bool), typeof(Display),
                                        new FrameworkPropertyMetadata(true,
                                            FrameworkPropertyMetadataOptions.AffectsRender
                                            | FrameworkPropertyMetadataOptions.AffectsArrange
                                            | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public bool ShowMoleculeGrouping
        {
            get { return (bool)GetValue(ShowMoleculeGroupingProperty); }
            set { SetValue(ShowMoleculeGroupingProperty, value); }
        }

        public static readonly DependencyProperty ShowMoleculeGroupingProperty =
            DependencyProperty.Register("ShowMoleculeGrouping", typeof(bool), typeof(Display),
                                        new FrameworkPropertyMetadata(true,
                                             FrameworkPropertyMetadataOptions.AffectsRender
                                             | FrameworkPropertyMetadataOptions.AffectsArrange
                                             | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public bool ShowAtomsInColour
        {
            get { return (bool)GetValue(ShowAtomsInColourProperty); }
            set { SetValue(ShowAtomsInColourProperty, value); }
        }

        public static readonly DependencyProperty ShowAtomsInColourProperty =
            DependencyProperty.Register("ShowAtomsInColour", typeof(bool), typeof(Display),
                                        new FrameworkPropertyMetadata(true,
                                              FrameworkPropertyMetadataOptions.AffectsRender));

        public bool ShowAllCarbonAtoms
        {
            get { return (bool)GetValue(ShowAllCarbonAtomsProperty); }
            set { SetValue(ShowAllCarbonAtomsProperty, value); }
        }

        public static readonly DependencyProperty ShowAllCarbonAtomsProperty =
            DependencyProperty.Register("ShowAllCarbonAtoms", typeof(bool), typeof(Display),
                                        new FrameworkPropertyMetadata(false,
                                           FrameworkPropertyMetadataOptions.AffectsRender
                                           | FrameworkPropertyMetadataOptions.AffectsArrange
                                           | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public bool ShowImplicitHydrogens
        {
            get { return (bool)GetValue(ShowImplicitHydrogensProperty); }
            set { SetValue(ShowImplicitHydrogensProperty, value); }
        }

        public static readonly DependencyProperty ShowImplicitHydrogensProperty =
            DependencyProperty.Register("ShowImplicitHydrogens", typeof(bool), typeof(Display),
                                        new FrameworkPropertyMetadata(true,
                                          FrameworkPropertyMetadataOptions.AffectsRender
                                          | FrameworkPropertyMetadataOptions.AffectsArrange
                                          | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public bool ShowOverbondedAtoms
        {
            get { return (bool)GetValue(ShowOverbondedAtomsProperty); }
            set { SetValue(ShowOverbondedAtomsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowOverbondedAtoms.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowOverbondedAtomsProperty =
            DependencyProperty.Register("ShowOverbondedAtoms", typeof(bool), typeof(Display), new PropertyMetadata(default(bool)));

        #endregion Public Properties

        #region Public Methods

        public void Clear()
        {
            var model = new Model();
            CurrentViewModel = new ViewModel(model);
            DrawChemistry(CurrentViewModel);
        }

        #endregion Public Methods

        #region Private Methods

        private static void ChemistryChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            if (source is Display display)
            {
                display.HandleDataContextChanged();
            }
        }

        private void HandleDataContextChanged()
        {
            Model chemistryModel = null;

            if (Chemistry is string)
            {
                var data = Chemistry as string;
                if (!string.IsNullOrEmpty(data))
                {
                    if (data.StartsWith("<"))
                    {
                        var conv = new CMLConverter();
                        chemistryModel = conv.Import(data);
                        chemistryModel.EnsureBondLength(20, false);
                    }
                    if (data.Contains("M  END"))
                    {
                        var conv = new SdFileConverter();
                        chemistryModel = conv.Import(data);
                        chemistryModel.EnsureBondLength(20, false);
                    }
                }
            }
            else
            {
                if (Chemistry != null && !(Chemistry is Model))
                {
                    Debugger.Break();
                    throw new ArgumentException($"Object must be of type {nameof(Model)}.");
                }
                chemistryModel = Chemistry as Model;
                if (chemistryModel != null)
                {
                    chemistryModel.EnsureBondLength(20, false);
                }
            }

            //assuming we've got this far, we should have something we can draw
            if (chemistryModel != null)
            {
                if (chemistryModel.TotalAtomsCount > 0)
                {
                    chemistryModel.RescaleForXaml(true, Constants.StandardBondLength);

                    CurrentViewModel = new ViewModel(chemistryModel);
                    CurrentViewModel.SetTextParams(chemistryModel.XamlBondLength);
                    DrawChemistry(CurrentViewModel);
                }
            }
        }

        private void DrawChemistry(ViewModel currentViewModel)
        {
            ChemCanvas.ViewModel = currentViewModel;
        }

        #endregion Private Methods

        #region Private EventHandlers

        /// <summary>
        /// Add this to the OnMouseLeftButtonDown attribute
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UIElementOnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            dynamic clobberedElement = sender;
            UserInteractions.InformUser(clobberedElement.ID);
        }

        #endregion Private EventHandlers
    }
}