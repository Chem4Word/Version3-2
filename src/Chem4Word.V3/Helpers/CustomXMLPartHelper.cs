// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Microsoft.Office.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;
using Word = Microsoft.Office.Interop.Word;

namespace Chem4Word.Helpers
{
    public static class CustomXmlPartHelper
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod()?.DeclaringType?.Name;

        private static CustomXMLParts AllChemistryParts(Word.Document document)
            => document.CustomXMLParts.SelectByNamespace("http://www.xml-cml.org/schema");

        public static int ChemistryXmlParts(Word.Document document)
            => AllChemistryParts(document).Count;

        public static CustomXMLPart FindCustomXmlPartInOtherDocuments(string id, string activeDocumentName)
        {
            CustomXMLPart result = null;

            foreach (Word.Document otherDocument in Globals.Chem4WordV3.Application.Documents)
            {
                if (!otherDocument.Name.Equals(activeDocumentName))
                {
                    foreach (CustomXMLPart customXmlPart in AllChemistryParts(otherDocument))
                    {
                        var molId = GetCmlId(customXmlPart);
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
            var guid = string.Empty;

            if (!string.IsNullOrEmpty(tag))
            {
                guid = tag.Contains(":") ? tag.Split(':')[1] : tag;
            }

            return guid;
        }

        public static CustomXMLPart GetCustomXmlPart(Word.Document document, string id)
        {
            CustomXMLPart result = null;

            var guid = GuidFromTag(id);

            if (!string.IsNullOrEmpty(guid))
            {
                foreach (CustomXMLPart xmlPart in AllChemistryParts(document))
                {
                    var cmlId = GetCmlId(xmlPart);
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
            var result = string.Empty;

            var xdoc = new XmlDocument();
            xdoc.LoadXml(xmlPart.XML);
            var nsmgr = new XmlNamespaceManager(xdoc.NameTable);
            nsmgr.AddNamespace("cml", "http://www.xml-cml.org/schema");
            nsmgr.AddNamespace("c4w", "http://www.chem4word.com/cml");

            var node = xdoc.SelectSingleNode("//c4w:customXmlPartGuid", nsmgr);
            if (node != null)
            {
                result = node.InnerText;
            }

            return result;
        }

        public static void RemoveOrphanedXmlParts(Word.Document document)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            if (document != null)
            {
                var referencedXmlParts = new Dictionary<string, int>();

                foreach (Word.ContentControl cc in document.ContentControls)
                {
                    if (cc.Title != null && cc.Title.Equals(Constants.ContentControlTitle))
                    {
                        var guid = GuidFromTag(cc?.Tag);

                        if (!string.IsNullOrEmpty(guid))
                        {
                            if (!referencedXmlParts.ContainsKey(guid))
                            {
                                referencedXmlParts.Add(guid, 1);
                            }
                        }
                    }
                }

                var backupFolder = Path.Combine(Globals.Chem4WordV3.AddInInfo.ProductAppDataPath, "Backups");

                foreach (CustomXMLPart customXmlPart in AllChemistryParts(document))
                {
                    var molId = GetCmlId(customXmlPart);
                    if (!referencedXmlParts.ContainsKey(molId))
                    {
                        try
                        {
                            var guid = Guid.NewGuid().ToString("N");
                            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);

                            var fileName = Path.Combine(backupFolder, $"Chem4Word-Orphaned-Structure-{timestamp}-{guid}.cml");
                            File.WriteAllText(fileName, XmlHelper.AddHeader(customXmlPart.XML));

                            customXmlPart.Delete();
                        }
                        catch (Exception exception)
                        {
                            RegistryHelper.StoreException(module, exception);
                        }
                    }
                }
            }
        }
    }
}