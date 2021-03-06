﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Chem4Word.ACME.Drawing;
using Chem4Word.Model2.Annotations;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.ACME.Adorners.Feedback
{
    public abstract class BaseHoverAdorner : Adorner
    {
        public SolidColorBrush BracketBrush { get; }
        public Pen BracketPen { get; }
        public ChemicalVisual TargetedVisual { get; }

        protected BaseHoverAdorner([NotNull] UIElement adornedElement, [NotNull] ChemicalVisual targetedVisual) : base(adornedElement)
        {
            BracketBrush = new SolidColorBrush(Globals.HoverAdornerColor);

            BracketPen = new Pen(BracketBrush, Globals.HoverAdornerThickness);
            BracketPen.StartLineCap = PenLineCap.Round;
            BracketPen.EndLineCap = PenLineCap.Round;

            TargetedVisual = targetedVisual;

            var myAdornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
            myAdornerLayer.Add(this);
        }
    }
}