// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Chem4Word.Core.Helpers;
using Newtonsoft.Json;

namespace Chem4Word.Model2.Converters.MDL
{
    public class SdFileConverter
    {
        private List<PropertyType> _propertyTypes = null;

        public SdFileConverter()
        {
            var resource = ResourceHelper.GetStringResource(Assembly.GetExecutingAssembly(), "PropertyTypes.json");
            _propertyTypes = JsonConvert.DeserializeObject<List<PropertyType>>(resource);
        }

        public static int LineNumber;

        public static string GetNextLine(StreamReader sr)
        {
            LineNumber++;
            return sr.ReadLine();
        }

        public Model Import(object data)
        {
            Model model = null;

            if (data != null)
            {
                string dataAsString = (string)data;
                if (!dataAsString.Contains("v3000") && !dataAsString.Contains("V3000"))
                {
                    model = new Model();
                    LineNumber = 0;
                    // Convert incoming string to a stream
                    MemoryStream stream = new MemoryStream();
                    StreamWriter writer = new StreamWriter(stream);
                    writer.Write(dataAsString);
                    writer.Flush();
                    stream.Position = 0;

                    StreamReader sr = new StreamReader(stream);

                    Molecule molecule = null;

                    SdfState state = SdfState.Null;

                    string message = null;

                    while (!sr.EndOfStream)
                    {
                        switch (state)
                        {
                            case SdfState.Null:
                            case SdfState.EndOfData:
                                molecule = new Molecule();
                                CtabProcessor pct = new CtabProcessor();
                                state = pct.ImportFromStream(sr, molecule, out message);
                                if (state == SdfState.Error)
                                {
                                    model.GeneralErrors.Add(message);
                                }

                                Molecule copy = molecule.Copy();
                                copy.SplitIntoChildren();

                                // If copy now contains (child) molecules, replace original
                                if (copy.Molecules.Count > 1)
                                {
                                    molecule = copy;
                                }

                                //Ensure we add the molecule after it's populated
                                model.AddMolecule(molecule);
                                molecule.Parent = model;
                                if (model.Molecules.Count >= 16)
                                {
                                    model.GeneralErrors.Add("This file has greater than 16 structures!");
                                    sr.ReadToEnd();
                                }

                                break;

                            case SdfState.EndOfCtab:
                                DataProcessor dp = new DataProcessor(_propertyTypes);
                                state = dp.ImportFromStream(sr, molecule, out message);
                                break;

                            case SdfState.Error:
                                // Swallow rest of stream
                                sr.ReadToEnd();
                                break;

                            case SdfState.Unsupported:
                                // Swallow rest of stream
                                sr.ReadToEnd();
                                break;
                        }
                    }

                    model.Relabel(true);
                    model.Refresh();
                }
            }

            return model;
        }

        public string Export(Model model)
        {
            string result;

            double average = model.MeanBondLength;
            if (average < 1.53 || average > 1.55)
            {
                // MDL Standard bond length is 1.54 Angstoms (Å)
                // Should have already been done in Ribbon Export Button code
                model.ScaleToAverageBondLength(1.54);
            }

            MemoryStream stream = new MemoryStream();
            using (StreamWriter writer = new StreamWriter(stream))
            {
                foreach (var mol in model.Molecules.Values)
                {
                    List<Atom> atoms = mol.Atoms.Values.ToList();
                    List<Bond> bonds = mol.Bonds.ToList();
                    List<TextualProperty> names = mol.Names.ToList();
                    List<TextualProperty> formulas = mol.Formulas.ToList();

                    if (mol.Molecules.Any())
                    {
                        foreach (var child in mol.Molecules.Values)
                        {
                            GatherChildren(child, atoms, bonds, names, formulas);
                        }
                    }

                    CtabProcessor pct = new CtabProcessor();
                    pct.ExportToStream(atoms, bonds, writer);

                    DataProcessor dp = new DataProcessor(_propertyTypes);
                    dp.ExportToStream(names, formulas, writer);
                }

                writer.Flush();
            }

            result = Encoding.ASCII.GetString(stream.ToArray());
            return result;

            // Local Function
            void GatherChildren(Molecule molecule, List<Atom> atoms, List<Bond> bonds,
                                List<TextualProperty> names,
                                List<TextualProperty> formulas)
            {
                atoms.AddRange(molecule.Atoms.Values.ToList());
                bonds.AddRange(molecule.Bonds.ToList());
                names.AddRange(molecule.Names.ToList());
                formulas.AddRange(molecule.Formulas.ToList());

                foreach (var child in molecule.Molecules.Values)
                {
                    GatherChildren(child, atoms, bonds, names, formulas);
                }
            }
        }
    }
}