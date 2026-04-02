using AionDpsMeter.Services.Models;
using AionDpsMeter.Services.Services.Settings;
using AionDpsMeter.UI.Utils;

namespace AionDpsMeter.UI.ViewModels.History
{
    public sealed class HistoryPlayerViewModel : ViewModelBase
    {
        private readonly PlayerStats _stats;
        private readonly IAppSettingsService _settingsService;

        public long    PlayerId              => _stats.PlayerId;
        public string  PlayerName            => _stats.PlayerName;
        public string  ServerName            => _stats.ServerName;
        public string? PlayerIcon            => _stats.PlayerIcon;
        public bool    HasPlayerIcon         => !string.IsNullOrEmpty(_stats.PlayerIcon);
        public string  ClassName             => _stats.ClassName;
        public string? ClassIcon             => _stats.ClassIcon;
        public bool    HasClassIcon          => !string.IsNullOrEmpty(_stats.ClassIcon);
        public bool    IsUser                => _stats.IsUser;
        public int     CombatPower           => _stats.CombatPower;

       
        public string PlayerNameDisplay
        {
            get
            {
                string name = _settingsService.IsNicknameHidden
                    ? NicknameObfuscator.Mask(_stats.PlayerName)
                    : _stats.PlayerName;

                return string.IsNullOrEmpty(_stats.ServerName)
                    ? name
                    : $"{name}[{_stats.ServerName}]";
            }
        }

        public string  TotalDamageDisplay    => DamageFormatter.FormatFull(_stats.TotalDamage);
        public long    TotalDamage           => _stats.TotalDamage;
        public string  DpsDisplay            => DamageFormatter.Format(_stats.DamagePerSecond);
        public double  DamagePercentage      => _stats.DamagePercentage;
        public string  DamagePercentDisplay  => DamageFormatter.FormatRate(_stats.DamagePercentage);
        public string  CritRateDisplay       => DamageFormatter.FormatRate(_stats.CriticalRate);
        public string  BackAttackRateDisplay => DamageFormatter.FormatRate(_stats.BackAttackRate);
        public string  PerfectRateDisplay    => DamageFormatter.FormatRate(_stats.PerfectRate);
        public string  DoubleDamageRateDisplay   => DamageFormatter.FormatRate(_stats.DoubleDamageRate);
        public string  ParryRateDisplay      => DamageFormatter.FormatRate(_stats.ParryRate);
        public string  DurationDisplay       => DamageFormatter.FormatDuration(_stats.CombatDuration);
        public int     HitCount              => _stats.HitCount;

        public IReadOnlyList<SkillStatsViewModel> Skills { get; }

        public HistoryPlayerViewModel(PlayerStats stats, IReadOnlyCollection<SkillStats> skills, IAppSettingsService settingsService)
        {
            _stats = stats;
            _settingsService = settingsService;
            Skills = skills
                .OrderByDescending(s => s.TotalDamage)
                .Select(s => new SkillStatsViewModel(s))
                .ToList();
        }
    }
}
