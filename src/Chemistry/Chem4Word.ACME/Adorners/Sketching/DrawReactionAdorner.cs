// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Chem4Word.ACME.Annotations;
using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Utils;
using Chem4Word.Model2;

namespace Chem4Word.ACME.Adorners.Sketching
{
    public class DrawReactionAdorner : Adorner
    {
        public Point StartPoint { get; set; }
        public Point EndPoint { get; set; }
        public ReactionType ReactionType { get; set; }
        protected bool Resizing;
        protected bool Dragging;
        private SolidColorBrush _solidColorBrush;
        private Pen _dashPen;
        private EditorCanvas CurrentEditor { get; }

        public DrawReactionAdorner([NotNull] UIElement adornedElement) : base(adornedElement)
        {
            _solidColorBrush = (SolidColorBrush)FindResource(Common.DrawAdornerBrush);
            _dashPen = new Pen(_solidColorBrush, 1);

            CurrentEditor = (EditorCanvas)adornedElement;
            Cursor = CursorUtils.Pencil;

            PreviewKeyDown += DrawBondAdorner_PreviewKeyDown;
            MouseLeftButtonUp += DrawReactionAdorner_MouseLeftButtonUp;

            var myAdornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
            myAdornerLayer.Add(this);

            Focusable = true;
            Focus();
        }

        private void DrawReactionAdorner_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        private void DrawBondAdorner_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            Graphics.Arrow arrowVisual;
            base.OnRender(drawingContext);

            switch (ReactionType)
            {
                case ReactionType.Reversible:
                    arrowVisual = new Graphics.EquilibriumArrow { StartPoint = StartPoint, EndPoint = EndPoint };
                    break;

                case ReactionType.ReversibleBiasedForward:
                    arrowVisual = new Graphics.EquilibriumArrow { StartPoint = StartPoint, EndPoint = EndPoint, Bias = Graphics.EquilibriumBias.Forward };
                    break;

                case ReactionType.ReversibleBiasedReverse:
                    arrowVisual = new Graphics.EquilibriumArrow { StartPoint = StartPoint, EndPoint = EndPoint, Bias = Graphics.EquilibriumBias.Backward };
                    break;

                case ReactionType.Blocked:
                    arrowVisual = new Graphics.BlockedArrow { StartPoint = StartPoint, EndPoint = EndPoint };
                    break;

                case ReactionType.Resonance:
                    arrowVisual = new Graphics.StraightArrow { StartPoint = StartPoint, EndPoint = EndPoint, ArrowEnds = Enums.ArrowEnds.Both };
                    break;

                case ReactionType.Retrosynthetic:
                    arrowVisual = new Graphics.RetrosyntheticArrow { StartPoint = StartPoint, EndPoint = EndPoint };
                    break;

                default:
                    arrowVisual = new Graphics.StraightArrow { StartPoint = StartPoint, EndPoint = EndPoint };
                    break;
            }
            arrowVisual.DrawArrowGeometry(drawingContext, _dashPen, _solidColorBrush);
        }
    }
}