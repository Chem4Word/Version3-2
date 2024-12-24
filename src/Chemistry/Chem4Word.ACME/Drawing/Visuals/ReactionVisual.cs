// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Drawing.Text;
using Chem4Word.ACME.Graphics;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;
using System.Windows;
using System.Windows.Media;

namespace Chem4Word.ACME.Drawing.Visuals
{
    public class ReactionVisual : ChemicalVisual
    {
        private const double BlockMargin = 10;
        private Reaction _reaction;
        private Rect? _conditionsBlockRect;
        private Rect? _reagentsBlockRect;

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
            _conditionsBlockRect = null;
            _reagentsBlockRect = null;
        }

        public double TextSize { get; set; }
        public double ScriptSize { get; set; }

        public Rect ConditionsBlockRect
        {
            get
            {
                if (_conditionsBlockRect is null || _conditionsBlockRect == Rect.Empty)
                {
                    return GetDefaultBlockRect(false);
                }
                else
                {
                    return _conditionsBlockRect.Value;
                }
            }
            private set { _conditionsBlockRect = value; }
        }

        private Rect GetDefaultBlockRect(bool aboveArrow)
        {
            //define a default Rect half the Reaction width and one third the height
            double reactionVectorLength = ParentReaction.ReactionVector.Length;
            Size defaultSize = new Size(reactionVectorLength / 2,
                                        reactionVectorLength / 3);
            Rect blockBounds = new Rect(new Point(0, 0), defaultSize);
            Vector shift = GetBlockOffset(aboveArrow, blockBounds, out Vector rxnVector, out Point topLeft, out Vector adjPerp);
            var startingPoint = ParentReaction.TailPoint + rxnVector / 2 + adjPerp + shift;
            blockBounds = new Rect(startingPoint, defaultSize);
            return blockBounds;
        }

        public Rect ReagentsBlockRect
        {
            get
            {
                if (_reagentsBlockRect is null || _reagentsBlockRect == Rect.Empty)
                {
                    return GetDefaultBlockRect(true);
                }
                else
                {
                    return _reagentsBlockRect.Value;
                }
            }
            private set { _reagentsBlockRect = value; }
        }

        public override void Render()
        {
            using (DrawingContext dc = RenderOpen())
            {
                RenderFullGeometry(ParentReaction.ReactionType, ParentReaction.TailPoint, ParentReaction.HeadPoint, dc, ParentReaction.ReagentText, ParentReaction.ConditionsText, new Pen(Brushes.Black, 1), Brushes.Black);
                dc.Close();
            }
        }

        public void RenderFullGeometry(ReactionType reactionType, Point tailPoint, Point headPoint, DrawingContext dc, string reagentText, string conditionsText, Pen outlinePen, Brush fill)
        {
            Arrow arrow;
            switch (reactionType)
            {
                case ReactionType.Reversible:
                    arrow = new EquilibriumArrow { StartPoint = tailPoint, EndPoint = headPoint };
                    break;

                case ReactionType.ReversibleBiasedForward:
                    arrow = new EquilibriumArrow { StartPoint = tailPoint, EndPoint = headPoint, Bias = EquilibriumBias.Forward };
                    break;

                case ReactionType.ReversibleBiasedReverse:
                    arrow = new EquilibriumArrow { StartPoint = tailPoint, EndPoint = headPoint, Bias = EquilibriumBias.Backward };
                    break;

                case ReactionType.Blocked:
                    arrow = new BlockedArrow { StartPoint = tailPoint, EndPoint = headPoint };
                    break;

                case ReactionType.Resonance:
                    arrow = new StraightArrow { StartPoint = tailPoint, EndPoint = headPoint, ArrowEnds = Enums.ArrowEnds.Both };
                    break;

                case ReactionType.Retrosynthetic:
                    arrow = new RetrosyntheticArrow { StartPoint = tailPoint, EndPoint = headPoint };
                    break;

                default:
                    arrow = new StraightArrow { StartPoint = tailPoint, EndPoint = headPoint };
                    break;
            }

            arrow.DrawArrowGeometry(dc, outlinePen, fill);

            //now do any text
            string blockColour = fill.ToString();
            if (!(string.IsNullOrEmpty(reagentText) || XAMLHelper.IsEmptyDocument(reagentText)))
            {
                ReagentsBlockRect = DrawTextBlock(dc, reagentText, blockColour);
            }
            else
            {
                //draw a transparent overlay over the block: this will help hit testing for empty blocks
                dc.DrawRectangle(Brushes.Transparent, null, GetDefaultBlockRect(true));
            }
            if (!(string.IsNullOrEmpty(conditionsText) || XAMLHelper.IsEmptyDocument(conditionsText)))
            {
                ConditionsBlockRect = DrawTextBlock(dc, conditionsText, blockColour, false);
            }
            else
            {
                //draw a transparent overlay over the block: this will help hit testing for empty blocks
                dc.DrawRectangle(Brushes.Transparent, null, GetDefaultBlockRect(false));
            }
        }

