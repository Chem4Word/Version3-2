// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using Chem4Word.Model2.Annotations;
using Chem4Word.Model2.Geometry;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.Model2
{
    public class Bond : ChemistryBase, INotifyPropertyChanged, IEquatable<Bond>
    {
        #region Properties

        public string EndAtomInternalId { get; set; }

        public string StartAtomInternalId { get; set; }

        public Atom EndAtom
        {
            get
            {
                return Parent.Atoms[EndAtomInternalId];
            }
        }

        public Atom StartAtom
        {
            get
            {
                return Parent.Atoms[StartAtomInternalId];
            }
        }

        public List<Atom> GetAtoms()
        {
            return new List<Atom> { StartAtom, EndAtom };
        }

        public Molecule Parent { get; set; }

        public Model Model
        {
            get { return Parent.Model; }
        }

        public List<Ring> Rings
        {
            get
            {
                List<Ring> result = new List<Ring>();
                foreach (Ring parentRing in Parent.Rings)
                {
                    if (parentRing.Atoms.Contains(StartAtom) && parentRing.Atoms.Contains(EndAtom))
                    {
                        result.Add(parentRing);
                    }
                }

                return result;
            }
        }

        public Point MidPoint => new Point((StartAtom.Position.X + EndAtom.Position.X) / 2,
            (StartAtom.Position.Y + EndAtom.Position.Y) / 2);

        public string Id { get; set; }

        public string InternalId { get; }

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

        #region Bond Orders

        private string _order;

        public string Order
        {
            get { return _order; }
            set
            {
                if (value.Equals("0.5"))
                {
                    value = Globals.OrderPartial01;
                    ResetStereo();
                }
                if (value.Equals("1") || value.Equals("S"))
                {
                    value = Globals.OrderSingle;
                }
                if (value.Equals("1.5"))
                {
                    value = Globals.OrderPartial12;
                    ResetStereo();
                }
                if (value.Equals("2") || value.Equals("D"))
                {
                    value = Globals.OrderDouble;
                }
                if (value.Equals("3") || value.Equals("T"))
                {
                    value = Globals.OrderTriple;
                    ResetStereo();
                }
                if (value.Equals("0"))
                {
                    value = Globals.OrderZero;
                    ResetStereo();
                }

                _order = value;
                OnPropertyChanged();

                // local function
                void ResetStereo()
                {
                    if (Stereo == Globals.BondStereo.Wedge
                        || Stereo == Globals.BondStereo.Hatch
                        || Stereo == Globals.BondStereo.Indeterminate)
                    {
                        Stereo = Globals.BondStereo.None;
                    }
                }
            }
        }

        public double? OrderValue => OrderToOrderValue(Order);

        public static double? OrderToOrderValue(string order)
        {
            switch (order)
            {
                case Globals.OrderZero:
                case Globals.OrderOther:
                    return 0;

                case Globals.OrderPartial01:
                    return 0.5;

                case Globals.OrderSingle:
                    return 1;

                case Globals.OrderPartial12:
                    return 1.5;

                case Globals.OrderAromatic:
                    return 1.5;

                case Globals.OrderDouble:
                    return 2;

                case Globals.OrderPartial23:
                    return 2.5;

                case Globals.OrderTriple:
                    return 3;

                default:
                    return null;
            }
        }

        #endregion Bond Orders

        private Globals.BondStereo _stereo;

        public Globals.BondStereo Stereo
        {
            get { return _stereo; }
            set
            {
                _stereo = value;
                OnPropertyChanged();
            }
        }

        public object Tag { get; set; }
        public List<string> Messages { get; private set; }

        public Globals.BondDirection Placement
        {
            get
            {
                if (OrderValue == 2 || OrderValue == 1.5 || OrderValue == 2.5)
                {
                    //force a recalc of the rings if necessary
                    if (!Parent.RingsCalculated)
                    {
                        Parent.RebuildRings();
                    }
                    return ExplicitPlacement ?? ImplicitPlacement ?? Globals.BondDirection.None;
                }

                return Globals.BondDirection.None;
            }
            set
            {
                ExplicitPlacement = value;
                OnPropertyChanged();
            }
        }

        public Globals.BondDirection? ExplicitPlacement { get; set; }

        private Vector? VectorOnSideOfNonHAtomFromStartLigands(Atom startAtom, Atom endAtom)
        {
            Vector? displacementVector = null;

            // GitHub: Issue #15 https://github.com/Chem4Word/Version3/issues/15
            try
            {
                Vector posDisplacementVector = BondVector.Perpendicular();
                Vector negDisplacementVector = -posDisplacementVector;
                posDisplacementVector.Normalize();
                negDisplacementVector.Normalize();

                posDisplacementVector = posDisplacementVector * 3;
                negDisplacementVector = negDisplacementVector * 3;

                Point posEndPoint = endAtom.Position + posDisplacementVector;
                Point negEndPoint = endAtom.Position + negDisplacementVector;

                Atom nonHAtom = startAtom.Neighbours.FirstOrDefault(n => n != endAtom && n.Element as Element != Globals.PeriodicTable.H);
                if (nonHAtom != null)
                {
                    Point nonHAtomLoc = nonHAtom.Position;

                    double posDist = (nonHAtomLoc - posEndPoint).Length;
                    double negDist = (nonHAtomLoc - negEndPoint).Length;

                    bool posDisplacement = posDist < negDist;
                    displacementVector = posDisplacement ? posDisplacementVector : negDisplacementVector;
                }
            }
            catch
            {
                // Do Nothing
            }

            return displacementVector;
        }

        public Ring PrimaryRing
        {
            get
            {
                Ring ring = null;

                if (Rings.Any())
                {
                    List<Ring> ringList = Parent.SortedRings;
                    var sortedRings = (
                        from Ring r in ringList
                        where r.Atoms.Contains(StartAtom) && r.Atoms.Contains(EndAtom)
                        select r
                    ).ToList();

                    if (sortedRings.Count >= 1)
                    {
                        ring = sortedRings[0];
                    }
                }

                return ring;
            }
        }

        public Ring SubsidiaryRing
        {
            get
            {
                Ring ring = null;

                if (Rings.Any())
                {
                    List<Ring> ringList = Parent.SortedRings;
                    var sortedRings = (
                        from Ring r in ringList
                        where r.Atoms.Contains(StartAtom) && r.Atoms.Contains(EndAtom)
                        select r
                    ).ToList();

                    if (sortedRings.Count >= 2)
                    {
                        ring = sortedRings[1];
                    }
                }

                return ring;
            }
        }

        public bool IsCyclic()
        {
            return Rings.Any();
        }

        public Vector? GetPrettyCyclicDoubleBondVector()
        {
            Debug.Assert(Parent.RingsCalculated);

            Vector? vector = null;

            if (PrimaryRing != null)
            {
                List<Ring> ringList = Rings.Where(x => x.Priority > 0).OrderBy(x => x.Priority).ToList();

                if (ringList.Any()) //no rings
                {
                    Point? ringCentroid = PrimaryRing.Centroid;
                    vector = ringCentroid - MidPoint;
                }
            }

            return vector;
        }

        public Vector? GetPrettyDoubleBondVector()
        {
            Vector? vector = null;

            if (IsCyclic())
            {
                return GetPrettyCyclicDoubleBondVector();
            }

            // We're acyclic.

            var startLigands = (from Atom a in StartAtom.Neighbours
                                where a != EndAtom
                                select a).ToList();

            if (!startLigands.Any())
            {
                return null;
            }

            var endLigands = (from Atom b in EndAtom.Neighbours
                              where b != StartAtom
                              select b).ToList();

            if (!endLigands.Any())
            {
                return null;
            }

            if (startLigands.Count > 2 || endLigands.Count > 2)
            {
                return null;
            }

            if (startLigands.Count == 2 && endLigands.Count == 2)
            {
                return null;
            }

            if (startLigands.AreAllH() && endLigands.AreAllH())
            {
                return null;
            }

            if (startLigands.ContainNoH() && endLigands.ContainNoH())
            {
                return null;
            }

            if (startLigands.GetHCount() == 1 && endLigands.GetNonHCount() == 1)
            {
                if (endLigands.Count == 2)
                {
                    if (endLigands.GetHCount() == 2 || endLigands.ContainNoH())
                    {
                        //Double sided bond on the side of the non H atom from StartLigands
                        //Elbow bond :¬)
                        return VectorOnSideOfNonHAtomFromStartLigands(StartAtom, EndAtom);
                    }
                    // Now must have 1 H and 1 !H
                    if (AtomsAreCis(startLigands.GetFirstNonH(), endLigands.GetFirstNonH())
                        /*if a2a H on the same side as a1a H*/)
                    {
                        //double bond on the side of non H
                        return VectorOnSideOfNonHAtomFromStartLigands(StartAtom, EndAtom);
                    }

                    //Now must be a trans bond.
                    return null;
                }

                //Count now 1
                if (endLigands.GetHCount() == 1)
                {
                    //Double bond on the side of non H from StartLigands, bevel 1 end.
                    return VectorOnSideOfNonHAtomFromStartLigands(StartAtom, EndAtom);
                }

                //Now !H
                if (AtomsAreCis(startLigands.GetFirstNonH(), endLigands.GetFirstNonH())
                    /*EndAtomAtom's !H is on the same side as StartAtomAtom's !H*/)
                {
                    //double bond on the side of !H from StartLigands, bevel both ends
                    return VectorOnSideOfNonHAtomFromStartLigands(StartAtom, EndAtom);
                }

                //Now must be a trans bond.
                return null;
            }

            if (startLigands.AreAllH())
            {
                if (endLigands.Count == 2)
                {
                    if (endLigands.ContainNoH())
                    {
                        return null;
                    }

                    //Must now have 1 H and 1 !H
                    //double bond on the side of EndLigands' !H, bevel 1 end only
                    return VectorOnSideOfNonHAtomFromStartLigands(StartAtom, EndAtom);
                }
                //Count must now be 1
                // Must now be 1 !H
                // Double bond on the side of EndLigands' !H, bevel 1 end only.
                return VectorOnSideOfNonHAtomFromStartLigands(StartAtom, EndAtom);
            }

            if (startLigands.GetHCount() == 0)
            {
                if (endLigands.Count == 2)
                {
                    if (endLigands.AreAllH())
                    {
                        return null;
                    }
                    if (endLigands.ContainNoH())
                    {
                        return null;
                    }
                    // Now must have 1 H and 1 !H
                    //Double bond on the side of EndLigands' !H, bevel both ends.
                    return VectorOnSideOfNonHAtomFromStartLigands(StartAtom, EndAtom);
                }
                //Count is 1
                if (endLigands.GetHCount() == 1)
                {
                    return null;
                }

                if (endLigands.GetHCount() == 0)
                {
                    //double bond on the side of EndLigands' !H, bevel both ends.
                    return VectorOnSideOfNonHAtomFromStartLigands(StartAtom, EndAtom);
                }
            }
            // StartLigands' count = 1
            else if (startLigands.GetHCount() == 1)
            {
                if (endLigands.Count == 2)
                {
                    if (endLigands.AreAllH())
                    {
                        return null;
                    }
                    if (endLigands.ContainNoH())
                    {
                        return null;
                    }

                    //Now EndLigands contains 1 H and 1 !H
                    //double bond on side of EndLigands' !H, bevel 1 end
                    return VectorOnSideOfNonHAtomFromStartLigands(StartAtom, EndAtom);
                }

                if (endLigands.AreAllH())
                {
                    return null;
                }
            }
            return vector;
        }

        public Vector? GetUncrowdedSideVector()
        {
            var dbv = GetPrettyDoubleBondVector();
            if (dbv != null)
            {
                return dbv.Value;
            }
            else
            {
                if (PseudoCentroid != null)
                {
                    return PseudoCentroid - MidPoint;
                }
                else
                {
                    if (StartAtom.Neighbours.Count() == 1 && EndAtom.Neighbours.Count() == 2)
                    {
                        var tempvector = EndAtom.NeighboursExcept(StartAtom)[0].Position - StartAtom.Position;
                        var perp = Vector.Multiply(BondVector.Perpendicular(), tempvector) * BondVector.Perpendicular();
                        return perp;
                    }
                    else if (StartAtom.Neighbours.Count() == 2 && EndAtom.Neighbours.Count() == 1)
                    {
                        var tempvector = StartAtom.NeighboursExcept(EndAtom)[0].Position - EndAtom.Position;
                        var perp = Vector.Multiply(BondVector.Perpendicular(), tempvector) * BondVector.Perpendicular();
                        return perp;
                    }
                    else
                    {
                        return BondVector.Perpendicular();
                    }
                }
            }
        }

        public Point? Centroid
        {
            get
            {
                if (PrimaryRing != null)
                {
                    return PrimaryRing.Centroid;
                }
                else
                {
                    return PseudoCentroid;
                }
            }
        }

        /// <summary>
        /// Returns a 'centroid' for a non-cyclic bond
        /// </summary>
        public Point? PseudoCentroid
        {
            get
            {
                var endLigands = EndAtom.NeighboursExcept(StartAtom);
                var startLigands = StartAtom.NeighboursExcept(EndAtom);
                Atom preferredStartLigand = null, preferredEndLigand = null;
                //first, narrow down to atoms on the same side of the bond

                int sign = -(int)Placement;
                double bondangle = 180d;

                foreach (Atom startLigand in startLigands)
                {
                    double angle = sign * Vector.AngleBetween(BondVector, startLigand.Position - StartAtom.Position);
                    if (angle > 0)
                    {
                        var abs = Math.Abs(angle);
                        if (abs < bondangle)
                        {
                            bondangle = abs;
                            preferredStartLigand = startLigand;
                        }
                    }
                }

                bondangle = 180d;
                foreach (Atom endLigand in endLigands)
                {
                    double angle = sign * Vector.AngleBetween(-BondVector, endLigand.Position - EndAtom.Position);
                    if (angle < 0)
                    {
                        var abs = Math.Abs(angle);
                        if (abs < bondangle)
                        {
                            bondangle = abs;
                            preferredEndLigand = endLigand;
                        }
                    }
                }
                //if we have two atoms on the same side as the bond...
                if (preferredStartLigand != null && preferredEndLigand != null)
                {
                    return preferredStartLigand.Position +
                           (preferredEndLigand.Position - preferredStartLigand.Position) / 2;
                }
                //if we have only one atom on the same side of the bond
                else if (preferredStartLigand != null) //preferredEndLigand == null
                {
                    return StartAtom.Position + (preferredStartLigand.Position - StartAtom.Position) + BondVector;
                }
                else if (preferredEndLigand != null) //preferredEndLigand == null
                {
                    return EndAtom.Position + (preferredEndLigand.Position - EndAtom.Position) - BondVector;
                }
                return null;
            }
        }

        private Atom GetCisLigand(Atom startLigand, List<Atom> endLigands, Atom startAtom, Atom endAtom)
        {
            //assume there are two endLigands

            if (BasicGeometry.LineSegmentsIntersect(startLigand.Position, endAtom.Position, endLigands[0].Position,
                                                    startAtom.Position) != null)
            {
                return endLigands[0];
            }
            else if (BasicGeometry.LineSegmentsIntersect(startLigand.Position, endAtom.Position, endLigands[1].Position, startAtom.Position) != null)

            {
                return endLigands[1];
            }
            return null;
        }

        private Globals.BondDirection? GetPlacement()
        {
            Globals.BondDirection dir = Globals.BondDirection.None;

            var vec = GetPrettyDoubleBondVector();

            if (vec == null)
            {
                dir = Globals.BondDirection.None;
            }
            else
            {
                // Azure DevOps #713
                if (double.IsNaN(vec.Value.X) || double.IsNaN(vec.Value.Y))
                {
                    dir = Globals.BondDirection.None;
                }
                else
                {
                    dir = (Globals.BondDirection)Math.Sign(Vector.CrossProduct(vec.Value, BondVector));
                }
            }

            return dir;
        }

        public Globals.BondDirection? ImplicitPlacement => GetPlacement();

        public Vector BondVector => EndAtom.Position - StartAtom.Position;

        public double Angle => Vector.AngleBetween(BasicGeometry.ScreenNorth, BondVector);
        public double BondLength => BondVector.Length;

        #endregion Properties

        #region Constructors

        public Bond()
        {
            Id = Guid.NewGuid().ToString("D");
            InternalId = Id;
            Messages = new List<string>();
        }

        public Bond(Atom startAtom, Atom endAtom) : this()
        {
            StartAtomInternalId = startAtom.InternalId;
            EndAtomInternalId = endAtom.InternalId;
        }

        #endregion Constructors

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged

        #region Methods

        public Atom OtherAtom(Atom a)
        {
            return OtherAtom(a.InternalId);
        }

        private Atom OtherAtom(string aId)
        {
            return Parent?.Atoms[OtherAtomID(aId)];
        }

        private string OtherAtomID(string aId)
        {
            if (aId.Equals(StartAtomInternalId))
            {
                return EndAtomInternalId;
            }
            else if (aId.Equals(EndAtomInternalId))
            {
                return StartAtomInternalId;
            }
            else
            {
                throw new ArgumentException("Atom ID is not part of this Bond.", aId);
            }
        }

        public void UpdateVisual()
        {
            OnPropertyChanged(nameof(Order));
        }

        #region Geometry Routines

        public bool AtomsAreCis(Atom atomA, Atom atomB)
        {
            // Note: Add null checks as this has been found to be blowing up
            if (atomA != null && atomB != null
                              && StartAtom.Neighbours != null && EndAtom.Neighbours != null
                              && StartAtom.Neighbours.Any() && EndAtom.Neighbours.Any())
            {
                if (StartAtom.Neighbours.Contains(atomA))
                {
                    //draw two lines from the end atom to atom a and start atom to atom b and see if they intersect
                    return BasicGeometry.LineSegmentsIntersect(EndAtom.Position, atomA.Position,
                               StartAtom.Position, atomB.Position) != null;
                }

                //draw the lines the other way around
                return BasicGeometry.LineSegmentsIntersect(EndAtom.Position, atomB.Position,
                           StartAtom.Position, atomA.Position) != null;
            }

            return false;
        }

        #endregion Geometry Routines

        #endregion Methods

        #region Overrides

        public override string ToString()
        {
            return $"Bond {Id} - {Path}: From {StartAtom.Path} to {EndAtom.Path}";
        }

        public override int GetHashCode()
        {
            return InternalId.GetHashCode();
        }

        #endregion Overrides

        /// <summary>
        /// Forces a notification event to be sent up the tree
        /// used to force a redraw
        /// </summary>
        public void SendDummyNotif()
        {
            OnPropertyChanged(nameof(Order));
        }

        //gets the bond angle from the perspective of the designated atom
        public double AngleStartingAt(Atom rootAtom)
        {
            if (rootAtom != StartAtom && rootAtom != EndAtom)
            {
                throw new ArgumentException("Atom not part of this bond.");
            }

            if (rootAtom == StartAtom)
            {
                return Angle;
            }
            else
            {
                return (Angle + 180d) % 360d;
            }
        }

        public bool Equals(Bond other)
        {
            if (other is null)
            {
                return false;
            }
            return other.InternalId == this.InternalId;
        }
    }
}