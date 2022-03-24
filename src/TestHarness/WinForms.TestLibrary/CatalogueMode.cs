using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using Chem4Word.ACME;
using Chem4Word.Core.UI.Wpf;
using Chem4Word.Libraries;
using Chem4Word.Telemetry;
using WinForms.TestLibrary.Wpf;

namespace WinForms.TestLibrary
{
    public partial class CatalogueMode : Form
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private SystemHelper _helper;
        private TelemetryWriter _telemetry;
        private LibraryOptions _libraryOptions;
        private AcmeOptions _acmeOptions;

        public CatalogueMode()
        {
            InitializeComponent();

            _helper = new SystemHelper();
            _telemetry = new TelemetryWriter(true, _helper);

            var location = Assembly.GetExecutingAssembly().Location;
            var path = Path.GetDirectoryName(location);

            // Use either path or null below
            _acmeOptions = new AcmeOptions(path);

            // Values for testing of binding
            //_acmeOptions.ColouredAtoms = false;
            //_acmeOptions.ShowCarbons = true;
            //_acmeOptions.ShowHydrogens = false;

            _libraryOptions = new LibraryOptions
            {
                ParentTopLeft = new Point(Left, Top),
                ProgramDataPath = @"C:\ProgramData\Chem4Word.V3",
                Chem4WordVersion = _helper.AssemblyVersionNumber,
                PreferredBondLength = 20,
                SetBondLengthOnImport = true,
                RemoveExplicitHydrogensOnImport = true
            };
        }

        private void LoadData_Click(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            _telemetry.Write(module, "Information", "Clicked");

            Stopwatch sw = new Stopwatch();
            sw.Start();

            var Controller = new CatalogueController(_telemetry, _libraryOptions);

            if (CatalogueHost.Child is CatalogueControl catalogueControl)
            {
                catalogueControl.TopLeft = new Point(Left, Top);
                catalogueControl.SetOptions(_telemetry, _acmeOptions, _libraryOptions);
                catalogueControl.DataContext = Controller;
                catalogueControl.UpdateStatusBar();
                catalogueControl.OnSelectionChange -= CatalogueControlOnOnSelectionChange;
                catalogueControl.OnSelectionChange += CatalogueControlOnOnSelectionChange;
            }

            sw.Stop();
            _telemetry.Write(module, "Information", $"Catalogue done at {sw.ElapsedMilliseconds}ms");
        }

        private void CatalogueControlOnOnSelectionChange(object sender, WpfEventArgs e)
        {
            Debug.WriteLine($"{e.Button} {e.OutputValue}");
        }
    }
}