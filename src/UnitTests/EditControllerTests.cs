// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME;
using Chem4Word.ACME.Models;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Enums;
using Chem4Word.Model2.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Xunit;

namespace Chem4WordTests
{
    public class EditControllerTests
    {
        [Fact]
        public void AddAtomChain_Creates_IsolatedAtom()
        {
            // Arrange
            var model = new Model();
            var editController = new EditController(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EditController.AddAtomChain)}[AddAtom]",
                                    $"1 - {nameof(EditController.AddAtomChain)}[AddMolecule]",
                                    "0 - #start#"
                                };

            // Act
            editController.AddAtomChain(null, new Point(0, 0), ClockDirections.Nothing);
            var undoStack1 = editController.UndoManager.ReadUndoStack();
            editController.UndoManager.Undo();
            editController.UndoManager.Redo();
            var undoStack2 = editController.UndoManager.ReadUndoStack();

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
            var editController = new EditController(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"2 - {nameof(EditController.AddNewBond)}",
                                    $"1 - {nameof(EditController.AddAtomChain)}[AddEndAtom]",
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
            editController.AddAtomChain(atom, new Point(5, 5), ClockDirections.Nothing);
            var undoStack1 = editController.UndoManager.ReadUndoStack();
            editController.UndoManager.Undo();
            editController.UndoManager.Redo();
            var undoStack2 = editController.UndoManager.ReadUndoStack();

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
            var editController = new EditController(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EditController.AddHydrogens)}",
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

            editController.AddToSelection(bond);

