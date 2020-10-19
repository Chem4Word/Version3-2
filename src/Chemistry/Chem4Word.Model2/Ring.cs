// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using Chem4Word.Model2.Annotations;
using Chem4Word.Model2.Geometry;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.Model2
{
    /// <summary>
    /// Represents an unordered ring of atoms
    /// </summary>
    public class Ring : IComparer<Ring>, INotifyPropertyChanged
    {
        /// <summary>
        /// Indicates which rings of a set of fused rings
        /// should host a double bond in preference
        /// </summary>
        public int Priority
        {
            get
            {
                int result = -1;

                if (Atoms != null)
                {
                    switch (Atoms.Count)
                    {
                        case 6:
                            result = 1;
                            break;

                        case 5:
                            result = 2;
                            break;

                        case 7:
                            result = 3;
                            break;

                        case 4:
                            result = 4;
                            break;

                        case 3:
                            result = 5;
                            break;

                        case 8:
                            result = 6;
                            break;

                        case 9:
                            result = 7;
                            break;

                        default:
                            result = -1;
                            break;
                    }
                }

                return result;
            }
        }

        #region Properties

        /// <summary>
        /// Collection of atoms that go to make up the ring
        /// </summary>
        public HashSet<Atom> Atoms { get; }

        /// <summary>
        /// Returns a dynamically inferred enumerable of ring bonds
        /// </summary>
        public IEnumerable<Bond> Bonds
        {
            get
            {
                HashSet<Bond> sofar = new HashSet<Bond>();
                //get back a list of lists of all those bonds shared between a ring atom and its ring neighbours
                var ringbonds = from a in Atoms
                                select new
                                {
                                    Bondlist = (
                                        from n in a.Neighbours
                                        where Atoms.Contains(n)
                                        select new { Bond = a.BondBetween(n) }
                                    )
                                };

                //and then flatten it
                var rbs = (from rb in ringbonds
                           from b in rb.Bondlist
                           select b.Bond).Distinct();

                return rbs;
            }
        }

        /// <summary>
        /// For the given Bond object determines how best to place its double bond line
        ///  </summary>
        /// <param name="b">Bond object (should be part of the ring)</param>
        /// <returns>BondDirection showing how to place the bond</returns>
        public Globals.BondDirection InternalPlacement(Bond b)
        {
            Point? center = Centroid;
            if (center != null)
            {
                Vector toCenter = center.Value - b.StartAtom.Position;
                Vector bv = b.BondVector;
                if (Vector.AngleBetween(toCenter, bv) > 0)
                {
                    return Globals.BondDirection.Clockwise;
                }

                return Globals.BondDirection.Anticlockwise;
            }

            return Globals.BondDirection.None;
        }

        /// <summary>
        /// Molecule that contains ring.
        ///  </summary>
        /// <remarks>Do NOT set explicitly.  Add or remove the ring from a Molecule</remarks>
        public Molecule Parent { get; set; }

        public void RingCentroidChanged()
        {
            OnPropertyChanged(nameof(Centroid));
        }

        /// <summary>
        /// The center of the ring
        /// </summary>
        public Point? Centroid
        {
            get
            {
                return Geometry<Atom>.GetCentroid(Traverse().ToArray(), atom => atom.Position);
            }
        }

        public List<Atom> ConvexHull
        {
            get
            {
                var atomList = AtomsSortedForHull();

                return Geometry<Atom>.GetHull(atomList, atom => atom.Position);
            }
        }

        private IOrderedEnumerable<Atom> AtomsSortedForHull()
        {
            var atomList = from Atom a in Atoms
                           orderby a.Position.X, a.Position.Y descending
                           select a;
            return atomList;
        }

        //generates a unique ID for each ring based on the atom hash codes()
        public string UniqueID
        {
            get
            {
                return Atoms.Select(a => a.GetHashCode().ToString()).OrderBy(hc => hc)
                    .Aggregate((s, hc) => s + "|" + hc);
            }
        }

        public string Membership
        {
            get { return "[" + Atoms.Select(a => a.Id).OrderBy(hc => hc).Aggregate((s, hc) => s + ", " + hc) + "]"; }
        }

        /// <summary>
        /// Circles a ring
        /// </summary>
        /// <param name="start">Atom to start at</param>
        /// <param name="direction">Which direction to go in</param>
        /// <returns>IEnumerable&lt;Atom&gt; that iterates through the ring</returns>
        public IEnumerable<Atom> Traverse(Atom start = null,
            Globals.BondDirection direction = Globals.BondDirection.Anticlockwise)
        {
            HashSet<Atom> res = new HashSet<Atom>();
            res.Add(start);
            Atom next;
            if (start == null)
            {
                start = Atoms.First();
            }

            //start with the start atom, and find the other two adjacent atoms that are part of the ring
            var adj = from n in start.Neighbours
                      where Atoms.Contains(n)
                      select n;
            var nextatoms = adj.ToArray();

            if (nextatoms.Length >= 2)
            {
                Vector v1 = nextatoms[0].Position - start.Position;
                Vector v2 = nextatoms[1].Position - start.Position;

                //make sure a positive angle is the direction in which we want to travel
                //multiply the angle by the direction to choose the correct atom
                double angle = (int)direction * Vector.AngleBetween(v1, v2);

                next = angle > 0 ? nextatoms[0] : nextatoms[1];
                //circle the ring, making sure we ignore atoms we've visited already
                while (next != null)
                {
                    yield return next;
                    res.Add(next);
                    var candidates = next.NeighbourSet; //get the set of atoms around the next atom
                    //get rid of all the atoms NOT in the ring or already in the set
                    candidates.RemoveWhere(a => res.Contains(a) || !Atoms.Contains(a));
                    next = candidates.FirstOrDefault();
                }
            }
        }

        #endregion Properties

        #region Constructors

        public Ring()
        {
            Atoms = new HashSet<Atom>();
        }

        public Ring(HashSet<Atom> ringAtoms) : this()
        {
            Atoms = new HashSet<Atom>(ringAtoms);
        }

        #endregion Constructors

        #region Operators

        public int Compare(Ring x, Ring y)
        {
            return string.Compare(x?.UniqueID, y?.UniqueID, StringComparison.Ordinal);
        }

        #endregion Operators

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}