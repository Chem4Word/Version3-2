// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Chem4Word.Model2.Helpers;
using Newtonsoft.Json;

namespace Chem4Word.Model2.Converters.JSON
{
    /// <summary>
    /// Converts a Model from and to JSON
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class JSONConverter
    {
        private const string Protruding = "protruding";
        private const string Recessed = "recessed";
        private const string Ambiguous = "ambiguous";

        private class AtomJSON
        {
            public string i;    // Id
            public double x;
            public double y;
            public string l;    // element
            public int? c;      // charge
        }

        private class BondJSON
        {
            public string i;    // Id
            public int? b;      //start atom
            public int? e;      //end atom
            public double? o;   //order
            public string s;    //stereo
        }

        private class MolJSON
        {
            public AtomJSON[] a;
            public BondJSON[] b;
        }

        private class ModelJSON
        {
            public MolJSON[] m;
        }

        public string Description => "JSON Molecular Format";
        public string[] Extensions => new string[] { "*.json" };

        public string Export(Model model, bool compressed = false)
        {
            ModelJSON mdJson = new ModelJSON();
            model.Relabel(false);

            if (model.Molecules.Count == 1)
            {
                Molecule m1 = model.Molecules.Values.First();
                var mj = ExportMol(m1);
                return WriteJson(mj, compressed);
            }
            else if (model.Molecules.Count > 1)
            {
                mdJson.m = new MolJSON[model.Molecules.Count];
                int i = 0;
                foreach (Molecule mol in model.Molecules.Values)
                {
                    var mj = ExportMol(mol);
                    mdJson.m[i] = mj;
                    i++;
                }
            }

            return WriteJson(mdJson, compressed);
        }

        private static string WriteJson(object json, bool compressed) =>
            JsonConvert.SerializeObject(json,
                                        compressed ? Formatting.None : Formatting.Indented,
                                        new JsonSerializerSettings
                                        {
                                            DefaultValueHandling = DefaultValueHandling.Ignore
                                        });

        private static MolJSON ExportMol(Molecule m1)
        {
            MolJSON mj = new MolJSON();

            mj.a = new AtomJSON[m1.Atoms.Count];
            Dictionary<Atom, int> indexLookup = new Dictionary<Atom, int>();

            int iAtom = 0;
            foreach (Atom a in m1.Atoms.Values)
            {
                string elem = null;
                if (a.Element.Symbol != "C")
                {
                    if (a.Element is Element element)
                    {
                        elem = element.Symbol;
                    }

                    if (a.Element is FunctionalGroup functionalGroup)
                    {
                        elem = functionalGroup.Name;
                    }
                }
                mj.a[iAtom] = new AtomJSON()
                {
                    i = a.Id,
                    x = a.Position.X,
                    y = a.Position.Y,
                    l = elem
                };
                if (a.FormalCharge != null)
                {
                    mj.a[iAtom].c = a.FormalCharge.Value;
                }
                indexLookup[a] = iAtom;
                iAtom++;
            }

            int iBond = 0;
            if (m1.Bonds.Any())
            {
                mj.b = new BondJSON[m1.Bonds.Count];
                foreach (Bond bond in m1.Bonds)
                {
                    mj.b[iBond] = new BondJSON()
                    {
                        i = bond.Id,
                        b = indexLookup[bond.StartAtom],
                        e = indexLookup[bond.EndAtom]
                    };

                    if (bond.Stereo == Globals.BondStereo.Wedge)
                    {
                        mj.b[iBond].s = Protruding;
                    }
                    else if (bond.Stereo == Globals.BondStereo.Hatch)
                    {
                        mj.b[iBond].s = Recessed;
                    }
                    else if (bond.Stereo == Globals.BondStereo.Indeterminate)
                    {
                        mj.b[iBond].s = Ambiguous;
                    }
                    if (bond.Order != Globals.OrderSingle)
                    {
                        mj.b[iBond].o = bond.OrderValue;
                    }
                    iBond++;
                }
            }
            return mj;
        }

        public Model Import(object data)
        {
            var jsonModel = JsonConvert.DeserializeObject<ModelJSON>(data as string);

            var newModel = new Model();
            if (jsonModel.m != null)
            {
                foreach (var molJson in jsonModel.m)
                {
                    AddMolecule(molJson, newModel);
                }
            }
            else
            {
                var jsonMol = JsonConvert.DeserializeObject<MolJSON>(data as string);
                AddMolecule(jsonMol, newModel);
            }

            newModel.Relabel(true);
            newModel.Refresh();
            return newModel;
        }

        private static void AddMolecule(dynamic data, Model newModel)
        {
            Dictionary<int, string> atoms = new Dictionary<int, string>();
            var newMol = new Molecule();
            ElementBase ce = Globals.PeriodicTable.C;
            int atomCount = 0;

            // GitHub: Issue #13 https://github.com/Chem4Word/Version3/issues/13
            if (data.a != null)
            {
                foreach (AtomJSON a in data.a)
                {
                    if (!string.IsNullOrEmpty(a.l))
                    {
                        ElementBase eb;
                        var ok = AtomHelpers.TryParse(a.l, out eb);
                        if (ok)
                        {
                            if (eb is Element element)
                            {
                                ce = element;
                            }

                            if (eb is FunctionalGroup functionalGroup)
                            {
                                ce = functionalGroup;
                            }
                        }
                    }
                    else
                    {
                        ce = Globals.PeriodicTable.C;
                    }

                    Atom atom = new Atom()
                    {
                        Element = ce,
                        Position = new Point(a.x, a.y)
                    };

                    if (a.c != null)
                    {
                        atom.FormalCharge = a.c.Value;
                    }

                    atoms.Add(atomCount++, atom.InternalId);
                    newMol.AddAtom(atom);
                    atom.Parent = newMol;
                }
            }

            if (data.b != null)
            {
                foreach (BondJSON b in data.b)
                {
                    string o;
                    if (b.o != null)
                    {
                        o = Globals.OrderValueToOrder(double.Parse(b.o.ToString()));
                    }
                    else
                    {
                        o = Globals.OrderSingle;
                    }

                    Globals.BondStereo s;
                    if (!string.IsNullOrEmpty(b.s))
                    {
                        if (o == Globals.OrderDouble)
                        {
                            if (b.s.Equals(Ambiguous))
                            {
                                s = Globals.BondStereo.Indeterminate;
                            }
                            else
                            {
                                s = Globals.BondStereo.None;
                            }
                        }
                        else
                        {
                            if (b.s.Equals(Recessed))
                            {
                                s = Globals.BondStereo.Hatch;
                            }
                            else if (b.s.Equals(Protruding))
                            {
                                s = Globals.BondStereo.Wedge;
                            }
                            else if (b.s.Equals(Ambiguous))
                            {
                                s = Globals.BondStereo.Indeterminate;
                            }
                            else
                            {
                                s = Globals.BondStereo.None;
                            }
                        }
                    }
                    else
                    {
                        s = Globals.BondStereo.None;
                    }

                    // Azure DevOps #715
                    if (b.b.HasValue && b.b.Value < atoms.Count && b.e.HasValue && b.e.Value < atoms.Count)
                    {
                        var sa = atoms[b.b.Value];
                        var ea = atoms[b.e.Value];
                        Bond newBond = new Bond()
                        {
                            StartAtomInternalId = sa,
                            EndAtomInternalId = ea,
                            Stereo = s,
                            Order = o
                        };
                        newMol.AddBond(newBond);
                        newBond.Parent = newMol;
                    }
                }
            }

            newModel.AddMolecule(newMol);
            newMol.Parent = newModel;
        }

        public bool CanImport
        {
            get { return true; }
        }

        public bool CanExport
        {
            get { return true; }
        }
    }
}