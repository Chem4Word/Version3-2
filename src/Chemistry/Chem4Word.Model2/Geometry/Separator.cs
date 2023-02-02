// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Linq;
using System.Windows;

namespace Chem4Word.Model2.Geometry
{
    public class Separator
    {
        private Model _model = null;

        public Separator(Model model)
        {
            _model = model;
        }

        public bool Separate(double padding, int maxLoops, out int loops)
        {
            loops = 0;

            Rect a;
            Rect b;
            Molecule mola;
            Molecule molb;

            double dx;
            double dxa;
            double dxb;

            double dy;
            double dya;
            double dyb;

            bool touching = false;

            do
            {
                loops++;
                touching = false;
                for (int i = 0; i < _model.Molecules.Count; i++)
                {
                    mola = _model.Molecules.Values.Skip(i).First();
                    a = ExpandBoundingBox(mola.BoundingBox, padding / 2);
                    for (int j = i + 1; j < _model.Molecules.Count; j++)
                    {
                        molb = _model.Molecules.Values.Skip(j).First();
                        b = ExpandBoundingBox(molb.BoundingBox, padding / 2);
                        if (a.IntersectsWith(b))
                        {
                            touching = true;

                            // find the two smallest deltas required to stop the overlap
                            dx = Math.Min(a.Right - b.Left + padding, a.Left - b.Right - padding);
                            dy = Math.Min(a.Bottom - b.Top + padding, a.Top - b.Bottom - padding);

                            if (j % 2 == 0)
                            {
                                // only keep the smallest delta
                                if (Math.Abs(dx) < Math.Abs(dy))
                                {
                                    dy = 0;
                                }
                                else
                                {
                                    dx = 0;
                                }
                            }
                            else
                            {
                                // only keep the smallest delta
                                if (Math.Abs(dy) < Math.Abs(dx))
                                {
                                    dx = 0;
                                }
                                else
                                {
                                    dy = 0;
                                }
                            }

                            // create a delta for each rectangle as half the whole delta.
                            dxa = -dx / 2;
                            dxb = dx + dxa;
                            dya = -dy / 2;
                            dyb = dy + dya;

                            // shift rectangles
                            if (j % 2 == 0)
                            {
                                molb.MoveAllAtoms(dxb, dyb);
                            }
                            else
                            {
                                mola.MoveAllAtoms(dxb, dyb);
                            }
                        }
                    }
                }
            } while (loops < maxLoops && touching);

            return loops < maxLoops;

            // Local function
            Rect ExpandBoundingBox(Rect rect, double margin)
            {
                Point topLeft = new Point(rect.TopLeft.X - margin, rect.TopLeft.Y - margin);
                Point bottomRight = new Point(rect.BottomRight.X + margin, rect.BottomRight.Y + margin);
                return new Rect(topLeft, bottomRight);
            }
        }
    }
}