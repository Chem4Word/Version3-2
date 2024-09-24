// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.UI.Forms;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Converters.MDL;
using IChem4Word.Contracts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace Chem4Word.WebServices
{
    public class PropertyCalculator
    {
        private static readonly string Product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static readonly string Class = MethodBase.GetCurrentMethod()?.DeclaringType?.Name;

        private readonly IChem4WordTelemetry _telemetry;
        private readonly Point _parentTopLeft;
        private readonly string _version;

        public PropertyCalculator(IChem4WordTelemetry telemetry, Point wordTopLeft, string version)
        {
            _telemetry = telemetry;
            _parentTopLeft = wordTopLeft;
            _version = version;
        }

        public int CalculateProperties(Model inputModel, bool showProgress = true)
        {
            string module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod()?.Name}()";

            Progress pb = null;

            if (showProgress)
            {
                pb = new Progress();
                pb.TopLeft = _parentTopLeft;
                pb.Value = 0;
                pb.Maximum = 1;
            }

            int changed = 0;

            var paths = new Dictionary<string, string>();

            var tempModel = new Model();
            tempModel.CreatorGuid = inputModel.CreatorGuid;

            var inputMolecules = inputModel.GetAllMolecules();

            foreach (var molecule in inputMolecules)
            {
                // Only consider molecules with at least one atom
                if (molecule.Atoms.Count > 0)
                {
                    var invalidBonds = new List<Bond>();
                    if (molecule.Bonds.Any())
                    {
                        invalidBonds = molecule.Bonds.Where(b => b.OrderValue != null && (CtabProcessor.MdlBondType(b.Order) < 1 || CtabProcessor.MdlBondType(b.Order) > 4)).ToList();
                    }

                    var maxAtomicNumber = 0;
                    var minAtomicNumber = 999;
                    var maxBonds = 0;

                    foreach (var atom in molecule.Atoms.Values)
                    {
                        if (atom.Element is Element element)
                        {
                            maxAtomicNumber = Math.Max(maxAtomicNumber, element.AtomicNumber);
                            minAtomicNumber = Math.Min(minAtomicNumber, element.AtomicNumber);
                        }

                        maxBonds = Math.Max(maxBonds, atom.Bonds.Count());
                    }

                    // If Molecule has any Functional Groups - don't add
                    // If Molecule only has CtabProcessor.MdlBondType(b.Order) between 1 and 4 - can add
                    // If all the Molecule's atoms are elements that have Atomic Numbers between 1 and 118 - can add
                    // If all the Molecule's atoms have <= 20 bonds - can add
                    if (!molecule.HasFunctionalGroups
                        && invalidBonds.Count == 0
                        && minAtomicNumber > 0 && maxAtomicNumber <= 118
                        && maxBonds <= 20)
                    {
                        var temp = molecule.Copy();
                        tempModel.AddMolecule(temp);
                        paths.Add(temp.Path, molecule.Path);
                    }
                    else
                    {
                        // Signify now that we are not going to try these ones
                        changed += UpsertProperty(molecule.Names, CMLConstants.ValueChem4WordInchiName, "Unable to calculate");
                        changed += UpsertProperty(molecule.Names, CMLConstants.ValueChem4WordInchiKeyName, "Unable to calculate");
                        changed += UpsertProperty(molecule.Names, CMLConstants.ValueChem4WordResolverIupacName, "Not requested");

                        changed += UpsertProperty(molecule.Formulas, CMLConstants.ValueChem4WordResolverFormulaName, "Not requested");
                        changed += UpsertProperty(molecule.Formulas, CMLConstants.ValueChem4WordResolverSmilesName, "Not requested");
                    }
                }
            }

            if (showProgress)
            {
                pb.Show();
                pb.Increment(1);
                pb.Message = $"Calculating InChiKey and Resolving Names using Chem4Word Web Service for {tempModel.Molecules.Count} molecules";
            }

            ChemicalServicesResult chemicalServicesResult = null;

            if (tempModel.TotalAtomsCount > 0)
            {
                var molConverter = new SdFileConverter();
                var sdfile = molConverter.Export(tempModel);

                try
                {
                    var chemicalServices = new ChemicalServices(_telemetry, _version);
                    chemicalServicesResult = chemicalServices.GetChemicalServicesResult(sdfile);
                }
                catch (Exception exception)
                {
                    _telemetry.Write(module, "Exception", $"{exception}");
                }
            }

            if (chemicalServicesResult != null)
            {
                var index = 0;
                foreach (var properties in chemicalServicesResult.Properties)
                {
                    var targetPath = paths.ElementAt(index);
                    var target = inputMolecules.FirstOrDefault(p => p.Path.Equals(targetPath.Value));

                    if (target != null)
                    {
                        var inchi = string.IsNullOrEmpty(properties.Inchi) ? "Not found" : properties.Inchi;
                        changed += UpsertProperty(target.Names, CMLConstants.ValueChem4WordInchiName, inchi);

                        var inchiKey = string.IsNullOrEmpty(properties.InchiKey) ? "Not found" : properties.InchiKey;
                        changed += UpsertProperty(target.Names, CMLConstants.ValueChem4WordInchiKeyName, inchiKey);

                        var name = string.IsNullOrEmpty(properties.Name) ? "Not found" : properties.Name;
                        changed += UpsertProperty(target.Names, CMLConstants.ValueChem4WordResolverIupacName, name);

                        var formula = string.IsNullOrEmpty(properties.Formula) ? "Not found" : properties.Formula;
                        changed += UpsertProperty(target.Formulas, CMLConstants.ValueChem4WordResolverFormulaName, formula);

                        var smiles = string.IsNullOrEmpty(properties.Smiles) ? "Not found" : properties.Smiles;
                        changed += UpsertProperty(target.Formulas, CMLConstants.ValueChem4WordResolverSmilesName, smiles);
                    }

                    index++;
                }
            }

            if (showProgress)
            {
                pb.Value = 0;
                pb.Hide();
                pb.Close();
            }

            return changed;
        }

        private int UpsertProperty(ObservableCollection<TextualProperty> list, string fullType, string value)
        {
            int result;

            var item = list.FirstOrDefault(i => i.FullType.Equals(fullType));
            if (item == null)
            {
                list.Add(new TextualProperty { FullType = fullType, Value = value });
                result = 1;
            }
            else
            {
                result = item.Value.Equals(value) ? 0 : 1;
                item.Value = value;
            }

            return result;
        }
    }
}