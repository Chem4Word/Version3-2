// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
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
        private static bool HasPermission(string path)
        {
            var tempFile = Path.Combine(path, Guid.NewGuid().ToString("N")) + ".tmp";

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
            var assemblyInfo = Assembly.GetExecutingAssembly();
            var uriCodeBase = new Uri(assemblyInfo.CodeBase);
            path = Path.GetDirectoryName(uriCodeBase.LocalPath);
            if (HasPermission(path))
            {
                return path;
            }

            // 3. Local AppData Path i.e. "C:\Users\{User}\AppData\Local\"
            path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return HasPermission(path) ? path : null;
        }

        /// <summary>
        /// Check to see if file is binary by checking if the first 8k characters contains at least n null characters
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="requiredConsecutiveNul"></param>
        /// <returns></returns>
        public static bool IsBinary(string filePath, int requiredConsecutiveNul = 1)
        {
            const int charsToCheck = 8096;
            const char nulChar = '\0';

            var nulCount = 0;

            using (var streamReader = new StreamReader(filePath))
            {
                for (var i = 0; i < charsToCheck; i++)
                {
                    if (streamReader.EndOfStream)
                    {
                        return false;
                    }

                    if ((char)streamReader.Read() == nulChar)
                    {
                        nulCount++;

                        if (nulCount >= requiredConsecutiveNul)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        nulCount = 0;
                    }
                }
            }

            return false;
        }

        public static string DetectFileType(string filePath)
        {
            var result = string.Empty;

            var contents = File.ReadAllText(filePath);

            if (contents.Contains("M  END"))
            {
                result = ".mol";
            }

            if (contents.StartsWith("<")
                && contents.Contains("<cml")
                && contents.Contains("</cml"))
            {
                result = ".cml";
            }

            if (contents.StartsWith("SketchEl"))
            {
                result = ".el";
            }

            return result;
        }
    }
}