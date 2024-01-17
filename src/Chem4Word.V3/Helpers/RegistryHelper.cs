// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Chem4Word.Helpers
{
    public static class RegistryHelper
    {
        private static readonly string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static readonly string _class = MethodBase.GetCurrentMethod()?.DeclaringType?.Name;

        private static int _counter = 1;

        public static void StoreMessage(string module, string message)
        {
            var key = Registry.CurrentUser.CreateSubKey(Constants.Chem4WordMessagesRegistryKey);
            if (key != null)
            {
                var procId = 0;
                try
                {
                    procId = Process.GetCurrentProcess().Id;
                }
                catch
                {
                    //
                }

                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                key.SetValue($"{timestamp} [{procId}.{_counter++:000}]", $"[{procId}] {module} {message}");
            }
        }

        public static void StoreException(string module, Exception exception)
        {
            var key = Registry.CurrentUser.CreateSubKey(Constants.Chem4WordExceptionsRegistryKey);
            if (key != null)
            {
                var procId = 0;
                try
                {
                    procId = Process.GetCurrentProcess().Id;
                }
                catch
                {
                    //
                }

                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                key.SetValue($"{timestamp} [{procId}.{_counter++:000}]", $"[{procId}] {module} {exception}");
            }
        }

        public static void SendSetupActions()
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            var registryKey = Registry.CurrentUser.OpenSubKey(Constants.Chem4WordSetupRegistryKey, true);
            if (registryKey != null)
            {
                SendValues(module, "Setup", registryKey);
            }
        }

        public static void SendUpdateActions()
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            var registryKey = Registry.CurrentUser.OpenSubKey(Constants.Chem4WordUpdateRegistryKey, true);
            if (registryKey != null)
            {
                SendValues(module, "Update", registryKey);
            }
        }

        public static void SendMessages()
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            var registryKey = Registry.CurrentUser.OpenSubKey(Constants.Chem4WordMessagesRegistryKey, true);
            if (registryKey != null)
            {
                SendValues(module, "Information", registryKey);
            }
        }

        public static void SendExceptions()
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            var registryKey = Registry.CurrentUser.OpenSubKey(Constants.Chem4WordExceptionsRegistryKey, true);
            if (registryKey != null)
            {
                SendValues(module, "Exceptions", registryKey);
            }
        }

        private static void SendValues(string module, string level, RegistryKey registryKey)
        {
            var names = registryKey.GetValueNames();
            var values = new List<string>();

            foreach (var name in names)
            {
                var message = registryKey.GetValue(name).ToString();

                var timestamp = name;
                var bracket = timestamp.IndexOf("[", StringComparison.InvariantCulture);
                if (bracket > 0)
                {
                    timestamp = timestamp.Substring(0, bracket).Trim();
                }

                values.Add($"{timestamp} {message}");
                registryKey.DeleteValue(name);
            }

            // Group the messages by day
            var groupedByDay = values
                .GroupBy(g => g.Substring(0, 10))
                .ToList();

            var depth = 0;
            foreach (var group in groupedByDay)
            {
                SendData(module, level, group.ToList(), ref depth);
            }
        }

        private static void SendData(string module, string level, List<string> values, ref int depth)
        {
            if (values.Any())
            {
                depth++;

                var message = string.Join(Environment.NewLine, values);

                if (values.Count == 1 || message.Length < 32_000)
                {
                    // Single value or message small enough to send
                    Globals.Chem4WordV3.Telemetry.Write(module, level, message);
                }
                else
                {
                    // Ensure that the recursion doesn't get too deep
                    if (depth < 4)
                    {
                        // Split it into smaller groups, by hour, minute, second
                        var extra = depth * 3;
                        var groups = values
                                     .GroupBy(g => g.Substring(0, 10 + extra))
                                     .ToList();

                        foreach (var group in groups)
                        {
                            SendData(module, level, group.ToList(), ref depth);
                        }
                    }
                }

                depth--;
            }
        }
    }
}