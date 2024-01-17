// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.IO;
using System.Text;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Xml;

namespace Chem4Word.Core.Helpers
{
    public static class XAMLHelper
    {
        public static FlowDocument GetFlowDocument(string text)
        {
            //make sure that we don't put spaces around the subscripts

            using (var xmlReader = XmlReader.Create(new StringReader(text),
                                            new XmlReaderSettings
                                            {
                                                IgnoreWhitespace = true,
                                            }))
            {
                xmlReader.Read();
                var s = xmlReader.ReadOuterXml();
                using (var mStream = new MemoryStream(Encoding.Unicode.GetBytes(s)))
                {
                    var tempDoc = (FlowDocument)XamlReader.Load(mStream);
                    return tempDoc;
                }
            }
        }

        public static bool IsEmptyDocument(string text)
        {
            var doc = GetFlowDocument(text);
            var doctext = new TextRange(doc.ContentStart, doc.ContentEnd).Text;
            return string.IsNullOrWhiteSpace(doctext);
        }
    }
}