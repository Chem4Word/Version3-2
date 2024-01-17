// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Adorners.Feedback;
using Chem4Word.ACME.Drawing.Visuals;
using Chem4Word.ACME.Utils;
using Chem4Word.Model2;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Chem4Word.ACME.Controls
{
    public class ChemistryCanvas : Canvas
    {
        private const double Spacing = 4.5;

        #region Fields

        private Adorner _highlightAdorner;
        private bool _positionOffsetApplied;

        #endregion Fields

        #region Constructors

        public ChemistryCanvas()
        {
            ChemicalVisuals = new Dictionary<object, DrawingVisual>();
            MouseMove += Canvas_MouseMove;
        }

        #endregion Constructors

        #region Properties

        private ChemicalVisual _activeVisual = null;

        public ChemicalVisual ActiveVisual
        {
            get { return _activeVisual; }
            set
            {
                if (_activeVisual != value)
                {
                    RemoveActiveAdorner();
                    if (HighlightActive)
                    {
                        SetActiveAdorner(value);
                    }

                    _activeVisual = value;
                }
            }
        }

        public AtomVisual ActiveAtomVisual => ActiveVisual as AtomVisual;

        public ReactionVisual ActiveReactionVisual => ActiveVisual as ReactionVisual;
        public BondVisual ActiveBondVisual => ActiveVisual as BondVisual;

        public BaseObject ActiveChemistry
        {
            get
            {
                switch (ActiveVisual)
                {
                    case GroupVisual gv:
                        return gv.ParentMolecule;

                    case BondVisual bv:
                        return bv.ParentBond;

                    case AtomVisual av:
                        return av.ParentAtom;

                    case ReactionVisual rv:
                        return rv.ParentReaction;

                    default:
                        return null;
                }
            }
            set
            {
                if (value == null)
                {
                    ActiveVisual = null;
                }
                else
                {
                    ActiveVisual = (ChemicalVisuals[(value)] as ChemicalVisual);
                }
            }
        }

        public bool ShowMoleculeGrouping
        {
            get { return (bool)GetValue(ShowMoleculeGroupingProperty); }
            set { SetValue(ShowMoleculeGroupingProperty, value); }
        }

        public static readonly DependencyProperty ShowMoleculeGroupingProperty =
            DependencyProperty.Register("ShowMoleculeGrouping", typeof(bool), typeof(ChemistryCanvas),
                                        new FrameworkPropertyMetadata(true,
                                            FrameworkPropertyMetadataOptions.AffectsParentArrange
                                            | FrameworkPropertyMetadataOptions.AffectsMeasure,
                                            RepaintCanvas));

        public bool ShowAtomsInColour
        {
            get { return (bool)GetValue(ShowAtomsInColourProperty); }
            set { SetValue(ShowAtomsInColourProperty, value); }
        }

        public static readonly DependencyProperty ShowAtomsInColourProperty =
            DependencyProperty.Register("ShowAtomsInColour", typeof(bool), typeof(ChemistryCanvas),
                                        new FrameworkPropertyMetadata(true,
                                              FrameworkPropertyMetadataOptions.AffectsRender,
                                              RepaintCanvas));

        public bool ShowAllCarbonAtoms
        {
            get { return (bool)GetValue(ShowAllCarbonAtomsProperty); }
            set { SetValue(ShowAllCarbonAtomsProperty, value); }
        }

        public static readonly DependencyProperty ShowAllCarbonAtomsProperty =
            DependencyProperty.Register("ShowAllCarbonAtoms", typeof(bool), typeof(ChemistryCanvas),
                                        new FrameworkPropertyMetadata(false,
                                              FrameworkPropertyMetadataOptions.AffectsParentArrange
                                              | FrameworkPropertyMetadataOptions.AffectsMeasure,
                                              RepaintCanvas));

        public bool ShowImplicitHydrogens
        {
            get { return (bool)GetValue(ShowImplicitHydrogensProperty); }
            set { SetValue(ShowImplicitHydrogensProperty, value); }
        }

        public static readonly DependencyProperty ShowImplicitHydrogensProperty =
            DependencyProperty.Register("ShowImplicitHydrogens", typeof(bool), typeof(ChemistryCanvas),
                                        new FrameworkPropertyMetadata(true,
                                          FrameworkPropertyMetadataOptions.AffectsParentArrange
                                          | FrameworkPropertyMetadataOptions.AffectsMeasure,
                                          RepaintCanvas));

        private static void RepaintCanvas(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            Debug.WriteLine($"RepaintCanvas({dependencyObject.GetHashCode()}) - Trigger {eventArgs.Property.Name} -> {dependencyObject.GetValue(eventArgs.Property)}");
            if (dependencyObject is ChemistryCanvas canvas)
            {
                canvas.Clear();
                canvas.DrawChemistry(canvas.Controller);
            }
        }

        public bool HighlightActive
        {
            get { return (bool)GetValue(HighlightActiveProperty); }
            set { SetValue(HighlightActiveProperty, value); }
        }

        public static readonly DependencyProperty HighlightActiveProperty =
            DependencyProperty.Register("HighlightActive", typeof(bool), typeof(ChemistryCanvas),
                                        new PropertyMetadata(true));

        public bool DisplayOverbondedAtoms
        {
            get { return (bool)GetValue(DisplayOverbondedAtomsProperty); }
            set { SetValue(DisplayOverbondedAtomsProperty, value); }
        }

        public static readonly DependencyProperty DisplayOverbondedAtomsProperty =
            DependencyProperty.Register("DisplayOverbondedAtoms", typeof(bool), typeof(ChemistryCanvas),
                                        new PropertyMetadata(default(bool)));

        #endregion Properties

        /// <summary>
        /// called during WPF layout phase
        /// </summary>
        /// <param name="constraint"></param>
        /// <returns></returns>
        protected override Size MeasureOverride(Size constraint)
        {
            Debug.WriteLine($"MeasureOverride() #{GetHashCode()} {_controller?.Model.ConciseFormula}");
            var size = GetBoundingBox();

            if (_controller != null
                && _controller.Model != null
                && !_positionOffsetApplied)
            {
                // Only need to do this on "small" structures
                if (_controller.Model.TotalAtomsCount < 100)
                {
                    var abb = _controller.Model.BoundingBoxOfCmlPoints;

                    double leftPadding = 0;
                    double topPadding = 0;

                    if (size.Left < abb.Left)
                    {
                        leftPadding = abb.Left - size.Left;
                    }

                    if (size.Top < abb.Top)
                    {
                        topPadding = abb.Top - size.Top;
                    }

                    _controller.Model.RepositionAll(-leftPadding, -topPadding);
                    DrawChemistry(_controller);
                }

                _positionOffsetApplied = true;
            }

            return size.Size;
        }

        #region Drawing

        #region Properties

        //properties
        private Controller _controller;

        public bool SuppressRedraw { get; set; }

        public Controller Controller
        {
            get { return _controller; }
            set
            {
                if (_controller != null && _controller != value)
                {
                    DisconnectHandlers();
                }

                _controller = value;
                _positionOffsetApplied = false;
                DrawChemistry(_controller);
                ConnectHandlers();
            }
        }

        private void ConnectHandlers()
        {
            _controller.Model.AtomsChanged += Model_AtomsChanged;
            _controller.Model.BondsChanged += Model_BondsChanged;
            _controller.Model.MoleculesChanged += Model_MoleculesChanged;
            _controller.Model.ReactionsChanged += Model_ReactionsChanged;
            _controller.Model.ReactionSchemesChanged += Model_ReactionSchemesChanged;
            _controller.Model.PropertyChanged += Model_PropertyChanged;
            _controller.Model.AnnotationsChanged += Model_AnnotationsChanged;
        }

        private void Model_AnnotationsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var eNewItem in e.NewItems)
                {
                    Annotation a = (Annotation)eNewItem;

                    AnnotationAdded(a);
                }
            }

            if (e.OldItems != null)
            {
                foreach (var eNewItem in e.OldItems)
                {
                    Annotation b = (Annotation)eNewItem;

                    AnnotationRemoved(b);
                }
            }
        }

        private void Model_ReactionSchemesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
        }

        public bool AutoResize = true;

        private void Model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (!SuppressRedraw)
            {
                switch (sender)
                {
                    case Atom a:
                        RedrawAtom(a);
                        break;

                    case Bond b:
                        RedrawBond(b);
                        break;

                    case Molecule m:
                        RedrawMolecule(m);
                        break;

                    case Reaction r:
                        RedrawReaction(r);
                        break;

                    case Annotation an:
                        RedrawAnnotation(an);
                        break;
                }

                if (AutoResize)
                {
                    InvalidateMeasure();
                }
            }
        }

        private void RedrawMolecule(Molecule molecule, Rect? boundingBox = null)
        {
            if (ChemicalVisuals.ContainsKey(molecule))
            {
                var doomed = ChemicalVisuals[molecule];
                DeleteVisual(doomed);
                ChemicalVisuals.Remove(molecule);
            }

            bool showBrackets = molecule.ShowMoleculeBrackets.HasValue && molecule.ShowMoleculeBrackets.Value
                                || molecule.Count.HasValue && molecule.Count.Value > 0
                                || molecule.FormalCharge.HasValue && molecule.FormalCharge.Value != 0
                                || molecule.SpinMultiplicity.HasValue && molecule.SpinMultiplicity.Value > 1;

            var groupKey = molecule.GetGroupKey();
            if (ChemicalVisuals.ContainsKey(groupKey)) //it's already in the list
            {
                var doomed = ChemicalVisuals[groupKey];
                DeleteVisual(doomed);
                ChemicalVisuals.Remove(groupKey);
            }

            var footprint = GetExtents(molecule);

            if (molecule.IsGrouped && ShowMoleculeGrouping)
            {
                //we may be passing in a null bounding box here

                ChemicalVisuals[groupKey] = new GroupVisual(molecule, ShowAtomsInColour, footprint);
                var gv = (GroupVisual)ChemicalVisuals[groupKey];
                gv.ChemicalVisuals = ChemicalVisuals;
                gv.Render();
                AddVisual(gv);
            }

            if (boundingBox == null)
            {
                boundingBox = Rect.Empty;
            }
            if (showBrackets)
            {
                boundingBox.Value.Union(footprint);
                var mv = new MoleculeVisual(molecule, footprint);
                ChemicalVisuals[molecule] = mv;
                mv.Render();
                AddVisual(mv);
                boundingBox.Value.Union(mv.ContentBounds);
            }
        }

        private void RedrawBond(Bond bond)
        {
            int refCount = 1;
            BondVisual bv;
            if (ChemicalVisuals.ContainsKey(bond))
            {
                bv = (BondVisual)ChemicalVisuals[bond];
                refCount = bv.RefCount;
                BondRemoved(bond);
            }

            BondAdded(bond);

            bv = (BondVisual)ChemicalVisuals[bond];
            bv.RefCount = refCount;
        }

        private void RedrawAtom(Atom atom)
        {
            int refCount = 1;
            AtomVisual av;
            if (ChemicalVisuals.ContainsKey(atom))
            {
                av = (AtomVisual)ChemicalVisuals[atom];
                refCount = av.RefCount;
                AtomRemoved(atom);
            }

            AtomAdded(atom);

            av = (AtomVisual)ChemicalVisuals[atom];
            av.RefCount = refCount;
        }

        private void RedrawReaction(Reaction reaction)
        {
            int refcount = 1;
            ReactionVisual rv;
            if (ChemicalVisuals.ContainsKey(reaction))
            {
                rv = (ReactionVisual)ChemicalVisuals[reaction];
                refcount = rv.RefCount;
                ReactionRemoved(reaction);
            }
            ReactionAdded(reaction);
            rv = (ReactionVisual)ChemicalVisuals[reaction];
            rv.RefCount = refcount;
        }

        private void Model_MoleculesChanged(object sender,
                                            NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var eNewItem in e.NewItems)
                {
                    Molecule a = (Molecule)eNewItem;

                    MoleculeAdded(a);
                }
            }

            if (e.OldItems != null)
            {
                foreach (var eNewItem in e.OldItems)
                {
                    Molecule b = (Molecule)eNewItem;

                    MoleculeRemoved(b);
                }
            }
        }

        private void Model_BondsChanged(object sender,
                                        NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var eNewItem in e.NewItems)
                {
                    Bond b = (Bond)eNewItem;

                    BondAdded(b);
                }
            }

            if (e.OldItems != null)
            {
                foreach (var eNewItem in e.OldItems)
                {
                    Bond b = (Bond)eNewItem;

                    BondRemoved(b);
                }
            }
        }

        private void Model_AtomsChanged(object sender,
                                        NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var eNewItem in e.NewItems)
                {
                    Atom a = (Atom)eNewItem;

                    AtomAdded(a);
                }
            }

            if (e.OldItems != null)
            {
                foreach (var eNewItem in e.OldItems)
                {
                    Atom a = (Atom)eNewItem;

                    AtomRemoved(a);
                }
            }
        }

        private void Model_ReactionsChanged(object sender,
                                          NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var eNewItem in e.NewItems)
                {
                    Reaction a = (Reaction)eNewItem;

                    ReactionAdded(a);
                }
            }

            if (e.OldItems != null)
            {
                foreach (var eNewItem in e.OldItems)
                {
                    Reaction b = (Reaction)eNewItem;

                    ReactionRemoved(b);
                }
            }
        }

        private void ReactionRemoved(Reaction reaction)
        {
            if (ChemicalVisuals.TryGetValue(reaction, out DrawingVisual dv))
            {
                var rv = (ReactionVisual)dv;
                DeleteVisual(rv);
                ChemicalVisuals.Remove(reaction);
            }
        }

        private void DisconnectHandlers()
        {
            _controller.Model.AtomsChanged -= Model_AtomsChanged;
            _controller.Model.BondsChanged -= Model_BondsChanged;
            _controller.Model.MoleculesChanged -= Model_MoleculesChanged;

            _controller.Model.PropertyChanged -= Model_PropertyChanged;
        }

        #endregion Properties

        #region Fields

        private readonly List<ChemicalVisual> _visuals = new List<ChemicalVisual>();

        #endregion Fields

        #region DPs

        public bool FitToContents
        {
            get { return (bool)GetValue(FitToContentsProperty); }
            set { SetValue(FitToContentsProperty, value); }
        }

        public static readonly DependencyProperty FitToContentsProperty =
            DependencyProperty.Register("FitToContents", typeof(bool), typeof(ChemistryCanvas),
                                        new PropertyMetadata(default(bool)));

        #endregion DPs

        #region Methods

        private Rect GetBoundingBox()
        {
            Rect currentbounds = Rect.Empty;

            try
            {
                foreach (DrawingVisual element in ChemicalVisuals.Values)
                {
                    var bounds = element.ContentBounds;
                    currentbounds.Union(bounds);
                    var descBounds = element.DescendantBounds;
                    currentbounds.Union(descBounds);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }

            return currentbounds;
        }

        #endregion Methods

        private void RemoveActiveAdorner()
        {
            if (_highlightAdorner != null)
            {
                var layer = AdornerLayer.GetAdornerLayer(this);
                layer.Remove(_highlightAdorner);
                _highlightAdorner = null;
            }
        }

        private void SetActiveAdorner(ChemicalVisual value)
        {
            switch (value)
            {
                case GroupVisual gv:
                    _highlightAdorner = new GroupHoverAdorner(this, gv);
                    break;

                case FunctionalGroupVisual fv:
                    _highlightAdorner = new AtomHoverAdorner(this, fv);
                    break;

                case HydrogenVisual hv:
                    _highlightAdorner = new HRotatorAdorner(this, hv);
                    break;

                case AtomVisual av:
                    _highlightAdorner = new AtomHoverAdorner(this, av);
                    break;

                case BondVisual bv:
                    _highlightAdorner = new BondHoverAdorner(this, bv);
                    break;

                case ReactionVisual rv:
                    _highlightAdorner = new ReactionHoverAdorner(this, rv);
                    break;

                default:
                    _highlightAdorner = null;
                    break;
            }
        }

        //overrides
        protected override Visual GetVisualChild(int index)
        {
            return ChemicalVisuals.ElementAt(index).Value;
        }

        protected override int VisualChildrenCount => ChemicalVisuals.Count;

        //bookkeeping collection
        public Dictionary<object, DrawingVisual> ChemicalVisuals { get; }

        public double TextSize { get; private set; }

        /// <summary>
        /// Draws the chemistry
        /// </summary>
        /// <param name="controller"></param>
        private void DrawChemistry(Controller controller)
        {
            Clear();

            if (controller != null)
            {
                foreach (Molecule molecule in controller.Model.Molecules.Values)
                {
                    MoleculeAdded(molecule);
                }
                if (controller.Model.ReactionSchemes.Any())
                {
                    foreach (Reaction reaction in controller.Model.DefaultReactionScheme.Reactions.Values)
                    {
                        ReactionAdded(reaction);
                    }
                }
                if (controller.Model.Annotations.Any())
                {
                    foreach (Annotation annotation in controller.Model.Annotations.Values)
                    {
                        AnnotationAdded(annotation);
                    }
                }
                InvalidateMeasure();
            }
        }

        private void AnnotationAdded(Annotation annotation)
        {
            AnnotationVisual av;
            if (ChemicalVisuals.ContainsKey(annotation)) //it's already in the list
            {
                av = (AnnotationVisual)ChemicalVisuals[annotation];
                DeleteVisual(av);
            }
            ChemicalVisuals[annotation] = new AnnotationVisual(annotation)
            {
                TextSize = annotation.SymbolSize ?? Controller.BlockTextSize,
                ScriptSize = TextSize * Controller.ScriptScalingFactor
            };
            av = (AnnotationVisual)ChemicalVisuals[annotation];
            av.ChemicalVisuals = ChemicalVisuals;
            av.Render();
            AddVisual(av);
        }

        private void AnnotationRemoved(Annotation annotation)
        {
            if (ChemicalVisuals.TryGetValue(annotation, out DrawingVisual dv))
            {
                var av = (AnnotationVisual)dv;

                DeleteVisual(av);
                ChemicalVisuals.Remove(annotation);
            }
        }

        private void RedrawAnnotation(Annotation annotation)
        {
            int refCount = 1;
            AnnotationVisual av;
            if (ChemicalVisuals.ContainsKey(annotation))
            {
                av = (AnnotationVisual)ChemicalVisuals[annotation];
                refCount = av.RefCount;
                AnnotationRemoved(annotation);
            }

            AnnotationAdded(annotation);

            av = (AnnotationVisual)ChemicalVisuals[annotation];
            av.RefCount = refCount;
        }

        private void ReactionAdded(Reaction reaction)
        {
            ReactionVisual rv;
            if (ChemicalVisuals.ContainsKey(reaction)) //it's already in the list
            {
                rv = (ReactionVisual)ChemicalVisuals[reaction];
                DeleteVisual(rv);
            }
            ChemicalVisuals[reaction] = new ReactionVisual(reaction)
            {
                TextSize = Controller.BlockTextSize,
                ScriptSize = Controller.BlockTextSize * Controller.ScriptScalingFactor
            };
            rv = (ReactionVisual)ChemicalVisuals[reaction];
            rv.ChemicalVisuals = ChemicalVisuals;
            rv.Render();
            AddVisual(rv);
        }

        public void Clear()
        {
            foreach (var visual in ChemicalVisuals.Values)
            {
                DeleteVisual(visual);
            }

            ChemicalVisuals.Clear();
        }

        private void DeleteVisual(DrawingVisual visual)
        {
            RemoveLogicalChild(visual);
            RemoveVisualChild(visual);
        }

        private void AddVisual(DrawingVisual visual)
        {
            AddVisualChild(visual);
            AddLogicalChild(visual);
        }

        public Rect GetDrawnBoundingBox(Molecule molecule)
        {
            var boundingBox = GetExtents(molecule);

            foreach (var m in molecule.Molecules.Values)
            {
                if (ChemicalVisuals.TryGetValue(m.GetGroupKey(), out DrawingVisual molVisual))
                {
                    GroupVisual gv = (GroupVisual)molVisual;
                    boundingBox.Union(gv.ContentBounds);
                    boundingBox.Inflate(Spacing, Spacing);
                }
                else
                {
                    boundingBox.Union(GetDrawnBoundingBox(m));
                }
            }

            return boundingBox;
        }

        public Rect GetDrawnBoundingBox(Reaction reaction)
        {
            Rect boundingBox = Rect.Empty;
            var reactionVisual = ChemicalVisuals[reaction];

            boundingBox.Union(reactionVisual.ContentBounds);
            boundingBox.Inflate(Spacing, Spacing);

            return boundingBox;
        }

        public Rect GetDrawnBoundingBox(Annotation annotation)
        {
            Rect boundingBox = Rect.Empty;
            var annotationVisual = ChemicalVisuals[annotation];
            boundingBox.Union(annotationVisual.ContentBounds);
            boundingBox.Inflate(Spacing, Spacing);
            return boundingBox;
        }

        private Rect GetExtents(Molecule molecule)
        {
            Rect boundingBox = Rect.Empty;

            foreach (var atom in molecule.Atoms.Values)
            {
                var mv = (AtomVisual)ChemicalVisuals[atom];
                var contentBounds = mv.ContentBounds;
                boundingBox.Union(contentBounds);
            }

            if (ChemicalVisuals.TryGetValue(molecule, out DrawingVisual molvisual))
            {
                boundingBox.Union(molvisual.ContentBounds);
            }

            foreach (var mol in molecule.Molecules.Values)
            {
                boundingBox.Union(GetExtents(mol));
            }
            return boundingBox;
        }

        private void MoleculeAdded(Molecule molecule)
        {
            foreach (Atom moleculeAtom in molecule.Atoms.Values)
            {
                AtomAdded(moleculeAtom);
            }

            foreach (Bond moleculeBond in molecule.Bonds)
            {
                BondAdded(moleculeBond);
            }

            foreach (Molecule child in molecule.Molecules.Values)
            {
                MoleculeAdded(child);
            }

            var boundingBox = GetDrawnBoundingBox(molecule);
            //do the final rendering of the group visual on top
            RedrawMolecule(molecule, boundingBox);
        }

        private void MoleculeRemoved(Molecule molecule)
        {
            //get rid of the molecule visual if any
            if (ChemicalVisuals.TryGetValue(molecule, out DrawingVisual mv))
            {
                DeleteVisual((MoleculeVisual)mv);
                ChemicalVisuals.Remove(molecule);
            }
            //do the group visual, if any
            if (molecule.IsGrouped
                && ChemicalVisuals.TryGetValue(molecule.GetGroupKey(), out DrawingVisual dv))
            {
                var gv = (GroupVisual)dv;

                DeleteVisual(gv);
                ChemicalVisuals.Remove(molecule.GetGroupKey());
            }

            foreach (Atom moleculeAtom in molecule.Atoms.Values)
            {
                AtomRemoved(moleculeAtom);
            }

            foreach (Bond moleculeBond in molecule.Bonds)
            {
                BondRemoved(moleculeBond);
            }

            foreach (Molecule child in molecule.Molecules.Values)
            {
                MoleculeRemoved(child);
            }
        }

        private void AtomAdded(Atom atom)
        {
            if (!ChemicalVisuals.ContainsKey(atom)) //it's not already in the list
            {
                if (atom.Element is FunctionalGroup)
                {
                    ChemicalVisuals[atom] = new FunctionalGroupVisual(atom, ShowAtomsInColour);
                }
                else
                {
                    ChemicalVisuals[atom] = new AtomVisual(atom, ShowAtomsInColour, ShowImplicitHydrogens, ShowAllCarbonAtoms);
                }
            }

            var cv = ChemicalVisuals[atom];

            if (cv is FunctionalGroupVisual fgv)
            {
                if (fgv.RefCount == 0) // it hasn't been added before
                {
                    fgv.ChemicalVisuals = ChemicalVisuals;

                    fgv.BackgroundColor = Background;
                    fgv.SymbolSize = Controller.SymbolSize;
                    fgv.SubscriptSize = Controller.SymbolSize * Controller.ScriptScalingFactor;
                    fgv.SuperscriptSize = Controller.SymbolSize * Controller.ScriptScalingFactor;
                    fgv.Standoff = Controller.Standoff;
                    fgv.Render();

                    AddVisual(fgv);
                }

                fgv.RefCount++;
            }
            else if (cv is AtomVisual av)
            {
                if (av.RefCount == 0) // it hasn't been added before
                {
                    av.ChemicalVisuals = ChemicalVisuals;
                    av.SymbolSize = Controller.SymbolSize;
                    av.SubscriptSize = Controller.SymbolSize * Controller.ScriptScalingFactor;
                    av.SuperscriptSize = Controller.SymbolSize * Controller.ScriptScalingFactor;
                    av.BackgroundColor = Background;
                    av.DisplayOverbonding = DisplayOverbondedAtoms;
                    av.Render();

                    AddVisual(av);
                }

                av.RefCount++;
            }
        }

        private void AtomRemoved(Atom atom)
        {
            if (ChemicalVisuals.TryGetValue(atom, out DrawingVisual dv))
            {
                var av = (AtomVisual)dv;

                if (av.RefCount == 1) //removing this atom will remove the last visual
                {
                    DeleteVisual(av);
                    ChemicalVisuals.Remove(atom);
                }
                else
                {
                    av.RefCount--;
                }
            }
        }

        private void BondAdded(Bond bond)
        {
            if (!ChemicalVisuals.ContainsKey(bond)) //it's already in the list
            {
                ChemicalVisuals[bond] = new BondVisual(bond);
            }

            BondVisual bv = (BondVisual)ChemicalVisuals[bond];

            if (bv.RefCount == 0) // it hasn't been added before
            {
                bv.ChemicalVisuals = ChemicalVisuals;
                bv.BondThickness = Common.BondThickness;
                bv.Standoff = Controller.Standoff;
                bv.Render();
                AddVisual(bv);
            }

            bv.RefCount++;
        }

        private void BondRemoved(Bond bond)
        {
            if (ChemicalVisuals.TryGetValue(bond, out DrawingVisual dv))
            {
                var bv = (BondVisual)dv;

                if (bv.RefCount == 1) //removing this atom will remove the last visual
                {
                    DeleteVisual(bv);
                    ChemicalVisuals.Remove(bond);
                }
                else
                {
                    bv.RefCount--;
                }
            }
        }

        #endregion Drawing

        #region Event handlers

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            ActiveVisual = GetTargetedVisual(e.GetPosition(this));
        }

        #endregion Event handlers

        #region Methods

        public ChemicalVisual GetTargetedVisual(Point p)
        {
            _visuals.Clear();

            // Re-Populate _visuals via ResultCallback with *ALL* ChemicalVisual's which are under the mouse cursor
            // GroupVisual's seem to be added first (with outermost first) one for each Group
            // Next are any BondVisual's one for each Bond
            // Next is the AtomVisual for the Atom
            VisualTreeHelper.HitTest(this, null, ResultCallback, new PointHitTestParameters(p));

            // First try to get a GroupVisual
            // HACK: [DCD] What guarantees that the first one found is the "top level" group?
            ChemicalVisual result = _visuals.FirstOrDefault(v => v is GroupVisual);

            //first try to see if we can locate the hydrogen visual
            if (result == null)
            {
                result = _visuals.FirstOrDefault(v => v is HydrogenVisual);
            }

            // If not successful try to get an AtomVisual (should only ever be one!)
            if (result == null)
            {
                result = _visuals.FirstOrDefault(v => v is AtomVisual);
            }

            // Finally get first ChemicalVisual which ought to be a BondVisual
            if (result == null)
            {
                result = _visuals.FirstOrDefault();
            }

            return result;
        }

        private HitTestResultBehavior ResultCallback(HitTestResult result)
        {
            if (result.VisualHit is HydrogenVisual hv)
            {
                _visuals.Add(hv);
            }
            else if (result.VisualHit is ChemicalVisual cv)
            {
                _visuals.Add(cv);
            }

            return HitTestResultBehavior.Continue;
        }

        #endregion Methods
    }
}