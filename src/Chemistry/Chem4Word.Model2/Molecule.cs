// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Chem4Word.Model2.Annotations;
using Chem4Word.Model2.Enums;
using Chem4Word.Model2.Helpers;
using Chem4Word.Model2.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Chem4Word.Model2
{
    public class Molecule : BaseObject, IChemistryContainer, INotifyPropertyChanged
    {
        #region Fields

        #region Collections

        public readonly List<Ring> Rings;
        public readonly ReadOnlyDictionary<Guid, Atom> Atoms; //keyed by InternalId
        internal readonly Dictionary<Guid, Atom> _atoms;
        public readonly ReadOnlyCollection<Bond> Bonds; //this is the edge list
        internal readonly List<Bond> _bonds;
        private readonly Dictionary<Guid, Molecule> _molecules;
        public readonly ReadOnlyDictionary<Guid, Molecule> Molecules;
        public ObservableCollection<TextualProperty> Formulas { get; internal set; }
        public ObservableCollection<TextualProperty> Names { get; internal set; }
        public List<string> Warnings { get; set; }
        public List<string> Errors { get; set; }

        public ObservableCollection<TextualProperty> Captions { get; set; }

        #endregion Collections

        #endregion Fields

        #region Properties

        /// <summary>
        /// True if this molecule has functional groups
        /// </summary>
        public bool HasFunctionalGroups
        {
            get
            {
                var result = false;

                foreach (Atom atom in Atoms.Values)
                {
                    if (atom.Element is FunctionalGroup)
                    {
                        result = true;
                        break;
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Count of atoms in this and nested molecules
        /// </summary>
        public int AtomCount
        {
            get
            {
                var count = 0;

                foreach (Molecule molecule in Molecules.Values)
                {
                    count += molecule.AtomCount;
                }

                count += Atoms.Count;

                return count;
            }
        }

        /// <summary>
        /// Count of bonds in this and nested molecules
        /// </summary>
        public int BondCount
        {
            get
            {
                var count = 0;

                foreach (Molecule molecule in _molecules.Values)
                {
                    count += molecule.BondCount;
                }

                count += _bonds.Count;

                return count;
            }
        }

        public List<double> BondLengths
        {
            get
            {
                var lengths = new List<double>(_bonds.Count);

                foreach (Molecule mol in _molecules.Values)
                {
                    lengths.AddRange(mol.BondLengths);
                }

                foreach (Bond bond in _bonds)
                {
                    lengths.Add(bond.BondVector.Length);
                }

                return lengths;
            }
        }

        public Rect BoundingBox
        {
            get
            {
                var boundingBox = Rect.Empty;

                if (Atoms != null && Atoms.Any())
                {
                    double xMax = Atoms.Values.Select(a => a.Position.X).Max();
                    double xMin = Atoms.Values.Select(a => a.Position.X).Min();

                    double yMax = Atoms.Values.Select(a => a.Position.Y).Max();
                    double yMin = Atoms.Values.Select(a => a.Position.Y).Min();

                    boundingBox = new Rect(new Point(xMin, yMin), new Point(xMax, yMax));
                }

                foreach (Molecule mol in Molecules.Values)
                {
                    boundingBox.Union(mol.BoundingBox);
                }

                return boundingBox;
            }
        }

        #region Structural Properties

        public string Id { get; set; }

        public Guid InternalId { get; }

        public IChemistryContainer Parent { get; set; }

        /// <summary>
        /// Returns the root IChemistryContainer  in the tree
        ///
        /// </summary>
        public IChemistryContainer Root
        {
            get
            {
                if (Parent != null)
                {
                    if (Parent.Root != null)
                    {
                        return Parent.Root;
                    }

                    return Parent;
                }

                return this;
            }
        }

        public Molecule RootMolecule
        {
            get
            {
                if (!(Parent is Molecule molecule))
                {
                    return this;
                }
                else
                {
                    return molecule.RootMolecule;
                }
            }
        }

        /// <summary>
        /// Returns a unique path to the molecule
        /// if the molecule is part of a model
        /// this starts with "/"
        /// </summary>
        public override string Path
        {
            get
            {
                var path = "";

                if (Parent == null)
                {
                    path = Id;
                }

                if (Parent is Model model)
                {
                    path = model.Path + Id;
                }

                if (Parent is Molecule molecule)
                {
                    path = molecule.Path + "/" + Id;
                }

                return path;
            }
        }

        public Model Model => Root as Model;

        #endregion Structural Properties

        #region Chemical properties

        private CalculatedFormula _calculatedFormula;

        public string ConciseFormula
        {
            get
            {
                if (_calculatedFormula == null)
                {
                    _calculatedFormula = new CalculatedFormula();

                    var molecules = Molecules.Values.ToList();
                    if (Molecules.Count > 0)
                    {
                        _calculatedFormula.Parts.Add(new MoleculeFormulaPart(FormulaPartType.Separator, "[", 0));

                        var i = molecules.Count - 1;

                        foreach (var molecule in Molecules.Values)
                        {
                            _calculatedFormula.Parts.AddRange(molecule.CalculatedFormula.Parts);
                            if (i-- > 0)
                            {
                                _calculatedFormula.Parts.Add(new MoleculeFormulaPart(FormulaPartType.Separator, " · ", 0));
                            }
                        }

                        _calculatedFormula.Parts.Add(new MoleculeFormulaPart(FormulaPartType.Separator, "]", 0));

                        if (FormalCharge != null)
                        {
                            var sign = "";
                            var absCharge = Math.Abs(FormalCharge.Value);
                            if (FormalCharge.Value > 0)
                            {
                                sign = "+";
                            }
                            if (FormalCharge.Value < 0)
                            {
                                sign = "-";
                            }

                            _calculatedFormula.Parts.Add(new MoleculeFormulaPart(FormulaPartType.Charge, sign, absCharge));
                        }
                    }
                    else
                    {
                        _calculatedFormula = new CalculatedFormula(GetFormulaParts());
                    }
                }

                return _calculatedFormula.ToString();
            }
        }

        public CalculatedFormula CalculatedFormula
        {
            get
            {
                if (_calculatedFormula == null)
                {
                    _calculatedFormula = new CalculatedFormula(GetFormulaParts());
                }

                return _calculatedFormula;
            }
        }

        private List<MoleculeFormulaPart> GetFormulaParts()
        {
            var cPart = new MoleculeFormulaPart(FormulaPartType.Element, "C", 0);
            var hPart = new MoleculeFormulaPart(FormulaPartType.Element, "H", 0);
            var otherParts = new Dictionary<string, MoleculeFormulaPart>();
            var chargePart = new MoleculeFormulaPart(FormulaPartType.Charge, "", 0);

            var sumOfParts = new List<MoleculeFormulaPart>();

            #region Gather counts

            foreach (var atom in Atoms.Values)
            {
                // Add this element
                if (atom.Element is Element e)
                {
                    // Obtain sum of charge on all atoms as we go round the loop
                    if (atom.FormalCharge != null)
                    {
                        chargePart.Count += atom.FormalCharge.Value;
                    }

                    var symbol = e.Symbol;

                    switch (symbol)
                    {
                        case "C":
                            cPart.Count++;
                            break;

                        case "H":
                            hPart.Count++;
                            break;

                        default:
                            if (otherParts.ContainsKey(symbol))
                            {
                                otherParts[symbol].Count++;
                            }
                            else
                            {
                                otherParts.Add(symbol, new MoleculeFormulaPart(FormulaPartType.Element, symbol, 1));
                            }

                            break;
                    }

                    var hCount = atom.ImplicitHydrogenCount;
                    if (hCount > 0)
                    {
                        hPart.Count += hCount;
                    }
                }

                // Expand functional group
                if (atom.Element is FunctionalGroup fg)
                {
                    var parts = fg.FormulaParts;
                    foreach (var part in parts)
                    {
                        switch (part.Key)
                        {
                            case "C":
                                cPart.Count += part.Value;
                                break;

                            case "H":
                                hPart.Count += part.Value;
                                break;

                            default:
                                if (otherParts.ContainsKey(part.Key))
                                {
                                    otherParts[part.Key].Count += part.Value;
                                }
                                else
                                {
                                    otherParts.Add(part.Key, new MoleculeFormulaPart(FormulaPartType.Element, part.Key, part.Value));
                                }
                                break;
                        }
                    }
                }
            }

            #endregion Gather counts

            #region Construct Hill Notation

            // Now add the parts in in the correct order (Hill Notation) C then H then the rest in alphabetical order then the charge
            if (cPart.Count > 0)
            {
                sumOfParts.Add(cPart);
            }

            if (hPart.Count > 0)
            {
                sumOfParts.Add(hPart);
            }

            if (otherParts.Any())
            {
                sumOfParts.AddRange(otherParts.Values);
            }

            #endregion Construct Hill Notation

            #region Add molecule charge if present

            // Get charge for the molecule
            if (FormalCharge != null)
            {
                // Add the molecule's charge to what's been calculated from the atoms
                chargePart.Count += FormalCharge.Value;
            }

            #endregion Add molecule charge if present

            if (chargePart.Count != 0)
            {
                if (chargePart.Count > 0)
                {
                    chargePart.Text = "+";
                }
                if (chargePart.Count < 0)
                {
                    chargePart.Text = "-";
                }

                sumOfParts.Add(chargePart);
            }

            return sumOfParts;
        }

        private bool? _showMoleculeBrackets;

        public bool? ShowMoleculeBrackets
        {
            get => _showMoleculeBrackets;
            set
            {
                _showMoleculeBrackets = value;
                OnPropertyChanged();
            }
        }

        private int? _spinMultiplicity;

        public int? SpinMultiplicity
        {
            get { return _spinMultiplicity; }
            set
            {
                _spinMultiplicity = value;
                OnPropertyChanged();
            }
        }

        private int? _count;

        public int? Count
        {
            get => _count;
            set
            {
                _count = value;
                OnPropertyChanged();
            }
        }

        private int? _formalCharge;

        public int? FormalCharge
        {
            get { return _formalCharge; }
            set
            {
                _formalCharge = value;
                OnPropertyChanged();
            }
        }

        #endregion Chemical properties

        #region Reaction Properties

        private SubstanceRole? _substanceRole;

        public SubstanceRole? SubstanceRole
        {
            get
            {
                return _substanceRole;
            }
            set
            {
                if (_substanceRole != value)
                {
                    _substanceRole = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _dictRef;

        public string DictRef
        {
            get
            {
                return _dictRef;
            }
            set
            {
                if (_dictRef != value)
                {
                    _dictRef = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _title;

        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion Reaction Properties

        #endregion Properties

        #region Constructor

        public Molecule()
        {
            InternalId = Guid.NewGuid();
            Id = InternalId.ToString("D");

            _atoms = new Dictionary<Guid, Atom>();
            Atoms = new ReadOnlyDictionary<Guid, Atom>(_atoms);
            _bonds = new List<Bond>();
            Bonds = new ReadOnlyCollection<Bond>(_bonds);
            _molecules = new Dictionary<Guid, Molecule>();
            Molecules = new ReadOnlyDictionary<Guid, Molecule>(_molecules);

            Formulas = new ObservableCollection<TextualProperty>();
            Names = new ObservableCollection<TextualProperty>();
            Captions = new ObservableCollection<TextualProperty>();

            Errors = new List<string>();
            Warnings = new List<string>();
            Rings = new List<Ring>();
        }

        #endregion Constructor

        public void AddBond(Bond newBond)
        {
            _bonds.Add(newBond);
            var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<Bond> { newBond });
            OnBondsChanged(this, e);
            UpdateBondsPropertyHandlers(e);
        }

        public void RemoveBond(Bond toRemove)
        {
            _bonds.Remove(toRemove);
            var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new List<Bond> { toRemove });
            OnBondsChanged(this, e);
            UpdateBondsPropertyHandlers(e);
        }

        public void AddAtom(Atom newAtom)
        {
            _atoms[newAtom.InternalId] = newAtom;
            var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<Atom> { newAtom });
            OnAtomsChanged(this, e);
            UpdateAtomPropertyHandlers(e);
        }

        public void RemoveAtom(Atom toRemove)
        {
            bool bondsExist = Bonds.Any(b => b.StartAtomInternalId.Equals(toRemove.InternalId)
                                                        || b.EndAtomInternalId.Equals(toRemove.InternalId));
            if (bondsExist)
            {
                throw new InvalidOperationException("Cannot remove an Atom without first removing the attached Bonds.");
            }

            bool result = _atoms.Remove(toRemove.InternalId);
            if (result)
            {
                var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new List<Atom> { toRemove });
                OnAtomsChanged(this, e);
                UpdateAtomPropertyHandlers(e);
            }
        }

        private void UpdateAtomPropertyHandlers(NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (object oldItem in e.OldItems)
                {
                    ((Atom)oldItem).PropertyChanged -= ChemObject_PropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (object newItem in e.NewItems)
                {
                    ((Atom)newItem).PropertyChanged += ChemObject_PropertyChanged;
                }
            }
        }

        public bool RemoveMolecule(Molecule mol)
        {
            var res = _molecules.Remove(mol.InternalId);
            if (res)
            {
                var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new List<Molecule> { mol });
                OnMoleculesChanged(this, e);
                UpdateMoleculeHandlers(e);
            }

            return res;
        }

        public Molecule AddMolecule(Molecule newMol)
        {
            _molecules[newMol.InternalId] = newMol;
            var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<Molecule> { newMol });
            OnMoleculesChanged(this, e);
            UpdateMoleculeHandlers(e);
            return newMol;
        }

        public List<TextualProperty> AllTextualProperties
        {
            get
            {
                var list = new List<TextualProperty>();

                list.AddRange(GetChildProperties(this));

                return list;

                // Local function for recursion
                List<TextualProperty> GetChildProperties(Molecule molecule)
                {
                    var properties = new List<TextualProperty>();

                    if (molecule.Atoms.Any())
                    {
                        properties.Add(new TextualProperty
                        {
                            Id = $"{molecule.Id}.f0",
                            TypeCode = "F",
                            FullType = "MoleculeConciseFormula",
                            Value = molecule.ConciseFormula
                        });

                        foreach (TextualProperty name in molecule.Names)
                        {
                            properties.Add(new TextualProperty
                            {
                                Id = name.Id,
                                TypeCode = "N",
                                FullType = name.FullType,
                                Value = name.Value
                            });
                        }

                        foreach (TextualProperty formula in molecule.Formulas)
                        {
                            properties.Add(new TextualProperty
                            {
                                Id = formula.Id,
                                TypeCode = "F",
                                FullType = formula.FullType,
                                Value = formula.Value
                            });
                        }

                        properties.Add(new TextualProperty
                        {
                            Id = "S",
                            TypeCode = "S",
                            FullType = "Separator",
                            Value = "S"
                        });
                    }
                    else
                    {
                        foreach (Molecule child in molecule.Molecules.Values)
                        {
                            properties.AddRange(GetChildProperties(child));
                        }
                    }

                    return properties;
                }
            }
        }

        public string CalculatedFormulaOfChildren
        {
            get
            {
                // Phase #1 Collect data
                var calculateFormulas = new List<string>();

                calculateFormulas.AddRange(GetChildFormulas(this));

                // Phase #2 - Collate the values
                var result = "";
                var dictionary = new Dictionary<string, int>();
                foreach (string formula in calculateFormulas)
                {
                    if (dictionary.ContainsKey(formula))
                    {
                        dictionary[formula]++;
                    }
                    else
                    {
                        dictionary.Add(formula, 1);
                    }
                }

                foreach (KeyValuePair<string, int> kvp in dictionary)
                {
                    if (kvp.Value == 1)
                    {
                        result += $"{kvp.Key} . ";
                    }
                    else
                    {
                        result += $"{kvp.Value} {kvp.Key} . ";
                    }
                }

                if (result.EndsWith(" . "))
                {
                    result = result.Substring(0, result.Length - 3);
                }

                return result;

                // Local Function
                List<string> GetChildFormulas(Molecule molecule)
                {
                    var childFormulae = new List<string>();

                    foreach (Molecule child in molecule.Molecules.Values)
                    {
                        if (child.Molecules.Count == 0)
                        {
                            childFormulae.Add(child.ConciseFormula);
                        }
                        else
                        {
                            childFormulae.AddRange(GetChildFormulas(child));
                        }
                    }

                    return childFormulae;
                }
            }
        }

        public double Top => BoundingBox.Top;
        public double Bottom => BoundingBox.Bottom;
        public double Left => BoundingBox.Left;
        public double Right => BoundingBox.Right;
        public Point Centre => new Point(Left + (Right - Left) / 2, Top + (Bottom - Top) / 2);

        #region Events

        public event NotifyCollectionChangedEventHandler AtomsChanged;

        public event NotifyCollectionChangedEventHandler BondsChanged;

        public event NotifyCollectionChangedEventHandler MoleculesChanged;

        #endregion Events

        #region Event handlers

        private void OnMoleculesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyCollectionChangedEventHandler temp = MoleculesChanged;
            if (temp != null)
            {
                temp.Invoke(sender, e);
            }
        }

        private void UpdateMoleculeHandlers(NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (object oldItem in e.OldItems)
                {
                    var mol = (Molecule)oldItem;
                    mol.AtomsChanged -= Atoms_CollectionChanged;
                    mol.BondsChanged -= Bonds_CollectionChanged;
                    mol.MoleculesChanged -= Molecules_CollectionChanged;
                    mol.PropertyChanged -= ChemObject_PropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (object newItem in e.NewItems)
                {
                    var mol = (Molecule)newItem;
                    mol.AtomsChanged += Atoms_CollectionChanged;
                    mol.BondsChanged += Bonds_CollectionChanged;
                    mol.MoleculesChanged += Molecules_CollectionChanged;
                    mol.PropertyChanged += ChemObject_PropertyChanged;
                }
            }
        }

        private void Molecules_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnMoleculesChanged(sender, e);
        }

        private void Bonds_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnBondsChanged(sender, e);
        }

        private void OnBondsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyCollectionChangedEventHandler temp = BondsChanged;
            if (temp != null)
            {
                temp.Invoke(sender, e);
            }
        }

        private void UpdateBondsPropertyHandlers(NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (object oldItem in e.OldItems)
                {
                    ((Bond)oldItem).PropertyChanged -= ChemObject_PropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (object newItem in e.NewItems)
                {
                    ((Bond)newItem).PropertyChanged += ChemObject_PropertyChanged;
                }
            }
        }

        private void Atoms_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnAtomsChanged(sender, e);
        }

        private void OnAtomsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyCollectionChangedEventHandler temp = AtomsChanged;
            if (temp != null)
            {
                temp.Invoke(sender, e);
            }
        }

        private void ChemObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(sender, e);
        }

        #endregion Event handlers

        #region Methods

        public string GetNextId(ObservableCollection<TextualProperty> properties, string seedSuffix)
        {
            string prefix = Id;
            string suffix = seedSuffix;
            var max = 0;

            foreach (TextualProperty property in properties)
            {
                if (!string.IsNullOrEmpty(property.Id)
                    && property.Id.Contains("."))
                {
                    string[] parts = property.Id.Split('.');
                    prefix = parts[0];
                    suffix = parts[1].Substring(0, 1);
                    int n;
                    if (int.TryParse(parts[1].Substring(1), out n))
                    {
                        max = Math.Max(max, n);
                    }
                }
            }

            return $"{prefix}.{suffix}{max + 1}";
        }

        public IEnumerable<Atom> GetAtomNeighbours(Atom atom)
        {
            foreach (Bond bond in _bonds)
            {
                if (bond.StartAtomInternalId.Equals(atom.InternalId))
                {
                    yield return Atoms[bond.EndAtomInternalId];
                }
                else if (bond.EndAtomInternalId.Equals(atom.InternalId))
                {
                    yield return Atoms[bond.StartAtomInternalId];
                }
            }
        }

        public void MoveAllAtoms(double x, double y)
        {
            var offsetVector = new Vector(x, y);

            foreach (Atom a in Atoms.Values)
            {
                a.Position += offsetVector;
            }

            foreach (Molecule child in Molecules.Values)
            {
                child.MoveAllAtoms(x, y);
            }
        }

        public List<Molecule> GetChildMolecules()
        {
            var molecules = new List<Molecule>();

            foreach (KeyValuePair<Guid, Molecule> kvp in Molecules)
            {
                Molecule molecule = kvp.Value;
                molecules.Add(molecule);

                if (molecule.Molecules.Any())
                {
                    molecules.AddRange(molecule.GetChildMolecules());
                }
            }

            return molecules;
        }

        public void SetAnyMissingNameIds()
        {
            // Fix any missing Ids of 1D labels (aka Names)
            foreach (TextualProperty formula in Formulas)
            {
                if (string.IsNullOrEmpty(formula.Id))
                {
                    formula.Id = GetNextId(Formulas, "f");
                }
            }

            foreach (TextualProperty name in Names)
            {
                if (string.IsNullOrEmpty(name.Id))
                {
                    name.Id = GetNextId(Names, "n");
                }
            }

            foreach (TextualProperty label in Captions)
            {
                if (string.IsNullOrEmpty(label.Id))
                {
                    label.Id = GetNextId(Captions, "l");
                }
            }

            foreach (Molecule mol in Molecules.Values)
            {
                mol.SetAnyMissingNameIds();
            }
        }

        public void SetProtectedLabels(List<string> protectedLabels)
        {
            if (protectedLabels != null)
            {
                foreach (TextualProperty formula in Formulas)
                {
                    formula.CanBeDeleted = !protectedLabels.Any(s => s.StartsWith($"{formula.Id}:"));
                }

                foreach (TextualProperty name in Names)
                {
                    name.CanBeDeleted = !protectedLabels.Any(s => s.StartsWith($"{name.Id}:"));
                }

                foreach (TextualProperty label in Captions)
                {
                    label.CanBeDeleted = !protectedLabels.Any(s => s.StartsWith($"{label.Id}:"));
                }
            }

            foreach (Molecule mol in Molecules.Values)
            {
                mol.SetProtectedLabels(protectedLabels);
            }
        }

        public void ReLabelGuids(ref int molCount, ref int atomCount, ref int bondCount)
        {
            Guid guid;
            if (Guid.TryParse(Id, out guid))
            {
                Id = $"m{++molCount}";
            }

            foreach (Atom a in Atoms.Values)
            {
                if (Guid.TryParse(a.Id, out guid))
                {
                    a.Id = $"a{++atomCount}";
                }
            }

            foreach (Bond b in Bonds)
            {
                if (Guid.TryParse(b.Id, out guid))
                {
                    b.Id = $"b{++bondCount}";
                }
            }

            foreach (Molecule molecule in Molecules.Values)
            {
                molecule.ReLabelGuids(ref molCount, ref atomCount, ref bondCount);
            }
        }

        public void ReLabel(ref int molCount, ref int atomCount, ref int bondCount, bool includeNames)
        {
            Id = $"m{++molCount}";

            foreach (Atom a in Atoms.Values)
            {
                a.Id = $"a{++atomCount}";
            }

            foreach (Bond b in Bonds)
            {
                b.Id = $"b{++bondCount}";
            }

            if (includeNames)
            {
                var count = 1;
                foreach (TextualProperty formula in Formulas)
                {
                    formula.Id = $"{Id}.f{count++}";
                }

                count = 1;
                foreach (TextualProperty name in Names)
                {
                    name.Id = $"{Id}.n{count++}";
                }

                count = 1;
                foreach (TextualProperty label in Captions)
                {
                    label.Id = $"{Id}.l{count++}";
                }
            }

            foreach (Molecule molecule in Molecules.Values)
            {
                molecule.ReLabel(ref molCount, ref atomCount, ref bondCount, includeNames);
            }
        }

        public IEnumerable<Bond> GetBonds(Guid atomId)
        {
            foreach (Bond bond in _bonds)
            {
                if (bond.StartAtomInternalId == atomId || bond.EndAtomInternalId == atomId)
                {
                    yield return bond;
                }
            }
        }

        public void Refresh()
        {
            foreach (Molecule child in Molecules.Values)
            {
                child.Refresh();
            }

            RebuildRings(true);
        }

        public void UpdateVisual()
        {
            foreach (Atom atom in Atoms.Values)
            {
                atom.SendDummyNotif();
            }

            foreach (Bond bond in Bonds)
            {
                bond.SendDummyNotif();
            }
            foreach (Molecule mol in Molecules.Values)
            {
                mol.UpdateVisual();
            }

            SendDummyNotif();
        }

        private void SendDummyNotif()
        {
            OnPropertyChanged(nameof(BoundingBox));
        }

        public Molecule Copy()
        {
            var copy = new Molecule();

            var aa = new Dictionary<string, Atom>();

            foreach (Atom atom in Atoms.Values)
            {
                var newAtom = new Atom
                {
                    Id = atom.Id,
                    Position = atom.Position,
                    Element = atom.Element,
                    FormalCharge = atom.FormalCharge,
                    IsotopeNumber = atom.IsotopeNumber,
                    ExplicitC = atom.ExplicitC,
                    ExplicitHPlacement = atom.ExplicitHPlacement,
                    ExplicitFunctionalGroupPlacement = atom.ExplicitFunctionalGroupPlacement
                };

                copy.AddAtom(newAtom);
                newAtom.Parent = copy;
                aa.Add(newAtom.Id, newAtom);
            }

            foreach (Bond bond in Bonds)
            {
                Atom s = aa[bond.StartAtom.Id];
                Atom e = aa[bond.EndAtom.Id];
                var newBond = new Bond(s, e)
                {
                    Id = bond.Id,
                    Order = bond.Order,
                    Stereo = bond.Stereo,
                    ExplicitPlacement = bond.ExplicitPlacement
                };

                copy.AddBond(newBond);
                newBond.Parent = copy;
            }

            foreach (TextualProperty property in Names)
            {
                var textualProperty = new TextualProperty
                {
                    Id = property.Id,
                    TypeCode = property.TypeCode,
                    FullType = property.FullType,
                    Value = property.Value
                };

                copy.Names.Add(textualProperty);
            }

            foreach (TextualProperty property in Formulas)
            {
                var textualProperty = new TextualProperty
                {
                    Id = property.Id,
                    TypeCode = property.TypeCode,
                    FullType = property.FullType,
                    Value = property.Value
                };

                copy.Formulas.Add(textualProperty);
            }

            foreach (TextualProperty label in Captions)
            {
                copy.Captions.Add(label);
            }

            copy.Id = Id;
            copy.FormalCharge = FormalCharge;
            copy.Count = Count;
            copy.SpinMultiplicity = SpinMultiplicity;
            copy.ShowMoleculeBrackets = ShowMoleculeBrackets;

            // Copy child molecules
            foreach (Molecule child in Molecules.Values)
            {
                Molecule c = child.Copy();
                copy.AddMolecule(c);
                c.Parent = copy;
            }

            return copy;
        }

        /// <summary>
        /// Checks to make sure the internals of the molecule haven't become busted up.
        /// </summary>
        public List<string> CheckIntegrity()
        {
            var result = new List<string>();

            //first, check to see whether there aren't more than one region
            if (TheoreticalRings < 0) //we have a disconnected graph!
            {
                result.Add($"Molecule {Path} is disconnected.");
            }

            foreach (KeyValuePair<Guid, Atom> atomObject in Atoms)
            {
                Guid key = atomObject.Key;
                Atom atom = atomObject.Value;

                if (atom.Parent == null)
                {
                    result.Add($"Atom {atom} is disconnected!");
                }

                if (key != atom.InternalId)
                {
                    result.Add($"Atom {atom} Key != InternalId");
                }
            }

            //now check to see that ever bond refers to a valid atom
            foreach (Bond bond in Bonds)
            {
                if (bond.Parent == null)
                {
                    result.Add($"Bond {bond} is disconnected");
                }

                if (!Atoms.ContainsKey(bond.StartAtomInternalId))
                {
                    result.Add($"Bond {bond} refers to a missing start atom {bond.StartAtomInternalId}");
                }

                if (!Atoms.ContainsKey(bond.EndAtomInternalId))
                {
                    result.Add($"Bond {bond} refers to a missing end atom {bond.EndAtomInternalId}");
                }
            }

            foreach (Molecule child in Molecules.Values)
            {
                result.AddRange(child.CheckIntegrity());
            }

            return result;
        }

        #endregion Methods

        #region Overrides

        public override string ToString()
        {
            return $"Molecule {Id} - {Path}; Atoms {Atoms.Count} Bonds {Bonds.Count} Molecules {Molecules.Count}";
        }

        #endregion Overrides

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged

        #region Ring stuff

        /// <summary>
        /// How many rings the molecule contains.  Non cyclic molecules ALWAYS have atoms = bonds +1
        /// </summary>
        public int TheoreticalRings
        {
            get { return Bonds.Count - Atoms.Count + 1; }
        }

        /// <summary>
        /// If the molecule has more atoms than bonds, it doesn't have a ring. Test this before running ring perception.
        /// </summary>
        private bool HasRings => TheoreticalRings > 0;

        /// <summary>
        /// Quick check to see if the rings need recalculating
        /// </summary>
        public bool RingsCalculated
        {
            get
            {
                if (!HasRings)
                {
                    return true; //don't bother recalculating the rings for a linear molecule
                }
                else
                {
                    return Rings.Count > 0; //we have rings present, so why haven't we calculated them?
                }
            }
        } //have we calculated the rings yet?

        /// <summary>
        /// Cleaves off a degree 1 atom from the working set.
        /// Reduces the adjacent atoms' degree by one
        /// </summary>
        /// <param name="toPrune">Atom to prune</param>
        /// <param name="workingSet">Dictionary of atoms</param>
        private static void PruneAtom(Atom toPrune, Dictionary<Atom, int> workingSet)
        {
            foreach (Atom neighbour in toPrune.Neighbours)
            {
                if (workingSet.ContainsKey(neighbour))
                {
                    workingSet[neighbour] -= 1;
                }
            }

            workingSet.Remove(toPrune);
        }

        /// <summary>
        /// Removes side chain atoms from the working set
        /// DOES NOT MODIFY the original molecule!
        /// Assumes we don't have any degree zero atoms
        /// (i.e this isn't a single atom Molecule)
        /// </summary>
        /// <param name="projection">Molecule to prune</param>
        private static void PruneSideChains(Dictionary<Atom, int> projection)
        {
            var hasPruned = true;

            while (hasPruned)
            {
                //clone the working set atoms first because otherwise LINQ will object

                var atomList = new List<Atom>();
                foreach (KeyValuePair<Atom, int> kvp in projection)
                {
                    if (kvp.Value < 2)
                    {
                        atomList.Add(kvp.Key);
                    }
                }

                hasPruned = atomList.Count > 0;

                foreach (Atom a in atomList)
                {
                    PruneAtom(a, projection);
                }
            }
        }

        private Dictionary<Atom, T> Projection<T>(Func<Atom, T> getProperty)
        {
            return Atoms.Values.ToDictionary(a => a, a => getProperty(a));
        }

        public void RebuildRings(bool force = false)
        {
            RebuildRingsFigueras();

            // -------------- //
            // Local Function //
            // -------------- //
            void RebuildRingsFigueras()
            {
                Rings.Clear();

                if (HasRings || force)
                {
                    //working set of atoms
                    //it's a dictionary, because we initially store the degree of each atom against it
                    //this will change as the pruning operation kicks in
                    Dictionary<Atom, int> workingSet = Projection(a => a.Degree);
                    //lop off any terminal branches
                    PruneSideChains(workingSet);

                    while (workingSet.Any()) //do we have any atoms left in the set
                    {
                        Atom startAtom =
                            workingSet.Keys.OrderByDescending(a => a.Degree).First(); // go for the highest degree atom
                        Ring nextRing = GetRing(startAtom);                           //identify a ring
                        if (nextRing != null)                                         //bingo
                        {
                            //and add the ring to the atoms
                            Rings.Add(nextRing); //add the ring to the set
                            foreach (Atom a in nextRing.Atoms.ToList())
                            {
                                if (workingSet.ContainsKey(a))
                                {
                                    //remove the atoms in the ring from the working set BUT NOT the graph!
                                    workingSet.Remove(a);
                                }
                            }
                        }
                        else
                        {
                            //the atom doesn't belong in a ring, remove it from the set.
                            workingSet.Remove(startAtom);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sorts rings for double bond placement
        /// using Alex Clark's rules
        /// </summary>
        public List<Ring> SortedRings => SortRingsForDBPlacement();

        public Point Centroid => GeometryTool.GetCentroid(BoundingBox);

        public bool IsGrouped => Molecules.Count > 0;

        /// <summary>
        /// Splits a molecule into child molecules and
        /// adds them in as immediate children
        /// Note this routine is destructive and should only
        /// be used on copies of molecules unless
        /// you are certain there are disconnected fragments
        /// </summary>
        public void SplitIntoChildren()
        {
            // Take a count of the atoms in the molecule
            int atomCount = Atoms.Count;

            // Keep a running total of processed atoms
            var processed = 0;

            while (processed < atomCount)
            {
                // Choose the first atom
                Atom start = Atoms.Values.First();
                var childAtoms = new HashSet<Atom>();
                TraverseBFS(start, a => childAtoms.Add(a), a => !childAtoms.Contains(a));
                processed += childAtoms.Count;

                var childBonds = new HashSet<Bond>();
                foreach (Atom childAtom in childAtoms)
                {
                    foreach (Bond childAtomBond in childAtom.Bonds)
                    {
                        childBonds.Add(childAtomBond);
                    }
                }

                // Create a new child
                var newChild = new Molecule
                {
                    Parent = this
                };

                // Add it to this molecule
                AddMolecule(newChild);

                foreach (Bond childBond in childBonds)
                {
                    RemoveBond(childBond);
                    childBond.Parent = newChild;
                    newChild.AddBond(childBond);
                }

                foreach (Atom childAtom in childAtoms)
                {
                    RemoveAtom(childAtom);
                    childAtom.Parent = newChild;
                    newChild.AddAtom(childAtom);
                }
            }
        }

        /// <summary>
        /// Sorts a series of small rings ready for determining double bond placement
        /// see DOI: 10.1002/minf.201200171
        /// Rendering molecular sketches for publication quality output
        /// Alex M Clark
        /// </summary>
        /// <returns>List of rings</returns>
        // ReSharper disable once InconsistentNaming
        public List<Ring> SortRingsForDBPlacement()
        {
            //1) All rings of sizes 6, 5, 7, 4 and 3 are discovered, in that order, and added to a list R.
            List<Ring> prioritisedRings = Rings.Where(x => x.Priority > 0).OrderBy(x => x.Priority).ToList();

            //Define B as an array of size equal to the number of atoms, where each value
            //is equal to the number of times the atom occurs in any of the rings R
            var atomFrequency = new Dictionary<Atom, int>();
            foreach (Atom atom in Atoms.Values)
            {
                atomFrequency[atom] = atom.RingCount;
            }

            //Define Q as an array of size equal to length of R, where each value is equal
            //to sum of B[r], where r iterates over each of the atoms within the ring.
            var cumulFreqPerRing = new Dictionary<Ring, int>();
            foreach (Ring ring in prioritisedRings)
            {
                var sumBr = 0;
                foreach (Atom atom in ring.Atoms)
                {
                    sumBr += atomFrequency[atom];
                }

                cumulFreqPerRing[ring] = sumBr;
            }

            //Perform a stable sort of the list of rings, R, so that those with the lowest values of Q are listed first.
            IOrderedEnumerable<Ring> lowestCumulFreq = prioritisedRings.OrderBy(r => cumulFreqPerRing[r]);

            //Define D as an array of size equal to length of R, where each value is equal to the number of double bonds within the corresponding ring
            var doubleBondsperRing = new Dictionary<Ring, int>();
            foreach (Ring ring in lowestCumulFreq)
            {
                doubleBondsperRing[ring] = ring.Bonds.Count(b => b.OrderValue == 2);
            }

            //Perform a stable sort of the list of rings, R, so that those with highest values of D are listed first

            IOrderedEnumerable<Ring> highestDBperRing = lowestCumulFreq.OrderByDescending(r => doubleBondsperRing[r]);

            List<Ring> sortRingsForDbPlacement = highestDBperRing.ToList();
            return sortRingsForDbPlacement;
        }

        /// <summary>
        /// Start with an atom and detect which ring it's part of
        /// </summary>
        /// <param name="startAtom">Atom of degree >= 2</param>
        ///
        private static Ring GetRing(Atom startAtom)
        {
            // Only returns the first ring.
            //
            // Uses the Figueras algorithm
            // Figueras, J, J. Chem. Inf. Comput. Sci., 1996,36, 96, 986-991
            // The algorithm goes as follows:
            //1. Remove node frontNode and its Source from the front of the queue.
            //2. For each node m attached to frontNode, and not equal to Source:
            //If path[m] is null, compute path[m] ) path[frontNode] +[m]
            //and put node m(with its Source, frontNode) on the back of the queue.
            //If path[m] is not null then
            //      1) Compute the intersection path[frontNode]*path[m].
            //      2) If the intersection is a singleton, compute the ring set  path[m]+path[frontNode] and exit.
            //3. Return to step 1.
            //set up the data structures
            Queue<AtomData> atomsSoFar; //needed for BFS
            var path = new Dictionary<Atom, HashSet<Atom>>();

            //initialise all the paths to empty
            foreach (Atom atom in startAtom.Parent.Atoms.Values)
            {
                path[atom] = new HashSet<Atom>();
            }

            //set up a new queue
            atomsSoFar = new Queue<AtomData>();

            //set up a front node and shove it onto the queue
            //shove the neigbours onto the queue to prime it
            foreach (Atom initialAtom in startAtom.Neighbours)
            {
                var node = new AtomData { Source = startAtom, CurrentAtom = initialAtom };
                path[initialAtom] = new HashSet<Atom> { startAtom, initialAtom };
                atomsSoFar.Enqueue(node);
            }

            //now scan the Molecule and detect all rings
            while (atomsSoFar.Any())
            {
                AtomData frontNode = atomsSoFar.Dequeue();
                foreach (Atom m in frontNode.CurrentAtom.Neighbours)
                {
                    if (m != frontNode.Source) //ignore an atom that we've visited
                    {
                        if (!path.ContainsKey(m) || path[m].Count == 0) //null path
                        {
                            var temp = new HashSet<Atom> { m };

                            temp.UnionWith(path[frontNode.CurrentAtom]);
                            path[m] = temp; //add on the path built up so far
                            var newItem = new AtomData { Source = frontNode.CurrentAtom, CurrentAtom = m };
                            atomsSoFar.Enqueue(newItem);
                        }
                        else //we've got a collision - is it a ring closure
                        {
                            var overlap = new HashSet<Atom>();
                            overlap.UnionWith(path[frontNode.CurrentAtom]); //clone this set
                            overlap.IntersectWith(path[m]);
                            if (overlap.Count == 1) //we've had a singleton overlap :  ring closure
                            {
                                var ringAtoms = new HashSet<Atom>();
                                ringAtoms.UnionWith(path[m]);
                                ringAtoms.UnionWith(path[frontNode.CurrentAtom]);

                                return new Ring(ringAtoms);
                            }
                        }
                    }
                }
            }

            //no collisions therefore no rings detected
            return null;
        }

        #endregion Ring stuff

        public void BuildAtomList(List<Atom> allAtoms)
        {
            allAtoms.AddRange(Atoms.Values);

            foreach (Molecule child in Molecules.Values)
            {
                child.BuildAtomList(allAtoms);
            }
        }

        public void BuildBondList(List<Bond> allBonds)
        {
            allBonds.AddRange(Bonds);

            foreach (Molecule child in Molecules.Values)
            {
                child.BuildBondList(allBonds);
            }
        }

        /// <summary>
        /// Moves all atoms of molecule by inverse of x and y
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void RepositionAll(double x, double y)
        {
            var offsetVector = new Vector(-x, -y);

            foreach (Atom a in Atoms.Values)
            {
                a.Position += offsetVector;
            }

            foreach (Molecule child in Molecules.Values)
            {
                child.RepositionAll(x, y);
            }
        }

        public void BuildMolList(List<Molecule> allMolecules)
        {
            allMolecules.Add(this);
            foreach (Molecule mol in Molecules.Values)
            {
                mol.BuildMolList(allMolecules);
            }
        }

        public void ClearProperties()
        {
            Names.Clear();
            Formulas.Clear();
        }

        /// <summary>
        /// Joins two molecules into a third
        /// </summary>
        /// <param name="a">First Molecule</param>
        /// <param name="b">Second Molecule</param>
        /// <param name="bond">Bond (must have StartAtomInternalID, EndAtomIternalID set)</param>
        /// <returns></returns>
        public static Molecule Join(Molecule a, Molecule b, Bond bond)
        {
            var copy = new Molecule();
            Transfer(a, copy);
            Transfer(b, copy);

            //finally add the joining bond
            bond.Parent = copy;
            copy.AddBond(bond);

            copy.CheckIntegrity();
            return copy;

            //local function
            void Transfer(Molecule source, Molecule destination)
            {
                //copy over and reparent the atoms and bonds
                foreach (Atom sourceAtom in source.Atoms.Values)
                {
                    sourceAtom.Parent = destination;
                    destination.AddAtom(sourceAtom);
                }

                foreach (Bond sourceBond in source.Bonds)
                {
                    sourceBond.Parent = destination;
                    destination.AddBond(sourceBond);
                }
            }
        }

        /// <summary>
        ///splits a molecule into two smaller ones but leaves original intact
        /// </summary>
        /// <param name="bond">Bond at which to split the molecule</param>
        /// <returns></returns>
        public (Molecule startMol, Molecule endMol) Split(Bond bond)
        {
            Atom startAtom = bond.StartAtom;
            Atom endAtom = bond.EndAtom;

            RemoveBond(bond);
            bond.Parent = null;

            var startAtoms = new HashSet<Atom>();
            var endAtoms = new HashSet<Atom>();
            Traverse(startAtom, atom => { startAtoms.Add(atom); }, a => !startAtoms.Contains(a));
            Traverse(endAtom, atom => { endAtoms.Add(atom); }, a => !endAtoms.Contains(a));

            Molecule startMol = IsolateMolecule(startAtoms);
            Molecule endMol = IsolateMolecule(endAtoms);

            return (startMol, endMol);

            //local function
            Molecule IsolateMolecule(HashSet<Atom> hashSet)
            {
                var molecule = new Molecule();
                foreach (Atom atom in hashSet)
                {
                    foreach (Atom atom1 in hashSet.Except(new[] { atom }))
                    {
                        Bond newBond;
                        if ((newBond = atom1.BondBetween(atom)) != null)
                        {
                            newBond.Parent = molecule;
                            molecule.AddBond(newBond);
                        }
                    }

                    atom.Parent = molecule;
                    molecule.AddAtom(atom);
                }

                return molecule;
            }
        }

        private static Atom NextUnprocessedAtom(Atom seed, Predicate<Atom> isntProcessed)
        {
            return seed.Neighbours.First(a => isntProcessed(a));
        }

        private static Atom NextUnprocessedAtom(Atom seed, Predicate<Atom> isntProcessed, HashSet<Bond> excludeBonds)
        {
            IEnumerable<Atom> unprocessedNeighbours = from a in seed.Neighbours
                                                      where isntProcessed(a) && !excludeBonds.Contains(seed.BondBetween(a))
                                                      select a;

            return unprocessedNeighbours.First();
        }

        /// <summary>
        /// Traverses a molecular graph applying an operation to each and every atom.
        /// Does not require that the atoms be already part of a Molecule.
        /// </summary>
        /// <param name="startAtom">start atom</param>
        /// <param name="operation">delegate pointing to operation to perform</param>
        /// <param name="isntProcessed"> Predicate test to tell us whether or not to process an atom</param>
        private void Traverse(Atom startAtom, Action<Atom> operation, Predicate<Atom> isntProcessed)
        {
            operation(startAtom);

            while (startAtom.UnprocessedDegree(isntProcessed) > 0)
            {
                if (startAtom.UnprocessedDegree(isntProcessed) == 1)
                {
                    startAtom = NextUnprocessedAtom(startAtom, isntProcessed);
                    operation(startAtom);
                }
                else
                {
                    IEnumerable<Atom> unassignedAtom = from a in startAtom.Neighbours
                                                       where isntProcessed(a)
                                                       select a;
                    foreach (Atom atom in unassignedAtom)
                    {
                        Traverse(atom, operation, isntProcessed);
                    }
                }
            }
        }

        /// <summary>
        /// Traverses a molecular graph applying an operation to each and every atom.
        /// Does not require that the atoms be already part of a Molecule.
        /// Overload allows list of bonds to be excluded;
        /// </summary>
        /// <param name="startAtom">start atom</param>
        /// <param name="operation">delegate pointing to operation to perform</param>
        /// <param name="isntProcessed"> Predicate test to tell us whether or not to process an atom</param>
        /// <param name="excludeBonds">List of bonds to exclude from traversion</param>
        public void Traverse(Atom startAtom, Action<Atom> operation, Predicate<Atom> isntProcessed,
                             HashSet<Bond> excludeBonds)
        {
            operation(startAtom);

            while (startAtom.UnprocessedDegree(isntProcessed, excludeBonds) > 0)
            {
                if (startAtom.UnprocessedDegree(isntProcessed, excludeBonds) == 1)
                {
                    Atom nextAtom = NextUnprocessedAtom(startAtom, isntProcessed, excludeBonds);

                    if (!excludeBonds.Contains(nextAtom.BondBetween(startAtom)))
                    {
                        operation(nextAtom);
                    }

                    Traverse(nextAtom, operation, isntProcessed, excludeBonds);
                }
                else
                {
                    IEnumerable<Atom> unassignedAtom = from a in startAtom.Neighbours
                                                       where isntProcessed(a) && !excludeBonds.Contains(startAtom.BondBetween(a))
                                                       select a;
                    foreach (Atom atom in unassignedAtom)
                    {
                        Traverse(atom, operation, isntProcessed, excludeBonds);
                    }
                }
            }
        }

        /// <summary>
        /// Traverses a molecular graph applying an operation to each and every atom.
        /// Uses breadth-first searching
        /// Does not require that the atoms be already part of a Molecule.
        /// Overload allows list of bonds to be excluded;
        /// </summary>
        /// <param name="startAtom">start atom</param>
        /// <param name="operation">delegate pointing to operation to perform</param>
        /// <param name="isntProcessed"> Predicate test to tell us whether or not to process an atom</param>
        /// <param name="excludeBonds">Optional ist of bonds to exclude from traversion</param>
        public void TraverseBFS(Atom startAtom, Action<Atom> operation, Predicate<Atom> isntProcessed,
                                HashSet<Bond> excludeBonds = null)
        {
            if (excludeBonds == null) //then create an empty exclusion set
            {
                excludeBonds = new HashSet<Bond>();
            }

            var toDo = new Queue<Atom>();

            toDo.Enqueue(startAtom);
            Atom next = null;
            while (toDo.Count > 0)
            {
                next = toDo.Dequeue();

                operation(next);

                IEnumerable<Atom> neighbours = from a in next.Neighbours
                                               where isntProcessed(a) && !excludeBonds.Contains(next.BondBetween(a))
                                               select a;
                foreach (Atom atom in neighbours)
                {
                    toDo.Enqueue(atom);
                }
            }
        }

        public void Reparent()
        {
            foreach (Atom atom in Atoms.Values)
            {
                atom.Parent = this;
            }

            foreach (Bond bond in Bonds)
            {
                bond.Parent = this;
            }
        }

        public string GetGroupKey()
        {
            return "G" + InternalId;
        }
    }
}