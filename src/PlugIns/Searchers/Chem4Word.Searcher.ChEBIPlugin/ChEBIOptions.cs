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
using Chem4Word.Core.Helpers;
using Newtonsoft.Json;

namespace Chem4Word.Searcher.ChEBIPlugin
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ChEBIOptions
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];

        #region Constructors

        /// <summary>
        /// Load clean set of Options with default values
        /// </summary>
        public ChEBIOptions()
        {
            Errors = new List<string>();
            RestoreDefaults();
        }

        /// <summary>
        /// Load set of Options
        /// </summary>
        /// <param name="path">Folder where the Options are to reside - pass null to load from default path</param>
        public ChEBIOptions(string path)
        {
            SettingsPath = path;
            Errors = new List<string>();
            Load();
        }

        #endregion Constructors

        #region Properties

        [JsonProperty]
        public string ChEBIWebServiceUri { get; set; }

        [JsonProperty]
        public int DisplayOrder { get; set; }

        [JsonProperty]
        public int MaximumResults { get; set; }

        // Not serialised
        public string SettingsPath { get; set; }

        public List<string> Errors { get; set; }

        #endregion Properties

        #region Methods

        private void Load()
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
                            Debug.WriteLine($"Reading Options from {optionsFile}");
                            string contents = File.ReadAllText(optionsFile);
                            var options = JsonConvert.DeserializeObject<ChEBIOptions>(contents);
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

        private void SetValuesFromCopy(ChEBIOptions options)
        {
            ChEBIWebServiceUri = options.StripTrailingSlash(options.ChEBIWebServiceUri);
            DisplayOrder = options.DisplayOrder;
            MaximumResults = options.MaximumResults;
        }

        private void PersistOptions(string optionsFile)
        {
            try
            {
                Debug.WriteLine($"Saving ChEBI Options to {optionsFile}");
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
        /// Save the Options to the path defined in SettingsPath using defaults if this is null or empty string
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

        private string GetFileName(string path)
        {
            string fileName = $"{_product}.json";
            string optionsFile = Path.Combine(path, fileName);
            return optionsFile;
        }

        public ChEBIOptions Clone()
        {
            ChEBIOptions clone = new ChEBIOptions();

            // Copy serialised properties
            clone.SetValuesFromCopy(this);

            clone.SettingsPath = SettingsPath;

            return clone;
        }

        public void RestoreDefaults()
        {
            ChEBIWebServiceUri = Constants.DefaultChEBIWebServiceUri;
            DisplayOrder = Constants.DefaultDisplayOrder;
            MaximumResults = Constants.DefaultMaximumSearchResults;
        }

        private string StripTrailingSlash(string uri)
        {
            if (!string.IsNullOrEmpty(uri) && uri.EndsWith("/"))
            {
                uri = uri.Remove(uri.Length - 1);
            }

            return uri;
        }

        #endregion Methods
    }
}