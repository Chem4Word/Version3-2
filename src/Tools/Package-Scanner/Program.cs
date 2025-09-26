using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace PackageScanner
{
    internal class Program
    {
        private static string baseDirectory = @"..\..\..\..\..";
        private static string readMeFile = "README.MD";

        private static List<string> before = new List<string>();
        private static List<string> middle = new List<string>();
        private static List<string> after = new List<string>();

        private static void Main(string[] args)
        {
            ScanPackages();
        }

        private static void ScanPackages()
        {
            var packages = new SortedDictionary<string, PackageDetails>(StringComparer.OrdinalIgnoreCase);

            ReadReadMe(Path.Combine(baseDirectory, readMeFile), packages);

            var files = Directory.GetFiles(baseDirectory, "packages.config", SearchOption.AllDirectories).ToList();
            // Include Wix projects
            files.AddRange(Directory.GetFiles(baseDirectory, "WiX*.wixproj", SearchOption.AllDirectories));
            files.AddRange(Directory.GetFiles(baseDirectory, "WiX*.csproj", SearchOption.AllDirectories));

            ProcessPackages(files, packages);

            foreach (var kvp in packages)
            {
                if (kvp.Value.UsageCount > 0)
                {
                    middle.Add($"{kvp.Value}");
                }
            }

            WriteReadMe(Path.Combine(baseDirectory, readMeFile));
        }

        private static void ReadReadMe(string readMeFileName, SortedDictionary<string, PackageDetails> packages)
        {
            var lines = File.ReadAllLines(readMeFileName);

            var section = 0;
            foreach (var line in lines)
            {
                if (line.Length > 0)
                {
                    if (line.Equals("## List of NuGet packages"))
                    {
                        section = 1;
                    }

                    if (line.Equals("## Acknowledgements"))
                    {
                        section = 2;
                    }
                }

                switch (section)
                {
                    case 0:
                        before.Add(line);
                        break;

                    case 1:
                        if (line.StartsWith("## List of NuGet packages")
                            || line.StartsWith("| Package |")
                            || line.StartsWith("|--|"))
                        {
                            middle.Add(line);
                        }
                        else
                        {
                            if (line.Length > 0)
                            {
                                var parts = line.Split('|');

                                var details = new PackageDetails
                                {
                                    PackageName = parts[1],
                                    PackageVersion = parts[2],
                                    PackageLicence = parts[3]
                                };

                                packages.Add($"{details.PackageName}|{details.PackageVersion}", details);
                            }
                        }
                        break;

                    case 2:
                        after.Add(line);
                        break;
                }
            }
        }

        private static void WriteReadMe(string readMeFileName)
        {
            var body = before;
            body.AddRange(middle);
            body.Add("");
            body.AddRange(after);

            File.WriteAllLines(readMeFileName, body);

            var data = string.Join(Environment.NewLine, middle);
            if (data.Contains("||"))
            {
                Console.WriteLine($"WARNING File {readMeFile} contains Unknown license(s)");
            }
            if (data.Contains("|0|"))
            {
                Console.WriteLine($"WARNING File {readMeFile} contains unused packages");
            }

            Console.WriteLine($"File {readMeFile} has been updated");
            if (Debugger.IsAttached)
            {
                Console.ReadLine();
            }
        }

        private static void ProcessPackages(List<string> files, SortedDictionary<string, PackageDetails> packages)
        {
            foreach (var file in files)
            {
                var lines = File.ReadAllLines(file);

                if (file.ToLower().EndsWith("packages.config"))
                {
                    foreach (var line in lines)
                    {
                        // <package id="DocumentFormat.OpenXml" version="2.11.3" targetFramework="net71" />
                        if (line.Trim().StartsWith("<package id"))
                        {
                            var packageId = string.Empty;
                            var packageVersion = string.Empty;

                            var parts = line.Split(' ');

                            foreach (var part in parts)
                            {
                                if (part.StartsWith("id"))
                                {
                                    packageId = part.Split('=')[1].Replace('"', ' ').Trim();
                                }

                                if (part.StartsWith("version"))
                                {
                                    packageVersion = part.Split('=')[1].Replace('"', ' ').Trim();
                                }
                            }

                            UpdatePackages(packageId, packageVersion);
                        }
                    }
                }
                else
                {
                    foreach (var line in lines)
                    {
                        // <PackageReference Include="WixToolset.Dtf.CustomAction" Version="6.0.0" />
                        if (line.Trim().StartsWith("<PackageReference"))
                        {
                            var packageId = string.Empty;
                            var packageVersion = string.Empty;

                            var parts = line.Split(' ');

                            foreach (var part in parts)
                            {
                                if (part.StartsWith("Include"))
                                {
                                    packageId = part.Split('=')[1].Replace('"', ' ').Trim();
                                }

                                if (part.StartsWith("Version"))
                                {
                                    packageVersion = part.Split('=')[1].Replace('"', ' ').Trim();
                                }
                            }

                            UpdatePackages(packageId, packageVersion);
                        }
                    }
                }
            }

            void UpdatePackages(string packageId, string packageVersion)
            {
                if (!string.IsNullOrEmpty(packageId) && !string.IsNullOrEmpty(packageVersion))
                {
                    var id = $"{packageId}|{packageVersion}";
                    if (packages.ContainsKey(id))
                    {
                        packages[id].UsageCount++;
                    }
                    else
                    {
                        var details = new PackageDetails
                        {
                            PackageName = packageId,
                            PackageVersion = packageVersion,
                            PackageLicence = FindLicence(packageId, packages),
                            UsageCount = 1
                        };
                        packages.Add(id, details);
                    }
                }
            }
        }

        private static string FindLicence(string packageId, SortedDictionary<string, PackageDetails> packages)
        {
            var result = string.Empty;

            foreach (var kvp in packages)
            {
                if (kvp.Key.StartsWith(packageId)
                    && kvp.Value.PackageName.Equals(packageId)
                    && !string.IsNullOrEmpty(kvp.Value.PackageLicence))
                {
                    result = kvp.Value.PackageLicence;
                    break;
                }
            }

            return result;
        }
    }
}
