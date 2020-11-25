// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Chem4Word.Model2.Annotations;
using Chem4Word.Model2.Converters.CML;

namespace Chem4Word.ACME.Models
{
    public class ChemistryObject : INotifyPropertyChanged
    {
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
                OnPropertyChanged();
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

        /// <summary>
        /// List of Tags
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        public bool HasTags => Tags.Any();

        /// <summary>
        /// List of Chemical Names for the structure (Library mode)
        /// </summary>
        public List<string> OtherNames { get; private set; } = new List<string>();

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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