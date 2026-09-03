// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

// Shared file (Add As Link)

using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;

namespace Chem4Word.Shared
{
    public static class ProcessHelper
    {
        private static int _counter = 1;
        private const string Chem4WordMsiActionsRegistryKey = @"SOFTWARE\Chem4Word V3\MsiActions";

        // Old: Chem4Word-Setup.3.3.{build}.Release.{n}.msi
        // New: msiexec /i Chem4Word-Setup.3.3.{build}.Release.{n}.msi /l*v Chem4Word-Setup.3.3.{build}.Release.{n}-{yyyy-MM-dd.HH-mm-ss.fff}.log
        public static int RunMsi(string fullyQualifiedFileName)
        {
            int exitCode = -1;

            WriteMsiAction($"Running Msi: '{fullyQualifiedFileName}'");

            FileInfo fileInfo = new FileInfo(fullyQualifiedFileName);
            string fileName = Path.GetFileNameWithoutExtension(fileInfo.Name);
            string timeStamp = DateTime.UtcNow.ToString("yyyy-MM-dd.HH-mm-ss.fff");
            string logFile = $"{fileName}-{timeStamp}.log";
            string arguments = $"/i {fileName}.msi /l*v {logFile}";

            string msiExec = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "msiexec.exe");

            if (!File.Exists(msiExec))
            {
                WriteMsiAction($"Error: File '{msiExec}' not found");
                return exitCode;
            }
            if (!File.Exists(fullyQualifiedFileName))
            {
                WriteMsiAction($"Error: File '{fullyQualifiedFileName}' not found");
                return exitCode;
            }
            if (string.IsNullOrEmpty(fileInfo.DirectoryName))
            {
                WriteMsiAction($"Error: Working Directory is empty '{fileInfo.DirectoryName}'");
                return exitCode;
            }

            // Safe to continue
            WriteMsiAction($"MsiExec Arguments: '{arguments}'");

            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                WorkingDirectory = fileInfo.DirectoryName,
                FileName = msiExec,
                Arguments = arguments
            };
            using (Process process = Process.Start(processStartInfo))
            {
                if (process == null)
                {
                    WriteMsiAction($"Error Starting {processStartInfo.FileName}");
                    exitCode = -1;
                }
                else
                {
                    process.WaitForExit();
                    exitCode = process.ExitCode;
                }
            }

            if (exitCode == 0)
            {
                Thread.Sleep(250);

                File.Delete(fullyQualifiedFileName);
                File.Delete(Path.Combine(fileInfo.DirectoryName, logFile));
            }
            return exitCode;
        }

        public static void WriteMsiAction(string action)
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(Chem4WordMsiActionsRegistryKey);
            if (key != null)
            {
                int procId = 0;
                try
                {
                    procId = Process.GetCurrentProcess().Id;
                }
                catch
                {
                    //
                }
                string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                key.SetValue($"{timestamp} [{procId}.{_counter++.ToString("000")}]", $"[{procId}] {action}");
                key.Dispose();
            }
        }
    }
}
