// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------
using Chem4Word.Model2.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Chem4Word.Model2
{
    public class ReactionScheme : INotifyPropertyChanged
    {
        public readonly ReadOnlyDictionary<string, Reaction> Reactions;
        private readonly Dictionary<string, Reaction> _reactions;
        public string Id { get; private set; }
        public string InternalId { get; }
        public Model Parent { get; set; }
        public IChemistryContainer Root => throw new System.NotImplementedException();

        public event NotifyCollectionChangedEventHandler ReactionsChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public ReactionScheme()
        {
             Id = Guid.NewGuid().ToString("D");
            InternalId = Id;
            _reactions = new Dictionary<string, Reaction>();
            Reactions = new ReadOnlyDictionary<string, Reaction>(_reactions);
        }

        public void AddReaction(Reaction reaction)
        {
            _reactions[reaction.InternalId] = reaction;
            NotifyCollectionChangedEventArgs e =
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,
                                                     new List<Reaction> { reaction });
            OnReactionsChanged(this, e);
            UpdateReactionEventHandlers(e);
        }
        //transmits a reaction being added or removed
        private void OnReactionsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyCollectionChangedEventHandler temp = ReactionsChanged;
            if (temp != null)
            {
                temp.Invoke(sender, e);
            }
        }

        public void RemoveReaction(Reaction reaction)
        {
            bool result = _reactions.Remove(reaction.InternalId); ;
            if (result)
            {
                NotifyCollectionChangedEventArgs e =
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
                                                         new List<Reaction> { reaction });
                OnReactionsChanged(this, e);
                UpdateReactionEventHandlers(e);
            }
        }

        private void UpdateReactionEventHandlers(NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var oldItem in e.OldItems)
                {
                    var r = (Reaction)oldItem;
                    r.ReactantsChanged -= Reaction_ReactantsChanged;
                    r.ProductsChanged -= Reaction_ProductsChanged;
                    r.PropertyChanged -= Reaction_PropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (var newItem in e.NewItems)
                {
                    var r = (Reaction)newItem;
                    r.ReactantsChanged += Reaction_ReactantsChanged;
                    r.ProductsChanged += Reaction_ProductsChanged;
                    r.PropertyChanged += Reaction_PropertyChanged;
                }
            }
        }

        private void Reaction_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(sender, e);
        }

        private void Reaction_ProductsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            throw new System.NotImplementedException();
        }

        private void Reaction_ReactantsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            throw new System.NotImplementedException();
        }
    }
}