using Chem4Word.Core.UI.Wpf;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace Wpf.UI.Sandbox
{
    /// <summary>
    /// Interaction logic for Ticker.xaml
    /// </summary>
    public partial class Ticker : Window
    {
        private int _index = 0;
        private List<TickerItem> _tickerItems = new List<TickerItem>
        {
            new TickerItem { Text = "#1 This is news item #1", Url = "https://item1" },
            new TickerItem { Text = "#2 This is news item #2", Url = "https://item2" },
            new TickerItem { Text = "#3 This is news item #3", Url = "https://item3" },
            new TickerItem { Text = "#4 This is news item #4", Url = "https://item4" },
            new TickerItem { Text = "#5 This is news item #5", Url = "https://item5" }
        };

        public Ticker()
        {
            InitializeComponent();
        }

        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine($"User Clicked {_tickerItems[_index].Url}");
        }

        private void Ticker_OnLoaded(object sender, RoutedEventArgs e)
        {
            Marquee.OnAnimationCompleted += MarqueeOnOnAnimationCompleted;
            Marquee.Start(_tickerItems[_index].Text);
        }

        private void MarqueeOnOnAnimationCompleted(object sender, WpfEventArgs e)
        {
            _index++;
            if (_index >= _tickerItems.Count)
            {
                _index = 0;
            }
            Marquee.Start(_tickerItems[_index].Text);
        }
    }
}