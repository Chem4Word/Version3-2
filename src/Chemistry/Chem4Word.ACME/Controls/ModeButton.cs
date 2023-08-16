// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;

namespace Chem4Word.ACME.Controls
{
    public class ModeButton : RadioButton
    {
        public Behavior<FrameworkElement> ActiveModeBehavior
        {
            get { return (Behavior<FrameworkElement>)GetValue(ActiveModeBehaviorProperty); }
            set { SetValue(ActiveModeBehaviorProperty, value); }
        }

        public static readonly DependencyProperty ActiveModeBehaviorProperty =
            DependencyProperty.Register("ActiveModeBehavior", typeof(Behavior<FrameworkElement>), typeof(ModeButton), new PropertyMetadata(null));
    }
}