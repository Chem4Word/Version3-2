// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Input;

namespace Chem4Word.ACME.Behaviors
{
    /// <summary>
    /// Captures and eats MouseWheel events so that a nested ListBox does not
    /// prevent an outer scrollable control from scrolling.
    ///
    /// Source https://stackoverflow.com/questions/2189053/disable-mouse-wheel-on-itemscontrol-in-wpf
    /// </summary>
    public class IgnoreMouseWheelBehavior : Behavior<UIElement>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewMouseWheel += AssociatedObject_PreviewMouseWheel;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewMouseWheel -= AssociatedObject_PreviewMouseWheel;
            base.OnDetaching();
        }

        private void AssociatedObject_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;

            var e2 = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
            e2.RoutedEvent = UIElement.MouseWheelEvent;

            AssociatedObject.RaiseEvent(e2);
        }
    }
}