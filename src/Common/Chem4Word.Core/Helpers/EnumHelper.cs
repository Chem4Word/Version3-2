// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Chem4Word.Core.Helpers
{
    public static class EnumHelper
    {
        public static string GetDescription(this Enum value)
        {
            FieldInfo field = value.GetType().GetField(value.ToString());
            if (field != null && Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
            {
                return attribute.Description;
            }
            return value.ToString(); // Fallback to the enum name if no description is found
        }

        public static IEnumerable<string> GetEnumDescriptions<T>() where T : Enum
        {
            return typeof(T).GetFields()
                            .Where(field => field.IsStatic)
                            .Select(field => field.GetCustomAttribute<DescriptionAttribute>()?.Description ?? field.Name);
        }

        public static IEnumerable<KeyValuePair<T, string>> GetEnumValuesWithDescriptions<T>() where T : Enum
        {
            return Enum.GetValues(typeof(T)).Cast<T>()
                       .Select(e => new KeyValuePair<T, string>(e, e.GetDescription()));
        }
    }
}
