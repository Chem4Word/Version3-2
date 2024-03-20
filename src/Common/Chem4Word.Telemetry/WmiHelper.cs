// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;

namespace Chem4Word.Telemetry
{
    public class WmiHelper
    {
        private const string Workstation = "Workstation";
        private const string DomainController = "Domain Controller";
        private const string Server = "Server";
        private const string Unknown = "Unknown";

        public WmiHelper()
        {
            GetWin32ComputerSystemData();
            GetWin32ProcessorData();
            GetWin32OperatingSystemData();
            GetAntiVirusStatus();
        }

        #region Fields

        private string _manufacturer;

        public string Manufacturer
        {
            get
            {
                if (_manufacturer == null)
                {
                    try
                    {
                        GetWin32ComputerSystemData();
                    }
                    catch (Exception)
                    {
                        // Do Nothing
                    }
                }

                return _manufacturer;
            }
        }

        private string _model;

        public string Model
        {
            get
            {
                if (_model == null)
                {
                    try
                    {
                        GetWin32ComputerSystemData();
                    }
                    catch (Exception)
                    {
                        // Do Nothing
                    }
                }

                return _model;
            }
        }

        private string _systemFamily;

        public string SystemFamily
        {
            get
            {
                if (_systemFamily == null)
                {
                    try
                    {
                        GetWin32ComputerSystemData();
                    }
                    catch (Exception)
                    {
                        // Do Nothing
                    }
                }

                return _systemFamily;
            }
        }

        private string _cpuName;

        public string CpuName
        {
            get
            {
                if (_cpuName == null)
                {
                    try
                    {
                        GetWin32ProcessorData();
                    }
                    catch (Exception)
                    {
                        // Do Nothing
                    }
                }

                return _cpuName;
            }
        }

        private string _totalPhysicalMemory;

        public string TotalPhysicalMemory
        {
            get
            {
                if (_totalPhysicalMemory == null)
                {
                    try
                    {
                        GetWin32ComputerSystemData();
                    }
                    catch (Exception)
                    {
                        // Do Nothing
                    }
                }

                return _totalPhysicalMemory;
            }
        }

        private string _cpuSpeed;

        public string CpuSpeed
        {
            get
            {
                if (_cpuSpeed == null)
                {
                    try
                    {
                        GetWin32ProcessorData();
                    }
                    catch (Exception)
                    {
                        // Do Nothing
                    }
                }

                return _cpuSpeed;
            }
        }

        private string _logicalProcessors;

        public string LogicalProcessors
        {
            get
            {
                if (_logicalProcessors == null)
                {
                    try
                    {
                        GetWin32ProcessorData();
                    }
                    catch (Exception)
                    {
                        // Do Nothing
                    }
                }

                return _logicalProcessors;
            }
        }

        private string _osCaption;

        public string OSCaption
        {
            get
            {
                if (_osCaption == null)
                {
                    try
                    {
                        GetWin32OperatingSystemData();
                    }
                    catch (Exception)
                    {
                        // Do Nothing
                    }
                }

                return _osCaption;
            }
        }

        private string _osVersion;

        public string OSVersion
        {
            get
            {
                if (_osVersion == null)
                {
                    try
                    {
                        GetWin32OperatingSystemData();
                    }
                    catch (Exception)
                    {
                        // Do Nothing
                    }
                }

                return _osVersion;
            }
        }

        private string _productType;

        public string ProductType
        {
            get
            {
                if (_productType == null)
                {
                    try
                    {
                        GetWin32OperatingSystemData();
                    }
                    catch (Exception)
                    {
                        // Do Nothing
                    }
                }

                return _productType;
            }
        }

        private string _antiVirusStatus;

        public string AntiVirusStatus
        {
            get
            {
                if (_antiVirusStatus == null)
                {
                    try
                    {
                        GetAntiVirusStatus();
                    }
                    catch (Exception)
                    {
                        // Do Nothing
                    }
                }

                return _antiVirusStatus;
            }
        }

        #endregion Fields

