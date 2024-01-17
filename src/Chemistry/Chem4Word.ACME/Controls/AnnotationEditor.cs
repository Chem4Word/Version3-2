// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Commands.BlockEditing;
using Chem4Word.ACME.Utils;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2.Converters.CML;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Xml.Linq;

namespace Chem4Word.ACME.Controls
{
    /// <summary>
    /// Wraps the RichTextBox to add additional editing functionality
    /// </summary>
    public class AnnotationEditor : RichTextBox
    {
        public delegate void AnnotationEditorEvent(object sender, AnnotationEditorEventArgs e);

        public event AnnotationEditorEvent Completed;

        public bool Dirty { get; private set; }

        public EditController Controller { get; set; }

        public bool SelectionIsSubscript
        {
            get
            {
                var range = new TextRange(Selection.Start, Selection.End);
                var alignment = range.GetPropertyValue(Inline.BaselineAlignmentProperty);
                return alignment != DependencyProperty.UnsetValue && (BaselineAlignment)alignment == BaselineAlignment.Subscript;
            }
        }

        public bool SelectionIsSuperscript
        {
            get
            {
                var range = new TextRange(Selection.Start, Selection.End);
                var alignment = range.GetPropertyValue(Inline.BaselineAlignmentProperty);
                return alignment != DependencyProperty.UnsetValue && (BaselineAlignment)alignment == BaselineAlignment.Superscript;
            }
        }

        public bool EditingReagents { get; internal set; }
        public SubscriptCommand MakeSubscriptCommand { get; }
        public SuperscriptCommand MakeSuperscriptCommand { get; }
        public InsertTextCommand InsertSymbolCommand { get; }
        public InsertTextCommand InsertDegreeCommand { get; }

        #region Constructors

        public AnnotationEditor()
        {
            SpellCheck.IsEnabled = false;

            PreviewKeyDown += AnnotationEditor_PreviewKeyDown;
            LostFocus += AnnotationEditor_LostFocus;
            TextChanged += AnnotationEditor_TextChanged;
            MakeSubscriptCommand = new SubscriptCommand(this);
            MakeSuperscriptCommand = new SuperscriptCommand(this);
            InsertSymbolCommand = new InsertTextCommand(this, "Δ");
            InsertDegreeCommand = new InsertTextCommand(this, "℃");
            SelectionChanged += AnnotationEditor_SelectionChanged;
            DataObject.AddPastingHandler(this, AnnotationEditor_Pasting);
        }

        #endregion Constructors

        #region Event Handlers

