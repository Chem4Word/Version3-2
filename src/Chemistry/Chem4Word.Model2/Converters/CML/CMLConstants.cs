// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace Chem4Word.Model2.Converters.CML
{
    // ReSharper disable once InconsistentNaming
    public static class CMLConstants
    {
        // Namespaces
        public const string TagCml = "cml";

        public const string TagConventions = "conventions";
        public const string TagCmlDict = "cmlDict";
        public const string TagNameDict = "nameDict";
        public const string TagConventionMolecular = "convention:molecular";
        public const string TagC4W = "c4w";

        // General
        public const string AttributeId = "id";

        // Molecule
        public const string TagMolecule = "molecule";

        public const string AttributeSpinMultiplicity = "spinMultiplicity";
        public const string AttributeCount = "count";

        public const string TagXmlPartGuid = "customXmlPartGuid";
        public const string AttributeShowMoleculeBrackets = "showBrackets";

        // Atoms
        public const string TagAtomArray = "atomArray";

        public const string TagAtom = "atom";
        public const string AttributeElementType = "elementType";
        public const string AttributeX2 = "x2";
        public const string AttributeY2 = "y2";
        public const string AttributeX3 = "x3";
        public const string AttributeY3 = "y3";
        public const string AttributeZ3 = "z3";
        public const string AttributeFormalCharge = "formalCharge";
        public const string AttributeIsotopeNumber = "isotopeNumber";
        public const string AttributeExplicit = "explicit";

        // Bonds
        public const string TagBondArray = "bondArray";

        public const string TagBond = "bond";
        public const string AttributeAtomRefs2 = "atomRefs2";
        public const string AttributeAtomRefs4 = "atomRefs4";
        public const string AttributeOrder = "order";
        public const string TagBondStereo = "bondStereo";
        public const string AttributePlacement = "placement";

        // Formula
        // <cml:formula id="m1.f1" convention="chem4word:Formula" inline="m1.f1" concise="C 6 H 14 Li 1 N 1" />
        public const string TagFormula = "formula";

        public const string AttributeConvention = "convention";
        public const string AttributeInline = "inline";
        public const string AttributeConcise = "concise";

        // Names
        // <cml:name id="m1.n1" dictRef="chem4word:Synonym">m1.n1</cml:name>
        public const string TagName = "name";

        public const string AttributeDictRef = "dictRef";
        public const string ValueChem4WordFormula = "chem4word:Formula";
        public const string ValueChem4WordSynonym = "chem4word:Synonym";
        public const string ValueNameDictUnknown = "nameDict:unknown";

        // Labels
        // <cml:label id="m1.l1" dictRef="chem4word:Label" value="m1.l1" />
        public const string TagLabel = "label";

        public const string AttributeNameValue = "value";
        public const string ValueChem4WordCaption = "chem4word:Caption";

        // Our DictRef values
        public const string ValueChem4WordInchiName = "chem4word:CalculatedInchi";

        //public const string ValueChem4WordAuxInfoName = "chem4word:CalculatedAuxInfo";
        public const string ValueChem4WordInchiKeyName = "chem4word:CalculatedInchikey";

        public const string ValueChem4WordResolverIupacName = "chem4word:ResolvedIupacname";
        public const string ValueChem4WordResolverSmilesName = "chem4word:ResolvedSmiles";
        public const string ValueChem4WordResolverFormulaName = "chem4word:ResolvedFormula";
    }
}