// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace Chem4Word.Model2.Converters.CML
{
    // https://docs.chemaxon.com/display/docs/marvin-documents-mrv.md

    public enum CmlFormat
    {
        Default,

        /// <summary>
        /// Makes molecule the root node
        /// </summary>
        ChemDraw,

        /// <summary>
        /// Custom for MarvinJS
        /// </summary>
        MarvinJs
    }
}