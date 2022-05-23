// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Helpers;
using Xunit;

namespace Chem4WordTests
{
    public class GeneralTests
    {
        [Fact]
        public void CheckClone()
        {
            Model model = new Model();

            Molecule molecule = new Molecule();
            molecule.Id = "m1";
            model.AddMolecule(molecule);
            molecule.Parent = model;

            Atom startAtom = new Atom();
            startAtom.Id = "a1";
            startAtom.Element = Globals.PeriodicTable.C;
            startAtom.Position = new Point(5, 5);
            molecule.AddAtom(startAtom);
            startAtom.Parent = molecule;

            Atom endAtom = new Atom();
            endAtom.Id = "a2";
            endAtom.Element = Globals.PeriodicTable.C;
            endAtom.Position = new Point(10, 10);
            molecule.AddAtom(endAtom);
            endAtom.Parent = molecule;

            Bond bond = new Bond(startAtom, endAtom);
            bond.Id = "b1";
            bond.Order = Globals.OrderSingle;
            molecule.AddBond(bond);
            bond.Parent = molecule;

            Assert.True(model.Molecules.Count == 1, $"Expected 1 Molecule; Got {model.Molecules.Count}");

            var a1 = model.Molecules.Values.First().Atoms.Values.First();
            Assert.True(Math.Abs(a1.Position.X - 5.0) < 0.001, $"Expected a1.X = 5; Got {a1.Position.X}");
            Assert.True(Math.Abs(a1.Position.Y - 5.0) < 0.001, $"Expected a1.Y = 5; Got {a1.Position.Y}");

            Model clone = model.Copy();

            Assert.True(model.Molecules.Count == 1, $"Expected 1 Molecule; Got {model.Molecules.Count}");
            Assert.True(clone.Molecules.Count == 1, $"Expected 1 Molecule; Got {clone.Molecules.Count}");

            var a2 = clone.Molecules.Values.First().Atoms.Values.First();
            Assert.True(Math.Abs(a2.Position.X - 5.0) < 0.001, $"Expected a2.X = 5; Got {a2.Position.X}");
            Assert.True(Math.Abs(a2.Position.Y - 5.0) < 0.001, $"Expected a2.Y = 5; Got {a2.Position.Y}");

            clone.ScaleToAverageBondLength(5);

            var a3 = model.Molecules.Values.First().Atoms.Values.First();
            Assert.True(Math.Abs(a3.Position.X - 5.0) < 0.001, $"Expected a3.X = 5; Got {a3.Position.X}");
            Assert.True(Math.Abs(a3.Position.Y - 5.0) < 0.001, $"Expected a3.Y = 5; Got {a3.Position.Y}");

            var a4 = clone.Molecules.Values.First().Atoms.Values.First();
            Assert.True(Math.Abs(a4.Position.X - 3.535) < 0.001, $"Expected a4.X = 3.535; Got {a4.Position.X}");
            Assert.True(Math.Abs(a4.Position.Y - 3.535) < 0.001, $"Expected a4.Y = 3.535; Got {a4.Position.Y}");
        }

        [Theory]
        [InlineData("O", 0, false)]
        [InlineData("S", 1, true)]
        [InlineData("N", 2, true)]
        public void CheckAtomRings(string element, int ringCount, bool isInRing)
        {
            CMLConverter mc = new CMLConverter();
            Model m = mc.Import(ResourceHelper.GetStringResource("Two-Rings.xml"));

            // Basic sanity checks
            Assert.True(m.Molecules.Count == 1, $"Expected 1 Molecule; Got {m.Molecules.Count}");
            var molecule = m.Molecules.Values.First();
            Assert.True(molecule.Rings.Count == 2, $"Expected 2 Rings; Got {molecule.Rings.Count}");

            // Get atom to test
            var atoms = molecule.Atoms.Values.Where(a => a.SymbolText == element).ToList();
            Assert.True(atoms.Count == 1, "Expected only one atom");
            Atom atom = atoms.FirstOrDefault();
            Assert.NotNull(atom);

            Assert.True(atom.RingCount == ringCount, $"Expected RingCount: {ringCount}; Got {atom.RingCount}");
            Assert.True(atom.IsInRing == isInRing, $"Expected IsInRing: {isInRing}; Got {atom.IsInRing}");
        }

        [Fact]
        public void CheckFormulasCanBeAdded()
        {
            Model model = new Model();

            Molecule molecule = new Molecule();
            molecule.Id = "m1";
            model.AddMolecule(molecule);
            molecule.Parent = model;

            var formula = new TextualProperty();
            formula.FullType = "";
            formula.Value = "";
            molecule.Formulas.Add(formula);

            Assert.True(molecule.Formulas.Count == 1, "Expected count to be 1");
            Assert.Equal("", molecule.Formulas[0].FullType);

            molecule.Formulas[0].FullType = "convention";
            Assert.Equal("convention", molecule.Formulas[0].FullType);
        }

