// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using IChem4Word.Contracts;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace Chem4Word.Renderer.OoXmlV4.OOXML
{
    // ReSharper disable PossiblyMistakenUseOfParamsMethod
    [SuppressMessage("Minor Code Smell", "S3220:Method calls should not resolve ambiguously to overloads with \"params\"", Justification = "<OoXml>")]
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
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            var cc = new CMLConverter();
            var model = cc.Import(cml);
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

            var fileName = string.Empty;

            var canRender = model.ReactionSchemes.Any()
                            || model.Annotations.Any()
                            || model.TotalAtomsCount > 0
                            && (model.TotalBondsCount == 0
                                || model.MeanBondLength > Core.Helpers.Constants.BondLengthTolerance / 2);

            if (canRender)
            {
                fileName = Path.Combine(Path.GetTempPath(), $"Chem4Word-V3-{guid}.docx");

                var bookmarkName = Core.Helpers.Constants.OoXmlBookmarkPrefix + guid;

                // Create a Wordprocessing document.
                using (var document = WordprocessingDocument.Create(fileName, WordprocessingDocumentType.Document))
                {
                    // Add a new main document part.
                    var mainDocumentPart = document.AddMainDocumentPart();
                    mainDocumentPart.Document = new Document(new Body());
                    var body = document.MainDocumentPart.Document.Body;

                    AddPictureFromModel(body, model, bookmarkName, options, telemetry, topLeft);

                    // Save changes to the main document part.
                    document.MainDocumentPart.Document.Save();
                }
            }

            return fileName;
        }

        /// <summary>
        /// Creates the DrawingML objects and adds them to the document
        /// </summary>
        /// <param name="body"></param>
        /// <param name="model"></param>
        /// <param name="bookmarkName"></param>
        /// <param name="options"></param>
        /// <param name="telemetry"></param>
        /// <param name="topLeft"></param>
        private static void AddPictureFromModel(Body body, Model model, string bookmarkName, OoXmlV4Options options, IChem4WordTelemetry telemetry, Point topLeft)
        {
            var paragraph1 = new Paragraph();
            if (!string.IsNullOrEmpty(bookmarkName))
            {
                var bookmarkStart = new BookmarkStart
                {
                    Name = bookmarkName,
                    Id = "1"
                };
                paragraph1.Append(bookmarkStart);
            }

            var renderer = new OoXmlRenderer(model, options, telemetry, topLeft);
            paragraph1.Append(renderer.GenerateRun());

            if (!string.IsNullOrEmpty(bookmarkName))
            {
                var bookmarkEnd = new BookmarkEnd
                {
                    Id = "1"
                };
                paragraph1.Append(bookmarkEnd);
            }

            body.Append(paragraph1);
        }
    }
}