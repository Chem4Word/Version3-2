// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Text;
using System.Windows;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2.Geometry;
using Chem4Word.Renderer.OoXmlV4.OOXML;
using Chem4Word.Renderer.OoXmlV4.TTF;

namespace Chem4Word.Renderer.OoXmlV4.Entities
{
    public class GroupOfCharacters
    {
        public string Text { get; private set; }

        private Rect _boundingBox = Rect.Empty;
        public Rect BoundingBox => _boundingBox;

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
        private double _bondLength;
        private string _atomPath;
        private string _parentPath;

        private TtfCharacter _hydrogenCharacter;

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
            foreach (char character in value)
            {
                AddCharacter(character, colour);
            }
        }

        public void AddCharacter(char character, string colour, bool isSubScript = false, bool isSuperScript = false)
        {
            Text += character;

            // Create new AtomLabelCharacter and add to Characters
            var ttfCharacter = _characterSet[character];
            if (ttfCharacter != null)
            {
                var isSmaller = isSubScript || isSuperScript;

                // Get character position as if it's standard size
                var thisCharacterPosition = GetCharacterPosition(_cursor, ttfCharacter);

                if (isSmaller)
                {
                    // Assume that it's SubScript so move it down by drop factor
                    thisCharacterPosition.Offset(0, OoXmlHelper.ScaleCsTtfToCml(_hydrogenCharacter.Height * OoXmlHelper.SUBSCRIPT_DROP_FACTOR, _bondLength));

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

                Size size = new Size(OoXmlHelper.ScaleCsTtfToCml(ttfCharacter.Width, _bondLength),
                                     OoXmlHelper.ScaleCsTtfToCml(ttfCharacter.Height, _bondLength));

                if (isSmaller)
                {
                    size.Width *= OoXmlHelper.SUBSCRIPT_SCALE_FACTOR;
                    size.Height *= OoXmlHelper.SUBSCRIPT_SCALE_FACTOR;
                }

                Rect thisBoundingBox = new Rect(thisCharacterPosition, size);
                _boundingBox.Union(thisBoundingBox);

                // Move to next Character position
                // We ought to be able to use ttfCharacter.IncrementX, but this does not work with strings such as "Bowl"
                if (isSmaller)
                {
                    _cursor.Offset(OoXmlHelper.ScaleCsTtfToCml(ttfCharacter.IncrementX, _bondLength) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR, 0);
                }
                else
                {
                    _cursor.Offset(OoXmlHelper.ScaleCsTtfToCml(ttfCharacter.IncrementX, _bondLength), 0);
                }
            }
        }

        public void AdjustPosition(Vector adjust)
        {
            foreach (AtomLabelCharacter c in Characters)
            {
                c.Position += adjust;
            }

            _boundingBox.Offset(adjust);
        }

        public void Nudge(CompassPoints direction)
        {
            var destination = Centre;
            switch (direction)
            {
                case CompassPoints.North:
                    destination = new Point(Centre.X, Centre.Y - _bondLength / 16);
                    break;

                case CompassPoints.East:
                    destination = new Point(Centre.X + _bondLength / 16, Centre.Y);
                    break;

                case CompassPoints.South:
                    destination = new Point(Centre.X, Centre.Y + _bondLength / 16);
                    break;

                case CompassPoints.West:
                    destination = new Point(Centre.X - _bondLength / 16, Centre.Y);
                    break;
            }
            AdjustPosition(destination - Centre);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"{Text}");
            sb.Append(" ");
            sb.Append($"at {PointHelper.AsString(BoundingBox.Location)}");
            sb.Append(" ");
            sb.Append($"size {SafeDouble.AsString4(BoundingBox.Size.Width)}x{SafeDouble.AsString4(BoundingBox.Size.Height)}");
            return sb.ToString();
        }

        // Add the (negative) OriginY to raise the character by it
        private Point GetCharacterPosition(Point cursorPosition, TtfCharacter character) =>
            new Point(cursorPosition.X, cursorPosition.Y + OoXmlHelper.ScaleCsTtfToCml(character.OriginY, _bondLength));
    }
}