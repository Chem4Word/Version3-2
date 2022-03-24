using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Chem4Word.ACME;
using Chem4Word.ACME.Annotations;
using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Entities;
using Chem4Word.ACME.Models;
using Chem4Word.ACME.Models.Chem4Word.Controls.TagControl;
using Chem4Word.ACME.Utils;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Wpf;
using Chem4Word.Libraries;
using Chem4Word.Libraries.Database;
using IChem4Word.Contracts;
using Newtonsoft.Json;
using Size = System.Windows.Size;

namespace WinForms.TestLibrary.Wpf
{
    /// <summary>
    /// Interaction logic for CatalogueControl.xaml
    /// </summary>
    public partial class CatalogueControl : UserControl
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private IChem4WordTelemetry _telemetry;
        private LibraryOptions _libraryOptions;
        private AcmeOptions _acmeOptions;
        private Library _library;

        private int _filteredItems;
        private int _checkedItems;
        private int _itemCount;

        private List<string> _lastTags = new List<string>();

        private const string UserTagsFileName = "MyTags.json";
        private SortedDictionary<string, long> _userTags = new SortedDictionary<string, long>();

        public Point TopLeft { get; set; }

        public CatalogueControl()
        {
            InitializeComponent();
        }

        public bool ShowAllCarbonAtoms => _acmeOptions.ShowCarbons;
        public bool ShowImplicitHydrogens => _acmeOptions.ShowHydrogens;
        public bool ShowAtomsInColour => _acmeOptions.ColouredAtoms;
        public bool ShowMoleculeGrouping => _acmeOptions.ShowMoleculeGrouping;

