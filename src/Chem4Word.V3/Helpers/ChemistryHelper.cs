// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Helpers;
using IChem4Word.Contracts;
using Microsoft.Office.Core;
using Word = Microsoft.Office.Interop.Word;

namespace Chem4Word.Helpers
{
    public static class ChemistryHelper
    {
        private static readonly string Product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static readonly string Class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private static object _missing = Type.Missing;

        public static Word.ContentControl Insert2DChemistry(Word.Document doc, string cml, bool isCopy)
        {
            string module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod().Name}()";

            if (Globals.Chem4WordV3.SystemOptions == null)
            {
                Globals.Chem4WordV3.LoadOptions();
            }

            // Calling routine should check that Globals.Chem4WordV3.ChemistryAllowed = true

            Word.ContentControl cc = null;
            Word.Application app = doc.Application;

            var wordSettings = new WordSettings(app);

            IChem4WordRenderer renderer =
                Globals.Chem4WordV3.GetRendererPlugIn(
                    Globals.Chem4WordV3.SystemOptions.SelectedRendererPlugIn);

            if (renderer != null)
            {
                try
                {
                    app.ScreenUpdating = false;
                    Globals.Chem4WordV3.DisableContentControlEvents();

                    var converter = new CMLConverter();
                    var model = converter.Import(cml);
                    var modified = false;

                    if (isCopy)
                    {
                        // Always generate new Guid on Import
                        model.CustomXmlPartGuid = Guid.NewGuid().ToString("N");
                        modified = true;
                    }

                    if (modified)
                    {
                        // Re-export as the CustomXmlPartGuid or Bond Length has been changed
                        cml = converter.Export(model);
                    }

                    string guid = model.CustomXmlPartGuid;

                    renderer.Properties = new Dictionary<string, string>();
                    renderer.Properties.Add("Guid", guid);
                    renderer.Cml = cml;

                    // Generate temp file which can be inserted into a content control
                    string tempfileName = renderer.Render();
                    if (File.Exists(tempfileName))
                    {
                        cc = doc.ContentControls.Add(Word.WdContentControlType.wdContentControlRichText, ref _missing);
                        Insert2D(cc.ID, tempfileName, guid);

                        Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"ContentControl inserted at position {cc.Range.Start}");

                        if (isCopy)
                        {
                            doc.CustomXMLParts.Add(cml);
                        }

                        try
                        {
                            // Delete the temporary file now we are finished with it
#if DEBUG
#else
                            File.Delete(tempfileName);
#endif
                        }
                        catch
                        {
                            // Not much we can do here
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error in Insert2DChemistry; See InnerException for details", ex);
                }
                finally
                {
                    app.ScreenUpdating = true;
                    Globals.Chem4WordV3.EnableContentControlEvents();
                }
            }

            wordSettings.RestoreSettings(app);

            return cc;
        }

        public static Word.ContentControl Insert1DChemistry(Word.Document doc, string text, bool isFormula, string tag)
        {
            string module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod().Name}()";

            if (Globals.Chem4WordV3.SystemOptions == null)
            {
                Globals.Chem4WordV3.LoadOptions();
            }

            Word.Application app = Globals.Chem4WordV3.Application;

            var wordSettings = new WordSettings(app);

            Word.ContentControl cc = doc.ContentControls.Add(Word.WdContentControlType.wdContentControlRichText, ref _missing);

            SetRichText(cc.ID, text, isFormula);

            Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"ContentControl inserted at position {cc.Range.Start}");

            wordSettings.RestoreSettings(app);

            cc.Tag = tag;
            cc.Title = Constants.ContentControlTitle;
            cc.LockContents = true;

