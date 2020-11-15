using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
    /// Interaction logic for LibraryView.xaml
    /// </summary>
    public partial class LibraryView : UserControl
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private AcmeOptions _options;

        public LibraryView()
        {
            InitializeComponent();
        }

        public bool ShowAllCarbonAtoms => _options.ShowCarbons;
        public bool ShowImplicitHydrogens => _options.ShowHydrogens;
        public bool ShowAtomsInColour => _options.ColouredAtoms;
        public bool ShowMoleculeGrouping => _options.ShowMoleculeGrouping;

        public void SetOptions(AcmeOptions options)
        {
            _options = options;
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

        private void ClearButton_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                SearchBox.Clear();

                if (DataContext != null)
                {
                    ICollectionView view = CollectionViewSource.GetDefaultView(((NewLibraryViewModel) DataContext).ChemistryItems);
                    view.Filter = null;
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
                if (!string.IsNullOrWhiteSpace(SearchBox.Text)
                    && DataContext != null)
                {
                    //get the view from the listbox's source
                    ICollectionView view = CollectionViewSource.GetDefaultView(((NewLibraryViewModel) DataContext).ChemistryItems);
                    //then try to match part of either its name or an alternative name to the string typed in
                    view.Filter = ci =>
                                  {
                                      var item = ci as ChemistryObject;
                                      var queryString = SearchBox.Text.ToUpper();
                                      return item != null
                                             && (item.Name.ToUpper().Contains(queryString)
                                                 || item.OtherNames.Any(n => n.ToUpper().Contains(queryString)));
                                  };
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

        private void OnChemistryItemButtonClick(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is WpfEventArgs source)
            {
                Debug.WriteLine($"{_class} -> {source.Button} {source.OutputValue}");
            }
        }
    }
}