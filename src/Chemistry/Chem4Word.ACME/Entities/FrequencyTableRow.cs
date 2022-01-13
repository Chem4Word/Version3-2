// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace Chem4Word.ACME.Entities
{
    /// <summary>
    /// Row in frequency table
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FrequencyTableRow<T>
    {
        public FrequencyTableRow(T item, long count)
        {
            Item = item;
            Count = count;
        }

        /// <summary>
        /// Item of frequency table
        /// </summary>
        public T Item { get; }

        /// <summary>
        /// Number of occurrences
        /// </summary>
        public long Count { get; }
    }
}