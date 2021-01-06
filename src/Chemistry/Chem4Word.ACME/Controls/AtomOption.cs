// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Chem4Word.ACME.Controls;
using Chem4Word.Model2;

namespace Chem4Word.ACME
{
    public class AtomOption : ComboBoxItem
    {
        public static readonly DependencyProperty ElementProperty =
            DependencyProperty.Register("Element", typeof(ElementBase), typeof(AtomOption),
                                        new PropertyMetadata(default(ElementBase)));

        public static readonly DependencyProperty DisplayStyleProperty =
            DependencyProperty.Register("DisplayStyle", typeof(Style), typeof(AtomOption),
                                        new PropertyMetadata(default(Style)));

        public AtomOption()
        {
        }

        public AtomOption(FunctionalGroup fg)
        {
            Element = fg;
            Foreground = new SolidColorBrush(Colors.Black);
            Content = new FunctionalGroupBlock { ParentGroup = fg };
        }

        public AtomOption(Element elem)
        {
            Element = elem;
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(elem.Colour));
            Content = elem.Symbol;
        }

        public ElementBase Element
        {
            get => (ElementBase)GetValue(ElementProperty);
            set => SetValue(ElementProperty, value);
        }
    }
}