        public Size ItemSize
        {
            get { return (Size)GetValue(ItemSizeProperty); }
            set { SetValue(ItemSizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemSizeProperty =
            DependencyProperty.Register("ItemSize", typeof(Size), typeof(CatalogueControl),
                                        new FrameworkPropertyMetadata(Size.Empty,
                                                                      FrameworkPropertyMetadataOptions.AffectsRender
                                                                      | FrameworkPropertyMetadataOptions.AffectsArrange
                                                                      | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public double DisplayWidth
        {
            get { return (double)GetValue(DisplayWidthProperty); }
            set { SetValue(DisplayWidthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DisplayWidthProperty =
            DependencyProperty.Register("DisplayWidth", typeof(double), typeof(CatalogueControl),
                                        new FrameworkPropertyMetadata(double.NaN,
                                                                      FrameworkPropertyMetadataOptions.AffectsRender
                                                                      | FrameworkPropertyMetadataOptions.AffectsArrange
                                                                      | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public double DisplayHeight
        {
            get { return (double)GetValue(DisplayHeightProperty); }
            set { SetValue(DisplayHeightProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DisplayHeightProperty =
            DependencyProperty.Register("DisplayHeight", typeof(double), typeof(CatalogueControl),
                                        new FrameworkPropertyMetadata(double.NaN,
                                                                      FrameworkPropertyMetadataOptions.AffectsRender
                                                                      | FrameworkPropertyMetadataOptions.AffectsArrange
                                                                      | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public void SetOptions(IChem4WordTelemetry telemetry, AcmeOptions acmeOptions, LibraryOptions libraryOptions)
        {
            _acmeOptions = acmeOptions;
            _telemetry = telemetry;
            _libraryOptions = libraryOptions;
            _library = new Library(_telemetry, _libraryOptions);
        }

        private void OnChemistryItemButtonClick(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is WpfEventArgs source
                && DataContext is CatalogueController controller)
            {
                Debug.WriteLine($"{_class} -> {source.Button} {source.OutputValue}");

                if (source.Button.StartsWith("CheckBox"))
                {
                    _itemCount = controller.ChemistryItems.Count;
                    _checkedItems = controller.ChemistryItems.Count(i => i.IsChecked);

                    TrashButton.IsEnabled = _checkedItems > 0;
                    CheckedFilterButton.IsEnabled = _checkedItems > 0;

                    UpdateStatusBar();
                }

                if (source.Button.StartsWith("DisplayDoubleClick"))
                {
                    var id = long.Parse(source.OutputValue.Split('=')[1]);
                    var item = controller.ChemistryItems.FirstOrDefault(o => o.Id == id);
                    if (item != null)
                    {
                        var topLeft = new Point(TopLeft.X + Constants.TopLeftOffset, TopLeft.Y + Constants.TopLeftOffset);
                        var result = UIUtils.ShowSketcher(_acmeOptions, _telemetry, topLeft, item.Cml);
                        if (result.IsDirty)
                        {
                            _library.UpdateChemistry(id, item.Name, result.Cml, result.Formua);

                            item.Cml = result.Cml;
                            item.Formula = result.Formua;
                            item.MolecularWeight = result.MolecularWeight;

                            controller.SelectedChemistryObject = item;
                        }
                    }
                }
            }
        }

        public void UpdateStatusBar()
        {
            var sb = new StringBuilder();
            if (_itemCount == 0
                && DataContext is CatalogueController controller)
            {
                _itemCount = controller.ChemistryItems.Count;
            }

            if (_filteredItems == 0)
            {
                sb.Append($"Showing all {_itemCount} items");
            }
            else
            {
                sb.Append($"Showing {_filteredItems} from {_itemCount}");
            }

            if (_checkedItems > 0)
            {
                sb.Append($" ({_checkedItems} checked)");
            }
            StatusBar.Text = sb.ToString();
        }

        private void Slider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ItemSize = new Size(Slider.Value, Slider.Value + 65);
            DisplayWidth = Slider.Value - 20;
            DisplayHeight = Slider.Value - 20;
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (string.IsNullOrWhiteSpace(SearchBox.Text))
                {
                    ClearButton_OnClick(null, null);
                }
                else
                {
                    SearchButton_OnClick(null, null);
                }
            }
        }

        private void SearchBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (string.IsNullOrWhiteSpace(SearchBox.Text))
                {
                    SearchButton.IsEnabled = false;
                    ClearButton.IsEnabled = false;
                }
                else
                {
                    SearchButton.IsEnabled = true;
                    ClearButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                //using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                //{
                //    form.ShowDialog();
                //}
            }
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (DataContext != null)
                {
                    ApplySort();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{module} {ex.Message}");
            }
        }

        private void ClearButton_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                CheckedFilterButton.IsChecked = false;
                SearchBox.Clear();

                if (DataContext != null)
                {
                    ClearFilter();
                    UpdateStatusBar();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{module} {ex.Message}");
            }
        }

        private void SearchButton_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                CheckedFilterButton.IsChecked = false;
                if (!string.IsNullOrWhiteSpace(SearchBox.Text)
                    && DataContext != null)
                {
                    FilterByText();
                    UpdateStatusBar();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                //using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                //{
                //    form.ShowDialog();
                //}
            }
        }

        private void CheckedFilterButton_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (DataContext != null)
                {
                    if (CheckedFilterButton.IsChecked != null && CheckedFilterButton.IsChecked.Value)
                    {
                        if (_checkedItems > 0)
                        {
                            SearchBox.Clear();
                            FilterByChecked();
                        }
                        else
                        {
                            CheckedFilterButton.IsChecked = false;
                        }
                    }
                    else
                    {
                        SearchBox.Clear();
                        ClearFilter();
                    }

                    UpdateStatusBar();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                //using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                //{
                //    form.ShowDialog();
                //}
            }
        }

        public event EventHandler<WpfEventArgs> OnSelectionChange;

        private void OnSelectionItemChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource is ListBox source
                && source.SelectedItem is ChemistryObject selected
                && source.DataContext is CatalogueController context)
            {
                context.SelectedChemistryObject = selected;
                _lastTags = context.SelectedChemistryObject.Tags;

                var args = new WpfEventArgs
                {
                    Button = "CatalogueView|SelectedItemChanged",
                    OutputValue = $"Id={selected.Id}"
                };
                OnSelectionChange?.Invoke(this, args);
            }
        }

        private class ChemistryObjectComparer : IComparer<ChemistryObject>
        {
            public int Compare(ChemistryObject x, ChemistryObject y)
                => string.CompareOrdinal(x?.Name, y?.Name);
        }

        private void ApplySort()
        {
            if (ComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var view = (ListCollectionView)CollectionViewSource.GetDefaultView(CatalogueItems.ItemsSource);
                view.SortDescriptions.Clear();

                var propertyName = selectedItem.Content.ToString();
                if (propertyName.Equals("Name"))
                {
                    view.CustomSort = (IComparer)new ChemistryObjectComparer();
                }
                else
                {
                    view.SortDescriptions.Add(new SortDescription(propertyName, ListSortDirection.Ascending));
                }
            }
        }

        private void ClearFilter()
        {
            //get the view from the listbox's source
            var view = (ListCollectionView)CollectionViewSource.GetDefaultView(CatalogueItems.ItemsSource);

            _filteredItems = 0;
            view.Filter = null;
        }

        private void FilterByText()
        {
            //get the view from the listbox's source
            var view = (ListCollectionView)CollectionViewSource.GetDefaultView(CatalogueItems.ItemsSource);

            //then try to match part of either its name or an alternative name to the string typed in
            _filteredItems = 0;
            view.Filter = ci =>
                          {
                              var item = ci as ChemistryObject;
                              var queryString = SearchBox.Text.ToUpper();
                              if (item != null
                                  && (item.Name.ToUpper().Contains(queryString)
                                      || item.OtherNames.Any(n => n.ToUpper().Contains(queryString))
                                      || item.Tags.Any(n => n.ToUpper().Contains(queryString)))
                              )
                              {
                                  _filteredItems++;
                                  return true;
                              }

                              return false;
                          };
        }

        private void FilterByChecked()
        {
            //get the view from the listbox's source
            var view = (ListCollectionView)CollectionViewSource.GetDefaultView(CatalogueItems.ItemsSource);

            //then try to match part of either its name or an alternative name to the string typed in
            _filteredItems = 0;
            view.Filter = ci =>
                          {
                              var item = ci as ChemistryObject;
                              if (item != null
                                  && item.IsChecked)
                              {
                                  _filteredItems++;
                                  return true;
                              }

                              return false;
                          };
        }

        private void AddButton_OnClick(object sender, RoutedEventArgs e)
        {
            // ToDo: Implement
            Debug.WriteLine($"{_class} -> Add Button Clicked");
        }

        private void BrowseButton_OnClick(object sender, RoutedEventArgs e)
        {
            // ToDo: Implement
            Debug.WriteLine($"{_class} -> Browse Button Clicked");
        }

        private void TrashButton_OnClick(object sender, RoutedEventArgs e)
        {
            // ToDo: Implement
            Debug.WriteLine($"{_class} -> Trash Button Clicked");
        }

        private void ImportButton_OnClick(object sender, RoutedEventArgs e)
        {
            // ToDo: Implement
            Debug.WriteLine($"{_class} -> Import Button Clicked");
        }

        private void ExportButton_OnClick(object sender, RoutedEventArgs e)
        {
            // ToDo: Implement
            Debug.WriteLine($"{_class} -> Export Button Clicked");
        }

        private void TagControlModelOnTagRemoved(object sender, WpfEventArgs e)
        {
            if (sender is TagControlModel tcm)
            {
                UpdateTags(tcm);
            }
        }

        public FrequencyTable<WordGroup> Words;
        private WordCloudData _wordcloudControlDataContext;
        private WordCloudData _wordCloudData;

        public WordCloudData WordCloudData
        {
            get { return _wordCloudData; }
            set
            {
                _wordCloudData = value;
                OnPropertyChanged(nameof(WordCloudData));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnSelectedItemChanged(object sender, DataTransferEventArgs e)
        {
            if (DataContext is CatalogueController dc
                && dc.SelectedChemistryObject != null)
            {
                // ToDo: Handle null SettingsPath
                var userTagsFile = Path.Combine(_acmeOptions.SettingsPath, UserTagsFileName);
                if (File.Exists(userTagsFile))
                {
                    try
                    {
                        var json = File.ReadAllText(userTagsFile);
                        _userTags = JsonConvert.DeserializeObject<SortedDictionary<string, long>>(json);
                    }
                    catch
                    {
                        _userTags = new SortedDictionary<string, long>();
                    }
                }

                var userTags = _userTags.Select(t => t.Key).ToList();
                var databaseTagsDto = _library.GetAllTags();
                var databaseTags = databaseTagsDto.Select(t => t.Text).ToList();

                var structureTags = dc.SelectedChemistryObject.Tags;
                _lastTags = structureTags;
                var availableTags = databaseTags.Union(userTags).Except(structureTags).ToList();

                TaggingControl.TagControlModel = new TagControlModel(new ObservableCollection<string>(availableTags),
                                                                     new ObservableCollection<string>(structureTags),
                                                                     _userTags);
                TaggingControl.DataContext = TaggingControl.TagControlModel;

                TaggingControl.TagControlModel.OnTagRemoved -= TagControlModelOnTagRemoved;
                TaggingControl.TagControlModel.OnTagRemoved += TagControlModelOnTagRemoved;

                var cloudTags = _userTags;
                foreach (var tag in databaseTagsDto)
                {
                    var frequency = Math.Max(1, tag.Frequency);
                    if (cloudTags.ContainsKey(tag.Text))
                    {
                        cloudTags[tag.Text] = Math.Max(cloudTags[tag.Text], frequency);
                    }
                    else
                    {
                        cloudTags.Add(tag.Text, frequency);
                    }
                }

                var wordlist = new List<FrequencyTableRow<WordGroup>>();
                long count = 0;
                foreach (var kvp in cloudTags.OrderByDescending(x => x.Value))
                {
                    wordlist.Add(new FrequencyTableRow<WordGroup>(new WordGroup(kvp.Key), kvp.Value));
                    count += kvp.Value;
                }

                WordCloudControl.Reset();

                if (count > 0)
                {
                    Words = new FrequencyTable<WordGroup>(wordlist, count);
                    _wordcloudControlDataContext = new WordCloudData(Words);
                    WordCloudControl.DataContext = _wordcloudControlDataContext;

                    WordCloudControl.AddWords(_wordcloudControlDataContext);
                    int failures = WordCloudControl.Failures;
                    if (failures > 0)
                    {
                        Debug.WriteLine($"Failed to place {failures} words");
                    }
                }
            }
        }

        private void TaggingControl_OnLostFocus(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            if (sender is TaggingControl tc
                && tc.DataContext is TagControlModel tcm)
            {
                UpdateTags(tcm);
            }
        }

        // Save the Tags to the database and update the used frequencies
        private void UpdateTags(TagControlModel tcm)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            // Collate new tags for database
            List<string> tags = new List<string>();
            foreach (var tag in tcm.CurrentTags.OfType<TagItem>())
            {
                tags.Add(tag.ItemLabel.Content as string);
            }

            // Prevent the database and files being written any more than necessary
            string lt = string.Join(",", _lastTags);
            string tt = string.Join(",", tags);
            if (!lt.Equals(tt))
            {
                // Save the updated user file
                // ToDo: Handle _acmeOptions.SettingsPath is null
                var userTagsFile = Path.Combine(_acmeOptions.SettingsPath, UserTagsFileName);
                var jsonOut = JsonConvert.SerializeObject(_userTags, Formatting.Indented);
                File.WriteAllText(userTagsFile, jsonOut);

                // Update the database
                var sw = new Stopwatch();
                sw.Start();

                var dc = DataContext as CatalogueController;
                _library.AddTags(dc.SelectedChemistryObject.Id, tags);

                sw.Stop();
                _telemetry.Write(module, "Timing", $"Writing {tags.Count} tags took {SafeDouble.AsString(sw.ElapsedMilliseconds)}ms");

                // Update the grid view
                dc.SelectedChemistryObject.Tags = tags;

                _lastTags = tags;
            }
        }

        private void CatalogueControl_OnWordSelected(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is string word)
            {
                Debug.WriteLine($"Clicked on '{word}'");
                TaggingControl.TagControlModel.AddTag(word);
                UpdateTags(TaggingControl.TagControlModel);
            }
        }
    }
}