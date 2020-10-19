// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Chem4Word.ACME;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Helpers;
using Xunit;
using EVM = Chem4Word.ACME.EditViewModel;

namespace Chem4WordTests
{
    public class EditViewModel
    {
        [Fact]
        public void AddAtomChain_Creates_IsolatedAtom()
        {
            // Arrange
            var model = new Model();
            var editViewModel = new EVM(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EVM.AddAtomChain)}[AddAtom]",
                                    $"1 - {nameof(EVM.AddAtomChain)}[AddMolecule]",
                                    "0 - #start#"
                                };

            // Act
            editViewModel.AddAtomChain(null, new Point(0, 0), Globals.ClockDirections.Nothing);
            var undoStack1 = editViewModel.UndoManager.ReadUndoStack();
            editViewModel.UndoManager.Undo();
            editViewModel.UndoManager.Redo();
            var undoStack2 = editViewModel.UndoManager.ReadUndoStack();

            // Assert(ions)
            CheckUndoStack(expectedStack, undoStack1);
            CheckUndoStack(expectedStack, undoStack2);
            CheckMoleculeCount(model, 1);
            CheckAtomCount(model, 1);
        }

        [Fact]
        public void AddAtomChain_AddsBond_ToAtom()
        {
            // Arrange
            var model = new Model();
            var editViewModel = new EVM(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"2 - {nameof(EVM.AddNewBond)}",
                                    $"1 - {nameof(EVM.AddAtomChain)}[AddEndAtom]",
                                    "0 - #start#"
                                };

            var atom = new Atom
            {
                Position = new Point(0, 0),
                Element = Globals.PeriodicTable.C
            };

            var molecule = new Molecule();
            molecule.AddAtom(atom);
            atom.Parent = molecule;

            model.AddMolecule(molecule);
            molecule.Parent = model;

            // Act
            editViewModel.AddAtomChain(atom, new Point(5, 5), Globals.ClockDirections.Nothing);
            var undoStack1 = editViewModel.UndoManager.ReadUndoStack();
            editViewModel.UndoManager.Undo();
            editViewModel.UndoManager.Redo();
            var undoStack2 = editViewModel.UndoManager.ReadUndoStack();

            // Assert(ions)
            CheckUndoStack(expectedStack, undoStack1);
            CheckUndoStack(expectedStack, undoStack2);
            CheckMoleculeCount(model, 1);
            CheckAtomCount(model, 2);
            CheckBondCount(model, 1);
        }

        [Fact]
        public void AddHydrogens_ToSingleMolecule()
        {
            // Arrange
            var model = new Model();
            var editViewModel = new EVM(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EVM.AddHydrogens)}",
                                    "0 - #start#"
                                };

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

            var molecule = new Molecule();
            molecule.AddAtom(atom1);
            atom1.Parent = molecule;
            molecule.AddAtom(atom2);
            atom2.Parent = molecule;

            var bond = new Bond(atom1, atom2);
            bond.Order = Globals.OrderSingle;
            molecule.AddBond(bond);
            bond.Parent = molecule;

            model.AddMolecule(molecule);
            molecule.Parent = model;

            editViewModel.AddToSelection(bond);

            // Act
            editViewModel.AddHydrogens();
            var undoStack1 = editViewModel.UndoManager.ReadUndoStack();
            editViewModel.UndoManager.Undo();
            editViewModel.UndoManager.Redo();
            var undoStack2 = editViewModel.UndoManager.ReadUndoStack();

            // Assert(ions)
            CheckUndoStack(expectedStack, undoStack1);
            CheckUndoStack(expectedStack, undoStack2);
            CheckMoleculeCount(model, 1);
            CheckAtomCount(model, 8);
            CheckBondCount(model, 7);
        }

        [Fact]
        public void AddHydrogens_ToOneMolecule_OfTwoMolecules()
        {
            // Arrange
            var model = new Model();
            var editViewModel = new EVM(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EVM.AddHydrogens)}",
                                    "0 - #start#"
                                };

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
            var molecule2 = new Molecule();
            molecule2.AddAtom(atom2);
            atom2.Parent = molecule2;

            model.AddMolecule(molecule1);
            molecule1.Parent = model;
            model.AddMolecule(molecule2);
            molecule2.Parent = model;

            editViewModel.AddToSelection(molecule1);

            // Act
            editViewModel.AddHydrogens();
            var undoStack1 = editViewModel.UndoManager.ReadUndoStack();
            editViewModel.UndoManager.Undo();
            editViewModel.UndoManager.Redo();
            var undoStack2 = editViewModel.UndoManager.ReadUndoStack();

            // Assert(ions)
            CheckUndoStack(expectedStack, undoStack1);
            CheckUndoStack(expectedStack, undoStack2);
            CheckMoleculeCount(model, 2);
            CheckAtomCount(model, 6);
            CheckBondCount(model, 4);
        }

        [Fact]
        public void DeleteAtoms_RemovingSingleton_LeavesOtherMolecule()
        {
            // Arrange
            var model = new Model();
            var editViewModel = new EVM(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EVM.DeleteAtoms)}[Singleton]",
                                    "0 - #start#"
                                };

            var atom1 = new Atom
            {
                Position = new Point(0, 0),
                Element = Globals.PeriodicTable.C
            };

            var atom2 = new Atom
            {
                Position = new Point(0, 10),
                Element = Globals.PeriodicTable.C
            };

            var molecule1 = new Molecule();
            molecule1.AddAtom(atom1);
            atom1.Parent = molecule1;
            var molecule2 = new Molecule();
            molecule2.AddAtom(atom2);
            atom2.Parent = molecule2;

            model.AddMolecule(molecule1);
            molecule1.Parent = model;
            model.AddMolecule(molecule2);
            molecule2.Parent = model;

            var a1 = model.GetAllAtoms().First();

            // Act
            editViewModel.DeleteAtoms(new List<Atom> { a1 });
            var undoStack1 = editViewModel.UndoManager.ReadUndoStack();
            editViewModel.UndoManager.Undo();
            editViewModel.UndoManager.Redo();
            var undoStack2 = editViewModel.UndoManager.ReadUndoStack();

            // Assert(ions)
            CheckUndoStack(expectedStack, undoStack1);
            CheckUndoStack(expectedStack, undoStack2);
            CheckMoleculeCount(model, 1);
            CheckAtomCount(model, 1);
            CheckBondCount(model, 0);
        }

        [Fact]
        public void DeleteAtoms_FromDifferentMolecules_LeavesSameMolecules()
        {
            // Arrange
            var mc = new CMLConverter();
            var model = mc.Import(ResourceHelper.GetStringResource("Two-Molecules-With-Foliage.xml"));

            var editViewModel = new EVM(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EVM.DeleteAtomsAndBonds)}[MultipleFragments]",
                                    "0 - #start#"
                                };

            // Act
            var atoms = model.GetAllAtoms().Where(a => (Element)a.Element == Globals.PeriodicTable.H);
            editViewModel.DeleteAtoms(atoms);
            var undoStack1 = editViewModel.UndoManager.ReadUndoStack();
            editViewModel.UndoManager.Undo();
            editViewModel.UndoManager.Redo();
            var undoStack2 = editViewModel.UndoManager.ReadUndoStack();

            // Assert(ions)
            CheckUndoStack(expectedStack, undoStack1);
            CheckUndoStack(expectedStack, undoStack2);
            CheckMoleculeCount(model, 2);
            CheckAtomCount(model, 4);
            CheckBondCount(model, 2);
        }

        [Fact]
        public void DeleteAtoms_FromOneMolecule_LeavesSameMolecule()
        {
            // Arrange
            var mc = new CMLConverter();
            var model = mc.Import(ResourceHelper.GetStringResource("cyclohexylidenecyclohexane.xml"));

            var editViewModel = new EVM(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EVM.DeleteAtomsAndBonds)}[SingleAtom]",
                                    "0 - #start#"
                                };

            // Act
            var a11 = model.GetAllAtoms().First(a => a.Id == "a11");
            var a12 = model.GetAllAtoms().First(a => a.Id == "a12");
            editViewModel.DeleteAtoms(new List<Atom> { a11, a12 });
            var undoStack1 = editViewModel.UndoManager.ReadUndoStack();
            editViewModel.UndoManager.Undo();
            editViewModel.UndoManager.Redo();
            var undoStack2 = editViewModel.UndoManager.ReadUndoStack();

            // Assert(ions)
            CheckUndoStack(expectedStack, undoStack1);
            CheckUndoStack(expectedStack, undoStack2);
            CheckMoleculeCount(model, 1);
            CheckAtomCount(model, 10);
            CheckBondCount(model, 9);
        }

        [Fact]
        public void DeleteAtoms_FromOneMolecule_LeavesTwoMolecules()
        {
            // Arrange
            var mc = new CMLConverter();
            var model = mc.Import(ResourceHelper.GetStringResource("cyclohexylidenecyclohexane.xml"));

            var editViewModel = new EVM(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EVM.DeleteAtomsAndBonds)}[MultipleFragments]",
                                    "0 - #start#"
                                };

            // Act
            var a1 = model.GetAllAtoms().First(a => a.Id == "a1");
            editViewModel.DeleteAtoms(new List<Atom> { a1 });
            var undoStack1 = editViewModel.UndoManager.ReadUndoStack();
            editViewModel.UndoManager.Undo();
            editViewModel.UndoManager.Redo();
            var undoStack2 = editViewModel.UndoManager.ReadUndoStack();

            // Assert(ions)
            CheckUndoStack(expectedStack, undoStack1);
            CheckUndoStack(expectedStack, undoStack2);
            CheckMoleculeCount(model, 2);
            CheckAtomCount(model, 11);
            CheckBondCount(model, 10);
        }

        [Fact]
        public void DeleteBonds_Creates_TwoMolecules()
        {
            // Arrange
            var mc = new CMLConverter();
            var model = mc.Import(ResourceHelper.GetStringResource("cyclohexylidenecyclohexane.xml"));

            var editViewModel = new EVM(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EVM.DeleteAtomsAndBonds)}[MultipleFragments]",
                                    "0 - #start#"
                                };

            // Act
            // find double bond connecting the two rings
            var targetBond = model.GetAllBonds().First(b => b.Order == Globals.OrderDouble);
            editViewModel.DeleteBonds(new List<Bond> { targetBond });
            var undoStack1 = editViewModel.UndoManager.ReadUndoStack();
            editViewModel.UndoManager.Undo();
            editViewModel.UndoManager.Redo();
            var undoStack2 = editViewModel.UndoManager.ReadUndoStack();

            // Assert(ions)
            CheckUndoStack(expectedStack, undoStack1);
            CheckUndoStack(expectedStack, undoStack2);
            CheckMoleculeCount(model, 2);
            CheckAtomCount(model, 12);
            CheckBondCount(model, 12);
        }

        [Fact]
        public void DeleteMolecule_Leaves_OneMolecule()
        {
            // Arrange
            var mc = new CMLConverter();
            var model = mc.Import(ResourceHelper.GetStringResource("Two-Molecules-With-Foliage.xml"));

            var editViewModel = new EVM(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EVM.DeleteMolecule)}",
                                    "0 - #start#"
                                };

            // Act
            var molecule = model.Molecules.First().Value;
            editViewModel.DeleteMolecule(molecule);
            var undoStack1 = editViewModel.UndoManager.ReadUndoStack();
            editViewModel.UndoManager.Undo();
            editViewModel.UndoManager.Redo();
            var undoStack2 = editViewModel.UndoManager.ReadUndoStack();

            // Assert(ions)
            CheckUndoStack(expectedStack, undoStack1);
            CheckUndoStack(expectedStack, undoStack2);
            CheckMoleculeCount(model, 1);
            CheckAtomCount(model, 8);
            CheckBondCount(model, 7);
        }

        [Fact]
        public void DeleteSelection_Leaves_SingleAtom()
        {
            // Arrange
            var mc = new CMLConverter();
            var model = mc.Import(ResourceHelper.GetStringResource("Two-Molecules-For-Joining.xml"));

            var editViewModel = new EVM(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"2 - {nameof(EVM.DeleteAtomsAndBonds)}[SingleAtom]",
                                    $"3 - {nameof(EVM.DeleteMolecule)}",
                                    "0 - #start#"
                                };

            // Act
            var m1 = model.GetAllMolecules().First(m => m.Id == "m1");
            var a4 = model.GetAllAtoms().First(a => a.Id == "a4");
            var a5 = model.GetAllAtoms().First(a => a.Id == "a5");
            var b4 = model.GetAllBonds().First(b => b.Id == "b4");
            editViewModel.AddToSelection(new List<object> { m1, a4, a5, b4 });
            editViewModel.DeleteSelection();
            var undoStack1 = editViewModel.UndoManager.ReadUndoStack();
            editViewModel.UndoManager.Undo();
            editViewModel.UndoManager.Redo();
            var undoStack2 = editViewModel.UndoManager.ReadUndoStack();

            // Assert(ions)
            CheckUndoStack(expectedStack, undoStack1);
            CheckUndoStack(expectedStack, undoStack2);
            CheckMoleculeCount(model, 1);
            CheckAtomCount(model, 1);
            CheckBondCount(model, 0);
        }

        [Fact]
        public void DeleteSelection_AtomsBondAndMolecule_LeavesSingleMolecule()
        {
            // Arrange
            var mc = new CMLConverter();
            var model = mc.Import(ResourceHelper.GetStringResource("Two-Molecules-For-Joining.xml"));

            var editViewModel = new EVM(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"3 - {nameof(EVM.DeleteMolecule)}",
                                    "0 - #start#"
                                };

            // Act
            var m1 = model.GetAllMolecules().First(m => m.Id == "m1");

            editViewModel.AddToSelection(new List<object> { m1 });
            editViewModel.DeleteSelection();
            var undoStack1 = editViewModel.UndoManager.ReadUndoStack();
            editViewModel.UndoManager.Undo();
            editViewModel.UndoManager.Redo();
            var undoStack2 = editViewModel.UndoManager.ReadUndoStack();

            // Assert(ions)
            CheckUndoStack(expectedStack, undoStack1);
            CheckUndoStack(expectedStack, undoStack2);
            CheckMoleculeCount(model, 1);
            CheckAtomCount(model, 3);
            CheckBondCount(model, 3);
        }

        [Fact]
        public void DeleteSelection_AtomsAndBond_LeavesTwoMolecules()
        {
            // Arrange
            var mc = new CMLConverter();
            var model = mc.Import(ResourceHelper.GetStringResource("Two-Molecules-For-Joining.xml"));

            var editViewModel = new EVM(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"2 - {nameof(EVM.DeleteAtomsAndBonds)}[SingleAtom]",
                                    "0 - #start#"
                                };

            // Act
            var a4 = model.GetAllAtoms().First(a => a.Id == "a4");
            var a5 = model.GetAllAtoms().First(a => a.Id == "a5");
            var b4 = model.GetAllBonds().First(b => b.Id == "b4");
            editViewModel.AddToSelection(new List<object> { a4, a5, b4 });
            editViewModel.DeleteSelection();
            var undoStack1 = editViewModel.UndoManager.ReadUndoStack();
            editViewModel.UndoManager.Undo();
            editViewModel.UndoManager.Redo();
            var undoStack2 = editViewModel.UndoManager.ReadUndoStack();

            // Assert(ions)
            CheckUndoStack(expectedStack, undoStack1);
            CheckUndoStack(expectedStack, undoStack2);
            CheckMoleculeCount(model, 2);
            CheckAtomCount(model, 4);
            CheckBondCount(model, 3);
        }

        [Fact]
        public void DeleteSelection_Bond_LeavesTwoMoleculesB()
        {
            // Arrange
            var mc = new CMLConverter();
            var model = mc.Import(ResourceHelper.GetStringResource("Two-Molecules-For-Joining.xml"));

            var editViewModel = new EVM(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"2 - {nameof(EVM.DeleteAtomsAndBonds)}[SingleAtom]",
                                    "0 - #start#"
                                };

            // Act
            var b4 = model.GetAllBonds().First(b => b.Id == "b4");
            editViewModel.AddToSelection(new List<object> { b4 });
            editViewModel.DeleteSelection();
            var undoStack1 = editViewModel.UndoManager.ReadUndoStack();
            editViewModel.UndoManager.Undo();
            editViewModel.UndoManager.Redo();
            var undoStack2 = editViewModel.UndoManager.ReadUndoStack();

            // Assert(ions)
            CheckUndoStack(expectedStack, undoStack1);
            CheckUndoStack(expectedStack, undoStack2);
            CheckMoleculeCount(model, 2);
            CheckAtomCount(model, 6);
            CheckBondCount(model, 5);
        }

        [Fact]
        public void DoTransform_TranslateAtoms()
        {
            // Arrange
            var model = new Model();
            var editViewModel = new EVM(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EVM.DoTransform)}",
                                    "0 - #start#"
                                };

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

            var molecule = new Molecule();
            molecule.AddAtom(atom1);
            atom1.Parent = molecule;
            molecule.AddAtom(atom2);
            atom2.Parent = molecule;

            var bond = new Bond(atom1, atom2);
            bond.Order = Globals.OrderSingle;
            molecule.AddBond(bond);
            bond.Parent = molecule;

            model.AddMolecule(molecule);
            molecule.Parent = model;

            // Act
            var tt = new TranslateTransform(5, 5);
            editViewModel.DoTransform(tt, new List<Atom> { atom1, atom2 });

            var undoStack1 = editViewModel.UndoManager.ReadUndoStack();
            editViewModel.UndoManager.Undo();
            editViewModel.UndoManager.Redo();
            var undoStack2 = editViewModel.UndoManager.ReadUndoStack();

            // Assert(ions)
            CheckUndoStack(expectedStack, undoStack1);
            CheckUndoStack(expectedStack, undoStack2);

            Assert.Equal(5, atom1.Position.X);
            Assert.Equal(5, atom1.Position.Y);
            Assert.Equal(15, atom2.Position.X);
            Assert.Equal(15, atom2.Position.Y);
        }

        [Fact]
        public void DoTransform_TranslateMolecule()
        {
            // Arrange
            var model = new Model();
            var editViewModel = new EVM(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EVM.DoTransform)}",
                                    "0 - #start#"
                                };

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

            var molecule = new Molecule();
            molecule.AddAtom(atom1);
            atom1.Parent = molecule;
            molecule.AddAtom(atom2);
            atom2.Parent = molecule;

            var bond = new Bond(atom1, atom2);
            bond.Order = Globals.OrderSingle;
            molecule.AddBond(bond);
            bond.Parent = molecule;

            model.AddMolecule(molecule);
            molecule.Parent = model;

            // Act
            var tt = new TranslateTransform(5, 5);
            editViewModel.DoTransform(tt, new List<Molecule> { molecule });

            var undoStack1 = editViewModel.UndoManager.ReadUndoStack();
            editViewModel.UndoManager.Undo();
            editViewModel.UndoManager.Redo();
            var undoStack2 = editViewModel.UndoManager.ReadUndoStack();

            // Assert(ions)
            CheckUndoStack(expectedStack, undoStack1);
            CheckUndoStack(expectedStack, undoStack2);

            Assert.Equal(5, atom1.Position.X);
            Assert.Equal(5, atom1.Position.Y);
            Assert.Equal(15, atom2.Position.X);
            Assert.Equal(15, atom2.Position.Y);
        }

        [Fact]
        public void DrawChain_Creates_IsolatedChainOfBonds()
        {
            // Arrange
            var model = new Model();
            var editViewModel = new EVM(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"3 - {nameof(EVM.AddNewBond)}",
                                    $"2 - {nameof(EVM.AddAtomChain)}[AddEndAtom]",
                                    $"3 - {nameof(EVM.AddNewBond)}",
                                    $"2 - {nameof(EVM.AddAtomChain)}[AddEndAtom]",
                                    $"2 - {nameof(EVM.AddAtomChain)}[AddAtom]",
                                    $"2 - {nameof(EVM.AddAtomChain)}[AddMolecule]",
                                    "0 - #start#"
                                };

            var placements = new List<Point>
                             {
                                new Point(0,0),
                                new Point(5,5),
                                new Point(10,10)
                             };

            // Act
            editViewModel.DrawChain(placements, null);
            var undoStack1 = editViewModel.UndoManager.ReadUndoStack();
            editViewModel.UndoManager.Undo();
            editViewModel.UndoManager.Redo();
            var undoStack2 = editViewModel.UndoManager.ReadUndoStack();

            // Assert(ions)
            CheckUndoStack(expectedStack, undoStack1);
            CheckUndoStack(expectedStack, undoStack2);
            CheckMoleculeCount(model, 1);
            CheckAtomCount(model, 3);
            CheckBondCount(model, 2);
        }

        [Fact]
        public void DrawChain_AddsChainOfBonds_ToAtom()
        {
            // Arrange
            var model = new Model();
            var editViewModel = new EVM(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"3 - {nameof(EVM.AddNewBond)}",
                                    $"2 - {nameof(EVM.AddAtomChain)}[AddEndAtom]",
                                    $"3 - {nameof(EVM.AddNewBond)}",
                                    $"2 - {nameof(EVM.AddAtomChain)}[AddEndAtom]",
                                    "0 - #start#"
                                };

            var atom = new Atom
            {
                Position = new Point(0, 0),
                Element = Globals.PeriodicTable.C
            };

            var molecule = new Molecule();
            molecule.AddAtom(atom);
            atom.Parent = molecule;

            model.AddMolecule(molecule);
            molecule.Parent = model;

            var placements = new List<Point>
                             {
                                 atom.Position,
                                 new Point(5,5),
                                 new Point(10,10)
                             };

            // Act
            editViewModel.DrawChain(placements, atom);
            var undoStack1 = editViewModel.UndoManager.ReadUndoStack();
            editViewModel.UndoManager.Undo();
            editViewModel.UndoManager.Redo();
            var undoStack2 = editViewModel.UndoManager.ReadUndoStack();

            // Assert(ions)
            CheckUndoStack(expectedStack, undoStack1);
            CheckUndoStack(expectedStack, undoStack2);
            CheckMoleculeCount(model, 1);
            CheckAtomCount(model, 3);
            CheckBondCount(model, 2);
        }

        [Fact]
        public void DrawRing_Creates_IsolatedRing()
        {
            // Arrange
            var model = new Model();
            var editViewModel = new EVM(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EVM.DrawRing)}",
                                    $"2 - {nameof(EVM.SetBondAttributes)}",
                                    $"2 - {nameof(EVM.SetBondAttributes)}",
                                    $"2 - {nameof(EVM.SetBondAttributes)}",
                                    $"2 - {nameof(EVM.AddNewBond)}",
                                    $"3 - {nameof(EVM.AddNewBond)}",
                                    $"2 - {nameof(EVM.AddAtomChain)}[AddEndAtom]",
                                    $"3 - {nameof(EVM.AddNewBond)}",
                                    $"2 - {nameof(EVM.AddAtomChain)}[AddEndAtom]",
                                    $"3 - {nameof(EVM.AddNewBond)}",
                                    $"2 - {nameof(EVM.AddAtomChain)}[AddEndAtom]",
                                    $"3 - {nameof(EVM.AddNewBond)}",
                                    $"2 - {nameof(EVM.AddAtomChain)}[AddEndAtom]",
                                    $"3 - {nameof(EVM.AddNewBond)}",
                                    $"2 - {nameof(EVM.AddAtomChain)}[AddEndAtom]",
                                    $"2 - {nameof(EVM.AddAtomChain)}[AddAtom]",
                                    $"2 - {nameof(EVM.AddAtomChain)}[AddMolecule]",
                                    "0 - #start#"
                                };

            // Act
            var placements = new List<NewAtomPlacement>
                             {
                                 new NewAtomPlacement
                                 {
                                     Position = new Point(752.859,751)
                                 },
                                 new NewAtomPlacement
                                 {
                                     Position = new Point(752.859,711)
                                 },
                                 new NewAtomPlacement
                                 {
                                     Position = new Point(787.5,691)
                                 },
                                 new NewAtomPlacement
                                 {
                                     Position = new Point(822.141,711)
                                 },
                                 new NewAtomPlacement
                                 {
                                     Position = new Point(822.141,751)
                                 },
                                 new NewAtomPlacement
                                 {
                                     Position = new Point(787.5,771)
                                 }
                             };
            editViewModel.DrawRing(placements, true, 0);
            var undoStack1 = editViewModel.UndoManager.ReadUndoStack();
            editViewModel.UndoManager.Undo();
            editViewModel.UndoManager.Redo();
            var undoStack2 = editViewModel.UndoManager.ReadUndoStack();

            // Assert(ions)
            CheckUndoStack(expectedStack, undoStack1);
            CheckUndoStack(expectedStack, undoStack2);
            CheckMoleculeCount(model, 1);
            CheckAtomCount(model, 6);
            CheckBondCount(model, 6);
        }

        [Fact]
        public void Group_TwoMolecules_NowHaveSameParent()
        {
            // Arrange
            var mc = new CMLConverter();
            var model = mc.Import(ResourceHelper.GetStringResource("Two-Molecules-For-Joining.xml"));

            var editViewModel = new EVM(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EVM.Group)}",
                                    "0 - #start#"
                                };

            // find molecules m1 and m2
            var m1 = model.GetAllMolecules().First(a => a.Id == "m1");
            var m2 = model.GetAllMolecules().First(a => a.Id == "m2");

            // Act
            editViewModel.Group(new List<object> { m1, m2 });
            var undoStack1 = editViewModel.UndoManager.ReadUndoStack();
            editViewModel.UndoManager.Undo();
            editViewModel.UndoManager.Redo();
            var undoStack2 = editViewModel.UndoManager.ReadUndoStack();

            var m0 = model.GetAllMolecules().First();
            var m0Path = m0.Path;
            var m1Path = model.GetAllMolecules().First(a => a.Id == "m1").Path;
            var m2Path = model.GetAllMolecules().First(a => a.Id == "m2").Path;

            // Assert(ions)

            Assert.Equal($"{m0Path}/m1", m1Path);
            Assert.Equal($"{m0Path}/m2", m2Path);
            CheckUndoStack(expectedStack, undoStack1);
            CheckUndoStack(expectedStack, undoStack2);
            CheckMoleculeCount(model, 3);
            CheckAtomCount(model, 6);
            CheckBondCount(model, 6);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 2)]
        [InlineData(2, 3)]
        [InlineData(3, 1)]
        public void IncreaseBondOrder(int existingOrder, int newOrder)
        {
            // Arrange
            var model = new Model();
            var editViewModel = new EVM(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EVM.IncreaseBondOrder)}",
                                    "0 - #start#"
                                };

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

            var molecule = new Molecule();
            molecule.AddAtom(atom1);
            atom1.Parent = molecule;
            molecule.AddAtom(atom2);
            atom2.Parent = molecule;

            var bond = new Bond(atom1, atom2)
            {
                Id = "b1"
            };
            switch (existingOrder)
            {
                case 1:
                    bond.Order = Globals.OrderSingle;
                    break;

                case 2:
                    bond.Order = Globals.OrderDouble;
                    break;

                case 3:
                    bond.Order = Globals.OrderTriple;
                    break;

                default:
                    bond.Order = Globals.OrderZero;
                    break;
            }
            molecule.AddBond(bond);
            bond.Parent = molecule;

            model.AddMolecule(molecule);
            molecule.Parent = model;

            // Act
            editViewModel.IncreaseBondOrder(bond);

            var undoStack1 = editViewModel.UndoManager.ReadUndoStack();
            editViewModel.UndoManager.Undo();
            editViewModel.UndoManager.Redo();
            var undoStack2 = editViewModel.UndoManager.ReadUndoStack();

            // Assert(ions)
            CheckUndoStack(expectedStack, undoStack1);
            CheckUndoStack(expectedStack, undoStack2);

            Assert.Equal(newOrder, (int)bond.OrderValue.Value);
        }

        [Fact]
        public void JoinMolecules_TwoMolecules_BecomeOne()
        {
            // Arrange
            var mc = new CMLConverter();
            var model = mc.Import(ResourceHelper.GetStringResource("Two-Molecules-For-Joining.xml"));

            var editViewModel = new EVM(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EVM.JoinMolecules)}",
                                    "0 - #start#"
                                };

            // find atom a1 and a4
            var a1 = model.GetAllAtoms().First(a => a.Id == "a1");
            var a4 = model.GetAllAtoms().First(a => a.Id == "a4");

            // Act
            editViewModel.JoinMolecules(a1, a4, Globals.OrderDouble, Globals.BondStereo.None);
            var undoStack1 = editViewModel.UndoManager.ReadUndoStack();
            editViewModel.UndoManager.Undo();
            editViewModel.UndoManager.Redo();
            var undoStack2 = editViewModel.UndoManager.ReadUndoStack();

            // Assert(ions)
            CheckUndoStack(expectedStack, undoStack1);
            CheckUndoStack(expectedStack, undoStack2);
            CheckMoleculeCount(model, 1);
            CheckAtomCount(model, 6);
            CheckBondCount(model, 7);
        }

        [Fact]
        public void RemoveHydrogens_FromBothMolecules()
        {
            // Arrange
            var mc = new CMLConverter();
            var model = mc.Import(ResourceHelper.GetStringResource("Two-Molecules-With-Foliage.xml"));

            var editViewModel = new EVM(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EVM.RemoveHydrogens)}",
                                    "0 - #start#"
                                };

            // Act
            editViewModel.RemoveHydrogens();
            var undoStack1 = editViewModel.UndoManager.ReadUndoStack();
            editViewModel.UndoManager.Undo();
            editViewModel.UndoManager.Redo();
            var undoStack2 = editViewModel.UndoManager.ReadUndoStack();

            // Assert(ions)
            CheckUndoStack(expectedStack, undoStack1);
            CheckUndoStack(expectedStack, undoStack2);
            CheckMoleculeCount(model, 2);
            CheckAtomCount(model, 4);
            CheckBondCount(model, 2);
        }

        [Fact]
        public void RemoveHydrogens_FromOneMolecule()
        {
            // Arrange
            var mc = new CMLConverter();
            var model = mc.Import(ResourceHelper.GetStringResource("Two-Molecules-With-Foliage.xml"));

            var editViewModel = new EVM(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EVM.RemoveHydrogens)}",
                                    "0 - #start#"
                                };

            // Act
            var molecule = model.Molecules.First().Value;
            editViewModel.AddToSelection(molecule);

            editViewModel.RemoveHydrogens();
            var undoStack1 = editViewModel.UndoManager.ReadUndoStack();
            editViewModel.UndoManager.Undo();
            editViewModel.UndoManager.Redo();
            var undoStack2 = editViewModel.UndoManager.ReadUndoStack();

            // Assert(ions)
            CheckUndoStack(expectedStack, undoStack1);
            CheckUndoStack(expectedStack, undoStack2);
            CheckMoleculeCount(model, 2);
            CheckAtomCount(model, 10);
            CheckBondCount(model, 8);
        }

        [Fact]
        public void SetAverageBondLength_FromTen_ToTwenty()
        {
            // Arrange
            var model = new Model();
            var editViewModel = new EVM(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EVM.SetAverageBondLength)}",
                                    "0 - #start#"
                                };

            var atom1 = new Atom
            {
                Position = new Point(0, 0),
                Element = Globals.PeriodicTable.C
            };

            var atom2 = new Atom
            {
                Position = new Point(0, 10),
                Element = Globals.PeriodicTable.C
            };

            var molecule = new Molecule();
            molecule.AddAtom(atom1);
            atom1.Parent = molecule;
            molecule.AddAtom(atom2);
            atom2.Parent = molecule;

            var bond = new Bond(atom1, atom2);
            bond.Order = Globals.OrderSingle;
            molecule.AddBond(bond);
            bond.Parent = molecule;

            model.AddMolecule(molecule);
            molecule.Parent = model;
            var before = model.MeanBondLength;

            // Act
            editViewModel.SetAverageBondLength(20);

            var undoStack1 = editViewModel.UndoManager.ReadUndoStack();
            editViewModel.UndoManager.Undo();
            editViewModel.UndoManager.Redo();
            var undoStack2 = editViewModel.UndoManager.ReadUndoStack();
            var after = model.MeanBondLength;

            // Assert(ions)
            CheckUndoStack(expectedStack, undoStack1);
            CheckUndoStack(expectedStack, undoStack2);

            Assert.Equal(10, before);
            Assert.Equal(20, after);
        }

        [Fact]
        public void SwapBondDirection()
        {
            // Arrange
            var model = new Model();
            var editViewModel = new EVM(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EVM.SwapBondDirection)}",
                                    "0 - #start#"
                                };

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

            var molecule = new Molecule();
            molecule.AddAtom(atom1);
            atom1.Parent = molecule;
            molecule.AddAtom(atom2);
            atom2.Parent = molecule;

            var bond = new Bond(atom1, atom2)
            {
                Id = "b1",
                Order = Globals.OrderSingle
            };
            molecule.AddBond(bond);
            bond.Parent = molecule;

            model.AddMolecule(molecule);
            molecule.Parent = model;

            // Act
            editViewModel.SwapBondDirection(bond);

            var undoStack1 = editViewModel.UndoManager.ReadUndoStack();
            editViewModel.UndoManager.Undo();
            editViewModel.UndoManager.Redo();
            var undoStack2 = editViewModel.UndoManager.ReadUndoStack();

            // Assert(ions)
            CheckUndoStack(expectedStack, undoStack1);
            CheckUndoStack(expectedStack, undoStack2);

            Assert.Equal("a2", bond.StartAtom.Id);
            Assert.Equal("a1", bond.EndAtom.Id);
        }

        [Fact]
        public void UnGroup_Leaves_TwoMolecules()
        {
            // Arrange
            var mc = new CMLConverter();
            var model = mc.Import(ResourceHelper.GetStringResource("Two-Grouped-Molecules.xml"));

            var editViewModel = new EVM(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EVM.UnGroup)}",
                                    "0 - #start#"
                                };

            // find molecules m1
            var m1 = model.GetAllMolecules().First(a => a.Id == "m1");

            // Act
            editViewModel.UnGroup(new List<object> { m1 });
            var undoStack1 = editViewModel.UndoManager.ReadUndoStack();
            editViewModel.UndoManager.Undo();
            editViewModel.UndoManager.Redo();
            var undoStack2 = editViewModel.UndoManager.ReadUndoStack();

            var m2Path = model.GetAllMolecules().First(a => a.Id == "m2").Path;
            var m3Path = model.GetAllMolecules().First(a => a.Id == "m3").Path;

            // Assert(ions)

            Assert.Equal("/m2", m2Path);
            Assert.Equal("/m3", m3Path);
            CheckUndoStack(expectedStack, undoStack1);
            CheckUndoStack(expectedStack, undoStack2);
            CheckMoleculeCount(model, 2);
            CheckAtomCount(model, 8);
            CheckBondCount(model, 8);
        }

        [Fact]
        public void Customer_Fault_Scenario()
        {
            // Arrange
            var model = new Model();
            var editViewModel = new EVM(model);

            // Act
            // AddAtomChain
            editViewModel.AddAtomChain(null, new Point(872, 709.5), Globals.ClockDirections.Nothing, Globals.PeriodicTable.C);

            // DrawRing Isolated
            var ring1Points = new List<NewAtomPlacement>
                                  {
                                      new NewAtomPlacement
                                      {
                                          Position = new Point(752.859,751)
                                      },
                                      new NewAtomPlacement
                                      {
                                          Position = new Point(752.859,711)
                                      },
                                      new NewAtomPlacement
                                      {
                                          Position = new Point(787.5,691)
                                      },
                                      new NewAtomPlacement
                                      {
                                          Position = new Point(822.141,711)
                                      },
                                      new NewAtomPlacement
                                      {
                                          Position = new Point(822.141,751)
                                      },
                                      new NewAtomPlacement
                                      {
                                          Position = new Point(787.5,771)
                                      }
                                  };
            editViewModel.DrawRing(ring1Points, true, 0);

            // DrawRing Joined to 1st atom
            var ring2Points = new List<NewAtomPlacement>
                              {
                                  new NewAtomPlacement
                                  {
                                      Position = new Point(872,709.5),
                                      ExistingAtom = FindAtom(model, new Point(872,709.5))
                                  },
                                  new NewAtomPlacement
                                  {
                                      Position = new Point(837.359,689.5)
                                  },
                                  new NewAtomPlacement
                                  {
                                      Position = new Point(837.359,649.5)
                                  },
                                  new NewAtomPlacement
                                  {
                                      Position = new Point(872,629.5)
                                  },
                                  new NewAtomPlacement
                                  {
                                      Position = new Point(906.641,649.5)
                                  },
                                  new NewAtomPlacement
                                  {
                                      Position = new Point(906.641,689.5)
                                  }
                              };
            editViewModel.DrawRing(ring2Points, true, 0);

            // DrawRing joined to bond
            var ring3Points = new List<NewAtomPlacement>
                              {
                                  new NewAtomPlacement
                                  {
                                      Position = new Point(906.641,649.5),
                                      ExistingAtom = FindAtom(model, new Point(906.641,649.5))
                                  },
                                  new NewAtomPlacement
                                  {
                                      Position = new Point(941.282,629.5)
                                  },
                                  new NewAtomPlacement
                                  {
                                      Position = new Point(975.923,649.5)
                                  },
                                  new NewAtomPlacement
                                  {
                                      Position = new Point(975.923,689.5)
                                  },
                                  new NewAtomPlacement
                                  {
                                      Position = new Point(941.282,709.5)
                                  },
                                  new NewAtomPlacement
                                  {
                                      Position = new Point(906.641,689.5),
                                      ExistingAtom = FindAtom(model, new Point(906.641,689.5))
                                  }
                              };
            editViewModel.DrawRing(ring3Points, true, 0);

            // Checks before Undo(s)
            CheckMoleculeCount(model, 2);
            CheckAtomCount(model, 16);
            CheckBondCount(model, 17);

            editViewModel.UndoManager.Undo();
            editViewModel.UndoManager.Undo();
            editViewModel.UndoManager.Undo();

            // Assert(ions)
            CheckMoleculeCount(model, 1);
            CheckAtomCount(model, 1);
        }

        #region Helpers

        private Atom FindAtom(Model model, Point point)
        {
            var atoms = model.GetAllAtoms();
            var atom = atoms.FirstOrDefault(a => a.Position == point);
            return atom;
        }

        private void CheckMoleculeCount(Model model, int expected)
        {
            var actual = model.GetAllMolecules().Count;
            Assert.True(actual == expected, $"Expected Molecules.Count = {expected}, Got {actual}");
        }

        private void CheckAtomCount(Model model, int expected)
        {
            var actual = model.GetAllAtoms().Count;
            Assert.True(actual == expected, $"Expected Atoms.Count = {expected}, Got {actual}");
        }

        private void CheckBondCount(Model model, int expected)
        {
            var actual = model.GetAllBonds().Count;
            Assert.True(actual == expected, $"Expected Bonds.Count = {expected}, Got {actual}");
        }

        private void CheckUndoStack(List<string> expected, List<string> actual)
        {
            Assert.True(expected.Count == actual.Count, $"Records - Expected {expected.Count} Got {actual.Count}");
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.Equal(expected[i], actual[i]);
            }
        }

        #endregion Helpers
    }
}