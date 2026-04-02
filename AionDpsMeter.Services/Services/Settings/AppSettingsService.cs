using System.Text.Json;
using System.Text.Json.Serialization;

namespace AionDpsMeter.Services.Services.Settings
{
    public sealed class AppSettingsService : IAppSettingsService
    {
        private const string SettingsFilePath = "appsettings.user.json";
        private readonly Lock _lock = new();
        private AppSettingsData _data;

        public event EventHandler? SettingsChanged;

        public AppSettingsService()
        {
            _data = Load();
        }

        public bool IsPacketLoggingEnabled
        {
            get { lock (_lock) return _data.IsPacketLoggingEnabled; }
            set
            {
                bool changed;
                lock (_lock)
                {
                    changed = _data.IsPacketLoggingEnabled != value;
                    _data.IsPacketLoggingEnabled = value;
                    if (changed) Save();
                }
                if (changed) SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool IsNicknameHidden
        {
            get { lock (_lock) return _data.IsNicknameHidden; }
            set
            {
                bool changed;
                lock (_lock)
                {
                    changed = _data.IsNicknameHidden != value;
                    _data.IsNicknameHidden = value;
                    if (changed) Save();
                }
                if (changed) SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public double? WindowLeft
        {
            get { lock (_lock) return _data.WindowLeft; }
            set { lock (_lock) { _data.WindowLeft = value; Save(); } }
        }

        public double? WindowTop
        {
            get { lock (_lock) return _data.WindowTop; }
            set { lock (_lock) { _data.WindowTop = value; Save(); } }
        }

        public double? WindowWidth
        {
            get { lock (_lock) return _data.WindowWidth; }
            set { lock (_lock) { _data.WindowWidth = value; Save(); } }
        }

        public double? WindowHeight
        {
            get { lock (_lock) return _data.WindowHeight; }
            set { lock (_lock) { _data.WindowHeight = value; Save(); } }
        }

        private AppSettingsData Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    return JsonSerializer.Deserialize<AppSettingsData>(json) ?? new AppSettingsData();
                }
            }
            catch { }
            return new AppSettingsData();
        }

        private void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch { }
        }

        private sealed class AppSettingsData
        {
            [JsonPropertyName("isPacketLoggingEnabled")]
            public bool IsPacketLoggingEnabled { get; set; }

            [JsonPropertyName("isNicknameHidden")]
            public bool IsNicknameHidden { get; set; }

            [JsonPropertyName("windowLeft")]
            public double? WindowLeft { get; set; }

            [JsonPropertyName("windowTop")]
            public double? WindowTop { get; set; }

            [JsonPropertyName("windowWidth")]
            public double? WindowWidth { get; set; }

            [JsonPropertyName("windowHeight")]
            public double? WindowHeight { get; set; }
        }
    }
}
