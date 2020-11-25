using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Chem4Word.ACME;
using Chem4Word.ACME.Models;
using Chem4Word.Libraries;
using Chem4Word.Libraries.Database;
using Chem4Word.Telemetry;
using WinForms.TestLibrary.Wpf;

namespace WinForms.TestLibrary
{
    public partial class MainForm : Form
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private SystemHelper _helper;
        private TelemetryWriter _telemetry;
        private LibraryOptions _libraryOptions;
        private AcmeOptions _acmeOptions;

        public MainForm()
        {
            InitializeComponent();

            _helper = new SystemHelper();
            _telemetry = new TelemetryWriter(true, _helper);

            var location = Assembly.GetExecutingAssembly().Location;
            var path = Path.GetDirectoryName(location);

            // Use either path or null below
            _acmeOptions = new AcmeOptions(null);

            // Values for testing of binding
            //_acmeOptions.ColouredAtoms = false;
            //_acmeOptions.ShowCarbons = true;
            //_acmeOptions.ShowHydrogens = false;

            _libraryOptions = new LibraryOptions
            {
                ParentTopLeft = new System.Windows.Point(0, 0),
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

            // This code belongs in the TaskPane Hosting Control
            if (LibraryHost.Child is LibraryView libraryControl)
            {
                var viewModel = new NewLibraryViewModel(_telemetry, _libraryOptions);
                libraryControl.SetOptions(_acmeOptions);
                libraryControl.DataContext = viewModel;
            }

            _telemetry.Write(module, "Information", $"Library done at {sw.ElapsedMilliseconds}ms");

            if (CatalogueHost.Child is CatalogueView catalogueControl)
            {
                var viewModel = new NewCatalogueViewModel(_telemetry, _libraryOptions);
                catalogueControl.SetOptions(_acmeOptions);
                catalogueControl.DataContext = viewModel;
                catalogueControl.UpdateStatusBar();
            }

            _telemetry.Write(module, "Information", $"Catalogue done at {sw.ElapsedMilliseconds}ms");

            if (NavigatorHost.Child is NavigatorView navigatorControl)
            {
                var viewModel = new NewNavigatorViewModel();

                var lib = new Library(_telemetry, _libraryOptions);
                List<ChemistryDTO> dto = lib.GetAllChemistry();

                foreach (var chemistryDto in dto)
                {
                    // Simulate item originating from a ContentControl
                    var obj = new ChemistryObject
                    {
                        CustomControlTag = Guid.NewGuid().ToString("N"),
                        Cml = chemistryDto.Cml,
                        Formula = chemistryDto.Formula
                    };

                    viewModel.NavigatorItems.Add(obj);
                }

                navigatorControl.SetOptions(_acmeOptions);
                navigatorControl.DataContext = viewModel;
            }

            sw.Stop();
            _telemetry.Write(module, "Information", $"Navigator done at {sw.ElapsedMilliseconds}ms");
        }

        private void FindLastItem_Click(object sender, EventArgs e)
        {
            if (NavigatorHost.Child is NavigatorView navigatorControl
                && !string.IsNullOrEmpty(navigatorControl.SelectedNavigatorItem))
            {
                int idx = 0;
                foreach (var item in navigatorControl.NavigatorList.Items)
                {
                    if (item is ChemistryObject chemistryObject
                        && chemistryObject.CustomControlTag.Equals(navigatorControl.SelectedNavigatorItem))
                    {
                        navigatorControl.NavigatorList.SelectedIndex = idx;
                        navigatorControl.NavigatorList.ScrollIntoView(navigatorControl.NavigatorList.SelectedItem);
                        break;
                    }

                    idx++;
                }
            }
        }
    }
}