﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
//  ---------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace Chem4Word.ACME.Drawing.Text
{
    public static class TextSupport
    {
           //draws text at the top-left specified, going down the page
        //set measureOnly to true to just work out the bounds
        public static Rect DrawText(DrawingContext dc, string blockText, Point topLeft, string colour,
                              FunctionalGroupTextSource.GenericTextParagraphProperties paraProps,
                              BlockTextSource defaultTextSourceProps, double defaultBlockWidth,
                              bool measureOnly = false)
        {
            double totalHeight = 0;

            Point linePosition = topLeft;
            int textStorePos = 0;
            double maxWidth = 0;
            double minLeft = double.MaxValue;
            using (TextFormatter textFormatter = TextFormatter.Create())
            {
                BlockTextSource textStore = defaultTextSourceProps;
                while (textStorePos <= textStore.Text.Length)
                {
                    using (TextLine textLine = textFormatter.FormatLine(textStore, textStorePos, defaultBlockWidth, paraProps, null))
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

        public static FunctionalGroupTextSource.GenericTextParagraphProperties DefaultParaProps(string colour, BlockTextRunProperties textRunProps, double textSize)
        {
            //set up the default paragraph properties
            return new FunctionalGroupTextSource.GenericTextParagraphProperties(
                FlowDirection.LeftToRight,
                TextAlignment.Center,
                true,
                false,
                textRunProps,
                TextWrapping.NoWrap,
                textSize,
                0d);
        }
    }
}