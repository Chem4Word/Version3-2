// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Chem4Word.ACME.Controls;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.ACME
{
    /// <summary>
    /// Interaction logic for LabelsEditor.xaml
    /// </summary>
    public partial class LabelsEditor : UserControl, IHostedWpfEditor
    {
        public Point TopLeft { get; set; }
        public List<string> Used1D { get; set; }
        public bool IsInitialised { get; set; }

        public bool IsDirty { get; set; }
        public Model EditedModel { get; private set; }

        private string _cml;
        public AcmeOptions Options { get; set; }

        public LabelsEditor()
        {
            Options = new AcmeOptions();
            InitializeComponent();
        }

        public LabelsEditor(AcmeOptions options)
        {
            Options = options;
            InitializeComponent();
        }

        public void SetOptions(AcmeOptions options)
        {
            Options = options;
        }

        public bool ShowAllCarbonAtoms => Options.ShowCarbons;
        public bool ShowImplicitHydrogens => Options.ShowHydrogens;
        public bool ShowAtomsInColour => Options.ColouredAtoms;
        public bool ShowMoleculeGrouping => Options.ShowMoleculeGrouping;

        public bool ShowTopPanel
        {
            get { return (bool)GetValue(ShowTopPanelProperty); }
            set { SetValue(ShowTopPanelProperty, value); }
        }

        public static readonly DependencyProperty ShowTopPanelProperty =
            DependencyProperty.Register("ShowTopPanel", typeof(bool),
                                        typeof(LabelsEditor),
                                        new FrameworkPropertyMetadata(true,
                                                                      FrameworkPropertyMetadataOptions.AffectsArrange
                                                                        | FrameworkPropertyMetadataOptions.AffectsMeasure
                                                                        | FrameworkPropertyMetadataOptions.AffectsRender));

        private void LabelsEditor_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!DesignerProperties.GetIsInDesignMode(this)
                && !string.IsNullOrEmpty(_cml)
                && !IsInitialised)
            {
                PopulateTreeView(_cml);
                TreeView_OnSelectedItemChanged(null, null);
                IsInitialised = true;
            }
        }

        private void TreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Display.Clear();

            LoadNamesEditor(NamesGrid, null);
            LoadNamesEditor(FormulaGrid, null);
            LoadNamesEditor(CaptionsGrid, null);

            var model = new Model();

            if (TreeView.SelectedItem is TreeViewItem item)
            {
                switch (item.Tag)
                {
                    case Model m:
                        Display.Chemistry = m.Copy();
                        break;

                    case Molecule thisMolecule:
                        {
                            model = new Model();
                            var copy = thisMolecule.Copy();
                            model.AddMolecule(copy);
                            copy.Parent = model;

                            if (thisMolecule.Molecules.Count == 0)
                            {
                                LoadNamesEditor(NamesGrid, thisMolecule.Names);
                                LoadNamesEditor(FormulaGrid, thisMolecule.Formulas);
                            }
                            LoadNamesEditor(CaptionsGrid, thisMolecule.Captions);
                            break;
                        }
                }
            }

            Display.Chemistry = model;
        }

        public void PopulateTreeView(string cml)
        {
            _cml = cml;
            var cc = new CMLConverter();
            EditedModel = cc.Import(_cml, Used1D, relabel: false);
            TreeView.Items.Clear();
            bool initialSelectionMade = false;

            if (EditedModel != null)
            {
                OverallConciseFormulaPanel.Children.Add(TextBlockFromFormula(EditedModel.ConciseFormula));

                var root = new TreeViewItem
                {
                    Header = "Structure",
                    Tag = EditedModel
                };
                TreeView.Items.Add(root);
                root.IsExpanded = true;

                AddNodes(root, EditedModel.Molecules.Values);
            }

            SetupNamesEditor(NamesGrid, "Add new Name", OnAddNameClick, "Alternative name(s) for molecule");
            SetupNamesEditor(FormulaGrid, "Add new Formula", OnAddFormulaClick, "Alternative formula for molecule");
            SetupNamesEditor(CaptionsGrid, "Add new Caption", OnAddLabelClick, "Molecule Caption(s)");

            TreeView.Focus();

            TreeView_OnSelectedItemChanged(null, null);

            // Local Function to support recursion
            void AddNodes(TreeViewItem parent, IEnumerable<Molecule> molecules)
            {
                foreach (var molecule in molecules)
                {
                    var tvi = new TreeViewItem();

                    if (molecule.Atoms.Count == 0)
                    {
                        var stackPanel = new StackPanel
                        {
                            Orientation = Orientation.Horizontal
                        };
                        stackPanel.Children.Add(TextBlockFromFormula(molecule.CalculatedFormulaOfChildren, "Group:"));
                        tvi.Header = stackPanel;
                        tvi.Tag = molecule;
                    }
                    else
                    {
                        var stackPanel = new StackPanel
                        {
                            Orientation = Orientation.Horizontal
                        };
                        stackPanel.Children.Add(TextBlockFromFormula(molecule.ConciseFormula));
                        tvi.Header = stackPanel;
                        tvi.Tag = molecule;
                    }

#if DEBUG
                    tvi.ToolTip = molecule.Path;
#endif

                    parent.Items.Add(tvi);
                    tvi.IsExpanded = true;
                    if (!initialSelectionMade)
                    {
                        tvi.IsSelected = true;
                        initialSelectionMade = true;
                    }

                    molecule.Captions.CollectionChanged += OnCollectionChanged;
                    foreach (var property in molecule.Captions)
                    {
                        property.PropertyChanged += OnTextualPropertyChanged;
                    }
                    molecule.Formulas.CollectionChanged += OnCollectionChanged;
                    foreach (var property in molecule.Formulas)
                    {
                        property.PropertyChanged += OnTextualPropertyChanged;
                    }
                    molecule.Names.CollectionChanged += OnCollectionChanged;
                    foreach (var property in molecule.Names)
                    {
                        property.PropertyChanged += OnTextualPropertyChanged;
                    }

                    AddNodes(tvi, molecule.Molecules.Values);
                }
            }
        }

        public void SetCount(int? value)
        {
            if (EditedModel != null)
            {
                var parent = EditedModel.Molecules.First().Value;
                parent.Count = value;
                TreeView_OnSelectedItemChanged(null, null);
            }
        }

        public void SetFormalCharge(int? value)
        {
            if (EditedModel != null)
            {
                var parent = EditedModel.Molecules.First().Value;
                parent.FormalCharge = value;
                TreeView_OnSelectedItemChanged(null, null);
            }
        }

        public void SetMultiplicity(int? value)
        {
            if (EditedModel != null)
            {
                var parent = EditedModel.Molecules.First().Value;
                parent.SpinMultiplicity = value;
                TreeView_OnSelectedItemChanged(null, null);
            }
        }

        public void SetShowBrackets(bool? value)
        {
            if (EditedModel != null)
            {
                var parent = EditedModel.Molecules.First().Value;
                parent.ShowMoleculeBrackets = value;
                TreeView_OnSelectedItemChanged(null, null);
            }
        }

        private void OnAddFormulaClick(object sender, RoutedEventArgs e)
        {
            if (TreeView.SelectedItem is TreeViewItem treeViewItem
                && treeViewItem.Tag is Molecule molecule)
            {
                molecule.Formulas.Add(new TextualProperty
                {
                    Id = molecule.GetNextId(molecule.Formulas, "f"),
                    FullType = CMLConstants.ValueChem4WordFormula,
                    Value = "?",
                    CanBeDeleted = true
                });
                FormulaGrid.ScrollViewer.ScrollToEnd();
            }
        }

        private void OnAddNameClick(object sender, RoutedEventArgs e)
        {
            if (TreeView.SelectedItem is TreeViewItem treeViewItem
                && treeViewItem.Tag is Molecule molecule)
            {
                molecule.Names.Add(new TextualProperty
                {
                    Id = molecule.GetNextId(molecule.Names, "n"),
                    FullType = CMLConstants.ValueChem4WordSynonym,
                    Value = "?",
                    CanBeDeleted = true
                });
                NamesGrid.ScrollViewer.ScrollToEnd();
            }
        }

        private void OnAddLabelClick(object sender, RoutedEventArgs e)
        {
            if (TreeView.SelectedItem is TreeViewItem treeViewItem
                && treeViewItem.Tag is Molecule molecule)
            {
                molecule.Captions.Add(new TextualProperty
                {
                    Id = molecule.GetNextId(molecule.Captions, "l"),
                    FullType = CMLConstants.ValueChem4WordCaption,
                    Value = "?",
                    CanBeDeleted = true
                });
                CaptionsGrid.ScrollViewer.ScrollToEnd();
            }
        }

        private void OnTextualPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            IsDirty = true;
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            IsDirty = true;
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (TextualProperty item in e.NewItems)
                {
                    item.PropertyChanged += OnTextualPropertyChanged;
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (TextualProperty item in e.OldItems)
                {
                    item.PropertyChanged -= OnTextualPropertyChanged;
                }
            }
        }

        private void SetupNamesEditor(NamesEditor namesEditor, string buttonCaption, RoutedEventHandler routedEventHandler, string toolTip)
        {
            namesEditor.AddButtonCaption.Text = buttonCaption;
            namesEditor.AddButton.ToolTip = toolTip;
            // Remove existing handler if present (NB: -= should never crash)
            namesEditor.AddButton.Click -= routedEventHandler;
            namesEditor.AddButton.Click += routedEventHandler;
            namesEditor.AddButton.IsEnabled = false;
        }

        private void LoadNamesEditor(NamesEditor namesEditor, ObservableCollection<TextualProperty> data)
        {
            namesEditor.AddButton.IsEnabled = data != null;
            namesEditor.NamesModel.ListOfNames = data;
        }

        // Copied from $\src\Chem4Word.V3\Navigator\FormulaBlock.cs
        // Ought to be made into common routine
        // Refactor into common code [MAW] ...
        private TextBlock TextBlockFromFormula(string formula, string prefix = null)
        {
            var textBlock = new TextBlock();

            if (!string.IsNullOrEmpty(prefix))
            {
                // Add in the new element
                Run run = new Run($"{prefix} ");
                textBlock.Inlines.Add(run);
            }

            var parts = FormulaHelper.ParseFormulaIntoParts(formula);
            foreach (MoleculeFormulaPart formulaPart in parts)
            {
                // Add in the new element
                Run atom = new Run(formulaPart.Element);
                textBlock.Inlines.Add(atom);

                if (formulaPart.Count > 1)
                {
                    var subs = new Run(formulaPart.Count.ToString())
                    {
                        BaselineAlignment = BaselineAlignment.Subscript
                    };

                    subs.FontSize = subs.FontSize - 2;
                    textBlock.Inlines.Add(subs);
                }
            }

            return textBlock;
        }
    }
}