namespace AionDpsMeter.Services.Services.Settings
{
    public interface IAppSettingsService
    {
        bool IsPacketLoggingEnabled { get; set; }
        bool IsNcApiEnabled { get; set; }
        bool ShowCombatPower { get; set; }
        bool ShowCombatScore { get; set; }
        event EventHandler SettingsChanged;
    }
}
