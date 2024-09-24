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
            GetWin32ComputerSystemProductData();
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

        private string _osSku;

        public string OSSKU
        {
            get
            {
                if (_osSku == null)
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

                return _osSku;
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

        private string _uuid;

        public string Uuid
        {
            get
            {
                if (_uuid == null)
                {
                    try
                    {
                        GetWin32ComputerSystemProductData();
                    }
                    catch (Exception)
                    {
                        // Do Nothing
                    }
                }

                return _uuid;
            }
        }

        private void GetWin32ComputerSystemProductData()
        {
            var fields = new List<string> { "UUID" };

            var sysObjects = GetWmiObjects(@"root\CIMV2", "Win32_ComputerSystemProduct", fields);

            foreach (var managementBaseObject in sysObjects)
            {
                var mgtObject = (ManagementObject)managementBaseObject;
                foreach (var field in fields)
                {
                    switch (field)
                    {
                        case "UUID":
                            try
                            {
                                _uuid = ObjectAsString(mgtObject[field]).ToLower();
                            }
                            catch
                            {
                                _uuid = "?";
                            }

                            break;
                    }
                }
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
                                var speed = SafeDouble.Parse(ObjectAsString(mgtObject[field])) / 1024;
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
            var fields = new List<string> { "ProductType", "Caption", "Version", "BuildNumber", "OperatingSystemSKU" };

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
                                        _productType = $"{Unknown} ProductType: [{productType}]";
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

                        case "OperatingSystemSKU":
                            try
                            {
                                _osSku = DecodeSKU(ObjectAsString(mgtObject[field]));
                            }
                            catch
                            {
                                _osSku = "?";
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
                var fields = new List<string> { "DisplayName", "ProductState" };

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

        private string DecodeSKU(string sku)
        {
            // OperatingSystemSKU
            // https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-operatingsystem
            // https: //learn.microsoft.com/en-us/windows/win32/api/sysinfoapi/nf-sysinfoapi-getproductinfo

            string result;

            switch (sku)
            {
                case "1":
                    result = "Ultimate Edition";
                    break;

                case "2":
                    result = "Home Basic Edition";
                    break;

                case "3":
                    result = "Home Premium Edition";
                    break;

                case "4":
                    result = "Enterprise Edition";
                    break;

                case "6":
                    result = "Business Edition";
                    break;

                case "7":
                    result = "Windows Server Standard Edition (Desktop Experience installation)";
                    break;

                case "8":
                    result = "Windows Server Datacenter Edition (Desktop Experience installation)";
                    break;

                case "9":
                    result = "Small Business Server Edition";
                    break;

                case "10":
                    result = "Enterprise Server Edition";
                    break;

                case "11":
                    result = "Starter Edition";
                    break;

                case "12":
                    result = "Datacenter Server Core Edition";
                    break;

                case "13":
                    result = "Standard Server Core Edition";
                    break;

                case "14":
                    result = "Enterprise Server Core Edition";
                    break;

                case "17":
                    result = "Web Server Edition";
                    break;

                case "19":
                    result = "Home Server Edition";
                    break;

                case "20":
                    result = "Storage Express Server Edition";
                    break;

                case "21":
                    result = "Windows Storage Server Standard Edition (Desktop Experience installation)";
                    break;

                case "22":
                    result = "Windows Storage Server Workgroup Edition (Desktop Experience installation)";
                    break;

                case "23":
                    result = "Storage Enterprise Server Edition";
                    break;

                case "24":
                    result = "Server For Small Business Edition";
                    break;

                case "25":
                    result = "Small Business Server Premium Edition";
                    break;

                case "27":
                    result = "Windows Enterprise Edition";
                    break;

                case "28":
                    result = "Windows Ultimate Edition";
                    break;

                case "29":
                    result = "Windows Server Web Server Edition (Server Core installation)";
                    break;

                case "36":
                    result = "Windows Server Standard Edition without Hyper-V";
                    break;

                case "37":
                    result = "Windows Server Datacenter Edition without Hyper-V (full installation)";
                    break;

                case "38":
                    result = "Windows Server Enterprise Edition without Hyper-V (full installation)";
                    break;

                case "39":
                    result = "Windows Server Datacenter Edition without Hyper-V (Server Core installation)";
                    break;

                case "40":
                    result = "Windows Server Standard Edition without Hyper-V (Server Core installation)";
                    break;

                case "41":
                    result = "Windows Server Enterprise Edition without Hyper-V (Server Core installation)";
                    break;

                case "42":
                    result = "Microsoft Hyper-V Server";
                    break;

                case "43":
                    result = "Storage Server Express Edition (Server Core installation)";
                    break;

                case "44":
                    result = "Storage Server Standard Edition (Server Core installation)n";
                    break;

                case "45":
                    result = "Storage Server Workgroup Edition (Server Core installation)";
                    break;

                case "46":
                    result = "Storage Server Enterprise Edition (Server Core installation)";
                    break;

                case "48":
                    result = "Windows Professional";
                    break;

                case "50":
                    result = "Windows Server Essentials (Desktop Experience installation)";
                    break;

                case "63":
                    result = "Small Business Server Premium (Server Core installation)";
                    break;

                case "64":
                    result = "Windows Compute Cluster Server without Hyper-V";
                    break;

                case "97":
                    result = "Windows RT";
                    break;

                case "101":
                    result = "Windows Home";
                    break;

                case "103":
                    result = "Windows Professional with Media Center";
                    break;

                case "104":
                    result = "Windows Mobile";
                    break;

                case "123":
                    result = "Windows IoT (Internet of Things) Core";
                    break;

                case "143":
                    result = "Windows Server Datacenter Edition (Nano Server installation)";
                    break;

                case "144":
                    result = "Windows Server Standard Edition (Nano Server installation)";
                    break;

                case "147":
                    result = "Windows Server Datacenter Edition (Server Core installation)";
                    break;

                case "148":
                    result = "Windows Server Standard Edition (Server Core installation)";
                    break;

                case "175":
                    result = "Windows Enterprise for Virtual Desktops (Azure Virtual Desktop)";
                    break;

                case "407":
                    result = "Windows Server Datacenter: Azure Edition";
                    break;

                default:
                    result = $"{Unknown} SKU: [{sku}]";
                    break;
            }

            return result;
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