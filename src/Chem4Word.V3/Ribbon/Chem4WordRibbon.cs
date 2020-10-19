// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
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
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                Chem4Word.Chem4WordV3.SetGlobalRibbon(this);

                RibbonTab tab = this.Tabs[0];

                string tabLabel = "Chemistry";
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

        private void OnRenderAsButtonClick(object sender, RibbonControlEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            if (Globals.Chem4WordV3.EventsEnabled)
            {
                Globals.Chem4WordV3.EventsEnabled = false;

                if (Globals.Chem4WordV3.ChemistryAllowed)
                {
                    Word.Application app = Globals.Chem4WordV3.Application;
                    Word.Document doc = app.ActiveDocument;
                    Word.ContentControl cc = null;

                    try
                    {
                        RibbonButton b = sender as RibbonButton;

                        Word.Selection sel = app.Selection;

                        CustomXMLPart customXmlPart = null;

                        if (sel.ContentControls.Count > 0)
                        {
                            cc = sel.ContentControls[1];
                            if (cc.Title != null && cc.Title.Equals(Constants.ContentControlTitle))
                            {
                                string chosenState = b.Tag.ToString();
                                string prefix = "2D";
                                string guid = cc.Tag;
                                if (guid.Contains(":"))
                                {
                                    prefix = cc.Tag.Split(':')[0];
                                    guid = cc.Tag.Split(':')[1];
                                }

                                if (!prefix.Equals(chosenState))
                                {
                                    IChem4WordRenderer renderer =
                                        Globals.Chem4WordV3.GetRendererPlugIn(
                                            Globals.Chem4WordV3.SystemOptions.SelectedRendererPlugIn);
                                    if (renderer != null)
                                    {
                                        customXmlPart = CustomXmlPartHelper.GetCustomXmlPart(guid, app.ActiveDocument);
                                        if (customXmlPart != null)
                                        {
                                            // Stop Screen Updating and Disable Document Event Handlers
                                            app.ScreenUpdating = false;
                                            Globals.Chem4WordV3.DisableContentControlEvents();

                                            // Erase old CC
                                            cc.LockContents = false;
                                            cc.Range.Delete();
                                            cc.Delete();

                                            // Insert new CC
                                            cc = doc.ContentControls.Add(Word.WdContentControlType.wdContentControlRichText, ref _missing);

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

                                                string tempfileName = renderer.Render();
                                                if (File.Exists(tempfileName))
                                                {
                                                    ChemistryHelper.Insert2D(cc.ID, tempfileName, guid);
                                                }
                                                else
                                                {
                                                    cc = null;
                                                }
                                            }
                                            else
                                            {
                                                var used1D = ChemistryHelper.GetUsed1D(doc, CustomXmlPartHelper.GuidFromTag(cc?.Tag));
                                                CMLConverter conv = new CMLConverter();
                                                Model model = conv.Import(customXmlPart.XML, used1D);

                                                bool isFormula = false;
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
                                                ChemistryHelper.Insert1D(cc.ID, text, isFormula, chosenState + ":" + guid);
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
                        app.ScreenUpdating = true;
                        Globals.Chem4WordV3.EnableContentControlEvents();

                        if (cc != null)
                        {
                            app.Selection.SetRange(cc.Range.End, cc.Range.End);
                        }
                    }
                }

                Globals.Chem4WordV3.EventsEnabled = true;
            }

            AfterButtonChecks(sender as RibbonButton);
        }

        private void AddDynamicMenuItems()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            try
            {
                ShowAsMenu.Items.Clear();

                Word.Application app = Globals.Chem4WordV3.Application;
                Word.Document doc = app.ActiveDocument;
                Word.Selection sel = app.Selection;
                Word.ContentControl cc = null;
                CustomXMLPart customXmlPart = null;

                if (sel.ContentControls.Count > 0)
                {
                    cc = sel.ContentControls[1];
                    if (cc.Title != null && cc.Title.Equals(Constants.ContentControlTitle))
                    {
                        string prefix = "2D";
                        if (cc.Tag.Contains(":"))
                        {
                            prefix = cc?.Tag.Split(':')[0];
                        }

                        Word.Application app1 = Globals.Chem4WordV3.Application;
                        customXmlPart = CustomXmlPartHelper.GetCustomXmlPart(cc?.Tag, app1.ActiveDocument);
                        if (customXmlPart != null)
                        {
                            var used1D = ChemistryHelper.GetUsed1D(doc, CustomXmlPartHelper.GuidFromTag(cc?.Tag));
                            string cml = customXmlPart.XML;
                            CMLConverter conv = new CMLConverter();
                            Model model = conv.Import(cml, used1D);

                            var list = model.AllTextualProperties;
                            foreach (var item in list)
                            {
                                if (item.IsValid && !item.FullType.ToLower().Contains("auxinfo"))
                                {
                                    RibbonButton ribbonButton = Factory.CreateRibbonButton();
                                    ribbonButton.Tag = item.Id;
                                    if (prefix.Equals(ribbonButton.Tag))
                                    {
                                        ribbonButton.Image = Properties.Resources.SmallTick;
                                    }
                                    ribbonButton.Label = item.Value;
                                    ribbonButton.Click += OnRenderAsButtonClick;

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
                                                ribbonButton.Label =
                                                    FormulaHelper.FormulaPartsAsUnicode(
                                                        FormulaHelper.ParseFormulaIntoParts(ribbonButton.Label));
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

        private void OnDrawOrEditClick(object sender, RibbonControlEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            if (Globals.Chem4WordV3.EventsEnabled && Globals.Chem4WordV3.ChemistryAllowed)
            {
                Globals.Chem4WordV3.EventsEnabled = false;

                PerformEdit();

                Globals.Chem4WordV3.EventsEnabled = true;
            }

            AfterButtonChecks(sender as RibbonButton);
        }

        private void OnOptionsClick(object sender, RibbonControlEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            using (var cursor = new WaitCursor())
            {
                if (Globals.Chem4WordV3.EventsEnabled)
                {
                    Globals.Chem4WordV3.EventsEnabled = false;
                    Word.Application app = Globals.Chem4WordV3.Application;

                    try
                    {
                        Chem4WordSettingsHost f = new Chem4WordSettingsHost(true);
                        var options = Globals.Chem4WordV3.SystemOptions.Clone();
                        options.SettingsPath = Globals.Chem4WordV3.AddInInfo.ProductAppDataPath;
                        f.SystemOptions = options;
                        f.TopLeft = Globals.Chem4WordV3.WordTopLeft;
                        f.SystemOptions.WordTopLeft = Globals.Chem4WordV3.WordTopLeft;

                        DialogResult dr = f.ShowDialog();
                        if (dr == DialogResult.OK)
                        {
                            Globals.Chem4WordV3.SystemOptions = f.SystemOptions.Clone();
                            Globals.Chem4WordV3.Telemetry = new TelemetryWriter(Globals.Chem4WordV3.SystemOptions.TelemetryEnabled, Globals.Chem4WordV3.Helper);
                            if (f.SystemOptions.Errors.Any())
                            {
                                Globals.Chem4WordV3.Telemetry.Write(module, "Exception", string.Join(Environment.NewLine, f.SystemOptions.Errors));
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
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            Word.Application app = Globals.Chem4WordV3.Application;
            Word.Document doc = app.ActiveDocument;

            try
            {
                Globals.Chem4WordV3.EvaluateChemistryAllowed();
                if (Globals.Chem4WordV3.ChemistryAllowed)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("All molecule files (*.cml, *.mol, *.sdf)|*.cml;*.mol;*.sdf");
                    sb.Append("|CML molecule files (*.cml)|*.cml");
                    sb.Append("|MDL molecule files (*.mol, *.sdf)|*.mol;*.sdf");

                    OpenFileDialog ofd = new OpenFileDialog();
                    ofd.Filter = sb.ToString();

                    DialogResult dr = ofd.ShowDialog();
                    if (dr == DialogResult.OK)
                    {
                        Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"Importing file '{ofd.SafeFileName}'");
                        if (ofd.FileName != null)
                        {
                            string fileType = Path.GetExtension(ofd.FileName).ToLower();
                            Model model = null;
                            string mol = string.Empty;
                            string cml = string.Empty;

                            using (var fileStream = new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                using (var textReader = new StreamReader(fileStream))
                                {
                                    mol = textReader.ReadToEnd();
                                }
                            }

                            switch (fileType)
                            {
                                case ".cml":
                                    CMLConverter cmlConverter = new CMLConverter();
                                    model = cmlConverter.Import(mol);
                                    break;

                                case ".mol":
                                case ".sdf":
                                    SdFileConverter sdFileConverter = new SdFileConverter();
                                    model = sdFileConverter.Import(mol);
                                    break;

                                default:
                                    // No need to do anything as model is already null
                                    break;
                            }

                            if (model != null)
                            {
                                dr = DialogResult.OK;
                                if (model.AllErrors.Count > 0 || model.AllWarnings.Count > 0)
                                {
                                    if (model.AllErrors.Count > 0)
                                    {
                                        Globals.Chem4WordV3.Telemetry.Write(module, "Exception(Data)", string.Join(Environment.NewLine, model.AllErrors));
                                    }
                                    if (model.AllWarnings.Count > 0)
                                    {
                                        Globals.Chem4WordV3.Telemetry.Write(module, "Exception(Data)", string.Join(Environment.NewLine, model.AllWarnings));
                                    }

                                    ImportErrors f = new ImportErrors();
                                    f.TopLeft = Globals.Chem4WordV3.WordTopLeft;
                                    model.ScaleToAverageBondLength(40);
                                    f.Model = model;
                                    dr = f.ShowDialog();
                                }

                                if (dr == DialogResult.OK)
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

                                    CMLConverter cmlConverter = new CMLConverter();
                                    cml = cmlConverter.Export(model);
                                    if (model.TotalAtomsCount > 0)
                                    {
                                        Word.ContentControl cc = ChemistryHelper.Insert2DChemistry(doc, cml, true);
                                        if (cc != null)
                                        {
                                            // Move selection point into the Content Control which was just inserted
                                            app.Selection.SetRange(cc.Range.Start, cc.Range.End);
                                        }
                                    }
                                    else
                                    {
                                        if (model.Molecules.Any() && model.Molecules.Values.First().Names.Any())
                                        {
                                            Word.ContentControl cc = ChemistryHelper.Insert1DChemistry(doc,
                                                model.Molecules.Values.First().Names[0].Value, false,
                                                $"{model.Molecules.Values.First().Names[0].Id}:{model.CustomXmlPartGuid}");
                                            doc.CustomXMLParts.Add(cml);
                                            if (cc != null)
                                            {
                                                // Move selection point into the Content Control which was just inserted
                                                app.Selection.SetRange(cc.Range.Start, cc.Range.End);
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
                                    Exception x = new Exception("Could not import file");
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

            Globals.Chem4WordV3.EvaluateChemistryAllowed();
        }

        private void AfterButtonChecks(RibbonButton button)
        {
            if (Globals.Chem4WordV3.Telemetry != null)
            {
                RegistryHelper.SendSetupActions();
                RegistryHelper.SendUpdateActions();
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
            bool checkForUpdates = true;
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

            Globals.Chem4WordV3.EvaluateChemistryAllowed();
            Globals.Chem4WordV3.ShowOrHideUpdateShield();
            if (Globals.Chem4WordV3.ChemistryAllowed)
            {
                Globals.Chem4WordV3.SelectChemistry(Globals.Chem4WordV3.Application.Selection);
            }
        }

        public static void PerformEdit()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            using (var cursor = new WaitCursor())
            {
                if (Globals.Chem4WordV3.IsEnabled)
                {
                    Word.Application app = Globals.Chem4WordV3.Application;

                    Globals.Chem4WordV3.Telemetry.Write(module, "Information", "Started");
                    Word.Document doc = app.ActiveDocument;
                    Word.ContentControl cc = null;

                    var wordSettings = new WordSettings(app);

                    try
                    {
                        if (Globals.Chem4WordV3.SystemOptions == null)
                        {
                            Globals.Chem4WordV3.LoadOptions();
                        }

                        IChem4WordEditor editor =
                            Globals.Chem4WordV3.GetEditorPlugIn(Globals.Chem4WordV3.SystemOptions.SelectedEditorPlugIn);

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
                                string beforeCml = editor.RequiresSeedAtom
                                    ? Properties.Resources.SingleCarbon_cml
                                    : Properties.Resources.EmptyStructure_cml;

                                bool isNewDrawing = true;

                                Word.Selection sel = app.Selection;

                                if (sel.ContentControls.Count > 0)
                                {
                                    cc = sel.ContentControls[1];
                                    if (cc.Title != null && cc.Title.Equals(Constants.ContentControlTitle))
                                    {
                                        Word.Application app1 = Globals.Chem4WordV3.Application;
                                        customXmlPart = CustomXmlPartHelper.GetCustomXmlPart(cc?.Tag, app1.ActiveDocument);
                                        if (customXmlPart != null)
                                        {
                                            beforeCml = customXmlPart.XML;
                                            CMLConverter cmlConverter = new CMLConverter();
                                            Model beforeModel = cmlConverter.Import(beforeCml);

                                            if (beforeModel.TotalAtomsCount == 0)
                                            {
                                                UserInteractions.InformUser("This chemistry item has no 2D data to edit!\nPlease use the 'Edit Labels' button.");
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
                                            Globals.Chem4WordV3.Telemetry.Write(module, "Exception", $"Can't find CML for {cc.Tag} in Active Document");
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
                                    fullTag = cc?.Tag;
                                    guidString = CustomXmlPartHelper.GuidFromTag(cc?.Tag);
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

                                var used1D = ChemistryHelper.GetUsed1D(doc, guidString);

                                editor.Cml = beforeCml;
                                editor.Used1DProperties = used1D;
                                DialogResult chemEditorResult = editor.Edit();

                                if (chemEditorResult == DialogResult.OK)
                                {
                                    // Stop Screen Updating and Disable Document Event Handlers
                                    app.ScreenUpdating = false;
                                    Globals.Chem4WordV3.DisableContentControlEvents();

                                    CMLConverter cmlConverter = new CMLConverter();

                                    Model beforeModel = cmlConverter.Import(beforeCml, used1D);
                                    Model afterModel = cmlConverter.Import(editor.Cml, used1D);

                                    var oldMolecules = beforeModel.GetAllMolecules();
                                    var newMolecules = afterModel.GetAllMolecules();

                                    if (!editor.CanEditNestedMolecules)
                                    {
                                        foreach (var molecule in newMolecules)
                                        {
                                            var mol = oldMolecules.FirstOrDefault(m => m.Path.Equals(molecule.Path));
                                            if (mol != null)
                                            {
                                                // Copy over existing Formulae and Names if Paths match
                                                foreach (var formula in mol.Formulas)
                                                {
                                                    molecule.Formulas.Add(formula);
                                                }
                                                foreach (var name in mol.Names)
                                                {
                                                    molecule.Names.Add(name);
                                                }
                                            }
                                        }
                                    }

                                    int changedProperties = ChemistryHelper.CalculateProperties(newMolecules);

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
                                        using (var host =
                                            new EditLabelsHost(
                                                new AcmeOptions(Globals.Chem4WordV3.AddInInfo.ProductAppDataPath)))
                                        {
                                            host.TopLeft = Globals.Chem4WordV3.WordTopLeft;
                                            host.Cml = cmlConverter.Export(afterModel);
                                            host.Used1D = used1D;

                                            host.Message = "Warning: At least one formula or name has changed; Please correct or delete any which are unnecessary or irrelevant !";

                                            // Show Label Editor
                                            DialogResult dr = host.ShowDialog();
                                            if (dr == DialogResult.OK)
                                            {
                                                afterModel = cmlConverter.Import(host.Cml, used1D);
                                            }

                                            host.Close();
                                        }
                                    }

                                    #endregion Show Label Editor

                                    string afterCml = cmlConverter.Export(afterModel);

                                    Globals.Chem4WordV3.SystemOptions.WordTopLeft = Globals.Chem4WordV3.WordTopLeft;
                                    IChem4WordRenderer renderer =
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

                                        string tempfileName = renderer.Render();

                                        if (!isNewDrawing)
                                        {
                                            // Erase old CC
                                            cc.LockContents = false;
                                            if (cc.Type == Word.WdContentControlType.wdContentControlPicture)
                                            {
                                                cc.Range.InlineShapes[1].Delete();
                                            }
                                            else
                                            {
                                                cc.Range.Delete();
                                            }
                                            cc.Delete();
                                        }

                                        // Insert a new CC
                                        cc = doc.ContentControls.Add(Word.WdContentControlType.wdContentControlRichText, ref _missing);

                                        cc.Title = Constants.ContentControlTitle;
                                        if (isNewDrawing)
                                        {
                                            cc.Tag = guidString;
                                        }
                                        else
                                        {
                                            cc.Tag = fullTag;
                                        }

                                        if (File.Exists(tempfileName))
                                        {
                                            ChemistryHelper.UpdateThisStructure(doc, afterModel, guidString, tempfileName);

                                            #region Replace CustomXMLPart with our new cml

                                            if (customXmlPart != null)
                                            {
                                                customXmlPart.Delete();
                                            }

                                            doc.CustomXMLParts.Add(afterCml);

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
                                            cc.Delete();
                                            cc = null;
                                        }
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
                        app.ScreenUpdating = true;
                        Globals.Chem4WordV3.EnableContentControlEvents();

                        if (cc != null)
                        {
                            // Move selection point into the Content Control which was just edited or added
                            app.Selection.SetRange(cc.Range.Start, cc.Range.End);
                            Globals.Chem4WordV3.SelectChemistry(app.Selection);
                        }
                        else
                        {
                            Globals.Chem4WordV3.Telemetry.Write(module, "Information", "Finished; No ContentControl was inserted");
                        }

                        wordSettings.RestoreSettings(app);
                        app.ActiveWindow.SetFocus();
                        app.Activate();
                    }
                }
            }
        }

        private void OnViewCmlClick(object sender, RibbonControlEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            if (Globals.Chem4WordV3.EventsEnabled && Globals.Chem4WordV3.ChemistryAllowed)
            {
                Globals.Chem4WordV3.EventsEnabled = false;
                Word.Application app = Globals.Chem4WordV3.Application;

                try
                {
                    Word.Selection sel = app.Selection;
                    Word.ContentControl cc = null;
                    CustomXMLPart customXmlPart = null;

                    if (sel.ContentControls.Count > 0)
                    {
                        cc = sel.ContentControls[1];
                        if (cc.Title != null && cc.Title.Equals(Constants.ContentControlTitle))
                        {
                            Word.Application app1 = Globals.Chem4WordV3.Application;
                            customXmlPart = CustomXmlPartHelper.GetCustomXmlPart(cc?.Tag, app1.ActiveDocument);
                            if (customXmlPart != null)
                            {
                                XmlViewer viewer = new XmlViewer();
                                viewer.TopLeft = Globals.Chem4WordV3.WordTopLeft;
                                viewer.XmlString = customXmlPart.XML;
                                viewer.ShowDialog();
                            }
                            app.Selection.SetRange(cc.Range.Start, cc.Range.End);
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

                app.ActiveWindow.SetFocus();
                app.Activate();

                Globals.Chem4WordV3.EventsEnabled = true;
            }

            AfterButtonChecks(sender as RibbonButton);
        }

        private void OnImportClick(object sender, RibbonControlEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            if (Globals.Chem4WordV3.EventsEnabled && Globals.Chem4WordV3.ChemistryAllowed)
            {
                Globals.Chem4WordV3.EventsEnabled = false;

                InsertFile();

                Globals.Chem4WordV3.EventsEnabled = true;
            }

            AfterButtonChecks(sender as RibbonButton);
        }

        private void OnExportClick(object sender, RibbonControlEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

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
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                Word.Application app = Globals.Chem4WordV3.Application;
                Word.Selection sel = app.Selection;
                Word.ContentControl cc = null;
                CustomXMLPart customXmlPart = null;

                if (sel.ContentControls.Count > 0)
                {
                    cc = sel.ContentControls[1];
                    if (cc.Title != null && cc.Title.Equals(Constants.ContentControlTitle))
                    {
                        Word.Application app1 = Globals.Chem4WordV3.Application;
                        customXmlPart = CustomXmlPartHelper.GetCustomXmlPart(cc?.Tag, app1.ActiveDocument);
                        if (customXmlPart != null)
                        {
                            CMLConverter cmlConverter = new CMLConverter();
                            var model = cmlConverter.Import(customXmlPart.XML);
                            model.CustomXmlPartGuid = "";

                            SaveFileDialog sfd = new SaveFileDialog();
                            sfd.Filter = "CML molecule files (*.cml)|*.cml|MDL molecule files (*.mol, *.sdf)|*.mol;*.sdf";
                            DialogResult dr = sfd.ShowDialog();
                            if (dr == DialogResult.OK)
                            {
                                FileInfo fi = new FileInfo(sfd.FileName);
                                Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"Exporting to '{fi.Name}'");
                                string fileType = Path.GetExtension(sfd.FileName).ToLower();
                                switch (fileType)
                                {
                                    case ".cml":
                                        string temp = "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
                                            + Environment.NewLine
                                            + cmlConverter.Export(model);
                                        File.WriteAllText(sfd.FileName, temp);
                                        break;

                                    case ".mol":
                                    case ".sdf":
                                        // https://www.chemaxon.com/marvin-archive/6.0.2/marvin/help/formats/mol-csmol-doc.html
                                        double before = model.MeanBondLength;
                                        // Set bond length to 1.54 angstroms (Å)
                                        model.ScaleToAverageBondLength(1.54);
                                        double after = model.MeanBondLength;
                                        Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"Structure rescaled from {SafeDouble.AsString(before)} to {SafeDouble.AsString(after)}");
                                        SdFileConverter converter = new SdFileConverter();
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

        private void OnEditLabelsClick(object sender, RibbonControlEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            using (var cursor = new WaitCursor())
            {
                if (Globals.Chem4WordV3.EventsEnabled && Globals.Chem4WordV3.ChemistryAllowed)
                {
                    Globals.Chem4WordV3.EventsEnabled = false;

                    Word.Application app = Globals.Chem4WordV3.Application;

                    try
                    {
                        Word.Document doc = app.ActiveDocument;
                        Word.Selection sel = app.Selection;
                        Word.ContentControl cc = null;
                        CustomXMLPart customXmlPart = null;

                        if (sel.ContentControls.Count > 0)
                        {
                            cc = sel.ContentControls[1];
                            if (cc.Title != null && cc.Title.Equals(Constants.ContentControlTitle))
                            {
                                Word.Application app1 = Globals.Chem4WordV3.Application;
                                string guid = CustomXmlPartHelper.GuidFromTag(cc?.Tag);

                                var used1D = ChemistryHelper.GetUsed1D(doc, guid);
                                customXmlPart = CustomXmlPartHelper.GetCustomXmlPart(cc?.Tag, app1.ActiveDocument);
                                if (customXmlPart != null)
                                {
                                    string cml = customXmlPart.XML;

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
                                            string afterCml = host.Cml;
                                            customXmlPart.Delete();
                                            doc.CustomXMLParts.Add(afterCml);

                                            IChem4WordRenderer renderer =
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

                                                string tempfileName = renderer.Render();

                                                if (File.Exists(tempfileName))
                                                {
                                                    CMLConverter conv = new CMLConverter();
                                                    Model model = conv.Import(afterCml, used1D);
                                                    ChemistryHelper.UpdateThisStructure(doc, model, guid, tempfileName);

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

                                            app.Selection.SetRange(cc.Range.Start, cc.Range.End);
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
                        app.ActiveWindow.SetFocus();
                        app.Activate();
                    }

                    Globals.Chem4WordV3.EventsEnabled = true;
                }
            }

            AfterButtonChecks(sender as RibbonButton);
        }

        private void OnViewAsItemsLoading(object sender, RibbonControlEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

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

        private void OnSearchItemsLoading(object sender, RibbonControlEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            try
            {
                if (Globals.Chem4WordV3.ChemistryAllowed)
                {
                    WebSearchMenu.Items.Clear();

                    if (Globals.Chem4WordV3.Searchers != null)
                    {
                        foreach (IChem4WordSearcher searcher in Globals.Chem4WordV3.Searchers.OrderBy(s => s.DisplayOrder))
                        {
                            RibbonButton ribbonButton = this.Factory.CreateRibbonButton();

                            ribbonButton.Label = searcher.ShortName;
                            ribbonButton.Tag = searcher.Name;
                            ribbonButton.SuperTip = searcher.Description;
                            ribbonButton.Image = searcher.Image;
                            ribbonButton.Click += OnSearcherClick;

                            WebSearchMenu.Items.Add(ribbonButton);
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

        private void OnSearcherClick(object sender, RibbonControlEventArgs ribbonControlEventArgs)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            if (Globals.Chem4WordV3.EventsEnabled && Globals.Chem4WordV3.ChemistryAllowed)
            {
                Globals.Chem4WordV3.EventsEnabled = false;
                Word.Application app = Globals.Chem4WordV3.Application;

                try
                {
                    RibbonButton clicked = sender as RibbonButton;
                    if (clicked != null)
                    {
                        IChem4WordSearcher searcher = Globals.Chem4WordV3.GetSearcherPlugIn(clicked.Tag);
                        if (searcher != null)
                        {
                            DialogResult dr = searcher.Search();
                            if (dr == DialogResult.OK)
                            {
                                Word.Document doc = Globals.Chem4WordV3.Application.ActiveDocument;

                                CMLConverter cmlConverter = new CMLConverter();
                                Model model = cmlConverter.Import(searcher.Cml);
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

                                Word.ContentControl cc = ChemistryHelper.Insert2DChemistry(doc, cmlConverter.Export(model), true);
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

                app.ActiveWindow.SetFocus();
                app.Activate();
            }

            AfterButtonChecks(sender as RibbonButton);
        }

        private void OnSaveToLibraryClick(object sender, RibbonControlEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            if (Globals.Chem4WordV3.EventsEnabled && Globals.Chem4WordV3.ChemistryAllowed)
            {
                Globals.Chem4WordV3.EventsEnabled = false;

                try
                {
                    Word.Application app = Globals.Chem4WordV3.Application;
                    Word.Selection sel = app.Selection;
                    Word.ContentControl cc = null;
                    CustomXMLPart customXmlPart = null;

                    if (sel.ContentControls.Count > 0)
                    {
                        Model model = null;
                        cc = sel.ContentControls[1];
                        if (cc.Title != null && cc.Title.Equals(Constants.ContentControlTitle))
                        {
                            Word.Application app1 = Globals.Chem4WordV3.Application;

                            customXmlPart = CustomXmlPartHelper.GetCustomXmlPart(cc.Tag, app1.ActiveDocument);
                            if (customXmlPart != null)
                            {
                                string cml = customXmlPart.XML;
                                model = new CMLConverter().Import(cml);
                                if (model.TotalAtomsCount > 0)
                                {
                                    if (Globals.Chem4WordV3.LibraryNames == null)
                                    {
                                        Globals.Chem4WordV3.LoadNamesFromLibrary();
                                    }

                                    var lib = new Database.Library();
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
                            foreach (CustomTaskPane taskPane in Globals.Chem4WordV3.CustomTaskPanes)
                            {
                                if (app.ActiveWindow == taskPane.Window && taskPane.Title == Constants.LibraryTaskPaneTitle)
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

        private void OnNavigatorClick(object sender, RibbonControlEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            try
            {
                //see https://msdn.microsoft.com/en-us/library/bb608620(v=vs.100).aspx

                Word.Application app = Globals.Chem4WordV3.Application;

                if (Globals.Chem4WordV3.EventsEnabled)
                {
                    app.System.Cursor = Word.WdCursorType.wdCursorWait;

                    if (app.Documents.Count > 0)
                    {
                        CustomTaskPane custTaskPane = null;
                        foreach (CustomTaskPane taskPane in Globals.Chem4WordV3.CustomTaskPanes)
                        {
                            if (app.ActiveWindow == taskPane.Window && taskPane.Title == Constants.NavigatorTaskPaneTitle)
                            {
                                custTaskPane = taskPane;
                            }
                        }

                        if (ShowNavigator.Checked)
                        {
                            if (custTaskPane == null)
                            {
                                custTaskPane =
                                    Globals.Chem4WordV3.CustomTaskPanes.Add(new NavigatorHost(app, app.ActiveDocument),
                                        Constants.NavigatorTaskPaneTitle, app.ActiveWindow);

                                custTaskPane.Width = Globals.Chem4WordV3.WordWidth / 4;
                                custTaskPane.VisibleChanged += OnNavigatorPaneVisibleChanged;
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

                    app.System.Cursor = Word.WdCursorType.wdCursorNormal;
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

        private void OnNavigatorPaneVisibleChanged(object sender, EventArgs eventArgs)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                CustomTaskPane taskPane = sender as CustomTaskPane;
                Word.Application app = Globals.Chem4WordV3.Application;

                if (Globals.Chem4WordV3.EventsEnabled && taskPane != null)
                {
                    Word.Window window = taskPane.Window;
                    if (window != null)
                    {
                        string taskdoc = window.Document.Name;
                        if (taskdoc.Equals(app.ActiveDocument.Name) && ShowNavigator.Checked != taskPane.Visible)
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

        private void OnShowLibraryClick(object sender, RibbonControlEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            if (Globals.Chem4WordV3.LibraryNames == null)
            {
                Globals.Chem4WordV3.LoadNamesFromLibrary();
            }
            try
            {
                // See https://msdn.microsoft.com/en-us/library/bb608590.aspx
                Word.Application app = Globals.Chem4WordV3.Application;
                using (new WaitCursor())
                {
                    if (Globals.Chem4WordV3.EventsEnabled)
                    {
                        if (app.Documents.Count > 0)
                        {
                            CustomTaskPane custTaskPane = null;
                            foreach (CustomTaskPane taskPane in Globals.Chem4WordV3.CustomTaskPanes)
                            {
                                if (app.ActiveWindow == taskPane.Window && taskPane.Title == Constants.LibraryTaskPaneTitle)
                                {
                                    custTaskPane = taskPane;
                                }
                            }

                            Globals.Chem4WordV3.LibraryState = ShowLibrary.Checked;
                            ShowLibrary.Label = ShowLibrary.Checked ? "Close" : "Open ";

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
                                    custTaskPane.VisibleChanged += OnLibraryPaneVisibleChanged;
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

        public void OnLibraryPaneVisibleChanged(object sender, EventArgs eventArgs)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                Word.Application app = Globals.Chem4WordV3.Application;
                CustomTaskPane taskPane = sender as CustomTaskPane;

                if (Globals.Chem4WordV3.EventsEnabled && taskPane != null)
                {
                    Word.Window window = taskPane.Window;
                    if (window != null)
                    {
                        string taskdoc = window.Document.Name;
                        if (taskdoc.Equals(app.ActiveDocument.Name))
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

        private void OnSeparateClick(object sender, RibbonControlEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            if (Globals.Chem4WordV3.EventsEnabled && Globals.Chem4WordV3.ChemistryAllowed)
            {
                Globals.Chem4WordV3.EventsEnabled = false;

                Word.Application app = Globals.Chem4WordV3.Application;
                Word.Document doc = app.ActiveDocument;
                Word.ContentControl cc = null;

                // Stop Screen Updating and Disable Document Event Handlers
                app.ScreenUpdating = false;
                Globals.Chem4WordV3.DisableContentControlEvents();

                try
                {
                    CustomXMLPart customXmlPart = null;
                    Word.Selection sel = app.Selection;

                    if (sel.ContentControls.Count > 0)
                    {
                        IChem4WordRenderer renderer =
                            Globals.Chem4WordV3.GetRendererPlugIn(
                                Globals.Chem4WordV3.SystemOptions.SelectedRendererPlugIn);
                        if (renderer == null)
                        {
                            UserInteractions.WarnUser("Unable to find a Renderer Plug-In");
                        }
                        else
                        {
                            cc = sel.ContentControls[1];
                            if (cc.Title != null && cc.Title.Equals(Constants.ContentControlTitle))
                            {
                                string fullTag = cc.Tag;
                                string guidString = CustomXmlPartHelper.GuidFromTag(cc.Tag);

                                Word.Application app1 = Globals.Chem4WordV3.Application;
                                customXmlPart = CustomXmlPartHelper.GetCustomXmlPart(cc.Tag, app1.ActiveDocument);
                                if (customXmlPart != null)
                                {
                                    string beforeCml = customXmlPart.XML;
                                    CMLConverter cmlConverter = new CMLConverter();
                                    Model model = cmlConverter.Import(beforeCml);

                                    Packer packer = new Packer();
                                    packer.Model = model;
                                    packer.Pack(model.MeanBondLength * 2);

                                    //Separator separator = new Separator(model)
                                    //int loops = 0
                                    //separator.Separate(model.MeanBondLength, 99, out loops)
                                    //Debug.WriteLine($"Separate took {loops} loops")

                                    string afterCml = cmlConverter.Export(model);

                                    if (Globals.Chem4WordV3.SystemOptions == null)
                                    {
                                        Globals.Chem4WordV3.LoadOptions();
                                    }

                                    renderer.Properties = new Dictionary<string, string>();
                                    renderer.Properties.Add("Guid", guidString);
                                    renderer.Cml = afterCml;

                                    string tempfile = renderer.Render();

                                    if (File.Exists(tempfile))
                                    {
                                        cc.LockContents = false;
                                        cc.Range.Delete();
                                        cc.Delete();

                                        // Insert a new CC
                                        cc = doc.ContentControls.Add(Word.WdContentControlType.wdContentControlRichText, ref _missing);

                                        cc.Title = Constants.ContentControlTitle;
                                        cc.Tag = fullTag;

                                        ChemistryHelper.UpdateThisStructure(doc, model, guidString, tempfile);

                                        customXmlPart.Delete();
                                        doc.CustomXMLParts.Add(afterCml);

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
                    app.ScreenUpdating = true;
                    Globals.Chem4WordV3.EnableContentControlEvents();

                    if (cc != null)
                    {
                        // Move selection point into the Content Control which was just edited or added
                        app.Selection.SetRange(cc.Range.Start, cc.Range.End);
                    }
                }
                Globals.Chem4WordV3.EventsEnabled = true;
            }

            AfterButtonChecks(sender as RibbonButton);
        }

        private void OnUpdateClick(object sender, RibbonControlEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            if (Globals.Chem4WordV3.EventsEnabled)
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

        private void OnShowAboutClick(object sender, RibbonControlEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            using (var cursor = new WaitCursor())
            {
                if (Globals.Chem4WordV3.EventsEnabled)
                {
                    Globals.Chem4WordV3.EventsEnabled = false;

                    Word.Application app = Globals.Chem4WordV3.Application;

                    //System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                    System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
                    try
                    {
                        AboutHost ah = new AboutHost();
                        ah.TopLeft = Globals.Chem4WordV3.WordTopLeft;

                        Assembly assembly = Assembly.GetExecutingAssembly();
                        FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                        UpdateHelper.ReadThisVersion(assembly);
                        if (Globals.Chem4WordV3.ThisVersion != null)
                        {
                            string temp = Globals.Chem4WordV3.ThisVersion.Root.Element("Number").Value;
                            int idx = temp.IndexOf(" ", StringComparison.InvariantCulture);
                            ah.VersionString = $"Chem4Word 2020 {temp.Substring(idx + 1)}";
                        }
                        else
                        {
                            ah.VersionString = $"Chem4Word 2020 {fvi.FileVersion}";
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

        private void OnShowHomeClick(object sender, RibbonControlEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

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

        private void OnCheckForUpdatesClick(object sender, RibbonControlEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            if (Globals.Chem4WordV3.EventsEnabled)
            {
                Globals.Chem4WordV3.EventsEnabled = false;

                UpdateHelper.ClearSettings();

                if (Globals.Chem4WordV3.SystemOptions == null)
                {
                    Globals.Chem4WordV3.LoadOptions();
                }

                int behind = UpdateHelper.CheckForUpdates(Globals.Chem4WordV3.SystemOptions.AutoUpdateFrequency);
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

                Globals.Chem4WordV3.EventsEnabled = true;
            }

            AfterButtonChecks(sender as RibbonButton);
        }

        private void OnReadManualClick(object sender, RibbonControlEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            if (Globals.Chem4WordV3.EventsEnabled)
            {
                Globals.Chem4WordV3.EventsEnabled = false;

                const string fileNameOfManual = "Chem4Word-Version3-1-User-Manual.docx";
                try
                {
                    string userManual = Path.Combine(Globals.Chem4WordV3.AddInInfo.DeploymentPath, "Manual", fileNameOfManual);
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

            AfterButtonChecks(sender as RibbonButton);
        }

        private void OnYouTubeClick(object sender, RibbonControlEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            if (Globals.Chem4WordV3.EventsEnabled)
            {
                Globals.Chem4WordV3.EventsEnabled = false;

                try
                {
                    Process.Start("https://www.youtube.com/channel/UCKX2kG9kZ3zoX0nCen5lfpQ");
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

        private void OnButtonsDisabledClick(object sender, RibbonControlEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            try
            {
                Globals.Chem4WordV3.EvaluateChemistryAllowed();
                UserInteractions.InformUser($"Chem4Word buttons are disabled because {Globals.Chem4WordV3.ChemistryProhibitedReason}");
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

        private void ShowSystemInfo_Click(object sender, RibbonControlEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            BeforeButtonChecks();
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            try
            {
                SystemInfo fa = new SystemInfo();
                fa.TopLeft = Globals.Chem4WordV3.WordTopLeft;

                fa.ShowDialog();
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