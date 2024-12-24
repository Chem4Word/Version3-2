// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
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

        public static void SendMsiActions()
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            var registryKey = Registry.CurrentUser.OpenSubKey(Constants.Chem4WordMsiActionsRegistryKey, true);
            if (registryKey != null)
            {
                SendValues(module, "Setup", registryKey);
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
                SendValues(module, "Exception", registryKey);
            }
        }

        private static void SendValues(string module, string level, RegistryKey registryKey)
        {
            var keys = registryKey.GetValueNames();
            var values = new List<RegistryMessage>();

            foreach (var key in keys)
            {
                var value = registryKey.GetValue(key).ToString();

                var timestamp = key;
                var bracket = timestamp.IndexOf("[", StringComparison.InvariantCulture);
                if (bracket > 0)
                {
                    timestamp = timestamp.Substring(0, bracket).Trim();
                }

                var temp = new RegistryMessage
                {
                    Date = timestamp
                };

                bracket = value.IndexOf("]", StringComparison.InvariantCulture);
                if (bracket > 0)
                {
                    temp.ProcessId = value.Substring(1, bracket - 1).Trim();
                    temp.Message = value.Substring(bracket + 1).Trim();
                }
                else
                {
                    temp.Message = value;
                }

                values.Add(temp);
                registryKey.DeleteValue(key);
            }

            // Group the messages by day
            var days = values
                .GroupBy(g => g.Date.Substring(0, 10))
                .ToList();

            var depth = 0;
            foreach (var day in days)
            {
                SendData(module, level, day.ToList(), ref depth);
            }
        }

        private static void SendData(string module, string level, List<RegistryMessage> messages, ref int depth)
        {
            if (messages.Any())
            {
                depth++;

                // Group messages by process id
                var processes = messages
                    .GroupBy(g => g.ProcessId)
                    .ToList();

                foreach (var process in processes)
                {
                    var message = string.Join(Environment.NewLine, process.ToList());
                    // Maximum length of a message in Azure Service bus is 32K, use 125 for testing
                    if (message.Length < 32_000)
                    {
                        Globals.Chem4WordV3.Telemetry.Write(module, level, message);
                    }
                    else
                    {
                        if (depth < 4)
                        {
                            var extra = depth * 3;

                            // Re-Group by Hour, Minute, Second based on depth
                            var groups = messages
                                .GroupBy(g => g.Date.Substring(0, 10 + extra))
                                .ToList();
                            foreach (var group in groups)
                            {
                                SendData(module, level, group.ToList(), ref depth);
                            }
                        }
                    }
                }

                depth--;
            }
        }
    }
}