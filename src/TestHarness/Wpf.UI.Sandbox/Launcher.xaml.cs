using System.Windows;

namespace Wpf.UI.Sandbox
{
    /// <summary>
    /// Interaction logic for Launcher.xaml
    /// </summary>
    public partial class Launcher : Window
    {
        public Launcher()
        {
            InitializeComponent();
        }

        private void OnClick_Button1(object sender, RoutedEventArgs e)
        {
            var window = new ShapesUI();
            window.ShowDialog();
        }

        private void OnClick_Button2(object sender, RoutedEventArgs e)
        {
            var top = Top + Height - 80;
            var window = new Ticker();
            window.Height = 80;
            window.Left = Left;
            window.Top = top;
            window.ShowDialog();
        }
    }
}