        //see https://thomaslevesque.com/2015/09/05/wpf-prevent-the-user-from-pasting-an-image-in-a-richtextbox/
        private void AnnotationEditor_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.FormatToApply == DataFormats.Bitmap
                || e.FormatToApply == DataFormats.Dib
                || e.FormatToApply == DataFormats.FileDrop)
            {
                e.CancelCommand();
            }
            else if (e.FormatToApply == DataFormats.Rtf)
            {
                var plainText = e.DataObject.GetData(DataFormats.Text);
                var dataObject = new DataObject();
                if (plainText != null)
                {
                    dataObject.SetData(DataFormats.UnicodeText, plainText);
                }

                e.DataObject = dataObject;
            }
        }

        private void AnnotationEditor_SelectionChanged(object sender, RoutedEventArgs e)
        {
            MakeSubscriptCommand.RaiseCanExecChanged();
        }

        private void AnnotationEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            Dirty = true;
        }

        private void AnnotationEditor_LostFocus(object sender, RoutedEventArgs e)
        {
            Completed?.Invoke(this, new AnnotationEditorEventArgs { Reason = AnnotationEditorExitArgsType.LostFocus });
        }

        /// <summary>
        /// Detects whether the enter key has been hit or just another editing key
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AnnotationEditor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) //abort the edit
            {
                Completed?.Invoke(this, new AnnotationEditorEventArgs { Reason = AnnotationEditorExitArgsType.Aborted });
                e.Handled = true;
            }
            if (e.Key == Key.Enter
                && !(KeyboardUtils.HoldingDownShift()
                     || KeyboardUtils.HoldingDownControl()))
            {
                Completed?.Invoke(this, new AnnotationEditorEventArgs { Reason = AnnotationEditorExitArgsType.ReturnPressed });
                e.Handled = true;
            }
        }

        #endregion Event Handlers

        #region Methods

        /// <summary>
        /// Loads the FlowDocument into the editor
        /// </summary>
        /// <param name="text">String containing the FlowDocument to be loaded.</param>
        public void LoadDocument(string text)
        {
            Document = XAMLHelper.GetFlowDocument(text);
            Dirty = false;
        }

        public string GetDocument()
        {
            var memoryStream = new MemoryStream();
            var flowDocSelection = new TextRange(Document.ContentStart, Document.ContentEnd);
            flowDocSelection.Save(memoryStream, DataFormats.Xaml, true);
            var bytes = memoryStream.ToArray();
            var result = Encoding.UTF8.GetString(bytes);
            result = StripOutSectionXaml(result);
            return result;
        }

        //gets rid of all the superfluous xml that the RichTextBox crams into the flow document
        private string StripOutSectionXaml(string xmlIn)
        {
            var xDocument = XDocument.Parse(xmlIn);

            // Construct new XDocument
            var newDocument = new XDocument();
            var root = new XElement(CMLNamespaces.xaml + "FlowDocument");
            newDocument.Add(root);
            var newParagraph = new XElement(CMLNamespaces.xaml + "Paragraph");
            root.Add(newParagraph);

            var paragraphs = xDocument.Descendants().Where(x => x.Name.LocalName == "Paragraph").ToList();
            var paragraphCount = paragraphs.Count;
            var loopCounter = 0;

            // Loop round adding only what we want to keep to the new document
            foreach (var paragraph in paragraphs)
            {
                foreach (var child in paragraph.Descendants())
                {
                    switch (child.Name.LocalName)
                    {
                        case "Run":
                            var element = new XElement(CMLNamespaces.xaml + "Run", child.Value);
                            foreach (var attribute in child.Attributes())
                            {
                                if (attribute.Name.LocalName == "BaselineAlignment")
                                {
                                    element.Add(new XAttribute("BaselineAlignment", attribute.Value));
                                }
                            }
                            newParagraph.Add(element);
                            break;

                        case "LineBreak":
                            newParagraph.Add(new XElement(CMLNamespaces.xaml + "LineBreak"));
                            break;
                    }
                }

                // Insert a LineBreak between any incoming paragraphs
                // There should only ever be more than one if text was pasted from an external source
                loopCounter++;
                if (loopCounter < paragraphCount)
                {
                    newParagraph.Add(new XElement(CMLNamespaces.xaml + "LineBreak"));
                }
            }

            return newDocument.ToString();
        }

        internal void Clear()
        {
            Document.Blocks.Clear();
        }

        public void ToggleSubscript(TextSelection selection)
        {
            var toSubscript = new TextRange(selection.Start, selection.End);
            var subscriptState = toSubscript.GetPropertyValue(Inline.BaselineAlignmentProperty);
            if (subscriptState != null)
            {
                if (subscriptState == DependencyProperty.UnsetValue
                    || (BaselineAlignment)subscriptState != BaselineAlignment.Subscript)
                {
                    toSubscript.ApplyPropertyValue(Inline.BaselineAlignmentProperty, BaselineAlignment.Subscript);
                    Controller.SelectionIsSubscript = true;
                }
                else
                {
                    toSubscript.ClearAllProperties();
                    Controller.SelectionIsSubscript = false;
                }
            }
        }

        public void ToggleSuperscript(TextSelection selection)
        {
            var toSuperscript = new TextRange(selection.Start, selection.End);
            var superscriptState = toSuperscript.GetPropertyValue(Inline.BaselineAlignmentProperty);
            if (superscriptState != null)
            {
                if (superscriptState == DependencyProperty.UnsetValue
                    || (BaselineAlignment)superscriptState != BaselineAlignment.Superscript)
                {
                    toSuperscript.ApplyPropertyValue(Inline.BaselineAlignmentProperty, BaselineAlignment.Superscript);
                    Controller.SelectionIsSuperscript = true;
                }
                else
                {
                    toSuperscript.ClearAllProperties();
                    Controller.SelectionIsSuperscript = false;
                }
            }
        }

        #endregion Methods
    }
}