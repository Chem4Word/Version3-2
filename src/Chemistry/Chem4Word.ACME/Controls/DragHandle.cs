// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------
using Chem4Word.Model2.Helpers;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Chem4Word.ACME.Controls
{
    public class DragHandle : Thumb
    {
        public DragHandle(string styleName = Globals.GrabHandleStyle, Cursor cursor= null)
        {
            Style =  (Style)FindResource(Globals.GrabHandleStyle);
            if(cursor is null)
            {
                cursor = Cursors.Hand;
            }
            Cursor = cursor;
            IsHitTestVisible =true;
        }
    }
}
