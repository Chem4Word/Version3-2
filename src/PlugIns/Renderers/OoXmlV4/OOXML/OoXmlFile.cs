// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.IO;
using System.Reflection;
using System.Windows;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using IChem4Word.Contracts;

namespace Chem4Word.Renderer.OoXmlV4.OOXML
{
    public static class OoXmlFile
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        /// <summary>
        /// Create an OpenXml Word Document from the CML
        /// </summary>
        /// <param name="cml">Input Chemistry</param>
        /// <param name="guid">Bookmark to create</param>
        /// <param name="options"></param>
        /// <param name="telemetry"></param>
        /// <param name="topLeft"></param>
        /// <returns></returns>
        public static string CreateFromCml(string cml, string guid, OoXmlV4Options options, IChem4WordTelemetry telemetry, Point topLeft)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            CMLConverter cc = new CMLConverter();
            Model model = cc.Import(cml);
            if (model.AllErrors.Count > 0 || model.AllWarnings.Count > 0)
            {
                if (model.AllErrors.Count > 0)
                {
                    telemetry.Write(module, "Exception(Data)", string.Join(Environment.NewLine, model.AllErrors));
                }

                if (model.AllWarnings.Count > 0)
                {
                    telemetry.Write(module, "Exception(Data)", string.Join(Environment.NewLine, model.AllWarnings));
                }
            }

            string fileName = string.Empty;

            bool canRender = model.TotalAtomsCount > 0
                             && (model.TotalBondsCount == 0
                                 || model.MeanBondLength > Core.Helpers.Constants.BondLengthTolerance / 2);

            if (canRender)
            {
                fileName = Path.Combine(Path.GetTempPath(), $"Chem4Word-V3-{guid}.docx");

                string bookmarkName = Core.Helpers.Constants.OoXmlBookmarkPrefix + guid;

                // Create a Wordprocessing document.
                using (WordprocessingDocument package = WordprocessingDocument.Create(fileName, WordprocessingDocumentType.Document))
                {
                    // Add a new main document part.
                    MainDocumentPart mdp = package.AddMainDocumentPart();
                    mdp.Document = new Document(new Body());
                    Body docbody = package.MainDocumentPart.Document.Body;

                    // This will be live
                    AddPictureFromModel(docbody, model, bookmarkName, options, telemetry, topLeft);

                    // Save changes to the main document part.
                    package.MainDocumentPart.Document.Save();
                }
            }

            return fileName;
        }

        /// <summary>
        /// Creates the DrawingML objects and adds them to the document
        /// </summary>
        /// <param name="docbody"></param>
        /// <param name="model"></param>
        /// <param name="bookmarkName"></param>
        /// <param name="options"></param>
        /// <param name="telemetry"></param>
        /// <param name="topLeft"></param>
        private static void AddPictureFromModel(Body docbody, Model model, string bookmarkName, OoXmlV4Options options, IChem4WordTelemetry telemetry, Point topLeft)
        {
            Paragraph paragraph1 = new Paragraph();
            if (!string.IsNullOrEmpty(bookmarkName))
            {
                BookmarkStart bookmarkstart = new BookmarkStart();
                bookmarkstart.Name = bookmarkName;
                bookmarkstart.Id = "1";
                paragraph1.Append(bookmarkstart);
            }

            OoXmlRenderer renderer = new OoXmlRenderer(model, options, telemetry, topLeft);
            paragraph1.Append(renderer.GenerateRun());

            if (!string.IsNullOrEmpty(bookmarkName))
            {
                BookmarkEnd bookmarkend = new BookmarkEnd();
                bookmarkend.Id = "1";
                paragraph1.Append(bookmarkend);
            }

            docbody.Append(paragraph1);
        }
    }
}