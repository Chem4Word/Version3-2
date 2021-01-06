// ---------------------------------------------------------------------------
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
using System.Security.AccessControl;
using System.Security.Principal;
using Chem4Word.Shared;
using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Win32;

namespace WiX.CustomAction
{
    public class CustomActions
    {
        private const string OfficeKey = @"Microsoft\Office";
        private const string WordAddinsKey = @"Word\Addins\Chem4Word V3";
        private const string ProductShortName = "Chem4Word 2020";
        private const string ProductInstallFolder = "Chem4Word V3";
        private const string ProgramDataFolder = "Chem4Word.V3";
        private const string ProductLongName = "Chemistry Add-In for Word (Chem4Word) 2020";
        private const string ManifestFile = "Chem4Word.V3.vsto";

        [CustomAction]
        public static ActionResult SetupChem4Word(Session session)
        {
            session.Log($"Begin {nameof(SetupChem4Word)}()");

            session.Log($"  Running as {Environment.UserName}");

            try
            {
                session.Log($"  Environment.Is64BitOperatingSystem: {Environment.Is64BitOperatingSystem}");

                string c4wPath = null;
                if (Environment.Is64BitOperatingSystem)
                {
                    session.Log("  Detected 64bit OS");
                    session.Log($"  Environment.SpecialFolder.ProgramFiles: {Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}");
                    session.Log($"  Environment.SpecialFolder.ProgramFilesX86: {Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)}");

                    c4wPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), ProductInstallFolder);
                }
                else
                {
                    session.Log("  Detected 32bit OS");
                    session.Log($"  Environment.SpecialFolder.ProgramFiles: {Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}");
                    c4wPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), ProductInstallFolder);
                }

                session.Log($"  Looking for folder '{c4wPath}'");
                if (Directory.Exists(c4wPath))
                {
                    session.Log("  Found Chem4Word installation folder");
                    string manifestFileLocation = Path.Combine(c4wPath, ManifestFile);
                    session.Log($"  Looking for file '{manifestFileLocation}'");
                    if (File.Exists(manifestFileLocation))
                    {
                        session.Log("  Found Chem4Word Add-In Manifest File");
                        AlterRegistry(session, $"file:///{manifestFileLocation}");
                    }
                    else
                    {
                        session.Log("  Error: Chem4Word Add-In Manifest File not found !!!");
                    }

                    ModifyFolderPermissions(session);
                }
                else
                {
                    session.Log("  Error: Chem4Word installation folder not found !!!");
                }
            }
            catch (Exception ex)
            {
                session.Log($"** Exception: {ex.Message} **");
            }

            session.Log($"End {nameof(SetupChem4Word)}()");

            return ActionResult.Success;
        }

        private static void ModifyFolderPermissions(Session session)
        {
            session.Log($"  Fixing SpecialFolder.CommonApplicationData {ProgramDataFolder}");

            try
            {
                string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                string folderPath = Path.Combine(programData, ProgramDataFolder);
                DirectorySecurity sec = Directory.GetAccessControl(folderPath);
                SecurityIdentifier users = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
                sec.AddAccessRule(new FileSystemAccessRule(users, FileSystemRights.Modify | FileSystemRights.Synchronize, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
                Directory.SetAccessControl(folderPath, sec);
            }
            catch (Exception ex)
            {
                session.Log(ex.Message);
            }

            session.Log($"  Fixed SpecialFolder.CommonApplicationData {ProgramDataFolder}");
        }

        [CustomAction]
        public static ActionResult CleanUserRegistry(Session session)
        {
            session.Log($"Begin {nameof(CleanUserRegistry)}()");

            session.Log($"  Running as {Environment.UserName}");

            try
            {
                // Possible locations that (old) add-in may have been registered
                var listOfKeys = new List<string>()
                                 {
                                     "Chemistry Add-in for Word",
                                     "Chem4Word",
                                     "Chem4Word V3",
                                     "Chem4Word.V3"
                                 };
                foreach (var key in listOfKeys)
                {
                    DeleteUserKey(session, $@"SOFTWARE\{OfficeKey}\Word\Addins\", $"{key}");
                    DeleteUserKey(session, $@"SOFTWARE\{OfficeKey}\Word\AddinsData\", $"{key}");
                }

                // User Settings
                EraseUserKey(session, @"SOFTWARE\Chem4Word V3");
            }
            catch (Exception ex)
            {
                session.Log($"** Exception: {ex.Message} **");
            }

            session.Log($"End {nameof(CleanUserRegistry)}()");

            return ActionResult.Success;
        }

        private static void EraseUserKey(Session session, string nameOfKey)
        {
            session.Log($"  {nameof(EraseUserKey)}({nameOfKey})");

            RegistryKey key = Registry.CurrentUser.OpenSubKey(nameOfKey, true);
            if (key == null)
            {
                key = Registry.CurrentUser.CreateSubKey(nameOfKey);
            }

            if (key != null)
            {
                try
                {
                    var values = key.GetValueNames();
                    // Erase previously stored Update Checks etc
                    foreach (string value in values)
                    {
                        session.Log($"Deleting Value '{value}'");
                        key.DeleteValue(value);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
        }

        private static void DeleteUserKey(Session session, string nameOfKey, string kkk)
        {
            session.Log($"  {nameof(DeleteUserKey)}({nameOfKey}, {kkk})");

            RegistryKey key = Registry.CurrentUser.OpenSubKey($"{nameOfKey}{kkk}", true);
            if (key != null)
            {
                try
                {
                    var values = key.GetValueNames();
                    foreach (string value in values)
                    {
                        session.Log($"Deleting Value '{value}'");
                        key.DeleteValue(value);
                    }

                    key = Registry.CurrentUser.OpenSubKey($"{nameOfKey}", true);
                    key?.DeleteSubKey(kkk);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
        }

        [CustomAction]
        public static ActionResult RemoveChem4Word(Session session)
        {
            session.Log($"Begin {nameof(RemoveChem4Word)}()");

            session.Log($"  Running as {Environment.UserName}");

            try
            {
                AlterRegistry(session, null);
            }
            catch (Exception ex)
            {
                session.Log($"** Exception: {ex.Message} **");
            }

            session.Log($"End {nameof(RemoveChem4Word)}()");

            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult FindWord(Session session)
        {
            session.Log($"Begin {nameof(FindWord)}()");

            int officeVersion = OfficeHelper.GetWinWordVersionNumber();
            if (officeVersion >= 2010)
            {
                // Must be UPPERCASE
                session["WINWORDVERSION"] = officeVersion.ToString();
            }

            session.Log($"End {nameof(FindWord)}()");

            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult WordProcessCount(Session session)
        {
            session.Log($"Begin {nameof(WordProcessCount)}()");

            Process[] processes = Process.GetProcessesByName("winword");

            if (processes.Length > 0)
            {
                session["WINWORDPROCESSCOUNT"] = processes.Length.ToString();
            }

            session.Log($"End {nameof(WordProcessCount)}()");

            return ActionResult.Success;
        }

        private static void AlterRegistry(Session session, string manifestLocation)
        {
            session.Log($" Begin {nameof(AlterRegistry)}()");

            try
            {
                if (Environment.Is64BitOperatingSystem)
                {
                    RegistryKey rk = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);

                    // A bit lazy here as I'm just blasting it to both possible locations
                    string keyName = $@"Software\{OfficeKey}";
                    session.Log($"  Opening {keyName}");
                    RegistryKey rk2 = rk.OpenSubKey(keyName, true);
                    if (rk2 != null)
                    {
                        RegisterChem4WordAddIn(session, rk2, manifestLocation);
                    }

                    keyName = $@"Software\WOW6432Node\{OfficeKey}";
                    session.Log($"  Opening {keyName}");
                    rk2 = rk.OpenSubKey(keyName, true);
                    if (rk2 != null)
                    {
                        RegisterChem4WordAddIn(session, rk2, manifestLocation);
                    }
                }
                else
                {
                    RegistryKey rk = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);

                    string keyName = $@"Software\{OfficeKey}";
                    session.Log($"  Opening {keyName}");
                    RegistryKey rk2 = rk.OpenSubKey(keyName, true);
                    if (rk2 != null)
                    {
                        RegisterChem4WordAddIn(session, rk2, manifestLocation);
                    }
                }
            }
            catch (Exception ex)
            {
                session.Log($"** Exception: {ex.Message} **");
            }

            session.Log($" End {nameof(AlterRegistry)}()");
        }

        private static void RegisterChem4WordAddIn(Session session, RegistryKey rk, string manifestLocation)
        {
            session.Log($" End {nameof(RegisterChem4WordAddIn)}()");

            try
            {
                if (!string.IsNullOrEmpty(manifestLocation))
                {
                    session.Log($"  Creating (or Opening) {WordAddinsKey}");
                    RegistryKey rk2 = rk.CreateSubKey(WordAddinsKey);
                    if (rk2 != null)
                    {
                        session.Log(" Registering Chem4Word Add-In");
                        rk2.SetValue("Description", ProductShortName);
                        rk2.SetValue("FriendlyName", ProductLongName);
                        rk2.SetValue("LoadBehavior", 3, RegistryValueKind.DWord);
                        rk2.SetValue("Manifest", $"{manifestLocation}|vstolocal");
                    }
                }
                else
                {
                    string[] parts = WordAddinsKey.Split('\\');
                    string keyName = parts.Last();
                    string keyParent = string.Join(@"\", parts.Take(parts.Length - 1));
                    session.Log($"  Opening {keyParent}");
                    RegistryKey rk2 = rk.OpenSubKey(keyParent, true);
                    if (rk2 != null)
                    {
                        session.Log(" UnRegistering Chem4Word Add-In");
                        rk2.DeleteSubKey(keyName);
                    }
                }
            }
            catch (Exception ex)
            {
                session.Log($"** Exception: {ex.Message} **");
            }

            session.Log($" End {nameof(RegisterChem4WordAddIn)}()");
        }
    }
}