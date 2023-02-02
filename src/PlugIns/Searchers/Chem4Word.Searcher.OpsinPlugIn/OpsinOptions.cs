// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
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

namespace Chem4Word.Searcher.OpsinPlugIn
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SearcherOptions
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];

        [JsonProperty]
        public string OpsinWebServiceUri { get; set; }

        [JsonProperty]
        public int DisplayOrder { get; set; }

        // Not serialised
        public string SettingsPath { get; set; }

        public List<string> Errors { get; set; }

        /// <summary>
        /// Load clean set of Options with default values
        /// </summary>
        public SearcherOptions()
        {
            Errors = new List<string>();
            RestoreDefaults();
        }

        /// <summary>
        /// Load set of options
        /// </summary>
        /// <param name="path">Folder where the options are to reside - pass null to load from default path</param>
        public SearcherOptions(string path)
        {
            SettingsPath = path;
            Errors = new List<string>();
            Load();
        }

        private string GetFileName(string path)
        {
            string fileName = $"{_product}.json";
            string optionsFile = Path.Combine(path, fileName);
            return optionsFile;
        }

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
                            var options = JsonConvert.DeserializeObject<SearcherOptions>(contents);
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

        private void PersistOptions(string optionsFile)
        {
            try
            {
                Debug.WriteLine($"Saving Opsin Options to {optionsFile}");
                string contents = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(optionsFile, contents);
            }
            catch (Exception exception)
            {
                Errors.Add(exception.Message);
                Errors.Add(exception.StackTrace);
            }
        }

        private void SetValuesFromCopy(SearcherOptions options)
        {
            OpsinWebServiceUri = options.OpsinWebServiceUri;
            DisplayOrder = options.DisplayOrder;
        }

        public SearcherOptions Clone()
        {
            SearcherOptions clone = new SearcherOptions();

            // Copy serialised properties
            clone.SetValuesFromCopy(this);

            clone.SettingsPath = SettingsPath;

            return clone;
        }

        public void RestoreDefaults()
        {
            OpsinWebServiceUri = Constants.DefaultOpsinWebServiceUri;
            DisplayOrder = Constants.DefaultDisplayOrder;
        }
    }
}