// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Chem4Word.ACME.Adorners.Selectors;
using Chem4Word.ACME.Behaviors;
using Chem4Word.ACME.Commands;
using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Drawing;
using Chem4Word.ACME.Enums;
using Chem4Word.ACME.Models;
using Chem4Word.ACME.Utils;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Wpf;
using Chem4Word.Model2;
using Chem4Word.Model2.Annotations;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Geometry;
using Chem4Word.Model2.Helpers;
using IChem4Word.Contracts;
using static Chem4Word.Model2.Helpers.Globals;
using Constants = Chem4Word.ACME.Resources.Constants;

namespace Chem4Word.ACME
{
    public class EditViewModel : ViewModel, INotifyPropertyChanged
    {
        private static readonly string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static readonly string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        #region Fields

        public readonly Dictionary<object, Adorner> SelectionAdorners = new Dictionary<object, Adorner>();
        public MultiAtomBondAdorner MultiAdorner { get; private set; }
        private Dictionary<int, BondOption> _bondOptions = new Dictionary<int, BondOption>();
        private int? _selectedBondOptionId;

        public AcmeOptions EditorOptions { get; set; }
        public IChem4WordTelemetry Telemetry { get; set; }

        #endregion Fields

        #region Properties

        public bool Loading { get; set; }

        public string CurrentBondOrder
        {
            get { return _bondOptions[_selectedBondOptionId.Value].Order; }
        }

        public BondStereo CurrentStereo
        {
            get { return _bondOptions[_selectedBondOptionId.Value].Stereo.Value; }
        }

        public double EditBondThickness
        {
            get { return BondThickness * DefaultBondLineFactor; }
        }

        public SelectionTypeCode SelectionType
        {
            get
            {
                var result = SelectionTypeCode.None;

                if (SelectedItems.OfType<Atom>().Any())
                {
                    result |= SelectionTypeCode.Atom;
                }

                if (SelectedItems.OfType<Bond>().Any())
                {
                    result |= SelectionTypeCode.Bond;
                }

                if (SelectedItems.OfType<Molecule>().Any())
                {
                    result |= SelectionTypeCode.Molecule;
                }

                return result;
            }
        }

        private ObservableCollection<object> _selectedItems;
        public ReadOnlyObservableCollection<object> SelectedItems { get; }

        public UndoHandler UndoManager { get; }

        private double _currentBondLength;

        public double CurrentBondLength
        {
            get { return _currentBondLength; }
            set
            {
                _currentBondLength = value;
                OnPropertyChanged();
                var scaled = value * ScaleFactorForXaml;
                // Decide if we need to rescale to current drawing
                if (!Loading && Math.Abs(Model.MeanBondLength - scaled) > 2.5)
                {
                    SetAverageBondLength(scaled);
                }
            }
        }

        private ElementBase _selectedElement;

        public ElementBase SelectedElement
        {
            get
            {
                var selElements = SelectedElements();

                switch (selElements.Count)
                {
                    // Nothing selected, return last value selected
                    case 0:
                        return _selectedElement;

                    case 1:
                        return selElements[0];
                    // More than one selected !
                    default:
                        return null;
                }
            }
            set
            {
                _selectedElement = value;

                var selAtoms = SelectedItems.OfType<Atom>().ToList();
                if (value != null)
                {
                    SetElement(value, selAtoms);
                }

                OnPropertyChanged();
            }
        }

        /// <summary>
        /// returns a distinct list of selected elements
        /// </summary>
        private List<ElementBase> SelectedElements()
        {
            var singletons = from Molecule m in SelectedItems.OfType<Molecule>()
                             where m.Atoms.Count == 1
                             select m;

            var allSelAtoms = (from Atom a in SelectedItems.OfType<Atom>()
                               select a).Union<Atom>(
                                                    from Molecule m in singletons
                                                    from Atom a1 in m.Atoms.Values
                                                    select a1);
            var elements = (from selAtom in allSelAtoms
                            select selAtom.Element).Distinct();

            return elements.ToList();
        }

        public int? SelectedBondOptionId
        {
            get
            {
                var selectedBondTypes = (from bt in SelectedBondOptions()
                                         select bt.Id).Distinct().ToList();

                switch (selectedBondTypes.Count)
                {
                    // Nothing selected, return last value selected
                    case 0:
                        return _selectedBondOptionId;

                    case 1:
                        return selectedBondTypes[0];
                    // More than one selected !
                    default:
                        return null;
                }
            }
            set
            {
                _selectedBondOptionId = value;
                if (value != null)
                {
                    SetBondOption();
                }
            }
        }

        private List<BondOption> SelectedBondOptions()
        {
            var selectedBonds = SelectedItems.OfType<Bond>();

            var selbonds = (from Bond selbond in selectedBonds
                            select new BondOption { Order = selbond.Order, Stereo = selbond.Stereo }).Distinct();

            var selOptions = from BondOption bo in _bondOptions.Values
                             join selbond1 in selbonds
                             on new { bo.Order, bo.Stereo } equals new { selbond1.Order, selbond1.Stereo }
                             select new BondOption { Id = bo.Id, Order = bo.Order, Stereo = bo.Stereo };

            return selOptions.ToList();
        }

        public Editor EditorControl { get; set; }
        public EditorCanvas CurrentEditor { get; set; }

        public ClipboardMonitor ClipboardMonitor { get; }

        #endregion Properties

        #region Events

        public event EventHandler<WpfEventArgs> OnFeedbackChange;

        internal void SendStatus(string value)
        {
            try
            {
                var args = new WpfEventArgs();
                args.OutputValue = value;
                OnFeedbackChange?.Invoke(this, args);
            }
            catch
            {
                // We don't care if this fails
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// The pivotal routine for handling selection in the EditViewModel
        /// All display for selections *must* go through this routine.  No ifs, no buts
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectedItems_Changed(object sender, NotifyCollectionChangedEventArgs e)
        {
            var newObjects = e.NewItems;
            var oldObjects = e.OldItems;

            if (newObjects != null)
            {
                AddSelectionAdorners(newObjects);
            }

            if (oldObjects != null)
            {
                RemoveSelectionAdorners(oldObjects);
            }

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                RemoveAllAdorners();
            }

            OnPropertyChanged(nameof(SelectedElement));
            OnPropertyChanged(nameof(SelectedBondOptionId));
            OnPropertyChanged(nameof(SelectionType));
            //tell the editor what commands are allowable
            UpdateCommandStatuses();
        }

        private void MolAdorner_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            //we've completed the drag operation
            //remove the existing molecule adorner
            var moleculeSelectionAdorner = ((MoleculeSelectionAdorner)sender);

            foreach (Molecule mol in moleculeSelectionAdorner.AdornedMolecules)
            {
                RemoveFromSelection(mol);
                //and add in a new one
                AddToSelection(mol);
            }
        }

        private void AtomAdorner_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            //we've completed the drag operation
            //remove the existing molecule adorner
            var moleculeSelectionAdorner = ((SingleAtomSelectionAdorner)sender);
            foreach (Molecule mol in moleculeSelectionAdorner.AdornedMolecules)
            {
                RemoveFromSelection(mol);
                //and add in a new one
                AddToSelection(mol);
            }
        }

        private void SelAdorner_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                _selectedItems.Clear();
                Molecule mol = null;
                var visual = CurrentEditor.GetTargetedVisual(e.GetPosition(CurrentEditor));
                if (visual is AtomVisual av)
                {
                    mol = av.ParentAtom.Parent;
                }
                else if (visual is BondVisual bv)
                {
                    mol = bv.ParentBond.Parent;
                }

