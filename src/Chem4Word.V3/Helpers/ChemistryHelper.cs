// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Enums;
using Chem4Word.Model2.Helpers;
using Microsoft.Office.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Word = Microsoft.Office.Interop.Word;

namespace Chem4Word.Helpers
{
    public static class ChemistryHelper
    {
        private static readonly string Product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static readonly string Class = MethodBase.GetCurrentMethod()?.DeclaringType?.Name;

        private static object _missing = Type.Missing;

        public static Word.ContentControl Insert2DChemistry(Word.Document document, string cml, bool isCopy)
        {
            var module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod()?.Name}()";

            if (Globals.Chem4WordV3.SystemOptions == null)
            {
                Globals.Chem4WordV3.LoadOptions();
            }

            // Calling routine should check that Globals.Chem4WordV3.ChemistryAllowed = true

            Word.ContentControl cc = null;
            var app = document.Application;

            var wordSettings = new WordSettings(app);

            var renderer =
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

                    var guid = model.CustomXmlPartGuid;

                    renderer.Properties = new Dictionary<string, string>();
                    renderer.Properties.Add("Guid", guid);
                    renderer.Cml = cml;

                    // Generate temp file which can be inserted into a content control
                    var tempfileName = renderer.Render();
                    if (File.Exists(tempfileName))
                    {
                        cc = document.ContentControls.Add(Word.WdContentControlType.wdContentControlRichText, ref _missing);
                        Insert2D(document, cc.ID, tempfileName, guid);

                        Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"ContentControl inserted at position {cc.Range.Start}");

                        if (isCopy)
                        {
                            document.CustomXMLParts.Add(XmlHelper.AddHeader(cml));
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

        public static Word.ContentControl Insert1DChemistry(Word.Document document, string text, bool isFormula, string tag)
        {
            var module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod()?.Name}()";

            if (Globals.Chem4WordV3.SystemOptions == null)
            {
                Globals.Chem4WordV3.LoadOptions();
            }

            var app = Globals.Chem4WordV3.Application;

            var wordSettings = new WordSettings(app);

            var cc = document.ContentControls.Add(Word.WdContentControlType.wdContentControlRichText, ref _missing);

            SetRichText(document, cc.ID, text, isFormula);

            Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"ContentControl inserted at position {cc.Range.Start}");

            wordSettings.RestoreSettings(app);

            cc.Tag = tag;
            cc.Title = Constants.ContentControlTitle;
            cc.LockContents = true;

            return cc;
        }

        public static void RefreshAllStructures(Word.Document document)
        {
            var module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod()?.Name}()";

            if (Globals.Chem4WordV3.SystemOptions == null)
            {
                Globals.Chem4WordV3.LoadOptions();
            }

            var renderer =
                Globals.Chem4WordV3.GetRendererPlugIn(
                    Globals.Chem4WordV3.SystemOptions.SelectedRendererPlugIn);

