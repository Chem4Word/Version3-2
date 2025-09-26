// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Azure.Messaging.ServiceBus;
using Chem4Word.Core;
using Chem4Word.Core.Helpers;
using Chem4Word.Shared;
using IChem4Word.Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace Chem4Word.Telemetry
{
    public class TelemetryWriter : IChem4WordTelemetry
    {
        private static AzureServiceBusWriter _azureServiceBusWriter;
        private static bool _systemInfoSent;
        private static bool _gitInfoSent;

        private static SystemHelper _helper;
        private static WmiHelper _wmiHelper;
        private static bool _machineIdWritten;

        private readonly bool _permissionGranted;
        private readonly bool _isBeta;

        public TelemetryWriter(bool permissionGranted, bool isBeta, SystemHelper helper)
        {
            _permissionGranted = permissionGranted;
            _isBeta = isBeta;
            _helper = helper;

            if (_helper == null)
            {
                //Debugger.Break();
            }

            if (_wmiHelper == null)
            {
                _wmiHelper = new WmiHelper();
            }
            _azureServiceBusWriter = new AzureServiceBusWriter(new AzureSettings(true));
        }

        public void Write(string source, string level, string message)
        {
            var unwanted = "Chem4Word.V3.";
            if (source.StartsWith(unwanted))
            {
                source = source.Remove(0, unwanted.Length);
            }
            unwanted = "Chem4WordV3.";
            if (source.StartsWith(unwanted))
            {
                source = source.Remove(0, unwanted.Length);
            }
            unwanted = "Chem4Word.";
            if (source.StartsWith(unwanted))
            {
                source = source.Remove(0, unwanted.Length);
            }

            if (string.IsNullOrEmpty(message))
            {
                Debug.WriteLine("message should not be empty");
                Debugger.Break();
            }

            try
            {
                var fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                            $@"Chem4Word.V3\Telemetry\{SafeDate.ToIsoShortDate(DateTime.UtcNow)}.log");
                using (var w = File.AppendText(fileName))
                {
                    if (!_machineIdWritten)
                    {
                        if (_helper != null && _helper.MachineId != Guid.Empty.ToString("D"))
                        {
                            w.WriteLine($"[{SafeDate.ToShortTime(DateTime.UtcNow)}] * InstallationId: {_helper.MachineId}");
                            _machineIdWritten = true;
                        }
                    }
                    var logMessage = $"[{SafeDate.ToShortTime(DateTime.UtcNow)}] {source} - {level} - {message}";
                    w.WriteLine(logMessage);
                }
            }
            catch
            {
                // Do nothing
            }

            if (_permissionGranted)
            {
                WritePrivate(source, level, message);

                if (_helper != null
                    && _helper?.IpAddress != null
                    && !_helper.IpAddress.Contains("0.0.0.0"))
                {
                    if (!_systemInfoSent)
                    {
                        WriteStartUpInfo();
                    }

                    if (!_gitInfoSent && !string.IsNullOrEmpty(_helper.GitStatus))
                    {
                        var tracking = _helper.GitStatus
                                              .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                                              .FirstOrDefault(l => l.StartsWith("##"));

                        if (!string.IsNullOrEmpty(tracking))
                        {
                            var idxStart = tracking.IndexOf('[');
                            var idxEnd = tracking.IndexOf(']');
                            if (idxStart > 0 && idxEnd > 0)
                            {
                                var info = tracking.Substring(idxStart, idxEnd - idxStart + 1);

                                if (info.Contains("behind"))
                                {
                                    MessageBox.Show("Your local source code is behind origin!", "WARNING",
                                                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                                }
                                if (info.Contains("gone"))
                                {
                                    MessageBox.Show("Your local source code is gone from origin!", "WARNING",
                                                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                                }
                            }
                        }

                        var failedToFetch = _helper.GitStatus
                                                   .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                                                   .FirstOrDefault(l => l.Contains("is not a git command"));
                        if (!string.IsNullOrEmpty(failedToFetch))
                        {
                            // One of these two commands is required to be run, most likely the first one ...
                            // git config --global --unset credential.helper
                            // git config --global credential.helper store
                            MessageBox.Show(@"Git fetch failed\nYou need to run 'git config --global --unset credential.helper' from a command prompt.", "WARNING");
                        }

                        WritePrivate("StartUp", "Information", _helper.GitStatus);
                        _gitInfoSent = true;
                    }
                }
            }
        }

        public void SendZipFile(byte[] bytes, string fileName)
        {
            var message = new ServiceBusMessage(bytes)
            {
                ContentType = "application/zip"
            };

            message.ApplicationProperties.Add("FileName", fileName);
            message.ApplicationProperties.Add("MachineId", _helper.MachineId);
            message.ApplicationProperties.Add("Chem4WordVersion", _helper.AddInVersion);

            _azureServiceBusWriter.SendZipFileMessage(message);
        }

        /// <summary>
        /// "Last chance" fix for missing word version number
        /// </summary>
        private void FixUpWordVersion()
        {
            var product = _helper.WordProduct;
            if (product.Contains("[16."))
            {
                // If this is Office 2016/2019/2021/2024/365
                if (product.Contains("2016")
                    || product.Contains("2019")
                    || product.Contains("2021")
                    || product.Contains("2024")
                    || product.Contains("365"))
                {
                    // Word version is set
                }
                else
                {
                    _helper.WordProduct = product + " 2016";
                }
            }
        }

        private void WriteStartUpInfo()
        {
            FixUpWordVersion();
            AddChartGeneratorProperties();

            WritePrivate("StartUp", "Information", $"Internal Version {_helper.WordVersion}");
            if (!string.IsNullOrEmpty(_helper.Click2RunProductIds))
            {
                WritePrivate("StartUp", "Information", $"{_helper.Click2RunProductIds}");
            }

            WritePrivate("StartUp", "Information", Environment.GetCommandLineArgs()[0]);

            if (_helper.StartUpTimings != null)
            {
                WritePrivate("StartUp", "Timing", string.Join(Environment.NewLine, _helper.StartUpTimings));
            }

            var lines = new List<string>();

            if (_helper.SystemUtcDateTime > DateTime.MinValue)
            {
                // Log UtcOffsets
                lines.Add($"Server UTC DateTime is {SafeDate.ToLongDate(_helper.ServerUtcDateTime)}");
                lines.Add($"System UTC DateTime is {SafeDate.ToLongDate(_helper.SystemUtcDateTime)}");

                lines.Add($"Server Header [Date] is {_helper.ServerDateHeader}");
                lines.Add($"Server UTC DateTime raw is {_helper.ServerUtcDateRaw}");

                lines.Add($"Calculated UTC Offset is {_helper.UtcOffset}");
                if (_helper.UtcOffset > 0)
                {
                    var delta = TimeSpan.FromTicks(_helper.UtcOffset);
                    lines.Add($"System UTC DateTime is {delta} ahead of Server time");
                }
                if (_helper.UtcOffset < 0)
                {
                    var delta = TimeSpan.FromTicks(0 - _helper.UtcOffset);
                    lines.Add($"System UTC DateTime is {delta} behind Server time");
                }

                WritePrivate("StartUp", "Information", string.Join(Environment.NewLine, lines));
            }

            // Log Wmi Gathered Data
            lines = new List<string>();

            lines.Add($"SYS: {_wmiHelper.Manufacturer} - {_wmiHelper.Model} - {_wmiHelper.SystemFamily}");
            lines.Add($"CPU: {_wmiHelper.CpuName}");
            lines.Add($"CPU Cores: {_wmiHelper.LogicalProcessors}");
            lines.Add($"CPU Speed: {_wmiHelper.CpuSpeed}");
            lines.Add($"Memory: {_wmiHelper.TotalPhysicalMemory}");
            lines.Add($"Booted Up: {_helper.LastBootUpTime}");
            lines.Add($"Logged In: {_helper.LastLoginTime}");
            lines.Add($"Type: {_wmiHelper.ProductType}");

            if (!string.IsNullOrEmpty(_wmiHelper.AntiVirusStatus))
            {
                lines.Add("AntiVirus:");
                var products = _wmiHelper.AntiVirusStatus.Split(';');
                foreach (var product in products)
                {
                    lines.Add($"  {product}");
                }
            }

            // Log Screen Sizes
            lines.Add($"Screens: {_helper.Screens}");

            WritePrivate("StartUp", "Information", string.Join(Environment.NewLine, lines));

            // Log All Add Ins
            if (_helper.AllAddIns != null && _helper.AllAddIns.Count > 0)
            {
                lines = new List<string>();
                foreach (var addIn in _helper.AllAddIns)
                {
                    if (addIn.LoadBehaviour >= 0 && !string.IsNullOrEmpty(addIn.Description))
                    {
                        lines.Add($"{addIn.KeyName} '{addIn.Description}' [{addIn.LoadBehaviour}] {addIn.Manifest}".Trim());
                    }
                }
                WritePrivate("StartUp", "Information", string.Join(Environment.NewLine, lines));
            }

#if DEBUG
            lines = new List<string>();

            var clientName = Environment.GetEnvironmentVariable("CLIENTNAME");
            var userDomainName = Environment.UserDomainName;
            var userName = Environment.UserName;
            var machineName = Environment.MachineName;
            string userSummary;

            if (userDomainName.Equals(machineName))
            {
                // Local account
                userSummary = $"Local user {userName} on {machineName}";
            }
            else
            {
                // Domain account
                userSummary = $@"Domain user {userDomainName}\{userName} on {machineName}";
            }

            // Include RDP Info if available
            if (!string.IsNullOrEmpty(clientName))
            {
                userSummary += $" via RDP from {clientName}";
            }

            lines.Add($"Debug - {userSummary}");

            lines.Add($"Debug - Environment.OSVersion: {Environment.OSVersion}");
            lines.Add($"Debug - Environment.Version: {Environment.Version}");

            lines.Add($"Debug - AddIn Location: {_helper.AddInLocation}");
            lines.Add($"Debug - Environment.CommandLine: {Environment.CommandLine}");

            WritePrivate("StartUp", "Information", string.Join(Environment.NewLine, lines));

            if (_isBeta)
            {
                WritePrivate("StartUp", "Information", $"Environment.Is64BitOperatingSystem: {Environment.Is64BitOperatingSystem}");
                WritePrivate("StartUp", "Information", $"Environment.Is64BitProcess: {Environment.Is64BitProcess}");

                WritePrivate("StartUp", "Information", string.Join(Environment.NewLine, OfficeHelper.GetWinWordSearchPaths()));
            }

#endif

            AddChartGeneratorProperties();

            _systemInfoSent = true;
        }

        private void AddChartGeneratorProperties()
        {
            // Used by nightly chart generator for website

            // OS Info
            if (string.IsNullOrEmpty(_wmiHelper.OSVersion) || string.IsNullOrEmpty(_wmiHelper.OSCaption))
            {
                WritePrivate("StartUp", "Information", $"{_helper.SystemOs}");
            }
            else
            {
                var bits = Environment.Is64BitOperatingSystem ? "64bit" : "32bit";
                var culture = CultureInfo.CurrentCulture.Name;
                WritePrivate("StartUp", "Information", $"{_wmiHelper.OSCaption} {bits} [{_wmiHelper.OSVersion}] {culture}");
            }

            WritePrivate("StartUp", "Information", $"UUID: {_wmiHelper.Uuid}");
#if DEBUG
            WritePrivate("StartUp", "Information", $"User: {_helper.UserName}");
#endif

            // Dot Net Version
            WritePrivate("StartUp", "Information", $"{_helper.DotNetVersion}");

            // Word Version
            WritePrivate("StartUp", "Information", $"{_helper.WordProduct}");

            // Add-In Version
            WritePrivate("StartUp", "Information", $"{_helper.AddInVersion}");

            // IP Address
            if (!_helper.IpAddress.Contains("8.8.8.8"))
            {
                WritePrivate("StartUp", "Information", $"{_helper.IpAddress}"); // ** Used by nightly chart generator
                WritePrivate("StartUp", "Information", $"{_helper.IpObtainedFrom}");
            }
        }

        private string GetVersionNumber()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var productVersion = assembly.GetName().Version;
            return productVersion.ToString();
        }

        private void WritePrivate(string operation, string level, string message)
        {
            Debug.WriteLine($"{operation} - {level} - {message}");

            // Default values to ensure we have something to log
            var processId = 666;
            var machineId = Guid.Empty.ToString("D");
            // This is updated automatically by Set-Assembly-Version.ps1
            var versionNumber = "3.2.23.666";

            try
            {
                if (_helper != null)
                {
                    processId = _helper.ProcessId;
                    if (string.IsNullOrEmpty(_helper.MachineId) || _helper.MachineId.Equals(Guid.Empty.ToString("D")))
                    {
                        machineId = SystemHelper.GetMachineId();
                    }
                    else
                    {
                        machineId = _helper.MachineId;
                    }

                    if (string.IsNullOrEmpty(_helper.AssemblyVersionNumber))
                    {
                        versionNumber = GetVersionNumber();
                    }
                    else
                    {
                        versionNumber = _helper.AssemblyVersionNumber;
                    }
                }
                else
                {
                    // This is what _helper would have done had it been initialised ...
                    versionNumber = GetVersionNumber();
                    processId = Process.GetCurrentProcess().Id;
                    machineId = SystemHelper.GetMachineId();
                }
            }
            catch
            {
                try
                {
                    // This is what _helper would have done had it been initialised ...
                    versionNumber = GetVersionNumber();
                    processId = Process.GetCurrentProcess().Id;
                    machineId = SystemHelper.GetMachineId();
                }
                catch
                {
                    // Do nothing
                }
            }

            var outputMessage = new OutputMessage(processId)
            {
                MachineId = machineId,
                Operation = operation,
                Level = level,
                Message = message,
                AssemblyVersionNumber = versionNumber
            };

            _azureServiceBusWriter.QueueMessage(outputMessage);
        }
    }
}
