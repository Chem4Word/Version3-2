// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core;
using Chem4Word.Core.Helpers;
using Chem4Word.Shared;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Chem4Word.Telemetry
{
    public class SystemHelper
    {
        private static string CryptoRoot = @"SOFTWARE\Microsoft\Cryptography";
        private static string SqmRoot = @"SOFTWARE\Microsoft\SQMClient";
        private string DotNetVersionKey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";

        public string MachineId { get; set; }
        public string DeviceId { get; set; }

        public int ProcessId { get; set; }

        public string SystemOs { get; set; }

        public string WordProduct { get; set; } = "Unknown";

        public string Click2RunProductIds { get; set; }

        public int WordVersion { get; set; }

        public string AddInVersion { get; set; }

        public List<AddInProperties> AllAddIns { get; set; }

        public bool MultipleVersions { get; set; }

        public string AssemblyVersionNumber { get; set; }

        public string AddInLocation { get; set; }

        private string _sourceCodeLocation;

        public string IpAddress { get; set; }

        public string IpObtainedFrom { get; set; }

        public string LastBootUpTime { get; set; }

        public string LastLoginTime { get; set; }

        public string DotNetVersion { get; set; }

        public string UserName { get; set; }
        public bool IsDomainUser { get; set; }

        public string Screens { get; set; }

        public string GitStatus { get; set; }
        public bool GitStatusObtained { get; set; }

        public long UtcOffset { get; set; }
        public DateTime SystemUtcDateTime { get; set; }
        public string ServerDateHeader { get; set; }
        public string ServerUtcDateRaw { get; set; }
        public DateTime ServerUtcDateTime { get; set; }
        public string BrowserVersion { get; set; }
        public List<string> StartUpTimings { get; set; }

        private static Stopwatch _ipStopwatch;

        private readonly List<string> _placesToTry = new List<string>();
        private int _attempts;

        public SystemHelper(List<string> timings)
        {
            StartUpTimings = timings;

            StartUpTimings.AddRange(Initialise());
        }

        public SystemHelper()
        {
            if (StartUpTimings == null)
            {
                StartUpTimings = new List<string>();
            }

            StartUpTimings.AddRange(Initialise());
        }

        private List<string> Initialise()
        {
            try
            {
                List<string> timings = new List<string>();

                string message = $"SystemHelper.Initialise() started at {SafeDate.ToLongDate(DateTime.UtcNow)}";
                timings.Add(message);
                Debug.WriteLine(message);

                Stopwatch sw = new Stopwatch();
                sw.Start();

                string userDomainName = Environment.UserDomainName;
                string userName = Environment.UserName;
                string machineName = Environment.MachineName;

                if (userDomainName.Equals(machineName))
                {
                    // Local account
                    UserName = $"{userName} on {machineName}";
                    IsDomainUser = false;
                }
                else
                {
                    // Domain account
                    UserName = $@"{userDomainName}\{userName} on {machineName}";
                    IsDomainUser = true;
                }

                WordVersion = -1;

                #region Get MachineId and DeviceId

                MachineId = GetMachineId();
                DeviceId = GetDeviceId();

                ProcessId = Process.GetCurrentProcess().Id;

                #endregion Get MachineId and DeviceId

                #region Get OS Version

                // The current code returns 6.2.* for Windows 8.1 and Windows 10 on some systems
                // https://msdn.microsoft.com/en-gb/library/windows/desktop/ms724832(v=vs.85).aspx
                // https://msdn.microsoft.com/en-gb/library/windows/desktop/dn481241(v=vs.85).aspx
                // However as we do not NEED the exact version number,
                //  I am not going to implement the above as they are too risky

                try
                {
                    OperatingSystem operatingSystem = Environment.OSVersion;

                    string ProductName = HKLM_GetString(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName");
                    string CsdVersion = HKLM_GetString(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CSDVersion");

                    if (!string.IsNullOrEmpty(ProductName))
                    {
                        StringBuilder sb = new StringBuilder();
                        if (!ProductName.StartsWith("Microsoft"))
                        {
                            sb.Append("Microsoft ");
                        }
                        sb.Append(ProductName);
                        if (!string.IsNullOrEmpty(CsdVersion))
                        {
                            sb.AppendLine($" {CsdVersion}");
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(operatingSystem.ServicePack))
                            {
                                sb.Append($" {operatingSystem.ServicePack}");
                            }
                        }

                        sb.Append($" {OsBits}");
                        sb.Append($" [{operatingSystem.Version}]");
                        sb.Append($" {CultureInfo.CurrentCulture.Name}");

                        SystemOs = sb.ToString().Replace(Environment.NewLine, "").Replace("Service Pack ", "SP");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    SystemOs = "Exception " + ex.Message;
                }

                #endregion Get OS Version

                #region Get Office/Word Version

                string functionName = "";
                try
                {
                    functionName = "OfficeHelper.GetClick2RunProductIds()";
                    Click2RunProductIds = OfficeHelper.GetClick2RunProductIds();

                    functionName = "OfficeHelper.GetWinWordVersionNumber()";
                    WordVersion = OfficeHelper.GetWinWordVersionNumber();

                    functionName = "OfficeHelper.GetWordProduct()";
                    WordProduct = OfficeHelper.GetWordProduct(Click2RunProductIds);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    string result = $"Exception in '{functionName}'"
                                    + Environment.NewLine + ex.Message
                                    + Environment.NewLine + ex.StackTrace;
                    if (ex.InnerException != null)
                    {
                        result += ex.InnerException.Message
                                  + Environment.NewLine + ex.InnerException.StackTrace;
                    }
                    WordProduct = result;
                }

                #endregion Get Office/Word Version

                #region Get Product Version and Location using reflection

                Assembly assembly = Assembly.GetExecutingAssembly();
                // CodeBase is the location of the installed files
                Uri uriCodeBase = new Uri(assembly.CodeBase);
                AddInLocation = Path.GetDirectoryName(uriCodeBase.LocalPath);

                _sourceCodeLocation = FindSolutionFolder(AddInLocation);

                Version productVersion = assembly.GetName().Version;
                AssemblyVersionNumber = productVersion.ToString();

                AddInVersion = "Chem4Word V" + productVersion;

                #endregion Get Product Version and Location using reflection

                #region Get IpAddress on Thread

                // These can be tested via http://www.ipv6proxy.net/

                // Our locations
                _placesToTry.Add($"https://www.chem4word.co.uk/{Constants.Chem4WordVersionFiles}/client-ip-date.php");
                _placesToTry.Add($"http://www.chem4word.com/{Constants.Chem4WordVersionFiles}/client-ip-date.php");
                _placesToTry.Add($"https://chem4word.azurewebsites.net/{Constants.Chem4WordVersionFiles}/client-ip-date.php");

                // Other Locations
                _placesToTry.Add("https://api.my-ip.io/ip");
                _placesToTry.Add("https://ip.seeip.org");
                _placesToTry.Add("https://ipapi.co/ip");
                _placesToTry.Add("https://ident.me/");
                // Dead Link [2022-12-08] _placesToTry.Add("https://api6.ipify.org/");
                _placesToTry.Add("https://v4v6.ipv6-test.com/api/myip.php");

                message = $"GetIpAddress started at {SafeDate.ToLongDate(DateTime.UtcNow)}";
                StartUpTimings.Add(message);
                Debug.WriteLine(message);

                _ipStopwatch = new Stopwatch();
                _ipStopwatch.Start();

                Thread thread1 = new Thread(GetExternalIpAddress);
                thread1.SetApartmentState(ApartmentState.STA);
                thread1.Start(null);

                #endregion Get IpAddress on Thread

                GetDotNetVersionFromRegistry();

                AllAddIns = InfoHelper.GetListOfAddIns();

                List<string> uniqueManifests = AllAddIns
                                               .Select(p => p.Manifest.ToLower())
                                               .Distinct()
                                               .Where(s => !string.IsNullOrEmpty(s) && s.Contains("chem4word"))
                                               .ToList();

                MultipleVersions = uniqueManifests.Count > 1;

                GatherBootUpTimeEtc();

                try
                {
                    BrowserVersion = new WebBrowser().Version.ToString();
                }
                catch
                {
                    BrowserVersion = "?";
                }

                GetScreens();

                if (Debugger.IsAttached)
                {
                    message = $"GetGitStatus started at {SafeDate.ToLongDate(DateTime.UtcNow)}";
                    StartUpTimings.Add(message);
                    Debug.WriteLine(message);

                    Thread thread2 = new Thread(GetGitStatus);
                    thread2.SetApartmentState(ApartmentState.STA);
                    thread2.Start(null);
                }
                else
                {
                    GitStatusObtained = true;
                }

                sw.Stop();

                message = $"SystemHelper.Initialise() took {SafeDouble.AsString0(sw.ElapsedMilliseconds)}ms";
                timings.Add(message);
                Debug.WriteLine(message);

                return timings;
            }
            catch (ThreadAbortException threadAbortException)
            {
                // Do Nothing
                Debug.WriteLine(threadAbortException.Message);
            }

            return new List<string>();
        }

        private void GatherBootUpTimeEtc()
        {
            LastBootUpTime = "";
            LastLoginTime = "";

            try
            {
                string query1 = "*[System/Provider/@Name='Microsoft-Windows-Kernel-Boot' and System/EventID=27]";
                DateTime dateTime1 = LastEventDateTime(query1);
                LastBootUpTime = $"{SafeDate.ToLongDate(dateTime1.ToUniversalTime())}";

                string query2 = "*[System/Provider/@Name='Microsoft-Windows-Winlogon' and System/EventID=7001]";
                DateTime dateTime2 = LastEventDateTime(query2);
                LastLoginTime = $"{SafeDate.ToLongDate(dateTime2.ToUniversalTime())}";
            }
            catch
            {
                // Do Nothing
            }

            // Local Function
            DateTime LastEventDateTime(string query)
            {
                DateTime result = DateTime.MinValue;

                EventLogQuery eventLogQuery = new EventLogQuery("System", PathType.LogName, query);
                using (EventLogReader elReader = new EventLogReader(eventLogQuery))
                {
                    EventRecord eventInstance = elReader.ReadEvent();
                    while (eventInstance != null)
                    {
                        if (eventInstance.TimeCreated.HasValue)
                        {
                            DateTime thisTime = eventInstance.TimeCreated.Value.ToUniversalTime();
                            if (thisTime > result)
                            {
                                result = thisTime;
                            }
                        }

                        eventInstance = elReader.ReadEvent();
                    }
                }

                if (result == DateTime.MinValue)
                {
                    result = DateTime.UtcNow;
                }
                return result;
            }
        }

        public static string GetDeviceId()
        {
            string result = "";
            try
            {
                // Need special routine here as MachineGuid does not exist in the wow6432 path
                // MachineId REG_SZ "{89E8086F-BD16-45BA-93DA-E1774A658BED}"
                string temp = RegistryWOW6432.GetRegKey64(RegHive.HKEY_LOCAL_MACHINE, SqmRoot, "MachineId");
                if (!string.IsNullOrEmpty(temp))
                {
                    temp = temp.Replace("{", "").Replace("}", "");
                    result = temp.ToLower();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                result = "Exception " + ex.Message;
            }

            return result;
        }

        public static string GetMachineId()
        {
            string result = "";
            try
            {
                // Need special routine here as MachineGuid does not exist in the wow6432 path
                // MachineGuid REG_SZ "455a2ce0-56a5-4ed6-8bbc-4b6f8b2fe140"
                result = RegistryWOW6432.GetRegKey64(RegHive.HKEY_LOCAL_MACHINE, CryptoRoot, "MachineGuid");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                result = "Exception " + ex.Message;
            }

            return result;
        }

        private void GetGitStatus(object o)
        {
            if (Debugger.IsAttached)
            {
                bool gitFound = false;

                string systemPath = Environment.GetEnvironmentVariable("path");
                if (!string.IsNullOrEmpty(systemPath))
                {
                    string[] paths = systemPath.Split(';');
                    foreach (string path in paths)
                    {
                        if (File.Exists(Path.Combine(path, "git.exe")))
                        {
                            gitFound = true;
                            break;
                        }
                    }
                }

                List<string> result = new List<string> { "Git Origin", $"Source Code Folder '{_sourceCodeLocation}'" };
                Debug.WriteLine($"Attempting to run from '{_sourceCodeLocation}'");

                if (!gitFound)
                {
                    UserInteractions.StopUser("'git.exe' was not found on your system path!");
                    result.Add("Warning: Could not find git.exe on the system path!");
                    return;
                }

                result.AddRange(RunCommand("git.exe", "config --get remote.origin.url", _sourceCodeLocation));

                // Ensure status is accurate
                List<string> fetchResult = RunCommand("git.exe", "fetch", _sourceCodeLocation);
                if (fetchResult.Any())
                {
                    result.Add("Git fetch");
                    result.AddRange(fetchResult);
                }

                // git status -s -b --porcelain == Gets Branch, Status and a List of any changed files
                List<string> statusResult = RunCommand("git.exe", "status -s -b --porcelain", _sourceCodeLocation);
                if (statusResult.Any())
                {
                    result.Add("Git Branch, Status & Changed files");
                    result.AddRange(statusResult);
                }
                GitStatus = string.Join(Environment.NewLine, result.ToArray());

                string message = $"GetGitStatus finished at {SafeDate.ToLongDate(DateTime.UtcNow)}";
                GitStatusObtained = true;

                StartUpTimings.Add(message);
                Debug.WriteLine(message);
            }
            else
            {
                GitStatusObtained = true;
            }
        }

        private string FindSolutionFolder(string startPath)
        {
            string current = startPath;
            while (!string.IsNullOrEmpty(current))
            {
                string[] slnFiles = Directory.GetFiles(current, "*.sln");
                if (slnFiles.Length > 0)
                {
                    return current;
                }

                current = Directory.GetParent(current)?.FullName;
            }

            return null;
        }

        private List<string> RunCommand(string exeName, string args, string folder)
        {
            List<string> results = new List<string>();

            if (!string.IsNullOrEmpty(folder))
            {
                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo(exeName)
                    {
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        WorkingDirectory = folder,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        Arguments = args
                    };

                    using (Process process = new Process())
                    {
                        process.StartInfo = startInfo;
                        process.Start();

                        while (!process.StandardOutput.EndOfStream)
                        {
                            results.Add(process.StandardOutput.ReadLine());
                        }
                    }
                }
                catch (Exception exception)
                {
                    results.Add($"Exception running '{exeName}' '{args}' in folder '{folder}'");
                    results.Add(exception.ToString());

                    Debugger.Break();
                }
            }
            else
            {
                results.Add("Folder is null");
            }

            return results;
        }

        private void GetScreens()
        {
            List<string> screens = new List<string>();

            int idx = 0;
            foreach (Screen screen in Screen.AllScreens)
            {
                idx++;
                string primary = screen.Primary ? "[P]" : "";
                screens.Add($"#{idx}{primary}: {screen.Bounds.Width}x{screen.Bounds.Height} @ {screen.Bounds.X},{screen.Bounds.Y}");
            }

            Screens = string.Join("; ", screens);
        }

        public static string OsBits => Environment.Is64BitOperatingSystem ? "64bit" : "32bit";

        private void GetDotNetVersionFromRegistry()
        {
            // https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed
            // https://en.wikipedia.org/wiki/Windows_10_version_history
            // https://en.wikipedia.org/wiki/Windows_11_version_history

            using (RegistryKey ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(DotNetVersionKey))
            {
                if (ndpKey != null)
                {
                    int releaseKey = Convert.ToInt32(ndpKey.GetValue("Release"));

                    // .Net 4.8.1
                    if (releaseKey >= 533325)
                    {
                        DotNetVersion = $".NET 4.8.1 [{releaseKey}]";
                        return;
                    }
                    if (releaseKey >= 533320)
                    {
                        DotNetVersion = $".NET 4.8.1 (W11 2022) [{releaseKey}]";
                        return;
                    }

                    // .Net 4.8
                    if (releaseKey >= 528449)
                    {
                        DotNetVersion = $".NET 4.8 (W11/S2022) [{releaseKey}]";
                        return;
                    }
                    if (releaseKey >= 528372)
                    {
                        DotNetVersion = $".NET 4.8 (W10 2004) [{releaseKey}]";
                        return;
                    }
                    if (releaseKey >= 528049)
                    {
                        DotNetVersion = $".NET 4.8 [{releaseKey}]";
                        return;
                    }
                    if (releaseKey >= 528040)
                    {
                        DotNetVersion = $".NET 4.8 (W10 1903) [{releaseKey}]";
                        return;
                    }

                    // .Net 4.7.2
                    if (releaseKey >= 461814)
                    {
                        DotNetVersion = $".NET 4.7.2 [{releaseKey}]";
                        return;
                    }
                    if (releaseKey >= 461808)
                    {
                        DotNetVersion = $".NET 4.7.2 (W10 1803) [{releaseKey}]";
                        return;
                    }

                    // .Net 4.7.1
                    if (releaseKey >= 461310)
                    {
                        DotNetVersion = $".NET 4.7.1 [{releaseKey}]";
                        return;
                    }
                    if (releaseKey >= 461308)
                    {
                        DotNetVersion = $".NET 4.7.1 (W10 1710) [{releaseKey}]";
                        return;
                    }

                    // .Net 4.7
                    if (releaseKey >= 460805)
                    {
                        DotNetVersion = $".NET 4.7 [{releaseKey}]";
                        return;
                    }
                    if (releaseKey >= 460798)
                    {
                        DotNetVersion = $".NET 4.7 (W10 1703) [{releaseKey}]";
                        return;
                    }

                    // .Net 4.6.2
                    if (releaseKey >= 394806)
                    {
                        DotNetVersion = $".NET 4.6.2 [{releaseKey}]";
                        return;
                    }
                    if (releaseKey >= 394802)
                    {
                        DotNetVersion = $".NET 4.6.2 (W10 1607) [{releaseKey}]";
                        return;
                    }

                    // .Net 4.6.1
                    if (releaseKey >= 394271)
                    {
                        DotNetVersion = $".NET 4.6.1 [{releaseKey}]";
                        return;
                    }
                    if (releaseKey >= 394254)
                    {
                        DotNetVersion = $".NET 4.6.1 (W10 1511) [{releaseKey}]";
                        return;
                    }

                    // .Net 4.6
                    if (releaseKey >= 393297)
                    {
                        DotNetVersion = $".NET 4.6 [{releaseKey}]";
                        return;
                    }
                    if (releaseKey >= 393295)
                    {
                        DotNetVersion = $".NET 4.6 (W10 1507) [{releaseKey}]";
                        return;
                    }

                    // .Net 4.5.2
                    if (releaseKey >= 379893)
                    {
                        DotNetVersion = $".NET 4.5.2 [{releaseKey}]";
                        return;
                    }

                    // .Net 4.5.1
                    if (releaseKey >= 378758)
                    {
                        DotNetVersion = $".NET 4.5.1 [{releaseKey}]";
                        return;
                    }
                    if (releaseKey >= 378675)
                    {
                        DotNetVersion = $".NET 4.5.1 [{releaseKey}]";
                        return;
                    }

                    // .Net 4.5
                    if (releaseKey >= 378389)
                    {
                        DotNetVersion = $".NET 4.5 [{releaseKey}]";
                        return;
                    }

                    DotNetVersion = $".Net Version Unknown [{releaseKey}]";
                }
            }
        }

        private string HKLM_GetString(string path, string key)
        {
            try
            {
                RegistryKey rk = Registry.LocalMachine.OpenSubKey(path, false);
                if (rk == null)
                {
                    return "";
                }
                return (string)rk.GetValue(key);
            }
            catch
            {
                return "";
            }
        }

        private void GetExternalIpAddress(object o)
        {
            string module = $"{MethodBase.GetCurrentMethod()?.Name}()";
            try
            {
                string message;
                IpAddress = "0.0.0.0";

                for (int i = 0; i < 2; i++)
                {
                    foreach (string place in _placesToTry)
                    {
                        _attempts++;

                        try
                        {
                            message = $"Attempt #{_attempts} using '{place}'";
                            StartUpTimings.Add(message);
                            Debug.WriteLine(message);

                            if (place.Contains("chem4word"))
                            {
                                GetInternalVersion(place);
                            }
                            else
                            {
                                GetExternalVersion(place);
                            }

                            // Exit out of inner loop
                            if (!IpAddress.Contains("0.0.0.0"))
                            {
                                break;
                            }
                        }
                        catch (Exception exception)
                        {
                            Debug.WriteLine(exception.Message);
                            StartUpTimings.Add(exception.Message);
                        }
                    }

                    // Exit out of outer loop
                    if (!IpAddress.Contains("0.0.0.0"))
                    {
                        break;
                    }
                }

                if (IpAddress.Contains("0.0.0.0"))
                {
                    // Handle failure
                    IpAddress = "8.8.8.8";
                }

                _ipStopwatch.Stop();

                message = $"{module} took {SafeDouble.AsString0(_ipStopwatch.ElapsedMilliseconds)}ms";
                StartUpTimings.Add(message);
                Debug.WriteLine(message);
            }
            catch (ThreadAbortException threadAbortException)
            {
                // Do Nothing
                Debug.WriteLine(threadAbortException.Message);
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception.Message);
                StartUpTimings.Add(exception.Message);
            }
        }

        private void GetInternalVersion(string url)
        {
            string data = GetData(url);

            // Tidy Up the data
            data = data.Replace("Your IP address : ", "");
            data = data.Replace("UTC Date : ", "");
            data = data.Replace("<br/>", "|");
            data = data.Replace("<br />", "|");

            string[] lines = data.Split('|');

            if (lines[0].Contains(":"))
            {
                string[] ipV6Parts = lines[0].Split(':');
                // Must have between 4 and 8 parts
                if (ipV6Parts.Length >= 4 && ipV6Parts.Length <= 8)
                {
                    IpAddress = "IpAddress " + lines[0];
                    IpObtainedFrom = $"IpAddress V6 obtained from '{url}' on attempt {_attempts}";
                }
            }

            if (lines[0].Contains("."))
            {
                // Must have 4 parts
                string[] ipV4Parts = lines[0].Split('.');
                if (ipV4Parts.Length == 4)
                {
                    IpAddress = "IpAddress " + lines[0];
                    IpObtainedFrom = $"IpAddress V4 obtained from '{url}' on attempt {_attempts}";
                }
            }

            SystemUtcDateTime = DateTime.UtcNow;
            if (lines.Length == 2)
            {
                ServerUtcDateRaw = lines[1];
                ServerUtcDateTime = FromPhpDate(lines[1]);

                UtcOffset = SystemUtcDateTime.Ticks - ServerUtcDateTime.Ticks;
            }
            else
            {
                ServerUtcDateRaw = string.Join(Environment.NewLine, lines);
                ServerUtcDateTime = DateTime.UtcNow;
            }
        }

        private void GetExternalVersion(string url)
        {
            string data = GetData(url);

            if (data.Contains(":"))
            {
                IpAddress = "IpAddress " + data;
                IpObtainedFrom = $"IpAddress V6 obtained from '{url}' on attempt {_attempts}";
            }

            if (data.Contains("."))
            {
                IpAddress = "IpAddress " + data;
                IpObtainedFrom = $"IpAddress V4 obtained from '{url}' on attempt {_attempts}";
            }
        }

        private string GetData(string url)
        {
            string result = "0.0.0.0";

            SecurityProtocolType securityProtocol = ServicePointManager.SecurityProtocol;

            try
            {
                if (url.StartsWith("https"))
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                }

                if (WebRequest.Create(url) is HttpWebRequest request)
                {
                    request.UserAgent = "Chem4Word Add-In";
                    request.Timeout = url.Contains("chem4word") ? 5000 : 2500;

                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                    try
                    {
                        // Get Server Date header i.e. "Tue, 01 Jan 2026 19:52:46 GMT"
                        ServerDateHeader = response.Headers["date"];
                        SystemUtcDateTime = DateTime.UtcNow;
                        ServerUtcDateTime = DateTime.Parse(ServerDateHeader).ToUniversalTime();
                        UtcOffset = SystemUtcDateTime.Ticks - ServerUtcDateTime.Ticks;
                    }
                    catch
                    {
                        // Indicate failure
                        ServerDateHeader = null;
                        SystemUtcDateTime = DateTime.MinValue;
                    }

                    if (HttpStatusCode.OK.Equals(response.StatusCode))
                    {
                        Stream stream = response.GetResponseStream();
                        if (stream != null)
                        {
                            using (StreamReader reader = new StreamReader(stream))
                            {
                                result = reader.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch (WebException webException)
            {
                StartUpTimings.Add(webException.Status == WebExceptionStatus.Timeout
                                       ? $"Timeout: '{url}'"
                                       : webException.Message);
            }
            catch (Exception exception)
            {
                StartUpTimings.Add(exception.Message);
            }
            finally
            {
                ServicePointManager.SecurityProtocol = securityProtocol;
            }

            return result;
        }

        private DateTime FromPhpDate(string line)
        {
            string[] p = line.Split(',');
            DateTime serverUtc = new DateTime(int.Parse(p[0]), int.Parse(p[1]), int.Parse(p[2]), int.Parse(p[3]), int.Parse(p[4]), int.Parse(p[5]));
            return DateTime.SpecifyKind(serverUtc, DateTimeKind.Utc);
        }
    }
}
