// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Media.TextFormatting;

namespace Chem4Word.ACME.Drawing
{
    partial class FunctionalGroupTextSource
    {
        public class GenericTextParagraphProperties : TextParagraphProperties
        {
            public GenericTextParagraphProperties(
                FlowDirection flowDirection,
                TextAlignment textAlignment,
                bool firstLineInParagraph,
                bool alwaysCollapsible,
                TextRunProperties defaultTextRunProperties,
                TextWrapping textWrap,
                double lineHeight,
                double indent)
            {
                _flowDirection = flowDirection;
                _textAlignment = textAlignment;
                _firstLineInParagraph = firstLineInParagraph;
                _alwaysCollapsible = alwaysCollapsible;
                _defaultTextRunProperties = defaultTextRunProperties;
                _textWrap = textWrap;
                _lineHeight = lineHeight;
                _indent = indent;
            }

            public override FlowDirection FlowDirection
            {
                get { return _flowDirection; }
            }

            public override TextAlignment TextAlignment
            {
                get { return _textAlignment; }
            }

            public override bool FirstLineInParagraph
            {
                get { return _firstLineInParagraph; }
            }

            public override bool AlwaysCollapsible
            {
                get { return _alwaysCollapsible; }
            }

            public override TextRunProperties DefaultTextRunProperties
            {
                get { return _defaultTextRunProperties; }
            }

            public override TextWrapping TextWrapping
            {
                get { return _textWrap; }
            }

            public override double LineHeight
            {
                get { return _lineHeight; }
            }

            public override double Indent
            {
                get { return _indent; }
            }

            public override TextMarkerProperties TextMarkerProperties
            {
                get { return null; }
            }

            private FlowDirection _flowDirection;
            private TextAlignment _textAlignment;
            private bool _firstLineInParagraph;
            private bool _alwaysCollapsible;
            private TextRunProperties _defaultTextRunProperties;
            private TextWrapping _textWrap;
            private double _indent;

            private double _lineHeight;
        }
    }
}