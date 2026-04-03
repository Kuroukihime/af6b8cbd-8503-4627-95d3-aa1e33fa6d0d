namespace AionDpsMeter.Services.Services.Settings
{
    public interface IAppSettingsService
    {
        bool IsPacketLoggingEnabled { get; set; }
        bool IsNicknameHidden { get; set; }
        int HistoryDamageThreshold { get; set; }

        // Main window position & size
        double? WindowLeft { get; set; }
        double? WindowTop { get; set; }
        double? WindowWidth { get; set; }
        double? WindowHeight { get; set; }

        event EventHandler SettingsChanged;
    }
}
