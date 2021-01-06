// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Windows.Data;
using System.Windows.Media;

namespace Chem4Word.ACME.Converters
{
    internal class ValueToForegroundColorConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            SolidColorBrush brush = new SolidColorBrush(Colors.Red);

            Double doubleValue = 0.0;
            if (value == null)
            {
                brush = new SolidColorBrush(Colors.Black);
                return brush;
            }
            Double.TryParse(value.ToString(), out doubleValue);

            if (doubleValue < 0)
            {
                brush = new SolidColorBrush(Colors.Blue);
            }
            else if (doubleValue == 0.0)
            {
                brush = new SolidColorBrush(Colors.Black);
            }

            return brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion IValueConverter Members
    }
}