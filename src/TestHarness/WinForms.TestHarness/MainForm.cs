// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME;
using Chem4Word.Core.Helpers;
using Chem4Word.Editor.ACME;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Converters.JSON;
using Chem4Word.Model2.Converters.MDL;
using Chem4Word.Model2.Helpers;
using Chem4Word.Renderer.OoXmlV4;
using Chem4Word.Searcher.ChEBIPlugin;
using Chem4Word.Searcher.OpsinPlugIn;
using Chem4Word.Searcher.PubChemPlugIn;
using Chem4Word.Shared;
using Chem4Word.Telemetry;
using Chem4Word.WebServices;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using MessageBox = System.Windows.Forms.MessageBox;

namespace WinForms.TestHarness
{
    public partial class FlexForm : Form
    {
        private Stack<Model> _undoStack = new Stack<Model>();
        private Stack<Model> _redoStack = new Stack<Model>();

        private SystemHelper _helper;
        private TelemetryWriter _telemetry;

        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private const string EmptyCml = "<cml></cml>";
        private string _lastCml = EmptyCml;

        private AcmeOptions _editorOptions;
        private OoXmlV4Options _renderOptions;
        private ConfigWatcher _configWatcher;

        public FlexForm()
        {
            InitializeComponent();

            _helper = new SystemHelper();
            _telemetry = new TelemetryWriter(true, true, _helper);

            var location = Assembly.GetExecutingAssembly().Location;
            var path = Path.GetDirectoryName(location);

            // Use either path or null below
            _editorOptions = new AcmeOptions(null);
            _renderOptions = new OoXmlV4Options(null);
        }

