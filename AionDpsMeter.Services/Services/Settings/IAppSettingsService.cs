namespace AionDpsMeter.Services.Services.Settings
{
    public interface IAppSettingsService
    {
        bool IsPacketLoggingEnabled { get; set; }
        event EventHandler SettingsChanged;
    }
}
