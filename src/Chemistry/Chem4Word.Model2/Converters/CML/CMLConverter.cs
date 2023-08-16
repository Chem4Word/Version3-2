// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Chem4Word.Model2.Enums;
using Chem4Word.Model2.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
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
            var newModel = new Model();

            if (data != null)
            {
                var cml = (string)data;
                if (cml.Contains("http://www.chemaxon.com") || cml.Contains("<MDocument>"))
                {
                    // Strip all xml namespaces to make it easier to process
                    cml = RemoveNamespaces(cml);

                    var temp = XDocument.Parse(cml);

                    var mol = temp.Descendants(CMLConstants.TagMolecule).FirstOrDefault();

                    if (mol != null)
                    {
                        // Make cml the same as if it has come from ChemDraw
                        cml = mol.ToString();
                    }
                }

                XDocument modelDoc = XDocument.Parse(cml);
                XElement root = modelDoc.Root;

                // Only import if not null
                XElement customXmlPartGuid = CMLHelper.GetCustomXmlPartGuid(root);
                if (customXmlPartGuid != null && !string.IsNullOrEmpty(customXmlPartGuid.Value))
                {
                    newModel.CustomXmlPartGuid = customXmlPartGuid.Value;
                }

                List<XElement> moleculeElements = CMLHelper.GetMolecules(root);

                foreach (XElement meElement in moleculeElements)
                {
                    Molecule newMol = GetMolecule(meElement);

                    AddMolecule(newModel, newMol);
                    newMol.Parent = newModel;
                }

                List<XElement> schemeElements = CMLHelper.GetReactionSchemes(root);
                foreach (XElement schemeElement in schemeElements)
                {
                    ReactionScheme newScheme = GetReactionScheme(schemeElement, newModel);
                    AddReactionScheme(newModel, newScheme);
                    newScheme.Parent = newModel;
                }

                //load any model-level annotations
                List<XElement> annotationElements = CMLHelper.GetAnnotations(root);
                foreach (XElement annElement in annotationElements)
                {
                    Annotation newAnnotation = GetAnnotation(annElement);
                    AddAnnotation(newModel, newAnnotation);
                    newAnnotation.Parent = newModel;
                }

                #region Handle 1D Labels

                if (protectedLabels != null && protectedLabels.Count >= 1)
                {
                    newModel.Relabel(false);
                    newModel.SetAnyMissingNameIds();
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

        private string RemoveNamespaces(string xml)
        {
            // From https://stackoverflow.com/questions/987135/how-to-remove-all-namespaces-from-xml-with-c
            return Regex.Replace(xml, "((?<=<|<\\/)|(?<= ))[A-Za-z0-9]+:| xmlns(:[A-Za-z0-9]+)?=\".*?\"", "");
        }

        private void AddAnnotation(Model newModel, Annotation newAnnotation)
        {
            newModel.AddAnnotation(newAnnotation);
        }

        private Annotation GetAnnotation(XElement cmlElement)
        {
            var newAnnotation = new Annotation();

            string xaml = cmlElement.CreateNavigator().InnerXml;
            if (!string.IsNullOrEmpty(xaml))
            {
                newAnnotation.Xaml = xaml;
            }

            string idValue = cmlElement.Attribute(CMLConstants.AttributeId)?.Value;
            if (!string.IsNullOrEmpty(idValue))
            {
                newAnnotation.Id = idValue;
            }

            newAnnotation.Position = CMLHelper.GetPosn(cmlElement, out _);

            string symbolSize = cmlElement.Attribute(name: CMLConstants.AttributeSymbolSize)?.Value;
            if (!string.IsNullOrEmpty(symbolSize))
            {
                newAnnotation.SymbolSize = double.Parse(symbolSize);
            }

            return newAnnotation;
        }

        private void AddReactionScheme(Model newModel, ReactionScheme newScheme)
        {
            newModel.AddReactionScheme(newScheme);
        }

        public string Export(Model model, bool compressed = false, CmlFormat format = CmlFormat.Default)
        {
            RelabelIfRequired();

            var selectedFormat = model.Molecules.Count > 1
                ? CmlFormat.Default :
                format;

            var xd = new XDocument();
            XElement root = null;

            switch (selectedFormat)
            {
                case CmlFormat.Default:
                    root = new XElement(CMLNamespaces.cml + CMLConstants.TagCml);

                    // Only export if set and format is default
                    if (!string.IsNullOrEmpty(model.CustomXmlPartGuid))
                    {
                        var customXmlPartGuid = new XElement(CMLNamespaces.c4w + CMLConstants.TagXmlPartGuid, model.CustomXmlPartGuid);
                        root.Add(customXmlPartGuid);
                    }

                    // Build document
                    foreach (var molecule in model.Molecules.Values)
                    {
                        root.Add(GetMoleculeElement(molecule));
                    }
                    foreach (var scheme in model.ReactionSchemes.Values)
                    {
                        root.Add(GetXElement(scheme));
                    }
                    foreach (var annotation in model.Annotations.Values)
                    {
                        root.Add(GetXElement(annotation));
                    }

                    // Add namespaces etc
                    root.Add(new XAttribute(XNamespace.Xmlns + CMLConstants.TagConventions, CMLNamespaces.conventions));
                    root.Add(new XAttribute(XNamespace.Xmlns + CMLConstants.TagCml, CMLNamespaces.cml));
                    root.Add(new XAttribute(XNamespace.Xmlns + CMLConstants.TagCmlDict, CMLNamespaces.cmlDict));
                    root.Add(new XAttribute(XNamespace.Xmlns + CMLConstants.TagNameDict, CMLNamespaces.nameDict));
                    root.Add(new XAttribute(XNamespace.Xmlns + CMLConstants.TagC4W, CMLNamespaces.c4w));
                    root.Add(new XAttribute(CMLConstants.TagConventions, CMLConstants.TagConventionMolecular));

                    break;

                case CmlFormat.ChemDraw:
                    // Build document
                    root = GetMoleculeElement(model.Molecules.Values.First(), format);

                    // Add namespaces etc
                    root.Add(new XAttribute("xmlns", CMLNamespaces.cml));

                    break;

                case CmlFormat.MarvinJs:
                    root = new XElement(CMLNamespaces.chemaxion + CMLConstants.TagCml);

                    // Build document structure
                    var mDocument = new XElement(CMLNamespaces.chemaxion + "MDocument");
                    root.Add(mDocument);
                    var mChemicalStructure = new XElement(CMLNamespaces.chemaxion + "MChemicalStruct");
                    mDocument.Add(mChemicalStructure);

                    // Import the molecule
                    mChemicalStructure.Add(GetMoleculeElement(model.Molecules.Values.First(), format));

                    // Correct the namespaces of the imported molecule
                    foreach (var element in mChemicalStructure.Descendants())
                    {
                        element.Name = CMLNamespaces.chemaxion + element.Name.LocalName;
                    }

                    // Add namespaces etc
                    root.Add(new XAttribute("xmlns", CMLNamespaces.chemaxion));
                    root.Add(new XAttribute("version", "ChemAxon file format v20.20.0, generated by vunknown"));

                    break;
            }

            if (root != null)
            {
                xd.Add(root);
            }

            return compressed ? xd.ToString(SaveOptions.DisableFormatting) : xd.ToString();

            // Local Function
            void RelabelIfRequired()
            {
                var relabelRequired = false;

                // Handle case where id's are null
                foreach (Molecule molecule in model.Molecules.Values)
                {
                    if (string.IsNullOrEmpty(molecule.Id))
                    {
                        relabelRequired = true;
                        break;
                    }

                    foreach (Atom atom in molecule.Atoms.Values)
                    {
                        if (string.IsNullOrEmpty(atom.Id))
                        {
                            relabelRequired = true;
                            break;
                        }
                    }

                    foreach (Bond bond in molecule.Bonds)
                    {
                        if (string.IsNullOrEmpty(bond.Id))
                        {
                            relabelRequired = true;
                            break;
                        }
                    }
                }

                foreach (ReactionScheme scheme in model.ReactionSchemes.Values)
                {
                    if (string.IsNullOrEmpty(scheme.Id))
                    {
                        relabelRequired = true;
                        break;
                    }

                    foreach (Reaction reaction in scheme.Reactions.Values)
                    {
                        if (string.IsNullOrEmpty(reaction.Id))
                        {
                            relabelRequired = true;
                            break;
                        }
                    }
                }

                foreach (Annotation annotation in model.Annotations.Values)
                {
                    if (string.IsNullOrEmpty(annotation.Id))
                    {
                        relabelRequired = true;
                        break;
                    }
                }

                // Finally do the re label if required
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
            var result = new XElement(CMLNamespaces.cml + CMLConstants.TagLabel);

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
            var result = new XElement(CMLNamespaces.cml + CMLConstants.TagName, name.Value);

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

            if (bond.Stereo != BondStereo.None)
            {
                if (bond.Stereo == BondStereo.Cis
                    || bond.Stereo == BondStereo.Trans)
                {
                    Atom firstAtom = bond.StartAtom;
                    Atom lastAtom = bond.EndAtom;

                    // Hack: [MAW] To find first and last atomRefs
                    foreach (Bond atomBond in bond.StartAtom.Bonds)
                    {
                        if (!bond.Id.Equals(atomBond.Id))
                        {
                            firstAtom = atomBond.OtherAtom(bond.StartAtom);
                            break;
                        }
                    }

                    foreach (Bond atomBond in bond.EndAtom.Bonds)
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
                        Bond.GetStereoString(bond.Stereo));
                }
                else
                {
                    result = new XElement(CMLNamespaces.cml + CMLConstants.TagBondStereo,
                        new XAttribute(CMLConstants.AttributeAtomRefs2, $"{bond.StartAtom.Id} {bond.EndAtom.Id}"),
                        Bond.GetStereoString(bond.Stereo));
                }
            }

            return result;
        }

        private XElement GetMoleculeElement(Molecule mol, CmlFormat format = CmlFormat.Default)
        {
            var molElement = new XElement(CMLNamespaces.cml + CMLConstants.TagMolecule, new XAttribute(CMLConstants.AttributeId, mol.Id));

            if (format == CmlFormat.Default)
            {
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
            }

            if (mol.Molecules.Any())
            {
                if (format == CmlFormat.Default)
                {
                    foreach (TextualProperty label in mol.Captions)
                    {
                        molElement.Add(GetCaptionXElement(label));
                    }
                }

                foreach (Molecule childMolecule in mol.Molecules.Values)
                {
                    molElement.Add(GetMoleculeElement(childMolecule));
                }
            }
            else
            {
                if (format == CmlFormat.Default)
                {
                    if (!string.IsNullOrEmpty(mol.ConciseFormula))
                    {
                        molElement.Add(GetXElement(mol.ConciseFormula, mol.Id));
                    }

                    foreach (TextualProperty formula in mol.Formulas)
                    {
                        molElement.Add(GetXElement(formula, mol.ConciseFormula));
                    }

                    foreach (TextualProperty chemicalName in mol.Names)
                    {
                        molElement.Add(GetNameXElement(chemicalName));
                    }

                    foreach (TextualProperty label in mol.Captions)
                    {
                        molElement.Add(GetCaptionXElement(label));
                    }
                }

                if (mol.Atoms.Count > 0)
                {
                    // Add atomArray element, then add atoms to it
                    var aaElement = new XElement(CMLNamespaces.cml + CMLConstants.TagAtomArray);
                    foreach (Atom atom in mol.Atoms.Values)
                    {
                        aaElement.Add(GetXElement(atom));
                    }
                    molElement.Add(aaElement);
                }

                if (mol.Bonds.Count > 0)
                {
                    // Add bondArray element, then add bonds to it
                    var baElement = new XElement(CMLNamespaces.cml + CMLConstants.TagBondArray);
                    foreach (Bond bond in mol.Bonds)
                    {
                        baElement.Add(GetXElement(bond, format));
                    }
                    molElement.Add(baElement);
                }
            }

            return molElement;
        }

        private XElement GetXElement(Bond bond, CmlFormat format = CmlFormat.Default)
        {
            var order = bond.Order;
            if (format != CmlFormat.Default)
            {
                if (bond.OrderValue != null)
                {
                    order = bond.OrderValue.Value.ToString(CultureInfo.InvariantCulture);
                }
            }

            var result = new XElement(CMLNamespaces.cml + CMLConstants.TagBond,
                                      new XAttribute(CMLConstants.AttributeId, bond.Id),
                                      new XAttribute(CMLConstants.AttributeAtomRefs2, $"{bond.StartAtom.Id} {bond.EndAtom.Id}"),
                                      new XAttribute(CMLConstants.AttributeOrder, order),
                                      GetStereoXElement(bond));

            if (bond.ExplicitPlacement != null)
            {
                result.Add(new XAttribute(CMLNamespaces.c4w + CMLConstants.AttributePlacement, bond.ExplicitPlacement));
            }

            return result;
        }

        private XElement GetXElement(ReactionScheme scheme)
        {
            var rsElement = new XElement(CMLNamespaces.cml + CMLConstants.TagReactionScheme,
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
            var reactionElement = new XElement(CMLNamespaces.cml + CMLConstants.TagReaction,
                                               new XAttribute(CMLConstants.AttributeId, reaction.Id),
                                               new XAttribute(CMLNamespaces.c4w + CMLConstants.AttributeArrowHead, PointHelper.AsCMLString(reaction.HeadPoint)),
                                               new XAttribute(CMLNamespaces.c4w + CMLConstants.AttributeArrowTail, PointHelper.AsCMLString(reaction.TailPoint)));

            switch (reaction.ReactionType)
            {
                case ReactionType.Normal:
                    break;

                case ReactionType.Reversible:
                    reactionElement.Add(new XAttribute(CMLNamespaces.cml + CMLConstants.AttributeReactionType, CMLConstants.AttrValueReversible));
                    break;

                case ReactionType.ReversibleBiasedForward:
                    reactionElement.Add(new XAttribute(CMLNamespaces.cml + CMLConstants.AttributeReactionType, CMLConstants.AttrValueReversible));
                    reactionElement.Add(new XAttribute(CMLNamespaces.cml + CMLConstants.AttributeReactionBias, CMLConstants.AttrValueBiasForward));
                    break;

                case ReactionType.ReversibleBiasedReverse:
                    reactionElement.Add(new XAttribute(CMLNamespaces.cml + CMLConstants.AttributeReactionType, CMLConstants.AttrValueReversible));
                    reactionElement.Add(new XAttribute(CMLNamespaces.cml + CMLConstants.AttributeReactionBias, CMLConstants.AttrValueBiasReverse));
                    break;

                case ReactionType.Blocked:
                    reactionElement.Add(new XAttribute(CMLNamespaces.cml + CMLConstants.AttributeReactionType, CMLConstants.AttrValueBlocked));
                    break;

                case ReactionType.Resonance:
                    reactionElement.Add(new XAttribute(CMLNamespaces.cml + CMLConstants.AttributeReactionType, CMLConstants.AttrValueResonance));
                    break;

                case ReactionType.Retrosynthetic:
                    reactionElement.Add(new XAttribute(CMLNamespaces.cml + CMLConstants.AttributeReactionType, CMLConstants.AttrValueRetrosynthetic));
                    break;
            }

            //do the reagents and conditions
            if (!string.IsNullOrEmpty(reaction.ReagentText) && !XAMLHelper.IsEmptyDocument(reaction.ReagentText))
            {
                var reagentText = new XElement(CMLNamespaces.c4w + CMLConstants.TagReagentText);
                XElement reagentTextElement = XElement.Parse(reaction.ReagentText);
                AddXAMLNamespaces(reagentTextElement);
                reagentText.Add(reagentTextElement);
                reactionElement.Add(reagentText);
            }
            if (!string.IsNullOrEmpty(reaction.ConditionsText) && !XAMLHelper.IsEmptyDocument(reaction.ConditionsText))
            {
                var conditionsText = new XElement(CMLNamespaces.c4w + CMLConstants.TagConditionsText);
                XElement conditionsTextElement = XElement.Parse(reaction.ConditionsText);
                AddXAMLNamespaces(conditionsTextElement);
                conditionsText.Add(conditionsTextElement);
                reactionElement.Add(conditionsText);
            }

            //do the reactants and products
            if (reaction.Reactants.Any())
            {
                var reactantListElement = new XElement(CMLNamespaces.cml + CMLConstants.TagReactantList);
                foreach (var reactant in reaction.Reactants.Values)
                {
                    XElement reactantElement = new XElement(CMLNamespaces.cml + CMLConstants.TagReactant, new XAttribute(CMLNamespaces.cml + CMLConstants.AttributeRef, reactant.Id));
                    reactantListElement.Add(reactantElement);
                }
                reactionElement.Add(reactantListElement);
            }

            if (reaction.Products.Any())
            {
                var productListElement = new XElement(CMLNamespaces.cml + CMLConstants.TagProductList);
                foreach (var product in reaction.Products.Values)
                {
                    XElement productElement = new XElement(CMLNamespaces.cml + CMLConstants.TagProduct, new XAttribute(CMLNamespaces.cml + CMLConstants.AttributeRef, product.Id));
                    productListElement.Add(productElement);
                }
                reactionElement.Add(productListElement);
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

            var result = new XElement(CMLNamespaces.cml + CMLConstants.TagAtom,
                                      new XAttribute(CMLConstants.AttributeId, atom.Id),
                                      new XAttribute(CMLConstants.AttributeElementType, elementType),
                                      new XAttribute(CMLConstants.AttributeX2, SafeDouble.AsCMLString(atom.Position.X)),
                                      new XAttribute(CMLConstants.AttributeY2, SafeDouble.AsCMLString(atom.Position.Y))
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
            var result = new XElement(CMLNamespaces.cml + CMLConstants.TagFormula);

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
            var result = new XElement(CMLNamespaces.cml + CMLConstants.TagFormula);

            if (concise != null)
            {
                result.Add(new XAttribute(CMLConstants.AttributeId, $"{molId}.f0"));
                result.Add(new XAttribute(CMLConstants.AttributeConcise, concise));
            }

            return result;
        }

        private XElement GetXElement(Annotation annotation)
        {
            var result = new XElement(CMLNamespaces.c4w + CMLConstants.TagAnnotation);
            if (annotation != null)
            {
                if (!string.IsNullOrEmpty(annotation.Xaml) && !XAMLHelper.IsEmptyDocument(annotation.Xaml))
                {
                    XElement flowDocElement = XElement.Parse(annotation.Xaml);
                    AddXAMLNamespaces(flowDocElement);
                    result.Add(flowDocElement);
                }
                result.Add(new XAttribute(CMLConstants.AttributeId, annotation.Id));
                result.Add(new XAttribute(CMLConstants.AttributeX2, annotation.Position.X.ToString("0.0###", CultureInfo.InvariantCulture)));
                result.Add(new XAttribute(CMLConstants.AttributeY2, annotation.Position.Y.ToString("0.0###", CultureInfo.InvariantCulture)));
                result.Add(new XAttribute(CMLConstants.AttributeIsEditable, annotation.IsEditable));
                if (annotation.SymbolSize != null)
                {
                    result.Add(new XAttribute(CMLConstants.AttributeSymbolSize, annotation.SymbolSize));
                }
            }
            return result;
        }

        // adds mc:Ignorable="c4w" xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:c4w="http://www.chem4word.com/cml"
        private void AddXAMLNamespaces(XElement elem)
        {
            if (elem.Attribute(XNamespace.Xmlns + "c4w") is null)
            {
                elem.Add(new XAttribute(XNamespace.Xmlns + "c4w", CMLNamespaces.c4w.NamespaceName));
            }
            if (elem.Attribute(XNamespace.Xmlns + "mc") is null)
            {
                elem.Add(new XAttribute(XNamespace.Xmlns + "mc", CMLNamespaces.mc.NamespaceName));
            }
            if (elem.Attribute(CMLNamespaces.mc + "Ignorable") is null)
            {
                elem.Add(new XAttribute(CMLNamespaces.mc + "Ignorable", "c4w"));
            }
            if (elem.Attribute("xmlns") is null)
            {
                elem.Add(new XAttribute("xmlns", CMLNamespaces.xaml.NamespaceName));
            }
        }

        #endregion Export Helpers

        #region Import Helpers

        private static void AddMolecule(Model newModel, Molecule newMol)
        {
            newModel.AddMolecule(newMol);
        }

        private static Molecule GetMolecule(XElement cmlElement)
        {
            var molecule = new Molecule();

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
            var childMolecules = new List<XElement>();
            if (cmlElement.Document?.Root != null
                && cmlElement.Document.Root.Name.LocalName != CMLConstants.TagMolecule)
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

            var reverseAtomLookup = new Dictionary<string, Guid>();
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

                // Check for duplicate bond
                var duplicates = (from b in molecule.Bonds
                                  where b.StartAtomInternalId == newBond.StartAtomInternalId && b.EndAtomInternalId == newBond.EndAtomInternalId
                                      || b.EndAtomInternalId == newBond.StartAtomInternalId && b.StartAtomInternalId == newBond.EndAtomInternalId
                                  select b).Any();
                if (duplicates)
                {
                    molecule.Errors.Add($"Duplicate bond {newBond.Id}: atoms [{bondElement.Attribute("atomRefs2").Value}] are already connected by a bond.");
                }

                molecule.AddBond(newBond);
                newBond.Parent = molecule;
            }

            foreach (XElement formulaElement in formulaElements)
            {
                TextualProperty formula = GetFormula(formulaElement);
                if (formula.IsValid)
                {
                    molecule.Formulas.Add(formula);
                }
            }

            foreach (XElement nameElement in nameElements)
            {
                TextualProperty name = GetName(nameElement);
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

            foreach (XElement labelElement in labelElements)
            {
                TextualProperty label = GetCaption(labelElement);
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
            var atom = new Atom();

            atom.Messages = new List<string>();
            string atomLabel = cmlElement.Attribute(CMLConstants.AttributeId)?.Value;

            string message = "";
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
            var bond = new Bond();

            string[] atomRefs = cmlElement.Attribute(CMLConstants.AttributeAtomRefs2)?.Value.Split(' ');
            if (atomRefs?.Length == 2)
            {
                bond.StartAtomInternalId = reverseAtomLookup[atomRefs[0]];
                bond.EndAtomInternalId = reverseAtomLookup[atomRefs[1]];
            }
            string bondRef = cmlElement.Attribute(CMLConstants.AttributeId)?.Value;
            bond.Id = bondRef ?? bond.Id;
            bond.Order = cmlElement.Attribute(CMLConstants.AttributeOrder)?.Value;
            // Convert any invalid values to a zero bond (ChEMBL clean yields "8")
            if (!CMLHelper.BondOrderIsValid(bond.Order))
            {
                bond.Order = "0";
            }

            List<XElement> stereoElems = CMLHelper.GetStereo(cmlElement);

            if (stereoElems.Any())
            {
                string stereo = stereoElems[0].Value;

                bond.Stereo = Bond.StereoFromString(stereo);
            }
            BondDirection? dir = null;

            XAttribute dirAttr = cmlElement.Attribute(CMLNamespaces.c4w + CMLConstants.AttributePlacement);
            if (dirAttr != null)
            {
                BondDirection temp;
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

        private static ReactionScheme GetReactionScheme(XElement cmlElement, Model newModel)
        {
            var scheme = new ReactionScheme();
            string idValue = cmlElement.Attribute(CMLConstants.AttributeId)?.Value;
            if (!string.IsNullOrEmpty(idValue))
            {
                scheme.Id = idValue;
            }
            List<XElement> reactionElements = CMLHelper.GetReactions(cmlElement);
            foreach (XElement reactionElement in reactionElements)
            {
                Reaction newReaction = GetReaction(reactionElement, newModel);
                scheme.AddReaction(newReaction);
                newReaction.Parent = scheme;
            }
            return scheme;
        }

        private static Reaction GetReaction(XElement cmlElement, Model model)
        {
            var reaction = new Reaction();
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

            reaction.ReactionType = ReactionType.Normal;

            if (!string.IsNullOrEmpty(reactionTypeValue))
            {
                switch (reactionTypeValue)
                {
                    case CMLConstants.AttrValueBlocked:
                        reaction.ReactionType = ReactionType.Blocked;
                        break;

                    case CMLConstants.AttrValueReversible:
                        reaction.ReactionType = ReactionType.Reversible;
                        {
                            string bias = cmlElement.Attribute(CMLNamespaces.cml + CMLConstants.AttributeReactionBias)?.Value;
                            if (!string.IsNullOrEmpty(bias))
                            {
                                if (bias == CMLConstants.AttrValueBiasForward)
                                {
                                    reaction.ReactionType = ReactionType.ReversibleBiasedForward;
                                }
                                else if (bias == CMLConstants.AttrValueBiasReverse)
                                {
                                    reaction.ReactionType = ReactionType.ReversibleBiasedReverse;
                                }
                            }
                        }
                        break;

                    case CMLConstants.AttrValueResonance:
                        reaction.ReactionType = ReactionType.Resonance;
                        break;

                    case CMLConstants.AttrValueRetrosynthetic:
                        reaction.ReactionType = ReactionType.Retrosynthetic;
                        break;
                }
            }

            //text boxes that go above and below the arrow
            XElement reagentElement = cmlElement.Element(CMLNamespaces.c4w + CMLConstants.TagReagentText);
            var reagentText = reagentElement?.ToString();
            if (!string.IsNullOrEmpty(reagentText))
            {
                reaction.ReagentText = reagentElement.CreateNavigator().InnerXml;
            }

            XElement conditionsElement = cmlElement.Element(CMLNamespaces.c4w + CMLConstants.TagConditionsText);
            var conditionsText = conditionsElement?.ToString();
            if (!string.IsNullOrEmpty(conditionsText))
            {
                reaction.ConditionsText = conditionsElement.CreateNavigator().InnerXml;
            }

            //reactants and products
            XElement reactantListElement = cmlElement.Element(CMLNamespaces.cml + CMLConstants.TagReactantList);
            if (reactantListElement != null)
            {
                foreach (var reactantElement in reactantListElement.Elements(CMLNamespaces.cml + CMLConstants.TagReactant))
                {
                    if (reactantElement.Attribute(CMLNamespaces.cml + CMLConstants.AttributeRef) != null)
                    {
                        Molecule reactant = GetParticipant(reactantElement, model);
                        if (reactant != null)
                        {
                            reaction.AddReactant(reactant);
                        }
                    }
                }
            }
            XElement productListElement = cmlElement.Element(CMLNamespaces.cml + CMLConstants.TagProductList);
            if (productListElement != null)
            {
                foreach (var productElement in productListElement.Elements(CMLNamespaces.cml + CMLConstants.TagProduct))
                {
                    if (productElement.Attribute(CMLNamespaces.cml + CMLConstants.AttributeRef) != null)
                    {
                        Molecule product = GetParticipant(productElement, model);
                        if (product != null)
                        {
                            reaction.AddProduct(product);
                        }
                    }
                }
            }
            return reaction;
        }

        private static Molecule GetParticipant(XElement participant, Model model)
        {
            foreach (var molecule in model.Molecules.Values)
            {
                if (molecule.Id == participant.Attribute(CMLNamespaces.cml + CMLConstants.AttributeRef).Value)
                {
                    return molecule;
                }
            }
            return null;
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
                string dictrefValue = cmlElement.Attribute(CMLConstants.AttributeDictRef)?.Value;
                if (dictrefValue != null && dictrefValue.Equals(CMLConstants.ValueChem4WordCaption))
                {
                    var result = new TextualProperty
                    {
                        FullType = CMLConstants.ValueChem4WordCaption
                    };

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
            var name = new TextualProperty
            {
                Id = cmlElement.Attribute(CMLConstants.AttributeId)?.Value
            };

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