        [Fact]
        public void CheckNamesCanBeAdded()
        {
            Model model = new Model();

            Molecule molecule = new Molecule();
            molecule.Id = "m1";
            model.AddMolecule(molecule);
            molecule.Parent = model;

            var name = new TextualProperty();
            name.FullType = "";
            name.Value = "";
            molecule.Names.Add(name);

            Assert.True(molecule.Names.Count == 1, "Expected count to be 1");
            Assert.Equal("", molecule.Names[0].FullType);

            molecule.Names[0].FullType = "dictref";
            Assert.Equal("dictref", molecule.Names[0].FullType);
        }

        [Theory]
        [InlineData(0, 0, 0, "C 2 H 5 F 1")]
        [InlineData(1, 0, 0, "C 2 H 4 F 1 +")]
        [InlineData(0, -1, 0, "C 2 H 4 F 1 -")]
        [InlineData(1, 0, -2, "C 2 H 4 F 1 -")]
        [InlineData(1, 1, 0, "C 2 H 3 F 1 + 2")]
        [InlineData(-1, -1, 0, "C 2 H 3 F 1 - 2")]
        [InlineData(1, 1, -1, "C 2 H 3 F 1 +")]
        [InlineData(1, 1, -2, "C 2 H 3 F 1")]
        public void CheckCalculatedFormula(int a1Charge, int a2Charge, int m1Charge, string expected)
        {
            var model = CreateSimpleMolecule();
            var a1 = model.GetAllAtoms().First(a => a.Id.Equals("a1")); // C
            var a2 = model.GetAllAtoms().First(a => a.Id.Equals("a2")); // F
            var m1 = model.GetAllMolecules().First(m => m.Id.Equals("m1"));

            if (a1Charge != 0)
            {
                a1.FormalCharge = a1Charge;
            }
            if (a2Charge != 0)
            {
                a2.FormalCharge = a2Charge;
            }
            if (m1Charge != 0)
            {
                m1.FormalCharge = m1Charge;
            }

            Assert.Equal(expected, model.ConciseFormula);
        }

        [Theory]
        [InlineData(0, 0, 0, 0, 0, 0, "[C 2 H 6 · H 1 F 1]")]
        [InlineData(1, 0, 0, 0, 0, 0, "[C 2 H 5 + · H 1 F 1]")]
        [InlineData(1, 1, 0, 0, 0, 0, "[C 2 H 4 + 2 · H 1 F 1]")]
        [InlineData(1, 1, 1, 0, 0, 0, "[C 2 H 4 + 2 · H 2 F 1 +]")]
        [InlineData(0, -1, 0, 0, 0, 0, "[C 2 H 5 - · H 1 F 1]")]
        [InlineData(0, 0, 0, 1, 0, 0, "[C 2 H 6 · H 1 F 1] + 1")]
        [InlineData(0, 0, 0, 0, 2, 0, "[C 2 H 6 + 2 · H 1 F 1]")]
        [InlineData(0, 0, 0, 0, 0, -1, "[C 2 H 6 · H 1 F 1 -]")]
        public void CheckCalculatedFormulaNested(int a1Charge, int a2Charge, int a3Charge, int m1Charge, int m2Charge, int m3Charge, string expected)
        {
            var model = CreateNestedMolecule();

            var a1 = model.GetAllAtoms().First(a => a.Id.Equals("a1"));     // C
            var a2 = model.GetAllAtoms().First(a => a.Id.Equals("a2"));     // C
            var a3 = model.GetAllAtoms().First(a => a.Id.Equals("a3"));     // F
            var m1 = model.GetAllMolecules().First(m => m.Id.Equals("m1")); // Parent
            var m2 = model.GetAllMolecules().First(m => m.Id.Equals("m2")); // C-C child
            var m3 = model.GetAllMolecules().First(m => m.Id.Equals("m3")); // F child

            if (a1Charge != 0)
            {
                a1.FormalCharge = a1Charge;
            }
            if (a2Charge != 0)
            {
                a2.FormalCharge = a2Charge;
            }
            if (a3Charge != 0)
            {
                a3.FormalCharge = a3Charge;
            }
            if (m1Charge != 0)
            {
                m1.FormalCharge = m1Charge;
            }
            if (m2Charge != 0)
            {
                m2.FormalCharge = m2Charge;
            }
            if (m3Charge != 0)
            {
                m3.FormalCharge = m3Charge;
            }

            Assert.Equal(expected, model.ConciseFormula);
        }

