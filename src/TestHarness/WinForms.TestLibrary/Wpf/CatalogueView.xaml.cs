using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Chem4Word.ACME;
using Chem4Word.ACME.Models;
using Chem4Word.Core.UI.Wpf;

namespace WinForms.TestLibrary.Wpf
{
    /// <summary>
    /// Interaction logic for CatalogueView.xaml
    /// </summary>
    public partial class CatalogueView : UserControl
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private AcmeOptions _options;

        private int _filteredItems;
        private int _checkedItems;
        private int _itemCount;

        public CatalogueView()
        {
            InitializeComponent();
        }

        public bool ShowAllCarbonAtoms => _options.ShowCarbons;
        public bool ShowImplicitHydrogens => _options.ShowHydrogens;
        public bool ShowAtomsInColour => _options.ColouredAtoms;
        public bool ShowMoleculeGrouping => _options.ShowMoleculeGrouping;

        public Size ItemSize
        {
            get { return (Size)GetValue(ItemSizeProperty); }
            set { SetValue(ItemSizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemSizeProperty =
            DependencyProperty.Register("ItemSize", typeof(Size), typeof(CatalogueView),
                                        new FrameworkPropertyMetadata(Size.Empty,
                                                                      FrameworkPropertyMetadataOptions.AffectsRender
                                                                      | FrameworkPropertyMetadataOptions.AffectsArrange
                                                                      | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public double DisplayWidth
        {
            get { return (double)GetValue(DisplayWidthProperty); }
            set { SetValue(DisplayWidthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DisplayWidthProperty =
            DependencyProperty.Register("DisplayWidth", typeof(double), typeof(CatalogueView),
                                        new FrameworkPropertyMetadata(double.NaN,
                                                                      FrameworkPropertyMetadataOptions.AffectsRender
                                                                      | FrameworkPropertyMetadataOptions.AffectsArrange
                                                                      | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public double DisplayHeight
        {
            get { return (double)GetValue(DisplayHeightProperty); }
            set { SetValue(DisplayHeightProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DisplayHeightProperty =
            DependencyProperty.Register("DisplayHeight", typeof(double), typeof(CatalogueView),
                                        new FrameworkPropertyMetadata(double.NaN,
                                                                      FrameworkPropertyMetadataOptions.AffectsRender
                                                                      | FrameworkPropertyMetadataOptions.AffectsArrange
                                                                      | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public void SetOptions(AcmeOptions options)
        {
            _options = options;
        }

        private void OnChemistryItemButtonClick(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is WpfEventArgs source)
            {
                Debug.WriteLine($"{_class} -> {source.Button} {source.OutputValue}");

                if (source.Button.StartsWith("CheckBox")
                    && DataContext is NewCatalogueViewModel viewModel)
                {
                    _itemCount = viewModel.ChemistryItems.Count;
                    _checkedItems = viewModel.ChemistryItems.Count(i => i.IsChecked);

                    TrashButton.IsEnabled = _checkedItems > 0;
                    ToggleButton.IsEnabled = _checkedItems > 0;

                    UpdateStatusBar();
                }
            }
        }

        public void UpdateStatusBar()
        {
            var sb = new StringBuilder();
            if (_itemCount == 0
                && DataContext is NewCatalogueViewModel viewModel)
            {
                _itemCount = viewModel.ChemistryItems.Count;
            }

            if (_filteredItems == 0)
            {
                sb.Append($"Showing all {_itemCount} items");
            }
            else
            {
                sb.Append($"Showing {_filteredItems} from {_itemCount}");
            }

            if (_checkedItems > 0)
            {
                sb.Append($" ({_checkedItems} checked)");
            }
            StatusBar.Text = sb.ToString();
        }

        private void Slider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ItemSize = new Size(Slider.Value, Slider.Value + 65);
            DisplayWidth = Slider.Value - 20;
            DisplayHeight = Slider.Value - 20;
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (string.IsNullOrWhiteSpace(SearchBox.Text))
                {
                    ClearButton_OnClick(null, null);
                }
                else
                {
                    SearchButton_OnClick(null, null);
                }
            }
        }

        private void SearchBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (string.IsNullOrWhiteSpace(SearchBox.Text))
                {
                    SearchButton.IsEnabled = false;
                    ClearButton.IsEnabled = false;
                }
                else
                {
                    SearchButton.IsEnabled = true;
                    ClearButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                //using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                //{
                //    form.ShowDialog();
                //}
            }
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (DataContext != null)
                {
                    ApplySort();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{module} {ex.Message}");
            }
        }

        private void ClearButton_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                ToggleButton.IsChecked = false;
                SearchBox.Clear();

                if (DataContext != null)
                {
                    ClearFilter();
                    UpdateStatusBar();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{module} {ex.Message}");
            }
        }

        private void SearchButton_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                ToggleButton.IsChecked = false;
                if (!string.IsNullOrWhiteSpace(SearchBox.Text)
                    && DataContext != null)
                {
                    FilterByText();
                    UpdateStatusBar();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                //using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                //{
                //    form.ShowDialog();
                //}
            }
        }

        private void ToggleButton_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (DataContext != null)
                {
                    if (ToggleButton.IsChecked != null && ToggleButton.IsChecked.Value)
                    {
                        if (_checkedItems > 0)
                        {
                            SearchBox.Clear();
                            FilterByChecked();
                        }
                        else
                        {
                            ToggleButton.IsChecked = false;
                        }
                    }
                    else
                    {
                        SearchBox.Clear();
                        ClearFilter();
                    }

                    UpdateStatusBar();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                //using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                //{
                //    form.ShowDialog();
                //}
            }
        }

        private class ChemistryObjectComparer : IComparer<ChemistryObject>
        {
            public int Compare(ChemistryObject x, ChemistryObject y)
                => string.CompareOrdinal(x?.Name, y?.Name);
        }

        private void ApplySort()
        {
            if (ComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var view = (ListCollectionView)CollectionViewSource.GetDefaultView(CatalogueItems.ItemsSource);
                view.SortDescriptions.Clear();

                var propertyName = selectedItem.Content.ToString();
                if (propertyName.Equals("Name"))
                {
                    view.CustomSort = (IComparer)new ChemistryObjectComparer();
                }
                else
                {
                    view.SortDescriptions.Add(new SortDescription(propertyName, ListSortDirection.Ascending));
                }
            }
        }

        private void ClearFilter()
        {
            //get the view from the listbox's source
            var view = (ListCollectionView)CollectionViewSource.GetDefaultView(CatalogueItems.ItemsSource);

            _filteredItems = 0;
            view.Filter = null;
        }

        private void FilterByText()
        {
            //get the view from the listbox's source
            var view = (ListCollectionView)CollectionViewSource.GetDefaultView(CatalogueItems.ItemsSource);

            //then try to match part of either its name or an alternative name to the string typed in
            _filteredItems = 0;
            view.Filter = ci =>
                          {
                              var item = ci as ChemistryObject;
                              var queryString = SearchBox.Text.ToUpper();
                              if (item != null
                                  && (item.Name.ToUpper().Contains(queryString)
                                      || item.OtherNames.Any(n => n.ToUpper().Contains(queryString))
                                      || item.Tags.Any(n => n.ToUpper().Contains(queryString)))
                              )
                              {
                                  _filteredItems++;
                                  return true;
                              }

                              return false;
                          };
        }

        private void FilterByChecked()
        {
            //get the view from the listbox's source
            var view = (ListCollectionView)CollectionViewSource.GetDefaultView(CatalogueItems.ItemsSource);

            //then try to match part of either its name or an alternative name to the string typed in
            _filteredItems = 0;
            view.Filter = ci =>
                          {
                              var item = ci as ChemistryObject;
                              if (item != null
                                  && item.IsChecked)
                              {
                                  _filteredItems++;
                                  return true;
                              }

                              return false;
                          };
        }

        private void AddButton_OnClick(object sender, RoutedEventArgs e)
        {
            // ToDo: Implement
            Debug.WriteLine($"{_class} -> Add Button Clicked");
        }

        private void BrowseButton_OnClick(object sender, RoutedEventArgs e)
        {
            // ToDo: Implement
            Debug.WriteLine($"{_class} -> Browse Button Clicked");
        }

        private void TrashButton_OnClick(object sender, RoutedEventArgs e)
        {
            // ToDo: Implement
            Debug.WriteLine($"{_class} -> Trash Button Clicked");
        }

        private void ImportButton_OnClick(object sender, RoutedEventArgs e)
        {
            // ToDo: Implement
            Debug.WriteLine($"{_class} -> Import Button Clicked");
        }

        private void ExportButton_OnClick(object sender, RoutedEventArgs e)
        {
            // ToDo: Implement
            Debug.WriteLine($"{_class} -> Export Button Clicked");
        }
    }
}