// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
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
using System.Windows.Controls;
using System.Xml.Linq;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Libraries;
using Chem4Word.Libraries.Database;
using Chem4Word.Model2.Converters.CML;

namespace Chem4Word.Library
{
    public class LibraryViewModel : DependencyObject
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private bool _initializing = false;

        //used for XAML data binding
        public ObservableCollection<LibraryItem> LibraryItems { get; }

        public ObservableCollection<Chemistry> ChemistryItems { get; }

        public LibraryViewModel(string filter = "")
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                ChemistryItems = new ObservableCollection<Chemistry>();
                ChemistryItems.CollectionChanged += ChemistryItems_CollectionChanged;

                LoadChemistryItems(filter);

                LibraryItems = new ObservableCollection<LibraryItem>();

                sw.Stop();
                Debug.WriteLine($"LibraryViewModel() took {sw.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
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
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        private void DeleteChemistry(IList eOldItems)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (!_initializing)
                {
                    var lib = new Libraries.Database.Library(Globals.Chem4WordV3.Telemetry,
                                                             new LibrarySettings
                                                             {
                                                                 ParentTopLeft = Globals.Chem4WordV3.WordTopLeft,
                                                                 ProgramDataPath = Globals.Chem4WordV3.AddInInfo.ProgramDataPath,
                                                                 PreferredBondLength = Globals.Chem4WordV3.SystemOptions.BondLength,
                                                                 SetBondLengthOnImport = Globals.Chem4WordV3.SystemOptions.SetBondLengthOnImportFromLibrary,
                                                                 RemoveExplicitHydrogensOnImport = Globals.Chem4WordV3.SystemOptions.RemoveExplicitHydrogensOnImportFromLibrary
                                                             });
                    foreach (Chemistry chemistry in eOldItems)
                    {
                        lib.DeleteChemistry(chemistry.ID);
                    }
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        private void AddNewChemistry(IList eNewItems)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (!_initializing)
                {
                    var lib = new Libraries.Database.Library(Globals.Chem4WordV3.Telemetry,
                                                             new LibrarySettings
                                                             {
                                                                 ParentTopLeft = Globals.Chem4WordV3.WordTopLeft,
                                                                 ProgramDataPath = Globals.Chem4WordV3.AddInInfo.ProgramDataPath,
                                                                 PreferredBondLength = Globals.Chem4WordV3.SystemOptions.BondLength,
                                                                 SetBondLengthOnImport = Globals.Chem4WordV3.SystemOptions.SetBondLengthOnImportFromLibrary,
                                                                 RemoveExplicitHydrogensOnImport = Globals.Chem4WordV3.SystemOptions.RemoveExplicitHydrogensOnImportFromLibrary
                                                             });
                    foreach (Chemistry chemistry in eNewItems)
                    {
                        var cmlConverter = new CMLConverter();
                        chemistry.ID = lib.AddChemistry(cmlConverter.Import(chemistry.XML), chemistry.Name, chemistry.Formula);
                    }
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }


        public void LoadChemistryItems(string filter)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                _initializing = true;
                ChemistryItems.Clear();
                var lib = new Libraries.Database.Library(Globals.Chem4WordV3.Telemetry,
                                                         new LibrarySettings
                                                         {
                                                             ParentTopLeft = Globals.Chem4WordV3.WordTopLeft,
                                                             ProgramDataPath = Globals.Chem4WordV3.AddInInfo.ProgramDataPath,
                                                             PreferredBondLength = Globals.Chem4WordV3.SystemOptions.BondLength,
                                                             SetBondLengthOnImport = Globals.Chem4WordV3.SystemOptions.SetBondLengthOnImportFromLibrary,
                                                             RemoveExplicitHydrogensOnImport = Globals.Chem4WordV3.SystemOptions.RemoveExplicitHydrogensOnImportFromLibrary
                                                         });
                List<ChemistryDTO> dto = lib.GetAllChemistry(filter);
                foreach (var chemistry in dto)
                {
                    var mol = new Chemistry();
                    mol.Initializing = true;

                    mol.ID = chemistry.Id;
                    mol.XML = chemistry.Cml;
                    mol.Name = chemistry.Name;
                    mol.Formula = chemistry.Formula;

                    ChemistryItems.Add(mol);
                    LoadOtherNames(mol);

                    mol.Initializing = false;
                }

                _initializing = false;
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        private void LoadOtherNames(Chemistry mol)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                XName nameNodeName = CMLNamespaces.cml + "name";

                var names = (from element in mol.CmlDoc.Descendants(nameNodeName)
                             where element.Name == nameNodeName
                             select element.Value).Distinct();

                foreach (string name in names)
                {
                    mol.HasOtherNames = true;
                    mol.OtherNames.Add(name);
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        public ListBoxItem SelectedItem
        {
            get { return (ListBoxItem)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(ListBoxItem), typeof(LibraryViewModel), new PropertyMetadata(null));

        public void SaveChanges()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                SaveChemistryChanges();
                //SaveTagChanges();
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        private void SaveTagChanges()
        {
            Debugger.Break();
        }

        private void SaveChemistryChanges()
        {
            foreach (Chemistry chemistryItem in ChemistryItems)
            {
                if (chemistryItem.Dirty)
                {
                    chemistryItem.Save();
                }
            }
        }
    }
}