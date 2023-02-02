// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.Model2
{
    public class ElementConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context,
            Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(ElementBase);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            try
            {
                var s = value as string;
                if (string.IsNullOrEmpty(s))
                {
                    Debugger.Break();
                    throw new InvalidOperationException();
                }
                else
                {
                    return Globals.PeriodicTable.Elements[s];
                }
            }
            catch
            {
                return null;
            }
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            return (value as ElementBase)?.Symbol;
        }
    }
}