        private double DefaultBlockWidth()
        {
            return ParentReaction.ReactionVector.Length;
        }

        /// <summary>
        /// Draws the formatted text associated with a reaction located on the offset
        /// </summary>
        /// <param name="dc">DrawingContext obtained from hosting ReactionVisual</param>
        /// <param name="blockXaml">String containing a FlowDocument specification of the text</param>
        /// <param name="blockColour"></param>
        /// <param name="aboveArrow">true=text goes above arrow, false=text goes below</param>
        private Rect DrawTextBlock(DrawingContext dc, string blockXaml, string blockColour, bool aboveArrow = true)
        {
            //measure the text and work out the offset vector
            Rect blockBounds = TextSupport.DrawText(dc, new Point(0, 0), DefaultParaProps(blockColour), DefaultTextSource(blockXaml, blockColour), DefaultBlockWidth(), true);

            var blockOffset = GetBlockOffset(aboveArrow, blockBounds, out var reactionVector, out var startingPoint, out var adjustedPerp);

            //now, the reaction arrow might clip the text box. If so, we need to nudge the box out until it doesn't

            //calculate the geometry of the adjusted text box
            Rect blockRect = TextSupport.DrawText(dc, startingPoint, DefaultParaProps(blockColour), DefaultTextSource(blockXaml, blockColour), DefaultBlockWidth(), true);

            //make the nudge factor dependent on the bond offset
            Vector nudge = adjustedPerp;
            nudge.Normalize();
            Model model = ParentReaction?.Parent?.Parent;
            if (model != null)
            {
                nudge *= model.MeanBondLength * Globals.BondOffsetPercentage;
            }
            else
            {
                nudge = adjustedPerp / 10;
            }
            //check for a clip
            while (GeometryTool.RectClips(blockRect, ParentReaction.TailPoint, ParentReaction.HeadPoint))
            {
                adjustedPerp += nudge;
                //recalculate the offset after taking into account the adjustment
                startingPoint = ParentReaction.TailPoint + reactionVector / 2 + adjustedPerp + blockOffset;
                blockRect = TextSupport.DrawText(dc, startingPoint, DefaultParaProps(blockColour),
                                     DefaultTextSource(blockXaml, blockColour), DefaultBlockWidth(), true);
            }
            //Draw the text to the screen for real
#if SHOWBOUNDS
            Pen offsetPen = new Pen(new SolidColorBrush(Colors.LightGreen) {Opacity = 0.4}, 1);
            dc.DrawLine(offsetPen, ParentReaction.TailPoint + reactionVector / 2, ParentReaction.TailPoint + reactionVector / 2 + adjustedPerp);
            dc.DrawLine(offsetPen, ParentReaction.TailPoint + reactionVector / 2 + adjustedPerp, ParentReaction.TailPoint + reactionVector / 2 + adjustedPerp + blockOffset);
#endif
            return TextSupport.DrawText(dc, startingPoint, DefaultParaProps(blockColour), DefaultTextSource(blockXaml, blockColour), DefaultBlockWidth());
        }

