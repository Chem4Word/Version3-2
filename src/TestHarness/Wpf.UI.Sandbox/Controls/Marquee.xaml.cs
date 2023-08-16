using Chem4Word.Core.UI.Wpf;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Wpf.UI.Sandbox.Controls
{
    /// <summary>
    /// Interaction logic for Marquee.xaml
    /// </summary>
    public partial class Marquee : UserControl
    {
        public event EventHandler<WpfEventArgs> OnAnimationCompleted;

        public Marquee()
        {
            InitializeComponent();
        }

        private void Marquee_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                Start("This control is running in design mode!");
            }
        }

        public void Start(string text)
        {
            TextToScroll.Text = text;
            var doubleAnimation = new DoubleAnimation
            {
                From = -TextToScroll.ActualWidth,
                To = ScrollingRegion.ActualWidth,
                Duration = new Duration(TimeSpan.Parse("0:0:15"))
            };

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                doubleAnimation.RepeatBehavior = RepeatBehavior.Forever;
            }
            doubleAnimation.Completed += DoubleAnimationOnCompleted;
            TextToScroll.BeginAnimation(Canvas.RightProperty, doubleAnimation);
        }

        private void DoubleAnimationOnCompleted(object sender, EventArgs e)
        {
            WpfEventArgs args = new WpfEventArgs();
            OnAnimationCompleted?.Invoke(this, args);
        }
    }
}