        private void GetWin32ComputerSystemData()
        {
            var fields = new List<string> { "Manufacturer", "Model", "SystemFamily", "TotalPhysicalMemory" };

            var sysObjects = GetWmiObjects(@"root\CIMV2", "Win32_ComputerSystem", fields);

            foreach (var managementBaseObject in sysObjects)
            {
                var mgtObject = (ManagementObject)managementBaseObject;
                foreach (var field in fields)
                {
                    switch (field)
                    {
                        case "Manufacturer":
                            try
                            {
                                _manufacturer = ObjectAsString(mgtObject[field]);
                            }
                            catch
                            {
                                _manufacturer = "?";
                            }
                            break;

                        case "Model":
                            try
                            {
                                _model = ObjectAsString(mgtObject[field]);
                            }
                            catch
                            {
                                _model = "?";
                            }
                            break;

                        case "SystemFamily":
                            try
                            {
                                _systemFamily = ObjectAsString(mgtObject[field]);
                            }
                            catch
                            {
                                _systemFamily = "?";
                            }
                            break;

                        case "TotalPhysicalMemory":
                            try
                            {
                                var temp = ObjectAsString(mgtObject[field]);
                                var memory = ulong.Parse(temp);
                                _totalPhysicalMemory = SafeDouble.AsString0((double)memory / (1024 * 1024 * 1024)) + "GiB";
                            }
                            catch
                            {
                                _totalPhysicalMemory = "?";
                            }
                            break;
                    }
                }
            }
        }

        private void GetWin32ProcessorData()
        {
            var fields = new List<string> { "Name", "NumberOfLogicalProcessors", "MaxClockSpeed" };

            var sysObjects = GetWmiObjects(@"root\CIMV2", "Win32_Processor", fields);

            foreach (var managementBaseObject in sysObjects)
            {
                var mgtObject = (ManagementObject)managementBaseObject;
                foreach (var field in fields)
                {
                    switch (field)
                    {
                        case "Name":
                            try
                            {
                                _cpuName = ObjectAsString(mgtObject[field]);
                            }
                            catch
                            {
                                _cpuName = "?";
                            }
                            break;

                        case "NumberOfLogicalProcessors":
                            try
                            {
                                _logicalProcessors = ObjectAsString(mgtObject[field]);
                            }
                            catch
                            {
                                _logicalProcessors = "?";
                            }
                            break;

                        case "MaxClockSpeed":
                            try
                            {
                                var speed = double.Parse(ObjectAsString(mgtObject[field])) / 1024;
                                _cpuSpeed = SafeDouble.AsString(speed) + "GHz";
                            }
                            catch
                            {
                                _cpuSpeed = "?";
                            }
                            break;
                    }
                }
            }
        }

        private void GetWin32OperatingSystemData()
        {
            var fields = new List<string> { "ProductType", "Caption", "Version" };

            var sysObjects = GetWmiObjects(@"root\CIMV2", "Win32_OperatingSystem", fields);

            foreach (var managementBaseObject in sysObjects)
            {
                var mgtObject = (ManagementObject)managementBaseObject;
                foreach (var field in fields)
                {
                    switch (field)
                    {
                        case "ProductType":
                            try
                            {
                                var productType = int.Parse(ObjectAsString(mgtObject[field]));
                                switch (productType)
                                {
                                    case 1:
                                        _productType = Workstation;
                                        break;

                                    case 2:
                                        _productType = DomainController;
                                        break;

                                    case 3:
                                        _productType = Server;
                                        break;

                                    default:
                                        _productType = Unknown + $" [{productType}]";
                                        break;
                                }
                            }
                            catch
                            {
                                _productType = "?";
                            }
                            break;

                        case "Caption":
                            try
                            {
                                _osCaption = ObjectAsString(mgtObject[field]);
                            }
                            catch
                            {
                                _osCaption = "?";
                            }
                            break;

                        case "Version":
                            try
                            {
                                _osVersion = ObjectAsString(mgtObject[field]);
                            }
                            catch
                            {
                                _osVersion = "?";
                            }
                            break;
                    }
                }
            }
        }