        [Fact]
        public void CheckCalculatedFormulaDoubleNested()
        {
            var model = CreateDoubleNestedMolecule();

            var expected = "[[ H 3 P 1  ·  H 2 O 1 ] · [ H 3 N 1  ·  Y 1 ]]";

            Debug.WriteLine(model.ConciseFormula);
            Assert.Equal(expected, model.ConciseFormula);
        }

        [Theory]
        // Invalid strings
        [InlineData("2", 0)]
        [InlineData("Q", 0)]
        [InlineData("Not found", 0)]
        [InlineData("Any Old Rubbish.", 0)]
        [InlineData("[ . ]", 0)]
        [InlineData("Any - Old + Rubbish!", 0)]
        [InlineData("55Any+ 999Old- -Rubbish", 0)]
        // Valid strings
        [InlineData("C  6  H  6", 2)]
        [InlineData("C7H6N", 3)]
        [InlineData("C7 H7 F1", 3)]
        [InlineData("C 5 H 5 Y 1 + 2", 4)]
        [InlineData("C28H31N2O3+", 5)]
        [InlineData("C6H10O12P2-4", 5)]
        [InlineData("C20H21N7O7-2", 5)]
        [InlineData("C57H101O18S3-3", 5)]
        [InlineData("C19H26NO4+", 5)]
        [InlineData("C15H22N2O17P2-2", 6)]
        [InlineData("C 6 H 6 · C7 H7 F1", 6)]
        [InlineData("2 C 6 H 6 · C 7 H 7", 6)]
        [InlineData("C14H15BrClNO6", 6)]
        [InlineData("[C2 H6 · H1 F1] + 1", 8)]
        [InlineData("[C2H6 · HF]+", 8)]
        [InlineData("C 5 H 5 P 1 - · C 5 H 5 N 1 - 2 · C 5 H 5 O 1 + · C 5 H 5 Y 1 + 2", 19)]
        public void ParseFormula(string formula, int count)
        {
            var listOfParts = FormulaHelper.ParseFormulaIntoParts(formula);

            //var i = 1;
            //Debug.WriteLine(formula);
            //foreach (var part in listOfParts)
            //{
            //    Debug.WriteLine($"    #{i++} {part.PartType} {part.Text} {part.Count}");
            //}

            Assert.Equal(count, listOfParts.Count);
        }

        [Theory]
        // Single Valent Element
        [InlineData("O", 0, 0, 2, false)]
        [InlineData("O", 0, 1, 1, false)]
        [InlineData("O", 0, 2, 0, false)]
        [InlineData("O", 1, 2, 1, false)]
        [InlineData("O", 1, 3, 0, false)]
        [InlineData("O", 0, 3, 0, true)]
        // Multi Valent Element: Valencies 1,3,5,7
        [InlineData("Cl", 0, 0, 1, false)]
        [InlineData("Cl", 0, 1, 0, false)]
        [InlineData("Cl", 0, 2, 1, false)]
        // Multi Valent Element: Valencies 3,5
        [InlineData("N", 0, 0, 3, false)]
        [InlineData("N", 0, 1, 2, false)]
        [InlineData("N", 0, 5, 0, false)]
        [InlineData("N", 0, 6, 0, true)]
        public void CheckValencyCalculations(string element, int charge, int bonds,
                      int expectedImplicitHCount, bool expectedOverbonding)
        {
            // Arrange
            var model = new Model();

            var molecule = new Molecule();
            model.AddMolecule(molecule);
            molecule.Parent = model;

            var atom = new Atom();
            molecule.AddAtom(atom);
            atom.Parent = molecule;

            atom.Element = Globals.PeriodicTable.Elements[element];
            atom.FormalCharge = charge;

            AddHBonds(molecule, atom, bonds);

            var bondOrders = (int)Math.Truncate(atom.BondOrders);

            // Act
            var implicitHydrogen = atom.ImplicitHydrogenCount;
            var over = atom.Overbonded;

            // Assert
            Assert.Equal(bondOrders, bonds);
            Assert.Equal(expectedImplicitHCount, implicitHydrogen);
            Assert.Equal(expectedOverbonding, over);
        }

        #region Support functions

