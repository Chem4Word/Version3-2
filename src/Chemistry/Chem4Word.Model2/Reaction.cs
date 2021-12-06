// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------
using Chem4Word.Model2.Geometry;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using static Chem4Word.Model2.Helpers.Globals;

namespace Chem4Word.Model2
{
    public class Reaction : ChemistryBase, INotifyPropertyChanged
    {
        #region Properties

        private ReactionType _reactionType;

        public ReactionType ReactionType
        {
            get { return _reactionType; }
            set
            {
                _reactionType = value;
                OnPropertyChanged();
            }
        }

        private Point _startPoint;

        public void UpdateVisual()
        {
            OnPropertyChanged(nameof(ReactionType));
        }

        public Point TailPoint
        {
            get { return _startPoint; }
            set
            {
                _startPoint = value;
                OnPropertyChanged();
            }
        }

        private Point _endPoint;

        public Point HeadPoint
        {
            get { return _endPoint; }
            set
            {
                _endPoint = value;
                OnPropertyChanged();
            }
        }

        private string _reagentText;

        public string ReagentText
        {
            get { return _reagentText; }
            set
            {
                _reagentText = value;
                OnPropertyChanged();
            }
        }

        private string _conditionsText;

        public string ConditionsText
        {
            get { return _conditionsText; }
            set
            {
                _conditionsText = value;
                OnPropertyChanged();
            }
        }

        public string Id { get; private set; }
        public string InternalId { get; }

        public ReactionScheme Parent { get; set; }
        public bool InhibitEvents { get; private set; }

        public override string Path => throw new NotImplementedException();

        public double Angle => Vector.AngleBetween(BasicGeometry.ScreenNorth, HeadPoint - TailPoint);
        public readonly ReadOnlyDictionary<string, Molecule> Reactants;
        public readonly ReadOnlyDictionary<string, Molecule> Products;

        #endregion Properties

        #region Fields

        private Dictionary<string, Molecule> _reactants;
        private Dictionary<string, Molecule> _products;

        #endregion Fields

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        public event NotifyCollectionChangedEventHandler ReactantsChanged;

        public event NotifyCollectionChangedEventHandler ProductsChanged;

        #endregion Events

        #region Constructors

        public Reaction()
        {
            // first set up the Ids
            Id = Guid.NewGuid().ToString("D");
            InternalId = Id;
            //initialise the collections
            _reactants = new Dictionary<string, Molecule>();
            _products = new Dictionary<string, Molecule>();
            Reactants = new ReadOnlyDictionary<string, Molecule>(_reactants);
            Products = new ReadOnlyDictionary<string, Molecule>(_products);
        }

        #endregion Constructors

        #region Methods

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void AddReactant(Molecule reactant)
        {
            _reactants[reactant.InternalId] = reactant;
            NotifyCollectionChangedEventArgs e =
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,
                                                     new List<Molecule> { reactant });
            OnReactantsChanged(this, e);
        }

        private void OnReactantsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyCollectionChangedEventHandler temp = ReactantsChanged;
            if (temp != null)
            {
                temp.Invoke(sender, e);
            }
        }

        public void RemoveReactant(Molecule reactant)
        {
            bool result = _reactants.Remove(reactant.InternalId); ;
            if (result)
            {
                NotifyCollectionChangedEventArgs e =
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
                                                         new List<Molecule> { reactant });
                OnReactantsChanged(this, e);
            }
        }

        public void AddProduct(Molecule product)
        {
            _products[product.InternalId] = product;
            NotifyCollectionChangedEventArgs e =
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,
                                                     new List<Molecule> { product });
            OnProductsChanged(this, e);
        }

        private void OnProductsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyCollectionChangedEventHandler temp = ProductsChanged;
            if (temp != null)
            {
                temp.Invoke(sender, e);
            }
        }

        public void RemoveProduct(Molecule product)
        {
            bool result = _products.Remove(product.InternalId); ;
            if (result)
            {
                NotifyCollectionChangedEventArgs e =
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
                                                         new List<Molecule> { product });
                OnProductsChanged(this, e);
            }
        }

        #endregion Methods
    }
}