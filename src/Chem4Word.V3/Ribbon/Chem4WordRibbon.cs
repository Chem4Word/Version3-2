// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME;
using Chem4Word.Core;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Helpers;
using Chem4Word.Library;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Converters.MDL;
using Chem4Word.Model2.Geometry;
using Chem4Word.Model2.Helpers;
using Chem4Word.Navigator;
using Chem4Word.Telemetry;
using Chem4Word.UI;
using Chem4Word.UI.WPF;
using IChem4Word.Contracts;
using Microsoft.Office.Core;
using Microsoft.Office.Tools.Ribbon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using CustomTaskPane = Microsoft.Office.Tools.CustomTaskPane;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using SaveFileDialog = System.Windows.Forms.SaveFileDialog;
using Word = Microsoft.Office.Interop.Word;

/*
 * ****************************
 * Do NOT Change this Namespace
 * ****************************
 */

// ReSharper disable once CheckNamespace
namespace Chem4Word
{
    public partial class CustomRibbon
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private static object _missing = Type.Missing;

        /*
            Notes :-
            Custom Ribbon Help for Office 2010 VSTO Add-Ins
            http://www.codeproject.com/Articles/463282/Custom-Ribbon-Help-for-Office-VSTO-Add-ins
        */

        private void CustomRibbon_Load(object sender, RibbonUIEventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                Chem4Word.Chem4WordV3.SetGlobalRibbon(this);

                var tab = this.Tabs[0];

                var tabLabel = "Chemistry";
#if DEBUG
                tabLabel += $" (Debug {Constants.Chem4WordVersion})";
#endif
                tab.Label = Globals.Chem4WordV3.WordVersion == 2013 ? tabLabel.ToUpper() : tabLabel;
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        public void ActivateChemistryTab()
        {
            try
            {
                RibbonUI.ActivateTab(Chem4WordV3.ControlId.ToString());
            }
            catch
            {
                // Do Nothing
            }
        }

        private void OnClick_RenderAs(object sender, RibbonControlEventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            if (Globals.Chem4WordV3.Telemetry != null)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            }
            else
            {
                RegistryHelper.StoreMessage(module, "Triggered");
            }

            if (Globals.Chem4WordV3.EventsEnabled)
            {
                Globals.Chem4WordV3.EventsEnabled = false;

                if (Globals.Chem4WordV3.ChemistryAllowed)
                {
                    var application = Globals.Chem4WordV3.Application;
                    var document = application.ActiveDocument;
                    Word.ContentControl contentControl = null;

                    try
                    {
                        var b = sender as RibbonButton;

                        var selection = application.Selection;

                        CustomXMLPart customXmlPart = null;

                        if (selection.ContentControls.Count > 0)
                        {
                            contentControl = selection.ContentControls[1];
                            if (contentControl.Title != null && contentControl.Title.Equals(Constants.ContentControlTitle))
                            {
                                string chosenState = b.Tag.ToString();
                                var prefix = "2D";
                                var guid = contentControl.Tag;
                                if (guid.Contains(":"))
                                {
                                    prefix = contentControl.Tag.Split(':')[0];
                                    guid = contentControl.Tag.Split(':')[1];
                                }

                                if (!prefix.Equals(chosenState))
                                {
                                    var renderer =
                                        Globals.Chem4WordV3.GetRendererPlugIn(
                                            Globals.Chem4WordV3.SystemOptions.SelectedRendererPlugIn);
                                    if (renderer != null)
                                    {
                                        customXmlPart = CustomXmlPartHelper.GetCustomXmlPart(document, guid);
                                        if (customXmlPart != null)
                                        {
                                            // Stop Screen Updating and Disable Document Event Handlers
                                            application.ScreenUpdating = false;
                                            Globals.Chem4WordV3.DisableContentControlEvents();

                                            // Erase old CC
                                            contentControl.LockContents = false;
                                            contentControl.Range.Delete();
                                            contentControl.Delete();

                                            // Insert new CC
                                            contentControl = document.ContentControls.Add(Word.WdContentControlType.wdContentControlRichText, ref _missing);
                                            if (contentControl != null)
                                            {
                                                Globals.Chem4WordV3.SystemOptions.WordTopLeft = Globals.Chem4WordV3.WordTopLeft;

                                                if (chosenState.Equals("2D"))
                                                {
                                                    if (Globals.Chem4WordV3.SystemOptions == null)
                                                    {
                                                        Globals.Chem4WordV3.LoadOptions();
                                                    }

                                                    renderer.Properties = new Dictionary<string, string>();
                                                    renderer.Properties.Add("Guid", guid);
                                                    renderer.Cml = customXmlPart.XML;

                                                    var tempfileName = renderer.Render();
                                                    if (File.Exists(tempfileName))
                                                    {
                                                        ChemistryHelper.Insert2D(document, contentControl.ID, tempfileName, guid);
                                                    }
                                                    else
                                                    {
                                                        contentControl = null;
                                                    }
                                                }
                                                else
                                                {
                                                    var used1D = ChemistryHelper.GetUsed1D(document, CustomXmlPartHelper.GuidFromTag(contentControl.Tag));
                                                    var conv = new CMLConverter();
                                                    var model = conv.Import(customXmlPart.XML, used1D);

                                                    var isFormula = false;
                                                    string text;
                                                    if (chosenState.Equals("c0"))
                                                    {
                                                        Globals.Chem4WordV3.Telemetry.Write(module, "Information", "Render structure as Overall ConciseFormula");
                                                        text = model.ConciseFormula;
                                                        isFormula = true;
                                                    }
                                                    else
                                                    {
                                                        string source;
                                                        text = ChemistryHelper.GetInlineText(model, chosenState, ref isFormula, out source);
                                                        Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"Render structure as {source}");
                                                    }
                                                    ChemistryHelper.Insert1D(document, contentControl.ID, text, isFormula, chosenState + ":" + guid);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // Get out of here
                                return;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                        {
                            form.ShowDialog();
                        }
                    }
                    finally
                    {
                        // Tidy Up - Resume Screen Updating and Enable Document Event Handlers
                        application.ScreenUpdating = true;
                        Globals.Chem4WordV3.EnableContentControlEvents();

                        if (contentControl != null)
                        {
                            application.Selection.SetRange(contentControl.Range.End, contentControl.Range.End);
                        }
                    }
                }

                Globals.Chem4WordV3.EventsEnabled = true;
            }

            AfterButtonChecks(sender as RibbonButton);
        }

        private void AddDynamicMenuItems()
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            try
            {
                ShowAsMenu.Items.Clear();

                var application = Globals.Chem4WordV3.Application;
                var document = application.ActiveDocument;
                var selection = application.Selection;
                Word.ContentControl contentControl = null;
                CustomXMLPart customXmlPart = null;

                if (selection.ContentControls.Count > 0)
                {
                    contentControl = selection.ContentControls[1];
                    if (contentControl.Title != null && contentControl.Title.Equals(Constants.ContentControlTitle))
                    {
                        var prefix = "2D";
                        if (contentControl.Tag.Contains(":"))
                        {
                            prefix = contentControl.Tag.Split(':')[0];
                        }

                        customXmlPart = CustomXmlPartHelper.GetCustomXmlPart(document, contentControl.Tag);
                        if (customXmlPart != null)
                        {
                            var used1D = ChemistryHelper.GetUsed1D(document, CustomXmlPartHelper.GuidFromTag(contentControl.Tag));
                            var cml = customXmlPart.XML;
                            var converter = new CMLConverter();
                            var model = converter.Import(cml, used1D);

                            var list = model.AllTextualProperties;
                            foreach (var item in list)
                            {
                                if (item.IsValid && !item.FullType.ToLower().Contains("auxinfo"))
                                {
                                    var ribbonButton = Factory.CreateRibbonButton();
                                    ribbonButton.Tag = item.Id;
                                    if (prefix.Equals(ribbonButton.Tag))
                                    {
                                        ribbonButton.Image = Properties.Resources.SmallTick;
                                    }
                                    ribbonButton.Label = item.Value;
                                    ribbonButton.Click += OnClick_RenderAs;

                                    switch (item.TypeCode)
                                    {
                                        case "S":
                                            ShowAsMenu.Items.Add(Factory.CreateRibbonSeparator());
                                            break;

                                        case "2D":
                                            ribbonButton.SuperTip = "Render as 2D image";
                                            ShowAsMenu.Items.Add(ribbonButton);
                                            break;

                                        case "N":
                                            ribbonButton.SuperTip = "Render as name";
                                            ShowAsMenu.Items.Add(ribbonButton);
                                            break;

                                        case "F":
                                            if (item.FullType.ToLower().Contains("formula"))
                                            {
                                                var parts = FormulaHelper.ParseFormulaIntoParts(item.Value);
                                                ribbonButton.Label = parts.Count == 0
                                                                        ? item.Value
                                                                        : FormulaHelper.FormulaPartsAsUnicode(parts);
                                            }
                                            if (item.Id.Equals("c0"))
                                            {
                                                ribbonButton.SuperTip = "Render as overall concise formula";
                                            }
                                            else
                                            {
                                                ribbonButton.SuperTip = "Render as " + (item.Id.EndsWith(".f0") ? "concise" : "") + " formula";
                                            }
                                            ShowAsMenu.Items.Add(ribbonButton);
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        private void OnClick_DrawOrEdit(object sender, RibbonControlEventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            if (Globals.Chem4WordV3.Telemetry != null)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            }
            else
            {
                RegistryHelper.StoreMessage(module, "Triggered");
            }

            if (Globals.Chem4WordV3.EventsEnabled && Globals.Chem4WordV3.ChemistryAllowed)
            {
                Globals.Chem4WordV3.EventsEnabled = false;

                PerformEdit();

                Globals.Chem4WordV3.EventsEnabled = true;
            }

            AfterButtonChecks(sender as RibbonButton);
        }

        private void OnClick_Options(object sender, RibbonControlEventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            BeforeButtonChecks();
            if (Globals.Chem4WordV3.Telemetry != null)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            }
            else
            {
                RegistryHelper.StoreMessage(module, "Triggered");
            }

            using (var cursor = new WaitCursor())
            {
                if (Globals.Chem4WordV3.EventsEnabled)
                {
                    Globals.Chem4WordV3.EventsEnabled = false;
                    var app = Globals.Chem4WordV3.Application;

                    try
                    {
                        var settingsHost = new Chem4WordSettingsHost(true);
                        var options = Globals.Chem4WordV3.SystemOptions.Clone();
                        options.SettingsPath = Globals.Chem4WordV3.AddInInfo.ProductAppDataPath;
                        settingsHost.SystemOptions = options;
                        settingsHost.TopLeft = Globals.Chem4WordV3.WordTopLeft;
                        settingsHost.SystemOptions.WordTopLeft = Globals.Chem4WordV3.WordTopLeft;

                        var dr = settingsHost.ShowDialog();
                        if (dr == DialogResult.OK)
                        {
                            Globals.Chem4WordV3.SystemOptions = settingsHost.SystemOptions.Clone();
                            // Re create telemetry object as it may now be disabled
                            Globals.Chem4WordV3.Telemetry = new TelemetryWriter(
                                Globals.Chem4WordV3.SystemOptions.TelemetryEnabled,
                                Globals.Chem4WordV3.IsBeta,
                                Globals.Chem4WordV3.Helper);
                            if (settingsHost.SystemOptions.Errors.Any())
                            {
                                Globals.Chem4WordV3.Telemetry.Write(module, "Exception", string.Join(Environment.NewLine, settingsHost.SystemOptions.Errors));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        cursor.Reset();
                        using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                        {
                            form.ShowDialog();
                        }
                    }
                    Globals.Chem4WordV3.EventsEnabled = true;

                    app.ActiveWindow.SetFocus();
                    app.Activate();
                }
            }

            AfterButtonChecks(sender as RibbonButton);
        }

        public static void InsertFile()
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            var application = Globals.Chem4WordV3.Application;
            var activeDocument = application.ActiveDocument;

            try
            {
                Globals.Chem4WordV3.EvaluateChemistryAllowed();
                if (Globals.Chem4WordV3.ChemistryAllowed)
                {
                    var sb = new StringBuilder();
                    sb.Append("All molecule files (*.cml, *.mol, *.sdf)|*.cml;*.mol;*.sdf");
                    sb.Append("|CML molecule files (*.cml)|*.cml");
                    sb.Append("|MDL molecule files (*.mol, *.sdf)|*.mol;*.sdf");

                    var ofd = new OpenFileDialog();
                    ofd.Filter = sb.ToString();

                    var dialogResult = ofd.ShowDialog();
                    if (dialogResult == DialogResult.OK)
                    {
                        Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"Importing file '{ofd.SafeFileName}'");
                        if (ofd.FileName != null)
                        {
                            if (FileSystemHelper.IsBinary(ofd.FileName))
                            {
                                Globals.Chem4WordV3.Telemetry.Write(module, "Warning", $"File '{ofd.SafeFileName}' detected as binary");
                                UserInteractions.InformUser("Sorry, Binary files such as images and office documents etc can't be imported");
                            }
                            else
                            {
                                var fileType = Path.GetExtension(ofd.FileName).ToLower();
                                Model model = null;
                                var mol = string.Empty;
                                var cml = string.Empty;

                                using (var fileStream = new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                                {
                                    using (var textReader = new StreamReader(fileStream, true))
                                    {
                                        mol = textReader.ReadToEnd();
                                    }
                                }

                                switch (fileType)
                                {
                                    case ".cml":
                                        var cmlConverter = new CMLConverter();
                                        model = cmlConverter.Import(mol);
                                        break;

                                    case ".mol":
                                    case ".sdf":
                                        var sdFileConverter = new SdFileConverter();
                                        model = sdFileConverter.Import(mol);
                                        break;

                                    default:
                                        // No need to do anything as model is already null
                                        break;
                                }

                                if (model != null)
                                {
                                    dialogResult = DialogResult.OK;
                                    if (model.GeneralErrors.Count > 0 || model.AllErrors.Count > 0 || model.AllWarnings.Count > 0)
                                    {
                                        if (model.AllErrors.Count > 0)
                                        {
                                            Globals.Chem4WordV3.Telemetry.Write(module, "Exception(Data)", string.Join(Environment.NewLine, model.AllErrors));
                                        }
                                        if (model.GeneralErrors.Count > 0)
                                        {
                                            Globals.Chem4WordV3.Telemetry.Write(module, "Exception(Data)", string.Join(Environment.NewLine, model.GeneralErrors));
                                        }
                                        if (model.AllWarnings.Count > 0)
                                        {
                                            Globals.Chem4WordV3.Telemetry.Write(module, "Exception(Data)", string.Join(Environment.NewLine, model.AllWarnings));
                                        }

                                        var importErrors = new ImportErrors();
                                        importErrors.TopLeft = Globals.Chem4WordV3.WordTopLeft;
                                        model.ScaleToAverageBondLength(Globals.Chem4WordV3.SystemOptions.BondLength);
                                        importErrors.Model = model;
                                        dialogResult = importErrors.ShowDialog();
                                    }

                                    if (dialogResult == DialogResult.OK)
                                    {
                                        model.CustomXmlPartGuid = Guid.NewGuid().ToString("N");

                                        // Remove Explicit Hydrogens if required
                                        if (Globals.Chem4WordV3.SystemOptions.RemoveExplicitHydrogensOnImportFromFile)
                                        {
                                            model.RemoveExplicitHydrogens();
                                        }

                                        var outcome = model.EnsureBondLength(Globals.Chem4WordV3.SystemOptions.BondLength,
                                                               Globals.Chem4WordV3.SystemOptions.SetBondLengthOnImportFromFile);
                                        if (!string.IsNullOrEmpty(outcome))
                                        {
                                            Globals.Chem4WordV3.Telemetry.Write(module, "Information", outcome);
                                        }

                                        var cmlConverter = new CMLConverter();
                                        cml = cmlConverter.Export(model);
                                        if (model.TotalAtomsCount > 0)
                                        {
                                            var cc = ChemistryHelper.Insert2DChemistry(activeDocument, cml, true);
                                            if (cc != null)
                                            {
                                                // Move selection point into the Content Control which was just inserted
                                                application.Selection.SetRange(cc.Range.Start, cc.Range.End);
                                            }
                                        }
                                        else
                                        {
                                            if (model.Molecules.Any() && model.Molecules.Values.First().Names.Any())
                                            {
                                                var cc = ChemistryHelper.Insert1DChemistry(activeDocument, model.Molecules.Values.First().Names[0].Value, false,
                                                                                           $"{model.Molecules.Values.First().Names[0].Id}:{model.CustomXmlPartGuid}");
                                                activeDocument.CustomXMLParts.Add(XmlHelper.AddHeader(cml));
                                                if (cc != null)
                                                {
                                                    // Move selection point into the Content Control which was just inserted
                                                    application.Selection.SetRange(cc.Range.Start, cc.Range.End);
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (mol.ToLower().Contains("v3000"))
                                    {
                                        UserInteractions.InformUser("Sorry, V3000 molfiles are not supported");
                                    }
                                    else
                                    {
                                        var x = new Exception("Could not import file");
                                        Globals.Chem4WordV3.Telemetry.Write(module, "Exception(Data)", mol);
                                        using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, x))
                                        {
                                            form.ShowDialog();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    UserInteractions.InformUser("Can't insert chemistry here because " + Globals.Chem4WordV3.ChemistryProhibitedReason);
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        private void BeforeButtonChecks()
        {
            if (Globals.Chem4WordV3.SystemOptions == null)
            {
                Globals.Chem4WordV3.LoadOptions();
            }

            if (Globals.Chem4WordV3.PlugInsHaveBeenLoaded)
            {
                Globals.Chem4WordV3.EvaluateChemistryAllowed();
            }
        }

        private void AfterButtonChecks(RibbonButton button)
        {
            if (Globals.Chem4WordV3.Telemetry != null)
            {
                RegistryHelper.SendMsiActions();
                RegistryHelper.SendSetupActions();
                RegistryHelper.SendUpdateActions();
                RegistryHelper.SendMessages();
                RegistryHelper.SendExceptions();
            }

            if (Globals.Chem4WordV3.SystemOptions == null)
            {
                Globals.Chem4WordV3.LoadOptions();
            }

            if (Globals.Chem4WordV3.OptionsReloadRequired)
            {
                Globals.Chem4WordV3.SystemOptions = new Chem4WordOptions(Globals.Chem4WordV3.SystemOptions.SettingsPath);
                Globals.Chem4WordV3.OptionsReloadRequired = false;
            }

            // Only do update check if we are not coming from an update button
            var checkForUpdates = true;
            if (button != null)
            {
                checkForUpdates = !button.Label.ToLower().Contains("update");
            }

            if (checkForUpdates)
            {
                if (Globals.Chem4WordV3.SystemOptions != null)
                {
                    UpdateHelper.CheckForUpdates(Globals.Chem4WordV3.SystemOptions.AutoUpdateFrequency);
                }
            }

            if (Globals.Chem4WordV3.PlugInsHaveBeenLoaded)
            {
                Globals.Chem4WordV3.EvaluateChemistryAllowed();
                Globals.Chem4WordV3.ShowOrHideUpdateShield();

                if (Globals.Chem4WordV3.ChemistryAllowed)
                {
                    Globals.Chem4WordV3.SelectChemistry(Globals.Chem4WordV3.Application.Selection);
                }
            }
        }

        public static void PerformEdit()
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            using (var cursor = new WaitCursor())
            {
                if (Globals.Chem4WordV3.IsEnabled)
                {
                    Globals.Chem4WordV3.Telemetry.Write(module, "Information", "Started");

                    var application = Globals.Chem4WordV3.Application;
                    var document = application.ActiveDocument;
                    Word.ContentControl contentControl = null;
                    var wordSettings = new WordSettings(application);

                    try
                    {
                        if (document != null)
                        {
                            if (Globals.Chem4WordV3.SystemOptions == null)
                            {
                                Globals.Chem4WordV3.LoadOptions();
                            }

                            var editor = Globals.Chem4WordV3.GetEditorPlugIn(Globals.Chem4WordV3.SystemOptions.SelectedEditorPlugIn);

                            if (editor == null)
                            {
                                UserInteractions.WarnUser("Unable to find an Editor Plug-In");
                            }
                            else
                            {
                                Globals.Chem4WordV3.EvaluateChemistryAllowed();
                                if (Globals.Chem4WordV3.ChemistryAllowed)
                                {
                                    CustomXMLPart customXmlPart = null;
                                    var beforeCml = editor.RequiresSeedAtom
                                        ? Properties.Resources.SingleCarbon_cml
                                        : Properties.Resources.EmptyStructure_cml;

                                    var isNewDrawing = true;

                                    var sel = application.Selection;

                                    if (sel.ContentControls.Count > 0)
                                    {
                                        contentControl = sel.ContentControls[1];
                                        if (contentControl.Title != null && contentControl.Title.Equals(Constants.ContentControlTitle))
                                        {
                                            customXmlPart = CustomXmlPartHelper.GetCustomXmlPart(document, contentControl.Tag);
                                            if (customXmlPart != null)
                                            {
                                                beforeCml = customXmlPart.XML;
                                                var cmlConverter = new CMLConverter();
                                                var beforeModel = cmlConverter.Import(beforeCml);

                                                if (beforeModel.AllErrors.Count > 0)
                                                {
                                                    Globals.Chem4WordV3.Telemetry.Write(module, "Exception(Data)", string.Join(Environment.NewLine, beforeModel.AllErrors));
                                                }

                                                if (beforeModel.GeneralErrors.Count > 0)
                                                {
                                                    Globals.Chem4WordV3.Telemetry.Write(module, "Exception(Data)", string.Join(Environment.NewLine, beforeModel.GeneralErrors));
                                                }

                                                if (beforeModel.AllWarnings.Count > 0)
                                                {
                                                    Globals.Chem4WordV3.Telemetry.Write(module, "Exception(Data)", string.Join(Environment.NewLine, beforeModel.AllWarnings));
                                                }

                                                if (beforeModel.TotalAtomsCount == 0 && !beforeModel.HasReactions)
                                                {
                                                    UserInteractions.InformUser("This chemistry item has no 2D data to edit!\nPlease use the 'Edit Labels' button.");
                                                    return;
                                                }

                                                if (beforeModel.HasReactions && !editor.CanEditReactions)
                                                {
                                                    UserInteractions.InformUser("This chemistry item has Reactions!\nPlease use ACME to edit this structure.");
                                                    return;
                                                }

                                                if (beforeModel.HasFunctionalGroups && !editor.CanEditFunctionalGroups)
                                                {
                                                    UserInteractions.InformUser("This chemistry item has Functional Groups!\nPlease use ACME to edit this structure.");
                                                    return;
                                                }

                                                if (beforeModel.HasNestedMolecules && !editor.CanEditNestedMolecules)
                                                {
                                                    UserInteractions.InformUser("This chemistry item has Nested molecules!\nPlease use ACME to edit this structure.");
                                                    return;
                                                }

                                                isNewDrawing = false;
                                            }
                                            else
                                            {
                                                Globals.Chem4WordV3.Telemetry.Write(module, "Exception", $"Can't find CML for {contentControl.Tag} in Active Document");
                                                UserInteractions.WarnUser("The CML for this chemistry item can't be found!");
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            // Get out of here
                                            return;
                                        }
                                    }

                                    string guidString;
                                    string fullTag;

                                    if (isNewDrawing)
                                    {
                                        guidString = Guid.NewGuid().ToString("N"); // No dashes
                                        fullTag = guidString;
                                    }
                                    else
                                    {
                                        fullTag = contentControl.Tag;
                                        guidString = CustomXmlPartHelper.GuidFromTag(fullTag);
                                        if (string.IsNullOrEmpty(guidString))
                                        {
                                            guidString = Guid.NewGuid().ToString("N"); // No dashes
                                        }
                                    }

                                    if (isNewDrawing)
                                    {
                                        Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"Starting create new structure {fullTag}");
                                    }
                                    else
                                    {
                                        Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"Starting edit existing structure {fullTag}");
                                    }

                                    var used1D = ChemistryHelper.GetUsed1D(document, guidString);

                                    editor.Cml = beforeCml;
                                    editor.Used1DProperties = used1D;
                                    var chemEditorResult = editor.Edit();

                                    if (chemEditorResult == DialogResult.OK)
                                    {
                                        // Stop Screen Updating and Disable Document Event Handlers
                                        application.ScreenUpdating = false;
                                        Globals.Chem4WordV3.DisableContentControlEvents();

                                        var cmlConverter = new CMLConverter();

                                        var afterModel = cmlConverter.Import(editor.Cml, used1D);

                                        if (afterModel.AllErrors.Count == 0 && afterModel.AllWarnings.Count == 0)
                                        {
                                            var pc = new WebServices.PropertyCalculator(Globals.Chem4WordV3.Telemetry,
                                                                                        Globals.Chem4WordV3.WordTopLeft,
                                                                                        Globals.Chem4WordV3.AddInInfo.AssemblyVersionNumber);
                                            afterModel.CreatorGuid = Globals.Chem4WordV3.Helper.MachineId;
                                            var changedProperties = pc.CalculateProperties(afterModel);

                                            if (isNewDrawing)
                                            {
                                                Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"Finished creating new structure {fullTag}");
                                            }
                                            else
                                            {
                                                Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"Finished editing existing structure {fullTag}");
                                            }

                                            // Copy back CustomXmlPartGuid which will get lost if edited via ChemDoodle Web
                                            if (string.IsNullOrEmpty(afterModel.CustomXmlPartGuid))
                                            {
                                                afterModel.CustomXmlPartGuid = guidString;
                                            }

                                            #region Show Label Editor

                                            if (changedProperties > 0)
                                            {
                                                afterModel.SetAnyMissingNameIds();
                                                afterModel.ReLabelGuids();
                                                afterModel.Relabel(true);

                                                using (var host =
                                                       new EditLabelsHost(
                                                           new AcmeOptions(Globals.Chem4WordV3.AddInInfo.ProductAppDataPath)))
                                                {
                                                    host.TopLeft = Globals.Chem4WordV3.WordTopLeft;
                                                    host.Cml = cmlConverter.Export(afterModel);
                                                    host.Used1D = used1D;

                                                    host.Message = "Warning: At least one formula or name has changed; Please correct or delete any which are unnecessary or irrelevant !";

                                                    // Show Label Editor
                                                    var dr = host.ShowDialog();
                                                    if (dr == DialogResult.OK)
                                                    {
                                                        afterModel = cmlConverter.Import(host.Cml, used1D);
                                                    }

                                                    host.Close();
                                                }
                                            }

                                            #endregion Show Label Editor

                                            var afterCml = cmlConverter.Export(afterModel);

                                            Globals.Chem4WordV3.SystemOptions.WordTopLeft = Globals.Chem4WordV3.WordTopLeft;
                                            var renderer =
                                                Globals.Chem4WordV3.GetRendererPlugIn(
                                                    Globals.Chem4WordV3.SystemOptions.SelectedRendererPlugIn);

                                            if (renderer == null)
                                            {
                                                UserInteractions.WarnUser("Unable to find a Renderer Plug-In");
                                            }
                                            else
                                            {
                                                // Always render the file.
                                                renderer.Properties = new Dictionary<string, string>();
                                                renderer.Properties.Add("Guid", guidString);
                                                renderer.Cml = afterCml;

                                                var tempfileName = renderer.Render();

                                                var readyToInsert = true;

                                                if (!isNewDrawing)
                                                {
                                                    Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"Erasing old ContentControl {contentControl.ID}");

                                                    contentControl = ChemistryHelper.GetContentControl(document, contentControl.ID);
                                                    if (contentControl != null)
                                                    {
                                                        // Erase old CC
                                                        contentControl.LockContents = false;
                                                        if (contentControl.Type == Word.WdContentControlType.wdContentControlPicture)
                                                        {
                                                            contentControl.Range.InlineShapes[1].Delete();
                                                        }
                                                        else
                                                        {
                                                            contentControl.Range.Delete();
                                                        }

                                                        contentControl.Delete();
                                                    }
                                                    else
                                                    {
                                                        readyToInsert = false;
                                                    }
                                                }

                                                if (readyToInsert)
                                                {
                                                    // Insert a new CC
                                                    contentControl = document.ContentControls.Add(Word.WdContentControlType.wdContentControlRichText, ref _missing);
                                                    Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"New ContentControl {contentControl.ID} created. HasReactions:{afterModel.HasReactions}");

                                                    contentControl.Title = Constants.ContentControlTitle;
                                                    if (isNewDrawing)
                                                    {
                                                        contentControl.Tag = guidString;
                                                    }
                                                    else
                                                    {
                                                        contentControl.Tag = fullTag;
                                                    }

                                                    if (File.Exists(tempfileName))
                                                    {
                                                        ChemistryHelper.UpdateThisStructure(document, afterModel, guidString, tempfileName);

                                                        #region Replace CustomXMLPart with our new cml

                                                        if (customXmlPart != null)
                                                        {
                                                            customXmlPart.Delete();
                                                        }

                                                        document.CustomXMLParts.Add(XmlHelper.AddHeader(afterCml));

                                                        #endregion Replace CustomXMLPart with our new cml

                                                        // Delete the temporary file now we are finished with it
                                                        try
                                                        {
#if !DEBUG
                                                        // Only delete file in release mode
                                                        File.Delete(tempfileName);
#endif
                                                        }
                                                        catch
                                                        {
                                                            // Not much we can do here
                                                        }
                                                    }
                                                    else
                                                    {
                                                        contentControl.Delete();
                                                        contentControl = null;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            // Editing aborted due to errors
                                            if (afterModel.GeneralErrors.Count > 0)
                                            {
                                                Globals.Chem4WordV3.Telemetry.Write(module, "Exception(Data)", string.Join(Environment.NewLine, afterModel.GeneralErrors));
                                            }
                                            if (afterModel.AllErrors.Count > 0)
                                            {
                                                Globals.Chem4WordV3.Telemetry.Write(module, "Exception(Data)", string.Join(Environment.NewLine, afterModel.AllErrors));
                                            }
                                            if (afterModel.AllWarnings.Count > 0)
                                            {
                                                Globals.Chem4WordV3.Telemetry.Write(module, "Exception(Data)", string.Join(Environment.NewLine, afterModel.AllWarnings));
                                            }

                                            var importErrors = new ImportErrors();
                                            importErrors.TopLeft = Globals.Chem4WordV3.WordTopLeft;
                                            importErrors.Model = afterModel;
                                            importErrors.ShowDialog();
                                        }
                                    }
                                    else
                                    {
                                        // Editing cancelled
                                    }
                                }
                                else
                                {
                                    UserInteractions.InformUser("Can't edit chemistry here because " + Globals.Chem4WordV3.ChemistryProhibitedReason);
                                }
                            }
                        }
                    }
                    catch (COMException cex)
                    {
                        if (Globals.Chem4WordV3.Telemetry == null)
                        {
                            RegistryHelper.StoreException(module, cex);
                        }
                        else
                        {
                            Globals.Chem4WordV3.Telemetry.Write(module, "Exception", cex.Message);
                            Globals.Chem4WordV3.Telemetry.Write(module, "Exception", cex.StackTrace);
                        }
                    }
                    catch (Exception ex)
                    {
                        cursor.Reset();
                        using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                        {
                            form.ShowDialog();
                        }
                    }
                    finally
                    {
                        // Tidy Up - Resume Screen Updating and Enable Document Event Handlers
                        application.ScreenUpdating = true;
                        Globals.Chem4WordV3.EnableContentControlEvents();

                        if (contentControl != null)
                        {
                            // Move selection point into the Content Control which was just edited or added
                            application.Selection.SetRange(contentControl.Range.Start, contentControl.Range.End);
                            Globals.Chem4WordV3.SelectChemistry(application.Selection);
                        }
                        else
                        {
                            Globals.Chem4WordV3.Telemetry.Write(module, "Information", "Finished; No ContentControl was inserted");
                        }

                        wordSettings.RestoreSettings(application);
                        application.ActiveWindow.SetFocus();
                        application.Activate();
                    }
                }
            }
        }

        private void OnClick_ViewCml(object sender, RibbonControlEventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            if (Globals.Chem4WordV3.Telemetry != null)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            }
            else
            {
                RegistryHelper.StoreMessage(module, "Triggered");
            }

            if (Globals.Chem4WordV3.EventsEnabled && Globals.Chem4WordV3.ChemistryAllowed)
            {
                Globals.Chem4WordV3.EventsEnabled = false;
                var application = Globals.Chem4WordV3.Application;

                try
                {
                    var selection = application.Selection;
                    Word.ContentControl contentControl = null;
                    CustomXMLPart customXmlPart = null;

                    if (selection.ContentControls.Count > 0)
                    {
                        contentControl = selection.ContentControls[1];
                        if (contentControl.Title != null && contentControl.Title.Equals(Constants.ContentControlTitle))
                        {
                            var document = Globals.Chem4WordV3.Application.ActiveDocument;
                            customXmlPart = CustomXmlPartHelper.GetCustomXmlPart(document, contentControl.Tag);
                            if (customXmlPart != null)
                            {
                                var viewer = new XmlViewer();
                                viewer.TopLeft = Globals.Chem4WordV3.WordTopLeft;
                                viewer.XmlString = customXmlPart.XML;
                                viewer.ShowDialog();
                            }
                            application.Selection.SetRange(contentControl.Range.Start, contentControl.Range.End);
                        }
                    }
                }
                catch (Exception ex)
                {
                    using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                    {
                        form.ShowDialog();
                    }
                }

                application.ActiveWindow.SetFocus();
                application.Activate();

                Globals.Chem4WordV3.EventsEnabled = true;
            }

            AfterButtonChecks(sender as RibbonButton);
        }

        private void OnClick_Import(object sender, RibbonControlEventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            if (Globals.Chem4WordV3.Telemetry != null)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            }
            else
            {
                RegistryHelper.StoreMessage(module, "Triggered");
            }

            if (Globals.Chem4WordV3.EventsEnabled && Globals.Chem4WordV3.ChemistryAllowed)
            {
                Globals.Chem4WordV3.EventsEnabled = false;

                InsertFile();

                Globals.Chem4WordV3.EventsEnabled = true;
            }

            AfterButtonChecks(sender as RibbonButton);
        }

        private void OnClick_Export(object sender, RibbonControlEventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            if (Globals.Chem4WordV3.Telemetry != null)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            }
            else
            {
                RegistryHelper.StoreMessage(module, "Triggered");
            }

            if (Globals.Chem4WordV3.EventsEnabled && Globals.Chem4WordV3.ChemistryAllowed)
            {
                Globals.Chem4WordV3.EventsEnabled = false;

                ExportFile();

                Globals.Chem4WordV3.EventsEnabled = true;
            }

            AfterButtonChecks(sender as RibbonButton);
        }

        private void ExportFile()
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                var application = Globals.Chem4WordV3.Application;
                var selection = application.Selection;
                Word.ContentControl contentControl = null;
                CustomXMLPart customXmlPart = null;

                if (selection.ContentControls.Count > 0)
                {
                    contentControl = selection.ContentControls[1];
                    if (contentControl.Title != null && contentControl.Title.Equals(Constants.ContentControlTitle))
                    {
                        var document = Globals.Chem4WordV3.Application.ActiveDocument;
                        customXmlPart = CustomXmlPartHelper.GetCustomXmlPart(document, contentControl.Tag);
                        if (customXmlPart != null)
                        {
                            var cmlConverter = new CMLConverter();
                            var model = cmlConverter.Import(customXmlPart.XML);
                            model.CustomXmlPartGuid = "";

                            var sfd = new SaveFileDialog();
                            sfd.Filter = "CML molecule files (*.cml)|*.cml|MDL molecule files (*.mol, *.sdf)|*.mol;*.sdf";
                            var dr = sfd.ShowDialog();
                            if (dr == DialogResult.OK)
                            {
                                var fi = new FileInfo(sfd.FileName);
                                Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"Exporting to '{fi.Name}'");
                                var fileType = Path.GetExtension(sfd.FileName).ToLower();
                                switch (fileType)
                                {
                                    case ".cml":
                                        var temp = XmlHelper.AddHeader(cmlConverter.Export(model));
                                        File.WriteAllText(sfd.FileName, temp);
                                        break;

                                    case ".mol":
                                    case ".sdf":
                                        // https://www.chemaxon.com/marvin-archive/6.0.2/marvin/help/formats/mol-csmol-doc.html
                                        var before = model.MeanBondLength;
                                        // Set bond length to 1.54 angstroms (Å)
                                        model.ScaleToAverageBondLength(1.54);
                                        var after = model.MeanBondLength;
                                        Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"Structure rescaled from {SafeDouble.AsString(before)} to {SafeDouble.AsString(after)}");
                                        var converter = new SdFileConverter();
                                        model.CreatorGuid = Globals.Chem4WordV3.Helper.MachineId;
                                        File.WriteAllText(sfd.FileName, converter.Export(model));
                                        break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        private void OnClick_EditLabels(object sender, RibbonControlEventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            if (Globals.Chem4WordV3.Telemetry != null)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            }
            else
            {
                RegistryHelper.StoreMessage(module, "Triggered");
            }

            using (var cursor = new WaitCursor())
            {
                if (Globals.Chem4WordV3.EventsEnabled && Globals.Chem4WordV3.ChemistryAllowed)
                {
                    Globals.Chem4WordV3.EventsEnabled = false;

                    var application = Globals.Chem4WordV3.Application;

                    try
                    {
                        var selection = application.Selection;
                        var document = application.ActiveDocument;
                        Word.ContentControl contentControl = null;
                        CustomXMLPart customXmlPart = null;

                        if (selection.ContentControls.Count > 0)
                        {
                            contentControl = selection.ContentControls[1];
                            if (contentControl.Title != null && contentControl.Title.Equals(Constants.ContentControlTitle))
                            {
                                var guid = CustomXmlPartHelper.GuidFromTag(contentControl.Tag);

                                var used1D = ChemistryHelper.GetUsed1D(document, guid);
                                customXmlPart = CustomXmlPartHelper.GetCustomXmlPart(document, contentControl.Tag);
                                if (customXmlPart != null)
                                {
                                    var cml = customXmlPart.XML;

                                    using (var host =
                                        new EditLabelsHost(
                                            new AcmeOptions(Globals.Chem4WordV3.AddInInfo.ProductAppDataPath)))
                                    {
                                        host.TopLeft = Globals.Chem4WordV3.WordTopLeft;
                                        host.Cml = cml;
                                        host.Used1D = used1D;
                                        host.Message = "";

                                        var result = host.ShowDialog();
                                        if (result == DialogResult.OK)
                                        {
                                            var afterCml = host.Cml;
                                            customXmlPart.Delete();
                                            document.CustomXMLParts.Add(XmlHelper.AddHeader(afterCml));

                                            var renderer =
                                                Globals.Chem4WordV3.GetRendererPlugIn(
                                                    Globals.Chem4WordV3.SystemOptions.SelectedRendererPlugIn);
                                            if (renderer == null)
                                            {
                                                UserInteractions.WarnUser("Unable to find a Renderer Plug-In");
                                            }
                                            else
                                            {
                                                // Always render the file.
                                                renderer.Properties = new Dictionary<string, string>();
                                                renderer.Properties.Add("Guid", guid);
                                                renderer.Cml = afterCml;

                                                var tempfileName = renderer.Render();

                                                if (File.Exists(tempfileName))
                                                {
                                                    var converter = new CMLConverter();
                                                    var model = converter.Import(afterCml, used1D);
                                                    ChemistryHelper.UpdateThisStructure(document, model, guid, tempfileName);

                                                    // Delete the temporary file now we are finished with it
                                                    try
                                                    {
                                                        File.Delete(tempfileName);
                                                    }
                                                    catch
                                                    {
                                                        // Not much we can do here
                                                    }
                                                }
                                            }

                                            application.Selection.SetRange(contentControl.Range.Start, contentControl.Range.End);
                                        }

                                        host.Close();
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        cursor.Reset();
                        using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                        {
                            form.ShowDialog();
                        }
                    }
                    finally
                    {
                        application.ActiveWindow.SetFocus();
                        application.Activate();
                    }

                    Globals.Chem4WordV3.EventsEnabled = true;
                }
            }

            AfterButtonChecks(sender as RibbonButton);
        }

        private void OnLoading_ViewAsItems(object sender, RibbonControlEventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            if (Globals.Chem4WordV3.Telemetry != null)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            }
            else
            {
                RegistryHelper.StoreMessage(module, "Triggered");
            }

            try
            {
                if (Globals.Chem4WordV3.ChemistryAllowed)
                {
                    AddDynamicMenuItems();
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        private void OnLoading_SearchItems(object sender, RibbonControlEventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            if (Globals.Chem4WordV3.Telemetry != null)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            }
            else
            {
                RegistryHelper.StoreMessage(module, "Triggered");
            }

            try
            {
                if (Globals.Chem4WordV3.ChemistryAllowed)
                {
                    WebSearchMenu.Items.Clear();

                    if (Globals.Chem4WordV3.Searchers != null)
                    {
                        foreach (var searcher in Globals.Chem4WordV3.Searchers.OrderBy(s => s.DisplayOrder))
                        {
                            if (searcher.DisplayOrder >= 0)
                            {
                                var ribbonButton = this.Factory.CreateRibbonButton();

                                ribbonButton.Label = searcher.ShortName;
                                ribbonButton.Tag = searcher.Name;
                                ribbonButton.SuperTip = searcher.Description;
                                ribbonButton.Image = searcher.Image;
                                ribbonButton.Click += OnClick_Searcher;

                                WebSearchMenu.Items.Add(ribbonButton);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        private void OnClick_Searcher(object sender, RibbonControlEventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            if (Globals.Chem4WordV3.Telemetry != null)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            }
            else
            {
                RegistryHelper.StoreMessage(module, "Triggered");
            }

            if (Globals.Chem4WordV3.EventsEnabled && Globals.Chem4WordV3.ChemistryAllowed)
            {
                Globals.Chem4WordV3.EventsEnabled = false;
                var application = Globals.Chem4WordV3.Application;

                try
                {
                    var clicked = sender as RibbonButton;
                    if (clicked != null)
                    {
                        IChem4WordSearcher searcher = Globals.Chem4WordV3.GetSearcherPlugIn(clicked.Tag);
                        if (searcher != null)
                        {
                            var dr = searcher.Search();
                            if (dr == DialogResult.OK && !string.IsNullOrEmpty(searcher.Cml))
                            {
                                var document = Globals.Chem4WordV3.Application.ActiveDocument;
                                var cmlConverter = new CMLConverter();
                                var model = cmlConverter.Import(searcher.Cml);
                                model.CustomXmlPartGuid = Guid.NewGuid().ToString("N");

                                // Remove Explicit Hydrogens if required
                                if (Globals.Chem4WordV3.SystemOptions.RemoveExplicitHydrogensOnImportFromSearch)
                                {
                                    model.RemoveExplicitHydrogens();
                                }

                                var outcome = model.EnsureBondLength(Globals.Chem4WordV3.SystemOptions.BondLength,
                                                       Globals.Chem4WordV3.SystemOptions.SetBondLengthOnImportFromSearch);
                                if (!string.IsNullOrEmpty(outcome))
                                {
                                    Globals.Chem4WordV3.Telemetry.Write(module, "Information", outcome);
                                }

                                var cc = ChemistryHelper.Insert2DChemistry(document, cmlConverter.Export(model), true);
                                if (cc != null)
                                {
                                    // Move selection point into the Content Control which was just inserted
                                    Globals.Chem4WordV3.Application.Selection.SetRange(cc.Range.Start, cc.Range.End);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                    {
                        form.ShowDialog();
                    }
                }

                Globals.Chem4WordV3.EventsEnabled = true;

                application.ActiveWindow.SetFocus();
                application.Activate();
            }

            AfterButtonChecks(sender as RibbonButton);
        }

        private void OnClick_SaveToLibrary(object sender, RibbonControlEventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            if (Globals.Chem4WordV3.Telemetry != null)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            }
            else
            {
                RegistryHelper.StoreMessage(module, "Triggered");
            }

            if (Globals.Chem4WordV3.EventsEnabled && Globals.Chem4WordV3.ChemistryAllowed)
            {
                Globals.Chem4WordV3.EventsEnabled = false;

                try
                {
                    var application = Globals.Chem4WordV3.Application;
                    var selection = application.Selection;
                    Word.ContentControl cc = null;
                    CustomXMLPart customXmlPart = null;

                    if (selection.ContentControls.Count > 0)
                    {
                        Model model = null;
                        cc = selection.ContentControls[1];
                        if (cc.Title != null && cc.Title.Equals(Constants.ContentControlTitle))
                        {
                            var document = Globals.Chem4WordV3.Application.ActiveDocument;
                            customXmlPart = CustomXmlPartHelper.GetCustomXmlPart(document, cc.Tag);
                            if (customXmlPart != null)
                            {
                                var cml = customXmlPart.XML;
                                model = new CMLConverter().Import(cml);
                                if (model.TotalAtomsCount > 0)
                                {
                                    if (Globals.Chem4WordV3.LibraryNames == null)
                                    {
                                        Globals.Chem4WordV3.LoadNamesFromLibrary();
                                    }

                                    var lib = new Libraries.Database.Library(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.LibraryOptions);
                                    var transaction = lib.StartTransaction();
                                    var done = lib.ImportCml(cml, transaction);
                                    lib.EndTransaction(transaction, !done);

                                    // Re- Read the Library Names
                                    Globals.Chem4WordV3.LoadNamesFromLibrary();

                                    UserInteractions.InformUser($"Structure '{model.ConciseFormula}' added into Library");
                                    Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"Structure '{model.ConciseFormula}' added into Library");
                                }
                                else
                                {
                                    UserInteractions.InformUser("Only chemistry with at least one Atom can be saved into the library.");
                                }
                            }

                            CustomTaskPane custTaskPane = null;
                            foreach (var taskPane in Globals.Chem4WordV3.CustomTaskPanes)
                            {
                                if (application.ActiveWindow == taskPane.Window && taskPane.Title == Constants.LibraryTaskPaneTitle)
                                {
                                    custTaskPane = taskPane;
                                }
                            }

                            if (custTaskPane != null)
                            {
                                (custTaskPane.Control as LibraryHost)?.Clear();
                                (custTaskPane.Control as LibraryHost)?.Refresh();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                    {
                        form.ShowDialog();
                    }
                }
                Globals.Chem4WordV3.EventsEnabled = true;
            }

            AfterButtonChecks(sender as RibbonButton);
        }

        private void OnClick_Navigator(object sender, RibbonControlEventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            if (Globals.Chem4WordV3.Telemetry != null)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            }
            else
            {
                RegistryHelper.StoreMessage(module, "Triggered");
            }

            try
            {
                //see https://msdn.microsoft.com/en-us/library/bb608620(v=vs.100).aspx

                var application = Globals.Chem4WordV3.Application;

                if (Globals.Chem4WordV3.EventsEnabled)
                {
                    application.System.Cursor = Word.WdCursorType.wdCursorWait;

                    if (application.Documents.Count > 0)
                    {
                        CustomTaskPane custTaskPane = null;
                        foreach (var taskPane in Globals.Chem4WordV3.CustomTaskPanes)
                        {
                            if (application.ActiveWindow == taskPane.Window && taskPane.Title == Constants.NavigatorTaskPaneTitle)
                            {
                                custTaskPane = taskPane;
                            }
                        }

                        if (ShowNavigator.Checked)
                        {
                            if (custTaskPane == null)
                            {
                                custTaskPane =
                                    Globals.Chem4WordV3.CustomTaskPanes.Add(new NavigatorHost(application.ActiveDocument),
                                        Constants.NavigatorTaskPaneTitle, application.ActiveWindow);

                                custTaskPane.Width = Globals.Chem4WordV3.WordWidth / 4;
                                custTaskPane.VisibleChanged += OnVisibleChanged_NavigatorPane;
                            }
                            custTaskPane.Visible = true;
                            Globals.Chem4WordV3.EvaluateChemistryAllowed();
                        }
                        else
                        {
                            if (custTaskPane != null)
                            {
                                custTaskPane.Visible = false;
                            }
                        }
                    }
                    else
                    {
                        ShowNavigator.Checked = false;
                    }

                    application.System.Cursor = Word.WdCursorType.wdCursorNormal;
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }

            AfterButtonChecks(sender as RibbonButton);
        }

        private void OnVisibleChanged_NavigatorPane(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                var taskPane = sender as CustomTaskPane;

                if (Globals.Chem4WordV3.EventsEnabled && taskPane != null)
                {
                    Word.Window window = taskPane.Window;
                    if (window != null)
                    {
                        var documentName = window.Document.Name;
                        var activeDocument = Globals.Chem4WordV3.Application.ActiveDocument;

                        if (documentName.Equals(activeDocument.Name) && ShowNavigator.Checked != taskPane.Visible)
                        {
                            ShowNavigator.Checked = taskPane.Visible;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        private void OnClick_ShowLibrary(object sender, RibbonControlEventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            if (Globals.Chem4WordV3.Telemetry != null)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            }
            else
            {
                RegistryHelper.StoreMessage(module, "Triggered");
            }

            if (Globals.Chem4WordV3.LibraryNames == null)
            {
                Globals.Chem4WordV3.LoadNamesFromLibrary();
            }
            try
            {
                // See https://msdn.microsoft.com/en-us/library/bb608590.aspx
                var app = Globals.Chem4WordV3.Application;
                using (new WaitCursor())
                {
                    if (Globals.Chem4WordV3.EventsEnabled)
                    {
                        if (app.Documents.Count > 0)
                        {
                            CustomTaskPane custTaskPane = null;
                            foreach (var taskPane in Globals.Chem4WordV3.CustomTaskPanes)
                            {
                                if (app.ActiveWindow == taskPane.Window && taskPane.Title == Constants.LibraryTaskPaneTitle)
                                {
                                    custTaskPane = taskPane;
                                }
                            }

                            Globals.Chem4WordV3.LibraryState = ShowLibrary.Checked;
                            ShowLibrary.Label = ShowLibrary.Checked ? "Close" : "Open";

                            if (ShowLibrary.Checked)
                            {
                                if (custTaskPane == null)
                                {
                                    custTaskPane =
                                        Globals.Chem4WordV3.CustomTaskPanes.Add(new LibraryHost(),
                                            Constants.LibraryTaskPaneTitle, app.ActiveWindow);

                                    // Opposite side to Navigator's default placement
                                    custTaskPane.DockPosition = MsoCTPDockPosition.msoCTPDockPositionLeft;

                                    custTaskPane.Width = Globals.Chem4WordV3.WordWidth / 4;
                                    custTaskPane.VisibleChanged += OnVisibleChanged_LibraryPane;
                                    (custTaskPane.Control as LibraryHost)?.Refresh();
                                }
                                custTaskPane.Visible = true;
                                Globals.Chem4WordV3.EvaluateChemistryAllowed();
                            }
                            else
                            {
                                if (custTaskPane != null)
                                {
                                    custTaskPane.Visible = false;
                                }
                            }
                        }
                        else
                        {
                            ShowLibrary.Checked = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }

            AfterButtonChecks(sender as RibbonButton);
        }

        public void OnVisibleChanged_LibraryPane(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                var taskPane = sender as CustomTaskPane;

                if (Globals.Chem4WordV3.EventsEnabled && taskPane != null)
                {
                    Globals.Chem4WordV3.LibraryState = taskPane.Visible;

                    Word.Window window = taskPane.Window;
                    if (window != null)
                    {
                        var documentName = window.Document.Name;
                        var activeDocument = Globals.Chem4WordV3.Application.ActiveDocument;

                        if (documentName.Equals(activeDocument.Name))
                        {
                            if (ShowLibrary.Checked != taskPane.Visible)
                            {
                                ShowLibrary.Checked = taskPane.Visible;
                            }
                            if (ShowLibrary.Checked)
                            {
                                (taskPane.Control as LibraryHost)?.Refresh();
                            }
                            ShowLibrary.Label = ShowLibrary.Checked ? "Close" : "Open";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        private void OnClick_Separate(object sender, RibbonControlEventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            if (Globals.Chem4WordV3.Telemetry != null)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            }
            else
            {
                RegistryHelper.StoreMessage(module, "Triggered");
            }

            if (Globals.Chem4WordV3.EventsEnabled && Globals.Chem4WordV3.ChemistryAllowed)
            {
                Globals.Chem4WordV3.EventsEnabled = false;

                var application = Globals.Chem4WordV3.Application;
                var document = application.ActiveDocument;

                Word.ContentControl contentControl = null;

                // Stop Screen Updating and Disable Document Event Handlers
                application.ScreenUpdating = false;
                Globals.Chem4WordV3.DisableContentControlEvents();

                try
                {
                    CustomXMLPart customXmlPart = null;
                    var sel = application.Selection;

                    if (sel.ContentControls.Count > 0)
                    {
                        var renderer =
                            Globals.Chem4WordV3.GetRendererPlugIn(
                                Globals.Chem4WordV3.SystemOptions.SelectedRendererPlugIn);
                        if (renderer == null)
                        {
                            UserInteractions.WarnUser("Unable to find a Renderer Plug-In");
                        }
                        else
                        {
                            contentControl = sel.ContentControls[1];
                            if (contentControl.Title != null && contentControl.Title.Equals(Constants.ContentControlTitle))
                            {
                                var fullTag = contentControl.Tag;
                                var guidString = CustomXmlPartHelper.GuidFromTag(contentControl.Tag);

                                customXmlPart = CustomXmlPartHelper.GetCustomXmlPart(document, contentControl.Tag);
                                if (customXmlPart != null)
                                {
                                    var beforeCml = customXmlPart.XML;
                                    var cmlConverter = new CMLConverter();
                                    var model = cmlConverter.Import(beforeCml);

                                    if (model.HasReactions)
                                    {
                                        UserInteractions.InformUser("It is not appropriate to run the arrange function on chemistry which has reactions!");
                                        return;
                                    }
                                    else
                                    {
                                        var packer = new Packer();
                                        packer.Model = model;
                                        packer.Pack(model.MeanBondLength * 2);

                                        //Separator separator = new Separator(model)
                                        //int loops = 0
                                        //separator.Separate(model.MeanBondLength, 99, out loops)
                                        //Debug.WriteLine($"Separate took {loops} loops")

                                        var afterCml = cmlConverter.Export(model);

                                        if (Globals.Chem4WordV3.SystemOptions == null)
                                        {
                                            Globals.Chem4WordV3.LoadOptions();
                                        }

                                        renderer.Properties = new Dictionary<string, string>();
                                        renderer.Properties.Add("Guid", guidString);
                                        renderer.Cml = afterCml;

                                        var tempfile = renderer.Render();

                                        if (File.Exists(tempfile))
                                        {
                                            contentControl.LockContents = false;
                                            contentControl.Range.Delete();
                                            contentControl.Delete();

                                            // Insert a new CC
                                            contentControl = document.ContentControls.Add(Word.WdContentControlType.wdContentControlRichText, ref _missing);

                                            contentControl.Title = Constants.ContentControlTitle;
                                            contentControl.Tag = fullTag;

                                            ChemistryHelper.UpdateThisStructure(document, model, guidString, tempfile);

                                            customXmlPart.Delete();
                                            document.CustomXMLParts.Add(XmlHelper.AddHeader(afterCml));

                                            // Delete the temporary file now we are finished with it
                                            try
                                            {
                                                File.Delete(tempfile);
                                            }
                                            catch
                                            {
                                                // Not much we can do here
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                    {
                        form.ShowDialog();
                    }
                }
                finally
                {
                    // Tidy Up - Resume Screen Updating and Enable Document Event Handlers
                    application.ScreenUpdating = true;
                    Globals.Chem4WordV3.EnableContentControlEvents();

                    if (contentControl != null)
                    {
                        // Move selection point into the Content Control which was just edited or added
                        application.Selection.SetRange(contentControl.Range.Start, contentControl.Range.End);
                    }
                }
                Globals.Chem4WordV3.EventsEnabled = true;
            }

            AfterButtonChecks(sender as RibbonButton);
        }

        private void OnClick_Update(object sender, RibbonControlEventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            if (Globals.Chem4WordV3.Telemetry != null)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            }
            else
            {
                RegistryHelper.StoreMessage(module, "Triggered");
            }

            if (Globals.Chem4WordV3.EventsEnabled && Globals.Chem4WordV3.PlugInsHaveBeenLoaded)
            {
                Globals.Chem4WordV3.EventsEnabled = false;

                try
                {
                    if (Globals.Chem4WordV3.ThisVersion == null || Globals.Chem4WordV3.AllVersions == null)
                    {
                        using (new WaitCursor())
                        {
                            UpdateHelper.FetchUpdateInfo();
                        }
                    }
                    UpdateHelper.ShowUpdateForm();
                }
                catch (Exception ex)
                {
                    using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                    {
                        form.ShowDialog();
                    }
                }

                Globals.Chem4WordV3.EventsEnabled = true;
            }

            AfterButtonChecks(sender as RibbonButton);
        }

        private void OnClick_ShowAbout(object sender, RibbonControlEventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            if (Globals.Chem4WordV3.Telemetry != null)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            }
            else
            {
                RegistryHelper.StoreMessage(module, "Triggered");
            }

            using (var cursor = new WaitCursor())
            {
                if (Globals.Chem4WordV3.EventsEnabled)
                {
                    Globals.Chem4WordV3.EventsEnabled = false;

                    var app = Globals.Chem4WordV3.Application;

                    Cursor.Current = Cursors.WaitCursor;
                    try
                    {
                        var ah = new AboutHost();
                        ah.TopLeft = Globals.Chem4WordV3.WordTopLeft;

                        var assembly = Assembly.GetExecutingAssembly();
                        var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                        UpdateHelper.ReadThisVersion(assembly);
                        if (Globals.Chem4WordV3.ThisVersion != null)
                        {
                            var temp = Globals.Chem4WordV3.ThisVersion.Root.Element("Number").Value;
                            var idx = temp.IndexOf(" ", StringComparison.InvariantCulture);
                            ah.VersionString = $"Chem4Word 2022 {temp.Substring(idx + 1)}";
                        }
                        else
                        {
                            ah.VersionString = $"Chem4Word 2022 {fvi.FileVersion}";
                        }
                        ah.ShowDialog();
                    }
                    catch (Exception ex)
                    {
                        cursor.Reset();
                        using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                        {
                            form.ShowDialog();
                        }
                    }

                    Globals.Chem4WordV3.EventsEnabled = true;

                    app.ActiveWindow.SetFocus();
                    app.Activate();
                }
            }

            AfterButtonChecks(sender as RibbonButton);
        }

        private void OnClick_ShowHome(object sender, RibbonControlEventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            if (Globals.Chem4WordV3.Telemetry != null)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            }
            else
            {
                RegistryHelper.StoreMessage(module, "Triggered");
            }

            if (Globals.Chem4WordV3.EventsEnabled)
            {
                Globals.Chem4WordV3.EventsEnabled = false;

                try
                {
                    Process.Start("https://www.chem4word.co.uk");
                }
                catch (Exception ex)
                {
                    using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                    {
                        form.ShowDialog();
                    }
                }

                Globals.Chem4WordV3.EventsEnabled = true;
            }

            AfterButtonChecks(sender as RibbonButton);
        }

        private void OnClick_CheckForUpdates(object sender, RibbonControlEventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            if (Globals.Chem4WordV3.Telemetry != null)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            }
            else
            {
                RegistryHelper.StoreMessage(module, "Triggered");
            }

            if (Globals.Chem4WordV3.EventsEnabled)
            {
                Globals.Chem4WordV3.EventsEnabled = false;

                UpdateHelper.ClearSettings();

                if (Globals.Chem4WordV3.SystemOptions == null)
                {
                    Globals.Chem4WordV3.LoadOptions();
                }

                if (Globals.Chem4WordV3.SystemOptions != null)
                {
                    var behind = UpdateHelper.CheckForUpdates(Globals.Chem4WordV3.SystemOptions.AutoUpdateFrequency);
                    if (Globals.Chem4WordV3.IsEndOfLife)
                    {
                        UserInteractions.InformUser("This version of Chem4Word is no longer supported");
                    }
                    else
                    {
                        if (behind == 0)
                        {
                            UserInteractions.InformUser("Your version of Chem4Word is the latest");
                        }
                    }
                }
                else
                {
                    UserInteractions.InformUser("Unable to check for updates because Chem4Word has not been initialised.");
                }

                Globals.Chem4WordV3.EventsEnabled = true;
            }

            AfterButtonChecks(sender as RibbonButton);
        }

        private void OnClick_ReadManual(object sender, RibbonControlEventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            if (Globals.Chem4WordV3.Telemetry != null)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            }
            else
            {
                RegistryHelper.StoreMessage(module, "Triggered");
            }

            if (Globals.Chem4WordV3.EventsEnabled && Globals.Chem4WordV3.PlugInsHaveBeenLoaded)
            {
                Globals.Chem4WordV3.EventsEnabled = false;

                const string fileNameOfManual = "Chem4Word-Version3-2-User-Manual.docx";
                try
                {
                    var userManual = Path.Combine(Globals.Chem4WordV3.AddInInfo.DeploymentPath, "Manual", fileNameOfManual);
                    if (File.Exists(userManual))
                    {
                        Globals.Chem4WordV3.Telemetry.Write(module, "ReadManual", userManual);
                        Globals.Chem4WordV3.Application.Documents.Open(userManual, ReadOnly: true);
                    }
                    else
                    {
                        // This code is used when this is not an installed version of Chem4Word
                        userManual = Path.Combine(Globals.Chem4WordV3.AddInInfo.DeploymentPath, @"..\..\..\..\docs", fileNameOfManual);
                        if (File.Exists(userManual))
                        {
                            Globals.Chem4WordV3.Telemetry.Write(module, "ReadManual", userManual);
                            Globals.Chem4WordV3.Application.Documents.Open(userManual, ReadOnly: true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                    {
                        form.ShowDialog();
                    }
                }

                Globals.Chem4WordV3.EventsEnabled = true;
            }
            else
            {
                UserInteractions.InformUser("Unable to locate user manual because Chem4Word has not been initialised.");
            }

            AfterButtonChecks(sender as RibbonButton);
        }

        private void OnClick_YouTube(object sender, RibbonControlEventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            if (Globals.Chem4WordV3.Telemetry != null)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            }
            else
            {
                RegistryHelper.StoreMessage(module, "Triggered");
            }

            if (Globals.Chem4WordV3.EventsEnabled)
            {
                Globals.Chem4WordV3.EventsEnabled = false;

                try
                {
                    Process.Start("https://www.youtube.com/@chem4word");
                }
                catch (Exception ex)
                {
                    using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                    {
                        form.ShowDialog();
                    }
                }

                Globals.Chem4WordV3.EventsEnabled = true;
            }

            AfterButtonChecks(sender as RibbonButton);
        }

        private void OnClick_ButtonsDisabled(object sender, RibbonControlEventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            BeforeButtonChecks();

            if (Globals.Chem4WordV3.Telemetry != null)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            }
            else
            {
                RegistryHelper.StoreMessage(module, "Triggered");
            }

            try
            {
                if (Globals.Chem4WordV3.PlugInsHaveBeenLoaded)
                {
                    Globals.Chem4WordV3.EvaluateChemistryAllowed();
                    UserInteractions.InformUser($"Chem4Word buttons are disabled because {Globals.Chem4WordV3.ChemistryProhibitedReason}");
                }
                else
                {
                    UserInteractions.InformUser("Chem4Word buttons are disabled because no plug Ins were found.");
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }

            AfterButtonChecks(sender as RibbonButton);
        }

        private void OnClick_ShowSystemInfo(object sender, RibbonControlEventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            if (Globals.Chem4WordV3.Telemetry != null)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            }
            else
            {
                RegistryHelper.StoreMessage(module, "Triggered");
            }

            try
            {
                if (Globals.Chem4WordV3.PlugInsHaveBeenLoaded)
                {
                    var fa = new SystemInfo();
                    fa.TopLeft = Globals.Chem4WordV3.WordTopLeft;

                    fa.ShowDialog();
                }
                else
                {
                    UserInteractions.InformUser("System Info is unavailable because Chem4Word has not been initialised.");
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }

            AfterButtonChecks(sender as RibbonButton);
        }
    }
}