                RemoveAtomBondAdorners(mol);
                if (mol != null)
                {
                    AddToSelection(mol);
                }
            }
        }

        #endregion Events

        public bool IsDirty => UndoManager.CanUndo;

        private BaseEditBehavior _activeMode;
        public List<string> Used1DProperties { get; set; }

        public BaseEditBehavior ActiveMode
        {
            get { return _activeMode; }
            set
            {
                if (_activeMode != null)
                {
                    _activeMode.Detach();
                    _activeMode = null;
                }

                _activeMode = value;
                if (_activeMode != null)
                {
                    _activeMode.Attach(CurrentEditor);
                    SendStatus(_activeMode.CurrentStatus);
                }
                OnPropertyChanged();
            }
        }

        public ObservableCollection<AtomOption> AtomOptions { get; set; }

        #region Commands

        public AddAtomCommand AddAtomCommand { get; set; }
        public UndoCommand UndoCommand { get; set; }
        public RedoCommand RedoCommand { get; set; }
        public CopyCommand CopyCommand { get; set; }
        public CutCommand CutCommand { get; set; }
        public PasteCommand PasteCommand { get; set; }
        public FlipVerticalCommand FlipVerticalCommand { get; set; }
        public FlipHorizontalCommand FlipHorizontalCommand { get; set; }
        public AddHydrogensCommand AddHydrogensCommand { get; set; }
        public RemoveHydrogensCommand RemoveHydrogensCommand { get; set; }
        public FuseCommand FuseCommand { get; set; }
        public GroupCommand GroupCommand { get; set; }
        public UnGroupCommand UnGroupCommand { get; set; }
        public SettingsCommand SettingsCommand { get; set; }
        public PickElementCommand PickElementCommand { get; set; }

        #endregion Commands

        #region Constructors

        /// <summary>
        /// "Normal" Constructor
        /// </summary>
        /// <param name="model"></param>
        /// <param name="currentEditor"></param>
        /// <param name="used1DProperties"></param>
        /// <param name="telemetry"></param>
        public EditViewModel(Model model, EditorCanvas currentEditor, List<string> used1DProperties, IChem4WordTelemetry telemetry) : base(model)
        {
            CurrentEditor = currentEditor;
            Used1DProperties = used1DProperties;
            Telemetry = telemetry;

            ClipboardMonitor = new ClipboardMonitor();
            ClipboardMonitor.OnClipboardContentChanged += ClipboardMonitor_OnClipboardContentChanged;

            AtomOptions = new ObservableCollection<AtomOption>();
            LoadAtomOptions();
            LoadBondOptions();

            _selectedItems = new ObservableCollection<object>();
            SelectedItems = new ReadOnlyObservableCollection<object>(_selectedItems);
            _selectedItems.CollectionChanged += SelectedItems_Changed;

            UndoManager = new UndoHandler(this, telemetry);
            SetupCommands();

            _selectedElement = Globals.PeriodicTable.C;
            _selectedBondOptionId = 1;
        }

        /// <summary>
        /// Constructor for [X]Unit Tests
        /// Initialises the minimum objects necessary to run [X]Unit Tests
        /// </summary>
        /// <param name="model"></param>
        public EditViewModel(Model model) : base(model)
        {
            LoadBondOptionsForUnitTest();

            _selectedItems = new ObservableCollection<object>();
            SelectedItems = new ReadOnlyObservableCollection<object>(_selectedItems);

            UndoManager = new UndoHandler(this, null);
            SetupCommands();

            _selectedBondOptionId = 1;
            _selectedElement = Globals.PeriodicTable.C;
        }

        private void SetupCommands()
        {
            RedoCommand = new RedoCommand(this);
            UndoCommand = new UndoCommand(this);
            AddAtomCommand = new AddAtomCommand(this);
            CopyCommand = new CopyCommand(this);
            CutCommand = new CutCommand(this);
            PasteCommand = new PasteCommand(this);
            FlipVerticalCommand = new FlipVerticalCommand(this);
            FlipHorizontalCommand = new FlipHorizontalCommand(this);
            AddHydrogensCommand = new AddHydrogensCommand(this);
            RemoveHydrogensCommand = new RemoveHydrogensCommand(this);
            FuseCommand = new FuseCommand(this);
            GroupCommand = new GroupCommand(this);
            UnGroupCommand = new UnGroupCommand(this);
            SettingsCommand = new SettingsCommand(this);
            PickElementCommand = new PickElementCommand(this);
        }

        private void ClipboardMonitor_OnClipboardContentChanged(object sender, EventArgs e)
        {
            PasteCommand.RaiseCanExecChanged();
        }

        private void LoadAtomOptions()
        {
            ClearAtomOptions();
            LoadStandardAtomOptions();
            LoadModelAtomOptions();
            LoadModelFGs();
        }

        private void ClearAtomOptions()
        {
            int limit = AtomOptions.Count - 1;
            for (int i = limit; i >= 0; i--)
            {
                AtomOptions.RemoveAt(i);
            }
        }

        private void LoadModelFGs()
        {
            var modelFGs = (from a in Model.GetAllAtoms()
                            where a.Element is FunctionalGroup && !(from ao in AtomOptions
                                                                    select ao.Element).Contains(a.Element)
                            orderby a.SymbolText
                            select a.Element).Distinct();

            var newOptions = from mfg in modelFGs
                             select new AtomOption((mfg as FunctionalGroup));
            foreach (var newOption in newOptions)
            {
                AtomOptions.Add(newOption);
            }
        }

        private void LoadModelAtomOptions(Element addition = null)
        {
            var modelElements = (from a in Model.GetAllAtoms()
                                 where a.Element is Element && !(from ao in AtomOptions
                                                                 select ao.Element).Contains(a.Element)
                                 orderby a.SymbolText
                                 select a.Element).Distinct();

            var newOptions = from e in Globals.PeriodicTable.ElementsSource
                             join me in modelElements
                                 on e equals me
                             select new AtomOption(e);

            foreach (var newOption in newOptions)
            {
                AtomOptions.Add(newOption);
            }

            if (addition != null && !AtomOptions.Select(ao => ao.Element).Contains(addition))
            {
                AtomOptions.Add(new AtomOption(addition));
            }
        }

        private void LoadStandardAtomOptions()
        {
            foreach (var atom in Constants.StandardAtoms)
            {
                AtomOptions.Add(new AtomOption(Globals.PeriodicTable.Elements[atom]));
            }

            foreach (var fg in Constants.StandardFunctionalGroups)
            {
                AtomOptions.Add(new AtomOption(FunctionalGroupsList.FirstOrDefault(f => f.Name.Equals(fg))));
            }
        }

        /// <summary>
        /// Loads up the bond options into the main dropdown
        /// </summary>
        private void LoadBondOptions()
        {
            var storedOptions = (BondOption[])CurrentEditor.FindResource("BondOptions");
            for (int i = 1; i <= storedOptions.Length; i++)
            {
                _bondOptions[i] = storedOptions[i - 1];
            }
        }

        private void LoadBondOptionsForUnitTest()
        {
            _bondOptions = new Dictionary<int, BondOption>
                           {
                               {
                                   1,
                                   new BondOption
                                   {
                                       Id = 1,
                                       Order = OrderSingle,
                                       Stereo = BondStereo.None
                                   }
                               },
                               {
                                   2,
                                   new BondOption
                                   {
                                       Id = 2,
                                       Order = OrderDouble,
                                       Stereo = BondStereo.None
                                   }
                               },
                               {
                                   3,
                                   new BondOption
                                   {
                                       Id = 3,
                                       Order = OrderTriple,
                                       Stereo = BondStereo.None
                                   }
                               }
                           };
        }

        #endregion Constructors

        public void SetElement(ElementBase value, List<Atom> selAtoms)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                var affectedAtoms = selAtoms == null ? "{null}" : selAtoms.Count.ToString();
                var countString = selAtoms == null ? "{null}" : $"{selAtoms.Count}";
                WriteTelemetry(module, "Debug", $"Atoms: {countString}; Symbol: {value?.Symbol ?? "{null}"}; Affected Atoms {affectedAtoms}");

                if (selAtoms != null && selAtoms.Any())
                {
                    UndoManager.BeginUndoBlock();

                    foreach (Atom selectedAtom in selAtoms)
                    {
                        var lastAtom = selectedAtom;
                        if (lastAtom.Element != value)
                        {
                            var currentIsotope = lastAtom.IsotopeNumber;
                            var lastElement = lastAtom.Element;

                            Action redo = () =>
                            {
                                lastAtom.Element = value;
                                lastAtom.IsotopeNumber = null;
                                lastAtom.UpdateVisual();

                                //reselect the atom to clear the adorner
                                RemoveFromSelection(lastAtom);
                                AddToSelection(lastAtom);
                            };

                            Action undo = () =>
                            {
                                lastAtom.Element = lastElement;
                                lastAtom.IsotopeNumber = currentIsotope;
                                lastAtom.UpdateVisual();

                                //reselect the atom to clear the adorner
                                RemoveFromSelection(lastAtom);
                                AddToSelection(lastAtom);
                            };

                            UndoManager.RecordAction(undo, redo, $"Set Element to {value?.Symbol ?? "null"}");
                            redo();

                            foreach (Bond bond in lastAtom.Bonds)
                            {
                                bond.UpdateVisual();
                            }
                        }
                    }

                    UndoManager.EndUndoBlock();
                }
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);
        }

        private void SetBondOption()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (_selectedBondOptionId != null)
                {
                    var bondOption = _bondOptions[_selectedBondOptionId.Value];
                    var stereoValue = bondOption.Stereo.HasValue ? bondOption.Stereo.Value.ToString() : "{null}";
                    var affectedBonds = SelectedItems.OfType<Bond>().Count();
                    WriteTelemetry(module, "Debug", $"Order: {bondOption.Order}; Stereo: {stereoValue}; Affected Bonds: {affectedBonds}");

                    if (SelectedItems.OfType<Bond>().Any())
                    {
                        UndoManager.BeginUndoBlock();
                        foreach (Bond bond in SelectedItems.OfType<Bond>())
                        {
                            Action redo = () =>
                            {
                                bond.Stereo = bondOption.Stereo.Value;
                                bond.Order = bondOption.Order;
                            };

                            var bondStereo = bond.Stereo;
                            var bondOrder = bond.Order;

                            Action undo = () =>
                            {
                                bond.Stereo = bondStereo;
                                bond.Order = bondOrder;
                            };

                            UndoManager.RecordAction(undo, redo);
                            bond.Order = bondOption.Order;
                            bond.Stereo = bondOption.Stereo.Value;
                        }

                        UndoManager.EndUndoBlock();
                    }
                }
                else
                {
                    WriteTelemetry(module, "Exception", "_selectedBondOptionId is {null}");
                }
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);
        }

        public void IncreaseBondOrder(Bond existingBond)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                var stereo = existingBond.Stereo;
                var order = existingBond.Order;

                Action redo = () =>
                              {
                                  existingBond.Stereo = BondStereo.None;
                                  switch (existingBond.Order)
                                  {
                                      case OrderZero:
                                          existingBond.Order = OrderSingle;
                                          break;

                                      case OrderSingle:
                                          existingBond.Order = OrderDouble;
                                          break;

                                      case OrderDouble:
                                          existingBond.Order = OrderTriple;
                                          break;

                                      case OrderTriple:
                                          existingBond.Order = OrderSingle;
                                          break;
                                  }

                                  existingBond.StartAtom.UpdateVisual();
                                  existingBond.EndAtom.UpdateVisual();
                                  existingBond.UpdateVisual();
                              };
                Action undo = () =>
                              {
                                  existingBond.Stereo = stereo;
                                  existingBond.Order = order;

                                  existingBond.StartAtom.UpdateVisual();
                                  existingBond.EndAtom.UpdateVisual();
                                  existingBond.UpdateVisual();
                              };

                UndoManager.BeginUndoBlock();
                UndoManager.RecordAction(undo, redo);
                UndoManager.EndUndoBlock();

                redo();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);
        }

        private List<string> DecodeTransform(Transform transform)
        {
            var result = new List<string>();

            try
            {
                var typeName = transform.GetType().Name.Replace("Transform", "");

                switch (transform)
                {
                    case TransformGroup group:
                        result.Add($"{typeName}");
                        foreach (var child in group.Children)
                        {
                            result.AddRange(DecodeTransform(child));
                        }
                        break;

                    case TranslateTransform translate:
                        result.Add($"{typeName} by {SafeDouble.AsString(translate.X)},{SafeDouble.AsString(translate.Y)}");
                        break;

                    case RotateTransform rotate:
                        result.Add($"{typeName} by {SafeDouble.AsString(rotate.Angle)} degrees about {SafeDouble.AsString(rotate.CenterX)},{SafeDouble.AsString(rotate.CenterY)}");
                        break;

                    case ScaleTransform scale:
                        result.Add($"{typeName} by {SafeDouble.AsString(scale.ScaleX)},{SafeDouble.AsString(scale.ScaleY)} about {SafeDouble.AsString(scale.CenterX)},{SafeDouble.AsString(scale.CenterY)}");
                        break;

                    default:
                        result.Add($"{typeName} ???");
                        break;
                }
            }
            catch
            {
                // Do Nothing
            }

            return result;
        }

        public void DoTransform(Transform operation, List<Atom> atoms)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                var countString = atoms == null ? "{null}" : $"{atoms.Count}";
                var transform = string.Join(";", DecodeTransform(operation));
                WriteTelemetry(module, "Debug", $"Atoms: {countString} Transform: {transform}");

                if (atoms.Any())
                {
                    var inverse = operation.Inverse;
                    if (inverse != null)
                    {
                        Atom[] atomsToTransform = atoms.ToArray();
                        //need an reference to the mol later
                        Molecule parent = atoms[0].Parent;

                        Action undo = () =>
                                      {
                                          _selectedItems.Clear();
                                          foreach (Atom atom in atomsToTransform)
                                          {
                                              atom.Position = inverse.Transform(atom.Position);
                                              atom.UpdateVisual();
                                          }

                                          parent.RootMolecule.UpdateVisual();
                                          foreach (Atom o in atomsToTransform)
                                          {
                                              AddToSelection(o);
                                          }
                                      };

                        Action redo = () =>
                                      {
                                          _selectedItems.Clear();
                                          foreach (Atom atom in atomsToTransform)
                                          {
                                              atom.Position = operation.Transform(atom.Position);
                                              atom.UpdateVisual();
                                          }

                                          parent.RootMolecule.UpdateVisual();
                                          foreach (Atom o in atomsToTransform)
                                          {
                                              AddToSelection(o);
                                          }
                                      };

                        UndoManager.BeginUndoBlock();
                        UndoManager.RecordAction(undo, redo);
                        UndoManager.EndUndoBlock();

                        redo();
                    }
                }
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);
        }

        public void DoTransform(Transform operation, List<Molecule> molecules)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                var countString = molecules == null ? "{null}" : $"{molecules.Count}";
                var transform = string.Join(";", DecodeTransform(operation));
                WriteTelemetry(module, "Debug", $"Molecules: {countString} Transform: {transform}");

                var inverse = operation.Inverse;
                if (inverse != null)
                {
                    var rootMolecules = from m in molecules
                                        where m.RootMolecule == m
                                        select m;
                    Molecule[] moleculesToTransform = rootMolecules.ToArray();

                    Action undo = () =>
                                  {
                                      SuppressEditorRedraw(true);
                                      _selectedItems.Clear();
                                      foreach (Molecule mol in moleculesToTransform)
                                      {
                                          mol.Transform((Transform)inverse);
                                      }
                                      SuppressEditorRedraw(false);

                                      foreach (Molecule mol in moleculesToTransform)
                                      {
                                          mol.UpdateVisual();
                                      }

                                      foreach (var o in moleculesToTransform)
                                      {
                                          AddToSelection(o);
                                      }
                                  };

                    Action redo = () =>
                                  {
                                      SuppressEditorRedraw(true);
                                      _selectedItems.Clear();
                                      foreach (Molecule mol in moleculesToTransform)
                                      {
                                          mol.Transform(operation);
                                      }
                                      SuppressEditorRedraw(false);

                                      foreach (Molecule mol in moleculesToTransform)
                                      {
                                          mol.UpdateVisual();
                                      }

                                      foreach (var o in moleculesToTransform)
                                      {
                                          AddToSelection(o);
                                      }
                                  };

                    UndoManager.BeginUndoBlock();
                    UndoManager.RecordAction(undo, redo);
                    UndoManager.EndUndoBlock();

                    redo();
                }
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);
        }

        private void SuppressEditorRedraw(bool state)
        {
            if (CurrentEditor != null)
            {
                CurrentEditor.SuppressRedraw = state;
            }
        }

        public void SwapBondDirection(Bond bond)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                var startAtom = bond.StartAtom;
                var endAtom = bond.EndAtom;

                Action undo = () =>
                              {
                                  bond.StartAtomInternalId = startAtom.InternalId;
                                  bond.EndAtomInternalId = endAtom.InternalId;
                                  bond.UpdateVisual();
                              };

                Action redo = () =>
                              {
                                  bond.EndAtomInternalId = startAtom.InternalId;
                                  bond.StartAtomInternalId = endAtom.InternalId;
                                  bond.UpdateVisual();
                              };

                UndoManager.BeginUndoBlock();
                UndoManager.RecordAction(undo, redo);
                UndoManager.EndUndoBlock();

                redo();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);
        }

        public void SetBondAttributes(Bond bond, string newOrder = null, BondStereo? newStereo = null)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                var orderParameter = newOrder ?? CurrentBondOrder;
                var stereoParameter = newStereo ?? CurrentStereo;
                WriteTelemetry(module, "Debug", $"Order: {orderParameter}; Stereo: {stereoParameter}");

                var order = bond.Order;
                var stereo = bond.Stereo;

                Action undo = () =>
                              {
                                  bond.Order = order;
                                  bond.Stereo = stereo;
                                  bond.StartAtom.UpdateVisual();
                                  bond.EndAtom.UpdateVisual();
                              };

                Action redo = () =>
                              {
                                  bond.Order = newOrder ?? CurrentBondOrder;
                                  bond.Stereo = newStereo ?? CurrentStereo;
                                  bond.StartAtom.UpdateVisual();
                                  bond.EndAtom.UpdateVisual();
                              };

                UndoManager.BeginUndoBlock();
                UndoManager.RecordAction(undo, redo);
                UndoManager.EndUndoBlock();

                redo();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        public void AddNewBond(Atom a, Atom b, Molecule mol, string order = null, BondStereo? stereo = null)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                var startAtomInfo = "{null}";
                if (a != null)
                {
                    if (mol.Atoms.ContainsKey(a.InternalId))
                    {
                        if (a.Element == null)
                        {
                            startAtomInfo = $"Null @ {PointHelper.AsString(a.Position)}";
                        }
                        else
                        {
                            startAtomInfo = $"{a.Element.Symbol} @ {PointHelper.AsString(a.Position)}";
                        }
                    }
                    else
                    {
                        startAtomInfo = $"{a.InternalId} not found";
                    }
                }
                var endAtomInfo = "{null}";
                if (b != null)
                {
                    if (mol.Atoms.ContainsKey(b.InternalId))
                    {
                        if (b.Element == null)
                        {
                            endAtomInfo = $"Null @ {PointHelper.AsString(b.Position)}";
                        }
                        else
                        {
                            endAtomInfo = $"{b.Element.Symbol} @ {PointHelper.AsString(b.Position)}";
                        }
                    }
                    else
                    {
                        endAtomInfo = $"{b.InternalId} not found";
                    }
                }

                var orderInfo = order ?? CurrentBondOrder;
                WriteTelemetry(module, "Debug", $"StartAtom: {startAtomInfo}; EndAtom: {endAtomInfo}; BondOrder; {orderInfo}");

                // Only allow a bond to be created if both atoms are children of the molecule passed in
                if (a.Parent == mol && b.Parent == mol)
                {
                    //keep a handle on some current properties
                    int theoreticalRings = mol.TheoreticalRings;
                    if (stereo == null)
                    {
                        stereo = CurrentStereo;
                    }

                    if (order == null)
                    {
                        order = CurrentBondOrder;
                    }

                    //stash the current molecule properties
                    MoleculePropertyBag mpb = new MoleculePropertyBag();
                    mpb.Store(mol);

                    Bond newbond = new Bond();

                    newbond.Stereo = stereo.Value;
                    newbond.Order = order;
                    newbond.Parent = mol;

                    Action undo = () =>
                    {
                        Atom startAtom = newbond.StartAtom;
                        Atom endAtom = newbond.EndAtom;
                        mol.RemoveBond(newbond);
                        newbond.Parent = null;
                        if (theoreticalRings != mol.TheoreticalRings)
                        {
                            mol.RebuildRings();
                            theoreticalRings = mol.TheoreticalRings;
                        }

                        RefreshAtoms(startAtom, endAtom);

                        mpb.Restore(mol);
                    };

                    Action redo = () =>
                    {
                        newbond.StartAtomInternalId = a.InternalId;
                        newbond.EndAtomInternalId = b.InternalId;
                        newbond.Parent = mol;
                        mol.AddBond(newbond);
                        if (theoreticalRings != mol.TheoreticalRings)
                        {
                            mol.RebuildRings();
                            theoreticalRings = mol.TheoreticalRings;
                        }

                        RefreshAtoms(newbond.StartAtom, newbond.EndAtom);
                        newbond.UpdateVisual();
                        mol.ClearProperties();
                    };

                    UndoManager.BeginUndoBlock();
                    UndoManager.RecordAction(undo, redo);
                    UndoManager.EndUndoBlock();

                    redo();

                    // local function
                    void RefreshAtoms(Atom startAtom, Atom endAtom)
                    {
                        startAtom.UpdateVisual();
                        endAtom.UpdateVisual();
                        foreach (Bond bond in startAtom.Bonds)
                        {
                            bond.UpdateVisual();
                        }

                        foreach (Bond bond in endAtom.Bonds)
                        {
                            bond.UpdateVisual();
                        }
                    }
                }
                else
                {
                    WriteTelemetry(module, "Exception", $"Molecule: {mol.Path} StartAtom: {a.Path} EndAtom: {b.Path}");
                }
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);
        }

        /// <summary>
        /// Adds a new atom to an existing molecule separated by one bond
        /// </summary>
        /// <param name="lastAtom">previous atom to which the new one is bonded.  can be null</param>
        /// <param name="newAtomPos">Position of new atom</param>
        /// <param name="dir">ClockDirection in which to add the atom</param>
        /// <param name="elem">Element of atom (can be a FunctionalGroup).  defaults to current selection</param>
        /// <param name="bondOrder"></param>
        /// <param name="stereo"></param>
        /// <returns></returns>
        public Atom AddAtomChain(Atom lastAtom, Point newAtomPos, ClockDirections dir, ElementBase elem = null,
                                 string bondOrder = null, BondStereo? stereo = null)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                var atomInfo = lastAtom == null ? "{null}" : $"{lastAtom.Element.Symbol} @ {PointHelper.AsString(lastAtom.Position)}";
                var eleInfo = elem == null ? $"{_selectedElement.Symbol}" : $"{elem.Symbol}";
                WriteTelemetry(module, "Debug", $"LastAtom: {atomInfo}; NewAtom: {eleInfo} @ {PointHelper.AsString(newAtomPos)}");

                //create the new atom based on the current selection
                Atom newAtom = new Atom { Element = elem ?? _selectedElement, Position = newAtomPos };

                //the tag stores sprout directions chosen for the chain
                object tag = null;

                if (dir != ClockDirections.Nothing)
                {
                    tag = dir;
                }

                //stash the last sprout direction
                object oldDir = lastAtom?.Tag;

                //are we drawing an isolated atom?
                if (lastAtom == null) //then it's isolated
                {
                    var newMolecule = new Molecule();

                    Action undoAddNewMolecule = () =>
                                             {
                                                 Model.RemoveMolecule(newMolecule);
                                                 newMolecule.Parent = null;
                                             };
                    Action redoAddNewMolecule = () =>
                                             {
                                                 newMolecule.Parent = Model;
                                                 Model.AddMolecule(newMolecule);
                                             };

                    Action undoAddIsolatedAtom = () =>
                                                 {
                                                     newAtom.Tag = null;
                                                     newMolecule.RemoveAtom(newAtom);
                                                     newAtom.Parent = null;
                                                 };
                    Action redoAddIsolatedAtom = () =>
                                                 {
                                                     newAtom.Parent = newMolecule;
                                                     newMolecule.AddAtom(newAtom);
                                                     newAtom.Tag = tag;
                                                 };

                    UndoManager.BeginUndoBlock();
                    UndoManager.RecordAction(undoAddNewMolecule, redoAddNewMolecule, $"{nameof(AddAtomChain)}[AddMolecule]");
                    UndoManager.RecordAction(undoAddIsolatedAtom, redoAddIsolatedAtom, $"{nameof(AddAtomChain)}[AddAtom]");
                    UndoManager.EndUndoBlock();

                    redoAddNewMolecule();
                    redoAddIsolatedAtom();
                }
                else
                {
                    var existingMolecule = lastAtom.Parent;
                    if (existingMolecule != null)
                    {
                        Action undoAddEndAtom = () =>
                        {
                            ClearSelection();
                            lastAtom.Tag = oldDir;
                            existingMolecule.RemoveAtom(newAtom);
                            newAtom.Parent = null;
                            lastAtom.UpdateVisual();
                        };
                        Action redoAddEndAtom = () =>
                        {
                            ClearSelection();
                            lastAtom.Tag = tag; //save the last sprouted direction in the tag object
                            newAtom.Parent = existingMolecule;
                            existingMolecule.AddAtom(newAtom);
                            lastAtom.UpdateVisual();
                            newAtom.UpdateVisual();
                        };

                        UndoManager.BeginUndoBlock();
                        UndoManager.RecordAction(undoAddEndAtom, redoAddEndAtom, $"{nameof(AddAtomChain)}[AddEndAtom]");

                        // Can't put these after of the UndoManager.EndUndoBlock as they are part of the same atomic action
                        redoAddEndAtom();
                        AddNewBond(lastAtom, newAtom, existingMolecule, bondOrder, stereo);

                        UndoManager.EndUndoBlock();

                        lastAtom.UpdateVisual();
                        newAtom.UpdateVisual();
                        foreach (Bond lastAtomBond in lastAtom.Bonds)
                        {
                            lastAtomBond.UpdateVisual();
                        }
                    }
                }

                return newAtom;
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            // This is an error!
            return null;
        }

        public void SetAverageBondLength(double newLength)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", $"Length {newLength / ScaleFactorForXaml}");

                var currentLength = Model.MeanBondLength;
                var currentSelection = Math.Round(currentLength / ScaleFactorForXaml / 5.0) * 5 * ScaleFactorForXaml;

                var centre = new Point(Model.BoundingBoxWithFontSize.Left + Model.BoundingBoxWithFontSize.Width / 2,
                                       Model.BoundingBoxWithFontSize.Top + Model.BoundingBoxWithFontSize.Height / 2);

                Action redoAction = () =>
                                    {
                                        Model.ScaleToAverageBondLength(newLength, centre);
                                        SetTextParams(newLength);
                                        RefreshMolecules(Model.Molecules.Values.ToList());
                                        Loading = true;
                                        CurrentBondLength = newLength / ScaleFactorForXaml;
                                        Loading = false;
                                    };
                Action undoAction = () =>
                                    {
                                        Model.ScaleToAverageBondLength(currentLength, centre);
                                        SetTextParams(currentSelection);
                                        RefreshMolecules(Model.Molecules.Values.ToList());
                                        Loading = true;
                                        CurrentBondLength = currentSelection / ScaleFactorForXaml;
                                        Loading = false;
                                    };

                UndoManager.BeginUndoBlock();
                UndoManager.RecordAction(undoAction, redoAction);
                UndoManager.EndUndoBlock();

                redoAction();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);
        }

        public void CopySelection()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                CMLConverter converter = new CMLConverter();
                Model tempModel = new Model();
                //if selection isn't null
                if (SelectedItems.Count > 0)
                {
                    HashSet<Atom> copiedAtoms = new HashSet<Atom>();
                    //iterate through the active selection
                    foreach (object selectedItem in SelectedItems)
                    {
                        //if the current selection is a molecule
                        if (selectedItem is Molecule molecule)
                        {
                            tempModel.AddMolecule(molecule);
                        }
                        else if (selectedItem is Atom atom)
                        {
                            copiedAtoms.Add(atom);
                        }
                    }

                    //keep track of added atoms
                    Dictionary<string, Atom> aa = new Dictionary<string, Atom>();
                    //while the atom copy list isn't empty
                    while (copiedAtoms.Any())
                    {
                        Atom seedAtom = copiedAtoms.First();
                        //create a new molecule
                        Molecule newMol = new Molecule();
                        Molecule oldParent = seedAtom.Parent;

                        HashSet<Atom> thisAtomGroup = new HashSet<Atom>();

                        //Traverse the molecule, excluding atoms that have been processed and bonds that aren't in the list
                        oldParent.TraverseBFS(seedAtom,
                                              atom =>
                                              {
                                                  copiedAtoms.Remove(atom);

                                                  thisAtomGroup.Add(atom);
                                              },
                                              atom2 =>
                                              {
                                                  return !thisAtomGroup.Contains(atom2) && copiedAtoms.Contains(atom2);
                                              });

                        //add the atoms and bonds to the new molecule

                        foreach (Atom thisAtom in thisAtomGroup)
                        {
                            Atom a = new Atom
                            {
                                Id = thisAtom.Id,
                                Position = thisAtom.Position,
                                Element = thisAtom.Element,
                                FormalCharge = thisAtom.FormalCharge,
                                IsotopeNumber = thisAtom.IsotopeNumber,
                                ExplicitC = thisAtom.ExplicitC,
                                Parent = newMol
                            };

                            newMol.AddAtom(a);
                            aa[a.Id] = a;
                        }

                        Bond thisBond = null;
                        List<Bond> copiedBonds = new List<Bond>();
                        foreach (Atom startAtom in thisAtomGroup)
                        {
                            foreach (Atom otherAtom in thisAtomGroup)
                            {
                                if ((thisBond = startAtom.BondBetween(otherAtom)) != null && !copiedBonds.Contains(thisBond))
                                {
                                    copiedBonds.Add(thisBond);
                                    Atom s = aa[thisBond.StartAtom.Id];
                                    Atom e = aa[thisBond.EndAtom.Id];
                                    Bond b = new Bond(s, e)
                                    {
                                        Id = thisBond.Id,
                                        Order = thisBond.Order,
                                        Stereo = thisBond.Stereo,
                                        ExplicitPlacement = thisBond.ExplicitPlacement,
                                        Parent = newMol
                                    };

                                    newMol.AddBond(b);
                                }
                            }
                        }

                        newMol.Parent = tempModel;
                        tempModel.AddMolecule(newMol);
                    }

                    tempModel.RescaleForCml();
                    string export = converter.Export(tempModel);
                    IDataObject ido = new DataObject();
                    ido.SetData(FormatCML, export);
                    string header = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n";
                    ido.SetData(DataFormats.Text, header + export);
                    Clipboard.SetDataObject(ido, true);
                }
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        public void CutSelection()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                UndoManager.BeginUndoBlock();
                CopySelection();
                DeleteSelection();
                UndoManager.EndUndoBlock();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        private void RemoveAtomBondAdorners(Molecule atomParent)
        {
            var layer = AdornerLayer.GetAdornerLayer(CurrentEditor);
            foreach (Bond bond in atomParent.Bonds)
            {
                if (SelectionAdorners.ContainsKey(bond))
                {
                    var selectionAdorner = SelectionAdorners[bond];
                    selectionAdorner.MouseLeftButtonDown -= SelAdorner_MouseLeftButtonDown;
                    layer.Remove(selectionAdorner);
                    SelectionAdorners.Remove(bond);
                }
            }

            foreach (Atom atom in atomParent.Atoms.Values)
            {
                if (SelectionAdorners.ContainsKey(atom))
                {
                    var selectionAdorner = SelectionAdorners[atom];
                    selectionAdorner.MouseLeftButtonDown -= SelAdorner_MouseLeftButtonDown;
                    layer.Remove(selectionAdorner);
                    SelectionAdorners.Remove(atom);
                }
            }

            foreach (var mol in atomParent.Molecules.Values)
            {
                RemoveAtomBondAdorners(mol);
            }
        }

        /// <summary>
        /// Updates the command status.  Should generally only be called
        /// after the active selection is changed
        /// </summary>
        private void UpdateCommandStatuses()
        {
            CopyCommand.RaiseCanExecChanged();
            GroupCommand.RaiseCanExecChanged();
            UnGroupCommand.RaiseCanExecChanged();
            CutCommand.RaiseCanExecChanged();
            FlipHorizontalCommand.RaiseCanExecChanged();
            FlipVerticalCommand.RaiseCanExecChanged();
            AddHydrogensCommand.RaiseCanExecChanged();
            RemoveHydrogensCommand.RaiseCanExecChanged();
        }

        public void RemoveAllAdorners()
        {
            var layer = AdornerLayer.GetAdornerLayer(CurrentEditor);
            if (layer != null)
            {
                var adornerList = layer.GetAdorners(CurrentEditor);
                if (adornerList != null)
                {
                    foreach (Adorner adorner in adornerList)
                    {
                        layer.Remove(adorner);
                    }
                }
            }
            SelectionAdorners.Clear();
        }

        public void UpdateAtomBondAdorners()
        {
            if (MultiAdorner != null)
            {
                MultiAdorner.MouseLeftButtonDown -= SelAdorner_MouseLeftButtonDown;
                var layer = AdornerLayer.GetAdornerLayer(CurrentEditor);
                layer.Remove(MultiAdorner);
                MultiAdorner = null;
            }

            var selAtomBonds = (from ChemistryBase sel in _selectedItems
                                where sel is Atom || sel is Bond
                                select sel).ToList();

            if (selAtomBonds.Any())
            {
                MultiAdorner = new MultiAtomBondAdorner(CurrentEditor, selAtomBonds);
                MultiAdorner.MouseLeftButtonDown += SelAdorner_MouseLeftButtonDown;
            }
        }

        private void RemoveSelectionAdorners(IList oldObjects)
        {
            var layer = AdornerLayer.GetAdornerLayer(CurrentEditor);
            foreach (object oldObject in oldObjects)
            {
                if (SelectionAdorners.ContainsKey(oldObject))
                {
                    var selectionAdorner = SelectionAdorners[oldObject];
                    if (selectionAdorner is MoleculeSelectionAdorner)
                    {
                        var msAdorner = (MoleculeSelectionAdorner)selectionAdorner;

                        msAdorner.DragCompleted -= MolAdorner_DragCompleted;
                        msAdorner.MouseLeftButtonDown -= SelAdorner_MouseLeftButtonDown;
                        (msAdorner as SingleAtomSelectionAdorner).DragCompleted -= MolAdorner_DragCompleted;
                    }

                    layer.Remove(selectionAdorner);
                    SelectionAdorners.Remove(oldObject);
                }
            }
        }

        /// <summary>
        /// Adds adorners for a list of objects.  Should only be called from
        /// events on the _selectedItems collection AFTER the collection
        /// has been updated
        /// </summary>
        /// <param name="newObjects"></param>
        private void AddSelectionAdorners(IList newObjects)
        {
            var singleAtomMols = (from m in newObjects.OfType<Molecule>().Union(SelectedItems.OfType<Molecule>())
                                  where m.Atoms.Count == 1
                                  select m).ToList();
            var groupMols = (from m in newObjects.OfType<Molecule>().Union(SelectedItems.OfType<Molecule>())
                             where m.IsGrouped
                             select m).ToList();
            var allMolecules = (from m in newObjects.OfType<Molecule>().Union(SelectedItems.OfType<Molecule>())
                                select m).ToList();

            var allSingletons = singleAtomMols.Count == allMolecules.Count && singleAtomMols.Any();
            var allGroups = allMolecules.Count == groupMols.Count && groupMols.Any();

            if (allSingletons)
            {
                RemoveAllAdorners();
                SingleAtomSelectionAdorner atomAdorner =
                    new SingleAtomSelectionAdorner(CurrentEditor, singleAtomMols);
                foreach (Molecule mol in singleAtomMols)
                {
                    SelectionAdorners[mol] = atomAdorner;
                }

                atomAdorner.MouseLeftButtonDown += SelAdorner_MouseLeftButtonDown;
                atomAdorner.DragCompleted += AtomAdorner_DragCompleted;
            }
            else if (allGroups)
            {
                RemoveAllAdorners();
                var groupAdorner = new GroupSelectionAdorner(CurrentEditor,
                                                       groupMols);
                foreach (Molecule mol in groupMols)
                {
                    SelectionAdorners[mol] = groupAdorner;
                }
                groupAdorner.MouseLeftButtonDown += SelAdorner_MouseLeftButtonDown;
                groupAdorner.DragCompleted += AtomAdorner_DragCompleted;
            }
            else if (allMolecules.Any())
            {
                RemoveAllAdorners();
                var molAdorner = new MoleculeSelectionAdorner(CurrentEditor,
                                                             allMolecules);
                foreach (Molecule mol in allMolecules)
                {
                    SelectionAdorners[mol] = molAdorner;
                }
                molAdorner.MouseLeftButtonDown += SelAdorner_MouseLeftButtonDown;
                molAdorner.DragCompleted += AtomAdorner_DragCompleted;
            }
        }

        private string ListPlacements(List<NewAtomPlacement> newAtomPlacements)
        {
            var lines = new List<string>();

            int count = 0;

            foreach (var placement in newAtomPlacements)
            {
                var line = new StringBuilder();
                line.Append($"{count++} - ");
                line.Append($"{PointHelper.AsString(placement.Position)}");

                if (placement.ExistingAtom != null)
                {
                    var atom = placement.ExistingAtom;
                    line.Append($" {atom.Element.Symbol} {atom.Path}");
                    if (atom.Position != placement.Position)
                    {
                        line.Append($" @ {PointHelper.AsString(atom.Position)}");
                    }
                }

                lines.Add(line.ToString());
            }

            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// Draws a ring as specified by the new atom placements
        /// </summary>
        /// <param name="newAtomPlacements"></param>
        /// <param name="unsaturated"></param>
        /// <param name="startAt"></param>
        public void DrawRing(List<NewAtomPlacement> newAtomPlacements, bool unsaturated, int startAt = 0)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                var count = newAtomPlacements == null ? "{null}" : $"{newAtomPlacements.Count}";
                WriteTelemetry(module, "Debug", $"Atoms: {count}; Unsaturated: {unsaturated}; StartAt: {startAt}");
                WriteTelemetry(module, "Debug", ListPlacements(newAtomPlacements));

                UndoManager.BeginUndoBlock();

                //work around the ring adding atoms
                for (int i = 1; i <= newAtomPlacements.Count; i++)
                {
                    int currIndex = i % newAtomPlacements.Count;
                    NewAtomPlacement currentPlacement = newAtomPlacements[currIndex];
                    NewAtomPlacement previousPlacement = newAtomPlacements[i - 1];

                    Atom previousAtom = previousPlacement.ExistingAtom;
                    Atom currentAtom = currentPlacement.ExistingAtom;

                    if (currentAtom == null)
                    {
                        Atom insertedAtom = AddAtomChain(previousAtom, currentPlacement.Position, ClockDirections.Nothing, Globals.PeriodicTable.C,
                                                  OrderSingle, BondStereo.None);
                        if (insertedAtom == null)
                        {
                            Debugger.Break();
                        }

                        currentPlacement.ExistingAtom = insertedAtom;
                    }
                    else if (previousAtom != null && previousAtom.BondBetween(currentAtom) == null)
                    {
                        AddNewBond(previousAtom, currentAtom, previousAtom.Parent, OrderSingle, BondStereo.None);
                    }
                }

                //join up the ring if there is no last bond
                Atom firstAtom = newAtomPlacements[0].ExistingAtom;
                Atom nextAtom = newAtomPlacements[1].ExistingAtom;
                if (firstAtom.BondBetween(nextAtom) == null)
                {
                    AddNewBond(firstAtom, nextAtom, firstAtom.Parent, OrderSingle, BondStereo.None);
                }
                //set the alternating single and double bonds if unsaturated
                if (unsaturated)
                {
                    MakeRingUnsaturated(newAtomPlacements);
                }

                firstAtom.Parent.RebuildRings();
                Action undo = () =>
                {
                    firstAtom.Parent.Refresh();
                    firstAtom.Parent.UpdateVisual();
                    _selectedItems.Clear();
                };
                Action redo = () =>
                {
                    firstAtom.Parent.Refresh();
                    firstAtom.Parent.UpdateVisual();
                };

                UndoManager.RecordAction(undo, redo);
                UndoManager.EndUndoBlock();

                //just refresh the atoms to be on the safe side
                foreach (var atomPlacement in newAtomPlacements)
                {
                    atomPlacement.ExistingAtom.UpdateVisual();
                }

                //local function
                void MakeRingUnsaturated(List<NewAtomPlacement> list)
                {
                    for (int i = startAt; i < list.Count + startAt; i++)
                    {
                        var firstIndex = i % list.Count;
                        var secondIndex = (i + 1) % list.Count;

                        Atom thisAtom = list[firstIndex].ExistingAtom;
                        Atom otherAtom = list[secondIndex].ExistingAtom;

                        if (!thisAtom.IsUnsaturated
                            && thisAtom.ImplicitHydrogenCount > 0
                            && !otherAtom.IsUnsaturated
                            && otherAtom.ImplicitHydrogenCount > 0)
                        {
                            Bond bondBetween = thisAtom.BondBetween(otherAtom);
                            if (bondBetween != null)
                            {
                                // Only do this if a bond was created / exists
                                SetBondAttributes(bondBetween, OrderDouble, Globals.BondStereo.None);
                                bondBetween.ExplicitPlacement = null;
                                bondBetween.UpdateVisual();
                            }
                            thisAtom.UpdateVisual();
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);
        }

        private string ListPoints(List<Point> placements)
        {
            var lines = new List<string>();

            int count = 0;

            foreach (var placement in placements)
            {
                var line = new StringBuilder();
                line.Append($"{count++} - ");
                line.Append($"{PointHelper.AsString(placement)}");

                lines.Add(line.ToString());
            }

            return string.Join(Environment.NewLine, lines);
        }

        public void DrawChain(List<Point> placements, Atom startAtom = null)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                var atomInfo = startAtom == null ? "{null}" : $"{startAtom.SymbolText} @ {PointHelper.AsString(startAtom.Position)}";
                var count = placements == null ? "{null}" : $"{placements.Count}";
                WriteTelemetry(module, "Debug", $"Atoms: {count}; StartAtom: {atomInfo}");
                WriteTelemetry(module, "Debug", ListPoints(placements));

                UndoManager.BeginUndoBlock();
                Atom lastAtom = startAtom;
                if (startAtom == null) //we're drawing an isolated chain
                {
                    lastAtom = AddAtomChain(null, placements[0], ClockDirections.Nothing, bondOrder: OrderSingle,
                                            stereo: BondStereo.None);
                    if (lastAtom == null)
                    {
                        Debugger.Break();
                    }
                }

                foreach (Point placement in placements.Skip(1))
                {
                    lastAtom = AddAtomChain(lastAtom, placement, ClockDirections.Nothing, bondOrder: OrderSingle,
                                            stereo: BondStereo.None);
                    if (lastAtom == null)
                    {
                        Debugger.Break();
                    }
                    if (placement != placements.Last())
                    {
                        lastAtom.ExplicitC = null;
                    }
                }

                if (startAtom != null)
                {
                    startAtom.UpdateVisual();
                    foreach (var bond in startAtom.Bonds)
                    {
                        bond.UpdateVisual();
                    }
                }

                UndoManager.EndUndoBlock();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);
        }

        private void DeleteMolecules(IEnumerable<Molecule> mols)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                UndoManager.BeginUndoBlock();

                foreach (Molecule mol in mols)
                {
                    DeleteMolecule(mol);
                }

                UndoManager.EndUndoBlock();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        public void DeleteMolecule(Molecule mol)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                Action redo = () =>
                              {
                                  RemoveFromSelection(mol);
                                  Model.RemoveMolecule(mol);
                                  mol.Parent = null;
                              };

                Action undo = () =>
                              {
                                  mol.Parent = Model;
                                  Model.AddMolecule(mol);
                                  AddToSelection(mol);
                              };

                UndoManager.BeginUndoBlock();
                UndoManager.RecordAction(undo, redo);
                UndoManager.EndUndoBlock();

                redo();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);
        }

        public void AddHydrogens()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                List<Atom> targetAtoms = new List<Atom>();
                var mols = SelectedItems.OfType<Molecule>().ToList();
                if (mols.Any())
                {
                    foreach (var mol in mols)
                    {
                        foreach (var atom in mol.Atoms.Values)
                        {
                            if (atom.ImplicitHydrogenCount > 0)
                            {
                                targetAtoms.Add(atom);
                            }
                        }
                    }
                }
                else
                {
                    foreach (var atom in Model.GetAllAtoms())
                    {
                        if (atom.ImplicitHydrogenCount > 0)
                        {
                            targetAtoms.Add(atom);
                        }
                    }
                }

                if (targetAtoms.Any())
                {
                    List<Atom> newAtoms = new List<Atom>();
                    List<Bond> newBonds = new List<Bond>();
                    Dictionary<string, Molecule> parents = new Dictionary<string, Molecule>();
                    foreach (var atom in targetAtoms)
                    {
                        double separation = 90.0;
                        if (atom.Bonds.Count() > 1)
                        {
                            separation = 30.0;
                        }

                        int hydrogenCount = atom.ImplicitHydrogenCount;
                        var vector = atom.BalancingVector();

                        switch (hydrogenCount)
                        {
                            case 1:
                                // Use balancing vector as is
                                break;

                            case 2:
                                Matrix matrix1 = new Matrix();
                                matrix1.Rotate(-separation / 2);
                                vector = vector * matrix1;
                                break;

                            case 3:
                                Matrix matrix2 = new Matrix();
                                matrix2.Rotate(-separation);
                                vector = vector * matrix2;
                                break;

                            case 4:
                                // Use default balancing vector (Screen.North) as is
                                break;
                        }

                        Matrix matrix3 = new Matrix();
                        matrix3.Rotate(separation);

                        for (int i = 0; i < hydrogenCount; i++)
                        {
                            if (i > 0)
                            {
                                vector = vector * matrix3;
                            }

                            var aa = new Atom
                            {
                                Element = Globals.PeriodicTable.H,
                                Position = atom.Position +
                                                    vector * (Model.XamlBondLength * ExplicitHydrogenBondPercentage)
                            };
                            newAtoms.Add(aa);
                            if (!parents.ContainsKey(aa.InternalId))
                            {
                                parents.Add(aa.InternalId, atom.Parent);
                            }

                            var bb = new Bond
                            {
                                StartAtomInternalId = atom.InternalId,
                                EndAtomInternalId = aa.InternalId,
                                Stereo = BondStereo.None,
                                Order = "S"
                            };
                            newBonds.Add(bb);
                            if (!parents.ContainsKey(bb.InternalId))
                            {
                                parents.Add(bb.InternalId, atom.Parent);
                            }
                        }
                    }

                    Action undoAction = () =>
                    {
                        foreach (var bond in newBonds)
                        {
                            bond.Parent.RemoveBond(bond);
                        }

                        foreach (var atom in newAtoms)
                        {
                            atom.Parent.RemoveAtom(atom);
                        }

                        if (mols.Any())
                        {
                            RefreshMolecules(mols);
                        }
                        else
                        {
                            RefreshMolecules(Model.Molecules.Values.ToList());
                        }

                        _selectedItems.Clear();
                    };

                    Action redoAction = () =>
                    {
                        Model.InhibitEvents = true;

                        foreach (var atom in newAtoms)
                        {
                            parents[atom.InternalId].AddAtom(atom);
                            atom.Parent = parents[atom.InternalId];
                        }

                        foreach (var bond in newBonds)
                        {
                            parents[bond.InternalId].AddBond(bond);
                            bond.Parent = parents[bond.InternalId];
                        }

                        Model.InhibitEvents = false;

                        if (mols.Any())
                        {
                            RefreshMolecules(mols);
                        }
                        else
                        {
                            RefreshMolecules(Model.Molecules.Values.ToList());
                        }

                        _selectedItems.Clear();
                    };

                    UndoManager.BeginUndoBlock();
                    UndoManager.RecordAction(undoAction, redoAction);
                    UndoManager.EndUndoBlock();

                    redoAction();
                }
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);
        }

        private void RefreshMolecules(List<Molecule> mols)
        {
            foreach (var mol in mols)
            {
                mol.UpdateVisual();
            }
        }

        public void RemoveHydrogens()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                HydrogenTargets targets;
                var molecules = SelectedItems.OfType<Molecule>().ToList();
                if (molecules.Any())
                {
                    targets = Model.GetHydrogenTargets(molecules);
                }
                else
                {
                    targets = Model.GetHydrogenTargets();
                }

                if (targets.Atoms.Any())
                {
                    Action undoAction = () =>
                    {
                        Model.InhibitEvents = true;

                        foreach (var atom in targets.Atoms)
                        {
                            targets.Molecules[atom.InternalId].AddAtom(atom);
                            atom.Parent = targets.Molecules[atom.InternalId];
                        }

                        foreach (var bond in targets.Bonds)
                        {
                            targets.Molecules[bond.InternalId].AddBond(bond);
                            bond.Parent = targets.Molecules[bond.InternalId];
                        }

                        Model.InhibitEvents = false;

                        if (molecules.Any())
                        {
                            RefreshMolecules(molecules);
                        }
                        else
                        {
                            RefreshMolecules(Model.Molecules.Values.ToList());
                        }

                        _selectedItems.Clear();
                    };

                    Action redoAction = () =>
                    {
                        foreach (var bond in targets.Bonds)
                        {
                            bond.Parent.RemoveBond(bond);
                        }

                        foreach (var atom in targets.Atoms)
                        {
                            atom.Parent.RemoveAtom(atom);
                        }

                        if (molecules.Any())
                        {
                            RefreshMolecules(molecules);
                        }
                        else
                        {
                            RefreshMolecules(Model.Molecules.Values.ToList());
                        }

                        _selectedItems.Clear();
                    };

                    UndoManager.BeginUndoBlock();
                    UndoManager.RecordAction(undoAction, redoAction);
                    UndoManager.EndUndoBlock();

                    redoAction();
                }
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);
        }

        public bool SingleMolSelected
        {
            get { return SelectedItems.Count == 1 && SelectedItems[0] is Molecule; }
        }

        public void FlipMolecule(Molecule selMolecule, bool flipVertically, bool flipStereo)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                int scaleX = 1;
                int scaleY = 1;

                if (flipVertically)
                {
                    scaleY = -1;
                }
                else
                {
                    scaleX = -1;
                }

                var bb = selMolecule.BoundingBox;

                double cx = bb.Left + (bb.Right - bb.Left) / 2;
                double cy = bb.Top + (bb.Bottom - bb.Top) / 2;

                ScaleTransform flipTransform = new ScaleTransform(scaleX, scaleY, cx, cy);

                Action undo = () =>
                {
                    selMolecule.Transform(flipTransform);

                    InvertPlacements(selMolecule);
                    selMolecule.UpdateVisual();
                    if (flipStereo)
                    {
                        InvertStereo(selMolecule);
                    }
                };

                Action redo = () =>
                {
                    selMolecule.Transform(flipTransform);

                    InvertPlacements(selMolecule);
                    selMolecule.UpdateVisual();
                    if (flipStereo)
                    {
                        InvertStereo(selMolecule);
                    }
                };

                UndoManager.BeginUndoBlock();
                UndoManager.RecordAction(undo, redo, flipVertically ? "Flip Vertical" : "Flip Horizontal");
                UndoManager.EndUndoBlock();

                redo();

                //local function
                void InvertStereo(Molecule m)
                {
                    foreach (Bond bond in m.Bonds)
                    {
                        if (bond.Stereo == BondStereo.Wedge)
                        {
                            bond.Stereo = BondStereo.Hatch;
                        }
                        else if (bond.Stereo == BondStereo.Hatch)
                        {
                            bond.Stereo = BondStereo.Wedge;
                        }
                    }
                }

                //local function
                void InvertPlacements(Molecule m)
                {
                    var ringBonds = from b in m.Bonds
                                    where b.Rings.Any()
                                          && b.OrderValue <= 2.5
                                          && b.OrderValue >= 1.5
                                    select b;
                    foreach (Bond ringBond in ringBonds)
                    {
                        if (ringBond.ExplicitPlacement != null)
                        {
                            ringBond.ExplicitPlacement = (BondDirection)(-(int)ringBond.ExplicitPlacement);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        public void AddToSelection(object thingToAdd)
        {
            var parent = (thingToAdd as Atom)?.Parent ?? (thingToAdd as Bond)?.Parent;
            var thingsToAdd = new List<object> { thingToAdd };
            if (parent != null)
            {
                if (!SelectedItems.Contains(parent))
                {
                    AddToSelection(thingsToAdd);
                }
            }
            else
            {
                AddToSelection(thingsToAdd);
            }
        }

        public void AddToSelection(List<object> thingsToAdd)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            DebugHelper.WriteLine($"Started at {SafeDate.ToShortTime(DateTime.Now)}");

            //take a snapshot of the current selection
            var currentSelection = SelectedItems.ToList();
            //add all the new items to the existing selection
            var allItems = currentSelection.Union(thingsToAdd).ToList();

            DebugHelper.WriteLine($"Timing: {sw.ElapsedMilliseconds}ms");

            //phase one - group atoms into the molecules
            //grab all parent molecules for selected atoms
            var allParents = (from a in allItems.OfType<Atom>()
                              group a by a.Parent
                               into parent
                              select new
                              {
                                  Parent = parent.Key,
                                  Count = parent.Count()
                              }).ToList();

            //and grab all of those that have all atoms selected
            var fullParents = (from m in allParents
                               where m.Count == m.Parent.AtomCount
                               select m.Parent).ToList();

            DebugHelper.WriteLine($"Timing: {sw.ElapsedMilliseconds}ms");

            //now add all the molecules that haven't been selected
            //first clear out the atoms
            foreach (var fm in fullParents)
            {
                foreach (var atom in fm.Atoms.Values)
                {
                    _selectedItems.Remove(atom);
                    thingsToAdd.Remove(atom);
                }

                foreach (Bond bond in fm.Bonds)
                {
                    _selectedItems.Remove(bond);
                    thingsToAdd.Remove(bond);
                }
                //and add in the selected parent
                if (!_selectedItems.Contains(fm.RootMolecule))
                {
                    _selectedItems.Add(fm.RootMolecule);
                }
            }

            DebugHelper.WriteLine($"Timing: {sw.ElapsedMilliseconds}ms");

            var newMols = thingsToAdd.OfType<Molecule>().ToList();
            foreach (var molecule in newMols)
            {
                if (!_selectedItems.Contains(molecule.RootMolecule))
                {
                    _selectedItems.Add(molecule);
                }
                thingsToAdd.Remove(molecule);
            }

            DebugHelper.WriteLine($"Timing: {sw.ElapsedMilliseconds}ms");

            //now we need to process remaining individual atoms
            var newAtoms = thingsToAdd.OfType<Atom>().ToList();
            foreach (var newAtom in newAtoms)
            {
                if (!_selectedItems.Contains(newAtom))
                {
                    _selectedItems.Add(newAtom);
                    thingsToAdd.Remove(newAtom);
                    //add in the bonds between this atom and any other selected atoms
                    foreach (Bond bond in newAtom.Bonds)
                    {
                        if (!(_selectedItems.Contains(bond)) && _selectedItems.Contains(bond.OtherAtom(newAtom)))
                        {
                            _selectedItems.Add(bond);
                            if (thingsToAdd.Contains(bond))
                            {
                                thingsToAdd.Remove(bond);
                            }
                        }
                    }
                }
            }

            DebugHelper.WriteLine($"Timing: {sw.ElapsedMilliseconds}ms");

            //now add in any remaining bonds
            var newBonds = thingsToAdd.OfType<Bond>().ToList();

            foreach (Bond newBond in newBonds)
            {
                if (!(_selectedItems.Contains(newBond)
                      || _selectedItems.Contains(newBond.Parent.RootMolecule)))
                {
                    _selectedItems.Add(newBond);
                    if (thingsToAdd.Contains(newBond))
                    {
                        thingsToAdd.Remove(newBond);
                    }
                }
            }

            if (CurrentEditor != null)
            {
                UpdateAtomBondAdorners();
            }

            DebugHelper.WriteLine($"Timing: {sw.ElapsedMilliseconds}ms");
            DebugHelper.WriteLine($"Finished at {DateTime.Now}");
        }

        public void ClearSelection()
        {
            _selectedItems.Clear();
        }

        public void RemoveFromSelection(object thingToRemove)
        {
            RemoveFromSelection(new List<object> { thingToRemove });
        }

        private void RemoveFromSelection(List<object> thingsToRemove)
        {
            // grab all the molecules that contain selected objects
            foreach (object o in thingsToRemove)
            {
                switch (o)
                {
                    case Atom atom:
                        {
                            if (atom.Singleton) //it's a single atom molecule
                            {
                                _selectedItems.Remove(atom.Parent);
                            }

                            if (_selectedItems.Contains(atom))
                            {
                                _selectedItems.Remove(atom);
                            }

                            break;
                        }

                    case Bond bond:
                        {
                            if (_selectedItems.Contains(bond))
                            {
                                _selectedItems.Remove(bond);
                            }

                            break;
                        }

                    case Molecule mol:
                        {
                            if (_selectedItems.Contains(mol))
                            {
                                _selectedItems.Remove(mol);
                            }

                            break;
                        }
                }
            }

            if (CurrentEditor != null)
            {
                UpdateAtomBondAdorners();
            }
        }

        public void JoinMolecules(Atom a, Atom b, string currentOrder, BondStereo currentStereo)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                var startAtomInfo = "{null}";
                if (a != null)
                {
                    if (a.Element == null)
                    {
                        startAtomInfo = $"Null @ {PointHelper.AsString(a.Position)}";
                    }
                    else
                    {
                        startAtomInfo = $"{a.SymbolText} @ {PointHelper.AsString(a.Position)}";
                    }
                }
                var endAtomInfo = "{null}";
                if (b != null)
                {
                    if (b.Element == null)
                    {
                        endAtomInfo = $"Null @ {PointHelper.AsString(b.Position)}";
                    }
                    else
                    {
                        endAtomInfo = $"{b.SymbolText} @ {PointHelper.AsString(b.Position)}";
                    }
                }

                var orderInfo = currentOrder ?? CurrentBondOrder;
                WriteTelemetry(module, "Debug", $"StartAtom: {startAtomInfo}; EndAtom: {endAtomInfo}; BondOrder; {orderInfo}");

                Molecule molA = a.Parent;
                Molecule molB = b.Parent;
                Molecule newMol = null;
                var parent = molA.Parent;

                Action redo = () =>
                              {
                                  Bond bond = new Bond(a, b);
                                  bond.Order = currentOrder;
                                  bond.Stereo = currentStereo;
                                  newMol = Molecule.Join(molA, molB, bond);
                                  newMol.Parent = parent;
                                  parent.AddMolecule(newMol);
                                  parent.RemoveMolecule(molA);
                                  molA.Parent = null;
                                  parent.RemoveMolecule(molB);
                                  molB.Parent = null;
                                  newMol.Model.Relabel(false);
                                  newMol.UpdateVisual();
                              };

                Action undo = () =>
                              {
                                  molA.Parent = parent;
                                  molA.Reparent();
                                  parent.AddMolecule(molA);
                                  molB.Parent = parent;
                                  molB.Reparent();
                                  parent.AddMolecule(molB);
                                  parent.RemoveMolecule(newMol);
                                  newMol.Parent = null;

                                  molA.UpdateVisual();
                                  molB.UpdateVisual();
                              };

                UndoManager.BeginUndoBlock();
                UndoManager.RecordAction(undo, redo);
                UndoManager.EndUndoBlock();

                redo();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);
        }

        public void DeleteAtoms(IEnumerable<Atom> atoms)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                var countString = atoms == null ? "{null}" : $"{atoms.Count()}";
                WriteTelemetry(module, "Debug", $"Atoms: {countString}");

                var atomList = atoms.ToArray();
                //Add all the selected atoms to a set A
                if (atomList.Length == 1 && atomList[0].Singleton)
                {
                    var delAtom = atomList[0];
                    var molecule = delAtom.Parent;
                    Model model = molecule.Model;

                    Action redo = () =>
                                  {
                                      model.RemoveMolecule(molecule);
                                  };
                    Action undo = () =>
                                  {
                                      model.AddMolecule(molecule);
                                  };

                    UndoManager.BeginUndoBlock();
                    UndoManager.RecordAction(undo, redo, $"{nameof(DeleteAtoms)}[Singleton]");
                    UndoManager.EndUndoBlock();

                    redo();
                }
                else
                {
                    DeleteAtomsAndBonds(atomList);
                }
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);
        }

        /// <summary>
        /// Deletes a list of atoms and bonds, splitting them into separate molecules if required
        /// </summary>
        /// <param name="atomlist"></param>
        /// <param name="bondList"></param>
        ///
        public void DeleteAtomsAndBonds(IEnumerable<Atom> atomlist = null, IEnumerable<Bond> bondList = null)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                var count1 = atomlist == null ? "{null}" : $"{atomlist.Count()}";
                var count2 = bondList == null ? "{null}" : $"{bondList.Count()}";

                WriteTelemetry(module, "Debug", $"Atoms: {count1}; Bonds: {count2}");

                HashSet<Atom> deleteAtoms = new HashSet<Atom>();
                HashSet<Bond> deleteBonds = new HashSet<Bond>();
                HashSet<Atom> neighbours = new HashSet<Atom>();

                if (atomlist != null)
                {
                    //Add all the selected atoms to a set A
                    foreach (Atom atom in atomlist)
                    {
                        deleteAtoms.Add(atom);

                        foreach (Bond bond in atom.Bonds)
                        {
                            //Add all the selected atoms' bonds to B
                            deleteBonds.Add(bond);
                            //Add start and end atoms B1s and B1E to neighbours
                            neighbours.Add(bond.StartAtom);
                            neighbours.Add(bond.EndAtom);
                        }
                    }
                }

                if (bondList != null)
                {
                    foreach (var bond in bondList)
                    {
                        //Add all the selected bonds to deleteBonds
                        deleteBonds.Add(bond);
                        //Add start and end atoms B1s and B1E to neighbours
                        neighbours.Add(bond.StartAtom);
                        neighbours.Add(bond.EndAtom);
                    }
                }

                //ignore the atoms we are going to delete anyway
                neighbours.ExceptWith(deleteAtoms);
                HashSet<Atom> updateAtoms = new HashSet<Atom>(neighbours);

                List<HashSet<Atom>> atomGroups = new List<HashSet<Atom>>();
                Molecule mol = null;

                //now, take groups of connected atoms from the remaining graph ignoring the excluded bonds
                while (neighbours.Count > 0)
                {
                    HashSet<Atom> atomGroup = new HashSet<Atom>();

                    //TODO: sort out the grouping of atoms
                    var firstAtom = neighbours.First();
                    mol = firstAtom.Parent;
                    mol.TraverseBFS(firstAtom, a1 => { atomGroup.Add(a1); }, a2 => !atomGroup.Contains(a2), deleteBonds);
                    atomGroups.Add(atomGroup);
                    //remove the list of atoms from the atom group
                    neighbours.ExceptWith(atomGroup);
                }

                //now, check to see whether there is a single atomgroup.  If so, then we still have one molecule
                if (atomGroups.Count == 1)
                {
                    MoleculePropertyBag mpb = new MoleculePropertyBag();
                    mpb.Store(mol);

                    Dictionary<Atom, bool?> explicitFlags = new Dictionary<Atom, bool?>();
                    foreach (Bond deleteBond in deleteBonds)
                    {
                        if (!explicitFlags.ContainsKey(deleteBond.StartAtom))
                        {
                            explicitFlags[deleteBond.StartAtom] = deleteBond.StartAtom.ExplicitC;
                        }
                        if (!explicitFlags.ContainsKey(deleteBond.EndAtom))
                        {
                            explicitFlags[deleteBond.EndAtom] = deleteBond.EndAtom.ExplicitC;
                        }
                    }

                    Action redo = () =>
                    {
                        _selectedItems.Clear();
                        int theoreticalRings = mol.TheoreticalRings;
                        foreach (Bond deleteBond in deleteBonds)
                        {
                            mol.RemoveBond(deleteBond);
                            RefreshRingBonds(theoreticalRings, mol, deleteBond);

                            deleteBond.StartAtom.ExplicitC = null;
                            deleteBond.StartAtom.UpdateVisual();

                            deleteBond.EndAtom.ExplicitC = null;
                            deleteBond.EndAtom.UpdateVisual();

                            foreach (Bond atomBond in deleteBond.StartAtom.Bonds)
                            {
                                atomBond.UpdateVisual();
                            }

                            foreach (Bond atomBond in deleteBond.EndAtom.Bonds)
                            {
                                atomBond.UpdateVisual();
                            }
                        }

                        foreach (Atom deleteAtom in deleteAtoms)
                        {
                            mol.RemoveAtom(deleteAtom);
                        }

                        mol.ClearProperties();
                        RefreshAtomVisuals(updateAtoms);
                    };

                    Action undo = () =>
                    {
                        _selectedItems.Clear();
                        foreach (Atom restoreAtom in deleteAtoms)
                        {
                            mol.AddAtom(restoreAtom);
                            restoreAtom.UpdateVisual();
                            AddToSelection(restoreAtom);
                        }

                        foreach (Bond restoreBond in deleteBonds)
                        {
                            int theoreticalRings = mol.TheoreticalRings;
                            mol.AddBond(restoreBond);

                            restoreBond.StartAtom.ExplicitC = explicitFlags[restoreBond.StartAtom];
                            restoreBond.StartAtom.UpdateVisual();
                            restoreBond.EndAtom.ExplicitC = explicitFlags[restoreBond.EndAtom];
                            restoreBond.EndAtom.UpdateVisual();

                            RefreshRingBonds(theoreticalRings, mol, restoreBond);

                            foreach (Bond atomBond in restoreBond.StartAtom.Bonds)
                            {
                                atomBond.UpdateVisual();
                            }

                            foreach (Bond atomBond in restoreBond.EndAtom.Bonds)
                            {
                                atomBond.UpdateVisual();
                            }

                            restoreBond.UpdateVisual();

                            AddToSelection(restoreBond);
                        }

                        mpb.Restore(mol);
                    };

                    UndoManager.BeginUndoBlock();
                    UndoManager.RecordAction(undo, redo, $"{nameof(DeleteAtomsAndBonds)}[SingleAtom]");
                    UndoManager.EndUndoBlock();

                    redo();
                }
                else //we have multiple fragments
                {
                    List<Molecule> newMolecules = new List<Molecule>();
                    List<Molecule> oldMolecules = new List<Molecule>();

                    Dictionary<Atom, bool?> explicitFlags = new Dictionary<Atom, bool?>();

                    //add all the relevant atoms and bonds to a new molecule
                    //grab the model for future reference
                    Model parentModel = null;
                    foreach (HashSet<Atom> atomGroup in atomGroups)
                    {
                        //assume that all atoms share the same parent model & molecule
                        var parent = atomGroup.First().Parent;
                        if (parentModel == null)
                        {
                            parentModel = parent.Model;
                        }

                        if (!oldMolecules.Contains(parent))
                        {
                            oldMolecules.Add(parent);
                        }

                        Molecule newMolecule = new Molecule();

                        foreach (Atom atom in atomGroup)
                        {
                            newMolecule.AddAtom(atom);
                            var bondsToAdd = from Bond bond in atom.Bonds
                                             where !newMolecule.Bonds.Contains(bond) && !deleteBonds.Contains(bond)
                                             select bond;
                            foreach (Bond bond in bondsToAdd)
                            {
                                newMolecule.AddBond(bond);
                            }
                        }

                        newMolecule.Parent = parentModel;
                        newMolecule.Reparent();
                        newMolecules.Add(newMolecule);
                        newMolecule.RebuildRings();

                        // Clear explicit flag on a lone atom
                        if (newMolecule.AtomCount == 1)
                        {
                            var loneAtom = newMolecule.Atoms.Values.First();
                            explicitFlags[loneAtom] = loneAtom.ExplicitC;
                            loneAtom.ExplicitC = null;
                        }

                        //add the molecule to the model
                        parentModel.AddMolecule(newMolecule);
                    }

                    foreach (Molecule oldMolecule in oldMolecules)
                    {
                        parentModel.RemoveMolecule(oldMolecule);
                        oldMolecule.Parent = null;
                    }

                    //refresh the neighbouring atoms
                    RefreshAtomVisuals(updateAtoms);

                    Action undo = () =>
                    {
                        _selectedItems.Clear();
                        foreach (Molecule oldMol in oldMolecules)
                        {
                            oldMol.Reparent();
                            oldMol.Parent = parentModel;

                            foreach (var atom in oldMol.Atoms.Values)
                            {
                                if (explicitFlags.ContainsKey(atom))
                                {
                                    atom.ExplicitC = explicitFlags[atom];
                                }
                            }
                            parentModel.AddMolecule(oldMol);

                            oldMol.UpdateVisual();
                        }

                        foreach (Molecule newMol in newMolecules)
                        {
                            parentModel.RemoveMolecule(newMol);
                            newMol.Parent = null;
                        }

                        RefreshAtomVisuals(updateAtoms);
                    };

                    Action redo = () =>
                    {
                        _selectedItems.Clear();
                        foreach (Molecule newmol in newMolecules)
                        {
                            newmol.Reparent();
                            newmol.Parent = parentModel;

                            if (newmol.AtomCount == 1)
                            {
                                newmol.Atoms.Values.First().ExplicitC = null;
                            }

                            parentModel.AddMolecule(newmol);
                            newmol.UpdateVisual();
                        }

                        foreach (Molecule oldMol in oldMolecules)
                        {
                            parentModel.RemoveMolecule(oldMol);
                            oldMol.Parent = null;
                        }

                        RefreshAtomVisuals(updateAtoms);
                    };

                    UndoManager.BeginUndoBlock();
                    UndoManager.RecordAction(undo, redo, $"{nameof(DeleteAtomsAndBonds)}[MultipleFragments]");
                    UndoManager.EndUndoBlock();
                }
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);

            // Local Function
            void RefreshRingBonds(int theoreticalRings, Molecule molecule, Bond deleteBond)
            {
                if (theoreticalRings != molecule.TheoreticalRings)
                {
                    molecule.RebuildRings();
                    foreach (Ring bondRing in deleteBond.Rings)
                    {
                        foreach (Bond bond in bondRing.Bonds)
                        {
                            bond.UpdateVisual();
                        }
                    }
                }
            }
        }

        private void RefreshAtomVisuals(HashSet<Atom> updateAtoms)
        {
            foreach (Atom updateAtom in updateAtoms)
            {
                updateAtom.UpdateVisual();
                foreach (Bond updateAtomBond in updateAtom.Bonds)
                {
                    updateAtomBond.UpdateVisual();
                }
            }
        }

        public void DeleteBonds(IEnumerable<Bond> bonds)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            var countString = bonds == null ? "{null}" : $"{bonds.Count()}";
            WriteTelemetry(module, "Debug", $"Bonds {countString}");

            DeleteAtomsAndBonds(bondList: bonds);

            CheckModelIntegrity(module);
        }

        public void UpdateAtom(Atom atom, AtomPropertiesModel model)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                ElementBase elementBaseBefore = atom.Element;
                int? chargeBefore = atom.FormalCharge;
                int? isotopeBefore = atom.IsotopeNumber;
                bool? explicitCBefore = atom.ExplicitC;

                ElementBase elementBaseAfter = model.Element;
                int? chargeAfter = null;
                int? isotopeAfter = null;
                bool? explicitCAfter = null;

                if (elementBaseAfter is Element)
                {
                    chargeAfter = model.Charge;
                    explicitCAfter = model.ExplicitC;
                    if (!string.IsNullOrEmpty(model.Isotope))
                    {
                        isotopeAfter = int.Parse(model.Isotope);
                    }
                }

                Action redo = () =>
                              {
                                  atom.Element = elementBaseAfter;
                                  atom.FormalCharge = chargeAfter;
                                  atom.IsotopeNumber = isotopeAfter;
                                  atom.ExplicitC = explicitCAfter;
                                  atom.Parent.UpdateVisual();
                                  //freshen any selection adorner
                                  if (SelectedItems.Contains(atom))
                                  {
                                      RemoveFromSelection(atom);
                                      AddToSelection(atom);
                                  }
                              };

                Action undo = () =>
                              {
                                  atom.Element = elementBaseBefore;
                                  atom.FormalCharge = chargeBefore;
                                  atom.IsotopeNumber = isotopeBefore;
                                  atom.ExplicitC = explicitCBefore;
                                  atom.Parent.UpdateVisual();
                                  //freshen any selection adorner
                                  if (SelectedItems.Contains(atom))
                                  {
                                      RemoveFromSelection(atom);
                                      AddToSelection(atom);
                                  }
                              };

                UndoManager.BeginUndoBlock();
                UndoManager.RecordAction(undo, redo);
                UndoManager.EndUndoBlock();

                redo();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);
        }

        public void UpdateMolecule(Molecule target, Molecule source)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                bool? showBefore = target.ShowMoleculeBrackets;
                bool? showAfter = source.ShowMoleculeBrackets;
                int? chargeBefore = target.FormalCharge;
                int? chargeAfter = source.FormalCharge;
                int? countBefore = target.Count;
                int? countAfter = source.Count;
                int? spinBefore = target.SpinMultiplicity;
                int? spinAfter = source.SpinMultiplicity;

                //caches the properties for undo/redo
                Dictionary<string, MoleculePropertyBag> sourceProps = new Dictionary<string, MoleculePropertyBag>();

                Action redo = () =>
                {
                    target.ShowMoleculeBrackets = showAfter;
                    target.FormalCharge = chargeAfter;
                    target.Count = countAfter;
                    target.SpinMultiplicity = spinAfter;

                    StashProperties(source, sourceProps);
                    UnstashProperties(target, sourceProps);
                };

                Action undo = () =>
                {
                    target.ShowMoleculeBrackets = showBefore;
                    target.FormalCharge = chargeBefore;
                    target.Count = countBefore;
                    target.SpinMultiplicity = spinBefore;

                    UnstashProperties(target, sourceProps);
                };

                UndoManager.BeginUndoBlock();
                UndoManager.RecordAction(undo, redo);
                UndoManager.EndUndoBlock();

                redo();

                //local function
                void StashProperties(Molecule mol, Dictionary<string, MoleculePropertyBag> propertyBags)
                {
                    MoleculePropertyBag bag = new MoleculePropertyBag();
                    bag.Store(mol);
                    propertyBags[mol.Id] = bag;
                    foreach (Molecule child in mol.Molecules.Values)
                    {
                        StashProperties(child, propertyBags);
                    }
                }

                //local function
                void UnstashProperties(Molecule mol, Dictionary<string, MoleculePropertyBag> propertyBags)
                {
                    MoleculePropertyBag bag = propertyBags[mol.Id];
                    bag.Restore(mol);
                    foreach (Molecule child in mol.Molecules.Values)
                    {
                        UnstashProperties(child, propertyBags);
                    }
                }
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        public void UpdateBond(Bond bond, BondPropertiesModel model)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                double bondOrderBefore = bond.OrderValue.Value;
                BondStereo stereoBefore = bond.Stereo;
                BondDirection? directionBefore = bond.ExplicitPlacement;

                double bondOrderAfter = model.BondOrderValue;
                BondStereo stereoAfter = BondStereo.None;
                BondDirection? directionAfter = null;

                var startAtom = bond.StartAtom;
                var endAtom = bond.EndAtom;

                bool swapAtoms = false;

                if (model.IsSingle)
                {
                    switch (model.SingleBondChoice)
                    {
                        case SingleBondType.None:
                            stereoAfter = BondStereo.None;
                            break;

                        case SingleBondType.Wedge:
                            stereoAfter = BondStereo.Wedge;
                            break;

                        case SingleBondType.BackWedge:
                            stereoAfter = BondStereo.Wedge;
                            swapAtoms = true;
                            break;

                        case SingleBondType.Hatch:
                            stereoAfter = BondStereo.Hatch;
                            break;

                        case SingleBondType.BackHatch:
                            stereoAfter = BondStereo.Hatch;
                            swapAtoms = true;
                            break;

                        case SingleBondType.Indeterminate:
                            stereoAfter = BondStereo.Indeterminate;
                            break;

                        default:
                            stereoAfter = BondStereo.None;
                            break;
                    }
                }

                if (model.Is1Point5 || model.Is2Point5 || model.IsDouble)
                {
                    if (model.DoubleBondChoice == DoubleBondType.Indeterminate)
                    {
                        stereoAfter = BondStereo.Indeterminate;
                    }
                    else
                    {
                        stereoAfter = BondStereo.None;
                        if (model.DoubleBondChoice != DoubleBondType.Auto)
                        {
                            directionAfter = (BondDirection)model.DoubleBondChoice;
                        }
                    }
                }

                Molecule mol = bond.Parent;
                RotateTransform transform = null;
                GeneralTransform inverse = null;
                bool singleBondTransform = false;
                Atom rotatedAtom = null;

                double angle;
                if (double.TryParse(model.BondAngle, out angle))
                {
                    if (angle >= -180 && angle <= 180)
                    {
                        var rotateBy = angle - bond.Angle;

                        if (Math.Abs(rotateBy) >= 0.005)
                        {
                            var startAtomBondCount = startAtom.Bonds.Count();
                            var endAtomBondCount = endAtom.Bonds.Count();

                            if (startAtomBondCount == 1 || endAtomBondCount == 1)
                            {
                                singleBondTransform = true;
                                if (startAtomBondCount == 1)
                                {
                                    transform = new RotateTransform(rotateBy, endAtom.Position.X, endAtom.Position.Y);
                                    rotatedAtom = startAtom;
                                    inverse = transform.Inverse;
                                }

                                if (endAtomBondCount == 1)
                                {
                                    transform = new RotateTransform(rotateBy, startAtom.Position.X, startAtom.Position.Y);
                                    rotatedAtom = endAtom;
                                    inverse = transform.Inverse;
                                }
                            }
                            else
                            {
                                var centroid = mol.Centroid;
                                transform = new RotateTransform(rotateBy, centroid.X, centroid.Y);
                                inverse = transform.Inverse;
                            }
                        }
                    }
                }

                Action redo = () =>
                {
                    bond.Order = OrderValueToOrder(bondOrderAfter);
                    bond.Stereo = stereoAfter;
                    bond.ExplicitPlacement = directionAfter;
                    bond.Parent.UpdateVisual();
                    if (swapAtoms)
                    {
                        bond.EndAtomInternalId = startAtom.InternalId;
                        bond.StartAtomInternalId = endAtom.InternalId;
                    }

                    bond.UpdateVisual();

                    if (transform != null)
                    {
                        if (singleBondTransform && rotatedAtom != null)
                        {
                            rotatedAtom.Position = transform.Transform(rotatedAtom.Position);
                            rotatedAtom.UpdateVisual();
                        }
                        else
                        {
                            mol.Transform((Transform)transform);
                            mol.UpdateVisual();
                        }
                        _selectedItems.Clear();
                    }
                };

                Action undo = () =>
                {
                    bond.Order = OrderValueToOrder(bondOrderBefore);
                    bond.Stereo = stereoBefore;
                    bond.ExplicitPlacement = directionBefore;
                    bond.Parent.UpdateVisual();
                    if (swapAtoms)
                    {
                        bond.StartAtomInternalId = startAtom.InternalId;
                        bond.EndAtomInternalId = endAtom.InternalId;
                    }

                    bond.UpdateVisual();

                    if (inverse != null)
                    {
                        if (singleBondTransform && rotatedAtom != null)
                        {
                            rotatedAtom.Position = inverse.Transform(rotatedAtom.Position);
                            rotatedAtom.UpdateVisual();
                        }
                        else
                        {
                            mol.Transform((Transform)inverse);
                            mol.UpdateVisual();
                        }
                        _selectedItems.Clear();
                    }
                };

                UndoManager.BeginUndoBlock();
                UndoManager.RecordAction(undo, redo);
                UndoManager.EndUndoBlock();

                redo();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);
        }

        public void PasteCML(string pastedCml)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            WriteTelemetry(module, "Debug", "Called");

            CMLConverter cc = new CMLConverter();
            Model buffer = cc.Import(pastedCml);
            PasteModel(buffer);
        }

        public void PasteModel(Model buffer)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");
                // Match to current model's settings
                buffer.Relabel(true);
                // above should be buffer.StripLabels(true)
                buffer.ScaleToAverageBondLength(Model.XamlBondLength);
                if (buffer.Molecules.Count > 1)
                {
                    Packer packer = new Packer();
                    packer.Model = buffer;
                    packer.Pack(Model.XamlBondLength * 2);
                }

                var molList = buffer.Molecules.Values.ToList();
                var abb = buffer.BoundingBoxWithFontSize;
                //grab the metrics of the editor's viewport
                var editorControlHorizontalOffset = EditorControl.HorizontalOffset;
                var editorControlViewportWidth = EditorControl.ViewportWidth;
                var editorControlVerticalOffset = EditorControl.VerticalOffset;
                var editorControlViewportHeight = EditorControl.ViewportHeight;
                //to center on the X coordinate, we need to set the left extent of the model to the horizontal offset
                //plus half the viewport width, minus half the model width
                //Similar for the height
                double leftCenter = editorControlHorizontalOffset + editorControlViewportWidth / 2;
                double topCenter = editorControlVerticalOffset + editorControlViewportHeight / 2;
                //these two coordinates now give us the point where the new model should be centered
                buffer.CenterOn(new Point(leftCenter, topCenter));

                Action undo = () =>
                {
                    foreach (var mol in molList)
                    {
                        RemoveFromSelection(mol);
                        Model.RemoveMolecule(mol);
                        mol.Parent = null;
                    }
                };
                Action redo = () =>
                {
                    _selectedItems.Clear();
                    foreach (var mol in molList)
                    {
                        mol.Parent = Model;
                        Model.AddMolecule(mol);
                        AddToSelection(mol);
                    }
                };

                UndoManager.BeginUndoBlock();
                UndoManager.RecordAction(undo, redo);
                UndoManager.EndUndoBlock();

                redo();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        public void DeleteSelection()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                var atoms = SelectedItems.OfType<Atom>().ToList();
                var bonds = SelectedItems.OfType<Bond>().ToList();
                var mols = SelectedItems.OfType<Molecule>().ToList();

                UndoManager.BeginUndoBlock();

                if (mols.Any())
                {
                    DeleteMolecules(mols);
                }

                if (atoms.Any() || bonds.Any())
                {
                    DeleteAtomsAndBonds(atoms, bonds);
                }

                _selectedItems.Clear();
                UndoManager.EndUndoBlock();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);
        }

        /// <summary>
        /// Ungroups selected molecules
        /// </summary>
        /// <param name="selection">Active selection within the editor</param>
        public void UnGroup(IEnumerable<object> selection)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                List<Molecule> selGroups;
                //grab just the grouped molecules first
                selGroups = (from Molecule mol in selection.OfType<Molecule>()
                             where mol.IsGrouped
                             select mol).ToList();
                UnGroup(selGroups);
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        private void UnGroup(List<Molecule> selGroups)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                //keep track of parent child relationships for later
                Dictionary<Molecule, List<Molecule>> parentsAndChildren = new Dictionary<Molecule, List<Molecule>>();

                foreach (Molecule selGroup in selGroups)
                {
                    parentsAndChildren[selGroup] = new List<Molecule>();
                    foreach (Molecule child in selGroup.Molecules.Values)
                    {
                        parentsAndChildren[selGroup].Add(child);
                    }
                }

                Action redo = () =>
                {
                    //selected groups are always top level objects
                    foreach (Molecule parent in parentsAndChildren.Keys)
                    {
                        RemoveFromSelection(parent);
                        Model.RemoveMolecule(parent);
                        foreach (var child in parentsAndChildren[parent])
                        {
                            child.Parent = Model;
                            Model.AddMolecule(child);
                            child.UpdateVisual();
                        }
                    }

                    foreach (List<Molecule> molecules in parentsAndChildren.Values)
                    {
                        foreach (Molecule child in molecules)
                        {
                            AddToSelection(child);
                        }
                    }
                };

                Action undo = () =>
                {
                    foreach (var oldParent in parentsAndChildren)
                    {
                        Model.AddMolecule(oldParent.Key);
                        foreach (Molecule child in oldParent.Value)
                        {
                            RemoveFromSelection(child);
                            Model.RemoveMolecule(child);
                            child.Parent = oldParent.Key;
                            oldParent.Key.AddMolecule(child);
                            child.UpdateVisual();
                        }

                        oldParent.Key.UpdateVisual();
                    }

                    foreach (Molecule parent in parentsAndChildren.Keys)
                    {
                        AddToSelection(parent);
                    }
                };

                UndoManager.BeginUndoBlock();
                UndoManager.RecordAction(undo, redo);
                UndoManager.EndUndoBlock();

                redo();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);
        }

        /// <summary>
        ///  Creates a parent molecule and makes all the selected molecules children
        /// </summary>
        /// <param name="selection">Observable collection of ChemistryBase objects</param>
        public void Group(IEnumerable<object> selection)
        {
            //grab just the molecules (to be grouped)
            var children = (from Molecule mol in selection.OfType<Molecule>()
                            select mol).ToList();
            Group(children);
        }

        /// <summary>
        /// Creates a parent molecule and makes all the selected molecules children
        /// </summary>
        /// <param name="children">List of child molecules</param>
        private void Group(List<Molecule> children)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                Molecule parent = new Molecule();
                Action redo = () =>
                {
                    _selectedItems.Clear();
                    parent.Parent = Model;
                    Model.AddMolecule(parent);
                    var kids = children.ToArray();
                    foreach (var molecule in kids)
                    {
                        if (Model.Molecules.Values.Contains(molecule))
                        {
                            Model.RemoveMolecule(molecule);
                            molecule.Parent = parent;
                            parent.AddMolecule(molecule);
                        }
                    }

                    parent.UpdateVisual();
                    AddToSelection(parent);
                };

                Action undo = () =>
                {
                    _selectedItems.Clear();

                    Model.RemoveMolecule(parent);
                    parent.Parent = null;
                    var kids = parent.Molecules.Values.ToArray();
                    foreach (var child in kids)
                    {
                        if (parent.Molecules.Values.Contains(child))
                        {
                            parent.RemoveMolecule(child);

                            child.Parent = Model;
                            Model.AddMolecule(child);
                            child.UpdateVisual();
                            AddToSelection(child);
                        }
                    }
                };

                UndoManager.BeginUndoBlock();
                UndoManager.RecordAction(undo, redo);
                UndoManager.EndUndoBlock();

                redo();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);
        }

        /// <summary>
        /// Selects all visible molecules
        /// </summary>
        public void SelectAll()
        {
            ClearSelection();
            foreach (var mol in Model.Molecules.Values)
            {
                AddToSelection(mol);
            }
        }

        private void WriteTelemetry(string source, string level, string message)
        {
            Telemetry?.Write(source, level, message);
        }

        private void WriteTelemetryException(string source, Exception exception)
        {
            Debugger.Break();

            if (Telemetry != null)
            {
                Telemetry.Write(source, "Exception", exception.Message);
                Telemetry.Write(source, "Exception", exception.StackTrace);
            }
            else
            {
                RegistryHelper.StoreException(source, exception);
            }
        }

        private void CheckModelIntegrity(string module)
        {
#if DEBUG
            var integrity = Model.CheckIntegrity();
            if (integrity.Count > 0)
            {
                Telemetry?.Write(module, "Integrity", string.Join(Environment.NewLine, integrity));
            }
#endif
        }
    }
}