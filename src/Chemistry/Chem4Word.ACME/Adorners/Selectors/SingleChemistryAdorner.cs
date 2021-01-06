// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Controls;
using Chem4Word.Model2;

namespace Chem4Word.ACME.Adorners.Selectors
{
    public abstract class SingleChemistryAdorner : BaseSelectionAdorner
    {
        #region Shared Properties

        //what the adorner attaches to.  Can be an atom or a bond
        protected ChemistryBase AdornedChemistry { get; }

        #endregion Shared Properties

        #region Constructors

        protected SingleChemistryAdorner(EditorCanvas currentEditor) : base(currentEditor)
        { }

        protected SingleChemistryAdorner(EditorCanvas currentEditor, ChemistryBase adornedChemistry) : this(currentEditor)
        {
            AdornedChemistry = adornedChemistry;
        }

        #endregion Constructors
    }
}