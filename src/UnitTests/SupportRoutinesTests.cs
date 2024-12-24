// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Utils;
using Xunit;

namespace Chem4WordTests
{
    public class SupportRoutinesTests
    {
        // Note expected En-Dash and not hyphen for negative values
        [Theory]
        [InlineData(-4, "4–")]
        [InlineData(-3, "3–")]
        [InlineData(-2, "2–")]
        [InlineData(-1, "–")]
        [InlineData(null, "")]
        [InlineData(0, "")]
        [InlineData(1, "+")]
        [InlineData(2, "2+")]
        [InlineData(3, "3+")]
        [InlineData(4, "4+")]
        public void AcmeGetChargeString(int? charge, string result)
        {
            var actual = TextUtils.GetChargeString(charge);
            Assert.Equal(result, actual);
        }
    }
}