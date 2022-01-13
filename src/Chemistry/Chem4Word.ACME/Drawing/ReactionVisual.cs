﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using Chem4Word.ACME.Graphics;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;
using static Chem4Word.Model2.Geometry.BasicGeometry;

namespace Chem4Word.ACME.Drawing
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
            Vector shift = GetBlockOffset(null, aboveArrow, blockBounds, out Vector rxnVector, out Point topLeft, out Vector adjPerp);
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
            Arrow arrow;
            switch (ParentReaction.ReactionType)
            {
                case Globals.ReactionType.Reversible:
                    arrow = new EquilibriumArrow { StartPoint = ParentReaction.TailPoint, EndPoint = ParentReaction.HeadPoint };
                    break;

                case Globals.ReactionType.ReversibleBiasedForward:
                    arrow = new EquilibriumArrow { StartPoint = ParentReaction.TailPoint, EndPoint = ParentReaction.HeadPoint, Bias = Graphics.EquilibriumBias.Forward };
                    break;

                case Globals.ReactionType.ReversibleBiasedReverse:
                    arrow = new EquilibriumArrow { StartPoint = ParentReaction.TailPoint, EndPoint = ParentReaction.HeadPoint, Bias = Graphics.EquilibriumBias.Backward };
                    break;

                case Globals.ReactionType.Blocked:
                    arrow = new BlockedArrow { StartPoint = ParentReaction.TailPoint, EndPoint = ParentReaction.HeadPoint };
                    break;

                case Globals.ReactionType.Resonance:
                    arrow = new StraightArrow { StartPoint = ParentReaction.TailPoint, EndPoint = ParentReaction.HeadPoint, ArrowEnds = Enums.ArrowEnds.Both };
                    break;

                default:
                    arrow = new StraightArrow { StartPoint = ParentReaction.TailPoint, EndPoint = ParentReaction.HeadPoint };
                    break;
            }
            using (DrawingContext dc = RenderOpen())
            {
                arrow.DrawArrowGeometry(dc, new Pen(Brushes.Black, 1), Brushes.Black);

                //now do any text
                if (!(string.IsNullOrEmpty(ParentReaction.ReagentText) || XAMLHelper.IsEmptyDocument(ParentReaction.ReagentText)))
                {
                    ReagentsBlockRect = DrawTextBlock(dc, ParentReaction.ReagentText, ParentReaction.ReagentsBlockOffset);
                }
                else
                {
                    //draw a transparent overlay over the block: this will help hit testing for empty blocks
                    dc.DrawRectangle(Brushes.Transparent, null, GetDefaultBlockRect(true));
                }
                if (!(string.IsNullOrEmpty(ParentReaction.ConditionsText) || XAMLHelper.IsEmptyDocument(ParentReaction.ConditionsText)))
                {
                    ConditionsBlockRect = DrawTextBlock(dc, ParentReaction.ConditionsText, ParentReaction.ConditionsBlockOffset, false);
                }
                else
                {
                    //draw a transparent overlay over the block: this will help hit testing for empty blocks
                    dc.DrawRectangle(Brushes.Transparent, null, GetDefaultBlockRect(false));
                }
                dc.Close();
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
        /// <param name="reagentsBlockOffset">TextOffset specifying the relative polar coordinates of the block</param>
        /// <param name="aboveArrow">true=text goes above arrow, false=text goes below</param>
        private Rect DrawTextBlock(DrawingContext dc, string blockXaml, Reaction.TextOffset? reagentsBlockOffset, bool aboveArrow = true)
        {
            const string blockColour = "#000000";

            //measure the text and work out the offset vector
            Rect blockBounds = DrawText(dc, blockXaml, new Point(0, 0), blockColour, true);

            var blockOffset = GetBlockOffset(reagentsBlockOffset, aboveArrow, blockBounds, out var reactionVector, out var startingPoint, out var adjustedPerp);

            //now, the reaction arrow might clip the text box. If so, we need to nudge the box out until it doesn't

            //calculate the geometry of the adjusted text box
            Rect blockRect = DrawText(dc, blockXaml, startingPoint, blockColour, true);

            //make the nudge factor dependent on the bond offset
            Vector nudge = adjustedPerp;
            nudge.Normalize();
            Model model = ParentReaction.Parent.Parent;
            if (model != null)
            {
                nudge *= model.MeanBondLength * Globals.BondOffsetPercentage;
            }
            else
            {
                nudge = adjustedPerp / 10;
            }
            //check for a clip
            while (RectClips(blockRect, ParentReaction.TailPoint, ParentReaction.HeadPoint))
            {
                adjustedPerp += nudge;
                //recalculate the offset after taking into account the adjustment
                startingPoint = ParentReaction.TailPoint + reactionVector / 2 + adjustedPerp + blockOffset;
                blockRect = DrawText(dc, blockXaml, startingPoint, blockColour, true);
            }
            //Draw the text to the screen for real
            return DrawText(dc, blockXaml, startingPoint, blockColour);

#if SHOWBOUNDS
            Pen offsetPen = new Pen(Brushes.Blue, 1);
            dc.DrawLine(offsetPen, ParentReaction.TailPoint + reactionVector / 2, ParentReaction.TailPoint + reactionVector / 2 + adjustedPerp);
            dc.DrawLine(offsetPen, ParentReaction.TailPoint + reactionVector / 2 + adjustedPerp, ParentReaction.TailPoint + reactionVector / 2 + adjustedPerp + blockOffset);
#endif
        }

        /// <summary>
        /// calculates the offset for a block of text.
        ///can be used to position an empty block
        /// </summary>
        /// <param name="storedOffset">The user specified offset for the block, set by dragging. can be null.</param>
        /// <param name="aboveArrow">Is the block above the arrow</param>
        /// <param name="blockBounds">Supplied block bounds</param>
        /// <param name="reactionVector">What it says</param>
        /// <param name="topLeft">Where the block should start</param>
        /// <param name="adjustedPerp">Perpendicular to reaction arrow, after adjustment</param>
        /// <returns></returns>
        public Vector GetBlockOffset(Reaction.TextOffset? storedOffset, bool aboveArrow, Rect blockBounds,
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

            if (storedOffset is null)
            {
                if (aboveArrow)
                {
                    rotator.Rotate(-perpAngle);
                }
                else
                {
                    rotator.Rotate(perpAngle);
                }
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
            var intersection = GetClippingPoint(blockBounds, blockCentre, rectMidPoint);
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

        //draws text at the top-left specified, going down the page
        //set measureOnly to true to just work out the bounds
        private Rect DrawText(DrawingContext dc, string blockText, Point topLeft, string colour, bool measureOnly = false)
        {
            double totalHeight = 0;

            Point linePosition = topLeft;
            int textStorePos = 0;
            double maxWidth = 0;
            double minLeft = double.MaxValue;
            using (TextFormatter textFormatter = TextFormatter.Create())
            {
                FunctionalGroupTextSource.GenericTextParagraphProperties paraprops = DefaultParaProps(colour);
                BlockTextSource textStore = DefaultTextSourceProps(blockText, colour);
                while (textStorePos <= textStore.Text.Length)
                {
                    using (TextLine textLine = textFormatter.FormatLine(textStore, textStorePos, DefaultBlockWidth(), paraprops, null))
                    {
                        Debug.WriteLine($"Text Line Start = {textLine.Start}");
                        if (!measureOnly)
                        {
                            textLine.Draw(dc, linePosition, InvertAxes.None);
                        }

                        if (textLine.Width > maxWidth)
                        {
                            maxWidth = textLine.Width;
                        }

                        minLeft = Math.Min(minLeft, textLine.Start);
                        textStorePos += textLine.Length;
                        linePosition.Y += textLine.Height;
                        totalHeight += textLine.Height;
                    }
                }
            }

            var rect = new Rect(topLeft + new Vector(minLeft, 0), new Size(maxWidth, totalHeight));
#if SHOWBOUNDS
            if (!measureOnly)
            {
                dc.DrawRectangle(null, new Pen(Brushes.LightGreen, 1), rect);
            }
#endif
            return rect;
        }

        private BlockTextSource DefaultTextSourceProps(string blockText, string colour)
        {
            return new BlockTextSource(blockText, colour)
            {
                BlockTextSize = TextSize,
                BlockScriptSize = ScriptSize
            };
        }

        private FunctionalGroupTextSource.GenericTextParagraphProperties DefaultParaProps(string colour)
        {
            //set up the default paragraph properties
            return new FunctionalGroupTextSource.GenericTextParagraphProperties(
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