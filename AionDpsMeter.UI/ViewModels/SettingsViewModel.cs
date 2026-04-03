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

        [ObservableProperty]
        private int _historyDamageThreshold;

        public SettingsViewModel(IAppSettingsService settingsService)
        {
            _settingsService = settingsService;
            _isPacketLoggingEnabled = settingsService.IsPacketLoggingEnabled;
            _isNicknameHidden = settingsService.IsNicknameHidden;
            _historyDamageThreshold = settingsService.HistoryDamageThreshold;
        }

        partial void OnIsPacketLoggingEnabledChanged(bool value)
        {
            _settingsService.IsPacketLoggingEnabled = value;
        }

        partial void OnIsNicknameHiddenChanged(bool value)
        {
            _settingsService.IsNicknameHidden = value;
        }

        partial void OnHistoryDamageThresholdChanged(int value)
        {
            _settingsService.HistoryDamageThreshold = Math.Clamp(value, 0, int.MaxValue);
        }
    }
}
