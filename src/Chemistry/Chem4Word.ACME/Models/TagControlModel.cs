// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Chem4Word.ACME.Controls;
using Chem4Word.Core.UI.Wpf;

namespace Chem4Word.ACME.Models
{
    namespace Chem4Word.Controls.TagControl
    {
        public class TagControlModel : DependencyObject
        {
            private readonly SortedDictionary<string, long> _userTags = new SortedDictionary<string, long>();

            /// <summary>
            /// Create a TagControlModel with no existing tags
            /// </summary>
            public TagControlModel()
            {
                CurrentTags = new ObservableCollection<object>();
                AvailableTags = new ObservableCollection<string>();

                // Add the textBox
                CreateAutoCompleteBox();
            }

            /// <summary>
            /// Create a TagControlModel with current and known tags
            /// </summary>
            /// <param name="availableTags"></param>
            /// <param name="currentTags"></param>
            /// <param name="userTags"></param>
            public TagControlModel(ObservableCollection<string> availableTags, ObservableCollection<string> currentTags, SortedDictionary<string, long> userTags)
            {
                _userTags = userTags;
                CurrentTags = new ObservableCollection<object>();
                AvailableTags = availableTags;
                foreach (var tag in currentTags)
                {
                    AddTag(tag);
                }

                // Now add the textBox
                CreateAutoCompleteBox();
            }

            public ObservableCollection<object> CurrentTags { get; set; }
            public ObservableCollection<string> AvailableTags { get; set; }

            #region Tag Management

            public void AddTag(string newTagText)
            {
                var tags = CurrentTags.OfType<TagItem>().Select(t => t.ItemLabel.Content as string).ToList();
                if (!tags.Contains(newTagText.ToLower(CultureInfo.InvariantCulture)))
                {
                    // Create a TagControlItem
                    TagItem tagControlItem = new TagItem();
                    tagControlItem.ItemLabel.Content = newTagText.ToLower(CultureInfo.InvariantCulture);

                    // Add the remove event to this tag
                    tagControlItem.Image.MouseUp += TagControlItem_OnMouseUp;

                    // Determine position of where to insert the new tag
                    int position = CurrentTags.Count;
                    if (CurrentTags.Count > 0
                        && CurrentTags[CurrentTags.Count - 1] is AutoCompleteBox)
                    {
                        // If the last object in the set is an AutoCompleteBox, then this one goes before it
                        position--;
                    }

                    // Add the new tag to the collection
                    CurrentTags.Insert(position, tagControlItem);

                    // Remove it from the collection of available tags
                    if (AvailableTags.Contains(newTagText))
                    {
                        AvailableTags.Remove(newTagText);
                    }
                }
            }

            /// <summary>
            /// Add multiple tags from a collection of strings
            /// </summary>
            /// <param name="collectionOfTags"></param>
            public void AddTags(ObservableCollection<string> collectionOfTags)
            {
                // Hope the collection isn't empty
                foreach (var tag in collectionOfTags)
                {
                    AddTag(tag);
                }
            }

            public void AddKnownTags(ObservableCollection<string> collectionOfTags)
            {
                foreach (var tag in collectionOfTags)
                {
                    AvailableTags.Add(tag);
                }
            }

            private void CreateAutoCompleteBox()
            {
                var autoCompleteBox = new AutoCompleteBox();
                autoCompleteBox.ItemsSource = AvailableTags;
                autoCompleteBox.MinWidth = 70;
                autoCompleteBox.Height = 25;
                autoCompleteBox.Width = double.NaN; // Auto
                autoCompleteBox.VerticalAlignment = VerticalAlignment.Center;
                autoCompleteBox.Margin = new Thickness(2.0);

                autoCompleteBox.KeyUp += TextBox_KeyUp;
                autoCompleteBox.DropDownClosed += AutoCompleteBox_DropDownClosed;

                // Add at end
                CurrentTags.Add(autoCompleteBox);
            }

            #endregion Tag Management

            #region Event handlers

            private void AutoCompleteBox_DropDownClosed(object sender, RoutedPropertyChangedEventArgs<bool> e)
            {
                if (sender is AutoCompleteBox autoCompleteBox
                    && autoCompleteBox.SelectedItem != null
                    && !string.IsNullOrEmpty(autoCompleteBox.Text))
                {
                    AddNewTag(autoCompleteBox);
                }
            }

            private void TextBox_KeyUp(object sender, KeyEventArgs e)
            {
                if (sender is AutoCompleteBox autoCompleteBox
                    && autoCompleteBox.Text.Length > 1)
                {
                    if (e.Key == Key.Enter)
                    {
                        AddNewTag(autoCompleteBox);
                    }

                    if (!string.IsNullOrEmpty(autoCompleteBox.Text))
                    {
                        // Grab the last character of the textbox
                        var lastChar = autoCompleteBox.Text[autoCompleteBox.Text.Length - 1];

                        // Any punctuation mark is acceptable, but exclude '-' as terminator because we want it!
                        if (char.IsPunctuation(lastChar) && !lastChar.Equals('-'))
                        {
                            AddNewTag(autoCompleteBox, true);
                        }
                    }
                }
            }

            private void TagControlItem_OnMouseUp(object sender, MouseButtonEventArgs e)
            {
                var parent = VisualTreeHelpers.FindAncestor<TagItem>(sender as Image);
                if (parent != null)
                {
                    // Remove this label from the collection
                    AvailableTags.Add(parent.ItemLabel.Content as string);
                    CurrentTags.Remove(parent);

                    var args = new WpfEventArgs();
                    TagRemoved(this, args);
                }
            }

            private void AddNewTag(AutoCompleteBox autoCompleteBox, bool exceptLastCharacter = false)
            {
                var tag = autoCompleteBox.Text.ToLowerInvariant();
                if (exceptLastCharacter)
                {
                    tag = autoCompleteBox.Text.Substring(0, autoCompleteBox.Text.Length - 1).ToLowerInvariant();
                }

                // Add a tag using the content of the textbox
                AddTag(tag);

                if (_userTags.ContainsKey(tag))
                {
                    _userTags[tag]++;
                }
                else
                {
                    _userTags.Add(tag, 1);
                }

                //Clear the text from the textbox
                autoCompleteBox.Text = "";
                autoCompleteBox.Focus();
            }

            public event EventHandler<WpfEventArgs> OnTagRemoved;

            private void TagRemoved(object sender, WpfEventArgs e)
            {
                OnTagRemoved?.Invoke(this, e);
            }

            #endregion Event handlers
        }
    }
}