// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Chem4Word.ACME.Interfaces;

namespace Chem4Word.ACME.Models
{
    public class CloudSpace
    {
        private const int RepositionAttempts = 4;
        private const int Pbgra32Bytes = 4;
        private const int CharacterMargin = 1;

        private StartPosition _defaultStartingPosition = StartPosition.Center;
        private BitArray _collisionMap;

        private readonly PixelFormat _pixelFormat = PixelFormats.Pbgra32;
        private readonly IRandomizer _randomizer;
        private readonly IPositioner _positioner;
        private readonly Pen _pen;

        private int _collisionMapWidth;
        private int _collisionMapHeight;

        public double Width { get; }
        public double Height { get; }

        private Point _cloudCenter;
        public ICollection<WordDrawing> FailedPlacements = new List<WordDrawing>();

        public CloudSpace(double width, double height)
        {
            Width = width;
            Height = height;
            _collisionMapWidth = (int)Width;
            _collisionMapHeight = (int)Height;
            _cloudCenter = new Point(width / 2, height / 2);

            _pen = new Pen(Brushes.Purple, 1);
            _pen.Freeze();

            _randomizer = new CryptoRandomizer();
            _positioner = new SpiralPositioner(new Size(Width, Height));
        }

        private bool CalculateNextStartingPoint(WordDrawing wordDrawing)
        {
            if (!_positioner.GetNextPoint(out var x, out var y))
            {
                return false;
            }

            wordDrawing.X = x;
            wordDrawing.Y = y;

            return true;
        }

        private void AdjustFinalPosition(BitArray newBytes, WordDrawing wordDrawing)
        {
            var previousX = wordDrawing.X;
            while (!HasCollision(newBytes, wordDrawing)
                   && Math.Abs(_cloudCenter.X - wordDrawing.X) > CharacterMargin + 3)
            {
                previousX = wordDrawing.X;
                wordDrawing.X += wordDrawing.X > _cloudCenter.X ? -CharacterMargin - 3 : CharacterMargin + 3;
            }

            wordDrawing.X = previousX;

            var previousY = wordDrawing.Y;
            while (!HasCollision(newBytes, wordDrawing)
                   && Math.Abs(_cloudCenter.Y - wordDrawing.Y) > CharacterMargin + 3)
            {
                previousY = wordDrawing.Y;
                wordDrawing.Y += wordDrawing.Y > _cloudCenter.Y ? -CharacterMargin - 3 : CharacterMargin + 3;
            }

            wordDrawing.Y = previousY;
        }

        private void CreateCollisionMap(WordDrawing wordDrawing)
        {
            SetStartingPosition(wordDrawing, StartPosition.Center);

            _collisionMapWidth = (int)Width;
            _collisionMapHeight = (int)Height;

            _collisionMap = new BitArray(_collisionMapWidth * _collisionMapHeight);
        }

        private void SetStartingPosition(WordDrawing wordDrawing, StartPosition position)
        {
            _positioner.Delta = wordDrawing.Width / wordDrawing.Height;

            switch (position)
            {
                case StartPosition.Center:
                    _positioner.StartX = _cloudCenter.X - wordDrawing.Width / 2;
                    _positioner.StartY = _cloudCenter.Y - wordDrawing.Height / 2;
                    break;

                case StartPosition.Random:
                    var quad = _randomizer.RandomInt(4);
                    var xMod = 0.0;
                    var yMod = 0.0;
                    switch (quad)
                    {
                        case 1:
                            yMod = -_cloudCenter.Y;
                            break;

                        case 2:
                            xMod = -_cloudCenter.X;
                            yMod = -_cloudCenter.Y;
                            break;

                        case 3:
                            xMod = -_cloudCenter.X;
                            break;
                    }

                    _positioner.StartX = xMod + _randomizer.RandomInt((int)(_cloudCenter.X * 1.5), (int)(_collisionMapWidth - wordDrawing.Width));
                    _positioner.StartY = yMod + _randomizer.RandomInt((int)(_cloudCenter.Y * 1.5), (int)(_collisionMapHeight - wordDrawing.Height));
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(position), position, null);
            }

            CalculateNextStartingPoint(wordDrawing);
        }

        private void AddNewCollisionPoint(BitArray collisionMap, int index)
        {
            var y = index / _collisionMapWidth;
            var x = index % _collisionMapWidth;

            collisionMap[index] = true;
            for (var i = 1; i <= CharacterMargin; i++)
            {
                if (x < _collisionMapWidth - i)
                {
                    collisionMap[index + i] = true;
                }

                if (y < _collisionMapHeight - i)
                {
                    collisionMap[index + _collisionMapWidth * i] = true;
                }

                if (x > i - 1)
                {
                    collisionMap[index - i] = true;
                }

                if (y > i - 1)
                {
                    collisionMap[index - _collisionMapWidth * i] = true;
                }
            }
        }

        private int _wordsFirstPixelIndex;

