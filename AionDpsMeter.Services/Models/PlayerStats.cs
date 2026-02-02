namespace AionDpsMeter.Services.Models
{
    public sealed class PlayerStats
    {
        public long PlayerId { get; init; }
        public string PlayerName { get; init; } = string.Empty;
        public string? PlayerIcon { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string? ClassIcon { get; set; }
        public long TotalDamage { get; set; }
        public int HitCount { get; set; }
        public int CriticalHits { get; set; }
        public int BackAttacks { get; set; }
        public int PerfectHits { get; set; }
        public int DoubleDamageHits { get; set; }
        public int ParryHits { get; set; }
        public double DamagePerSecond { get; set; }
        public double DamagePercentage { get; set; }
        public DateTime FirstHit { get; set; }
        public DateTime LastHit { get; set; }

        public double CriticalRate => HitCount > 0 ? (double)CriticalHits / HitCount * 100 : 0;
        public double BackAttackRate => HitCount > 0 ? (double)BackAttacks / HitCount * 100 : 0;
        public double PerfectRate => HitCount > 0 ? (double)PerfectHits / HitCount * 100 : 0;
        public double DoubleDamageRate => HitCount > 0 ? (double)DoubleDamageHits / HitCount * 100 : 0;
        public double ParryRate => HitCount > 0 ? (double)ParryHits / HitCount * 100 : 0;
        public TimeSpan CombatDuration => LastHit > FirstHit ? LastHit - FirstHit : TimeSpan.Zero;
    }
}
