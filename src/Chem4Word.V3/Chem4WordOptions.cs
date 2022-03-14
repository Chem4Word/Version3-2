// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using Chem4Word.Core.Helpers;
using Chem4Word.Helpers;
using Newtonsoft.Json;

namespace Chem4Word
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Chem4WordOptions
    {
        private const int DefaultCheckInterval = 7;
        private const bool DefaultCheckingEnabled = true;

        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

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
            var clone = new Chem4WordOptions();

            // Copy serialised properties
            clone.SetValuesFromCopy(this);

            return clone;
        }

        private string GetFileName(string path)
        {
            var fileName = $"{_product}.json";
            var optionsFile = Path.Combine(path, fileName);
            return optionsFile;
        }

        /// <summary>
        /// Load the Chem4Word Options from the path defined in SettingsPath using defaults if this is null or empty string
        /// </summary>
        public void Load()
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                var path = FileSystemHelper.GetWritablePath(SettingsPath);

                if (!string.IsNullOrEmpty(path))
                {
                    var optionsFile = GetFileName(path);

                    if (File.Exists(optionsFile))
                    {
                        try
                        {
                            Debug.WriteLine($"Reading Chem4Word Options from {optionsFile}");
                            var contents = ReadOptionsFile(optionsFile);

                            var options = JsonConvert.DeserializeObject<Chem4WordOptions>(contents);
                            SetValuesFromCopy(options);

                            var temp = JsonConvert.SerializeObject(options, Formatting.Indented);
                            if (!contents.Equals(temp))
                            {
                                // Auto fix the file if required
                                RegistryHelper.StoreMessage(module, $"Auto fixing {optionsFile}");
                                PersistOptions(optionsFile);
                            }
                        }
                        catch (Exception exception)
                        {
                            Debug.WriteLine(exception.Message);
                            Errors.Add(exception.Message);
                            Errors.Add(exception.StackTrace);

                            RestoreDefaults();

                            RegistryHelper.StoreException(module, exception);
                            RegistryHelper.StoreMessage(module, $"Setting {optionsFile} to defaults");
                            PersistOptions(optionsFile);
                        }
                    }
                    else
                    {
                        RestoreDefaults();
                        RegistryHelper.StoreMessage(module, $"Creating {optionsFile} with defaults");
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

        /// <summary>
        /// Save the Chem4Word Options to the path defined in SettingsPath using defaults if this is null or empty string
        /// </summary>
        public void Save()
        {
            var path = FileSystemHelper.GetWritablePath(SettingsPath);
            if (!string.IsNullOrEmpty(path))
            {
                var optionsFile = GetFileName(path);
                PersistOptions(optionsFile);
            }
        }

        private string ReadOptionsFile(string filename)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            var lines = new List<string>();

            try
            {
                using (var stream = new FileStream(filename,
                                                   FileMode.Open,
                                                   FileAccess.Read,
                                                   FileShare.ReadWrite))
                {
                    using (var bufferedStream = new BufferedStream(stream))
                    {
                        using (var streamReader = new StreamReader(bufferedStream))
                        {
                            string line;
                            while ((line = streamReader.ReadLine()) != null)
                            {
                                lines.Add(line);
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Errors.Add(exception.Message);
                Errors.Add(exception.StackTrace);
            }

            return string.Join(Environment.NewLine, lines);
        }

        private void PersistOptions(string filename)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                RegistryHelper.StoreMessage(module, $"Saving Chem4Word Options to {filename}");
                Debug.WriteLine($"Saving Chem4Word Options to {filename}");

                var contents = JsonConvert.SerializeObject(this, Formatting.Indented);

                using (var outStream = new FileStream(filename,
                                                      FileMode.OpenOrCreate,
                                                      FileAccess.Write,
                                                      FileShare.ReadWrite))
                {
                    var bytes = Encoding.UTF8.GetBytes(contents);
                    outStream.Write(bytes, 0, bytes.Length);
                }
            }
            catch (Exception exception)
            {
                Errors.Add(exception.Message);
                Errors.Add(exception.StackTrace);
            }
        }
    }
}