// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
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
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using System.Xml.XPath;
using Chem4Word.ACME.Adorners.Selectors;
using Chem4Word.ACME.Behaviors;
using Chem4Word.ACME.Commands;
using Chem4Word.ACME.Commands.Editing;
using Chem4Word.ACME.Commands.Grouping;
using Chem4Word.ACME.Commands.Layout.Alignment;
using Chem4Word.ACME.Commands.Layout.Flipping;
using Chem4Word.ACME.Commands.Reactions;
using Chem4Word.ACME.Commands.Sketching;
using Chem4Word.ACME.Commands.Undo;
using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Drawing.Visuals;
using Chem4Word.ACME.Enums;
using Chem4Word.ACME.Models;
using Chem4Word.ACME.Utils;
using Chem4Word.Core.Enums;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Wpf;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Enums;
using Chem4Word.Model2.Geometry;
using Chem4Word.Model2.Helpers;
using IChem4Word.Contracts;
using static Chem4Word.Model2.Helpers.Globals;
using Constants = Chem4Word.ACME.Resources.Constants;

namespace Chem4Word.ACME
{
    /// <summary>
    /// The master brain of ACME. All editing operations arise from this class.
    /// We use commands to perform instantaneous operations, such as deletion.
    /// We use behaviors to put the editor into one mode or another
    /// </summary>
    public class EditController : Controller, INotifyPropertyChanged
    {
        private const int BlockTextPadding = 10;
        private static readonly string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static readonly string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        #region Fields

        private readonly Dictionary<object, Adorner> SelectionAdorners = new Dictionary<object, Adorner>();
        public MultiAtomBondAdorner MultiAdorner { get; private set; }
        private Dictionary<int, BondOption> _bondOptions = new Dictionary<int, BondOption>();
        private int? _selectedBondOptionId;

        public AcmeOptions EditorOptions { get; set; }
        private IChem4WordTelemetry Telemetry { get; set; }
        private BaseEditBehavior _activeMode;

        #endregion Fields

        #region Properties

        public bool Loading { get; set; }

