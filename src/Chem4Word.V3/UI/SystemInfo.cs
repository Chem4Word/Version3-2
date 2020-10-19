// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Helpers;

namespace Chem4Word.UI
{
    public partial class SystemInfo : Form
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public System.Windows.Point TopLeft { get; set; }

        public SystemInfo()
        {
            InitializeComponent();
        }

        private void SystemInfo_Load(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (!PointHelper.PointIsEmpty(TopLeft))
                {
                    Left = (int)TopLeft.X;
                    Top = (int)TopLeft.Y;
                }

                StringBuilder sb = new StringBuilder();

                #region Add In Version

                Assembly assembly = Assembly.GetExecutingAssembly();
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                UpdateHelper.ReadThisVersion(assembly);

                string version = string.Empty;
                if (Globals.Chem4WordV3.ThisVersion != null)
                {
                    string[] parts = Globals.Chem4WordV3.ThisVersion.Root.Element("Number").Value.Split(' ');
                    string temp = Globals.Chem4WordV3.ThisVersion.Root.Element("Number").Value;
                    int idx = temp.IndexOf(" ");
                    version = $"Chem4Word 2020 {temp.Substring(idx + 1)} - [{fvi.FileVersion}]";
                }
                else
                {
                    version = $"Chem4Word Version: V{fvi.FileVersion}";
                }

                sb.AppendLine(version);

                #endregion Add In Version

                sb.AppendLine("");
                sb.AppendLine($"Installation Id: {Globals.Chem4WordV3.Helper.MachineId}");
                sb.AppendLine($"Operating System: {Globals.Chem4WordV3.Helper.SystemOs}");
                sb.AppendLine($"Word Product: {Globals.Chem4WordV3.Helper.WordProduct}");
                sb.AppendLine($"Internet Explorer Version: {Globals.Chem4WordV3.Helper.BrowserVersion}");
                sb.AppendLine($".Net Framework Runtime: {Globals.Chem4WordV3.Helper.DotNetVersion}");

                sb.AppendLine("");
                sb.AppendLine($"Settings Folder: {Globals.Chem4WordV3.AddInInfo.ProductAppDataPath}");
                sb.AppendLine($"Library Folder: {Globals.Chem4WordV3.AddInInfo.ProgramDataPath}");

                sb.AppendLine("");
                string plugInPath = Path.Combine(Globals.Chem4WordV3.AddInInfo.DeploymentPath, "PlugIns");
                sb.AppendLine($"PlugIns Folder: {plugInPath}");

                if (Directory.Exists(plugInPath))
                {
                    var files = Directory.GetFiles(plugInPath, "Chem4Word*.dll");
                    if (files.Length > 0)
                    {
                        foreach (var file in files)
                        {
                            var fileInfo = FileVersionInfo.GetVersionInfo(file);
                            var shortName = file.Replace($@"{plugInPath}\", "");
                            sb.AppendLine($"  {shortName} - [{fileInfo.FileVersion}]");
                        }
                    }
                }

                var dlls = AppDomain.CurrentDomain.GetAssemblies().Select(s => s.FullName).ToList();
                dlls = dlls.OrderBy(s => s).ToList();

                sb.AppendLine("");
                sb.AppendLine(".Net Assemblies loaded in memory");
                foreach (var dll in dlls)
                {
                    var idx = dll.IndexOf(", Culture", StringComparison.InvariantCulture);
                    if (idx > 0)
                    {
                        sb.AppendLine($"  {dll.Substring(0, idx)}");
                    }
                    else
                    {
                        sb.AppendLine($"  {dll}");
                    }
                }

                Information.Text = sb.ToString();
                Information.SelectionStart = Information.Text.Length;
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }
    }
}