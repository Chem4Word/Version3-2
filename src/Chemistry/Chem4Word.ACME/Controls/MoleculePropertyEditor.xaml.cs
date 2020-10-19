// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Chem4Word.ACME.Annotations;
using Chem4Word.ACME.Entities;
using Chem4Word.ACME.Models;
using Chem4Word.ACME.Utils;
using Chem4Word.Core;
using Chem4Word.Model2.Converters.CML;

namespace Chem4Word.ACME.Controls
{
    /// <summary>
    /// Interaction logic for MoleculePropertyEditor.xaml
    /// </summary>
    public partial class MoleculePropertyEditor : Window, INotifyPropertyChanged
    {
        private bool _closedByUser;

        private bool _isDirty;

        private bool IsDirty
        {
            get => _isDirty || LabelsEditor.IsDirty;
            set => _isDirty = value;
        }

        private MoleculePropertiesModel _moleculePropertiesModel;

        private MoleculePropertiesModel MpeModel
        {
            get
            {
                return _moleculePropertiesModel;
            }
            set
            {
                _moleculePropertiesModel = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MoleculePropertyEditor()
        {
            InitializeComponent();
        }

        public MoleculePropertyEditor(MoleculePropertiesModel model, AcmeOptions options)
        {
            InitializeComponent();
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                MpeModel = model;
                MpeModel.Options = options;
                DataContext = model;
                MoleculePath.Text = MpeModel.Path;
                LabelsEditor.SetOptions(options);
            }
        }

        private void MoleculePropertyEditor_OnLoaded(object sender, RoutedEventArgs e)
        {
            var point = UIUtils.GetOffScreenPoint();
            Left = point.X;
            Top = point.Y;

            PopulateTabOne();
            PopulateTabTwo();
            SetControlState();
        }

        private void SetControlState()
        {
            var thisMolecule = _moleculePropertiesModel.Data.Molecules.First().Value;

            bool showBrackets = thisMolecule.Count.HasValue && thisMolecule.Count.Value > 0
                             || thisMolecule.FormalCharge.HasValue && thisMolecule.FormalCharge.Value != 0
                             || thisMolecule.SpinMultiplicity.HasValue && thisMolecule.SpinMultiplicity.Value > 1;

            ShowBracketsValue.IsEnabled = !showBrackets;
            IsDirty = true;
        }

        private void MoleculePropertyEditor_OnContentRendered(object sender, EventArgs e)
        {
            // This moves the window to the correct position
            var point = UIUtils.GetOnScreenPoint(_moleculePropertiesModel.Centre, ActualWidth, ActualHeight);
            Left = point.X;
            Top = point.Y;

            InvalidateArrange();
            IsDirty = false;
        }

        private void Save_OnClick(object sender, RoutedEventArgs e)
        {
            _moleculePropertiesModel.Save = true;
            _closedByUser = true;

            GatherData();
            Close();
        }

        private void GatherData()
        {
            // Get data from labels editor
            _moleculePropertiesModel.Data = LabelsEditor.EditedModel;

            // Merge in data from the first tab
            var thisMolecule = _moleculePropertiesModel.Data.Molecules.First().Value;
            thisMolecule.Count = GetCountField();
            thisMolecule.FormalCharge = GetChargeField();
            thisMolecule.SpinMultiplicity = GetMultiplicityField();
            thisMolecule.ShowMoleculeBrackets = GetShowBracketsField();
        }

        private void Close_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void PopulateTabOne()
        {
            Preview.Chemistry = _moleculePropertiesModel.Data.Copy();
        }

        private void PopulateTabTwo()
        {
            if (!LabelsEditor.IsInitialised)
            {
                CMLConverter cc = new CMLConverter();
                LabelsEditor.Used1D = _moleculePropertiesModel.Used1DProperties;
                LabelsEditor.SetOptions(_moleculePropertiesModel.Options);
                LabelsEditor.PopulateTreeView(cc.Export(_moleculePropertiesModel.Data));
            }
        }

        private void CountSpinnerIncreaseButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(CountSpinner.Text))
            {
                CountSpinner.Text = "1";
            }
            else
            {
                int count;
                if (int.TryParse(CountSpinner.Text, out count)
                    && count < 100)
                {
                    count++;
                    CountSpinner.Text = count.ToString();
                }
            }
        }

