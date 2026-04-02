using AionDpsMeter.Services.Services.Settings;
using AionDpsMeter.UI.ViewModels.History;
using System.Windows;
using System.Windows.Input;

namespace AionDpsMeter.UI
{
    public partial class HistoryWindow : Window
    {
        private readonly IAppSettingsService _settingsService;

        public HistoryWindow(IAppSettingsService settingsService)
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

        private void SessionItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement el && el.Tag is HistoryEntryViewModel entry)
            {
                var sessionWindow = new HistorySessionWindow(_settingsService)
                {
                    DataContext = new HistorySessionViewModel(entry.Snapshot, _settingsService),
                    Owner = this
                };
                sessionWindow.Show();
            }
        }
    }
}