        //indicates whether a text editor is active
        public bool TextEditorIsActive { get; set; }

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
            get { return Common.BondThickness * Common.DefaultBondLineFactor; }
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
                if (SelectedItems.OfType<Reaction>().Any())
                {
                    result |= SelectionTypeCode.Reaction;
                }
                if (SelectedItems.OfType<Annotation>().Any())
                {
                    result |= SelectionTypeCode.Annotation;
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
        private ReactionType? _selectedReactionType;
        private ReactionVisual _selReactionVisual;

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

        public Editor EditorControl { get; set; }
        public EditorCanvas CurrentEditor { get; set; }
        public Canvas CurrentHostingCanvas { get; }
        public AnnotationEditor BlockEditor { get; }

        private bool _selectionIsSubscript;

        public bool SelectionIsSubscript
        {
            get
            {
                return _selectionIsSubscript;
            }
            set
            {
                _selectionIsSubscript = value;
                OnPropertyChanged();
            }
        }

        private bool _selectionIsSuperscript;

        public bool SelectionIsSuperscript
        {
            get
            {
                return _selectionIsSuperscript;
            }
            set
            {
                _selectionIsSuperscript = value;
                OnPropertyChanged();
            }
        }

        public ClipboardMonitor ClipboardMonitor { get; }

        public List<string> Used1DProperties { get; set; }

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

        public void MoveReaction(Reaction reaction, Point startPoint, Point endPoint)
        {
            var oldStart = reaction.TailPoint;
            var oldEnd = reaction.HeadPoint;

            Action redo = () =>
                {
                    reaction.TailPoint = startPoint;
                    reaction.HeadPoint = endPoint;
                    RemoveFromSelection(reaction);
                    AddToSelection(reaction);
                };
            Action undo = () =>
                {
                    reaction.TailPoint = oldStart;
                    reaction.HeadPoint = oldEnd;
                    RemoveFromSelection(reaction);
                    AddToSelection(reaction);
                };

            UndoManager.BeginUndoBlock();
            UndoManager.RecordAction(undo, redo);
            UndoManager.EndUndoBlock();
            redo();
        }

        public ReactionType? SelectedReactionType
        {
            get
            {
                var selectedReactionTypes = (from ro in SelectedReactions()
                                             select ro.ReactionType).Distinct().ToList();
                switch (selectedReactionTypes.Count)
                {
                    case 0:
                        return _selectedReactionType;

                    case 1:
                        return selectedReactionTypes[0];

                    default:
                        return null;
                }
            }
            set
            {
                _selectedReactionType = value;
                {
                    if (value != null)
                    {
                        SetReactionType(value.Value);
                    }
                }
            }
        }

        //sets all selected reactions to the currently applied type
        public void SetReactionType(ReactionType value, Reaction parentReaction = null)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                List<Reaction> reactions;
                if (parentReaction is null)
                {
                    reactions = SelectedReactions().ToList();
                }
                else
                {
                    reactions = new List<Reaction> { parentReaction };
                }
                var affectedReactions = reactions.Count;
                WriteTelemetry(module, "Debug", $"Type: {SelectedReactionType}; Affected Reactions {affectedReactions}");

                if (reactions.Any())
                {
                    UndoManager.BeginUndoBlock();

                    foreach (Reaction reaction in reactions)
                    {
                        Action redo = () =>
                         {
                             reaction.ReactionType = value;
                             if (SelectedReactions().Contains(reaction))
                             {
                                 RemoveFromSelection(reaction);
                                 AddToSelection(reaction);
                             }
                         };
                        var reactionType = reaction.ReactionType;
                        Action undo = () =>
                        {
                            reaction.ReactionType = reactionType;
                            if (SelectedReactions().Contains(reaction))
                            {
                                RemoveFromSelection(reaction);
                                AddToSelection(reaction);
                            }
                        };
                        UndoManager.RecordAction(undo, redo);
                        redo();
                    }

                    UndoManager.EndUndoBlock();
                }
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        private List<Reaction> SelectedReactions()
        {
            var selectedReactions = SelectedItems.OfType<Reaction>();
            return selectedReactions.ToList();
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

        public bool IsDirty => UndoManager.CanUndo;

        private AnnotationEditor _annotationEditor;
        private bool _isBlockEditing;
        private const string EditingTextStatus = "[Shift-Enter] = new line; [Enter] = save text; [Esc] = cancel editing. ";

        public AnnotationEditor ActiveBlockEditor
        {
            get
            {
                return _annotationEditor;
            }
            set
            {
                _annotationEditor = value;
                OnPropertyChanged();
            }
        }

        public bool IsBlockEditing
        {
            get
            {
                return _isBlockEditing;
            }
            set
            {
                _isBlockEditing = value;
                OnPropertyChanged();
            }
        }

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

        /// <summary>
        /// The pivotal routine for handling selection in the EditController
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
            OnPropertyChanged(nameof(SelectedReactionType));
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
            var moleculeSelectionAdorner = ((SingleObjectSelectionAdorner)sender);
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
                ClearSelection();
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

        public AlignBottomsCommand AlignBottomsCommand { get; set; }
        public AlignMiddlesCommand AlignMiddlesCommand { get; set; }
        public AlignTopsCommand AlignTopsCommand { get; set; }

        public AlignLeftsCommand AlignLeftsCommand { get; set; }
        public AlignCentresCommand AlignCentresCommand { get; set; }
        public AlignRightsCommand AlignRightsCommand { get; set; }

        public EditReagentsCommand EditReagentsCommand { get; set; }
        public EditConditionsCommand EditConditionsCommand { get; set; }

        public AssignReactionRolesCommand AssignReactionRolesCommand { get; set; }
        public ClearReactionRolesCommand ClearReactionRolesCommand { get; set; }

        #endregion Commands

        #region Constructors

        /// <summary>
        /// "Normal" Constructor
        /// </summary>
        /// <param name="model"></param>
        /// <param name="currentEditor"></param>
        /// <param name="used1DProperties"></param>
        /// <param name="telemetry"></param>
        public EditController(Model model, EditorCanvas currentEditor, Canvas hostingCanvas, AnnotationEditor annotationEditor, List<string> used1DProperties, IChem4WordTelemetry telemetry) : base(model)
        {
            CurrentEditor = currentEditor;
            CurrentHostingCanvas = hostingCanvas;
            BlockEditor = annotationEditor;
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

            DefaultSettings();
        }

        /// <summary>
        /// Constructor for [X]Unit Tests
        /// Initialises the minimum objects necessary to run [X]Unit Tests
        /// </summary>
        /// <param name="model"></param>
        public EditController(Model model) : base(model)
        {
            LoadBondOptionsForUnitTest();

            _selectedItems = new ObservableCollection<object>();
            SelectedItems = new ReadOnlyObservableCollection<object>(_selectedItems);

            UndoManager = new UndoHandler(this, null);

            SetupCommands();

            DefaultSettings();

            IsBlockEditing = false;
        }

        private void DefaultSettings()
        {
            _selectedBondOptionId = 1;
            _selectedReactionType = ReactionType.Normal;
            _selectedElement = Globals.PeriodicTable.C;
            SelectionIsSubscript = false;
            SelectionIsSuperscript = false;
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

            AlignBottomsCommand = new AlignBottomsCommand(this);
            AlignMiddlesCommand = new AlignMiddlesCommand(this);
            AlignTopsCommand = new AlignTopsCommand(this);

            AlignLeftsCommand = new AlignLeftsCommand(this);
            AlignCentresCommand = new AlignCentresCommand(this);
            AlignRightsCommand = new AlignRightsCommand(this);

            EditConditionsCommand = new EditConditionsCommand(this);
            EditReagentsCommand = new EditReagentsCommand(this);
            AssignReactionRolesCommand = new AssignReactionRolesCommand(this);
            ClearReactionRolesCommand = new ClearReactionRolesCommand(this);
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

        #region Methods

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
                            };

                            Action undo = () =>
                            {
                                lastAtom.Element = lastElement;
                                lastAtom.IsotopeNumber = currentIsotope;
                                lastAtom.UpdateVisual();
                            };

                            UndoManager.RecordAction(undo, redo, $"Set Element to {value?.Symbol ?? "null"}");
                            redo();

                            ClearSelection();

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

        public void SetExplicitHPlacement(Atom selAtom, CompassPoints? newPlacement)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            WriteTelemetry(module, "Debug", $"{selAtom}; Placement {newPlacement}");
            var oldPlacement = selAtom.ImplicitHPlacement;

            Action undo = () =>
                          {
                              selAtom.ExplicitHPlacement = oldPlacement;
                              selAtom.UpdateVisual();
                              foreach (Bond selBond in selAtom.Bonds)
                              {
                                  selBond.UpdateVisual();
                              }
                          };
            Action redo = () =>
                          {
                              ClearSelection();
                              selAtom.ExplicitHPlacement = newPlacement;
                              selAtom.UpdateVisual();
                              foreach (Bond selBond in selAtom.Bonds)
                              {
                                  selBond.UpdateVisual();
                              }
                          };

            UndoManager.BeginUndoBlock();
            UndoManager.RecordAction(undo, redo);
            UndoManager.EndUndoBlock();
            redo();
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
                        ClearSelection();
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

        public void TransformAtoms(Transform operation, List<Atom> atoms)
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
                                          ClearSelection();
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
                                          ClearSelection();
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

        public void TransformObjects(Transform operation, List<BaseObject> objectsToTransform)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            var molecules = objectsToTransform.OfType<Molecule>().ToList();
            var reactions = objectsToTransform.OfType<Reaction>().ToList();
            var annotations = objectsToTransform.OfType<Annotation>().ToList();

            try
            {
                var countMolString = molecules == null ? "{null}" : $"{molecules.Count}";
                var countReactString = reactions == null ? "{null}" : $"{reactions.Count}";
                var countAnnotationString = annotations == null ? "{null}" : $"{annotations.Count}";
                var rootMolecules = from m in molecules
                                    where m.RootMolecule == m
                                    select m;
                Molecule[] moleculesToTransform = rootMolecules.ToArray();
                var transform = string.Join(";", DecodeTransform(operation));

                Action undo = () =>
                              {
                                  SuppressEditorRedraw(true);
                                  ClearSelection();

                                  var inverse = operation.Inverse;

                                  for (int i = 0; i < moleculesToTransform.Count(); i++)
                                  {
                                      WriteTelemetry(module, "Debug", $"Molecules: {countMolString} Transform: {transform}");
                                      Transform(moleculesToTransform[i], (Transform)inverse);
                                  }

                                  WriteTelemetry(module, "Debug", $"Reactions: {countReactString} Transform: {transform}");
                                  WriteTelemetry(module, "Debug", $"Annotations: {countAnnotationString} Transform: {transform}");

                                  foreach (Reaction reaction in reactions)
                                  {
                                      reaction.TailPoint = inverse.Transform(reaction.TailPoint);
                                      reaction.HeadPoint = inverse.Transform(reaction.HeadPoint);
                                  }

                                  foreach (Annotation ann in annotations)
                                  {
                                      ann.Position = inverse.Transform(ann.Position);
                                  }
                                  SuppressEditorRedraw(false);

                                  foreach (Molecule mol in moleculesToTransform)
                                  {
                                      mol.UpdateVisual();
                                  }
                                  foreach (Reaction react in reactions)
                                  {
                                      react.UpdateVisual();
                                  }

                                  foreach (Annotation ann in annotations)
                                  {
                                      ann.UpdateVisual();
                                  }
                                  AddObjectListToSelection(molecules.Cast<BaseObject>().ToList());
                                  AddObjectListToSelection(reactions.Cast<BaseObject>().ToList());
                                  AddObjectListToSelection(annotations.Cast<BaseObject>().ToList());
                              };

                Action redo = () =>
                              {
                                  SuppressEditorRedraw(true);
                                  ClearSelection();
                                  for (int i = 0; i < moleculesToTransform.Count(); i++)
                                  {
                                      WriteTelemetry(module, "Debug", $"Molecules: {countMolString} Transform: {transform}");
                                      Transform(moleculesToTransform[i], operation);
                                  }

                                  WriteTelemetry(module, "Debug", $"Reactions: {countReactString} Transform: {transform}");
                                  WriteTelemetry(module, "Debug", $"Annotations: {countAnnotationString} Transform: {transform}");

                                  foreach (Reaction reaction in reactions)
                                  {
                                      reaction.TailPoint = operation.Transform(reaction.TailPoint);
                                      reaction.HeadPoint = operation.Transform(reaction.HeadPoint);
                                  }

                                  foreach (Annotation ann in annotations)
                                  {
                                      ann.Position = operation.Transform(ann.Position);
                                  }
                                  SuppressEditorRedraw(false);

                                  foreach (Molecule mol in moleculesToTransform)
                                  {
                                      mol.UpdateVisual();
                                  }
                                  foreach (Reaction react in reactions)
                                  {
                                      react.UpdateVisual();
                                  }
                                  foreach (Annotation ann in annotations)
                                  {
                                      ann.UpdateVisual();
                                  }
                                  AddObjectListToSelection(molecules.Cast<BaseObject>().ToList());
                                  AddObjectListToSelection(reactions.Cast<BaseObject>().ToList());
                                  AddObjectListToSelection(annotations.Cast<BaseObject>().ToList());
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

        private void Transform(Molecule molecule, Transform lastOperation)
        {
            if (!molecule.IsGrouped)
            {
                foreach (Atom atom in molecule.Atoms.Values)
                {
                    atom.Position = lastOperation.Transform(atom.Position);
                    atom.UpdateVisual();
                }
            }
            else
            {
                foreach (Molecule mol in molecule.Molecules.Values)
                {
                    Transform(mol, lastOperation);
                    mol.UpdateVisual();
                }
            }
        }

        public void MultiTransformMolecules(List<Transform> operation, List<Molecule> molecules)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                var countString = molecules == null ? "{null}" : $"{molecules.Count}";

                var rootMolecules = from m in molecules
                                    where m.RootMolecule == m
                                    select m;
                Molecule[] moleculesToTransform = rootMolecules.ToArray();

                Action undo = () =>
                              {
                                  SuppressEditorRedraw(true);

                                  for (int i = 0; i < moleculesToTransform.Count(); i++)
                                  {
                                      var transform = string.Join(";", DecodeTransform(operation[i]));
                                      WriteTelemetry(module, "Debug", $"Molecules: {countString} Transform: {transform}");
                                      var inverse = operation[i].Inverse;
                                      Transform(moleculesToTransform[i], (Transform)inverse);
                                  }
                                  SuppressEditorRedraw(false);

                                  foreach (Molecule mol in moleculesToTransform)
                                  {
                                      mol.UpdateVisual();
                                  }
                                  AddObjectListToSelection(moleculesToTransform.Cast<BaseObject>().ToList());
                              };

                Action redo = () =>
                              {
                                  SuppressEditorRedraw(true);

                                  for (int i = 0; i < moleculesToTransform.Count(); i++)
                                  {
                                      var transform = string.Join(";", DecodeTransform(operation[i]));
                                      WriteTelemetry(module, "Debug", $"Molecules: {countString} Transform: {transform}");
                                      Transform(moleculesToTransform[i], operation[i]);
                                  }
                                  SuppressEditorRedraw(false);

                                  foreach (Molecule mol in moleculesToTransform)
                                  {
                                      mol.UpdateVisual();
                                  }
                                  AddObjectListToSelection(moleculesToTransform.Cast<BaseObject>().ToList());
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

        public void TransformMoleculeList(Transform operation, List<Molecule> molecules)
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
                                      ClearSelection();
                                      foreach (Molecule mol in moleculesToTransform)
                                      {
                                          Transform(mol, (Transform)inverse);
                                      }
                                      SuppressEditorRedraw(false);

                                      foreach (Molecule mol in moleculesToTransform)
                                      {
                                          mol.UpdateVisual();
                                      }
                                      AddObjectListToSelection(molecules.Cast<BaseObject>().ToList());
                                  };

                    Action redo = () =>
                                  {
                                      SuppressEditorRedraw(true);
                                      ClearSelection();
                                      foreach (Molecule mol in moleculesToTransform)
                                      {
                                          Transform(mol, operation);
                                      }
                                      SuppressEditorRedraw(false);

                                      foreach (Molecule mol in moleculesToTransform)
                                      {
                                          mol.UpdateVisual();
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
                                  RefreshConnectingWedges(bond);
                              };

                Action redo = () =>
                              {
                                  bond.Order = newOrder ?? CurrentBondOrder;
                                  bond.Stereo = newStereo ?? CurrentStereo;
                                  bond.StartAtom.UpdateVisual();
                                  bond.EndAtom.UpdateVisual();
                                  RefreshConnectingWedges(bond);
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

            //local function
            void RefreshConnectingWedges(Bond b)
            {
                foreach (var a in b.StartAtom.NeighboursExcept(b.EndAtom))
                {
                    var otherBond = b.StartAtom.BondBetween(a);
                    if (otherBond.Stereo == BondStereo.Wedge || otherBond.Stereo == BondStereo.Hatch)
                    {
                        otherBond.UpdateVisual();
                    }
                }

                foreach (var a in b.EndAtom.NeighboursExcept(b.StartAtom))
                {
                    var otherBond = b.EndAtom.BondBetween(a);
                    if (otherBond.Stereo == BondStereo.Wedge || otherBond.Stereo == BondStereo.Hatch)
                    {
                        otherBond.UpdateVisual();
                    }
                }
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
                    WriteTelemetry(module, "Warning", $"Molecule: {mol.Path}{Environment.NewLine}StartAtom: {a.Path} EndAtom: {b.Path}");
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

        /// <summary>
        /// Adds a new reaction to the model
        /// </summary>
        /// <param name="reaction">New reaction to add.</param>
        public void AddReaction(Reaction reaction)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Adding a reaction");

                Action redo = () =>
                {
                    //check to see if we have a scheme
                    var scheme = Model.DefaultReactionScheme;
                    scheme.AddReaction(reaction);
                    reaction.Parent = scheme;
                };
                Action undo = () =>
                {
                    var scheme = Model.DefaultReactionScheme;
                    ClearSelection();
                    scheme.RemoveReaction(reaction);
                    if (!scheme.Reactions.Any())
                    {
                        Model.RemoveReactionScheme(scheme);
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
                                        RefreshReactions(Model.DefaultReactionScheme.Reactions.Values.ToList());
                                        Loading = true;
                                        CurrentBondLength = newLength / ScaleFactorForXaml;
                                        Loading = false;
                                    };
                Action undoAction = () =>
                                    {
                                        Model.ScaleToAverageBondLength(currentLength, centre);
                                        SetTextParams(currentSelection);
                                        RefreshMolecules(Model.Molecules.Values.ToList());
                                        RefreshReactions(Model.DefaultReactionScheme.Reactions.Values.ToList());
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

        private void RefreshReactions(List<Reaction> reactions)
        {
            foreach (var reaction in reactions)
            {
                reaction.UpdateVisual();
            }
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
                        else if (selectedItem is Reaction reaction)
                        {
                            //TODO: Handle multiple reaction schemes in future. This is a kludge
                            tempModel.DefaultReactionScheme.AddReaction(reaction);
                        }
                        else if (selectedItem is Annotation annotation)
                        {
                            tempModel.AddAnnotation(annotation);
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
                    Clipboard.Clear();
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

            AlignBottomsCommand.RaiseCanExecChanged();
            AlignMiddlesCommand.RaiseCanExecChanged();
            AlignTopsCommand.RaiseCanExecChanged();

            AlignLeftsCommand.RaiseCanExecChanged();
            AlignCentresCommand.RaiseCanExecChanged();
            AlignRightsCommand.RaiseCanExecChanged();

            EditReagentsCommand.RaiseCanExecChanged();
            EditConditionsCommand.RaiseCanExecChanged();

            AssignReactionRolesCommand.RaiseCanExecChanged();
            ClearReactionRolesCommand.RaiseCanExecChanged();
        }

        public void RemoveAllAdorners()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
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
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        public void UpdateAtomBondAdorners()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (MultiAdorner != null)
                {
                    MultiAdorner.MouseLeftButtonDown -= SelAdorner_MouseLeftButtonDown;
                    var layer = AdornerLayer.GetAdornerLayer(CurrentEditor);
                    layer.Remove(MultiAdorner);
                    MultiAdorner = null;
                }

                var selAtomBonds = (from BaseObject sel in _selectedItems
                                    where sel is Atom || sel is Bond
                                    select sel).ToList();

                if (selAtomBonds.Any())
                {
                    MultiAdorner = new MultiAtomBondAdorner(CurrentEditor, selAtomBonds);
                    MultiAdorner.MouseLeftButtonDown += SelAdorner_MouseLeftButtonDown;
                }
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
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

                        msAdorner.DragIsCompleted -= MolAdorner_DragCompleted;
                        msAdorner.MouseLeftButtonDown -= SelAdorner_MouseLeftButtonDown;
                        (msAdorner as SingleObjectSelectionAdorner).DragIsCompleted -= MolAdorner_DragCompleted;
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
            var allReactions = (from r in newObjects.OfType<Reaction>().Union(SelectedItems.OfType<Reaction>())
                                select r).ToList();

            var allAnnotations = (from r in newObjects.OfType<Annotation>().Union(SelectedItems.OfType<Annotation>())
                                  select r).ToList();

            var allSingletons = singleAtomMols.Count == allMolecules.Count && singleAtomMols.Any();
            var allGroups = allMolecules.Count == groupMols.Count && groupMols.Any();

            if (allSingletons) //all single objects
            {
                RemoveAllAdorners();
                SingleObjectSelectionAdorner atomAdorner =
                    new SingleObjectSelectionAdorner(CurrentEditor, singleAtomMols);
                foreach (Molecule mol in singleAtomMols)
                {
                    SelectionAdorners[mol] = atomAdorner;
                }

                atomAdorner.MouseLeftButtonDown += SelAdorner_MouseLeftButtonDown;
                atomAdorner.DragIsCompleted += AtomAdorner_DragCompleted;
            }
            else if (allGroups)
            {
                if (!(allReactions.Any() || allAnnotations.Any())) //no reactions selected
                {
                    RemoveAllAdorners();
                    var groupAdorner = new GroupSelectionAdorner(CurrentEditor,
                                                           groupMols.Cast<BaseObject>().ToList());
                    foreach (Molecule mol in groupMols)
                    {
                        SelectionAdorners[mol] = groupAdorner;
                    }
                    groupAdorner.MouseLeftButtonDown += SelAdorner_MouseLeftButtonDown;
                    groupAdorner.DragIsCompleted += AtomAdorner_DragCompleted;
                }
                else //some reactions & groups
                {
                    RemoveAllAdorners();
                    AddMixed(allMolecules, allReactions.Cast<BaseObject>().Union(allAnnotations.Cast<BaseObject>()).ToList());
                }
            }
            else if (allMolecules.Any())
            {
                if (!(allReactions.Any() || allAnnotations.Any()))//no reactions
                {
                    RemoveAllAdorners();
                    var molAdorner = new MoleculeSelectionAdorner(CurrentEditor,
                                                                 allMolecules.Cast<BaseObject>().ToList());
                    foreach (Molecule mol in allMolecules)
                    {
                        SelectionAdorners[mol] = molAdorner;
                    }
                    molAdorner.MouseLeftButtonDown += SelAdorner_MouseLeftButtonDown;
                    molAdorner.DragIsCompleted += AtomAdorner_DragCompleted;
                }
                else //some reactions & molecules
                {
                    RemoveAllAdorners();
                    AddMixed(allMolecules, allReactions.Cast<BaseObject>().Union(allAnnotations.Cast<BaseObject>()).ToList());
                }
            }
            else  //just reactions or annotations
            {
                RemoveAllAdorners();
                if (allReactions.Count + allAnnotations.Count > 1)
                {
                    AddMixed(allMolecules, allReactions.Cast<BaseObject>().Union(allAnnotations.Cast<BaseObject>()).ToList());
                }
                else
                {
                    if (allReactions.Any())
                    {
                        var r = allReactions.First();
                        var reactionAdorner = new ReactionSelectionAdorner(CurrentEditor, CurrentEditor.ChemicalVisuals[r] as ReactionVisual);
                        SelectionAdorners[r] = reactionAdorner;
                        reactionAdorner.MouseLeftButtonDown += SelAdorner_MouseLeftButtonDown;
                    }
                    if (allAnnotations.Any())
                    {
                        var a = allAnnotations.First();
                        var annotationAdorner = new SingleObjectSelectionAdorner(CurrentEditor, new List<BaseObject> { a });
                        SelectionAdorners[a] = annotationAdorner;
                        annotationAdorner.MouseLeftButtonDown += SelAdorner_MouseLeftButtonDown;
                    }
                }
            }
            //local function
            void AddMixed(List<Molecule> mols, List<BaseObject> selObjects)
            {
                List<BaseObject> objects = mols.Cast<BaseObject>().ToList();
                objects = objects.Union(selObjects).ToList();
                MoleculeSelectionAdorner selector;
                if (mols.Where(m => m.IsGrouped).Any())
                {
                    selector = new GroupSelectionAdorner(CurrentEditor, objects);
                }
                else
                {
                    selector = new MoleculeSelectionAdorner(CurrentEditor, objects);
                }

                foreach (object o in selObjects)
                {
                    SelectionAdorners[o] = selector;
                }
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
                    ClearSelection();
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
                                SetBondAttributes(bondBetween, OrderDouble, BondStereo.None);
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
                    Dictionary<Guid, Molecule> parents = new Dictionary<Guid, Molecule>();
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
                                vector *= matrix3;
                            }

                            var aa = new Atom
                            {
                                Element = Globals.PeriodicTable.H,
                                Position = atom.Position +
                                                    vector * (Model.XamlBondLength * Common.ExplicitHydrogenBondPercentage)
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

                        ClearSelection();
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

                        ClearSelection();
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

                        ClearSelection();
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

                        ClearSelection();
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

        private void ReactionAdorner_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            //we've completed the drag operation
            //remove the existing molecule adorner
            var selectionAdorner = ((ReactionSelectionAdorner)sender);
            RemoveFromSelection(selectionAdorner.AdornedReaction);
            //and add in a new one
            AddToSelection(selectionAdorner.AdornedReaction);
        }

        public bool SingleMolSelected
        {
            get { return SelectedItems.Count == 1 && SelectedItems[0] is Molecule; }
        }

        public List<Molecule> ReactantsInSelection
        {
            get
            {
                List<Molecule> reactants = new List<Molecule>();
                var molsSelected = SelectedItems.OfType<Molecule>().ToList();
                var reactionSelected = SelectedItems.OfType<Reaction>().ToList()[0];

                Point start;
                Point end;

                if (reactionSelected.ReactionType != ReactionType.Retrosynthetic)
                {
                    start = reactionSelected.TailPoint;
                    end = reactionSelected.HeadPoint;
                }
                else
                {
                    end = reactionSelected.TailPoint;
                    start = reactionSelected.HeadPoint;
                }

                foreach (var mol in molsSelected)
                {
                    if ((mol.Centroid - start).Length < (mol.Centroid - end).Length)
                    {
                        reactants.Add(mol);
                    }
                }
                return reactants;
            }
        }

        public List<Molecule> ProductsInSelection
        {
            get
            {
                List<Molecule> products = new List<Molecule>();
                var molsSelected = SelectedItems.OfType<Molecule>().ToList();
                var reactionSelected = SelectedItems.OfType<Reaction>().ToList()[0];

                Point start;
                Point end;

                if (reactionSelected.ReactionType != ReactionType.Retrosynthetic)
                {
                    start = reactionSelected.TailPoint;
                    end = reactionSelected.HeadPoint;
                }
                else
                {
                    end = reactionSelected.TailPoint;
                    start = reactionSelected.HeadPoint;
                }

                foreach (var mol in molsSelected)
                {
                    if ((mol.Centroid - end).Length < (mol.Centroid - start).Length)
                    {
                        products.Add(mol);
                    }
                }
                return products;
            }
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
                    Transform(selMolecule, flipTransform);

                    InvertPlacements(selMolecule);
                    selMolecule.UpdateVisual();
                    if (flipStereo)
                    {
                        InvertStereo(selMolecule);
                    }
                };

                Action redo = () =>
                {
                    Transform(selMolecule, flipTransform);

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

        public void AddToSelection(BaseObject thingToAdd)
        {
            var parent = (thingToAdd as Atom)?.Parent ?? (thingToAdd as Bond)?.Parent;

            var thingsToAdd = new List<BaseObject> { thingToAdd };
            if (parent != null)
            {
                if (!SelectedItems.Contains(parent))
                {
                    AddObjectListToSelection(thingsToAdd);
                }
            }
            else
            {
                if (SelectedItems.Contains(thingsToAdd))
                {
                    RemoveFromSelection(thingsToAdd);
                }
                AddObjectListToSelection(thingsToAdd);
            }
        }

        public void AddObjectListToSelection(List<BaseObject> thingsToAdd)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
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
                foreach (var fullMolecule in fullParents)
                {
                    foreach (var atom in fullMolecule.Atoms.Values)
                    {
                        _selectedItems.Remove(atom);
                        thingsToAdd.Remove(atom);
                    }

                    foreach (Bond bond in fullMolecule.Bonds)
                    {
                        _selectedItems.Remove(bond);
                        thingsToAdd.Remove(bond);
                    }
                    //and add in the selected parent
                    if (!_selectedItems.Contains(fullMolecule.RootMolecule))
                    {
                        _selectedItems.Add(fullMolecule.RootMolecule);
                    }
                }

                DebugHelper.WriteLine($"Timing: {sw.ElapsedMilliseconds}ms");

                var newMols = thingsToAdd.OfType<Molecule>().ToList();
                foreach (var molecule in newMols)
                {
                    if (_selectedItems.Contains(molecule.RootMolecule))
                    {
                        _selectedItems.Remove(molecule.RootMolecule);
                    }
                    _selectedItems.Add(molecule.RootMolecule);
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
                //now do the reactions
                var newReactions = thingsToAdd.OfType<Reaction>().ToList();

                foreach (Reaction newReaction in newReactions)
                {
                    if (!_selectedItems.Contains(newReaction))
                    {
                        _selectedItems.Add(newReaction);
                        if (thingsToAdd.Contains(newReaction))
                        {
                            thingsToAdd.Remove(newReaction);
                        }
                    }
                }

                //finally the annotations
                var newAnnotations = thingsToAdd.OfType<Annotation>().ToList();

                foreach (Annotation annotation in newAnnotations)
                {
                    if (!_selectedItems.Contains(annotation))
                    {
                        _selectedItems.Add(annotation);
                        if (thingsToAdd.Contains(annotation))
                        {
                            thingsToAdd.Remove(annotation);
                        }
                    }
                }

                DebugHelper.WriteLine($"Timing: {sw.ElapsedMilliseconds}ms");
                DebugHelper.WriteLine($"Finished at {DateTime.Now}");
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        public void ClearSelection()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                _selectedItems.Clear();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        public void RemoveFromSelection(object thingToRemove)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                RemoveFromSelection(new List<object> { thingToRemove });
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        private void RemoveFromSelection(List<object> thingsToRemove)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
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
                        case Reaction r:
                            {
                                _selectedItems.Remove(r);
                            }
                            break;
                    }
                }

                if (CurrentEditor != null)
                {
                    UpdateAtomBondAdorners();
                }
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
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
                        ClearSelection();
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
                        ClearSelection();
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
                        ClearSelection();
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
                        ClearSelection();
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
            try
            {
                var countString = bonds == null ? "{null}" : $"{bonds.Count()}";
                WriteTelemetry(module, "Debug", $"Bonds {countString}");

                DeleteAtomsAndBonds(bondList: bonds);
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
            finally
            {
                CheckModelIntegrity(module);
            }
        }

        public void UpdateAtom(Atom atom, AtomPropertiesModel model)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                ElementBase elementBaseBefore = atom.Element;
                int? chargeBefore = atom.FormalCharge;
                CompassPoints? explicitFGPlacementBefore = atom.ExplicitFunctionalGroupPlacement;
                int? isotopeBefore = atom.IsotopeNumber;
                bool? explicitCBefore = atom.ExplicitC;
                CompassPoints? hydrogenPlacementBefore = atom.ExplicitHPlacement;

                ElementBase elementBaseAfter = model.Element;
                int? chargeAfter = null;
                int? isotopeAfter = null;
                bool? explicitCAfter = null;

                CompassPoints? hydrogenPlacementAfter = null;
                CompassPoints? explicitFGPlacementAfter = null;
                if (elementBaseAfter is FunctionalGroup)
                {
                    explicitFGPlacementAfter = model.ExplicitFunctionalGroupPlacement;
                }
                else if (elementBaseAfter is Element)
                {
                    chargeAfter = model.Charge;
                    explicitCAfter = model.ExplicitC;
                    hydrogenPlacementAfter = model.ExplicitHydrogenPlacement;
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
                                  atom.ExplicitHPlacement = hydrogenPlacementAfter;
                                  atom.ExplicitFunctionalGroupPlacement = explicitFGPlacementAfter;
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
                                  atom.ExplicitHPlacement = hydrogenPlacementBefore;
                                  atom.ExplicitFunctionalGroupPlacement = explicitFGPlacementBefore;
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
                    bond.Order = Bond.OrderValueToOrder(bondOrderAfter);
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
                            Transform(mol, (Transform)transform);
                            mol.UpdateVisual();
                        }
                        ClearSelection();
                    }
                };

                Action undo = () =>
                {
                    bond.Order = Bond.OrderValueToOrder(bondOrderBefore);
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
                            Transform(mol, (Transform)inverse);
                            mol.UpdateVisual();
                        }
                        ClearSelection();
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
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                CMLConverter cc = new CMLConverter();
                Model buffer = cc.Import(pastedCml);
                PasteModel(buffer, true);
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        public void PasteModel(Model buffer, bool fromCML = false)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");
                // Match to current model's settings
                buffer.Relabel(true);
                // above should be buffer.StripLabels(true)
                buffer.ScaleToAverageBondLength(Model.XamlBondLength);

                if (!fromCML && buffer.Molecules.Count > 1)
                {
                    Packer packer = new Packer();
                    packer.Model = buffer;
                    packer.Pack(Model.XamlBondLength * 2);
                }

                var molList = buffer.Molecules.Values.ToList();
                var reactionList = buffer.DefaultReactionScheme.Reactions.Values.ToList();
                var annotationList = buffer.Annotations.Values.ToList();

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
                    foreach (var reaction in reactionList)
                    {
                        RemoveFromSelection(reaction);
                        Model.DefaultReactionScheme.RemoveReaction(reaction);
                        reaction.Parent = null;
                    }
                    foreach (var annotation in annotationList)
                    {
                        RemoveFromSelection(annotation);
                        Model.RemoveAnnotation(annotation);
                        annotation.Parent = null;
                    }
                };
                Action redo = () =>
                {
                    ClearSelection();
                    foreach (var mol in molList)
                    {
                        mol.Parent = Model;
                        Model.AddMolecule(mol);
                        AddToSelection(mol);
                    }
                    foreach (var reaction in reactionList)
                    {
                        reaction.Parent = Model.DefaultReactionScheme;
                        Model.DefaultReactionScheme.AddReaction(reaction);
                        AddToSelection(reaction);
                    }
                    foreach (var annotation in annotationList)
                    {
                        annotation.Parent = Model;
                        Model.AddAnnotation(annotation);
                        AddToSelection(annotation);
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
                var reactions = SelectedItems.OfType<Reaction>().ToList();
                var annotations = SelectedItems.OfType<Annotation>().ToList();
                UndoManager.BeginUndoBlock();

                if (mols.Any())
                {
                    DeleteMolecules(mols);
                }

                if (atoms.Any() || bonds.Any())
                {
                    DeleteAtomsAndBonds(atoms, bonds);
                }
                if (reactions.Any())
                {
                    DeleteReactions(reactions);
                }
                if (annotations.Any())
                {
                    DeleteAnnotations(annotations);
                }
                ClearSelection();
                UndoManager.EndUndoBlock();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);
        }

        public void DeleteAnnotations(IEnumerable<Annotation> annotations)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");
                {
                    UndoManager.BeginUndoBlock();
                    foreach (Annotation a in annotations)
                    {
                        var parent = a.Parent;
                        Action redo = () =>
                              {
                                  ClearSelection();
                                  Model.RemoveAnnotation(a);
                                  a.Parent = null;
                              };

                        Action undo = () =>
                                      {
                                          Model.AddAnnotation(a);
                                          a.Parent = parent;
                                          AddToSelection(a);
                                      };
                        redo();
                        UndoManager.RecordAction(undo, redo);
                    }
                    UndoManager.EndUndoBlock();
                }
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        internal void DeleteReactions(IEnumerable<Reaction> reactions)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");
                {
                    UndoManager.BeginUndoBlock();
                    foreach (Reaction r in reactions)
                    {
                        Action redo = () =>
                              {
                                  ClearSelection();
                                  Model.DefaultReactionScheme.RemoveReaction(r);
                                  r.Parent = null;
                              };

                        Action undo = () =>
                                      {
                                          Model.DefaultReactionScheme.AddReaction(r);
                                          r.Parent = Model.DefaultReactionScheme;
                                          AddToSelection(r);
                                      };
                        redo();
                        UndoManager.RecordAction(undo, redo);
                    }
                    UndoManager.EndUndoBlock();
                }
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
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
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                //grab just the molecules (to be grouped)
                var children = (from Molecule mol in selection.OfType<Molecule>()
                                select mol).ToList();
                Group(children);
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
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
                    ClearSelection();
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
                    ClearSelection();

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
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                ClearSelection();
                List<BaseObject> selection = new List<BaseObject>();
                foreach (var mol in Model.Molecules.Values)
                {
                    selection.Add(mol);
                }

                foreach (var r in Model.DefaultReactionScheme.Reactions.Values)
                {
                    selection.Add(r);
                }

                foreach (var a in Model.Annotations.Values)
                {
                    selection.Add(a);
                }
                AddObjectListToSelection(selection);
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
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

        public void RotateHydrogen(Atom parentAtom)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                var oldPlacement = parentAtom.ImplicitHPlacement;

                switch (oldPlacement)
                {
                    case CompassPoints.North:
                        SetExplicitHPlacement(parentAtom, CompassPoints.East);
                        break;

                    case CompassPoints.East:
                        SetExplicitHPlacement(parentAtom, CompassPoints.South);
                        break;

                    case CompassPoints.South:
                        SetExplicitHPlacement(parentAtom, CompassPoints.West);
                        break;

                    case CompassPoints.West:
                        SetExplicitHPlacement(parentAtom, CompassPoints.North);
                        break;
                }
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        public void AlignMiddles(List<BaseObject> objects)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", $"Aligning middles of {objects.Count} objects");

                List<Transform> shifts = new List<Transform>();
                List<Transform> annShifts = new List<Transform>();

                var molsToAlign = objects.OfType<Molecule>().ToList();
                var annotationsToAlign = objects.OfType<Annotation>().ToList();

                double molsMiddle = 0;
                double annotationsMiddle = 0;

                if (molsToAlign.Any())
                {
                    molsMiddle = molsToAlign.Average(m => m.Centre.Y);
                }
                if (annotationsToAlign.Any())
                {
                    annotationsMiddle = annotationsToAlign.Average(a => (CurrentEditor.ChemicalVisuals[a].ContentBounds.Top + CurrentEditor.ChemicalVisuals[a].ContentBounds.Bottom) / 2);
                }

                double middle = (annotationsMiddle * annotationsToAlign.Count + molsMiddle * molsToAlign.Count) / (molsToAlign.Count + annotationsToAlign.Count);

                for (int i = 0; i < molsToAlign.Count; i++)
                {
                    var shift = new TranslateTransform();
                    shift.Y = middle - molsToAlign[i].Centre.Y;
                    shifts.Add(shift);
                }

                for (int i = 0; i < annotationsToAlign.Count; i++)
                {
                    var shift = new TranslateTransform();
                    var a = annotationsToAlign[i];
                    shift.Y = middle - (CurrentEditor.ChemicalVisuals[a].ContentBounds.Top + CurrentEditor.ChemicalVisuals[a].ContentBounds.Bottom) / 2;
                    annShifts.Add(shift);
                }

                UndoManager.BeginUndoBlock();
                AlignMolecules(molsToAlign, shifts);
                AlignAnnotations(annotationsToAlign, annShifts);
                List<Reaction> reacts = objects.OfType<Reaction>().ToList();
                AlignReactionMiddles(reacts, middle);
                UndoManager.EndUndoBlock();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        public void AlignReactionMiddles(List<Reaction> reactions, double middle)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Dictionary<Reaction, (Point start, Point end)> originalPos = new Dictionary<Reaction, (Point, Point)>();
                foreach (Reaction r in reactions)
                {
                    originalPos[r] = (r.TailPoint, r.HeadPoint);
                }

                Action redo = () =>
                {
                    foreach (Reaction r in reactions)
                    {
                        Point newTailPoint = new Point(r.TailPoint.X, middle);
                        Point newHeadPoint = new Point(r.HeadPoint.X, middle);

                        if (newHeadPoint != newTailPoint)
                        {
                            r.TailPoint = newTailPoint;
                            r.HeadPoint = newHeadPoint;
                        }
                        else
                        {
                            WriteTelemetry(module, "Warning", $"Can't align middles of reaction {r.Id}");
                        }
                    }
                    AddObjectListToSelection(reactions.Cast<BaseObject>().ToList());
                };

                Action undo = () =>
                {
                    foreach (Reaction r in reactions)
                    {
                        r.TailPoint = new Point(r.TailPoint.X, originalPos[r].start.Y);
                        r.HeadPoint = new Point(r.HeadPoint.X, originalPos[r].end.Y);
                    }
                    AddObjectListToSelection(reactions.Cast<BaseObject>().ToList());
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

        public void AlignReactionCentres(List<Reaction> reacts, double centre)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Dictionary<Reaction, (Point start, Point end)> originalPos = new Dictionary<Reaction, (Point, Point)>();
                foreach (Reaction r in reacts)
                {
                    originalPos[r] = (r.TailPoint, r.HeadPoint);
                }
                Action redo = () =>
                {
                    foreach (Reaction r in reacts)
                    {
                        Point newTailPoint = new Point(centre, r.TailPoint.Y);
                        Point newHeadpoint = new Point(centre, r.HeadPoint.Y);
                        if (newHeadpoint != newTailPoint)
                        {
                            r.TailPoint = newTailPoint;
                            r.HeadPoint = newHeadpoint;
                        }
                        else
                        {
                            WriteTelemetry(module, "Warning", $"Can't align centres of reaction {r.Id}");
                        }
                    }
                    AddObjectListToSelection(reacts.Cast<BaseObject>().ToList());
                };
                Action undo = () =>
                {
                    foreach (Reaction r in reacts)
                    {
                        r.TailPoint = new Point(originalPos[r].start.X, r.TailPoint.Y);
                        r.HeadPoint = new Point(originalPos[r].end.X, r.HeadPoint.Y);
                    }
                    AddObjectListToSelection(reacts.Cast<BaseObject>().ToList());
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

        public void AlignTops(List<BaseObject> objects)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", $"Aligning tops of {objects.Count} objects");

                List<Transform> shifts = new List<Transform>();
                List<Transform> annShifts = new List<Transform>();

                var molsToAlign = objects.OfType<Molecule>().ToList();
                var annotationsToAlign = objects.OfType<Annotation>().ToList();

                double stupidMin = 1.0E6;

                double top = Math.Min(molsToAlign.Select(m => m.Top)
                                                 .DefaultIfEmpty(stupidMin).Min(),
                                      annotationsToAlign.Select(a => CurrentEditor.ChemicalVisuals[a].ContentBounds.Top)
                                                        .DefaultIfEmpty(stupidMin).Min());

                for (int i = 0; i < molsToAlign.Count; i++)
                {
                    var shift = new TranslateTransform();
                    shift.Y = top - molsToAlign[i].Top;
                    shifts.Add(shift);
                }

                for (int i = 0; i < annotationsToAlign.Count; i++)
                {
                    var shift = new TranslateTransform();
                    shift.Y = top - CurrentEditor.ChemicalVisuals[annotationsToAlign[i]].ContentBounds.Top;
                    annShifts.Add(shift);
                }

                UndoManager.BeginUndoBlock();
                AlignMolecules(molsToAlign, shifts);
                AlignAnnotations(annotationsToAlign, annShifts);
                UndoManager.EndUndoBlock();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        public void AlignAnnotations(List<Annotation> annotationsToAlign, List<Transform> shifts)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Action redo = () =>
                {
                    for (int i = 0; i < annotationsToAlign.Count; i++)
                    {
                        annotationsToAlign[i].Position = shifts[i].Transform(annotationsToAlign[i].Position);
                    }
                };
                Action undo = () =>
                {
                    for (int i = 0; i < annotationsToAlign.Count; i++)
                    {
                        annotationsToAlign[i].Position = shifts[i].Inverse.Transform(annotationsToAlign[i].Position);
                    }
                };
                UndoManager.BeginUndoBlock();
                UndoManager.RecordAction(undo, redo);
                UndoManager.EndUndoBlock();
                redo();
                AddObjectListToSelection(annotationsToAlign.Cast<BaseObject>().ToList());
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        public void AlignBottoms(List<BaseObject> objects)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", $"Aligning bottoms of {objects.Count} objects");

                List<Transform> shifts = new List<Transform>();
                List<Transform> annShifts = new List<Transform>();

                var molsToAlign = objects.OfType<Molecule>().ToList();
                var annotationsToAlign = objects.OfType<Annotation>().ToList();

                double stupidMax = -100;

                double bottom = Math.Max(molsToAlign.Select(m => m.Bottom)
                                                    .DefaultIfEmpty(stupidMax).Max(),
                                         annotationsToAlign.Select(a => CurrentEditor.ChemicalVisuals[a].ContentBounds.Bottom)
                                                           .DefaultIfEmpty(stupidMax).Max());

                for (int i = 0; i < molsToAlign.Count; i++)
                {
                    var shift = new TranslateTransform();
                    shift.Y = bottom - molsToAlign[i].Bottom;
                    shifts.Add(shift);
                }

                for (int i = 0; i < annotationsToAlign.Count; i++)
                {
                    var shift = new TranslateTransform();
                    shift.Y = bottom - CurrentEditor.ChemicalVisuals[annotationsToAlign[i]].ContentBounds.Bottom;
                    annShifts.Add(shift);
                }

                UndoManager.BeginUndoBlock();
                AlignMolecules(molsToAlign, shifts);
                AlignAnnotations(annotationsToAlign, annShifts);
                UndoManager.EndUndoBlock();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        public void AlignCentres(List<BaseObject> objects)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", $"Aligning centres of {objects.Count} objects");

                List<Transform> shifts = new List<Transform>();
                List<Transform> annShifts = new List<Transform>();

                var molsToAlign = objects.OfType<Molecule>().ToList();
                var annotationsToAlign = objects.OfType<Annotation>().ToList();

                double molsCentre = 0;
                double annotationsCentre = 0;

                if (molsToAlign.Any())
                {
                    molsCentre = molsToAlign.Average(m => m.Centre.X);
                }
                if (annotationsToAlign.Any())
                {
                    annotationsCentre = annotationsToAlign
                        .Average(a => (CurrentEditor.ChemicalVisuals[a].ContentBounds.Left + CurrentEditor.ChemicalVisuals[a].ContentBounds.Right) / 2);
                }

                double centre = (annotationsCentre * annotationsToAlign.Count + molsCentre * molsToAlign.Count) / (molsToAlign.Count + annotationsToAlign.Count);

                for (int i = 0; i < molsToAlign.Count; i++)
                {
                    var shift = new TranslateTransform();
                    shift.X = centre - molsToAlign[i].Centre.X;
                    shifts.Add(shift);
                }

                for (int i = 0; i < annotationsToAlign.Count; i++)
                {
                    var shift = new TranslateTransform();
                    var a = annotationsToAlign[i];
                    shift.X = centre - (CurrentEditor.ChemicalVisuals[a].ContentBounds.Left + CurrentEditor.ChemicalVisuals[a].ContentBounds.Right) / 2;
                    annShifts.Add(shift);
                }

                UndoManager.BeginUndoBlock();
                AlignMolecules(molsToAlign, shifts);
                AlignAnnotations(annotationsToAlign, annShifts);
                List<Reaction> reacts = objects.OfType<Reaction>().ToList();
                AlignReactionCentres(reacts, centre);
                UndoManager.EndUndoBlock();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        public void AlignLefts(List<BaseObject> objects)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", $"Aligning lefts of {objects.Count} objects");

                List<Transform> shifts = new List<Transform>();
                List<Transform> annShifts = new List<Transform>();

                var molsToAlign = objects.OfType<Molecule>().ToList();
                var annotationsToAlign = objects.OfType<Annotation>().ToList();

                double stupidMin = 1.0E6;

                double left = Math.Min(molsToAlign.Select(m => m.Left)
                                                  .DefaultIfEmpty(stupidMin).Min(),
                                       annotationsToAlign.Select(a => CurrentEditor.ChemicalVisuals[a].ContentBounds.Left)
                                                         .DefaultIfEmpty(stupidMin).Min());

                for (int i = 0; i < molsToAlign.Count; i++)
                {
                    var shift = new TranslateTransform();
                    shift.X = left - molsToAlign[i].Left;
                    shifts.Add(shift);
                }

                for (int i = 0; i < annotationsToAlign.Count; i++)
                {
                    var shift = new TranslateTransform();
                    shift.X = left - CurrentEditor.ChemicalVisuals[annotationsToAlign[i]].ContentBounds.Left;
                    annShifts.Add(shift);
                }

                UndoManager.BeginUndoBlock();
                AlignMolecules(molsToAlign, shifts);
                AlignAnnotations(annotationsToAlign, annShifts);
                UndoManager.EndUndoBlock();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        public void AlignRights(List<BaseObject> objects)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", $"Aligning rights of {objects.Count} objects");

                List<Transform> shifts = new List<Transform>();
                List<Transform> annShifts = new List<Transform>();

                var molsToAlign = objects.OfType<Molecule>().ToList();
                var annotationsToAlign = objects.OfType<Annotation>().ToList();

                double stupidMax = -100;

                double right = Math.Max(molsToAlign.Select(m => m.Right)
                                                   .DefaultIfEmpty(stupidMax).Max(),
                                        annotationsToAlign.Select(a => CurrentEditor.ChemicalVisuals[a].ContentBounds.Right)
                                                          .DefaultIfEmpty(stupidMax).Max());

                for (int i = 0; i < molsToAlign.Count; i++)
                {
                    var shift = new TranslateTransform();
                    shift.X = right - molsToAlign[i].Right;
                    shifts.Add(shift);
                }

                for (int i = 0; i < annotationsToAlign.Count; i++)
                {
                    var shift = new TranslateTransform();
                    shift.X = right - CurrentEditor.ChemicalVisuals[annotationsToAlign[i]].ContentBounds.Right;
                    annShifts.Add(shift);
                }

                UndoManager.BeginUndoBlock();
                AlignMolecules(molsToAlign, shifts);
                AlignAnnotations(annotationsToAlign, annShifts);
                UndoManager.EndUndoBlock();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        // aligns a set of molecules given a set of adjusting transforms
        private void AlignMolecules(List<Molecule> molsToAlign, List<Transform> adjustments)
        {
            //first check to see whether or not we have an equal number of adjustments and molecules
            Debug.Assert(molsToAlign.Count == adjustments.Count);

            UndoManager.BeginUndoBlock();
            MultiTransformMolecules(adjustments, molsToAlign);
            AddObjectListToSelection(molsToAlign.Cast<BaseObject>().ToList());
            UndoManager.EndUndoBlock();
        }

        //edits the current reagent block
        public void EditReagents()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                CreateBlockEditor(SelectedItems[0] as Reaction, editingReagents: true);
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        private void CreateBlockEditor(Reaction reaction, bool editingReagents)
        {
            string blocktext;
            Rect block;
            _selReactionVisual = CurrentEditor.ChemicalVisuals[reaction] as ReactionVisual; //should NOT be null!

            //remove the reaction from the selection, otherwise the adorner gets in the way

            RemoveFromSelection(reaction);

            //decide whether we're doing reagents or conditions
            if (editingReagents)
            {
                block = _selReactionVisual.ReagentsBlockRect;
                blocktext = reaction.ReagentText;
            }
            else
            {
                block = _selReactionVisual.ConditionsBlockRect;
                blocktext = reaction.ConditionsText;
            }

            //make the block a bit bigger
            block.Inflate(BlockTextPadding, BlockTextPadding);

            //locate the editor properly
            BlockEditor.Controller = this;
            BlockEditor.MinWidth = block.Width;
            BlockEditor.MinHeight = block.Height;
            BlockEditor.Visibility = Visibility.Visible;
            BlockEditor.EditingReagents = editingReagents;
            Canvas.SetLeft(BlockEditor, block.Left);
            Canvas.SetTop(BlockEditor, block.Top);

            if (!string.IsNullOrEmpty(blocktext))
            {
                BlockEditor.LoadDocument(blocktext);
            }

            ActiveBlockEditor = BlockEditor;
            BlockEditor.Completed += BlockEditor_EditorClosed;
            BlockEditor.SelectionChanged += BlockEditor_SelectionChanged;
            IsBlockEditing = true;
            SendStatus(EditingTextStatus);
            BlockEditor.Focus();
        }

        private void BlockEditor_SelectionChanged(object sender, RoutedEventArgs e)
        {
            SelectionIsSubscript = BlockEditor.SelectionIsSubscript;
            SelectionIsSuperscript = BlockEditor.SelectionIsSuperscript;
        }

        // Event handler on termination of the textblock editor
        private void BlockEditor_EditorClosed(object sender, AnnotationEditorEventArgs e)
        {
            var activeEditor = (AnnotationEditor)sender;
            if (e.Reason != AnnotationEditorExitArgsType.Aborted)
            {
                UpdateTextBlock(_selReactionVisual, activeEditor, activeEditor.EditingReagents);
            }
            activeEditor.Visibility = Visibility.Collapsed;
            activeEditor.Clear();
            activeEditor.Completed -= BlockEditor_EditorClosed;
            activeEditor.SelectionChanged -= BlockEditor_SelectionChanged;
            activeEditor.Controller = null;
            IsBlockEditing = false;
            ActiveBlockEditor = null;
        }

        private void UpdateTextBlock(ReactionVisual selReactionVisual, AnnotationEditor editor, bool editingReagents)
        {
            var reaction = selReactionVisual.ParentReaction;
            string oldText;
            if (editingReagents)
            {
                oldText = reaction.ReagentText?.Trim() ?? "";
            }
            else
            {
                oldText = reaction.ConditionsText?.Trim() ?? "";
            }
            //only commit the text if it's been chnaged.

            string result = editor.GetDocument();
            if (oldText != result)
            {
                Action redo = () =>
                {
                    if (editingReagents)
                    {
                        reaction.ReagentText = result;
                    }
                    else
                    {
                        reaction.ConditionsText = result;
                    }
                };
                Action undo = () =>
                {
                    if (editingReagents)
                    {
                        reaction.ReagentText = oldText;
                    }
                    else
                    {
                        reaction.ConditionsText = oldText;
                    }
                };
                UndoManager.BeginUndoBlock();
                UndoManager.RecordAction(undo, redo, $"Update {(editingReagents ? "Reagents" : "Conditions")}");
                UndoManager.EndUndoBlock();
                redo();
            }
        }

        //edits the current conditions block
        public void EditConditions()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                CreateBlockEditor(SelectedItems[0] as Reaction, editingReagents: false);
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        /// <summary>
        /// Adds a floating symbol (typically a + sign)
        /// </summary>
        /// <param name="pos">Top-left corner of symbol</param>
        /// <param name="symbolText">Text to display</param>
        public void AddFloatingSymbol(Point pos, string symbolText)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", $"Adding floating symbol '{symbolText}'");
                AddAnnotation(pos, symbolText, false);
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        private void AddAnnotation(Point pos, string text, bool isEditable = true)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                WriteTelemetry(module, "Debug", "Adding Annotation");

                var newSymbol = new Annotation();
                newSymbol.Position = pos;
                newSymbol.IsEditable = isEditable;
                var docElement = new XElement(CMLNamespaces.xaml + "FlowDocument",
                                              new XElement(CMLNamespaces.xaml + "Paragraph",
                                                           new XElement(CMLNamespaces.xaml + "Run",
                                                                        text)));
                newSymbol.Xaml = docElement.CreateNavigator().OuterXml;
                Action redo = () =>
                              {
                                  Model.AddAnnotation(newSymbol);
                              };

                Action undo = () =>
                              {
                                  Model.RemoveAnnotation(newSymbol);
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

        public bool FullReactionSelected()
        {
            if ((SelectionType & SelectionTypeCode.Molecule) == SelectionTypeCode.Molecule
                && (SelectionType & SelectionTypeCode.Reaction) == SelectionTypeCode.Reaction)
            {
                return SelectedItems.OfType<Reaction>().ToList().Count == 1 && (ReactantsInSelection.Any() & ProductsInSelection.Any());
            }
            return false;
        }

        /// <summary>
        /// Assigns roles to molecules involved in a reaction
        /// </summary>
        ///
        public void AssignReactionRoles()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Assign reaction roles");

                var selectedReaction = SelectedItems.OfType<Reaction>().ToList()[0];

                var currentReactants = ReactantsInSelection.ToArray();
                var currentProducts = ProductsInSelection.ToArray();

                Action redo = () =>
                {
                    selectedReaction.ClearReactants();
                    selectedReaction.ClearProducts();
                    foreach (var reactant in currentReactants)
                    {
                        selectedReaction.AddReactant(reactant);
                    }

                    foreach (var product in currentProducts)
                    {
                        selectedReaction.AddProduct(product);
                    }
                    ClearSelection();
                    AddToSelection(selectedReaction);
                };

                Action undo = () =>
                {
                    selectedReaction.ClearReactants();
                    selectedReaction.ClearProducts();
                    foreach (var reactant in currentReactants)
                    {
                        selectedReaction.RemoveReactant(reactant);
                    }

                    foreach (var product in currentProducts)
                    {
                        selectedReaction.RemoveProduct(product);
                    }
                    ClearSelection();
                    AddToSelection(selectedReaction);
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

        public bool SingleReactionSelected()
        {
            return SelectedReactions().Count == 1 && (SelectedReactions()[0].Reactants.Any() || SelectedReactions()[0].Products.Any());
        }

        public void ClearReactionRoles()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Clearing reaction roles");

                var selectedReaction = SelectedItems.OfType<Reaction>().ToList()[0];

                var currentReactants = selectedReaction.Reactants.Values.ToArray();
                var currentProducts = selectedReaction.Products.Values.ToArray();

                Action redo = () =>
                {
                    selectedReaction.ClearReactants();
                    selectedReaction.ClearProducts();

                    ClearSelection();
                    AddToSelection(selectedReaction);
                };

                Action undo = () =>
                {
                    foreach (var reactant in currentReactants)
                    {
                        selectedReaction.AddReactant(reactant);
                    }

                    foreach (var product in currentProducts)
                    {
                        selectedReaction.AddProduct(product);
                    }
                    ClearSelection();
                    AddToSelection(selectedReaction);
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

        #endregion Methods
    }
}