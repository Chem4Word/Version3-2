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
using Chem4Word.Core.Helpers;
using Chem4Word.Model2.Annotations;
using Chem4Word.Model2.Geometry;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.Model2
{
    public class Atom : ChemistryBase, INotifyPropertyChanged, IEquatable<Atom>
    {
        #region Fields

        public List<string> Messages = new List<string>();

        #endregion Fields

        #region Properties

        public CompassPoints FunctionalGroupPlacement
        {
            get
            {
                if (Element is FunctionalGroup fg)
                {
                    if (Bonds.Count() == 1)
                    {
                        var centroid = Parent.Centroid;
                        var vector = Position - centroid;
                        var angle = Vector.AngleBetween(BasicGeometry.ScreenNorth, vector);
                        return angle < 0 ? CompassPoints.West : CompassPoints.East;
                    }
                    else if (Bonds.Count() > 1)
                    {
                        int leftBondCount = 0, rightBondCount = 0;
                        foreach (Atom neighbour in Neighbours)
                        {
                            Vector tempBondVector = neighbour.Position - Position;
                            double angle = Vector.AngleBetween(BasicGeometry.ScreenNorth, tempBondVector);
                            if (angle >= 5.0 && angle <= 175.0)
                            {
                                rightBondCount++;
                            }
                            else
                            {
                                leftBondCount++;
                            }
                        }
                        return rightBondCount > leftBondCount ? CompassPoints.West : CompassPoints.East;
                    }
                }

                return CompassPoints.East;
            }
        }

        public bool? ExplicitC { get; set; }

        private ElementBase _element;

        public ElementBase Element
        {
            get { return _element; }
            set
            {
                _element = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SymbolText));
                OnPropertyChanged(nameof(ImplicitHydrogenCount));
                UpdateVisual();
                if (Bonds.Any())
                {
                    foreach (Bond bond in Bonds)
                    {
                        bond.UpdateVisual();
                    }
                }
            }
        }

        public IEnumerable<Atom> Neighbours
        {
            get { return Parent.GetAtomNeighbours(this); }
        }

        public HashSet<Atom> NeighbourSet => new HashSet<Atom>(Neighbours);

        /// <summary>
        /// Count of rings that this atom is a member of
        /// </summary>
        public int RingCount
        {
            get
            {
                int result = 0;

                var allRings = Parent.Rings;
                foreach (Ring ring in allRings)
                {
                    if (ring.Atoms.Contains(this))
                    {
                        result++;
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// List of rings that this atom belongs to
        /// </summary>
        public IEnumerable<Ring> Rings
        {
            get
            {
                var result = new List<Ring>();

                var allRings = Parent.Rings;
                foreach (Ring ring in allRings)
                {
                    if (ring.Atoms.Contains(this))
                    {
                        result.Add(ring);
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Detect if this atom is a member of any rings
        /// </summary>
        public bool IsInRing
        {
            get
            {
                var result = false;

                var allRings = Parent.Rings;
                foreach (Ring ring in allRings)
                {
                    if (ring.Atoms.Contains(this))
                    {
                        result = true;
                        break;
                    }
                }

                return result;
            }
        }

        public Molecule Parent { get; set; }

        public IEnumerable<Bond> Bonds
        {
            get
            {
                IEnumerable<Bond> bonds = new List<Bond>();

                if (Parent != null)
                {
                    bonds = Parent.GetBonds(InternalId);
                }

                return bonds;
            }
        }

        public int Degree => Bonds.Count();

        private string _id;

        public string Id
        {
            get { return _id; }
            set
            {
                if (_id != value)
                {
                    _id = value;
                }
            }
        }

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

        public Point Position
        {
            get { return _position; }
            set
            {
                _position = value;
                OnPropertyChanged();
                if (Bonds.Any())
                {
                    foreach (Bond bond in Bonds)
                    {
                        bond.UpdateVisual();
                    }
                }
            }
        }

        public bool ShowSymbol
        {
            get
            {
                bool result = true;
                _isAllenic = false;

                if (Element == null)
                {
                    result = false;
                }
                else
                {
                    if (Element is FunctionalGroup)
                    {
                        // Use initialised value of true
                    }
                    else
                    {
                        if (IsotopeNumber != null || (FormalCharge ?? 0) != 0)
                        {
                            // Use initialised value of true
                        }
                        else if (Element.Symbol == Globals.CarbonSymbol)
                        {
                            result = false;

                            if (ExplicitC.HasValue)
                            {
                                result = ExplicitC.Value;
                            }
                            else
                            {
                                if (Degree <= 1)
                                {
                                    result = true;
                                }

                                if (Degree == 2)
                                {
                                    var bonds = Bonds.ToArray();
                                    // This code is triggered when adding the first Atom to a bond
                                    //  at this point one of the atoms is undefined
                                    Atom a1 = bonds[0].OtherAtom(this);
                                    Atom a2 = bonds[1].OtherAtom(this);
                                    if (a1 != null && a2 != null)
                                    {
                                        double angle1 =
                                            Vector.AngleBetween(-(Position - a1.Position),
                                                                Position - a2.Position);
                                        if (Math.Abs(angle1) < 8)
                                        {
                                            if (bonds[0].OrderValue == 2
                                                && bonds[1].OrderValue == 2)
                                            {
                                                _isAllenic = true;
                                            }
                                            else
                                            {
                                                result = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return result;
            }
        }

        private bool _isAllenic;

        //tries to get an estimated bounding box for each atom symbol
        public Rect BoundingBox(double fontSize)
        {
            double halfBoxWidth = fontSize * 0.5;
            Point position = Position;
            Rect baseAtomBox = new Rect(
                new Point(position.X - halfBoxWidth, position.Y - halfBoxWidth),
                new Point(position.X + halfBoxWidth, position.Y + halfBoxWidth));
            if (SymbolText != "")
            {
                double symbolWidth = SymbolText.Length * fontSize;
                Rect mainElementBox = new Rect(
                    new Point(position.X - symbolWidth / 2, position.Y - halfBoxWidth),
                    new Size(symbolWidth, fontSize));

                if (ImplicitHydrogenCount > 0)
                {
                    Vector shift = new Vector();
                    Rect hydrogenBox = baseAtomBox;
                    switch (GetDefaultHOrientation())
                    {
                        case CompassPoints.East:
                            shift = BasicGeometry.ScreenEast * fontSize;
                            break;

                        case CompassPoints.North:
                            shift = BasicGeometry.ScreenNorth * fontSize;
                            break;

                        case CompassPoints.South:
                            shift = BasicGeometry.ScreenSouth * fontSize;
                            break;

                        case CompassPoints.West:
                            shift = BasicGeometry.ScreenWest * fontSize;
                            break;
                    }

                    hydrogenBox.Offset(shift);
                    mainElementBox.Union(hydrogenBox);
                }

                return mainElementBox;
            }
            else
            {
                return baseAtomBox;
            }
        }

        public string SymbolText
        {
            get
            {
                string result = string.Empty;

                if (Element != null)
                {
                    result = Element.Symbol;

                    if (!ShowSymbol)
                    {
                        result = string.Empty;
                    }

                    if (_isAllenic)
                    {
                        result = Globals.AllenicCarbonSymbol;
                    }
                }

                return result;
            }
        }

        public object Tag { get; set; }

        private int? _isotopeNumber;

        public int? IsotopeNumber
        {
            get { return _isotopeNumber; }
            set
            {
                _isotopeNumber = value;
                OnPropertyChanged();
            }
        }

        private int? _spinMultiplicity;

        public int? SpinMultiplicity
        {
            get { return _spinMultiplicity; }
            set
            {
                _spinMultiplicity = value;
                OnPropertyChanged();
            }
        }

        private int? _formalCharge;

        public int? FormalCharge
        {
            get { return _formalCharge; }
            set
            {
                _formalCharge = value;

                OnPropertyChanged();
            }
        }

        private bool _doubletRadical;
        private Point _position;

        public int ImplicitHydrogenCount
        {
            get
            {
                // Return -1 if we don't need to do anything
                int iHydrogenCount = -1;

                if (Element is FunctionalGroup)
                {
                    return iHydrogenCount;
                }

                if (Element != null)
                {
                    // Applies to "B,C,N,O,F,Si,P,S,Cl,As,Se,Br,Te,I,At"
                    string appliesTo = Globals.PeriodicTable.ImplicitHydrogenTargets;

                    if (appliesTo.Contains(Element.Symbol))
                    {
                        int bondCount = (int)Math.Truncate(BondOrders);
                        int charge = FormalCharge ?? 0;
                        int availableElectrons = Globals.PeriodicTable.AvailableElectrons(Element as Element, bondCount, charge);
                        iHydrogenCount = availableElectrons <= 0 ? 0 : availableElectrons;
                    }
                }
                return iHydrogenCount;
            }
        }

        public double BondOrders
        {
            get
            {
                double order = 0d;
                if (Parent != null)
                {
                    foreach (Bond bond in Bonds)
                    {
                        order += bond.OrderValue ?? 0d;
                    }
                }

                return order;
            }
        }

        public bool DoubletRadical
        {
            get { return _doubletRadical; }
            set
            {
                _doubletRadical = value;
                //Attributed call knows who we are, no need to pass "DoubletRadical" as an argument
                OnPropertyChanged();
            }
        }

        public bool IsUnsaturated => Bonds.Any(b => b.OrderValue >= 2);

        //drawing related properties
        public Vector BalancingVector(bool forLabelPlacement = false)
        {
            Vector vsumVector = BasicGeometry.ScreenNorth;

            if (Bonds.Any())
            {
                double sumOfLengths = 0;
                foreach (var bond in Bonds)
                {
                    Vector v = bond.OtherAtom(this).Position - this.Position;

                    if (forLabelPlacement)
                    {
                        // Multiply by bond order to bias away from double or triple bonds
                        double order = bond.OrderValue.Value;
                        if (order > 0.1)
                        {
                            v = v * bond.OrderValue.Value;
                        }
                    }

                    sumOfLengths += v.Length;
                    vsumVector += v;
                }

                // Set tiny amount as 10% of average bond length
                double tinyAmount = sumOfLengths / Bonds.Count() * 0.1;
                double xy = vsumVector.Length;

                // Is resultant vector is big enough for us to use?
                if (xy >= tinyAmount)
                {
                    // Get vector in opposite direction
                    vsumVector = -vsumVector;
                    vsumVector.Normalize();
                }
                else
                {
                    // Get vector of first bond
                    Vector vector = Bonds.First().OtherAtom(this).Position - Position;
                    if (Bonds.Count() == 2)
                    {
                        // Get vector at right angles
                        vsumVector = vector.Perpendicular();
                        vsumVector = -vsumVector;
                    }
                    else
                    {
                        // Get vector in opposite direction
                        vsumVector = -vector;
                    }
                    vsumVector.Normalize();
                }
            }

            //Debug.WriteLine($"Atom {Id} Resultant Balancing Vector Angle is {Vector.AngleBetween(BasicGeometry.ScreenNorth, vsumVector)}");
            return vsumVector;
        }

        public List<Atom> UnprocessedNeighbours(Predicate<Atom> unprocessedTest)
        {
            return Neighbours.Where(a => unprocessedTest(a)).ToList();
        }

        /// <summary>
        /// How many atoms we haven't 'done' yet when we're traversing the graph
        /// </summary>
        public int UnprocessedDegree(Predicate<Atom> unprocessedTest) => UnprocessedNeighbours(unprocessedTest).Count;

        #endregion Properties

        #region Constructors

        public Atom()
        {
            Id = Guid.NewGuid().ToString("D");
            InternalId = Id;
        }

        /// <summary>
        /// The internal ID is what is used to tie atoms and bonds together
        /// </summary>
        public string InternalId { get; }

        public bool Singleton => Parent.Atoms.Count == 1 && Parent.Atoms.Values.First() == this;

        #endregion Constructors

        #region Methods

        public List<Atom> NeighboursExcept(Atom toIgnore)
        {
            return Neighbours.Where(a => a != toIgnore).ToList();
        }

        public List<Atom> NeighboursExcept(params Atom[] toIgnore)
        {
            return Neighbours.Where(a => !toIgnore.Contains(a)).ToList();
        }

        public Bond BondBetween(Atom atom)
        {
            foreach (var parentBond in Parent.Bonds)
            {
                if (parentBond.StartAtomInternalId.Equals(InternalId) && parentBond.EndAtomInternalId.Equals(atom.InternalId))
                {
                    return parentBond;
                }
                if (parentBond.EndAtomInternalId.Equals(InternalId) && parentBond.StartAtomInternalId.Equals(atom.InternalId))
                {
                    return parentBond;
                }
            }
            return null;
        }

        public CompassPoints GetDefaultHOrientation()
        {
            var orientation = CompassPoints.East;

            if (ImplicitHydrogenCount >= 1 && Bonds.Any())
            {
                double angleFromNorth = Vector.AngleBetween(BasicGeometry.ScreenNorth, BalancingVector(true));
                orientation = Bonds.Count() == 1 ? BasicGeometry.SnapTo2EW(angleFromNorth) : BasicGeometry.SnapTo4NESW(angleFromNorth);
            }

            return orientation;
        }

        //notification methods
        public void UpdateVisual()
        {
            OnPropertyChanged(nameof(SymbolText));
        }

        #endregion Methods

        #region Overrides

        public override string ToString()
        {
            var symbol = Element != null ? Element.Symbol : "???";
            return $"Atom {Id} - {Path}: {symbol} @ {PointHelper.AsString(Position)}";
        }

        public override int GetHashCode()
        {
            return InternalId.GetHashCode();
        }

        #endregion Overrides

        #region Events

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged

        #endregion Events

        public void SendDummyNotif()
        {
            OnPropertyChanged(nameof(SymbolText));
        }

        public int UnprocessedDegree(Predicate<Atom> unprocessedTest, HashSet<Bond> excludeBonds)
        {
            var unproc = from a in UnprocessedNeighbours(unprocessedTest)
                         where !excludeBonds.Contains(this.BondBetween(a)) && unprocessedTest(a)
                         select a;
            return unproc.Count();
        }

        public bool Equals(Atom other)
        {
            if (other is null)
            {
                return false;
            }
            return other.InternalId == this.InternalId;
        }

        /// <summary>
        /// indicates whether an atom has exceeded its maximum valence count
        /// </summary>
        public bool Overbonded
        {
            get
            {
                int bondCount = (int)Math.Truncate(BondOrders);
                int charge = FormalCharge ?? 0;
                int availableElectrons = Globals.PeriodicTable.AvailableElectrons(Element as Element, bondCount, charge);
                bool result = availableElectrons < 0;
                return result;
            }
        }
    }
}