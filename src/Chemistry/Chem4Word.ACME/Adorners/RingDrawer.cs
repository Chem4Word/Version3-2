// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Chem4Word.ACME.Drawing;
using Chem4Word.Model2.Geometry;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.ACME.Adorners
{
    public class RingDrawer
    {
        private FixedRingAdorner _fixedRingAdorner;

        public RingDrawer(FixedRingAdorner fixedRingAdorner)
        {
            _fixedRingAdorner = fixedRingAdorner;
        }

        public void DrawNRing(DrawingContext drawingContext, List<NewAtomPlacement> newPlacements)
        {
            StreamGeometry ringGeometry = new StreamGeometry();
            if (_fixedRingAdorner.Unsaturated) //bit complicated as we have unsaturated bonds
            {
                using (var sgc = ringGeometry.Open())
                {
                    int newPlacementsCount = newPlacements.Count;

                    var locations = (from p in newPlacements.ToArray().Reverse()
                                     select p.Position).ToArray();
                    HashSet<NewAtomPlacement> visited = new HashSet<NewAtomPlacement>();
                    Point? centroid = Geometry<Point>.GetCentroid(locations, p => p);

                    var startAt =
                        newPlacementsCount % 2; //place the double bonds in odd membered rings where they should start

                    for (int i = startAt; i < newPlacementsCount + startAt; i++)
                    {
                        int firstIndex = i % newPlacementsCount;
                        var oldAtomPlacement = newPlacements[firstIndex];
                        int secondIndex = (firstIndex + 1) % newPlacementsCount;

                        var newAtomPlacement = newPlacements[secondIndex];

                        if (!visited.Contains(oldAtomPlacement)
                            && !visited.Contains(newAtomPlacement)
                            && !(oldAtomPlacement.ExistingAtom?.IsUnsaturated ?? false)
                            && !(newAtomPlacement.ExistingAtom?.IsUnsaturated ?? false))
                        {
                            List<Point> dummy = new List<Point>();

                            DoubleBondLayout dbd = new DoubleBondLayout
                            {
                                Start = oldAtomPlacement.Position,
                                End = newAtomPlacement.Position,
                                Placement = Globals.BondDirection.Anticlockwise,
                                PrimaryCentroid = centroid
                            };
                            BondGeometry.GetDoubleBondGeometry(dbd, dbd.PrincipleVector.Length, _fixedRingAdorner.CurrentEditor.ViewModel.Standoff);
                            BasicGeometry.DrawGeometry(sgc, dbd.DefiningGeometry);
                            visited.Add(oldAtomPlacement);
                            visited.Add(newAtomPlacement);
                        }
                        else
                        {
                            BondLayout sbd = new BondLayout
                            {
                                Start = oldAtomPlacement.Position,
                                End = newAtomPlacement.Position
                            };
                            BondGeometry.GetSingleBondGeometry(sbd, _fixedRingAdorner.CurrentEditor.ViewModel.Standoff);
                            BasicGeometry.DrawGeometry(sgc, sbd.DefiningGeometry);
                        }

                        oldAtomPlacement = newAtomPlacement;
                    }

                    sgc.Close();
                }

                drawingContext.DrawGeometry(null, _fixedRingAdorner.BondPen, ringGeometry);
            }
            else //saturated ring, just draw a polygon
            {
                drawingContext.DrawGeometry(null, _fixedRingAdorner.BondPen, BasicGeometry.BuildPolyPath(_fixedRingAdorner.Placements, true));
            }
        }
    }
}