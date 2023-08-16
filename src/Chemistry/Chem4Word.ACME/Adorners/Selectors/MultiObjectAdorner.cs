// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Controls;
using Chem4Word.Model2;
using System.Collections.Generic;
using System.Linq;

namespace Chem4Word.ACME.Adorners.Selectors
{
    public abstract class MultiObjectAdorner : BaseSelectionAdorner
    {
        #region Shared Properties

        public List<BaseObject> AdornedObjects { get; }

        #endregion Shared Properties

        #region Constructors

        protected MultiObjectAdorner(EditorCanvas currentEditor, List<BaseObject> chemistries) : base(currentEditor)
        {
            AdornedObjects = new List<BaseObject>();
            AdornedObjects.AddRange(chemistries.Distinct());
        }

        #endregion Constructors
    }
}