// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Chem4Word.ACME.Controls;
using Chem4Word.Model2;

namespace Chem4Word.ACME.Adorners.Selectors
{
    public abstract class MultiChemistryAdorner : BaseSelectionAdorner
    {
        #region Shared Properties

        public List<ChemistryBase> AdornedChemistries { get; }

        #endregion Shared Properties

        #region Constructors

        protected MultiChemistryAdorner(EditorCanvas currentEditor, List<ChemistryBase> chemistries) : base(currentEditor)
        {
            AdornedChemistries = new List<ChemistryBase>();
            AdornedChemistries.AddRange(chemistries.Distinct());
        }

        #endregion Constructors
    }
}