        private Model CreateDoubleNestedMolecule()
        {
            var model = new Model();

            var atom1 = new Atom
            {
                Id = "a1",
                Position = new Point(0, 0),
                Element = Globals.PeriodicTable.P
            };

            var atom2 = new Atom
            {
                Id = "a2",
                Position = new Point(10, 10),
                Element = Globals.PeriodicTable.O
            };

            var atom3 = new Atom
            {
                Id = "a3",
                Position = new Point(20, 20),
                Element = Globals.PeriodicTable.N
            };

            var atom4 = new Atom
            {
                Id = "a4",
                Position = new Point(30, 30),
                Element = Globals.PeriodicTable.Y
            };

            var molecule1 = new Molecule
            {
                Id = "m1"
            };
            var molecule2 = new Molecule
            {
                Id = "m2"
            };
            var molecule3 = new Molecule
            {
                Id = "m3"
            };
            var molecule4 = new Molecule
            {
                Id = "m4"
            };
            var molecule5 = new Molecule
            {
                Id = "m5"
            };
            var molecule6 = new Molecule
            {
                Id = "m6"
            };
            var molecule7 = new Molecule
            {
                Id = "m7"
            };

            molecule3.AddAtom(atom1);
            atom1.Parent = molecule3;

            molecule4.AddAtom(atom2);
            atom2.Parent = molecule4;

            molecule6.AddAtom(atom3);
            atom3.Parent = molecule6;

            molecule7.AddAtom(atom4);
            atom4.Parent = molecule7;

            molecule2.AddMolecule(molecule3);
            molecule3.Parent = molecule2;
            molecule2.AddMolecule(molecule4);
            molecule4.Parent = molecule2;

            molecule5.AddMolecule(molecule6);
            molecule6.Parent = molecule5;
            molecule5.AddMolecule(molecule7);
            molecule7.Parent = molecule5;

            molecule1.AddMolecule(molecule2);
            molecule2.Parent = molecule1;
            molecule1.AddMolecule(molecule5);
            molecule5.Parent = molecule1;

            model.AddMolecule(molecule1);
            molecule1.Parent = model;

            return model;
        }

        private Model CreateNestedMolecule()
        {
            var model = new Model();

            var atom1 = new Atom
            {
                Id = "a1",
                Position = new Point(0, 0),
                Element = Globals.PeriodicTable.C
            };

            var atom2 = new Atom
            {
                Id = "a2",
                Position = new Point(10, 10),
                Element = Globals.PeriodicTable.C
            };

            var atom3 = new Atom
            {
                Id = "a3",
                Position = new Point(20, 20),
                Element = Globals.PeriodicTable.F
            };

            var molecule1 = new Molecule
            {
                Id = "m1"
            };
            var molecule2 = new Molecule
            {
                Id = "m2"
            };
            var molecule3 = new Molecule
            {
                Id = "m3"
            };

            molecule2.AddAtom(atom1);
            atom1.Parent = molecule2;
            molecule2.AddAtom(atom2);
            atom2.Parent = molecule2;

            molecule3.AddAtom(atom3);
            atom3.Parent = molecule3;

            var bond1 = new Bond(atom1, atom2)
            {
                Id = "b1",
                Order = Globals.OrderSingle
            };
            molecule2.AddBond(bond1);
            bond1.Parent = molecule2;

            model.AddMolecule(molecule1);
            molecule1.Parent = model;

            molecule1.AddMolecule(molecule2);
            molecule2.Parent = molecule1;

            molecule1.AddMolecule(molecule3);
            molecule3.Parent = molecule1;

            return model;
        }

        private Model CreateSimpleMolecule()
        {
            var model = new Model();

            var atom1 = new Atom
            {
                Id = "a1",
                Position = new Point(0, 0),
                Element = Globals.PeriodicTable.C
            };

            var atom2 = new Atom
            {
                Id = "a2",
                Position = new Point(10, 10),
                Element = Globals.PeriodicTable.C
            };

            var atom3 = new Atom
            {
                Id = "a3",
                Position = new Point(20, 20),
                Element = Globals.PeriodicTable.F
            };

            var molecule = new Molecule
            {
                Id = "m1"
            };
            molecule.AddAtom(atom1);
            atom1.Parent = molecule;
            molecule.AddAtom(atom2);
            atom2.Parent = molecule;
            molecule.AddAtom(atom3);
            atom3.Parent = molecule;

            var bond1 = new Bond(atom1, atom2)
            {
                Id = "b1",
                Order = Globals.OrderSingle
            };
            molecule.AddBond(bond1);
            bond1.Parent = molecule;
            var bond2 = new Bond(atom2, atom3)
            {
                Id = "b2",
                Order = Globals.OrderSingle
            };
            molecule.AddBond(bond2);
            bond2.Parent = molecule;

            model.AddMolecule(molecule);
            molecule.Parent = model;

            return model;
        }

        private void AddHBonds(Molecule molecule, Atom atom, int bonds)
        {
            for (int i = 0; i < bonds; i++)
            {
                Atom h = new Atom();
                h.Element = Globals.PeriodicTable.H;

                molecule.AddAtom(h);
                h.Parent = molecule;

                Bond bond = new Bond(atom, h);
                bond.Order = "S";
                molecule.AddBond(bond);
                bond.Parent = molecule;
            }
        }

        #endregion Support functions
    }
}