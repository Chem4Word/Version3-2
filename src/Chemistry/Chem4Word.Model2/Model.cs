// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2.Enums;
using Chem4Word.Model2.Helpers;
using Chem4Word.Model2.Interfaces;

namespace Chem4Word.Model2
{
    public class Model : IChemistryContainer, INotifyPropertyChanged
    {
        #region Fields

        public event NotifyCollectionChangedEventHandler AtomsChanged;

        public event NotifyCollectionChangedEventHandler BondsChanged;

        public event NotifyCollectionChangedEventHandler MoleculesChanged;

        public event NotifyCollectionChangedEventHandler ReactionSchemesChanged;

        public event NotifyCollectionChangedEventHandler ReactionsChanged;

        public event NotifyCollectionChangedEventHandler AnnotationsChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion Fields

        #region Event handlers

        private void UpdateMoleculeEventHandlers(NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var oldItem in e.OldItems)
                {
                    var mol = ((Molecule)oldItem);
                    mol.AtomsChanged -= Atoms_CollectionChanged;
                    mol.BondsChanged -= Bonds_CollectionChanged;
                    mol.PropertyChanged -= ChemObject_PropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (var newItem in e.NewItems)
                {
                    var mol = ((Molecule)newItem);
                    mol.AtomsChanged += Atoms_CollectionChanged;
                    mol.BondsChanged += Bonds_CollectionChanged;
                    mol.PropertyChanged += ChemObject_PropertyChanged;
                }
            }
        }

