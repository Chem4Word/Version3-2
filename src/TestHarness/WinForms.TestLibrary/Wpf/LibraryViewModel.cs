﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using Chem4Word.ACME.Models;
using Chem4Word.Libraries;
using Chem4Word.Libraries.Database;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Telemetry;

namespace WinForms.TestLibrary.Wpf
{
    public class LibraryViewModel : DependencyObject
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        //used for XAML data binding
        public ObservableCollection<ChemistryObject> ChemistryItems { get; }

        private bool _initializing = false;
        private TelemetryWriter _telemetry;
        private LibraryOptions _libraryOptions;

        public LibraryViewModel(TelemetryWriter telemetry, LibraryOptions libraryOptions)
        {
            _telemetry = telemetry;
            _libraryOptions = libraryOptions;

            ChemistryItems = new ObservableCollection<ChemistryObject>();
            ChemistryItems.CollectionChanged += ChemistryItems_CollectionChanged;

            LoadChemistryItems();
        }

        public void LoadChemistryItems()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                _initializing = true;
                ChemistryItems.Clear();

                var lib = new Library(_telemetry, _libraryOptions);
                List<ChemistryDataObject> dto = lib.GetAllChemistry();

                foreach (var chemistryDto in dto)
                {
                    var obj = new ChemistryObject
                    {
                        Id = chemistryDto.Id,
                        Cml = chemistryDto.Cml,
                        Formula = chemistryDto.Formula,
                        Name = chemistryDto.Name,
                        Tags = chemistryDto.Tags.Select(t => t.Text).ToList()
                    };

                    ChemistryItems.Add(obj);
                }

                _initializing = false;
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
                        if (!_initializing)
                        {
                            AddNewChemistry(e.NewItems);
                        }
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        if (!_initializing)
                        {
                            DeleteChemistry(e.OldItems);
                        }
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

        private void DeleteChemistry(IList eOldItems)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (!_initializing)
                {
                    var lib = new Library(_telemetry, _libraryOptions);
                    foreach (ChemistryObject chemistry in eOldItems)
                    {
                        lib.DeleteChemistry(chemistry.Id);
                    }
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

        private void AddNewChemistry(IList eNewItems)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (!_initializing)
                {
                    var lib = new Library(_telemetry, _libraryOptions);
                    foreach (ChemistryObject chemistry in eNewItems)
                    {
                        var cmlConverter = new CMLConverter();
                        chemistry.Id = lib.AddChemistry(cmlConverter.Import(chemistry.Cml), chemistry.Name, chemistry.Formula);
                    }
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