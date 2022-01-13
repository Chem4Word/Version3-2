// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;

namespace Chem4Word.Libraries.Database
{
    public class ChemistryDataObject
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Formula { get; set; }
        public string Cml { get; set; }

        public List<ChemistryTagDataObject> Tags { get; set; }
    }
}