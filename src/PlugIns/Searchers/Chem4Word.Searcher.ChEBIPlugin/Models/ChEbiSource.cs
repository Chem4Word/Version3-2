// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Chem4Word.Searcher.ChEBIPlugin.Models
{
    public class ChEbiSource
    {
        [JsonProperty("chebi_accession")]
        public string ChebiId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("ascii_name")]
        public string AsciiName { get; set; }

        [JsonProperty("stars")]
        public int Stars { get; set; }

        [JsonProperty("default_structure")]
        public int DefaultStructure { get; set; }

        [JsonProperty("definition")]
        public string Definition { get; set; }

        [JsonProperty("mass")]
        public double Mass { get; set; }

        [JsonProperty("formula")]
        public string Formula { get; set; }

        [JsonProperty("inchikey")]
        public string InchiKey { get; set; }

        [JsonProperty("monoisotopicmass")]
        public double MonoIsotopicMass { get; set; }

        [JsonProperty("smiles")]
        public string Smiles { get; set; }

        [JsonProperty("charge")]
        public int Charge { get; set; }

        [JsonProperty("inchi")]
        public string Inchi { get; set; }

        [JsonProperty("structures")]
        public List<int> Structures { get; set; }
    }
}
