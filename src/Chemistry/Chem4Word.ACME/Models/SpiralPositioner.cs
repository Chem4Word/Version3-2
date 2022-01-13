// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Windows;
using Chem4Word.ACME.Interfaces;

namespace Chem4Word.ACME.Models
{
    public class SpiralPositioner : IPositioner
    {
        private const double Chord = 10;
        private const double Rotation = Math.PI / 30;
        private readonly double _awayStep;
        private readonly double _deltaMax;

        private double _delta = 1;
        private Point _startPoint;

        public SpiralPositioner(Size canvasSize)
        {
            var coils = Math.Max(canvasSize.Width / 2, canvasSize.Height / 2) / Chord;
            _deltaMax = coils * 2 * Math.PI;
            _awayStep = Math.Max(canvasSize.Width / 2, canvasSize.Height / 2) / _deltaMax;
        }

        public double StartX
        {
            get => _startPoint.X;
            set => _startPoint.X = value;
        }

        public double StartY
        {
            get => _startPoint.Y;
            set => _startPoint.Y = value;
        }

        // ReSharper disable once ConvertToAutoPropertyWhenPossible
        public double Delta
        {
            get => _delta;
            set => _delta = value;
        }

        public bool GetNextPoint(out double x, out double y)
        {
            var away = _awayStep * _delta;

            x = _startPoint.X + Math.Cos(_delta + Rotation) * away;
            y = _startPoint.Y + Math.Sin(_delta + Rotation) * away;

            _delta += Chord / away;

            return _delta <= _deltaMax;
        }
    }
}