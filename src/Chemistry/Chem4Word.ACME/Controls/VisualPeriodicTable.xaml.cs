// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Chem4Word.Model2;

namespace Chem4Word.ACME.Controls
{
    /// <summary>
    /// Interaction logic for VisualPeriodicTable.xaml
    /// </summary>
    public partial class VisualPeriodicTable : UserControl
    {
        public VisualPeriodicTable()
        {
            InitializeComponent();
            var window = Window.GetWindow(this);
            Focusable = true;
            Focus();
            ElementGrid.ItemContainerGenerator.StatusChanged += new EventHandler(ItemContainerGenerator_StatusChanged);
        }

        public Element SelectedElement
        {
            get { return (Element)GetValue(SelectedElementProperty); }
            set { SetValue(SelectedElementProperty, value); }
        }

        public static readonly DependencyProperty SelectedElementProperty =
            DependencyProperty.Register("SelectedElement", typeof(Element), typeof(VisualPeriodicTable),
                                        new PropertyMetadata(null, SelectedElementChanged));

        private static void SelectedElementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var newElement = e.NewValue as Element;
            var control = d as VisualPeriodicTable;
        }

        private void ItemContainerGenerator_StatusChanged(object sender, EventArgs e)
        {
            if (ElementGrid.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                HighlightSelectedElement(SelectedElement, this);
            }
        }

        public static void HighlightSelectedElement(Element newElement, VisualPeriodicTable vpt)
        {
            for (int i = 0; i < vpt.ElementGrid.Items.Count; i++)
            {
                ContentPresenter c =
                    (ContentPresenter)vpt.ElementGrid.ItemContainerGenerator.ContainerFromItem(
                        vpt.ElementGrid.Items[i]);
                c.ApplyTemplate();
                TextBlock tb = c.ContentTemplate.FindName("ElementBlock", c) as TextBlock;
                var currentElement = tb.Tag as Element;
                var elementSquare = c.ContentTemplate.FindName("ElementSquare", c) as Border;

                if (currentElement == newElement)
                {
                    elementSquare.BorderBrush = new SolidColorBrush(Colors.Red);
                }
                else
                {
                    elementSquare.BorderBrush = null;
                }
            }
        }

        public class ElementEventArgs : EventArgs
        {
            public ElementBase SelectedElement { get; set; }
        }

        public delegate void ElementSelectedEvent(object sender, ElementEventArgs e);

        public event ElementSelectedEvent ElementSelected;

        private void ElementSquare_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var elementBlocks = VisualTreeHelpers.FindAllChildren<TextBlock>(sender as Border);
            SelectedElement = (elementBlocks.ToList()[0].Tag as Element);
            ElementSelected?.Invoke(sender, new ElementEventArgs { SelectedElement = this.SelectedElement });
        }
    }
}