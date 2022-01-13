// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Reflection;
using System.Xml;
using Microsoft.Office.Core;
using Word = Microsoft.Office.Interop.Word;

namespace WinForms.TestLibrary.Helpers
{
    public static class CustomXmlPartHelper
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private static Microsoft.Office.Core.CustomXMLParts AllChemistryParts(Word.Document document)
            => document.CustomXMLParts.SelectByNamespace("http://www.xml-cml.org/schema");

        public static int ChemistryXmlParts(Word.Document doc)
            => AllChemistryParts(doc).Count;

        public static CustomXMLPart FindCustomXmlPart(string id, Word.Document document)
        {
            CustomXMLPart result = null;

            Word.Document activeDocument = document;
            string activeDocumentName = activeDocument.Name;

            foreach (Word.Document otherDocument in activeDocument.Application.Documents)
            {
                if (!otherDocument.Name.Equals(activeDocumentName))
                {
                    foreach (
                        CustomXMLPart customXmlPart in AllChemistryParts(otherDocument))
                    {
                        string molId = GetCmlId(customXmlPart);
                        if (molId.Equals(id))
                        {
                            result = customXmlPart;
                            break;
                        }
                    }
                }
                if (result != null)
                {
                    break;
                }
            }

            return result;
        }

        public static string GuidFromTag(string tag)
        {
            string guid = string.Empty;

            if (!string.IsNullOrEmpty(tag))
            {
                guid = tag.Contains(":") ? tag.Split(':')[1] : tag;
            }

            return guid;
        }

        public static CustomXMLPart GetCustomXmlPart(string id, Word.Document activeDocument)
        {
            CustomXMLPart result = null;

            string guid = GuidFromTag(id);

            if (!string.IsNullOrEmpty(guid))
            {
                Word.Document doc = activeDocument;

                foreach (CustomXMLPart xmlPart in AllChemistryParts(doc))
                {
                    string cmlId = GetCmlId(xmlPart);
                    if (!string.IsNullOrEmpty(cmlId))
                    {
                        if (cmlId.Equals(guid))
                        {
                            result = xmlPart;
                            break;
                        }
                    }
                }
            }

            return result;
        }

        public static string GetCmlId(CustomXMLPart xmlPart)
        {
            string result = string.Empty;

            XmlDocument xdoc = new XmlDocument();
            xdoc.LoadXml(xmlPart.XML);
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xdoc.NameTable);
            nsmgr.AddNamespace("cml", "http://www.xml-cml.org/schema");
            nsmgr.AddNamespace("c4w", "http://www.chem4word.com/cml");

            XmlNode node = xdoc.SelectSingleNode("//c4w:customXmlPartGuid", nsmgr);
            if (node != null)
            {
                result = node.InnerText;
            }

            return result;
        }
    }
}