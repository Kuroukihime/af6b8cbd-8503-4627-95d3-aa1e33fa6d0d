using AionDpsMeter.Services.Services.Settings;
using AionDpsMeter.UI.ViewModels;
using AionDpsMeter.UI.ViewModels.History;
using System.Windows;
using System.Windows.Input;

namespace AionDpsMeter.UI
{
    public partial class HistorySessionWindow : Window
    {
        private readonly IAppSettingsService _settingsService;

        public HistorySessionWindow(IAppSettingsService settingsService)
        {
            InitializeComponent();
            _settingsService = settingsService;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void PlayerItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement el &&
                el.Tag is HistoryPlayerViewModel player &&
                DataContext is HistorySessionViewModel session)
            {
                var vm = PlayerDetailsViewModel.FromSnapshot(player, _settingsService, session.TargetName);
                var detailsWindow = new PlayerDetailsWindow
                {
                    DataContext = vm,
                    Owner = this
                };
                detailsWindow.Show();
            }
        }
    }
}
