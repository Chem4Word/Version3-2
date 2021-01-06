// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using Chem4Word.Model2.Helpers;
using Newtonsoft.Json;

namespace Chem4Word.Model2
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Group
    {
        [JsonProperty]
        public string Component { get; set; }

        [JsonProperty]
        public int Count { get; set; }

        public Group(string e, int c)
        {
            Component = e;
            Count = c;
        }

        public override string ToString()
        {
            return $"{Component} * {Count}";
        }

        /// <summary>
        /// Calculated AtomicWeight
        /// </summary>
        public double AtomicWeight
        {
            get
            {
                double atomicWeight = 0;
                ElementBase eb;
                if (AtomHelpers.TryParse(Component, out eb))
                {
                    if (eb is Element e)
                    {
                        atomicWeight = e.AtomicWeight;
                    }

                    if (eb is FunctionalGroup fg)
                    {
                        atomicWeight = fg.AtomicWeight;
                    }
                }

                return atomicWeight;
            }
        }

        public Dictionary<string, int> FormulaParts
        {
            get
            {
                Dictionary<string, int> parts = new Dictionary<string, int>();

                ElementBase eb;
                if (AtomHelpers.TryParse(Component, out eb))
                {
                    if (eb is Element e)
                    {
                        parts.Add(e.Symbol, 1);
                    }

                    if (eb is FunctionalGroup fg)
                    {
                        foreach (var component in fg.Components)
                        {
                            var pp = component.FormulaParts;
                            foreach (var p in pp)
                            {
                                if (parts.ContainsKey(p.Key))
                                {
                                    parts[p.Key] += p.Value;
                                }
                                else
                                {
                                    parts.Add(p.Key, p.Value);
                                }
                            }
                        }
                    }
                }

                return parts;
            }
        }
    }
}