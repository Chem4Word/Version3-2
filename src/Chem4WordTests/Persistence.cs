// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Linq;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Converters.MDL;
using Xunit;

namespace Chem4WordTests
{
    public class Persistence
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
        [InlineData("Benzene.xml", 1, 6, 6, 1, 1, 3, 2)]
        [InlineData("Testosterone.xml", 1, 25, 28, 4, 4, 4, 2)]
        [InlineData("Phthalocyanine.xml", 1, 58, 66, 9, 8, 2, 3)]
        [InlineData("CopperPhthalocyanine.xml", 1, 57, 68, 12, 12, 1, 0)]
        public void CmlImport(string file, int molecules, int atoms, int bonds, int allRings, int placementRings, int names, int formulas)
        {
            CMLConverter mc = new CMLConverter();
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

        [Fact]
        public void CmlImportNested()
        {
            CMLConverter mc = new CMLConverter();
            Model model = mc.Import(ResourceHelper.GetStringResource("NestedMolecules.xml"));

            // Basic Sanity Checks
            Assert.True(model.Molecules.Count == 1, $"Expected 1 Molecule; Got {model.Molecules.Count}");
            // Check molecule m0 has 4 child molecules and no atoms
            Molecule molecule = model.Molecules.Values.First();
            Assert.True(molecule.Molecules.Count == 4, $"Expected 4 Molecule; Got {molecule.Molecules.Count}");
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