﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Chem4Word.Model2
{
    public class PeriodicTable
    {
        public Element Null { get; private set; }

        #region Pseudo Elements - Code generated by PeriodicTable.xlsx

        public Element Du { get; private set; } // Dummy - Atomic Number 0
        public Element M { get; private set; }  // Metal - Atomic Number 0
        public Element R { get; private set; }  // Residue - Atomic Number 0
        public Element X { get; private set; }  // Halogen - Atomic Number 0

        #endregion Pseudo Elements - Code generated by PeriodicTable.xlsx

        #region Real Elements  - Code generated by PeriodicTable.xlsx

        public Element D { get; private set; } // Deuterium - Atomic Number 1
        public Element H { get; private set; } // Hydrogen - Atomic Number 1
        public Element T { get; private set; } // Tritium - Atomic Number 1
        public Element He { get; private set; } // Helium - Atomic Number 2
        public Element Li { get; private set; } // Lithium - Atomic Number 3
        public Element Be { get; private set; } // Beryllium - Atomic Number 4
        public Element B { get; private set; } // Boron - Atomic Number 5
        public Element C { get; private set; } // Carbon - Atomic Number 6
        public Element N { get; private set; } // Nitrogen - Atomic Number 7
        public Element O { get; private set; } // Oxygen - Atomic Number 8
        public Element F { get; private set; } // Fluorine - Atomic Number 9
        public Element Ne { get; private set; } // Neon - Atomic Number 10
        public Element Na { get; private set; } // Sodium - Atomic Number 11
        public Element Mg { get; private set; } // Magnesium - Atomic Number 12
        public Element Al { get; private set; } // Aluminum - Atomic Number 13
        public Element Si { get; private set; } // Silicon - Atomic Number 14
        public Element P { get; private set; } // Phosphorus - Atomic Number 15
        public Element S { get; private set; } // Sulfur - Atomic Number 16
        public Element Cl { get; private set; } // Chlorine - Atomic Number 17
        public Element Ar { get; private set; } // Argon - Atomic Number 18
        public Element K { get; private set; } // Potassium - Atomic Number 19
        public Element Ca { get; private set; } // Calcium - Atomic Number 20
        public Element Sc { get; private set; } // Scandium - Atomic Number 21
        public Element Ti { get; private set; } // Titanium - Atomic Number 22
        public Element V { get; private set; } // Vanadium - Atomic Number 23
        public Element Cr { get; private set; } // Chromium - Atomic Number 24
        public Element Mn { get; private set; } // Manganese - Atomic Number 25
        public Element Fe { get; private set; } // Iron - Atomic Number 26
        public Element Co { get; private set; } // Cobalt - Atomic Number 27
        public Element Ni { get; private set; } // Nickel - Atomic Number 28
        public Element Cu { get; private set; } // Copper - Atomic Number 29
        public Element Zn { get; private set; } // Zinc - Atomic Number 30
        public Element Ga { get; private set; } // Gallium - Atomic Number 31
        public Element Ge { get; private set; } // Germanium - Atomic Number 32
        public Element As { get; private set; } // Arsenic - Atomic Number 33
        public Element Se { get; private set; } // Selenium - Atomic Number 34
        public Element Br { get; private set; } // Bromine - Atomic Number 35
        public Element Kr { get; private set; } // Krypton - Atomic Number 36
        public Element Rb { get; private set; } // Rubidium - Atomic Number 37
        public Element Sr { get; private set; } // Strontium - Atomic Number 38
        public Element Y { get; private set; } // Yttrium - Atomic Number 39
        public Element Zr { get; private set; } // Zirconium - Atomic Number 40
        public Element Nb { get; private set; } // Niobium - Atomic Number 41
        public Element Mo { get; private set; } // Molybdenum - Atomic Number 42
        public Element Tc { get; private set; } // Technetium - Atomic Number 43
        public Element Ru { get; private set; } // Ruthenium - Atomic Number 44
        public Element Rh { get; private set; } // Rhodium - Atomic Number 45
        public Element Pd { get; private set; } // Palladium - Atomic Number 46
        public Element Ag { get; private set; } // Silver - Atomic Number 47
        public Element Cd { get; private set; } // Cadmium - Atomic Number 48
        public Element In { get; private set; } // Indium - Atomic Number 49
        public Element Sn { get; private set; } // Tin - Atomic Number 50
        public Element Sb { get; private set; } // Antimony - Atomic Number 51
        public Element Te { get; private set; } // Tellurium - Atomic Number 52
        public Element I { get; private set; } // Iodine - Atomic Number 53
        public Element Xe { get; private set; } // Xenon - Atomic Number 54
        public Element Cs { get; private set; } // Cesium - Atomic Number 55
        public Element Ba { get; private set; } // Barium - Atomic Number 56
        public Element La { get; private set; } // Lanthanum - Atomic Number 57
        public Element Ce { get; private set; } // Cerium - Atomic Number 58
        public Element Pr { get; private set; } // Praseodymium - Atomic Number 59
        public Element Nd { get; private set; } // Neodymium - Atomic Number 60
        public Element Pm { get; private set; } // Promethium - Atomic Number 61
        public Element Sm { get; private set; } // Samarium - Atomic Number 62
        public Element Eu { get; private set; } // Europium - Atomic Number 63
        public Element Gd { get; private set; } // Gadolinium - Atomic Number 64
        public Element Tb { get; private set; } // Terbium - Atomic Number 65
        public Element Dy { get; private set; } // Dysprosium - Atomic Number 66
        public Element Ho { get; private set; } // Holmium - Atomic Number 67
        public Element Er { get; private set; } // Erbium - Atomic Number 68
        public Element Tm { get; private set; } // Thulium - Atomic Number 69
        public Element Yb { get; private set; } // Ytterbium - Atomic Number 70
        public Element Lu { get; private set; } // Lutetium - Atomic Number 71
        public Element Hf { get; private set; } // Hafnium - Atomic Number 72
        public Element Ta { get; private set; } // Tantalum - Atomic Number 73
        public Element W { get; private set; } // Tungsten - Atomic Number 74
        public Element Re { get; private set; } // Rhenium - Atomic Number 75
        public Element Os { get; private set; } // Osmium - Atomic Number 76
        public Element Ir { get; private set; } // Iridium - Atomic Number 77
        public Element Pt { get; private set; } // Platinum - Atomic Number 78
        public Element Au { get; private set; } // Gold - Atomic Number 79
        public Element Hg { get; private set; } // Mercury - Atomic Number 80
        public Element Tl { get; private set; } // Thallium - Atomic Number 81
        public Element Pb { get; private set; } // Lead - Atomic Number 82
        public Element Bi { get; private set; } // Bismuth - Atomic Number 83
        public Element Po { get; private set; } // Polonium - Atomic Number 84
        public Element At { get; private set; } // Astatine - Atomic Number 85
        public Element Rn { get; private set; } // Radon - Atomic Number 86
        public Element Fr { get; private set; } // Francium - Atomic Number 87
        public Element Ra { get; private set; } // Radium - Atomic Number 88
        public Element Ac { get; private set; } // Actinium - Atomic Number 89
        public Element Th { get; private set; } // Thorium - Atomic Number 90
        public Element Pa { get; private set; } // Protactinium - Atomic Number 91
        public Element U { get; private set; } // Uranium - Atomic Number 92
        public Element Np { get; private set; } // Neptunium - Atomic Number 93
        public Element Pu { get; private set; } // Plutonium - Atomic Number 94
        public Element Am { get; private set; } // Americium - Atomic Number 95
        public Element Cm { get; private set; } // Curium - Atomic Number 96
        public Element Bk { get; private set; } // Berkelium - Atomic Number 97
        public Element Cf { get; private set; } // Californium - Atomic Number 98
        public Element Es { get; private set; } // Einsteinium - Atomic Number 99
        public Element Fm { get; private set; } // Fermium - Atomic Number 100
        public Element Md { get; private set; } // Mendelevium - Atomic Number 101
        public Element No { get; private set; } // Nobelium - Atomic Number 102
        public Element Lr { get; private set; } // Lawrencium - Atomic Number 103
        public Element Rf { get; private set; } // Rutherfordium - Atomic Number 104
        public Element Db { get; private set; } // Dubnium - Atomic Number 105
        public Element Sg { get; private set; } // Seaborgium - Atomic Number 106
        public Element Bh { get; private set; } // Bohrium - Atomic Number 107
        public Element Hs { get; private set; } // Hassium - Atomic Number 108
        public Element Mt { get; private set; } // Meitnerium - Atomic Number 109
        public Element Ds { get; private set; } // Darmstadtium - Atomic Number 110
        public Element Rg { get; private set; } // Roentgenium - Atomic Number 111
        public Element Cn { get; private set; } // Copernicium - Atomic Number 112
        public Element Nh { get; private set; } // Nihonium - Atomic Number 113
        public Element Fl { get; private set; } // Flerovium - Atomic Number 114
        public Element Mc { get; private set; } // Moscovium - Atomic Number 115
        public Element Lv { get; private set; } // Livermorium - Atomic Number 116
        public Element Ts { get; private set; } // Tennessine - Atomic Number 117
        public Element Og { get; private set; } // Oganesson - Atomic Number 118

        #endregion Real Elements  - Code generated by PeriodicTable.xlsx

        public Dictionary<string, Element> Elements { get; private set; }

        public IEnumerable<Element> ElementsSource => Elements.Values.ToList();

        public string ValidElements { get; }

        public string ImplicitHydrogenTargets { get; }

        public PeriodicTable()
        {
            LoadFromCsv();
            ValidElements = Elements.Values.Select(e => e.Symbol).Aggregate((start, next) => start + "|" + next);
            ImplicitHydrogenTargets = string.Join(",", Elements.Values.Where(e => e.AddHydrogens).Select(e => e.Symbol));
        }

        public bool HasElement(string symbol) => Elements.ContainsKey(symbol);

        /// <summary>
        /// Calculates the number of spare valencies an atom has.
        /// Uses the obscure formula from the original C4W
        /// valence calculation routine
        /// </summary>
        /// <param name="element">Element of the atom</param>
        /// <param name="sumOfBondOrder">Total order of bonds involved</param>
        /// <param name="charge">Formal charge on atom</param>
        /// <returns>Integer: can be -ve, in which case atom is overbonded</returns>
        public int SpareValencies(Element element, int sumOfBondOrder, int charge)
        {
            int valence = GetValence(element, sumOfBondOrder);
            int diff = valence - sumOfBondOrder;

            if (charge > 0)
            {
                int vDiff = 4 - valence;
                if (charge < vDiff)
                {
                    diff += charge;
                }
                else
                {
                    diff = 4 - sumOfBondOrder - charge + vDiff;
                }
            }
            else
            {
                diff += charge;
            }

            return diff;
        }

        /// <summary>
        /// Returns the minimum possible valence that
        /// fits the bond order.
        /// If this fails, returns the maximum valence of the atom
        /// </summary>
        /// <param name="element"></param>
        /// <param name="sumOfBondOrder"></param>
        /// <returns></returns>
        private int GetValence(Element element, int sumOfBondOrder)
        {
            //find the first possible valence that accommodates the bond order sum
            foreach (int v in element.Valencies)
            {
                if (v >= sumOfBondOrder)
                {
                    return v;
                }
            }
            //if we've run out of valences that might cope, return the last one
            return element.Valencies.Last();
        }

        private object this[string propertyName]
        {
            set => GetType().GetProperty(propertyName)?.SetValue(this, value, null);
        }

        private void LoadFromCsv()
        {
            Elements = new Dictionary<string, Element>();

            string csv = ResourceHelper.GetStringResource(Assembly.GetExecutingAssembly(), "PeriodicTable.csv");

            string[] lines = csv.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                //Symbol,Name,AtomicNumber,AddH,Colour,CovalentRadius,VdWRadius,Valency,Mass,Valences,Isotopes,Group,Row
                string[] data = line.Split(',');

                try
                {
                    // Ignore header
                    if (data[0].Length <= 3)
                    {
                        Element x = new Element();

                        string symbol = data[0];

                        x.Symbol = symbol;
                        x.Name = data[1];
                        x.AtomicNumber = IntParser(data[2]);
                        x.AddHydrogens = BoolParser(data[3]);
                        x.Colour = string.IsNullOrEmpty(data[4]) ? "#000000" : data[4];
                        x.CovalentRadius = DoubleParser(data[5]);
                        x.VdWRadius = DoubleParser(data[6]);
                        x.Valency = IntParser(data[7]);
                        x.AtomicWeight = DoubleParser(data[8]);
                        x.Valencies = IntArrayFromString(data[9]);
                        x.IsotopeMasses = IntArrayFromString(data[10]);
                        x.Group = IntParser(data[11]);
                        x.Row = IntParser(data[12]);
                        x.PTRow = IntParser(data[13]);
                        x.PTColumn = IntParser(data[14]);
                        x.PTElementType = data[15];
                        x.ElectronicConfiguration = data[16];

                        Elements.Add(symbol, x);
                        this[symbol] = x;
                    }
                }
                catch (Exception ex)
                {
                    string message = $"Exception {ex.Message} setting properties of Element {data[0]}";
                    throw new NotImplementedException(message);
                }
            }
        }

        private static double DoubleParser(string input)
        {
            double result;
            double.TryParse(input, out result);
            return result;
        }

        private static bool BoolParser(string input)
        {
            bool result;
            bool.TryParse(input, out result);
            return result;
        }

        private static int IntParser(string input)
        {
            int result;
            int.TryParse(input, out result);
            return result;
        }

        private static int[] IntArrayFromString(string ints)
        {
            int[] result = null;

            if (!string.IsNullOrEmpty(ints))
            {
                string[] vv = ints.Split('|');
                result = new int[vv.Length];
                for (int i = 0; i < vv.Length; i++)
                {
                    result[i] = int.Parse(vv[i]);
                }
            }

            return result;
        }
    }
}