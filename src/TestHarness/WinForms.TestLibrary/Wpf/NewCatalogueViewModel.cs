// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using Chem4Word.ACME.Models;
using Chem4Word.Libraries;
using Chem4Word.Libraries.Database;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Telemetry;

namespace WinForms.TestLibrary.Wpf
{
    public class NewCatalogueViewModel : DependencyObject
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private readonly TelemetryWriter _telemetry;
        private readonly LibraryOptions _libraryOptions;

        public NewCatalogueViewModel(TelemetryWriter telemetry, LibraryOptions libraryOptions)
        {
            _telemetry = telemetry;
            _libraryOptions = libraryOptions;
            ChemistryItems = new ObservableCollection<ChemistryObject>();
            LoadChemistryItems();
            ChemistryItems.CollectionChanged += ChemistryItems_CollectionChanged;
        }

        //used for XAML data binding
        public ObservableCollection<ChemistryObject> ChemistryItems { get; }

        private void LoadChemistryItems()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                ChemistryItems.Clear();

                var lib = new Library(_telemetry, _libraryOptions);
                List<ChemistryDTO> dto = lib.GetAllChemistry();

                var cmlCoverter = new CMLConverter();

                foreach (var chemistryDto in dto)
                {
                    var model = cmlCoverter.Import(chemistryDto.Cml);
                    double weight = 0;
                    foreach (var atom in model.GetAllAtoms())
                    {
                        weight += atom.Element.AtomicWeight;
                    }
                    var obj = new ChemistryObject
                    {
                        Id = chemistryDto.Id,
                        Cml = chemistryDto.Cml,
                        Formula = chemistryDto.Formula,
                        Name = chemistryDto.Name,
                        MolecularWeight = weight
                    };

                    ChemistryItems.Add(obj);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{module} {ex.Message}");
                //using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                //{
                //    form.ShowDialog();
                //}
            }
        }

        private void ChemistryItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{module} {ex.Message}");
                //using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                //{
                //    form.ShowDialog();
                //}
            }
        }
    }
}