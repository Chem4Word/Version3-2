// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2;
using Chem4Word.ACME.Graphics;
using Chem4Word.Model2.Helpers;
using System.Windows.Media;

namespace Chem4Word.ACME.Drawing
{
    public class ReactionVisual : ChemicalVisual
    {
        private Reaction _reaction;

        public Reaction ParentReaction
        {
            get { return _reaction; }
            set
            {
                _reaction = value;
            }
        }

        public ReactionVisual(Reaction reaction)
        {
            ParentReaction = reaction;
        }

        public override void Render()
        {
            Arrow arrow; 
            switch (ParentReaction.ReactionType)
            {
                case Globals.ReactionType.Reversible:
                    arrow = new Graphics.EquilibriumArrow { StartPoint = ParentReaction.TailPoint, EndPoint = ParentReaction.HeadPoint };
                    break;

                case Globals.ReactionType.ReversibleBiasedForward:
                    arrow = new Graphics.EquilibriumArrow { StartPoint = ParentReaction.TailPoint, EndPoint = ParentReaction.HeadPoint, Bias = Graphics.EquilibriumBias.Forward };
                    break;

                case Globals.ReactionType.ReversibleBiasedReverse:
                    arrow = new Graphics.EquilibriumArrow { StartPoint = ParentReaction.TailPoint, EndPoint = ParentReaction.HeadPoint, Bias = Graphics.EquilibriumBias.Backward };
                    break;

                case Globals.ReactionType.Blocked:
                    arrow = new Graphics.BlockedArrow { StartPoint = ParentReaction.TailPoint, EndPoint = ParentReaction.HeadPoint };
                    break;

                default:
                    arrow = new Graphics.StraightArrow { StartPoint = ParentReaction.TailPoint, EndPoint = ParentReaction.HeadPoint };
                    break;
            }
             using (DrawingContext dc = RenderOpen())
            {
                arrow.DrawArrowGeometry(dc, new Pen(Brushes.Black, 1), Brushes.Black);
            }
        }
        
    }
}