            return cc;
        }

        public static void RefreshAllStructures(Word.Document doc)
        {
            string module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod().Name}()";

            if (Globals.Chem4WordV3.SystemOptions == null)
            {
                Globals.Chem4WordV3.LoadOptions();
            }

            IChem4WordRenderer renderer =
                Globals.Chem4WordV3.GetRendererPlugIn(
                    Globals.Chem4WordV3.SystemOptions.SelectedRendererPlugIn);

            if (renderer != null)
            {
                foreach (CustomXMLPart xmlPart in doc.CustomXMLParts)
                {
                    var cml = xmlPart.XML;

                    var cxmlId = CustomXmlPartHelper.GetCmlId(xmlPart);
                    var cc = new CMLConverter();
                    var model = cc.Import(cml);

                    renderer.Properties = new Dictionary<string, string>();
                    renderer.Properties.Add("Guid", cxmlId);
                    renderer.Cml = cml;

                    string tempFilename = renderer.Render();
                    if (File.Exists(tempFilename))
                    {
                        UpdateThisStructure(doc, model, cxmlId, tempFilename);
                    }
                }
            }
        }

        public static void UpdateThisStructure(Word.Document doc, Model model, string cxmlId, string tempFilename)
        {
            string module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod().Name}()";

            // Use LINQ to get a list of all our ContentControls
            // Using $"{}" to coerce null to empty string
            var targets = (from Word.ContentControl ccs in doc.ContentControls
                           orderby ccs.Range.Start
                           where $"{ccs.Title}" == Constants.ContentControlTitle
                                 && $"{ccs.Tag}".Contains(cxmlId)
                           select new KeyValuePair<string, string>(ccs.ID, ccs.Tag)).ToList();

            foreach (var target in targets)
            {
                string prefix = "";
                string ccTag = target.Value;

                if (ccTag != null && ccTag.Contains(":"))
                {
                    prefix = ccTag.Split(':')[0];
                }

                if (ccTag != null && ccTag.Equals(cxmlId))
                {
                    // Only 2D Structures if filename supplied
                    if (!string.IsNullOrEmpty(tempFilename))
                    {
                        Update2D(target.Key, tempFilename, cxmlId);
                    }
                }
                else
                {
                    // 1D Structures
                    if (prefix.Equals("c0"))
                    {
                        Update1D(target.Key, model.ConciseFormula, true, $"c0:{cxmlId}");
                    }
                    else
                    {
                        var isFormula = false;
                        string text = GetInlineText(model, prefix, ref isFormula, out _);
                        Update1D(target.Key, text, isFormula, $"{prefix}:{cxmlId}");
                    }
                }
            }
        }

        public static void Insert2D(string ccId, string tempfileName, string guid)
        {
            string module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod().Name}()";

            Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"Inserting 2D structure in ContentControl {ccId} Tag {guid}");

            Word.Application app = Globals.Chem4WordV3.Application;
            Word.Document doc = app.ActiveDocument;
            var wordSettings = new WordSettings(app);

            var cc = GetContentControl(ccId);

            string bookmarkName = Constants.OoXmlBookmarkPrefix + guid;

            cc.Range.InsertFile(tempfileName, bookmarkName);
            if (doc.Bookmarks.Exists(bookmarkName))
            {
                doc.Bookmarks[bookmarkName].Delete();
            }

            wordSettings.RestoreSettings(app);

            cc.Tag = guid;
            cc.Title = Constants.ContentControlTitle;
            cc.LockContents = true;
        }

        private static void Update2D(string ccId, string tempfileName, string guid)
        {
            string module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod().Name}()";

            Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"Updating 2D structure in ContentControl {ccId} Tag {guid}");

            Word.Application app = Globals.Chem4WordV3.Application;
            Word.Document doc = app.ActiveDocument;
            var wordSettings = new WordSettings(app);

            var cc = GetContentControl(ccId);

            cc.LockContents = false;
            if (cc.Type == Word.WdContentControlType.wdContentControlPicture)
            {
                // Handle old Word 2007 style
                Word.Range range = cc.Range;
                cc.Delete();
                cc = doc.ContentControls.Add(Word.WdContentControlType.wdContentControlRichText, range);
                cc.Tag = guid;
                cc.Title = Constants.ContentControlTitle;
                cc.Range.Delete();
            }
            else
            {
                cc.Range.Delete();
            }

            string bookmarkName = Constants.OoXmlBookmarkPrefix + guid;
            cc.Range.InsertFile(tempfileName, bookmarkName);
            if (doc.Bookmarks.Exists(bookmarkName))
            {
                doc.Bookmarks[bookmarkName].Delete();
            }

            Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"ContentControl updated at position {cc.Range.Start}");

            wordSettings.RestoreSettings(app);

            cc.Tag = guid;
            cc.Title = Constants.ContentControlTitle;
            cc.LockContents = true;
        }

        public static void Insert1D(string ccId, string text, bool isFormula, string tag)
        {
            string module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod().Name}()";

            Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"Inserting 1D label in ContentControl {ccId} Tag {tag}");

            Word.Application app = Globals.Chem4WordV3.Application;
            var wordSettings = new WordSettings(app);

            var cc = GetContentControl(ccId);

            SetRichText(cc.ID, text, isFormula);

            wordSettings.RestoreSettings(app);

            cc.Tag = tag;
            cc.Title = Constants.ContentControlTitle;
            cc.LockContents = true;
        }

        private static void Update1D(string ccId, string text, bool isFormula, string tag)
        {
            string module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod().Name}()";

            Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"Updating 1D label in ContentControl {ccId} Tag {tag}");

            Word.Application app = Globals.Chem4WordV3.Application;
            var wordSettings = new WordSettings(app);

            var cc = GetContentControl(ccId);

            cc.LockContents = false;
            cc.Range.Delete();

            SetRichText(cc.ID, text, isFormula);

            Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"ContentControl updated at position {cc.Range.Start}");

            wordSettings.RestoreSettings(app);

            cc.Tag = tag;
            cc.Title = Constants.ContentControlTitle;
            cc.LockContents = true;
        }

        public static string GetInlineText(Model model, string prefix, ref bool isFormula, out string source)
        {
            string module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod().Name}()";

            source = null;
            string text;

            var tp = model.GetTextPropertyById(prefix);
            if (tp != null)
            {
                text = tp.Value;
                if (tp.Id.EndsWith("f0"))
                {
                    source = "ConciseFormula";
                    isFormula = true;
                }
                else
                {
                    source = tp.FullType;
                    isFormula = tp.FullType.ToLower().Contains("formula");
                }
            }
            else
            {
                text = $"Unable to find formula or name with id of '{prefix}'";
            }

            return text;
        }

        private static Word.ContentControl GetContentControl(string Id)
        {
            Word.ContentControl result = null;

            Word.Document d = Globals.Chem4WordV3.Application.ActiveDocument;
            foreach (Word.ContentControl contentControl in d.ContentControls)
            {
                if (contentControl.ID.Equals(Id))
                {
                    result = contentControl;
                    break;
                }
            }

            return result;
        }

        private static void SetRichText(string ccId, string text, bool isFormula)
        {
            string module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod().Name}()";

            var cc = GetContentControl(ccId);

            if (isFormula)
            {
                Word.Range r = cc.Range;
                List<MoleculeFormulaPart> parts = FormulaHelper.ParseFormulaIntoParts(text);
                foreach (var part in parts)
                {
                    switch (part.Count)
                    {
                        case 0: // Separator or multiplier
                        case 1: // No Subscript
                            if (!string.IsNullOrEmpty(part.Element))
                            {
                                r.InsertAfter(part.Element);
                                r.Font.Subscript = 0;
                                r.Start = cc.Range.End;
                            }
                            break;

                        default: // With Subscript
                            if (!string.IsNullOrEmpty(part.Element))
                            {
                                r.InsertAfter(part.Element);
                                r.Font.Subscript = 0;
                                r.Start = cc.Range.End;
                            }

                            if (part.Count > 0)
                            {
                                r.InsertAfter($"{part.Count}");
                                r.Font.Subscript = 1;
                                r.Start = cc.Range.End;
                            }
                            break;
                    }
                }
            }
            else
            {
                cc.Range.Text = text;
            }
        }

        public static List<string> GetUsed1D(Word.Document doc, string guidString)
        {
            string module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod().Name}()";

            // Using $"{}" to coerce null to empty string
            List<string> targets = (from Word.ContentControl ccs in doc.ContentControls
                                    orderby ccs.Range.Start
                                    where $"{ccs.Title}" == Constants.ContentControlTitle
                                          && $"{ccs.Tag}".Contains(guidString)
                                          && !$"{ccs.Tag}".Equals(guidString)
                                    select ccs.Tag).Distinct().ToList();

            return targets;
        }

        public static List<string> GetUsed2D(Word.Document doc, string guidString)
        {
            string module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod().Name}()";

            // Using $"{}" to coerce null to empty string
            List<string> targets = (from Word.ContentControl ccs in doc.ContentControls
                                    orderby ccs.Range.Start
                                    where $"{ccs.Title}" == Constants.ContentControlTitle
                                          && $"{ccs.Tag}".Equals(guidString)
                                    select ccs.Tag).Distinct().ToList();

            return targets;
        }
    }
}