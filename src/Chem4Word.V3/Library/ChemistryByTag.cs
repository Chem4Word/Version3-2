// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using Chem4Word.ACME.Annotations;
using Chem4Word.Core.UI.Forms;

namespace Chem4Word.Library
{
    public class ChemistryByTag : INotifyPropertyChanged
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private long _galleryID;

        public long GalleryID
        {
            get { return _galleryID; }
            set { _galleryID = value; }
        }

        private long _tagID;

        public long tagID
        {
            get { return _tagID; }
            set { _tagID = value; }
        }

        private long _id;

        public long ID
        {
            get { return _id; }
            set { _id = value; }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
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

        public event PropertyChangedEventHandler PropertyChanged;
    }
}