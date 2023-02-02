// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Drawing;
using Chem4Word.ACME.Drawing.LayoutSupport;
using Chem4Word.ACME.Utils;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using Chem4Word.Model2.Annotations;
using Chem4Word.Model2.Enums;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.ACME.Adorners.Sketching
{
    public class DrawBondAdorner : Adorner
    {
        private SolidColorBrush _solidColorBrush;

        private Pen _dashPen;

        private EditorCanvas CurrentEditor { get; }
        public BondStereo Stereo { get; set; }

        public string BondOrder { get; set; }

        public Point StartPoint
        {
            get { return (Point)GetValue(StartPointProperty); }
            set { SetValue(StartPointProperty, value); }
        }

        public static readonly DependencyProperty StartPointProperty =
            DependencyProperty.Register("StartPoint", typeof(Point), typeof(DrawBondAdorner),
                                        new FrameworkPropertyMetadata(new Point(0d, 0d),
                                                                      FrameworkPropertyMetadataOptions.AffectsRender));

        public Point EndPoint
        {
            get { return (Point)GetValue(EndPointProperty); }
            set { SetValue(EndPointProperty, value); }
        }

        public Bond ExistingBond { get; set; }

        public static readonly DependencyProperty EndPointProperty =
            DependencyProperty.Register("EndPoint", typeof(Point), typeof(DrawBondAdorner),
                                        new FrameworkPropertyMetadata(new Point(0d, 0d),
                                                                      FrameworkPropertyMetadataOptions.AffectsRender));

        public DrawBondAdorner([NotNull] UIElement adornedElement, double bondThickness) : base(adornedElement)
        {
            _solidColorBrush = (SolidColorBrush)FindResource(Common.DrawAdornerBrush);
            _dashPen = new Pen(_solidColorBrush, bondThickness);

            CurrentEditor = (EditorCanvas)adornedElement;

            PreviewKeyDown += DrawBondAdorner_PreviewKeyDown;

            var myAdornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
            myAdornerLayer.Add(this);

            Focusable = true;
            Focus();
        }

        private void DrawBondAdorner_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            Brush brush = _solidColorBrush;
            Pen pen = _dashPen;
            BondLayout layout;

            var length = CurrentEditor.Controller.Model.XamlBondLength;
            if (ExistingBond == null || !ExistingBond.IsCyclic())
            {
                layout = GetBondLayout(StartPoint, EndPoint, length, Stereo, BondOrder);
            }
            else
            {
                layout = GetBondLayout(StartPoint, EndPoint, length, Stereo, BondOrder, ExistingBond.PrimaryRing,
                                       ExistingBond.SubsidiaryRing);
            }

            if (Stereo == BondStereo.Hatch)
            {
                brush = new LinearGradientBrush
                {
                    MappingMode = BrushMappingMode.Absolute,
                    SpreadMethod = GradientSpreadMethod.Repeat,
                    StartPoint = new Point(50, 0),
                    EndPoint = new Point(50, 3),
                    GradientStops = new GradientStopCollection()
                                            {
                                                new GradientStop {Offset = 0d, Color = _solidColorBrush.Color},
                                                new GradientStop {Offset = 0.25d, Color = _solidColorBrush.Color},
                                                new GradientStop {Offset = 0.25d, Color = Colors.Transparent},
                                                new GradientStop {Offset = 0.30, Color = Colors.Transparent}
                                            },

                    Transform = new RotateTransform
                    {
                        Angle = Vector.AngleBetween(GeometryTool.ScreenNorth,
                                                                        EndPoint - StartPoint)
                    }
                };
            }

            switch (BondOrder)
            {
                case Globals.OrderZero:
                case Globals.OrderOther:
                case "unknown":

                    pen = pen.Clone();
                    pen.DashStyle = DashStyles.Dot;
                    drawingContext.DrawGeometry(brush, pen, layout.DefiningGeometry);
                    break;

                case Globals.OrderPartial01:
                    pen = pen.Clone();
                    pen.DashStyle = DashStyles.Dash;
                    drawingContext.DrawGeometry(brush, pen, layout.DefiningGeometry);
                    break;

                case Globals.OrderAromatic:
                case Globals.OrderPartial12:
                    var secondPen = pen.Clone();
                    secondPen.DashStyle = DashStyles.Dash;
                    drawingContext.DrawLine(pen, layout.Start, layout.End);
                    var doubleBondDescriptor = layout as DoubleBondLayout;
                    drawingContext.DrawLine(secondPen,
                                            doubleBondDescriptor.SecondaryStart,
                                            doubleBondDescriptor.SecondaryEnd);
                    break;

                case Globals.OrderPartial23:
                    var tbd = layout as TripleBondLayout;
                    secondPen = pen.Clone();
                    secondPen.DashStyle = DashStyles.Dash;
                    drawingContext.DrawLine(pen, tbd.SecondaryStart, tbd.SecondaryEnd);
                    drawingContext.DrawLine(pen, tbd.Start, tbd.End);
                    drawingContext.DrawLine(secondPen, tbd.TertiaryStart, tbd.TertiaryEnd);
                    break;

                case Globals.OrderTriple:
                    tbd = layout as TripleBondLayout;
                    drawingContext.DrawLine(pen, tbd.SecondaryStart, tbd.SecondaryEnd);
                    drawingContext.DrawLine(pen, tbd.Start, tbd.End);
                    drawingContext.DrawLine(pen, tbd.TertiaryStart, tbd.TertiaryEnd);
                    break;

                default:
                    drawingContext.DrawGeometry(brush, pen, layout.DefiningGeometry);
                    break;
            }
        }

        public BondLayout GetBondLayout(Point startPoint, Point endPoint, double bondLength,
                                         BondStereo stereo, string order, Ring existingRing = null,
                                         Ring subsidiaryRing = null)
        {
            BondLayout descriptor = null;
            //check to see if it's a wedge or a hatch yet
            if (stereo == BondStereo.Wedge || stereo == BondStereo.Hatch)
            {
                var wbd = new WedgeBondLayout { Start = startPoint, End = endPoint };
                BondGeometry.GetWedgeBondGeometry(wbd, bondLength, CurrentEditor.Controller.Standoff);
                return wbd;
            }

            if (stereo == BondStereo.Indeterminate && order == Globals.OrderSingle)
            {
                descriptor = new BondLayout { Start = startPoint, End = endPoint };
                BondGeometry.GetWavyBondGeometry(descriptor, bondLength, CurrentEditor.Controller.Standoff);
                return descriptor;
            }

            var orderValue = Bond.OrderToOrderValue(order);
            //single or dotted bond
            if (orderValue <= 1)
            {
                descriptor = new BondLayout { Start = startPoint, End = endPoint };
                BondGeometry.GetSingleBondGeometry(descriptor, CurrentEditor.Controller.Standoff);
            }

            //double bond
            if (orderValue == 2 || orderValue == 1.5)
            {
                DoubleBondLayout dbd = new DoubleBondLayout { Start = startPoint, End = endPoint };
                if (stereo == BondStereo.Indeterminate)
                {
                    BondGeometry.GetCrossedDoubleGeometry(dbd, bondLength, CurrentEditor.Controller.Standoff);
                }
                else
                {
                    dbd.PrimaryCentroid = existingRing?.Centroid;
                    dbd.SecondaryCentroid = subsidiaryRing?.Centroid;
                    BondGeometry.GetDoubleBondGeometry(dbd, bondLength, CurrentEditor.Controller.Standoff);
                }

                descriptor = dbd;
            }

            //tripe bond
            if (orderValue == 2.5 || orderValue == 3)
            {
                var tbd = new TripleBondLayout { Start = startPoint, End = endPoint };
                BondGeometry.GetTripleBondGeometry(tbd, bondLength, CurrentEditor.Controller.Standoff);
                descriptor = tbd;
            }

            return descriptor;
        }
    }
}