        private void OnMoleculesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!InhibitEvents)
            {
                var temp = MoleculesChanged;
                if (temp != null)
                {
                    temp.Invoke(sender, e);
                }
            }
        }

        //responds to a property being changed on an object
        private void ChemObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(sender, e);
        }

        //transmits the property being changed on an object
        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!InhibitEvents)
            {
                var temp = PropertyChanged;
                if (temp != null)
                {
                    temp.Invoke(sender, e);
                }
            }
        }

        public Annotation AddAnnotation(Annotation newAnnotation)
        {
            _annotations[newAnnotation.InternalId] = newAnnotation;
            NotifyCollectionChangedEventArgs e =
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<Annotation> { newAnnotation });
            UpdateAnnotationsEventHandlers(e);
            OnAnnotationsChanged(this, e);
            return newAnnotation;
        }

        private void Annotations_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnAnnotationsChanged(sender, e);
        }

        private void OnAnnotationsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!InhibitEvents)
            {
                var temp = AnnotationsChanged;
                if (temp != null)
                {
                    temp.Invoke(sender, e);
                }
            }
        }

        public void RemoveAnnotation(Annotation annotation)
        {
            _annotations.Remove(annotation.InternalId);
            NotifyCollectionChangedEventArgs e =
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
                    new List<Annotation> { annotation });
            UpdateAnnotationsEventHandlers(e);
            OnAnnotationsChanged(this, e);
        }

        //responds to bonds being added or removed
        private void Bonds_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnBondsChanged(sender, e);
        }

        //transmits bonds being added or removed
        private void OnBondsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!InhibitEvents)
            {
                var temp = BondsChanged;
                if (temp != null)
                {
                    temp.Invoke(sender, e);
                }
            }
        }

        //responds to atoms being added or removed
        private void Atoms_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnAtomsChanged(sender, e);
        }

        //transmits atoms being added or removed
        private void OnAtomsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!InhibitEvents)
            {
                var temp = AtomsChanged;
                if (temp != null)
                {
                    temp.Invoke(sender, e);
                }
            }
        }

        #endregion Event handlers

        #region Properties

        public bool InhibitEvents { get; set; }

        public Dictionary<string, CrossedBonds> CrossedBonds { get; set; } = new Dictionary<string, CrossedBonds>();

        /// <summary>
        /// True if this model has any reactions
        /// </summary>
        public bool HasReactions => ReactionSchemes.Count > 0;

        /// <summary>
        /// True if this model has functional groups
        /// </summary>
        public bool HasFunctionalGroups
        {
            get
            {
                bool result = false;

                var allAtoms = GetAllAtoms();

                foreach (var atom in allAtoms)
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
        /// True if this model has nested molecules
        /// </summary>
        public bool HasNestedMolecules
        {
            get
            {
                bool result = false;

                foreach (var child in Molecules.Values)
                {
                    if (child.Molecules.Count > 0)
                    {
                        result = true;
                        break;
                    }
                }

                return result;
            }
        }

        public List<TextualProperty> AllTextualProperties
        {
            get
            {
                var list = new List<TextualProperty>();

                // Add 2D if relevant
                if (TotalAtomsCount > 0)
                {
                    list.Add(new TextualProperty
                    {
                        Id = "2D",
                        TypeCode = "2D",
                        FullType = "2D",
                        Value = "2D"
                    });
                    list.Add(new TextualProperty
                    {
                        Id = "c0",
                        TypeCode = "F",
                        FullType = "ConciseFormula",
                        Value = ConciseFormula
                    });
                    list.Add(new TextualProperty
                    {
                        Id = "S",
                        TypeCode = "S",
                        FullType = "Separator",
                        Value = "S"
                    });
                }

                foreach (var child in Molecules.Values)
                {
                    list.AddRange(child.AllTextualProperties);
                }

                if (list.Count > 0)
                {
                    list = list.Take(list.Count - 1).ToList();
                }

                return list;
            }
        }

        /// <summary>
        /// Count of atoms in all molecules
        /// </summary>
        public int TotalAtomsCount
        {
            get
            {
                int count = 0;

                foreach (var molecule in Molecules.Values)
                {
                    count += molecule.AtomCount;
                }

                return count;
            }
        }

        /// <summary>
        /// Lowest atomic number of any atom (if element) in all molecules
        /// </summary>
        public int MinAtomicNumber
        {
            get
            {
                // This number is used because it is higher than the Maximum value in the periodic table
                int min = 255;

                var allAtoms = GetAllAtoms();

                foreach (var atom in allAtoms)
                {
                    if (atom.Element is Element e)
                    {
                        min = Math.Min(min, e.AtomicNumber);
                    }
                }

                return min;
            }
        }

        /// <summary>
        /// Highest atomic number of any atom (if element) in all molecules
        /// </summary>
        public int MaxAtomicNumber
        {
            get
            {
                int max = 0;

                var allAtoms = GetAllAtoms();

                foreach (var atom in allAtoms)
                {
                    if (atom.Element is Element e)
                    {
                        max = Math.Max(max, e.AtomicNumber);
                    }
                }

                return max;
            }
        }

        public int TotalMoleculesCount
        {
            get
            {
                return GetAllMolecules().Count;
            }
        }

        /// <summary>
        /// Count of bonds in all molecules
        /// </summary>
        public int TotalBondsCount
        {
            get
            {
                int count = 0;

                foreach (var molecule in Molecules.Values)
                {
                    count += molecule.BondCount;
                }

                return count;
            }
        }

        /// <summary>
        /// Average bond length of all molecules
        /// </summary>
        public double MeanBondLength
        {
            get
            {
                double result = 0.0;
                List<double> lengths = new List<double>();

                foreach (var mol in Molecules.Values)
                {
                    lengths.AddRange(mol.BondLengths);
                }

                if (lengths.Any())
                {
                    result = lengths.Average();
                }
                else
                {
                    if (ScaledForXaml)
                    {
                        result = XamlBondLength;
                    }
                }

                return result;
            }
        }

        public double MolecularWeight
        {
            get
            {
                double weight = 0;

                foreach (var atom in GetAllAtoms())
                {
                    weight += atom.Element.AtomicWeight;
                }

                return weight;
            }
        }

        /// <summary>
        /// Bond length used in Xaml
        /// </summary>
        public double XamlBondLength { get; internal set; }

        /// <summary>
        /// Overall Cml bounding box for all atoms
        /// </summary>
        public Rect BoundingBoxOfCmlPoints
        {
            get
            {
                Rect boundingBox = Rect.Empty;

                foreach (var mol in Molecules.Values)
                {
                    boundingBox.Union(mol.BoundingBox);
                }

                foreach (ReactionScheme scheme in ReactionSchemes.Values)
                {
                    foreach (Reaction reaction in scheme.Reactions.Values)
                    {
                        Rect reactionRect = new Rect(reaction.TailPoint, reaction.HeadPoint);
                        boundingBox.Union(reactionRect);
                    }
                }

                foreach (var ann in Annotations.Values)
                {
                    // Use a very small rectangle here
                    Rect rectangle = new Rect(ann.Position, new Size(0.1, 0.1));
                    boundingBox.Union(rectangle);
                }

                return boundingBox;
            }
        }

        public string Path => "/";
        public IChemistryContainer Root => null;

        public bool ScaledForXaml { get; set; }

        private Rect _boundingBox = Rect.Empty;

        public double MinX => BoundingBoxWithFontSize.Left;
        public double MaxX => BoundingBoxWithFontSize.Right;
        public double MinY => BoundingBoxWithFontSize.Top;
        public double MaxY => BoundingBoxWithFontSize.Bottom;

        /// <summary>
        /// Overall bounding box for all objects allowing for Font Size
        /// </summary>
        public Rect BoundingBoxWithFontSize
        {
            get
            {
                if (_boundingBox == Rect.Empty)
                {
                    var allAtoms = GetAllAtoms();

                    Rect boundingBox = Rect.Empty;

                    if (allAtoms.Count > 0)
                    {
                        boundingBox = allAtoms[0].BoundingBox(FontSize);
                        for (int i = 1; i < allAtoms.Count; i++)
                        {
                            var atom = allAtoms[i];
                            boundingBox.Union(atom.BoundingBox(FontSize));
                        }
                    }

                    foreach (ReactionScheme scheme in ReactionSchemes.Values)
                    {
                        foreach (Reaction reaction in scheme.Reactions.Values)
                        {
                            Rect reactionRect = new Rect(reaction.TailPoint, reaction.HeadPoint);
                            boundingBox.Union(reactionRect);
                        }
                    }

                    foreach (Annotation ann in Annotations.Values)
                    {
                        boundingBox.Union(ann.BoundingBox(FontSize));
                    }

                    _boundingBox = boundingBox;
                }

                return _boundingBox;
            }
        }

        /// <summary>
        /// Font size used for Xaml
        /// </summary>
        public double FontSize
        {
            get
            {
                var allBonds = GetAllBonds();
                double fontSize = Globals.DefaultFontSize * Globals.ScaleFactorForXaml;

                if (allBonds.Any())
                {
                    fontSize = XamlBondLength * Globals.FontSizePercentageBond;
                }

                return fontSize;
            }
        }

        private readonly Dictionary<Guid, Molecule> _molecules;

        //wraps up the above Molecules collection
        public ReadOnlyDictionary<Guid, Molecule> Molecules;

        private readonly Dictionary<Guid, ReactionScheme> _reactionSchemes;
        public ReadOnlyDictionary<Guid, ReactionScheme> ReactionSchemes;

        private readonly Dictionary<Guid, Annotation> _annotations;
        public ReadOnlyDictionary<Guid, Annotation> Annotations;
        public string CustomXmlPartGuid { get; set; }

        public List<string> GeneralErrors { get; set; }

        public void SetXamlBondLength(int bondLength)
        {
            XamlBondLength = bondLength;
        }

        /// <summary>
        /// List of all warnings encountered during the import from external file format
        /// </summary>
        public List<string> AllWarnings
        {
            get
            {
                var list = new List<string>();
                foreach (var molecule in Molecules.Values)
                {
                    list.AddRange(molecule.Warnings);
                }

                return list;
            }
        }

        /// <summary>
        /// List of all errors encountered during the import from external file format
        /// </summary>
        public List<string> AllErrors
        {
            get
            {
                var list = new List<string>();
                foreach (var molecule in Molecules.Values)
                {
                    list.AddRange(molecule.Errors);
                }

                return list;
            }
        }

        private Dictionary<string, int> _calculatedFormulas;

        /// <summary>
        /// Concise formula for the model
        /// </summary>
        public string ConciseFormula
        {
            get
            {
                if (_calculatedFormulas == null)
                {
                    _calculatedFormulas = new Dictionary<string, int>();
                    GatherFormulas(Molecules.Values.ToList());
                }

                return CalculatedFormulaAsString();
            }
        }

        private void GatherFormulas(List<Molecule> molecules)
        {
            foreach (var molecule in molecules)
            {
                if (molecule.Atoms.Count > 0)
                {
                    // Add into running totals
                    if (_calculatedFormulas.ContainsKey(molecule.ConciseFormula))
                    {
                        _calculatedFormulas[molecule.ConciseFormula]++;
                    }
                    else
                    {
                        _calculatedFormulas.Add(molecule.ConciseFormula, 1);
                    }
                }
                else
                {
                    // Gather the formulas of the children
                    var children = new List<string>();
                    foreach (var childMolecule in molecule.Molecules.Values.ToList())
                    {
                        children.Add(childMolecule.ConciseFormula);
                    }

                    // Add Brackets and join using Bullet character <Alt>0183
                    var combined = "[" + string.Join(" · ", children) + "]";

                    // Add charge here
                    if (molecule.FormalCharge != null)
                    {
                        var charge = molecule.FormalCharge.Value;
                        var absCharge = Math.Abs(charge);

                        if (charge > 0)
                        {
                            combined += $" + {absCharge}";
                        }
                        if (charge < 0)
                        {
                            combined += $" - {absCharge}";
                        }
                    }

                    // Add combined value into running totals
                    if (_calculatedFormulas.ContainsKey(combined))
                    {
                        _calculatedFormulas[combined]++;
                    }
                    else
                    {
                        _calculatedFormulas.Add(combined, 1);
                    }
                }
            }
        }

        private string CalculatedFormulaAsString()
        {
            var strings = new List<string>();
            foreach (var calculatedFormula in _calculatedFormulas)
            {
                if (calculatedFormula.Value > 1)
                {
                    strings.Add($"{calculatedFormula.Value} {calculatedFormula.Key}");
                }
                else
                {
                    strings.Add(calculatedFormula.Key);
                }
            }

            // Join using Bullet character <Alt>0183
            return string.Join(" · ", strings);
        }

        public ReactionScheme DefaultReactionScheme
        {
            get
            {
                if (!ReactionSchemes.Any())
                {
                    var rs = new ReactionScheme();
                    AddReactionScheme(rs);
                }
                return ReactionSchemes.Values.First();
            }
        }

        #endregion Properties

        #region Constructors

        public Model()
        {
            _molecules = new Dictionary<Guid, Molecule>();
            Molecules = new ReadOnlyDictionary<Guid, Molecule>(_molecules);

            _reactionSchemes = new Dictionary<Guid, ReactionScheme>();
            ReactionSchemes = new ReadOnlyDictionary<Guid, ReactionScheme>(_reactionSchemes);

            _annotations = new Dictionary<Guid, Annotation>();
            Annotations = new ReadOnlyDictionary<Guid, Annotation>(_annotations);

            GeneralErrors = new List<string>();
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Drags all Atoms back to the origin by the specified offset
        /// </summary>
        /// <param name="x">X offset</param>
        /// <param name="y">Y offset</param>
        public void RepositionAll(double x, double y)
        {
            foreach (Molecule molecule in Molecules.Values)
            {
                molecule.RepositionAll(x, y);
            }
            foreach (ReactionScheme rs in ReactionSchemes.Values)
            {
                rs.RepositionAll(x, y);
            }
            foreach (Annotation annotation in Annotations.Values)
            {
                annotation.RepositionAll(x, y);
            }
            _boundingBox = Rect.Empty;
        }

        public void CenterOn(Point point)
        {
            Rect boundingBox = BoundingBoxWithFontSize;
            Point midPoint = new Point(boundingBox.Left + boundingBox.Width / 2, boundingBox.Top + boundingBox.Height / 2);
            Vector displacement = midPoint - point;
            RepositionAll(displacement.X, displacement.Y);
        }

        public bool RemoveMolecule(Molecule mol)
        {
            var res = _molecules.Remove(mol.InternalId);
            if (res)
            {
                NotifyCollectionChangedEventArgs e =
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
                        new List<Molecule> { mol });
                OnMoleculesChanged(this, e);
                UpdateMoleculeEventHandlers(e);
            }

            return res;
        }

        public Molecule AddMolecule(Molecule newMol)
        {
            _molecules[newMol.InternalId] = newMol;
            NotifyCollectionChangedEventArgs e =
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,
                    new List<Molecule> { newMol });
            UpdateMoleculeEventHandlers(e);
            OnMoleculesChanged(this, e);
            return newMol;
        }

        public void SetProtectedLabels(List<string> protectedLabels)
        {
            foreach (Molecule m in Molecules.Values)
            {
                m.SetProtectedLabels(protectedLabels);
            }
        }

        public void ReLabelGuids()
        {
            int bondCount = 0;
            int atomCount = 0;
            int molCount = 0;
            int reactionSchemeCount = 0;
            int reactionCount = 0;
            int annotationCount = 0;

            foreach (var molecule in GetAllMolecules())
            {
                var number = molecule.Id.Substring(1);
                int n;
                if (int.TryParse(number, out n))
                {
                    molCount = Math.Max(molCount, n);
                }
            }

            foreach (var atom in GetAllAtoms())
            {
                var number = atom.Id.Substring(1);
                int n;
                if (int.TryParse(number, out n))
                {
                    atomCount = Math.Max(atomCount, n);
                }
            }

            foreach (var bond in GetAllBonds())
            {
                var number = bond.Id.Substring(1);
                int n;
                if (int.TryParse(number, out n))
                {
                    bondCount = Math.Max(bondCount, n);
                }
            }

            foreach (ReactionScheme scheme in GetAllReactionSchemes())
            {
                var number = scheme.Id.Substring(1);
                int n;
                if (int.TryParse(number, out n))
                {
                    reactionSchemeCount = Math.Max(reactionSchemeCount, n);
                }
            }

            foreach (Reaction reaction in GetAllReactions())
            {
                var number = reaction.Id.Substring(1);
                int n;
                if (int.TryParse(number, out n))
                {
                    reactionCount = Math.Max(reactionCount, n);
                }
            }

            foreach (Molecule m in Molecules.Values)
            {
                m.ReLabelGuids(ref molCount, ref atomCount, ref bondCount);
            }

            foreach (ReactionScheme rs in ReactionSchemes.Values)
            {
                rs.ReLabelGuids(ref reactionSchemeCount, ref reactionCount);
            }

            foreach (Annotation an in Annotations.Values)
            {
                an.ReLabelGuids(ref annotationCount);
            }
        }

        private IEnumerable<Reaction> GetAllReactions()
        {
            var reactions = from rs in GetAllReactionSchemes()
                            from r in rs.Reactions.Values
                            select r;
            return reactions;
        }

        private IEnumerable<ReactionScheme> GetAllReactionSchemes()
        {
            var schemes = from rs in ReactionSchemes.Values
                          select rs;
            return schemes;
        }

        public void Relabel(bool includeNames)
        {
            int bondCount = 0;
            int atomCount = 0;
            int molCount = 0;
            int reactionSchemeCount = 0;
            int reactionCount = 0;
            int annotationCount = 0;

            foreach (Molecule m in Molecules.Values)
            {
                m.ReLabel(ref molCount, ref atomCount, ref bondCount, includeNames);
            }

            foreach (ReactionScheme scheme in ReactionSchemes.Values)
            {
                scheme.ReLabel(ref reactionSchemeCount, ref reactionCount);
            }

            foreach (Annotation annotation in Annotations.Values)
            {
                annotation.ReLabel(ref annotationCount);
            }
        }

        public void Refresh()
        {
            foreach (var molecule in Molecules.Values)
            {
                molecule.Refresh();
            }
        }

        public Model Copy()
        {
            Model modelCopy = new Model();

            foreach (var child in Molecules.Values)
            {
                Molecule molCopy = child.Copy();
                modelCopy.AddMolecule(molCopy);
                molCopy.Parent = modelCopy;
            }

            foreach (var rs in ReactionSchemes.Values)
            {
                ReactionScheme schemeCopy = rs.Copy(modelCopy);
                modelCopy.AddReactionScheme(schemeCopy);
                schemeCopy.Parent = modelCopy;
            }

            foreach (var ann in Annotations.Values)
            {
                Annotation annCopy = ann.Copy();
                modelCopy.AddAnnotation(annCopy);
                annCopy.Parent = modelCopy;
            }
            modelCopy.ScaledForXaml = ScaledForXaml;
            modelCopy.CustomXmlPartGuid = CustomXmlPartGuid;

            return modelCopy;
        }

        private void ClearMolecules()
        {
            _molecules.Clear();
        }

        public void RemoveExplicitHydrogens()
        {
            var targets = GetHydrogenTargets();

            if (targets.Atoms.Any())
            {
                foreach (var bond in targets.Bonds)
                {
                    bond.Parent.RemoveBond(bond);
                }
                foreach (var atom in targets.Atoms)
                {
                    atom.Parent.RemoveAtom(atom);
                }
            }
        }

        public HydrogenTargets GetHydrogenTargets(List<Molecule> molecules = null)
        {
            var targets = new HydrogenTargets();

            if (molecules == null)
            {
                var allHydrogens = GetAllAtoms().Where(a => a.Element.Symbol.Equals("H")).ToList();
                ProcessHydrogens(allHydrogens);
            }
            else
            {
                foreach (var mol in molecules)
                {
                    var allHydrogens = mol.Atoms.Values.Where(a => a.Element.Symbol.Equals("H")).ToList();
                    ProcessHydrogens(allHydrogens);
                }
            }

            return targets;

            // Local function
            void ProcessHydrogens(List<Atom> hydrogens)
            {
                if (hydrogens.Any())
                {
                    foreach (var hydrogen in hydrogens)
                    {
                        // Terminal Atom?
                        if (hydrogen.Degree == 1)
                        {
                            // Not Stereo
                            if (hydrogen.Bonds.First().Stereo == BondStereo.None)
                            {
                                if (!targets.Molecules.ContainsKey(hydrogen.InternalId))
                                {
                                    targets.Molecules.Add(hydrogen.InternalId, hydrogen.Parent);
                                }
                                targets.Atoms.Add(hydrogen);
                                if (!targets.Molecules.ContainsKey(hydrogen.Bonds.First().InternalId))
                                {
                                    targets.Molecules.Add(hydrogen.Bonds.First().InternalId, hydrogen.Parent);
                                }
                                targets.Bonds.Add(hydrogen.Bonds.First());
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Ensure that bond length is between 5 and 95, and force to default if required
        /// </summary>
        /// <param name="target">Target bond length (if force true)</param>
        /// <param name="force">true to force setting of bond length to target bond length</param>
        public string EnsureBondLength(double target, bool force)
        {
            string result = string.Empty;

            if (TotalBondsCount > 0 && MeanBondLength > 0)
            {
                if (Math.Abs(MeanBondLength - target) < 0.1)
                {
                    result = string.Empty;
                }
                else
                {
                    if (force)
                    {
                        result = $"Forced BondLength from {SafeDouble.AsString(MeanBondLength)} to {SafeDouble.AsString(target)}";
                        ScaleToAverageBondLength(target);
                    }
                    else
                    {
                        if (MeanBondLength < Constants.MinimumBondLength - Constants.BondLengthTolerance
                            || MeanBondLength > Constants.MaximumBondLength + Constants.BondLengthTolerance)
                        {
                            result = $"Adjusted BondLength from {SafeDouble.AsString(MeanBondLength)} to {SafeDouble.AsString(target)}";
                            ScaleToAverageBondLength(target);
                        }
                        else
                        {
                            result = $"BondLength of {SafeDouble.AsString(MeanBondLength)} is within tolerance";
                        }
                    }
                }
            }

            return result;
        }

        public void ScaleToAverageBondLength(double newLength, Point centre)
        {
            if ((TotalBondsCount + DefaultReactionScheme.Reactions.Count) > 0 && MeanBondLength > 0)
            {
                ScaleToAverageBondLength(newLength);

                var bb = BoundingBoxWithFontSize;
                var c = new Point(bb.Left + bb.Width / 2, bb.Top + bb.Height / 2);
                RepositionAll(c.X - centre.X, c.Y - centre.Y);
                _boundingBox = Rect.Empty;
            }

            if (ScaledForXaml)
            {
                XamlBondLength = newLength;
            }
        }

        public void ScaleToAverageBondLength(double newLength)
        {
            if (TotalBondsCount > 0 && MeanBondLength > 0)
            {
                double scale = newLength / MeanBondLength;
                var allAtoms = GetAllAtoms();

                foreach (var atom in allAtoms)
                {
                    atom.Position = new Point(atom.Position.X * scale, atom.Position.Y * scale);
                }

                foreach (var scheme in ReactionSchemes.Values)
                {
                    foreach (var reaction in scheme.Reactions.Values)
                    {
                        reaction.TailPoint = new Point(reaction.TailPoint.X * scale, reaction.TailPoint.Y * scale);
                        reaction.HeadPoint = new Point(reaction.HeadPoint.X * scale, reaction.HeadPoint.Y * scale);
                    }
                }

                foreach (Annotation annotation in Annotations.Values)
                {
                    annotation.Position = new Point(annotation.Position.X * scale, annotation.Position.Y * scale);
                }

                _boundingBox = Rect.Empty;
            }
        }

        public List<Atom> GetAllAtoms()
        {
            List<Atom> allAtoms = new List<Atom>();
            foreach (Molecule mol in Molecules.Values)
            {
                mol.BuildAtomList(allAtoms);
            }

            return allAtoms;
        }

        public List<Bond> GetAllBonds()
        {
            List<Bond> allBonds = new List<Bond>();
            foreach (Molecule mol in Molecules.Values)
            {
                mol.BuildBondList(allBonds);
            }

            return allBonds;
        }

        public void SetAnyMissingNameIds()
        {
            foreach (Molecule m in Molecules.Values)
            {
                m.SetAnyMissingNameIds();
            }
        }

        public TextualProperty GetTextPropertyById(string id)
        {
            TextualProperty tp = null;

            foreach (var molecule in GetAllMolecules())
            {
                if (id.StartsWith(molecule.Id))
                {
                    if (id.EndsWith("f0"))
                    {
                        tp = new TextualProperty
                        {
                            Id = $"{molecule.Id}.f0",
                            Value = molecule.ConciseFormula,
                            FullType = "ConciseFormula"
                        };
                        break;
                    }

                    tp = molecule.Formulas.SingleOrDefault(f => f.Id.Equals(id));
                    if (tp != null)
                    {
                        break;
                    }

                    tp = molecule.Names.SingleOrDefault(n => n.Id.Equals(id));
                    if (tp != null)
                    {
                        break;
                    }
                }
            }

            return tp;
        }

        public List<Molecule> GetAllMolecules()
        {
            List<Molecule> allMolecules = new List<Molecule>();
            foreach (Molecule mol in Molecules.Values)
            {
                mol.BuildMolList(allMolecules);
            }

            return allMolecules;
        }

        public void RescaleForCml()
        {
            if (ScaledForXaml)
            {
                double newLength = Constants.StandardBondLength / Globals.ScaleFactorForXaml;

                if (TotalBondsCount > 0 && MeanBondLength > 0)
                {
                    newLength = MeanBondLength / Globals.ScaleFactorForXaml;
                }

                ScaleToAverageBondLength(newLength);

                ScaledForXaml = false;
            }
        }

        public void RescaleForXaml(bool forDisplay, double preferredBondLength)
        {
            if (!ScaledForXaml)
            {
                double newLength;

                if (TotalBondsCount > 0 && MeanBondLength > 0)
                {
                    newLength = MeanBondLength * Globals.ScaleFactorForXaml;
                }
                else
                {
                    newLength = preferredBondLength * Globals.ScaleFactorForXaml;
                }

                ScaleToAverageBondLength(newLength);
                XamlBondLength = newLength;
                ScaledForXaml = true;

                var middle = BoundingBoxOfCmlPoints;

                if (forDisplay)
                {
                    // Move to (0,0)
                    RepositionAll(middle.Left, middle.Top);
                }

                OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(BoundingBoxWithFontSize)));
                OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(XamlBondLength)));
            }
        }

        /// <summary>
        /// Checks to make sure the internals of the molecule haven't become busted up.
        /// </summary>
        public List<string> CheckIntegrity()
        {
            var mols = GetAllMolecules();
            var result = new List<string>();

            foreach (Molecule mol in mols)
            {
                result.AddRange(mol.CheckIntegrity());
            }

            var atoms = GetAllAtoms().ToList();
            foreach (var atom in atoms)
            {
                var matches = atoms.Where(a => a.Id != atom.Id && SamePoint(atom.Position, a.Position, MeanBondLength * Globals.BondOffsetPercentage)).ToList();
                if (matches.Any())
                {
                    var plural = matches.Count > 1 ? "s" : "";
                    var clashes = matches.Select(a => a.Id);
                    result.Add($"Atom {atom.Id} - {atom.Element.Symbol} @ {PointHelper.AsString(atom.Position)} clashes with atom{plural} {string.Join(",", clashes)}");
                }
            }

            // Local Function
            bool SamePoint(Point a, Point b, double tolerance)
            {
                bool samePoint;

                if (a.Equals(b))
                {
                    samePoint = true;
                }
                else
                {
                    Vector v = a - b;
                    samePoint = v.Length <= tolerance;
                }

                return samePoint;
            }

            return result;
        }

        public void CentreInCanvas(Size size)
        {
            // Re-Centre scaled drawing on Canvas, does not need to be undone
            double desiredLeft = (size.Width - BoundingBoxWithFontSize.Width) / 2.0;
            double desiredTop = (size.Height - BoundingBoxWithFontSize.Height) / 2.0;
            double offsetLeft = BoundingBoxWithFontSize.Left - desiredLeft;
            double offsetTop = BoundingBoxWithFontSize.Top - desiredTop;

            RepositionAll(offsetLeft, offsetTop);
        }

        public ReactionScheme AddReactionScheme(ReactionScheme newScheme)
        {
            _reactionSchemes[newScheme.InternalId] = newScheme;
            NotifyCollectionChangedEventArgs e =
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,
                    new List<ReactionScheme> { newScheme });
            UpdateReactionSchemeEventHandlers(e);
            OnReactionSchemesChanged(this, e);
            return newScheme;
        }

        private void OnReactionSchemesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!InhibitEvents)
            {
                var temp = ReactionSchemesChanged;
                if (temp != null)
                {
                    temp.Invoke(sender, e);
                }
            }
        }

        private void UpdateReactionSchemeEventHandlers(NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var oldItem in e.OldItems)
                {
                    var rs = ((ReactionScheme)oldItem);
                    rs.ReactionsChanged -= Reactions_CollectionChanged;
                    rs.PropertyChanged -= ChemObject_PropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (var newItem in e.NewItems)
                {
                    var rs = ((ReactionScheme)newItem);
                    rs.ReactionsChanged += Reactions_CollectionChanged;
                    rs.PropertyChanged += ChemObject_PropertyChanged;
                }
            }
        }

        private void UpdateAnnotationsEventHandlers(NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var oldItem in e.OldItems)
                {
                    var ann = ((Annotation)oldItem);

                    ann.PropertyChanged -= ChemObject_PropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (var newItem in e.NewItems)
                {
                    var ann = ((Annotation)newItem);
                    ann.PropertyChanged += ChemObject_PropertyChanged;
                }
            }
        }

        private void Reactions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnReactionsChanged(sender, e);
        }

        private void OnReactionsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!InhibitEvents)
            {
                var temp = ReactionsChanged;
                if (temp != null)
                {
                    temp.Invoke(sender, e);
                }
            }
        }

        public void RemoveReactionScheme(ReactionScheme scheme)
        {
            _reactionSchemes.Remove(scheme.InternalId);
            NotifyCollectionChangedEventArgs e =
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
                    new List<ReactionScheme> { scheme });
            UpdateReactionSchemeEventHandlers(e);
            OnReactionSchemesChanged(this, e);
        }

        /// <summary>
        /// Detects which bond lines are crossing.
        /// Uses a variation of Bentley–Ottmann algorithm
        /// </summary>
        public void DetectCrossingLines()
        {
            CrossedBonds = new Dictionary<string, CrossedBonds>();

            foreach (var molecule in Molecules.Values)
            {
                DetectCrossingLines(molecule);
            }
        }

        private void DetectCrossingLines(Molecule molecule)
        {
            var clippingTargets = new List<ClippingTarget>();

            // Step 1 - Fill list with simple facilitating class
            foreach (var bond in molecule.Bonds)
            {
                clippingTargets.Add(new ClippingTarget(bond));
            }

            // Step 2 - Sort the list of bonds by smallest X co-ordinate value
            clippingTargets.Sort();

            // Step 3 - Do the sweep
            foreach (var clippingTarget in clippingTargets)
            {
                // Determine if the bounding box of this line intersects with the next one
                var targets = clippingTargets
                              .Where(a => a.BoundingBox.IntersectsWith(clippingTarget.BoundingBox))
                              .ToList();
                targets.Remove(clippingTarget);

                if (targets.Count > 0)
                {
                    // If any targets found
                    foreach (var target in targets)
                    {
                        var intersection = GeometryTool.GetIntersection(clippingTarget.Start, clippingTarget.End,
                                                                              target.Start, target.End);
                        if (intersection != null)
                        {
                            if (!PointIsAtEndOfALine(intersection.Value, clippingTarget, target))
                            {
                                // Construct key
                                var names = new List<string>
                                            {
                                                target.Name,
                                                clippingTarget.Name
                                            };
                                names.Sort(); // Alphabetically
                                var key = string.Join("|", names);

                                if (!CrossedBonds.ContainsKey(key))
                                {
                                    // Only add it if it's not been seen before
                                    CrossedBonds.Add(key, new CrossedBonds(target.Bond, clippingTarget.Bond, intersection.Value));
                                }
                            }
                            else
                            {
                                // Ignore any false positive where the intersection is a line ending
                            }
                        }
                    }
                }
            }

            // Finally recurse into any child molecules
            foreach (var child in molecule.Molecules.Values)
            {
                DetectCrossingLines(child);
            }
        }

        // Detect if a point is at any end of two lines
        private bool PointIsAtEndOfALine(Point point, ClippingTarget line1, ClippingTarget line2)
        {
            return point.Equals(line1.Start) || point.Equals(line1.End) || point.Equals(line2.Start) || point.Equals(line2.End);
        }

        #endregion Methods
    }
}