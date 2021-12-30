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
        #region Internal Constructs

        /// <summary>
        /// The relative offset of a text block to its reaction arrow.
        /// Specified as a polar coordinate
        ///  - fractional length along the arrow (default 0.5)
        ///  - angle between the start of the arrow and the offset centre
        /// </summary>
        public struct TextOffset
        {
            // Temporarily commented out as not yet used ...
            //private double FractionalLength;
            //private double Angle;
        }

        #endregion Internal Constructs

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

        public TextOffset? ReagentsBlockOffset { get; set; }
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

        public TextOffset? ConditionsBlockOffset { get; set; }

        public string Id { get; set; }
        public Guid InternalId { get; }

        public ReactionScheme Parent { get; set; }
        public bool InhibitEvents { get; private set; }

        public override string Path
        {
            get
            {
                if (Parent == null)
                {
                    return Id;
                }
                else
                {
                    return Parent.Path + "/" + Id;
                }
            }
        }

        public double Angle => Vector.AngleBetween(BasicGeometry.ScreenNorth, ReactionVector);

        public Vector ReactionVector => HeadPoint-TailPoint;

        public Point MidPoint => TailPoint + ReactionVector /2;

        public readonly ReadOnlyDictionary<Guid, Molecule> Reactants;
        public readonly ReadOnlyDictionary<Guid, Molecule> Products;

        #endregion Properties

        #region Fields

        private Dictionary<Guid, Molecule> _reactants;
        private Dictionary<Guid, Molecule> _products;

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
            InternalId = Guid.NewGuid();
            Id = InternalId.ToString("D");

            //initialise the collections
            _reactants = new Dictionary<Guid, Molecule>();
            _products = new Dictionary<Guid, Molecule>();
            Reactants = new ReadOnlyDictionary<Guid, Molecule>(_reactants);
            Products = new ReadOnlyDictionary<Guid, Molecule>(_products);
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

        public Reaction Copy()
        {
            //TODO: Copy reactants and products
            Reaction newReaction = new Reaction();
            newReaction.ConditionsText = ConditionsText;
            newReaction.HeadPoint = HeadPoint;
            newReaction.Id = Id;
            newReaction.ReactionType = ReactionType;
            newReaction.ReagentText = ReagentText;
            newReaction.TailPoint = TailPoint;
            return newReaction;
        }

        internal void RepositionAll(double x, double y)
        {
            var offsetVector = new Vector(-x, -y);
            HeadPoint += offsetVector;
            TailPoint += offsetVector;
        }

        public void AddProduct(Molecule product)
        {
            _products[product.InternalId] = product;
            NotifyCollectionChangedEventArgs e =
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,
                                                     new List<Molecule> { product });
            OnProductsChanged(this, e);
        }

        internal void ReLabelGuids(ref int reactionCount)
        {
            if (Guid.TryParse(Id, out _))
            {
                Id = $"r{++reactionCount}";
            }
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

        public void UpdateVisual()
        {
            OnPropertyChanged(nameof(ReactionType));
        }

        #endregion Methods
    }
}