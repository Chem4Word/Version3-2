// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Xml.Linq;

namespace Chem4Word.Model2.Converters.CML
{
    // ReSharper disable once InconsistentNaming
    public class CMLNamespaces
    {
        // ReSharper disable once InconsistentNaming
        public static XNamespace cml = "http://www.xml-cml.org/schema";

        public static XNamespace chemaxion = "http://www.chemaxon.com";

        // ReSharper disable once InconsistentNaming
        public static XNamespace cmlDict = "http://www.xml-cml.org/dictionary/cml/";

        // ReSharper disable once InconsistentNaming
        public static XNamespace nameDict = "http://www.xml-cml.org/dictionary/cml/name/";

        // ReSharper disable once InconsistentNaming
        public static XNamespace conventions = "http://www.xml-cml.org/convention/";

        // ReSharper disable once InconsistentNaming
        public static XNamespace c4w = "http://www.chem4word.com/cml";

        public static XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

        public static XNamespace mc = "http://schemas.openxmlformats.org/markup-compatibility/2006";
    }
}