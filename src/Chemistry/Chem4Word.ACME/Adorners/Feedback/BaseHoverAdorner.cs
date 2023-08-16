// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Drawing.Visuals;
using Chem4Word.ACME.Utils;
using Chem4Word.Model2.Annotations;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Chem4Word.ACME.Adorners.Feedback
{
    public abstract class BaseHoverAdorner : Adorner
    {
        public SolidColorBrush BracketBrush { get; }
        public Pen BracketPen { get; }
        public ChemicalVisual TargetedVisual { get; }

        protected BaseHoverAdorner([NotNull] UIElement adornedElement, [NotNull] ChemicalVisual targetedVisual) : base(adornedElement)
        {
            BracketBrush = new SolidColorBrush(Utils.Common.HoverAdornerColor);

            BracketPen = new Pen(BracketBrush, Common.HoverAdornerThickness);
            BracketPen.StartLineCap = PenLineCap.Round;
            BracketPen.EndLineCap = PenLineCap.Round;

            TargetedVisual = targetedVisual;

            var myAdornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
            myAdornerLayer.Add(this);
        }
    }
}