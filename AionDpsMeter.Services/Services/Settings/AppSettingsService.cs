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
                lock (_lock) { changed = _data.IsPacketLoggingEnabled != value; _data.IsPacketLoggingEnabled = value; if (changed) Save(); }
                if (changed) SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool IsNcApiEnabled
        {
            get { lock (_lock) return _data.IsNcApiEnabled; }
            set
            {
                bool changed;
                lock (_lock) { changed = _data.IsNcApiEnabled != value; _data.IsNcApiEnabled = value; if (changed) Save(); }
                if (changed) SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool ShowCombatPower
        {
            get { lock (_lock) return _data.ShowCombatPower; }
            set
            {
                bool changed;
                lock (_lock) { changed = _data.ShowCombatPower != value; _data.ShowCombatPower = value; if (changed) Save(); }
                if (changed) SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool ShowCombatScore
        {
            get { lock (_lock) return _data.ShowCombatScore; }
            set
            {
                bool changed;
                lock (_lock) { changed = _data.ShowCombatScore != value; _data.ShowCombatScore = value; if (changed) Save(); }
                if (changed) SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
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

            [JsonPropertyName("isNcApiEnabled")]
            public bool IsNcApiEnabled { get; set; } = true;

            [JsonPropertyName("showCombatPower")]
            public bool ShowCombatPower { get; set; } = true;

            [JsonPropertyName("showCombatScore")]
            public bool ShowCombatScore { get; set; } = true;
        }
    }
}
