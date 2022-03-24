// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Converters.MDL;
using IChem4Word.Contracts;

namespace Chem4Word.WebServices
{
    public class PropertyCalculator
    {
        private static readonly string Product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static readonly string Class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private readonly IChem4WordTelemetry _telemetry;
        private readonly Point _parentTopLeft;
        private readonly string _version;

        public PropertyCalculator(IChem4WordTelemetry telemetry, Point wordTopLeft, string version)
        {
            _telemetry = telemetry;
            _parentTopLeft = wordTopLeft;
            _version = version;
        }

        public int CalculateProperties(List<Molecule> newMolecules)
        {
            string module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod().Name}()";

            var molConverter = new SdFileConverter();
            int changedProperties = 0;
            int newProperties = 0;

            int webServiceCalls = newMolecules.Count + 1;

            Progress pb = new Progress();
            pb.TopLeft = _parentTopLeft;
            pb.Value = 0;
            pb.Maximum = webServiceCalls;

            foreach (var molecule in newMolecules)
            {
                Model temp = new Model();
                var mol = molecule.Copy();
                temp.AddMolecule(mol);

                // GitHub: Issue #9 https://github.com/Chem4Word/Version3/issues/9
                int maxAtomicNumber = temp.MaxAtomicNumber;
                int minAtomicNumber = temp.MinAtomicNumber;

                var invalidBonds = new List<Bond>();
                if (mol.Bonds.Any())
                {
                    invalidBonds = mol.Bonds.Where(b => b.OrderValue != null && (CtabProcessor.MdlBondType(b.Order) < 1 || CtabProcessor.MdlBondType(b.Order) > 4)).ToList();
                }

                var calculatedNames = new List<TextualProperty>();
                var calculatedFormulae = new List<TextualProperty>();

                if (mol.HasFunctionalGroups || invalidBonds.Any() || minAtomicNumber < 1 || maxAtomicNumber > 118)
                {
                    // IUPAC InChi (1.05) generator does not support Mdl Bond Types < 1 or > 4 or Elements < 1 or > 118 or 'our' functional groups

                    #region Set Default properties

                    _telemetry.Write(module, "Information", $"Not sending structure to Web Service; HasFunctionalGroups: {mol.HasFunctionalGroups} Invalid Bonds: {invalidBonds?.Count} Min Atomic Number: {minAtomicNumber} Max Atomic Number: {maxAtomicNumber}");
                    calculatedNames.Add(new TextualProperty { FullType = CMLConstants.ValueChem4WordInchiName, Value = "Unable to calculate" });
                    //calculatedNames.Add(new TextualProperty { FullType = CMLConstants.ValueChem4WordAuxInfoName, Value = "Unable to calculate" });
                    calculatedNames.Add(new TextualProperty { FullType = CMLConstants.ValueChem4WordInchiKeyName, Value = "Unable to calculate" });

                    calculatedFormulae.Add(new TextualProperty { FullType = CMLConstants.ValueChem4WordResolverFormulaName, Value = "Not requested" });
                    calculatedNames.Add(new TextualProperty { FullType = CMLConstants.ValueChem4WordResolverIupacName, Value = "Not requested" });
                    calculatedFormulae.Add(new TextualProperty { FullType = CMLConstants.ValueChem4WordResolverSmilesName, Value = "Not requested" });

                    #endregion Set Default properties
                }
                else
                {
                    pb.Show();
                    pb.Increment(1);
                    pb.Message = $"Calculating InChiKey and Resolving Names using Chem4Word Web Service for molecule {molecule.Id}";

                    #region Obtain Calculated Properties

                    try
                    {
                        string afterMolFile = molConverter.Export(temp);

                        ChemicalServices cs = new ChemicalServices(_telemetry, _version);
                        var csr = cs.GetChemicalServicesResult(afterMolFile);

                        if (csr?.Properties != null && csr.Properties.Any())
                        {
                            var first = csr.Properties[0];
                            if (first != null)
                            {
                                var value = string.IsNullOrEmpty(first.Inchi) ? "Not found" : first.Inchi;
                                calculatedNames.Add(new TextualProperty { FullType = CMLConstants.ValueChem4WordInchiName, Value = value });

                                //value = string.IsNullOrEmpty(first.AuxInfo) ? "Not found" : first.AuxInfo;
                                //calculatedNames.Add(new TextualProperty { FullType = CMLConstants.ValueChem4WordAuxInfoName, Value = value });

                                value = string.IsNullOrEmpty(first.InchiKey) ? "Not found" : first.InchiKey;
                                calculatedNames.Add(new TextualProperty { FullType = CMLConstants.ValueChem4WordInchiKeyName, Value = value });

                                value = string.IsNullOrEmpty(first.Formula) ? "Not found" : first.Formula;
                                calculatedFormulae.Add(new TextualProperty { FullType = CMLConstants.ValueChem4WordResolverFormulaName, Value = value });

                                value = string.IsNullOrEmpty(first.Name) ? "Not found" : first.Name;
                                calculatedNames.Add(new TextualProperty { FullType = CMLConstants.ValueChem4WordResolverIupacName, Value = value });

                                value = string.IsNullOrEmpty(first.Smiles) ? "Not found" : first.Smiles;
                                calculatedFormulae.Add(new TextualProperty { FullType = CMLConstants.ValueChem4WordResolverSmilesName, Value = value });
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        _telemetry.Write(module, "Exception", $"{e}");
                    }

                    #endregion Obtain Calculated Properties
                }

                #region Merge in properties

                foreach (var formula in calculatedFormulae)
                {
                    var target = molecule.Formulas.FirstOrDefault(f => f.FullType.Equals(formula.FullType));
                    if (target == null)
                    {
                        molecule.Formulas.Add(formula);
                        newProperties++;
                    }
                    else
                    {
                        if (!target.Value.Equals(formula.Value))
                        {
                            target.Value = formula.Value;
                            changedProperties++;
                        }
                    }
                }

                foreach (var name in calculatedNames)
                {
                    var target = molecule.Names.FirstOrDefault(f => f.FullType.Equals(name.FullType));
                    if (target == null)
                    {
                        molecule.Names.Add(name);
                        newProperties++;
                    }
                    else
                    {
                        if (!target.Value.Equals(name.Value))
                        {
                            target.Value = name.Value;
                            changedProperties++;
                        }
                    }
                }

                #endregion Merge in properties
            }

            pb.Value = 0;
            pb.Hide();
            pb.Close();

            return changedProperties + newProperties;
        }
    }
}