using AionDpsMeter.Services.Models;
using AionDpsMeter.Services.Services.Settings;

namespace AionDpsMeter.UI.ViewModels
{
    public sealed class PlayerStatsViewModel : ViewModelBase
    {
        private readonly PlayerStats _stats;
        private readonly IAppSettingsService _settings;

        // Smoothly-animated percentage for the progress bar
        private double _animatedPercentage;
        public double AnimatedPercentage => _animatedPercentage;

        public PlayerStatsViewModel(PlayerStats stats, IAppSettingsService settings)
        {
            _stats    = stats;
            _settings = settings;
            _animatedPercentage = stats.DamagePercentage;
        }

        public long    PlayerId          => _stats.PlayerId;
        public string  PlayerName        => _stats.PlayerName;
        public string  ServerName        => _stats.ServerName;
        /// <summary>Nickname formatted as <c>Name[Server]</c> when server is known, otherwise just <c>Name</c>.</summary>
        public string  PlayerNameDisplay => string.IsNullOrEmpty(_stats.ServerName)
                                               ? _stats.PlayerName
                                               : $"{_stats.PlayerName}[{_stats.ServerName}]";
        public string? PlayerIcon        => _stats.PlayerIcon;
        public string  ClassName         => _stats.ClassName;
        public string? ClassIcon         => _stats.ClassIcon;
        public int     CombatPower       => _stats.CombatPower;
        public int     CombatScore       => _stats.CombatScore;

        public bool    HasPlayerIcon     => !string.IsNullOrEmpty(_stats.PlayerIcon);
        public bool    HasClassIcon      => !string.IsNullOrEmpty(_stats.ClassIcon);

        public bool ShowCombatPower => _settings.IsNcApiEnabled && _settings.ShowCombatPower && _stats.CombatPower > 0;
        public bool ShowCombatScore => _settings.IsNcApiEnabled && _settings.ShowCombatScore && _stats.CombatScore > 0;

        public long   TotalDamage          => _stats.TotalDamage;
        public string TotalDamageFormatted => DamageFormatter.Format(_stats.TotalDamage);
        public double DamagePerSecond      => _stats.DamagePerSecond;
        public string DpsFormatted         => DamageFormatter.Format(_stats.DamagePerSecond);
        public double DamagePercentage     => _stats.DamagePercentage;
        public int    HitCount             => _stats.HitCount;
        public double CriticalRate         => _stats.CriticalRate;
        public double BackAttackRate       => _stats.BackAttackRate;

        public void Update(PlayerStats updatedStats)
        {
            var properties = typeof(PlayerStats).GetProperties();
            foreach (var prop in properties)
            {
                if (prop.CanWrite)
                    prop.SetValue(_stats, prop.GetValue(updatedStats));
            }

            // Smooth ease-towards animation for the progress bar
            double target = _stats.DamagePercentage;
            double diff   = target - _animatedPercentage;
            if (Math.Abs(diff) < 0.05)
                _animatedPercentage = target;
            else
                _animatedPercentage += diff * 0.25;

            OnPropertyChanged(nameof(TotalDamage));
            OnPropertyChanged(nameof(TotalDamageFormatted));
            OnPropertyChanged(nameof(DamagePerSecond));
            OnPropertyChanged(nameof(DpsFormatted));
            OnPropertyChanged(nameof(DamagePercentage));
            OnPropertyChanged(nameof(AnimatedPercentage));
            OnPropertyChanged(nameof(HitCount));
            OnPropertyChanged(nameof(CriticalRate));
            OnPropertyChanged(nameof(BackAttackRate));
            OnPropertyChanged(nameof(ClassName));
            OnPropertyChanged(nameof(ClassIcon));
            OnPropertyChanged(nameof(PlayerIcon));
            OnPropertyChanged(nameof(HasPlayerIcon));
            OnPropertyChanged(nameof(HasClassIcon));
            OnPropertyChanged(nameof(CombatPower));
            OnPropertyChanged(nameof(CombatScore));
            OnPropertyChanged(nameof(ShowCombatPower));
            OnPropertyChanged(nameof(ShowCombatScore));
            OnPropertyChanged(nameof(ServerName));
            OnPropertyChanged(nameof(PlayerNameDisplay));
        }
    }
}
