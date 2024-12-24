// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.IO;
using System.Reflection;

namespace Chem4WordTests
{
    public static class ResourceHelper
    {
        private static Stream GetBinaryResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();

            Stream data = null;

            var fullName = string.Empty;
            var count = 0;

            var resources = assembly.GetManifestResourceNames();
            foreach (var s in resources)
            {
                if (s.EndsWith($".{resourceName}"))
                {
                    count++;
                    fullName = s;
                }
            }

            if (!string.IsNullOrEmpty(fullName))
            {
                data = assembly.GetManifestResourceStream(fullName);
            }

            if (count != 1)
            {
                return null;
            }

            return data;
        }

        public static string GetStringResource(string resourceName)
        {
            string data = null;

            var resource = GetBinaryResource(resourceName);
            if (resource != null)
            {
                var textStreamReader = new StreamReader(resource);
                data = textStreamReader.ReadToEnd();

                // Repair any "broken" line feeds to Windows style
                var etx = (char)3;
                var temp = data.Replace("\r\n", $"{etx}");
                temp = temp.Replace("\n", $"{etx}");
                temp = temp.Replace("\r", $"{etx}");
                var lines = temp.Split(etx);
                data = string.Join(Environment.NewLine, lines);
            }

            return data;
        }
    }
}