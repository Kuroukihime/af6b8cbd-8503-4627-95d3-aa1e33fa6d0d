using AionDpsMeter.Services.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AionDpsMeter.UI.ViewModels
{
    public sealed partial class SettingsViewModel : ViewModelBase
    {
        private readonly IAppSettingsService _settingsService;

        [ObservableProperty]
        private bool _isPacketLoggingEnabled;

        [ObservableProperty]
        private bool _isNicknameHidden;

        public SettingsViewModel(IAppSettingsService settingsService)
        {
            _settingsService = settingsService;
            _isPacketLoggingEnabled = settingsService.IsPacketLoggingEnabled;
            _isNicknameHidden = settingsService.IsNicknameHidden;
        }

        partial void OnIsPacketLoggingEnabledChanged(bool value)
        {
            _settingsService.IsPacketLoggingEnabled = value;
        }

        partial void OnIsNicknameHiddenChanged(bool value)
        {
            _settingsService.IsNicknameHidden = value;
        }
    }
}
