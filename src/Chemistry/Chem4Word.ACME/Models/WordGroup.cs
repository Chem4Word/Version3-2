// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;

namespace Chem4Word.ACME.Models
{
    public class WordGroup
    {
        public WordGroup(string word)
            : this(word, new List<string>())
        { }

        public WordGroup(string word, IList<string> groupedWords)
        {
            Word = word;
            GroupedWords = groupedWords;
        }

        public string Word { get; set; }
        public IList<string> GroupedWords { get; }

        public override string ToString()
        {
            return string.Join(", ", GroupedWords);
        }
    }
}