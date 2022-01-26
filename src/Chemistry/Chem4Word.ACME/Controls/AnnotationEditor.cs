// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Xml.Linq;
using Chem4Word.ACME.Commands.Block_Editing;
using Chem4Word.ACME.Utils;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2.Converters.CML;

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
                TextRange range = new TextRange(Selection.Start, Selection.End);
                object alignment = range.GetPropertyValue(Inline.BaselineAlignmentProperty);
                return alignment != DependencyProperty.UnsetValue && (BaselineAlignment)alignment == BaselineAlignment.Subscript;
            }
        }

        public bool SelectionIsSuperscript
        {
            get
            {
                TextRange range = new TextRange(Selection.Start, Selection.End);
                object alignment = range.GetPropertyValue(Inline.BaselineAlignmentProperty);
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

        //see https://thomaslevesque.com/2015/09/05/wpf-prevent-the-user-from-pasting-an-image-in-a-richtextbox/
        private void AnnotationEditor_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.FormatToApply == DataFormats.Bitmap || e.FormatToApply == DataFormats.Dib || e.FormatToApply == DataFormats.FileDrop)
            {
                e.CancelCommand();
            }
            else if (e.FormatToApply == DataFormats.Rtf)
            {
                var plainText = e.DataObject.GetData(DataFormats.Text);
                DataObject d = new DataObject();
                d.SetData(DataFormats.UnicodeText, plainText);
                e.DataObject = d;
            }
        }

        private void AnnotationEditor_SelectionChanged(object sender, RoutedEventArgs e)
        {
            MakeSubscriptCommand.RaiseCanExecChanged();
        }

        #endregion Constructors

        #region Event Handlers

        private void AnnotationEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            Dirty = true;
        }

        private void AnnotationEditor_LostFocus(object sender, RoutedEventArgs e)
        {
            Completed?.Invoke(this, new AnnotationEditorEventArgs { Reason = AnnotationEditorEventArgs.AnnotationEditorExitArgsType.LostFocus });
        }

        /// <summary>
        /// Detects whether the enter key has been hit or just another editing key
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AnnotationEditor_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape) //abort the edit
            {
                Completed.Invoke(this, new AnnotationEditorEventArgs { Reason = AnnotationEditorEventArgs.AnnotationEditorExitArgsType.Aborted });
                e.Handled = true;
            }
            if (e.Key == Key.Enter && !(KeyboardUtils.HoldingDownShift() || KeyboardUtils.HoldingDownControl()))
            {
                Completed.Invoke(this, new AnnotationEditorEventArgs { Reason = AnnotationEditorEventArgs.AnnotationEditorExitArgsType.ReturnPressed });
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
            //make sure that we don't put spaces aroung the subscripts
            //use IgnoreWhitespace to ensure that this happens
            Document = XAMLHelper.GetFlowDocument(text);
            Dirty = false;
        }

        public string GetDocument()
        {
            MemoryStream ms = new MemoryStream();
            TextRange flowDocSelection = new TextRange(Document.ContentStart, Document.ContentEnd);
            flowDocSelection.Save(ms, DataFormats.Xaml);
            var bytes = ms.ToArray();
            string result = Encoding.UTF8.GetString(bytes);
            result = StripOutSectionXaml(result);
            return result;
        }

        //gets rid of all the superfluous xaml that the RTB crams into the flowdocument
        private string StripOutSectionXaml(string result)
        {
            XDocument doc = XDocument.Parse(result);
            XElement paraElement = null;
            foreach (XElement para in doc.Descendants())
            {
                if (para.Name.LocalName == "Paragraph")
                {
                    paraElement = para;
                    break;
                }
            }
            XDocument newDocument = new XDocument();
            XElement root = new XElement(CMLNamespaces.xaml + "FlowDocument");
            root.Add(paraElement);
            newDocument.Add(root);
            return newDocument.ToString();
        }

        internal void Clear()
        {
            Document.Blocks.Clear();
        }

        public void ToggleSubscript(TextSelection selection)
        {
            TextRange toSubscript = new TextRange(selection.Start, selection.End);
            var subscriptState = toSubscript.GetPropertyValue(Inline.BaselineAlignmentProperty);
            if (subscriptState != null)
            {
                if (subscriptState == DependencyProperty.UnsetValue || (BaselineAlignment)subscriptState != BaselineAlignment.Subscript)
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
            TextRange toSuperscript = new TextRange(selection.Start, selection.End);
            var superscriptState = toSuperscript.GetPropertyValue(Inline.BaselineAlignmentProperty);
            if (superscriptState != null)
            {
                if (superscriptState == DependencyProperty.UnsetValue || (BaselineAlignment)superscriptState != BaselineAlignment.Superscript)
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

    /// <summary>
    /// Event argument for Completed event
    /// </summary>
    public class AnnotationEditorEventArgs : RoutedEventArgs
    {
        public enum AnnotationEditorExitArgsType
        {
            LostFocus,
            ReturnPressed,
            Aborted
        }

        public AnnotationEditorExitArgsType Reason { get; set; }
    }
}