        private void GetAntiVirusStatus()
        {
            // This is a combination of information from the following sources

            // http://neophob.com/2010/03/wmi-query-windows-securitycenter2/
            // https://mspscripts.com/get-installed-antivirus-information-2/
            // https://gallery.technet.microsoft.com/scriptcenter/Get-the-status-of-4b748f25
            // https://stackoverflow.com/questions/4700897/wmi-security-center-productstate-clarification/4711211
            // https://blogs.msdn.microsoft.com/alejacma/2008/05/12/how-to-get-antivirus-information-with-wmi-vbscript/#comment-442

            // Only works if not a server
            if (!string.IsNullOrEmpty(ProductType) && ProductType.Equals(Workstation))
            {
                var fields = new List<string> { "DisplayName", "ProductState"};

                var sysObjects = GetWmiObjects(@"root\SecurityCenter2", "AntiVirusProduct", fields);

                try
                {
                    var products = new List<string>();

                    foreach (var managementBaseObject in sysObjects)
                    {
                        var mgtObject = (ManagementObject)managementBaseObject;

                        var product = ObjectAsString(mgtObject["DisplayName"]);
                        var status = int.Parse(ObjectAsString(mgtObject["ProductState"]));

                        var hex = Hex(status);
                        var bin = Binary(status);
                        var reversed = Reverse(bin);

                        // https://blogs.msdn.microsoft.com/alejacma/2008/05/12/how-to-get-antivirus-information-with-wmi-vbscript/#comment-442
                        // 19th bit = Not so sure but, AV is turned on (I wouldn't be sure it's enabled)
                        // 13th bit = On Access Scanning (Memory Resident Scanning) is on, this tells you that the product is scanning every file that you open as opposed to just scanning at regular intervals.
                        //  5th Bit = If this is true (==1) the virus scanner is out of date

                        var enabled = GetBit(reversed, 18);
                        var scanning = GetBit(reversed, 12);
                        var outdated = GetBit(reversed, 4);

                        products.Add($"{product} Status: {status} [0x{hex}] --> Enabled: {enabled} Scanning: {scanning} Outdated: {outdated}");
                    }

                    // Return distinct list of products and states
                    _antiVirusStatus = string.Join(";", products.Distinct());
                }
                catch (Exception exception)
                {
                    _antiVirusStatus = $"{exception.Message}";
                }
            }
        }

        private ManagementObjectCollection GetWmiObjects(string nameSpace, string className, List<string> fields)
        {
            // https://stackoverflow.com/questions/28989279/communicating-with-non-english-wmi
            // https://www.autoitscript.com/autoit3/docs/appendix/OSLangCodes.htm

            var options = new ConnectionOptions
                          {
                              Locale = "MS_409" // en-US
                          };
            var scope = new ManagementScope($"{nameSpace}", options);
            scope.Connect();

            var objectQuery = new ObjectQuery($"SELECT {string.Join(",", fields)} FROM {className}");

            var searcher = new ManagementObjectSearcher(scope, objectQuery);
            var objects = searcher.Get();

            return objects;
        }

        #region Utility methods

        private string ObjectAsString(object input)
        {
            var temp = input.ToString();

            // Replace tab with space
            temp = temp.Replace("\t", " ");

            // Replace double spaces with single space
            temp = temp.Replace("  ", " ");

            return temp.Trim();
        }

        private string Hex(int value)
        {
            try
            {
                return Convert.ToString(value, 16).PadLeft(6, '0');
            }
            catch
            {
                return string.Empty;
            }
        }

        private string Binary(int value)
        {
            try
            {
                return Convert.ToString(value, 2).PadLeft(24, '0');
            }
            catch
            {
                return string.Empty;
            }
        }

        private string Reverse(string value)
        {
            try
            {
                return new string(value.Reverse().ToArray());
            }
            catch
            {
                return string.Empty;
            }
        }

        private bool GetBit(string value, int index)
        {
            try
            {
                return value.Substring(index, 1).Equals("1");
            }
            catch
            {
                return false;
            }
        }

        #endregion Utility methods
    }
}