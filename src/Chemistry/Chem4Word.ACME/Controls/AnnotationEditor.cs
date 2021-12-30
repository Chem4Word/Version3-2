// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Chem4Word.ACME.Controls
{
    /// <summary>
    /// Wraps the RichTextBox to add additional editing functionality
    ///
    /// </summary>
    public class AnnotationEditor : RichTextBox
    {
        #region Constructors
        public AnnotationEditor()
        {
            Height=80;
            Width=80;
            Background = Brushes.LightSeaGreen;
            BorderThickness=new Thickness(1);
            BorderBrush=Brushes.Black;
            Document.Blocks.Add(new Paragraph(new Run("lorem ipsum")));
        }
        public AnnotationEditor(EditController controller): this()
        {
            EditorCanvas canvas = controller.CurrentEditor;
            canvas.Children.Add(this);
            this.BringIntoView();
        }
        #endregion
        #region Properties
        #endregion 
        #region Methods

        /// <summary>
        /// Centres the editor on a specific point in the canvas
        /// </summary>
        /// <param name="point"></param>
        public void CentreOn(Point point)
        {
            Canvas.SetLeft(this, point.X - (Width / 2));
            Canvas.SetTop(this, point.Y - (Height/ 2));
        }
        #region Overrides
        protected override Size MeasureOverride(Size constraint)
        {
            var mySize = new Size
            {
                Width = 80,
                Height = 80
            };
            return mySize;
        }
        #endregion
        #endregion
    }
}