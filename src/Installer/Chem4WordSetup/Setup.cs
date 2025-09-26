// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Shared;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Chem4WordSetup
{
    public partial class Setup : Form
    {
        private const string VersionsFile = "files3-2/Chem4Word-Versions.xml";
        private const string PrimaryDomain = "https://www.chem4word.co.uk";
        private static readonly string[] OurDomains = { "https://www.chem4word.co.uk", "http://www.chem4word.com", "https://chem4word.azurewebsites.net" };
        private const string VersionsFileMarker = "<Id>f3c4f4db-2fff-46db-b14a-feb8e09f7742</Id>";

        private const string RegistryKeyName = @"SOFTWARE\Chem4Word V3";

        private const string DetectV2AddIn = @"Chemistry Add-in for Word\Chem4Word.AddIn.vsto";
        private const string DetectV3AddIn = @"Chem4Word V3\Chem4Word.V3.dll";

        private const string DefaultMsiFile = "https://www.chem4word.co.uk/files3-2/Chem4Word-Setup.3.2.23.Release.19.msi";
        private const string VstoInstaller = "https://www.chem4word.co.uk/files3-2/vstor_redist.exe";

        private bool _isHigherVersionInstalled;
        private Version _currentVersion;

        private WebClient _webClient;
        private string _downloadedFile = string.Empty;
        private string _downloadSource = string.Empty;
        private State _state = State.Done;
        private State _previousState = State.Done;
        private string _latestVersion = string.Empty;
        private int _retryCount;
        private string _domainUsed = string.Empty;
        private Stopwatch _sw = new Stopwatch();

        public Setup()
        {
            InitializeComponent();
        }

        private void OnLoad_Setup(object sender, EventArgs e)
        {
            // Move up and left by half the form size
            Left -= Width / 2;
            Top -= Height / 2;

            Show();
            Application.DoEvents();

            var isDesignTimeInstalled = false;
            var isRuntimeInstalled = false;
            var isOperatingSystemWindows7Plus = false;

            #region Detect Windows Version

            var osVer = Environment.OSVersion;
            // Check that OsVersion is greater or equal to 6.1
            if (osVer.Version.Major >= 6 && osVer.Version.Minor >= 1
                || osVer.Version.Major >= 10)
            {
                // Running Windows 7 or Windows 2008 R2
                isOperatingSystemWindows7Plus = true;
            }

            #endregion Detect Windows Version

            WindowsInstalled.Indicator = isOperatingSystemWindows7Plus ? Properties.Resources.Windows : Properties.Resources.Halt;
            Application.DoEvents();

            #region Detect Word

            var isWordInstalled = OfficeHelper.GetWinWordVersionNumber() >= 2010;

            #endregion Detect Word

            WordInstalled.Indicator = isWordInstalled ? Properties.Resources.Word : Properties.Resources.Halt;
            WordRunning.Indicator = Properties.Resources.Halt;
            Application.DoEvents();

            if (isWordInstalled)
            {
                if (WordIsRunning())
                {
                    RegistryHelper.WriteAction("Word is running");
                    WordRunning.Indicator = Properties.Resources.No;
                    Information.Text = "Please close all instances of Microsoft Word to continue.";
                    Action.Text = "Waiting";
                    Action.Enabled = false;
                    timer2.Enabled = true;
                }
                else
                {
                    WordRunning.Description = "Microsoft Word is not running";
                    WordRunning.Indicator = Properties.Resources.Yes;
                }
            }

            #region .Net Framework

            // HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\NET Framework Setup\NDP
            // Not sure if this can be done as this is a .Net 4.5.2 app !

            #endregion .Net Framework

            #region Detect Design Time VSTO

            var feature = GetRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VSTO_DT\VS10\Feature");
            if (!string.IsNullOrEmpty(feature))
            {
                isDesignTimeInstalled = true;
            }

            #endregion Detect Design Time VSTO

            #region Detect Runtime VSTO

            var vstoVersion = GetRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VSTO Runtime Setup\v4R", "Version");
            if (string.IsNullOrEmpty(vstoVersion))
            {
                vstoVersion = GetRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\VSTO Runtime Setup\v4R", "Version");
            }

            var minimumVstoVersion = new Version("10.0.60724");
            var result = -2;

            if (!string.IsNullOrEmpty(vstoVersion))
            {
                var installedVersion = new Version(vstoVersion);
                result = installedVersion.CompareTo(minimumVstoVersion);

                if (result >= 0)
                {
                    isRuntimeInstalled = true;
                }
            }

            // SOFTWARE\Microsoft\VSTO_DT\VS10\Feature
            if (!isRuntimeInstalled)
            {
                vstoVersion = GetRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VSTO_DT\VS10\Feature");

                if (string.IsNullOrEmpty(vstoVersion))
                {
                    vstoVersion = GetRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\VSTO_DT\VS10\Feature");
                }

                if (!string.IsNullOrEmpty(vstoVersion))
                {
                    isRuntimeInstalled = true;
                }
            }

            #endregion Detect Runtime VSTO

            if (isDesignTimeInstalled || isRuntimeInstalled)
            {
                VstoInstalled.Indicator = Properties.Resources.Yes;
                _state = State.DownloadChem4Word;
            }
            else
            {
                VstoInstalled.Indicator = Properties.Resources.Waiting;
                _state = State.DownloadVsto;
            }
            Application.DoEvents();

            #region Is Chem4Word Installed

            var thisVersion = Assembly.GetExecutingAssembly().GetName().Version;
            _currentVersion = GetCurrentVersion();

            if (_currentVersion >= thisVersion)
            {
                _isHigherVersionInstalled = true;
            }

            var isChem4WordVersion2Installed = FindOldVersion();

            #endregion Is Chem4Word Installed

            if (isOperatingSystemWindows7Plus && isWordInstalled)
            {
                if (isChem4WordVersion2Installed)
                {
                    RegistryHelper.WriteAction("Old Version of Chem4Word detected");
                    AddInInstalled.Indicator = Properties.Resources.Halt;
                    AddInInstalled.Description = "Version 2 of Chem4Word detected";
                    Information.Text = "A previous version of Chem4Word has been detected, please uninstall it.";
                    Action.Text = "Cancel";
                    Action.Enabled = true;
                }
                else if (_isHigherVersionInstalled)
                {
                    RegistryHelper.WriteAction($"Newer Chem4Word detected V{_currentVersion}");
                    AddInInstalled.Indicator = Properties.Resources.Halt;
                    AddInInstalled.Description = $"V{_currentVersion} of Chem4Word detected";
                    Information.Text = "Can't continue, because a newer version of Chem4Word has been detected.";
                    Action.Text = "Cancel";
                    Action.Enabled = true;
                }
                else
                {
                    AddInInstalled.Indicator = Properties.Resources.Waiting;
                    Application.DoEvents();

                    RegistryHelper.WriteAction("Downloading Chem4Word-Versions.xml");
                    var xml = GetVersionsXmlFile();
                    if (!string.IsNullOrEmpty(xml))
                    {
                        var x = XDocument.Parse(xml);
                        var versions = x.XPathSelectElements("//Version");
                        foreach (var element in versions)
                        {
                            if (string.IsNullOrEmpty(_latestVersion))
                            {
                                _latestVersion = element.Element("Url")?.Value;
                                RegistryHelper.WriteAction($"Latest version is {_latestVersion}");
                            }
                            break;
                        }
                    }

                    // Default to Specific Beta
                    if (string.IsNullOrEmpty(_latestVersion))
                    {
                        _latestVersion = ChangeDomain(DefaultMsiFile);
                        RegistryHelper.WriteAction($"Defaulting to {_latestVersion}");
                    }
                }
            }
            else
            {
                if (!isWordInstalled)
                {
                    WordInstalled.Indicator = Properties.Resources.No;
                    Information.Text = "Please install Microsoft Word (2010, 2013, 2016, 2019, 2024 or 365).";
                }

                if (!isOperatingSystemWindows7Plus)
                {
                    WindowsInstalled.Indicator = Properties.Resources.No;
                    Information.Text = "This program requires Windows 7 or greater.";
                }

                VstoInstalled.Indicator = Properties.Resources.Halt;
                AddInInstalled.Indicator = Properties.Resources.Halt;

                Action.Text = "Cancel";
            }
        }

        private bool WordIsRunning()
        {
            // Detect if word is running
            var processes = Process.GetProcessesByName("winword");
            return processes.Length != 0;
        }

        private string GetRegistryValue(string keyName, string valueName = "")
        {
            var result = "";

            try
            {
                var value = Registry.GetValue(keyName, valueName, null);
                if (value != null)
                {
                    result = value.ToString();
                }
            }
            catch
            {
                // Just return empty string
            }

            return result;
        }

        private string ChangeDomain(string input)
        {
            var output = input;

            if (!string.IsNullOrEmpty(_domainUsed))
            {
                if (!_domainUsed.Equals(PrimaryDomain))
                {
                    output = input.Replace(PrimaryDomain, _domainUsed);
                }
            }

            return output;
        }

        private string GetVersionsXmlFile()
        {
            string contents = null;

            var foundOurXmlFile = false;

            var securityProtocol = ServicePointManager.SecurityProtocol;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            foreach (var domain in OurDomains)
            {
                using (var client = new HttpClient())
                {
                    string exceptionMessage;

                    try
                    {
                        RegistryHelper.WriteAction($"Looking for Chem4Word-Versions.xml at {domain}");

                        client.DefaultRequestHeaders.Add("user-agent", "Chem4Word Bootstrapper");
                        client.BaseAddress = new Uri(domain);
                        var response = client.GetAsync(VersionsFile).Result;
                        response.EnsureSuccessStatusCode();
                        Debug.Write(response.StatusCode);

                        var result = response.Content.ReadAsStringAsync().Result;
                        if (result.Contains(VersionsFileMarker))
                        {
                            foundOurXmlFile = true;
                            _domainUsed = domain;
                            contents = ChangeDomain(result);
                        }
                        else
                        {
                            RegistryHelper.WriteAction($"Chem4Word-Versions.xml at {domain} is corrupt");
                        }
                    }
                    catch (ArgumentNullException nex)
                    {
                        exceptionMessage = GetExceptionMessages(nex);
                        RegistryHelper.WriteAction($"ArgumentNullException: [{domain}] - {exceptionMessage}");
                    }
                    catch (HttpRequestException hex)
                    {
                        exceptionMessage = GetExceptionMessages(hex);
                        RegistryHelper.WriteAction($"HttpRequestException: [{domain}] - {exceptionMessage}");
                    }
                    catch (WebException wex)
                    {
                        exceptionMessage = GetExceptionMessages(wex);
                        RegistryHelper.WriteAction($"WebException: [{domain}] - {exceptionMessage}");
                    }
                    catch (Exception ex)
                    {
                        exceptionMessage = GetExceptionMessages(ex);
                        RegistryHelper.WriteAction($"Exception: [{domain}] - {exceptionMessage}");
                    }

                    if (foundOurXmlFile)
                    {
                        break;
                    }
                }
            }

            ServicePointManager.SecurityProtocol = securityProtocol;
            return contents;
        }

        private string GetExceptionMessages(Exception ex)
        {
            var message = ex.Message;

            if (ex.InnerException != null)
            {
                message = message + Environment.NewLine + GetExceptionMessages(ex.InnerException);
            }

            return message;
        }

        private void OnClick_Action(object sender, EventArgs e)
        {
            if (Action.Text.Equals("Install"))
            {
                timer1.Enabled = true;
                Action.Enabled = false;
            }
            else
            {
                Close();
            }
        }

        private void HandleNextState()
        {
            switch (_state)
            {
                case State.DownloadVsto:
                    // Download VSTO
                    RegistryHelper.WriteAction("Downloading VSTO");
                    Information.Text = "Downloading VSTO ...";
                    VstoInstalled.Indicator = Properties.Resources.Downloading;
                    if (DownloadFile(ChangeDomain(VstoInstaller)))
                    {
                        _previousState = _state;
                        _state = State.WaitingForVstoDownload;
                    }
                    else
                    {
                        VstoInstalled.Indicator = Properties.Resources.No;
                        Information.Text = $"Error downloading VSTO; {Information.Text}";
                        Action.Text = "Exit";
                        _state = State.Done;
                    }
                    break;

                case State.WaitingForVstoDownload:
                    break;

                case State.InstallVsto:
                    // Install VSTO
                    RegistryHelper.WriteAction("Installing VSTO");
                    Information.Text = "Installing VSTO ...";
                    try
                    {
                        VstoInstalled.Indicator = Properties.Resources.Runing;
                        _state = State.WaitingForInstaller;
                        var exitCode = RunProcess(_downloadedFile, "/passive /norestart");
                        RegistryHelper.WriteAction($"VSTO ExitCode: {exitCode}");
                        switch (exitCode)
                        {
                            case 0: // Success.
                            case 1641: // Success - Reboot started.
                            case 3010: // Success - Reboot required.
                                VstoInstalled.Indicator = Properties.Resources.Yes;
                                _retryCount = 0;
                                _state = State.DownloadChem4Word;
                                break;

                            default:
                                VstoInstalled.Indicator = Properties.Resources.No;
                                Information.Text = $"Error installing VSTO; ExitCode: {exitCode}";
                                Action.Text = "Exit";
                                _state = State.Done;
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Information.Text = ex.Message;
                        Debug.WriteLine(ex.Message);
                        Action.Text = "Exit";
                        Action.Enabled = true;
                        timer1.Enabled = false;
                        _state = State.Done;
                    }
                    break;

                case State.DownloadChem4Word:
                    // Download Chem4Word
                    RegistryHelper.WriteAction($"Downloading {_latestVersion}");
                    Information.Text = "Downloading Chem4Word ...";
                    AddInInstalled.Indicator = Properties.Resources.Downloading;
                    if (DownloadFile(_latestVersion))
                    {
                        _previousState = _state;
                        _state = State.WaitingForChem4WordDownload;
                    }
                    else
                    {
                        AddInInstalled.Indicator = Properties.Resources.No;
                        Information.Text = $"Error downloading Chem4Word; {Information.Text}";
                        Action.Text = "Exit";
                        _state = State.Done;
                    }
                    break;

                case State.WaitingForChem4WordDownload:
                    break;

                case State.InstallChem4Word:
                    // Install Chem4Word
                    RegistryHelper.WriteAction("Installing Chem4Word");
                    Information.Text = "Installing Chem4Word ...";
                    try
                    {
                        AddInInstalled.Indicator = Properties.Resources.Runing;
                        _state = State.WaitingForInstaller;

                        const int retryLimit = 3;
                        var exitCode = -1;
                        var retries = 0;
                        while (exitCode != 0 && retries < retryLimit)
                        {
                            exitCode = ProcessHelper.RunMsi(_downloadedFile);
                            RegistryHelper.WriteAction($"Chem4Word ExitCode: {exitCode} Attempt: {retries}");
                            if (exitCode == 0)
                            {
                                break;
                            }

                            if (exitCode == 1602 || exitCode == 1603)
                            {
                                if (retries < retryLimit - 1)
                                {
                                    Information.Text = $"ExitCode: {exitCode}; Waiting for a few seconds before trying again!";
                                    Application.DoEvents();
                                    Thread.Sleep(3000);
                                }
                                retries++;
                            }
                            else
                            {
                                break;
                            }
                        }

                        if (exitCode == 0)
                        {
                            var key = Registry.CurrentUser.OpenSubKey(RegistryKeyName, true);
                            if (key == null)
                            {
                                key = Registry.CurrentUser.CreateSubKey(RegistryKeyName);
                            }

                            if (key != null)
                            {
                                try
                                {
                                    // Erase previously stored Update Checks etc
                                    foreach (var keyName in key.GetSubKeyNames())
                                    {
                                        key.DeleteValue(keyName);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine(ex.Message);
                                }
                            }

                            progressBar1.Value = 0;
                            progressBar1.Maximum = 100;
                            progressBar1.Value = 100;

                            AddInInstalled.Indicator = Properties.Resources.Yes;
                            Information.Text = "Chem4Word successfully installed. Please start Microsoft Word, then select Chemistry Tab in the ribbon.";
                            Action.Text = "Finish";
                        }
                        else
                        {
                            AddInInstalled.Indicator = Properties.Resources.No;
                            Information.Text = $"Error installing Chem4Word; ExitCode: {exitCode}";
                            Action.Text = "Exit";
                        }

                        _state = State.Done;
                    }
                    catch (Exception ex)
                    {
                        Information.Text = ex.Message;
                        Debug.WriteLine(ex.Message);
                        Action.Text = "Exit";
                    }

                    Action.Enabled = true;
                    timer1.Enabled = false;
                    _state = State.Done;
                    break;

                case State.WaitingForInstaller:
                    break;

                case State.Done:
                    timer1.Enabled = false;
                    Action.Enabled = true;
                    break;
            }
        }

        private Version GetCurrentVersion()
        {
            var result = new Version(0, 0, 0, 0);
            var path = string.Empty;
            var found = false;

            if (Environment.Is64BitOperatingSystem)
            {
                // Try "C:\Program Files (x86)" first
                var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                path = Path.Combine(pf, DetectV3AddIn);
                found = File.Exists(path);
                if (!found)
                {
                    // Try "C:\Program Files"
                    pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                    found = File.Exists(Path.Combine(pf, DetectV3AddIn));
                }
            }
            else
            {
                // Try "C:\Program Files"
                var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                path = Path.Combine(pf, DetectV3AddIn);
                found = File.Exists(Path.Combine(path));
            }

            if (found)
            {
                FileVersionInfo fi = FileVersionInfo.GetVersionInfo(path);
                result = new Version(fi.FileVersion);
            }

            return result;
        }

        private bool FindOldVersion()
        {
            var found = false;

            if (Environment.Is64BitOperatingSystem)
            {
                // Try "C:\Program Files (x86)" first
                var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                found = File.Exists(Path.Combine(pf, DetectV2AddIn));

                if (!found)
                {
                    // Try "C:\Program Files"
                    pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                    found = File.Exists(Path.Combine(pf, DetectV2AddIn));
                }
            }
            else
            {
                // Try "C:\Program Files"
                var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                found = File.Exists(Path.Combine(pf, DetectV2AddIn));
            }

            return found;
        }

        private int RunProcess(string exePath, string arguments)
        {
            var exitCode = -1;

            var processStartInfo = new ProcessStartInfo
            {
                Arguments = arguments,
                FileName = exePath
            };
            using (var process = Process.Start(processStartInfo))
            {
                process.WaitForExit();
                exitCode = process.ExitCode;
            }

            return exitCode;
        }

        private bool DownloadFile(string url)
        {
            var started = false;

            var securityProtocol = ServicePointManager.SecurityProtocol;

            _sw = new Stopwatch();
            _sw.Start();

            try
            {
                var parts = url.Split('/');
                var filename = parts[parts.Length - 1];
                _downloadSource = filename;

                progressBar1.Value = 0;
                Cursor.Current = Cursors.WaitCursor;

                var downloadPath = FolderHelper.GetPath(KnownFolder.Downloads);
                if (!Directory.Exists(downloadPath))
                {
                    downloadPath = Path.GetTempPath();
                }

                _downloadedFile = Path.Combine(downloadPath, filename);

                if (File.Exists(_downloadedFile))
                {
                    try
                    {
                        File.Delete(_downloadedFile);
                    }
                    catch
                    {
                        // Do Nothing
                    }
                }

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                _webClient = new WebClient();
                _webClient.Headers.Add("user-agent", "Chem4Word Bootstrapper");

                _webClient.DownloadProgressChanged += OnDownloadProgressChanged;
                _webClient.DownloadFileCompleted += OnDownloadComplete;

                _webClient.DownloadFileAsync(new Uri(url), _downloadedFile);

                started = true;
            }
            catch (Exception ex)
            {
                RegistryHelper.WriteAction(ex.Message);
                Information.Text = ex.Message;
            }
            finally
            {
                ServicePointManager.SecurityProtocol = securityProtocol;
            }

            return started;
        }

        private void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        private void OnDownloadComplete(object sender, AsyncCompletedEventArgs e)
        {
            _sw.Stop();
            Cursor.Current = Cursors.Default;
            progressBar1.Value = 100;

            if (e.Cancelled)
            {
                RegistryHelper.WriteAction($"Downloading of {_downloadSource} was Cancelled");
                Information.Text = $"Downloading of {_downloadSource} was Cancelled";
                _state = State.Done;
            }
            else if (e.Error != null)
            {
                RegistryHelper.WriteAction($"Error downloading {_downloadSource} Exception: {e.Error.Message}");
                _retryCount++;
                if (_retryCount > 3)
                {
                    Information.Text = $"Too many errors downloading {_downloadSource}, please check your internet connection and try again!";
                    Action.Text = "Exit";
                    _state = State.Done;
                }
                else
                {
                    _state = _previousState;
                }
            }
            else
            {
                _webClient.DownloadProgressChanged -= OnDownloadProgressChanged;
                _webClient.DownloadFileCompleted -= OnDownloadComplete;

                _webClient.Dispose();
                _webClient = null;

                var fi = new FileInfo(_downloadedFile);
                if (fi.Length == 0)
                {
                    _retryCount++;
                    if (_retryCount > 3)
                    {
                        Information.Text = $"Too many errors downloading {_downloadSource}, please check your internet connection and try again!";
                        Action.Text = "Exit";
                        _state = State.Done;
                    }
                    else
                    {
                        _state = _previousState;
                    }
                }
                else
                {
                    RegistryHelper.WriteAction($"Downloading of {_downloadSource} took {_sw.ElapsedMilliseconds.ToString("#,##0", CultureInfo.InvariantCulture)}ms");
                    var seconds = _sw.ElapsedMilliseconds / 1000.0;
                    var kiloBytes = fi.Length / 1024.0;
                    var speed = kiloBytes / seconds / 1000.0;
                    RegistryHelper.WriteAction($"Download speed {speed.ToString("#,##0.000", CultureInfo.InvariantCulture)}MiB/s");
                    switch (_state)
                    {
                        case State.WaitingForVstoDownload:
                            _state = State.InstallVsto;
                            break;

                        case State.WaitingForChem4WordDownload:
                            _state = State.InstallChem4Word;
                            break;
                    }
                }
            }
        }

        private void OnTick_timer1(object sender, EventArgs e)
        {
            HandleNextState();
        }

        private void OnTick_timer2(object sender, EventArgs e)
        {
            if (!WordIsRunning())
            {
                RegistryHelper.WriteAction("Word is no longer running");

                WordRunning.Description = "Microsoft Word is not running";
                WordRunning.Indicator = Properties.Resources.Yes;

                if (_isHigherVersionInstalled)
                {
                    AddInInstalled.Indicator = Properties.Resources.Halt;
                    AddInInstalled.Description = $"V{_currentVersion} of Chem4Word detected";
                    Information.Text = "Can't continue, because a newer version of Chem4Word has been detected.";
                    Action.Text = "Cancel";
                }
                else
                {
                    Information.Text = "Click on Install to start downloading and installing the required components.";
                    Action.Text = "Install";
                }
                Action.Enabled = true;
                timer2.Enabled = false;
            }
        }
    }
}
