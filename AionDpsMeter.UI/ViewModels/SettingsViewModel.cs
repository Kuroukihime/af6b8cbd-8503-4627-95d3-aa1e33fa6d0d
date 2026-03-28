using AionDpsMeter.Services.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AionDpsMeter.UI.ViewModels
{
    public sealed partial class SettingsViewModel : ViewModelBase
    {
        private readonly IAppSettingsService _settingsService;

        [ObservableProperty]
        private bool _isPacketLoggingEnabled;

        public SettingsViewModel(IAppSettingsService settingsService)
        {
            _settingsService = settingsService;
            _isPacketLoggingEnabled = settingsService.IsPacketLoggingEnabled;
        }

        partial void OnIsPacketLoggingEnabledChanged(bool value)
        {
            _settingsService.IsPacketLoggingEnabled = value;
        }
    }
}
