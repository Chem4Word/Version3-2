// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Enums;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using Chem4Word.Renderer.OoXmlV4.OOXML;
using Chem4Word.Renderer.OoXmlV4.TTF;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace Chem4Word.Renderer.OoXmlV4.Entities
{
    public class GroupOfCharacters
    {
        public string Text { get; private set; }

        private Rect _boundingBox = Rect.Empty;
        public Rect BoundingBox => _boundingBox;

        private int? _firstCharacterOfLineOriginX;

        public List<AtomLabelCharacter> Characters { get; } = new List<AtomLabelCharacter>();

        public Point Centre =>
            new Point(_boundingBox.Left + _boundingBox.Width / 2,
                      _boundingBox.Top + _boundingBox.Height / 2);

        public Point NorthCentre =>
            new Point(_boundingBox.Left + _boundingBox.Width / 2,
                      _boundingBox.Top);

        public Point EastCentre =>
            new Point(_boundingBox.Right,
                      _boundingBox.Top + _boundingBox.Height / 2);

        public Point SouthCentre =>
            new Point(_boundingBox.Left + _boundingBox.Width / 2,
                      _boundingBox.Bottom);

        public Point WestCentre =>
            new Point(_boundingBox.Left,
                      _boundingBox.Top + _boundingBox.Height / 2);

        private Dictionary<char, TtfCharacter> _characterSet { get; set; }

        private Point _cursor;
        private readonly double _bondLength;
        private readonly string _atomPath;
        private readonly string _parentPath;

        private readonly TtfCharacter _hydrogenCharacter;

        public GroupOfCharacters(Point cursor, string atomPath, string parentPath, Dictionary<char, TtfCharacter> characterSet, double bondLength)
        {
            _cursor = cursor;
            _atomPath = atomPath;
            _parentPath = parentPath;
            _characterSet = characterSet;
            _bondLength = bondLength;

            _hydrogenCharacter = _characterSet['H'];
        }

        public void AddString(string value, string colour)
        {
            foreach (var character in value)
            {
                AddCharacter(character, colour);
            }
        }

        public void AddParts(List<FunctionalGroupPart> parts, string colour)
        {
            foreach (var part in parts)
            {
                foreach (var character in part.Text)
                {
                    switch (part.Type)
                    {
                        case FunctionalGroupPartType.Normal:
                            AddCharacter(character, colour);
                            break;

                        case FunctionalGroupPartType.Subscript:
                            AddCharacter(character, colour, true);
                            break;

                        case FunctionalGroupPartType.Superscript:
                            AddCharacter(character, colour, isSuperScript: true);
                            break;
                    }
                }
            }
        }

        public void AddCharacter(char characterIn, string colour, bool isSubScript = false, bool isSuperScript = false)
        {
            // Ensure we only create known characters
            var knownCharacter = characterIn;
            if (!_characterSet.ContainsKey(characterIn))
            {
                knownCharacter = OoXmlHelper.DefaultCharacter;
            }

            Text += knownCharacter;

            // Create new AtomLabelCharacter and add to Characters
            var ttfCharacter = _characterSet[knownCharacter];
            if (ttfCharacter != null)
            {
                if (_firstCharacterOfLineOriginX == null)
                {
                    _firstCharacterOfLineOriginX = ttfCharacter.OriginX;
                }
                var isSmaller = isSubScript || isSuperScript;

                // Get character position as if it's standard size
                var thisCharacterPosition = GetCharacterPosition(_cursor, ttfCharacter);

                if (isSmaller)
                {
                    // Assume that it's SubScript so move it down by drop factor
                    thisCharacterPosition.Offset(0, OoXmlHelper.ScaleCsTtfToCml(_hydrogenCharacter.Height * OoXmlHelper.SubscriptDropFactor, _bondLength));

                    // If it is SuperScript move it back up by height of 'H'
                    if (isSuperScript)
                    {
                        thisCharacterPosition.Offset(0, -OoXmlHelper.ScaleCsTtfToCml(_hydrogenCharacter.Height, _bondLength));
                    }
                }

                var alc = new AtomLabelCharacter(thisCharacterPosition, ttfCharacter, colour, _atomPath, _parentPath);
                alc.IsSubScript = isSubScript;
                alc.IsSuperScript = isSuperScript;
                alc.IsSmaller = isSmaller;
                Characters.Add(alc);

                var size = new Size(OoXmlHelper.ScaleCsTtfToCml(ttfCharacter.Width, _bondLength),
                                    OoXmlHelper.ScaleCsTtfToCml(ttfCharacter.Height, _bondLength));

                if (isSmaller)
                {
                    size.Width *= OoXmlHelper.SubscriptScaleFactor;
                    size.Height *= OoXmlHelper.SubscriptScaleFactor;
                }

                var thisBoundingBox = new Rect(thisCharacterPosition, size);
                _boundingBox.Union(thisBoundingBox);

                // Move to next Character position
                if (isSmaller)
                {
                    _cursor.Offset(OoXmlHelper.ScaleCsTtfToCml(ttfCharacter.IncrementX, _bondLength) * OoXmlHelper.SubscriptScaleFactor, 0);
                }
                else
                {
                    _cursor.Offset(OoXmlHelper.ScaleCsTtfToCml(ttfCharacter.IncrementX, _bondLength), 0);
                }
            }
        }

        public void AdjustPosition(Vector adjust)
        {
            foreach (var c in Characters)
            {
                c.Position += adjust;
            }

            _boundingBox.Offset(adjust);
        }

        public void NewLine(double xOffset = 0)
        {
            _cursor = BoundingBox.BottomLeft;
            if (_firstCharacterOfLineOriginX != null)
            {
                _cursor.Offset(-OoXmlHelper.ScaleCsTtfToCml(_firstCharacterOfLineOriginX.Value, _bondLength), 0);
                _firstCharacterOfLineOriginX = null;
            }
            _cursor.Offset(xOffset, OoXmlHelper.ScaleCsTtfToCml(_hydrogenCharacter.Height * 1.25, _bondLength));
        }

        public void Nudge(CompassPoints direction, double pixels = 0)
        {
            var moveBy = pixels;
            if (pixels == 0)
            {
                moveBy = _bondLength / 16;
            }

            var destination = Centre;
            switch (direction)
            {
                case CompassPoints.North:
                    destination = new Point(Centre.X, Centre.Y - moveBy);
                    break;

                case CompassPoints.East:
                    destination = new Point(Centre.X + moveBy, Centre.Y);
                    break;

                case CompassPoints.South:
                    destination = new Point(Centre.X, Centre.Y + moveBy);
                    break;

                case CompassPoints.West:
                    destination = new Point(Centre.X - moveBy, Centre.Y);
                    break;
            }

            AdjustPosition(destination - Centre);
        }

        private Point GetCharacterPosition(Point cursorPosition, TtfCharacter character)
        {
            var position = new Point(cursorPosition.X + OoXmlHelper.ScaleCsTtfToCml(character.OriginX, _bondLength),
                                     cursorPosition.Y + OoXmlHelper.ScaleCsTtfToCml(character.OriginY, _bondLength));

            return position;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"{Text}");
            sb.Append(" ");
            sb.Append($"at {PointHelper.AsString(BoundingBox.Location)}");
            sb.Append(" ");
            sb.Append($"size {SafeDouble.AsString4(BoundingBox.Size.Width)}x{SafeDouble.AsString4(BoundingBox.Size.Height)}");
            return sb.ToString();
        }
    }
}