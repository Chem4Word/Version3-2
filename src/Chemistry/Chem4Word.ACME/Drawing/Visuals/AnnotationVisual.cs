// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
//  ---------------------------------------------------------------------------

using Chem4Word.ACME.Drawing.Text;
using Chem4Word.Model2;
using System.Windows;
using System.Windows.Media;

namespace Chem4Word.ACME.Drawing.Visuals
{
    public class AnnotationVisual : ChemicalVisual
    {
        public double TextSize { get; set; }
        public double ScriptSize { get; set; }
        public bool IsEditable { get; set; }
        public double Width { get; set; }
        public Annotation ParentAnnotation { get; }
        public BlockTextSource TextSource { get; set; }
        private const string BlockColour = "#000000";

        public AnnotationVisual(Model2.Annotation annotation, bool isEditable = true) : base()
        {
            ParentAnnotation = annotation;
            IsEditable = isEditable;
        }

        private BlockTextSource XamlTextSource(string blockText, string colour)
        {
            return new BlockTextSource(blockText, colour)
            {
                BlockTextSize = TextSize,
                BlockScriptSize = ScriptSize,
            };
        }

        public override void Render()
        {
            RenderXaml(ParentAnnotation.Xaml);
        }

        private void RenderXaml(string xaml)
        {
            TextSource = XamlTextSource(xaml, BlockColour);
            var runProps = new BlockTextRunProperties(BlockColour, TextSize);
            using (DrawingContext dc = RenderOpen())
            {
                var props = new GenericTextParagraphProperties(
                  FlowDirection.LeftToRight,
                  TextAlignment.Left,
                  true,
                  false,
                  runProps,
                  TextWrapping.NoWrap,
                  TextSize,
                  0d);
                TextSupport.DrawText(dc, ParentAnnotation.Position, props, TextSource, TextSource.MaxLineLength, false);
            }
        }
    }
}