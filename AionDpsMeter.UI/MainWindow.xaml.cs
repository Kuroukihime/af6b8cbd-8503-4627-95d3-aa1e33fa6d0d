using AionDpsMeter.Services.Services.Settings;
using AionDpsMeter.UI.Utils;
using AionDpsMeter.UI.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace AionDpsMeter.UI
{
    public partial class MainWindow : Window
    {
        private readonly SettingsViewModel settingsViewModel;
        private readonly IAppSettingsService settingsService;
        private SettingsWindow? settingsWindow;
        private HistoryWindow? historyWindow;

        public MainWindow(MainViewModel viewModel, SettingsViewModel settingsViewModel, IAppSettingsService settingsService)
        {
            InitializeComponent();
            DataContext = viewModel;
            this.settingsViewModel = settingsViewModel;
            this.settingsService = settingsService;

      
            RestoreWindowBounds();
        }

     
        private void RestoreWindowBounds()
        {
            var left   = settingsService.WindowLeft;
            var top    = settingsService.WindowTop;
            var width  = settingsService.WindowWidth;
            var height = settingsService.WindowHeight;

            if (!left.HasValue || !top.HasValue)
                return;

            double w = width.HasValue  ? Math.Max(MinWidth,  width.Value)  : Width;
            double h = height.HasValue ? Math.Max(MinHeight, height.Value) : Height;

            var wa = ScreenHelper.GetWorkingAreaForPoint(left.Value, top.Value);

            double l = Math.Max(wa.Left, Math.Min(left.Value, wa.Right  - w));
            double t = Math.Max(wa.Top,  Math.Min(top.Value,  wa.Bottom - h));

            WindowStartupLocation = WindowStartupLocation.Manual;
            Left   = l;
            Top    = t;
            Width  = w;
            Height = h;
        }

      
        private void SaveWindowBounds()
        {
            if (WindowState != WindowState.Normal) return;
            settingsService.WindowLeft   = Left;
            settingsService.WindowTop    = Top;
            settingsService.WindowWidth  = Width;
            settingsService.WindowHeight = Height;
        }

        private void PositionWindowToRight(Window child)
        {
            const double gap = 8;

            var wa = ScreenHelper.GetWorkingAreaForWindow(this);

            double mainRight     = Left + Width;
            double candidateLeft = mainRight + gap;

            double childLeft;
            if (candidateLeft + child.Width <= wa.Right)
            {
                childLeft = candidateLeft;
            }
            else
            {
                childLeft = Math.Max(wa.Left, wa.Right - child.Width);
            }

            double childTop = Math.Max(wa.Top, Math.Min(Top, wa.Bottom - child.Height));

            child.WindowStartupLocation = WindowStartupLocation.Manual;
            child.Left = childLeft;
            child.Top  = childTop;
        }

        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
            SaveWindowBounds();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            SaveWindowBounds();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainViewModel viewModel) return;

            // Singleton: bring existing window to front instead of opening a new one
            if (historyWindow is { IsVisible: true })
            {
                historyWindow.Activate();
                return;
            }

            var snapshot = viewModel.SessionManager.GetHistorySnapshot();

            historyWindow = new HistoryWindow(settingsService)
            {
                DataContext = new AionDpsMeter.UI.ViewModels.History.HistoryViewModel(snapshot),
                Owner = this
            };

            PositionWindowToRight(historyWindow);
            historyWindow.Show();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (settingsWindow is { IsVisible: true })
            {
                settingsWindow.Activate();
                return;
            }

            settingsWindow = new SettingsWindow
            {
                DataContext = settingsViewModel,
                Owner = this
            };

            PositionWindowToRight(settingsWindow);
            settingsWindow.Show();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
                viewModel.Dispose();
            Application.Current.Shutdown();
        }

        private void PlayerItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element &&
                element.Tag is PlayerStatsViewModel player &&
                DataContext is MainViewModel viewModel)
            {
                var detailsWindow = new PlayerDetailsWindow
                {
                    DataContext = new PlayerDetailsViewModel(
                        viewModel.SessionManager,
                        player.PlayerId,
                        player.PlayerName,
                        player.ClassName,
                        player.PlayerIcon,
                        player.ClassIcon,
                        settingsService,
                        player.CombatPower,
                        player.ServerName),
                    Owner = this
                };

                PositionWindowToRight(detailsWindow);
                detailsWindow.Show();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
                viewModel.Dispose();
            base.OnClosed(e);
        }
    }
}