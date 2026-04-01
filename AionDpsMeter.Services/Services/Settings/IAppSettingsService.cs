namespace AionDpsMeter.Services.Services.Settings
{
    public interface IAppSettingsService
    {
        bool IsPacketLoggingEnabled { get; set; }
        bool IsNicknameHidden { get; set; }
        event EventHandler SettingsChanged;
    }
}
