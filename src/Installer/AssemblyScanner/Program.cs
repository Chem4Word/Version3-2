using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace AssemblyScanner
{
    internal class Program
    {
        private static void Main()
        {
            const string ThirdParty = @"..\..\..\WixInstaller\ThirdParty.wxs";
            const string SourceFolder = @"..\..\..\..\Chem4Word.V3\bin\Setup";

            string[] files = Directory.GetFiles(SourceFolder, "*.dll");

            XNamespace ns = "http://wixtoolset.org/schemas/v4/wxs";
            XDocument doc = XDocument.Load(ThirdParty);

            XElement group = doc.Descendants(ns + "ComponentGroup").FirstOrDefault();

            if (group != null)
            {
                group.RemoveNodes();

                foreach (string file in files)
                {
                    FileInfo fi = new FileInfo(file);
                    if (fi.Name.Contains("Chem4Word") || fi.Name.Contains("WebView2"))
                    {
                        Debug.WriteLine($"Skipping {fi.Name}");
                    }
                    else
                    {
                        Console.WriteLine($"Adding {fi.Name}");

                        XElement xe = new XElement(ns + "Component",
                                                   new XAttribute("Id", $"{fi.Name.Replace(".dll", "")}"),
                                                   new XElement(ns + "File",
                                                                new XAttribute("Id", $"{fi.Name}"),
                                                                new XAttribute("Source",
                                                                               $"$(var.Chem4Word.V3.TargetDir){fi.Name}")));
                        group.Add(xe);
                    }
                }

                doc.Save(ThirdParty);
            }
        }
    }
}