        private bool HasCollision(BitArray newWordBitArray, WordDrawing newWord)
        {
            if (IsOutOfBounds(newWord))
            {
                return true;
            }
            var srcWidth = _collisionMapWidth;

            var testX = newWord.IntX;
            var testY = newWord.IntY;
            var testWidth = newWord.IntWidth;
            var mapStart = srcWidth * testY + testX;
            var r1 = _wordsFirstPixelIndex % testWidth;

            var y1 = _wordsFirstPixelIndex / testWidth * srcWidth;
            for (var i = _wordsFirstPixelIndex; i < newWordBitArray.Length; ++i)
            {
                if (_wordsFirstPixelIndex == 0 && newWordBitArray[i])
                {
                    _wordsFirstPixelIndex = i;
                }

                if (newWordBitArray[i] && _collisionMap[mapStart + y1 + r1])
                {
                    return true;
                }

                if (++r1 == testWidth)
                {
                    r1 = 0;
                    y1 += srcWidth;
                }
            }

            _wordsFirstPixelIndex = 0;
            return false;
        }

        private BitArray CreateBitArrayFromGeometry(WordDrawing wordDrawing)
        {
            var bm = new RenderTargetBitmap(wordDrawing.IntWidth, wordDrawing.IntHeight, 96, 96, _pixelFormat);
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                dc.DrawGeometry(Brushes.Purple, _pen, wordDrawing.Geo);
            }

            bm.Render(dv);

            var bitmapStride = (bm.PixelWidth * _pixelFormat.BitsPerPixel + 7) / 8;
            var newBytes = new byte[bm.PixelHeight * bitmapStride];
            var totalPixels = wordDrawing.IntWidth * wordDrawing.IntHeight;
            var bitArray = new BitArray(totalPixels);

            bm.CopyPixels(newBytes, bitmapStride, 0);

            for (var i = 0; i < totalPixels - Pbgra32Bytes; ++i)
            {
                if (newBytes[i * Pbgra32Bytes + 3] <= 0)
                {
                    continue;
                }

                bitArray[i] = true;
            }

            return bitArray;
        }

        private void UpdateCollisionMap(BitArray newWordBitArray, WordDrawing newWord)
        {
            if (newWord.IntBottom > _collisionMapHeight
                || newWord.IntRight > _collisionMapWidth
                || newWord.IntRight < 0
                || newWord.IntBottom < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(newWordBitArray));
            }

            var srcWidth = _collisionMapWidth;
            var testX = newWord.IntX;
            var testY = newWord.IntY;
            var testWidth = newWord.IntWidth;
            var testHeight = newWord.IntHeight;
            var mapPosition = testY * srcWidth + testX;
            var testOffset = 0;
            for (var line = 0; line < testHeight; ++line)
            {
                for (var i = 0; i < testWidth; i++)
                {
                    if (newWordBitArray[testOffset + i])
                    {
                        AddNewCollisionPoint(_collisionMap, mapPosition + i);
                    }
                }

                mapPosition += srcWidth;
                testOffset += testWidth;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsOutOfBounds(WordDrawing wordDrawing)
        {
            return wordDrawing.IntRight >= _collisionMapWidth - CharacterMargin - 2
                   || wordDrawing.IntBottom >= _collisionMapHeight - CharacterMargin - 2
                   || wordDrawing.X < 0
                   || wordDrawing.Y < 0;
        }

        public bool AddWordGeometry(WordDrawing wordDrawing)
        {
            var newWordBitArray = CreateBitArrayFromGeometry(wordDrawing);

            if (_collisionMap == null)
            {
                CreateCollisionMap(wordDrawing);
                SetStartingPosition(wordDrawing, StartPosition.Center);
            }
            else
            {
                SetStartingPosition(wordDrawing, _defaultStartingPosition);
                if (!PlaceWord(wordDrawing, newWordBitArray))
                {
                    return false;
                }
            }

            UpdateCollisionMap(newWordBitArray, wordDrawing);

            return true;
        }

        private bool PlaceWord(WordDrawing wordDrawing, BitArray newWordBitArray)
        {
            var attempts = 0;
            while (HasCollision(newWordBitArray, wordDrawing))
            {
                do
                {
                    var result = CalculateNextStartingPoint(wordDrawing);
                    if (!result && attempts > RepositionAttempts)
                    {
                        FailedPlacements.Add(wordDrawing);
                        return false;
                    }

                    if (result)
                    {
                        continue;
                    }
                    _defaultStartingPosition = StartPosition.Random;
                    SetStartingPosition(wordDrawing, StartPosition.Random);
                    attempts++;
                } while (IsOutOfBounds(wordDrawing));
            }

            if (_defaultStartingPosition == StartPosition.Random)
            {
                AdjustFinalPosition(newWordBitArray, wordDrawing);
            }
            return true;
        }
    }

    internal enum StartPosition
    {
        Center,
        Random,
    }
}