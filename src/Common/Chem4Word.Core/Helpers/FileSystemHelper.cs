// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Chem4Word.Core.Helpers
{
    public static class FileSystemHelper
    {
        public static bool HasPermission(string path)
        {
            string tempFile = Path.Combine(path, Guid.NewGuid().ToString("N")) + ".tmp";

            try
            {
                File.Create(tempFile).Close();
                File.Delete(tempFile);
                return true;
            }
            catch
            {
                // If anything goes wrong assume user can't read and write to the folder
                Debug.WriteLine($"Access denied to {path}");
                return false;
            }
        }

        public static string GetWritablePath(string path)
        {
            // 1. Check supplied path
            if (!string.IsNullOrEmpty(path) && HasPermission(path))
            {
                return path;
            }

            // 2. Executable path
            Assembly assemblyInfo = Assembly.GetExecutingAssembly();
            Uri uriCodeBase = new Uri(assemblyInfo.CodeBase);
            path = Path.GetDirectoryName(uriCodeBase.LocalPath);
            if (HasPermission(path))
            {
                return path;
            }

            // 3. Local AppData Path i.e. "C:\Users\{User}\AppData\Local\"
            path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (HasPermission(path))
            {
                return path;
            }

            return null;
        }
    }
}