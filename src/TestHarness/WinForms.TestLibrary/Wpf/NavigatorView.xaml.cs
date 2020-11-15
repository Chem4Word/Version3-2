using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Chem4Word.ACME;
using Chem4Word.Core.UI.Wpf;

namespace WinForms.TestLibrary.Wpf
{
    /// <summary>
    /// Interaction logic for NavigatorView.xaml
    /// </summary>
    public partial class NavigatorView : UserControl
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private AcmeOptions _options;

        public NavigatorView()
        {
            InitializeComponent();
        }

        public bool ShowAllCarbonAtoms => _options.ShowCarbons;
        public bool ShowImplicitHydrogens => _options.ShowHydrogens;
        public bool ShowAtomsInColour => _options.ColouredAtoms;
        public bool ShowMoleculeGrouping => _options.ShowMoleculeGrouping;

        public string SelectedNavigatorItem { get; set; }

        public void SetOptions(AcmeOptions options)
        {
            _options = options;
        }

        private void OnChemistryItemButtonClick(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is WpfEventArgs source)
            {
                // ToDo: Implement
                Debug.WriteLine($"{_class} -> {source.Button} {source.OutputValue}");

                var parts = source.OutputValue.Split('=');
                SelectedNavigatorItem = parts[1];
            }
        }
    }
}