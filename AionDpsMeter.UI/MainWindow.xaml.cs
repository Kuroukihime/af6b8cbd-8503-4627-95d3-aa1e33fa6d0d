using AionDpsMeter.UI.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace AionDpsMeter.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Allow dragging window
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.Dispose();
            }
            Application.Current.Shutdown();
        }

        private void PlayerItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element &&
                element.Tag is PlayerStatsViewModel player &&
                DataContext is MainViewModel viewModel)
            {
                // Open player details in a new window
                var detailsWindow = new PlayerDetailsWindow
                {
                    DataContext = new PlayerDetailsViewModel(
                        viewModel.SessionManager,
                        player.PlayerId,
                        player.PlayerName,
                        player.ClassName,
                        player.PlayerIcon,
                        player.ClassIcon),
                    Owner = this
                };
                detailsWindow.Show();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.Dispose();
            }
            base.OnClosed(e);
        }
    }
}