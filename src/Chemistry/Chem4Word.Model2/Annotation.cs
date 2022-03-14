// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Xml.Linq;
using Chem4Word.Model2.Interfaces;

namespace Chem4Word.Model2
{
    /// <summary>
    /// A textual annotation. Can be free floating or attached to an object
    /// </summary>
    public class Annotation : BaseObject, INotifyPropertyChanged
    {
        private string _xaml;
        private bool _isEditable;
        private Point _position;

        #region Properties

        public override string Path
        {
            get
            {
                var path = "";

                if (Parent == null)
                {
                    path = Id;
                }

                if (Parent is Model model)
                {
                    path = model.Path + Id;
                }

                if (Parent is Molecule molecule)
                {
                    path = molecule.Path + "/" + Id;
                }

                return path;
            }
        }

        public Guid InternalId { get; }
        public string Id { get; set; }
        public IChemistryContainer Parent { get; set; }

        public bool IsEditable
        {
            get => _isEditable;
            set { _isEditable = value; OnPropertyChanged(); }
        }

        public string Xaml
        {
            get => _xaml;

            set
            {
                _xaml = value;
                XDocument flowDoc = XDocument.Parse(value);
                var content = flowDoc.Descendants().OfType<XText>().Aggregate("", (a, b) => a + b);
                EstLength = content.Length;
                OnPropertyChanged();
            }
        }

        public Point Position
        {
            get => _position;
            set { _position = value; OnPropertyChanged(); }
        }

        private int EstLength { get; set; }
        public double? SymbolSize { get; set; }

        #endregion Properties

        #region Constructors

        public Annotation()
        {
            InternalId = Guid.NewGuid();
            Id = InternalId.ToString("D");
            IsEditable = true;
        }

        #endregion Constructors

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        internal void RepositionAll(double x, double y)
        {
            var offsetVector = new Vector(-x, -y);
            Position += offsetVector;
        }

        //tries to get an estimated bounding box for each annotation
        public Rect BoundingBox(double fontSize)
        {
            double width = fontSize * EstLength;
            var boundingBox = new Rect(Position, new Size(width, fontSize));
            return boundingBox;
        }

        public void ReLabel(ref int annotationCount)
        {
            Id = $"t{++annotationCount}";
        }

        public void ReLabelGuids(ref int annotationCount)
        {
            Guid guid;
            if (Guid.TryParse(Id, out guid))
            {
                Id = $"t{++annotationCount}";
            }
        }

        public Annotation Copy()
        {
            var copy = new Annotation
            {
                Id = Id,
                IsEditable = IsEditable,
                Position = Position,
                Xaml = Xaml
            };
            return copy;
        }

        public void UpdateVisual()
        {
            OnPropertyChanged(nameof(Position));
        }
    }
}