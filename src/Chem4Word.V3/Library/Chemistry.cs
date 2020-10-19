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
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Chem4Word.ACME.Annotations;
using Chem4Word.Core.UI.Forms;

namespace Chem4Word.Library
{
    public class Chemistry : INotifyPropertyChanged
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public ObservableCollection<UserTag> Tags { get; }
        public bool Dirty { get; set; }
        public bool Initializing { get; set; }

        internal XDocument CmlDoc;

        public string XML
        {
            get
            {
                return CmlDoc.ToString();
            }
            set
            {
                CmlDoc = XDocument.Parse(value);
                if (!Initializing)
                {
                    Save();
                }
                OnPropertyChanged();
            }
        }

        private string _name;

        public string Name
        {
            get { return _name; }
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

        public List<String> OtherNames { get; }
        public bool HasOtherNames { get; internal set; }

        private long _id;

        public long ID
        {
            get { return _id; }
            set
            {
                _id = value;
                OnPropertyChanged();
            }
        }

        private string _formula;

        public string Formula
        {
            get { return _formula; }
            set
            {
                _formula = value;
                if (!Initializing)
                {
                    Save();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public Chemistry()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                OtherNames = new List<string>();
                Dirty = false;
                Tags = new ObservableCollection<UserTag>();
                Tags.CollectionChanged += Tags_CollectionChanged;
                HasOtherNames = false;
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Dirty = true;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        public void Save()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                var lib = new Database.Library();
                lib.UpdateChemistry(ID, Name, XML, Formula);
                Dirty = false;
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        private void Tags_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (Initializing)
                {
                    return;
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
    }
}