        private void LoadStructure_Click(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Model model = null;

                StringBuilder sb = new StringBuilder();
                sb.Append("All molecule files (*.cml, *.xml, *.mol, *.sdf, *.json)|*.cml;*.xml;*.mol;*.sdf;*.json");
                sb.Append("|CML molecule files (*.cml, *.xml)|*.cml;*.xml");
                sb.Append("|MDL molecule files (*.mol, *.sdf)|*.mol;*.sdf");
                sb.Append("|ChemDoodle Web json files (*.json)|*.json");

                openFileDialog1.Title = "Open Structure";
                openFileDialog1.InitialDirectory = Environment.SpecialFolder.MyDocuments.ToString();
                openFileDialog1.Filter = sb.ToString();
                openFileDialog1.FileName = "";
                openFileDialog1.ShowHelp = false;

                DialogResult dr = openFileDialog1.ShowDialog();

                if (dr == DialogResult.OK)
                {
                    string fileType = Path.GetExtension(openFileDialog1.FileName).ToLower();
                    string filename = Path.GetFileName(openFileDialog1.FileName);
                    string mol = File.ReadAllText(openFileDialog1.FileName);

                    CMLConverter cmlConvertor = new CMLConverter();
                    SdFileConverter sdFileConverter = new SdFileConverter();

                    Stopwatch stopwatch;
                    TimeSpan elapsed1 = default;
                    TimeSpan elapsed2;

                    switch (fileType)
                    {
                        case ".mol":
                        case ".sdf":
                            stopwatch = new Stopwatch();
                            stopwatch.Start();
                            model = sdFileConverter.Import(mol);
                            stopwatch.Stop();
                            elapsed1 = stopwatch.Elapsed;
                            break;

                        case ".cml":
                        case ".xml":
                            stopwatch = new Stopwatch();
                            stopwatch.Start();
                            model = cmlConvertor.Import(mol);
                            stopwatch.Stop();
                            elapsed1 = stopwatch.Elapsed;
                            break;

                        case ".json":
                            stopwatch = new Stopwatch();
                            stopwatch.Start();
                            var jsonConvertor = new JSONConverter();
                            model = jsonConvertor.Import(mol);
                            stopwatch.Stop();
                            elapsed1 = stopwatch.Elapsed;
                            break;
                    }

                    if (model != null)
                    {
                        if (model.AllWarnings.Count > 0)
                        {
                            _telemetry.Write(module, "Warnings", string.Join(Environment.NewLine, model.AllWarnings));
                            MessageBox.Show(string.Join(Environment.NewLine, model.AllWarnings), "Model has warnings!");
                        }

                        if (model.AllErrors.Count == 0 && model.GeneralErrors.Count == 0)
                        {
                            var originalBondLength = model.MeanBondLength;
                            model.EnsureBondLength(20, false);

                            if (string.IsNullOrEmpty(model.CustomXmlPartGuid))
                            {
                                model.CustomXmlPartGuid = Guid.NewGuid().ToString("N");
                            }

                            if (!string.IsNullOrEmpty(_lastCml))
                            {
                                if (_lastCml != EmptyCml)
                                {
                                    var clone = cmlConvertor.Import(_lastCml);
                                    Debug.WriteLine($"Pushing F: {clone.ConciseFormula} BL: {clone.MeanBondLength:#,##0.00} onto Stack");
                                    _undoStack.Push(clone);
                                }
                            }

                            stopwatch = new Stopwatch();
                            stopwatch.Start();
                            _lastCml = cmlConvertor.Export(model);
                            stopwatch.Stop();
                            elapsed2 = stopwatch.Elapsed;

                            _telemetry.Write(module, "Information", $"File: '{filename}'; Original bond length {originalBondLength:#,##0.00}");
                            _telemetry.Write(module, "Timing", $"Import took {elapsed1}; Export took {elapsed2}");
                            ShowChemistry(filename, model);
                        }
                        else
                        {
                            var errors = model.GeneralErrors;
                            errors.AddRange(model.AllErrors);

                            _telemetry.Write(module, "Exception(Data)", string.Join(Environment.NewLine, errors));
                            MessageBox.Show(string.Join(Environment.NewLine, errors), "Model has Errors!");
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                _telemetry.Write(module, "Exception", $"Exception: {exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"Exception: {exception}");
                MessageBox.Show(exception.StackTrace, exception.Message);
            }
        }

        #region Disconnected Code - Please Keep for reference

        private void ChangeBackground_Click(object sender, EventArgs e)
        {
            DialogResult dr = colorDialog1.ShowDialog();
            if (dr == DialogResult.OK)
            {
                DisplayHost.BackColor = colorDialog1.Color;
            }
        }

        private Brush ColorToBrush(System.Drawing.Color colour)
        {
            string hex = $"#{colour.A:X2}{colour.R:X2}{colour.G:X2}{colour.B:X2}";
            var converter = new BrushConverter();
            return (Brush)converter.ConvertFromString(hex);
        }

        private void ShowCarbons_CheckedChanged(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (Display.Chemistry is Model model)
                {
                    Model copy = model.Copy();
                    copy.Refresh();
                    Debug.WriteLine($"Old Model: ({model.MinX}, {model.MinY}):({model.MaxX}, {model.MaxY})");
                    Debug.WriteLine($"New Model: ({copy.MinX}, {copy.MinY}):({copy.MaxX}, {copy.MaxY})");
                    Display.Chemistry = copy;
                }
            }
            catch (Exception exception)
            {
                _telemetry.Write(module, "Exception", $"Exception: {exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"Exception: {exception}");
                MessageBox.Show(exception.StackTrace, exception.Message);
            }
        }

        private void RemoveAtom_Click(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (Display.Chemistry is Model model)
                {
                    var allAtoms = model.GetAllAtoms();
                    if (model.GetAllAtoms().Any())
                    {
                        Molecule modelMolecule =
                            model.GetAllMolecules().FirstOrDefault(m => allAtoms.Any() && m.Atoms.Count > 0);
                        var atom = modelMolecule.Atoms.Values.First();
                        var bondList = atom.Bonds.ToList();
                        foreach (var neighbouringBond in bondList)
                        {
                            modelMolecule.RemoveBond(neighbouringBond);
                            neighbouringBond.OtherAtom(atom).UpdateVisual();
                            foreach (Bond bond in neighbouringBond.OtherAtom(atom).Bonds)
                            {
                                bond.UpdateVisual();
                            }
                        }

                        modelMolecule.RemoveAtom(atom);
                    }

                    model.Refresh();
                    Information.Text =
                        $"Formula: {model.ConciseFormula} BondLength: {model.MeanBondLength.ToString("#,##0.00")}";
                }
            }
            catch (Exception exception)
            {
                _telemetry.Write(module, "Exception", $"Exception: {exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"Exception: {exception}");
                MessageBox.Show(exception.StackTrace, exception.Message);
            }
        }

        private void RandomElement_Click(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (Display.Chemistry is Model model)
                {
                    var allAtoms = model.GetAllAtoms();
                    if (allAtoms.Any())
                    {
                        var rnd = new Random(DateTime.UtcNow.Millisecond);

                        var maxAtoms = allAtoms.Count;
                        int targetAtom = rnd.Next(0, maxAtoms);

                        var elements = Globals.PeriodicTable.Elements;
                        int newElement = rnd.Next(0, elements.Values.Max(v => v.AtomicNumber));
                        var x = elements.Values.FirstOrDefault(v => v.AtomicNumber == newElement);

                        if (x == null)
                        {
                            Debugger.Break();
                        }

                        allAtoms[targetAtom].Element = x as ElementBase;
                        if (x.Symbol.Equals("C"))
                        {
                            //allAtoms[targetAtom].ShowSymbol = ShowCarbons.Checked
                        }

                        allAtoms[targetAtom].UpdateVisual();

                        foreach (Bond b in allAtoms[targetAtom].Bonds)
                        {
                            b.UpdateVisual();
                        }

                        model.Refresh();
                        Information.Text =
                            $"Formula: {model.ConciseFormula} BondLength: {model.MeanBondLength.ToString("#,##0.00")}";
                    }
                }
            }
            catch (Exception exception)
            {
                _telemetry.Write(module, "Exception", $"Exception: {exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"Exception: {exception}");
                MessageBox.Show(exception.StackTrace, exception.Message);
            }
        }

        #endregion Disconnected Code - Please Keep for reference

        private void EditLabels_Click(object sender, EventArgs e)
        {
#if !DEBUG
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
#endif

            if (!string.IsNullOrEmpty(_lastCml))
            {
                using (EditorHost editorHost = new EditorHost(_lastCml, "LABELS"))
                {
                    editorHost.ShowDialog(this);
                    if (editorHost.DialogResult == DialogResult.OK)
                    {
                        HandleChangedCml(editorHost.OutputValue, "Labels Editor result");
                    }
                }
                TopMost = true;
                TopMost = false;
                Activate();
            }
#if !DEBUG
            }
            catch (Exception exception)
            {
                _telemetry.Write(module, "Exception", $"Exception: {exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"Exception: {exception}");
                MessageBox.Show(exception.StackTrace, exception.Message);
            }
#endif
        }

        private void EditWithAcme_Click(object sender, EventArgs e)
        {
#if !DEBUG
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
#endif
            if (!string.IsNullOrEmpty(_lastCml))
            {
                using (EditorHost editorHost = new EditorHost(_lastCml, "ACME"))
                {
                    editorHost.ShowDialog(this);
                    if (editorHost.DialogResult == DialogResult.OK)
                    {
                        HandleChangedCml(editorHost.OutputValue, "ACME result");
                    }
                }
                TopMost = true;
                TopMost = false;
                Activate();
            }
#if !DEBUG
            }
            catch (Exception exception)
            {
                _telemetry.Write(module, "Exception", $"Exception: {exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"Exception: {exception}");
                MessageBox.Show(exception.StackTrace, exception.Message);
            }
#endif
        }

        private void ShowChemistry(string filename, Model model)
        {
            Display.Clear();

            if (model != null)
            {
                if (model.AllErrors.Any() || model.GeneralErrors.Any())
                {
                    List<string> lines = new List<string>();

                    if (model.GeneralErrors.Any())
                    {
                        lines.Add("General Error(s)");
                        lines.AddRange(model.GeneralErrors);
                    }
                    if (model.AllErrors.Any())
                    {
                        lines.Add("All Error(s)");
                        lines.AddRange(model.AllErrors);
                    }

                    MessageBox.Show(string.Join(Environment.NewLine, lines));
                }
                else
                {
                    if (!string.IsNullOrEmpty(filename))
                    {
                        Text = filename;
                    }

                    Information.Text =
                        $"Formula: {model.ConciseFormula} BondLength: {model.MeanBondLength.ToString("#,##0.00")}";

                    model.Refresh();

                    Display.Chemistry = model;
                    Debug.WriteLine($"FlexForm is displaying {model.ConciseFormula}");

                    EnableNormalButtons();
                    EnableUndoRedoButtonsAndShowStacks();
                }
            }
        }

        private void EnableNormalButtons()
        {
            EditWithAcme.Enabled = true;
            EditLabels.Enabled = true;
            EditCml.Enabled = true;
            CalculateProperties.Enabled = true;

            ShowCml.Enabled = true;
            ClearChemistry.Enabled = true;
            SaveStructure.Enabled = true;
            LayoutStructure.Enabled = true;
            RenderOoXml.Enabled = true;

            ListStacks();
        }

        private List<Controller> StackToList(Stack<Model> stack)
        {
            List<Controller> list = new List<Controller>();
            foreach (var item in stack)
            {
                var model = item.Copy();
                model.Refresh();
                list.Add(new Controller(model));
            }

            return list;
        }

        private void EnableUndoRedoButtonsAndShowStacks()
        {
            Redo.Enabled = _redoStack.Count > 0;
            Undo.Enabled = _undoStack.Count > 0;
            UndoStack.ListOfDisplays.ItemsSource = StackToList(_undoStack);
            RedoStack.ListOfDisplays.ItemsSource = StackToList(_redoStack);
        }

        private void Undo_Click(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Model m = _undoStack.Pop();
                Debug.WriteLine(
                    $"Popped F: {m.ConciseFormula} BL: {m.MeanBondLength.ToString("#,##0.00")} from Undo Stack");

                if (!string.IsNullOrEmpty(_lastCml))
                {
                    CMLConverter cc = new CMLConverter();
                    var copy = cc.Import(_lastCml);
                    _lastCml = cc.Export(m);

                    Debug.WriteLine(
                        $"Pushing F: {copy.ConciseFormula} BL: {copy.MeanBondLength.ToString("#,##0.00")} onto Redo Stack");
                    _redoStack.Push(copy);
                }

                ShowChemistry($"Undo -> {FormulaHelper.FormulaPartsAsUnicode(FormulaHelper.ParseFormulaIntoParts(m.ConciseFormula))}", m);
            }
            catch (Exception exception)
            {
                _telemetry.Write(module, "Exception", $"Exception: {exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"Exception: {exception}");
                MessageBox.Show(exception.StackTrace, exception.Message);
            }
        }

        private void Redo_Click(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Model m = _redoStack.Pop();
                Debug.WriteLine(
                    $"Popped F: {m.ConciseFormula} BL: {m.MeanBondLength.ToString("#,##0.00")} from Redo Stack");

                if (!string.IsNullOrEmpty(_lastCml))
                {
                    CMLConverter cc = new CMLConverter();
                    var clone = cc.Import(_lastCml);
                    _lastCml = cc.Export(m);

                    Debug.WriteLine(
                        $"Pushing F: {clone.ConciseFormula} BL: {clone.MeanBondLength.ToString("#,##0.00")} onto Undo Stack");
                    _undoStack.Push(clone);
                }

                ShowChemistry($"Redo -> {FormulaHelper.FormulaPartsAsUnicode(FormulaHelper.ParseFormulaIntoParts(m.ConciseFormula))}", m);
            }
            catch (Exception exception)
            {
                _telemetry.Write(module, "Exception", $"Exception: {exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"Exception: {exception}");
                MessageBox.Show(exception.StackTrace, exception.Message);
            }
        }

        private void ListStacks()
        {
            if (_undoStack.Any())
            {
                Debug.WriteLine("Undo Stack");
                foreach (var model in _undoStack)
                {
                    Debug.WriteLine(
                        $"{model.ConciseFormula} [{model.GetHashCode()}] {model.MeanBondLength.ToString("#,##0.00")}");
                }
            }

            if (_redoStack.Any())
            {
                Debug.WriteLine("Redo Stack");
                foreach (var model in _redoStack)
                {
                    Debug.WriteLine(
                        $"{model.ConciseFormula} [{model.GetHashCode()}] {model.MeanBondLength.ToString("#,##0.00")}");
                }
            }
        }

        private void EditCml_Click(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                if (!string.IsNullOrEmpty(_lastCml))
                {
                    using (EditorHost editorHost = new EditorHost(_lastCml, "CML"))
                    {
                        editorHost.ShowDialog(this);
                        if (editorHost.DialogResult == DialogResult.OK)
                        {
                            HandleChangedCml(editorHost.OutputValue, "CML Editor result");
                        }
                    }
                    TopMost = true;
                    TopMost = false;
                    Activate();
                }
            }
            catch (Exception exception)
            {
                _telemetry.Write(module, "Exception", $"Exception: {exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"Exception: {exception}");
                MessageBox.Show(exception.StackTrace, exception.Message);
            }
        }

        private void ShowCml_Click(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (!string.IsNullOrEmpty(_lastCml))
                {
                    using (var f = new ShowCml { Cml = _lastCml })
                    {
                        f.ShowDialog(this);
                    }
                }
            }
            catch (Exception exception)
            {
                _telemetry.Write(module, "Exception", $"Exception: {exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"Exception: {exception}");
                MessageBox.Show(exception.StackTrace, exception.Message);
            }
        }

        private void SaveStructure_Click(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                CMLConverter cmlConverter = new CMLConverter();
                Model m = cmlConverter.Import(_lastCml);
                m.CustomXmlPartGuid = "";

                StringBuilder sb = new StringBuilder();
                sb.Append("CML molecule files (*.cml, *.xml)|*.cml;*.xml");
                sb.Append("|MDL molecule files (*.mol, *.sdf)|*.mol;*.sdf");
                sb.Append("|ChemDoodle Web json files (*.json)|*.json");

                using (SaveFileDialog sfd = new SaveFileDialog { Filter = sb.ToString() })
                {
                    sfd.AddExtension = true;
                    DialogResult dr = sfd.ShowDialog();
                    if (dr == DialogResult.OK)
                    {
                        FileInfo fi = new FileInfo(sfd.FileName);
                        _telemetry.Write(module, "Information", $"Exporting to '{fi.Name}'");
                        string fileType = Path.GetExtension(sfd.FileName).ToLower();
                        switch (fileType)
                        {
                            case ".cml":
                            case ".xml":
                                File.WriteAllText(sfd.FileName, XmlHelper.AddHeader(cmlConverter.Export(m)));
                                break;

                            case ".mol":
                            case ".sdf":
                                // https://www.chemaxon.com/marvin-archive/6.0.2/marvin/help/formats/mol-csmol-doc.html
                                double before = m.MeanBondLength;
                                // Set bond length to 1.54 angstroms (Å)
                                m.ScaleToAverageBondLength(1.54);
                                double after = m.MeanBondLength;
                                _telemetry.Write(module, "Information", $"Structure rescaled from {before.ToString("#0.00")} to {after.ToString("#0.00")}");
                                SdFileConverter converter = new SdFileConverter();
                                File.WriteAllText(sfd.FileName, converter.Export(m));
                                break;

                            case ".json":
                                var jsonConverter = new JSONConverter();
                                File.WriteAllText(sfd.FileName, jsonConverter.Export(m));
                                break;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                _telemetry.Write(module, "Exception", $"Exception: {exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"Exception: {exception}");
                MessageBox.Show(exception.StackTrace, exception.Message);
            }
        }

        private void ClearChemistry_Click(object sender, EventArgs e)
        {
            var cc = new CMLConverter();
            _undoStack.Push(cc.Import(_lastCml));
            _lastCml = EmptyCml;

            Display.Clear();
            EnableUndoRedoButtonsAndShowStacks();
        }

        private void FlexForm_Load(object sender, EventArgs e)
        {
            SetDisplayOptions();
            Display.HighlightActive = false;

            RedoStack = new StackViewer(_editorOptions);
            RedoHost.Child = RedoStack;
            UndoStack = new StackViewer(_editorOptions);
            UndoHost.Child = UndoStack;

            var location = Assembly.GetExecutingAssembly().Location;
            var path = Path.GetDirectoryName(location);
            _configWatcher = new ConfigWatcher(path);
        }

        private void OptionsChanged()
        {
            // Allow time for FileSytemWatcher to fire
            Thread.Sleep(250);

            _renderOptions = new OoXmlV4Options(null);
            _editorOptions = new AcmeOptions(null);
            Debug.WriteLine($"ACME ColouredAtoms {_editorOptions.ColouredAtoms}");
            Debug.WriteLine($"OoXml ColouredAtoms {_renderOptions.ColouredAtoms}");
            UpdateControls();
        }

        private void ChangeOoXmlSettings_Click(object sender, EventArgs e)
        {
            OoXmlV4Settings settings = new OoXmlV4Settings();
            settings.Telemetry = _telemetry;
            settings.TopLeft = new System.Windows.Point(Left + 24, Top + 24);

            var tempOptions = _renderOptions.Clone();
            settings.RendererOptions = tempOptions;

            DialogResult dr = settings.ShowDialog();
            if (dr == DialogResult.OK)
            {
                _renderOptions = tempOptions.Clone();
                OptionsChanged();
            }

            settings.Close();
        }

        private void ChangeAcmeSettings_Click(object sender, EventArgs e)
        {
            AcmeSettingsHost settings = new AcmeSettingsHost();
            settings.Telemetry = _telemetry;
            settings.TopLeft = new System.Windows.Point(Left + 24, Top + 24);

            var tempOptions = _editorOptions.Clone();
            settings.EditorOptions = tempOptions;

            DialogResult dr = settings.ShowDialog();
            if (dr == DialogResult.OK)
            {
                _editorOptions = tempOptions.Clone();
                OptionsChanged();
            }

            settings.Close();
        }

        private void UpdateControls()
        {
            SetDisplayOptions();
            Display.Chemistry = _lastCml;
            RedoStack.SetOptions(_editorOptions);
            UndoStack.SetOptions(_editorOptions);
            UndoStack.ListOfDisplays.ItemsSource = StackToList(_undoStack);
            RedoStack.ListOfDisplays.ItemsSource = StackToList(_redoStack);
        }

        private void SetDisplayOptions()
        {
            Display.ShowAllCarbonAtoms = _editorOptions.ShowCarbons;
            Display.ShowImplicitHydrogens = _editorOptions.ShowHydrogens;
            Display.ShowAtomsInColour = _editorOptions.ColouredAtoms;
            Display.ShowMoleculeGrouping = _editorOptions.ShowMoleculeGrouping;
        }

        private void LayoutStructure_Click(object sender, EventArgs e)
        {
            LayoutUsingCheblClean();
        }

        private void LayoutUsingCheblClean()
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            var cc = new CMLConverter();
            var model = cc.Import(_lastCml);

            if (model.TotalMoleculesCount == 1
                && !model.HasNestedMolecules
                && !model.HasFunctionalGroups)
            {
                var bondLength = model.MeanBondLength;
                var marvin = cc.Export(model, true, CmlFormat.MarvinJs);

                // Replace double quote with single quote
                marvin = marvin.Replace("\"", "'");

                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        httpClient.Timeout = TimeSpan.FromSeconds(15);
                        httpClient.DefaultRequestHeaders.Add("user-agent", "Chem4Word");

                        try
                        {
                            var request = new HttpRequestMessage(HttpMethod.Post, "https://www.ebi.ac.uk/chembl/api/utils/clean");
                            request.Headers.Add("User-Agent", "Chem4Word");

                            var body = JsonConvert.SerializeObject(new { structure = $"{marvin}", parameters = new { dim = 2, opts = "s" } });
                            request.Content = new StringContent(body, Encoding.UTF8, "text/plain");

                            var response = httpClient.SendAsync(request).Result;

                            if (!response.IsSuccessStatusCode)
                            {
                                // Handle Error
                                Debug.WriteLine($"{response.StatusCode} - {response.RequestMessage}");
                            }

                            var answer = response.Content.ReadAsStringAsync();
                            Debug.WriteLine(answer.Result);

                            model = cc.Import(answer.Result);
                            model.EnsureBondLength(bondLength, false);
                            if (string.IsNullOrEmpty(model.CustomXmlPartGuid))
                            {
                                model.CustomXmlPartGuid = Guid.NewGuid().ToString("N");
                            }

                            var clone = cc.Import(_lastCml);
                            _undoStack.Push(clone);

                            _lastCml = cc.Export(model);
                            ShowChemistry("ChEMBL clean", model);
                        }
                        catch (Exception innerException)
                        {
                            _telemetry.Write(module, "Exception", innerException.Message);
                            _telemetry.Write(module, "Exception", innerException.ToString());
                            Debug.WriteLine(innerException.Message);
                        }
                    }
                }
                catch (Exception exception)
                {
                    _telemetry.Write(module, "Exception", exception.Message);
                    _telemetry.Write(module, "Exception", exception.ToString());
                    Debug.WriteLine(exception.Message);
                }
            }
            else
            {
                MessageBox.Show("Clean only handles single molecules without any functional groups (at the moment)", "Test Harness");
            }
        }

        private void RenderOoXml_Click(object sender, EventArgs e)
        {
            var renderer = new Renderer();
            renderer.Telemetry = _telemetry;
            renderer.TopLeft = new System.Windows.Point(Left + 24, Top + 24);
            renderer.Cml = _lastCml;
            renderer.Properties = new Dictionary<string, string>();
            renderer.Properties.Add("Guid", Guid.NewGuid().ToString("N"));
            string file = renderer.Render();
            if (string.IsNullOrEmpty(file))
            {
                MessageBox.Show("Something went wrong!", "Error");
            }
            else
            {
                // Start word in quiet mode [/q] without any add ins loaded [/a]
                Process.Start(OfficeHelper.GetWinWordPath(), $"/q /a {file}");
            }
        }

        private void SearchChEBI_Click(object sender, EventArgs e)
        {
            using (var searcher = new SearchChEBI())
            {
                searcher.Telemetry = _telemetry;
                searcher.UserOptions = new ChEBIOptions();
                searcher.TopLeft = new System.Windows.Point(Left + 24, Top + 24);

                DialogResult result = searcher.ShowDialog(this);
                if (result == DialogResult.OK)
                {
                    HandleChangedCml(searcher.Cml, "ChEBI Search result");
                }
            }

            TopMost = true;
            TopMost = false;
            Activate();
        }

        private void SearchPubChem_Click(object sender, EventArgs e)
        {
            using (var searcher = new SearchPubChem())
            {
                searcher.Telemetry = _telemetry;
                searcher.UserOptions = new PubChemOptions();
                searcher.TopLeft = new System.Windows.Point(Left + 24, Top + 24);

                DialogResult result = searcher.ShowDialog(this);
                if (result == DialogResult.OK)
                {
                    HandleChangedCml(searcher.Cml, "PubChem Search result");
                }
            }

            TopMost = true;
            TopMost = false;
            Activate();
        }

        private void SearchOpsin_Click(object sender, EventArgs e)
        {
            using (var searcher = new SearchOpsin())
            {
                searcher.Telemetry = _telemetry;
                searcher.UserOptions = new SearcherOptions();
                searcher.TopLeft = new System.Windows.Point(Left + 24, Top + 24);

                DialogResult result = searcher.ShowDialog(this);
                if (result == DialogResult.OK)
                {
                    HandleChangedCml(searcher.Cml, "ChEBI Search result");
                }
            }

            TopMost = true;
            TopMost = false;
            Activate();
        }

        private void HandleChangedCml(string cml, string captionPrefix)
        {
            var cc = new CMLConverter();
            if (_lastCml != EmptyCml)
            {
                var clone = cc.Import(_lastCml);
                Debug.WriteLine(
                    $"Pushing F: {clone.ConciseFormula} BL: {clone.MeanBondLength.ToString("#,##0.00")} onto Stack");
                _undoStack.Push(clone);
            }

            Model model = cc.Import(cml);
            if (model.GeneralErrors.Count == 0 && model.AllErrors.Count == 0 && model.AllWarnings.Count == 0)
            {
                model.Relabel(true);
                model.EnsureBondLength(20, false);
                _lastCml = cc.Export(model);

                // Cause re-read of settings (in case they have changed)
                _editorOptions = new AcmeOptions(null);
                SetDisplayOptions();
                RedoStack.SetOptions(_editorOptions);
                UndoStack.SetOptions(_editorOptions);

                ShowChemistry($"{captionPrefix} {FormulaHelper.FormulaPartsAsUnicode(FormulaHelper.ParseFormulaIntoParts(model.ConciseFormula))}", model);
            }
            else
            {
                var errors = model.GeneralErrors;
                errors.AddRange(model.AllErrors);
                errors.AddRange(model.AllWarnings);

                MessageBox.Show(string.Join(Environment.NewLine, errors), "Model has errors or warnings!");
            }
        }

        private void CalculateProperties_Click(object sender, EventArgs e)
        {
            var cc = new CMLConverter();
            if (_lastCml != EmptyCml)
            {
                var clone = cc.Import(_lastCml);
                Debug.WriteLine(
                    $"Pushing F: {clone.ConciseFormula} BL: {clone.MeanBondLength.ToString("#,##0.00")} onto Stack");
                _undoStack.Push(clone);
            }

            Model model = cc.Import(_lastCml);

            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            var pc = new PropertyCalculator(_telemetry, new Point(Left, Top), version.ToString());

            int changedProperties;

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // Code being tested
            changedProperties = pc.CalculateProperties(model);

            stopwatch.Stop();
            Debug.WriteLine($"Calculating {changedProperties} changed properties took {stopwatch.Elapsed}");
            _lastCml = cc.Export(model);

            // Cause re-read of settings (in case they have changed)
            _editorOptions = new AcmeOptions(null);
            SetDisplayOptions();
            RedoStack.SetOptions(_editorOptions);
            UndoStack.SetOptions(_editorOptions);

            ShowChemistry($"{changedProperties} changed properties; {FormulaHelper.FormulaPartsAsUnicode(FormulaHelper.ParseFormulaIntoParts(model.ConciseFormula))}", model);
        }
    }
}