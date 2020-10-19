// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.ComponentModel;
using System.Runtime.CompilerServices;
using Chem4Word.Model2.Annotations;
using Chem4Word.Model2.Converters.CML;

namespace Chem4Word.Model2
{
    public class TextualProperty : INotifyPropertyChanged
    {
        public string Id { get; set; }

        public bool CanBeDeleted { get; set; } = true;
        public bool IsReadOnly { get; private set; }
        public bool IsValid { get; private set; }

        private string _typeCode;
        private string _fullType;
        private string _value;

        public string TypeCode
        {
            get => _typeCode;
            set
            {
                _typeCode = value;
                OnPropertyChanged(nameof(TypeCode));
            }
        }

        public string FullType
        {
            get => _fullType;
            set
            {
                _fullType = value;
                SetEditFlag();
                OnPropertyChanged(nameof(FullType));
            }
        }

        public string Value
        {
            get => _value;
            set
            {
                _value = value;
                IsValid = !string.IsNullOrEmpty(_value);
                OnPropertyChanged(nameof(Value));
            }
        }

        private void SetEditFlag()
        {
            if (!string.IsNullOrEmpty(_fullType))
            {
                IsReadOnly = !(_fullType.Equals(CMLConstants.ValueChem4WordCaption)
                               || _fullType.Equals(CMLConstants.ValueChem4WordFormula)
                               || _fullType.Equals(CMLConstants.ValueChem4WordSynonym));
            }
        }

        public override string ToString()
        {
            string result = string.Empty;

            if (!string.IsNullOrEmpty(Id))
            {
                result += $"{Id} ";
            }

            if (!string.IsNullOrEmpty(_typeCode))
            {
                result += $"{_typeCode} ";
            }

            if (!string.IsNullOrEmpty(_fullType))
            {
                result += $"{_fullType} ";
            }

            if (!string.IsNullOrEmpty(_value))
            {
                result += $"{_value} ";
            }

            return result.Trim();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}