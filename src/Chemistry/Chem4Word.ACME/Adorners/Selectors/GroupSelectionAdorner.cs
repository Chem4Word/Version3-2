// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Utils;
using Chem4Word.Model2;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace Chem4Word.ACME.Adorners.Selectors
{
    public class GroupSelectionAdorner : MoleculeSelectionAdorner
    {
        public GroupSelectionAdorner(EditorCanvas currentEditor, List<BaseObject> molecules)
            : base(currentEditor, molecules)
        {
        }

        protected override void SetThumbStyle(Thumb cornerThumb)
        {
            cornerThumb.Style = (Style)FindResource(Common.GroupHandleStyle);
        }
    }
}