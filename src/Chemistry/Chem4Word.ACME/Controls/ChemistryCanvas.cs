// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Chem4Word.ACME.Adorners.Feedback;
using Chem4Word.ACME.Drawing;
using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;

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
            chemicalVisuals = new Dictionary<object, DrawingVisual>();
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

        public AtomVisual ActiveAtomVisual => (ActiveVisual as AtomVisual);

        public BondVisual ActiveBondVisual => (ActiveVisual as BondVisual);

        public ChemistryBase ActiveChemistry
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
                    ActiveVisual = (chemicalVisuals[(value)] as ChemicalVisual);
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
                canvas.DrawChemistry(canvas.ViewModel);
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
            Debug.WriteLine($"MeasureOverride({GetHashCode()})");
            var size = GetBoundingBox();

            if (_viewModel != null
                && _viewModel.Model != null
                && !_positionOffsetApplied)
            {
                // Only need to do this on "small" structures
                if (_viewModel.Model.TotalAtomsCount < 100)
                {
                    var abb = _viewModel.Model.BoundingBoxOfCmlPoints;

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

                    _viewModel.Model.RepositionAll(-leftPadding, -topPadding);
                    DrawChemistry(_viewModel);
                }

                _positionOffsetApplied = true;
            }

            return size.Size;
        }

        #region Drawing

        #region Properties

        //properties
        private ViewModel _viewModel;

        public bool SuppressRedraw { get; set; }

        public ViewModel ViewModel
        {
            get { return _viewModel; }
            set
            {
                if (_viewModel != null && _viewModel != value)
                {
                    DisconnectHandlers();
                }

                _viewModel = value;
                _positionOffsetApplied = false;
                DrawChemistry(_viewModel);
                ConnectHandlers();
            }
        }

        private void ConnectHandlers()
        {
            _viewModel.Model.AtomsChanged += Model_AtomsChanged;
            _viewModel.Model.BondsChanged += Model_BondsChanged;
            _viewModel.Model.MoleculesChanged += Model_MoleculesChanged;

            _viewModel.Model.PropertyChanged += Model_PropertyChanged;
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
                }

                if (AutoResize)
                {
                    InvalidateMeasure();
                }
            }
        }

        private void RedrawMolecule(Molecule molecule, Rect? boundingBox = null)
        {
            if (chemicalVisuals.ContainsKey(molecule))
            {
                var doomed = chemicalVisuals[molecule];
                DeleteVisual(doomed);
                chemicalVisuals.Remove(molecule);
            }

            bool showBrackets = molecule.ShowMoleculeBrackets.HasValue && molecule.ShowMoleculeBrackets.Value
                                || molecule.Count.HasValue && molecule.Count.Value > 0
                                || molecule.FormalCharge.HasValue && molecule.FormalCharge.Value != 0
                                || molecule.SpinMultiplicity.HasValue && molecule.SpinMultiplicity.Value > 1;

            var groupKey = molecule.GetGroupKey();
            if (chemicalVisuals.ContainsKey(groupKey)) //it's already in the list
            {
                var doomed = chemicalVisuals[groupKey];
                DeleteVisual(doomed);
                chemicalVisuals.Remove(groupKey);
            }

            var footprint = GetExtents(molecule);

            if (molecule.IsGrouped && ShowMoleculeGrouping)
            {
                //we may be passing in a null bounding box here

                chemicalVisuals[groupKey] = new GroupVisual(molecule, ShowAtomsInColour, footprint);
                var gv = (GroupVisual)chemicalVisuals[groupKey];
                gv.ChemicalVisuals = chemicalVisuals;
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
                chemicalVisuals[molecule] = mv;
                mv.Render();
                AddVisual(mv);
                boundingBox.Value.Union(mv.ContentBounds);
            }
        }

        private void RedrawBond(Bond bond)
        {
            int refCount = 1;
            BondVisual bv = null;
            if (chemicalVisuals.ContainsKey(bond))
            {
                bv = (BondVisual)chemicalVisuals[bond];
                refCount = bv.RefCount;
                BondRemoved(bond);
            }

            BondAdded(bond);

            bv = (BondVisual)chemicalVisuals[bond];
            bv.RefCount = refCount;
        }

        private void RedrawAtom(Atom atom)
        {
            int refCount = 1;
            AtomVisual av = null;
            if (chemicalVisuals.ContainsKey(atom))
            {
                av = (AtomVisual)chemicalVisuals[atom];
                refCount = av.RefCount;
                AtomRemoved(atom);
            }

            AtomAdded(atom);

            av = (AtomVisual)chemicalVisuals[atom];
            av.RefCount = refCount;
        }

        private void Model_MoleculesChanged(object sender,
                                            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
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
                                        System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
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
                                        System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
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

        private void DisconnectHandlers()
        {
            _viewModel.Model.AtomsChanged -= Model_AtomsChanged;
            _viewModel.Model.BondsChanged -= Model_BondsChanged;
            _viewModel.Model.MoleculesChanged -= Model_MoleculesChanged;

            _viewModel.Model.PropertyChanged -= Model_PropertyChanged;
        }

        #endregion Properties

        #region Fields

        private ChemicalVisual _visualHit;

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
                foreach (DrawingVisual element in chemicalVisuals.Values)
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

                case AtomVisual av:
                    _highlightAdorner = new AtomHoverAdorner(this, av);
                    break;

                case BondVisual bv:
                    _highlightAdorner = new BondHoverAdorner(this, bv);
                    break;

                default:
                    _highlightAdorner = null;
                    break;
            }
        }

        //overrides
        protected override Visual GetVisualChild(int index)
        {
            return chemicalVisuals.ElementAt(index).Value;
        }

        protected override int VisualChildrenCount => chemicalVisuals.Count;

        //bookkeeping collection
        protected Dictionary<object, DrawingVisual> chemicalVisuals { get; }

        /// <summary>
        /// Draws the chemistry
        /// </summary>
        /// <param name="vm"></param>
        private void DrawChemistry(ViewModel vm)
        {
            Clear();

            if (vm != null)
            {
                foreach (Molecule molecule in vm.Model.Molecules.Values)
                {
                    MoleculeAdded(molecule);
                }

                InvalidateMeasure();
            }
        }

        public void Clear()
        {
            foreach (var visual in chemicalVisuals.Values)
            {
                DeleteVisual(visual);
            }

            chemicalVisuals.Clear();
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
            var bb = GetExtents(molecule);

            foreach (var m in molecule.Molecules.Values)
            {
                if (chemicalVisuals.TryGetValue(m.GetGroupKey(), out DrawingVisual molVisual))
                {
                    GroupVisual gv = (GroupVisual)molVisual;
                    bb.Union(gv.ContentBounds);
                    bb.Inflate(Spacing, Spacing);
                }
                else
                {
                    bb.Union(GetDrawnBoundingBox(m));
                }
            }

            return bb;
        }

        private Rect GetExtents(Molecule molecule)
        {
            Rect bb = Rect.Empty;

            foreach (var atom in molecule.Atoms.Values)
            {
                var mv = (AtomVisual)chemicalVisuals[atom];
                var contentBounds = mv.ContentBounds;
                bb.Union(contentBounds);
            }

            if (chemicalVisuals.TryGetValue(molecule, out DrawingVisual molvisual))
            {
                bb.Union(molvisual.ContentBounds);
            }

            foreach (var mol in molecule.Molecules.Values)
            {
                bb.Union(GetExtents(mol));
            }
            return bb;
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

            var bb = GetDrawnBoundingBox(molecule);
            //do the final rendering of the group visual on top
            RedrawMolecule(molecule, bb);
        }

        private void MoleculeRemoved(Molecule molecule)
        {
            //get rid of the molecule visual if any
            if (chemicalVisuals.TryGetValue(molecule, out DrawingVisual mv))
            {
                DeleteVisual((MoleculeVisual)mv);
                chemicalVisuals.Remove(molecule);
            }
            //do the group visual, if any
            if (molecule.IsGrouped
                && chemicalVisuals.TryGetValue(molecule.GetGroupKey(), out DrawingVisual dv))
            {
                var gv = (GroupVisual)dv;

                DeleteVisual(gv);
                chemicalVisuals.Remove(molecule.GetGroupKey());
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
            if (!chemicalVisuals.ContainsKey(atom)) //it's not already in the list
            {
                if (atom.Element is FunctionalGroup)
                {
                    chemicalVisuals[atom] = new FunctionalGroupVisual(atom, ShowAtomsInColour);
                }
                else
                {
                    chemicalVisuals[atom] = new AtomVisual(atom, ShowAtomsInColour, ShowImplicitHydrogens, ShowAllCarbonAtoms);
                }
            }

            var cv = chemicalVisuals[atom];

            if (cv is FunctionalGroupVisual fgv)
            {
                if (fgv.RefCount == 0) // it hasn't been added before
                {
                    fgv.ChemicalVisuals = chemicalVisuals;

                    fgv.BackgroundColor = Background;
                    fgv.SymbolSize = ViewModel.SymbolSize;
                    fgv.SubscriptSize = ViewModel.SymbolSize * ViewModel.ScriptScalingFactor;
                    fgv.SuperscriptSize = ViewModel.SymbolSize * ViewModel.ScriptScalingFactor;
                    fgv.Standoff = ViewModel.Standoff;
                    fgv.Render();

                    AddVisual(fgv);
                }

                fgv.RefCount++;
            }
            else if (cv is AtomVisual av)
            {
                if (av.RefCount == 0) // it hasn't been added before
                {
                    av.ChemicalVisuals = chemicalVisuals;
                    av.SymbolSize = ViewModel.SymbolSize;
                    av.SubscriptSize = ViewModel.SymbolSize * ViewModel.ScriptScalingFactor;
                    av.SuperscriptSize = ViewModel.SymbolSize * ViewModel.ScriptScalingFactor;
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
            if (chemicalVisuals.TryGetValue(atom, out DrawingVisual dv))
            {
                var av = (AtomVisual)dv;

                if (av.RefCount == 1) //removing this atom will remove the last visual
                {
                    DeleteVisual(av);
                    chemicalVisuals.Remove(atom);
                }
                else
                {
                    av.RefCount--;
                }
            }
        }

        private void BondAdded(Bond bond)
        {
            if (!chemicalVisuals.ContainsKey(bond)) //it's already in the list
            {
                chemicalVisuals[bond] = new BondVisual(bond);
            }

            BondVisual bv = (BondVisual)chemicalVisuals[bond];

            if (bv.RefCount == 0) // it hasn't been added before
            {
                bv.ChemicalVisuals = chemicalVisuals;
                bv.BondThickness = Globals.BondThickness;
                bv.Standoff = ViewModel.Standoff;
                bv.Render();
                AddVisual(bv);
            }

            bv.RefCount++;
        }

        private void BondRemoved(Bond bond)
        {
            if (chemicalVisuals.TryGetValue(bond, out DrawingVisual dv))
            {
                var bv = (BondVisual)dv;

                if (bv.RefCount == 1) //removing this atom will remove the last visual
                {
                    DeleteVisual(bv);
                    chemicalVisuals.Remove(bond);
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
            // HACK: What guarantees that the first one found is the "top level" group?
            ChemicalVisual result = _visuals.FirstOrDefault(v => v is GroupVisual);

            // If not successful try to get an AtomVisual (should only ever be one!)
            if (result == null)
            {
                result = _visuals.FirstOrDefault(v => v is AtomVisual || v is FunctionalGroupVisual);
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
            _visualHit = result.VisualHit as ChemicalVisual;

            if (_visualHit != null)
            {
                _visuals.Add(_visualHit);
            }

            return HitTestResultBehavior.Continue;
        }

        #endregion Methods
    }
}