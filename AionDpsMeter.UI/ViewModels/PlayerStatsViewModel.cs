using AionDpsMeter.Services.Models;

namespace AionDpsMeter.UI.ViewModels
{
    public sealed class PlayerStatsViewModel : ViewModelBase
    {
        private readonly PlayerStats stats;

        public PlayerStatsViewModel(PlayerStats stats)
        {
            this.stats = stats;
        }

        public long PlayerId => stats.PlayerId;
        public string PlayerName => stats.PlayerName;
        public string? PlayerIcon => stats.PlayerIcon;
        public string ClassName => stats.ClassName;
        public string? ClassIcon => stats.ClassIcon;
        public bool HasPlayerIcon => !string.IsNullOrEmpty(stats.PlayerIcon);
        public bool HasClassIcon => !string.IsNullOrEmpty(stats.ClassIcon);
        public long TotalDamage => stats.TotalDamage;
        public string TotalDamageFormatted => FormatDamage(stats.TotalDamage);
        public double DamagePerSecond => stats.DamagePerSecond;
        public string DpsFormatted => FormatDamage((long)stats.DamagePerSecond);
        public double DamagePercentage => stats.DamagePercentage;
        public int HitCount => stats.HitCount;
        public double CriticalRate => stats.CriticalRate;
        public double BackAttackRate => stats.BackAttackRate;

        public void Update(PlayerStats updatedStats)
        {
            var properties = typeof(PlayerStats).GetProperties();
            foreach (var prop in properties)
            {
                if (prop.CanWrite)
                {
                    prop.SetValue(stats, prop.GetValue(updatedStats));
                }
            }

            // Notify all properties changed
            OnPropertyChanged(nameof(TotalDamage));
            OnPropertyChanged(nameof(TotalDamageFormatted));
            OnPropertyChanged(nameof(DamagePerSecond));
            OnPropertyChanged(nameof(DpsFormatted));
            OnPropertyChanged(nameof(DamagePercentage));
            OnPropertyChanged(nameof(HitCount));
            OnPropertyChanged(nameof(CriticalRate));
            OnPropertyChanged(nameof(BackAttackRate));
            OnPropertyChanged(nameof(ClassName));
            OnPropertyChanged(nameof(ClassIcon));
            OnPropertyChanged(nameof(PlayerIcon));
            OnPropertyChanged(nameof(HasPlayerIcon));
            OnPropertyChanged(nameof(HasClassIcon));
        }

        private static string FormatDamage(long damage)
        {
            if (damage >= 1_000_000_000)
                return $"{damage / 1_000_000_000.0:F2}B";
            if (damage >= 1_000_000)
                return $"{damage / 1_000_000.0:F2}M";
            if (damage >= 1_000)
                return $"{damage / 1_000.0:F2}K";
            return damage.ToString();
        }
    }
}
