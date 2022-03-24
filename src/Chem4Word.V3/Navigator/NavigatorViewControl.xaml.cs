// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Chem4Word.ACME;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Core.UI.Wpf;
using Chem4Word.Helpers;
using Microsoft.Office.Interop.Word;

using Word = Microsoft.Office.Interop.Word;

namespace Chem4Word.Navigator
{
    /// <summary>
    /// Interaction logic for NavigatorView.xaml
    /// </summary>
    public partial class NavigatorViewControl : UserControl
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private AcmeOptions _options;

        public NavigatorViewControl()
        {
            InitializeComponent();
        }

        public bool ShowAllCarbonAtoms => _options.ShowCarbons;
        public bool ShowImplicitHydrogens => _options.ShowHydrogens;
        public bool ShowAtomsInColour => _options.ColouredAtoms;
        public bool ShowMoleculeGrouping => _options.ShowMoleculeGrouping;

        public void SetOptions(AcmeOptions options)
        {
            _options = options;
        }

        public Document ActiveDocument { get; set; }

        private void OnItemButtonClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                if (Globals.Chem4WordV3.EventsEnabled
                    && e.OriginalSource is WpfEventArgs source)
                {
                    Globals.Chem4WordV3.Telemetry.Write(module, "Action", $"Source: {source.Button} Data: {source.OutputValue}");

                    var parts = source.OutputValue.Split('=');
                    var item = parts[1];

                    if (DataContext is NavigatorController controller)
                    {
                        var clicked = controller.NavigatorItems.FirstOrDefault(c => c.CustomControlTag == item);
                        if (clicked != null)
                        {
                            Globals.Chem4WordV3.EventsEnabled = false;

                            if (Globals.Chem4WordV3.Application.Documents.Count > 0
                                && ActiveDocument?.ActiveWindow?.Selection != null)
                            {
                                switch (source.Button)
                                {
                                    case "Navigator|InsertCopy":
                                        TaskPaneHelper.InsertChemistry(true, ActiveDocument.Application, clicked.Cml, false);
                                        break;

                                    case "Navigator|InsertLink":
                                        TaskPaneHelper.InsertChemistry(false, ActiveDocument.Application, clicked.Cml, false);
                                        break;

                                    case "Navigator|Previous":
                                        SelectPrevious(clicked.CustomControlTag);
                                        break;

                                    case "Navigator|Next":
                                        SelectNext(clicked.CustomControlTag);
                                        break;
                                }
                            }

                            Globals.Chem4WordV3.EventsEnabled = true;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, exception))
                {
                    form.ShowDialog();
                }
            }
            finally
            {
                Globals.Chem4WordV3.EventsEnabled = true;
            }
        }

        private void SelectNext(string tag)
        {
            var currentSelPoint = ActiveDocument.ActiveWindow.Selection;
            var linkedControls = (from Word.ContentControl cc in ActiveDocument.ContentControls
                                  orderby cc.Range.Start
                                  where CustomXmlPartHelper.GuidFromTag(cc.Tag) == tag
                                  select cc).ToList();

            // Grab current selection point
            int current = currentSelPoint.Range.End;
            foreach (Word.ContentControl cc in linkedControls)
            {
                if (cc.Range.Start > current)
                {
                    cc.Range.Select();
                    ActiveDocument.ActiveWindow.ScrollIntoView(cc.Range);
                    Globals.Chem4WordV3.SelectChemistry(ActiveDocument, ActiveDocument.ActiveWindow.Selection);
                    return;
                }
            }

            // Rewind to Start of document
            current = 0;
            foreach (Word.ContentControl cc in linkedControls)
            {
                if (cc.Range.Start > current)
                {
                    cc.Range.Select();
                    ActiveDocument.ActiveWindow.ScrollIntoView(cc.Range);
                    Globals.Chem4WordV3.SelectChemistry(ActiveDocument, ActiveDocument.ActiveWindow.Selection);
                    return;
                }
            }
        }

        private void SelectPrevious(string tag)
        {
            var currentSelPoint = ActiveDocument.ActiveWindow.Selection;
            var linkedControls = (from Word.ContentControl cc in ActiveDocument.ContentControls
                                  orderby cc.Range.Start descending
                                  where CustomXmlPartHelper.GuidFromTag(cc.Tag) == tag
                                  select cc).ToList();

            // Grab current selection point
            int current = currentSelPoint.Range.Start;
            foreach (Word.ContentControl cc in linkedControls)
            {
                if (cc.Range.Start < current)
                {
                    cc.Range.Select();
                    ActiveDocument.ActiveWindow.ScrollIntoView(cc.Range);
                    Globals.Chem4WordV3.SelectChemistry(ActiveDocument, ActiveDocument.ActiveWindow.Selection);
                    return;
                }
            }

            // Fast Forward to end of document
            current = int.MaxValue;
            foreach (Word.ContentControl cc in linkedControls)
            {
                if (cc.Range.Start < current)
                {
                    cc.Range.Select();
                    ActiveDocument.ActiveWindow.ScrollIntoView(cc.Range);
                    Globals.Chem4WordV3.SelectChemistry(ActiveDocument, ActiveDocument.ActiveWindow.Selection);
                    return;
                }
            }
        }

        // https://stackoverflow.com/a/50004583/2527555

        private DependencyObject GetScrollViewer(DependencyObject o)
        {
            // Return the DependencyObject if it is a ScrollViewer
            if (o is ScrollViewer)
            {
                return o;
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
            {
                var child = VisualTreeHelper.GetChild(o, i);

                var result = GetScrollViewer(child);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private void HandleScrollSpeed(object sender, MouseWheelEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                if (sender is DependencyObject dependencyObject
                    && GetScrollViewer(dependencyObject) is ScrollViewer scrollViewer)
                {
                    var items = scrollViewer.ExtentHeight;
                    var current = scrollViewer.VerticalOffset;
                    var amount = Math.Max(Math.Min(scrollViewer.ViewportHeight, 3), 1);

                    // e.Delta is +ve for scroll up and -ve for scroll down
                    if (e.Delta > 0 && current > 0)
                    {
                        scrollViewer.ScrollToVerticalOffset(current - amount);
                    }
                    if (e.Delta < 0 && current < items)
                    {
                        scrollViewer.ScrollToVerticalOffset(current + amount);
                    }

                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Exception", ex.Message);
                Globals.Chem4WordV3.Telemetry.Write(module, "Exception", ex.StackTrace);
            }
        }
    }
}