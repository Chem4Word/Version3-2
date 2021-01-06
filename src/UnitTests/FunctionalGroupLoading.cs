// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Chem4WordTests
{
    public class FunctionalGroupLoading
    {
        [Fact]
        public void ShortCutListHasNoDuplicates()
        {
            var names = FunctionalGroups.ShortcutList.Select(s => s.Name).ToList();
            var distinct = names.Distinct().ToList();

            Assert.Equal(names.Count, distinct.Count);
        }

        [Fact]
        public void ShortcutListIsPopulated()
        {
            var message = "Expected at least one entry in FunctionalGroups shortcut list";
            Assert.True(FunctionalGroups.ShortcutList.Any(), message);
        }

        [Fact]
        public void ShortcutListDoesNotContainElements()
        {
            var names = FunctionalGroups.ShortcutList.Select(s => s.Name).ToList();
            var elements = Globals.PeriodicTable.Elements.Keys.ToList();
            var mixed = names.Intersect(elements).ToList();

            Assert.True(mixed.Count == 0);
        }

        [Theory]
        [InlineData("R1", "R 1")]
        [InlineData("R9", "R 1")]
        [InlineData("CH2", "C 1 H 2")]
        [InlineData("CH3", "C 1 H 3")]
        [InlineData("CO2H", "C 1 O 2 H 1")]
        [InlineData("CH2CH2OH", "C 2 H 3 O 1")]
        [InlineData("Et", "C 2 H 5")]
        [InlineData("TMS", "Si 1 C 3 H 3")]
        public void FormulaPartsCalculated(string shortcut, string expected)
        {
            var functionalGroup = FunctionalGroups.ShortcutList.FirstOrDefault(f => f.Name.Equals(shortcut));

            var calculated = functionalGroup.FormulaParts;
            string actual = string.Empty;
            foreach (var kvp in calculated)
            {
                actual += $"{kvp.Key} {kvp.Value} ";
            }

            actual = actual.Trim();
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("R1", 0.00)]
        [InlineData("R9", 0.00)]
        [InlineData("CH2", 14.03)]
        [InlineData("CH3", 15.04)]
        [InlineData("CO2H", 45.02)]
        [InlineData("CH2CH2OH", 45.06)]
        [InlineData("Et", 29.06)]
        [InlineData("TMS", 73.19)]
        public void AtomicWeightIsCalculated(string shortcut, double expectedAtomicWeight)
        {
            var functionalGroup = FunctionalGroups.ShortcutList.FirstOrDefault(f => f.Name.Equals(shortcut));

            var actualAtomicWeight = functionalGroup.AtomicWeight;

            Assert.Equal(expectedAtomicWeight, actualAtomicWeight, 2);
        }

        [Theory]
        [InlineData("R1", false, "[R{1}]")]
        [InlineData("R9", false, "[R{9}]")]
        [InlineData("CH2", false, "[C]H2")]
        [InlineData("CH2", true, "H2[C]")]
        [InlineData("CH3", false, "[C]H3")]
        [InlineData("CH3", true, "H3[C]")]
        [InlineData("CO2H", false, "[C]O2H")]
        [InlineData("CO2H", true, "HO2[C]")]
        [InlineData("CH2CH2OH", false, "[CH2]CH2OH")]
        [InlineData("CH2CH2OH", true, "HOCH2[CH2]")]
        [InlineData("Et", false, "[Et]")]
        [InlineData("TMS", false, "[Si](CH3)3")]
        [InlineData("TMS", true, "(CH3)3[Si]")]
        public void Expansion(string shortcut, bool reverse, string expected)
        {
            var functionalGroup = FunctionalGroups.ShortcutList.FirstOrDefault(f => f.Name.Equals(shortcut));

            var terms = functionalGroup.ExpandIntoTerms(reverse);
            var item = Flatten(terms);

            Assert.Equal(expected, item);
        }

        private string Flatten(List<FunctionalGroupTerm> terms)
        {
            string result = string.Empty;

            foreach (var term in terms)
            {
                if (term.IsAnchor)
                {
                    result += "[";
                }
                foreach (var part in term.Parts)
                {
                    if (part.Type == FunctionalGroupPartType.Superscript)
                    {
                        result += "{" + part.Text + "}";
                    }
                    else
                    {
                        result += part.Text;
                    }
                }
                if (term.IsAnchor)
                {
                    result += "]";
                }
            }

            return result;
        }
    }
}