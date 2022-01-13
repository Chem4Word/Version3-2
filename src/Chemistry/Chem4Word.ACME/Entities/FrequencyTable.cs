// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Chem4Word.ACME.Entities
{
    /// <summary>
    /// Frequency table generic class
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FrequencyTable<T>
    {
        public FrequencyTable(IList<FrequencyTableRow<T>> rows, long totalCount)
        {
            if (totalCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalCount));
            }

            Rows = rows ?? throw new ArgumentNullException(nameof(rows));
            TotalCount = totalCount;
        }

        /// <summary>
        /// Row elements of the table
        /// </summary>
        public IList<FrequencyTableRow<T>> Rows { get; }

        /// <summary>
        /// Total count, this may not be the same as the sum of the rows
        /// </summary>
        public long TotalCount { get; }
    }
}