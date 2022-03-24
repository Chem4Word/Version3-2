// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Chem4Word.ACME.Utils;

namespace Chem4Word.ACME.Controls
{
    public class DragHandle : Thumb
    {
        public DragHandle(string styleName = Common.GrabHandleStyle, Cursor cursor = null)
        {
            Style = (Style)FindResource(Common.GrabHandleStyle);
            if (cursor is null)
            {
                cursor = Cursors.Hand;
            }
            Cursor = cursor;
            IsHitTestVisible = true;
        }
    }
}