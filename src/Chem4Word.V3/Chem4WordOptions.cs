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
using System.Windows;
using Chem4Word.Core.Helpers;
using Newtonsoft.Json;

namespace Chem4Word
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Chem4WordOptions
    {
        private const int DefaultCheckInterval = 7;
        private const bool DefaultCheckingEnabled = true;

        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];

        #region Telemetry

        [JsonProperty]
        public bool TelemetryEnabled { get; set; }

        #endregion Telemetry

        #region Automatic Updates - Not Serialised

        public bool AutoUpdateEnabled { get; set; }

        public int AutoUpdateFrequency { get; set; }

        #endregion Automatic Updates - Not Serialised

        #region Selected Plug Ins

        [JsonProperty]
        public string SelectedEditorPlugIn { get; set; }

        [JsonProperty]
        public string SelectedRendererPlugIn { get; set; }

        #endregion Selected Plug Ins

        #region General

        [JsonProperty]
        public int BondLength { get; set; }

        [JsonProperty]
        public bool RemoveExplicitHydrogensOnImportFromFile { get; set; }

        [JsonProperty]
        public bool RemoveExplicitHydrogensOnImportFromSearch { get; set; }

        [JsonProperty]
        public bool RemoveExplicitHydrogensOnImportFromLibrary { get; set; }

        [JsonProperty]
        public bool SetBondLengthOnImportFromFile { get; set; }

        [JsonProperty]
        public bool SetBondLengthOnImportFromSearch { get; set; }

        [JsonProperty]
        public bool SetBondLengthOnImportFromLibrary { get; set; }

        #endregion General

        // Not serialised
        public Point WordTopLeft { get; set; }

        public string SettingsPath { get; set; }

        public List<string> Errors { get; set; }

        /// <summary>
        /// Load clean set of Chem4Word Options with default values
        /// </summary>
        public Chem4WordOptions()
        {
            Errors = new List<string>();
            RestoreDefaults();
        }

        /// <summary>
        /// Load set of Chem4Word options
        /// </summary>
        /// <param name="path">Folder where the Chem4Word options are to reside - pass null to load from default path</param>
        public Chem4WordOptions(string path)
        {
            SettingsPath = path;
            Errors = new List<string>();
            Load();
        }

        public void RestoreDefaults()
        {
            TelemetryEnabled = true;

            SelectedEditorPlugIn = Constants.DefaultEditorPlugIn;
            SelectedRendererPlugIn = Constants.DefaultRendererPlugIn;

            BondLength = (int)Constants.StandardBondLength;

            SetBondLengthOnImportFromFile = true;
            SetBondLengthOnImportFromSearch = true;
            SetBondLengthOnImportFromLibrary = true;

            RemoveExplicitHydrogensOnImportFromFile = false;
            RemoveExplicitHydrogensOnImportFromSearch = false;
            RemoveExplicitHydrogensOnImportFromLibrary = false;

            // Non serialised settings
            AutoUpdateEnabled = DefaultCheckingEnabled;
            AutoUpdateFrequency = DefaultCheckInterval;
        }

        public Chem4WordOptions Clone()
        {
            Chem4WordOptions clone = new Chem4WordOptions();

            // Copy serialised properties
            clone.SetValuesFromCopy(this);

            return clone;
        }

        private string GetFileName(string path)
        {
            string fileName = $"{_product}.json";
            string optionsFile = Path.Combine(path, fileName);
            return optionsFile;
        }

        /// <summary>
        /// Load the Chem4Word Options from the path defined in SettingsPath using defaults if this is null or empty string
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
                            Debug.WriteLine($"Reading Chem4Word Options from {optionsFile}");
                            string contents = File.ReadAllText(optionsFile);
                            var options = JsonConvert.DeserializeObject<Chem4WordOptions>(contents);
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

        private void SetValuesFromCopy(Chem4WordOptions copy)
        {
            // Serialised values
            TelemetryEnabled = copy.TelemetryEnabled;

            SelectedEditorPlugIn = copy.SelectedEditorPlugIn;
            SelectedRendererPlugIn = copy.SelectedRendererPlugIn;

            BondLength = copy.BondLength;

            SetBondLengthOnImportFromFile = copy.SetBondLengthOnImportFromFile;
            SetBondLengthOnImportFromSearch = copy.SetBondLengthOnImportFromSearch;
            SetBondLengthOnImportFromLibrary = copy.SetBondLengthOnImportFromLibrary;

            RemoveExplicitHydrogensOnImportFromFile = copy.RemoveExplicitHydrogensOnImportFromFile;
            RemoveExplicitHydrogensOnImportFromSearch = copy.RemoveExplicitHydrogensOnImportFromSearch;
            RemoveExplicitHydrogensOnImportFromLibrary = copy.RemoveExplicitHydrogensOnImportFromLibrary;

            // Non serialised settings
            AutoUpdateEnabled = copy.AutoUpdateEnabled;
            AutoUpdateFrequency = copy.AutoUpdateFrequency;
        }

        private void PersistOptions(string optionsFile)
        {
            try
            {
                Debug.WriteLine($"Saving Chem4Word Options to {optionsFile}");
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
        /// Save the Chem4Word Options to the path defined in SettingsPath using defaults if this is null or empty string
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