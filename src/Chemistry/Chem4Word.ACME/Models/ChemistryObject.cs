// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Chem4Word.Libraries;
using Chem4Word.Libraries.Database;
using Chem4Word.Model2.Annotations;
using Chem4Word.Model2.Converters.CML;
using IChem4Word.Contracts;

namespace Chem4Word.ACME.Models
{
    public class ChemistryObject : INotifyPropertyChanged
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private readonly IChem4WordTelemetry _telemetry;
        private readonly LibraryOptions _libraryOptions;

        public bool Initializing { get; set; }

        public ChemistryObject()
        {
            // Required for WPF XAML Designer
        }

        public ChemistryObject(IChem4WordTelemetry telemetry, LibraryOptions libraryOptions)
        {
            _telemetry = telemetry;
            _libraryOptions = libraryOptions;
            Initializing = true;
        }

        private string _cml;

        /// <summary>
        /// The Cml of the structure
        /// </summary>
        public string Cml
        {
            get => _cml;
            set
            {
                _cml = value;
                LoadOtherNames(value);
                if (!Initializing)
                {
                    Save();
                }
                OnPropertyChanged();
            }
        }

        private void Save()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (_libraryOptions != null)
                {
                    var lib = new Library(_telemetry, _libraryOptions);
                    lib.UpdateChemistry(Id, Name, Cml, Formula);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debugger.Break();
            }
        }

        /// <summary>
        /// Formula of the structure
        /// </summary>
        public string Formula { get; set; }

        private string _name;

        /// <summary>
        /// Name of the structure
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                if (!Initializing)
                {
                    Save();
                }
                OnPropertyChanged();
            }
        }

        private bool _isChecked;

        /// <summary>
        /// True if selected (Catalogue mode)
        /// </summary>
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                _isChecked = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The CustomControl Tag (Navigator mode)
        /// </summary>
        public string CustomControlTag { get; set; }

        /// <summary>
        /// The Library Database Id of the structure (Library and Catalogue mode)
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// The calculated Molecular Weight
        /// </summary>
        public double MolecularWeight { get; set; }

        public string MolecularWeightAsString => $"{MolecularWeight:N3}";

        private List<string> _tags = new List<string>();

        /// <summary>
        /// List of Tags
        /// </summary>
        public List<string> Tags
        {
            get
            {
                return _tags;
            }
            set
            {
                _tags = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// List of Chemical Names for the structure (Library mode)
        /// </summary>
        public List<string> OtherNames { get; private set; } = new List<string>();

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            if (PropertyChanged != null)
            {
                Debug.WriteLine($"OnPropertyChanged invoked for {propertyName} from {this}");
            }
        }

        private void LoadOtherNames(string cml)
        {
            XDocument cmlDoc = XDocument.Parse(cml);
            XName nameNodeName = CMLNamespaces.cml + "name";
            OtherNames = (from element in cmlDoc.Descendants(nameNodeName)
                          where element.Name == nameNodeName
                          select element.Value).Distinct().ToList();
        }
    }
}