        /// <summary>
        /// calculates the offset for a block of text.
        ///can be used to position an empty block
        /// </summary>
        /// <param name="aboveArrow">Is the block above the arrow</param>
        /// <param name="blockBounds">Supplied block bounds</param>
        /// <param name="reactionVector">What it says</param>
        /// <param name="topLeft">Where the block should start</param>
        /// <param name="adjustedPerp">Perpendicular to reaction arrow, after adjustment</param>
        /// <returns></returns>
        public Vector GetBlockOffset(bool aboveArrow, Rect blockBounds,
                                      out Vector reactionVector, out Point topLeft, out Vector adjustedPerp)
        {
            //work out the initial offset assuming the block is centre
            Vector blockOffset = -(blockBounds.BottomRight - blockBounds.TopLeft) / 2;
            //now pull the block to the left
            blockOffset -= new Vector(blockBounds.Left, 0);
            //get the reaction vector
            reactionVector = ParentReaction.ReactionVector;
            //find the perpendicular and normalise it
            Matrix rotator = new Matrix();
            //above or below the arrow
            bool arrowBackwards = ParentReaction.TailPoint.X > ParentReaction.HeadPoint.X;
            double perpAngle = 90;
            if (arrowBackwards)
            {
                perpAngle = -perpAngle;
            }

            if (aboveArrow)
            {
                rotator.Rotate(-perpAngle);
            }
            else
            {
                rotator.Rotate(perpAngle);
            }

            Vector perp = reactionVector;
            perp.Normalize();

            //multiply the perpendicular by the max of half the height or width of the text block
            perp *= (blockBounds.Width + blockBounds.Height) / 4;
            perp *= rotator;

            //and add in the block offset to get the top left staring point for the text block
            topLeft = ParentReaction.TailPoint + reactionVector / 2 + perp + blockOffset;

            //now we adjust the starting point to pad appropriately
            Rect tempbounds = blockBounds;
            tempbounds.Offset(topLeft.X, topLeft.Y);
            adjustedPerp = CalcProperOffset(tempbounds, ParentReaction.MidPoint);

            //recalculate the offset after taking into account the adjustment
            topLeft = ParentReaction.TailPoint + reactionVector / 2 + adjustedPerp + blockOffset;
            return blockOffset;
        }

        /// <summary>
        /// works out the best spacing between a text block and the parent reaction visual
        /// </summary>
        /// <param name="blockBounds">Rect describing the bounds of the Text Block</param>
        /// <param name="blockCentre">Exact centre of the Text Block</param>
        /// <returns></returns>
        private Vector CalcProperOffset(Rect blockBounds, Point blockCentre)
        {
            //first work out the line between the midpoint of the reaction and the rectangle
            Point rectMidPoint = new Point(blockBounds.Left + blockBounds.Width / 2, blockBounds.Top + blockBounds.Height / 2);

            //iterate through the 4 sides of the rectangle until we get a crossing point
            var intersection = GeometryTool.GetClippingPoint(blockBounds, blockCentre, rectMidPoint);
            if (intersection is null)
            {
                //Houston we have a problem
                //the only realistic circumstance under which this would happen is
                //if the reaction midpoint lay INSIDE the text box

                //so increase the distance between the reaction mid point and the text block vastly, and then recalculate the intersection
                return CalcProperOffset(blockBounds, rectMidPoint + (blockCentre - rectMidPoint) * 5);
            }
            //work out the vector from centre to crossing point
            Vector radius = rectMidPoint - intersection.Value;
            //work out the relative lengths including the padding
            Vector padding = radius;
            padding.Normalize();
            padding *= BlockMargin;
            return radius + padding;
        }

        public BlockTextSource DefaultTextSource(string blockText, string colour)
        {
            return new BlockTextSource(blockText, colour)
            {
                BlockTextSize = TextSize,
                BlockScriptSize = ScriptSize
            };
        }

        private GenericTextParagraphProperties DefaultParaProps(string colour)
        {
            //set up the default paragraph properties
            return new GenericTextParagraphProperties(
                FlowDirection.LeftToRight,
                TextAlignment.Center,
                true,
                false,
                new BlockTextRunProperties(colour, TextSize),
                TextWrapping.NoWrap,
                TextSize,
                0d);
        }
    }
}