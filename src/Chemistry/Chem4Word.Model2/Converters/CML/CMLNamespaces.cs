// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
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

        // ReSharper disable once InconsistentNaming
        public static XNamespace cmlDict = "http://www.xml-cml.org/dictionary/cml/";

        // ReSharper disable once InconsistentNaming
        public static XNamespace nameDict = "http://www.xml-cml.org/dictionary/cml/name/";

        // ReSharper disable once InconsistentNaming
        public static XNamespace conventions = "http://www.xml-cml.org/convention/";

        // ReSharper disable once InconsistentNaming
        public static XNamespace c4w = "http://www.chem4word.com/cml";
    }
}