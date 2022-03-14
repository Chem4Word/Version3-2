// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Converters.MDL;
using Chem4Word.Model2.Helpers;
using Xunit;

namespace Chem4WordTests
{
    public class PersistenceTests
    {
        [Theory]
        [InlineData("Trimethylamine-Normal.xml")]
        [InlineData("Trimethylamine-MinusArrays.xml")]
        [InlineData("Trimethylamine-MinusNamespace.xml")]
        [InlineData("Trimethylamine-MoleculeRoot.xml")]
        public void CheckCmlImportVariants(string file)
        {
            // Arrange

            // Act
            CMLConverter mc = new CMLConverter();
            Model m = mc.Import(ResourceHelper.GetStringResource(file));

            // Assert
            Assert.True(m.Molecules.Count == 1, $"Expected 1 Molecule; Got {m.Molecules.Count}");
            var molecule = m.Molecules.Values.First();
            Assert.True(molecule.Atoms.Count == 4, $"Expected 4 Atoms; Got {molecule.Atoms.Count}");
            Assert.True(molecule.Bonds.Count == 3, $"Expected 3 Bonds; Got {molecule.Bonds.Count}");

            var atom = molecule.Atoms.Values.ToArray()[1];
            Assert.True(atom.SymbolText == "N", $"Expected N; Got {atom.SymbolText}");
        }

