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
        private bool _isNcApiEnabled;

        [ObservableProperty]
        private bool _showCombatPower;

        [ObservableProperty]
        private bool _showCombatScore;

        public SettingsViewModel(IAppSettingsService settingsService)
        {
            _settingsService = settingsService;
            _isPacketLoggingEnabled = settingsService.IsPacketLoggingEnabled;
            _isNcApiEnabled         = settingsService.IsNcApiEnabled;
            _showCombatPower        = settingsService.ShowCombatPower;
            _showCombatScore        = settingsService.ShowCombatScore;
        }

        partial void OnIsPacketLoggingEnabledChanged(bool value) { _settingsService.IsPacketLoggingEnabled = value; }
        partial void OnIsNcApiEnabledChanged(bool value)         { _settingsService.IsNcApiEnabled = value; }
        partial void OnShowCombatPowerChanged(bool value)        { _settingsService.ShowCombatPower = value; }
        partial void OnShowCombatScoreChanged(bool value)        { _settingsService.ShowCombatScore = value; }
    }
}
