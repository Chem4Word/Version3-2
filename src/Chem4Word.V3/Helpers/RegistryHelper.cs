// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
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
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

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
            var messageSize = 0;
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
                messageSize += timestamp.Length + message.Length;
                if (messageSize > 30000)
                {
                    // Send the first 30k
                    SendData(module, level, values);
                    values = new List<string>();
                    messageSize = 0;
                }
                registryKey.DeleteValue(name);
            }

            // Finally send the rest of the data
            SendData(module, level, values);
        }

        private static void SendData(string module, string level, List<string> values)
        {
            if (values.Any())
            {
                Globals.Chem4WordV3.Telemetry.Write(module, level, string.Join(Environment.NewLine, values));
            }
        }
    }
}