        [Theory]
        [InlineData("NoAtoms.xml", 1, 0, 0, 0, 0, 1, 0)]
        [InlineData("Benzene.xml", 1, 6, 6, 1, 1, 2, 2)]
        [InlineData("Testosterone.xml", 1, 25, 28, 4, 4, 4, 2)]
        [InlineData("Phthalocyanine.xml", 1, 58, 66, 9, 8, 2, 3)]
        [InlineData("CopperPhthalocyanine.xml", 1, 57, 68, 12, 12, 1, 0)]
        public void CmlImport(string file, int molecules, int atoms, int bonds, int allRings, int placementRings, int names, int formulas)
        {
            var mc = new CMLConverter();
            Model model = mc.Import(ResourceHelper.GetStringResource(file));

            Assert.True(model.Molecules.Count == molecules, $"Expected {molecules} Molecules; Got {model.Molecules.Count}");
            Assert.True(model.TotalAtomsCount == atoms, $"Expected {atoms} Atoms; Got {model.TotalAtomsCount}");
            Assert.True(model.TotalBondsCount == bonds, $"Expected {bonds} Bonds; Got {model.TotalBondsCount}");

            Molecule molecule = model.Molecules.Values.First();
            Assert.True(molecule.Rings.Count == allRings, $"Expected {allRings} Rings; Got {molecule.Rings.Count}");
            Assert.True(molecule.Names.Count == names, $"Expected {names} Chemical Names; Got {molecule.Names.Count}");
            Assert.True(molecule.Formulas.Count == formulas, $"Expected {formulas} Chemical Formulas; Got {molecule.Formulas.Count}");

            var list = molecule.SortRingsForDBPlacement();
            Assert.True(list.Count == placementRings, $"Expected {placementRings} Placement Rings; Got {list.Count}");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CheckMissingIdsGenerated(bool hasUsed1D)
        {
            var mc = new CMLConverter();
            var used1dLabels = new List<string>
                               {
                                   "m1.fx", // Used formula
                                   "m1.nx", // Used name
                                   "m1.lx"  // Used caption
                               };

            Model model;
            if (hasUsed1D)
            {
                model = mc.Import(ResourceHelper.GetStringResource("Benzene-With-Missing-Ids.xml"), used1dLabels, false);
            }
            else
            {
                model = mc.Import(ResourceHelper.GetStringResource("Benzene-With-Missing-Ids.xml"));
            }

            // Basic Sanity Checks
            Assert.True(model.Molecules.Count == 1, $"Expected 1 Molecule; Got {model.Molecules.Count}");
            Molecule molecule = model.Molecules.Values.First();
            Assert.True(molecule.Atoms.Count == 6, $"Expected 6 Atoms; Got {molecule.Atoms.Count}");
            Assert.True(molecule.Bonds.Count == 6, $"Expected 6 Bonds; Got {molecule.Bonds.Count}");

            Assert.True(molecule.Formulas.Count == 2, $"Expected 2 Formulas; Got {molecule.Formulas.Count}");
            Assert.True(molecule.Names.Count == 3, $"Expected 3 Names; Got {molecule.Names.Count}");
            Assert.True(molecule.Captions.Count == 3, $"Expected 3 Captions; Got {molecule.Captions.Count}");

            // Expect atoms and bonds to always be re-labeled
            Assert.True(molecule.Atoms.Last().Value.Id == "a6", $"Expected Id of 'a6'; Got Id of '{molecule.Atoms.Last().Value.Id}'");
            Assert.True(molecule.Bonds[5].Id == "b6", $"Expected Id of 'b6'; Got Id of '{molecule.Bonds[5].Id}'");

            if (hasUsed1D)
            {
                Assert.True(molecule.Formulas[0].Id == "m1.fx", $"Expected Id of 'm1.fx'; Got Id of '{molecule.Formulas[0].Id}'");
                Assert.True(molecule.Formulas[1].Id == "m1.f1", $"Expected Id of 'm1.f1'; Got Id of '{molecule.Formulas[1].Id}'");

                Assert.True(molecule.Names[0].Id == "m1.n1", $"Expected Id of 'm1.n1'; Got Id of '{molecule.Names[0].Id}'");
                Assert.True(molecule.Names[1].Id == "m1.nx", $"Expected Id of 'm1.nx'; Got Id of '{molecule.Names[1].Id}'");
                Assert.True(molecule.Names[2].Id == "m1.n2", $"Expected Id of 'm1.n2'; Got Id of '{molecule.Names[2].Id}'");

                Assert.True(molecule.Captions[0].Id == "m1.lx", $"Expected Id of 'm1.lx'; Got Id of '{molecule.Captions[0].Id}'");
                Assert.True(molecule.Captions[1].Id == "m1.l9", $"Expected Id of 'm1.l9'; Got Id of '{molecule.Captions[1].Id}'");
                Assert.True(molecule.Captions[2].Id == "m1.l10", $"Expected Id of 'm1.l10'; Got Id of '{molecule.Captions[2].Id}'");
            }
            else
            {
                Assert.True(molecule.Formulas[0].Id == "m1.f1", $"Expected Id of 'm1.f1'; Got Id of '{molecule.Formulas[0].Id}'");
                Assert.True(molecule.Formulas[1].Id == "m1.f2", $"Expected Id of 'm1.f2'; Got Id of '{molecule.Formulas[1].Id}'");

                Assert.True(molecule.Names[0].Id == "m1.n1", $"Expected Id of 'm1.n1'; Got Id of '{molecule.Names[0].Id}'");
                Assert.True(molecule.Names[1].Id == "m1.n2", $"Expected Id of 'm1.n2'; Got Id of '{molecule.Names[1].Id}'");
                Assert.True(molecule.Names[2].Id == "m1.n3", $"Expected Id of 'm1.n3'; Got Id of '{molecule.Names[2].Id}'");

                Assert.True(molecule.Captions[0].Id == "m1.l1", $"Expected Id of 'm1.l1'; Got Id of '{molecule.Captions[0].Id}'");
                Assert.True(molecule.Captions[1].Id == "m1.l2", $"Expected Id of 'm1.l2'; Got Id of '{molecule.Captions[1].Id}'");
                Assert.True(molecule.Captions[2].Id == "m1.l3", $"Expected Id of 'm1.l3'; Got Id of '{molecule.Captions[2].Id}'");
            }
        }

        [Fact]
        public void CmlImportNested()
        {
            CMLConverter mc = new CMLConverter();
            Model model = mc.Import(ResourceHelper.GetStringResource("NestedMolecules.xml"));

            // Basic Sanity Checks
            Assert.True(model.Molecules.Count == 1, $"Expected 1 Molecule; Got {model.Molecules.Count}");
            // Check molecule m0 has 4 child molecules and no atoms
            Molecule molecule = model.Molecules.Values.First();
            Assert.True(molecule.Molecules.Count == 4, $"Expected 4 Molecules; Got {molecule.Molecules.Count}");
            Assert.True(molecule.Atoms.Count == 0, $"Expected 0 Atoms; Got {molecule.Atoms.Count}");
            // Check molecule m2 has no child molecules and 6 atoms
            molecule = model.Molecules.Values.First().Molecules.Values.ToList()[1];
            Assert.True(molecule.Molecules.Count == 0, $"Expected 0 Molecule; Got {molecule.Molecules.Count}");
            Assert.True(molecule.Atoms.Count == 6, $"Expected 6 Atoms; Got {molecule.Atoms.Count}");
            // Check molecule m1 has 1 child molecules and no atoms
            molecule = model.Molecules.Values.First().Molecules.Values.First();
            Assert.True(molecule.Molecules.Count == 1, $"Expected 1 Molecule; Got {molecule.Molecules.Count}");
            Assert.True(molecule.Atoms.Count == 0, $"Expected 0 Atoms; Got {molecule.Atoms.Count}");
            // Check molecule m5 has 1 child molecules and 6 atoms
            molecule = model.Molecules.Values.First().Molecules.Values.First().Molecules.Values.First();
            Assert.True(molecule.Molecules.Count == 0, $"Expected 0 Molecule; Got {molecule.Molecules.Count}");
            Assert.True(molecule.Atoms.Count == 6, $"Expected 6 Atoms; Got {molecule.Atoms.Count}");
        }

        [Fact]
        public void CmlExportSingleMolecule_Cml_Root()
        {
            // Arrange
            Model model = CreateSingleMolecule();

            // Act
            var cml = new CMLConverter().Export(model);

            // Assert
            var expected = ResourceHelper.GetStringResource("OneMoleculeWthCmlRoot.xml");
            Assert.Equal(expected, cml);
        }

        [Fact]
        public void CmlExportFlatMolecules_Cml_Root()
        {
            // Arrange
            Model model = CreateTwoFlatMolecule();

            // Act
            var cml = new CMLConverter().Export(model);

            // Assert
            var expected = ResourceHelper.GetStringResource("TwoMoleculesWithCmlRoot.xml");
            Assert.Equal(expected, cml);
        }

        [Fact]
        public void CmlExportSingleMolecule_RequestChemDrawFormat_IsAccepted()
        {
            // Arrange
            Model model = CreateSingleMolecule();

            // Act
            var cml = new CMLConverter().Export(model, format: CmlFormat.ChemDraw);

            // Assert
            var expected = ResourceHelper.GetStringResource("ChemDraw.xml");
            Assert.Equal(expected, cml);
        }

        [Fact]
        public void CmlExportSingleMolecule_RequestMarvinJSFormat_IsAccepted()
        {
            // Arrange
            Model model = CreateSingleMolecule();

            // Act
            var cml = new CMLConverter().Export(model, format: CmlFormat.MarvinJs);

            // Assert
            var expected = ResourceHelper.GetStringResource("MarvinJs.xml");
            Assert.Equal(expected, cml);
        }

        [Fact]
        public void CmlExportTwoFlatMolecules_RequestChemDrawFormat_IsRejected()
        {
            // Arrange
            Model model = CreateTwoFlatMolecule();

            // Act
            var cml = new CMLConverter().Export(model, format: CmlFormat.ChemDraw);

            // Assert
            var expected = ResourceHelper.GetStringResource("TwoMoleculesWithCmlRoot.xml");
            Assert.Equal(expected, cml);
        }

        private Model CreateSingleMolecule()
        {
            Model model = new Model();
            var atom1 = new Atom
            {
                Position = new Point(0, 0),
                Element = Globals.PeriodicTable.C
            };

            var atom2 = new Atom
            {
                Position = new Point(10, 10),
                Element = Globals.PeriodicTable.C
            };

            var molecule1 = new Molecule();
            molecule1.AddAtom(atom1);
            atom1.Parent = molecule1;
            molecule1.AddAtom(atom2);
            atom2.Parent = molecule1;

            var bond1 = new Bond(atom1, atom2)
            {
                Order = Globals.OrderSingle
            };
            molecule1.AddBond(bond1);
            bond1.Parent = molecule1;

            model.AddMolecule(molecule1);
            molecule1.Parent = model;

            model.Relabel(true);

            return model;
        }

        private Model CreateTwoFlatMolecule()
        {
            Model model = new Model();
            var atom1 = new Atom
            {
                Position = new Point(0, 0),
                Element = Globals.PeriodicTable.C
            };

            var atom2 = new Atom
            {
                Position = new Point(10, 10),
                Element = Globals.PeriodicTable.C
            };

            var bond1 = new Bond(atom1, atom2)
            {
                Order = Globals.OrderSingle
            };

            var molecule1 = new Molecule();
            molecule1.AddAtom(atom1);
            atom1.Parent = molecule1;
            molecule1.AddAtom(atom2);
            atom2.Parent = molecule1;
            molecule1.AddBond(bond1);
            bond1.Parent = molecule1;

            var atom3 = new Atom
            {
                Position = new Point(20, 20),
                Element = Globals.PeriodicTable.C
            };

            var atom4 = new Atom
            {
                Position = new Point(30, 30),
                Element = Globals.PeriodicTable.C
            };

            var bond2 = new Bond(atom3, atom4)
            {
                Order = Globals.OrderSingle
            };

            var molecule2 = new Molecule();
            molecule2.AddAtom(atom3);
            atom1.Parent = molecule2;
            molecule2.AddAtom(atom4);
            atom2.Parent = molecule2;
            molecule2.AddBond(bond2);
            bond2.Parent = molecule2;

            model.AddMolecule(molecule1);
            molecule1.Parent = model;
            model.AddMolecule(molecule2);
            molecule2.Parent = model;

            model.Relabel(true);

            return model;
        }

        [Fact]
        public void CmlImportExportNested()
        {
            CMLConverter mc = new CMLConverter();
            Model model_1 = mc.Import(ResourceHelper.GetStringResource("NestedMolecules.xml"));

            // Basic Sanity Checks
            Assert.True(model_1.Molecules.Count == 1, $"Expected 1 Molecule; Got {model_1.Molecules.Count}");
            // Check molecule m0 has 4 child molecules and no atoms
            Molecule molecule_1 = model_1.Molecules.Values.First();
            Assert.True(molecule_1.Molecules.Count == 4, $"Expected 4 Molecule; Got {molecule_1.Molecules.Count}");
            Assert.True(molecule_1.Atoms.Count == 0, $"Expected 0 Atoms; Got {molecule_1.Atoms.Count}");
            // Check molecule m2 has no child molecules and 6 atoms
            molecule_1 = model_1.Molecules.Values.First().Molecules.Values.ToList()[1];
            Assert.True(molecule_1.Molecules.Count == 0, $"Expected 0 Molecule; Got {molecule_1.Molecules.Count}");
            Assert.True(molecule_1.Atoms.Count == 6, $"Expected 6 Atoms; Got {molecule_1.Atoms.Count}");
            // Check molecule m1 has 1 child molecules and no atoms
            molecule_1 = model_1.Molecules.Values.First().Molecules.Values.First();
            Assert.True(molecule_1.Molecules.Count == 1, $"Expected 1 Molecule; Got {molecule_1.Molecules.Count}");
            Assert.True(molecule_1.Atoms.Count == 0, $"Expected 0 Atoms; Got {molecule_1.Atoms.Count}");
            // Check molecule m5 has 1 child molecules and 6 atoms
            molecule_1 = model_1.Molecules.Values.First().Molecules.Values.First().Molecules.Values.First();
            Assert.True(molecule_1.Molecules.Count == 0, $"Expected 0 Molecule; Got {molecule_1.Molecules.Count}");
            Assert.True(molecule_1.Atoms.Count == 6, $"Expected 6 Atoms; Got {molecule_1.Atoms.Count}");

            var exported = mc.Export(model_1);
            Model model_2 = mc.Import(exported);

            // Basic Sanity Checks
            Assert.True(model_2.Molecules.Count == 1, $"Expected 1 Molecule; Got {model_2.Molecules.Count}");
            // Check molecule m0 has 4 child molecules and no atoms
            Molecule molecule_2 = model_2.Molecules.Values.First();
            Assert.True(molecule_2.Molecules.Count == 4, $"Expected 4 Molecule; Got {molecule_2.Molecules.Count}");
            Assert.True(molecule_2.Atoms.Count == 0, $"Expected 0 Atoms; Got {molecule_2.Atoms.Count}");
            // Check molecule m2 has no child molecules and 6 atoms
            molecule_2 = model_2.Molecules.Values.First().Molecules.Values.ToList()[1];
            Assert.True(molecule_2.Molecules.Count == 0, $"Expected 0 Molecule; Got {molecule_2.Molecules.Count}");
            Assert.True(molecule_2.Atoms.Count == 6, $"Expected 6 Atoms; Got {molecule_2.Atoms.Count}");
            // Check molecule m1 has 1 child molecules and no atoms
            molecule_2 = model_2.Molecules.Values.First().Molecules.Values.First();
            Assert.True(molecule_2.Molecules.Count == 1, $"Expected 1 Molecule; Got {molecule_2.Molecules.Count}");
            Assert.True(molecule_2.Atoms.Count == 0, $"Expected 0 Atoms; Got {molecule_2.Atoms.Count}");
            // Check molecule m5 has 1 child molecules and 6 atoms
            molecule_2 = model_2.Molecules.Values.First().Molecules.Values.First().Molecules.Values.First();
            Assert.True(molecule_2.Molecules.Count == 0, $"Expected 0 Molecule; Got {molecule_2.Molecules.Count}");
            Assert.True(molecule_2.Atoms.Count == 6, $"Expected 6 Atoms; Got {molecule_2.Atoms.Count}");
        }

        // SDFile and MOLFile import
        [Fact]
        public void SdfImportBenzene()
        {
            SdFileConverter mc = new SdFileConverter();
            Model m = mc.Import(ResourceHelper.GetStringResource("Benzene.txt"));

            // Basic sanity checks
            Assert.True(m.Molecules.Count == 1, $"Expected 1 Molecule; Got {m.Molecules.Count}");
            Assert.True(m.TotalAtomsCount == 6, $"Expected 6 Atoms; Got {m.TotalAtomsCount}");
            Assert.True(m.TotalBondsCount == 6, $"Expected 6 Bonds; Got {m.TotalBondsCount}");

            // Check that names and formulae have not been trashed
            Assert.True(m.Molecules.Values.First().Names.Count == 2, $"Expected 2 Chemical Names; Got {m.Molecules.Values.First().Names.Count}");
            Assert.True(m.Molecules.Values.First().Formulas.Count == 2, $"Expected 2 Formulae; Got {m.Molecules.Values.First().Formulas.Count }");

            // Check that we have one ring
            Assert.True(m.Molecules.Values.First().Rings.Count == 1, $"Expected 1 Ring; Got {m.Molecules.Values.First().Rings.Count}");
        }

        [Fact]
        public void SdfImportBasicParafuchsin()
        {
            SdFileConverter mc = new SdFileConverter();
            Model m = mc.Import(ResourceHelper.GetStringResource("BasicParafuchsin.txt"));

            // Basic sanity checks
            Assert.True(m.Molecules.Count == 1, $"Expected 1 Molecule; Got {m.Molecules.Count}");

            var mol = m.Molecules.Values.First();
            Assert.True(mol.Molecules.Count == 2, $"Expected 2 Child Molecules; Got {mol.Molecules.Count}");
            Assert.True(m.TotalAtomsCount == 41, $"Expected 41 Atoms; Got {m.TotalAtomsCount}");
            Assert.True(m.TotalBondsCount == 42, $"Expected 42 Bonds; Got {m.TotalBondsCount}");

            // Check that we got three rings
            var mol2 = mol.Molecules.Values.Skip(1).First();
            Assert.True(mol2.Rings.Count == 3, $"Expected 3 Rings; Got {mol2.Rings.Count}");
        }
    }
}