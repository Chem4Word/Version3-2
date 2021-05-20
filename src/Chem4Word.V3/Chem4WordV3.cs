﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Xml.Linq;
using Chem4Word.Core;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Helpers;
using Chem4Word.Libraries;
using Chem4Word.Library;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Navigator;
using Chem4Word.Telemetry;
using IChem4Word.Contracts;
using Microsoft.Office.Core;
using Newtonsoft.Json;
using Extensions = Microsoft.Office.Tools.Word.Extensions;
using OfficeTools = Microsoft.Office.Tools;
using Word = Microsoft.Office.Interop.Word;
using WordTools = Microsoft.Office.Tools.Word;

namespace Chem4Word
{
    public partial class Chem4WordV3
    {
        private static readonly string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static readonly string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public static CustomRibbon Ribbon;

        public int VersionsBehind = 0;
        public DateTime VersionLastChecked = DateTime.MinValue;
        public string VersionAvailable = string.Empty;
        public bool VersionAvailableIsBeta = false;
        public bool IsEnabled;
        public bool IsEndOfLife;
        public bool WordIsActivated;

        public XDocument AllVersions;
        public XDocument ThisVersion;

        public bool EventsEnabled = true;

        public bool ChemistryAllowed = false;
        public string ChemistryProhibitedReason = "";
        private string _lastContentControlAdded = "";

        private bool _chemistrySelected = false;
        private bool _markAsChemistryHandled = false;
        private bool _plugInsLoaded = false;

        public bool OptionsReloadRequired = false;
        private int _rightClickEvents;
        private ConfigWatcher _configWatcher;

        public bool LibraryState = false;

        public List<string> StartUpTimings = new List<string>();
        public C4wAddInInfo AddInInfo;
        public SystemHelper Helper;
        public Chem4WordOptions SystemOptions;
        public LibraryOptions LibraryOptions;
        public TelemetryWriter Telemetry;

        public List<IChem4WordEditor> Editors = new List<IChem4WordEditor>();
        public List<IChem4WordRenderer> Renderers = new List<IChem4WordRenderer>();
        public List<IChem4WordSearcher> Searchers = new List<IChem4WordSearcher>();

        public Dictionary<string, int> LibraryNames = null;

        private static readonly string[] ContextMenusTargets = { "Text", "Table Text", "Spelling", "Grammar", "Grammar (2)", "Lists", "Table Lists" };
        private const string ContextMenuTag = "2829AECC-061C-4DC5-8CC0-CAEC821B9127";
        private const string ContextMenuText = "Convert to Chemistry";

        public int WordWidth
        {
            get
            {
                int width = 0;

                try
                {
                    CommandBar commandBar1 = Application.CommandBars["Ribbon"];
                    if (commandBar1 != null)
                    {
                        width = Math.Max(width, commandBar1.Width);
                    }
                    CommandBar commandBar2 = Application.CommandBars["Status Bar"];
                    if (commandBar2 != null)
                    {
                        width = Math.Max(width, commandBar2.Width);
                    }
                }
                catch
                {
                    //
                }

                try
                {
                    if (width == 0)
                    {
                        width = Screen.PrimaryScreen.Bounds.Width;
                    }
                }
                catch
                {
                    //
                }

                return width;
            }
        }

        public Point WordTopLeft
        {
            get
            {
                Point pp = new Point();

                try
                {
                    // Get position of Standard CommandBar (<ALT>+F)
                    CommandBar commandBar = Application.CommandBars["Standard"];
                    pp.X = commandBar.Left + commandBar.Height;
                    pp.Y = commandBar.Top + commandBar.Height;
                }
                catch
                {
                    //
                }
                return pp;
            }
        }

        public int WordVersion
        {
            get
            {
                int version = -1;

                try
                {
                    switch (Application.Version)
                    {
                        case "12.0":
                            version = 2007;
                            break;

                        case "14.0":
                            version = 2010;
                            break;

                        case "15.0":
                            version = 2013;
                            break;

                        case "16.0":
                            version = 2016;
                            break;
                    }
                }
                catch
                {
                    //
                }

                return version;
            }
        }

        public static void SetGlobalRibbon(CustomRibbon ribbon)
        {
            Ribbon = ribbon;
        }

        private void C4WAddIn_Startup(object sender, EventArgs e)
        {
            string module = $"{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                // Deliberate crash to test Error Reporting
                //int ii = 2;
                //int dd = 0;
                //int bang = ii / dd;

                var cmd = Environment.CommandLine.ToLower();
                if (Ribbon != null && !cmd.Contains("-embedding"))
                {
                    string message = $"{module} started at {SafeDate.ToLongDate(DateTime.Now)}";
                    Debug.WriteLine(message);
                    StartUpTimings.Add(message);

                    Stopwatch sw = new Stopwatch();
                    sw.Start();

                    CheckIfWordIsActivated();
                    PerformStartUpActions();

                    sw.Stop();
                    message = $"{module} took {SafeDouble.AsString(sw.ElapsedMilliseconds)}ms";
                    StartUpTimings.Add(message);
                    Debug.WriteLine(message);

                    if (!WordIsActivated)
                    {
                        UserInteractions.AlertUser("Microsoft Word is not activated!\nChem4Word uses features of Word which are only available if it is activated.");
                    }
                }
                else
                {
#if DEBUG
                    if (Ribbon == null)
                    {
                        RegistryHelper.StoreMessage(module, "Ribbon is null");
                    }
                    RegistryHelper.StoreMessage(module, $"Command line {cmd}");
#endif
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"{module} {exception.Message}");
                RegistryHelper.StoreException(module, exception);
            }
        }

