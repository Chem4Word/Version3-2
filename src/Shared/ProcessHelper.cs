// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

// Shared file (Add As Link)

using System;
using System.Diagnostics;
using System.IO;

namespace Chem4Word.Shared
{
    public static class ProcessHelper
    {
        // Current: Chem4Word-Setup.3.3.10.Release.8.msi
        // New: msiexec /i Chem4Word-Setup.3.3.10.Release.8.msi /l*v Chem4Word-Setup.3.3.10.Release.8-2025-04-17.13-53-19.212.log
        public static int RunMsi(string fullyQualifiedFileName)
        {
            var exitCode = -1;

            var fileInfo = new FileInfo(fullyQualifiedFileName);
            var fileName = Path.GetFileNameWithoutExtension(fileInfo.Name);
            var timeStamp = DateTime.UtcNow.ToString("yyyy-MM-dd.HH-mm-ss.fff");
            var logFile = $"{fileName}-{timeStamp}.log";
            var arguments = $"/i {fileName}.msi /l*v {logFile}";

            var processStartInfo = new ProcessStartInfo
                        {
                            WorkingDirectory = fileInfo.DirectoryName,
                            FileName = "msiexec",
                            Arguments = arguments
                        };
            using (var process = Process.Start(processStartInfo))
            {
                process.WaitForExit();
                exitCode = process.ExitCode;
            }

            if (exitCode == 0)
            {
                File.Delete(fullyQualifiedFileName);
                File.Delete(Path.Combine(fileInfo.DirectoryName, logFile));
            }
            return exitCode;
        }
    }
}