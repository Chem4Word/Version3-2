// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Chem4Word.ACME.Controls
{
    public class GridAwareItemsControl : ItemsControl
    {
        public GridAwareItemsControl()
        {
            VirtualizingPanel.SetIsVirtualizing(this, false);
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            ContentPresenter container = (ContentPresenter)base.GetContainerForItemOverride();
            if (ItemTemplate == null)
            {
                return container;
            }

            FrameworkElement content = (FrameworkElement)ItemTemplate.LoadContent();
            BindingExpression rowBinding = content.GetBindingExpression(Grid.RowProperty);
            BindingExpression columnBinding = content.GetBindingExpression(Grid.ColumnProperty);

            if (rowBinding != null)
            {
                container.SetBinding(Grid.RowProperty, rowBinding.ParentBinding);
            }

            if (columnBinding != null)
            {
                container.SetBinding(Grid.ColumnProperty, columnBinding.ParentBinding);
            }

            return container;
        }
    }
}