        private void CountSpinnerDecreaseButton_OnClick(object sender, RoutedEventArgs e)
        {
            int count;
            if (int.TryParse(CountSpinner.Text, out count))
            {
                if (count > 1)
                {
                    count--;
                    CountSpinner.Text = count.ToString();
                }
                else
                {
                    CountSpinner.Text = "";
                }
            }
        }

        private void CountSpinner_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var thisMolecule = _moleculePropertiesModel.Data.Molecules.First().Value;
            var value = GetCountField();
            thisMolecule.Count = value;
            LabelsEditor.SetCount(value);
            Preview.Chemistry = _moleculePropertiesModel.Data.Copy();
            SetControlState();
        }

        private void ChargeValues_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var thisMolecule = _moleculePropertiesModel.Data.Molecules.First().Value;
            var value = GetChargeField();
            thisMolecule.FormalCharge = value;
            LabelsEditor.SetFormalCharge(value);
            Preview.Chemistry = _moleculePropertiesModel.Data.Copy();
            SetControlState();
        }

        private void SpinMultiplicityValues_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var thisMolecule = _moleculePropertiesModel.Data.Molecules.First().Value;
            var value = GetMultiplicityField();
            thisMolecule.SpinMultiplicity = value;
            LabelsEditor.SetMultiplicity(value);
            Preview.Chemistry = _moleculePropertiesModel.Data.Copy();
            SetControlState();
        }

        private void ShowBracketsValue_OnClick(object sender, RoutedEventArgs e)
        {
            var thisMolecule = _moleculePropertiesModel.Data.Molecules.First().Value;
            var value = GetShowBracketsField();
            thisMolecule.ShowMoleculeBrackets = value;
            LabelsEditor.SetShowBrackets(value);
            Preview.Chemistry = _moleculePropertiesModel.Data.Copy();
            SetControlState();
        }

        private bool? GetShowBracketsField()
        {
            bool? result = null;

            if (ShowBracketsValue.IsChecked != null)
            {
                result = ShowBracketsValue.IsChecked;
            }

            return result;
        }

        private int? GetMultiplicityField()
        {
            int? result = null;

            if (SpinMultiplicityValues.SelectedItem is ChargeValue spin
                && spin.Value != 0)
            {
                result = spin.Value;
            }

            return result;
        }

        private int? GetChargeField()
        {
            int? result = null;

            if (ChargeValues.SelectedItem is ChargeValue charge
                && charge.Value != 0)
            {
                result = charge.Value;
            }

            return result;
        }

        private int? GetCountField()
        {
            int? result = null;

            if (!string.IsNullOrEmpty(CountSpinner.Text))
            {
                int value;
                if (int.TryParse(CountSpinner.Text, out value)
                    && value > 0 && value <= 99)
                {
                    result = value;
                }
            }

            return result;
        }

        private void MoleculePropertyEditor_OnClosing(object sender, CancelEventArgs e)
        {
            if (!_closedByUser && IsDirty)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Do you wish to save your changes?");
                sb.AppendLine("  Click 'Yes' to save your changes and exit.");
                sb.AppendLine("  Click 'No' to discard your changes and exit.");
                sb.AppendLine("  Click 'Cancel' to return to the form.");

                DialogResult dr = UserInteractions.AskUserYesNoCancel(sb.ToString());
                switch (dr)
                {
                    case System.Windows.Forms.DialogResult.Yes:
                        GatherData();
                        _moleculePropertiesModel.Save = true;
                        break;

                    case System.Windows.Forms.DialogResult.No:
                        _moleculePropertiesModel.Save = false;
                        break;

                    case System.Windows.Forms.DialogResult.Cancel:
                        e.Cancel = true;
                        break;
                }
            }
        }
    }
}