            if (renderer != null)
            {
                foreach (CustomXMLPart xmlPart in document.CustomXMLParts)
                {
                    var cml = xmlPart.XML;

                    var cxmlId = CustomXmlPartHelper.GetCmlId(xmlPart);
                    var cc = new CMLConverter();
                    var model = cc.Import(cml);

                    renderer.Properties = new Dictionary<string, string>();
                    renderer.Properties.Add("Guid", cxmlId);
                    renderer.Cml = cml;

                    var tempfileName = renderer.Render();
                    if (File.Exists(tempfileName))
                    {
                        UpdateThisStructure(document, model, cxmlId, tempfileName);
                    }
                }
            }
        }

        public static void UpdateThisStructure(Word.Document document, Model model, string cxmlId, string tempFilename)
        {
            var module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod()?.Name}()";

            // Use LINQ to get a list of all our ContentControls
            // Using $"{}" to coerce null to empty string
            var targets = (from Word.ContentControl ccs in document.ContentControls
                           orderby ccs.Range.Start
                           where $"{ccs.Title}" == Constants.ContentControlTitle
                                 && $"{ccs.Tag}".Contains(cxmlId)
                           select new KeyValuePair<string, string>(ccs.ID, ccs.Tag)).ToList();

            foreach (var target in targets)
            {
                var ccTag = target.Value;
                var prefix = CustomXmlPartHelper.PrefixFromTag(target.Value);

                if (ccTag != null && ccTag.Equals(cxmlId))
                {
                    // Only 2D Structures if filename supplied
                    if (!string.IsNullOrEmpty(tempFilename))
                    {
                        Update2D(document, target.Key, tempFilename, cxmlId);
                    }
                }
                else
                {
                    // 1D Structures
                    if (prefix.Equals("c0"))
                    {
                        Update1D(document, target.Key, model.ConciseFormula, true, $"c0:{cxmlId}");
                    }
                    else
                    {
                        var isFormula = false;
                        var text = GetInlineText(model, prefix, ref isFormula, out _);
                        Update1D(document, target.Key, text, isFormula, $"{prefix}:{cxmlId}");
                    }
                }
            }
        }

        public static void Insert2D(Word.Document document, string ccId, string tempfileName, string guid)
        {
            var module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod()?.Name}()";

            Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"Inserting 2D structure in ContentControl {ccId} Tag {guid}");

            var application = Globals.Chem4WordV3.Application;

            var wordSettings = new WordSettings(application);

            var contentControl = GetContentControl(document, ccId);

            var bookmarkName = Constants.OoXmlBookmarkPrefix + guid;

            contentControl.Range.InsertFile(tempfileName, bookmarkName);
            if (document.Bookmarks.Exists(bookmarkName))
            {
                document.Bookmarks[bookmarkName].Delete();
            }

            wordSettings.RestoreSettings(application);

            contentControl.Tag = guid;
            contentControl.Title = Constants.ContentControlTitle;
            contentControl.LockContents = true;
        }

        private static void Update2D(Word.Document document, string ccId, string tempfileName, string guid)
        {
            var module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod()?.Name}()";

            Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"Updating 2D structure in ContentControl {ccId} Tag {guid}");

            var cc = GetContentControl(document, ccId);
            if (cc != null)
            {
                var app = Globals.Chem4WordV3.Application;
                var wordSettings = new WordSettings(app);

                cc.LockContents = false;
                if (cc.Type == Word.WdContentControlType.wdContentControlPicture)
                {
                    // Handle old Word 2007 style
                    var range = cc.Range;
                    cc.Delete();
                    cc = document.ContentControls.Add(Word.WdContentControlType.wdContentControlRichText, range);
                    cc.Tag = guid;
                    cc.Title = Constants.ContentControlTitle;
                    cc.Range.Delete();
                }
                else
                {
                    cc.Range.Delete();
                }

                var bookmarkName = Constants.OoXmlBookmarkPrefix + guid;
                cc.Range.InsertFile(tempfileName, bookmarkName);
                if (document.Bookmarks.Exists(bookmarkName))
                {
                    document.Bookmarks[bookmarkName].Delete();
                }

                wordSettings.RestoreSettings(app);

                cc.Tag = guid;
                cc.Title = Constants.ContentControlTitle;
                cc.LockContents = true;
                Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"ContentControl updated at position {cc.Range.Start}");
            }
            else
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Warning", $"Unable to find ContentControl with Id of {ccId}");
            }
        }

        public static void Insert1D(Word.Document document, string ccId, string text, bool isFormula, string tag)
        {
            var module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod()?.Name}()";

            Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"Inserting 1D label in ContentControl {ccId} Tag {tag}");

            var cc = GetContentControl(document, ccId);
            if (cc != null)
            {
                var app = Globals.Chem4WordV3.Application;
                var wordSettings = new WordSettings(app);

                SetRichText(document, cc.ID, text, isFormula);

                Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"ContentControl updated at position {cc.Range.Start}");

                wordSettings.RestoreSettings(app);

                cc.Tag = tag;
                cc.Title = Constants.ContentControlTitle;
                cc.LockContents = true;
            }
        }

        private static void Update1D(Word.Document document, string ccId, string text, bool isFormula, string tag)
        {
            var module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod()?.Name}()";

            Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"Updating 1D label in ContentControl {ccId} Tag {tag}");

            var cc = GetContentControl(document, ccId);
            if (cc != null)
            {
                var app = Globals.Chem4WordV3.Application;
                var wordSettings = new WordSettings(app);

                cc.LockContents = false;
                cc.Range.Delete();

                SetRichText(document, cc.ID, text, isFormula);

                Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"ContentControl updated at position {cc.Range.Start}");

                wordSettings.RestoreSettings(app);

                cc.Tag = tag;
                cc.Title = Constants.ContentControlTitle;
                cc.LockContents = true;
            }
        }

        public static string GetInlineText(Model model, string prefix, ref bool isFormula, out string source)
        {
            var module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod()?.Name}()";

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
                    var parts = FormulaHelper.ParseFormulaIntoParts(tp.Value);
                    isFormula = parts.Count > 0;
                }
            }
            else
            {
                text = $"Unable to find formula or name with id of '{prefix}'";
            }

            return text;
        }

        public static Word.ContentControl GetContentControl(Word.Document document, string id)
        {
            Word.ContentControl result = null;

            foreach (Word.ContentControl contentControl in document.ContentControls)
            {
                if (contentControl.ID.Equals(id))
                {
                    result = contentControl;
                    break;
                }
            }

            return result;
        }

        private static void SetRichText(Word.Document document, string ccId, string text, bool isFormula)
        {
            var module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod()?.Name}()";

            var cc = GetContentControl(document, ccId);
            if (cc != null)
            {
                if (isFormula)
                {
                    var r = cc.Range;
                    var parts = FormulaHelper.ParseFormulaIntoParts(text);
                    foreach (var part in parts)
                    {
                        switch (part.PartType)
                        {
                            case FormulaPartType.Separator:
                            case FormulaPartType.Multiplier:
                                if (!string.IsNullOrEmpty(part.Text))
                                {
                                    r.InsertAfter(part.Text);
                                    r.Font.Subscript = 0;
                                    r.Font.Superscript = 0;
                                    r.Start = cc.Range.End;
                                }
                                break;

                            case FormulaPartType.Element:
                                switch (part.Count)
                                {
                                    case 1: // No Subscript
                                        if (!string.IsNullOrEmpty(part.Text))
                                        {
                                            r.InsertAfter(part.Text);
                                            r.Font.Subscript = 0;
                                            r.Font.Superscript = 0;
                                            r.Start = cc.Range.End;
                                        }
                                        break;

                                    default: // With Subscript
                                        if (!string.IsNullOrEmpty(part.Text))
                                        {
                                            r.InsertAfter(part.Text);
                                            r.Font.Subscript = 0;
                                            r.Font.Superscript = 0;
                                            r.Start = cc.Range.End;
                                        }

                                        if (part.Count > 0)
                                        {
                                            r.InsertAfter($"{part.Count}");
                                            r.Font.Superscript = 0;
                                            r.Font.Subscript = 1;
                                            r.Start = cc.Range.End;
                                        }
                                        break;
                                }
                                break;

                            case FormulaPartType.Charge:
                                int absCharge = Math.Abs(part.Count);
                                if (absCharge > 1)
                                {
                                    r.InsertAfter($"{absCharge}");
                                    r.Font.Subscript = 0;
                                    r.Font.Superscript = 1;
                                    r.Start = cc.Range.End;
                                }
                                if (!string.IsNullOrEmpty(part.Text))
                                {
                                    r.InsertAfter(part.Text);
                                    r.Font.Subscript = 0;
                                    r.Font.Superscript = 1;
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
        }

        public static List<string> GetUsed1D(Word.Document document, string guidString)
        {
            var module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod()?.Name}()";

            // Using $"{}" to coerce null to empty string
            var targets = (from Word.ContentControl ccs in document.ContentControls
                           orderby ccs.Range.Start
                           where $"{ccs.Title}" == Constants.ContentControlTitle
                                 && $"{ccs.Tag}".Contains(guidString)
                                 && !$"{ccs.Tag}".Equals(guidString)
                           select ccs.Tag).Distinct().ToList();

            return targets;
        }

        public static List<string> GetUsed2D(Word.Document document, string guidString)
        {
            var module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod()?.Name}()";

            // Using $"{}" to coerce null to empty string
            var targets = (from Word.ContentControl ccs in document.ContentControls
                           orderby ccs.Range.Start
                           where $"{ccs.Title}" == Constants.ContentControlTitle
                                 && $"{ccs.Tag}".Equals(guidString)
                           select ccs.Tag).Distinct().ToList();

            return targets;
        }
    }
}