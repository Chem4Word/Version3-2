// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Chem4Word.ACME.Converters
{
    public sealed class BoolToColorConverter : IValueConverter
    {
        public SolidColorBrush TrueValue { get; set; }
        public SolidColorBrush FalseValue { get; set; }

        public BoolToColorConverter()
        {
            TrueValue = new SolidColorBrush(Colors.Black);
            FalseValue = new SolidColorBrush(Colors.Black);
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool))
            {
                return null;
            }

            return (bool)value ? TrueValue : FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Equals(value, TrueValue))
            {
                return true;
            }

            if (Equals(value, FalseValue))
            {
                return false;
            }

            return null;
        }
    }
}