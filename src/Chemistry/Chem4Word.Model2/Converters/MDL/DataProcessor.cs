// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Chem4Word.Model2.Converters.MDL
{
    public class DataProcessor : SdFileBase
    {
        private List<PropertyType> _propertyTypes;
        private Molecule _molecule;

        public DataProcessor(List<PropertyType> propertyTypes)
        {
            _propertyTypes = propertyTypes;
        }

        public override SdfState ImportFromStream(StreamReader reader, Molecule molecule, out string message)
        {
            message = null;
            _molecule = molecule;

            SdfState result = SdfState.Null;

            try
            {
                bool isFormula = false;
                string internalName = "";

                while (!reader.EndOfStream)
                {
                    string line = SdFileConverter.GetNextLine(reader); //reader.ReadLine();;

                    if (!string.IsNullOrEmpty(line))
                    {
                        if (line.Equals(MDLConstants.SDF_END))
                        {
                            // End of SDF Section
                            result = SdfState.EndOfData;
                            break;
                        }

                        if (line.StartsWith(">"))
                        {
                            // Clear existing Property Name
                            internalName = string.Empty;

                            // See if we can find the property in our translation table
                            foreach (var property in _propertyTypes)
                            {
                                if (line.Equals(property.ExternalName))
                                {
                                    isFormula = property.IsFormula;
                                    internalName = property.InternalName;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            // Property Data
                            if (!string.IsNullOrEmpty(internalName))
                            {
                                if (isFormula)
                                {
                                    var formula = new TextualProperty();
                                    formula.FullType = internalName;
                                    formula.Value = line;
                                    _molecule.Formulas.Add(formula);
                                }
                                else
                                {
                                    var name = new TextualProperty();
                                    name.FullType = internalName;
                                    name.Value = line;
                                    _molecule.Names.Add(name);
                                }
                            }
                        }
                    }
                    else
                    {
                        internalName = string.Empty;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine(ex.Message);
                result = SdfState.Error;
            }

            return result;
        }

        public void ExportToStream(List<TextualProperty> names, List<TextualProperty> formulas, StreamWriter writer)
        {
            Dictionary<string, List<string>> properties = new Dictionary<string, List<string>>();

            foreach (var name in names)
            {
                if (properties.ContainsKey(name.FullType))
                {
                    properties[name.FullType].Add(name.Value);
                }
                else
                {
                    List<string> dataNames = new List<string>();
                    dataNames.Add(name.Value);
                    properties.Add(name.FullType, dataNames);
                }
            }

            foreach (var formula in formulas)
            {
                if (properties.ContainsKey(formula.FullType))
                {
                    properties[formula.FullType].Add(formula.Value);
                }
                else
                {
                    List<string> dataNames = new List<string>();
                    dataNames.Add(formula.Value);
                    properties.Add(formula.FullType, dataNames);
                }
            }

            foreach (var property in properties)
            {
                string externalName = null;
                foreach (var propertyType in _propertyTypes)
                {
                    if (propertyType.InternalName.Equals(property.Key))
                    {
                        externalName = propertyType.ExternalName;
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(externalName))
                {
                    writer.WriteLine(externalName);
                    foreach (var line in property.Value)
                    {
                        writer.WriteLine(line);
                    }
                    writer.WriteLine("");
                }
            }

            writer.WriteLine(MDLConstants.SDF_END);
        }
    }
}