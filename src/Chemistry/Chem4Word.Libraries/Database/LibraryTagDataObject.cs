// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace Chem4Word.Libraries.Database
{
    public class LibraryTagDataObject
    {
        public long Id { get; set; }
        public string Text { get; set; }

        public long Frequency { get; set; }
    }
}