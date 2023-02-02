// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
//  ---------------------------------------------------------------------------

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace Chem4Word.ACME.Drawing.Text
{
    public static class TextSupport
    {
        //draws text at the top-left specified, going down the page
        //set measureOnly to true to just work out the bounds
        public static Rect DrawText(DrawingContext dc, Point topLeft,
                              GenericTextParagraphProperties paraProps,
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
                dc.DrawRectangle(null, new Pen(new SolidColorBrush(Colors.LightGreen) {Opacity = 0.4}, 1), rect);
            }
#endif
            return rect;
        }
    }
}