            // Act
            editController.AddHydrogens();
            var undoStack1 = editController.UndoManager.ReadUndoStack();
            editController.UndoManager.Undo();
            editController.UndoManager.Redo();
            var undoStack2 = editController.UndoManager.ReadUndoStack();

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
            var editController = new EditController(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EditController.AddHydrogens)}",
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

            editController.AddToSelection(molecule1);

            // Act
            editController.AddHydrogens();
            var undoStack1 = editController.UndoManager.ReadUndoStack();
            editController.UndoManager.Undo();
            editController.UndoManager.Redo();
            var undoStack2 = editController.UndoManager.ReadUndoStack();

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
            var editController = new EditController(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EditController.DeleteAtoms)}[Singleton]",
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
            editController.DeleteAtoms(new List<Atom> { a1 });
            var undoStack1 = editController.UndoManager.ReadUndoStack();
            editController.UndoManager.Undo();
            editController.UndoManager.Redo();
            var undoStack2 = editController.UndoManager.ReadUndoStack();

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

            var editController = new EditController(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EditController.DeleteAtomsAndBonds)}[MultipleFragments]",
                                    "0 - #start#"
                                };

            // Act
            var atoms = model.GetAllAtoms().Where(a => (Element)a.Element == Globals.PeriodicTable.H);
            editController.DeleteAtoms(atoms);
            var undoStack1 = editController.UndoManager.ReadUndoStack();
            editController.UndoManager.Undo();
            editController.UndoManager.Redo();
            var undoStack2 = editController.UndoManager.ReadUndoStack();

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

            var editController = new EditController(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EditController.DeleteAtomsAndBonds)}[SingleAtom]",
                                    "0 - #start#"
                                };

            // Act
            var a11 = model.GetAllAtoms().First(a => a.Id == "a11");
            var a12 = model.GetAllAtoms().First(a => a.Id == "a12");
            editController.DeleteAtoms(new List<Atom> { a11, a12 });
            var undoStack1 = editController.UndoManager.ReadUndoStack();
            editController.UndoManager.Undo();
            editController.UndoManager.Redo();
            var undoStack2 = editController.UndoManager.ReadUndoStack();

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

            var editController = new EditController(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EditController.DeleteAtomsAndBonds)}[MultipleFragments]",
                                    "0 - #start#"
                                };

            // Act
            var a1 = model.GetAllAtoms().First(a => a.Id == "a1");
            editController.DeleteAtoms(new List<Atom> { a1 });
            var undoStack1 = editController.UndoManager.ReadUndoStack();
            editController.UndoManager.Undo();
            editController.UndoManager.Redo();
            var undoStack2 = editController.UndoManager.ReadUndoStack();

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

            var editController = new EditController(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EditController.DeleteAtomsAndBonds)}[MultipleFragments]",
                                    "0 - #start#"
                                };

            // Act
            // find double bond connecting the two rings
            var targetBond = model.GetAllBonds().First(b => b.Order == Globals.OrderDouble);
            editController.DeleteBonds(new List<Bond> { targetBond });
            var undoStack1 = editController.UndoManager.ReadUndoStack();
            editController.UndoManager.Undo();
            editController.UndoManager.Redo();
            var undoStack2 = editController.UndoManager.ReadUndoStack();

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

            var editController = new EditController(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EditController.DeleteMolecule)}",
                                    "0 - #start#"
                                };

            // Act
            var molecule = model.Molecules.First().Value;
            editController.DeleteMolecule(molecule);
            var undoStack1 = editController.UndoManager.ReadUndoStack();
            editController.UndoManager.Undo();
            editController.UndoManager.Redo();
            var undoStack2 = editController.UndoManager.ReadUndoStack();

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

            var editController = new EditController(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"2 - {nameof(EditController.DeleteAtomsAndBonds)}[SingleAtom]",
                                    $"3 - {nameof(EditController.DeleteMolecule)}",
                                    "0 - #start#"
                                };

            // Act
            var m1 = model.GetAllMolecules().First(m => m.Id == "m1");
            var a4 = model.GetAllAtoms().First(a => a.Id == "a4");
            var a5 = model.GetAllAtoms().First(a => a.Id == "a5");
            var b4 = model.GetAllBonds().First(b => b.Id == "b4");
            editController.AddObjectListToSelection(new List<BaseObject> { m1, a4, a5, b4 });
            editController.DeleteSelection();
            var undoStack1 = editController.UndoManager.ReadUndoStack();
            editController.UndoManager.Undo();
            editController.UndoManager.Redo();
            var undoStack2 = editController.UndoManager.ReadUndoStack();

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

            var editController = new EditController(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"3 - {nameof(EditController.DeleteMolecule)}",
                                    "0 - #start#"
                                };

            // Act
            var m1 = model.GetAllMolecules().First(m => m.Id == "m1");

            editController.AddObjectListToSelection(new List<BaseObject> { m1 });
            editController.DeleteSelection();
            var undoStack1 = editController.UndoManager.ReadUndoStack();
            editController.UndoManager.Undo();
            editController.UndoManager.Redo();
            var undoStack2 = editController.UndoManager.ReadUndoStack();

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

            var editController = new EditController(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"2 - {nameof(EditController.DeleteAtomsAndBonds)}[SingleAtom]",
                                    "0 - #start#"
                                };

            // Act
            var a4 = model.GetAllAtoms().First(a => a.Id == "a4");
            var a5 = model.GetAllAtoms().First(a => a.Id == "a5");
            var b4 = model.GetAllBonds().First(b => b.Id == "b4");
            editController.AddObjectListToSelection(new List<BaseObject> { a4, a5, b4 });
            editController.DeleteSelection();
            var undoStack1 = editController.UndoManager.ReadUndoStack();
            editController.UndoManager.Undo();
            editController.UndoManager.Redo();
            var undoStack2 = editController.UndoManager.ReadUndoStack();

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

            var editController = new EditController(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"2 - {nameof(EditController.DeleteAtomsAndBonds)}[SingleAtom]",
                                    "0 - #start#"
                                };

            // Act
            var b4 = model.GetAllBonds().First(b => b.Id == "b4");
            editController.AddObjectListToSelection(new List<BaseObject> { b4 });
            editController.DeleteSelection();
            var undoStack1 = editController.UndoManager.ReadUndoStack();
            editController.UndoManager.Undo();
            editController.UndoManager.Redo();
            var undoStack2 = editController.UndoManager.ReadUndoStack();

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
            var editController = new EditController(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EditController.TransformAtoms)}",
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
            editController.TransformAtoms(tt, new List<Atom> { atom1, atom2 });

            var undoStack1 = editController.UndoManager.ReadUndoStack();
            editController.UndoManager.Undo();
            editController.UndoManager.Redo();
            var undoStack2 = editController.UndoManager.ReadUndoStack();

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
            var editController = new EditController(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EditController.TransformMoleculeList)}",
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
            editController.TransformMoleculeList(tt, new List<Molecule> { molecule });

            var undoStack1 = editController.UndoManager.ReadUndoStack();
            editController.UndoManager.Undo();
            editController.UndoManager.Redo();
            var undoStack2 = editController.UndoManager.ReadUndoStack();

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
            var editController = new EditController(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"3 - {nameof(EditController.AddNewBond)}",
                                    $"2 - {nameof(EditController.AddAtomChain)}[AddEndAtom]",
                                    $"3 - {nameof(EditController.AddNewBond)}",
                                    $"2 - {nameof(EditController.AddAtomChain)}[AddEndAtom]",
                                    $"2 - {nameof(EditController.AddAtomChain)}[AddAtom]",
                                    $"2 - {nameof(EditController.AddAtomChain)}[AddMolecule]",
                                    "0 - #start#"
                                };

            var placements = new List<Point>
                             {
                                new Point(0,0),
                                new Point(5,5),
                                new Point(10,10)
                             };

            // Act
            editController.DrawChain(placements, null);
            var undoStack1 = editController.UndoManager.ReadUndoStack();
            editController.UndoManager.Undo();
            editController.UndoManager.Redo();
            var undoStack2 = editController.UndoManager.ReadUndoStack();

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
            var editController = new EditController(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"3 - {nameof(EditController.AddNewBond)}",
                                    $"2 - {nameof(EditController.AddAtomChain)}[AddEndAtom]",
                                    $"3 - {nameof(EditController.AddNewBond)}",
                                    $"2 - {nameof(EditController.AddAtomChain)}[AddEndAtom]",
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
            editController.DrawChain(placements, atom);
            var undoStack1 = editController.UndoManager.ReadUndoStack();
            editController.UndoManager.Undo();
            editController.UndoManager.Redo();
            var undoStack2 = editController.UndoManager.ReadUndoStack();

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
            var editController = new EditController(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EditController.DrawRing)}",
                                    $"2 - {nameof(EditController.SetBondAttributes)}",
                                    $"2 - {nameof(EditController.SetBondAttributes)}",
                                    $"2 - {nameof(EditController.SetBondAttributes)}",
                                    $"2 - {nameof(EditController.AddNewBond)}",
                                    $"3 - {nameof(EditController.AddNewBond)}",
                                    $"2 - {nameof(EditController.AddAtomChain)}[AddEndAtom]",
                                    $"3 - {nameof(EditController.AddNewBond)}",
                                    $"2 - {nameof(EditController.AddAtomChain)}[AddEndAtom]",
                                    $"3 - {nameof(EditController.AddNewBond)}",
                                    $"2 - {nameof(EditController.AddAtomChain)}[AddEndAtom]",
                                    $"3 - {nameof(EditController.AddNewBond)}",
                                    $"2 - {nameof(EditController.AddAtomChain)}[AddEndAtom]",
                                    $"3 - {nameof(EditController.AddNewBond)}",
                                    $"2 - {nameof(EditController.AddAtomChain)}[AddEndAtom]",
                                    $"2 - {nameof(EditController.AddAtomChain)}[AddAtom]",
                                    $"2 - {nameof(EditController.AddAtomChain)}[AddMolecule]",
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
            editController.DrawRing(placements, true, 0);
            var undoStack1 = editController.UndoManager.ReadUndoStack();
            editController.UndoManager.Undo();
            editController.UndoManager.Redo();
            var undoStack2 = editController.UndoManager.ReadUndoStack();

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

            var editController = new EditController(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EditController.Group)}",
                                    "0 - #start#"
                                };

            // find molecules m1 and m2
            var m1 = model.GetAllMolecules().First(a => a.Id == "m1");
            var m2 = model.GetAllMolecules().First(a => a.Id == "m2");

            // Act
            editController.Group(new List<object> { m1, m2 });
            var undoStack1 = editController.UndoManager.ReadUndoStack();
            editController.UndoManager.Undo();
            editController.UndoManager.Redo();
            var undoStack2 = editController.UndoManager.ReadUndoStack();

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
            var editController = new EditController(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EditController.IncreaseBondOrder)}",
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
            editController.IncreaseBondOrder(bond);

            var undoStack1 = editController.UndoManager.ReadUndoStack();
            editController.UndoManager.Undo();
            editController.UndoManager.Redo();
            var undoStack2 = editController.UndoManager.ReadUndoStack();

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

            var editController = new EditController(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EditController.JoinMolecules)}",
                                    "0 - #start#"
                                };

            // find atom a1 and a4
            var a1 = model.GetAllAtoms().First(a => a.Id == "a1");
            var a4 = model.GetAllAtoms().First(a => a.Id == "a4");

            // Act
            editController.JoinMolecules(a1, a4, Globals.OrderDouble, BondStereo.None);
            var undoStack1 = editController.UndoManager.ReadUndoStack();
            editController.UndoManager.Undo();
            editController.UndoManager.Redo();
            var undoStack2 = editController.UndoManager.ReadUndoStack();

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

            var editController = new EditController(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EditController.RemoveHydrogens)}",
                                    "0 - #start#"
                                };

            // Act
            editController.RemoveHydrogens();
            var undoStack1 = editController.UndoManager.ReadUndoStack();
            editController.UndoManager.Undo();
            editController.UndoManager.Redo();
            var undoStack2 = editController.UndoManager.ReadUndoStack();

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

            var editController = new EditController(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EditController.RemoveHydrogens)}",
                                    "0 - #start#"
                                };

            // Act
            var molecule = model.Molecules.First().Value;
            editController.AddToSelection(molecule);

            editController.RemoveHydrogens();
            var undoStack1 = editController.UndoManager.ReadUndoStack();
            editController.UndoManager.Undo();
            editController.UndoManager.Redo();
            var undoStack2 = editController.UndoManager.ReadUndoStack();

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
            var editController = new EditController(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EditController.SetAverageBondLength)}",
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
            editController.SetAverageBondLength(20);

            var undoStack1 = editController.UndoManager.ReadUndoStack();
            editController.UndoManager.Undo();
            editController.UndoManager.Redo();
            var undoStack2 = editController.UndoManager.ReadUndoStack();
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
            var editController = new EditController(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EditController.SwapBondDirection)}",
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
            editController.SwapBondDirection(bond);

            var undoStack1 = editController.UndoManager.ReadUndoStack();
            editController.UndoManager.Undo();
            editController.UndoManager.Redo();
            var undoStack2 = editController.UndoManager.ReadUndoStack();

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

            var editController = new EditController(model);

            var expectedStack = new List<string>
                                {
                                    "0 - #end#",
                                    $"1 - {nameof(EditController.UnGroup)}",
                                    "0 - #start#"
                                };

            // find molecules m1
            var m1 = model.GetAllMolecules().First(a => a.Id == "m1");

            // Act
            editController.UnGroup(new List<object> { m1 });
            var undoStack1 = editController.UndoManager.ReadUndoStack();
            editController.UndoManager.Undo();
            editController.UndoManager.Redo();
            var undoStack2 = editController.UndoManager.ReadUndoStack();

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

        [Theory]
        [InlineData("AlignLefts", 0.75, 0.75, 0.5, 2.5)]
        [InlineData("AlignCentres", 1.625, 0.75, 1.625, 2.5)]
        [InlineData("AlignRights", 2.25, 0.75, 2.5, 2.5)]
        [InlineData("AlignTops", 0.75, 0.75, 2.5, 0.5)]
        [InlineData("AlignMiddles", 0.75, 1.625, 2.5, 1.625)]
        [InlineData("AlignBottoms", 0.75, 2.25, 2.5, 2.5)]
        public void Aligning_Molecules(string method, double m1x, double m1y, double m2x, double m2y)
        {
            // Arrange
            var model = new Model();
            var controller = new EditController(model);

            var expectedStack = new List<string>
                                 {
                                    "0 - #end#",
                                    $"2 - {nameof(EditController.AlignAnnotations)}",
                                    $"3 - {nameof(EditController.MultiTransformMolecules)}",
                                    "0 - #start#"
                                };

            #region Create Model

            Molecule m1 = new Molecule();

            Atom a1 = new Atom { Position = new Point(0, 0) };
            Atom a2 = new Atom { Position = new Point(0, 1.5) };
            Atom a3 = new Atom { Position = new Point(1.5, 1.5) };
            Atom a4 = new Atom { Position = new Point(1.5, 0) };
            m1.AddAtom(a1);
            a1.Parent = m1;
            m1.AddAtom(a2);
            a2.Parent = m1;
            m1.AddAtom(a3);
            a3.Parent = m1;
            m1.AddAtom(a4);
            a4.Parent = m1;

            Bond b1 = new Bond(a1, a2);
            Bond b2 = new Bond(a2, a3);
            Bond b3 = new Bond(a3, a4);
            Bond b4 = new Bond(a4, a1);
            m1.AddBond(b1);
            b1.Parent = m1;
            m1.AddBond(b2);
            b2.Parent = m1;
            m1.AddBond(b3);
            b3.Parent = m1;
            m1.AddBond(b4);
            b4.Parent = m1;

            Molecule m2 = new Molecule();
            Atom a5 = new Atom { Position = new Point(2, 2) };
            Atom a6 = new Atom { Position = new Point(2, 3) };
            Atom a7 = new Atom { Position = new Point(3, 3) };
            Atom a8 = new Atom { Position = new Point(3, 2) };
            m2.AddAtom(a5);
            a5.Parent = m2;
            m2.AddAtom(a6);
            a6.Parent = m2;
            m2.AddAtom(a7);
            a7.Parent = m2;
            m2.AddAtom(a8);
            a8.Parent = m2;

            Bond b5 = new Bond(a5, a6);
            Bond b6 = new Bond(a6, a7);
            Bond b7 = new Bond(a7, a8);
            Bond b8 = new Bond(a8, a5);
            m2.AddBond(b5);
            b5.Parent = m2;
            m2.AddBond(b6);
            b6.Parent = m2;
            m2.AddBond(b7);
            b7.Parent = m2;
            m2.AddBond(b8);
            b8.Parent = m2;

            model.AddMolecule(m1);
            m1.Parent = model;
            model.AddMolecule(m2);
            m2.Parent = model;

            #endregion Create Model

            // Act
            switch (method)
            {
                case "AlignLefts":
                    controller.AlignLefts(new List<BaseObject> { m1, m2 });
                    break;

                case "AlignCentres":
                    expectedStack = new List<string>
                                    {
                                        "0 - #end#",
                                        $"2 - {nameof(EditController.AlignReactionCentres)}",
                                        $"2 - {nameof(EditController.AlignAnnotations)}",
                                        $"3 - {nameof(EditController.MultiTransformMolecules)}",
                                        "0 - #start#"
                                    };
                    controller.AlignCentres(new List<BaseObject> { m1, m2 });
                    break;

                case "AlignRights":
                    controller.AlignRights(new List<BaseObject> { m1, m2 });
                    break;

                case "AlignTops":
                    controller.AlignTops(new List<BaseObject> { m1, m2 });
                    break;

                case "AlignMiddles":
                    expectedStack = new List<string>
                                    {
                                        "0 - #end#",
                                        $"2 - {nameof(EditController.AlignReactionMiddles)}",
                                        $"2 - {nameof(EditController.AlignAnnotations)}",
                                        $"3 - {nameof(EditController.MultiTransformMolecules)}",
                                        "0 - #start#"
                                    };
                    controller.AlignMiddles(new List<BaseObject> { m1, m2 });
                    break;

                case "AlignBottoms":
                    controller.AlignBottoms(new List<BaseObject> { m1, m2 });
                    break;
            }

            var undoStack1 = controller.UndoManager.ReadUndoStack();
            controller.UndoManager.Undo();
            controller.UndoManager.Redo();
            var undoStack2 = controller.UndoManager.ReadUndoStack();

            // Assert
            CheckUndoStack(expectedStack, undoStack1);
            CheckUndoStack(expectedStack, undoStack2);
            Assert.Equal(m1.Centre, new Point(m1x, m1y));
            Assert.Equal(m2.Centre, new Point(m2x, m2y));
        }

        [Fact]
        public void Customer_Fault_Scenario()
        {
            // Arrange
            var model = new Model();
            var editController = new EditController(model);

            // Act
            // AddAtomChain
            editController.AddAtomChain(null, new Point(872, 709.5), ClockDirections.Nothing, Globals.PeriodicTable.C);

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
            editController.DrawRing(ring1Points, true, 0);

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
            editController.DrawRing(ring2Points, true, 0);

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
            editController.DrawRing(ring3Points, true, 0);

            // Checks before Undo(s)
            CheckMoleculeCount(model, 2);
            CheckAtomCount(model, 16);
            CheckBondCount(model, 17);

            editController.UndoManager.Undo();
            editController.UndoManager.Undo();
            editController.UndoManager.Undo();

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