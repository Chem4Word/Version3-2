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
using System.Reflection;
using Chem4Word.Core.Helpers;
using Newtonsoft.Json;

namespace Chem4Word.ACME
{
    [JsonObject(MemberSerialization.OptIn)]
    public class AcmeOptions
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];

        private int _bondLength;

        [JsonProperty]
        public int BondLength
        {
            get => _bondLength;
            set
            {
                _bondLength = value;
                Dirty = true;
            }
        }

        [JsonProperty]
        public bool ShowMoleculeGrouping { get; set; }

        [JsonProperty]
        public bool ShowHydrogens { get; set; }

        [JsonProperty]
        public bool ColouredAtoms { get; set; }

        [JsonProperty]
        public bool ShowCarbons { get; set; }

        public string SettingsPath { get; set; }
        public List<string> Errors { get; set; }

        public bool Dirty { get; set; }

        /// <summary>
        /// Load clean set of ACME Options with default values
        /// </summary>
        public AcmeOptions()
        {
            Errors = new List<string>();
            RestoreDefaults();
        }

        /// <summary>
        /// Load set of ACME options
        /// </summary>
        /// <param name="path">Folder where the ACME options are to reside - pass null to load from default path</param>
        public AcmeOptions(string path)
        {
            SettingsPath = path;
            Errors = new List<string>();
            Load();
        }

        /// <summary>
        /// Make an independent copy of the settings
        /// </summary>
        /// <returns>AcmeOptions</returns>
        public AcmeOptions Clone()
        {
            AcmeOptions clone = new AcmeOptions();

            // Copy serialised properties
            clone.SetValuesFromCopy(this);

            clone.SettingsPath = SettingsPath;

            return clone;
        }

        private void SetValuesFromCopy(AcmeOptions copy)
        {
            BondLength = copy.BondLength;
            ShowMoleculeGrouping = copy.ShowMoleculeGrouping;
            ColouredAtoms = copy.ColouredAtoms;
            ShowHydrogens = copy.ShowHydrogens;
            ShowCarbons = copy.ShowCarbons;
        }

        /// <summary>
        /// Restore ACME system default options
        /// </summary>
        public void RestoreDefaults()
        {
            BondLength = (int)Constants.StandardBondLength;

            ShowMoleculeGrouping = true;
            ShowHydrogens = true;
            ColouredAtoms = true;
            ShowCarbons = false;

            // Non serialised
            Dirty = false;
        }

        private string GetFileName(string path)
        {
            string fileName = $"{_product}.json";
            string optionsFile = Path.Combine(path, fileName);
            return optionsFile;
        }

        /// <summary>
        /// Load the ACME Options from the path defined in SettingsPath using defaults if this is null or empty string
        /// </summary>
        public void Load()
        {
            try
            {
                string path = FileSystemHelper.GetWritablePath(SettingsPath);

                if (!string.IsNullOrEmpty(path))
                {
                    string optionsFile = GetFileName(path);

                    if (File.Exists(optionsFile))
                    {
                        try
                        {
                            Debug.WriteLine($"Reading ACME Options from {optionsFile}");
                            string contents = File.ReadAllText(optionsFile);
                            var options = JsonConvert.DeserializeObject<AcmeOptions>(contents);
                            SetValuesFromCopy(options);

                            string temp = JsonConvert.SerializeObject(options, Formatting.Indented);
                            if (!contents.Equals(temp))
                            {
                                // Auto fix the file if required
                                PersistOptions(optionsFile);
                            }
                        }
                        catch (Exception exception)
                        {
                            Debug.WriteLine(exception.Message);
                            Errors.Add(exception.Message);
                            Errors.Add(exception.StackTrace);

                            RestoreDefaults();
                            PersistOptions(optionsFile);
                        }
                    }
                    else
                    {
                        RestoreDefaults();
                        PersistOptions(optionsFile);
                    }
                }
            }
            catch (Exception exception)
            {
                Errors.Add(exception.Message);
                Errors.Add(exception.StackTrace);
            }
        }

        private void PersistOptions(string optionsFile)
        {
            try
            {
                Debug.WriteLine($"Saving ACME Options to {optionsFile}");
                string contents = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(optionsFile, contents);
            }
            catch (Exception exception)
            {
                Errors.Add(exception.Message);
                Errors.Add(exception.StackTrace);
            }
        }

        /// <summary>
        /// Save the ACME Options to the path defined in SettingsPath using defaults if this is null or empty string
        /// </summary>
        public void Save()
        {
            string path = FileSystemHelper.GetWritablePath(SettingsPath);
            if (!string.IsNullOrEmpty(path))
            {
                string optionsFile = GetFileName(path);
                PersistOptions(optionsFile);
            }
        }
    }
}