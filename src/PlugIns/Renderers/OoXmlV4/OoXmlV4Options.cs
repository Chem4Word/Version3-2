// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
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

namespace Chem4Word.Renderer.OoXmlV4
{
    [JsonObject(MemberSerialization.OptIn)]
    public class OoXmlV4Options
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];

        [JsonProperty]
        public bool ShowHydrogens { get; set; }

        [JsonProperty]
        public bool ColouredAtoms { get; set; }

        [JsonProperty]
        public bool ShowMoleculeGrouping { get; set; }

        [JsonProperty]
        public bool ShowCarbons { get; set; }

        [JsonProperty]
        public bool ShowMoleculeCaptions { get; set; }

        [JsonProperty]
        public bool RenderCaptionsAsTextBox { get; set; }

        // Not editable here, kept in sync by file system watcher
        [JsonProperty]
        public int BondLength { get; set; }

        // Debugging
        [JsonProperty]
        public bool ClipLines { get; set; }

        // Debugging
        [JsonProperty]
        public bool ShowBondClippingLines { get; set; }

        // Debugging
        [JsonProperty]
        public bool ShowBondDirection { get; set; }

        // Debugging
        [JsonProperty]
        public bool ShowCharacterBoundingBoxes { get; set; }

        // Debugging
        [JsonProperty]
        public bool ShowMoleculeBoundingBoxes { get; set; }

        // Debugging
        [JsonProperty]
        public bool ShowAtomPositions { get; set; }

        // Debugging
        [JsonProperty]
        public bool ShowHulls { get; set; }

        // Debugging
        [JsonProperty]
        public bool ShowRingCentres { get; set; }

        // Not serialised
        public string SettingsPath { get; set; }

        public List<string> Errors { get; set; }

        /// <summary>
        /// Load clean set of Chem4Word Options with default values
        /// </summary>
        public OoXmlV4Options()
        {
            Errors = new List<string>();
            RestoreDefaults();
        }

        /// <summary>
        /// Load set of OoXmlV4 options
        /// </summary>
        /// <param name="path">Folder where the OoXmlV4 options are to reside - pass null to load from default path</param>
        public OoXmlV4Options(string path)
        {
            SettingsPath = path;
            Errors = new List<string>();
            Load();
        }

        /// <summary>
        /// Load the OoXmlV4 Options from the path defined in SettingsPath using defaults if this is null or empty string
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
                            Debug.WriteLine($"Reading OoXmlV4 Options from {optionsFile}");
                            string contents = File.ReadAllText(optionsFile);
                            var options = JsonConvert.DeserializeObject<OoXmlV4Options>(contents);
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
        /// Save the OoXmlV4 Options to the path defined in SettingsPath using defaults if this is null or empty string
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
                Debug.WriteLine($"Saving OoXmlV4 Options to {optionsFile}");
                string contents = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(optionsFile, contents);
            }
            catch (Exception exception)
            {
                Errors.Add(exception.Message);
                Errors.Add(exception.StackTrace);
            }
        }

        private void SetValuesFromCopy(OoXmlV4Options options)
        {
            // Main User Options
            ColouredAtoms = options.ColouredAtoms;
            ShowHydrogens = options.ShowHydrogens;
            ShowMoleculeGrouping = options.ShowMoleculeGrouping;
            ShowMoleculeCaptions = options.ShowMoleculeCaptions;
            RenderCaptionsAsTextBox = options.RenderCaptionsAsTextBox;
            ShowCarbons = options.ShowCarbons;

            // Hidden
            BondLength = options.BondLength;

            // Debugging Options
            ClipLines = options.ClipLines;
            ShowCharacterBoundingBoxes = options.ShowCharacterBoundingBoxes;
            ShowMoleculeBoundingBoxes = options.ShowMoleculeBoundingBoxes;
            ShowRingCentres = options.ShowRingCentres;
            ShowAtomPositions = options.ShowAtomPositions;
            ShowHulls = options.ShowHulls;
            ShowBondClippingLines = options.ShowBondClippingLines;
            ShowBondDirection = options.ShowBondDirection;
        }

        private string GetFileName(string path)
        {
            string fileName = $"{_product}.json";
            string optionsFile = Path.Combine(path, fileName);
            return optionsFile;
        }

        public OoXmlV4Options Clone()
        {
            OoXmlV4Options clone = new OoXmlV4Options();

            // Copy serialised properties
            clone.SetValuesFromCopy(this);

            clone.SettingsPath = SettingsPath;

            return clone;
        }

        public void RestoreDefaults()
        {
            // Main User Options
            ShowHydrogens = true;
            ColouredAtoms = true;
            ShowMoleculeGrouping = true;
            ShowMoleculeCaptions = false;
            RenderCaptionsAsTextBox = false;
            ShowCarbons = false;

            // Hidden
            BondLength = (int)Constants.StandardBondLength;

            // Debugging Options
            ClipLines = true;
            ShowCharacterBoundingBoxes = false;
            ShowMoleculeBoundingBoxes = false;
            ShowRingCentres = false;
            ShowAtomPositions = false;
            ShowHulls = false;
            ShowBondClippingLines = false;
            ShowBondDirection = false;
        }
    }
}