using AionDpsMeter.Services.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace AionDpsMeter.UI.ViewModels
{
    public sealed class SkillStatsViewModel : ViewModelBase
    {
        private readonly SkillStats stats;

        public SkillStatsViewModel(SkillStats stats)
        {
            this.stats = stats;
        }

        public long SkillId => stats.SkillId;
        public string SkillName => stats.SkillName;
        public string? SkillIcon => stats.SkillIcon;
        public bool HasSkillIcon => !string.IsNullOrEmpty(stats.SkillIcon);
        public long TotalDamage => stats.TotalDamage;
        public string TotalDamageFormatted => FormatDamage(stats.TotalDamage);
        public int HitCount => stats.HitCount;
        public string HitCountFormatted => stats.HitCount.ToString();
        public int CriticalHits => stats.CriticalHits;
        public int BackAttacks => stats.BackAttacks;
        public int PerfectHits => stats.PerfectHits;
        public int DoubleDamageHits => stats.DoubleDamageHits;
        public int ParryHits => stats.ParryHits;
        public double AverageDamage => stats.AverageDamage;
        public string AverageDamageFormatted => FormatDamage((long)stats.AverageDamage);
        public long MaxHit => stats.MaxHit;
        public long MinHit => stats.MinHit == long.MaxValue ? 0 : stats.MinHit;
        public double DamagePercentage => stats.DamagePercentage;
        public string DamagePercentageFormatted => $"{stats.DamagePercentage:F1}%";
        public double DamagePerSecond => stats.DamagePerSecond;
        public string DpsFormatted => FormatDamage((long)stats.DamagePerSecond);
        public double CriticalRate => stats.CriticalRate;
        public string CriticalRateFormatted => $"{stats.CriticalRate:F0}%";
        public double BackAttackRate => stats.BackAttackRate;
        public string BackAttackRateFormatted => $"{stats.BackAttackRate:F0}%";
        public double PerfectRate => stats.PerfectRate;
        public string PerfectRateFormatted => $"{stats.PerfectRate:F0}%";
        public double DoubleDamageRate => stats.DoubleDamageRate;
        public string DoubleDamageRateFormatted => $"{stats.DoubleDamageRate:F0}%";
        public double ParryRate => stats.ParryRate;
        public string ParryRateFormatted => $"{stats.ParryRate:F0}%";

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
