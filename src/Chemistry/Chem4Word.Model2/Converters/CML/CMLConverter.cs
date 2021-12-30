﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Chem4Word.Model2.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Chem4Word.Model2.Converters.CML
{
    // ReSharper disable once InconsistentNaming
    public class CMLConverter
    {
        public Model Import(object data, List<string> protectedLabels = null, bool relabel = true)
        {
            Model newModel = new Model();

            if (data != null)
            {
                XDocument modelDoc = XDocument.Parse((string)data);
                var root = modelDoc.Root;

                // Only import if not null
                var customXmlPartGuid = CMLHelper.GetCustomXmlPartGuid(root);
                if (customXmlPartGuid != null && !string.IsNullOrEmpty(customXmlPartGuid.Value))
                {
                    newModel.CustomXmlPartGuid = customXmlPartGuid.Value;
                }

                var moleculeElements = CMLHelper.GetMolecules(root);

                foreach (XElement meElement in moleculeElements)
                {
                    var newMol = GetMolecule(meElement);

                    AddMolecule(newModel, newMol);
                    newMol.Parent = newModel;
                }
                var schemeElements = CMLHelper.GetReactionSchemes(root);
                foreach (XElement schemeElement in schemeElements)
                {
                    var newScheme = GetReactionScheme(schemeElement);
                    AddReactionScheme(newModel, newScheme);
                    newScheme.Parent = newModel;
                }

                #region Handle 1D Labels

                newModel.SetMissingIds();
                if (protectedLabels != null && protectedLabels.Count >= 1)
                {
                    newModel.SetProtectedLabels(protectedLabels);
                }
                else if (relabel)
                {
                    newModel.Relabel(true);
                }

                #endregion Handle 1D Labels

                // Calculate dynamic properties
                newModel.Refresh();
            }

            return newModel;
        }

        private void AddReactionScheme(Model newModel, ReactionScheme newScheme)
        {
            newModel.AddReactionScheme(newScheme);
        }

        public string Export(Model model, bool compressed = false, bool cmlIsRoot = true)
        {
            var rootIsCml = cmlIsRoot || model.Molecules.Count > 1;

            XDocument xd = new XDocument();

            XElement root = new XElement(CMLNamespaces.cml + (rootIsCml ? CMLConstants.TagCml : CMLConstants.TagMolecule));

            // Only export if set
            if (rootIsCml && !string.IsNullOrEmpty(model.CustomXmlPartGuid))
            {
                XElement customXmlPartGuid = new XElement(CMLNamespaces.c4w + CMLConstants.TagXmlPartGuid, model.CustomXmlPartGuid);
                root.Add(customXmlPartGuid);
            }

            RelabelIfRequired();

            if (rootIsCml)
            {
                foreach (Molecule molecule in model.Molecules.Values)
                {
                    root.Add(GetMoleculeElement(molecule));
                }
                foreach (ReactionScheme scheme in model.ReactionSchemes.Values)
                {
                    root.Add(GetXElement(scheme));
                }
            }
            else
            {
                root = GetMoleculeElement(model.Molecules.Values.First());
            }

            root.Add(new XAttribute(XNamespace.Xmlns + CMLConstants.TagConventions, CMLNamespaces.conventions));
            root.Add(new XAttribute(XNamespace.Xmlns + CMLConstants.TagCml, CMLNamespaces.cml));
            root.Add(new XAttribute(XNamespace.Xmlns + CMLConstants.TagCmlDict, CMLNamespaces.cmlDict));
            root.Add(new XAttribute(XNamespace.Xmlns + CMLConstants.TagNameDict, CMLNamespaces.nameDict));
            root.Add(new XAttribute(XNamespace.Xmlns + CMLConstants.TagC4W, CMLNamespaces.c4w));
            root.Add(new XAttribute(CMLConstants.TagConventions, CMLConstants.TagConventionMolecular));

            xd.Add(root);

            return compressed ? xd.ToString(SaveOptions.DisableFormatting) : xd.ToString();

            // Local Function
            void RelabelIfRequired()
            {
                bool relabelRequired = false;

                // Handle case where id's are null
                foreach (Molecule molecule in model.Molecules.Values)
                {
                    if (molecule.Id == null)
                    {
                        relabelRequired = true;
                        break;
                    }

                    foreach (Atom atom in molecule.Atoms.Values)
                    {
                        if (atom.Id == null)
                        {
                            relabelRequired = true;
                            break;
                        }
                    }

                    foreach (Bond bond in molecule.Bonds)
                    {
                        if (bond.Id == null)
                        {
                            relabelRequired = true;
                            break;
                        }
                    }
                }
                foreach (ReactionScheme scheme in model.ReactionSchemes.Values)
                {
                    if (scheme.Id == null)
                    {
                        relabelRequired = true;
                        break;
                    }
                    foreach (Reaction reaction in scheme.Reactions.Values)
                    {
                        if (reaction.Id == null)
                        {
                            relabelRequired = true;
                            break;
                        }
                    }
                }
                if (relabelRequired)
                {
                    model.Relabel(false);
                }
            }
        }

        #region Export Helpers

        // <cml:label id="m1.l1" dictRef="chem4word:Caption "value="C19"/>
        private XElement GetCaptionXElement(TextualProperty label)
        {
            XElement result = new XElement(CMLNamespaces.cml + CMLConstants.TagLabel);

            if (label.Id != null)
            {
                result.Add(new XAttribute(CMLConstants.AttributeId, label.Id));
            }

            result.Add(new XAttribute(CMLConstants.AttributeDictRef, CMLConstants.ValueChem4WordCaption));

            if (label.Value != null)
            {
                result.Add(new XAttribute(CMLConstants.AttributeNameValue, label.Value));
            }

            return result;
        }

        // <cml:name id="m1.n1" dictRef="chem4word:Synonym">m1.n1</cml:name>
        private XElement GetNameXElement(TextualProperty name)
        {
            XElement result = new XElement(CMLNamespaces.cml + CMLConstants.TagName, name.Value);

            if (name.Id != null)
            {
                result.Add(new XAttribute(CMLConstants.AttributeId, name.Id));
            }

            if (name.FullType != null)
            {
                result.Add(new XAttribute(CMLConstants.AttributeDictRef, name.FullType));
            }

            return result;
        }

        private XElement GetStereoXElement(Bond bond)
        {
            XElement result = null;

            if (bond.Stereo != Globals.BondStereo.None)
            {
                if (bond.Stereo == Globals.BondStereo.Cis || bond.Stereo == Globals.BondStereo.Trans)
                {
                    Atom firstAtom = bond.StartAtom;
                    Atom lastAtom = bond.EndAtom;

                    // Hack: [MAW] To find first and last atomRefs
                    foreach (var atomBond in bond.StartAtom.Bonds)
                    {
                        if (!bond.Id.Equals(atomBond.Id))
                        {
                            firstAtom = atomBond.OtherAtom(bond.StartAtom);
                            break;
                        }
                    }

                    foreach (var atomBond in bond.EndAtom.Bonds)
                    {
                        if (!bond.Id.Equals(atomBond.Id))
                        {
                            lastAtom = atomBond.OtherAtom(bond.EndAtom);
                            break;
                        }
                    }

                    result = new XElement(CMLNamespaces.cml + CMLConstants.TagBondStereo,
                        new XAttribute(CMLConstants.AttributeAtomRefs4,
                            $"{firstAtom.Id} {bond.StartAtom.Id} {bond.EndAtom.Id} {lastAtom.Id}"),
                        Globals.GetStereoString(bond.Stereo));
                }
                else
                {
                    result = new XElement(CMLNamespaces.cml + CMLConstants.TagBondStereo,
                        new XAttribute(CMLConstants.AttributeAtomRefs2, $"{bond.StartAtom.Id} {bond.EndAtom.Id}"),
                        Globals.GetStereoString(bond.Stereo));
                }
            }

            return result;
        }

        public XElement GetMoleculeElement(Molecule mol)
        {
            XElement molElement = new XElement(CMLNamespaces.cml + CMLConstants.TagMolecule, new XAttribute(CMLConstants.AttributeId, mol.Id));

            if (mol.ShowMoleculeBrackets != null)
            {
                molElement.Add(new XAttribute(CMLNamespaces.c4w + CMLConstants.AttributeShowMoleculeBrackets, mol.ShowMoleculeBrackets.Value));
            }

            if (mol.FormalCharge != null)
            {
                molElement.Add(new XAttribute(CMLConstants.AttributeFormalCharge, mol.FormalCharge.Value));
            }

            if (mol.SpinMultiplicity != null)
            {
                molElement.Add(new XAttribute(CMLConstants.AttributeSpinMultiplicity, mol.SpinMultiplicity.Value));
            }

            if (mol.Count != null)
            {
                molElement.Add(new XAttribute(CMLConstants.AttributeCount, mol.Count.Value));
            }

            if (mol.Molecules.Any())
            {
                foreach (var label in mol.Captions)
                {
                    molElement.Add(GetCaptionXElement(label));
                }

                foreach (var childMolecule in mol.Molecules.Values)
                {
                    molElement.Add(GetMoleculeElement(childMolecule));
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(mol.ConciseFormula))
                {
                    molElement.Add(GetXElement(mol.ConciseFormula, mol.Id));
                }

                foreach (var formula in mol.Formulas)
                {
                    molElement.Add(GetXElement(formula, mol.ConciseFormula));
                }

                foreach (var chemicalName in mol.Names)
                {
                    molElement.Add(GetNameXElement(chemicalName));
                }

                foreach (var label in mol.Captions)
                {
                    molElement.Add(GetCaptionXElement(label));
                }

                if (mol.Atoms.Count > 0)
                {
                    // Add atomArray element, then add atoms to it
                    XElement aaElement = new XElement(CMLNamespaces.cml + CMLConstants.TagAtomArray);
                    foreach (Atom atom in mol.Atoms.Values)
                    {
                        aaElement.Add(GetXElement(atom));
                    }
                    molElement.Add(aaElement);
                }

                if (mol.Bonds.Count > 0)
                {
                    // Add bondArray element, then add bonds to it
                    XElement baElement = new XElement(CMLNamespaces.cml + CMLConstants.TagBondArray);
                    foreach (Bond bond in mol.Bonds)
                    {
                        baElement.Add(GetXElement(bond));
                    }
                    molElement.Add(baElement);
                }
            }

            return molElement;
        }

        private XElement GetXElement(Bond bond)
        {
            XElement result = new XElement(CMLNamespaces.cml + CMLConstants.TagBond,
                new XAttribute(CMLConstants.AttributeId, bond.Id),
                new XAttribute(CMLConstants.AttributeAtomRefs2, $"{bond.StartAtom.Id} {bond.EndAtom.Id}"),
                new XAttribute(CMLConstants.AttributeOrder, bond.Order),
                GetStereoXElement(bond));

            if (bond.ExplicitPlacement != null)
            {
                result.Add(new XAttribute(CMLNamespaces.c4w + CMLConstants.AttributePlacement, bond.ExplicitPlacement));
            }

            return result;
        }

        private XElement GetXElement(ReactionScheme scheme)
        {
            XElement rsElement = new XElement(CMLNamespaces.cml + CMLConstants.TagReactionScheme,
                new XAttribute(CMLConstants.AttributeId, scheme.Id));
            if (scheme.Reactions.Any())
            {
                foreach (Reaction reaction in scheme.Reactions.Values)
                {
                    rsElement.Add(GetXElement(reaction));
                }
            }
            return rsElement;
        }

        private XElement GetXElement(Reaction reaction)
        {
            XElement reactionElement = new XElement(CMLNamespaces.cml + CMLConstants.TagReaction,
                new XAttribute(CMLConstants.AttributeId, reaction.Id),
                new XAttribute(CMLNamespaces.c4w + CMLConstants.AttributeArrowHead, PointHelper.AsCMLString(reaction.HeadPoint)),
                new XAttribute(CMLNamespaces.c4w + CMLConstants.AttributeArrowTail, PointHelper.AsCMLString(reaction.TailPoint)));

            switch (reaction.ReactionType)
            {
                case Globals.ReactionType.Normal:
                    break;

                case Globals.ReactionType.Reversible:
                    reactionElement.Add(new XAttribute(CMLNamespaces.cml + CMLConstants.AttributeReactionType, CMLConstants.AttrValueReversible));
                    break;

                case Globals.ReactionType.ReversibleBiasedForward:
                    reactionElement.Add(new XAttribute(CMLNamespaces.cml + CMLConstants.AttributeReactionType, CMLConstants.AttrValueReversible));
                    reactionElement.Add(new XAttribute(CMLNamespaces.cml + CMLConstants.AttributeReactionBias, CMLConstants.AttrValueBiasForward));
                    break;

                case Globals.ReactionType.ReversibleBiasedReverse:
                    reactionElement.Add(new XAttribute(CMLNamespaces.cml + CMLConstants.AttributeReactionType, CMLConstants.AttrValueReversible));
                    reactionElement.Add(new XAttribute(CMLNamespaces.cml + CMLConstants.AttributeReactionBias, CMLConstants.AttrValueBiasReverse));
                    break;

                case Globals.ReactionType.Blocked:
                    reactionElement.Add(new XAttribute(CMLNamespaces.cml + CMLConstants.AttributeReactionType, CMLConstants.AttrValueBlocked));
                    break;
                 case Globals.ReactionType.Resonance:
                    reactionElement.Add(new XAttribute(CMLNamespaces.cml + CMLConstants.AttributeReactionType, CMLConstants.AttrValueResonance));
                    break;
            }

            if(!string.IsNullOrEmpty(reaction.ReagentText))
            {
                XElement reagentText = new XElement(CMLNamespaces.c4w + CMLConstants.TagReagentText);
                XElement reagentTextElement = XElement.Parse(reaction.ReagentText);
                reagentText.Add(reagentTextElement);
                reactionElement.Add(reagentText);
            }
             if(!string.IsNullOrEmpty(reaction.ConditionsText))
            {
                XElement conditionsText = new XElement(CMLNamespaces.c4w + CMLConstants.TagConditionsText);
                 XElement conditionsTextElement = XElement.Parse(reaction.ConditionsText);
                conditionsText.Add(conditionsTextElement);
                reactionElement.Add(conditionsText);
            }
            return reactionElement;
        }

        private XElement GetXElement(Atom atom)
        {
            var elementType = "";
            if (atom.Element is Element element)
            {
                elementType = element.Symbol;
            }

            if (atom.Element is FunctionalGroup functionalGroup)
            {
                elementType = functionalGroup.Name;
            }

            XElement result = new XElement(CMLNamespaces.cml + CMLConstants.TagAtom,
                new XAttribute(CMLConstants.AttributeId, atom.Id),
                new XAttribute(CMLConstants.AttributeElementType, elementType),
                // We are writing co-ordinates with 4 decimal places to be consistent with industry standard MDL format
                new XAttribute(CMLConstants.AttributeX2, atom.Position.X.ToString("0.0###", CultureInfo.InvariantCulture)),
                new XAttribute(CMLConstants.AttributeY2, atom.Position.Y.ToString("0.0###", CultureInfo.InvariantCulture))
            );

            if (atom.FormalCharge != null && atom.FormalCharge.Value != 0)
            {
                result.Add(new XAttribute(CMLConstants.AttributeFormalCharge, atom.FormalCharge.Value));
            }

            if (atom.IsotopeNumber != null && atom.IsotopeNumber.Value != 0)
            {
                result.Add(new XAttribute(CMLConstants.AttributeIsotopeNumber, atom.IsotopeNumber.Value));
            }

            if (atom.Element is Element element2
                && element2 == Globals.PeriodicTable.C
                && atom.ExplicitC != null)
            {
                result.Add(new XAttribute(CMLNamespaces.c4w + CMLConstants.AttributeExplicit, atom.ExplicitC));
            }

            if (atom.Element is Element && atom.ExplicitHPlacement != null)
            {
                result.Add(new XAttribute(CMLNamespaces.c4w + CMLConstants.AttributeHydrogenPlacement, atom.ExplicitHPlacement));
            }

            if (atom.Element is FunctionalGroup && atom.ExplicitFunctionalGroupPlacement != null)
            {
                result.Add(new XAttribute(CMLNamespaces.c4w + CMLConstants.AttributeFunctionalGroupPlacement, atom.ExplicitFunctionalGroupPlacement));
            }
            return result;
        }

        // <cml:formula id="m1.f1" convention="chemspider:Smiles" inline="m1.f1" concise="C 6 H 14 Li 1 N 1" />
        private XElement GetXElement(TextualProperty f, string concise)
        {
            XElement result = new XElement(CMLNamespaces.cml + CMLConstants.TagFormula);

            if (f.Id != null)
            {
                result.Add(new XAttribute(CMLConstants.AttributeId, f.Id));
            }

            if (f.FullType != null)
            {
                result.Add(new XAttribute(CMLConstants.AttributeConvention, f.FullType));
            }

            if (f.Value != null)
            {
                result.Add(new XAttribute(CMLConstants.AttributeInline, f.Value));
            }

            if (concise != null)
            {
                result.Add(new XAttribute(CMLConstants.AttributeConcise, concise));
            }

            return result;
        }

        // <cml:formula id="m1.f0" concise="C 6 H 14 Li 1 N 1" />
        private XElement GetXElement(string concise, string molId)
        {
            XElement result = new XElement(CMLNamespaces.cml + CMLConstants.TagFormula);

            if (concise != null)
            {
                result.Add(new XAttribute(CMLConstants.AttributeId, $"{molId}.f0"));
                result.Add(new XAttribute(CMLConstants.AttributeConcise, concise));
            }

            return result;
        }

        #endregion Export Helpers

        #region Import Helpers

        private static void AddMolecule(Model newModel, Molecule newMol)
        {
            newModel.AddMolecule(newMol);
        }

        public static Molecule GetMolecule(XElement cmlElement)
        {
            Molecule molecule = new Molecule();

            string showBracketsValue = cmlElement.Attribute(CMLNamespaces.c4w + CMLConstants.AttributeShowMoleculeBrackets)?.Value;
            if (!string.IsNullOrEmpty(showBracketsValue))
            {
                molecule.ShowMoleculeBrackets = bool.Parse(showBracketsValue);
            }

            string idValue = cmlElement.Attribute(CMLConstants.AttributeId)?.Value;
            if (!string.IsNullOrEmpty(idValue))
            {
                molecule.Id = idValue;
            }

            string countValue = cmlElement.Attribute(CMLConstants.AttributeCount)?.Value;
            if (!string.IsNullOrEmpty(countValue))
            {
                molecule.Count = int.Parse(countValue);
            }

            string chargeValue = cmlElement.Attribute(CMLConstants.AttributeFormalCharge)?.Value;
            if (!string.IsNullOrEmpty(chargeValue))
            {
                molecule.FormalCharge = int.Parse(chargeValue);
            }

            string spinValue = cmlElement.Attribute(CMLConstants.AttributeSpinMultiplicity)?.Value;
            if (!string.IsNullOrEmpty(spinValue))
            {
                molecule.SpinMultiplicity = int.Parse(spinValue);
            }

            molecule.Errors = new List<string>();
            molecule.Warnings = new List<string>();

            // Task 736 - Handle ChemDraw 19.1 cml variant
            List<XElement> childMolecules = new List<XElement>();
            if (cmlElement.Document.Root.Name.LocalName != CMLConstants.TagMolecule)
            {
                childMolecules = CMLHelper.GetMolecules(cmlElement);
            }

            List<XElement> atomElements = CMLHelper.GetAtoms(cmlElement);
            List<XElement> bondElements = CMLHelper.GetBonds(cmlElement);
            List<XElement> nameElements = CMLHelper.GetNames(cmlElement);
            List<XElement> formulaElements = CMLHelper.GetFormulas(cmlElement);
            List<XElement> labelElements = CMLHelper.GetMoleculeLabels(cmlElement);

            foreach (XElement childElement in childMolecules)
            {
                Molecule newMol = GetMolecule(childElement);
                molecule.AddMolecule(newMol);
                newMol.Parent = molecule;
            }

            Dictionary<string, Guid> reverseAtomLookup = new Dictionary<string, Guid>();
            foreach (XElement atomElement in atomElements)
            {
                Atom newAtom = GetAtom(atomElement);
                if (newAtom.Messages.Count > 0)
                {
                    molecule.Errors.AddRange(newAtom.Messages);
                }

                molecule.AddAtom(newAtom);
                reverseAtomLookup[newAtom.Id] = newAtom.InternalId;
                newAtom.Parent = molecule;
            }

            foreach (XElement bondElement in bondElements)
            {
                Bond newBond = GetBond(bondElement, reverseAtomLookup);

                if (newBond.Messages.Count > 0)
                {
                    molecule.Errors.AddRange(newBond.Messages);
                }

                molecule.AddBond(newBond);
                newBond.Parent = molecule;
            }

            foreach (XElement formulaElement in formulaElements)
            {
                var formula = GetFormula(formulaElement);
                if (formula.IsValid)
                {
                    molecule.Formulas.Add(formula);
                }
            }

            foreach (XElement nameElement in nameElements)
            {
                var name = GetName(nameElement);
                if (name.IsValid)
                {
                    molecule.Names.Add(name);
                }
            }

            Molecule copy = molecule.Copy();
            copy.SplitIntoChildren();

            // If copy now contains (child) molecules, replace original
            if (copy.Molecules.Count > 1)
            {
                molecule = copy;
            }

            foreach (var labelElement in labelElements)
            {
                var label = GetCaption(labelElement);
                if (label != null && label.IsValid)
                {
                    molecule.Captions.Add(label);
                }
            }

            // Fix Invalid data; If only one atom
            if (molecule.Atoms.Count == 1)
            {
                // Remove ExplicitC flag
                molecule.Atoms.First().Value.ExplicitC = null;

                // Remove invalid molecule properties
                molecule.ShowMoleculeBrackets = null;
                molecule.SpinMultiplicity = null;
                molecule.FormalCharge = null;
                molecule.Count = null;
            }

            molecule.RebuildRings();

            return molecule;
        }

        private static Atom GetAtom(XElement cmlElement)
        {
            Atom atom = new Atom();

            atom.Messages = new List<string>();
            string message = "";
            string atomLabel = cmlElement.Attribute(CMLConstants.AttributeId)?.Value;

            Point p = CMLHelper.GetPosn(cmlElement, out message);
            if (!string.IsNullOrEmpty(message))
            {
                atom.Messages.Add(message);
            }

            atom.Id = atomLabel;
            atom.Position = p;

            ElementBase e = CMLHelper.GetElementOrFunctionalGroup(cmlElement, out message);
            if (!string.IsNullOrEmpty(message))
            {
                atom.Messages.Add(message);
            }

            if (e != null)
            {
                atom.Element = e;
                atom.FormalCharge = CMLHelper.GetFormalCharge(cmlElement);
                atom.IsotopeNumber = CMLHelper.GetIsotopeNumber(cmlElement);
                atom.ExplicitC = CMLHelper.GetExplicit(cmlElement);
                atom.ExplicitHPlacement = CMLHelper.GetExplicitHPlacement(cmlElement);
                atom.ExplicitFunctionalGroupPlacement = CMLHelper.GetExplicitGroupPlacement(cmlElement);
            }

            return atom;
        }

        private static Bond GetBond(XElement cmlElement, Dictionary<string, Guid> reverseAtomLookup)
        {
            Bond bond = new Bond();

            string[] atomRefs = cmlElement.Attribute(CMLConstants.AttributeAtomRefs2)?.Value.Split(' ');
            if (atomRefs?.Length == 2)
            {
                bond.StartAtomInternalId = reverseAtomLookup[atomRefs[0]];
                bond.EndAtomInternalId = reverseAtomLookup[atomRefs[1]];
            }
            var bondRef = cmlElement.Attribute(CMLConstants.AttributeId)?.Value;
            bond.Id = bondRef ?? bond.Id;
            bond.Order = cmlElement.Attribute(CMLConstants.AttributeOrder)?.Value;

            var stereoElems = CMLHelper.GetStereo(cmlElement);

            if (stereoElems.Any())
            {
                var stereo = stereoElems[0].Value;

                bond.Stereo = Globals.StereoFromString(stereo);
            }
            Globals.BondDirection? dir = null;

            var dirAttr = cmlElement.Attribute(CMLNamespaces.c4w + CMLConstants.AttributePlacement);
            if (dirAttr != null)
            {
                Globals.BondDirection temp;

                if (Enum.TryParse(dirAttr.Value, out temp))
                {
                    dir = temp;
                }
            }

            if (dir != null)
            {
                bond.Placement = dir.Value;
            }

            return bond;
        }

        private static ReactionScheme GetReactionScheme(XElement cmlElement)
        {
            ReactionScheme scheme = new ReactionScheme();
            string idValue = cmlElement.Attribute(CMLConstants.AttributeId)?.Value;
            if (!string.IsNullOrEmpty(idValue))
            {
                scheme.Id = idValue;
            }
            List<XElement> reactionElements = CMLHelper.GetReactions(cmlElement);
            foreach (XElement reactionElement in reactionElements)
            {
                Reaction newReaction = GetReaction(reactionElement);
                scheme.AddReaction(newReaction);
                newReaction.Parent = scheme;
            }
            return scheme;
        }

        private static Reaction GetReaction(XElement cmlElement)
        {
            Reaction reaction = new Reaction();
            string idValue = cmlElement.Attribute(CMLConstants.AttributeId)?.Value;
            if (!string.IsNullOrEmpty(idValue))
            {
                reaction.Id = idValue;
            }

            string arrowTailValue = cmlElement.Attribute(CMLNamespaces.c4w + CMLConstants.AttributeArrowTail)?.Value;
            if (!string.IsNullOrEmpty(arrowTailValue))
            {
                reaction.TailPoint = Point.Parse(arrowTailValue);
            }

            string arrowHeadValue = cmlElement.Attribute(CMLNamespaces.c4w + CMLConstants.AttributeArrowHead)?.Value;
            if (!string.IsNullOrEmpty(arrowHeadValue))
            {
                reaction.HeadPoint = Point.Parse(arrowHeadValue);
            }

            string reactionTypeValue = cmlElement.Attribute(CMLNamespaces.cml + CMLConstants.AttributeReactionType)?.Value;

            reaction.ReactionType = Globals.ReactionType.Normal;

            if (!string.IsNullOrEmpty(reactionTypeValue))
            {
                switch (reactionTypeValue)
                {
                    case CMLConstants.AttrValueBlocked:
                        reaction.ReactionType = Globals.ReactionType.Blocked;
                        break;

                    case CMLConstants.AttrValueReversible:
                        reaction.ReactionType = Globals.ReactionType.Reversible;
                        {
                            string bias = cmlElement.Attribute(CMLNamespaces.cml + CMLConstants.AttributeReactionBias)?.Value;
                            if (!string.IsNullOrEmpty(bias))
                            {
                                if (bias == CMLConstants.AttrValueBiasForward)
                                {
                                    reaction.ReactionType = Globals.ReactionType.ReversibleBiasedForward;
                                }
                                else if (bias == CMLConstants.AttrValueBiasReverse)
                                {
                                    reaction.ReactionType = Globals.ReactionType.ReversibleBiasedReverse;
                                }
                            }
                        }
                        break;
                    case CMLConstants.AttrValueResonance:
                        reaction.ReactionType = Globals.ReactionType.Resonance;
                        break;
                }
            }

            XElement reagentElement = cmlElement.Element(CMLNamespaces.c4w + CMLConstants.TagReagentText);
            string reagentText = reagentElement.ToString();
            if(!string.IsNullOrEmpty(reagentText))
            {
                reaction.ReagentText = reagentElement.CreateNavigator().InnerXml;
            }

            XElement conditionsElement = cmlElement.Element(CMLNamespaces.c4w + CMLConstants.TagConditionsText);
            string conditionsText = conditionsElement.ToString();
            if(!string.IsNullOrEmpty(reagentText))
            {
                reaction.ConditionsText = conditionsElement.CreateNavigator().InnerXml;
            }
            return reaction;
        }

        // <cml:formula id="m1.f1" convention="chemspider:Smiles" inline="m1.f1" concise="C 6 H 14 Li 1 N 1" />
        // <cml:formula id="m1.f0" concise="C 6 H 14 Li 1 N 1" />
        private static TextualProperty GetFormula(XElement cmlElement)
        {
            var formula = new TextualProperty();

            if (cmlElement.Attribute(CMLConstants.AttributeId) != null)
            {
                formula.Id = cmlElement.Attribute(CMLConstants.AttributeId)?.Value;
            }

            if (cmlElement.Attribute(CMLConstants.AttributeConvention) == null)
            {
                formula.FullType = CMLConstants.ValueChem4WordFormula;
            }
            else
            {
                formula.FullType = cmlElement.Attribute(CMLConstants.AttributeConvention)?.Value;
            }

            // Correct import from legacy Add-In
            if (string.IsNullOrEmpty(formula.FullType))
            {
                formula.FullType = CMLConstants.ValueChem4WordFormula;
            }

            if (cmlElement.Attribute(CMLConstants.AttributeInline) != null)
            {
                formula.Value = cmlElement.Attribute(CMLConstants.AttributeInline)?.Value;
            }

            return formula;
        }

        // <cml:label id="" dictRef="chem4word:Caption" value="C19 />
        private static TextualProperty GetCaption(XElement cmlElement)
        {
            if (cmlElement.Attribute(CMLConstants.AttributeDictRef) != null)
            {
                var dictrefValue = cmlElement.Attribute(CMLConstants.AttributeDictRef)?.Value;
                if (dictrefValue != null && dictrefValue.Equals(CMLConstants.ValueChem4WordCaption))
                {
                    var result = new TextualProperty();

                    result.FullType = CMLConstants.ValueChem4WordCaption;

                    if (cmlElement.Attribute(CMLConstants.AttributeId) != null)
                    {
                        result.Id = cmlElement.Attribute(CMLConstants.AttributeId)?.Value;
                    }

                    if (cmlElement.Attribute(CMLConstants.AttributeNameValue) != null)
                    {
                        result.Value = cmlElement.Attribute(CMLConstants.AttributeNameValue)?.Value;
                    }
                    result.CanBeDeleted = true;

                    return result;
                }
            }

            return null;
        }

        // <cml:name id="m1.n1" dictRef="chem4word:Synonym">m1.n1</cml:name>
        private static TextualProperty GetName(XElement cmlElement)
        {
            var name = new TextualProperty();

            name.Id = cmlElement.Attribute(CMLConstants.AttributeId)?.Value;

            if (cmlElement.Attribute(CMLConstants.AttributeDictRef) == null)
            {
                name.FullType = CMLConstants.ValueChem4WordSynonym;
            }
            else
            {
                name.FullType = cmlElement.Attribute(CMLConstants.AttributeDictRef)?.Value;
            }

            // Correct import from legacy Add-In
            if (string.IsNullOrEmpty(name.FullType) || name.FullType.Equals(CMLConstants.ValueNameDictUnknown))
            {
                name.FullType = CMLConstants.ValueChem4WordSynonym;
            }

            name.Value = cmlElement.Value;

            return name;
        }
    }

    #endregion Import Helpers
}