        private void CheckIfWordIsActivated()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                bool? flag = Globals.Chem4WordV3.Application.Options.SmartCutPaste;
                WordIsActivated = flag.HasValue;
            }
            catch (Exception exception)
            {
                WordIsActivated = false;
                Debug.WriteLine($"{module} {exception.Message}");
                RegistryHelper.StoreException(module, exception);
            }
        }

        private void C4WAddIn_Shutdown(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (Ribbon != null)
                {
                    PerformShutDownActions();
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"{module} {exception.Message}");
                RegistryHelper.StoreException(module, exception);
            }
        }

        private void SlowOperations()
        {
            string module = $"{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                string message = $"{module} started at {SafeDate.ToLongDate(DateTime.Now)}";

                Debug.WriteLine(message);
                StartUpTimings.Add(message);

                Stopwatch sw = new Stopwatch();
                sw.Start();

                Thread thread = new Thread(LoadPluginsOnThread);
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();

                Helper = new SystemHelper(StartUpTimings);

                ServicePointManager.DefaultConnectionLimit = 100;
                ServicePointManager.UseNagleAlgorithm = false;
                ServicePointManager.Expect100Continue = false;

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                _configWatcher = new ConfigWatcher(AddInInfo.ProductAppDataPath);

                Telemetry = new TelemetryWriter(true, Helper);

                sw.Stop();
                message = $"{module} took {SafeDouble.AsString(sw.ElapsedMilliseconds)}ms";
                Debug.WriteLine(message);
                StartUpTimings.Add(message);
            }
            catch // (ThreadAbortException threadAbortException)
            {
                // Do Nothing
            }
        }

        private void PerformStartUpActions()
        {
            string module = $"{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                AddInInfo = new C4wAddInInfo();

                UpdateHelper.ReadSavedValues();
                UpdateHelper.ReadThisVersion(Assembly.GetExecutingAssembly());
                ShowOrHideUpdateShield();

                // Handle slower startup stuff on thread
                Thread thread = new Thread(SlowOperations);
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();

                if (VersionsBehind < Constants.MaximumVersionsBehind)
                {
                    Word.Application app = Application;

                    // Hook in Global Application level events
                    app.WindowBeforeDoubleClick += OnWindowBeforeDoubleClick;
                    app.WindowSelectionChange += OnWindowSelectionChange;
                    app.WindowActivate += OnWindowActivate;
                    app.WindowBeforeRightClick += OnWindowBeforeRightClick;

                    // Hook in Global Document Level Events
                    app.DocumentOpen += OnDocumentOpen;
                    app.DocumentChange += OnDocumentChange;
                    app.DocumentBeforeClose += OnDocumentBeforeClose;
                    app.DocumentBeforeSave += OnDocumentBeforeSave;
                    ((Word.ApplicationEvents4_Event)Application).NewDocument += OnNewDocument;

                    if (app.Documents.Count > 0)
                    {
                        EnableContentControlEvents();

                        if (app.ActiveDocument.CompatibilityMode >= (int)Word.WdCompatibilityMode.wdWord2010)
                        {
                            SetButtonStates(ButtonState.CanInsert);
                        }
                        else
                        {
                            SetButtonStates(ButtonState.NoDocument);
                        }
                    }

                    if (AddInInfo.DeploymentPath.ToLower().Contains("vso-ci"))
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine($"Hey {Environment.UserName}");
                        sb.AppendLine("");
                        sb.AppendLine("You should not be running this build configuration");
                        sb.AppendLine("Please select Debug or Release build!");
                        UserInteractions.StopUser(sb.ToString());
                    }
                }
                else
                {
                    SetButtonStates(ButtonState.Disabled);
                    StartUpTimings.Add(
                        $"{module} chemistry operations disabled because Chem4Word is {VersionsBehind} versions behind!");
                    RegistryHelper.StoreMessage(module, $"Chem4Word is disabled because it is {VersionsBehind} versions behind!");
                }

                // Deliberate crash to test Error Reporting
                //int ii = 2;
                //int dd = 0;
                //int bang = ii / dd;
            }
            catch (ThreadAbortException threadAbortException)
            {
                RegistryHelper.StoreException(module, threadAbortException);
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"{module} {exception.Message}");
                RegistryHelper.StoreException(module, exception);

                using (var form = new ReportError(Telemetry, WordTopLeft, module, exception))
                {
                    form.ShowDialog();
                }
            }
        }

        public void LoadNamesFromLibrary()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                var lib = new Libraries.Database.Library(Telemetry, LibraryOptions);
                LibraryNames = lib.GetLibraryNames();
            }
            catch (Exception exception)
            {
                if (Telemetry != null)
                {
                    Telemetry.Write(module, "Exception", exception.Message);
                    Telemetry.Write(module, "Exception", exception.StackTrace);
                }
                else
                {
                    RegistryHelper.StoreException(module, exception);
                }

                LibraryNames = null;
            }
        }

        public void LoadOptions()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                // Initialise Telemetry with send permission
                Telemetry = new TelemetryWriter(true, Helper);

                if (AddInInfo != null)
                {
                    // Read in options file
                    SystemOptions = new Chem4WordOptions(AddInInfo.ProductAppDataPath);
                    if (SystemOptions.Errors.Any())
                    {
                        Telemetry.Write(module, "Exception", string.Join(Environment.NewLine, SystemOptions.Errors));
                        SystemOptions.Errors = new List<string>();
                    }

                    bool isBeta = true;
                    try
                    {
                        if (ThisVersion != null)
                        {
                            string betaValue = ThisVersion.Root?.Element("IsBeta")?.Value;
                            isBeta = betaValue != null && bool.Parse(betaValue);
                        }
                    }
                    catch
                    {
                        // Assume isBeta
                    }

                    // Belt and braces ...
                    if (SystemOptions == null)
                    {
                        SystemOptions = new Chem4WordOptions
                        {
                            SettingsPath = AddInInfo.ProductAppDataPath
                        };
                    }

                    // ... as we are seeing some errors here ?
                    // Re-Initialise Telemetry with granted permissions
                    Telemetry = new TelemetryWriter(isBeta || SystemOptions.TelemetryEnabled, Helper);

                    try
                    {
                        bool settingsChanged = false;

                        if (string.IsNullOrEmpty(SystemOptions.SelectedEditorPlugIn))
                        {
                            SystemOptions.SelectedEditorPlugIn = Constants.DefaultEditorPlugIn;
                        }
                        else
                        {
                            if (Editors.Count > 0)
                            {
                                var editor = GetEditorPlugIn(SystemOptions.SelectedEditorPlugIn);
                                if (editor == null)
                                {
                                    SystemOptions.SelectedEditorPlugIn = Constants.DefaultEditorPlugIn;
                                    Telemetry.Write(module, "Information", $"Setting editor to {SystemOptions.SelectedEditorPlugIn}");
                                    settingsChanged = true;
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(SystemOptions.SelectedRendererPlugIn))
                        {
                            SystemOptions.SelectedRendererPlugIn = Constants.DefaultRendererPlugIn;
                        }
                        else
                        {
                            if (Renderers.Count > 0)
                            {
                                var renderer = GetRendererPlugIn(SystemOptions.SelectedRendererPlugIn);
                                if (renderer == null)
                                {
                                    SystemOptions.SelectedRendererPlugIn = Constants.DefaultRendererPlugIn;
                                    Telemetry.Write(module, "Information", $"Setting renderer to {SystemOptions.SelectedRendererPlugIn}");
                                    settingsChanged = true;
                                }
                            }
                        }

                        if (settingsChanged)
                        {
                            Telemetry.Write(module, "Information", "Saving revised settings");
                            SystemOptions.Save();
                            if (SystemOptions.Errors.Any())
                            {
                                Telemetry.Write(module, "Exception", string.Join(Environment.NewLine, SystemOptions.Errors));
                                SystemOptions.Errors = new List<string>();
                            }
                        }
                    }
                    catch
                    {
                        //
                    }

                    LibraryOptions = new LibraryOptions
                    {
                        ParentTopLeft = WordTopLeft,
                        ProgramDataPath = AddInInfo.ProgramDataPath,
                        PreferredBondLength = SystemOptions.BondLength,
                        SetBondLengthOnImport = SystemOptions.SetBondLengthOnImportFromLibrary,
                        RemoveExplicitHydrogensOnImport = SystemOptions.RemoveExplicitHydrogensOnImportFromLibrary
                    };
                }
            }
            catch (Exception exception)
            {
                if (Telemetry != null)
                {
                    Telemetry.Write(module, "Exception", exception.Message);
                    Telemetry.Write(module, "Exception", exception.StackTrace);
                }
                else
                {
                    RegistryHelper.StoreException(module, exception);
                }

                SystemOptions = null;
            }
        }

        private void PerformShutDownActions()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                if (Editors != null)
                {
                    for (int i = 0; i < Editors.Count; i++)
                    {
                        Editors[i].Telemetry = null;
                        Editors[i] = null;
                    }
                }

                if (Renderers != null)
                {
                    for (int i = 0; i < Renderers.Count; i++)
                    {
                        Renderers[i].Telemetry = null;
                        Renderers[i] = null;
                    }
                }

                if (Searchers != null)
                {
                    for (int i = 0; i < Searchers.Count; i++)
                    {
                        Searchers[i].Telemetry = null;
                        Searchers[i] = null;
                    }
                }

                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            finally
            {
                // Fix reported issue with Windows 8 and WPF See
                // https://social.msdn.microsoft.com/Forums/office/en-US/bb990ddb-ecde-4161-8915-e66e913e3a3b/invalidoperationexception-localdatastoreslot-storage-has-been-freed?forum=exceldev
                // I saw this on Server 2008 R2 which is very closely related to Windows 8
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        }

        private void LoadPluginsOnThread()
        {
            string module = $"{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                LoadPlugIns(false);
                if (Ribbon != null)
                {
                    _plugInsLoaded = true;
                    if (VersionsBehind >= Constants.MaximumVersionsBehind)
                    {
                        SetButtonStates(ButtonState.Disabled);
                        ChemistryProhibitedReason = Constants.Chem4WordTooOld;
                    }
                    else
                    {
                        try
                        {
                            if (Application.Documents.Count > 0 && Application.Selection != null)
                            {
                                OnWindowSelectionChange(Application.Selection);
                            }
                        }
                        catch
                        {
                            // Do Nothing
                        }
                    }

                    LoadOptions();
                }
            }
            catch (ThreadAbortException)
            {
                // Do Nothing
            }
            catch (Exception exception)
            {
                RegistryHelper.StoreException(module, exception);
            }
        }

        private void LoadPlugIns(bool mustBeSigned)
        {
            string module = $"{MethodBase.GetCurrentMethod().Name}()";
            // http://www.codeproject.com/Articles/453778/Loading-Assemblies-from-Anywhere-into-a-New-AppDom
            string message = $"{module} started at {SafeDate.ToLongDate(DateTime.Now)}";
            StartUpTimings.Add(message);
            Debug.WriteLine(message);

            Stopwatch sw = new Stopwatch();
            sw.Start();

            string plugInPath = Path.Combine(AddInInfo.DeploymentPath, "PlugIns");
            //Telemetry.Write(module, "Debug", $"Looking for Plug-Ins in folder {plugInPath}");

            string[] files = null;
            int filesFound = 0;

            if (Directory.Exists(plugInPath))
            {
                files = Directory.GetFiles(plugInPath, "Chem4Word*.dll");
                filesFound = files.Length;
            }

            List<string> plugInsFound = new List<string>();

            #region Find Our PlugIns

            foreach (string file in files)
            {
                using (var manager = new AssemblyReflectionManager())
                {
                    try
                    {
                        var success = manager.LoadAssembly(file, "Chem4WordTempDomain");
                        if (success)
                        {
                            #region Find Our Interfaces

                            var results = manager.Reflect(file, a =>
                            {
                                var names = new List<string>();

                                #region Get Code Signing Certificate details

                                string signedBy = "";

                                Module mod = a.GetModules().First();
                                var certificate = mod.GetSignerCertificate();
                                if (certificate != null)
                                {
                                    signedBy = certificate.Subject;
                                    // E=developer@chem4word.co.uk, CN="Open Source Developer, Mike Williams", O=Open Source Developer, C=GB
                                    Debug.WriteLine(certificate.Subject);
                                    Debug.WriteLine(certificate.Issuer);
                                }

                                #endregion Get Code Signing Certificate details

                                var types = a.GetTypes();
                                foreach (var t in types)
                                {
                                    if (t.IsClass && t.IsPublic && !t.IsAbstract)
                                    {
                                        var ifaces = t.GetInterfaces();
                                        foreach (var iface in ifaces)
                                        {
                                            if (iface.FullName.StartsWith("IChem4Word.Contracts"))
                                            {
                                                string[] parts = a.FullName.Split(',');
                                                FileInfo fi = new FileInfo(t.Module.FullyQualifiedName);
                                                names.Add($"{parts[0]}|{iface.FullName}|{fi.Name}|{signedBy}");
                                            }
                                        }
                                    }
                                }

                                return names;
                            });

                            #endregion Find Our Interfaces

                            manager.UnloadAssembly(file);

                            plugInsFound.AddRange(results);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }
            }

            #endregion Find Our PlugIns

            Type editorType = typeof(IChem4WordEditor);
            Type rendererType = typeof(IChem4WordRenderer);
            Type searcherType = typeof(IChem4WordSearcher);

            foreach (string plugIn in plugInsFound)
            {
                string[] parts = plugIn.Split('|');
                Debug.WriteLine($"Loading PlugIn {parts[0]} with Interface {parts[1]} from file {parts[2]} signed by {parts[3]}");

                bool allowed = true;
                if (mustBeSigned)
                {
                    // Is it signed by us?
                    allowed = parts[3].Contains("admin@chem4word.co.uk");
                }

                if (allowed)
                {
                    #region Find Source File

                    string sourceFile = "";
                    foreach (string file in files)
                    {
                        if (file.EndsWith(parts[2]))
                        {
                            sourceFile = file;
                            break;
                        }
                    }

                    #endregion Find Source File

                    if (!string.IsNullOrEmpty(sourceFile))
                    {
                        #region Load Editor(s)

                        if (parts[1].Contains("IChem4WordEditor"))
                        {
                            Assembly asm = Assembly.LoadFile(sourceFile);
                            Type[] types = asm.GetTypes();
                            foreach (Type type in types)
                            {
                                if (type.GetInterface(editorType.FullName) != null)
                                {
                                    IChem4WordEditor plugin = (IChem4WordEditor)Activator.CreateInstance(type);
                                    plugin.SettingsPath = AddInInfo.ProductAppDataPath;
                                    Editors.Add(plugin);
                                    break;
                                }
                            }
                        }

                        #endregion Load Editor(s)

                        #region Load Renderer(s)

                        if (parts[1].Contains("IChem4WordRenderer"))
                        {
                            Assembly asm = Assembly.LoadFile(sourceFile);
                            Type[] types = asm.GetTypes();
                            foreach (Type type in types)
                            {
                                if (type.GetInterface(rendererType.FullName) != null)
                                {
                                    IChem4WordRenderer plugin = (IChem4WordRenderer)Activator.CreateInstance(type);
                                    plugin.SettingsPath = AddInInfo.ProductAppDataPath;
                                    Renderers.Add(plugin);
                                    break;
                                }
                            }
                        }

                        #endregion Load Renderer(s)

                        #region Load Searcher(s)

                        if (parts[1].Contains("IChem4WordSearcher"))
                        {
                            Assembly asm = Assembly.LoadFile(sourceFile);
                            Type[] types = asm.GetTypes();
                            foreach (Type type in types)
                            {
                                if (type.GetInterface(searcherType.FullName) != null)
                                {
                                    IChem4WordSearcher plugin = (IChem4WordSearcher)Activator.CreateInstance(type);
                                    plugin.SettingsPath = AddInInfo.ProductAppDataPath;
                                    Searchers.Add(plugin);
                                    break;
                                }
                            }
                        }

                        #endregion Load Searcher(s)
                    }
                }
            }

            sw.Stop();

            message = $"{module} examining {filesFound} files took {SafeDouble.AsString(sw.ElapsedMilliseconds)}ms";
            Debug.WriteLine(message);
            StartUpTimings.Add(message);
        }

        public IChem4WordEditor GetEditorPlugIn(string name)
        {
            IChem4WordEditor plugin = null;

            if (!string.IsNullOrEmpty(name))
            {
                foreach (IChem4WordEditor ice in Editors)
                {
                    if (ice.Name.Equals(name))
                    {
                        plugin = ice;
                        plugin.Telemetry = Telemetry;
                        plugin.SettingsPath = AddInInfo.ProductAppDataPath;
                        plugin.TopLeft = WordTopLeft;
                        break;
                    }
                }
            }

            return plugin;
        }

        public IChem4WordRenderer GetRendererPlugIn(string name)
        {
            IChem4WordRenderer plugin = null;

            if (!string.IsNullOrEmpty(name))
            {
                foreach (IChem4WordRenderer ice in Renderers)
                {
                    if (ice.Name.Equals(name))
                    {
                        plugin = ice;
                        plugin.Telemetry = Telemetry;
                        plugin.SettingsPath = AddInInfo.ProductAppDataPath;
                        plugin.TopLeft = WordTopLeft;
                        break;
                    }
                }
            }

            return plugin;
        }

        public IChem4WordSearcher GetSearcherPlugIn(string name)
        {
            IChem4WordSearcher plugin = null;

            if (!string.IsNullOrEmpty(name))
            {
                foreach (IChem4WordSearcher ice in Searchers)
                {
                    if (ice.Name.Equals(name))
                    {
                        plugin = ice;
                        plugin.Telemetry = Telemetry;
                        plugin.SettingsPath = AddInInfo.ProductAppDataPath;
                        plugin.TopLeft = WordTopLeft;
                        break;
                    }
                }
            }

            return plugin;
        }

        [HandleProcessCorruptedStateExceptions]
        public void EnableContentControlEvents()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            //Debug.WriteLine("EnableContentControlEvents()");

            try
            {
                if (Application.Documents.Count > 0)
                {
                    // Get reference to active document
                    var wdoc = Globals.Factory.GetVstoObject(Globals.Chem4WordV3.Application.ActiveDocument);

                    // Hook in Content Control Events
                    // See: https://msdn.microsoft.com/en-us/library/Microsoft.Office.Interop.Word.DocumentEvents2_methods%28v=office.14%29.aspx
                    // See: https://msdn.microsoft.com/en-us/library/microsoft.office.interop.word.documentevents2_event_methods%28v=office.14%29.aspx

                    // Remember to add corresponding code in DisableContentControlEvents()

                    // ContentControlOnEnter Event Handler
                    try
                    {
                        wdoc.ContentControlOnEnter -= OnContentControlOnEnter;
                        wdoc.ContentControlOnEnter += OnContentControlOnEnter;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }
                    // ContentControlOnExit Event Handler
                    try
                    {
                        wdoc.ContentControlOnExit -= OnContentControlOnExit;
                        wdoc.ContentControlOnExit += OnContentControlOnExit;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }
                    // ContentControlBeforeDelete Event Handler
                    try
                    {
                        wdoc.ContentControlBeforeDelete -= OnContentControlBeforeDelete;
                        wdoc.ContentControlBeforeDelete += OnContentControlBeforeDelete;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }
                    // ContentControlAfterAdd Event Handler
                    try
                    {
                        wdoc.ContentControlAfterAdd -= OnContentControlAfterAdd;
                        wdoc.ContentControlAfterAdd += OnContentControlAfterAdd;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }

                    EventsEnabled = true;
                }
            }
            catch // (Exception exception)
            {
                // RegistryHelper.StoreException(module, exception)
            }
        }

        [HandleProcessCorruptedStateExceptions]
        public void DisableContentControlEvents()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            //Debug.WriteLine("DisableContentControlEvents()");

            try
            {
                EventsEnabled = false;

                if (Application.Documents.Count > 0)
                {
                    // Get reference to active document
                    var wdoc = Globals.Factory.GetVstoObject(Globals.Chem4WordV3.Application.ActiveDocument);

                    // Hook out Content Control Events
                    // See: https://msdn.microsoft.com/en-us/library/Microsoft.Office.Interop.Word.DocumentEvents2_methods%28v=office.14%29.aspx
                    // See: https://msdn.microsoft.com/en-us/library/microsoft.office.interop.word.documentevents2_event_methods%28v=office.14%29.aspx

                    // Remember to add corresponding code in EnableContentControlEvents()

                    // ContentControlOnEnter Event Handler
                    try
                    {
                        wdoc.ContentControlOnEnter -= OnContentControlOnEnter;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }
                    // ContentControlOnExit Event Handler
                    try
                    {
                        wdoc.ContentControlOnExit -= OnContentControlOnExit;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }
                    // ContentControlBeforeDelete Event Handler
                    try
                    {
                        wdoc.ContentControlBeforeDelete -= OnContentControlBeforeDelete;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }
                    // ContentControlAfterAdd Event Handler
                    try
                    {
                        wdoc.ContentControlAfterAdd -= OnContentControlAfterAdd;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }
                }
            }
            catch // (Exception exception)
            {
                // RegistryHelper.StoreException(module, exception)
            }
        }

        private void SetButtonStates(ButtonState state)
        {
            if (Ribbon != null && _plugInsLoaded)
            {
                // Always enabled
                Ribbon.HelpMenu.Enabled = true;

                bool plugInsLoaded = Editors.Count + Renderers.Count + Searchers.Count > 0;
                // Enabled once any PlugIns are loaded
                Ribbon.ChangeOptions.Enabled = plugInsLoaded;
                IsEnabled = true;

                switch (state)
                {
                    case ButtonState.Disabled:
                    case ButtonState.NoDocument:
                        Ribbon.EditStructure.Enabled = false;
                        Ribbon.EditStructure.Label = "Draw";
                        Ribbon.EditLabels.Enabled = false;
                        Ribbon.ViewCml.Enabled = false;
                        Ribbon.ImportFromFile.Enabled = false;
                        Ribbon.ExportToFile.Enabled = false;
                        Ribbon.ShowAsMenu.Enabled = false;
                        Ribbon.ShowNavigator.Enabled = false;
                        Ribbon.ShowLibrary.Enabled = false;
                        Ribbon.WebSearchMenu.Enabled = false;
                        Ribbon.SaveToLibrary.Enabled = false;
                        Ribbon.ArrangeMolecules.Enabled = false;
                        Ribbon.ButtonsDisabled.Enabled = true;
                        break;

                    case ButtonState.CanEdit:
                        Ribbon.EditStructure.Enabled = plugInsLoaded && Editors.Count > 0;
                        Ribbon.EditStructure.Label = "Edit";
                        Ribbon.EditLabels.Enabled = true;
                        Ribbon.ViewCml.Enabled = true;
                        Ribbon.ImportFromFile.Enabled = false;
                        Ribbon.ExportToFile.Enabled = true;
                        Ribbon.ShowAsMenu.Enabled = true;
                        Ribbon.ShowNavigator.Enabled = true;
                        Ribbon.ShowLibrary.Enabled = true;
                        Ribbon.WebSearchMenu.Enabled = false;
                        Ribbon.SaveToLibrary.Enabled = true;
                        Ribbon.ArrangeMolecules.Enabled = true;
                        Ribbon.ButtonsDisabled.Enabled = false;
                        break;

                    case ButtonState.CanInsert:
                        Ribbon.EditStructure.Enabled = plugInsLoaded && Editors.Count > 0;
                        Ribbon.EditStructure.Label = "Draw";
                        Ribbon.EditLabels.Enabled = false;
                        Ribbon.ViewCml.Enabled = false;
                        Ribbon.ImportFromFile.Enabled = plugInsLoaded;
                        Ribbon.ExportToFile.Enabled = false;
                        Ribbon.ShowAsMenu.Enabled = false;
                        Ribbon.ShowNavigator.Enabled = true;
                        Ribbon.ShowLibrary.Enabled = true;
                        Ribbon.WebSearchMenu.Enabled = plugInsLoaded && Searchers.Count > 0;
                        Ribbon.SaveToLibrary.Enabled = false;
                        Ribbon.ArrangeMolecules.Enabled = false;
                        Ribbon.ButtonsDisabled.Enabled = false;
                        break;
                }

                string betaValue = Globals.Chem4WordV3.ThisVersion.Root?.Element("IsBeta")?.Value;
                bool isBeta = betaValue != null && bool.Parse(betaValue);

                if (IsEndOfLife || isBeta && !VersionAvailableIsBeta)
                {
                    Ribbon.EditStructure.Enabled = false;
                    Ribbon.EditStructure.Label = "Draw";
                    Ribbon.EditLabels.Enabled = false;
                    Ribbon.ViewCml.Enabled = false;
                    Ribbon.ImportFromFile.Enabled = false;
                    Ribbon.ExportToFile.Enabled = false;
                    Ribbon.ShowAsMenu.Enabled = false;
                    Ribbon.ShowNavigator.Enabled = false;
                    Ribbon.ShowLibrary.Enabled = false;
                    Ribbon.WebSearchMenu.Enabled = false;
                    Ribbon.SaveToLibrary.Enabled = false;
                    Ribbon.ArrangeMolecules.Enabled = false;
                    Ribbon.ButtonsDisabled.Enabled = true;

                    IsEnabled = false;
                }
            }
        }

        public void ShowOrHideUpdateShield()
        {
            if (Ribbon != null)
            {
                switch (VersionsBehind)
                {
                    case 0:
                        Ribbon.Update.Visible = false;
                        Ribbon.Update.Enabled = false;
                        Ribbon.Update.Image = Properties.Resources.Shield_Good;
                        ChemistryProhibitedReason = "";
                        break;

                    case 1:
                    case 2:
                    case 3:
                        Ribbon.Update.Visible = true;
                        Ribbon.Update.Enabled = true;
                        Ribbon.Update.Image = Properties.Resources.Shield_Warning;
                        Ribbon.Update.Label = "Update Advised";
                        Ribbon.Update.ScreenTip = "Please update";
                        Ribbon.Update.SuperTip = $"You are {VersionsBehind} versions behind.";
                        ChemistryProhibitedReason = "";
                        break;

                    case 4:
                    case 5:
                    case 6:
                        Ribbon.Update.Visible = true;
                        Ribbon.Update.Enabled = true;
                        Ribbon.Update.Image = Properties.Resources.Shield_Danger;
                        Ribbon.Update.Label = "Update Essential";
                        Ribbon.Update.ScreenTip = "Please update";
                        Ribbon.Update.SuperTip = $"You are {VersionsBehind} versions behind.";
                        ChemistryProhibitedReason = "";
                        break;

                    default:
                        Ribbon.Update.Visible = true;
                        Ribbon.Update.Enabled = true;
                        Ribbon.Update.Image = Properties.Resources.Shield_Danger;
                        Ribbon.Update.Label = "Update to use Chem4Word again";
                        Ribbon.Update.ScreenTip = "You must update to continue using Chem4Word";
                        Ribbon.Update.SuperTip = $"You are {VersionsBehind} versions behind therefore Chem4Word has been disabled as it is too many versions old.";
                        SetButtonStates(ButtonState.Disabled);
                        ChemistryProhibitedReason = Constants.Chem4WordTooOld;
                        break;
                }

                string betaValue = Globals.Chem4WordV3.ThisVersion.Root?.Element("IsBeta")?.Value;
                bool isBeta = betaValue != null && bool.Parse(betaValue);

                if (isBeta && VersionsBehind > 0 && !VersionAvailableIsBeta)
                {
                    Ribbon.Update.Visible = true;
                    Ribbon.Update.Enabled = true;
                    Ribbon.Update.Image = Properties.Resources.Shield_Danger;
                    Ribbon.Update.Label = "Update to use Chem4Word again";
                    Ribbon.Update.ScreenTip = "You must update to continue using Chem4Word";
                    Ribbon.Update.SuperTip = $"Your beta version has been disabled, because a production release '{VersionAvailable}' is available.";
                    SetButtonStates(ButtonState.Disabled);
                    ChemistryProhibitedReason = Constants.Chem4WordIsBeta;
                }

                if (IsEndOfLife)
                {
                    Ribbon.Update.Visible = true;
                    Ribbon.Update.Enabled = true;
                    Ribbon.Update.Image = Properties.Resources.Shield_Danger;
                    Ribbon.Update.Label = "End of Life";
                    Ribbon.Update.ScreenTip = "This version of Chem4Word is no longer supported";
                    Ribbon.Update.SuperTip = "Please see our website https://www.chem4word.co.uk for details of where to obtain a new version from.";
                    SetButtonStates(ButtonState.Disabled);
                    ChemistryProhibitedReason = Constants.Chem4WordTooOld;
                }
            }
        }

        public void SelectChemistry(Word.Selection sel)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            EventsEnabled = false;

            bool chemistrySelected = false;

            try
            {
                if (sel != null)
                {
                    Word.Document doc = sel.Application.ActiveDocument;
                    int ccCount = sel.ContentControls.Count;

                    var targets = (from Word.ContentControl ccs in doc.ContentControls
                                  orderby ccs.Range.Start
                                  where $"{ccs.Title}" == Constants.ContentControlTitle
                                  select ccs).ToList();

                    foreach (Word.ContentControl cc in targets)
                    {
                        // Already Selected
                        if (sel.Range.Start == cc.Range.Start - 1 && sel.Range.End == cc.Range.End + 1)
                        {
                            if (cc.Title != null && cc.Title.Equals(Constants.ContentControlTitle))
                            {
                                NavigatorSupport.SelectNavigatorItem(CustomXmlPartHelper.GuidFromTag(cc.Tag));
                                chemistrySelected = true;
                            }
                            break;
                        }

                        // Inside CC
                        if (sel.Range.Start >= cc.Range.Start && sel.Range.End <= cc.Range.End)
                        {
                            if (cc.Title != null && cc.Title.Equals(Constants.ContentControlTitle))
                            {
                                doc.Application.Selection.SetRange(cc.Range.Start - 1, cc.Range.End + 1);
                                NavigatorSupport.SelectNavigatorItem(CustomXmlPartHelper.GuidFromTag(cc.Tag));
                                chemistrySelected = true;
                            }
                            break;
                        }
                    }

                    if (VersionsBehind >= Constants.MaximumVersionsBehind)
                    {
                        SetButtonStates(ButtonState.Disabled);
                        ChemistryProhibitedReason = Constants.Chem4WordTooOld;
                    }
                    else
                    {
                        if (chemistrySelected)
                        {
                            Ribbon.ActivateChemistryTab();
                            SetButtonStates(ButtonState.CanEdit);
                        }
                        else
                        {
                            if (ccCount == 0)
                            {
                                SetButtonStates(ButtonState.CanInsert);
                            }
                            else
                            {
                                SetButtonStates(ButtonState.NoDocument);
                                ChemistryProhibitedReason = "more than a single content control selected";
                            }
                        }
                    }

                    _chemistrySelected = chemistrySelected;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            finally
            {
                EventsEnabled = true;
            }
        }

        #region Right Click

        #region Events

        private void OnWindowBeforeRightClick(Word.Selection sel, ref bool cancel)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            if (!IsEndOfLife && VersionsBehind < Constants.MaximumVersionsBehind)
            {
                try
                {
                    ClearChemistryContextMenus();
                    EvaluateChemistryAllowed(inRightClick: true);
                    if (ChemistryAllowed)
                    {
                        if (sel.Start != sel.End)
                        {
                            HandleRightClick(sel);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (SystemOptions == null)
                    {
                        LoadOptions();
                    }

                    using (var form = new ReportError(Telemetry, WordTopLeft, module, ex))
                    {
                        form.ShowDialog();
                    }

                    UpdateHelper.ClearSettings();
                    UpdateHelper.CheckForUpdates(SystemOptions.AutoUpdateFrequency);
                }
            }
        }

        private void OnCommandBarButtonClick(CommandBarButton ctrl, ref bool cancelDefault)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                if (SystemOptions == null)
                {
                    LoadOptions();
                }

                _rightClickEvents++;

                ClearChemistryContextMenus();
                if (!_markAsChemistryHandled)
                {
                    TargetWord tw = JsonConvert.DeserializeObject<TargetWord>(ctrl.Tag);

                    var lib = new Libraries.Database.Library(Telemetry, LibraryOptions);
                    string cml = lib.GetChemistryById(tw.ChemistryId);

                    if (cml == null)
                    {
                        UserInteractions.WarnUser($"No match for '{tw.ChemicalName}' was found in your library");
                    }
                    else
                    {
                        Word.Application app = Application;
                        Word.Document doc = app.ActiveDocument;

                        // Generate new CustomXmlPartGuid
                        CMLConverter converter = new CMLConverter();
                        var model = converter.Import(cml);
                        model.CustomXmlPartGuid = Guid.NewGuid().ToString("N");
                        model.EnsureBondLength(SystemOptions.BondLength,
                                               SystemOptions.SetBondLengthOnImportFromLibrary);
                        cml = converter.Export(model);

                        #region Find Id of name

                        string tagPrefix = "";
                        foreach (var mol in model.Molecules.Values)
                        {
                            foreach (var name in mol.Names)
                            {
                                if (tw.ChemicalName.ToLower().Equals(name.Value.ToLower()))
                                {
                                    tagPrefix = name.Id;
                                    break;
                                }
                            }

                            if (!string.IsNullOrEmpty(tagPrefix))
                            {
                                break;
                            }
                        }

                        if (string.IsNullOrEmpty(tagPrefix))
                        {
                            tagPrefix = "c0";
                        }

                        #endregion Find Id of name

                        // Test phrases (ensure benzene is in your library)
                        // This is benzene, this is not.
                        // This is benzene. This is not.

                        Word.ContentControl cc = null;
                        var wordSettings = new WordSettings(app);

                        try
                        {
                            app.ScreenUpdating = false;
                            DisableContentControlEvents();

                            int insertionPoint = tw.Start;
                            doc.Range(tw.Start, tw.Start + tw.ChemicalName.Length).Delete();

                            app.Selection.SetRange(insertionPoint, insertionPoint);

                            string tag = $"{tagPrefix}:{model.CustomXmlPartGuid}";
                            cc = ChemistryHelper.Insert1DChemistry(doc, tw.ChemicalName, true, tag);

                            Telemetry.Write(module, "Information", $"Inserted 1D version of {tw.ChemicalName} from library");
                        }
                        catch (Exception e)
                        {
                            Telemetry.Write(module, "Exception", e.Message);
                            Telemetry.Write(module, "Exception", e.StackTrace);
                        }
                        finally
                        {
                            EnableContentControlEvents();
                            app.ScreenUpdating = true;
                            wordSettings.RestoreSettings(app);
                        }

                        if (cc != null)
                        {
                            doc.CustomXMLParts.Add(cml);
                            app.Selection.SetRange(cc.Range.Start, cc.Range.End);
                        }
                    }

                    _markAsChemistryHandled = true;
                }
            }
            catch (Exception ex)
            {
                if (SystemOptions == null)
                {
                    LoadOptions();
                }

                using (var form = new ReportError(Telemetry, WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }

                UpdateHelper.ClearSettings();
                UpdateHelper.CheckForUpdates(SystemOptions.AutoUpdateFrequency);
            }
        }

        #endregion Events

        #region Methods

        private void HandleRightClick(Word.Selection sel)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            _markAsChemistryHandled = false;
            _rightClickEvents = 0;

            List<TargetWord> selectedWords = new List<TargetWord>();

            try
            {
                if (LibraryNames == null)
                {
                    LoadNamesFromLibrary();
                }

                if (LibraryNames != null && LibraryNames.Any())
                {
                    // Limit to selections which have less than 5 sentences
                    if (sel != null && sel.Sentences != null && sel.Sentences.Count <= 5)
                    {
                        Word.Document doc = Application.ActiveDocument;
                        if (doc != null)
                        {
                            int last = doc.Range().End;
                            int sentenceCount = sel.Sentences.Count;
                            // Handling the selected text sentence by sentence should make us immune to return character sizing.
                            for (int i = 1; i <= sentenceCount; i++)
                            {
                                // GitHub: Issue #10 https://github.com/Chem4Word/Version3/issues/10
                                try
                                {
                                    var sentence = sel.Sentences[i];
                                    int start = Math.Max(sentence.Start, sel.Start);
                                    start = Math.Max(0, start);
                                    int end = Math.Min(sel.End, sentence.End);
                                    end = Math.Min(end, last);
                                    if (start < end)
                                    {
                                        var range = doc.Range(start, end);
                                        //Exclude any ranges which contain content controls
                                        if (range.ContentControls.Count == 0)
                                        {
                                            string sentenceText = range.Text;
                                            if (!string.IsNullOrEmpty(sentenceText))
                                            {
                                                foreach (var kvp in LibraryNames)
                                                {
                                                    int idx = sentenceText.IndexOf(kvp.Key, StringComparison.InvariantCultureIgnoreCase);
                                                    if (idx >= 0)
                                                    {
                                                        var tw = new TargetWord
                                                        {
                                                            ChemicalName = kvp.Key,
                                                            Start = start + idx,
                                                            ChemistryId = kvp.Value,
                                                            End = start + idx + kvp.Key.Length
                                                        };
                                                        selectedWords.Add(tw);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    Telemetry.Write(module, "Exception", $"Handled; Sentences[{i}] of {sentenceCount} not found");
                                    Telemetry.Write(module, "Exception", e.Message);
                                    Telemetry.Write(module, "Exception", e.ToString());
                                }
                            }
                        }
                    }
                }
            }
            catch (COMException cex)
            {
                string comCode = HexErrorCode(cex.ErrorCode);
                switch (comCode)
                {
                    case "0x800A1200":
                        ChemistryAllowed = false;
                        ChemistryProhibitedReason = "can't create a selection object";
                        break;

                    default:
                        // Keep exception hidden from end user.
                        Telemetry.Write(module, "Exception", $"ErrorCode: {comCode}");
                        Telemetry.Write(module, "Exception", cex.Message);
                        Telemetry.Write(module, "Exception", cex.ToString());
                        break;
                }
            }
            catch (Exception ex)
            {
                // Keep exception hidden from end user.
                Telemetry.Write(module, "Exception", ex.Message);
                Telemetry.Write(module, "Exception", ex.ToString());
            }

            if (selectedWords.Count > 0)
            {
                AddChemistryMenuPopup(selectedWords);
            }
        }

        #endregion Methods

        [HandleProcessCorruptedStateExceptions]
        private void AddChemistryMenuPopup(List<TargetWord> selectedWords)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                ClearChemistryContextMenus();

                if (Application.Documents.Count > 0)
                {
                    WordTools.Document doc = Extensions.DocumentExtensions.GetVstoObject(Application.ActiveDocument, Globals.Factory);
                    Application.CustomizationContext = doc.AttachedTemplate;

                    foreach (string contextMenuName in ContextMenusTargets)
                    {
                        CommandBar contextMenu = Application.CommandBars[contextMenuName];
                        if (contextMenu != null)
                        {
                            CommandBarPopup popupControl = (CommandBarPopup)contextMenu.Controls.Add(
                                MsoControlType.msoControlPopup,
                                Type.Missing, Type.Missing, Type.Missing, true);
                            if (popupControl != null)
                            {
                                popupControl.Caption = ContextMenuText;
                                popupControl.Tag = ContextMenuTag;
                                foreach (var word in selectedWords)
                                {
                                    CommandBarButton button = (CommandBarButton)popupControl.Controls.Add(
                                        MsoControlType.msoControlButton,
                                        Type.Missing, Type.Missing, Type.Missing, true);
                                    if (button != null)
                                    {
                                        button.Caption = word.ChemicalName;
                                        button.Tag = JsonConvert.SerializeObject(word);
                                        button.FaceId = 1241;
                                        button.Click += OnCommandBarButtonClick;
                                    }
                                }
                            }
                        }
                    }

                    ((Word.Template)doc.AttachedTemplate).Saved = true;
                }
            }
            catch (Exception exception)
            {
                RegistryHelper.StoreException(module, exception);
            }
        }

        [HandleProcessCorruptedStateExceptions]
        private void ClearChemistryContextMenus()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                if (Application.Documents.Count > 0)
                {
                    WordTools.Document doc = Extensions.DocumentExtensions.GetVstoObject(Application.ActiveDocument, Globals.Factory);
                    Application.CustomizationContext = doc.AttachedTemplate;

                    foreach (string contextMenuName in ContextMenusTargets)
                    {
                        CommandBar contextMenu = Application.CommandBars[contextMenuName];
                        if (contextMenu != null)
                        {
                            CommandBarPopup popupControl = (CommandBarPopup)contextMenu.FindControl(
                                MsoControlType.msoControlPopup, Type.Missing,
                                ContextMenuTag, true, true);
                            if (popupControl != null)
                            {
                                popupControl.Delete(true);
                            }
                        }
                    }

                    ((Word.Template)doc.AttachedTemplate).Saved = true;
                }
            }
            catch (Exception exception)
            {
                RegistryHelper.StoreException(module, exception);
            }
        }

        #endregion Right Click

        #region Document Events

        private void OnNewDocument(Word.Document doc)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            if (VersionsBehind < Constants.MaximumVersionsBehind)
            {
                try
                {
                    if (Ribbon != null)
                    {
                        if (SystemOptions == null && Helper != null)
                        {
                            LoadOptions();
                        }
                    }
                }
                catch (Exception exception)
                {
                    if (SystemOptions == null && Helper != null)
                    {
                        LoadOptions();
                    }

                    if (Telemetry == null || Helper == null)
                    {
                        RegistryHelper.StoreException(module, exception);
                    }
                    else
                    {
                        using (var form = new ReportError(Telemetry, WordTopLeft, module, exception))
                        {
                            form.ShowDialog();
                        }
                    }

                    UpdateHelper.ClearSettings();
                    UpdateHelper.CheckForUpdates(SystemOptions.AutoUpdateFrequency);
                }
            }
        }

        public void CloseLibrary()
        {
            LibraryState = false;
            Ribbon.ShowLibrary.Checked = LibraryState;
            if (Application.Documents.Count > 0)
            {
                HandleLibraryPane(Application.ActiveDocument, false, true);
            }
        }

        private void OnDocumentChange()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            if (VersionsBehind < Constants.MaximumVersionsBehind)
            {
                try
                {
                    if (Application.Documents.Count > 0)
                    {
                        Word.Document doc = null;
                        try
                        {
                            doc = Application.ActiveDocument;
                        }
                        catch (Exception ex1)
                        {
                            // This only happens when document is in protected mode
                            Debug.WriteLine($"Module: {module}; Exception: {ex1.Message}");
                        }

                        if (doc != null)
                        {
                            bool docxMode = doc.CompatibilityMode >= (int)Word.WdCompatibilityMode.wdWord2010;

                            if (Ribbon != null)
                            {
                                Ribbon.ShowNavigator.Checked = false;
                                Ribbon.ShowLibrary.Checked = LibraryState;
                                Ribbon.ShowLibrary.Label = Ribbon.ShowLibrary.Checked ? "Close" : "Open ";
                            }

                            DialogResult answer = Upgrader.UpgradeIsRequired(doc);
                            switch (answer)
                            {
                                case DialogResult.Yes:
                                    if (SystemOptions == null)
                                    {
                                        LoadOptions();
                                    }

                                    Upgrader.DoUpgrade(doc);
                                    break;

                                case DialogResult.No:
                                    Telemetry.Write(module, "Information", "User chose not to upgrade");
                                    break;

                                case DialogResult.Cancel:
                                    // Returns Cancel if nothing to do
                                    break;
                            }

                            HandleNavigatorPane(doc);

                            HandleLibraryPane(doc, docxMode);

                            if (docxMode)
                            {
                                // Call disable first to ensure events not registered multiple times
                                DisableContentControlEvents();
                                EnableContentControlEvents();

                                SelectChemistry(doc.Application.Selection);
                                EvaluateChemistryAllowed();

                                if (!ChemistryAllowed)
                                {
                                    SetButtonStates(ButtonState.NoDocument);
                                }
                            }
                            else
                            {
                                SetButtonStates(ButtonState.NoDocument);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (SystemOptions == null)
                    {
                        LoadOptions();
                    }

                    using (var form = new ReportError(Telemetry, WordTopLeft, module, ex))
                    {
                        form.ShowDialog();
                    }

                    UpdateHelper.ClearSettings();
                    UpdateHelper.CheckForUpdates(SystemOptions.AutoUpdateFrequency);
                }
            }
        }

        private void HandleLibraryPane(Word.Document doc, bool showPane, bool clear = false)
        {
            #region Handle Library Task Panes

            try
            {
                bool libraryFound = false;

                foreach (var taskPane in CustomTaskPanes)
                {
                    if (taskPane.Window != null)
                    {
                        string taskdoc = ((Word.Window) taskPane.Window).Document.Name;
                        if (doc.Name.Equals(taskdoc))
                        {
                            if (taskPane.Title.Equals(Constants.LibraryTaskPaneTitle))
                            {
                                //Debug.WriteLine($"Found Library Task Pane. Visible: {taskPane.Visible}");
                                if (Ribbon != null)
                                {
                                    if (clear)
                                    {
                                        (taskPane.Control as LibraryHost)?.Clear();
                                    }
                                    if (!showPane)
                                    {
                                        Ribbon.ShowLibrary.Checked = false;
                                    }

                                    taskPane.Visible = Ribbon.ShowLibrary.Checked;
                                    Ribbon.ShowLibrary.Label = Ribbon.ShowLibrary.Checked ? "Close" : "Open";
                                }

                                libraryFound = true;
                                break;
                            }
                        }
                    }
                }

                if (!libraryFound)
                {
                    if (Ribbon != null && Ribbon.ShowLibrary.Checked)
                    {
                        if (showPane)
                        {
                            OfficeTools.CustomTaskPane custTaskPane =
                                CustomTaskPanes.Add(new LibraryHost(),
                                                    Constants.LibraryTaskPaneTitle, Application.ActiveWindow);
                            // Opposite side to Navigator's default placement
                            custTaskPane.DockPosition = MsoCTPDockPosition.msoCTPDockPositionLeft;
                            custTaskPane.Width = WordWidth / 4;
                            custTaskPane.VisibleChanged += Ribbon.OnLibraryPaneVisibleChanged;
                            custTaskPane.Visible = true;
                            (custTaskPane.Control as LibraryHost)?.Refresh();
                        }
                    }
                }
            }
            catch
            {
                // Do Nothing
            }

            #endregion Handle Library Task Panes
        }

        private void HandleNavigatorPane(Word.Document doc)
        {
            #region Handle Navigator Task Panes

            try
            {
                foreach (var taskPane in CustomTaskPanes)
                {
                    if (taskPane.Window != null)
                    {
                        string taskdoc = ((Word.Window) taskPane.Window).Document.Name;
                        if (doc.Name.Equals(taskdoc))
                        {
                            if (taskPane.Title.Equals(Constants.NavigatorTaskPaneTitle))
                            {
                                //Debug.WriteLine($"Found Navigator Task Pane. Visible: {taskPane.Visible}");
                                if (Ribbon != null)
                                {
                                    Ribbon.ShowNavigator.Checked = taskPane.Visible;
                                }

                                break;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Do Nothing
            }

            #endregion Handle Navigator Task Panes
        }

        private void OnDocumentOpen(Word.Document doc)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            if (VersionsBehind < Constants.MaximumVersionsBehind)
            {
                try
                {
                    if (Ribbon != null)
                    {
                        if (SystemOptions == null)
                        {
                            LoadOptions();
                        }
                        Debug.WriteLine(module);
                    }
                }
                catch (Exception ex)
                {
                    if (SystemOptions == null)
                    {
                        LoadOptions();
                    }

                    using (var form = new ReportError(Telemetry, WordTopLeft, module, ex))
                    {
                        form.ShowDialog();
                    }

                    UpdateHelper.ClearSettings();
                    UpdateHelper.CheckForUpdates(SystemOptions.AutoUpdateFrequency);
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="doc">The document that is being saved.</param>
        /// <param name="saveAsUi">True if the Save As dialog box is displayed, whether to save a new document, in response to the Save command; or in response to the Save As command; or in response to the SaveAs or SaveAs2 method.</param>
        /// <param name="cancel">False when the event occurs.
        /// If the event procedure sets this argument to True, the document is not saved when the procedure is finished.</param>
        private void OnDocumentBeforeSave(Word.Document doc, ref bool saveAsUi, ref bool cancel)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            if (VersionsBehind < Constants.MaximumVersionsBehind)
            {
                try
                {
                    if (SystemOptions == null)
                    {
                        LoadOptions();
                    }

                    if (!doc.ReadOnly)
                    {
                        if (Upgrader.LegacyChemistryCount(doc) == 0)
                        {
                            // Handle Word 2013+ AutoSave
                            if (WordVersion >= 2013)
                            {
                                if (!doc.IsInAutosave)
                                {
                                    CustomXmlPartHelper.RemoveOrphanedXmlParts(doc);
                                }
                            }
                            else
                            {
                                CustomXmlPartHelper.RemoveOrphanedXmlParts(doc);
                            }
                        }
                    }
                }
                catch (COMException cex)
                {
                    string comCode = HexErrorCode(cex.ErrorCode);
                    switch (comCode)
                    {
                        case "0xE0041804":
                            Telemetry.Write(module, "Exception", $"ErrorCode: {comCode}");
                            Telemetry.Write(module, "Exception", $"Handled {cex.Message}");
                            Telemetry.Write(module, "Exception", cex.ToString());
                            break;

                        default:
                            // Keep exception hidden from end user.
                            Telemetry.Write(module, "Exception", $"ErrorCode: {comCode}");
                            Telemetry.Write(module, "Exception", cex.Message);
                            Telemetry.Write(module, "Exception", cex.ToString());
                            break;
                    }
                }
                catch (Exception ex)
                {
                    RegistryHelper.StoreException(module, ex);

                    if (SystemOptions == null)
                    {
                        LoadOptions();
                    }

                    // Keep exception hidden from end user.
                    //using (var form = new ReportError(Telemetry, WordTopLeft, module, ex))
                    //{
                    //    form.ShowDialog();
                    //}

                    UpdateHelper.ClearSettings();
                    UpdateHelper.CheckForUpdates(SystemOptions.AutoUpdateFrequency);
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="doc">The document that's being closed.</param>
        /// <param name="cancel">False when the event occurs.
        /// If the event procedure sets this argument to True, the document doesn't close when the procedure is finished.</param>
        private void OnDocumentBeforeClose(Word.Document doc, ref bool cancel)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            if (VersionsBehind < Constants.MaximumVersionsBehind)
            {
                try
                {
                    if (SystemOptions == null)
                    {
                        LoadOptions();
                    }

                    if (Ribbon != null)
                    {
                        SetButtonStates(ButtonState.NoDocument);
                    }

                    Word.Application app = Application;
                    OfficeTools.CustomTaskPane custTaskPane = null;

                    foreach (OfficeTools.CustomTaskPane taskPane in CustomTaskPanes)
                    {
                        try
                        {
                            if (app.ActiveWindow == taskPane.Window)
                            {
                                custTaskPane = taskPane;
                            }
                        }
                        catch
                        {
                            // Nothing much we can do here!
                        }
                    }
                    if (custTaskPane != null)
                    {
                        try
                        {
                            CustomTaskPanes.Remove(custTaskPane);
                        }
                        catch
                        {
                            // Nothing much we can do here!
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (SystemOptions == null)
                    {
                        LoadOptions();
                    }

                    using (var form = new ReportError(Telemetry, WordTopLeft, module, ex))
                    {
                        form.ShowDialog();
                    }

                    UpdateHelper.ClearSettings();
                    UpdateHelper.CheckForUpdates(SystemOptions.AutoUpdateFrequency);
                }
            }
        }

        #endregion Document Events

        #region Window Events

        /// <summary>
        ///
        /// </summary>
        /// <param name="sel">The text selected.
        /// If no text is selected, the Sel parameter returns either nothing or the first character to the right of the insertion point.</param>
        private void OnWindowSelectionChange(Word.Selection sel)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            if (VersionsBehind < Constants.MaximumVersionsBehind)
            {
                try
                {
                    if (EventsEnabled)
                    {
                        EventsEnabled = false;

                        SelectChemistry(sel);
                        EvaluateChemistryAllowed();

                        if (!ChemistryAllowed)
                        {
                            SetButtonStates(ButtonState.NoDocument);
                        }

                        EventsEnabled = true;
                    }

                    // Deliberate crash to test Error Reporting
                    //int ii = 2;
                    //int dd = 0;
                    //int bang = ii / dd;
                }
                catch (ThreadAbortException tex)
                {
                    RegistryHelper.StoreException(module, tex);
                }
                catch (Exception ex)
                {
                    if (SystemOptions == null)
                    {
                        LoadOptions();
                    }

                    if (Telemetry != null)
                    {
                        using (var form = new ReportError(Telemetry, WordTopLeft, module, ex))
                        {
                            form.ShowDialog();
                        }
                    }
                    else
                    {
                        RegistryHelper.StoreException(module, ex);
                    }

                    UpdateHelper.ClearSettings();
                    UpdateHelper.CheckForUpdates(SystemOptions.AutoUpdateFrequency);
                }
            }
        }

        public void EvaluateChemistryAllowed(bool inRightClick = false)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            string betaValue = Globals.Chem4WordV3.ThisVersion.Root?.Element("IsBeta")?.Value;
            bool isBeta = betaValue != null && bool.Parse(betaValue);

            if (IsEndOfLife || VersionsBehind >= Constants.MaximumVersionsBehind
                            || !WordIsActivated
                            || isBeta && !VersionAvailableIsBeta)
            {
                if (isBeta && !VersionAvailableIsBeta)
                {
                    ChemistryProhibitedReason = Constants.Chem4WordIsBeta;
                    ChemistryAllowed = false;
                }

                if (IsEndOfLife || VersionsBehind >= Constants.MaximumVersionsBehind)
                {
                    ChemistryProhibitedReason = Constants.Chem4WordTooOld;
                    ChemistryAllowed = false;
                }

                if (!WordIsActivated)
                {
                    ChemistryProhibitedReason = Constants.WordIsNotActivated;
                    ChemistryAllowed = false;
                }
            }
            else
            {
                bool allowed = true;

                try
                {
                    if (Application.Documents != null && Application.Documents.Count > 0)
                    {
                        Word.Document doc = null;
                        try
                        {
                            doc = Application.ActiveDocument;
                        }
                        catch
                        {
                            // This only happens when document is in protected mode
                            allowed = false;
                            doc = null;
                            ChemistryProhibitedReason = "document is readonly.";
                        }

                        if (allowed && doc != null)
                        {
                            try
                            {
                                if (doc.CompatibilityMode < (int)Word.WdCompatibilityMode.wdWord2010)
                                {
                                    allowed = false;
                                    ChemistryProhibitedReason = "document is in compatibility mode.";
                                }
                            }
                            catch
                            {
                                allowed = false;
                                ChemistryProhibitedReason = "can't determine if document is in compatibility mode.";
                            }

                            try
                            {
                                //if (doc.CoAuthoring.Conflicts.Count > 0) // <-- This clears current selection ???
                                if (doc.CoAuthoring.Locks.Count > 0)
                                {
                                    allowed = false;
                                    ChemistryProhibitedReason = "document is in co-authoring mode.";
                                }
                            }
                            catch
                            {
                                // CoAuthoring or Conflicts/Locks may not be initialised!
                            }

                            try
                            {
                                if (allowed && doc.IsSubdocument)
                                {
                                    ChemistryProhibitedReason = "current document is a sub document.";
                                    allowed = false;
                                }
                            }
                            catch
                            {
                                allowed = false;
                                ChemistryProhibitedReason = "can't determine if document is a sub document.";
                            }

                            Word.Selection sel = Application.Selection;
                            if (allowed && sel != null)
                            {
                                if (!inRightClick)
                                {
                                    if (allowed && sel.Start != sel.End)
                                    {
                                        if (!string.IsNullOrEmpty(sel.Text) && sel.Text.Contains("\r"))
                                        {
                                            ChemistryProhibitedReason = "selection contains Line Ending";
                                            allowed = false;
                                        }
                                    }

                                    if (allowed && sel.Paragraphs.Count > 1)
                                    {
                                        ChemistryProhibitedReason = $"selection contains {sel.Paragraphs.Count} paragraphs.";
                                        allowed = false;
                                    }
                                }

                                if (allowed && sel.OMaths.Count > 0)
                                {
                                    ChemistryProhibitedReason = "selection is in an Equation.";
                                    allowed = false;
                                }

                                if (allowed && sel.Tables.Count > 0)
                                {
                                    try
                                    {
                                        if (sel.Cells.Count > 1)
                                        {
                                            ChemistryProhibitedReason = "selection contains more than one cell of a table.";
                                            allowed = false;
                                        }
                                    }
                                    catch
                                    {
                                        // Cells may not be initialised!
                                    }
                                }

                                if (allowed)
                                {
                                    try
                                    {
                                        Word.WdStoryType story = sel.StoryType;
                                        if (story != Word.WdStoryType.wdMainTextStory)
                                        {
                                            ChemistryProhibitedReason = $"selection is in a '{DecodeStoryType(story)}' story.";
                                            allowed = false;
                                        }
                                    }
                                    catch
                                    {
                                        // ComException 0x80004005
                                        ChemistryProhibitedReason = "can't determine which part of the story the selection point is.";
                                        allowed = false;
                                    }
                                }

                                if (allowed)
                                {
                                    int ccCount = sel.ContentControls.Count;
                                    if (ccCount > 1)
                                    {
                                        allowed = false;
                                        ChemistryProhibitedReason = "more than one ContentControl is selected";
                                    }
                                }

                                if (allowed)
                                {
                                    Word.WdContentControlType? contentControlType = null;
                                    string title = "";
                                    foreach (Word.ContentControl ccd in doc.ContentControls)
                                    {
                                        if (ccd.Range.Start <= sel.Range.Start && ccd.Range.End >= sel.Range.End)
                                        {
                                            contentControlType = ccd.Type;
                                            title = ccd.Title;
                                            break;
                                        }
                                    }

                                    if (contentControlType != null)
                                    {
                                        if (!string.IsNullOrEmpty(title) && title.Equals(Constants.ContentControlTitle))
                                        {
                                            // Handle old Word 2007 style
                                            if (contentControlType != Word.WdContentControlType.wdContentControlRichText
                                                && contentControlType != Word.WdContentControlType.wdContentControlPicture)
                                            {
                                                allowed = false;
                                                ChemistryProhibitedReason =
                                                    $"selection is in a '{DecodeContentControlType(contentControlType)}' Content Control.";
                                            }
                                        }
                                        else
                                        {
                                            if (contentControlType != Word.WdContentControlType.wdContentControlRichText)
                                            {
                                                allowed = false;
                                                ChemistryProhibitedReason =
                                                    $"selection is in a '{DecodeContentControlType(contentControlType)}' Content Control";
                                            }

                                            // Test for Shape inside CC which is not ours
                                            if (allowed)
                                            {
                                                try
                                                {
                                                    if (sel.ShapeRange.Count > 0)
                                                    {
                                                        ChemistryProhibitedReason = "selection contains shape(s) inside Content Control.";
                                                        allowed = false;
                                                    }
                                                }
                                                catch
                                                {
                                                    // Shape may not evaluate
                                                }
                                            }
                                        }
                                    }
                                }

                                // Test for Shape in document body
                                if (allowed)
                                {
                                    try
                                    {
                                        if (sel.ShapeRange.Count > 0)
                                        {
                                            ChemistryProhibitedReason = "selection contains shape(s).";
                                            allowed = false;
                                        }
                                    }
                                    catch
                                    {
                                        // Shape may not evaluate
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        allowed = false;
                        ChemistryProhibitedReason = "no document is open.";
                    }
                }
                catch (COMException cex)
                {
                    string comCode = HexErrorCode(cex.ErrorCode);
                    switch (comCode)
                    {
                        case "0x80004005":
                            ChemistryAllowed = false;
                            ChemistryProhibitedReason = "can't determine where the current selection is.";
                            break;

                        case "0x800A11FD":
                            ChemistryAllowed = false;
                            ChemistryProhibitedReason = "changes are not permitted in the current selection.";
                            break;

                        case "0x800A1759":
                            ChemistryAllowed = false;
                            ChemistryProhibitedReason = "can't create a selection when a dialogue is active.";
                            break;

                        default:
                            ChemistryAllowed = false;
                            ChemistryProhibitedReason = $"COMException {cex.Message} ErrorCode: {comCode}";
                            if (Telemetry != null)
                            {
                                // Keep exception hidden from end user.
                                Telemetry.Write(module, "Exception", $"ErrorCode: {comCode}");
                                Telemetry.Write(module, "Exception", cex.Message);
                                Telemetry.Write(module, "Exception", cex.ToString());
                            }
                            else
                            {
                                RegistryHelper.StoreException(module, cex);
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    if (Telemetry != null)
                    {
                        // Keep exception hidden from end user.
                        Telemetry.Write(module, "Exception", ex.Message);
                        Telemetry.Write(module, "Exception", ex.ToString());
                    }
                    else
                    {
                        RegistryHelper.StoreException(module, ex);
                    }
                }

                ChemistryAllowed = allowed;
                if (!allowed)
                {
                    Debug.WriteLine($"ChemistryProhibitedReason: {ChemistryProhibitedReason}");
                }
            }
        }

        private string HexErrorCode(int code)
        {
            try
            {
                long x = code & 0xFFFFFFFF;
                return string.Format("0x{0:X}", x);
            }
            catch
            {
                return string.Empty;
            }
        }

        private object DecodeStoryType(Word.WdStoryType storyType)
        {
            // Data from https://msdn.microsoft.com/en-us/vba/word-vba/articles/wdstorytype-enumeration-word
            string result;

            switch (storyType)
            {
                case Word.WdStoryType.wdCommentsStory:
                    result = "Comments";
                    break;

                case Word.WdStoryType.wdEndnoteContinuationNoticeStory:
                    result = "Endnote continuation notice";
                    break;

                case Word.WdStoryType.wdEndnoteContinuationSeparatorStory:
                    result = "Endnote continuation separator";
                    break;

                case Word.WdStoryType.wdEndnoteSeparatorStory:
                    result = "Endnote separator";
                    break;

                case Word.WdStoryType.wdEndnotesStory:
                    result = "Endnotes";
                    break;

                case Word.WdStoryType.wdEvenPagesFooterStory:
                    result = "Even pages footer";
                    break;

                case Word.WdStoryType.wdEvenPagesHeaderStory:
                    result = "Even pages header";
                    break;

                case Word.WdStoryType.wdFirstPageFooterStory:
                    result = "First page footer";
                    break;

                case Word.WdStoryType.wdFirstPageHeaderStory:
                    result = "First page header";
                    break;

                case Word.WdStoryType.wdFootnoteContinuationNoticeStory:
                    result = "Footnote continuation notice";
                    break;

                case Word.WdStoryType.wdFootnoteContinuationSeparatorStory:
                    result = "Footnote continuation separator";
                    break;

                case Word.WdStoryType.wdFootnoteSeparatorStory:
                    result = "Footnote separator";
                    break;

                case Word.WdStoryType.wdFootnotesStory:
                    result = "Footnotes";
                    break;

                case Word.WdStoryType.wdMainTextStory:
                    result = "Main text";
                    break;

                case Word.WdStoryType.wdPrimaryFooterStory:
                    result = "Primary footer";
                    break;

                case Word.WdStoryType.wdPrimaryHeaderStory:
                    result = "Primary header";
                    break;

                case Word.WdStoryType.wdTextFrameStory:
                    result = "Text frame";
                    break;

                default:
                    result = storyType.ToString();
                    break;
            }

            return result;
        }

        private static string DecodeContentControlType(Word.WdContentControlType? contentControlType)
        {
            // Date from https://msdn.microsoft.com/en-us/library/microsoft.office.interop.word.wdcontentcontroltype(v=office.14).aspx
            string result;

            switch (contentControlType)
            {
                case Word.WdContentControlType.wdContentControlRichText:
                    result = "Rich-Text";
                    break;

                case Word.WdContentControlType.wdContentControlText:
                    result = "Text";
                    break;

                case Word.WdContentControlType.wdContentControlBuildingBlockGallery:
                    result = "Picture";
                    break;

                case Word.WdContentControlType.wdContentControlComboBox:
                    result = "ComboBox";
                    break;

                case Word.WdContentControlType.wdContentControlDropdownList:
                    result = "Drop-Down List";
                    break;

                case Word.WdContentControlType.wdContentControlPicture:
                    result = "Building Block Gallery";
                    break;

                case Word.WdContentControlType.wdContentControlDate:
                    result = "Date";
                    break;

                case Word.WdContentControlType.wdContentControlGroup:
                    result = "Group";
                    break;

                case Word.WdContentControlType.wdContentControlCheckBox:
                    result = "CheckBox";
                    break;

                case Word.WdContentControlType.wdContentControlRepeatingSection:
                    result = "Repeating Section";
                    break;

                default:
                    result = contentControlType.ToString();
                    break;
            }

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sel">The current selection.</param>
        /// <param name="cancel">False when the event occurs.
        /// If the event procedure sets this argument to True, the default double-click action does not occur when the procedure is finished.</param>
        private void OnWindowBeforeDoubleClick(Word.Selection sel, ref bool cancel)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            if (VersionsBehind < Constants.MaximumVersionsBehind)
            {
                try
                {
                    //Debug.WriteLine($"{module.Replace("()", $"({sel.Document.Name})")}");
                    //Debug.WriteLine("  Selection: from " + sel.Range.Start + " to " + sel.Range.End);

                    if (EventsEnabled && _chemistrySelected)
                    {
                        CustomRibbon.PerformEdit();
                    }
                }
                catch (Exception ex)
                {
                    if (SystemOptions == null)
                    {
                        LoadOptions();
                    }

                    using (var form = new ReportError(Telemetry, WordTopLeft, module, ex))
                    {
                        form.ShowDialog();
                    }

                    UpdateHelper.ClearSettings();
                    UpdateHelper.CheckForUpdates(SystemOptions.AutoUpdateFrequency);
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="win"></param>
        private void OnWindowActivate(Word.Document doc, Word.Window win)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            if (VersionsBehind < Constants.MaximumVersionsBehind)
            {
                try
                {
                    Debug.WriteLine($"{module.Replace("()", $"({doc.Name})")}");

                    EvaluateChemistryAllowed();

                    // Deliberate crash to test Error Reporting
                    //int ii = 2;
                    //int dd = 0;
                    //int bang = ii / dd;
                }
                catch (Exception ex)
                {
                    if (SystemOptions == null)
                    {
                        LoadOptions();
                    }

                    using (var form = new ReportError(Telemetry, WordTopLeft, module, ex))
                    {
                        form.ShowDialog();
                    }

                    UpdateHelper.ClearSettings();
                    UpdateHelper.CheckForUpdates(SystemOptions.AutoUpdateFrequency);
                }
            }
        }

        #endregion Window Events

        #region Content Control Events

        /// <summary>
        ///
        /// </summary>
        /// <param name="NewContentControl"></param>
        /// <param name="InUndoRedo"></param>
        private void OnContentControlAfterAdd(Word.ContentControl NewContentControl, bool InUndoRedo)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                //Debug.WriteLine($"{module}");

                if (SystemOptions == null)
                {
                    LoadOptions();
                }

                string ccId = NewContentControl.ID;
                string ccTag = NewContentControl.Tag;
                if (!InUndoRedo && !string.IsNullOrEmpty(ccTag))
                {
                    if (!ccId.Equals(_lastContentControlAdded))
                    {
                        // Check that tag looks like it might be a C4W Tag
                        var regex = @"^[0-9a-fmn.:]+$";
                        Match match = Regex.Match(ccTag, regex, RegexOptions.IgnoreCase);
                        if (match.Success)
                        {
                            string message = $"ContentControl {ccId} added; Looking for structure {ccTag}";
                            //Debug.WriteLine("  " + message);
                            Telemetry.Write(module, "Information", message);

                            Word.Document doc = NewContentControl.Application.ActiveDocument;
                            Word.Application app = Application;
                            CustomXMLPart cxml = CustomXmlPartHelper.GetCustomXmlPart(ccTag, app.ActiveDocument);
                            if (cxml != null)
                            {
                                Telemetry.Write(module, "Information", "Found copy of " + ccTag + " in this document.");
                            }
                            else
                            {
                                if (doc.Application.Documents.Count > 1)
                                {
                                    Word.Application app1 = Application;
                                    cxml = CustomXmlPartHelper.FindCustomXmlPart(ccTag, app1.ActiveDocument);
                                    if (cxml != null)
                                    {
                                        Telemetry.Write(module, "Information", "Found copy of " + ccTag + " in other document, adding it into this.");

                                        // Generate new molecule Guid and apply it
                                        string newGuid = Guid.NewGuid().ToString("N");
                                        NewContentControl.Tag = newGuid;

                                        CMLConverter cmlConverter = new CMLConverter();
                                        Model model = cmlConverter.Import(cxml.XML);
                                        model.CustomXmlPartGuid = newGuid;
                                        doc.CustomXMLParts.Add(cmlConverter.Export(model));
                                    }
                                }
                            }
                        }

                        _lastContentControlAdded = ccId;
                    }
                }
            }
            catch (Exception ex)
            {
                if (SystemOptions == null)
                {
                    LoadOptions();
                }

                using (var form = new ReportError(Telemetry, WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }

                UpdateHelper.ClearSettings();
                UpdateHelper.CheckForUpdates(SystemOptions.AutoUpdateFrequency);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="contentControl"></param>
        /// <param name="Cancel"></param>
        private void OnContentControlBeforeDelete(Word.ContentControl contentControl, bool Cancel)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                //Debug.WriteLine($"{module.Replace("()", $"({contentControl.Application.ActiveDocument.Name})")}");

                if (SystemOptions == null)
                {
                    LoadOptions();
                }
            }
            catch (Exception ex)
            {
                if (SystemOptions == null)
                {
                    LoadOptions();
                }

                using (var form = new ReportError(Telemetry, WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }

                UpdateHelper.ClearSettings();
                UpdateHelper.CheckForUpdates(SystemOptions.AutoUpdateFrequency);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="contentControl"></param>
        /// <param name="Cancel"></param>
        private void OnContentControlOnExit(Word.ContentControl contentControl, ref bool Cancel)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                if (SystemOptions == null)
                {
                    LoadOptions();
                }

                //Debug.WriteLine($"{module.Replace("()", $"({contentControl.Application.ActiveDocument.Name})")}");
            }
            catch (Exception ex)
            {
                if (SystemOptions == null)
                {
                    LoadOptions();
                }

                using (var form = new ReportError(Telemetry, WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }

                UpdateHelper.ClearSettings();
                UpdateHelper.CheckForUpdates(SystemOptions.AutoUpdateFrequency);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="contentControl"></param>
        private void OnContentControlOnEnter(Word.ContentControl contentControl)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                if (SystemOptions == null)
                {
                    LoadOptions();
                }

                if (EventsEnabled)
                {
                    EventsEnabled = false;
#if DEBUG
                    //Word.Document doc = contentControl.Application.ActiveDocument
                    //Word.Selection sel = doc.Application.Selection
                    //Debug.WriteLine($"  OnContentControlOnEnter() CC: {contentControl.Title}")
                    //Debug.WriteLine($"  OnContentControlOnEnter() Document: {doc.Name} Selection from {sel.Range.Start} to {sel.Range.End}")
                    //Debug.WriteLine($"  OnContentControlOnEnter() Document: {doc.Name} Selection has {sel.ContentControls.Count} CCs")
#endif
                    EvaluateChemistryAllowed();
                    EventsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                if (SystemOptions == null)
                {
                    LoadOptions();
                }

                using (var form = new ReportError(Telemetry, WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }

                UpdateHelper.ClearSettings();
                UpdateHelper.CheckForUpdates(SystemOptions.AutoUpdateFrequency);
            }
        }

        #endregion Content Control Events

        #region VSTO generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InternalStartup()
        {
            this.Startup += new EventHandler(C4WAddIn_Startup);
            this.Shutdown += new EventHandler(C4WAddIn_Shutdown);
        }

        #endregion VSTO generated code
    }

    public enum ButtonState
    {
        Disabled,
        NoDocument,
        CanEdit,
        CanInsert
    }
}